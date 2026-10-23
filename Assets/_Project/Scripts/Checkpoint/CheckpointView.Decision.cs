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
    // Right column: scanner, stamps, utility buttons, monitor; the verdict flow.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // Stacking order is build order: questions and the tools/camera row sit BELOW
        // the papers layer; the stamp tray sits ABOVE it; overlays top everything.
        private void BuildQuestionsPanel()
        {
            var qPanel = UIFactory.BorderedPanel(_desk, "qpanel", UIFactory.Paper, UIFactory.Border, 2);
            Reg("questions", qPanel, L.Questions);
            qPanel.gameObject.AddComponent<RectMask2D>();
            var qHead = UIFactory.Label(qPanel, "head", "QUESTIONS", 13, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(qHead.rectTransform, L.Inner.QHead);
            _budgetLabel = UIFactory.Label(qPanel, "budget", "QUESTIONS: 3 LEFT", 11,
                new Color(0.35f, 0.35f, 0.34f), TextAnchor.MiddleRight);
            L.Place(_budgetLabel.rectTransform, L.Inner.QBudget);
            _budgetLabel.gameObject.SetActive(false);

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
                _qLabels[i] = qs[i];
                b.gameObject.SetActive(false);
            }
            // answers live in the speech log now; no testimony box, no hint line

        }

        private void BuildToolsAndCamera()
        {            _scanBtn = GridButton(_desk, "scan", "SCAN", DoScan, L.UtilityScan, 11);
            Reg("scan", _scanBtn, L.UtilityScan);
            _recordsBtn = GridButton(_desk, "records", "TL",
                () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, L.UtilityRecords, 11);
            Reg("records", _recordsBtn, L.UtilityRecords);
            _mapBtn = GridButton(_desk, "map", "MAP", () => _gameRoot.OpenMap(), L.UtilityMap, 16);
            Reg("map", _mapBtn, L.UtilityMap);
            _notesBtn = GridButton(_desk, "notes", "RN",
                () => ToggleOverlay(_notesRoot), L.UtilityNotes, 11);
            Reg("notes", _notesBtn, L.UtilityNotes);

            // verdicts are physical: drag a stamp onto the subject's ID to commit.
            // No confirm buttons anywhere — the stamp act IS the confirmation.

            // the CAMERA is the scan device: drop the ID on it to scan; readout inside
            var mon = UIFactory.BorderedPanel(_desk, "monitor", new Color(0.055f, 0.062f, 0.055f),
                UIFactory.Border, 2);
            Reg("monitor", mon, L.Monitor);
            var camCap = UIFactory.Label(mon, "camcap", "CAMERA — DROP THE ID TO SCAN", 11,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(camCap.rectTransform, L.Inner.MonitorCaption);
            _monitorOutput = UIFactory.MaskedLabel(mon, "out",
                L.Inner.MonitorOut.X, L.Inner.MonitorOut.Y, L.Inner.MonitorOut.W, L.Inner.MonitorOut.H,
                13, new Color(0.55f, 0.75f, 0.55f));
        }

        // The interview at a glance: budget pips, asked-tags on the four buttons, and the
        // hint line (hover copy, or the budget-spent reason when the policy caps you out).
        private void RefreshQuestionsPanel()
        {
            if (_budgetLabel != null)
            {
                string pips = "";
                for (int i = 0; i < _budget.Max; i++) pips += (i < _budget.Remaining ? "● " : "○ ");
                _budgetLabel.text = "QUESTIONS: " + _budget.Remaining + " LEFT  " + pips.Trim();
            }
            for (int i = 0; i < 4; i++)
            {
                if (_questionButtons[i] == null) continue;
                bool asked = _budget.HasAsked(QuestionSystem.All[i]);
                var t = _questionButtons[i].GetComponentInChildren<Text>();
                if (t != null) t.text = asked ? _qLabels[i] + "  ✓ ASKED" : _qLabels[i];
            }
        }

        private void BuildStampTray()
        {
            BuildStamp("stamp_allow", "A — ACCEPT", new Color(0.42f, 0.58f, 0.42f), L.StampAllow, Verdict.Allow, 0, 18);
            BuildStamp("stamp_deny", "D — DENY", new Color(0.58f, 0.28f, 0.28f), L.StampDeny, Verdict.Reject, 1, 18);
        }

        private void BuildStamp(string goName, string label, Color tint, L.Region region, Verdict verdict, int idx, int fontSize = 13)
        {
            var dim = new Color(tint.r, tint.g, tint.b, 0.35f);   // born dim; RefreshStamps wakes them
            var panel = UIFactory.Panel(_desk, goName, dim);
            Reg(goName, panel.rectTransform, region);
            var t = UIFactory.Label(panel.rectTransform, "label", label, fontSize,
                UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(t.rectTransform);
            var drag = panel.gameObject.AddComponent<DragPaper>();
            drag.Init(_desk, 0f);
            drag.ReturnsHome = true;                 // stamps live in the tray
            drag.OnDropped = center => TryStampOnDrop(center, verdict);
            _stampTints[idx] = panel;
            _stampBase[idx] = tint;
        }

        // The stamp lands ON THE SLIP, exactly where it was dropped, with a small seeded
        // rotation. It records a PENDING verdict: nothing is scored until hand-back.
        private void TryStampOnDrop(Vector2 center, Verdict v)
        {
            if (_slipGo == null || !_slipGo.activeSelf || !_slipTaken || _current == null || _verdictLocked) return;
            var srt = _slipGo.GetComponent<RectTransform>();
            var ss = srt.rect.size;
            var sc = srt.localScale;                 // zoomed slip = bigger target, true placement
            float w = ss.x * sc.x, h = ss.y * sc.y;
            var top = new Vector2(srt.anchoredPosition.x, -srt.anchoredPosition.y);
            bool onSlip = center.x >= top.x - 8f && center.x <= top.x + w + 8f &&
                          center.y >= top.y - 8f && center.y <= top.y + h + 8f;
            if (!onSlip) return;                                    // off the slip: back to the rack
            if (_slipStamped) { _gameRoot.Audio.PlaySfx("sfx_paper", 0.25f); return; }   // one stamp per slip
            _pendingVerdict = v;
            _slipStamped = true;
            PlaceStampMark(center, v);
            RefreshHandoverPrompt();
            _gameRoot.Audio.PlaySfx(v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.8f);
        }

        // Keyboard path: same placement rule as a drop on the slip's centre (rotation seeded).
        internal void StampSlipByKey(Verdict v)
        {
            if (!_slipTaken || _slipStamped || _current == null || _verdictLocked) return;
            var srt = _slipGo.GetComponent<RectTransform>();
            var ss = srt.rect.size;
            var sc = srt.localScale;
            var top = new Vector2(srt.anchoredPosition.x, -srt.anchoredPosition.y);
            var center = top + new Vector2(ss.x * sc.x / 2f, ss.y * sc.y / 2f);
            _pendingVerdict = v;
            _slipStamped = true;
            PlaceStampMark(center, v);
            _gameRoot.Audio.PlaySfx(v == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 0.8f);
        }

        internal void HandBackByKey()
        {
            if (!_slipStamped || !_documentsTaken) return;
            HandBackId();
            HandBackSlip();
        }

        // Free placement: the mark becomes a layer of the slip at the drop point, so it
        // moves, flips and zooms with the paper. Rotation is seeded per inspection.
        private void PlaceStampMark(Vector2 deskCenter, Verdict v)
        {
            if (_stampOverlay == null || _slipGo == null) return;
            var srt = _slipGo.GetComponent<RectTransform>();
            _stampOverlay.transform.SetParent(srt, false);
            if (_punch != null) StopCoroutine(_punch);      // one punch at a time
            var rt = _stampOverlay.rectTransform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 90f);            // point anchor: give it a real size
            _stampOverlay.preserveAspect = true;
            _stampOverlay.raycastTarget = false;             // the mark never eats clicks
            var top = new Vector2(srt.anchoredPosition.x, -srt.anchoredPosition.y);
            var local = deskCenter - top;
            var sc2 = srt.localScale;
            rt.anchoredPosition = new Vector2(local.x / sc2.x, -local.y / sc2.y);
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
                UIFactory.SetButtonEnabled(_questionButtons[i],
                    atWindow && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            }
            _budgetLabel.gameObject.SetActive(atWindow);
            RefreshQuestionsPanel();
            RefreshStamps(live);
            UIFactory.SetButtonEnabled(_patrolBtn, !atWindow &&
                Queue != null && Queue.Started && !collapsed);
            UIFactory.SetButtonEnabled(_endShiftBtn, Queue != null && Queue.ShiftComplete &&
                _gameRoot.Session.Mode == GameMode.Normal);
        }

        public void DoScan()
        {
            if (_current == null || _verdictLocked) return;
            if (!_documentsTaken) { _monitorOutput.text = "TAKE THE ID FIRST."; return; }
            if (_gameRoot.Roster == null) return;
            var res = _gameRoot.Roster.Scan(_current, _gameRoot.Session.TonightLog);
            _monitorOutput.text = res.Display;
            _monitorOutput.color = res.FoundInDatabase && !res.DuplicateTonight
                ? new Color(0.55f, 0.75f, 0.55f)
                : new Color(0.85f, 0.35f, 0.35f);
            _gameRoot.Audio.PlaySfx(res.FormatValid && res.FoundInDatabase && !res.DuplicateTonight ? "sfx_scan_ok" : "sfx_scan_error", 0.8f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScanPerformed, res));
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
