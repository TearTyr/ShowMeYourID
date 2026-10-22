using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The inspection desk: papers, questions, stamps, banners. Created by CheckpointSceneBootstrap.
    // Every element lives in one fixed 1280x720 DeskFrame and is placed only through the
    // CheckpointLayout grid — see desk_layout_wireframe.svg for the approved design.
    public partial class CheckpointView : MonoBehaviour, IScreenView
    {
        private const int SpeechKeep = 6;   // last ~3 exchanges, newest at the bottom
        private const string BulletinShort = "⚠ DISTRICT BULLETIN — NEW RULE IN EFFECT · SEE RULES NOTE";

        private GameRoot _gameRoot;
        private RectTransform _root;
        private RectTransform _desk;
        private DeskLayoutAudit _audit;

        public VisitorData CurrentVisitor => _current;

        // The queue itself lives in GameRoot (persistent run state); the view only
        // presents it. Queue is null-safe so bootstraps without a run stay quiet.
        private CheckpointQueue Queue => _gameRoot != null ? _gameRoot.Queue : null;

        private ShiftDef _shift;
        private VisitorData _current;
        private QuestionSystem.Budget _budget = new QuestionSystem.Budget();
        private bool _verdictLocked = true;
        private bool _flowActive;

        private Text _topLeft, _topClock;
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin
        private Text _queueStatus;   // QUEUE: n | CURRENT: a/t (+ pressure colors)
        private Text _queueBadge;    // waiting-count badge above the right silhouette strip
        private readonly Image[] _silhouettes = new Image[8];  // waiting line, 4 per strip
        private Text _queueOverflow;
        private GameObject _stickyRoot;
        private Button _stickyButton;

        private Image _visitorPortrait;
        private RectTransform _speechPanel;
        private Text _speechLog;
        private readonly List<string[]> _speechEntries = new List<string[]>();
        private Image _glitchOverlay;

        private Text _idName, _idNumber, _idYear, _idCourse, _idExpires, _idStatus, _idHeader;
        private Image _idPhoto;
        private Text _recName, _recNumber, _recYear, _recCourse, _recStatus, _recClassroom, _recHeader, _recEnrolled;
        private Text _monitorOutput;
        private StringBuilder _tonightsLog = new StringBuilder();

        private Button[] _questionButtons = new Button[4];
        private readonly string[] _qLabels = new string[4];
        private Text _budgetLabel;
        private Text _qHint;
        private Button _interviewBtn;      // the QUESTION gate: reveals the four questions
        private bool _interviewOpen;
        private bool _documentsTaken;      // the ID is in your hand: papers, scan and stamps unlock
        private GameObject _handoverTarget;   // invisible click target over the window slot
        private GameObject _offeredRoot;      // centred offered ID + its dimmer
        private Image _offeredPhoto;
        private Text _offeredName, _offeredNumber, _offeredExpires;
        private GameObject _recordGo, _idCardGo;   // hidden until the ID is taken
        private Button _allowBtn, _rejectBtn, _scanBtn, _notesBtn, _recordsBtn, _mapBtn, _patrolBtn, _endShiftBtn;
        private Text _testimony;
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private GameObject _confirmBox;
        private Text _confirmText;

        private GameObject _bannerRoot; private Text _bannerTitle, _bannerBody;
        private Image _stampOverlay;
        private GameObject _notesRoot; private Text _notesText;
        private GameObject _recordsRoot; private Text _recordsText;

        // Amber readout color while the waiting line is backing up (pressure level 1+).
        private static readonly Color PressureColor = new Color(0.95f, 0.72f, 0.30f);

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = (RectTransform)transform;

            var bg = UIFactory.Img(_root, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = false;
            bg.color = Color.black;   // placeholder art direction: plain black room behind the desk

            _desk = L.DeskFrame(_root, "DeskFrame");
            _audit = _desk.gameObject.AddComponent<DeskLayoutAudit>();

            BuildTopBar();
            BuildLeftColumn();
            BuildWindowArea();
            BuildOfferedId();
            BuildCenterColumn();
            BuildRightColumn();
            if (ShowcaseBuild.LineDisplay) BuildQueueStrips();
            BuildOverlays();
            BuildStickyNote();

            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.RunTimerTick, e => OnRunTimerTick(e.Value)));
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.MutationChanged,
                e => ShowBulletin(e.Payload<RuleMutationDef>())));
            gameObject.SetActive(false);
        }

        // Places on the grid AND registers for the one-time rect audit — the only way desk
        // elements get positioned.
        private void Reg(string name, RectTransform rt, L.Region r)
        {
            L.Place(rt, r);
            _audit.Register(name, rt, r);
        }

        // Buttons only hand over their Button reference; the audit still tracks the rect.
        private void Reg(string name, Button button, L.Region r)
        {
            Reg(name, button.GetComponent<RectTransform>(), r);
        }


        private static void SizeLabel(Button b, int size)
        {
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, size);
        }

        // StandardButton born at its grid rect's size; placement still goes through Place/Reg.
        private static Button GridButton(Transform parent, string name, string label, System.Action onClick,
            L.Region r, int fontSize, bool danger = false)
        {
            var b = UIFactory.StandardButton(parent, name, label, onClick, (int)r.W, (int)r.H, danger);
            SizeLabel(b, fontSize);
            return b;
        }



















        public void Show() { gameObject.SetActive(true); RefreshHud(); }
        public void Hide()
        {
            gameObject.SetActive(false);
            _bannerRoot.SetActive(false);
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
        }

        public void BeginRun(ShiftDef shift)
        {
            Debug.Log("[Flow] BeginRun queue=" + (Queue != null ? Queue.EffectiveTotal : -1));
            _shift = shift;
            ResetShiftState();
            RestoreQueuePresentation();
        }

        public void ResumeRun()
        {
            _shift = ShiftPlan.Get(Mathf.Clamp(_gameRoot.Session.CurrentShift, 1, ShiftPlan.TotalShifts));
            ResetShiftState();
            Show();

            // returning from patrol: the queue survived in GameRoot (clock paused while
            // the desk was gone) — rebuild the presentation from it
            RestoreQueuePresentation();
        }

        // Shared start-of-shift reset for BeginRun (new game / continue) and ResumeRun
        // (back from patrol): clear per-visitor state, re-post the challenge bulletin,
        // and show the score strip only in endless/challenge.
        private void ResetShiftState()
        {
            _tonightsLog.Clear();
            _speechEntries.Clear();
            _flowActive = true;
            _current = null;
            _verdictLocked = true;
            if (_gameRoot.Session.Mode == GameMode.Challenge) ShowBulletin(_gameRoot.Session.ActiveMutation);
            else ShowBulletin(null);
            bool runMode = _gameRoot.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
        }

        // Rebuilds what the player sees at the window from the persistent queue: a started
        // shift restores its waiting line and current visitor (no sticky note, no second
        // arrival clock); an unstarted shift waits behind the sticky-note gate.
        private void RestoreQueuePresentation()
        {
            if (Queue != null && Queue.Started)
            {
                _stickyRoot.SetActive(false);
                if (Queue.ResolutionPending) Queue.CompleteActive();   // a quit mid-banner finishes the step-up
                if (Queue.Active != null) PresentFromQueue(Queue.Active, true);
                else ApplyTickResult(Queue.ResumePresentation(), true);
            }
            else
            {
                // a fresh shift begins behind the sticky-note gate: nobody arrives,
                // nothing can be inspected, and the clock stays idle until it is clicked
                SetVisitorVisible(false);
                _stickyRoot.SetActive(true);
            }
            RefreshQueueLine();
            RefreshHud();
        }

        private IEnumerator WaitUnpaused(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (_gameRoot == null || !_gameRoot.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }



        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription?.Dispose();
            _subscriptions.Clear();
        }

        // ---------------------------------------------------------------- queue drive
        // One frame of the arrival clock. Ticked on SCALED time from Update, so pause
        // (timeScale 0) and blocking sheets freeze it; the desk being destroyed (patrol)
        // stops it entirely — the chosen rule: the line waits while you walk.
        public void AdvanceQueue(float dt)
        {
            if (!_flowActive || Queue == null || !Queue.Started || Queue.Collapsed) return;
            if (_gameRoot.State != AppViewState.Checkpoint) return;
            if (!gameObject.activeInHierarchy) return;
            if (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) return;
            var r = Queue.Tick(dt);
            ApplyTickResult(r, false);
            if (r.Any) RefreshHud();
        }

        // The view-side reaction to whatever the pure queue reported: present, sounds,
        // radio lines, warnings. All audio stays direct-call through _gameRoot.Audio.
        private void ApplyTickResult(QueueTickResult r, bool restoreBudget)
        {
            if (r.ToWindow != null)
            {
                ExhibitionHintOnce();
                PresentFromQueue(r.ToWindow, restoreBudget);
            }
            else if (r.ToLine != null)
            {
                _gameRoot.Audio.PlaySfx("sfx_footstep", 0.45f);   // footsteps join the line
            }
            if (r.MissingStudent)
            {
                SayLine("RADIO", "Roll call just came up one short. Someone signed in for night class who never reached your line.");
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "ROSTER DISCREPANCY — an expected student never reached the checkpoint."));
                _gameRoot.Audio.PlaySfx("sfx_static", 0.25f);
            }
            if (r.PressureWarning)
            {
                SayLine("RADIO", "Custodian — your line is backing up. Pick up the pace.");
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "QUEUE PRESSURE — " + Queue.WaitingCount + " waiting at the gate."));
                _gameRoot.Audio.PlaySfx("sfx_intercom", 0.6f);
            }
            if (r.Overwhelmed)
            {
                // one consequence, one announcement: GameRoot owns both (toast + ending)
                _gameRoot.TriggerQueueOverwhelmed();
            }
            if (r.LineChanged) RefreshQueueLine();
        }

        private void ExhibitionHintOnce()
        {
            if (_gameRoot.Config.exhibitionMode && _gameRoot.Session.Processed == 0 && _gameRoot.Session.CurrentShift == 1)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "EXHIBITION: photo and expiry first, then one question if in doubt."));
        }

        // A visitor reached the window (from the line or the clock): fresh inspection
        // state, then the existing presentation. restoreBudget re-applies the question
        // budget after a mid-inspection quit/continue so it can never be refunded.
        private void PresentFromQueue(VisitorData v, bool restoreBudget)
        {
            Debug.Log("[Flow] PresentVisitor " + v.ClaimedName);
            if (_testimony != null) _testimony.text = "";
            _current = v;
            _budget = new QuestionSystem.Budget();
            if (restoreBudget && Queue != null) _budget.RestoreFromMask(Queue.ActiveBudgetMask);
            _monitorOutput.text = "SCANNER READY";
            _verdictLocked = false;
            // fresh hand-over state: nothing on the desk until the ID is offered and taken
            _documentsTaken = false;
            _interviewOpen = false;
            SetDocumentsVisible(false);
            _offeredRoot.SetActive(false);
            PresentVisitor(v);
            RefreshQueueLine();
            RefreshHud();
        }

        // The HUD prompt line teaches the hand-over: where the ID is, and how to get it.
        // Published only on change, same contract as the exploration prompts.
        private string _lastHandoverPrompt = "\0";






        internal void DebugTakeDocuments() { OnWindowClicked(); TakeId(); }

        // One visitor reached the window: portrait and voices only — the papers arrive
        // through the hand-over step (OnWindowClicked -> TakeId), not automatically.
        private void PresentVisitor(VisitorData v)
        {
            bool admin = v.IsAdministrator;
            ShowVisitorPortrait(v);
            PlayArrivalVoices(v, admin);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VisitorArrived, v));
        }










        private float BannerSeconds => _gameRoot.Config.exhibitionMode ? _gameRoot.Config.exhibitionBannerSeconds : _gameRoot.Config.verdictBannerSeconds;












        public void GoPatrol()
        {
            if (_current != null && !_verdictLocked)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, "Finish the inspection at the window first."));
                return;
            }
            if (Queue != null && (!Queue.Started || Queue.Collapsed))
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    Queue.Started ? "The line has overrun the checkpoint." : "Begin the night shift first."));
                return;
            }
            _gameRoot.EnterExploration();   // the queue rides in GameRoot; the clock pauses off-desk
        }

        private void Update()
        {
            if (_gameRoot == null || !gameObject.activeInHierarchy) return;
            AdvanceQueue(Time.deltaTime);
            TickHotkeys();
        }

        // Split out so the headless FlowCheck can drive the same input path deterministically
        // (stub MonoBehaviours never get Update calls).
        internal void TickHotkeys()
        {
            // Escape belongs to an open overlay sheet (Options/Controls/Credits) first —
            // without this guard Esc over the pause menu would close Options AND unpause.
            if (Input.GetKeyDown(KeyCode.Escape) && !_bannerRoot.activeSelf && !_gameRoot.BlockingOverlayOpen)
                _gameRoot.TogglePause();
            if (Input.GetKeyDown(KeyCode.M) && _gameRoot.State == AppViewState.Checkpoint) _gameRoot.OpenMap();
            // 1-4 ask the four questions while the interview is open (labels say so)
            for (int k = 0; k < 4; k++)
                if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + k))) AskByHotkey(k);
        }

        // Everything on the desk that mirrors run state, refreshed after any change:
        // top bar, queue readout, question budget, and which buttons may be clicked.
        private void RefreshHud()
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            RefreshTopBar();
            RefreshHandoverPrompt();
            RefreshQueueStatus();
            RefreshDecisionButtons();
        }






        private void OnEnable()
        {
            if (_gameRoot != null && _runStats != null && _gameRoot.Session.Mode != GameMode.Normal)
                _runStats.gameObject.SetActive(true);
        }
    }
}
