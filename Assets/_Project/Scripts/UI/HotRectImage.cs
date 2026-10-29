using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    /// <summary>
    /// An Image whose pointer raycast only validates inside ONE local rectangle, so a big
    /// sheet of paper can be a drag surface by its header band while the body stays
    /// click-through (the widgets under the sheet keep working). The hot rect uses the
    /// desk layout's convention: units of the rect's own size, x from the left edge and
    /// y DOWN from the top edge. An empty rect means "the whole image", i.e. plain Image.
    /// </summary>
    public class HotRectImage : Image
    {
        [SerializeField] private Rect _hotRectTopLeft = new Rect(0f, 0f, 0f, 0f);

        /// <summary>Hit area in rect units, y down from the top-left corner.</summary>
        public Rect HotRectTopLeft
        {
            get => _hotRectTopLeft;
            set => _hotRectTopLeft = value;
        }

        public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.IsRaycastLocationValid(screenPoint, eventCamera)) return false;
            if (_hotRectTopLeft.width <= 0f || _hotRectTopLeft.height <= 0f) return true;
            var rt = rectTransform;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out local))
                return true;
            // local is pivot-relative; the hot rect speaks top-left-relative, y down
            float x = local.x + rt.pivot.x * rt.rect.width;
            float y = rt.pivot.y * rt.rect.height - local.y;
            return x >= _hotRectTopLeft.x && x <= _hotRectTopLeft.x + _hotRectTopLeft.width
                && y >= _hotRectTopLeft.y && y <= _hotRectTopLeft.y + _hotRectTopLeft.height;
        }
    }
}
