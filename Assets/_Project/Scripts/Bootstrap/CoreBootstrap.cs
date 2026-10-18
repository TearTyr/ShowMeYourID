using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;   // CanvasScaler (used in NewCanvas) lives in UnityEngine.UI

namespace Hollowbrook.Bootstrap
{
    /// <summary>Scene names. Scene loading is centralized here so new gameplay modes can be
    /// added later by registering a scene + bootstrap without touching the menu.</summary>
    public static class SceneRouter
    {
        public const string Menu = "MainMenu";
        public const string Checkpoint = "Hollowbrook";   // Scene A — "That's Not My Neighbor" desk loop
        public const string School = "School";           // Scene B — side-scroller + security room

        public static void Load(string sceneName)
        {
            Debug.Log("[SceneRouter] loading scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>Persistent core (survives scene loads): GameRoot + services + audio +
    /// EventSystem + camera, all DontDestroyOnLoad. Each scene bootstrap then builds only
    /// its own canvases/views under a per-scene root that Unity unloads normally.</summary>
    public static class CoreBootstrap
    {
        public static EventSystem CreatedEventSystem { get; private set; }
        public static GameObject CurrentSceneRoot { get; private set; }

        public static GameRoot EnsureCore()
        {
            if (GameRoot.Instance != null) return GameRoot.Instance;

            var bus = new EventBus();
            var config = GameConfigSO.LoadOrDefault();
            var settings = new SettingsSystem(bus);
            var saves = new SaveSystem(bus);

            // audio first (DDOL) so music/sfx survive every scene change
            var audioGo = new GameObject("AudioDirector");
            var audio = audioGo.AddComponent<AudioDirector>();
            audio.Init(bus, settings);
            Object.DontDestroyOnLoad(audioGo);

            var rootGo = new GameObject("GameRoot");
            var root = rootGo.AddComponent<GameRoot>();
            root.InitServices(bus, config, saves, settings, audio);
            Object.DontDestroyOnLoad(rootGo);

            // legacy input module event system (required for ALL uGUI clicks)
            var esGo = new GameObject("EventSystem");
            CreatedEventSystem = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(esGo);

            // single persistent camera
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            Object.DontDestroyOnLoad(camGo);
            root.MainCamera = cam;

            return root;
        }

        /// <summary>Destroys the previous scene root (headless: deactivates it) and creates a
        /// fresh container for the incoming scene's views.</summary>
        public static GameObject BeginSceneRoot(string sceneName)
        {
            if (CurrentSceneRoot != null) Object.Destroy(CurrentSceneRoot);
            CurrentSceneRoot = new GameObject("SceneRoot_" + sceneName);
            return CurrentSceneRoot;
        }

        public static Canvas NewCanvas(GameObject sceneRoot, string name, int order, GameConfigSO cfg)
        {
            var canvas = UIFactory.CreateRootCanvas(name, order, cfg.canvasReferenceWidth, cfg.canvasReferenceHeight);
            canvas.transform.SetParent(sceneRoot.transform, false);
            var guard = canvas.gameObject.AddComponent<Core.AspectMatchGuard>();
            guard.Init(canvas.gameObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        /// <summary>Safety net: if Play starts in a scene without any bootstrap object
        /// (e.g. Unity's default SampleScene), route to the menu instead of a dead screen.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrappedScene()
        {
            if (GameRoot.Instance != null) return;
            Debug.LogWarning("[CoreBootstrap] no bootstrap in active scene — loading MainMenu.");
            SceneManager.LoadScene(SceneRouter.Menu);
        }

        public static T CreateView<T>(Canvas canvas, string name, GameRoot root) where T : MonoBehaviour
        {
            var rt = UIFactory.NewUI(canvas.transform, name);
            UIFactory.AnchorsFull(rt);
            var view = rt.gameObject.AddComponent<T>();
            (view as Core.IScreenView)?.Init(root);
            return view;
        }
    }

    /// <summary>Maps scene names to builder delegates. Used by headless verification to
    /// simulate SceneManager.LoadScene; in the real editor each scene's bootstrap object
    /// runs the same Build from Awake.</summary>
    public static class SceneBootstrapRegistry
    {
        private static Dictionary<string, System.Action> _map;

        private static void Ensure()
        {
            if (_map != null) return;
            _map = new Dictionary<string, System.Action>
            {
                { SceneRouter.Menu, () => MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore()) },
                { SceneRouter.Checkpoint, () => CheckpointSceneBootstrap.Build(CoreBootstrap.EnsureCore()) },
                { SceneRouter.School, () => SchoolSceneBootstrap.Build(CoreBootstrap.EnsureCore()) },
            };
        }

        public static void SimulateLoad(string sceneName)
        {
            Ensure();
            if (_map.TryGetValue(sceneName, out var build)) build();
            else Debug.LogError("[SceneBootstrapRegistry] no bootstrap registered for scene '" + sceneName + "'");
        }
    }
}
