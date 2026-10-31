// Unity API stubs (uGUI subset) - see UnityEngine.Core.cs header.
// Namespace placement mirrors real Unity 2022.3:
//   UnityEngine    : Canvas, CanvasScaler, CanvasGroup, RectTransform, Font, TextAnchor, …
//   UnityEngine.UI : Graphic, Image, Text, Button, Slider, Toggle, layouts, Outline, GraphicRaycaster, …
using System;
using UnityEngine.Events;

namespace UnityEngine
{
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    public class Canvas : Behaviour
    {
        public Canvas rootCanvas => this;
        public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
        public Camera worldCamera;
        public int sortingOrder = 0;
        public float scaleFactor = 1f;      // stub: harnesses run at reference resolution
        public float referencePixelsPerUnit = 100f;
        public float planeDistance = 100f;
    }

    public class CanvasGroup : Behaviour
    {
        public float alpha = 1f;
        public bool interactable = true;
        public bool blocksRaycasts = true;
        public bool ignoreParentGroups = false;
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
        public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = Vector2.zero;
        // Unity's stretch drivers: the rect edges hang off the anchor points, and
        // anchoredPosition/sizeDelta are the derived pair. Modelling them (instead of four
        // independent fields) keeps code that writes offsets - DialogueView, FileDrawer edges,
        // paper stripes - in the same geometry as code that writes anchoredPosition.
        public Vector2 offsetMin
        {
            get => anchoredPosition - new Vector2(pivot.x * sizeDelta.x, pivot.y * sizeDelta.y);
            set
            {
                var max = offsetMax;
                sizeDelta = max - value;
                anchoredPosition = new Vector2(value.x + pivot.x * sizeDelta.x,
                                               value.y + pivot.y * sizeDelta.y);
            }
        }
        public Vector2 offsetMax
        {
            get => anchoredPosition + new Vector2((1f - pivot.x) * sizeDelta.x,
                                                  (1f - pivot.y) * sizeDelta.y);
            set
            {
                var min = offsetMin;
                sizeDelta = value - min;
                anchoredPosition = new Vector2(min.x + pivot.x * sizeDelta.x,
                                               min.y + pivot.y * sizeDelta.y);
            }
        }
        public new Rect rect => new Rect(anchoredPosition.x, anchoredPosition.y, sizeDelta.x, sizeDelta.y);
        public void SetSizeWithCurrentAnchors(Axis axis, float size) { }
        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size) { }
        public enum Axis { Horizontal = 0, Vertical = 1 }
        public enum Edge { Left, Right, Top, Bottom }
        public void GetWorldCorners(Vector3[] fourCornersArray) { }
    }

    public class Font : Object
    {
        public int fontSize = 16;
        public static Font CreateDynamicFontFromOSFont(string name, int size) => new Font { name = name };
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rt, Vector2 sp, Camera cam, out Vector2 local)
        {
            local = new Vector2(sp.x - rt.rect.width * 0.5f, sp.y - rt.rect.height * 0.5f);
            return true;
        }
    }
}

namespace UnityEngine.UI
{
    public class CanvasScaler : Behaviour
    {
        public ScaleMode uiScaleMode = ScaleMode.ConstantPixelSize;
        public Vector2 referenceResolution = new Vector2(800, 600);
        [Range(0, 1)] public float matchWidthOrHeight = 0f;
        public float referencePixelsPerUnit = 100f;
        public float scaleFactor = 100f;
        public enum ScaleMode { ConstantPixelSize = 0, ScaleWithScreenSize = 1, ConstantPhysicalSize = 2 }
        public enum ScreenMatchMode { MatchWidthOrHeight = 0, Expand = 1, Shrink = 2 }
        public ScreenMatchMode screenMatchMode = ScreenMatchMode.MatchWidthOrHeight;
    }

    public class GraphicRaycaster : Behaviour { }

