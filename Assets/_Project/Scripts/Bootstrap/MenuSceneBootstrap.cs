using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    public class MenuSceneBootstrap : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            var sceneRoot = CoreBootstrap.BeginSceneRoot("MainMenu");
            var ui = CoreBootstrap.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);

            root.MainMenu = CoreBootstrap.CreateView<MainMenuView>(ui, "MainMenuView", root);
            root.ModeSelect = CoreBootstrap.CreateView<ModeSelectView>(ui, "ModeSelectView", root);
            root.SettingsScreen = CoreBootstrap.CreateView<SettingsView>(ui, "SettingsView", root);
            root.ControlsScreen = CoreBootstrap.CreateView<ControlsView>(ui, "ControlsView", root);
            root.CreditsScreen = CoreBootstrap.CreateView<CreditsView>(ui, "CreditsView", root);
            root.Captions = CoreBootstrap.CreateView<CaptionBar>(ui, "CaptionBar", root);
            root.Confirm = CoreBootstrap.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);

            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.BootToMainMenu();
        }

        private void Awake() { CoreBootstrap.SafeBuild("MainMenu", () => Build(CoreBootstrap.EnsureCore())); }
    }

}
