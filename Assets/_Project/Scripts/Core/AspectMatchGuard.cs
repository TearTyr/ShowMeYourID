using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // CanvasScaler lives in UnityEngine.UI (com.unity.ugui), not UnityEngine

namespace Hollowbrook.Core
{
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

        public Vector2 VisibleReferenceSize()
        {
            float scale = _scaler != null && _scaler.matchWidthOrHeight > 0.5f
                ? _h / 720f
                : _w / 1280f;
            if (scale <= 0f) scale = 1f;
            return new Vector2(_w / scale, _h / scale);
        }
    }
}
