using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    // The Options modal (Phase 10.3): audio only — MASTER / MUSIC / SFX and a way out.
    // Display, accessibility and data-reset live in SettingsSystem/PlayerPrefs and their
    // other entry points (boot-time ApplyStoredDisplay, the menu's F2×3 reset); this sheet
    // deliberately exposes just the three volumes. Created by each scene bootstrap and
    // opened through GameRoot.OpenSettings from the main menu or the pause menu — the
    // LeftSheet dimmer blocks clicks behind it, Escape/BACK close it, and closing returns
    // to whatever was underneath (pause stays paused; the menu stays the menu).
    public class SettingsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private bool _refreshing;

        private Slider _masterSlider, _musicSlider, _sfxSlider;
        private readonly List<Selectable> _navOrder = new List<Selectable>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "OPTIONS",
                "Audio only. Applies immediately. Remembered on this machine.", out var col);
            col.GetComponent<VerticalLayoutGroup>().spacing = UIFactory.SpaceUnit;
            col.sizeDelta = new Vector2(560f, 150f);
            col.anchoredPosition = new Vector2(320f, -220f);   // rows span 145..295 from the top

            var s = _gameRoot.Settings;

            Section(col, "AUDIO");
            _masterSlider = VolumeRow(col, "master", "Master volume", s.Current.MasterVolume,
                v => { if (!_refreshing) s.Apply(d => d.MasterVolume = v); });
            _musicSlider = VolumeRow(col, "music", "Music volume", s.Current.MusicVolume,
                v => { if (!_refreshing) s.Apply(d => d.MusicVolume = v); });
            _sfxSlider = VolumeRow(col, "sfx", "SFX volume", s.Current.SfxVolume,
                v => { if (!_refreshing) s.Apply(d => d.SfxVolume = v); });

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.CloseSettings();
            }, 360);

            gameObject.SetActive(false);
        }

        private Slider VolumeRow(RectTransform col, string name, string label, float value,
            System.Action<float> onChanged)
        {
            var slider = UIFactory.SliderRow(col, name, label, value, onChanged, 560);
            UIFactory.Layout(slider.GetComponent<RectTransform>(), 560, 34);
            return slider;
        }

        private static void Section(RectTransform col, string title)
        {
            var lab = UIFactory.Label(col, "sec_" + title, title, UIFactory.TypeSection, UIFactory.InkDim,
                TextAnchor.MiddleLeft);
            lab.rectTransform.sizeDelta = new Vector2(560f, 18f);
            lab.raycastTarget = false;
            UIFactory.Layout(lab.rectTransform, 560, 18);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
            _navOrder.Clear();
            foreach (var sel in new Selectable[] { _masterSlider, _musicSlider, _sfxSlider })
                if (sel != null && sel.interactable && sel.gameObject.activeInHierarchy) _navOrder.Add(sel);
            if (_navOrder.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_navOrder[0].gameObject);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy || _gameRoot == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { _gameRoot.CloseSettings(); return; }
            if (Input.GetKeyDown(KeyCode.Tab) && _navOrder.Count > 0 && EventSystem.current != null)
            {
                var curSel = EventSystem.current.currentSelectedGameObject != null
                    ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : null;
                int i = _navOrder.IndexOf(curSel);
                i = (i + (Input.GetKey(KeyCode.LeftShift) ? _navOrder.Count - 1 : 1) + _navOrder.Count) % _navOrder.Count;
                EventSystem.current.SetSelectedGameObject(_navOrder[i].gameObject);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // Sliders reflect the stored values whenever the modal reopens.
        private void Refresh()
        {
            _refreshing = true;
            var s = _gameRoot.Settings.Current;
            if (_masterSlider != null) _masterSlider.value = s.MasterVolume;
            if (_musicSlider != null) _musicSlider.value = s.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = s.SfxVolume;
            _refreshing = false;
        }
    }
}
