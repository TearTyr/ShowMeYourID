// ---------------------------------------------------------------------------
// Unity API stubs for headless compilation & logic testing of Show Me Your ID!
// NOT part of the Unity project (lives outside Assets/, never imported).
// Signatures mirror the real UnityEngine 2022.3 API for the members used by
// the game code. Minimal behavior is implemented where headless tests need it
// (scene graph, PlayerPrefs, Application paths, math).
// ---------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    // ---- core object model -------------------------------------------------
    public class Object
    {
        public string name = "";
        internal bool destroyed;
        // Set on stand-ins fabricated by Resources.Load: they represent IMPORTED ASSETS on
        // disk. The real engine refuses to Destroy those (editor and builds alike) and logs
        // "Destroying assets is not permitted to avoid data loss." - the stub mirrors that
        // guard so asset-destroy bugs surface headlessly instead of only in the owner's
        // editor console.
        internal bool isPersistentAsset;
        public int GetInstanceID() => GetHashCode();
        public static void Destroy(Object obj)
        {
            // headless stub: deactivates + detaches so scene swaps isolate in tests
            if (ReferenceEquals(obj, null) || obj.destroyed) return;
            if (obj.isPersistentAsset)
            {
                Debug.LogError("Destroying assets is not permitted to avoid data loss.\n" +
                               "If you really want to remove an asset use DestroyImmediate (theObject, true);");
                return;
            }
            DestroyCore(obj);
        }
        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets = false)
        {
            if (ReferenceEquals(obj, null) || obj.destroyed) return;
            if (obj.isPersistentAsset && !allowDestroyingAssets)
            {
                Debug.LogError("Destroying assets is not permitted to avoid data loss.\n" +
                               "If you really want to remove an asset use DestroyImmediate (theObject, true);");
                return;
            }
            DestroyCore(obj);
        }
        private static void DestroyCore(Object obj)
        {
            obj.destroyed = true;
            if (obj is GameObject go)
            {
                go.SetActive(false);
                if (go.transform.parent != null) go.transform.SetParent(null);
            }
            else if (obj is Component c && c.gameObject != null && !c.gameObject.destroyed)
            {
                if (c is Behaviour b) b.enabled = false;
            }
        }
        public static void DontDestroyOnLoad(Object obj) { }
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null) && !o.destroyed;
        public override string ToString() => name;
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform => gameObject != null ? gameObject.transform : null;
        public T GetComponent<T>() where T : class => gameObject != null ? gameObject.GetComponent<T>() : null;
        public T GetComponentInChildren<T>(bool includeInactive = false) where T : class =>
            gameObject != null ? gameObject.GetComponentInChildrenHelper<T>(includeInactive, true) : null;
        public Component GetComponent(Type t) => gameObject != null ? gameObject.GetComponent(t) : null;
        public bool TryGetComponent<T>(out T c) where T : class { c = GetComponent<T>(); return c != null; }
        public T GetComponentInParent<T>() where T : class
        {
            Transform t = transform;
            while (t != null)
            {
                var c = t.GetComponent<T>();
                if (c != null) return c;
                t = t.parent;
            }
            return null;
        }
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeInHierarchy;
    }

    public class Coroutine { }
    public class YieldInstruction { }
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
    public class WaitForSecondsRealtime : YieldInstruction { public WaitForSecondsRealtime(float s) { } }

    public class MonoBehaviour : Behaviour
    {
        /// <summary>HEADLESS STUB BEHAVIOR: runs the enumerator synchronously (yielded
        /// instructions complete instantly) with an iteration guard, so game flow
        /// (transitions, banners, typewriters) executes in smoke tests. Real Unity paces
        /// these over frames - engine code is written for real semantics.</summary>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            RunSync(routine);
            return new Coroutine();
        }

        private static void RunSync(IEnumerator e)
        {
            if (e == null) return;
            int guard = 0;
            while (true)
            {
                if (guard++ > 20000)
                {
                    Debug.LogWarning("[stub] coroutine iteration guard hit (infinite loop truncated)");
                    break;
                }
                bool more;
                try { more = e.MoveNext(); }
                catch (Exception ex) { Debug.LogError("[stub] coroutine exception: " + ex); break; }
                if (!more) break;
            }
        }

        public void StartCoroutine_Auto() { }
        public void StopCoroutine(Coroutine c) { }
        public void StopAllCoroutines() { }
        public bool useGUILayout = false;
        public void Invoke(string m, float t) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string methodName) { }
        public void InvokeRepeating(string m, float delay, float rate) { }
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
        public static ScriptableObject CreateInstance(Type t) => (ScriptableObject)Activator.CreateInstance(t);
    }

    public sealed class GameObject : Object
    {
        private readonly List<Component> _components = new List<Component>();
        public Transform transform { get; private set; }
        public bool activeSelf { get; private set; } = true;
        public GameObject parent => transform != null && transform.parent != null ? transform.parent.gameObject : null;
        public bool activeInHierarchy
        {
            get
            {
                var g = this;
                while (g != null)
                {
                    if (!g.activeSelf) return false;
                    g = g.parent;
                }
                return true;
            }
        }
        public int layer = 0;
        public string tag = "Untagged";
        public Scene scene => default;

        public GameObject(string name, params Type[] components) : this(name)
        {
            foreach (var t in components) AddComponent(t);
        }

        public GameObject() : this("GameObject") { }
        public GameObject(string name)
        {
            this.name = name;
            transform = new Transform { gameObject = this };
            _components.Add(transform);
        }

        public T AddComponent<T>() where T : Component
        {
            // adding RectTransform where one already exists returns it (Unity converts in place)
            if (typeof(T) == typeof(RectTransform) && transform is RectTransform existingRt)
                return (T)(Component)existingRt;
            var c = (T)Activator.CreateInstance(typeof(T));
            AddComponentInternal(c);
            return c;
        }

        public Component AddComponent(Type t)
        {
            if (t == typeof(RectTransform) && transform is RectTransform existingRt) return existingRt;
            var c = (Component)Activator.CreateInstance(t);
            AddComponentInternal(c);
            return c;
        }

        private void AddComponentInternal(Component c)
        {
            c.gameObject = this;
            if (c is RectTransform rt)
            {
                // RectTransform REPLACES the plain Transform, exactly like Unity UI conversion
                if (transform != null && !(transform is RectTransform)) _components.Remove(transform);
                transform = rt;
                if (!_components.Contains(rt)) _components.Add(rt);
                return;
            }
            if (c is Transform) throw new InvalidOperationException("Cannot add Transform");
            _components.Add(c);
        }

        public T GetComponent<T>() where T : class
        {
            foreach (var c in _components) if (c is T t) return t;
            return null;
        }

        public Component GetComponent(Type t)
        {
            foreach (var c in _components) if (t.IsInstanceOfType(c)) return c;
            return null;
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive = false) where T : class
        {
            var list = new List<T>();
            Collect(this, list, includeInactive);
            return list.ToArray();
        }

        internal T GetComponentInChildrenHelper<T>(bool includeInactive, bool includeSelf) where T : class
        {
            var list = new List<T>();
            Collect(this, list, includeInactive);
            return list.Count > 0 ? list[0] : null;
        }

        private static void Collect<T>(GameObject g, List<T> list, bool includeInactive) where T : class
        {
            if (!includeInactive && !g.activeSelf) return;
            foreach (var c in g._components) if (c is T t) list.Add(t);
            foreach (var child in g.transform.Children()) Collect(child.gameObject, list, includeInactive);
        }

        public void SetActive(bool v) { activeSelf = v; }
        public static GameObject Find(string n) => null; // gameplay code must not use this
    }

    public struct Scene { public string name; public bool isLoaded; }

    // scene management stub: tests install SceneLoadHook to simulate SceneManager.LoadScene
    public static class SceneLoadBridge { public static System.Action<string> SceneLoadHook; }

    public class Transform : Component, IEnumerable
    {
        private readonly List<Transform> _children = new List<Transform>();
        public Vector3 position = Vector3.zero;
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localScale = Vector3.one;
        public Quaternion rotation = Quaternion.identity;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 localEulerAngles = Vector3.zero;
        public Transform parent { get; private set; }
        public string name => gameObject != null ? gameObject.name : "";
        public int childCount => _children.Count;
        public Vector3 right = new Vector3(1, 0, 0);
        public Vector3 up = new Vector3(0, 1, 0);
        public Vector3 forward = new Vector3(0, 0, 1);
        public Rect rect => new Rect(0, 0, 0, 0); // RectTransform overrides

        public void SetParent(Transform p, bool worldPositionStays = true)
        {
            if (parent != null) parent._children.Remove(this);
            parent = p;
            if (p != null) p._children.Add(this);
        }
        public Transform GetChild(int i) => _children[i];
        public Transform Find(string n)
        {
            // real Unity supports slash paths ("a/b/c")
            if (n != null && n.IndexOf('/') >= 0)
            {
                Transform cur = this;
                foreach (var part in n.Split('/'))
                {
                    if (cur == null) return null;
                    cur = cur.Find(part);
                }
                return cur;
            }
            foreach (var c in _children) if (c.gameObject.name == n) return c;
            return null;
        }
        public int GetSiblingIndex() => parent != null ? parent.Children().IndexOf(this) : 0;
        public void SetSiblingIndex(int index)
        {
            if (parent == null) return;
            var kids = parent.Children();
            kids.Remove(this);
            if (index < 0) index = kids.Count + 1 + index;
            if (index > kids.Count) index = kids.Count;
            kids.Insert(index, this);
        }
        public void SetAsLastSibling()
        {
            if (parent == null) return;
            parent._children.Remove(this);
            parent._children.Add(this);
        }
        public void SetAsFirstSibling() { }
        public void DetachChildren() { _children.Clear(); }
        internal List<Transform> Children() => _children;
        public IEnumerator GetEnumerator() => _children.GetEnumerator();
    }

    // ---- attributes ----------------------------------------------------------
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SpaceAttribute : Attribute { public SpaceAttribute(float h = 0) { } }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }

    public enum RuntimeInitializeLoadType { AfterSceneLoad = 0, BeforeSceneLoad = 1, AfterAssembliesLoaded = 2, BeforeSplashScreen = 3, SubsystemRegistration = 4 }
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }

    // ---- engine statics ------------------------------------------------------
    public static class Debug
    {
        public static bool isDebugBuild = true;
        // total LogError/LogException calls so far - proofs diff this around risky
        // operations (e.g. teardown) to assert the engine logged nothing
        public static int ErrorCount { get; private set; }
        public static void Log(object m) => Console.WriteLine("[LOG] " + m);
        public static void LogWarning(object m) => Console.WriteLine("[WARN] " + m);
        public static void LogError(object m) { ErrorCount++; Console.Error.WriteLine("[ERR] " + m); }
        public static void LogException(Exception e) { ErrorCount++; Console.Error.WriteLine("[EXC] " + e); }
        public static void Assert(bool cond) { if (!cond) LogError("Assertion failed"); }
    }

    public enum RuntimePlatform { WindowsPlayer, WindowsEditor, LinuxPlayer, OSXPlayer }

    public static class Application
    {
        // settable in stubs so headless tests can redirect save paths
        public static string persistentDataPath { get; set; } = "/tmp/smyid-persistent";
        public static string dataPath { get; set; } = "/tmp/smyid-data";
        public static bool isEditor = false;
        public static RuntimePlatform platform = RuntimePlatform.LinuxPlayer;
        public static int targetFrameRate = 60;
        public static bool runInBackground = true;
        public static string productName = "Show Me Your ID!";
        public static string version = "1.0.0";
        public static bool absoluteURL = false;
        public static void Quit() { }
        public static void Quit(int code) { }
    }

    public static class Time
    {
        public static float time { get; set; }
        public static float deltaTime { get; set; } = 1f / 60f;
        public static float unscaledDeltaTime { get; set; } = 1f / 60f;
        public static float realtimeSinceStartup { get; set; }
        public static float unscaledTime { get; set; }
        public static float timeScale { get; set; } = 1f;
        public static int frameCount { get; set; }
    }

    public enum FullScreenMode { ExclusiveFullScreen = 0, FullScreenWindow = 1, MaximizedWindow = 2, Windowed = 3 }

    public struct Resolution
    {
        public int width;
        public int height;
        public int refreshRate;
    }

    public static class Screen
    {
        public static int width { get; set; } = 1280;
        public static int height { get; set; } = 720;
        public static float dpi => 96f;
        public static FullScreenMode fullScreenMode { get; set; } = FullScreenMode.FullScreenWindow;
        public static Resolution[] resolutions { get; set; } = new Resolution[0];
        public static Resolution currentResolution => new Resolution { width = width, height = height };
        public static void SetResolution(int w, int h, bool fullscreen) { width = w; height = h; }
        public static void SetResolution(int w, int h, FullScreenMode mode) { width = w; height = h; fullScreenMode = mode; }
    }

    // Cursor swap stub (HoverCursor). Headless there is no OS cursor: record the last
    // texture/hotspot so proofs can assert the swap happened, and do nothing else.
    public enum CursorMode { Auto = 0, ForceSoftware = 1 }

    public static class Cursor
    {
        public static Texture2D LastTexture { get; private set; }
        public static Vector2 LastHotspot { get; private set; }
        public static void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode)
        {
            LastTexture = texture;
            LastHotspot = hotspot;
        }
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, string> Store = new Dictionary<string, string>();
        public static void SetFloat(string k, float v) => Store[k] = v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        public static float GetFloat(string k, float d = 0f) => Store.TryGetValue(k, out var v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r) ? r : d;
        public static void SetInt(string k, int v) => Store[k] = v.ToString();
        public static int GetInt(string k, int d = 0) => Store.TryGetValue(k, out var v) && int.TryParse(v, out int r) ? r : d;
        public static void SetString(string k, string v) => Store[k] = v ?? "";
        public static string GetString(string k, string d = "") => Store.TryGetValue(k, out var v) ? v : d;
        public static bool HasKey(string k) => Store.ContainsKey(k);
        public static void DeleteKey(string k) => Store.Remove(k);
        public static void DeleteAll() => Store.Clear();
        public static void Save() { }
    }

    public static class Resources
    {
        // Headless stand-in for Unity's Resources folder: Texture2D loads probe the REAL
        // Assets/_Project/Resources tree (PNG header only - no pixel memory), so drop-in art
        // (faces, stamps, the SecurityRoom kit) is exercised by the headless proofs exactly as
        // the editor would resolve it. Every other type stays null, as before.
        public static T Load<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(Texture2D))
            {
                var tex = ProbeTexture(path);
                if (tex != null) return (T)(Object)tex;
            }
            // Fonts probe the real Resources tree too (file presence, no parsing): the kit
            // families extracted from Art/SecurityRoom/1. Fonts/*.zip therefore win the
            // UIFactory load order headless exactly as they do in the editor, and the proofs
            // can assert WHICH family a label ends up wearing.
            if (typeof(T) == typeof(Font))
            {
                var font = ProbeFont(path);
                if (font != null) return (T)(Object)font;
            }
            return null;
        }

        private static Font ProbeFont(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            while (dir != null && !System.IO.Directory.Exists(
                       System.IO.Path.Combine(dir.FullName, "Assets/_Project")))
                dir = dir.Parent;
            if (dir == null) return null;
            foreach (var ext in new[] { ".ttf", ".otf" })
            {
                string file = System.IO.Path.Combine(dir.FullName, "Assets/_Project/Resources", path + ext);
                if (System.IO.File.Exists(file))
                    return new Font { name = System.IO.Path.GetFileName(path), isPersistentAsset = true };
            }
            return null;
        }

        private static Texture2D ProbeTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var dir = new System.IO.DirectoryInfo(Environment.CurrentDirectory);
            while (dir != null && !System.IO.Directory.Exists(
                       System.IO.Path.Combine(dir.FullName, "Assets/_Project")))
                dir = dir.Parent;
            if (dir == null) return null;
            string file = System.IO.Path.Combine(dir.FullName, "Assets/_Project/Resources", path + ".png");
            if (!System.IO.File.Exists(file)) return null;
            try
            {
                using (var fs = System.IO.File.OpenRead(file))
                {
                    var hdr = new byte[24];
                    if (fs.Read(hdr, 0, 24) != 24) return null;
                    if (hdr[0] != 0x89 || hdr[1] != 0x50 || hdr[2] != 0x4E || hdr[3] != 0x47) return null;
                    int w = (hdr[16] << 24) | (hdr[17] << 16) | (hdr[18] << 8) | hdr[19];
                    int h = (hdr[20] << 24) | (hdr[21] << 16) | (hdr[22] << 8) | hdr[23];
                    if (w <= 0 || h <= 0) return null;
                    var asset = Texture2D.HeaderOnly(w, h, System.IO.Path.GetFileName(path));
                    // stands in for an imported PNG: an asset on disk, never Destroy-able
                    asset.isPersistentAsset = true;
                    return asset;
                }
            }
            catch (System.Exception) { return null; }
        }
        public static T GetBuiltinResource<T>(string path) where T : Object
        {
            // real Unity returns the built-in font/meshes; stub fabricates one so text
            // construction paths execute headlessly
            if (typeof(T) == typeof(Font)) return (T)(Object)new Font { name = path };
            return null;
        }
    }

    // ---- math -----------------------------------------------------------------
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 down => new Vector2(0, -1);
        public static Vector2 left => new Vector2(-1, 0);
        public static Vector2 right => new Vector2(1, 0);
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public Vector2 normalized { get { float m = magnitude; return m > 0 ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public static Vector2 operator *(float s, Vector2 a) => a * s;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public override bool Equals(object o) => o is Vector2 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override string ToString() => "(" + x + ", " + y + ")";
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { t = Mathf.Clamp01(t); return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t); }
        public static Vector2 MoveTowards(Vector2 a, Vector2 b, float d) => Lerp(a, b, 1f);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public Vector3 normalized { get { float m = magnitude; return m > 0 ? new Vector3(x / m, y / m, z / m) : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static bool operator ==(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3 a, Vector3 b) => !(a == b);
        public override bool Equals(object o) => o is Vector3 v && v == this;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() << 4);
        public override string ToString() => "(" + x + ", " + y + ", " + z + ")";
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t); }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float maxDelta)
        {
            float d = Distance(a, b);
            if (d <= maxDelta || d == 0) return b;
            return Lerp(a, b, maxDelta / d);
        }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public float sqrMagnitude => x * x + y * y + z * z + w * w;
        public float magnitude => Mathf.Sqrt(sqrMagnitude);
        public static Vector4 zero => new Vector4(0f, 0f, 0f, 0f);
        public static Vector4 one => new Vector4(1f, 1f, 1f, 1f);
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => new Quaternion { w = 1 };
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 v) => identity;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color black => new Color(0, 0, 0, 1);
        public static Color white => new Color(1, 1, 1, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color red => new Color(1, 0, 0, 1);
        public static Color green => new Color(0, 1, 0, 1);
        public static Color gray => new Color(0.5f, 0.5f, 0.5f, 1);
        public static Color grey => gray;
        public static Color yellow => new Color(1, 0.92f, 0.016f, 1);
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }
        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public static bool operator !=(Color a, Color b) => !(a == b);
        public override bool Equals(object o) => o is Color c && c == this;
        public override int GetHashCode() => base.GetHashCode();
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMin => x; public float yMin => y;
        public float xMax => x + width; public float yMax => y + height;
        public Vector2 size => new Vector2(width, height);
        public bool Contains(Vector2 p) => p.x >= xMin && p.x <= xMax && p.y >= yMin && p.y <= yMax;
    }

    public struct RectOffset
    {
        public int left, right, top, bottom;
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Deg2Rad = PI / 180f;
        public const float Epsilon = 1e-6f;
        public const float Infinity = float.PositiveInfinity;
        public static float Abs(float v) => v < 0 ? -v : v;
        public static float Sign(float v) => v < 0 ? -1f : (v > 0 ? 1f : 0f);
        public static int Abs(int v) => v < 0 ? -v : v;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
        public static int Clamp(int v, int a, int b) => v < a ? a : (v > b ? b : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float MoveTowards(float a, float b, float d)
        {
            if (Abs(b - a) <= d) return b;
            return a + (b > a ? d : -d);
        }
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Atan(float v) => (float)Math.Atan(v);
        public const float Rad2Deg = 180f / PI;
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Floor(float v) => (float)Math.Floor(v);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static float Ceil(float v) => (float)Math.Ceiling(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Round(float v) => (float)Math.Round(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static bool Approximately(float a, float b) => Abs(a - b) < 1e-5f;
        public static float Repeat(float t, float len) => t - Floor(t / len) * len;
        public static float PingPong(float t, float len) { t = Repeat(t, len * 2); return len - Abs(t - len); }
        // UnityEngine.Mathf.DeltaAngle: the shortest signed difference between two angles
        public static float DeltaAngle(float current, float target)
        {
            float num = Repeat(target - current, 360f);
            if (num > 180f) num -= 360f;
            return num;
        }
    }

    public static class Random
    {
        private static readonly System.Random R = new System.Random();
        public static float value => (float)R.NextDouble();
        public static int Range(int a, int b) => R.Next(a, b);
        public static float Range(float a, float b) => a + (float)R.NextDouble() * (b - a);
        public static Vector2 insideUnitCircle => new Vector2((float)R.NextDouble() * 2 - 1, (float)R.NextDouble() * 2 - 1);
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T arg);
    public class UnityEvent
    {
        private readonly List<UnityAction> _calls = new List<UnityAction>();
        public void AddListener(UnityAction a) => _calls.Add(a);
        public void RemoveListener(UnityAction a) => _calls.Remove(a);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke() { foreach (var c in _calls.ToArray()) c(); }
    }
    public class UnityEvent<T>
    {
        private readonly List<UnityAction<T>> _calls = new List<UnityAction<T>>();
        public void AddListener(UnityAction<T> a) => _calls.Add(a);
        public void RemoveListener(UnityAction<T> a) => _calls.Remove(a);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke(T v) { foreach (var c in _calls.ToArray()) c(v); }
    }
}
