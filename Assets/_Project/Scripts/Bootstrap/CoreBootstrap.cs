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
    public static class SceneRouter
    {
        public const string Menu = "MainMenu";
        public const string Checkpoint = "Hollowbrook";   // Scene A — "That's Not My Neighbor" desk loop
        public const string School = "School";           // Scene B — side-scroller + security room

        public static bool IsRegistered(string sceneName) =>
            sceneName == Menu || sceneName == Checkpoint || sceneName == School;

        public static void Load(string sceneName)
        {
            if (!IsRegistered(sceneName))
                Debug.LogError("[SceneRouter] refusing unknown scene '" + sceneName + "'.");
            Debug.Log("[Flow] loading scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }

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

        public static GameObject BeginSceneRoot(string sceneName)
        {
            if (CurrentSceneRoot != null) Object.Destroy(CurrentSceneRoot);
            CurrentSceneRoot = new GameObject("SceneRoot_" + sceneName);
            return CurrentSceneRoot;
        }

        public static void SafeBuild(string sceneName, System.Action build)
        {
            try { build(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[CoreBootstrap] scene '" + sceneName + "' failed to build: " + e.Message +
                               " - returning to the main menu.");
                if (GameRoot.Instance != null)
                {
                    GameRoot.Instance.ClearQueuedSceneAction();
                    GameRoot.Instance.Bus.Publish(GameEvent.Of(EventIds.Notification,
                        "Scene '" + sceneName + "' failed to start - back to the menu."));
                }
                SceneManager.LoadScene(SceneRouter.Menu);
            }
        }

        public static Canvas NewCanvas(GameObject sceneRoot, string name, int order, GameConfigSO cfg)
        {
            var canvas = UIFactory.CreateRootCanvas(name, order, cfg.canvasReferenceWidth, cfg.canvasReferenceHeight);
            canvas.transform.SetParent(sceneRoot.transform, false);
            var guard = canvas.gameObject.AddComponent<Core.AspectMatchGuard>();
            guard.Init(canvas.gameObject.GetComponent<CanvasScaler>());
            return canvas;
        }

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
}
