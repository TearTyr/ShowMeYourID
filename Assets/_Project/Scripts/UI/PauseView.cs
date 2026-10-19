using System;
using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // Pause sheet with resume/settings/quit. Created by each scene bootstrap.
    public class PauseView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "PAUSED", "The night waits. It is patient.", out var col);

            Add(col.transform, "RESUME", () => _gameRoot.TogglePause(false), false);
            Add(col.transform, "CONTROLS", () => _gameRoot.OpenControls(), false);
            Add(col.transform, "SETTINGS", () => _gameRoot.OpenSettings(), false);
            Add(col.transform, "SAVE & QUIT TO MENU", () =>
                _gameRoot.Confirm.Ask("QUIT TO MENU?", "Progress is saved at shift boundaries. Quit now?", () => _gameRoot.QuitToMenu(true)), true);
            Add(col.transform, "QUIT GAME", () =>
                _gameRoot.Confirm.Ask("QUIT GAME?", "Save and exit to desktop?", () => _gameRoot.QuitApplication()), true);

            gameObject.SetActive(false);
        }

        private void Add(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool danger)
        {
            var b = UIFactory.StandardButtonLeft(parent, label, label, () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_click", 0.7f);
                onClick();
            }, 420, 46, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), 420, 46);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
