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
        /// <summary>The ID card's resting display scale (~18% under the old default). The inspect
        /// view (DragPaper.Inspect) enlarges it from there.</summary>
        internal const float IdCardScale = L.IdScale;   // 320x220 design art -> 220x151 desk rect
        /// <summary>True once the SecurityRoom kit skin applied: paper-ink labels flip dark.</summary>
        private bool _kitOn;
        /// <summary>True once the HUD's live sanity meter got its kit tint (HUD is built after the desk).</summary>
        private bool _kitSanityApplied;
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

        private Text _topLeft, _topClock, _topNight;
        private string _lastClockText = "";
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin
        private GameObject _stickyRoot;
        private Button _stickyButton;

        private Image _visitorPortrait;
        private RectTransform _portraitRoot;
        private Image _glitchOverlay;

        private Text _idName, _idNumber, _idYear, _idCourse, _idExpires, _idStatus, _idHeader;
        private Image _idPhoto;
        private Text _recName, _recNumber, _recYear, _recCourse, _recStatus, _recClassroom, _recHeader, _recEnrolled;
        private StringBuilder _tonightsLog = new StringBuilder();

        private Button[] _questionButtons = new Button[4];
        private readonly string[] _qLabels = new string[4];
        private Text _budgetLabel;
        private Text _qEmpty;   // deliberate empty state for the questions panel
        private GameObject _slipGo;
        private GameObject _papersLayer;
        private Text _slipName, _slipId, _slipReason;
        private bool _slipTaken, _slipStamped, _backId;
        private Verdict _pendingVerdict = Verdict.None;
        private GameRng _stampRng = new GameRng(1);
        private Coroutine _punch;
        internal Verdict PendingVerdict => _pendingVerdict;
        internal bool SlipStamped => _slipStamped;
        private bool _documentsTaken;      // the ID is in your hand: papers, scan and stamps unlock
        private GameObject _offeredRoot;      // centred offered ID + its dimmer
        private Image _offeredPhoto;
        private Text _offeredName, _offeredNumber, _offeredExpires;
        private GameObject _recordGo, _idCardGo;   // hidden until the ID is taken
        private Button _notesBtn, _recordsBtn, _mapBtn, _patrolBtn, _endShiftBtn;
        private readonly Image[] _stampTints = new Image[3];
        private readonly Color[] _stampBase = new Color[3];
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();

        private Image _stampOverlay;
        private GameObject _notesRoot; private Text _notesText;
        private GameObject _recordsRoot; private Text _recordsText;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = (RectTransform)transform;

            // night-shift stage: radial grayscale wash + grain + vignette (the mockup's #stage)
            var bg = UIFactory.Img(_root, "bg", ProceduralArt.Get("desk_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = false;
            var grain = UIFactory.Img(_root, "grain", ProceduralArt.Get("noise_overlay"));
            UIFactory.AnchorsFull(grain.rectTransform);
            grain.raycastTarget = false;
            grain.color = new Color(0.6f, 0.6f, 0.6f, 0.10f);
            var vig = UIFactory.Img(_root, "vignette", ProceduralArt.Get("vignette"));
            UIFactory.AnchorsFull(vig.rectTransform);
            vig.raycastTarget = false;
            vig.color = new Color(1f, 1f, 1f, 0.55f);

            _desk = L.DeskFrame(_root, "DeskFrame");
            _audit = _desk.gameObject.AddComponent<DeskLayoutAudit>();

            BuildTopBar();
            BuildFileCabinet();    // floor-files station + the physical pull drawer...
            BuildLeftColumn();     // ...then the papers that ride above it
            BuildWindowArea();
            BuildOfferedId();
            BuildCenterColumn();
            BuildQuestionsPanel();
            BuildToolsRow();
            BuildPapersLayer();      // papers live above window/bubble/tools...
            BuildStampTray();        // ...and below the stamp tray; overlays top everything
            BuildStands();
            BuildOverlays();
            BuildStickyNote();
            BuildAdminTalk();    // the Shift-5 conversation panel + its re-open bar
            ApplyKitSkin();      // the owner's SecurityRoom art, over the audited grid

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
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
            HideBubbleNow();   // leaving the desk never carries a line along
            CloseFileDrawer(true);   // ...nor an open drawer: the cabinet is shut on return
            // ...nor a half-spoken conversation: the panel goes dark, the state stays in the
            // session, and ResumeRun puts the re-open bar back if the administrator still waits
            if (_talkOpen)
            {
                if (_talkPause != null) { StopCoroutine(_talkPause); _talkPause = null; }
                _talkOpen = false;
                _talkCommitting = false;
                if (_talkPanel != null) _talkPanel.SetActive(false);
                SetTalkColumn(false);
            }
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
            HideBubbleNow();
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
                // the radio speaks through the HUD toast — never through the student's bubble
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "RADIO: ROSTER DISCREPANCY — an expected student never reached the checkpoint."));
                _gameRoot.Audio.PlaySfx("sfx_static", 0.25f);
            }
            if (r.PressureWarning)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "RADIO: QUEUE PRESSURE — " + Queue.WaitingCount + " waiting at the gate."));
                _gameRoot.Audio.PlaySfx("sfx_intercom", 0.6f);
            }
            if (r.Overwhelmed)
            {
                // one consequence, one announcement: GameRoot owns both (toast + ending)
                _gameRoot.TriggerQueueOverwhelmed();
            }
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
            _current = v;
            _budget = new QuestionSystem.Budget();
            if (restoreBudget && Queue != null) _budget.RestoreFromMask(Queue.ActiveBudgetMask);
            _verdictLocked = false;
            // fresh hand-over state: nothing on the desk until the ID is offered and taken
            _documentsTaken = false;
            SetDocumentsVisible(false);
            _offeredRoot.SetActive(false);
            PresentVisitor(v);
            RefreshHud();
        }

        // The HUD prompt line teaches the hand-over: where the ID is, and how to get it.
        // Published only on change, same contract as the exploration prompts.
        private string _lastHandoverPrompt = "\0";

        internal void DebugTakeDocuments() { TakeId(); DockIdToSlot(); }

        // The tap/debug docking: the card's top-left lands on its slot corner. (Centring
        // the SLOT RECT would drift now that the card draws smaller than the rect —
        // IdCardScale 0.82.)
        private void DockIdToSlot()
        {
            var drag = _idCardGo != null ? _idCardGo.GetComponent<DragPaper>() : null;
            if (drag != null) drag.PlaceAt(new Vector2(L.IdCard.X, L.IdCard.Y));
        }

        // One visitor reached the window: portrait and voices only — the papers arrive
        // through the hand-over step (OnWindowClicked -> TakeId), not automatically.
        private void PresentVisitor(VisitorData v)
        {
            bool admin = v.IsAdministrator;
            ShowVisitorPortrait(v);
            PlayArrivalVoices(v, admin);
            ResetHandover(v);          // slip + ID slide to the counter edge, unasked
            RefreshHud();              // tray/prompt state always matches the new visitor
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

        // Flip the held ID — its black spine is the click handle; the F key flips whichever
        // paper you touched last. The back face carries the subtle tier. The papers are
        // DECOUPLED: flipping one never touches the other (the slip used to ride along
        // because both flips rode one shared "last paper" state).
        internal void FlipId() => FlipPaper(_idCardGo);

        // Keyboard flip (F): only the ID has a back face — the slip never flips.
        internal void FlipLastPaper() => FlipPaper(_idCardGo);

        // Both faces of one paper back to their born state: back hidden, front shown. The
        // stamp mark is left exactly as it is — it is a layer of the slip and is cleared
        // (alpha + sprite) by the hand-over reset, never by a flip.
        // The printed faces live on the paper's scaled art layer (cardart / slipart); the
        // container only carries the pointer surface and the desk rect.
        private static Transform FaceRoot(GameObject paper)
        {
            if (paper == null) return null;
            var art = paper.transform.Find("cardart");
            if (art == null) art = paper.transform.Find("slipart");
            return art != null ? art : paper.transform;
        }

        private static void FaceUp(GameObject paper)
        {
            var root = FaceRoot(paper);
            if (root == null) return;
            foreach (Transform ch in root)
            {
                if (ch.name == "stampoverlay") continue;
                ch.gameObject.SetActive(!ch.name.EndsWith("back"));
            }
        }

        private void FlipPaper(GameObject paper)
        {
            if (paper == null || !paper.activeSelf) return;
            var root = FaceRoot(paper);
            Transform bt = null;
            foreach (Transform ch in root)
                if (ch.name.EndsWith("back")) { bt = ch; break; }
            if (bt == null) return;
            bool on = !bt.gameObject.activeSelf;
            bt.gameObject.SetActive(on);
            foreach (Transform ch in root)
            {
                if (ch == bt || ch.name == "stampoverlay") continue;   // the stamp mark stays on the slip
                ch.gameObject.SetActive(!on);
            }
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.35f);
        }

        private void Update()
        {
            if (_gameRoot == null || !gameObject.activeInHierarchy) return;
            TickClockText();                  // the header clock follows the model every frame
            AdvanceQueue(Time.deltaTime);
            TickDrawer(Time.unscaledDeltaTime);   // the floor-files drawer's pull/slide
            TickKitLight(Time.unscaledDeltaTime); // the booth lamp's slow breath
            TickHotkeys();
        }

        // Split out so the headless FlowCheck can drive the same input path deterministically
        // (stub MonoBehaviours never get Update calls).
        internal void TickHotkeys()
        {
            // Escape belongs to an open overlay sheet (Options/Controls/Credits) first —
            // without this guard Esc over the pause menu would close Options AND unpause.
            if (Input.GetKeyDown(KeyCode.Escape) && !_gameRoot.BlockingOverlayOpen && InputGate.TryEscape())
                _gameRoot.TogglePause();
            // gameplay keys stand down while paused, in overlays, in dialogue, or off-desk
            if (_gameRoot.Paused || _gameRoot.BlockingOverlayOpen ||
                (_gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive) ||
                _gameRoot.State != AppViewState.Checkpoint) return;
            if (Input.GetKeyDown(KeyCode.M) && InputGate.TryM()) _gameRoot.OpenMap();
            // TAB pulls the floor-files drawer open / pushes it shut (same act as clicking
            // the cabinet front); with a floor already open it re-opens that floor's files
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleDrawerByKey();
            // T speaks with the administrator (or steps back out of the conversation); it is
            // inert unless the administrator is actually at the window
            if (Input.GetKeyDown(KeyCode.T)) ToggleAdminTalkByKey();
            // 1-4 ask the four questions while the interview is open (labels say so)
            for (int k = 0; k < 4; k++)
                if (Input.GetKeyDown((KeyCode)(KeyCode.Alpha1 + k))) AskByHotkey(k);
            // F flips the ID (its spine does too). Verdicts and the hand-back have NO key: a
            // stamp is dragged onto the slip and the ID is dragged back to the window by hand.
            if (Input.GetKeyDown(KeyCode.F)) FlipLastPaper();
        }

        // Everything on the desk that mirrors run state, refreshed after any change:
        // top bar, queue readout, question budget, and which buttons may be clicked.
        // The waiting line, as hooded figures on the mockup's stands: one silhouette per
        // waiting visitor (capped at four per stand), refreshed with the HUD.
        private readonly List<Image> _standFigures = new List<Image>();
        private void BuildStands()
        {
            var strips = new[] { L.QueueStripL, L.QueueStripR };
            var stripNames = new[] { "standL", "standR" };
            for (int si = 0; si < strips.Length; si++)
            {
                var strip = strips[si];
                var rt = UIFactory.NewUI(_desk, stripNames[si]);
                L.Place(rt, strip);
                rt.gameObject.SetActive(true);
                for (int i = 0; i < 4; i++)
                {
                    var fig = UIFactory.Img(rt, "hood" + i, ProceduralArt.Get("hood_figure"));
                    fig.rectTransform.anchorMin = new Vector2(0, 0);
                    fig.rectTransform.anchorMax = new Vector2(0, 0);
                    fig.rectTransform.pivot = new Vector2(0.5f, 0f);
                    fig.rectTransform.anchoredPosition = new Vector2(10 + i * 16, 0);
                    fig.rectTransform.sizeDelta = new Vector2(12, 24);
                    fig.raycastTarget = false;
                    fig.gameObject.SetActive(false);
                    _standFigures.Add(fig);
                }
            }
        }

        private void RefreshStands()
        {
            int waiting = Queue != null ? Queue.WaitingCount : 0;
            for (int i = 0; i < _standFigures.Count; i++)
                _standFigures[i].gameObject.SetActive(i < waiting);
        }

        private void RefreshHud()
        {
            if (_gameRoot == null) return;
            // Bootstrap builds the desk BEFORE the HUD, so ApplyKitSkin could not tint the
            // live sanity meter at Init — land it lazily on the first refresh where the HUD exists.
            if (_kitOn && !_kitSanityApplied && _gameRoot.HUD != null)
            {
                _gameRoot.HUD.ApplyKitSanity(true);
                _kitSanityApplied = true;
            }
            if (_gameRoot.Session == null) return;
            RefreshTopBar();
            RefreshHandoverPrompt();
            RefreshDecisionButtons();
            RefreshStands();
            RefreshAdminTalk();
        }

        private void OnEnable()
        {
            if (_gameRoot != null && _runStats != null && _gameRoot.Session.Mode != GameMode.Normal)
                _runStats.gameObject.SetActive(true);
        }
    }
}
