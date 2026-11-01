using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.Checkpoint
{
    // One box on the night security log. Left click counts up, right click counts down. It only
    // records: nothing in the game reads these numbers and nothing is scored from them.
    public class LogBox : MonoBehaviour, IPointerClickHandler
    {
        private Action<int> _change;

        public void Init(Action<int> change) { _change = change; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_change == null) return;
            if (eventData.button == PointerEventData.InputButton.Left) _change(+1);
            else if (eventData.button == PointerEventData.InputButton.Right) _change(-1);
        }
    }
}
