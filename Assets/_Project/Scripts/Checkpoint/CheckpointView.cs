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
    public class CheckpointView : MonoBehaviour, IScreenView
    {
        private const int SpeechKeep = 6;   // last ~3 exchanges, newest at the bottom
        private const string BulletinShort = "⚠ DISTRICT BULLETIN — NEW RULE IN EFFECT · SEE RULES NOTE";

        private GameRoot _gameRoot;
        private RectTransform _root;
        private RectTransform _desk;
        private DeskLayoutAudit _audit;

        public VisitorData CurrentVisitor => _current;

        private List<VisitorData> _roster;
        private ShiftDef _shift;
        private int _index = -1;
        private VisitorData _current;
        private QuestionSystem.Budget _budget = new QuestionSystem.Budget();
        private int[] _checklist = new int[8]; // 0 = blank, 1 = ok, 2 = issue (8 rows)
        private bool _verdictLocked = true;
        private bool _flowActive;
        private bool _waitingReturnFromPatrol;
        private bool _rosterComplete;

        private Text _topLeft, _topClock;
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin

        private Image _visitorPortrait;
        private RectTransform _speechPanel;
        private Text _speechLog;
        private readonly List<string[]> _speechEntries = new List<string[]>();
        private Image _glitchOverlay;

        private Text _idName, _idNumber, _idYear, _idCourse, _idExpires, _idStatus, _idHeader;
        private Image _idPhoto;
        private Text _recName, _recNumber, _recYear, _recCourse, _recStatus, _recClassroom, _recHeader, _recEnrolled;
        private Text[] _checkRows = new Text[8];
        private Text _monitorOutput;
        private StringBuilder _tonightsLog = new StringBuilder();

        private Button[] _questionButtons = new Button[4];
        private Text _budgetLabel;
        private Button _allowBtn, _rejectBtn, _scanBtn, _notesBtn, _recordsBtn, _mapBtn, _patrolBtn, _endShiftBtn;
        private Text _testimony;
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();
        private GameObject _confirmBox;
        private Text _confirmText, _noteText;

        private GameObject _bannerRoot; private Text _bannerTitle, _bannerBody;
        private Image _stampOverlay;
        private GameObject _notesRoot; private Text _notesText;
        private GameObject _recordsRoot; private Text _recordsText;

        private static readonly string[] ChecklistLabels =
        {
            "ID number format", "Photo matches person", "Expiration valid", "Year level matches",
            "Course matches", "Status matches", "Record name matches", "Behavior / answers",
        };

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = (RectTransform)transform;
            for (int i = 0; i < _checklist.Length; i++) _checklist[i] = 0;

            var bg = UIFactory.Img(_root, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = false;

            _desk = L.DeskFrame(_root, "DeskFrame");
            _audit = _desk.gameObject.AddComponent<DeskLayoutAudit>();

            BuildTopBar();
            BuildLeftColumn();
            BuildWindowArea();
            BuildCenterColumn();
            BuildRightColumn();
            BuildOverlays();

            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.RunTimerTick, e => OnRunTimerTick(e.Value)));
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.MutationChanged, e =>
            {
                var m = e.Payload<RuleMutationDef>();
                bool active = m != null && !string.IsNullOrEmpty(m.RuleText);
                _mutationStrip.gameObject.SetActive(active);
                if (!active) return;
                _mutationStrip.text = BulletinShort;         // full rule text: speech entry + RULES NOTE
                SayLine("DISTRICT BULLETIN", m.RuleText);
            }));
            gameObject.SetActive(false);
        }

        // Places on the grid AND registers for the one-time rect audit — the only way desk
        // elements get positioned.
        private void Reg(string name, RectTransform rt, L.Region r)
        {
            L.Place(rt, r);
            _audit.Register(name, rt, r);
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

        private Text Body(RectTransform parent, string name, L.Region r, int size, bool scroll)
        {
            return UIFactory.MaskedLabel(parent, name, r.X, r.Y, r.W, r.H, size,
                UIFactory.PaperInk, TextAnchor.UpperLeft, scroll);
        }

        private void BuildTopBar()
        {
            var bar = UIFactory.Panel(_desk, "topbar", UIFactory.PanelAlt);
            Reg("topbar", bar.rectTransform, L.TopBar);
            bar.raycastTarget = false;

            var banner = UIFactory.BorderedPanel(_desk, "daybanner", UIFactory.Paper, UIFactory.Border, 2);
            Reg("banner", banner, L.Banner);
            _topLeft = UIFactory.Label(banner, "left", "", 14, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_topLeft.rectTransform);

            _topClock = UIFactory.Label(_desk, "clock", "22:00", 20, UIFactory.InkDim, TextAnchor.MiddleCenter);
            Reg("clock", _topClock.rectTransform, L.Clock);

            var pause = GridButton(_desk, "pause", "II  PAUSE", () => _gameRoot.TogglePause(true), L.Pause, 12);
            Reg("pause", pause.GetComponent<RectTransform>(), L.Pause);

            _runStats = UIFactory.Label(_desk, "runstats", "", 13, UIFactory.Ink, TextAnchor.MiddleCenter);
            Reg("runstats", _runStats.rectTransform, L.RunStats);
            _runStats.gameObject.SetActive(false);

            var mutBg = UIFactory.Panel(_desk, "mutationbg", new Color(0.04f, 0.04f, 0.05f, 0.95f));
            Reg("mutation", mutBg.rectTransform, L.Mutation);
            mutBg.raycastTarget = false;
            _mutationStrip = UIFactory.Label(mutBg.rectTransform, "mutation", "", 13,
                new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_mutationStrip.rectTransform);
            _mutationStrip.gameObject.SetActive(false);
        }

        private void BuildLeftColumn()
        {
            var rec = UIFactory.Img(_desk, "record", ProceduralArt.Get("record_sheet"));
            Reg("record", rec.rectTransform, L.Record);
            rec.gameObject.AddComponent<RectMask2D>();   // rows can never spill off the sheet
            _recHeader = UIFactory.Label(rec.rectTransform, "header", "STUDENT RECORD — FORM 11-C", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_recHeader.rectTransform, L.Inner.RecordHeader);
            _recName = Row(rec.rectTransform, "rname", 0);
            _recNumber = Row(rec.rectTransform, "rnum", 1);
            _recYear = Row(rec.rectTransform, "ryear", 2);
            _recCourse = Row(rec.rectTransform, "rcourse", 3);
            _recStatus = Row(rec.rectTransform, "rstatus", 4);
            _recClassroom = Row(rec.rectTransform, "rroom", 5);
            _recEnrolled = Row(rec.rectTransform, "renrolled", 6);

            // respects Reduced Motion (no punch, instant) and never blocks clicks
            _stampOverlay = UIFactory.Img(rec.rectTransform, "stampoverlay", null, new Color(1, 1, 1, 0f));
            L.Place(_stampOverlay.rectTransform, L.Inner.StampLocal);
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            var idCard = UIFactory.Img(_desk, "idcard", ProceduralArt.Get("id_card_frame"));
            Reg("idcard", idCard.rectTransform, L.IdCard);
            idCard.gameObject.AddComponent<RectMask2D>();
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_idHeader.rectTransform, L.Inner.IdHeader);
            _idPhoto = UIFactory.Img(idCard.rectTransform, "photo", null);
            L.Place(_idPhoto.rectTransform, L.Inner.IdPhoto);
            _idPhoto.preserveAspect = true;
            _idName = IdRow(idCard.rectTransform, "name", 0);
            _idNumber = IdRow(idCard.rectTransform, "num", 1);
            _idYear = IdRow(idCard.rectTransform, "year", 2);
            _idCourse = IdRow(idCard.rectTransform, "course", 3);
            _idStatus = IdRow(idCard.rectTransform, "status", 4);
            _idExpires = IdRow(idCard.rectTransform, "exp", 5);

            var clip = UIFactory.BorderedPanel(_desk, "checklist", UIFactory.Paper, UIFactory.Border, 2);
            Reg("checklist", clip, L.Checklist);
            clip.gameObject.AddComponent<RectMask2D>();
            var clipHead = UIFactory.Label(clip, "head", "CHECKLIST — click a row to mark it", 11,
                UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(clipHead.rectTransform, L.Inner.ChecklistHead);
            for (int i = 0; i < 8; i++)
            {
                int row = i;
                var rb = UIFactory.StandardButton(clip, "check" + i, "[  ] " + ChecklistLabels[i],
                    () => CycleCheckRow(row), (int)L.Inner.CheckRowW, (int)L.Inner.CheckRowH);
                L.Place(rb.GetComponent<RectTransform>(),
                    new L.Region(L.Inner.CheckRowX, L.Inner.CheckRowY0 + i * L.Inner.CheckRowStep,
                        L.Inner.CheckRowW, L.Inner.CheckRowH));
                StylePaperRow(rb);
                _checkRows[i] = rb.GetComponentInChildren<Text>();
            }
        }

        private void BuildWindowArea()
        {
            var win = UIFactory.Img(_desk, "windowframe", ProceduralArt.Get("window_frame"));
            Reg("window", win.rectTransform, L.Window);
            win.raycastTarget = false;

            var glass = UIFactory.Panel(_desk, "glass", new Color(0.227f, 0.227f, 0.251f));
            Reg("glass", glass.rectTransform, L.Glass);
            glass.raycastTarget = false;
            var facade = UIFactory.Img(glass.rectTransform, "facade", ProceduralArt.Get("school_facade"));
            UIFactory.AnchorsFull(facade.rectTransform);
            facade.raycastTarget = false;
            var tint = UIFactory.Panel(glass.rectTransform, "tint", UIFactory.Glass);
            UIFactory.AnchorsFull(tint.rectTransform);
            tint.raycastTarget = false;

            _visitorPortrait = UIFactory.Img(_desk, "visitor", null);
            Reg("portrait", _visitorPortrait.rectTransform, L.Portrait);
            _visitorPortrait.preserveAspect = true;

            _glitchOverlay = UIFactory.Img(_desk, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            Reg("glitch", _glitchOverlay.rectTransform, L.Glitch);
            _glitchOverlay.raycastTarget = false;

            var sill = UIFactory.Panel(_desk, "sill", new Color(0.10f, 0.10f, 0.11f));
            Reg("sill", sill.rectTransform, L.Sill);
            sill.raycastTarget = false;
        }

        private void BuildCenterColumn()
        {
            _speechPanel = UIFactory.BorderedPanel(_desk, "speech",
                new Color(0.859f, 0.851f, 0.824f), UIFactory.Border, 2);
            Reg("speech", _speechPanel, L.Speech);
            _speechLog = UIFactory.MaskedLabel(_speechPanel, "speechlog",
                L.Inner.SpeechText.X, L.Inner.SpeechText.Y, L.Inner.SpeechText.W, L.Inner.SpeechText.H,
                12, UIFactory.PaperInk, TextAnchor.LowerLeft);
            _speechPanel.gameObject.SetActive(false);

            var note = UIFactory.BorderedPanel(_desk, "principalnote", UIFactory.Paper, UIFactory.Border, 2);
            Reg("note", note, L.Note);
            var noteHead = UIFactory.Label(note, "head", "PRINCIPAL NOTE", 12,
                UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(noteHead.rectTransform, L.Inner.NoteHead);
            _noteText = Body(note, "notebody", L.Inner.NoteBody, 11, true);

            _patrolBtn = GridButton(_desk, "patrol", "PATROL HALLWAY  ▸", GoPatrol, L.PatrolBtn, 12);
            Reg("patrol", _patrolBtn.GetComponent<RectTransform>(), L.PatrolBtn);
            _endShiftBtn = GridButton(_desk, "endshift", "END SHIFT — CLOCK OUT",
                () => _gameRoot.FinishShift(), L.EndShiftBtn, 11);
            Reg("endshift", _endShiftBtn.GetComponent<RectTransform>(), L.EndShiftBtn);
            UIFactory.SetButtonEnabled(_patrolBtn, false);
            UIFactory.SetButtonEnabled(_endShiftBtn, false);
        }

        private void BuildRightColumn()
        {
            var qPanel = UIFactory.BorderedPanel(_desk, "qpanel", UIFactory.Paper, UIFactory.Border, 2);
            Reg("questions", qPanel, L.Questions);
            qPanel.gameObject.AddComponent<RectMask2D>();
            var qHead = UIFactory.Label(qPanel, "head", "QUESTIONS", 13, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(qHead.rectTransform, L.Inner.QHead);
            _budgetLabel = UIFactory.Label(qPanel, "budget", "QUESTIONS: 3 LEFT", 11,
                new Color(0.35f, 0.35f, 0.34f), TextAnchor.MiddleRight);
            L.Place(_budgetLabel.rectTransform, L.Inner.QBudget);

            string[] qs =
            {
                "1 · Your course?", "2 · Your year?",
                "3 · Regular or transferee?", "4 · First classroom?",
            };
            var qids = QuestionSystem.All;
            for (int i = 0; i < 4; i++)
            {
                int qi = i;
                var b = UIFactory.StandardButton(qPanel, "q" + i, qs[i], () => AskQuestion(qids[qi]),
                    (int)L.Inner.QBtnW, (int)L.Inner.QBtnH);
                L.Place(b.GetComponent<RectTransform>(),
                    new L.Region(L.Inner.QBtnX, L.Inner.QBtnY0 + i * L.Inner.QBtnStep,
                        L.Inner.QBtnW, L.Inner.QBtnH));
                SizeLabel(b, 13);
                _questionButtons[i] = b;
            }
            _testimony = UIFactory.Label(qPanel, "testimony", "", 10,
                new Color(0.30f, 0.30f, 0.29f), TextAnchor.UpperLeft);
            L.Place(_testimony.rectTransform, L.Inner.QTestimony);

            _scanBtn = GridButton(_desk, "scan", "SCAN ID", DoScan, L.UtilityScan, 12);
            Reg("scan", _scanBtn.GetComponent<RectTransform>(), L.UtilityScan);
            _recordsBtn = GridButton(_desk, "records", "TONIGHT'S LOG",
                () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, L.UtilityRecords, 12);
            Reg("records", _recordsBtn.GetComponent<RectTransform>(), L.UtilityRecords);
            _mapBtn = GridButton(_desk, "map", "MAP", () => _gameRoot.OpenMap(), L.UtilityMap, 12);
            Reg("map", _mapBtn.GetComponent<RectTransform>(), L.UtilityMap);
            _notesBtn = GridButton(_desk, "notes", "RULES NOTE",
                () => ToggleOverlay(_notesRoot), L.UtilityNotes, 12);
            Reg("notes", _notesBtn.GetComponent<RectTransform>(), L.UtilityNotes);

            _rejectBtn = GridButton(_desk, "reject", "CALL PRINCIPAL", AskPrincipal, L.StampReject, 12, true);
            MakeRound(_rejectBtn, false);
            Reg("callprincipal", _rejectBtn.GetComponent<RectTransform>(), L.StampReject);
            _allowBtn = GridButton(_desk, "allow", "ALLOW", () => Stamp(Verdict.Allow), L.StampAllow, 12);
            MakeRound(_allowBtn, true);
            Reg("allow", _allowBtn.GetComponent<RectTransform>(), L.StampAllow);

            var mon = UIFactory.BorderedPanel(_desk, "monitor", new Color(0.055f, 0.062f, 0.055f),
                UIFactory.Border, 2);
            Reg("monitor", mon, L.Monitor);
            var camCap = UIFactory.Label(mon, "camcap", "CAMERA", 11, UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(camCap.rectTransform, L.Inner.MonitorCaption);
            _monitorOutput = UIFactory.MaskedLabel(mon, "out",
                L.Inner.MonitorOut.X, L.Inner.MonitorOut.Y, L.Inner.MonitorOut.W, L.Inner.MonitorOut.H,
                13, new Color(0.55f, 0.75f, 0.55f));
        }

        private void BuildOverlays()
        {
            var brt = UIFactory.NewUI(_desk, "Banner");
            Reg("verdictbanner", brt, L.VerdictBanner);
            _bannerRoot = brt.gameObject;
            var bgPanel = UIFactory.BorderedPanel(brt, "bg", new Color(0.04f, 0.04f, 0.05f, 0.97f),
                UIFactory.Border, 2);
            UIFactory.AnchorsFull(bgPanel);
            _bannerTitle = UIFactory.Label(brt, "title", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter);
            L.Place(_bannerTitle.rectTransform, L.Inner.BannerTitle);
            _bannerBody = UIFactory.Label(brt, "body", "", 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            L.Place(_bannerBody.rectTransform, L.Inner.BannerBody);
            _bannerRoot.SetActive(false);

            _notesRoot = BuildPaperOverlay("OverlayRules", "PAPER NOTE — INSPECTION RULES", out _notesText);
            _recordsRoot = BuildPaperOverlay("OverlayLog", "TONIGHT'S LOG — ANOMALY RECORD", out _recordsText);

            var conf = UIFactory.BorderedPanel(_desk, "confirmbox", UIFactory.Paper, UIFactory.Border, 2);
            Reg("confirm", conf, L.ConfirmBox);
            _confirmText = UIFactory.Label(conf, "txt", "", 14, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            L.Place(_confirmText.rectTransform, L.Inner.ConfirmText);
            var no = GridButton(conf, "no", "No", () => _confirmBox.SetActive(false), L.Inner.ConfirmNo, 14);
            L.Place(no.GetComponent<RectTransform>(), L.Inner.ConfirmNo);
            var yes = GridButton(conf, "yes", "Yes",
                () => { _confirmBox.SetActive(false); Stamp(Verdict.Reject); }, L.Inner.ConfirmYes, 14);
            L.Place(yes.GetComponent<RectTransform>(), L.Inner.ConfirmYes);
            _confirmBox = conf.gameObject;
            _confirmBox.SetActive(false);
        }

        private GameObject BuildPaperOverlay(string goName, string title, out Text body)
        {
            var goRt = UIFactory.NewUI(_desk, goName);
            var go = goRt.gameObject;
            L.Place(goRt, L.FullDesk);
            UIFactory.Dimmer(goRt, "dim", 0.6f);
            var panel = UIFactory.Img(goRt, "paper", ProceduralArt.Get("paper_flat"));
            L.Place(panel.rectTransform, L.PaperOverlay);
            var head = UIFactory.Label(panel.rectTransform, "title", title, 20,
                UIFactory.PaperInk, TextAnchor.MiddleCenter);
            L.Place(head.rectTransform, L.Inner.PaperHead);
            body = Body(panel.rectTransform, "body", L.Inner.PaperBody, 15, true);
            var close = GridButton(panel.rectTransform, "close", "CLOSE [X]",
                () => go.SetActive(false), L.Inner.PaperClose, 14);
            L.Place(close.GetComponent<RectTransform>(), L.Inner.PaperClose);
            go.SetActive(false);
            return go;
        }

        private void StylePaperRow(Button b)
        {
            b.GetComponent<Image>().color = new Color(0.74f, 0.73f, 0.69f);
            var inner = b.transform.GetChild(0).GetComponent<Image>();
            inner.color = new Color(0.80f, 0.79f, 0.75f);
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, 10);
            t.color = UIFactory.PaperInk;
            t.alignment = TextAnchor.MiddleLeft;
            UIFactory.AnchorsFull(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(4, 0);
            t.rectTransform.offsetMax = new Vector2(-2, 0);
        }

        private Text Row(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 11, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.RecordRowX,
                L.Inner.RecordRules[rowIndex] - L.Inner.RecordRowH, L.Inner.RecordRowW, L.Inner.RecordRowH));
            return t;
        }

        private Text IdRow(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 12, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.IdRowX,
                L.Inner.IdRules[rowIndex] - L.Inner.IdRowH, L.Inner.IdRowW, L.Inner.IdRowH));
            return t;
        }

        private void ToggleOverlay(GameObject go)
        {
            bool on = !go.activeSelf;
            go.SetActive(on);
            _gameRoot.Audio.PlaySfx(on ? "sfx_ui_open" : "sfx_ui_close", 0.5f);
            if (on && go == _notesRoot) RefreshNotes();
        }

        private void RefreshNotes()
        {
            var sb = new StringBuilder();
            sb.Append("BASE RULES — Every person presents a Student ID and record form.\n");
            sb.Append("Verify: ID format (HB-YYYY-NNNN) · photo vs. the person · expiration · year level · course/program · Regular/Transferee status · record name.\n");
            sb.Append("You may ask up to ").Append(Content.MaxQuestionsPerInspection).Append(" questions. Decisions are permanent.\n");
            sb.Append("SCAN cross-checks the student database and tonight's presentation log.\n\n");
            if (_shift != null) { sb.Append(_shift.RuleNoteAddendum).Append('\n'); }
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText)) sb.Append('\n').Append("▸ ").Append(m.RuleText);
            }
            _notesText.text = sb.ToString();
        }

        private void RefreshRecords()
        {
            var sb = new StringBuilder();
            sb.Append("PRESENTED TONIGHT:\n");
            sb.Append(_tonightsLog.Length == 0 ? "  (no one yet)\n" : _tonightsLog.ToString());
            sb.Append("\nBREACHES (anomalies allowed in): ").Append(_gameRoot.Session.Breaches).Append(" / 3\n");
            sb.Append("FALSE REJECTIONS: ").Append(_gameRoot.Session.FalseRejections).Append(" / 3\n");
            sb.Append("FINDINGS DISCOVERED: ").Append(_gameRoot.Session.Clues.FoundCount).Append(" / 10\n");
            _recordsText.text = sb.ToString();
        }

        public void Show() { gameObject.SetActive(true); RefreshHud(); }
        public void Hide()
        {
            gameObject.SetActive(false);
            _bannerRoot.SetActive(false);
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
        }

        public void BeginRun(List<VisitorData> roster, ShiftDef shift, int resumeFromVisitor = 0)
        {
            _index = resumeFromVisitor - 1;
            Debug.Log("[Flow] BeginRun roster=" + (roster != null ? roster.Count : -1));
            if (_noteText != null && _gameRoot.CurrentShiftDef != null) _noteText.text = _gameRoot.CurrentShiftDef.RuleNoteAddendum;
            _roster = roster;
            _shift = shift;
            _gameRoot.CurrentVisitorIndex = Mathf.Max(0, _index + 1);
            _tonightsLog.Clear();
            _speechEntries.Clear();
            _flowActive = true;
            _rosterComplete = false;
            _mutationStrip.gameObject.SetActive(false);
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText))
                {
                    _mutationStrip.text = BulletinShort;
                    _mutationStrip.gameObject.SetActive(true);
                    SayLine("DISTRICT BULLETIN", m.RuleText);
                }
            }
            bool runMode = _gameRoot.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            NextVisitor();
        }

        public void ResumeAfterExploration()
        {
            if (!_waitingReturnFromPatrol) return;
            _waitingReturnFromPatrol = false;
            StartCoroutine(DelayedNext(0.6f));
        }

        public void ResumeRun()
        {
            _shift = ShiftPlan.Get(Mathf.Clamp(_gameRoot.Session.CurrentShift, 1, ShiftPlan.TotalShifts));
            _roster = _gameRoot.CurrentRoster;
            _index = _gameRoot.CurrentVisitorIndex - 1;
            _tonightsLog.Clear();
            _speechEntries.Clear();
            _flowActive = true;
            _waitingReturnFromPatrol = false;
            _rosterComplete = false;
            _mutationStrip.gameObject.SetActive(false);
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText))
                {
                    _mutationStrip.text = BulletinShort;
                    _mutationStrip.gameObject.SetActive(true);
                    SayLine("DISTRICT BULLETIN", m.RuleText);
                }
            }
            bool runMode = _gameRoot.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            Show();
            NextVisitor();
        }

        private IEnumerator DelayedNext(float s)
        {
            yield return WaitUnpaused(s);
            if (_flowActive && gameObject.activeInHierarchy) NextVisitor();
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

        private void AskPrincipal()
        {
            if (_current == null || _verdictLocked) return;
            if (_confirmText != null) _confirmText.text = "PRINCIPAL... (Are you sure?)";
            if (_confirmBox != null) _confirmBox.SetActive(true);
        }

        // Flat wireframe circles: art is baked into the border sprite, the inner rect only
        // carries the label, and the border is the button's tint target.
        private void MakeRound(Button b, bool paper)
        {
            var border = b.GetComponent<Image>();
            border.sprite = ProceduralArt.Get(paper ? "circle_paper" : "circle_dark");
            border.color = Color.white;
            var inner = b.transform.GetChild(0).GetComponent<Image>();
            inner.color = new Color(0f, 0f, 0f, 0f);
            b.targetGraphic = border;
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, 12);
            t.color = paper ? UIFactory.PaperInk : UIFactory.Ink;
        }

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription?.Dispose();
            _subscriptions.Clear();
        }

        private void NextVisitor()
        {
            Debug.Log("[Flow] NextVisitor index=" + _index);
            if (_testimony != null) _testimony.text = "";
            if (_gameRoot.Config.exhibitionMode && _gameRoot.Session.Processed == 0 && _gameRoot.Session.CurrentShift == 1)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "EXHIBITION: photo and expiry first, then one question if in doubt."));
            if (!_flowActive) return;
            VisitorData v;
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                _index++;
                if (_index >= _roster.Count)
                {
                    _current = null;
                    _verdictLocked = true;
                    _rosterComplete = true;
                    SetVisitorVisible(false);
                    RefreshHud();
                    SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
                    return;
                }
                v = _roster[_index];
            }
            else
            {
                v = _gameRoot.NextRunVisitor();
                _current = null;
            }

            _current = v;
            _budget = new QuestionSystem.Budget();
            for (int i = 0; i < _checklist.Length; i++) { _checklist[i] = 0; _checkRows[i].text = "[  ] " + ChecklistLabels[i]; _checkRows[i].color = UIFactory.PaperInk; }
            _monitorOutput.text = "SCANNER READY";
            _verdictLocked = false;
            PresentVisitor(v);
            RefreshHud();
        }

        private void PresentVisitor(VisitorData v)
        {
            Debug.Log("[Flow] PresentVisitor " + v.ClaimedName);
            SetVisitorVisible(true);
            _visitorPortrait.sprite = ProceduralArt.Portrait("window", v.WindowPortrait, 240, 296);
            _idPhoto.sprite = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            if (_stampOverlay != null)
            {
                var sc = _stampOverlay.color; sc.a = 0f; _stampOverlay.color = sc;
                _stampOverlay.sprite = null;
            }
            var pc = _visitorPortrait.color; pc.a = 0f; _visitorPortrait.color = pc;
            StartCoroutine(PortraitFadeIn());

            bool admin = v.IsAdministrator;
            _idHeader.text = admin ? "DISTRICT STAFF BADGE" : "HOLLOWBROOK COLLEGE — STUDENT ID";
            _recHeader.text = admin ? "DISTRICT ROSTER MEMO" : "STUDENT RECORD — FORM 11-C";

            _idName.text = "NAME: " + v.ClaimedName;
            _idNumber.text = (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            if (admin)
            {
                _idYear.text = "DIVISION: Facilities Oversight";
                _idCourse.text = "AUTHORITY: District Office";
                _idStatus.text = "CLEARANCE: Full building";
                _recName.text = "ASSIGNED: " + v.RecordName;
                _recNumber.text = "BADGE NO: " + v.RecordIdNumber;
                _recYear.text = "EFFECTIVE: " + Content.ShiftDates[Mathf.Clamp(_gameRoot.Session.CurrentShift - 1, 0, 4)];
                _recCourse.text = "PURPOSE: Assume control of site";
                _recStatus.text = "SIGNED: District Seal (smudged)";
                _recClassroom.text = "ESCORT: None authorized";
                _recEnrolled.text = "FILED: 1987-10-05";
            }
            else
            {
                _idYear.text = "YEAR: " + Content.YearNames[v.IdYearLevel - 1];
                _idCourse.text = "COURSE: " + Content.Courses[v.IdCourseIndex];
                _idStatus.text = "STATUS: " + (v.IdIsTransferee ? "TRANSFEREE" : "REGULAR");
                _recName.text = "NAME: " + v.RecordName;
                _recNumber.text = "ID NO: " + v.RecordIdNumber;
                _recYear.text = "YEAR LEVEL: " + Content.YearNames[v.RecordYearLevel - 1];
                _recCourse.text = "COURSE/PROGRAM: " + Content.Courses[v.RecordCourseIndex];
                _recStatus.text = "STATUS: " + (v.RecordIsTransferee ? "TRANSFEREE" : "REGULAR");
                _recClassroom.text = "FIRST CLASS: " + v.RecordFirstClassroom;
                _recEnrolled.text = "EXPIRES: " + v.ExpirationDisplay;
            }
            _idExpires.text = admin ? "VALID THRU: " + v.ExpirationDisplay : "EXPIRES: " + v.ExpirationDisplay;

            _gameRoot.Audio.PlaySfx("sfx_knock", 0.7f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.DocumentsShown, v));
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.6f);

            if (admin)
            {
                var lines = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[0] },
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[1] },
                };
                if (_gameRoot.Session.Clues.FoundCount >= 6)
                    lines.Add(new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "That face. The old school photo — the staff member who never aged. It's them." });
                lines.Add(new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[3] });
                SayLine("AT THE WINDOW", "A formal coat. A briefcase. A smile that arrives a half-second late.");
                if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, null);
                _gameRoot.Audio.PlayMusic("final");
            }
            else
            {
                Say(v.Greeting);
                if (v.IsAnomaly && v.BehaviorGlitchVisible && _gameRoot.Session.Sanity < 70f)
                    StartCoroutine(GlitchHint());
            }
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VisitorArrived, v));
        }

        private IEnumerator GlitchHint()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            if (_current == null || !gameObject.activeInHierarchy) yield break;
            _glitchOverlay.gameObject.SetActive(true);
            var c = _glitchOverlay.color; c.a = 0.35f; _glitchOverlay.color = c;
            _gameRoot.Audio.PlaySfx("sfx_glitch", 0.35f);
            yield return new WaitForSecondsRealtime(0.12f);
            c = _glitchOverlay.color; c.a = 0f; _glitchOverlay.color = c;
            _glitchOverlay.gameObject.SetActive(false);
        }

        private void SetVisitorVisible(bool on)
        {
            _visitorPortrait.gameObject.SetActive(on);
            _speechPanel.gameObject.SetActive(on);
            _glitchOverlay.gameObject.SetActive(on && _glitchOverlay.color.a > 0f);
        }

        private void Say(string text) => SayLine("AT THE WINDOW", text);

        // Rolling two-sided conversation: speaker tag + line per entry, newest at the bottom,
        // oldest clipped away by the panel's RectMask2D once the box is full.
        private void SayLine(string who, string text)
        {
            _speechEntries.Add(new[] { who, text });
            while (_speechEntries.Count > SpeechKeep) _speechEntries.RemoveAt(0);
            var sb = new StringBuilder();
            foreach (var entry in _speechEntries)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("<b><color=#6f6d68>").Append(entry[0]).Append("</color></b>\n").Append(entry[1]);
            }
            _speechLog.text = sb.ToString();
            _speechPanel.gameObject.SetActive(true);
        }

        public void AskQuestion(QuestionId q)
        {
            if (_current == null || _verdictLocked) return;
            if (!_budget.TryAsk(_current, q, out var answer))
            {
                _monitorOutput.text = _budget.HasAsked(q) ? "ALREADY ASKED THAT." : "NO MORE QUESTIONS — DISTRICT POLICY.";
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.35f);
                return;
            }
            string tone = answer.Tone == ToneTag.Normal ? "" : "  [" + ToneLabel(answer.Tone) + "]";
            SayLine("YOU", QuestionSpeech(q));
            SayLine("AT THE WINDOW", "“" + answer.Text + "”" + tone);
            AppendTestimony(q, answer);
            _gameRoot.Audio.PlaySfx("sfx_intercom", 0.5f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, q));
            RefreshHud();
        }

        private float BannerSeconds => _gameRoot.Config.exhibitionMode ? _gameRoot.Config.exhibitionBannerSeconds : _gameRoot.Config.verdictBannerSeconds;
        private float BetweenSeconds => _gameRoot.Config.exhibitionMode ? _gameRoot.Config.exhibitionBetweenVisitors : _gameRoot.Config.betweenVisitorsSeconds;

        private void AppendTestimony(QuestionId q, Answer answer)
        {
            if (_testimony == null) return;
            string check = answer.ContradictsDocuments ? " x PAPERS DISAGREE"
                         : " = matches papers";
            string tone = answer.Tone == ToneTag.Normal ? "" : " (" + ToneLabel(answer.Tone).ToLowerInvariant() + " voice)";
            string line = "• " + QuestionTextShort(q) + ": " + check + tone;
            _testimony.text = string.IsNullOrEmpty(_testimony.text)
                ? line : _testimony.text + "\n" + line;
        }

        private static string ToneLabel(ToneTag t)
        {
            switch (t)
            {
                case ToneTag.Hesitant: return "hesitant";
                case ToneTag.Flat: return "flat, rehearsed";
                case ToneTag.TooSmooth: return "too smooth";
                default: return "echoing, as if reading";
            }
        }

        private static string QuestionTextShort(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "COURSE";
                case QuestionId.Year: return "YEAR";
                case QuestionId.Status: return "STATUS";
                default: return "FIRST CLASS";
            }
        }

        private static string QuestionSpeech(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "Your course?";
                case QuestionId.Year: return "Your year?";
                case QuestionId.Status: return "Regular or transferee?";
                default: return "Which classroom is your first class in?";
            }
        }

        public void DoScan()
        {
            if (_current == null || _verdictLocked || _gameRoot.Roster == null) return;
            var res = _gameRoot.Roster.Scan(_current, _gameRoot.Session.TonightLog);
            _monitorOutput.text = res.Display;
            _monitorOutput.color = res.FoundInDatabase && !res.DuplicateTonight
                ? new Color(0.55f, 0.75f, 0.55f)
                : new Color(0.85f, 0.35f, 0.35f);
            _gameRoot.Audio.PlaySfx(res.FormatValid && res.FoundInDatabase && !res.DuplicateTonight ? "sfx_scan_ok" : "sfx_scan_error", 0.8f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScanPerformed, res));
        }

        private void CycleCheckRow(int row)
        {
            if (_current == null || _verdictLocked) return;
            _checklist[row] = (_checklist[row] + 1) % 3;
            switch (_checklist[row])
            {
                case 1: _checkRows[row].text = "[✓] " + ChecklistLabels[row]; _checkRows[row].color = new Color(0.1f, 0.35f, 0.12f); break;
                case 2: _checkRows[row].text = "[✗] " + ChecklistLabels[row]; _checkRows[row].color = new Color(0.55f, 0.1f, 0.12f); break;
                default: _checkRows[row].text = "[  ] " + ChecklistLabels[row]; _checkRows[row].color = UIFactory.PaperInk; break;
            }
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.35f);
        }

        public void Stamp(Verdict verdict)
        {
            if (_current == null || _verdictLocked) return;
            _verdictLocked = true; // §2.4 — decisions are permanent
            var mutation = _gameRoot.Session.Mode == GameMode.Challenge ? _gameRoot.Session.ActiveMutation : null;
            var outcome = InspectionRules.Apply(_current, verdict, mutation);

            _gameRoot.Audio.PlaySfx(verdict == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 1f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, outcome));
            if (_stampOverlay != null) StartCoroutine(StampPunch(verdict));

            _tonightsLog.Append("  ").Append(_current.IsAdministrator ? "ADMINISTRATOR" : _current.ClaimedName)
                .Append(" — ").Append(verdict == Verdict.Allow ? "ALLOWED" : "REJECTED")
                .Append(outcome.Correct ? " (correct)\n" : " (MISTAKE)\n");

            int checklistHits = CountChecklistAccuracy(_current, outcome);
            var resolved = _current;
            _gameRoot.CurrentVisitorIndex = _index + 1;               // survive scene reloads (patrol)
            _gameRoot.OnVerdictResolved(resolved, outcome);           // counters update BEFORE the banner reads them
            if (_gameRoot.State == AppViewState.Ending) { Hide(); return; } // ending took over (C3/C4)
            ShowBanner(verdict, outcome, checklistHits);
            RefreshHud();

            StartCoroutine(AfterBanner(resolved));
        }

        private int CountChecklistAccuracy(VisitorData v, InspectionRules.Outcome o)
        {
            bool[] truth = GroundTruthRows(v);
            int hits = 0;
            for (int i = 0; i < 8; i++)
            {
                if (_checklist[i] == 0) continue;
                bool markedIssue = _checklist[i] == 2;
                if (markedIssue == truth[i]) hits++;
            }
            return hits;
        }

        private bool[] GroundTruthRows(VisitorData v)
        {
            bool[] t = new bool[8];
            t[0] = !IdRules.IsFormatValid(v.IdNumber);
            t[1] = !v.PhotoMatches;
            t[2] = IdRules.IsExpired(v, v.ShiftNumber);
            t[3] = v.IdYearLevel != v.RecordYearLevel;
            t[4] = v.IdCourseIndex != v.RecordCourseIndex;
            t[5] = v.IdIsTransferee != v.RecordIsTransferee;
            t[6] = v.ClaimedName != v.RecordName;
            t[7] = v.Defects.Contains(DefectType.BehaviorOddity);
            return t;
        }

        private void ShowBanner(Verdict verdict, InspectionRules.Outcome o, int checklistHits)
        {
            string title = verdict == Verdict.Allow ? "ACCESS GRANTED" : "ACCESS DENIED";
            _bannerTitle.text = title;
            _bannerTitle.color = verdict == Verdict.Allow ? UIFactory.Ink : UIFactory.AccentBright;

            var sb = new StringBuilder();
            if (o.Correct) sb.Append("CORRECT DECISION.  (+").Append(o.WasAnomaly ? 100 : 50).Append(")\n");
            else if (o.FalseRejection) sb.Append("MISTAKE — FALSE REJECTION. A real student was turned away. (")
                    .Append(_gameRoot.Session.FalseRejections).Append("/3 before the district reviews your contract)\n");
            else sb.Append("MISTAKE — ANOMALY ENTRY. Something is inside the school now. (")
                    .Append(_gameRoot.Session.Breaches).Append("/3 before the district reviews your contract)\n");
            sb.Append("\nWHAT THE DOCUMENTS SHOWED:\n");
            foreach (var line in o.RevealedDefects) sb.Append("  · ").Append(line).Append('\n');
            sb.Append("\nYour checklist: ").Append(checklistHits).Append("/8 rows accurate.");
            _bannerBody.text = sb.ToString();
            _bannerRoot.SetActive(true);
        }

        private IEnumerator StampPunch(Verdict verdict)
        {
            _stampOverlay.sprite = ProceduralArt.Get(verdict == Verdict.Allow ? "stamp_allow" : "stamp_reject");
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (!reduce)
            {
                float t = 0f;
                while (t < 0.16f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = 1.7f - 0.7f * Mathf.Clamp01(t / 0.16f);
                    _stampOverlay.rectTransform.localScale = new Vector3(k, k, 1f);
                    var c0 = _stampOverlay.color; c0.a = Mathf.Clamp01(t / 0.06f); _stampOverlay.color = c0;
                    yield return null;
                }
            }
            _stampOverlay.rectTransform.localScale = Vector3.one;
            var c = _stampOverlay.color; c.a = 1f; _stampOverlay.color = c;
            yield return new WaitForSecondsRealtime(1.8f);
            float f = 0f;
            while (f < 0.5f)
            {
                f += Time.unscaledDeltaTime;
                var cf = _stampOverlay.color; cf.a = 1f - Mathf.Clamp01(f / 0.5f); _stampOverlay.color = cf;
                yield return null;
            }
            var ce = _stampOverlay.color; ce.a = 0f; _stampOverlay.color = ce;
        }

        private IEnumerator PortraitFadeIn()
        {
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (reduce)
            {
                var cr = _visitorPortrait.color; cr.a = 1f; _visitorPortrait.color = cr;
                yield break;
            }
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                var c = _visitorPortrait.color; c.a = Mathf.Clamp01(t / 0.3f); _visitorPortrait.color = c;
                yield return null;
            }
            var c2 = _visitorPortrait.color; c2.a = 1f; _visitorPortrait.color = c2;
        }

        private IEnumerator AfterBanner(VisitorData resolved)
        {
            yield return WaitUnpaused(BannerSeconds);
            _bannerRoot.SetActive(false);
            if (_gameRoot.State != AppViewState.Checkpoint && _gameRoot.State != AppViewState.AdminEncounter) yield break;

            if (resolved.IsAdministrator && _gameRoot.Session.AdminRejected)
            {
                var outro = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "…You will regret this. We are already inside your records." },
                    new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The thing wearing Marlowe's face unfolds itself from the window and walks back into the dark. The seal on the server room holds — for tonight." },
                };
                _gameRoot.Dialogue.Play(outro, () =>
                {
                    SetVisitorVisible(false);
                    _rosterComplete = true;
                    RefreshHud();
                    Bus_MusicCheckpoint();
                });
                yield break;
            }

            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                bool rosterDone = _index + 1 >= _roster.Count;
                if (rosterDone)
                {
                    SetVisitorVisible(false);
                    _rosterComplete = true;
                    SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
                }
                else
                {
                    SayLine("RADIO", "Next one's crossing the yard. Patrol now if you need to.");
                    RefreshHud();
                    yield return new WaitForSecondsRealtime(BetweenSeconds + 2.2f);
                    if (_flowActive && !_waitingReturnFromPatrol && _gameRoot.State == AppViewState.Checkpoint)
                        NextVisitor();
                }
            }
            else
            {
                SetVisitorVisible(false);
                yield return WaitUnpaused(Mathf.Max(0.4f, BetweenSeconds * 0.5f));
                if (_flowActive && _gameRoot.State == AppViewState.Checkpoint) NextVisitor();
            }
            RefreshHud();
        }

        private void Bus_MusicCheckpoint() => _gameRoot.Audio.PlayMusic("checkpoint");

        public void GoPatrol()
        {
            if (_current != null && !_verdictLocked)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, "Finish the inspection at the window first."));
                return;
            }
            _waitingReturnFromPatrol = true;
            _gameRoot.EnterExploration();
        }

        private void Update()
        {
            if (_gameRoot == null || !gameObject.activeInHierarchy) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !_bannerRoot.activeSelf) _gameRoot.TogglePause();
            if (Input.GetKeyDown(KeyCode.M) && _gameRoot.State == AppViewState.Checkpoint) _gameRoot.OpenMap();
        }

        private void RefreshHud()
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                int total = _roster != null ? _roster.Count : 0;
                int shown = Mathf.Clamp(_index + 1, 0, total);
                _topLeft.text = "NIGHT " + _gameRoot.Session.CurrentShift + " · " + (_shift != null ? _shift.DateLabel : "") +
                                " — VISITOR " + shown + "/" + total;
                _topClock.text = _gameRoot.ClockString;
            }
            else
            {
                _topLeft.text = (_gameRoot.Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL") +
                                "   ·   PROCESSED " + _gameRoot.Session.RunProcessed;
                _topClock.text = _gameRoot.ClockString;
                float mult = EndlessRules.StreakMultiplier(_gameRoot.Session.Streak) *
                             (_gameRoot.Session.Mode == GameMode.Challenge ? ChallengeRules.ScoreMultiplier : 1f);
                _runStats.text = "SCORE " + _gameRoot.Session.Score + "   ·   STREAK " + _gameRoot.Session.Streak + " (×" + mult.ToString("0.0") + ")";
            }
            _budgetLabel.text = "QUESTIONS: " + _budget.Remaining + " LEFT";
            bool live = _current != null && !_verdictLocked;
            for (int i = 0; i < 4; i++)
                UIFactory.SetButtonEnabled(_questionButtons[i], live && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            UIFactory.SetButtonEnabled(_allowBtn, live);
            UIFactory.SetButtonEnabled(_rejectBtn, live);
            // patrol/end-shift stay visible per the wireframe; they dim until usable
            UIFactory.SetButtonEnabled(_patrolBtn, !live && !_bannerRoot.activeSelf);
            UIFactory.SetButtonEnabled(_endShiftBtn, _rosterComplete && _gameRoot.Session.Mode == GameMode.Normal);
        }

        public void OnRunTimerTick(float secondsLeft)
        {
            if (_runStats == null || !_runStats.gameObject.activeInHierarchy) return;
            int m = (int)(secondsLeft / 60f), s = (int)(secondsLeft % 60f);
            _runStats.text = "TIME " + m + ":" + s.ToString("D2") + "   ·   SCORE " + _gameRoot.Session.Score +
                             "   ·   STREAK ×" + EndlessRules.StreakMultiplier(_gameRoot.Session.Streak).ToString("0.0");
            _runStats.color = secondsLeft < 30f ? UIFactory.AccentBright : UIFactory.Ink;
        }

        private void OnEnable()
        {
            if (_gameRoot != null && _runStats != null && _gameRoot.Session.Mode != GameMode.Normal)
                _runStats.gameObject.SetActive(true);
        }
    }
}
