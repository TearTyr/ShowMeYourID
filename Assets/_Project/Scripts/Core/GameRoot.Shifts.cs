using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: starting and continuing a run, beginning a night, the checkpoint queue
    // set-up, verdict bookkeeping and Endless/Challenge scoring. Split out of GameRoot.cs so
    // each file reads on its own; it is all still one class.
    public partial class GameRoot
    {
        public void StartNewGame(GameMode mode)
        {
            // carry meta records across runs
            var old = Saves.Read();
            Records = old?.Records ?? new RecordsData();

            int seed = mode == GameMode.Normal ? 20261102 : (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            Session = new GameSession { Mode = mode, Seed = seed };
            _resolvedVisitorIndices.Clear();
            _runRng = new GameRng(seed ^ 0x5EED);
            _continuePending = false;
            EndlessProcessed = 0;

            if (mode == GameMode.Normal)
            {
                QueueSceneAction(() => BeginShift(1));
            }
            else
            {
                StartRunMode(mode, seed);
            }
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        // Endless/Challenge: run timer, on-demand visitor source and the mode's intro.
        private void StartRunMode(GameMode mode, int seed)
        {
            Session.RunTimer = EndlessRules.StartSeconds;
            Roster = new RosterFactory(seed);
            // run modes draw visitors on demand - the queue gets the same infinite source
            Queue = new CheckpointQueue();
            Queue.AttachRunSource(NextRunVisitor, BuildQueueTuning(), seed ^ 0x9E37);
            var intro = mode == GameMode.Endless
                ? new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISPATCH", Text = "Endless watch. They keep coming until the clock beats you. Every anomaly you turn away buys you seconds." },
                }
                : new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISPATCH", Text = "Challenge protocol: district rules change without warning, your nerves fray twice as fast, and every correct call is worth double." },
                };
            QueueSceneAction(() => PlayIntroThenCheckpoint(intro, null));
        }

        public void ContinueGame()
        {
            var save = Saves.Read();
            if (save == null)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "No save found."));
                SetState(AppViewState.MainMenu);
                return;
            }
            Session = save.Session;
            Records = save.Records;
            Settings.AdoptFromSaveIfFresh(save.Settings);
            _resolvedVisitorIndices.Clear();
            _runRng = new GameRng(Session.Seed ^ 0x5EED);
            _continuePending = true;
            if (Session.Mode == GameMode.Normal)
            {
                int shift = Mathf.Clamp(Session.CurrentShift, 1, ShiftPlan.TotalShifts);
                int resumeFrom = save.Session.VisitorIndexAtSave;
                QueueSceneAction(() =>
                {
                    bool isResume = (save.Session.QueueState != null && save.Session.QueueState.Started) || resumeFrom > 0;
                    BeginShift(shift, resumeFrom, isResume);
                    if (isResume) RemapAdmittedToRoster();
                });
                Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
            }
            else
            {
                // Run modes only persist records; restart the mode fresh.
                _continuePending = false;
                StartNewGame(Session.Mode);
            }
        }

        /// <summary>Normal mode: prepare and enter a shift. Roster generation replays earlier
        /// shifts on a fresh factory so the seeded RNG lands on the identical state (saves resume
        /// mid-story without storing the whole roster).</summary>
        public void BeginShift(int number, int resumeFromVisitor = 0, bool isResume = false)
        {
            _resumeFromVisitor = resumeFromVisitor;
            number = Mathf.Clamp(number, 1, ShiftPlan.TotalShifts);
            Session.CurrentShift = number;
            PrepareShiftRoster(number, isResume);
            // a resume keeps the loaded night state: presentation log, admitted students
            // and the anomaly pool all come FROM the save, not from a fresh shift setup
            if (!isResume)
            {
                Session.TonightLog.Clear();
                Session.AdmittedStudents.Clear();
                Session.BeginNight();            // pays last night's failure (less sanity, a stack, a roamer)
            }
            ShiftFailing = false;
            // a resume continues the saved night at its saved clock; a fresh shift starts at 22:00
            _shiftMinutes = isResume ? Session.ShiftClockMinutes : 0f;
            if (!isResume) Session.ShiftClockMinutes = 0f;
            SetupShiftQueue(number, resumeFromVisitor);
            PlayIntroThenCheckpoint(BuildShiftIntroLines(number), CurrentShiftDef);
            _resumeFromVisitor = 0;
        }

        // Roster generation replays earlier shifts on a fresh factory so the seeded RNG lands
        // on the identical state (saves resume mid-story without storing the whole roster).
        private void PrepareShiftRoster(int number, bool keepPool = false)
        {
            var plan = ShiftPlan.Build();
            Roster = new RosterFactory(Session.Seed);
            for (int i = 0; i < number - 1; i++) Roster.GenerateShift(plan[i]); // advance RNG deterministically
            CurrentShiftDef = (Config != null && Config.exhibitionMode && number == 1)
                ? ShiftPlan.ExhibitionDef() : plan[number - 1];
            CurrentRoster = Roster.GenerateShift(CurrentShiftDef);
            // one shared pool: this shift's anomalies, outstanding until resolved.
            // Kinds derive from seed^shift (not the shared stream) so a resumed night
            // rebuilds the IDENTICAL pool the save recorded.
            var kinds = new System.Collections.Generic.List<AnomalyBehaviorKind>();
            var krng = new GameRng(Session.Seed ^ (number * 7919));
            for (int i = 0; i < CurrentShiftDef.AnomalyCount; i++)
                kinds.Add((AnomalyBehaviorKind)krng.NextInt(0, 4));
            if (!keepPool) Session.Pool.Seed(kinds);
        }

        // Saved admitted lists ride as roster indices; re-attach ids/names once the roster exists.
        private void RemapAdmittedToRoster()
        {
            if (CurrentRoster == null) return;
            foreach (var st in Session.AdmittedStudents)
            {
                if (st.VisitorIndex < 0) continue;
                foreach (var v in CurrentRoster)
                    if (v.VisitorIndex == st.VisitorIndex) { st.Id = v.IdNumber; st.Name = v.ClaimedName; break; }
            }
        }

        // The night's opening cards: title, objective and the rules addendum (plus the
        // Night-5 administrator warning).
        private List<DialogueLine> BuildShiftIntroLines(int number)
        {
            var intro = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "NIGHT " + number + " - " + CurrentShiftDef.DateLabel.ToUpperInvariant(), Text = CurrentShiftDef.Title + ". " + CurrentShiftDef.Objective },
                new DialogueLine { Speaker = "PAPER NOTE", Text = CurrentShiftDef.RuleNoteAddendum },
            };
            if (number == 5)
            {
                intro.Add(new DialogueLine { Speaker = "???", Text = "Tonight the district sends an administrator to 'take control'. Check the credentials like you would check anyone else's. Trust the evidence." });
            }
            return intro;
        }

        /// <summary>Attach the fresh shift roster to a new queue. A pending CONTINUE restores
        /// the saved line instead (snapshot first; pre-queue saves fall back to the legacy
        /// visitorAtSave cursor). A brand-new shift always starts behind the sticky note.</summary>
        private void SetupShiftQueue(int shiftNumber, int resumeFromVisitor)
        {
            var snapshot = _continuePending ? Session.QueueState : null;
            _continuePending = false;
            Session.QueueState = null;              // the live queue is truth until the next PersistRun
            Session.QueueOverwhelmed = false;
            Queue = new CheckpointQueue();
            Queue.AttachRoster(CurrentRoster, BuildQueueTuning(), QueueSeed(shiftNumber));
            if (snapshot != null)
            {
                if (snapshot.HasProgress) Queue.RestoreFromSnapshot(snapshot, QueueSeed(shiftNumber));
                // a save made before the sticky note was clicked resumes as a fresh start
            }
            else if (resumeFromVisitor > 0)
            {
                Queue.RestoreLegacy(resumeFromVisitor);   // pre-queue save format
            }
        }

        private int QueueSeed(int shiftNumber) => Session.Seed ^ (shiftNumber * 0x9E37);

        private QueueTuning BuildQueueTuning()
        {
            var t = new QueueTuning();
            if (Config == null) return t;
            // Normal mode paces the night: a slow base clock that tightens every night and
            // hurries only while the window is busy. Endless/Challenge keep the fast race
            // clock and never hurry (their timer is the pressure).
            bool normal = Session != null && Session.Mode == GameMode.Normal;
            if (normal)
            {
                float scale = Mathf.Pow(Config.queueNightScale, Mathf.Max(0, Session.CurrentShift - 1));
                t.ArrivalMinSeconds = Config.queueArrivalMinSeconds * scale;
                t.ArrivalMaxSeconds = Config.queueArrivalMaxSeconds * scale;
                t.BusyHurryMultiplier = Config.queueBusyHurryMultiplier;
            }
            else
            {
                t.ArrivalMinSeconds = Config.queueRunMinSeconds;
                t.ArrivalMaxSeconds = Config.queueRunMaxSeconds;
            }
            t.PressureWarnAt = Config.queuePressureWarnAt;
            t.OverwhelmedAt = Config.queueOverwhelmedAt;
            t.MissingStudentChance = Config.missingStudentChance;
            t.MissingStudentLimit = Config.missingStudentLimit;
            return t;
        }

        /// <summary>The configured overwhelmed consequence: the waiting line
        /// overran the checkpoint. One-shot - the queue has already collapsed, so no
        /// arrivals follow and no second ending/penalty can fire.</summary>
        public void TriggerQueueOverwhelmed()
        {
            if (Session.QueueOverwhelmed || Session.RunFinished || State == AppViewState.Ending) return;
            Session.QueueOverwhelmed = true;
            Audio.PlaySfx("sfx_alarm", 1f);
            Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the line surges against the glass - too many, too fast*", 3.5f, null));
            Bus.Publish(GameEvent.Of(EventIds.Notification, "THE LINE HAS OVERRUN THE CHECKPOINT."));
            TriggerEnding(Session.EvaluateEnding());
        }

        private void PlayIntroThenCheckpoint(List<DialogueLine> intro, ShiftDef shift)
        {
            SetState(AppViewState.ShiftIntro);
            Bus.Publish(GameEvent.Of(EventIds.ShiftStarted, shift));
            ObjectiveText = shift != null ? shift.Objective :
                (Session.Mode == GameMode.Endless ? "Survive the clock. Reject every anomaly." : "Survive the clock under shifting district rules.");
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
            Audio.PlayMusic("checkpoint");
            Debug.Log("[Flow] intro shown (loading=" + (Loading != null) + " dialogue=" + (Dialogue != null) + ")");
            if (Dialogue == null) Debug.LogError("[GameRoot] intro: Dialogue view NULL - check scene bootstrap view creation.");
            if (Loading == null) Debug.LogError("[GameRoot] intro: Loading view NULL - no NIGHT backdrop will show.");
            if (Checkpoint == null) Debug.LogError("[GameRoot] intro: Checkpoint view NULL - the desk cannot start.");
            // the night title fades in slowly on the black card FIRST, and only then does the
            // dialogue box open over it (a click skips the title)
            System.Action startDialogue = () => PlayIntroDialogue(intro, shift);
            if (Loading != null)
                Loading.PlayNightTitle(
                    shift != null ? "NIGHT " + shift.Number
                        : (Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL"),
                    shift != null ? shift.DateLabel.ToUpperInvariant() : "HOLLOWBROOK COLLEGE - NIGHT CUSTODIAN PROGRAM",
                    startDialogue);
            else startDialogue();
        }

        private void PlayIntroDialogue(List<DialogueLine> intro, ShiftDef shift)
        {
            if (Dialogue != null)
                Dialogue.Play(intro, () =>
                {
                    if (VerboseFlow) Debug.Log("[Flow] intro callback fired");
                    EnterCheckpoint(shift);
                }, true);   // the box fades away before the booth appears
            else EnterCheckpoint(shift);
        }

        // The intro's end state, shared by the dialogue callback and the no-dialogue fallback.
        private void EnterCheckpoint(ShiftDef shift)
        {
            SetState(AppViewState.Checkpoint);
            if (Checkpoint != null) Checkpoint.BeginRun(shift);
            else Debug.LogError("[GameRoot] intro finished but Checkpoint is still NULL.");
            // the black lifts off the shut booth; the shutter opens when the first student arrives
            if (Loading != null) Loading.RevealDesk();
            Debug.Log("[Flow] intro done -> checkpoint state");
        }

        /// <summary>Called by the checkpoint after every stamped verdict.</summary>
        // Exactly-once per visitor per run: a verdict callback that fires twice (a re-entrant
        // banner, a resumed desk re-committing) must not double-count anything. The queue's
        // ResolutionPending already guards its own books; this guards the session counters.
        private readonly HashSet<int> _resolvedVisitorIndices = new HashSet<int>();

        public void OnVerdictResolved(VisitorData visitor, InspectionRules.Outcome outcome)
        {
            if (visitor == null || !_resolvedVisitorIndices.Add(visitor.VisitorIndex)) return;
            Session.RecordVerdict(outcome);
            Session.TonightLog.RecordFrom(visitor, "earlier tonight");
            _shiftMinutes += 240f / (CurrentRoster.Count + 1);

            if (Session.Mode == GameMode.Endless || Session.Mode == GameMode.Challenge)
                ApplyRunModeScoring(outcome);
            else
                Session.Score += outcome.Correct ? (outcome.WasAnomaly ? 100 : 50) : 0;

            // atmosphere reacts to breaches
            if (outcome.AnomalyBreach && !outcome.WasAdministrator)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "Something walked past you into the school."));
                if (Views != null) Views.NoteBreach();
            }

            // ending check - lose conditions can fire mid-shift (C3/C4)
            var eval = Session.EvaluateEnding();
            if (eval.Ending != EndingType.None)
            {
                TriggerEnding(eval);
            }
        }

        // Endless/Challenge bookkeeping per verdict: processed count, score, bought seconds
        // and the challenge mutation wave.
        private void ApplyRunModeScoring(InspectionRules.Outcome outcome)
        {
            Session.RunProcessed++;
            EndlessProcessed = Session.RunProcessed;
            Session.Score += ComputeRunScore(outcome);
            float delta = Session.ApplyRunTimer(outcome);
            if (HUD != null) HUD.Notify(delta >= 0 ? "+" + delta.ToString("0") + "s" : delta.ToString("0") + "s");
            if (Session.Mode == GameMode.Challenge && Session.RunProcessed % ChallengeRules.VisitorsPerRuleWave == 0)
                RollChallengeMutation();
        }

        private int ComputeRunScore(InspectionRules.Outcome o)
        {
            int baseScore = EndlessRules.ScoreFor(o, Session.Streak);
            if (Session.Mode == GameMode.Challenge) baseScore = (int)(baseScore * ChallengeRules.ScoreMultiplier);
            return baseScore;
        }

        private void RollChallengeMutation()
        {
            int wave = Session.RunProcessed / ChallengeRules.VisitorsPerRuleWave;
            var m = ChallengeRules.RollMutation(wave, _runRng);
            Session.ActiveMutationKey = m.Key;
            Bus.Publish(GameEvent.Of(EventIds.MutationChanged, m));
            Audio.PlaySfx("sfx_intercom");
            Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*intercom crackles: new district bulletin*", 3f, null));
        }

        /// <summary>Endless/Challenge run generation (per-visitor, on demand).</summary>
        public VisitorData NextRunVisitor()
        {
            if (Roster == null) Roster = new RosterFactory(Session.Seed);
            return Roster.GenerateRunVisitor(Session.RunProcessed);
        }
    }
}
