using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // The one component on Hollowbrook.unity (the desk). The desk screens are objects in the scene
    // file (edit them there); this connects them to the game when the scene loads and builds anything
    // the file lacks.
    public class DeskScene : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            BuildContent(root);
            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            Debug.Log("[Flow] desk scene ready (checkpoint=" + (root.Checkpoint != null) +
                      " loading=" + (root.Loading != null) + " dialogue=" + (root.Dialogue != null) + ")");
            root.RunQueuedSceneAction();
        }

        /// <summary>The screens only (what the editor saves into the scene file).</summary>
        public static void BuildContent(GameRoot root)
        {
            var sceneRoot = GameCore.BeginSceneRoot("Hollowbrook");
            var edits = SceneEdits.Begin(sceneRoot);   // what was changed in the scene file
            SceneBuild.Begin();
            try
            {
                var fx = GameCore.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
                var ui = GameCore.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
                var scare = GameCore.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

                // world-scoped references from other scenes must not leak in
                root.Views = null;

                root.Checkpoint = GameCore.CreateView<CheckpointView>(ui, "CheckpointView", root);
                root.HUD = GameCore.CreateView<HUDView>(ui, "HUDView", root);
                root.MapScreen = GameCore.CreateView<MapView>(ui, "MapView", root);
                root.ShiftResults = GameCore.CreateView<ShiftResultsView>(ui, "ShiftResultsView", root);
                root.Ending = GameCore.CreateView<EndingView>(ui, "EndingView", root);
                root.Loading = GameCore.CreateView<LoadingView>(ui, "LoadingView", root);
                root.Dialogue = GameCore.CreateView<DialogueView>(ui, "DialogueView", root);
                root.PauseScreen = GameCore.CreateView<PauseView>(ui, "PauseView", root);
                root.Captions = GameCore.CreateView<CaptionBar>(ui, "CaptionBar", root);
                root.Confirm = GameCore.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
                root.SettingsScreen = GameCore.CreateView<SettingsView>(ui, "SettingsView", root);
                root.ControlsScreen = GameCore.CreateView<ControlsView>(ui, "ControlsView", root);
                root.CreditsScreen = GameCore.CreateView<CreditsView>(ui, "CreditsView", root);

                var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
                root.SanityCtl = sanityRt.gameObject.GetOrAdd<Horror.SanityController>();
                root.SanityCtl.Init(root, fx);

                var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
                root.Scares = scareRt.gameObject.GetOrAdd<Horror.ScareSystem>();
                root.Scares.Init(root, scare);

                root.Hallucinations = GameCore.NewObject(sceneRoot, "Hallucinations").GetOrAdd<Horror.HallucinationSystem>();
                root.Hallucinations.Init(root);
            }
            finally { SceneBuild.End(); }
            edits.Apply();   // the scene file changes go back on top
        }

        private void Awake() { GameCore.SafeBuild("Hollowbrook", () => Build(GameCore.EnsureCore())); }
    }
}