    // Minimal headless InputField: the text plus the events real uGUI fires.
    // Headless proofs set .text and invoke onEndEdit / SimulateEnter directly.
    // Line semantics mirror real uGUI and the project's input contract:
    //   MultiLineSubmit - Enter submits (onSubmit), Shift+Enter inserts a newline;
    //   MultiLineNewline - Enter inserts a newline, Shift+Enter submits;
    //   SingleLine - Enter submits, newlines are refused.
    public class InputField : Behaviour
    {
        private string _text = "";
        // Assigning mirrors real uGUI: the visible ink and the placeholder's enabled state
        // follow the string, so headless dumps render exactly what the editor would show.
        public string text
        {
            get => _text;
            set
            {
                _text = value ?? "";
                if (characterLimit > 0 && _text.Length > characterLimit) _text = _text.Substring(0, characterLimit);
                if (textComponent != null) textComponent.text = _text;
                if (placeholder != null) placeholder.enabled = _text.Length == 0;
            }
        }
        public bool isFocused;
        public bool interactable = true;
        public int characterLimit = 0;
        public LineType lineType = LineType.SingleLine;
        public Graphic targetGraphic;
        public Text textComponent;
        public Graphic placeholder;
        public UnityEngine.Events.UnityEvent<string> onEndEdit = new UnityEngine.Events.UnityEvent<string>();
        public UnityEngine.Events.UnityEvent<string> onValueChanged = new UnityEngine.Events.UnityEvent<string>();
        public UnityEngine.Events.UnityEvent<string> onSubmit = new UnityEngine.Events.UnityEvent<string>();
        public enum LineType { SingleLine = 0, MultiLineNewline = 1, MultiLineSubmit = 2 }

