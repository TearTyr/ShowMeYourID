using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.UI
{
    // One raycastable UI element that reports plain clicks (the student portrait at the window).
    // Created by CheckpointView's face-compare build; the callback is a plain Action because the
    // creator and the clicked widget already know each other. Duplicate clicks are harmless: the
    // owner's handler is a single delegate assigned once at build time.
    public class ClickSense : MonoBehaviour, IPointerClickHandler
    {
        public Action OnClick;

        public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke();
    }
}
