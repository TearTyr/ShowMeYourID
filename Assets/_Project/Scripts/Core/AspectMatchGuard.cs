using System.Collections;
using UnityEngine;
using UnityEngine.UI;   // CanvasScaler lives in UnityEngine.UI (com.unity.ugui), not UnityEngine

namespace Hollowbrook.Core
{
    /// <summary>Keeps every canvas at or above the 1280×720 reference rectangle on ANY
    /// aspect ratio, which is what guarantees "no overlap / no clipping / no shrinking":
    ///  • wider than 16:9  => match height (extra width becomes negative space);
    ///  • narrower than 16:9 => match width (extra height becomes negative space).
    /// Attached automatically to every canvas by CoreBootstrap.</summary>
    public class AspectMatchGuard : MonoBehaviour
    {
        private CanvasScaler _scaler;
        private int _w, _h;

        /// <summary>Test hook: headless suites enumerate live guards.</summary>
        public static readonly System.Collections.Generic.List<AspectMatchGuard> Instances =
            new System.Collections.Generic.List<AspectMatchGuard>();

        public const float ReferenceAspect = 1280f / 720f;

        public void Init(CanvasScaler scaler)
        {
            _scaler = scaler;
            Instances.Add(this);
            Apply();
        }

        private void OnDestroy() => Instances.Remove(this);

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

        /// <summary>Visible reference-space size under the current match setting.</summary>
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
