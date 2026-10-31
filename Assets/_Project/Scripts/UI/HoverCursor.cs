using Hollowbrook.Bootstrap;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.UI
{
    // The hand cursor for the desk's "clickable slivers" (the ID's black spine): swaps the OS
    // cursor for the procedural `cursor_hand` while the pointer is over the handle and puts the
    // arrow back on exit, disable or destroy, so a torn-down desk can never strand the hand.
    // Added by CheckpointView.Papers' SpineHandle beside the spine's Button.
    public class HoverCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // The fingertip pixel of cursor_hand (canvas row 0 is the TOP) - Unity's hotspot is
        // measured from the texture's top-left, so this is the point that "clicks".
        private static readonly Vector2 Hotspot = new Vector2(8f, 2f);

        private bool _hovering;

        public void OnPointerEnter(PointerEventData eventData) { _hovering = true; Apply(); }

        public void OnPointerExit(PointerEventData eventData) { _hovering = false; Apply(); }

        private void OnDisable() { _hovering = false; Restore(); }

        private void OnDestroy() => Restore();

        private void Apply()
        {
            if (!_hovering) { Restore(); return; }
            var sprite = ProceduralArt.Get("cursor_hand");
            var tex = sprite != null ? sprite.texture : null;
            if (tex == null) { Restore(); return; }   // no art: keep the OS arrow
            Cursor.SetCursor(tex, Hotspot, CursorMode.Auto);
        }

        private static void Restore() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
