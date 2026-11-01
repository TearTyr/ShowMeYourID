using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hollowbrook.EditorTools
{
    // One click project setup. Menu: Hollowbrook > Set Up Project.
    // It only fixes what is broken, so it is safe to click as many times as you like.
    // This file is inside an Editor folder, so it is never part of the game build.
    public static class ProjectSetup
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string ArtFolder = "Assets/_Project/Resources/Art";
        private const string ConfigPath = "Assets/_Project/Resources/GameConfig.asset";
        private const int MaxTextureSize = 4096;   // the desk, menu and hallway art are bigger than 2048

        // One entry per scene: where the file lives and which script builds it.
        private class SceneInfo
        {
            public string Path;
            public string ObjectName;
            public System.Type Bootstrap;
        }

        // The order matters: build index 0 is the scene the built game starts in.
        private static readonly SceneInfo[] Scenes =
        {
            new SceneInfo { Path = ScenesFolder + "/MainMenu.unity", ObjectName = "MenuBootstrap", Bootstrap = typeof(MenuSceneBootstrap) },
            new SceneInfo { Path = ScenesFolder + "/Hollowbrook.unity", ObjectName = "CheckpointBootstrap", Bootstrap = typeof(CheckpointSceneBootstrap) },
            new SceneInfo { Path = ScenesFolder + "/School.unity", ObjectName = "SchoolBootstrap", Bootstrap = typeof(SchoolSceneBootstrap) },
        };

        [MenuItem("Hollowbrook/Set Up Project (one click)")]
        public static void SetUp()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Hollowbrook", "Stop Play mode first, then run the setup again.", "OK");
                return;
            }

            var done = new List<string>();
            FixConfig(done);
            FixArt(done);
            FixScenes(done);
            FixBuildSettings(done);
            FixPlayModeStart(done);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var problems = FindProblems();
            string message = done.Count == 0 ? "Nothing needed fixing.\n" : "Fixed:\n- " + string.Join("\n- ", done) + "\n";
            if (problems.Count > 0)
                message += "\nStill wrong (the setup could not fix these):\n- " + string.Join("\n- ", problems);
            else
                message += "\nEverything is set. Open MainMenu and press Play.";

            Debug.Log("[Setup] " + message);
            EditorUtility.DisplayDialog("Hollowbrook setup", message, "OK");
        }

        [MenuItem("Hollowbrook/Check Project")]
        public static void Check()
        {
            var problems = FindProblems();
            string message = problems.Count == 0
                ? "Everything looks right."
                : "Found problems (run Hollowbrook > Set Up Project to fix them):\n- " + string.Join("\n- ", problems);
            Debug.Log("[Setup] " + message);
            EditorUtility.DisplayDialog("Hollowbrook check", message, "OK");
        }

        // ------------------------------------------------------------------ fixes

        private static void FixConfig(List<string> done)
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfigSO>(), ConfigPath);
            done.Add("created GameConfig.asset (it was missing)");
        }

        private static void FixArt(List<string> done)
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || ArtIsCorrect(importer)) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = MaxTextureSize;
                importer.SaveAndReimport();
                changed++;
            }
            if (changed > 0) done.Add("set " + changed + " pictures to Sprite, max size " + MaxTextureSize);
        }

        private static void FixScenes(List<string> done)
        {
            var broken = new List<SceneInfo>();
            foreach (var info in Scenes)
                if (!SceneHasBootstrap(info)) broken.Add(info);
            if (broken.Count == 0) return;

            // Making a scene replaces the one that is open, so ask about unsaved work first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var info in broken)
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject(info.ObjectName);
                go.AddComponent(info.Bootstrap);
                EditorSceneManager.SaveScene(scene, info.Path);
                done.Add("rebuilt scene " + info.Path);
            }

            // Leave the editor on the main menu, like a fresh project.
            EditorSceneManager.OpenScene(Scenes[0].Path);
        }

        private static void FixBuildSettings(List<string> done)
        {
            if (BuildSettingsAreCorrect()) return;
            var list = new EditorBuildSettingsScene[Scenes.Length];
            for (int i = 0; i < Scenes.Length; i++)
                list[i] = new EditorBuildSettingsScene(Scenes[i].Path, true);
            EditorBuildSettings.scenes = list;
            done.Add("listed the 3 scenes in Build Settings (MainMenu first)");
        }

        // Pressing Play always starts at the main menu, whichever scene is open.
        private static void FixPlayModeStart(List<string> done)
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes[0].Path);
            if (menu == null || EditorSceneManager.playModeStartScene == menu) return;
            EditorSceneManager.playModeStartScene = menu;
            done.Add("Play now always starts at MainMenu");
        }

        // ------------------------------------------------------------------ checks

        private static List<string> FindProblems()
        {
            var problems = new List<string>();

            if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath) == null)
                problems.Add("GameConfig.asset is missing from " + ResourcesFolder);

            foreach (var info in Scenes)
                if (!SceneHasBootstrap(info)) problems.Add(info.Path + " is missing or has no " + info.Bootstrap.Name);

            if (!BuildSettingsAreCorrect())
                problems.Add("Build Settings do not list MainMenu, Hollowbrook and School in that order");

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !ArtIsCorrect(importer)) problems.Add(path + " is not imported as a Sprite");
            }

            return problems;
        }

        private static bool ArtIsCorrect(TextureImporter importer)
        {
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.maxTextureSize >= MaxTextureSize;
        }

        // The scene file must exist and use the bootstrap script. GetDependencies lists every
        // asset a file points at, so the script shows up there when the component is on an object.
        private static bool SceneHasBootstrap(SceneInfo info)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(info.Path) == null) return false;
            foreach (string dependency in AssetDatabase.GetDependencies(info.Path, false))
                if (dependency.EndsWith("/" + info.Bootstrap.Name + ".cs")) return true;
            return false;
        }

        private static bool BuildSettingsAreCorrect()
        {
            var current = EditorBuildSettings.scenes;
            if (current.Length != Scenes.Length) return false;
            for (int i = 0; i < Scenes.Length; i++)
                if (current[i].path != Scenes[i].Path || !current[i].enabled) return false;
            return true;
        }
    }
}
