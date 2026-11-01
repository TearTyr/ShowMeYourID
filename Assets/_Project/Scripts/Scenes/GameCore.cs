using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;   // CanvasScaler (used in NewCanvas) lives in UnityEngine.UI

namespace Hollowbrook.Scenes
{
    public static class SceneRouter
    {
        public const string Menu = "MainMenu";
        public const string Checkpoint = "Hollowbrook";   // Scene A - "That's Not My Neighbor" desk loop
        public const string School = "School";           // Scene B - side-scroller + security room

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

    // The persistent core (game root, audio, event system, camera) and the helpers every scene uses
    // to set its screens up. The screens themselves live in the scene files (see SceneBuild and
    // SceneEdits); this code only finds them, connects them to the game and builds what is missing.
    public static class GameCore
    {
        public static EventSystem CreatedEventSystem { get; private set; }
        public static GameObject CurrentSceneRoot { get; private set; }

        // Headless proofs: pretend this object is the scene root the loaded scene already holds.
        internal static GameObject ExistingRootForTests;

        // Editor only: the core made for setting a scene up is not part of any scene and is removed after.
        private static readonly List<GameObject> EditorCore = new List<GameObject>();

        public static GameRoot EnsureCore()
        {
            if (GameRoot.Instance != null) return GameRoot.Instance;

            var bus = new EventBus();
            var config = GameConfigSO.LoadOrDefault();
            var settings = new SettingsSystem(bus);
            var saves = new SaveSystem();

            // audio first (DDOL) so music/sfx survive every scene change
            var audioGo = new GameObject("AudioDirector");
            var audio = audioGo.AddComponent<AudioDirector>();
            audio.Init(bus, settings);
            KeepAlive(audioGo);

            var rootGo = new GameObject("GameRoot");
            var root = rootGo.AddComponent<GameRoot>();
            root.InitServices(bus, config, saves, settings, audio);
            KeepAlive(rootGo);

            // legacy input module event system (required for ALL uGUI clicks)
            var esGo = new GameObject("EventSystem");
            CreatedEventSystem = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            KeepAlive(esGo);

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            KeepAlive(camGo);
            root.MainCamera = cam;

            return root;
        }

        // Playing: the core survives every scene change. In the editor (setting a scene up) it must
        // never end up inside the scene file, so it is hidden from saving and removed afterwards.
        private static void KeepAlive(GameObject go)
        {
            if (Application.isPlaying) { Object.DontDestroyOnLoad(go); return; }
            go.hideFlags = HideFlags.HideAndDontSave;
            EditorCore.Add(go);
        }

        /// <summary>Destroy an object whether the game is playing (Destroy) or the editor is setting a
        /// scene up (DestroyImmediate, the only kind it allows).</summary>
        public static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        // ---------------------------------------------------------------- editor: set a scene up

        /// <summary>Editor only (Hollowbrook > Set Up Project): build one scene's screens into the open
        /// scene so they can be saved in the scene file. Returns the scene root, or null. Call
        /// <see cref="EndEditorBuild"/> when the scene has been saved.</summary>
        public static GameObject BuildSceneForEditor(string sceneName)
        {
            if (Application.isPlaying) return null;
            var root = EnsureCore();
            switch (sceneName)
            {
                case SceneRouter.Menu: MenuScene.BuildContent(root); break;
                case SceneRouter.Checkpoint: DeskScene.BuildContent(root); break;
                case SceneRouter.School: PatrolScene.BuildContent(root); break;
                default: return null;
            }
            return CurrentSceneRoot;
        }

        /// <summary>Editor only: remove the core that was made for setting a scene up.</summary>
        public static void EndEditorBuild()
        {
            if (Application.isPlaying) return;
            for (int i = EditorCore.Count - 1; i >= 0; i--) Kill(EditorCore[i]);   // the root frees the art cache
            EditorCore.Clear();
            CreatedEventSystem = null;
            CurrentSceneRoot = null;
        }

        // ---------------------------------------------------------------- scenes

        /// <summary>The scene's root object: the one the scene file already holds, or a new one.</summary>
        public static GameObject BeginSceneRoot(string sceneName)
        {
            string rootName = "SceneRoot_" + sceneName;
            var held = ExistingRootForTests != null ? ExistingRootForTests : FindRootInScene(rootName);
            if (held != null)
            {
                CurrentSceneRoot = held;
                return held;
            }
            if (CurrentSceneRoot != null) Kill(CurrentSceneRoot);
            CurrentSceneRoot = new GameObject(rootName);
            return CurrentSceneRoot;
        }

        private static GameObject FindRootInScene(string rootName)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in roots)
                if (go != null && go.name == rootName) return go;
            return null;
        }

        /// <summary>A plain object under the scene root: the one the scene file holds, or a new one.</summary>
        public static GameObject NewObject(GameObject sceneRoot, string name)
        {
            var held = SceneBuild.Reuse(sceneRoot.transform, name);
            if (held != null) return held.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(sceneRoot.transform, false);
            return go;
        }

        public static void SafeBuild(string sceneName, System.Action build)
        {
            try { build(); }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("[GameCore] scene '" + sceneName + "' failed to build: " + e.Message +
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
            Canvas canvas;
            var held = SceneBuild.Reuse(sceneRoot.transform, name);
            if (held != null)
            {
                canvas = UIFactory.ConfigureRootCanvas(held.gameObject, order, cfg.canvasReferenceWidth, cfg.canvasReferenceHeight);
            }
            else
            {
                canvas = UIFactory.CreateRootCanvas(name, order, cfg.canvasReferenceWidth, cfg.canvasReferenceHeight);
                canvas.transform.SetParent(sceneRoot.transform, false);
            }
            var guard = canvas.gameObject.GetOrAdd<Core.AspectMatchGuard>();
            guard.Init(canvas.gameObject.GetComponent<CanvasScaler>());
            return canvas;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneStarted()
        {
            if (GameRoot.Instance != null) return;
            Debug.LogWarning("[GameCore] the scene has no scene component - loading MainMenu.");
            SceneManager.LoadScene(SceneRouter.Menu);
        }

        public static T CreateView<T>(Canvas canvas, string name, GameRoot root) where T : MonoBehaviour
        {
            var rt = UIFactory.NewUI(canvas.transform, name);
            UIFactory.AnchorsFull(rt);
            var view = rt.gameObject.GetOrAdd<T>();
            (view as Core.IScreenView)?.Init(root);
            return view;
        }
    }
}
