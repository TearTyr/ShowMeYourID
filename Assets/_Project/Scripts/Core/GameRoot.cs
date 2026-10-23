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
    /// <summary>Central game orchestrator (the "root"). Owns services, the run session and
    /// explicit references to every view — assigned once by CoreBootstrap + the scene bootstraps.
    /// Gameplay code never uses GameObject.Find; it goes through this class or the EventBus.</summary>
    public class GameRoot : MonoBehaviour
    {
        // Flip to false to silence the [Flow] breadcrumb logs once the boot flow is trusted.
        private const bool VerboseFlow = true;
        public static GameRoot Instance { get; private set; }

        /// <summary>Menu gate for Endless/Challenge (run modes). The systems are fully
        /// implemented and headless-tested, but the menu keeps them LOCKED placeholders
        /// until the team flips this to true (structure is ready for later — per current
        /// scope: New Game → Normal story only).</summary>
        public static bool RunModesUnlocked = false;

        // ---- services ----
        public EventBus Bus { get; private set; }
        public GameConfigSO Config { get; private set; }
        public SaveSystem Saves { get; private set; }
        public SettingsSystem Settings { get; private set; }
        public AudioDirector Audio { get; private set; }

        // ---- run state ----
        public GameSession Session { get; private set; } = new GameSession();
        public RecordsData Records { get; private set; } = new RecordsData();
        public RosterFactory Roster { get; private set; }
        public List<VisitorData> CurrentRoster { get; private set; } = new List<VisitorData>();
        public ShiftDef CurrentShiftDef { get; private set; }
        public AppViewState State { get; private set; } = AppViewState.Boot;
        public bool Paused { get; private set; }
        // The classroom being peeked into from the doorway (null when not peeking).
        // Set by EnterRoomPeek; cleared by ViewManager.ExitPeek when the step-back ends.
        public RoomId? PeekRoom { get; internal set; }
        public int EndlessProcessed { get; private set; }

        /// <summary>The checkpoint arrival queue — persistent run state. Owned here (not by
        /// the desk view) so the waiting line, roster cursor, arrival timer and start gate
        /// survive scene reloads; the desk view is only its presenter and ticker. The clock
        /// runs while the desk is the active screen — patrol explicitly pauses the line.</summary>
        public CheckpointQueue Queue { get; private set; } = new CheckpointQueue();
        private bool _continuePending;

        // ---- views (explicit references, set by the per-scene bootstraps) ----
        public Camera MainCamera;
        public ViewManager Views;
        public CheckpointView Checkpoint;
        public HUDView HUD;
        public DialogueView Dialogue;
        public MainMenuView MainMenu;
        public ModeSelectView ModeSelect;
        public SettingsView SettingsScreen;
        public PauseView PauseScreen;
        public ControlsView ControlsScreen;
        public CreditsView CreditsScreen;
        public MapView MapScreen;
        public LoadingView Loading;
        public EndingView Ending;
        public ShiftResultsView ShiftResults;
        public ConfirmationPopup Confirm;
        public CaptionBar Captions;
        public Hollowbrook.World.SecurityRoomView SecurityRoom;
        public Hollowbrook.World.StudentSpawner Students;
        public SanityController SanityCtl;
        public ScareSystem Scares;
        public HallucinationSystem Hallucinations;

        /// <summary>True while a full-screen sheet (Options/Controls/Credits) owns the
        /// screen: Escape belongs to that sheet then, and the in-game Esc-unpause hotkeys
        /// must stand down or they would tear the player out of the pause menu.</summary>
        public bool BlockingOverlayOpen =>
            (SettingsScreen != null && SettingsScreen.gameObject.activeInHierarchy) ||
            (ControlsScreen != null && ControlsScreen.gameObject.activeInHierarchy) ||
            (CreditsScreen != null && CreditsScreen.gameObject.activeInHierarchy) ||
            (Confirm != null && Confirm.IsOpen);

        private System.Action _queuedSceneAction;
        public void QueueSceneAction(System.Action a)
        {
            _queuedSceneAction = a;
            _queuedAt = Time.unscaledTime;
            _handshakeArmed = true;
        }

        public void ClearQueuedSceneAction() { _queuedSceneAction = null; _handshakeArmed = false; }

        private float _queuedAt;
        private bool _handshakeArmed;

        // Safety net: if a scene load never runs its bootstrap (missing bootstrap component,
        // scene not in Build Settings), the queued start action is never consumed and the
        // player would stare at a dead screen. After the window we say exactly that and go
        // back to the menu. Do not remove: this is the only detector for that failure.
        private void CheckSceneHandshake()
        {
            if (!_handshakeArmed || _queuedSceneAction == null) return;
            if (Time.unscaledTime - _queuedAt < HandshakeTimeoutSeconds) return;
            _handshakeArmed = false;
            _queuedSceneAction = null;
            Debug.LogError("[GameRoot] scene handshake FAILED: the queued start action was never " +
                "consumed. Usual causes: target scene not enabled in Build Settings, or its " +
                "bootstrap component missing / stale script GUID (Tools/generate_meta.py pins them).");
            Bus.Publish(GameEvent.Of(EventIds.Notification,
                "The game scene failed to start. Check Build Settings / scene bootstrap."));
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Menu);
        }

        private const float HandshakeTimeoutSeconds = 3.5f;

        public void RunQueuedSceneAction()
        {
            Debug.Log("[Flow] queued action consumed in scene '" +
                      UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "'");
            var a = _queuedSceneAction;
            _queuedSceneAction = null;
            a?.Invoke();
        }

        // shift clock (22:00 → 02:00): real time x Config rate in any in-shift scene,
        // plus a bump per processed visitor. Atmosphere only — dawn never ends a shift early.
        private float _shiftMinutes;
        /// <summary>Last published objective, so a HUD built after the publish (scene load)
        /// can show it immediately instead of staying blank.</summary>
        public string ObjectiveText { get; private set; } = "";

        /// <summary>Advance the in-story shift clock. Public so headless suites can drive it
        /// deterministically; Update is the only real-time caller.</summary>
        public void AdvanceShiftClock(float minutes) { _shiftMinutes += minutes; }
        public string ClockString
        {
            get
            {
                int total = (22 * 60 + (int)_shiftMinutes) % (24 * 60);
                return (total / 60).ToString("D2") + ":" + (total % 60).ToString("D2");
            }
        }

        private GameRng _runRng;
        private int _resumeFromVisitor;

        // ---------------------------------------------------------------- lifecycle

        public void InitServices(EventBus bus, GameConfigSO config, SaveSystem saves,
            SettingsSystem settings, AudioDirector audio)
        {
            Instance = this;
            Bus = bus; Config = config; Saves = saves; Settings = settings; Audio = audio;
            DontDestroyOnLoad(gameObject);
            // One boot-time application of the stored display preference. Missing or unusable
            // preferences are cleared inside; the launcher default otherwise stands.
            settings.ApplyStoredDisplay();
            Bus.Subscribe(EventIds.SettingsChanged, e =>
            {
                UIFactory.ApplyTextScale(Settings.TextScaleMultiplier);
                if (HUD != null) HUD.Refresh();
            });
        }

        private void Update()
        {
            CheckSceneHandshake();
            if (Paused || Session == null) return;
            TickShiftClock();
            TickRunCountdown();
            TickSanity();
        }

        // The shift clock runs on patrol too (scope: patrol has stakes). Single source:
        // real time x Config rate, in whichever scene the night is happening.
        private void TickShiftClock()
        {
            if (State == AppViewState.Checkpoint || State == AppViewState.Exploration ||
                State == AppViewState.SecurityRoom || State == AppViewState.Map ||
                State == AppViewState.RoomPeek)
            {
                AdvanceShiftClock(Time.deltaTime * (Config != null ? Config.clockMinutesPerSecond : 0.25f));
            }
        }

        // Endless / Challenge countdown — only while actually inspecting (and only
        // once the shift has begun; the sticky note must not burn run time).
        private void TickRunCountdown()
        {
            bool runMode = Session.Mode == GameMode.Endless || Session.Mode == GameMode.Challenge;
            if (!runMode || State != AppViewState.Checkpoint) return;
            if (Dialogue == null || Dialogue.IsActive) return;
            if (Queue == null || !Queue.Started) return;
            Session.RunTimer -= Time.deltaTime;
            Bus.Publish(GameEvent.Of(EventIds.RunTimerTick, Session.RunTimer));
            if (Session.RunTimer <= 0f)
            {
                Session.RunTimer = 0f;
                FinishRun();
            }
        }

        // Sanity is atmosphere: it ticks at the desk and on patrol, nowhere else.
        private void TickSanity()
        {
            if (SanityCtl != null && (State == AppViewState.Checkpoint || State == AppViewState.Exploration))
                SanityCtl.Tick(State == AppViewState.Checkpoint);
        }

        // ---------------------------------------------------------------- flow API

        public void SetState(AppViewState s)
        {
            if (VerboseFlow) Debug.Log("[Flow] SetState " + s);
            State = s;
            Bus.Publish(GameEvent.Of(EventIds.StateChanged, s));
            ApplyVisibility(s);
        }

        private void ApplyVisibility(AppViewState s)
        {
            if (VerboseFlow) Debug.Log("[Flow] ApplyVisibility " + s);
            bool menu = s == AppViewState.MainMenu || s == AppViewState.ModeSelect;
            if (MainMenu != null) { if (s == AppViewState.MainMenu) MainMenu.Show(); else MainMenu.Hide(); }
            if (ModeSelect != null) { if (s == AppViewState.ModeSelect) ModeSelect.Show(); else ModeSelect.Hide(); }

            if (SecurityRoom != null) { if (s == AppViewState.SecurityRoom) SecurityRoom.Show(); else SecurityRoom.Hide(); }
            bool inRun = s == AppViewState.Checkpoint || s == AppViewState.Exploration ||
                         s == AppViewState.ShiftIntro || s == AppViewState.AdminEncounter ||
                         s == AppViewState.Map || s == AppViewState.ShiftResults ||
                         s == AppViewState.SecurityRoom || s == AppViewState.RoomPeek;
            if (HUD != null) { if (inRun) HUD.Show(); else HUD.Hide(); }
            if (Checkpoint != null)
            {
                if (s == AppViewState.Checkpoint || s == AppViewState.AdminEncounter) Checkpoint.Show();
                else Checkpoint.Hide();
            }
            if (Views != null)
            {
                // peek keeps the world on screen: only the presentation changes
                if (s == AppViewState.Exploration || s == AppViewState.RoomPeek) Views.ShowWorld();
                else Views.HideWorld();
            }
            if (MapScreen != null) { if (s == AppViewState.Map) MapScreen.Show(); else MapScreen.Hide(); }
            if (ShiftResults != null) { if (s == AppViewState.ShiftResults) ShiftResults.Show(); else ShiftResults.Hide(); }
            if (Ending != null) { if (s == AppViewState.Ending || s == AppViewState.EndlessResults) Ending.Show(); else Ending.Hide(); }
            if (Captions != null) { if (menu) Captions.Hide(); else Captions.Show(); }
        }

        public void BootToMainMenu()
        {
            Session = new GameSession();
            Queue = new CheckpointQueue();
            SetState(AppViewState.MainMenu);
            Audio.PlayMusic("menu");
        }

        public bool HasSave()
        {
            var s = Saves.Read();
            return s != null && s.Session.Mode == GameMode.Normal && !s.Session.RunFinished
                && s.Session.CompletedShifts < ShiftPlan.TotalShifts;
        }

        public void StartNewGame(GameMode mode)
        {
            // carry meta records across runs
            var old = Saves.Read();
            Records = old?.Records ?? new RecordsData();

            int seed = mode == GameMode.Normal ? 20261102 : (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            Session = new GameSession { Mode = mode, Seed = seed };
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
            // run modes draw visitors on demand — the queue gets the same infinite source
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
            }
            _shiftMinutes = 0f;
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
                new DialogueLine { Speaker = "NIGHT " + number + " — " + CurrentShiftDef.DateLabel.ToUpperInvariant(), Text = CurrentShiftDef.Title + ". " + CurrentShiftDef.Objective },
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

        /// <summary>The configured overwhelmed consequence (Phase 9.5): the waiting line
        /// overran the checkpoint. One-shot — the queue has already collapsed, so no
        /// arrivals follow and no second ending/penalty can fire.</summary>
        public void TriggerQueueOverwhelmed()
        {
            if (Session.QueueOverwhelmed || Session.RunFinished || State == AppViewState.Ending) return;
            Session.QueueOverwhelmed = true;
            Audio.PlaySfx("sfx_alarm", 1f);
            Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*the line surges against the glass — too many, too fast*", 3.5f, null));
            Bus.Publish(GameEvent.Of(EventIds.Notification, "THE LINE HAS OVERRUN THE CHECKPOINT."));
            TriggerEnding(Session.EvaluateEnding());
        }

        private void PlayIntroThenCheckpoint(List<DialogueLine> intro, ShiftDef shift)
        {
            if (Loading != null)
                Loading.Show(shift != null
                    ? "SHIFT " + shift.Number + "  ·  " + shift.DateLabel
                    : (Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL"));
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
            if (Dialogue != null)
                Dialogue.Play(intro, () =>
                {
                    if (VerboseFlow) Debug.Log("[Flow] intro callback fired");
                    EnterCheckpoint(shift);
                });
            else EnterCheckpoint(shift);
        }

        // The intro's end state, shared by the dialogue callback and the no-dialogue fallback.
        private void EnterCheckpoint(ShiftDef shift)
        {
            if (Loading != null) Loading.Hide();
            SetState(AppViewState.Checkpoint);
            if (Checkpoint != null) Checkpoint.BeginRun(shift);
            else Debug.LogError("[GameRoot] intro finished but Checkpoint is still NULL.");
            Debug.Log("[Flow] intro done -> checkpoint state");
        }

        /// <summary>Called by the checkpoint after every stamped verdict.</summary>
        public void OnVerdictResolved(VisitorData visitor, InspectionRules.Outcome outcome)
        {
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

            // ending check — lose conditions can fire mid-shift (C3/C4)
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

        // ---- exploration ----

        public void EnterExploration(RoomId room = RoomId.Hallway)
        {
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Exploration);
                Bus.Publish(GameEvent.Of(EventIds.PatrolStarted));
                ObjectiveText = "Night " + Session.CurrentShift + " — patrol: check the rooms, follow the chain, return to the checkpoint before dawn.";
                Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
                if (Views != null) Views.EnterRoom(room);
                Audio.PlayMusic(MusicForRoom(room));
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.School);
        }

        public static string MusicForRoom(RoomId room)
        {
            switch (room)
            {
                case RoomId.Classroom1: return "classroom";
                case RoomId.Classroom2: return "darkroom";
                default: return "hallway";
            }
        }

        /// <summary>Flashlight peek into a classroom: the player never walks inside — the
        /// camera frames the room and a cursor-locked light circle reveals it. Classrooms
        /// are reachable ONLY this way; the hallway and office keep their normal paths.</summary>
        public void EnterRoomPeek(RoomId room)
        {
            if (Views == null) return;
            PeekRoom = room;
            SetState(AppViewState.RoomPeek);
            Views.EnterPeek(room);
        }

        /// <summary>Step back from a peek into the hallway. ViewManager.ExitPeek runs the
        /// fade, clears PeekRoom and returns the state to Exploration itself.</summary>
        public void ExitRoomPeek()
        {
            if (Views == null) PeekRoom = null;
            else Views.ExitPeek();
        }

        public void ReturnToCheckpoint()
        {
            QueueSceneAction(() =>
            {
                Bus.Publish(GameEvent.Of(EventIds.PatrolEnded));
                SetState(AppViewState.Checkpoint);
                Audio.PlayMusic("checkpoint");
                if (Checkpoint != null) Checkpoint.ResumeRun();
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        /// <summary>Classroom security-office door (Scene B prototype).</summary>
        public void OpenSecurityRoom()
        {
            SetState(AppViewState.SecurityRoom);
            ObjectiveText = "Night " + Session.CurrentShift + " — office: late list and expiry rule. Verdicts count.";
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
            Audio.PlayMusic("darkroom");
        }

        public void OpenMap()
        {
            if (MapScreen == null)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "The map is pinned up at the checkpoint."));
                return;
            }
            if (State != AppViewState.Exploration && State != AppViewState.Checkpoint) return;
            _preMapState = State;
            SetState(AppViewState.Map);
            Audio.PlaySfx("sfx_ui_open");
        }

        private AppViewState _preMapState = AppViewState.Checkpoint;

        public void CloseMap()
        {
            Audio.PlaySfx("sfx_ui_close");
            SetState(_preMapState == AppViewState.Map ? AppViewState.Checkpoint : _preMapState);
        }

        public void TravelViaMap(RoomId room)
        {
            CloseMap();
            if (room == RoomId.Security) ReturnToCheckpoint();
            else EnterExploration(room);
        }

        // ---- shift / run completion ----

        public void FinishShift()
        {
            Bus.Publish(GameEvent.Of(EventIds.ShiftEnded, CurrentShiftDef));
            Session.CompleteShift();
            // the finished shift's line must never leak into the next one: the queue
            // resets here, so the save written below resumes as "next shift, not started"
            Queue = new CheckpointQueue();
            Session.QueueState = null;
            PersistRun();

            var eval = Session.EvaluateEnding();
            if (eval.Ending == EndingType.None && Config != null && Config.exhibitionMode && Session.CompletedShifts >= 1)
            {
                Session.Breaches += Session.Pool.Pending;         // booth night closes its books too
                eval = EndingRules.EvaluateExhibition(Session);   // booth cut closes after one night
            }
            if (eval.Ending != EndingType.None) { TriggerEnding(eval); return; }

            SetState(AppViewState.ShiftResults);
            if (ShiftResults != null) ShiftResults.Populate(Session, CurrentShiftDef);
            Audio.PlayMusic("");
        }

        public void AdvanceToNextShift()
        {
            if (Session.CurrentShift > ShiftPlan.TotalShifts) { BootToMainMenu(); return; }
            BeginShift(Session.CurrentShift);
        }

        public void FinishRun() // endless/challenge time-out
        {
            if (Session.Mode == GameMode.Endless && Session.Score > Records.EndlessBest) Records.EndlessBest = Session.Score;
            if (Session.Mode == GameMode.Challenge && Session.Score > Records.ChallengeBest) Records.ChallengeBest = Session.Score;
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.EndlessResults);
            if (Ending != null) Ending.PopulateRunResults(Session, Records);
            Audio.PlayMusic(Session.Score > 0 ? "end_win" : "end_lose");
        }

        public void TriggerEnding(EndingRules.Evaluation eval)
        {
            string key = eval.Ending == EndingType.Secret ? "secret" : (eval.Ending == EndingType.Win ? "win" : "lose");
            if (eval.ReasonKey != "exhibition" && !Records.EndingsSeen.Contains(key)) Records.EndingsSeen.Add(key);
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.Ending);
            if (Ending != null) Ending.Populate(eval, Session, Records);
            Bus.Publish(GameEvent.Of(EventIds.EndingReached, eval));
            Audio.PlayMusic("end_" + key);
            Audio.PlaySfx(eval.Ending == EndingType.Lose ? "sfx_alarm" : "sfx_bell");
        }

        /// <summary>F2×3 data reset: deletes the save file (run progress, records, endings
        /// seen) and clears in-memory run state. Accessibility/local settings are preserved
        /// on purpose (PlayerPrefs). Callers should confirm with the player first.</summary>
        public void ResetAllData()
        {
            Saves.Delete();
            Records = new RecordsData();
            Session = new GameSession();
            Bus.Publish(GameEvent.Of(EventIds.Notification, "All save data erased."));
            Debug.Log("[GameRoot] data reset complete (settings preserved).");
        }

        /// <summary>The single confirmation + reset path shared by the F2 x3 shortcut and the
        /// Options > RESET DATA button, so both can never drift apart. Settings (volumes,
        /// display, accessibility) live in PlayerPrefs and are deliberately untouched.</summary>
        public void RequestDataReset(System.Action afterReset = null)
        {
            if (Confirm == null) { ResetAllData(); if (afterReset != null) afterReset(); return; }
            Confirm.Ask("ERASE ALL DATA?",
                "Deletes your save: shift progress, records and unlocked endings.\n" +
                "Volumes, display and accessibility settings are kept.",
                () =>
                {
                    ResetAllData();
                    Audio.PlaySfx("sfx_static", 0.3f);
                    if (afterReset != null) afterReset();
                });
        }

        /// <summary>Save at safe boundaries (shift end / run end / quit). Corrupt-safe by design.</summary>
        public void PersistRun()
        {
            if (Queue != null && Queue.IsAttached)
            {
                Session.QueueState = Queue.BuildSnapshot();
                Session.QueueOverwhelmed = Queue.Collapsed;
            }
            var save = new SaveGame
            {
                Session = Session,
                Settings = Settings.Current.Clone(),
                Records = Records,
                SavedAtLabel = "Night " + Session.CurrentShift + " · " + ClockString,
            };
            save.Session.VisitorIndexAtSave = Queue != null && Queue.IsAttached && !Queue.InfiniteSource
                ? Queue.RosterCursor : 0;
            Saves.Write(save);
        }

        // ---- overlay sub-screens (settings / controls / credits) ----
        // These overlay the current context without changing State, so closing them returns
        // exactly where the player was (main menu, pause, checkpoint…).

        public void OpenSettings()
        {
            if (Paused && PauseScreen != null) PauseScreen.Hide();
            if (SettingsScreen != null) SettingsScreen.Show();
        }

        public void CloseSettings()
        {
            if (SettingsScreen != null) SettingsScreen.Hide();
            if (Paused && PauseScreen != null) PauseScreen.Show();
        }

        public void OpenControls()
        {
            if (Paused && PauseScreen != null) PauseScreen.Hide();
            if (ControlsScreen != null) ControlsScreen.Show();
        }

        public void CloseControls()
        {
            if (ControlsScreen != null) ControlsScreen.Hide();
            if (Paused && PauseScreen != null) PauseScreen.Show();
        }

        public void OpenCredits()
        {
            if (CreditsScreen != null) CreditsScreen.Show();
        }

        public void CloseCredits()
        {
            if (CreditsScreen != null) CreditsScreen.Hide();
        }

        // ---- pause / quit ----

        public void TogglePause(bool? force = null)
        {
            bool target = force ?? !Paused;
            if (target == Paused) return;
            bool allowed = State == AppViewState.Checkpoint || State == AppViewState.Exploration ||
                           State == AppViewState.AdminEncounter || State == AppViewState.Map ||
                           State == AppViewState.SecurityRoom || State == AppViewState.RoomPeek;
            if (target && !allowed) return;
            Paused = target;
            Time.timeScale = Paused ? 0f : 1f;
            if (PauseScreen != null) { if (Paused) PauseScreen.Show(); else PauseScreen.Hide(); }
            Bus.Publish(GameEvent.Of(EventIds.PauseToggled, Paused));
            Audio.PlaySfx(Paused ? "sfx_ui_open" : "sfx_ui_close");
        }

        public void QuitToMenu(bool saveFirst)
        {
            if (SettingsScreen != null) SettingsScreen.Hide();
            if (ControlsScreen != null) ControlsScreen.Hide();
            if (CreditsScreen != null) CreditsScreen.Hide();
            if (Confirm != null) Confirm.Hide();
            if (Dialogue != null) Dialogue.Hide();
            if (Paused) TogglePause(false);
            if (saveFirst && Session != null && Session.Mode == GameMode.Normal && !Session.RunFinished &&
                Session.CompletedShifts < ShiftPlan.TotalShifts && State != AppViewState.MainMenu)
            {
                PersistRun();
            }
            if (Views != null) Views.HideWorld();
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Menu);
        }

        /// <summary>True while a run is actually in progress — the menu's blank session
        /// must never be persisted over a real save (issue #1: EXIT on the menu wiped saves).</summary>
        private bool InRunProgress() =>
            Session != null && Session.Mode == GameMode.Normal && !Session.RunFinished &&
            Session.CompletedShifts < ShiftPlan.TotalShifts &&
            State != AppViewState.MainMenu && State != AppViewState.ModeSelect && State != AppViewState.Boot;

        public void QuitApplication()
        {
            if (InRunProgress()) PersistRun();
            if (Settings != null) Settings.Persist();
            Application.Quit();
        }

        // Alt+F4 / window close: same guard as QuitApplication, no blank-save overwrite
        private void OnApplicationQuit()
        {
            if (InRunProgress()) PersistRun();
            if (Settings != null) Settings.Persist();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
            // the procedural atlases are native allocations: release them with the core
            Hollowbrook.Bootstrap.ProceduralArt.ClearCache();
            Hollowbrook.Bootstrap.ProceduralAudio.ClearCache();
        }
    }
}
