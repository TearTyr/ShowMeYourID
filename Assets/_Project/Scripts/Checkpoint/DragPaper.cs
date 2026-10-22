using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.Checkpoint
{
    // Makes a desk paper (ID card, record sheet) a physical object: grab it anywhere,
    // drag it over the desk, drop it. While held the paper lifts (slight scale, straightened
    // tilt, top sibling) and settles back on drop. Clamped to the desk frame so papers can
    // never leave the desk or block the top bar. Drop actions (e.g. drop-on-scanner) ride
    // the OnDropped callback so the view keeps all rules; this component is pure movement.
    // BeginDrag/DragBy/EndDrag are public so the headless harness can drive the same path
    // the pointer handlers use (the stubs do not simulate pointers).
    public class DragPaper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rt;
        private RectTransform _bounds;
        private Vector2 _home;
        private float _tilt;
        private Vector3 _baseScale = Vector3.one;
        private const float LiftScale = 1.04f;

        public bool Dragging { get; private set; }
        /// <summary>Tools (stamps) return to their tray slot after every drop; papers stay
        /// where the player left them.</summary>
        public bool ReturnsHome;
        /// <summary>Invoked with the paper's desk-space centre on drop; the view decides
        /// whether a drop zone (scanner) was hit.</summary>
        public Action<Vector2> OnDropped;

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
            _rt.SetAsLastSibling();                       // lift above the other papers
            _rt.localScale = _baseScale * LiftScale;
            _rt.localEulerAngles = Vector3.zero;          // straighten while held
        }

        public void DragBy(Vector2 delta)
        {
            if (!Dragging) return;
            _rt.anchoredPosition += delta;
            Clamp();
        }

        public void EndDrag()
        {
            if (!Dragging) return;
            Dragging = false;
            Vector2 dropCenter = CenterInBounds();              // where the player let go
            _rt.localScale = _baseScale;
            _rt.localEulerAngles = new Vector3(0f, 0f, _tilt);   // settle back
            if (ReturnsHome) _rt.anchoredPosition = _home;      // tools go back to the tray
            Clamp();
            OnDropped?.Invoke(dropCenter);
        }

        // Headless-friendly teleport (tests + future "tidy desk" action).
        public void PlaceAt(Vector2 deskTopLeft)
        {
            _rt.anchoredPosition = new Vector2(deskTopLeft.x, -deskTopLeft.y);
            Clamp();
        }

        public Vector2 CenterInBounds()
        {
            var p = _rt.anchoredPosition;
            var s = _rt.rect.size;
            return new Vector2(p.x + s.x / 2f, -p.y + s.y / 2f);
        }

        // Papers use top-left anchors/pivot inside the desk frame, so the allowed
        // anchoredPosition box is [0, W-w] x [-(H-h), 0].
        private void Clamp()
        {
            if (_bounds == null || _rt == null) return;
            var size = _rt.rect.size;
            float maxX = Mathf.Max(0f, _bounds.rect.width - size.x);
            float maxY = Mathf.Max(0f, _bounds.rect.height - size.y);
            var p = _rt.anchoredPosition;
            p.x = Mathf.Clamp(p.x, 0f, maxX);
            p.y = Mathf.Clamp(p.y, -maxY, 0f);
            _rt.anchoredPosition = p;
        }
    }
}
