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
    // The black NIGHT title card behind intro dialogue. Created by each scene bootstrap.
    // PlayNightTitle fades the card's words in slowly, holds them, then hands over to the
    // dialogue box - a click skips ahead.
    public class LoadingView : MonoBehaviour, IScreenView
    {
        private Text _label, _sub;
        private GameObject _bg;
        private Coroutine _routine;
        private Action _onTitleDone;          // fires exactly once when the night title ends or is skipped
        private const int FadeSteps = 40;     // stepped waits (not a frame loop) so any frame rate paces it
        private const float FadeInSeconds = 1.8f, HoldSeconds = 1.3f, FadeOutSeconds = 1f;
        private const float RevealSeconds = 1.4f;   // black lifting off the shut booth

        public void Init(GameRoot root)
        {
            _bg = UIFactory.Panel(transform, "bg", Color.black).gameObject;
            UIFactory.AnchorsFull(_bg.GetComponent<RectTransform>());
            _label = UIFactory.Label(_bg.transform, "label", "", 40, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0, 30));
            _sub = UIFactory.Label(_bg.transform, "sub", "HOLLOWBROOK COLLEGE - NIGHT CUSTODIAN PROGRAM", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 30), new Vector2(0, -30));
            // a click anywhere on the card skips the title (only live while a title is playing)
            var skip = _bg.AddComponent<Button>();
            skip.targetGraphic = _bg.GetComponent<Image>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(SkipTitle);
            _bg.SetActive(false);
        }

        public void Flash(string label)
        {
            _label.text = label ?? "";
            _bg.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HideAfter(1.35f));
        }

        private IEnumerator HideAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            _bg.SetActive(false);
            _routine = null;
        }

        /// <summary>The night title: on a black card "NIGHT 1" and its date fade in slowly, hold,
        /// and fade out again, then <paramref name="onDone"/> runs exactly once so the dialogue box
        /// can open on the black. A click skips ahead. A new call replaces any title still playing.</summary>
        public void PlayNightTitle(string title, string sub, Action onDone)
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _label.text = title ?? "";
            _sub.text = sub ?? "";
            SetTitleAlpha(0f);
            _bg.SetActive(true);
            _onTitleDone = onDone;
            _routine = StartCoroutine(NightTitleRoutine());
        }

        private IEnumerator NightTitleRoutine()
        {
            for (int i = 1; i <= FadeSteps; i++)
            {
                SetTitleAlpha(i / (float)FadeSteps);
                yield return new WaitForSecondsRealtime(FadeInSeconds / FadeSteps);
            }
            yield return new WaitForSecondsRealtime(HoldSeconds);
            for (int i = FadeSteps - 1; i >= 0; i--)
            {
                SetTitleAlpha(i / (float)FadeSteps);
                yield return new WaitForSecondsRealtime(FadeOutSeconds / FadeSteps);
            }
            _routine = null;
            FinishTitle();
        }

        private void SkipTitle()
        {
            if (_onTitleDone == null) return;       // nothing is playing: the card is just a backdrop
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            SetTitleAlpha(0f);
            FinishTitle();
        }

        /// <summary>The black card fades away, showing what is behind it (the shut booth).</summary>
        public void RevealDesk()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _onTitleDone = null;
            SetTitleAlpha(0f);
            _bg.SetActive(true);
            _routine = StartCoroutine(RevealRoutine());
        }

        private IEnumerator RevealRoutine()
        {
            for (int i = FadeSteps - 1; i >= 0; i--)
            {
                SetCardAlpha(i / (float)FadeSteps);
                yield return new WaitForSecondsRealtime(RevealSeconds / FadeSteps);
            }
            Hide();
        }

        private void SetCardAlpha(float a)
        {
            var img = _bg.GetComponent<Image>();
            var c = img.color; c.a = a; img.color = c;
        }

        private void FinishTitle()
        {
            var done = _onTitleDone;
            _onTitleDone = null;                     // exactly once, whether it ended or was skipped
            if (done != null) done();
        }

        private void SetTitleAlpha(float a)
        {
            var c = _label.color; c.a = a; _label.color = c;
            var d = _sub.color; d.a = a; _sub.color = d;
        }

        public void Show(string label)
        {
            _label.text = label ?? "";
            SetTitleAlpha(1f);
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(true);
        }

        public void Show() { _bg.SetActive(true); }
        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _onTitleDone = null;                     // a hidden card never fires a late "title done"
            SetCardAlpha(1f);
            _bg.SetActive(false);
        }
    }
}
