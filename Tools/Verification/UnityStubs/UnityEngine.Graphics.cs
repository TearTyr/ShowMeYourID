// Unity API stubs (graphics / audio / input) — see UnityEngine.Core.cs header.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public enum TextureFormat { Alpha8 = 1, RGB24 = 3, RGBA32 = 4, ARGB32 = 5, R8 = 63 }
    public enum FilterMode { Point = 0, Bilinear = 1, Trilinear = 2 }
    public enum TextureWrapMode { Repeat = 0, Clamp = 1 }

    public class Texture : Object
    {
        public int width { get; protected set; }
        public int height { get; protected set; }
        public FilterMode filterMode = FilterMode.Bilinear;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
    }

    public class Texture2D : Texture
    {
        private readonly Color[] _pixels;
        public Texture2D(int w, int h) : this(w, h, TextureFormat.RGBA32, false) { }
        public Texture2D(int w, int h, TextureFormat fmt, bool mipChain)
        {
            width = w; height = h;
            _pixels = new Color[w * h];
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = Color.clear;
        }
        public static Texture2D whiteTexture { get; } = new Texture2D(4, 4);
        public void SetPixel(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            _pixels[y * width + x] = c;
        }
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return Color.clear;
            return _pixels[y * width + x];
        }
        public void SetPixels(Color[] colors) { if (colors != null) Array.Copy(colors, _pixels, Math.Min(colors.Length, _pixels.Length)); }
        public void SetPixels32(Color32[] colors) { }
        public Color[] GetPixels() => _pixels;
        public void Apply(bool updateMipmaps = true, bool makeNoLongerReadable = false) { }
        public byte[] EncodeToPNG() => new byte[0];
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static implicit operator Color32(Color c) => new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
    }

    public enum SpriteMeshType { FullRect = 0, Tight = 1 }

    public class Sprite : Object
    {
        public Texture2D texture { get; private set; }
        public Rect rect { get; private set; }
        public Rect textureRect => rect;
        public Bounds bounds => new Bounds(Vector3.zero, new Vector3(rect.width, rect.height, 0));
        public float pixelsPerUnit { get; private set; } = 100f;

        public static Sprite Create(Texture2D tex, Rect rect, Vector2 pivot) =>
            Create(tex, rect, pivot, 100f);
        public static Sprite Create(Texture2D tex, Rect rect, Vector2 pivot, float pixelsPerUnit) =>
            new Sprite { texture = tex, rect = rect, pixelsPerUnit = pixelsPerUnit, name = tex != null ? tex.name : "sprite" };
        public static Sprite Create(Texture2D tex, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType) =>
            Create(tex, rect, pivot, pixelsPerUnit);
    }

    public struct Bounds
    {
        public Vector3 center, size, extents;
        public Bounds(Vector3 center, Vector3 size) { this.center = center; this.size = size; extents = size * 0.5f; }
    }

    public class Material : Object
    {
        public Shader shader;
        public Color color = Color.white;
        public Material(Shader s) { shader = s; }
        public void SetColor(string n, Color c) { color = c; }
        public void SetFloat(string n, float v) { }
        public bool HasProperty(string n) => false;
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => new Shader { name = name };
    }

    public class Renderer : Component
    {
        public Material material;
        public Material[] materials = new Material[0];
        public int sortingOrder = 0;
        public string sortingLayerName = "Default";
        public bool enabled = true;
        public Bounds bounds => new Bounds(Vector3.zero, Vector3.one);
    }

    public enum SpriteMaskInteraction { None = 0, VisibleInsideMask = 1, VisibleOutsideMask = 2 }

    public class SpriteMask : Behaviour
    {
        public Sprite sprite;
        public bool isCustomRangeActive;
        public int frontSortingOrder;
        public int backSortingOrder;
    }

    public class SpriteRenderer : Renderer
    {
        public Sprite sprite;
        public Color color = Color.white;
        public bool flipX, flipY;
        public DrawMode drawMode = DrawMode.Simple;
        public Vector2 size = Vector2.one;
        public enum DrawMode { Simple = 0, Sliced = 1, Tiled = 2 }
        public SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None;
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public class Camera : Behaviour
    {
        public static Camera main { get; set; }
        public bool orthographic = true;
        public float orthographicSize = 5f;
        public Color backgroundColor = Color.black;
        public CameraClearFlags clearFlags = CameraClearFlags.SolidColor;
        public int depth = 0;
        public float aspect = 16f / 9f;
        public float fieldOfView = 60f;
        public Rect rect = new Rect(0, 0, 1, 1);
        public Vector3 ScreenToWorldPoint(Vector3 p) => p;
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }

    // ---- audio ---------------------------------------------------------------
    public class AudioClip : Object
    {
        public int samples { get; private set; }
        public int channels { get; private set; }
        public int frequency { get; private set; }
        public float length => frequency > 0 ? (float)samples / frequency : 0f;
        private float[] _data = new float[0];

        public static AudioClip Create(string clipName, int lengthSamples, int channels, int frequency, bool stream) =>
            new AudioClip { name = clipName, samples = lengthSamples, channels = channels, frequency = frequency, _data = new float[lengthSamples * channels] };

        public bool SetData(float[] data, int offsetSamples)
        {
            if (data == null) return false;
            Array.Copy(data, 0, _data, 0, Math.Min(data.Length, _data.Length));
            return true;
        }
        public bool GetData(float[] data, int offsetSamples) { Array.Copy(_data, data, Math.Min(data.Length, _data.Length)); return true; }
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
        public bool playOnAwake = true;
        public bool mute = false;
        public float spatialBlend = 0f;
        public bool isPlaying { get; private set; }
        public float time = 0f;
        public double dspTime = 0;
        public void Play() { isPlaying = clip != null; }
        public void PlayDelayed(float d) { isPlaying = clip != null; }
        public void PlayOneShot(AudioClip c) { }
        public void PlayOneShot(AudioClip c, float v) { }
        public void Stop() { isPlaying = false; }
        public void Pause() { isPlaying = false; }
        public void UnPause() { isPlaying = clip != null; }
    }

    public class AudioListener : Behaviour
    {
        public static float volume = 1f;
    }

    // ---- input ---------------------------------------------------------------
    public enum KeyCode
    {
        None = 0, Backspace = 8, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1 = 49, Alpha2 = 50, Alpha3 = 51, Alpha4 = 52,
        Alpha5 = 53, Alpha6 = 54, Alpha7 = 55, Alpha8 = 56, Alpha9 = 57,
        A = 97, B = 98, C = 99, D = 100, E = 101, F = 102, G = 103, H = 104, I = 105,
        J = 106, K = 107, L = 108, M = 109, N = 110, O = 111, P = 112, Q = 113, R = 114,
        S = 115, T = 116, U = 117, V = 118, W = 119, X = 120, Y = 121, Z = 122,
        LeftShift = 304, RightShift = 303, LeftControl = 305, RightControl = 306,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        F1 = 282, F2 = 283, F3 = 284,
    }

    public static class Input
    {
        // stub state: tests/tools can set these to simulate input
        public static Vector3 mousePosition = Vector3.zero;
        public static bool mousePresent = true;
        public static HashSet<KeyCode> KeysDown = new HashSet<KeyCode>();
        public static HashSet<KeyCode> KeysHeld = new HashSet<KeyCode>();
        public static HashSet<int> MouseDown = new HashSet<int>();
        public static float AxisRawHorizontal = 0f;
        public static float AxisRawVertical = 0f;

        public static bool GetKeyDown(KeyCode k) => KeysDown.Contains(k);
        public static bool GetKeyUp(KeyCode k) => false;
        public static bool GetKey(KeyCode k) => KeysHeld.Contains(k);
        public static bool GetMouseButtonDown(int b) => MouseDown.Contains(b);
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static float GetAxisRaw(string axis) =>
            axis == "Horizontal" ? AxisRawHorizontal : (axis == "Vertical" ? AxisRawVertical : 0f);
        public static float GetAxis(string axis) => GetAxisRaw(axis);
        public static bool anyKeyDown => KeysDown.Count > 0;
    }
}
