using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class DialogueLine
    {
        public string Speaker = "";
        public string Text = "";
        public string PortraitSpriteKey; // optional procedural portrait key
    }

    // Click-through dialogue box with typewriter text. Created by each scene bootstrap.
    public class DialogueView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private GameObject _panel;
        private Text _speaker, _text, _continue;
        private Image _dim;
        private Image _portrait;
        private List<DialogueLine> _queue = new List<DialogueLine>();
        private int _qIndex;
        private Action _onDone;
        private bool _typing;
        private string _fullText;
        private float _typed;

        public bool IsActive { get; private set; }

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _dim = UIFactory.Dimmer(transform, "dim", 0.35f);
            _dim.raycastTarget = true;

            // Pre-gameplay narration rides the kit's CAPTION BAR (sec_caption_bar): the torn
            // white bar at its own proportions — the panel rect carries the bar's 325:90
            // aspect, so the art scales uniformly and is never stretched — with dark ink
            // inside the torn edges. The desk speech bubble (sec_bubble) stays the desk
            // bubble's skin only: narration and student dialogue are two UI contexts.
            var panelRt = UIFactory.NewUI(transform, "panel_border");
            UIFactory.Anchor(panelRt, new Vector2(0.5f, 0f), new Vector2(940, 220), new Vector2(0, 202));
            var bar = panelRt.gameObject.AddComponent<Image>();
            var barSprite = ProceduralArt.Get("sec_caption_bar");
            if (barSprite != null) { bar.sprite = barSprite; bar.color = Color.white; }
            else bar.color = new Color(0.92f, 0.92f, 0.90f);   // no kit: plain paper stock
            bar.raycastTarget = false;

            _portrait = UIFactory.Img(panelRt, "portrait", ProceduralArt.Get("face_placeholder"));
            UIFactory.Anchor(_portrait.rectTransform, new Vector2(0, 0.5f), new Vector2(104, 128), new Vector2(-430, 0));
            _portrait.gameObject.SetActive(false);

            // dark ink on the white bar; the speaker line in the desk's restrained red
            _speaker = UIFactory.Label(panelRt, "speaker", "", 16,
                new Color(0.55f, 0.12f, 0.14f), TextAnchor.MiddleLeft);
            UIFactory.Anchor(_speaker.rectTransform, new Vector2(0, 1), new Vector2(800, 26), new Vector2(472, -43));

            _text = UIFactory.Label(panelRt, "text", "", 18, UIFactory.NsInk);
            var trt = _text.rectTransform;
            trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(72, 40); trt.offsetMax = new Vector2(-72, -64);

            _continue = UIFactory.Label(panelRt, "cont", "▼", 16,
                new Color(0.20f, 0.20f, 0.20f), TextAnchor.MiddleRight);
            UIFactory.Anchor(_continue.rectTransform, new Vector2(1, 0), new Vector2(40, 24), new Vector2(-84, 38));
            _continue.gameObject.SetActive(false);

            var clickCatcher = panelRt.gameObject.AddComponent<Button>();
            clickCatcher.targetGraphic = null;
            clickCatcher.onClick.AddListener(Advance);
            var dimBtn = _dim.gameObject.AddComponent<Button>();
            dimBtn.targetGraphic = _dim;
            dimBtn.onClick.AddListener(Advance);

            IsActive = false;
            gameObject.SetActive(true);
            _panel = panelRt.gameObject;
            HideVisuals();
        }

        public void Play(List<DialogueLine> lines, Action onDone)
        {
            if (lines == null || lines.Count == 0) { onDone?.Invoke(); return; }
            _queue = new List<DialogueLine>(lines);
            _qIndex = 0;
            _onDone = onDone;
            IsActive = true;
            ShowVisuals();
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var line = _queue[_qIndex];
            _speaker.text = (line.Speaker ?? "").ToUpperInvariant();
            _fullText = line.Text ?? "";
            _portrait.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(line.PortraitSpriteKey))
            {
                _portrait.sprite = ProceduralArt.Get(line.PortraitSpriteKey);
                _portrait.gameObject.SetActive(_portrait.sprite != null);
            }
            bool instant = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            _typed = instant ? _fullText.Length : 0f;
            _text.text = instant ? _fullText : "";
            _typing = !instant;
            _continue.gameObject.SetActive(instant);
            if (!instant) StartCoroutine(TypeRoutine());
        }

        private IEnumerator TypeRoutine()
        {
            float cps = _gameRoot.Config != null ? _gameRoot.Config.dialogueCharsPerSecond : 42f;
            float tick = 0f;
            while (_typing && _typed < _fullText.Length)
            {
                _typed += cps * Time.unscaledDeltaTime;
                int n = Mathf.Min(_fullText.Length, (int)_typed);
                if (n != _text.text.Length)
                {
                    _text.text = _fullText.Substring(0, n);
                    tick -= Time.unscaledDeltaTime;
                    if (tick <= 0f && n < _fullText.Length && n % 3 == 0)
                    {
                        tick = 0.09f;
                        _gameRoot.Audio.PlaySfx("sfx_click", 0.10f);
                    }
                }
                yield return null;
            }
            _text.text = _fullText;
            _typing = false;
            _continue.gameObject.SetActive(true);
        }

        public void Advance()
        {
            if (!IsActive) return;
            if (_typing)
            {
                _typing = false;
                _typed = _fullText.Length;
                _text.text = _fullText;
                _continue.gameObject.SetActive(true);
                return;
            }
            _qIndex++;
            if (_qIndex < _queue.Count) { ShowCurrent(); return; }
            IsActive = false;
            HideVisuals();
            var cb = _onDone;
            _onDone = null;
            Debug.Log("[Flow] dialogue finished, invoking callback");
            cb?.Invoke();
        }

        private void Update()
        {
            if (!IsActive) return;
            if (_gameRoot != null && _gameRoot.Paused) return; // no dialogue advance while paused
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                Advance();
        }

        private void ShowVisuals() { _dim.gameObject.SetActive(true); _panel.SetActive(true); }
        private void HideVisuals() { _dim.gameObject.SetActive(false); _panel.SetActive(false); }

        public void Show() { /* dialogue visibility is queue-driven */ }
        public void Hide()
        {
            IsActive = false;
            _onDone = null;
            HideVisuals();
        }
    }
}
