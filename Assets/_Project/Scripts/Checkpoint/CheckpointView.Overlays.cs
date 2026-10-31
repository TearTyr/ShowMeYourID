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
            // there is no verdict banner: post-decision feedback is the stamp mark, the
            // sound and the subject walking off — never text

            _notesRoot = BuildPaperOverlay("OverlayRules", "sec_rules_paper", out _notesText);
            _recordsRoot = BuildPaperOverlay("OverlayLog", "sec_log_paper", out _recordsText);

            // there is no yes/no confirm popup: stamping IS confirming
        }

        // The reading sheet IS the owner's photographed document: sec_rules_paper /
        // sec_log_paper carry the crest, the hand title, the hand-written rules block (or
        // the log checklist with its boxes) AND the paper's tilt, printed at prop size.
        // Re-typesetting any of that over the photo double-printed the ink (the earlier
        // plain-sheet pass) and stretching the photo over a wide sheet dragged the baked
        // tilt under straight type. So: the paper is shown at its own aspect, untouched,
        // and the LIVE body — the per-night briefing / tonight's entries, which no photo
        // can carry — rides on a typed addendum slip pinned over the sheet's blank bottom
        // band, exactly where a clipped typed page would sit on a photographed document.
        // Nothing of either document's content is lost; nothing is stretched or re-inked.
        private GameObject BuildPaperOverlay(string goName, string kitKey, out Text body)
        {
            var goRt = UIFactory.NewUI(_desk, goName);
            var go = goRt.gameObject;
            L.Place(goRt, L.FullDesk);
            var dim = UIFactory.Dimmer(goRt, "dim", 0.66f);
            var dimClose = dim.gameObject.AddComponent<Button>();   // a click outside the paper closes it
            dimClose.targetGraphic = dim;
            dimClose.transition = Selectable.Transition.None;
            dimClose.onClick.AddListener(() => { go.SetActive(false); _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.5f); });
            // the photographed paper, centred at its own proportions (preserveAspect keeps
            // the 789x879 export undistorted whatever the sheet rect rounds to); no extra
            // rotation — the print already lies at its angle
            var panel = UIFactory.Img(goRt, "paper", ProceduralArt.Get(kitKey));
            L.Place(panel.rectTransform, L.PaperOverlay);
            panel.preserveAspect = true;
            panel.raycastTarget = false;
            // the typed addendum slip: distressed stock, a breath of tilt, over the sheet's
            // blank bottom band — the live body scrolls inside its own masked column
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

        /// <summary>Headless dumps open a reading sheet exactly as its desk prop click
        /// would — same refresh, same toggle, no shortcut path.</summary>
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
            sb.Append("BASE RULES — Every person presents a Student ID and record form.\n");
            sb.Append("Verify: ID format (HB-YYYY-NNNN) · photo vs. the person · expiration · year level · course/program · Regular/Transferee status · record name.\n");
            sb.Append("Ask which classroom they are going to, then check the answer against their file in the drawer. Decisions are permanent.\n");
            sb.Append("Compare the face at the window with the ID photo yourself — the desk keeps no appearance form.\n\n");
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
