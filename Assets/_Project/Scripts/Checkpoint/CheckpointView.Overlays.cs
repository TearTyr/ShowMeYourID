using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Paper overlays: rules note, tonight's log, verdict banner sheet.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        private void BuildOverlays()
        {
            // there is no verdict banner: post-decision feedback is the stamp mark, the
            // sound and the subject walking off - never text

            _notesRoot = BuildPaperOverlay("OverlayRules", "sec_rules_paper", out _notesText);
            _recordsRoot = BuildPaperOverlay("OverlayLog", "sec_log_paper", out _recordsText);
            BuildLogBoxes();
            BuildReportList();

            // there is no yes/no confirm popup: stamping IS confirming
        }

        // THE REPORT LIST (nights 1 to 4): the slip under the log paper lists everyone the player let in, as
        // buttons. Clicking a name reports that person (one at a time, click again to take it back). At END SHIFT
        // a right report cancels that anomaly and a wrong one costs sanity (see NightLogRules).
        private const int ReportSlots = 8;   // the longest night lets in 8 at most
        private readonly Button[] _reportBtns = new Button[ReportSlots];
        private readonly int[] _reportRecord = new int[ReportSlots];   // which NightRecords entry each button stands for

        private void BuildReportList()
        {
            var slip = _recordsRoot.transform.Find("slip") as RectTransform;
            if (slip == null) return;
            // the typed text shrinks to a two line header, the buttons fill the rest of the slip
            var host = _recordsText.transform.parent as RectTransform;
            if (host != null) UIFactory.PlaceTL(host, 14f, 6f, 442f, 30f);
            const float colWidth = 215f, rowHeight = 19f, rowPitch = 20f;
            for (int i = 0; i < ReportSlots; i++)
            {
                int slot = i;
                var b = UIFactory.StandardButton(slip, "report_" + i, "", () => Report(slot), (int)colWidth, (int)rowHeight);
                SizeLabel(b, 11);
                UIFactory.PlaceTL(b.GetComponent<RectTransform>(), 14f + (i % 2) * (colWidth + 12f), 38f + (i / 2) * rowPitch, colWidth, rowHeight);
                SkinButton(b, "sec_btn_pill");
                b.gameObject.SetActive(false);
                _reportBtns[i] = b;
            }
        }

        private void Report(int slot)
        {
            if (!_gameRoot.LogRequired) return;
            _gameRoot.Session.ToggleReport(_reportRecord[slot]);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
            RefreshRecords();
            RefreshHud();   // the desk prompt stops asking for a report
        }

        /// <summary>True while the log could still be used tonight: someone was let in and nobody is reported yet.</summary>
        private bool ReportPending()
        {
            var session = _gameRoot.Session;
            if (!_gameRoot.LogRequired || session.ReportedRecord >= 0) return false;
            return ReportListHasNames();
        }

        // The reading sheet IS the owner's photographed document: sec_rules_paper /
        // sec_log_paper carry the crest, the hand title, the hand-written rules block (or
        // the log checklist with its boxes) AND the paper's tilt, printed at prop size.
        // Re-typesetting any of that over the photo double-printed the ink (the earlier
        // plain-sheet pass) and stretching the photo over a wide sheet dragged the baked
        // tilt under straight type. So: the paper is shown at its own aspect, untouched,
        // and the LIVE body - the per-night briefing / tonight's entries, which no photo
        // can carry - rides on a typed addendum slip pinned over the sheet's blank bottom
        // band, exactly where a clipped typed page would sit on a photographed document.
        // Nothing of either document's content is lost; nothing is stretched or re-inked.
        private GameObject BuildPaperOverlay(string goName, string kitKey, out Text body)
        {
            var goRt = UIFactory.NewUI(_desk, goName);
            var go = goRt.gameObject;
            L.Place(goRt, L.FullDesk);
            var dim = UIFactory.Dimmer(goRt, "dim", 0.66f);
            var dimClose = dim.gameObject.GetOrAdd<Button>();   // a click outside the paper closes it
            dimClose.targetGraphic = dim;
            dimClose.transition = Selectable.Transition.None;
            dimClose.onClick.AddListener(() => { go.SetActive(false); _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f); });
            // the photographed paper, centred at its own proportions (preserveAspect keeps
            // the 789x879 export undistorted whatever the sheet rect rounds to); no extra
            // rotation - the print already lies at its angle
            var panel = UIFactory.Img(goRt, "paper", ProceduralArt.Get(kitKey));
            L.Place(panel.rectTransform, L.PaperOverlay);
            panel.preserveAspect = true;
            panel.raycastTarget = false;
            // the typed addendum slip: distressed stock, a breath of tilt, over the sheet's
            // blank bottom band - the live body scrolls inside its own masked column
            var slip = UIFactory.Img(goRt, "slip", ProceduralArt.Get("paper_flat"), Color.white);
            L.Place(slip.rectTransform, L.Inner.PaperSlip);
            slip.raycastTarget = false;
            slip.rectTransform.localEulerAngles = new Vector3(0f, 0f, -1.5f);
            var slipEdge = UIFactory.Img(slip.rectTransform, "slipedge",
                ProceduralArt.Get("solid_white"), new Color(0f, 0f, 0f, 0.35f));
            UIFactory.PlaceTL(slipEdge.rectTransform, 2f, L.Inner.PaperSlip.H - 3f,
                L.Inner.PaperSlip.W - 4f, 3f);
            slipEdge.raycastTarget = false;
            body = Body(slip.rectTransform, "body", L.Inner.PaperSlipBody, 13, true);
            // the close pill keeps its wording and its click; it sits on the sheet's blank
            // top-right corner so the document's printed face stays clear
            var close = GridButton(goRt, "close", "CLOSE [X]",
                () => go.SetActive(false), L.Inner.PaperClose, 14);
            L.Place(close.GetComponent<RectTransform>(), L.Inner.PaperClose);
            SkinButton(close, "sec_btn_pill");
            var closeLbl = close.GetComponentInChildren<Text>();
            if (closeLbl != null) closeLbl.color = UIFactory.NsInk;
            go.SetActive(false);
            return go;
        }

        // The night security log's three boxes (STUDENT ENCOUNTERS, ANOMALIES DETECTED, SECURITY INCIDENTS).
        // The game fills them in by itself, nobody counts. ANOMALIES DETECTED is what the player rejected, not what
        // really was an anomaly, so the sheet never gives a verdict away. Each box is a plain object under
        // OverlayLog/paper, so when the paper art changes the boxes can simply be moved in the scene. Centres are
        // fractions of the paper picture, measured on the SecurityLog art (789x879 after the 2x cut).
        private readonly Text[] _logCounts = new Text[3];
        private static readonly Vector2[] LogBoxCentres =
            { new Vector2(0.822f, 0.498f), new Vector2(0.836f, 0.589f), new Vector2(0.848f, 0.680f) };
        private static readonly string[] LogBoxNames = { "student_encounters", "anomalies_detected", "security_incidents" };

        private void BuildLogBoxes()
        {
            var paper = _recordsRoot.transform.Find("paper") as RectTransform;
            if (paper == null) return;
            for (int i = 0; i < _logCounts.Length; i++)
            {
                var box = UIFactory.NewUI(paper, "logbox_" + LogBoxNames[i]);
                box.anchorMin = box.anchorMax = new Vector2(LogBoxCentres[i].x, 1f - LogBoxCentres[i].y);
                box.pivot = new Vector2(0.5f, 0.5f);
                box.sizeDelta = new Vector2(60f, 54f);
                box.anchoredPosition = Vector2.zero;
                var count = UIFactory.Label(box, "count", "0", 34, UIFactory.PaperInk, TextAnchor.MiddleCenter);
                count.font = UIFactory.HandFont;
                count.raycastTarget = false;
                UIFactory.AnchorsFull(count.rectTransform);
                _logCounts[i] = count;
            }
        }

        private void RefreshLogBoxes()
        {
            var counts = NightLogRules.Boxes(_gameRoot.Session.NightRecords, _gameRoot.NightIncidents());
            for (int i = 0; i < _logCounts.Length; i++)
                if (_logCounts[i] != null) _logCounts[i].text = counts[i].ToString();
        }

        /// <summary>Headless dumps open a reading sheet exactly as its desk prop click
        /// would - same refresh, same toggle, no shortcut path.</summary>
        internal void DebugOpenOverlay(string which)
        {
            if (which == "rules") { RefreshNotes(); ToggleOverlay(_notesRoot); }
            else if (which == "log") { RefreshRecords(); ToggleOverlay(_recordsRoot); }
        }

        /// <summary>Close the rules note / tonight's log if one is open (X key, Esc, or a click
        /// outside the paper). Returns whether anything was open.</summary>
        private bool CloseOpenSheets()
        {
            bool any = (_notesRoot != null && _notesRoot.activeSelf) || (_recordsRoot != null && _recordsRoot.activeSelf);
            if (!any) return false;
            _notesRoot.SetActive(false);
            _recordsRoot.SetActive(false);
            _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f);
            return true;
        }

        private Text Body(RectTransform parent, string name, L.Region r, int size, bool scroll)
        {
            return UIFactory.MaskedLabel(parent, name, r.X, r.Y, r.W, r.H, size,
                UIFactory.PaperInk, TextAnchor.UpperLeft, scroll);
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
            sb.Append("BASE RULES - Check every ID against the file in the drawer: number format, photo, expiry, year, course, status, name. Ask where they are going and check that too. Decisions are permanent.\n\n");
            // last night's log: the only place the player learns what the three boxes got wrong
            string lastLog = _gameRoot.Session.LogNote;
            if (!string.IsNullOrEmpty(lastLog))
                sb.Append("LAST NIGHT'S LOG\n").Append(lastLog).Append("\n\n");
            if (_gameRoot.LogRequired)
                sb.Append("SECURITY LOG - It fills itself in. Before END SHIFT you may report ONE person you let in. If they were an anomaly the night does not count it against you. If they were a real student it costs sanity.\n\n");
            if (_shift != null) { sb.Append(_shift.RuleNoteAddendum).Append('\n'); }
            _notesText.text = sb.ToString();
        }

        private void RefreshRecords()
        {
            RefreshLogBoxes();
            var session = _gameRoot.Session;
            var sb = new StringBuilder();
            if (!_gameRoot.LogRequired) sb.Append("No log duty tonight.\n");
            else if (ReportListHasNames()) sb.Append("REPORT ONE PERSON YOU LET IN (right: cancelled, wrong: -5 sanity)\n");
            else sb.Append("NOBODY LET IN YET. THE LOG FILLS ITSELF IN.\n");
            sb.Append("FINDINGS DISCOVERED: ").Append(session.Clues.FoundCount).Append(" / ").Append(session.Clues.TotalCount);
            _recordsText.text = sb.ToString();
            RefreshReportList();
        }

        private bool ReportListHasNames()
        {
            foreach (var r in _gameRoot.Session.NightRecords) if (r.Allowed) return true;
            return false;
        }

        // one button per person let in tonight, in the order they came; the reported one is marked
        private void RefreshReportList()
        {
            var session = _gameRoot.Session;
            int slot = 0;
            for (int i = 0; i < session.NightRecords.Count && slot < ReportSlots; i++)
            {
                var r = session.NightRecords[i];
                if (!r.Allowed || !_gameRoot.LogRequired || _reportBtns[slot] == null) continue;
                bool reported = session.ReportedRecord == i;
                _reportRecord[slot] = i;
                var label = _reportBtns[slot].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = reported ? r.Name + "  [REPORTED]" : r.Name + "  [REPORT]";
                    label.color = reported ? UIFactory.AccentBright : UIFactory.NsInk;
                }
                _reportBtns[slot].gameObject.SetActive(true);
                slot++;
            }
            for (; slot < ReportSlots; slot++)
                if (_reportBtns[slot] != null) _reportBtns[slot].gameObject.SetActive(false);
        }
    }
}
