using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.UI
{
    /// <summary>The Night 1 tutorial's text box: a title, a few lines, SKIP TUTORIAL always, and GOT IT
    /// on the last step. It lives in the HUD frame, so it shows at the desk, in the hallway and in a peek.
    /// TutorialSteps decides what it says; this class only draws it.</summary>
    public class TutorialGuide : MonoBehaviour
    {
        private static readonly L.Region Box = new L.Region(470, 82, 430, 98);
        private static readonly L.Region TitleSlot = new L.Region(10, 4, 410, 18);
        private static readonly L.Region BodySlot = new L.Region(10, 22, 410, 52);
        private static readonly L.Region GotItSlot = new L.Region(10, 74, 100, 20);
        private static readonly L.Region SkipSlot = new L.Region(290, 74, 130, 20);
        private const int ButtonFontSize = 11;

        private GameRoot _root;
        private RectTransform _box;
        private Text _title;
        private Text _body;
        private Button _gotIt;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root, RectTransform frame)
        {
            _root = root;
            _box = UIFactory.BorderedPanel(frame, "tutorialguide", UIFactory.PanelBase, UIFactory.AccentBright, 2);
            L.Place(_box, Box);
            _title = UIFactory.Label(_box, "title", "", 14, UIFactory.AccentBright, TextAnchor.UpperLeft);
            _title.font = UIFactory.UiFontBold;
            L.Place(_title.rectTransform, TitleSlot);
            _body = UIFactory.Label(_box, "body", "", 13, UIFactory.Ink, TextAnchor.UpperLeft);
            L.Place(_body.rectTransform, BodySlot);
            _gotIt = SmallButton("gotit", "GOT IT", root.TutorialGotIt, GotItSlot);
            SmallButton("skip", "SKIP TUTORIAL", root.SkipTutorial, SkipSlot);
            _subs.Add(root.Bus.Subscribe(EventIds.TutorialChanged, e => Refresh()));
            Refresh();
        }

        private Button SmallButton(string name, string label, System.Action onClick, L.Region slot)
        {
            var button = UIFactory.StandardButton(_box, name, label, onClick, (int)slot.W, (int)slot.H);
            L.Place(button.GetComponent<RectTransform>(), slot);
            UIFactory.SetBaseFontSize(button.GetComponentInChildren<Text>(), ButtonFontSize);
            return button;
        }

        /// <summary>Show the current step, or nothing when the tutorial is over.</summary>
        public void Refresh()
        {
            if (_root == null || _box == null) return;
            var step = _root.TutorialStep;
            var state = _root.State;
            bool playing = state == AppViewState.Checkpoint || state == AppViewState.Exploration || state == AppViewState.RoomPeek;
            _box.gameObject.SetActive(TutorialSteps.Active(step) && playing);   // not over the night card or the map
            _title.text = TutorialSteps.Title(step);
            _body.text = TutorialSteps.Body(step);
            _gotIt.gameObject.SetActive(TutorialSteps.NeedsGotIt(step));
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }
    }
}
