using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Checkpoint
{
    // The inspection desk: papers, questions, stamps, banners. Created by CheckpointSceneBootstrap.
    public class CheckpointView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private RectTransform _root;

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

        private Text _topLeft, _topClock;
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin

        private Image _visitorPortrait;
        private RectTransform _speechPanel;
        private Text _speechSpeaker, _speechText;
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
        private RectTransform _idCardRt, _recRt, _monRt, _qColRt;
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

        // printed rule lines of the two paper sprites, in panel pixels after stretching
        private static readonly float[] RecordRules = { 42f, 63f, 84f, 105f, 126f, 147f, 168f };
        private static readonly float[] IdCardRules = { 52f, 75f, 99f, 123f, 146f, 179f };

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = (RectTransform)transform;
            for (int i = 0; i < _checklist.Length; i++) _checklist[i] = 0;

            var bg = UIFactory.Img(_root, "bg", ProceduralArt.Get("security_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = false;

            BuildTopBar();
            BuildWindowArea();
            BuildDesk();
            BuildControls();
            BuildOverlays();

            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.RunTimerTick, e => OnRunTimerTick(e.Value)));
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.MutationChanged, e =>
            {
                var m = e.Payload<RuleMutationDef>();
                if (_mutationStrip != null)
                {
                    _mutationStrip.text = m != null && !string.IsNullOrEmpty(m.RuleText)
                        ? "⚠ DISTRICT BULLETIN — " + m.RuleText : "";
                    _mutationStrip.gameObject.SetActive(m != null && !string.IsNullOrEmpty(m.RuleText));
                }
            }));
            BuildDecor();
            gameObject.SetActive(false);
        }

        private void BuildTopBar()
        {
            var bar = UIFactory.Panel(_root, "topbar", new Color(0.05f, 0.05f, 0.06f, 0.92f)).rectTransform;
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, CheckpointLayout.TopBarHeight); bar.anchoredPosition = Vector2.zero;

            var banner = UIFactory.Img(_root, "daybanner", ProceduralArt.Get("rough_panel"));
            CheckpointLayout.Place(banner.rectTransform, CheckpointLayout.Banner, CheckpointLayout.Pin.TopLeft);
            _topLeft = UIFactory.Label(banner.rectTransform, "left", "", 16, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_topLeft.rectTransform);

            _topClock = UIFactory.Label(_root, "clock", "22:00", 22, UIFactory.InkDim, TextAnchor.MiddleCenter);
            CheckpointLayout.Place(_topClock.rectTransform, CheckpointLayout.Clock, CheckpointLayout.Pin.TopRight);

            var pause = UIFactory.StandardButton(bar, "pause", "II  PAUSE", () => _gameRoot.TogglePause(true), 104, 34);
            CheckpointLayout.Place(pause.GetComponent<RectTransform>(), CheckpointLayout.Pause, CheckpointLayout.Pin.TopRight);

            _runStats = UIFactory.Label(_root, "runstats", "", 17, UIFactory.Ink, TextAnchor.MiddleCenter);
            CheckpointLayout.Place(_runStats.rectTransform, CheckpointLayout.RunStats, CheckpointLayout.Pin.TopCenter);
            _runStats.gameObject.SetActive(false);

            var mutBg = UIFactory.Panel(_root, "mutationbg", new Color(0.04f, 0.04f, 0.05f, 0.9f)).rectTransform;
            CheckpointLayout.Place(mutBg, CheckpointLayout.Mutation, CheckpointLayout.Pin.TopCenter);
            _mutationStrip = UIFactory.Label(mutBg, "mutation", "", 15, new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_mutationStrip.rectTransform);
            _mutationStrip.gameObject.SetActive(false);
        }

        private void BuildWindowArea()
        {
            var win = UIFactory.Img(_root, "windowframe", ProceduralArt.Get("window_frame"));
            CheckpointLayout.Place(win.rectTransform, CheckpointLayout.Window, CheckpointLayout.Pin.TopCenter);
            var facade = UIFactory.Img(win.rectTransform, "facade", ProceduralArt.Get("school_facade"));
            CheckpointLayout.PlaceLocal(facade.rectTransform, 15, 10, 440, 296);
            var glass = UIFactory.Panel(win.rectTransform, "glass", UIFactory.Glass);
            UIFactory.AnchorsFull(glass.rectTransform);
            glass.raycastTarget = false;
            var sill = UIFactory.Panel(_root, "sill", new Color(0.10f, 0.10f, 0.11f));
            CheckpointLayout.Place(sill.rectTransform, CheckpointLayout.Sill, CheckpointLayout.Pin.TopCenter);

            _visitorPortrait = UIFactory.Img(_root, "visitor", null);
            CheckpointLayout.Place(_visitorPortrait.rectTransform, CheckpointLayout.Portrait, CheckpointLayout.Pin.TopCenter);
            _visitorPortrait.preserveAspect = true;

            _glitchOverlay = UIFactory.Img(_root, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            CheckpointLayout.Place(_glitchOverlay.rectTransform, CheckpointLayout.Glitch, CheckpointLayout.Pin.TopCenter);
            _glitchOverlay.raycastTarget = false;

            _speechPanel = UIFactory.Panel(_root, "speech", new Color(0.86f, 0.85f, 0.82f, 0.96f)).rectTransform;
            CheckpointLayout.Place(_speechPanel, CheckpointLayout.Speech, CheckpointLayout.Pin.BottomCenter);
            _speechSpeaker = UIFactory.Label(_speechPanel, "who", "", 13, UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(_speechSpeaker.rectTransform, 10, 6, 280, 18);
            _speechText = UIFactory.Label(_speechPanel, "text", "", 15, UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(_speechText.rectTransform, 10, 28, 280, 74);
            _speechPanel.gameObject.SetActive(false);
        }

        private void BuildDesk()
        {
            var rec = UIFactory.Img(_root, "record", ProceduralArt.Get("record_sheet"));
            _recRt = rec.rectTransform;
            CheckpointLayout.Place(rec.rectTransform, CheckpointLayout.Record, CheckpointLayout.Pin.TopLeft);
            _recHeader = UIFactory.Label(rec.rectTransform, "header", "STUDENT RECORD — FORM 11-C", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(_recHeader.rectTransform, 0, 3, 320, 26);
            _recName = Row(rec.rectTransform, "rname", 0);
            _recNumber = Row(rec.rectTransform, "rnum", 1);
            _recYear = Row(rec.rectTransform, "ryear", 2);
            _recCourse = Row(rec.rectTransform, "rcourse", 3);
            _recStatus = Row(rec.rectTransform, "rstatus", 4);
            _recClassroom = Row(rec.rectTransform, "rroom", 5);
            _recEnrolled = Row(rec.rectTransform, "renrolled", 6);

            // respects Reduced Motion (no punch, instant) and never blocks clicks
            _stampOverlay = UIFactory.Img(rec.rectTransform, "stampoverlay", null, new Color(1, 1, 1, 0f));
            CheckpointLayout.PlaceLocal(_stampOverlay.rectTransform, 180, 170, 110, 110);
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            var idCard = UIFactory.Img(_root, "idcard", ProceduralArt.Get("id_card_frame"));
            _idCardRt = idCard.rectTransform;
            CheckpointLayout.Place(idCard.rectTransform, CheckpointLayout.IdCard, CheckpointLayout.Pin.BottomLeft);
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(_idHeader.rectTransform, 0, 3, 320, 28);
            _idPhoto = UIFactory.Img(idCard.rectTransform, "photo", null);
            CheckpointLayout.PlaceLocal(_idPhoto.rectTransform, 10, 46, 90, 118);
            _idPhoto.preserveAspect = true;
            _idName = IdRow(idCard.rectTransform, "name", 0);
            _idNumber = IdRow(idCard.rectTransform, "num", 1);
            _idYear = IdRow(idCard.rectTransform, "year", 2);
            _idCourse = IdRow(idCard.rectTransform, "course", 3);
            _idStatus = IdRow(idCard.rectTransform, "status", 4);
            _idExpires = IdRow(idCard.rectTransform, "exp", 5);

            var clip = UIFactory.Img(_root, "checklist", ProceduralArt.Get("rough_panel"));
            CheckpointLayout.Place(clip.rectTransform, CheckpointLayout.Checklist, CheckpointLayout.Pin.BottomLeft);
            var clipHead = UIFactory.Label(clip.rectTransform, "head", "CHECKLIST — click a row to mark it", 12,
                UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(clipHead.rectTransform, 8, 4, 300, 16);
            for (int i = 0; i < 8; i++)
            {
                int row = i;
                var rb = UIFactory.StandardButton(clip.rectTransform, "check" + i, "[  ] " + ChecklistLabels[i],
                    () => CycleCheckRow(row), 308, 12);
                CheckpointLayout.PlaceLocal(rb.GetComponent<RectTransform>(), 6, 22 + i * 12.5f, 308, 12);
                StylePaperRow(rb);
                _checkRows[i] = rb.GetComponentInChildren<Text>();
            }

            var mon = UIFactory.Panel(_root, "monitor", new Color(0.06f, 0.07f, 0.06f)).rectTransform;
            _monRt = mon;
            CheckpointLayout.Place(mon, CheckpointLayout.Monitor, CheckpointLayout.Pin.BottomRight);
            var monBorder = UIFactory.Panel(mon, "border", UIFactory.Border).rectTransform;
            UIFactory.AnchorsFull(monBorder);
            var monInner = UIFactory.Panel(monBorder, "inner", new Color(0.03f, 0.04f, 0.03f)).rectTransform;
            monInner.anchorMin = Vector2.zero; monInner.anchorMax = Vector2.one;
            monInner.offsetMin = new Vector2(2, 2); monInner.offsetMax = new Vector2(-2, -2);
            var camCap = UIFactory.Label(mon, "camcap", "CAMERA", 11, UIFactory.InkDim, TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(camCap.rectTransform, 0, 2, 296, 16);
            _monitorOutput = UIFactory.Label(monInner, "out", "SCANNER READY", 13, new Color(0.55f, 0.75f, 0.55f));
            CheckpointLayout.PlaceLocal(_monitorOutput.rectTransform, 8, 18, 272, 116);
        }

        private void StylePaperRow(Button b)
        {
            b.GetComponent<Image>().color = new Color(0.74f, 0.73f, 0.69f);
            var inner = b.transform.GetChild(0).GetComponent<Image>();
            inner.color = new Color(0.80f, 0.79f, 0.75f);
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, 11);
            t.color = UIFactory.PaperInk;
            t.alignment = TextAnchor.MiddleLeft;
            UIFactory.AnchorsFull(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(4, 0);
            t.rectTransform.offsetMax = new Vector2(-2, 0);
        }

        private Text Row(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 11, UIFactory.PaperInk);
            CheckpointLayout.PlaceLocal(t.rectTransform, 14, RecordRules[rowIndex] - 14, 292, 14);
            return t;
        }

        private Text IdRow(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 12, UIFactory.PaperInk);
            CheckpointLayout.PlaceLocal(t.rectTransform, 112, IdCardRules[rowIndex] - 16, 200, 16);
            return t;
        }

        private void BuildControls()
        {
            var qPanel = UIFactory.Img(_root, "qpanel", ProceduralArt.Get("rough_panel"));
            CheckpointLayout.Place(qPanel.rectTransform, CheckpointLayout.Questions, CheckpointLayout.Pin.TopRight);
            var qHead = UIFactory.Label(qPanel.rectTransform, "head", "QUESTIONS", 14, UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(qHead.rectTransform, 12, 6, 140, 20);

            var qCol = UIFactory.NewUI(_root, "questions");
            _qColRt = qCol;
            CheckpointLayout.Place(qCol, CheckpointLayout.Questions, CheckpointLayout.Pin.TopRight);
            _budgetLabel = UIFactory.Label(qCol, "budget", "QUESTIONS: 2 LEFT", 11,
                new Color(0.35f, 0.35f, 0.34f), TextAnchor.MiddleRight);
            CheckpointLayout.PlaceLocal(_budgetLabel.rectTransform, 146, 8, 138, 16);
            string[] qs =
            {
                "1 · Your course?", "2 · Your year?",
                "3 · Regular or transferee?", "4 · First classroom?",
            };
            var qids = QuestionSystem.All;
            for (int i = 0; i < 4; i++)
            {
                int qi = i;
                var b = UIFactory.StandardButton(qCol, "q" + i, qs[i], () => AskQuestion(qids[qi]), 272, 38);
                CheckpointLayout.PlaceLocal(b.GetComponent<RectTransform>(), 12, 30 + i * 42, 272, 38);
                _questionButtons[i] = b;
            }
            _testimony = UIFactory.Label(qCol, "testimony", "", 11, new Color(0.30f, 0.30f, 0.29f), TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(_testimony.rectTransform, 12, 200, 272, 54);

            _allowBtn = UIFactory.StandardButton(_root, "allow", "ALLOW", () => Stamp(Verdict.Allow), 110, 110);
            MakeRound(_allowBtn, true);
            CheckpointLayout.Place(_allowBtn.GetComponent<RectTransform>(), CheckpointLayout.StampAllow, CheckpointLayout.Pin.BottomRight);
            _rejectBtn = UIFactory.StandardButton(_root, "reject", "CALL PRINCIPAL", () => AskPrincipal(), 110, 110, true);
            MakeRound(_rejectBtn, false);
            CheckpointLayout.Place(_rejectBtn.GetComponent<RectTransform>(), CheckpointLayout.StampReject, CheckpointLayout.Pin.BottomRight);

            _scanBtn = UIFactory.StandardButton(_root, "scan", "SCAN ID", () => DoScan(), 140, 36);
            CheckpointLayout.Place(_scanBtn.GetComponent<RectTransform>(), CheckpointLayout.UtilityScan, CheckpointLayout.Pin.TopRight);
            _recordsBtn = UIFactory.StandardButton(_root, "records", "TONIGHT'S LOG", () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, 140, 36);
            CheckpointLayout.Place(_recordsBtn.GetComponent<RectTransform>(), CheckpointLayout.UtilityRecords, CheckpointLayout.Pin.TopRight);
            _mapBtn = UIFactory.StandardButton(_root, "map", "MAP", () => _gameRoot.OpenMap(), 140, 36);
            CheckpointLayout.Place(_mapBtn.GetComponent<RectTransform>(), CheckpointLayout.UtilityMap, CheckpointLayout.Pin.TopRight);
            _notesBtn = UIFactory.StandardButton(_root, "notes", "RULES NOTE", () => ToggleOverlay(_notesRoot), 140, 36);
            CheckpointLayout.Place(_notesBtn.GetComponent<RectTransform>(), CheckpointLayout.UtilityNotes, CheckpointLayout.Pin.TopRight);

            _patrolBtn = UIFactory.StandardButton(_root, "patrol", "PATROL HALLWAY  ▸", () => GoPatrol(), 180, 36);
            CheckpointLayout.Place(_patrolBtn.GetComponent<RectTransform>(), CheckpointLayout.PatrolBtn, CheckpointLayout.Pin.BottomCenter);
            _endShiftBtn = UIFactory.StandardButton(_root, "endshift", "END SHIFT — CLOCK OUT", () => _gameRoot.FinishShift(), 200, 36);
            CheckpointLayout.Place(_endShiftBtn.GetComponent<RectTransform>(), CheckpointLayout.EndShiftBtn, CheckpointLayout.Pin.BottomCenter);
            _endShiftBtn.gameObject.SetActive(false);
        }

        private void BuildOverlays()
        {
            _bannerRoot = new GameObject("Banner");
            var brt = UIFactory.NewUI(_bannerRoot.transform, "inner");
            _bannerRoot.transform.SetParent(_root, false);
            CheckpointLayout.Place(brt, CheckpointLayout.VerdictBanner, CheckpointLayout.Pin.Center);
            var bimg = UIFactory.Panel(brt, "bg", new Color(0.04f, 0.04f, 0.05f, 0.97f));
            UIFactory.AnchorsFull(bimg.rectTransform);
            var border = UIFactory.Panel(brt, "borderline", UIFactory.Border).rectTransform;
            border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(2, 2); border.offsetMax = new Vector2(-2, -2);
            border.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.97f);
            _bannerTitle = UIFactory.Label(brt, "title", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(_bannerTitle.rectTransform, 20, 22, 600, 44);
            _bannerBody = UIFactory.Label(brt, "body", "", 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(_bannerBody.rectTransform, 24, 70, 592, 164);
            _bannerRoot.SetActive(false);

            _notesRoot = BuildPaperOverlay("note_paper", "PAPER NOTE — INSPECTION RULES", out _notesText);
            _recordsRoot = BuildPaperOverlay("record_sheet", "TONIGHT'S LOG — ANOMALY RECORD", out _recordsText);
        }

        private GameObject BuildPaperOverlay(string spriteKey, string title, out Text body)
        {
            var go = new GameObject("Overlay_" + title);
            go.transform.SetParent(_root, false);
            UIFactory.Dimmer(go.transform, "dim", 0.6f);
            var panel = UIFactory.Img(go.transform, "paper", ProceduralArt.Get(spriteKey));
            CheckpointLayout.Place(panel.rectTransform, CheckpointLayout.PaperOverlay, CheckpointLayout.Pin.Center);
            var head = UIFactory.Label(panel.rectTransform, "title", title, 20, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(head.rectTransform, 30, 22, 700, 32);
            body = UIFactory.Label(panel.rectTransform, "body", "", 16, UIFactory.PaperInk);
            CheckpointLayout.PlaceLocal(body.rectTransform, 40, 62, 680, 344);
            var close = UIFactory.StandardButton(panel.rectTransform, "close", "CLOSE [X]", () => go.SetActive(false), 140, 38);
            CheckpointLayout.PlaceLocal(close.GetComponent<RectTransform>(), 310, 416, 140, 38);
            go.SetActive(false);
            return go;
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
            _flowActive = true;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
            if (_gameRoot.Session.Mode == GameMode.Challenge)
            {
                var m = _gameRoot.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText))
                {
                    _mutationStrip.text = "⚠ DISTRICT BULLETIN — " + m.RuleText;
                    _mutationStrip.gameObject.SetActive(true);
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
            _flowActive = true;
            _waitingReturnFromPatrol = false;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
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

        private void MakeRound(Button b, bool paper)
        {
            var border = b.GetComponent<Image>();
            border.sprite = ProceduralArt.Get("rough_circle");
            border.color = paper ? UIFactory.Paper : new Color(0.10f, 0.10f, 0.11f);
            var inner = b.transform.GetChild(0).GetComponent<Image>();
            inner.sprite = ProceduralArt.Get("rough_circle");
            inner.color = paper ? UIFactory.Paper : new Color(0.13f, 0.13f, 0.14f);
            var t = b.GetComponentInChildren<Text>();
            UIFactory.SetBaseFontSize(t, 13);
            t.color = paper ? UIFactory.PaperInk : UIFactory.Ink;
        }

        private void BuildDecor()
        {
            var note = UIFactory.Img(_root, "principalnote", ProceduralArt.Get("rough_panel"));
            CheckpointLayout.Place(note.rectTransform, CheckpointLayout.Note, CheckpointLayout.Pin.BottomCenter);
            var head = UIFactory.Label(note.rectTransform, "head", "PRINCIPAL NOTE", 13, UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(head.rectTransform, 10, 6, 170, 18);
            _noteText = UIFactory.Label(note.rectTransform, "body", "", 11, UIFactory.PaperInk, TextAnchor.UpperLeft);
            CheckpointLayout.PlaceLocal(_noteText.rectTransform, 10, 26, 260, 78);

            var conf = UIFactory.Img(_root, "confirmbox", ProceduralArt.Get("rough_panel"));
            CheckpointLayout.Place(conf.rectTransform, CheckpointLayout.ConfirmBox, CheckpointLayout.Pin.Center);
            _confirmText = UIFactory.Label(conf.rectTransform, "txt", "", 14, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            CheckpointLayout.PlaceLocal(_confirmText.rectTransform, 20, 12, 380, 40);
            var no = UIFactory.StandardButton(conf.rectTransform, "no", "No", () => _confirmBox.SetActive(false), 90, 30);
            CheckpointLayout.PlaceLocal(no.GetComponent<RectTransform>(), 150, 64, 90, 30);
            var yes = UIFactory.StandardButton(conf.rectTransform, "yes", "Yes",
                () => { _confirmBox.SetActive(false); Stamp(Verdict.Reject); }, 90, 30);
            CheckpointLayout.PlaceLocal(yes.GetComponent<RectTransform>(), 250, 64, 90, 30);
            _confirmBox = conf.gameObject;
            _confirmBox.SetActive(false);
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
                    SetVisitorVisible(false);
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
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
            _endShiftBtn.gameObject.SetActive(false);
            _patrolBtn.gameObject.SetActive(false);
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

            _idName.text = (admin ? "NAME: " : "NAME: ") + v.ClaimedName;
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

        private void SayLine(string who, string text)
        {
            _speechSpeaker.text = who;
            _speechText.text = text;
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
            SayLine("ANSWER · " + QuestionTextShort(q), "“" + answer.Text + "”" + tone);
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
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
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
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
                }
                else
                {
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "Next one's crossing the yard. Patrol now if you need to.");
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
            for (int i = 0; i < 4; i++)
                UIFactory.SetButtonEnabled(_questionButtons[i], _current != null && !_verdictLocked && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            UIFactory.SetButtonEnabled(_allowBtn, _current != null && !_verdictLocked);
            UIFactory.SetButtonEnabled(_rejectBtn, _current != null && !_verdictLocked);
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
