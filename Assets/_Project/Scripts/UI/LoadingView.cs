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
    public class LoadingView : MonoBehaviour, IScreenView
    {
        private Text _label;
        private GameObject _bg;
        private Coroutine _routine;

        public void Init(GameRoot root)
        {
            _bg = UIFactory.Panel(transform, "bg", Color.black).gameObject;
            UIFactory.AnchorsFull(_bg.GetComponent<RectTransform>());
            // the shut booth behind the NIGHT card: the kit's closed-shutter composite, dimmed
            var kitBg = UIFactory.Img(_bg.transform, "kitbg", ProceduralArt.Get("sec_background_close"),
                new Color(1f, 1f, 1f, 0.35f));
            UIFactory.AnchorsFull(kitBg.rectTransform);
            kitBg.raycastTarget = false;
            kitBg.rectTransform.SetAsFirstSibling();
            _label = UIFactory.Label(_bg.transform, "label", "", 40, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0, 30));
            var sub = UIFactory.Label(_bg.transform, "sub", "HOLLOWBROOK COLLEGE — NIGHT CUSTODIAN PROGRAM", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 30), new Vector2(0, -30));
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

        public void Show(string label)
        {
            _label.text = label ?? "";
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(true);
        }

        public void Show() { _bg.SetActive(true); }
        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(false);
        }
    }
}
