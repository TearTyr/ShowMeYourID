using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Scenes;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: starting and continuing a run, beginning a night, the checkpoint queue
    // set-up, verdict bookkeeping. Split out of GameRoot.cs so
    // each file reads on its own; it is all still one class.
    public partial class GameRoot
    {
        public void StartNewGame(GameMode mode)
        {
            // carry meta records across runs
            var old = Saves.Read();
            Records = old?.Records ?? new RecordsData();

            int seed = GameRules.Current.NewRunSeed();
            Session = new GameSession { Mode = mode, Seed = seed };
            Queue = new CheckpointQueue();   // the last run's line must never be saved into this one
            _resolvedVisitorIndices.Clear();
            _continuePending = false;

            // the Challenge is night 6 on its own: it never touches the story save (see PersistRun)
            QueueSceneAction(() => BeginShift(mode == GameMode.Challenge ? ShiftPlan.Night6Number : 1));
            Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Checkpoint);
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
            // the game closed while the lost-hunt scene played: that night is lost, so play it again
            if (save.Session.Hunt == HuntState.Lost)
            {
                var start = Saves.ReadNightStart();
                if (start != null) save = start; else save.Session.RewindNight();
            }
            Session = save.Session;
            Records = save.Records;
            Queue = new CheckpointQueue();   // the failed attempt's line must never reach the save
            Settings.AdoptFromSaveIfFresh(save.Settings);
            _resolvedVisitorIndices.Clear();
            _continuePending = true;
            int shift = Mathf.Clamp(Session.CurrentShift, 1, ShiftPlan.TotalShifts);
            int resumeFrom = save.Session.VisitorIndexAtSave;
            QueueSceneAction(() =>
            {
                bool isResume = (save.Session.QueueState != null && save.Session.QueueState.Started) || resumeFrom > 0;
                BeginShift(shift, resumeFrom, isResume);
                if (isResume) RemapAdmittedToRoster();
            });
            Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Checkpoint);
        }

        /// <summary>Normal mode: prepare and enter a shift. Roster generation replays earlier
        /// shifts on a fresh factory so the seeded RNG lands on the identical state (saves resume
        /// mid-story without storing the whole roster).</summary>
        public void BeginShift(int number, int resumeFromVisitor = 0, bool isResume = false)
        {
            _resumeFromVisitor = resumeFromVisitor;
            number = Mathf.Clamp(number, 1, ShiftPlan.LastNight(Session.Mode));
            string penaltyNote = isResume ? null : PenaltyNote();   // read before BeginNight pays and clears it
            Session.CurrentShift = number;
            PrepareShiftRoster(number, isResume);
            // a resume keeps the loaded night state: presentation log, admitted students
            // and the anomaly pool all come FROM the save, not from a fresh shift setup
            if (!isResume)
            {
                Session.TonightLog.Clear();
                Session.AdmittedStudents.Clear();
                Session.ClearedRooms = 0;
                Session.PeekAnomaliesGone = 0;
                Session.BeginNight();            // pays last night's failure (less sanity, a stack, a roamer)
            }
            ShiftFailing = false;
            // a resume continues the saved night at its saved clock; a fresh shift starts at 22:00
            _shiftMinutes = isResume ? Session.ShiftClockMinutes : 0f;
            if (!isResume) Session.ShiftClockMinutes = 0f;
            SetupShiftQueue(number, resumeFromVisitor);
            RememberNightStart(isResume);   // after the queue: this saves, and only tonight's queue may be saved
            PlayIntroThenCheckpoint(BuildShiftIntroLines(number, penaltyNote), CurrentShiftDef);
            _resumeFromVisitor = 0;
        }

        // Roster generation replays earlier shifts on a fresh factory so the seeded RNG lands
        // on the identical state (saves resume mid-story without storing the whole roster).
        private void PrepareShiftRoster(int number, bool keepPool = false)
        {
            var plan = ShiftPlan.Build();
            Roster = new RosterFactory(Session.Seed);
            for (int i = 0; i < number - 1; i++) Roster.GenerateShift(plan[i]); // advance RNG deterministically
            CurrentShiftDef = number == ShiftPlan.Night6Number ? ShiftPlan.Night6Def() : plan[number - 1];
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

        // What a failed night costs tonight, said out loud in the intro. Null after a clean night.
        internal string PenaltyNote()
        {
            int severity = Session.PendingPenaltySeverity;
            if (severity <= 0) return null;
            int loss = Mathf.RoundToInt(ShiftFailureRules.StartSanityLoss(severity, Session.PendingPenaltyNight));
            string cause = Session.LastShiftFailure == ShiftFailure.Blackout
                ? "You blacked out at your post, so the night starts again."
                : "You clocked out with an anomaly let in.";
            return cause + " Tonight you start with " + loss + " less sanity, and something is already walking the halls.";
        }

        // The night's opening cards: title, objective and the rules addendum (plus the
        // Night-5 administrator warning).
        private List<DialogueLine> BuildShiftIntroLines(int number, string penaltyNote)
        {
            var intro = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "NIGHT " + number + " - " + CurrentShiftDef.DateLabel.ToUpperInvariant(), Text = CurrentShiftDef.Title + ". " + CurrentShiftDef.Objective },
                new DialogueLine { Speaker = "PAPER NOTE", Text = CurrentShiftDef.RuleNoteAddendum },
            };
            if (!string.IsNullOrEmpty(penaltyNote))
                intro.Add(new DialogueLine { Speaker = "LAST NIGHT", Text = penaltyNote });
if (!string.IsNullOrEmpty(CurrentShiftDef.IntroWarning))
                intro.Add(new DialogueLine { Speaker = "???", Text = CurrentShiftDef.IntroWarning });
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
            // The night is paced by a slow base clock that tightens every night and
            // hurries only while the window is busy.
            float scale = Mathf.Pow(Config.queueNightScale, Mathf.Max(0, Session.CurrentShift - 1));
            t.ArrivalMinSeconds = Config.queueArrivalMinSeconds * scale;
            t.ArrivalMaxSeconds = Config.queueArrivalMaxSeconds * scale;
            t.BusyHurryMultiplier = Config.queueBusyHurryMultiplier;
            t.NextStudentSeconds = Config.nextStudentSeconds;
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
            ObjectiveText = shift.Objective;
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
            Audio.PlayMusic("checkpoint");
            Debug.Log("[Flow] intro shown (loading=" + (Loading != null) + " dialogue=" + (Dialogue != null) + ")");
            if (Dialogue == null) Debug.LogError("[GameRoot] intro: Dialogue view NULL - check the scene set up.");
            if (Loading == null) Debug.LogError("[GameRoot] intro: Loading view NULL - no NIGHT backdrop will show.");
            if (Checkpoint == null) Debug.LogError("[GameRoot] intro: Checkpoint view NULL - the desk cannot start.");
            // the night title fades in slowly on the black card FIRST, and only then does the
            // dialogue box open over it (a click skips the title)
            System.Action startDialogue = () => PlayIntroDialogue(intro, shift);
            if (Loading != null)
                Loading.PlayNightTitle("NIGHT " + shift.Number, shift.DateLabel.ToUpperInvariant(), startDialogue);
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
    }
}