        // ActivateInputField mirrors real uGUI - and the stub deliberately exposes ONLY the
        // real surface (uGUI's InputField has no Focus(); an invented member here once let a
        // stub-only call compile headless and then fail in the editor). The field becomes the
        // EventSystem's selected object, so UIFactory.TypingInField() sees it exactly as the
        // editor would.
        public void ActivateInputField() { isFocused = true; Select(); }
        public void DeactivateInputField()
        {
            isFocused = false;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.currentSelectedGameObject == gameObject) es.SetSelectedGameObject(null);
            onEndEdit.Invoke(text);
        }
        private void Select()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null) es.SetSelectedGameObject(gameObject);
        }

        /// <summary>Headless stand-in for pressing Return in the field: applies the line-type
        /// semantics above (submit vs. newline) exactly as real uGUI would.</summary>
        public void SimulateEnter(bool shift)
        {
            if (!interactable) return;
            bool submit = lineType == LineType.MultiLineNewline ? shift     // Enter newline, Shift+Enter submit
                        : lineType == LineType.MultiLineSubmit ? !shift     // Enter submit, Shift+Enter newline
                        : true;                                             // single line: Enter submits
            if (submit) { onSubmit.Invoke(text); return; }
            Append("\n");
        }

        /// <summary>Headless stand-in for typing a string into the field.</summary>
        public void SimulateTyping(string s)
        {
            if (!interactable) return;
            Append(s);
        }

        private void Append(string s)
        {
            string next = text + s;
            if (characterLimit > 0 && next.Length > characterLimit) next = next.Substring(0, characterLimit);
            if (lineType == LineType.SingleLine) next = next.Replace("\n", " ");
            text = next;
            if (textComponent != null) textComponent.text = next;
            onValueChanged.Invoke(text);
        }
    }
    public class Mask : Behaviour { public bool showMaskGraphic = true; }
    public class RectMask2D : Behaviour { }

    public class Graphic : Behaviour
    {
        public Color color = Color.white;
        public bool raycastTarget = true;
        public Material material;
        public int depth = 0;
        public Canvas canvas => null;
        public RectTransform rectTransform => transform as RectTransform;
        public void SetAllDirty() { }
        public void SetLayoutDirty() { }
        public void SetVerticesDirty() { }
        public void SetMaterialDirty() { }
        public void CrossFadeAlpha(float a, float d, bool ig) { }
    }

    public class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public Sprite sprite;
        public Type type = Type.Simple;
        [Range(0, 1)] public float fillAmount = 1f;
        public bool preserveAspect = false;
        public bool fillCenter = true;
        public FillMethod fillMethod = FillMethod.Horizontal;
        public int fillOrigin = 0;
        public Sprite overrideSprite { get; set; }
        public float pixelsPerUnitMultiplier = 1f;
        public float pixelsPerUnit => sprite != null ? sprite.pixelsPerUnit / 100f : 1f;   // canvas reference PPU 100
        public void SetNativeSize() { }
        // uGUI asks every Graphic under the pointer whether the hit counts; the real
        // Image tests the sprite rect, HotRectImage narrows that to one local band
        public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => raycastTarget;
        public enum Type { Simple = 0, Sliced = 1, Filled = 2, Tiled = 3 }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture;
        public Rect uvRect = new Rect(0, 0, 1, 1);
    }

    public class Text : MaskableGraphic
    {
        public string text = "";
        public Font font;
        public int fontSize = 14;
        public FontStyle fontStyle = FontStyle.Normal;
        public TextAnchor alignment = TextAnchor.UpperLeft;
        public HorizontalWrapMode horizontalOverflow = HorizontalWrapMode.Wrap;
        public VerticalWrapMode verticalOverflow = VerticalWrapMode.Truncate;
        public float lineSpacing = 1f;
        public bool supportRichText = true;
        public bool resizeTextForBestFit = false;
        public int resizeTextMinSize = 11;
        public int resizeTextMaxSize = 100;
        public int preferredWidth => 0;
        public int preferredHeight => 0;
    }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
        public static ColorBlock defaultColorBlock => new ColorBlock
        {
            normalColor = new Color(1f, 1f, 1f, 1f),
            highlightedColor = new Color(1f, 1f, 1f, 1f),
            pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f),
            selectedColor = new Color(1f, 1f, 1f, 1f),
            disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };
    }

    public class Selectable : Behaviour
    {
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
        public Transition transition = Transition.ColorTint;
        public bool interactable = true;
        public Graphic targetGraphic;
        public Navigation navigation = default;
        public ColorBlock colors = ColorBlock.defaultColorBlock;
        public struct Navigation { }
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
        public class ButtonClickedEvent : UnityEvent { }
    }

    public class Toggle : Selectable
    {
        public bool isOn = true;
        public ToggleEvent onValueChanged = new ToggleEvent();
        public class ToggleEvent : UnityEvent<bool> { }
        public Graphic graphic;
    }

    public class Slider : Selectable
    {
        public float minValue = 0f;
        public float maxValue = 1f;
        public bool wholeNumbers = false;
        public float value = 0f;
        public SliderEvent onValueChanged = new SliderEvent();
        public class SliderEvent : UnityEvent<float> { }
        public RectTransform fillRect;
        public RectTransform handleRect;
    }

    public class ScrollRect : Behaviour
    {
        public RectTransform content;
        public bool horizontal = true;
        public bool vertical = true;
        public float verticalNormalizedPosition = 1f;
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public MovementType movementType = MovementType.Clamped;
        public float scrollSensitivity = 24f;
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding = new RectOffset(0, 0, 0, 0);
        public TextAnchor childAlignment = TextAnchor.UpperLeft;
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing = 0f;
        public bool childControlWidth = true;
        public bool childControlHeight = true;
        public bool childForceExpandWidth = true;
        public bool childForceExpandHeight = true;
        public bool childScaleWidth = false;
        public bool childScaleHeight = false;
        public bool reverseArrangement = false;
    }

    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class GridLayoutGroup : LayoutGroup
    {
        public Vector2 cellSize = new Vector2(100, 100);
        public Vector2 spacing = Vector2.zero;
    }

    public class LayoutElement : Behaviour
    {
        public bool ignoreLayout = false;
        public float minWidth = -1, minHeight = -1;
        public float preferredWidth = -1, preferredHeight = -1;
        public float flexibleWidth = -1, flexibleHeight = -1;
        public int layoutPriority = 1;
    }

    public class ContentSizeFitter : Behaviour
    {
        public FitMode horizontalFit = FitMode.Unconstrained;
        public FitMode verticalFit = FitMode.Unconstrained;
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }

    public class VertexHelper : IDisposable
    {
        public void Dispose() { }
        public void Clear() { }
    }

    public enum CanvasUpdate { Prelayout, Layout, PostLayout, PreRender, LatePreRender, MaxUpdateValue }

    public interface ICanvasElement
    {
        void Rebuild(CanvasUpdate update);
        Transform transform { get; }
        void LayoutComplete();
        void GraphicUpdateComplete();
    }

    public abstract class BaseMeshEffect : Behaviour, ICanvasElement
    {
        public abstract void ModifyMesh(VertexHelper vh);
        public virtual void Rebuild(CanvasUpdate update) { }
        public virtual void LayoutComplete() { }
        public virtual void GraphicUpdateComplete() { }
    }

    public abstract class Shadow : BaseMeshEffect
    {
        public Color effectColor = new Color(0f, 0f, 0f, 0.5f);
        public Vector2 effectDistance = new Vector2(1f, -1f);
        public bool useGraphicAlpha = true;
        public override void ModifyMesh(VertexHelper vh) { }
    }

    public class Outline : Shadow
    {
        public override void ModifyMesh(VertexHelper vh) { }
    }
}

