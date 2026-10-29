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
            // the verdict banner retired: post-decision feedback is the stamp mark, the
            // sound and the subject walking off — never text

            _notesRoot = BuildPaperOverlay("OverlayRules", "NIGHT SECURITY RULES",
                RulesHandLines, false, L.TiltRules, out _notesText);
            _recordsRoot = BuildPaperOverlay("OverlayLog", "NIGHT SECURITY LOG",
                LogHandLines, true, L.TiltLog, out _recordsText);

            // the yes/no confirm popup retired with the button verdicts: stamping IS confirming
        }

        // The kit's prop sheets are PHOTOGRAPHED documents: crest, hand title and hand
        // rules baked into the paper at prop size, tilt included. Stretching them across
        // the reading sheet printed the live text on top of that baked ink (dark type on
        // dark handwriting, the crest swallowing whole words) and slid the baked tilt
        // under straight type — which is exactly why both documents read as broken.
        // The reading sheet is therefore rebuilt from plain distressed paper in the same
        // language: the kit crest cropped out of the prop, the hand face for the title
        // and the static block (rules lines / the log checklist with its boxes), and the
        // printed live body in its own masked column. The desk props keep the photographed
        // art; nothing of their content is lost, and every line here is readable.
        private static readonly string[] RulesHandLines =
        {
            "MONITOR STUDENTS AND SCHOOL PREMISES.",
            "REPORT SUSPICIOUS ACTIVITIES.",
            "PREVENT UNAUTHORIZED ENTRY.",
            "RECORD ALL ANOMALIES IN THE LOGBOOK.",
            "REPORT STUDENT VIOLATIONS TO THE ADMINISTRATION.",
            "NOTE: STAY ALERT AND REPORT ALL INCIDENTS IMMEDIATELY.",
        };
        private static readonly string[] LogHandLines =
        {
            "STUDENT ACTIVITIES",
            "RULE VIOLATIONS",
            "INCIDENTS REPORTED",
        };

        private GameObject BuildPaperOverlay(string goName, string title, string[] handLines,
            bool checklist, float tilt, out Text body)
        {
            var goRt = UIFactory.NewUI(_desk, goName);
            var go = goRt.gameObject;
            L.Place(goRt, L.FullDesk);
            UIFactory.Dimmer(goRt, "dim", 0.6f);
            var panel = UIFactory.Img(goRt, "paper", ProceduralArt.Get("paper_flat"));
            L.Place(panel.rectTransform, L.PaperOverlay);
            panel.raycastTarget = false;
            // the documents lie on the desk, they are not pinned to the grid: the whole
            // sheet (type included) carries the concept's tilt as one transform
            panel.rectTransform.localEulerAngles = new Vector3(0f, 0f, tilt);
            var crest = UIFactory.Img(panel.rectTransform, "crest",
                CropCrest(goName == "OverlayRules" ? "sec_rules_paper" : "sec_log_paper"));
            crest.raycastTarget = false;
            L.Place(crest.rectTransform, L.Inner.PaperCrest);
            var head = UIFactory.Label(panel.rectTransform, "title", title, 34,
                UIFactory.NsInk, TextAnchor.MiddleCenter);
            head.font = UIFactory.HandFont;
            head.raycastTarget = false;
            L.Place(head.rectTransform, L.Inner.PaperHead);
            if (checklist)
            {
                // the prop's checklist, rebuilt as live type: three hand lines, each with
                // its printed box — same words, same squares, readable at sheet size
                for (int i = 0; i < LogHandLines.Length; i++)
                {
                    float y = L.Inner.PaperCheckY0 + i * L.Inner.PaperCheckStep;
                    var row = UIFactory.Label(panel.rectTransform, "chk" + i, LogHandLines[i], 22,
                        UIFactory.NsInk, TextAnchor.MiddleLeft);
                    row.font = UIFactory.HandFont;
                    row.raycastTarget = false;
                    UIFactory.PlaceTL(row.rectTransform, L.Inner.PaperCheckX, y,
                        L.Inner.PaperCheckW, L.Inner.PaperCheckH);
                    var box = UIFactory.BorderedPanel(panel.rectTransform, "chkbox" + i,
                        new Color(0.87f, 0.86f, 0.83f), UIFactory.NsInk, 2);
                    UIFactory.PlaceTL(box, L.Inner.PaperCheckBoxX, y - 1f,
                        L.Inner.PaperCheckBoxS, L.Inner.PaperCheckBoxS);
                }
            }
            else if (handLines != null && handLines.Length > 0)
            {
                var hand = UIFactory.Label(panel.rectTransform, "hand",
                    string.Join("\n", handLines), 21, UIFactory.NsInk, TextAnchor.UpperLeft);
                hand.font = UIFactory.HandFont;
                hand.lineSpacing = 0.95f;
                hand.raycastTarget = false;
                L.Place(hand.rectTransform, L.Inner.PaperHand);
            }
            body = Body(panel.rectTransform, "body", L.Inner.PaperBody, 15, true);
            var close = GridButton(panel.rectTransform, "close", "CLOSE [X]",
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

        // The crest sits top-centre of both prop sheets (measured off the 789x879 kit
        // exports). Cropping it out of the prop keeps the owner's ink on the reading
        // sheet without dragging the baked handwriting along.
        private static Sprite _crestSprite;
        private static Sprite CropCrest(string kitKey)
        {
            if (_crestSprite != null) return _crestSprite;
            var src = ProceduralArt.Get(kitKey);
            var tex = src != null ? src.texture : null;
            if (tex == null || tex.width < 64 || tex.height < 64) return null;
            float x = Mathf.Floor(tex.width * (295f / 789f));
            float y = Mathf.Floor(tex.height * (70f / 879f));
            float w = Mathf.Ceil(tex.width * (125f / 789f));
            float h = Mathf.Ceil(tex.height * (132f / 879f));
            _crestSprite = Sprite.Create(tex, new Rect(x, tex.height - y - h, w, h),
                new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            _crestSprite.name = "sec_crest_cropped";
            return _crestSprite;
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
