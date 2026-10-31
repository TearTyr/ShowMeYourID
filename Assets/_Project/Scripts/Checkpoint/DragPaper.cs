using System;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.Checkpoint
{
    // Makes a desk paper (ID card, record file, stamp) a physical object: grab it anywhere,
    // drag it over the desk, drop it. While held the paper lifts (slight scale, straightened
    // tilt, top sibling) and settles back on drop. A drop OUTSIDE the desk frame returns the
    // paper to its home slot; a drop inside stays where the player put it (clamped). A tap
    // (a click, or barely any movement) fires OnTapped instead of OnDropped - that is the
    // path for "pick up" / "inspect". An Inspectable paper (the ID) is lifted by a tap into the
    // inspect view - centred, untilted, enlarged, over a dimmer - and a click anywhere off it
    // puts it back where it lay. Tools with ReturnsHome (stamps, the offered ID) always go back
    // to their tray/counter slot after every drop. BeginDrag/DragBy/PlaceAt/EndDrag/Tap are
    // public so the headless harness drives the same path the pointer handlers use.
    public class DragPaper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private RectTransform _rt;
        private RectTransform _bounds;
        private float _tilt;
        private Vector2 _home;
        private Vector3 _baseScale = Vector3.one;
        private float _moved;
        private Vector2 _preInspectPos;
        private Vector3 _preInspectScale = Vector3.one;
        private RectTransform _catcher;   // the full-layer click-away dimmer behind an inspected paper
        private const float LiftScale = 1.04f;
        private const float InspectDim = 0.45f;
        private const float TapThreshold = 10f;   // canvas units, not screen px

        public bool Dragging { get; private set; }
        /// <summary>Per-paper flag: a click on the resting paper lifts it into the inspect view
        /// (see Inspect). The ID is the only inspectable paper; the slip and the record form
        /// are read where they lie, so they leave this off.</summary>
        public bool Inspectable;
        /// <summary>How much the paper grows in the inspect view (it is also untilted and centred).</summary>
        public float InspectScale = 2f;
        /// <summary>True while the paper is lifted in the inspect view.</summary>
        public bool Inspecting { get; private set; }
        /// <summary>Tools (stamps, the offered ID) return to their slot after every drop;
        /// papers stay where the player left them.</summary>
        public bool ReturnsHome;
        /// <summary>Invoked the moment the paper is picked up (a drag starts).</summary>
        public Action OnPickedUp;
        /// <summary>A paper that stays bright above the dimmer while this one is inspected
        /// (the student file, so it can be read next to the enlarged ID).</summary>
        public Transform KeepVisible;
        /// <summary>Invoked with the desk-space drop centre after a real drag.</summary>
        public Action<Vector2> OnDropped;
        /// <summary>Invoked with the desk-space centre after a tap (movement < threshold).</summary>
        public Action<Vector2> OnTapped;

        public void Init(RectTransform bounds, float tiltDegrees)
        {
            _rt = GetComponent<RectTransform>();
            _bounds = bounds;
            _tilt = tiltDegrees;
            _home = _rt.anchoredPosition;
            _baseScale = _rt.localScale;
            _rt.localEulerAngles = new Vector3(0f, 0f, _tilt);
        }

        public void OnBeginDrag(PointerEventData e) => BeginDrag();
        public void OnDrag(PointerEventData e) => DragBy(e.delta);
        public void OnEndDrag(PointerEventData e) => EndDrag();
        // a plain click (no drag) is a tap: uGUI starts a drag only after the pointer moves
        // past its threshold, so without this a still click would never reach OnTapped
        public void OnPointerClick(PointerEventData e) { if (!e.dragging) Tap(); }

        /// <summary>The tap path: invoke OnTapped with the paper's centre. Ignored mid-drag.</summary>
        public void Tap()
        {
            if (Dragging) return;
            OnTapped?.Invoke(CenterInBounds());
        }

        public void BeginDrag()
        {
            if (Dragging || Inspecting) return;   // an inspected paper is put back by clicking off it
            Dragging = true;
            _moved = 0f;
            _baseScale = _rt.localScale;   // the current scale is the base the lift grows from
            _rt.SetAsLastSibling();                       // papers stack: the held one is on top
            _rt.localScale = _baseScale * LiftScale;
            _rt.localEulerAngles = Vector3.zero;          // straighten while held
            OnPickedUp?.Invoke();
        }

        // pointer deltas arrive in screen pixels; the desk lives in canvas-scaled units
        private float ScaleFactor
        {
            get
            {
                if (_bounds == null) return 1f;
                var c = _bounds.GetComponentInParent<Canvas>();
                var r = c != null ? c.rootCanvas : null;
                return r != null && r.scaleFactor > 0.01f ? r.scaleFactor : 1f;
            }
        }

        public void DragBy(Vector2 delta)
        {
            if (!Dragging) return;
            _rt.anchoredPosition += delta / ScaleFactor;  // 1:1 with the cursor at any resolution
            _moved += (Mathf.Abs(delta.x) + Mathf.Abs(delta.y)) / ScaleFactor;
        }

        public void EndDrag()
        {
            if (!Dragging) return;
            Dragging = false;
            Vector2 dropCenter = CenterInBounds();        // where the player let go
            _rt.localScale = _baseScale;
            _rt.localEulerAngles = new Vector3(0f, 0f, _tilt);   // settle back
            bool tapped = _moved < TapThreshold;
            bool home = ReturnsHome || !InsideDesk(dropCenter);
            if (home) _rt.anchoredPosition = _home;       // tools return; off-desk drops return
            else Clamp();
            if (tapped) OnTapped?.Invoke(dropCenter);
            else OnDropped?.Invoke(dropCenter);
        }

        // ---- inspect view ----

        /// <summary>Lift the paper into the inspect view: centred on the desk, straight, enlarged,
        /// over a dimmer whose click puts it back. No-op unless Inspectable.</summary>
        public void Inspect()
        {
            if (!Inspectable || Inspecting || Dragging || _bounds == null) return;
            Inspecting = true;
            _preInspectPos = _rt.anchoredPosition;
            _preInspectScale = _rt.localScale;
            ShowCatcher();
            if (KeepVisible != null && KeepVisible.gameObject.activeInHierarchy && KeepVisible.parent == _rt.parent)
                KeepVisible.SetAsLastSibling();           // the file stays readable beside the big ID
            _rt.SetAsLastSibling();                       // above its own dimmer
            _rt.localScale = _preInspectScale * InspectScale;
            _rt.localEulerAngles = Vector3.zero;          // straight, to be read
            var size = ScaledSize();
            var b = _bounds.rect.size;
            _rt.anchoredPosition = new Vector2((b.x - size.x) / 2f, -(b.y - size.y) / 2f);
        }

        /// <summary>Put an inspected paper back exactly where, how big and at what tilt it lay.</summary>
        public void EndInspect()
        {
            if (!Inspecting) return;
            Inspecting = false;
            if (_catcher != null) _catcher.gameObject.SetActive(false);
            _rt.localScale = _preInspectScale;
            _rt.localEulerAngles = new Vector3(0f, 0f, _tilt);
            _rt.anchoredPosition = _preInspectPos;
        }

        // a hidden / handed-back paper must never leave the dimmer up
        private void OnDisable() => EndInspect();

        private void ShowCatcher()
        {
            if (_catcher == null)
            {
                _catcher = UIFactory.NewUI(_rt.parent, "inspectcatcher_" + gameObject.name);
                UIFactory.AnchorsFull(_catcher);
                var dim = UIFactory.Img(_catcher, "dim", null, new Color(0f, 0f, 0f, InspectDim), true);
                UIFactory.AnchorsFull(dim.rectTransform);
                var click = _catcher.gameObject.AddComponent<Button>();
                click.targetGraphic = dim;
                click.transition = Selectable.Transition.None;
                click.onClick.AddListener(EndInspect);
            }
            _catcher.SetAsLastSibling();
            _catcher.gameObject.SetActive(true);
        }

        /// <summary>Re-home a paper after it changes state (counter -> desk slot).</summary>
        public void SetHome(Vector2 deskTopLeft)
        {
            _home = new Vector2(deskTopLeft.x, -deskTopLeft.y);
        }

        // Headless-friendly teleport (tests + drop positioning); counts as a drag, not a tap.
        public void PlaceAt(Vector2 deskTopLeft)
        {
            _moved = 999f;
            _rt.anchoredPosition = new Vector2(deskTopLeft.x, -deskTopLeft.y);
            Clamp();
        }

        // Headless-friendly placement by centre point.
        public void PlaceCenter(Vector2 deskCenter)
        {
            var s = ScaledSize();
            PlaceAt(new Vector2(deskCenter.x - s.x / 2f, deskCenter.y - s.y / 2f));
        }

        public Vector2 CenterInBounds()
        {
            var p = _rt.anchoredPosition;
            var s = ScaledSize();
            return new Vector2(p.x + s.x / 2f, -p.y + s.y / 2f);
        }

        private Vector2 ScaledSize()
        {
            var r = _rt.rect.size;
            return new Vector2(r.x * _rt.localScale.x, r.y * _rt.localScale.y);
        }

        private bool InsideDesk(Vector2 deskCenter)
        {
            if (_bounds == null) return true;
            var b = _bounds.rect.size;
            return deskCenter.x >= 0f && deskCenter.x <= b.x && deskCenter.y >= 0f && deskCenter.y <= b.y;
        }

        // Papers use top-left anchors/pivot inside the desk frame, so the allowed
        // anchoredPosition box keeps the whole (scaled) paper inside the desk.
        private void Clamp()
        {
            if (_bounds == null || _rt == null) return;
            var size = ScaledSize();
            float maxX = Mathf.Max(0f, _bounds.rect.width - size.x);
            float maxY = Mathf.Max(0f, _bounds.rect.height - size.y);
            var p = _rt.anchoredPosition;
            p.x = Mathf.Clamp(p.x, 0f, maxX);
            p.y = Mathf.Clamp(p.y, -maxY, 0f);
            _rt.anchoredPosition = p;
        }
    }
}
