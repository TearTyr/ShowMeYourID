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

            // classroom placeholder + student spawner: explicit wiring, no Find
            var classroom = views.Room(Hollowbrook.Logic.RoomId.Classroom1);
            var placeholder = World.ClassroomPlaceholder.Build(classroom.Root.transform,
                World.RoomInstance.FloorY, Hollowbrook.Logic.RoomGraph.DoorX(Hollowbrook.Logic.RoomId.Classroom1, Hollowbrook.Logic.RoomId.Hallway));
            var spawnerGo = new GameObject("StudentSpawner");
            spawnerGo.transform.SetParent(sceneRoot.transform, false);
            root.Students = spawnerGo.AddComponent<World.StudentSpawner>();
            root.Students.Init(root, placeholder);

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
            // Pause menu entries need their screens in this scene too, or they silently do nothing.
            root.SettingsScreen = CoreBootstrap.CreateView<SettingsView>(ui, "SettingsView", root);
            root.ControlsScreen = CoreBootstrap.CreateView<ControlsView>(ui, "ControlsView", root);
            root.CreditsScreen = CoreBootstrap.CreateView<CreditsView>(ui, "CreditsView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.RunQueuedSceneAction();
        }

        private void Awake() { CoreBootstrap.SafeBuild("School", () => Build(CoreBootstrap.EnsureCore())); }
    }
}
