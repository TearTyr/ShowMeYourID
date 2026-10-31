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
        // Stacking order is build order: the questions panel and the tools row sit BELOW
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
            _qKeyHint = UIFactory.Label(qPanel, "keyhint", "KEY [1]", 14, UIFactory.NsMid, TextAnchor.MiddleRight);
            _qKeyHint.font = UIFactory.UiFontBold;
            _qKeyHint.horizontalOverflow = HorizontalWrapMode.Overflow;
            L.Place(_qKeyHint.rectTransform, L.Inner.QKeyHint);
            _qKeyHint.gameObject.SetActive(false);
            _qEmpty = UIFactory.Label(qPanel, "qempty", "NO VISITOR AT THE WINDOW", 12,
                UIFactory.NsMid, TextAnchor.MiddleCenter);
            _qEmpty.font = UIFactory.UiFont;
            L.Place(_qEmpty.rectTransform, new L.Region(L.Inner.QBtnX, 100f, L.Inner.QBtnW, 20));
            _qEmpty.gameObject.SetActive(false);

            _questionBtn = UIFactory.StandardButton(qPanel, "q0", ClassroomQuestion.ButtonLabel, AskClassroom,
                (int)L.Inner.QBtnW, (int)L.Inner.QBtnH);
            L.Place(_questionBtn.GetComponent<RectTransform>(),
                new L.Region(L.Inner.QBtnX, L.Inner.QContentY0 + 2f, L.Inner.QBtnW, L.Inner.QBtnH));
            SizeLabel(_questionBtn, 14);
            var bt = _questionBtn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (bt != null)
            {
                bt.font = UIFactory.UiFont;
                bt.color = UIFactory.NsChalk;
                bt.horizontalOverflow = HorizontalWrapMode.Wrap;   // the question needs two lines
                bt.verticalOverflow = VerticalWrapMode.Truncate;
            }
            _questionBtn.gameObject.SetActive(false);

            // what the student answered stays here, so it can be checked against the file
            _qAnswerHead = UIFactory.Label(qPanel, "answerhead", "THEY ANSWERED:", 11,
                UIFactory.NsMid, TextAnchor.MiddleLeft);
            _qAnswerHead.font = UIFactory.UiFontBold;
            L.Place(_qAnswerHead.rectTransform, L.Inner.QAnswerHead);
            _qAnswer = UIFactory.Label(qPanel, "answer", "", 16, UIFactory.NsChalk, TextAnchor.UpperLeft);
            _qAnswer.font = UIFactory.UiFontBold;
            _qAnswer.horizontalOverflow = HorizontalWrapMode.Wrap;
            L.Place(_qAnswer.rectTransform, L.Inner.QAnswer);
            _qAnswerHead.gameObject.SetActive(false);
            _qAnswer.gameObject.SetActive(false);
        }

        private void BuildToolsRow()
        {
            // the tools row: tonight's log, map, rules note. (The SCAN button and the
            // camera monitor do not exist; verification
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

        // The questions panel at a glance: the button label colour and the answer note.
        private void RefreshQuestionsPanel(bool atWindow)
        {
            Color ink = _kitOn ? UIFactory.NsInk : UIFactory.NsChalk;
            var t = _questionBtn.GetComponentInChildren<Text>();
            if (t != null) t.color = ink;
            bool answered = atWindow && _classroomAsked && _current != null;
            _qAnswerHead.gameObject.SetActive(answered);
            _qAnswer.gameObject.SetActive(answered);
            if (!answered) return;
            _qAnswerHead.color = _kitOn ? new Color(0.30f, 0.30f, 0.30f) : UIFactory.NsMid;
            _qAnswer.color = ink;
            _qAnswer.text = _current.ClassroomAnswer;
        }

        private void BuildStampTray()
        {
            // both stamps are plain black-and-white ink like the concept art; the caption
            // pill under each one (added by the kit skin) says which is which
            BuildStamp("stamp_allow", "APPROVE", Color.white, L.StampAllow, Verdict.Allow, 0, 20);
            BuildStamp("stamp_deny", "DECLINE", Color.white, L.StampDeny, Verdict.Reject, 1, 20);
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
            drag.OnPickedUp = () => SetSlipGlow(true);    // the slip lights up: that is where it goes
            drag.OnDropped = center => { SetSlipGlow(false); TryStampOnDrop(center, verdict); };
            drag.OnTapped = center => { SetSlipGlow(false); ToastDesk("DRAG THE STAMP ONTO THE SLIP."); };
            _stampTints[idx] = panel;
            _stampBase[idx] = tint;
        }

        // Papers, Please stamping: the mark lands WHERE the player released the stamp on
        // the slip. The drop centre (desk space) is converted into the slip's own space —
        // position, scale and resting tilt included — then clamped so the whole
        // tilted mark stays inside the printable area: a drop near a corner stamps near
        // that corner, a drop in the middle stamps the middle, nothing ever hangs off the
        // paper. It records a PENDING verdict: nothing is scored until hand-back.
        private void TryStampOnDrop(Vector2 center, Verdict v)
        {
            if (_slipGo == null || !_slipGo.activeSelf || !_slipTaken || _current == null || _verdictLocked) return;
            var srt = _slipGo.GetComponent<RectTransform>();
            // the written slip lives on its scaled art layer: placement maths runs in the
            // art's 200x120 design space, converted through the container's scale+tilt
            var artT = _slipGo.transform.Find("slipart");
            var art = artT != null ? artT.GetComponent<RectTransform>() : srt;
            var ss = art.rect.size;
            var sc = new Vector3(srt.localScale.x * art.localScale.x, srt.localScale.y * art.localScale.y, 1f);
            float w = ss.x * sc.x, h = ss.y * sc.y;
            var top = new Vector2(srt.anchoredPosition.x, -srt.anchoredPosition.y);
            bool onSlip = center.x >= top.x - 8f && center.x <= top.x + w + 8f &&
                          center.y >= top.y - 8f && center.y <= top.y + h + 8f;
            if (!onSlip)
            {
                ToastDesk("MISSED - DROP THE STAMP ON THE SLIP.");   // back to the rack, but say why
                return;
            }
            // a second stamp before the hand-back REPLACES the first: the verdict only counts
            // once the ID goes back to the student, so a slip of the hand can be fixed
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

        // The mark becomes a layer of the slip AT THE DROPPED SPOT (localCentre is in the
        // slip's own space, already clamped inside the printable area), so it moves, flips
        // and scales with the paper and can never overhang the edge. Rotation is seeded per
        // inspection; the clamp budget (StampMaxTilt) covers the tilt's corners.
        //
        // 60px on the 200x120 slip, big enough to read at a glance
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

        // A short note on the HUD toast line (the same channel as every desk refusal).
        private void ToastDesk(string text)
        {
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification, text));
        }

        // While a stamp is in hand the slip glows, so the target is never a guess.
        private void SetSlipGlow(bool on)
        {
            if (_slipBg == null) return;
            _slipBg.color = on ? new Color(1f, 0.97f, 0.72f) : SlipPaperColor;
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
            _questionBtn.gameObject.SetActive(atWindow);
            _qEmpty.gameObject.SetActive(!atWindow);
            _qKeyHint.gameObject.SetActive(atWindow);
            UIFactory.SetButtonEnabled(_questionBtn, atWindow);
            RefreshQuestionsPanel(atWindow);
            RefreshStamps(live);
            bool patrolOn = !atWindow && Queue != null && Queue.Started && !collapsed;
            bool endOn = Queue != null && Queue.ShiftComplete &&
                         _gameRoot.Session.Mode == GameMode.Normal && !_gameRoot.HuntActive;
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
                .Append(" (").Append(_current.IdNumber).Append(")")
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
            if (_gameRoot.State != AppViewState.Checkpoint) yield break;

            // the resolved visitor leaves the window; the oldest waiting visitor steps up
            // immediately (no second waiting period when someone is already queued)
            var promoted = Queue != null ? Queue.CompleteActive() : null;

            if (resolved.IsAdministrator && _gameRoot.Session.AdminRejected)
            {
                // he does not leave: the line is sent home and he forces the gate (the hunt)
                if (Queue != null) Queue.AbandonLine();
                _current = null;
                SetVisitorVisible(false);
                RefreshHud();
                _gameRoot.BeginAdminHunt();
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
