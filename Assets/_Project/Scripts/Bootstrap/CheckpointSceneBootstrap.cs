using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    /// <summary>Scene A — "That's Not My Neighbor" gameplay: the fixed inspection desk.
    /// Checkpoint flow, HUD, dialogue, results, endings, sanity/scare FX. No world rooms here;
    /// PATROL loads the School scene.</summary>
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
            // ORDER MATTERS: Loading is an opaque click-blocking backdrop — it must be created
            // BEFORE Dialogue (and everything else that overlays it), otherwise shift intros
            // render invisibly behind it and clicks never reach the dialogue (v1.0.4 bug).
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
            root.RunQueuedSceneAction();
        }

        private void Awake() { Build(CoreBootstrap.EnsureCore()); }
    }

}
