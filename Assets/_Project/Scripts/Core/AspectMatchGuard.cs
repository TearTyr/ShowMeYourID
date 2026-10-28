using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // CanvasScaler lives in UnityEngine.UI (com.unity.ugui), not UnityEngine

namespace Hollowbrook.Core
{
    // Keeps every canvas at or above the 1280x720 reference on any aspect.
    public class AspectMatchGuard : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private int _w, _h;

        public const float ReferenceAspect = 1280f / 720f;

        public void Init(CanvasScaler scaler)
        {
            _scaler = scaler;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Apply();
        }

        public void Apply()
        {
            _w = Screen.width; _h = Screen.height;
            if (_scaler == null || _h <= 0) return;
            float aspect = _w / (float)_h;
            _scaler.matchWidthOrHeight = aspect > ReferenceAspect ? 1f : 0f;
        }

        /// <summary>How much reference space the screen actually shows right now. Matching the
        /// width leaves 1280 across and lets the height grow; matching the height leaves 720 down
        /// and lets the width grow. Anything that must stay centred AND fully visible sizes
        /// itself from this instead of assuming 1280x720 (the ground-floor map sheet).</summary>
        public Vector2 VisibleReferenceSize() => VisibleReferenceSize(_scaler, _w, _h);

        /// <summary>The same rule for callers that found their own CanvasScaler.</summary>
        public static Vector2 VisibleReferenceSize(CanvasScaler scaler, int screenWidth, int screenHeight)
        {
            int w = screenWidth > 0 ? screenWidth : Screen.width;
            int h = screenHeight > 0 ? screenHeight : Screen.height;
            if (scaler == null ||
                scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f ||
                w <= 0 || h <= 0)
                return new Vector2(w > 0 ? w : 1280f, h > 0 ? h : 720f);
            float scale = scaler.matchWidthOrHeight > 0.5f
                ? scaler.referenceResolution.y / h
                : scaler.referenceResolution.x / w;
            if (scale <= 0f) scale = 1f;
            // scale is reference-units-per-screen-pixel (refY/h when the height is matched,
            // refX/w when the width is), so the visible reference area is the screen size
            // TIMES it. Dividing (the old bug) squared the error: 1920x1080 reported 2880x1620
            // instead of the 1280x720 the scaler actually shows, and 4:3 under-reported.
            return new Vector2(w * scale, h * scale);
        }
    }
}
