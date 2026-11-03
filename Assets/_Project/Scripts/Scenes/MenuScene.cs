using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // The one component on MenuScene.unity. The menu screens are objects in the scene file (edit them
    // there); this connects them to the game when the scene loads and builds anything the file lacks.
    public class MenuScene : MonoBehaviour
    {
        public static void Build(GameRoot root)
        {
            BuildContent(root);
            UIFactory.ApplyTextScale(root.Settings.TextScaleMultiplier);
            root.BootToMainMenu();
            GameCore.RefreshScreens(root);
        }

        /// <summary>The screens only (what the editor saves into the scene file).</summary>
        public static void BuildContent(GameRoot root)
        {
            var sceneRoot = GameCore.BeginSceneRoot(SceneRouter.Menu);
            var edits = SceneEdits.Begin(sceneRoot);   // what was changed in the scene file
            SceneBuild.Begin();
            try
            {
                var ui = GameCore.NewCanvas(sceneRoot, "UICanvas", 10, root.Config);

                root.MainMenu = GameCore.CreateView<MainMenuView>(ui, "MainMenuView", root);
                root.SettingsScreen = GameCore.CreateView<SettingsView>(ui, "SettingsView", root);
                root.ControlsScreen = GameCore.CreateView<ControlsView>(ui, "ControlsView", root);
                root.CreditsScreen = GameCore.CreateView<CreditsView>(ui, "CreditsView", root);
                root.Captions = GameCore.CreateView<CaptionBar>(ui, "CaptionBar", root);
                root.Confirm = GameCore.CreateView<ConfirmationPopup>(ui, "ConfirmationPopup", root);
            }
            finally { SceneBuild.End(); }
            edits.Apply();   // the scene file changes go back on top
        }

        private void Awake() { GameCore.SafeBuild(SceneRouter.Menu, () => Build(GameCore.EnsureCore())); }
    }
}
