using System.Collections.Generic;
using System.IO;
using Hollowbrook.Core;
using Hollowbrook.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.EditorTools
{
    // One click project setup. Menu: Hollowbrook > Set Up Project.
    // It fills the three scene files with the real objects (canvases, every screen, every button and
    // label) so they can be edited in the Hierarchy and Inspector. It only adds what is missing and
    // never overwrites a change you made, so it is safe to click as many times as you like.
    // This file is inside an Editor folder, so it is never part of the game build.
    public static class ProjectSetup
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string ResourcesFolder = "Assets/_Project/Resources";
        private const string ArtFolder = "Assets/_Project/Resources/Art";
        private const string SceneArtFolder = "Assets/_Project/SceneArt";   // pictures the code paints, saved as files
        private const string ConfigPath = "Assets/_Project/Resources/GameConfig.asset";
        private const int MaxTextureSize = 4096;   // the desk, menu and hallway art are bigger than 2048

        // One entry per scene: where the file lives, the script that starts it, and one screen script
        // that proves the scene really holds its screens.
        private class SceneInfo
        {
            public string Path;
            public string Name;            // the name SceneRouter knows it by
            public string StarterObject;   // the object that holds the starter script
            public System.Type Starter;
            public string ScreenScript;
        }

        // The order matters: build index 0 is the scene the built game starts in.
        private static readonly SceneInfo[] Scenes =
        {
            new SceneInfo { Path = ScenesFolder + "/MainMenu.unity", Name = SceneRouter.Menu,
                StarterObject = "MenuScene", Starter = typeof(MenuScene), ScreenScript = "MainMenuView.cs" },
            new SceneInfo { Path = ScenesFolder + "/Hollowbrook.unity", Name = SceneRouter.Checkpoint,
                StarterObject = "DeskScene", Starter = typeof(DeskScene), ScreenScript = "CheckpointView.cs" },
            new SceneInfo { Path = ScenesFolder + "/School.unity", Name = SceneRouter.School,
                StarterObject = "PatrolScene", Starter = typeof(PatrolScene), ScreenScript = "HUDView.cs" },
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
            FixScenes(done, false);
            FixBuildSettings(done);
            FixPlayModeStart(done);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowResult("Hollowbrook setup", done);
        }

        // For a scene that is badly broken: throws its objects away and fills it again from the code.
        [MenuItem("Hollowbrook/Rebuild Scenes From Scratch")]
        public static void RebuildFromScratch()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Hollowbrook", "Stop Play mode first.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Rebuild scenes from scratch",
                    "This throws away EVERY change you made inside MainMenu, Hollowbrook and School and fills them " +
                    "again from the code.\n\nIf you only want to add what is missing, use Set Up Project instead.\n\nContinue?",
                    "Rebuild", "Cancel"))
                return;

            var done = new List<string>();
            FixConfig(done);
            FixArt(done);
            FixScenes(done, true);
            FixBuildSettings(done);
            FixPlayModeStart(done);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowResult("Hollowbrook rebuild", done);
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

        private static void ShowResult(string title, List<string> done)
        {
            var problems = FindProblems();
            string message = done.Count == 0 ? "Nothing needed fixing.\n" : "Fixed:\n- " + string.Join("\n- ", done) + "\n";
            if (problems.Count > 0)
                message += "\nStill wrong (the setup could not fix these):\n- " + string.Join("\n- ", problems);
            else
                message += "\nEverything is set. Open MainMenu and press Play.";

            Debug.Log("[Setup] " + message);
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        // ------------------------------------------------------------------ fixes

        private static void FixConfig(List<string> done)
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfigSO>(ConfigPath) != null) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfigSO>(), ConfigPath);
            done.Add("created GameConfig.asset (it was missing)");
        }

        // Every picture under Resources/Art becomes a Sprite with the size the game draws it at.
        private static void FixArt(List<string> done)
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || ArtIsCorrect(importer, path)) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.maxTextureSize = MaxTextureSize;
                string key = Path.GetFileNameWithoutExtension(path);
                importer.spritePixelsPerUnit = ProceduralArt.FilePpuFor(key);
                importer.spriteBorder = ProceduralArt.FileBorderFor(key);
                importer.SaveAndReimport();
                changed++;
            }
            if (changed > 0) done.Add("set " + changed + " pictures to Sprite with the right size, max size " + MaxTextureSize);
        }

        private static void FixScenes(List<string> done, bool fromScratch)
        {
            var todo = new List<SceneInfo>();
            foreach (var info in Scenes)
                if (fromScratch || !SceneIsFilled(info)) todo.Add(info);
            if (todo.Count == 0) return;

            // Filling a scene replaces the one that is open, so ask about unsaved work first.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var info in todo)
            {
                bool fresh = fromScratch || AssetDatabase.LoadAssetAtPath<SceneAsset>(info.Path) == null;
                var message = FillScene(info, fresh);
                if (message != null) done.Add(message);
            }

            // Leave the editor on the main menu, like a fresh project.
            EditorSceneManager.OpenScene(Scenes[0].Path);
        }

        // Open (or start) one scene, let the game code build its screens into it, put real picture files
        // in place of the code-painted ones, and save it.
        private static string FillScene(SceneInfo info, bool fresh)
        {
            var scene = fresh
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(info.Path, OpenSceneMode.Single);

            // the object that starts the scene when it loads
            var starter = FindRoot(scene, info.StarterObject);
            if (starter == null) starter = new GameObject(info.StarterObject);
            if (starter.GetComponent(info.Starter) == null) starter.AddComponent(info.Starter);

            // what a designer changed in this scene before (so it is kept)
            var oldRoot = FindRoot(scene, "SceneRoot_" + info.Name);
            var kept = SceneEdits.Begin(oldRoot);

            try
            {
                var root = GameCore.BuildSceneForEditor(info.Name);
                if (root == null) return "could not fill " + info.Path + " (the scene code returned nothing)";

                int swapped = UseRealPictures(root);

                var edits = root.GetComponent<SceneEdits>();
                if (edits == null) edits = root.AddComponent<SceneEdits>();
                if (edits.IsBaked) edits.RefreshBaseline(root.transform, kept);
                else edits.StoreBaseline(root.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, info.Path);
                return (fresh ? "filled scene " : "updated scene ") + info.Path + " (" + swapped + " code-painted pictures saved as files)";
            }
            finally
            {
                GameCore.EndEditorBuild();   // the game core made for this is not part of the scene
                ProceduralArt.ClearCache();
            }
        }

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name) return go;
            return null;
        }

        // A scene file cannot hold a picture the code painted in memory. Every such picture is written to
        // SceneArt as a png and the scene points at that file instead. Pictures that already come from
        // a file under Resources/Art point at that file. Returns how many were written.
        private static int UseRealPictures(GameObject root)
        {
            var assets = new Dictionary<string, Sprite>();
            int written = 0;
            Directory.CreateDirectory(SceneArtFolder);

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null || AssetDatabase.Contains(image.sprite)) continue;
                image.sprite = AssetSpriteFor(image.sprite, assets, ref written);
            }
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sprite == null || AssetDatabase.Contains(renderer.sprite)) continue;
                renderer.sprite = AssetSpriteFor(renderer.sprite, assets, ref written);
            }
            return written;
        }

        private static Sprite AssetSpriteFor(Sprite painted, Dictionary<string, Sprite> cache, ref int written)
        {
            string key = painted.name;
            Sprite found;
            if (cache.TryGetValue(key, out found)) return found;

            Sprite asset = null;
            if (ProceduralArt.IsFileBacked(key)) asset = FindFileSprite(key);
            if (asset == null) asset = SavePaintedSprite(painted, ref written);
            cache[key] = asset != null ? asset : painted;   // if nothing worked the scene keeps the in-memory one
            return cache[key];
        }

        private static Sprite FindFileSprite(string key)
        {
            foreach (string guid in AssetDatabase.FindAssets(key + " t:Sprite", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == key)
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Sprite SavePaintedSprite(Sprite painted, ref int written)
        {
            if (painted.texture == null) return null;
            string file = SafeFileName(painted.name);
            string path = SceneArtFolder + "/" + file + ".png";
            byte[] bytes = PngOf(painted);
            if (bytes == null) return null;

            if (!File.Exists(path) || !SameBytes(File.ReadAllBytes(path), bytes))
            {
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                written++;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !SceneArtIsCorrect(importer, painted))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = painted.pixelsPerUnit;
                importer.spriteBorder = painted.border;
                importer.maxTextureSize = MaxTextureSize;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // The sprite's own pixels as a png. A picture the code painted can be encoded directly. A sprite
        // cut out of a picture file (the desk's drawer sheet) sits on a texture that is not readable, so
        // it is copied through a render texture first, cropped to the sprite's rectangle.
        private static byte[] PngOf(Sprite sprite)
        {
            var source = sprite.texture;
            var area = sprite.textureRect;
            bool whole = source.isReadable && (int)area.width == source.width && (int)area.height == source.height;
            if (whole) return source.EncodeToPNG();

            var temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Graphics.Blit(source, temp);
            RenderTexture.active = temp;
            var copy = new Texture2D((int)area.width, (int)area.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(area.x, area.y, area.width, area.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temp);
            byte[] bytes = copy.EncodeToPNG();
            Object.DestroyImmediate(copy);
            return bytes;
        }

        private static string SafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrEmpty(name) ? "sprite" : name;
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
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
                if (!SceneIsFilled(info))
                    problems.Add(info.Path + " is missing, empty or has no " + info.Starter.Name + " (run Set Up Project)");

            if (!BuildSettingsAreCorrect())
                problems.Add("Build Settings do not list MainMenu, Hollowbrook and School in that order");

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && !ArtIsCorrect(importer, path)) problems.Add(path + " is not imported as a Sprite at the right size");
            }

            // the game no longer builds its screens from a script when it starts
            foreach (string guid in AssetDatabase.FindAssets("Bootstrap t:MonoScript"))
                problems.Add(AssetDatabase.GUIDToAssetPath(guid) + " is a bootstrap script (the screens now live in the scenes)");

            return problems;
        }

        private static bool ArtIsCorrect(TextureImporter importer, string path)
        {
            string key = Path.GetFileNameWithoutExtension(path);
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.maxTextureSize >= MaxTextureSize
                && Mathf.Abs(importer.spritePixelsPerUnit - ProceduralArt.FilePpuFor(key)) < 0.01f
                && importer.spriteBorder == ProceduralArt.FileBorderFor(key);
        }

        private static bool SceneArtIsCorrect(TextureImporter importer, Sprite painted)
        {
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.maxTextureSize >= MaxTextureSize
                && Mathf.Abs(importer.spritePixelsPerUnit - painted.pixelsPerUnit) < 0.01f
                && importer.spriteBorder == painted.border;
        }

        // The scene file must exist, hold its start script and hold its screens. GetDependencies lists
        // every asset a file points at, so the scripts show up there when their components are on objects.
        // The scene must also hold the saved baseline (SceneEdits), which only the setup tool writes.
        private static bool SceneIsFilled(SceneInfo info)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(info.Path) == null) return false;
            bool starter = false, screen = false, baseline = false;
            foreach (string dependency in AssetDatabase.GetDependencies(info.Path, false))
            {
                if (dependency.EndsWith("/" + info.Starter.Name + ".cs")) starter = true;
                if (dependency.EndsWith("/" + info.ScreenScript)) screen = true;
                if (dependency.EndsWith("/SceneEdits.cs")) baseline = true;
            }
            return starter && screen && baseline;
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
