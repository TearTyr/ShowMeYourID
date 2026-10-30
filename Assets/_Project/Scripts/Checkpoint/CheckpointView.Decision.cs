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
    // Right column: tools row, stamps; the verdict flow.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // Stacking order is build order: questions and the tools row sit BELOW
        // the papers layer; the stamp tray sits ABOVE it; overlays top everything.
        private void BuildQuestionsPanel()
        {
            var qPanel = UIFactory.BorderedPanel(_desk, "qpanel", UIFactory.Paper, UIFactory.Border, 2);
            Reg("questions", qPanel, L.Questions);
            qPanel.gameObject.AddComponent<RectMask2D>();
            var qHead = UIFactory.Label(qPanel, "head", "QUESTIONS", 24, UIFactory.NsChalk, TextAnchor.UpperLeft);
            qHead.font = UIFactory.UiFontBold;
            L.Place(qHead.rectTransform, L.Inner.QHead);
            qHead.rectTransform.localScale = new Vector3(0.72f, 1f, 1f);   // condensed, like the board
            _budgetLabel = UIFactory.Label(qPanel, "budget", "2/2", 22,
                UIFactory.NsMid, TextAnchor.MiddleRight);
            _budgetLabel.font = UIFactory.UiFontBold;
            // short HUD labels never word-wrap: the counter keeps one clean line and grows
            // leftwards from its right anchor instead of stacking fragments (owner #2B)
            _budgetLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            L.Place(_budgetLabel.rectTransform, L.Inner.QBudget);
            _budgetLabel.gameObject.SetActive(false);
            _qEmpty = UIFactory.Label(qPanel, "qempty", "NO VISITOR AT THE WINDOW", 12,
                UIFactory.NsMid, TextAnchor.MiddleCenter);
            _qEmpty.font = UIFactory.UiFont;
            L.Place(_qEmpty.rectTransform, new L.Region(L.Inner.QBtnX,
                (L.Inner.QContentY0 + L.Inner.QContentY1) * 0.5f - 10f, L.Inner.QBtnW, 20));
            _qEmpty.gameObject.SetActive(false);

            string[] qs =
            {
                "YOUR COURSE?", "YOUR YEAR?",
                "REGULAR OR TRANSFEREE?", "FIRST CLASSROOM?",
            };
            var qids = QuestionSystem.All;
            for (int i = 0; i < 4; i++)
            {
                int qi = i;
                var b = UIFactory.StandardButton(qPanel, "q" + i, qs[i], () => AskQuestion(qids[qi]),
                    (int)L.Inner.QBtnW, 28);
                // the row rect comes from the shared column layout below, never from a
                // per-button screen coordinate: heading, counter and choices are one panel
                LayoutQuestionRow(b.GetComponent<RectTransform>(), i, 4);
                // 14 u is the size at which the longest row PLUS its asked-state suffix
                // ("… ✓ ASKED") still reads on one line inside the pill; RefreshQuestionsPanel
                // keeps a best-fit window under it, and a wrapped long row stays inside the
                // pill instead of clipping (wrap on, best-fit shrinks before it overflows)
                SizeLabel(b, 14);
                var bt = b.GetComponentInChildren<UnityEngine.UI.Text>();
                if (bt != null)
                {
                    bt.font = UIFactory.UiFont; bt.color = UIFactory.NsChalk;
                    bt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    bt.verticalOverflow = VerticalWrapMode.Truncate;
                }
                _questionButtons[i] = b;
                _qLabels[i] = qs[i];
                b.gameObject.SetActive(false);
            }
            // answers live in the speech log now; no testimony box, no hint line

        }

        // The choice column is ONE parent-driven layout: the panel's content box (between
        // the header strip and the kit frame's painted bottom border) is divided evenly
        // between the visible choices, each row centred in its slot with a uniform gap.
        // Fewer visible choices get taller rows instead of leaving the panel half empty;
        // four rows land clear of the frame in every question/asked state.
        private void LayoutQuestionRow(RectTransform rt, int index, int visible)
        {
            int n = Mathf.Max(1, visible);
            float avail = L.Inner.QContentY1 - L.Inner.QContentY0;
            float step = avail / n;
            float h = Mathf.Clamp(step - L.Inner.QBtnGap, 22f, 40f);
            float y = L.Inner.QContentY0 + index * step + (step - h) * 0.5f;
            L.Place(rt, new L.Region(L.Inner.QBtnX, y, L.Inner.QBtnW, h));
        }

        /// <summary>Re-runs the column layout for the number of choices on screen (the
        /// panel shows all four while an inspection is live, none otherwise).</summary>
        private void LayoutQuestions(int visible)
        {
            for (int i = 0; i < _questionButtons.Length; i++)
                if (_questionButtons[i] != null)
                    LayoutQuestionRow(_questionButtons[i].GetComponent<RectTransform>(), i, visible);
        }

        private void BuildToolsRow()
        {
            // the tools row: tonight's log, map, rules note. (The SCAN button and the
            // camera monitor are retired — the ID scanner feature is gone; verification
            // is manual: documents, floor files and questions.)
            // the two desk papers are PROPS: the kit's printed sheets lying on the desk at
            // the concept art's angles, clickable to open their overlay sheet
            _recordsBtn = PaperProp("records", "sec_log_paper",
                () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, L.UtilityRecords, L.TiltLog);
            Reg("records", _recordsBtn, L.UtilityRecords);
            // the mockup's inline college map: a panel-button (click still opens the full
            // overlay) whose chalk list mirrors the ground-floor plan the overlay draws —
            // same rooms, same order, same wording, so the two can never disagree
            _mapBtn = GridButton(_desk, "map", "", () => _gameRoot.OpenMap(), L.UtilityMap, 16);
            Reg("map", _mapBtn, L.UtilityMap);
            // the reference paints a torn paper plan, not a chalk list: the overlay's paper and
            // room inks reused at desk scale, so the two can never disagree
            var mpaper = UIFactory.Img(_mapBtn.transform, "mpaper", ProceduralArt.Get("paper_flat"),
                Color.white);
            mpaper.raycastTarget = false;
            UIFactory.AnchorsFull(mpaper.rectTransform);
            var mt = UIFactory.Label(_mapBtn.transform, "mtitle", "HOLLOWBROOK COLLEGE MAP", 13,
                UIFactory.NsInk, TextAnchor.MiddleCenter);
            L.Place(mt.rectTransform, new L.Region(0, 8, 290, 16));
            mt.font = UIFactory.UiFontBold;
            var mf = UIFactory.Label(_mapBtn.transform, "mfloor", "FIRST FLOOR", 10,
                UIFactory.NsInk, TextAnchor.MiddleLeft);
            L.Place(mf.rectTransform, new L.Region(14, 42, 90, 14));
            mf.font = UIFactory.UiFont;
            // hand-drawn room boxes (map-local): the reference plan's composition in
            // miniature — both classrooms along the north wall, the hall between them and
            // the security room, the checkpoint south-west, the comfort rooms north and
            // south of the sealed server room at the east end. Same rooms, same order as
            // the full plan, so the two can never disagree.
            float[][] boxes =
            {
                new float[] { 66, 56, 58, 28 },   new float[] { 132, 56, 58, 28 },
                new float[] { 52, 92, 138, 20 },  new float[] { 18, 118, 50, 24 },
                new float[] { 214, 56, 40, 20 },  new float[] { 214, 118, 40, 20 },
                new float[] { 214, 84, 52, 26 },
            };
            string[] tags = { "ROOM 101", "ROOM 202", "MAIN HALL", "SECURITY",
                              "CR-1", "CR-2", "SERVER — SEALED" };
            for (int i = 0; i < boxes.Length; i++)
            {
                bool sealedRoom = i == boxes.Length - 1;
                var box = UIFactory.Img(_mapBtn.transform, "mbox" + i,
                    ProceduralArt.Get(sealedRoom ? "map_sealed" : "map_room"), Color.white);
                box.raycastTarget = false;
                L.Place(box.rectTransform, new L.Region(boxes[i][0], boxes[i][1], boxes[i][2], boxes[i][3]));
                var bl = UIFactory.Label(box.rectTransform, "t", tags[i], 8,
                    sealedRoom ? new Color(0.80f, 0.32f, 0.33f) : UIFactory.NsInk,
                    TextAnchor.MiddleCenter);
                UIFactory.AnchorsFull(bl.rectTransform);
                bl.font = UIFactory.UiFont;
            }
            var mh = UIFactory.Label(_mapBtn.transform, "mhint", "CLICK — OPEN THE FULL PLAN", 9,
                UIFactory.NsMid, TextAnchor.MiddleRight);
            L.Place(mh.rectTransform, new L.Region(90, 150, 190, 12));
            mh.font = UIFactory.UiFont;
            // two tape tabs pin the sheet down, like the reference's corners
            for (int tI = 0; tI < 2; tI++)
            {
                var tape = UIFactory.Panel(_mapBtn.transform, "mtape" + tI,
                    new Color(0.13f, 0.13f, 0.14f, 0.85f));
                tape.raycastTarget = false;
                L.Place(tape.rectTransform, new L.Region(tI == 0 ? -8f : 244f, -6f, 46f, 14f));
                tape.rectTransform.localEulerAngles = new Vector3(0, 0, tI == 0 ? 8f : -8f);
            }
            _notesBtn = PaperProp("notes", "sec_rules_paper",
                () => ToggleOverlay(_notesRoot), L.UtilityNotes, L.TiltRules);
            Reg("notes", _notesBtn, L.UtilityNotes);

            // verdicts are physical: drag a stamp onto the slip to commit.
            // No confirm buttons anywhere — the stamp act IS the confirmation.
        }

        // A paper prop: kit sheet art on the desk, tilted like the concept art paints it,
        // and a real Button (the sheet itself is the click target). Falls soft: without the
        // kit the prop is a plain paper panel, still clickable.
        private Button PaperProp(string name, string kitKey, System.Action onClick, L.Region r, float tilt)
        {
            var rt = UIFactory.NewUI(_desk, name);
            var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            var sp = ProceduralArt.Get(kitKey);
            if (sp != null) { img.sprite = sp; img.color = Color.white; }
            else img.color = UIFactory.Paper;
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            L.Place(rt, r);
            rt.localEulerAngles = new Vector3(0, 0, tilt);
            return b;
        }

        // The interview at a glance: budget pips, asked-tags on the four buttons, and the
        // hint line (hover copy, or the budget-spent reason when the policy caps you out).
        private void RefreshQuestionsPanel()
        {
            if (_budgetLabel != null)
            {
                _budgetLabel.text = _budget.Remaining + "/" + _budget.Max;
            }
            for (int i = 0; i < 4; i++)
            {
                if (_questionButtons[i] == null) continue;
                bool asked = _budget.HasAsked(QuestionSystem.All[i]);
                var t = _questionButtons[i].GetComponentInChildren<Text>();
                if (t != null) t.text = asked ? _qLabels[i] + "  ✓ ASKED" : _qLabels[i];
                if (t != null) t.color = _kitOn ? UIFactory.NsInk : UIFactory.NsChalk;
                // one shared fit rule for every row: the asked suffix makes a row longer, so
                // the label best-fits DOWN (never up) inside its fixed box — the four rows
                // keep identical rects in every state, and no word ever wraps or clips
                if (t != null)
                {
                    t.resizeTextForBestFit = true;
                    t.resizeTextMaxSize = t.fontSize;
                    t.resizeTextMinSize = Mathf.Max(9, t.fontSize - 3);
                }
                LayoutQuestionRow(_questionButtons[i].GetComponent<RectTransform>(), i, 4);
                RefreshQuestionArrow(i, asked);   // the kit's arrow is the asked-mark
            }
        }

        private void BuildStampTray()
        {
            BuildStamp("stamp_allow", "APPROVE", new Color(0.42f, 0.58f, 0.42f), L.StampAllow, Verdict.Allow, 0, 20);
            BuildStamp("stamp_deny", "DECLINE", new Color(0.58f, 0.28f, 0.28f), L.StampDeny, Verdict.Reject, 1, 20);
        }

        private void BuildStamp(string goName, string label, Color tint, L.Region region, Verdict verdict, int idx, int fontSize = 13)
        {
            var dim = new Color(tint.r, tint.g, tint.b, 0.35f);   // born dim; RefreshStamps wakes them
            var panel = UIFactory.Panel(_desk, goName, dim);
            Reg(goName, panel.rectTransform, region);
            // NO word over the artwork: the stamp illustration stays clean ink. The
            // verdict is told by the stamp's own colour and the check/cross badge on its head.
            var drag = panel.gameObject.AddComponent<DragPaper>();
            drag.Init(_desk, 0f);
            drag.ReturnsHome = true;                 // stamps live in the tray
            drag.OnDropped = center => TryStampOnDrop(center, verdict);
            _stampTints[idx] = panel;
            _stampBase[idx] = tint;
        }

        // Papers, Please stamping: the mark lands WHERE the player released the stamp on
        // the slip. The drop centre (desk space) is converted into the slip's own space —
        // position, zoom scale and resting tilt included — then clamped so the whole
        // tilted mark stays inside the printable area: a drop near a corner stamps near
        // that corner, a drop in the middle stamps the middle, nothing ever hangs off the
        // paper. It records a PENDING verdict: nothing is scored until hand-back.
        private void TryStampOnDrop(Vector2 center, Verdict v)
        {
            if (_slipGo == null || !_slipGo.activeSelf || !_slipTaken || _current == null || _verdictLocked) return;
            var srt = _slipGo.GetComponent<RectTransform>();
            // the written slip lives on its scaled art layer: placement maths runs in the
            // art's 200x120 design space, converted through the container's zoom+tilt
            var artT = _slipGo.transform.Find("slipart");
            var art = artT != null ? artT.GetComponent<RectTransform>() : srt;
            var ss = art.rect.size;
            var sc = new Vector3(srt.localScale.x * art.localScale.x, srt.localScale.y * art.localScale.y, 1f);
            float w = ss.x * sc.x, h = ss.y * sc.y;
            var top = new Vector2(srt.anchoredPosition.x, -srt.anchoredPosition.y);
            bool onSlip = center.x >= top.x - 8f && center.x <= top.x + w + 8f &&
                          center.y >= top.y - 8f && center.y <= top.y + h + 8f;
            if (!onSlip) return;                                    // off the slip: back to the rack
            if (_slipStamped) { _gameRoot.Audio.PlaySfx("sfx_paper", 0.25f); return; }   // one stamp per slip
            _pendingVerdict = v;
            _slipStamped = true;
            var local = StampPlacement.DeskToLocal(center.x, center.y, top.x, top.y,
                sc.x, srt.localEulerAngles.z);
            local = StampPlacement.ClampToPrintable(local, ss.x, ss.y,
                StampMarkSize, StampMaxTilt, StampEdgePad);
            PlaceStampMark(v, new Vector2(local.X, local.Y));
            RefreshHandoverPrompt();
            _gameRoot.Audio.PlaySfx(v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.8f);
        }

        internal void HandBackByKey()
        {
            if (!_slipStamped || !_documentsTaken) return;
            HandBackId();
            HandBackSlip();
        }

        // The mark becomes a layer of the slip AT THE DROPPED SPOT (localCentre is in the
        // slip's own space, already clamped inside the printable area), so it moves, flips
        // and zooms with the paper and can never overhang the edge. Rotation is seeded per
        // inspection; the clamp budget (StampMaxTilt) covers the tilt's corners.
        //
        // 60px on the 200x120 slip: +33.3% over the 45px mark (this pass's readability request)
        // and 30% of the slip's width — the same footprint class as the original 56px art, with
        // the temp ink asset's tighter margins keeping the NAME / ID lines readable around it.
        // Square sizeDelta + preserveAspect keep the ink undistorted; the printable clamp
        // (margin 39.8px) still holds the whole tilted mark on the paper. internal so the
        // headless proofs clamp and measure with the SAME numbers the view renders with.
        internal const float StampMarkSize = 60f;
        internal const float StampMaxTilt = 18f;   // |−12 + [−6..+6]| — the seeded tilt span
        internal const float StampEdgePad = 2f;    // breathing room between mark and edge

        private void PlaceStampMark(Verdict v, Vector2 localCentre)
        {
            if (_stampOverlay == null || _slipGo == null) return;
            var artT = _slipGo.transform.Find("slipart");
            var srt = artT != null ? artT : _slipGo.transform;
            _stampOverlay.transform.SetParent(srt, false);
            if (_punch != null) StopCoroutine(_punch);      // one punch at a time
            var rt = _stampOverlay.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(StampMarkSize, StampMarkSize);
            _stampOverlay.preserveAspect = true;
            _stampOverlay.raycastTarget = false;             // the mark never eats clicks
            rt.anchoredPosition = new Vector2(localCentre.x, -localCentre.y);
            rt.localEulerAngles = new Vector3(0, 0, -12 + _stampRng.NextInt(-6, 7));
            _stampOverlay.sprite = ProceduralArt.Get(v == Verdict.Allow ? "stamp_allow" : "stamp_reject");
            rt.localScale = Vector3.one;
            var c = _stampOverlay.color; c.a = 0f; _stampOverlay.color = c;
            _punch = StartCoroutine(StampPunch(v));
        }

        private void RefreshStamps(bool live)
        {
            for (int i = 0; i < 3; i++)
            {
                if (_stampTints[i] == null) continue;
                var c = _stampBase[i];
                c.a = live ? 1f : 0.35f;             // dim tray until an inspection is live
                _stampTints[i].color = c;
            }
        }

        // Decisions are clickable only while an inspection is live AND the ID is in hand;
        // the interview column swaps between the QUESTION gate and the open questions.
        private void RefreshDecisionButtons()
        {
            bool collapsed = Queue != null && Queue.Collapsed;
            bool atWindow = _current != null && !_verdictLocked && !collapsed;
            bool live = atWindow && _documentsTaken;
            for (int i = 0; i < 4; i++)
            {
                _questionButtons[i].gameObject.SetActive(atWindow);   // compact row, no gate
                if (i == 0 && _qEmpty != null) _qEmpty.gameObject.SetActive(!atWindow);
                if (i == 0) LayoutQuestions(atWindow ? _questionButtons.Length : 0);
                UIFactory.SetButtonEnabled(_questionButtons[i],
                    atWindow && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            }
            _budgetLabel.gameObject.SetActive(atWindow);
            RefreshQuestionsPanel();
            RefreshStamps(live);
            bool patrolOn = !atWindow && Queue != null && Queue.Started && !collapsed;
            bool endOn = Queue != null && Queue.ShiftComplete &&
                         _gameRoot.Session.Mode == GameMode.Normal;
            if (_kitOn) { ApplyKitPill(_patrolBtn, patrolOn); ApplyKitPill(_endShiftBtn, endOn); }
            else
            {
                UIFactory.SetButtonEnabled(_patrolBtn, patrolOn);
                UIFactory.SetButtonEnabled(_endShiftBtn, endOn);
            }
        }

        public void Stamp(Verdict verdict)
        {
            if (_current == null || _verdictLocked || !_documentsTaken) return;
            if (Queue != null && Queue.Collapsed) return;   // the line overran the checkpoint — verdicts are dead
            _verdictLocked = true; // §2.4 — decisions are permanent
            var mutation = _gameRoot.Session.Mode == GameMode.Challenge ? _gameRoot.Session.ActiveMutation : null;
            var outcome = InspectionRules.Apply(_current, verdict, mutation);

            // the thud already played when the stamp landed; hand-back owns its own sound
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, outcome));

            // silent verdict: the log records the call, never whether it was right
            _tonightsLog.Append("  ").Append(_current.IsAdministrator ? "ADMINISTRATOR" : _current.ClaimedName)
                .Append(" — ").Append(verdict == Verdict.Allow ? "ALLOWED" : "REJECTED").Append("\n");

            var resolved = _current;
            _gameRoot.OnVerdictResolved(resolved, outcome);           // counters update BEFORE the banner reads them
            if (_gameRoot.State == AppViewState.Ending) { Hide(); return; } // ending took over (C3/C4)
            if (Queue != null) Queue.CommitActiveResolution();                // the queue books it NOW — a
                                                                      // quit mid-banner can never make
                                                                      // this visitor stampable again
            RefreshHud();

            StartCoroutine(AfterStamp(resolved));
        }

        // Silent verdict: the banner states the decision and nothing else — never whether
        // the call was correct. The thrill lives in not knowing until the night judges you.

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
            // the mark STAYS on the slip: the student gets their stamped paper back
        }

        // The post-stamp beat: the mark lands, the subject leaves, the next steps up.
        // Deliberately wordless — feedback is the stamp, the sound, the walking figure.
        private IEnumerator AfterStamp(VisitorData resolved)
        {
            yield return WaitUnpaused(BannerSeconds);
            if (_gameRoot.State != AppViewState.Checkpoint && _gameRoot.State != AppViewState.AdminEncounter) yield break;

            // the resolved visitor leaves the window; the oldest waiting visitor steps up
            // immediately (no second waiting period when someone is already queued)
            var promoted = Queue != null ? Queue.CompleteActive() : null;

            if (resolved.IsAdministrator && _gameRoot.Session.AdminRejected)
            {
                // the night ends without words: the line is sent home, the window closes
                if (Queue != null) Queue.AbandonLine();
                _current = null;
                SetVisitorVisible(false);
                    RefreshHud();
                _gameRoot.Audio.PlayMusic("checkpoint");
                yield break;
            }

            if (promoted != null)
            {
                PresentFromQueue(promoted, false);
                yield break;
            }

            _current = null;
            _verdictLocked = true;
            SetVisitorVisible(false);
            RefreshHud();
        }


    }
}
