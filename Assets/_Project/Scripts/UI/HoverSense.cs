using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.UI
{
    // Pointer enter/exit callbacks for one raycastable UI element (the speech bubble panel),
    // so a view can hold something on screen while the cursor reads it. Created by
    // CheckpointView.Speech beside the bubble; the callbacks are plain Actions because the
    // creator and the hovered widget already know each other.
    public class HoverSense : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnEnter;
        public Action OnExit;

        /// <summary>True between enter and exit - owners use it to ignore duplicate exits.</summary>
        public bool Hovering { get; private set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovering = true;
            OnEnter?.Invoke();                    // owners are idempotent; never swallow a re-enter
        }

        public void OnPointerExit(PointerEventData eventData) => Leave();

        private void Leave()
        {
            if (!Hovering) return;                // no duplicate / stale exits
            Hovering = false;
            OnExit?.Invoke();
        }
    }
}
