using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Subtitle line for important sounds. Lives in each scene file, connected by the scene script.
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
            // Text only: no box behind it. A dark outline keeps the words readable on any picture, and an
            // empty caption shows nothing at all (a lone black strip at the bottom looked like a bug).
            _bg = UIFactory.Panel(transform, "bg", new Color(0, 0, 0, 0)).gameObject;
            _bg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; // display only, never blocks buttons
            UIFactory.Anchor(_bg.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(880, 34), new Vector2(0, 54));
            _text = UIFactory.Label(_bg.transform, "t", "", 16, new Color(0.92f, 0.92f, 0.88f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_text.rectTransform);
            _text.raycastTarget = false;
            var outline = _text.gameObject.GetOrAdd<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            _bg.SetActive(false);
            _sub?.Dispose();   // a second Init must not leave the first handler behind
            _sub = root.Bus.Subscribe(EventIds.CaptionRequested, e =>
            {
                // the scene that owned this bar is gone (its text was destroyed): stop listening instead of failing on every caption
                if (this == null || _text == null) { _sub?.Dispose(); return; }
                if (_gameRoot.Settings == null || !_gameRoot.Settings.Captions) return;
                // callers send the words as the event's data (GameEvent.Of(id, text, seconds, null)), others
                // as its Text: either works. This used to read only Text, so every caption showed an EMPTY
                // black strip at the bottom of the screen.
                string words = !string.IsNullOrEmpty(e.Text) ? e.Text : e.Data as string;
                if (string.IsNullOrEmpty(words)) return;   // nothing to say, nothing to show
                _text.text = words;
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
