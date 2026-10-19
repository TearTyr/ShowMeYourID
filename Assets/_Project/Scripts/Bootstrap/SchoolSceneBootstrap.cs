using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Builds the patrol scene. Attached to School.unity.
    public class SchoolSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("School");
            var fx = CoreBootstrap.NewCanvas(sceneRoot, "FXCanvas", 5, root.Config);
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);
            var scare = CoreBootstrap.NewCanvas(sceneRoot, "ScareCanvas", 20, root.Config);

            root.Checkpoint = null;
            root.MapScreen = CoreBootstrap.CreateView<MapView>(ui, "MapView", root);
            root.ShiftResults = null;
            root.Ending = null;
            root.Loading = null;

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(sceneRoot.transform, false);
            worldGo.SetActive(false);

            var views = worldGo.AddComponent<World.ViewManager>();
            views.Init(root, root.MainCamera, worldGo);
            root.Views = views;

            var hallGo = new GameObject("Hallucinations");
            hallGo.transform.SetParent(sceneRoot.transform, false);
            root.Hallucinations = hallGo.AddComponent<Horror.HallucinationSystem>();
            root.Hallucinations.Init(root);

            var sanityRt = UIFactory.NewUI(fx.transform, "SanityFx");
            root.SanityCtl = sanityRt.gameObject.AddComponent<Horror.SanityController>();
            root.SanityCtl.Init(root, fx);

            var scareRt = UIFactory.NewUI(scare.transform, "ScareFx");
            root.Scares = scareRt.gameObject.AddComponent<Horror.ScareSystem>();
            root.Scares.Init(root, scare);

            root.HUD = CoreBootstrap.CreateView<HUDView>(ui, "HUDView", root);
            root.Dialogue = CoreBootstrap.CreateView<DialogueView>(ui, "DialogueView", root);
            root.PauseScreen = CoreBootstrap.CreateView<PauseView>(ui, "PauseView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            root.SecurityRoom = CoreBootstrap.CreateView<World.SecurityRoomView>(ui, "SecurityRoomView", root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.RunQueuedSceneAction();
        }

        private void Awake() { CoreBootstrap.SafeBuild("School", () => Build(CoreBootstrap.EnsureCore())); }
    }
}
