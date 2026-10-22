using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.Checkpoint
{
    // Hover feedback for desk controls: while the pointer rests on the object, a hint line
    // elsewhere on the panel shows what it does. Pure presentation — the hint setter is
    // owned by the view, so wording lives with the rest of the desk copy.
    public class HoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private System.Action<string> _set;
        private string _hint;

        public void Init(string hint, System.Action<string> set)
        {
            _hint = hint;
            _set = set;
        }

        public void OnPointerEnter(PointerEventData e) => _set?.Invoke(_hint);
        public void OnPointerExit(PointerEventData e) => _set?.Invoke("");
    }
}
