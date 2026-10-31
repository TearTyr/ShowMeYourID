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
    // The Night-5 administrator conversation: the choice panel that takes over the right
    // column while he is at the window. One area of the inspection desk; see CheckpointView.cs
    // for the shared fields and GameLogic/AdminTalk.cs for what he says. This file only shows
    // his answers and the choices, and passes your pick on.
    //
    // Rules of this panel:
    //  * the conversation state lives in Session.AdminTalk (this view keeps no copy);
    //  * a pick is final the moment it is clicked (_talkBusy), so a double click cannot take
    //    the same option twice;
    //  * talking never stamps, never gives clues and never changes the score. The one
    //    exception is REFUSE ENTRY, which is exactly the DENY stamp (RefuseAdminEntry);
    //  * while it is open the questions panel and the small map step aside and come back
    //    when it closes. The classroom question still works with the 1 key meanwhile.
    public partial class CheckpointView
    {
        private const int MaxTalkOptions = 8;

        private GameObject _talkPanel;
        private Text _talkHead, _talkSub;
        private RectTransform _talkOptRoot;
        private readonly List<Button> _talkOptButtons = new List<Button>();
        private Button _talkBar;
        private GameObject _qPanelGo;          // steps aside while the conversation is open
        private bool _talkOpen;
        private bool _talkBusy;                // his answer is on screen: no picks yet

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

            // after stepping back the conversation stays available until the stamp falls
            _talkBar = GridButton(_desk, "admintalkbar", "SPEAK THROUGH THE GLASS  [T]",
                () => OpenAdminTalk(), L.AdminTalkBar, 10);
            Reg("admintalkbar", _talkBar.GetComponent<RectTransform>(), L.AdminTalkBar);
            _talkBar.gameObject.SetActive(false);

            // a verdict on the administrator ends the encounter for good; a verdict on anyone
            // else only closes the panel if it happens to be open
            _subscriptions.Add(_gameRoot.Bus.Subscribe(EventIds.VerdictStamped,
                _ => CloseAdminTalk(_current != null && _current.IsAdministrator)));
        }

        private bool ClueFound(string key)
        {
            foreach (var k in _gameRoot.Session.Clues.FoundKeys)
                if (k == key) return true;
            return false;
        }

        // What the script may use, read from the real papers and the real clue tracker.
        private AdminTalkContext BuildTalkContext()
        {
            var v = _current;
            return new AdminTalkContext
            {
                BadgeName = v != null ? v.ClaimedName : "",
                MemoName = v != null ? v.RecordName : "",
                BadgeNumber = v != null ? v.IdNumber : "",
                MemoNumber = v != null ? v.RecordIdNumber : "",
                AnswerClassroom = v != null ? v.ClassroomAnswer : "",
                CluePhoto = ClueFound("photo"),
                ClueCabinet = ClueFound("cabinet"),
                AskedClassroom = _classroomAsked,
            };
        }

        // ---------------------------------------------------------------- open / close
        /// <summary>Open the conversation (the arrival speech ended, or T / the bar was used).
        /// The first open shows his opening line; a re-open shows the options where you left.</summary>
        internal void OpenAdminTalk()
        {
            if (!AdminAtWindow || _talkOpen || _talkBusy) return;
            var st = Talk;
            if (st == null || st.Completed) return;
            _talkOpen = true;
            SetTalkColumn(true);
            if (string.IsNullOrEmpty(st.Node))
            {
                _talkBusy = true;
                var step = AdminTalk.Open(st, BuildTalkContext());
                PersistTalk();
                Speak(step, ShowOptions);
            }
            else ShowOptions();
            RefreshHud();
        }

        /// <summary>Close the panel. <paramref name="final"/> ends the encounter for good (a
        /// refusal, the "step back" option, or a verdict landing); otherwise the bar stays.</summary>
        internal void CloseAdminTalk(bool final)
        {
            _talkOpen = false;
            _talkBusy = false;
            if (_talkPanel != null) _talkPanel.SetActive(false);
            var st = Talk;
            if (final && st != null) st.Completed = true;
            SetTalkColumn(false);
            if (st != null) PersistTalk();
            RefreshHud();
        }

        // The conversation borrows the right column: the questions panel and the small map
        // step aside while it is open and come back exactly as they were.
        private void SetTalkColumn(bool talking)
        {
            if (_qPanelGo == null)
            {
                var q = FindChild(transform, "qpanel_border");
                _qPanelGo = q != null ? q.gameObject : null;
            }
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
            _talkBusy = false;
            if (_talkPanel == null) return;
            ClearOptionButtons();
            var st = Talk;
            if (st == null) return;
            var ctx = BuildTalkContext();
            var options = AdminTalk.OptionsFor(st, ctx);
            _talkSub.text = "Pick what to say. The stamp is still yours alone.";
            for (int i = 0; i < options.Count && i < MaxTalkOptions; i++)
                _talkOptButtons.Add(BuildOptionRow(options[i], i, ctx));
            _talkPanel.SetActive(true);
            if (_talkBar != null) _talkBar.gameObject.SetActive(false);
        }

        // One tall row: the label wraps to two lines instead of being cut off.
        private Button BuildOptionRow(AdminTalkOption opt, int row, AdminTalkContext ctx)
        {
            var rt = UIFactory.NewUI(_talkOptRoot, "opt_" + opt.Id);
            L.Place(rt, new L.Region(L.InnerAdminTalk.Opt.X, L.InnerAdminTalk.OptY0 + row * L.InnerAdminTalk.OptStep,
                L.InnerAdminTalk.Opt.W, L.InnerAdminTalk.OptH));
            var bg = rt.gameObject.AddComponent<Image>();
            bool refuse = opt.Action == AdminTalkAction.Refuse;
            bg.color = refuse ? new Color(0.22f, 0.07f, 0.07f) : new Color(0.10f, 0.10f, 0.115f);
            bg.raycastTarget = true;
            var label = UIFactory.Label(rt, "label", AdminTalk.Resolve(opt.Label, ctx), 11,
                UIFactory.NsChalk, TextAnchor.MiddleLeft);
            label.font = UIFactory.UiFont;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
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
            return btn;
        }

        private void ClearOptionButtons()
        {
            for (int i = 0; i < _talkOptButtons.Count; i++)
                if (_talkOptButtons[i] != null) Destroy(_talkOptButtons[i].gameObject);
            _talkOptButtons.Clear();
        }

        internal void ChooseAdminTalkOption(string optionId)
        {
            if (!_talkOpen || _talkBusy) return;      // one pick at a time, ever
            var st = Talk;
            if (st == null || st.Completed) return;
            _talkBusy = true;
            ClearOptionButtons();                     // the panel goes quiet while he answers
            var step = AdminTalk.Choose(st, BuildTalkContext(), optionId);
            PersistTalk();
            if (!step.Completed) { Speak(step, ShowOptions); return; }
            CloseAdminTalk(true);
            if (step.Action == AdminTalkAction.Refuse) RefuseAdminEntry();
        }

        /// <summary>REFUSE ENTRY: exactly what stamping DENY and handing the ID back does, in
        /// one click. It goes through the same FinalizeIfReady path, so scoring, the hunt and
        /// the save all behave identically to the physical stamp.</summary>
        internal void RefuseAdminEntry()
        {
            if (!AdminAtWindow || _slipStamped) return;
            _pendingVerdict = Verdict.Reject;
            _slipStamped = true;
            _backId = true;
            PlaceStampMark(Verdict.Reject, new Vector2(100f, 60f));
            _gameRoot.Audio.PlaySfx("sfx_stamp_reject", 0.8f);
            FinalizeIfReady();
        }

        // His answer goes through the normal dialogue box (typewriter, click to continue).
        private void Speak(AdminTalkStep step, System.Action onDone)
        {
            if (step == null || string.IsNullOrEmpty(step.Text) || _gameRoot.Dialogue == null)
            {
                onDone?.Invoke();
                return;
            }
            var lines = new List<DialogueLine> { new DialogueLine { Speaker = step.Speaker, Text = step.Text } };
            _gameRoot.Dialogue.Play(lines, () => onDone?.Invoke());
        }

        private void PersistTalk()
        {
            if (_gameRoot != null) _gameRoot.PersistRun();   // the conversation rides the run save
        }

        // ---------------------------------------------------------------- desk integration
        // Called on every HUD refresh: keeps the re-open bar and the borrowed column in step with
        // who is at the window.
        internal void RefreshAdminTalk()
        {
            var st = Talk;
            bool admin = AdminAtWindow;
            if (_talkOpen && (!admin || (st != null && st.Completed))) CloseAdminTalk(false);
            bool barOn = admin && st != null && !st.Completed && !_talkOpen && !string.IsNullOrEmpty(st.Node);
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

        // Leaving the desk (patrol, quit) closes the panel but keeps the saved state.
        private void ParkAdminTalk()
        {
            if (!_talkOpen) return;
            _talkOpen = false;
            _talkBusy = false;
            if (_talkPanel != null) _talkPanel.SetActive(false);
            SetTalkColumn(false);
        }
    }
}
