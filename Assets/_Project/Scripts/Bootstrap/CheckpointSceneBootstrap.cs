using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    public class CheckpointSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("Hollowbrook");
            var fx = CoreBootstrap.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
            var scare = CoreBootstrap.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

            // world-scoped references from other scenes must not leak in
            root.Views = null;
            root.SecurityRoom = null;

            root.Checkpoint = CoreBootstrap.CreateView<CheckpointView>(ui, "CheckpointView", root);
            root.HUD = CoreBootstrap.CreateView<HUDView>(ui, "HUDView", root);
            root.MapScreen = CoreBootstrap.CreateView<MapView>(ui, "MapView", root);
            root.ShiftResults = CoreBootstrap.CreateView<ShiftResultsView>(ui, "ShiftResultsView", root);
            root.Ending = CoreBootstrap.CreateView<EndingView>(ui, "EndingView", root);
            root.Loading = CoreBootstrap.CreateView<LoadingView>(ui, "LoadingView", root);
            root.Dialogue = CoreBootstrap.CreateView<DialogueView>(ui, "DialogueView", root);
            root.PauseScreen = CoreBootstrap.CreateView<PauseView>(ui, "PauseView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            root.SettingsScreen = CoreBootstrap.CreateView<SettingsView>(ui, "SettingsView", root);
            root.ControlsScreen = CoreBootstrap.CreateView<ControlsView>(ui, "ControlsView", root);
            root.CreditsScreen = CoreBootstrap.CreateView<CreditsView>(ui, "CreditsView", root);

            var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
            root.SanityCtl = sanityRt.gameObject.AddComponent<Horror.SanityController>();
            root.SanityCtl.Init(root, fx);

            var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
            root.Scares = scareRt.gameObject.AddComponent<Horror.ScareSystem>();
            root.Scares.Init(root, scare);

            var hallGo = new GameObject("Hallucinations");
            hallGo.transform.SetParent(sceneRoot.transform, false);
            root.Hallucinations = hallGo.AddComponent<Horror.HallucinationSystem>();
            root.Hallucinations.Init(root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            Debug.Log("[Flow] checkpoint bootstrap built (checkpoint=" + (root.Checkpoint != null) +
                      " loading=" + (root.Loading != null) + " dialogue=" + (root.Dialogue != null) + ")");
            root.RunQueuedSceneAction();
        }

        private void Awake() { CoreBootstrap.SafeBuild("Hollowbrook", () => Build(CoreBootstrap.EnsureCore())); }
    }

}
