using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.Checkpoint
{
    // Makes a desk paper (ID card, record file, stamp) a physical object: grab it anywhere,
    // drag it over the desk, drop it. While held the paper lifts (slight scale, straightened
    // tilt, top sibling) and settles back on drop. A drop OUTSIDE the desk frame returns the
    // paper to its home slot; a drop inside stays where the player put it (clamped). A tap
    // (barely any movement) fires OnTapped instead of OnDropped — that is the touch path for
    // "pick up" / "flip". Tools with ReturnsHome (stamps, the offered ID) always go back to
    // their tray/counter slot after every drop. BeginDrag/DragBy/PlaceAt/EndDrag are public
    // so the headless harness drives the same path the pointer handlers use.
    public class DragPaper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rt;
        private RectTransform _bounds;
        private float _tilt;
        private Vector2 _home;
        private Vector3 _baseScale = Vector3.one;
        private float _moved;
        private const float LiftScale = 1.04f;
        private const float TapThreshold = 10f;   // canvas units, not screen px

        public bool Dragging { get; private set; }
        /// <summary>The paper's resting display scale — Zoom rides ON TOP of it. The ID
        /// card ships at 0.82 (a slightly smaller default card); everything else is 1.
        /// localScale is always BaseScale * Zoom, so lift/drop/zoom maths is unchanged.</summary>
        public float BaseScale = 1f;
        /// <summary>Per-paper zoom (1..1.8), relative to BaseScale. Lift/drop preserve it;
        /// arrival resets it.</summary>
        public float Zoom
        {
            get => _rt != null ? _rt.localScale.x / Mathf.Max(0.01f, BaseScale) : 1f;
            set
            {
                if (_rt != null)
                {
                    var z = Mathf.Clamp(value, 1f, 1.8f) * Mathf.Max(0.01f, BaseScale);
                    _rt.localScale = new Vector3(z, z, 1f);
                }
            }
        }
        /// <summary>Tools (stamps, the offered ID) return to their slot after every drop;
        /// papers stay where the player left them.</summary>
        public bool ReturnsHome;
        /// <summary>Invoked with the desk-space drop centre after a real drag.</summary>
        public Action<Vector2> OnDropped;
        /// <summary>Invoked with the desk-space centre after a tap (movement < threshold).</summary>
        public Action<Vector2> OnTapped;
        /// <summary>Invoked when the paper is lifted — the view uses it to retarget flip/zoom.</summary>
        public Action OnLift;

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

        public void BeginDrag()
        {
            if (Dragging) return;
            Dragging = true;
            _moved = 0f;
            _baseScale = _rt.localScale;   // zoom level becomes the new base while lifted
            _rt.SetAsLastSibling();                       // papers stack: the held one is on top
            OnLift?.Invoke();
            _rt.localScale = _baseScale * LiftScale;
            _rt.localEulerAngles = Vector3.zero;          // straighten while held
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