namespace UnityEngine.EventSystems
{
    public abstract class BaseInputModule : Behaviour { }

    public class StandaloneInputModule : BaseInputModule
    {
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public string submitButton = "Submit";
        public string cancelButton = "Cancel";
        public float inputActionsPerSecond = 10f;
        public float repeatDelay = 0.5f;
    }

    public class BaseEventData { }

    // Pointer payload + the handler interfaces real uGUI dispatches to. The stubs never
    // dispatch pointer events themselves; headless proofs call the handler methods or the
    // components' public entry points directly.
    public class PointerEventData : BaseEventData
    {
        public Vector2 position = Vector2.zero;
        public Vector2 delta = Vector2.zero;
        public int pointerId = -1;
        public bool dragging;
    }

    public interface IPointerEnterHandler { void OnPointerEnter(PointerEventData eventData); }
    public interface IPointerExitHandler { void OnPointerExit(PointerEventData eventData); }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData eventData); }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData eventData); }
    public interface IBeginDragHandler { void OnBeginDrag(PointerEventData eventData); }
    public interface IDragHandler { void OnDrag(PointerEventData eventData); }
    public interface IEndDragHandler { void OnEndDrag(PointerEventData eventData); }

    public enum EventTriggerType
    {
        PointerEnter = 0, PointerExit = 1, PointerDown = 2, PointerUp = 3, PointerClick = 4,
        InitializePotentialDrag = 5, BeginDrag = 6, Drag = 7, EndDrag = 8, Drop = 9,
        Scroll = 10, UpdateSelected = 11, Select = 12, Deselect = 13, Move = 14,
        Submit = 15, Cancel = 16,
    }

    public class EventTrigger : Behaviour
    {
        public class TriggerEvent : UnityEngine.Events.UnityEvent<BaseEventData> { }
        public class Entry
        {
            public EventTriggerType eventID;
            public TriggerEvent callback = new TriggerEvent();
        }
        public System.Collections.Generic.List<Entry> triggers = new System.Collections.Generic.List<Entry>();
    }

    public class EventSystem : Behaviour
    {
        // real Unity assigns this on Awake; the stub assigns on construction so headless
        // harnesses that build a bootstrap (CoreBootstrap) get a live EventSystem for free.
        public EventSystem() { current = this; }
        public static EventSystem current { get; set; }
        public GameObject currentSelectedGameObject;
        public bool sendNavigationEvents = true;
        public void SetSelectedGameObject(GameObject go) { currentSelectedGameObject = go; }
    }
}
