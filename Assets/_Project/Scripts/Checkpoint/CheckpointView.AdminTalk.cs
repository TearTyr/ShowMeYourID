using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The Shift-5 administrator conversation: the compact choice panel that lives in the right
    // column while the administrator is at the window. One area of the inspection desk; see
    // CheckpointView.cs for the shared fields and state machine, and GameLogic/AdminTalk.cs for
    // the branching rules themselves — this file only PRESENTS nodes and forwards choices.
    //
    // Ownership rules (owner brief §7/§8):
    //  * the conversation state is Session.AdminTalk — this view holds no copy of it;
    //  * a choice commits BEFORE its lines play (_talkCommitting), so a double click can never
    //    take the same option twice, and the panel is empty while the administrator speaks;
    //  * the conversation never stamps, never budgets, never clues, never ends the run: closing
    //    it (or stamping through it) hands the desk back exactly as it was;
    //  * while it is open it borrows the questions panel's column, so that panel and the inline
    //    map step aside and are restored on close; verification questions stay reachable through
    //    their hotkeys (1-4), which is how the administrator's answers feed back into the talk.
    public partial class CheckpointView
    {
        private GameObject _talkPanel;
        private Text _talkHead, _talkSub;
        private RectTransform _talkOptRoot;
        private readonly List<Button> _talkOptButtons = new List<Button>();
        private Button _talkBar;
        private GameObject _qPanelGo;          // steps aside while the conversation is open
        private bool _talkOpen;
        private bool _talkCommitting;
        private Coroutine _talkPause;

        private AdminTalkState Talk => _gameRoot != null && _gameRoot.Session != null
            ? _gameRoot.Session.AdminTalk
            : null;

        private bool AdminAtWindow => _current != null && _current.IsAdministrator && !_verdictLocked;

        // ---------------------------------------------------------------- build
        private void BuildAdminTalk()
        {
            var panel = UIFactory.BorderedPanel(_desk, "admintalk", new Color(0.055f, 0.055f, 0.06f, 0.97f),
                UIFactory.Border, 2);
            Reg("admintalk", panel, L.AdminTalk);
            _talkPanel = panel.gameObject;
            _talkHead = UIFactory.Label(panel, "head", "THE CONVERSATION", 13,
                UIFactory.AccentBright, TextAnchor.MiddleLeft);
            _talkHead.font = UIFactory.UiFontBold;
            _talkHead.raycastTarget = false;
            L.Place(_talkHead.rectTransform, L.InnerAdminTalk.Head);
            _talkSub = UIFactory.Label(panel, "sub", "", 10, UIFactory.NsMid, TextAnchor.MiddleLeft);
            _talkSub.font = UIFactory.UiFont;
            _talkSub.raycastTarget = false;
            L.Place(_talkSub.rectTransform, L.InnerAdminTalk.Sub);
            _talkOptRoot = UIFactory.NewUI(panel, "options");
            UIFactory.AnchorsFull(_talkOptRoot);
            _talkPanel.SetActive(false);

            // the re-open bar: after stepping back, the conversation stays available until the
            // stamp falls — one slim strip, hotkey T, never covering anything else
            _talkBar = GridButton(_desk, "admintalkbar", "SPEAK THROUGH THE GLASS  [T]",
                () => OpenAdminTalk(), L.AdminTalkBar, 10);
            Reg("admintalkbar", _talkBar.GetComponent<RectTransform>(), L.AdminTalkBar);
            _talkBar.gameObject.SetActive(false);

            // a verdict on THE ADMINISTRATOR ends the encounter for good; a verdict on anyone
            // else merely closes the panel if it happens to be open (and must not mark the
            // administrator's conversation as completed — that state is the run's memory)
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.VerdictStamped,
                _ => CloseAdminTalk(_current != null && _current.IsAdministrator)));
        }

        // FoundKeys is an IReadOnlyCollection (no Linq in this assembly): a plain walk.
        private bool ClueFound(string key)
        {
            foreach (var k in _gameRoot.Session.Clues.FoundKeys)
                if (k == key) return true;
            return false;
        }

        // ---------------------------------------------------------------- context
        // Built from the LIVE run on every open/choose: the real badge/memo facts, the real
        // question answers, the real clue tracker. The engine never reaches into the session.
        private AdminTalkContext BuildTalkContext()
        {
            var v = _current;
            var st = Talk;
            var ctx = new AdminTalkContext
            {
                BadgeName = v != null ? v.ClaimedName : "",
                MemoName = v != null ? v.RecordName : "",
                BadgeNumber = v != null ? v.IdNumber : "",
                MemoNumber = v != null ? v.RecordIdNumber : "",
                AnswerCourse = "",
                AnswerStatus = "",
                AnswerFirstClassroom = v != null ? v.ClassroomAnswer : "",
                CluePhoto = ClueFound("photo"),
                ClueCabinet = ClueFound("cabinet"),
                AskedCourse = false,
                AskedStatus = false,
                AskedFirstClassroom = _classroomAsked,
            };
            if (st != null)
            {
                ctx.Compliance = st.Compliance;
                ctx.Challenge = st.Challenge;
                ctx.Evidence = st.Evidence;
                ctx.Silence = st.Silence;
                ctx.HubVisits = st.HubVisits;
            }
            return ctx;
        }

        // ---------------------------------------------------------------- open / close
        /// <summary>The arrival sequence finished (or the player pressed T / the bar): open the
        /// conversation on whatever node the state holds. First open speaks the hub; a re-open
        /// simply shows the options where they were left.</summary>
        internal void OpenAdminTalk()
        {
            if (!AdminAtWindow || _talkOpen || _talkCommitting) return;
            var st = Talk;
            if (st == null || st.Completed) return;
            _talkCommitting = true;
            SetTalkColumn(true);
            _talkOpen = true;

            if (string.IsNullOrEmpty(st.Node))
            {
                var step = AdminTalk.Open(st, BuildTalkContext());
                PersistTalk();
                SpeakStep(step, () => { ShowOptions(); _talkCommitting = false; });
            }
            else
            {
                ShowOptions();
                _talkCommitting = false;
            }
            RefreshHud();
        }

        /// <summary>Step back out of the conversation without finishing it: the desk returns to
        /// inspection, the re-open bar stays. <paramref name="final"/> closes the encounter for
        /// good (the "step back" option, or a verdict landing).</summary>
        internal void CloseAdminTalk(bool final)
        {
            if (_talkPause != null) { StopCoroutine(_talkPause); _talkPause = null; }
            _talkOpen = false;
            _talkCommitting = false;
            if (_talkPanel != null) _talkPanel.SetActive(false);
            var st = Talk;
            if (final && st != null) st.Completed = true;
            SetTalkColumn(false);
            if (st != null) PersistTalk();
            RefreshHud();
        }

        // The conversation borrows the right column: questions panel and inline map step aside
        // while it is open, and come back exactly where they were when it closes.
        private void SetTalkColumn(bool talking)
        {
            if (_qPanelGo == null)
            {
                var q = FindChild(transform, "qpanel_border");
                _qPanelGo = q != null ? q.gameObject : null;
            }
            // the question row (budget label included — it is a child of the panel) and the
            // inline map step aside for the conversation and return when it closes
            if (_qPanelGo != null) _qPanelGo.SetActive(!talking);
            if (_mapBtn != null) _mapBtn.gameObject.SetActive(!talking);
        }

        private static Transform FindChild(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindChild(t.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        // ---------------------------------------------------------------- choices
        private void ShowOptions()
        {
            if (_talkPanel == null) return;
            ClearOptionButtons();
            var st = Talk;
            if (st == null) return;
            var options = AdminTalk.OptionsFor(st, BuildTalkContext());
            _talkSub.text = st.Node == AdminTalk.HubId
                ? "The administrator waits at the glass. Choose your sentence."
                : "The conversation is yours to steer. The stamp is still yours alone.";
            for (int i = 0; i < options.Count && i < 14; i++)
            {
                var opt = options[i];
                int row = i;
                var rt = UIFactory.NewUI(_talkOptRoot, "opt_" + opt.Id);
                L.Place(rt, new L.Region(L.InnerAdminTalk.Opt.X,
                    L.InnerAdminTalk.OptY0 + row * L.InnerAdminTalk.OptStep,
                    L.InnerAdminTalk.Opt.W, L.InnerAdminTalk.OptH));
                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = new Color(0.10f, 0.10f, 0.115f);
                bg.raycastTarget = true;
                var label = UIFactory.Label(rt, "label", AdminTalk.Resolve(opt.Label, BuildTalkContext()),
                    11, UIFactory.NsChalk, TextAnchor.MiddleLeft);
                label.font = UIFactory.UiFont;
                label.raycastTarget = false;
                UIFactory.PlaceTL(label.rectTransform, 8, 0, L.InnerAdminTalk.Opt.W - 16, L.InnerAdminTalk.OptH);
                var btn = rt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.35f, 1.33f, 1.26f);
                cb.pressedColor = new Color(1.6f, 1.58f, 1.5f);
                cb.fadeDuration = 0.10f;
                btn.colors = cb;
                string captured = opt.Id;
                btn.onClick.AddListener(() => ChooseAdminTalkOption(captured));
                rt.gameObject.AddComponent<HoverCursor>();
                _talkOptButtons.Add(btn);
            }
            _talkPanel.SetActive(true);
            if (_talkBar != null) _talkBar.gameObject.SetActive(false);
        }

        private void ClearOptionButtons()
        {
            for (int i = 0; i < _talkOptButtons.Count; i++)
                if (_talkOptButtons[i] != null) Destroy(_talkOptButtons[i].gameObject);
            _talkOptButtons.Clear();
        }

        internal void ChooseAdminTalkOption(string optionId)
        {
            if (!_talkOpen || _talkCommitting) return;      // one commit at a time, ever
            var st = Talk;
            if (st == null || st.Completed) return;
            _talkCommitting = true;
            ClearOptionButtons();                           // the panel goes quiet while it speaks
            _talkPanel.SetActive(true);
            var step = AdminTalk.Choose(st, BuildTalkContext(), optionId);
            PersistTalk();
            if (step.Completed)
            {
                CloseAdminTalk(true);
                return;
            }
            SpeakStep(step, () => { ShowOptions(); _talkCommitting = false; });
        }

        // The administrator's beat: an optional deliberate pause (silence nodes), then the
        // lines through the established DialogueView — typewriter, Reduced Motion instant,
        // pause-gated — and only then the next set of choices.
        private void SpeakStep(AdminTalkStep step, System.Action onDone)
        {
            if (step == null || step.Lines == null || step.Lines.Count == 0) { onDone?.Invoke(); return; }
            if (_talkPause != null) { StopCoroutine(_talkPause); _talkPause = null; }
            if (step.PauseSeconds > 0.01f)
                _talkPause = StartCoroutine(SpeakAfterPause(step, onDone));
            else
                PlayTalkLines(step, onDone);
        }

        private IEnumerator SpeakAfterPause(AdminTalkStep step, System.Action onDone)
        {
            yield return WaitUnpaused(step.PauseSeconds);
            _talkPause = null;
            PlayTalkLines(step, onDone);
        }

        private void PlayTalkLines(AdminTalkStep step, System.Action onDone)
        {
            var lines = new List<DialogueLine>();
            for (int i = 0; i < step.Lines.Count; i++)
                lines.Add(new DialogueLine { Speaker = step.Speaker, Text = step.Lines[i] });
            if (_gameRoot.Dialogue != null)
                _gameRoot.Dialogue.Play(lines, () => onDone?.Invoke());
            else
                onDone?.Invoke();
        }

        private void PersistTalk()
        {
            // the conversation rides the run save, same convention as the queue budget mask
            if (_gameRoot != null) _gameRoot.PersistRun();
        }

        // ---------------------------------------------------------------- desk integration
        // Called from PresentFromQueue/ResumeRun paths: an administrator who has not been
        // talked out yet keeps the re-open bar; a finished conversation keeps nothing.
        internal void RefreshAdminTalk()
        {
            var st = Talk;
            bool admin = AdminAtWindow;
            if (_talkOpen && (!admin || (st != null && st.Completed))) CloseAdminTalk(false);
            bool barOn = admin && st != null && !st.Completed && !_talkOpen &&
                         !string.IsNullOrEmpty(st.Node);
            if (_talkBar != null) _talkBar.gameObject.SetActive(barOn);
            if (!_talkOpen) SetTalkColumn(false);
        }

        internal bool AdminTalkOpen => _talkOpen;

        // T at the desk: the same act as the re-open bar / stepping back.
        internal void ToggleAdminTalkByKey()
        {
            if (!AdminAtWindow) return;
            if (_talkOpen) CloseAdminTalk(false);
            else OpenAdminTalk();
        }
    }
}
