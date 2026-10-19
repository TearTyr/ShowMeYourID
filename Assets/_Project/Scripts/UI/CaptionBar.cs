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
    // Subtitle line for important sounds. Created by each scene bootstrap.
    public class CaptionBar : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Text _text;
        private GameObject _bg;
        private float _until;
        private EventSubscription _sub;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _bg = UIFactory.Panel(transform, "bg", new Color(0, 0, 0, 0.72f)).gameObject;
            _bg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; // display only, never blocks buttons
            UIFactory.Anchor(_bg.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(880, 34), new Vector2(0, 54));
            _text = UIFactory.Label(_bg.transform, "t", "", 16, new Color(0.85f, 0.85f, 0.8f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_text.rectTransform);
            _bg.SetActive(false);
            _sub = root.Bus.Subscribe(EventIds.CaptionRequested, e =>
            {
                if (_gameRoot.Settings == null || !_gameRoot.Settings.Captions) return;
                _text.text = e.Text ?? "";
                _until = Time.unscaledTime + (e.Value > 0 ? e.Value : 2.5f);
                _bg.SetActive(true);
            });
        }

        private void Update()
        {
            if (_bg.activeSelf && Time.unscaledTime > _until) _bg.SetActive(false);
        }

        private void OnDestroy() { _sub?.Dispose(); }
        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
