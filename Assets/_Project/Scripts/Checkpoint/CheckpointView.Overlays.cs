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
    // Paper overlays: rules note, tonight's log, verdict banner sheet.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
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
            if (Queue != null && Queue.MissingIncidents > 0)
                sb.Append("\nROSTER DISCREPANCY: ").Append(Queue.MissingIncidents)
                  .Append(" expected student(s) never reached the window.\n");
            sb.Append("\nBREACHES (anomalies allowed in): ").Append(_gameRoot.Session.Breaches).Append(" / 3\n");
            sb.Append("FALSE REJECTIONS: ").Append(_gameRoot.Session.FalseRejections).Append(" / 3\n");
            sb.Append("FINDINGS DISCOVERED: ").Append(_gameRoot.Session.Clues.FoundCount).Append(" / 10\n");
            _recordsText.text = sb.ToString();
        }
    }
}
