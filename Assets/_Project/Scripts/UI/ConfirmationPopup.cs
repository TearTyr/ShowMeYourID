using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Yes/No box used for exits and data reset. Created by each scene bootstrap.
    public class ConfirmationPopup : MonoBehaviour, IScreenView
    {
        private GameObject _root;
        private Text _title, _body;
        private System.Action _onYes;
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _root = UIFactory.Panel(transform, "panel", new Color(0, 0, 0, 0.55f)).gameObject;
            UIFactory.AnchorsFull(_root.GetComponent<RectTransform>());
            var panel = UIFactory.BorderedPanel(_root.transform, "box", new Color(0.07f, 0.07f, 0.08f, 0.98f), UIFactory.Border);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(560, 240), Vector2.zero);
            _title = UIFactory.Label(panel, "title", "", 24, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1), new Vector2(520, 36), new Vector2(0, -34));
            _body = UIFactory.Label(panel, "body", "", 16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_body.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 70), new Vector2(0, -92));
            var yes = UIFactory.StandardButton(panel, "yes", "YES", () =>
            {
                var act = _onYes;
                Hide();
                act?.Invoke();
            }, 160, 44, true);
            UIFactory.Anchor(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(-100, 40));
            var no = UIFactory.StandardButton(panel, "no", "NO", Hide, 160, 44);
            UIFactory.Anchor(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(100, 40));
            _root.SetActive(false);
        }

        public void Ask(string title, string body, System.Action onYes)
        {
            _title.text = title;
            _body.text = body;
            _onYes = onYes;
            _root.SetActive(true);
        }

        public void Show() { }
        public void Hide() { _root.SetActive(false); _onYes = null; }
    }
}
