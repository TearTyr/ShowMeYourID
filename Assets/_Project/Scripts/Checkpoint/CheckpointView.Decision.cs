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
        private void BuildRightColumn()
        {
            var qPanel = UIFactory.BorderedPanel(_desk, "qpanel", UIFactory.Paper, UIFactory.Border, 2);
            Reg("questions", qPanel, L.Questions);
            qPanel.gameObject.AddComponent<RectMask2D>();
            var qHead = UIFactory.Label(qPanel, "head", "QUESTIONS", 13, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(qHead.rectTransform, L.Inner.QHead);
            // interview gate: the four questions stay hidden until YOU open them
            _interviewBtn = GridButton(qPanel, "interview", "QUESTION", ToggleInterview, L.Inner.QBudget, 11);
            L.Place(_interviewBtn.GetComponent<RectTransform>(), L.Inner.QBudget);
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
                var hh = b.gameObject.AddComponent<HoverHint>();
                hh.Init(HintFor(qids[i]), SetQHint);
                b.gameObject.SetActive(false);
            }
            _testimony = UIFactory.MaskedLabel(qPanel, "testimony",
                L.Inner.QTestimony.X, L.Inner.QTestimony.Y, L.Inner.QTestimony.W, L.Inner.QTestimony.H,
                10, new Color(0.30f, 0.30f, 0.29f), TextAnchor.UpperLeft, true);
            _testimony.gameObject.SetActive(false);
            _qHint = UIFactory.Label(qPanel, "qhint", "", 10,
                new Color(0.42f, 0.42f, 0.42f), TextAnchor.MiddleLeft);
            L.Place(_qHint.rectTransform, L.Inner.QHint);
            _qHint.gameObject.SetActive(false);

            _scanBtn = GridButton(_desk, "scan", "SCAN ID", DoScan, L.UtilityScan, 12);
            Reg("scan", _scanBtn, L.UtilityScan);
            _recordsBtn = GridButton(_desk, "records", "TONIGHT'S LOG",
                () => { RefreshRecords(); ToggleOverlay(_recordsRoot); }, L.UtilityRecords, 12);
            Reg("records", _recordsBtn, L.UtilityRecords);
            _mapBtn = GridButton(_desk, "map", "MAP", () => _gameRoot.OpenMap(), L.UtilityMap, 12);
            Reg("map", _mapBtn, L.UtilityMap);
            _notesBtn = GridButton(_desk, "notes", "RULES NOTE",
                () => ToggleOverlay(_notesRoot), L.UtilityNotes, 12);
            Reg("notes", _notesBtn, L.UtilityNotes);

            _rejectBtn = GridButton(_desk, "reject", "CALL PRINCIPAL", AskPrincipal, L.StampReject, 12, true);
            MakeRound(_rejectBtn, false);
            Reg("callprincipal", _rejectBtn, L.StampReject);
            _allowBtn = GridButton(_desk, "allow", "ALLOW", () => Stamp(Verdict.Allow), L.StampAllow, 12);
            MakeRound(_allowBtn, true);
            Reg("allow", _allowBtn, L.StampAllow);

            var mon = UIFactory.BorderedPanel(_desk, "monitor", new Color(0.055f, 0.062f, 0.055f),
                UIFactory.Border, 2);
            Reg("monitor", mon, L.Monitor);
            var camCap = UIFactory.Label(mon, "camcap", "CAMERA", 11, UIFactory.InkDim, TextAnchor.MiddleCenter);
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
            SetQHint("");
        }

        private void SetQHint(string hover)
        {
            if (_qHint == null) return;
            _qHint.text = string.IsNullOrEmpty(hover) ? DefaultQHint() : hover;
        }

        private string DefaultQHint() =>
            _interviewOpen && _budget.Remaining <= 0
                ? "No more questions — they're getting nervous."
                : "Hover a question: each cross-checks one paper line. Two per inspection.";

        private static string HintFor(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "Cross-check the course against the record sheet.";
                case QuestionId.Year: return "Must match the year level on both papers.";
                case QuestionId.Status: return "Transferees enrol mid-year — the record says.";
                default: return "Rooming lists live in the RULES NOTE overlay.";
            }
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

        // Decisions are clickable only while an inspection is live AND the ID is in hand;
        // the interview column swaps between the QUESTION gate and the open questions.
        private void RefreshDecisionButtons()
        {
            bool collapsed = Queue != null && Queue.Collapsed;
            bool atWindow = _current != null && !_verdictLocked && !collapsed;
            bool live = atWindow && _documentsTaken;
            for (int i = 0; i < 4; i++)
            {
                _questionButtons[i].gameObject.SetActive(_interviewOpen);
                UIFactory.SetButtonEnabled(_questionButtons[i],
                    atWindow && _interviewOpen && !_budget.HasAsked(QuestionSystem.All[i]) && _budget.Remaining > 0);
            }
            _interviewBtn.gameObject.SetActive(atWindow && !_interviewOpen);
            _budgetLabel.gameObject.SetActive(_interviewOpen);
            _testimony.gameObject.SetActive(_interviewOpen);
            _qHint.gameObject.SetActive(_interviewOpen);
            RefreshQuestionsPanel();
            if (_handoverTarget != null) _handoverTarget.SetActive(atWindow && !_documentsTaken);
            UIFactory.SetButtonEnabled(_allowBtn, live);
            UIFactory.SetButtonEnabled(_rejectBtn, live);
            UIFactory.SetButtonEnabled(_patrolBtn, !atWindow && !_bannerRoot.activeSelf &&
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

            _gameRoot.Audio.PlaySfx(verdict == Verdict.Allow ? "sfx_stamp_allow" : "sfx_stamp_reject", 1f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.VerdictStamped, outcome));
            if (_stampOverlay != null) StartCoroutine(StampPunch(verdict));

            // silent verdict: the log records the call, never whether it was right
            _tonightsLog.Append("  ").Append(_current.IsAdministrator ? "ADMINISTRATOR" : _current.ClaimedName)
                .Append(" — ").Append(verdict == Verdict.Allow ? "ALLOWED" : "REJECTED").Append("\n");

            var resolved = _current;
            _gameRoot.OnVerdictResolved(resolved, outcome);           // counters update BEFORE the banner reads them
            if (_gameRoot.State == AppViewState.Ending) { Hide(); return; } // ending took over (C3/C4)
            if (Queue != null) Queue.CommitActiveResolution();                // the queue books it NOW — a
                                                                      // quit mid-banner can never make
                                                                      // this visitor stampable again
            ShowBanner(verdict);
            RefreshHud();

            StartCoroutine(AfterBanner(resolved));
        }

        // Silent verdict: the banner states the decision and nothing else — never whether
        // the call was correct. The thrill lives in not knowing until the night judges you.
        private void ShowBanner(Verdict verdict)
        {
            _bannerTitle.text = verdict == Verdict.Allow ? "ACCESS GRANTED" : "ACCESS DENIED";
            _bannerTitle.color = verdict == Verdict.Allow ? UIFactory.Ink : UIFactory.AccentBright;
            _bannerBody.text = "";
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

        private IEnumerator AfterBanner(VisitorData resolved)
        {
            yield return WaitUnpaused(BannerSeconds);
            _bannerRoot.SetActive(false);
            if (_gameRoot.State != AppViewState.Checkpoint && _gameRoot.State != AppViewState.AdminEncounter) yield break;

            // the resolved visitor leaves the window; the oldest waiting visitor steps up
            // immediately (no second waiting period when someone is already queued)
            var promoted = Queue != null ? Queue.CompleteActive() : null;

            if (resolved.IsAdministrator && _gameRoot.Session.AdminRejected)
            {
                PlayAdminRejectOutro();
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
            RefreshQueueLine();
            if (Queue != null && Queue.ShiftComplete)
                SayLine("RADIO", "That's the last of them… for now. Clock out when the floor is clear.");
            else if (Queue == null || Queue.InfiniteSource || Queue.RemainingSource > 0)
                SayLine("RADIO", "Yard's quiet for a moment — patrol now if you need to. The line starts again when you're back.");
            RefreshHud();
        }

        // Rejecting the district administrator ends the night: their parting threat plays,
        // then the callback sends the waiting line home whatever is still in it.
        private void PlayAdminRejectOutro()
        {
            var outro = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "…You will regret this. We are already inside your records." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The thing wearing Marlowe's face unfolds itself from the window and walks back into the dark. The seal on the server room holds — for tonight." },
            };
            _gameRoot.Dialogue.Play(outro, () =>
            {
                // the night is over whatever is still waiting: the line is sent home
                if (Queue != null) Queue.AbandonLine();
                _current = null;
                _verdictLocked = true;
                SetVisitorVisible(false);
                RefreshQueueLine();
                RefreshHud();
                _gameRoot.Audio.PlayMusic("checkpoint");
            });
        }

        private void AskPrincipal()
        {
            if (_current == null || _verdictLocked) return;
            if (_confirmText != null) _confirmText.text = "PRINCIPAL... (Are you sure?)";
            if (_confirmBox != null) _confirmBox.SetActive(true);
        }
    }
}
