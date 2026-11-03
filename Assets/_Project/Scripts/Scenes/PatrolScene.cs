using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // The one component on PatrolScene.unity (the patrol). The patrol screens are objects in the scene
    // file (edit them there); this connects them to the game when the scene loads and builds anything
    // the file lacks. The rooms, doors and items of the patrol world are not in the file: they are
    // data (hit boxes, clues) and are still built here each time the scene loads.
    public class PatrolScene : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            BuildScreens(root, true);
            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.RunQueuedSceneAction();
            GameCore.RefreshScreens(root);
        }

        /// <summary>The screens only (what the editor saves into the scene file).</summary>
        public static void BuildContent(GameRoot root) => BuildScreens(root, false);

        private static void BuildScreens(GameRoot root, bool withWorld)
        {
            var sceneRoot = GameCore.BeginSceneRoot(SceneRouter.School);
            var edits = SceneEdits.Begin(sceneRoot);   // what was changed in the scene file
            SceneBuild.Begin();
            try
            {
                var fx = GameCore.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
                var ui = GameCore.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
                var scare = GameCore.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

                root.Checkpoint = null;
                root.MapScreen = GameCore.CreateView<MapView>(ui, "MapView", root);
                root.ShiftResults = null;
                root.Ending = null;
                root.Loading = null;

                if (withWorld)
                {
                    var worldGo = new GameObject("World");
                    worldGo.transform.SetParent(sceneRoot.transform, false);
                    worldGo.SetActive(false);

                    var views = worldGo.AddComponent<World.ViewManager>();
                    views.Init(root, root.MainCamera, worldGo);
                    root.Views = views;
                }

                root.Hallucinations = GameCore.NewObject(sceneRoot, "Hallucinations").GetOrAdd<Horror.HallucinationSystem>();
                root.Hallucinations.Init(root);

                var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
                root.SanityCtl = sanityRt.gameObject.GetOrAdd<Horror.SanityController>();
                root.SanityCtl.Init(root, fx);

                var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
                root.Scares = scareRt.gameObject.GetOrAdd<Horror.ScareSystem>();
                root.Scares.Init(root, scare);

                root.HUD = GameCore.CreateView<HUDView>(ui, "HUDView", root);
                root.Dialogue = GameCore.CreateView<DialogueView>(ui, "DialogueView", root);
                root.PauseScreen = GameCore.CreateView<PauseView>(ui, "PauseView", root);
                // Pause menu entries need their screens in this scene too, or they silently do nothing.
                root.SettingsScreen = GameCore.CreateView<SettingsView>(ui, "SettingsView", root);
                root.ControlsScreen = GameCore.CreateView<ControlsView>(ui, "ControlsView", root);
                root.CreditsScreen = GameCore.CreateView<CreditsView>(ui, "CreditsView", root);
                root.Captions = GameCore.CreateView<CaptionBar>(ui, "CaptionBar", root);
                root.Confirm = GameCore.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            }
            finally { SceneBuild.End(); }
            edits.Apply();   // the scene file changes go back on top
        }

        private void Awake() { GameCore.SafeBuild(SceneRouter.School, () => Build(GameCore.EnsureCore())); }
    }
}
