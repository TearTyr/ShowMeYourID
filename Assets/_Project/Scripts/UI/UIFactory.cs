using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Builds every button/label/panel so the whole game shares one look.
    public static class UIFactory
    {
        public static readonly Color PanelBase = new Color(0.09f, 0.09f, 0.10f);
        // ---- night-shift desk palette (from the approved HTML mockup) ----
        public static readonly Color NsInk = new Color(0.047f, 0.047f, 0.047f);      // #0c0c0c
        public static readonly Color NsDesk = new Color(0.086f, 0.086f, 0.086f);     // #161616
        public static readonly Color NsPaper = new Color(0.925f, 0.925f, 0.925f);    // #ececec
        public static readonly Color NsChalk = new Color(1f, 1f, 1f);                // #ffffff
        public static readonly Color NsMid = new Color(0.541f, 0.541f, 0.541f);      // #8a8a8a

        public static readonly Color PanelAlt = NsDesk;
        public static readonly Color Border = NsInk;
        public static readonly Color Ink = NsChalk;
        public static readonly Color InkDim = NsMid;
        public static readonly Color InkFaint = new Color(0.42f, 0.42f, 0.42f);
        public static readonly Color Accent = new Color(0.70f, 0.13f, 0.17f);   // restrained red
        public static readonly Color AccentBright = new Color(0.90f, 0.22f, 0.26f);
        public static readonly Color Paper = NsPaper;    // documents
        public static readonly Color PaperInk = NsInk;
        public static readonly Color Glass = new Color(0.75f, 0.78f, 0.80f, 0.10f);

        public const int TypeSection = 14;
        public const int SpaceUnit = 8;

        // ---- mockup type roles (bundled open-licence fonts, safe fallback to GameFont) ----
        private static Font _uiFont, _uiFontBold, _typeFont, _handFont;
        // The owner's SecurityRoom kit ships its own type families (Bahnschrift for the UI,
        // Chiller for the horror display face). They are tried FIRST, with the bundled open
        // licence families as fallbacks, so a missing kit file can never leave text unrendered.
        public static Font UiFont => LoadFont(ref _uiFont,
            "Fonts/kit_bahnschrift_regular", "Fonts/BarlowCondensed-Regular");
        public static Font UiFontBold => LoadFont(ref _uiFontBold,
            "Fonts/kit_bahnschrift_bold", "Fonts/BarlowCondensed-SemiBold");
        public static Font TypeFont => LoadFont(ref _typeFont,
            "Fonts/kit_chiller", "Fonts/SpecialElite-Regular");
        public static Font HandFont => LoadFont(ref _handFont,
            "Fonts/CaveatBrush-Regular", "Fonts/CaveatBrush-Regular");

        private static Font LoadFont(ref Font f, string path, string fallbackPath)
        {
            if (f != null) return f;
            try { f = Resources.Load<Font>(path); } catch (Exception) { }
            if (f == null) try { f = Resources.Load<Font>(fallbackPath); } catch (Exception) { }
            if (f == null) f = GameFont;   // headless / pre-import: text must never break
            return f;
        }

        private static Font _font;
        private static readonly List<Text> _texts = new List<Text>();
        private static readonly Dictionary<Text, int> _baseSizes = new Dictionary<Text, int>();
        private static float _textScale = 1f;

        public static Font GameFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
                if (_font == null) Debug.LogError("[UIFactory] No built-in font available — text will not render.");
                return _font;
            }
        }

        public static Canvas CreateRootCanvas(string name, int sortingOrder, int refW, int refH)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;  // explicit: AspectMatchGuard drives matchWidthOrHeight
            scaler.referenceResolution = new Vector2(refW, refH);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform NewUI(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>How much reference-space this element's canvas actually shows: finds the
        /// canvas' AspectMatchGuard (the thing that decides which axis is matched) and asks it.
        /// Matching the width leaves 1280 across and lets the height grow; matching the height
        /// leaves 720 down and lets the width grow. Anything that must stay centred AND fully
        /// visible sizes itself from this instead of assuming 1280x720. Falls back to the raw
        /// screen size when there is no canvas/guard (headless harnesses, pre-import).</summary>
        public static Vector2 VisibleReferenceSize(RectTransform rt)
        {
            CanvasScaler scaler = null;
            if (rt != null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                if (root != null) scaler = root.GetComponent<CanvasScaler>();
            }
            var guard = scaler != null ? scaler.GetComponent<Core.AspectMatchGuard>() : null;
            if (guard != null) return guard.VisibleReferenceSize();
            return Core.AspectMatchGuard.VisibleReferenceSize(scaler, Screen.width, Screen.height);
        }

        public static void AnchorsFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Stretch an existing GameObject across its parent (adding the RectTransform when
        // the GO does not carry one yet) — for full-screen layers built outside the factory.
        public static RectTransform NewRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            AnchorsFull(rt);
            return rt;
        }

        // A plain stretched Image on an existing GameObject (transition fades, dimmers).
        public static Image FullImage(GameObject go, Color c)
        {
            var img = go.AddComponent<Image>();
            img.color = c;
            NewRect(go);
            return img;
        }

        public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // The one placement rule for the desk grid: top-left anchor, pivot (0,1), y down.
        // Every desk/HUD element is placed only through this, so (x,y,w,h) IS the on-screen
        // rect inside the fixed frame — no anchor/pivot maths left to drift.
        public static void PlaceTL(RectTransform rt, float x, float y, float w, float h)
        {
            var tl = new Vector2(0f, 1f);
            rt.anchorMin = tl;
            rt.anchorMax = tl;
            rt.pivot = tl;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        // Fixed-size frame centred in the parent; grid children never touch the screen edges.
        public static RectTransform FixedFrame(Transform parent, string name, float w, float h)
        {
            var rt = NewUI(parent, name);
            var c = new Vector2(0.5f, 0.5f);
            rt.anchorMin = c; rt.anchorMax = c; rt.pivot = c;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        // Text that can never spill outside (x,y,w,h): a RectMask2D viewport clips it, and with
        // scroll=true overflowing content can be dragged (principal note, log overlays).
        public static Text MaskedLabel(RectTransform parent, string name, float x, float y, float w, float h,
            int fontSize, Color color, TextAnchor align = TextAnchor.UpperLeft, bool scroll = false)
        {
            var host = NewUI(parent, name);
            PlaceTL(host, x, y, w, h);
            host.gameObject.AddComponent<RectMask2D>();
            var t = Label(host, "text", "", fontSize, color, align);
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (!scroll)
            {
                StretchToHost(t.rectTransform);
                return t;
            }
            MakeScrollable(host, t, h);
            return t;
        }

        // Text fills the host rect exactly; the RectMask2D clips any visual spill.
        private static void StretchToHost(RectTransform trt)
        {
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        }

        // Turns the masked host into a drag-scrollable viewport: an invisible catcher image
        // receives drags, the ScrollRect moves the text, and the ContentSizeFitter lets the
        // text grow past the viewport so there is something to scroll.
        private static void MakeScrollable(RectTransform host, Text t, float viewportH)
        {
            var catcher = host.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;                       // drag source for the ScrollRect
            var sr = host.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            var trt = t.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            trt.sizeDelta = new Vector2(0f, viewportH);
            t.raycastTarget = true;
            var fitter = trt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = trt;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var rt = NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return img;
        }

        // A filled panel with a drawn border. Returns the OUTER rect: callers anchor and
        // show/hide the outer, and the fill stretches inside it automatically. (Returning the
        // inner once left a full-screen border rect behind when callers hid only the inner.)
        public static RectTransform BorderedPanel(Transform parent, string name, Color fill, Color border, int thickness = 2)
        {
            var outer = Panel(parent, name + "_border", border).rectTransform;
            var inner = Panel(outer, name, fill).rectTransform;
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(thickness, thickness);
            inner.offsetMax = new Vector2(-thickness, -thickness);
            return outer;
        }

        // Records a new base size for the text-scale setting. Use this instead of assigning
        // .fontSize directly, otherwise ApplyTextScale overwrites the manual size later.
        public static void SetBaseFontSize(Text text, int size)
        {
            if (text == null) return;
            _baseSizes[text] = size;
            text.fontSize = Mathf.RoundToInt(size * _textScale);
        }

        public static Text Label(Transform parent, string name, string text, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = NewUI(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GameFont;
            t.text = text ?? "";
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            _baseSizes[t] = fontSize;
            t.fontSize = Mathf.RoundToInt(fontSize * _textScale);
            _texts.Add(t);
            return t;
        }

        public static void ApplyTextScale(float multiplier)
        {
            _textScale = multiplier;
            PruneDestroyedTexts();
            foreach (var t in _texts)
                if (_baseSizes.TryGetValue(t, out int baseSize))
                    t.fontSize = Mathf.RoundToInt(baseSize * multiplier);
        }

        /// <summary>The current text-scale multiplier, for the views that re-fit themselves to
        /// the screen and therefore compute their own font sizes (the ground-floor map sheet).
        /// Such a view keeps its DESIGN size as the recorded base, so ApplyTextScale and a
        /// relayout multiply independently instead of compounding.</summary>
        public static float TextScale => _textScale;

        /// <summary>Record a label's design size once (Label already records the size it was
        /// built with; use this when the intended base differs from the first rendered size).</summary>
        public static void RememberBaseSize(Text text, int designSize)
        {
            if (text == null) return;
            if (!_baseSizes.ContainsKey(text)) _baseSizes[text] = designSize;
        }

        public static void PruneDestroyedTexts()
        {
            for (int i = _texts.Count - 1; i >= 0; i--)
                if (_texts[i] == null || _texts[i].gameObject == null) _texts.RemoveAt(i);
        }

        public static Button StandardButton(Transform parent, string name, string label, Action onClick,
            int width = 260, int height = 46, bool danger = false)
        {
            var rt = NewUI(parent, name);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            var border = rt.gameObject.AddComponent<Image>();
            border.color = danger ? Accent : Border;
            var innerRt = NewUI(rt, "bg");
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2); innerRt.offsetMax = new Vector2(-2, -2);
            var inner = innerRt.gameObject.AddComponent<Image>();
            inner.color = danger ? new Color(0.16f, 0.05f, 0.06f) : PanelAlt;

            var txt = Label(innerRt, "label", label, 20, danger ? AccentBright : Ink, TextAnchor.MiddleCenter);
            Anchor(txt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(width - 12, height - 8), Vector2.zero);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = inner;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Button StandardButtonLeft(Transform parent, string name, string label, Action onClick,
            int width = 360, int height = 46, bool danger = false)
        {
            var b = StandardButton(parent, name, label, onClick, width, height, danger);
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null)
            {
                var rt = lbl.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(width - 44, height - 8);
                rt.anchoredPosition = new Vector2(22 + (width - 44) / 2f, 0);
                lbl.alignment = TextAnchor.MiddleLeft;
            }
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.45f, 1.45f, 1.55f);   // subtle brighten on hover/focus
            cb.pressedColor = new Color(1.9f, 1.9f, 2.0f);
            cb.selectedColor = new Color(1.25f, 1.25f, 1.3f);
            cb.disabledColor = new Color(0.55f, 0.55f, 0.58f);
            cb.fadeDuration = 0.12f;
            b.colors = cb;
            return b;
        }

        public static void SetButtonEnabled(Button b, bool on)
        {
            if (b == null) return;
            b.interactable = on;
            // Dim the whole button instead of repainting its fill: round decision buttons carry
            // custom paper/ink colours that a generic disabled colour would destroy.
            var cg = b.GetComponent<CanvasGroup>();
            if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = on ? 1f : 0.45f;
        }

        public static Slider SliderRow(Transform parent, string name, string labelText, float value,
            Action<float> onChanged, int width = 420)
        {
            // One settings row: [label | track with fill+handle | % value], wired to a Slider.
            var row = NewUI(parent, name);
            row.sizeDelta = new Vector2(width, 40);

            var rowBg = Panel(row, "rowbg", new Color(0f, 0f, 0f, 0f));
            AnchorsFull(rowBg.rectTransform);
            rowBg.raycastTarget = true;

            float labW = width * 0.36f;
            var lab = Label(row, "label", labelText, 17, Ink);
            Anchor(lab.rectTransform, new Vector2(0f, 0.5f), new Vector2(labW, 30), new Vector2(labW / 2f, 0));
            lab.alignment = TextAnchor.MiddleLeft;
            lab.raycastTarget = false;

            float trackX = width * 0.38f;
            float trackW = width * 0.50f;
            var track = Panel(row, "track", PanelAlt).rectTransform;
            Anchor(track, new Vector2(0f, 0.5f), new Vector2(trackW, 8), new Vector2(trackX + trackW / 2f, 0));

            var fillArea = NewUI(track, "fillarea");
            fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
            var fill = Panel(fillArea, "fill", Ink).rectTransform;
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.sizeDelta = new Vector2(0, 0);
            fill.anchoredPosition = Vector2.zero;

            var handleArea = NewUI(track, "handlearea");
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = Vector2.zero; handleArea.offsetMax = Vector2.zero;
            var handleImg = Panel(handleArea, "handle", InkDim);
            var handle = handleImg.rectTransform;
            Anchor(handle, new Vector2(0.5f, 0.5f), new Vector2(14, 22), Vector2.zero);

            float valW = width * 0.12f;
            var val = Label(row, "value", Percent(value), 15, InkDim, TextAnchor.MiddleRight);
            Anchor(val.rectTransform, new Vector2(0f, 0.5f), new Vector2(valW, 26),
                new Vector2(trackX + trackW + valW / 2f, 0));
            val.raycastTarget = false;

            var slider = row.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.value = value;
            slider.targetGraphic = handleImg;
            slider.onValueChanged.AddListener(v =>
            {
                if (val != null) val.text = Percent(v);
                if (onChanged != null) onChanged(v);
            });

            AddHover(row.gameObject,
                () => { if (handleImg != null) handleImg.color = Ink; if (lab != null) lab.color = Ink; },
                () => { if (handleImg != null) handleImg.color = InkDim; if (lab != null) lab.color = Ink; });
            return slider;
        }

        private static string Percent(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

        public static void AddHover(GameObject go, UnityEngine.Events.UnityAction enter,
            UnityEngine.Events.UnityAction exit)
        {
            if (go == null) return;
            var et = go.AddComponent<EventTrigger>();
            var on = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            on.callback.AddListener(_ => { if (enter != null) enter(); });
            et.triggers.Add(on);
            var off = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            off.callback.AddListener(_ => { if (exit != null) exit(); });
            et.triggers.Add(off);
        }

        public static VerticalLayoutGroup VStack(Transform parent, string name, int spacing,
            TextAnchor align = TextAnchor.UpperCenter, bool controlW = true, bool controlH = true,
            bool forceW = false, bool forceH = false)
        {
            var rt = NewUI(parent, name);
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = controlW;
            g.childControlHeight = controlH;
            g.childForceExpandWidth = forceW;
            g.childForceExpandHeight = forceH;
            return g;
        }

        public static LayoutElement Layout(RectTransform rt, float prefW = -1, float prefH = -1,
            float minW = -1, float minH = -1)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.minWidth = minW; le.minHeight = minH;
            return le;
        }

        public static RectTransform LeftSheet(Transform parent, string title, string subtitle,
            out RectTransform contentColumn)
        {
            Dimmer(parent, "dim", 0.82f);
            var panel = Panel(parent, "sheet", PanelBase);
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.sizeDelta = new Vector2(640f, 0f);
            prt.anchoredPosition = Vector2.zero;

            var edge = Panel(prt, "edge", Border);
            var ert = edge.rectTransform;
            ert.anchorMin = new Vector2(1f, 0f); ert.anchorMax = new Vector2(1f, 1f);
            ert.offsetMin = new Vector2(-2f, 0f); ert.offsetMax = Vector2.zero;

            var t = Label(prt, "title", title, 30, Ink, TextAnchor.UpperLeft);
            Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(560f, 40f), new Vector2(320f, -52f));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var st = Label(prt, "subtitle", subtitle, 13, InkFaint, TextAnchor.UpperLeft);
                Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(560f, 20f), new Vector2(320f, -88f));
            }

            var col = VStack(prt, "rows", 10, TextAnchor.UpperLeft, false, false);
            Anchor(col.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(560f, 470f),
                new Vector2(320f, -120f - 235f));
            contentColumn = col.GetComponent<RectTransform>();
            return prt;
        }

        public static Button SheetButton(RectTransform sheet, string name, string label, Action onClick,
            float centerX, bool danger = false, int width = 200)
        {
            var b = StandardButton(sheet, name, label, onClick, width, 42, danger);
            Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(width, 42),
                new Vector2(centerX, 40f));
            return b;
        }

        // The rect a preserveAspect Image ACTUALLY paints inside its box: the sprite letterboxes
        // while the Image's rect covers the whole box, so anything that must line up with the
        // VISIBLE pixels (a centred portrait, the ID photo in its slot) needs this letterbox.
        // Pure float maths (top-left inside the box), unit-testable.
        public struct Box
        {
            public float X, Y, W, H;
            public Box(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
        }

        public static Box Letterbox(float boxW, float boxH, float spriteW, float spriteH)
        {
            if (boxW <= 0f || boxH <= 0f || spriteW <= 0f || spriteH <= 0f)
                return new Box(0f, 0f, Mathf.Max(0f, boxW), Mathf.Max(0f, boxH));
            float spriteAspect = spriteW / spriteH;
            float w, h;
            if (spriteAspect > boxW / boxH) { w = boxW; h = boxW / spriteAspect; }
            else { h = boxH; w = boxH * spriteAspect; }
            return new Box((boxW - w) / 2f, (boxH - h) / 2f, w, h);
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color? color = null, bool raycast = false)
        {
            var rt = NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = raycast;
            return img;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            return g;
        }

        public static Image Dimmer(Transform parent, string name, float alpha)
        {
            var img = Panel(parent, name, new Color(0, 0, 0, alpha));
            AnchorsFull(img.rectTransform);
            return img;
        }
    }
}
