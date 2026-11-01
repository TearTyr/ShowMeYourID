using Hollowbrook.Logic;
using Hollowbrook.Scenes;
using UnityEngine;

namespace Hollowbrook.World
{
    // The dark over the hallway while the flashlight is on. It is one picture the size of the
    // screen that is painted again every frame: as dark as the room is everywhere, and completely
    // clear inside the beam. So the beam really clears the area and the room's own art shows.
    // (A Unity sprite mask used to do this and did not show up in the game; plain pixels cannot fail.)
    // While it is on, the room's own dark layers are switched off (LightController.SetBeamCover),
    // so the dark is never counted twice.
    public class BeamCover : MonoBehaviour
    {
        private const int Cols = 192, Rows = 108;   // about 0.1 world units per pixel; the soft edge comes from smoothing
        private const float Ppu = 100f;
        private const float SideSoftness = 0.6f;    // how far in from the beam's side the light fades in
        private const float FarSoftness = 1.0f;     // how far before the beam's end it fades out
        private const float Margin = 1f;            // extra world units around the screen

        private Texture2D _tex;
        private Color[] _px;
        private SpriteRenderer _sr;
        private LightController _light;
        private Vector2 _origin, _dir;
        private float _range, _halfTan;

        public int SortingOrder => _sr != null ? _sr.sortingOrder : 0;

        public void Init()
        {
            _tex = new Texture2D(Cols, Rows, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
            _px = new Color[Cols * Rows];
            _sr = gameObject.GetOrAdd<SpriteRenderer>();
            _sr.sprite = Sprite.Create(_tex, new Rect(0, 0, Cols, Rows), new Vector2(0.5f, 0.5f), Ppu);
            _sr.sortingOrder = WorldLayers.Darkness;   // where the room's own dark layers draw; the player (73) stays above
        }

        /// <summary>Paint the dark for this frame. The beam is the cone from origin along dir.</summary>
        public void Draw(Camera cam, LightController light, Vector3 origin, Vector3 dir, float range, float halfTan)
        {
            if (_tex == null || cam == null || light == null) return;
            _light = light;
            _origin = new Vector2(origin.x, origin.y);
            _dir = new Vector2(dir.x, dir.y);
            _range = range;
            _halfTan = halfTan;

            float worldH = cam.orthographicSize * 2f + Margin * 2f;
            float worldW = worldH * cam.aspect + Margin * 2f;
            var centre = cam.transform.position;
            transform.localPosition = new Vector3(centre.x, centre.y, 0f);
            transform.localScale = new Vector3(worldW / (Cols / Ppu), worldH / (Rows / Ppu), 1f);

            for (int j = 0; j < Rows; j++)
            {
                float y = centre.y + ((j + 0.5f) / Rows - 0.5f) * worldH;
                for (int i = 0; i < Cols; i++)
                {
                    float x = centre.x + ((i + 0.5f) / Cols - 0.5f) * worldW;
                    _px[j * Cols + i] = new Color(0f, 0f, 0f, DarkAt(x, y));
                }
            }
            _tex.SetPixels(_px);
            _tex.Apply(false, false);
        }

        /// <summary>How dark one point of the room is with the beam on: the room's dark, taken away by the beam.</summary>
        public float DarkAt(float x, float y)
        {
            if (_light == null) return 0f;
            return _light.DarkAlphaAt(x, y) * (1f - BeamAt(x, y));
        }

        // 1 inside the beam, 0 outside, smooth at the sides and at the far end.
        private float BeamAt(float x, float y)
        {
            float rx = x - _origin.x, ry = y - _origin.y;
            float along = rx * _dir.x + ry * _dir.y;
            if (along <= 0f || along >= _range) return 0f;
            float across = Mathf.Abs(rx * _dir.y - ry * _dir.x);
            float inside = (along * _halfTan - across) / Mathf.Sqrt(1f + _halfTan * _halfTan);   // distance in from the side
            if (inside <= 0f) return 0f;
            return Smooth(inside / SideSoftness) * Smooth((_range - along) / FarSoftness);
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void OnDestroy()
        {
            if (_sr != null && _sr.sprite != null) GameCore.Kill(_sr.sprite);
            if (_tex != null) GameCore.Kill(_tex);
        }
    }
}
