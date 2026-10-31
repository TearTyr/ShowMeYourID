// Unity API stubs (scene management) - see UnityEngine.Core.cs header.
using System;

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single = 0, Additive = 1 }

    public static class SceneManager
    {
        // In headless runs, EngineSmoke installs SceneLoadBridge.SceneLoadHook to simulate
        // scene loads (real Unity loads the scene and runs its bootstrap objects' Awake).
        public static void LoadScene(string sceneName) => LoadScene(sceneName, LoadSceneMode.Single);
        public static void LoadScene(string sceneName, LoadSceneMode mode)
        {
            if (SceneLoadBridge.SceneLoadHook != null) SceneLoadBridge.SceneLoadHook(sceneName);
        }
        public static Scene GetActiveScene() => new Scene { name = "stub", isLoaded = true };
        public static int sceneCount { get; set; } = 1;
    }
}
