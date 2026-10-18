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
    /// <summary>The security-room checkpoint: fixed point-and-click inspection station
    /// (approved §2.1). Presents the visitor at the window, the Student ID, the record form,
    /// the clipboard checklist, the paper note (rules), the scanner/database, the map button
    /// and the permanent Allow/Reject stamps. Also runs Endless/Challenge flows and the
    /// Shift-5 administrator encounter.</summary>
    public class CheckpointView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private RectTransform _root;

        /// <summary>The visitor currently at the window (null between visitors).</summary>
        public VisitorData CurrentVisitor => _current;

        // flow state
        private List<VisitorData> _roster;
        private ShiftDef _shift;
        private int _index = -1;
        private VisitorData _current;
        private QuestionSystem.Budget _budget = new QuestionSystem.Budget();
        private int[] _checklist = new int[8]; // 0 = blank, 1 = ok, 2 = issue (8 rows)
        private bool _verdictLocked = true;
        private bool _flowActive;
        private bool _waitingReturnFromPatrol;

        // top bar
        private Text _topLeft, _topClock;
        private Text _runStats;      // endless/challenge: score · streak · timer
        private Text _mutationStrip; // challenge bulletin

        // window area
        private Image _visitorPortrait;
        private RectTransform _speechPanel;
        private Text _speechSpeaker, _speechText;
        private Image _glitchOverlay;

        // desk documents
        private Text _idName, _idNumber, _idYear, _idCourse, _idExpires, _idStatus, _idHeader;
        private Image _idPhoto;
        private Text _recName, _recNumber, _recYear, _recCourse, _recStatus, _recClassroom, _recHeader, _recEnrolled;
        private Text[] _checkRows = new Text[8];
        private Text _monitorOutput;
        private StringBuilder _tonightsLog = new StringBuilder();

        // controls
        private Button[] _questionButtons = new Button[4];
        private Text _budgetLabel;
        private Button _allowBtn, _rejectBtn, _scanBtn, _notesBtn, _recordsBtn, _mapBtn, _patrolBtn, _endShiftBtn;

        // overlays
        private GameObject _bannerRoot; private Text _bannerTitle, _bannerBody;
        private Image _stampOverlay;
        private GameObject _notesRoot; private Text _notesText;
        private GameObject _recordsRoot; private Text _recordsText;

        private static readonly string[] ChecklistLabels =
        {
            "ID number format", "Photo matches person", "Expiration valid", "Year level matches",
            "Course matches", "Status matches", "Record name matches", "Behavior / answers",
        };

        // ---------------------------------------------------------------- build

        public void Init(GameRoot root)
        {
            R = root;
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

            R.Bus.Subscribe(EventIds.RunTimerTick, e => OnRunTimerTick(e.Value));
            R.Bus.Subscribe(EventIds.MutationChanged, e =>
            {
                var m = e.Payload<RuleMutationDef>();
                if (_mutationStrip != null)
                {
                    _mutationStrip.text = m != null && !string.IsNullOrEmpty(m.RuleText)
                        ? "⚠ DISTRICT BULLETIN — " + m.RuleText : "";
                    _mutationStrip.gameObject.SetActive(m != null && !string.IsNullOrEmpty(m.RuleText));
                }
            });
            gameObject.SetActive(false);
        }

        private void BuildTopBar()
        {
            var bar = UIFactory.Panel(_root, "topbar", new Color(0.05f, 0.05f, 0.06f, 0.92f)).rectTransform;
            bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, 52); bar.anchoredPosition = Vector2.zero;

            _topLeft = UIFactory.Label(bar, "left", "", 19, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_topLeft.rectTransform, new Vector2(0, 0.5f), new Vector2(640, 44), new Vector2(332, 0));

            _topClock = UIFactory.Label(bar, "clock", "22:00", 22, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_topClock.rectTransform, new Vector2(1, 0.5f), new Vector2(160, 44), new Vector2(-140, 0));

            var pause = UIFactory.StandardButton(bar, "pause", "II  PAUSE", () => R.TogglePause(true), 104, 34);
            UIFactory.Anchor(pause.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(104, 34), new Vector2(-60, 0));

            _runStats = UIFactory.Label(bar, "runstats", "", 17, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_runStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420, 40), new Vector2(60, 0));
            _runStats.gameObject.SetActive(false);

            _mutationStrip = UIFactory.Label(_root, "mutation", "", 15, new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
            var msRt = _mutationStrip.rectTransform;
            msRt.anchorMin = new Vector2(0, 1); msRt.anchorMax = new Vector2(1, 1);
            msRt.pivot = new Vector2(0.5f, 1);
            msRt.sizeDelta = new Vector2(-24, 34); msRt.anchoredPosition = new Vector2(0, -56);
            _mutationStrip.gameObject.SetActive(false);
        }

        private void BuildWindowArea()
        {
            // visitor at the security window (bg art frames x 24%..76%, y ~8%..52%)
            _visitorPortrait = UIFactory.Img(_root, "visitor", null);
            UIFactory.Anchor(_visitorPortrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(240, 296), new Vector2(0, -244));
            _visitorPortrait.preserveAspect = true;

            _glitchOverlay = UIFactory.Img(_root, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            UIFactory.Anchor(_glitchOverlay.rectTransform, new Vector2(0.5f, 1f), new Vector2(256, 300), new Vector2(0, -212));

            _speechPanel = UIFactory.Panel(_root, "speech", new Color(0.86f, 0.85f, 0.82f, 0.96f)).rectTransform;
            UIFactory.Anchor(_speechPanel, new Vector2(0.5f, 1f), new Vector2(330, 120), new Vector2(300, -150));
            _speechSpeaker = UIFactory.Label(_speechPanel, "who", "", 14, UIFactory.PaperInk, TextAnchor.UpperLeft);
            UIFactory.Anchor(_speechSpeaker.rectTransform, new Vector2(0, 1), new Vector2(310, 20), new Vector2(170, -14));
            _speechText = UIFactory.Label(_speechPanel, "text", "", 17, UIFactory.PaperInk, TextAnchor.UpperLeft);
            var stRt = _speechText.rectTransform;
            stRt.anchorMin = new Vector2(0, 0); stRt.anchorMax = new Vector2(1, 1);
            stRt.offsetMin = new Vector2(12, 8); stRt.offsetMax = new Vector2(-12, -36);
            _speechPanel.gameObject.SetActive(false);
        }

        private void BuildDesk()
        {
            // ---- Student ID card (left) ----
            var idCard = UIFactory.Img(_root, "idcard", ProceduralArt.Get("id_card_frame"));
            UIFactory.Anchor(idCard.rectTransform, new Vector2(0, 0), new Vector2(286, 186), new Vector2(158, 122));
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 13,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_idHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(270, 26), new Vector2(0, -14));
            _idPhoto = UIFactory.Img(idCard.rectTransform, "photo", null);
            UIFactory.Anchor(_idPhoto.rectTransform, new Vector2(0, 0), new Vector2(84, 104), new Vector2(58, 74));
            _idPhoto.preserveAspect = true;
            _idName = UIFactory.Label(idCard.rectTransform, "name", "", 14, UIFactory.PaperInk);
            UIFactory.Anchor(_idName.rectTransform, new Vector2(0, 0), new Vector2(170, 22), new Vector2(196, 152));
            _idNumber = UIFactory.Label(idCard.rectTransform, "num", "", 14, UIFactory.PaperInk);
            UIFactory.Anchor(_idNumber.rectTransform, new Vector2(0, 0), new Vector2(170, 22), new Vector2(196, 126));
            _idYear = UIFactory.Label(idCard.rectTransform, "year", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idYear.rectTransform, new Vector2(0, 0), new Vector2(170, 20), new Vector2(196, 102));
            _idCourse = UIFactory.Label(idCard.rectTransform, "course", "", 12, UIFactory.PaperInk);
            UIFactory.Anchor(_idCourse.rectTransform, new Vector2(0, 0), new Vector2(176, 20), new Vector2(199, 80));
            _idStatus = UIFactory.Label(idCard.rectTransform, "status", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idStatus.rectTransform, new Vector2(0, 0), new Vector2(170, 20), new Vector2(196, 58));
            _idExpires = UIFactory.Label(idCard.rectTransform, "exp", "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(_idExpires.rectTransform, new Vector2(0, 0), new Vector2(200, 20), new Vector2(112, 30));

            // ---- Student record form (middle) ----
            var rec = UIFactory.Img(_root, "record", ProceduralArt.Get("record_sheet"));
            UIFactory.Anchor(rec.rectTransform, new Vector2(0, 0), new Vector2(238, 300), new Vector2(436, 160));
            _recHeader = UIFactory.Label(rec.rectTransform, "header", "STUDENT RECORD — FORM 11-C", 13,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_recHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(224, 30), new Vector2(0, -16));
            _recName = Row(rec.rectTransform, "rname", 250);
            _recNumber = Row(rec.rectTransform, "rnum", 224);
            _recYear = Row(rec.rectTransform, "ryear", 198);
            _recCourse = Row(rec.rectTransform, "rcourse", 172);
            _recStatus = Row(rec.rectTransform, "rstatus", 146);
            _recClassroom = Row(rec.rectTransform, "rroom", 120);
            _recEnrolled = Row(rec.rectTransform, "renrolled", 94);

            // verdict stamp that slams onto the record (That's-Not-My-Neighbor style feedback);
            // respects Reduced Motion (no punch, instant) and never blocks clicks
            _stampOverlay = UIFactory.Img(rec.rectTransform, "stampoverlay", null, new Color(1, 1, 1, 0f));
            UIFactory.Anchor(_stampOverlay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(132, 132), new Vector2(56, 100));
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            // ---- Clipboard checklist ----
            var clip = UIFactory.Img(_root, "clipboard", ProceduralArt.Get("clipboard"));
            UIFactory.Anchor(clip.rectTransform, new Vector2(0, 0), new Vector2(180, 238), new Vector2(650, 128));
            var clipTitle = UIFactory.Label(clip.rectTransform, "title", "CHECKLIST", 13, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.Anchor(clipTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(160, 22), new Vector2(0, -34));
            for (int i = 0; i < 8; i++)
            {
                int row = i;
                var t = UIFactory.Label(clip.rectTransform, "row" + i, "[  ] " + ChecklistLabels[i], 12, UIFactory.PaperInk);
                UIFactory.Anchor(t.rectTransform, new Vector2(0, 1), new Vector2(166, 20), new Vector2(90, -60 - i * 21));
                _checkRows[i] = t;
                var btn = t.gameObject.AddComponent<Button>();
                btn.targetGraphic = t;
                btn.onClick.AddListener(() => CycleCheckRow(row));
            }

            // ---- Scanner monitor ----
            var mon = UIFactory.Panel(_root, "monitor", new Color(0.06f, 0.07f, 0.06f)).rectTransform;
            UIFactory.Anchor(mon, new Vector2(1, 0), new Vector2(252, 118), new Vector2(-142, 359));
            var monBorder = UIFactory.Panel(mon, "border", UIFactory.Border).rectTransform;
            UIFactory.AnchorsFull(monBorder);
            var monInner = UIFactory.Panel(monBorder, "inner", new Color(0.03f, 0.04f, 0.03f)).rectTransform;
            monInner.anchorMin = Vector2.zero; monInner.anchorMax = Vector2.one;
            monInner.offsetMin = new Vector2(2, 2); monInner.offsetMax = new Vector2(-2, -2);
            _monitorOutput = UIFactory.Label(monInner, "out", "SCANNER READY", 13, new Color(0.55f, 0.75f, 0.55f));
            UIFactory.AnchorsFull(_monitorOutput.rectTransform);
            _monitorOutput.rectTransform.offsetMin = new Vector2(10, 8);
            _monitorOutput.rectTransform.offsetMax = new Vector2(-10, -8);
        }

        private Text Row(RectTransform parent, string name, float yFromTop)
        {
            var t = UIFactory.Label(parent, name, "", 13, UIFactory.PaperInk);
            UIFactory.Anchor(t.rectTransform, new Vector2(0, 1), new Vector2(220, 20), new Vector2(120, -yFromTop + 60));
            return t;
        }

        private void BuildControls()
        {
            // questions column (right)
            var qCol = UIFactory.NewUI(_root, "questions");
            UIFactory.Anchor(qCol, new Vector2(1, 0), new Vector2(260, 200), new Vector2(-146, 181));
            _budgetLabel = UIFactory.Label(qCol, "budget", "QUESTIONS: 2 LEFT", 14, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_budgetLabel.rectTransform, new Vector2(0, 1), new Vector2(260, 24), new Vector2(130, -12));
            string[] qs =
            {
                "1 · Your course?", "2 · Your year?",
                "3 · Regular or transferee?", "4 · First classroom?",
            };
            var qids = QuestionSystem.All;
            for (int i = 0; i < 4; i++)
            {
                int qi = i;
                var b = UIFactory.StandardButton(qCol, "q" + i, qs[i], () => AskQuestion(qids[qi]), 260, 40);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(260, 40), new Vector2(130, -44 - i * 44));
                _questionButtons[i] = b;
            }

            // stamps
            _allowBtn = UIFactory.StandardButton(_root, "allow", "✓ ALLOW", () => Stamp(Verdict.Allow), 138, 62);
            UIFactory.Anchor(_allowBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(138, 62), new Vector2(-228, 44));
            _allowBtn.targetGraphic.color = new Color(0.75f, 0.75f, 0.73f);
            _rejectBtn = UIFactory.StandardButton(_root, "reject", "✗ REJECT", () => Stamp(Verdict.Reject), 138, 62, true);
            UIFactory.Anchor(_rejectBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(138, 62), new Vector2(-76, 44));

            // tools column (left of stamps)
            _scanBtn = UIFactory.StandardButton(_root, "scan", "SCAN ID", () => DoScan(), 130, 38);
            UIFactory.Anchor(_scanBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 277));
            _notesBtn = UIFactory.StandardButton(_root, "notes", "RULES NOTE", () => ToggleOverlay(_notesRoot), 130, 38);
            UIFactory.Anchor(_notesBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 233));
            _recordsBtn = UIFactory.StandardButton(_root, "records", "TONIGHT'S LOG", () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, 130, 38);
            UIFactory.Anchor(_recordsBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 189));
            _mapBtn = UIFactory.StandardButton(_root, "map", "MAP", () => R.OpenMap(), 130, 38);
            UIFactory.Anchor(_mapBtn.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(130, 38), new Vector2(-344, 145));

            // patrol + end shift (bottom left)
            _patrolBtn = UIFactory.StandardButton(_root, "patrol", "PATROL HALLWAY  ▸", () => GoPatrol(), 220, 44);
            UIFactory.Anchor(_patrolBtn.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(220, 44), new Vector2(124, 30));
            _endShiftBtn = UIFactory.StandardButton(_root, "endshift", "END SHIFT — CLOCK OUT", () => R.FinishShift(), 250, 44);
            UIFactory.Anchor(_endShiftBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(250, 44), new Vector2(0, 30));
            _endShiftBtn.gameObject.SetActive(false);
        }

        private void BuildOverlays()
        {
            // result banner
            _bannerRoot = new GameObject("Banner");
            var brt = UIFactory.NewUI(_bannerRoot.transform, "inner");
            _bannerRoot.transform.SetParent(_root, false);
            UIFactory.Anchor(brt, new Vector2(0.5f, 0.5f), new Vector2(640, 250), new Vector2(0, 60));
            var bimg = UIFactory.Panel(brt, "bg", new Color(0.04f, 0.04f, 0.05f, 0.97f));
            UIFactory.AnchorsFull(bimg.rectTransform);
            var border = UIFactory.Panel(brt, "borderline", UIFactory.Border).rectTransform;
            border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
            border.offsetMin = new Vector2(2, 2); border.offsetMax = new Vector2(-2, -2);
            border.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.97f);
            _bannerTitle = UIFactory.Label(brt, "title", "", 34, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_bannerTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 52), new Vector2(0, -34));
            _bannerBody = UIFactory.Label(brt, "body", "", 16, UIFactory.InkDim, TextAnchor.UpperCenter);
            var bbRt = _bannerBody.rectTransform;
            bbRt.anchorMin = new Vector2(0, 0); bbRt.anchorMax = new Vector2(1, 1);
            bbRt.offsetMin = new Vector2(24, 16); bbRt.offsetMax = new Vector2(-24, -70);
            _bannerRoot.SetActive(false);

            // rules note overlay
            _notesRoot = BuildPaperOverlay("note_paper", "PAPER NOTE — INSPECTION RULES", out _notesText);
            _recordsRoot = BuildPaperOverlay("record_sheet", "TONIGHT'S LOG — ANOMALY RECORD", out _recordsText);
        }

        private GameObject BuildPaperOverlay(string spriteKey, string title, out Text body)
        {
            var go = new GameObject("Overlay_" + title);
            go.transform.SetParent(_root, false);
            UIFactory.Dimmer(go.transform, "dim", 0.6f);
            var panel = UIFactory.Img(go.transform, "paper", ProceduralArt.Get(spriteKey));
            UIFactory.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760, 480), Vector2.zero);
            var head = UIFactory.Label(panel.rectTransform, "title", title, 20, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.Anchor(head.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 40), new Vector2(0, -30));
            body = UIFactory.Label(panel.rectTransform, "body", "", 16, UIFactory.PaperInk);
            var bRt = body.rectTransform;
            bRt.anchorMin = new Vector2(0, 0); bRt.anchorMax = new Vector2(1, 1);
            bRt.offsetMin = new Vector2(40, 64); bRt.offsetMax = new Vector2(-40, -70);
            var close = UIFactory.StandardButton(panel.rectTransform, "close", "CLOSE [X]", () => go.SetActive(false), 140, 38);
            UIFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(140, 38), new Vector2(0, 26));
            go.SetActive(false);
            return go;
        }

        private void ToggleOverlay(GameObject go)
        {
            bool on = !go.activeSelf;
            go.SetActive(on);
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, on ? "sfx_ui_open" : "sfx_ui_close", 0.5f, null));
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
            if (R.Session.Mode == GameMode.Challenge)
            {
                var m = R.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText)) sb.Append('\n').Append("▸ ").Append(m.RuleText);
            }
            _notesText.text = sb.ToString();
        }

        private void RefreshRecords()
        {
            var sb = new StringBuilder();
            sb.Append("PRESENTED TONIGHT:\n");
            sb.Append(_tonightsLog.Length == 0 ? "  (no one yet)\n" : _tonightsLog.ToString());
            sb.Append("\nBREACHES (anomalies allowed in): ").Append(R.Session.Breaches).Append(" / 3\n");
            sb.Append("FALSE REJECTIONS: ").Append(R.Session.FalseRejections).Append(" / 3\n");
            sb.Append("FINDINGS DISCOVERED: ").Append(R.Session.Clues.FoundCount).Append(" / 10\n");
            _recordsText.text = sb.ToString();
        }

        // ---------------------------------------------------------------- show/hide

        public void Show() { gameObject.SetActive(true); RefreshHud(); }
        public void Hide()
        {
            gameObject.SetActive(false);
            _bannerRoot.SetActive(false);
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
        }

        // ---------------------------------------------------------------- run flow

        /// <summary>[Flow] breadcrumb: the desk actually started with a roster.</summary>
        public void BeginRun(List<VisitorData> roster, ShiftDef shift)
        {
            Debug.Log("[Flow] checkpoint started with " + (roster != null ? roster.Count : 0) + " visitors");
            _roster = roster;
            _shift = shift;
            _index = -1;
            R.CurrentVisitorIndex = 0;
            _tonightsLog.Clear();
            _flowActive = true;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
            if (R.Session.Mode == GameMode.Challenge)
            {
                var m = R.Session.ActiveMutation;
                if (m != null && !string.IsNullOrEmpty(m.RuleText))
                {
                    _mutationStrip.text = "⚠ DISTRICT BULLETIN — " + m.RuleText;
                    _mutationStrip.gameObject.SetActive(true);
                }
            }
            bool runMode = R.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            NextVisitor();
        }

        public void ResumeAfterExploration()
        {
            if (!_waitingReturnFromPatrol) return;
            _waitingReturnFromPatrol = false;
            StartCoroutine(DelayedNext(0.6f));
        }

        /// <summary>Scene-reload resume: continues the same shift's roster at the shared
        /// cursor (used when returning from the School scene / Continue).</summary>
        public void ResumeRun()
        {
            _shift = ShiftPlan.Get(Mathf.Clamp(R.Session.CurrentShift, 1, ShiftPlan.TotalShifts));
            _roster = R.CurrentRoster;
            _index = R.CurrentVisitorIndex - 1;
            _tonightsLog.Clear();
            _flowActive = true;
            _waitingReturnFromPatrol = false;
            _endShiftBtn.gameObject.SetActive(false);
            _mutationStrip.gameObject.SetActive(false);
            bool runMode = R.Session.Mode != GameMode.Normal;
            _runStats.gameObject.SetActive(runMode);
            Show();
            NextVisitor();
        }

        private IEnumerator DelayedNext(float s)
        {
            yield return WaitUnpaused(s);
            if (_flowActive && gameObject.activeInHierarchy) NextVisitor();
        }

        /// <summary>Realtime wait that freezes while the game is paused, so pausing during a
        /// banner never lets the flow advance underneath the pause menu.</summary>
        private IEnumerator WaitUnpaused(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (R == null || !R.Paused) t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void NextVisitor()
        {
            if (!_flowActive) return;
            VisitorData v;
            if (R.Session.Mode == GameMode.Normal)
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
                v = R.NextRunVisitor();
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
                _recYear.text = "EFFECTIVE: " + Content.ShiftDates[Mathf.Clamp(R.Session.CurrentShift - 1, 0, 4)];
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

            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_knock", 0.7f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.DocumentsShown, v));
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_paper", 0.6f, null));

            if (admin)
            {
                var lines = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[0] },
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[1] },
                };
                if (R.Session.Clues.FoundCount >= 6)
                    lines.Add(new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "That face. The old school photo — the staff member who never aged. It's them." });
                lines.Add(new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[3] });
                SayLine("AT THE WINDOW", "A formal coat. A briefcase. A smile that arrives a half-second late.");
                if (R.Dialogue != null) R.Dialogue.Play(lines, null);
                R.Bus.Publish(GameEvent.Of(EventIds.MusicContextRequested, "final"));
            }
            else
            {
                Say(v.Greeting);
                if (v.IsAnomaly && v.BehaviorGlitchVisible && R.Session.Sanity < 70f)
                    StartCoroutine(GlitchHint());
            }
            R.Bus.Publish(GameEvent.Of(EventIds.VisitorArrived, v));
        }

        private IEnumerator GlitchHint()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            if (_current == null || !gameObject.activeInHierarchy) yield break;
            _glitchOverlay.gameObject.SetActive(true);
            var c = _glitchOverlay.color; c.a = 0.35f; _glitchOverlay.color = c;
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.35f, null));
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

        // ---------------------------------------------------------------- interactions

        public void AskQuestion(QuestionId q)
        {
            if (_current == null || _verdictLocked) return;
            if (!_budget.TryAsk(_current, q, out var answer))
            {
                _monitorOutput.text = _budget.HasAsked(q) ? "ALREADY ASKED THAT." : "NO MORE QUESTIONS — DISTRICT POLICY.";
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_scan_error", 0.35f, null));
                return;
            }
            string tone = answer.Tone == ToneTag.Normal ? "" : "  [" + ToneLabel(answer.Tone) + "]";
            SayLine("ANSWER · " + QuestionTextShort(q), "“" + answer.Text + "”" + tone);
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_intercom", 0.5f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.QuestionAsked, q));
            RefreshHud();
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
            if (_current == null || _verdictLocked || R.Roster == null) return;
            var res = R.Roster.Scan(_current, R.Session.TonightLog);
            _monitorOutput.text = res.Display;
            _monitorOutput.color = res.FoundInDatabase && !res.DuplicateTonight
                ? new Color(0.55f, 0.75f, 0.55f)
                : new Color(0.85f, 0.35f, 0.35f);
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested,
                res.FormatValid && res.FoundInDatabase && !res.DuplicateTonight ? "sfx_scan_ok" : "sfx_scan_error", 0.8f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.ScanPerformed, res));
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
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_paper", 0.35f, null));
        }

        public void Stamp(Verdict verdict)
        {
            if (_current == null || _verdictLocked) return;
            _verdictLocked = true; // §2.4 — decisions are permanent
            var mutation = R.Session.Mode == GameMode.Challenge ? R.Session.ActiveMutation : null;
            var outcome = InspectionRules.Apply(_current, verdict, mutation);

            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested,
                verdict == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 1f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, outcome));
            if (_stampOverlay != null) StartCoroutine(StampPunch(verdict));

            // log tonight's presentation
            _tonightsLog.Append("  ").Append(_current.IsAdministrator ? "ADMINISTRATOR" : _current.ClaimedName)
                .Append(" — ").Append(verdict == Verdict.Allow ? "ALLOWED" : "REJECTED")
                .Append(outcome.Correct ? " (correct)\n" : " (MISTAKE)\n");

            int checklistHits = CountChecklistAccuracy(_current, outcome);
            var resolved = _current;
            R.CurrentVisitorIndex = _index + 1;               // survive scene reloads (patrol)
            R.OnVerdictResolved(resolved, outcome);           // counters update BEFORE the banner reads them
            if (R.State == AppViewState.Ending) { Hide(); return; } // ending took over (C3/C4)
            ShowBanner(verdict, outcome, checklistHits);

            StartCoroutine(AfterBanner(resolved));
        }

        private int CountChecklistAccuracy(VisitorData v, InspectionRules.Outcome o)
        {
            // objective ground truth per checklist row
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
            // truth[i] = true when the row has an ISSUE
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
                    .Append(R.Session.FalseRejections).Append("/3 before the district reviews your contract)\n");
            else sb.Append("MISTAKE — ANOMALY ENTRY. Something is inside the school now. (")
                    .Append(R.Session.Breaches).Append("/3 before the district reviews your contract)\n");
            sb.Append("\nWHAT THE DOCUMENTS SHOWED:\n");
            foreach (var line in o.RevealedDefects) sb.Append("  · ").Append(line).Append('\n');
            sb.Append("\nYour checklist: ").Append(checklistHits).Append("/8 rows accurate.");
            _bannerBody.text = sb.ToString();
            _bannerRoot.SetActive(true);
        }

        private IEnumerator StampPunch(Verdict verdict)
        {
            _stampOverlay.sprite = ProceduralArt.Get(verdict == Verdict.Allow ? "stamp_allow" : "stamp_reject");
            bool reduce = R.Settings != null && R.Settings.ReduceMotion;
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
            bool reduce = R.Settings != null && R.Settings.ReduceMotion;
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
            yield return WaitUnpaused(R.Config.verdictBannerSeconds);
            _bannerRoot.SetActive(false);
            if (R.State != AppViewState.Checkpoint && R.State != AppViewState.AdminEncounter) yield break;

            if (resolved.IsAdministrator && R.Session.AdminRejected)
            {
                // administrator rejected — climax beat before clocking out
                var outro = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "…You will regret this. We are already inside your records." },
                    new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The thing wearing Marlowe's face unfolds itself from the window and walks back into the dark. The seal on the server room holds — for tonight." },
                };
                R.Dialogue.Play(outro, () =>
                {
                    SetVisitorVisible(false);
                    _endShiftBtn.gameObject.SetActive(true);
                    _patrolBtn.gameObject.SetActive(true);
                    Bus_MusicCheckpoint();
                });
                yield break;
            }

            if (R.Session.Mode == GameMode.Normal)
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
                    // breathing room: patrol prompt, then auto-next after a beat
                    _patrolBtn.gameObject.SetActive(true);
                    SayLine("RADIO", "Next one's crossing the yard. Patrol now if you need to.");
                    yield return new WaitForSecondsRealtime(R.Config.betweenVisitorsSeconds + 2.2f);
                    if (_flowActive && !_waitingReturnFromPatrol && R.State == AppViewState.Checkpoint)
                        NextVisitor();
                }
            }
            else
            {
                SetVisitorVisible(false);
                yield return WaitUnpaused(Mathf.Max(0.4f, R.Config.betweenVisitorsSeconds * 0.5f));
                if (_flowActive && R.State == AppViewState.Checkpoint) NextVisitor();
            }
        }

        private void Bus_MusicCheckpoint() => R.Bus.Publish(GameEvent.Of(EventIds.MusicContextRequested, "checkpoint"));

        public void GoPatrol()
        {
            if (_current != null && !_verdictLocked)
            {
                R.Bus.Publish(GameEvent.Of(EventIds.Notification, "Finish the inspection at the window first."));
                return;
            }
            _waitingReturnFromPatrol = true;
            R.EnterExploration();
        }

        private void Update()
        {
            if (R == null || !gameObject.activeInHierarchy) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !_bannerRoot.activeSelf) R.TogglePause();
            if (Input.GetKeyDown(KeyCode.M) && R.State == AppViewState.Checkpoint) R.OpenMap();
        }

        private void RefreshHud()
        {
            if (R == null || R.Session == null) return;
            if (R.Session.Mode == GameMode.Normal)
            {
                int total = _roster != null ? _roster.Count : 0;
                int shown = Mathf.Clamp(_index + 1, 0, total);
                _topLeft.text = "NIGHT " + R.Session.CurrentShift + " · " + (_shift != null ? _shift.DateLabel : "") +
                                "   —   VISITOR " + shown + " / " + total;
                _topClock.text = R.ClockString;
            }
            else
            {
                _topLeft.text = (R.Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL") +
                                "   ·   PROCESSED " + R.Session.RunProcessed;
                _topClock.text = R.ClockString;
                float mult = EndlessRules.StreakMultiplier(R.Session.Streak) *
                             (R.Session.Mode == GameMode.Challenge ? ChallengeRules.ScoreMultiplier : 1f);
                _runStats.text = "SCORE " + R.Session.Score + "   ·   STREAK " + R.Session.Streak + " (×" + mult.ToString("0.0") + ")";
            }
            _budgetLabel.text = "QUESTIONS: " + _budget.Remaining + " LEFT";
            for (int i = 0; i < 4; i++)
                UIFactory.SetButtonEnabled(_questionButtons[i], _current != null && !_verdictLocked && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            UIFactory.SetButtonEnabled(_allowBtn, _current != null && !_verdictLocked);
            UIFactory.SetButtonEnabled(_rejectBtn, _current != null && !_verdictLocked);
        }

        /// <summary>Endless/Challenge timer readout — subscribed via RunTimerTick for immediacy.</summary>
        public void OnRunTimerTick(float secondsLeft)
        {
            if (_runStats == null || !_runStats.gameObject.activeInHierarchy) return;
            int m = (int)(secondsLeft / 60f), s = (int)(secondsLeft % 60f);
            _runStats.text = "TIME " + m + ":" + s.ToString("D2") + "   ·   SCORE " + R.Session.Score +
                             "   ·   STREAK ×" + EndlessRules.StreakMultiplier(R.Session.Streak).ToString("0.0");
            _runStats.color = secondsLeft < 30f ? UIFactory.AccentBright : UIFactory.Ink;
        }

        private void OnEnable()
        {
            if (R != null && _runStats != null && R.Session.Mode != GameMode.Normal)
                _runStats.gameObject.SetActive(true);
        }
    }
}
