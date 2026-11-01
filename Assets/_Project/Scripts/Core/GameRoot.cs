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
    /// <summary>Central game orchestrator (the "root"). Owns services, the run session and
    /// explicit references to every view - assigned once by GameCore + the scene scripts.
    /// Gameplay code never uses GameObject.Find; it goes through this class or the EventBus.</summary>
    public partial class GameRoot : MonoBehaviour
    {
        // Flip to false to silence the [Flow] breadcrumb logs once the boot flow is trusted.
        private const bool VerboseFlow = true;
        public static GameRoot Instance { get; private set; }

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

        /// <summary>The checkpoint arrival queue - persistent run state. Owned here (not by
        /// the desk view) so the waiting line, roster cursor, arrival timer and start gate
        /// survive scene reloads; the desk view is only its presenter and ticker. The clock
        /// runs while the desk is the active screen - patrol explicitly pauses the line.</summary>
        public CheckpointQueue Queue { get; private set; } = new CheckpointQueue();
        private bool _continuePending;
        // The save as it was when the night began. A night that is failed (blackout, lost hunt) goes
        // back to this, so Continue and RETRY play the same night again instead of moving on.
        private SaveGame _nightStart;

        // ---- views (explicit references, set by the per-scene scripts) ----
        public Camera MainCamera;
        public ViewManager Views;
        public CheckpointView Checkpoint;
        public HUDView HUD;
        public DialogueView Dialogue;
        public MainMenuView MainMenu;
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

        // Safety net: if a scene load never runs its scene script (missing scene component,
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
                "scene component missing / stale script GUID (Tools/generate_meta.py pins them).");
            Bus.Publish(GameEvent.Of(EventIds.Notification,
                "The game scene failed to start. Check Build Settings / the scene script."));
            Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Menu);
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
        // plus a bump per processed visitor. Atmosphere only - dawn never ends a shift early.
        private float _shiftMinutes;
        /// <summary>One frame's worth of simulated time. Clamped so a stall (tab-out, GC hitch,
        /// debugger) can never fast-forward the night or burn run-timer seconds in one jump:
        /// pause/resume and focus loss already freeze the clock, this covers the wake-up spike.</summary>
        public static float ClampedDelta => Mathf.Min(Time.deltaTime, 0.5f);
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

        private int _resumeFromVisitor;

        // ---------------------------------------------------------------- lifecycle

        public void InitServices(EventBus bus, GameConfigSO config, SaveSystem saves,
            SettingsSystem settings, AudioDirector audio)
        {
            Instance = this;
            Bus = bus; Config = config; Saves = saves; Settings = settings; Audio = audio;
            if (config != null) GameRules.Current = config.rules;
            // (while the editor sets a scene up there is no game to keep alive, and no screen to resize)
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            // One boot-time application of the stored display preference. Missing or unusable
            // preferences are cleared inside; the launcher default otherwise stands.
            if (Application.isPlaying) settings.ApplyStoredDisplay();
            Bus.Subscribe(EventIds.SettingsChanged, e =>
            {
                UIFactory.ApplyTextScale(Settings.TextScaleMultiplier);
                if (HUD != null) HUD.Refresh();
            });
        }

        // Tab-out / alt-tab: the OS stops painting us, so the first frame back carries a huge
        // deltaTime. Pause the moment focus goes instead of letting the night jump forward -
        // only during a live run, never from menus, and never while already paused.
        private void OnApplicationFocus(bool focused)
        {
            if (focused || Paused || Session == null) return;
            if (State == AppViewState.MainMenu) return;
            TogglePause(true);
        }

        private void Update()
        {
            CheckSceneHandshake();
            if (Paused || Session == null) return;
            TickShiftClock();
            TickSanity();
        }

        // The shift clock runs on patrol too (scope: patrol has stakes). Single source:
        // real time x Config rate, in whichever scene the night is happening.
        private void TickShiftClock()
        {
            if (State == AppViewState.Checkpoint || State == AppViewState.Exploration ||
                State == AppViewState.Map || State == AppViewState.RoomPeek)
            {
                AdvanceShiftClock(ClampedDelta * (Config != null ? Config.clockMinutesPerSecond : 0.25f));
            }
        }

        // Sanity ticks at the desk, on patrol and in a peek, nowhere else.
        private void TickSanity()
        {
            if (SanityCtl == null) return;
            if (State == AppViewState.Checkpoint) SanityCtl.Tick(true);
            else if (State == AppViewState.Exploration || State == AppViewState.RoomPeek) SanityCtl.Tick(false);
        }

        /// <summary>The administrator's nights (5 and 6): sanity never rests there.</summary>
        public bool AdministratorNight => CurrentShiftDef != null && CurrentShiftDef.HasAdministrator;

        /// <summary>Sanity drains all the time, wherever the player is, from the moment the administrator
        /// matters: night 6 has only him, so it is always on there. Night 5 has students first, so it starts
        /// when he is at the window or the hunt is on (the students before him use the normal desk rules).</summary>
        public bool AdministratorDrainOn
        {
            get
            {
                if (!AdministratorNight) return false;
                if (CurrentShiftDef.VisitorCount == 0 || HuntReady || HuntActive) return true;
                var at = Checkpoint != null ? Checkpoint.CurrentVisitor : null;
                return at != null && at.IsAdministrator;
            }
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
            bool menu = s == AppViewState.MainMenu;
            if (MainMenu != null) { if (s == AppViewState.MainMenu) MainMenu.Show(); else MainMenu.Hide(); }

            bool inRun = s == AppViewState.Checkpoint || s == AppViewState.Exploration ||
                         s == AppViewState.ShiftIntro ||
                         s == AppViewState.Map || s == AppViewState.ShiftResults ||
                         s == AppViewState.RoomPeek;
            if (HUD != null) { if (inRun) HUD.Show(); else HUD.Hide(); }
            if (Checkpoint != null)
            {
                if (s == AppViewState.Checkpoint) Checkpoint.Show();
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
            if (Ending != null) { if (s == AppViewState.Ending) Ending.Show(); else Ending.Hide(); }
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
                           State == AppViewState.Map ||
                           State == AppViewState.RoomPeek;
            if (target && !allowed) return;
            Paused = target;
            Time.timeScale = Paused ? 0f : 1f;
            if (PauseScreen != null) { if (Paused) PauseScreen.Show(); else PauseScreen.Hide(); }
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
            Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Menu);
        }

        /// <summary>True while a run is actually in progress - the menu's blank session
        /// must never be persisted over a real save (EXIT on the menu must never wipe a save).</summary>
        private bool InRunProgress() =>
            Session != null && Session.Mode == GameMode.Normal && !Session.RunFinished &&
            Session.CompletedShifts < ShiftPlan.TotalShifts &&
            State != AppViewState.MainMenu && State != AppViewState.Boot;

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
            Hollowbrook.Scenes.ProceduralArt.ClearCache();
            Hollowbrook.Scenes.ProceduralAudio.ClearCache();
        }
    }
}
