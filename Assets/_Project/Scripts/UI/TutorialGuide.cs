using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.UI
{
    /// <summary>The Night 1 tutorial's text box: a title, a few lines, SKIP TUTORIAL always, and a button on the steps
    /// that wait for one (CONTINUE after the keybinds, GOT IT at the end). It lives in the HUD frame, so it shows at
    /// the desk, in the hallway and in a peek. TutorialSteps decides what it says; this class only draws it.</summary>
    public class TutorialGuide : MonoBehaviour
    {
        // the lessons' box is short; the keybind card is a list, so its box is taller
        private static readonly L.Region LessonBox = new L.Region(470, 82, 430, 98);
        private static readonly L.Region KeysBox = new L.Region(470, 82, 430, 190);
        private const float TitleHeight = 18f;
        private const float ButtonHeight = 20f;
        private const float Pad = 10f;
        private const int ButtonFontSize = 11;

        private GameRoot _root;
        private RectTransform _box;
        private Text _title;
        private Text _body;
        private Button _gotIt;
        private Text _gotItLabel;
        private Button _skip;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root, RectTransform frame)
        {
            _root = root;
            _box = UIFactory.BorderedPanel(frame, "tutorialguide", UIFactory.PanelBase, UIFactory.AccentBright, 2);
            _title = UIFactory.Label(_box, "title", "", 14, UIFactory.AccentBright, TextAnchor.UpperLeft);
            _title.font = UIFactory.UiFontBold;
            _body = UIFactory.Label(_box, "body", "", 13, UIFactory.Ink, TextAnchor.UpperLeft);
            _gotIt = SmallButton("gotit", "GOT IT", root.TutorialGotIt, 100);
            _gotItLabel = _gotIt.GetComponentInChildren<Text>();
            _skip = SmallButton("skip", "SKIP TUTORIAL", root.SkipTutorial, 130);
            _subs.Add(root.Bus.Subscribe(EventIds.TutorialChanged, e => Refresh()));
            Refresh();
        }

        private Button SmallButton(string name, string label, System.Action onClick, int width)
        {
            var button = UIFactory.StandardButton(_box, name, label, onClick, width, (int)ButtonHeight);
            UIFactory.SetBaseFontSize(button.GetComponentInChildren<Text>(), ButtonFontSize);
            return button;
        }

        // The box, the words and the buttons for the box's height: the buttons sit along the bottom edge.
        private void Layout(L.Region box)
        {
            L.Place(_box, box);
            float bottom = box.H - ButtonHeight - 4f;
            L.Place(_title.rectTransform, new L.Region(Pad, 4f, box.W - 2f * Pad, TitleHeight));
            L.Place(_body.rectTransform, new L.Region(Pad, 4f + TitleHeight, box.W - 2f * Pad, bottom - 4f - TitleHeight));
            L.Place(_gotIt.GetComponent<RectTransform>(), new L.Region(Pad, bottom, 100f, ButtonHeight));
            L.Place(_skip.GetComponent<RectTransform>(), new L.Region(box.W - Pad - 130f, bottom, 130f, ButtonHeight));
        }

        /// <summary>Show the current step, or nothing when the tutorial is over.</summary>
        public void Refresh()
        {
            if (_root == null || _box == null) return;
            var step = _root.TutorialStep;
            var state = _root.State;
            bool playing = state == AppViewState.Checkpoint || state == AppViewState.Exploration || state == AppViewState.RoomPeek;
            _box.gameObject.SetActive(TutorialSteps.Active(step) && playing);   // not over the night card or the map
            Layout(TutorialSteps.IsKeyList(step) ? KeysBox : LessonBox);
            _title.text = TutorialSteps.Title(step);
            _body.text = TutorialSteps.Body(step);
            _gotIt.gameObject.SetActive(TutorialSteps.NeedsGotIt(step));
            if (_gotItLabel != null) _gotItLabel.text = TutorialSteps.ButtonLabel(step);
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }
    }
}
