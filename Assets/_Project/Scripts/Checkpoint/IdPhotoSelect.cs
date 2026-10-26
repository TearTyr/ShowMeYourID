using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.Checkpoint
{
    // The ID card's face photograph as a click target: a clean click SELECTS the photo for the
    // face comparison, while a real drag is forwarded to the card's DragPaper once the pointer
    // has moved past a small threshold — so selecting never yanks the card and dragging still
    // works from anywhere on it. Created by CheckpointView's face-compare build; the selection
    // callback lives on the view (they know each other, so no bus traffic).
    public class IdPhotoSelect : MonoBehaviour, IPointerDownHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler
    {
        // screen px of movement before a hold on the photo counts as "dragging the card"
        private const float ForwardThreshold = 6f;

        /// <summary>Fired on a clean click (press + release without dragging).</summary>
        public Action OnSelected;

        private DragPaper _paper;
        private bool _forward;
        private float _moved;

        public void Init(DragPaper paper) { _paper = paper; }

        public void OnPointerDown(PointerEventData eventData) { _forward = false; _moved = 0f; }

        // Handling begin/drag/end HERE stops the event walking up to the card's DragPaper,
        // which is what keeps a selection click from lifting the card.
        public void OnBeginDrag(PointerEventData eventData) { _forward = false; _moved = 0f; }

        public void OnDrag(PointerEventData eventData)
        {
            _moved += eventData.delta.magnitude;
            if (!_forward && _moved > ForwardThreshold && _paper != null)
            {
                _forward = true;
                _paper.BeginDrag();      // it is a drag after all: hand the card over mid-gesture
            }
            if (_forward && _paper != null) _paper.DragBy(eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_forward) { if (_paper != null) _paper.EndDrag(); return; }
            OnSelected?.Invoke();        // a click: select the photo, card never moved
        }
    }
}
