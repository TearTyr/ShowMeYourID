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
        private GameRoot R;

        public void Init(GameRoot root)
        {
            R = root;
            var sheet = UIFactory.LeftSheet(transform, "PAUSED", "The night waits. It is patient.", out var col);

            Add(col.transform, "RESUME", () => R.TogglePause(false), false);
            Add(col.transform, "CONTROLS", () => R.OpenControls(), false);
            Add(col.transform, "SETTINGS", () => R.OpenSettings(), false);
            Add(col.transform, "SAVE & QUIT TO MENU", () =>
                R.Confirm.Ask("QUIT TO MENU?", "Progress is saved at shift boundaries. Quit now?", () => R.QuitToMenu(true)), true);
            Add(col.transform, "QUIT GAME", () =>
                R.Confirm.Ask("QUIT GAME?", "Save and exit to desktop?", () => R.QuitApplication()), true);

            gameObject.SetActive(false);
        }

        private void Add(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool danger)
        {
            var b = UIFactory.StandardButtonLeft(parent, label, label, () =>
            {
                R.Audio.PlaySfx("sfx_click", 0.7f);
                onClick();
            }, 420, 46, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), 420, 46);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
