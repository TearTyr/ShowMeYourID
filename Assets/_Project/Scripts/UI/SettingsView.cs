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
    // The Options sheet: volumes, display, accessibility, reset. Created by each scene bootstrap.
    public class SettingsView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private bool _refreshing;

        private Slider _masterSlider, _sfxSlider;
        private Toggle _fullscreenToggle;
        private readonly Toggle[] _toggles = new Toggle[3];
        private readonly Button[] _textScaleButtons = new Button[3];
        private readonly Button[] _resButtons = new Button[4];
        private readonly Text[] _resLabels = new Text[4];
        private List<ModeSize> _choices = new List<ModeSize>();

        private RectTransform _displayNoteRow;
        private Text _displayNote;
        private SettingsData _displayBefore;
        private Coroutine _revertRoutine;
        private int _revertLeft;
        private Text _dataHint;
        private ScrollRect _scroll;
        private Slider _musicSlider;
        private GameObject _resRow;
        private Text _resNote;
        private readonly List<Selectable> _navOrder = new List<Selectable>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            var sheet = UIFactory.LeftSheet(transform, "OPTIONS",
                "Applies immediately. Remembered on this machine.", out var col);
            col.GetComponent<VerticalLayoutGroup>().spacing = UIFactory.SpaceUnit;
            col.sizeDelta = new Vector2(560f, 505f);
            col.anchoredPosition = new Vector2(320f, -364f);

            var viewport = UIFactory.NewUI(sheet, "optionsViewport");
            UIFactory.Anchor(viewport, new Vector2(0f, 1f), new Vector2(560f, 505f), new Vector2(320f, -364f));
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = false;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            col.SetParent(viewport, false);
            col.anchorMin = new Vector2(0f, 1f); col.anchorMax = new Vector2(1f, 1f);
            col.pivot = new Vector2(0.5f, 1f);
            col.anchoredPosition = Vector2.zero;
            col.sizeDelta = Vector2.zero;
            col.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = col;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 24f;

            var s = _gameRoot.Settings;

            Section(col, "AUDIO");
            _masterSlider = UIFactory.SliderRow(col, "master", "Master volume", s.Current.MasterVolume,
                v => { if (!_refreshing) s.Apply(d => d.MasterVolume = v); }, 560);
            UIFactory.Layout(_masterSlider.GetComponent<RectTransform>(), 560, 34);
            _musicSlider = UIFactory.SliderRow(col, "music", "Music volume", s.Current.MusicVolume,
                v => { if (!_refreshing) s.Apply(d => d.MusicVolume = v); }, 560);
            UIFactory.Layout(_musicSlider.GetComponent<RectTransform>(), 560, 34);
            _sfxSlider = UIFactory.SliderRow(col, "sfx", "SFX volume", s.Current.SfxVolume,
                v => { if (!_refreshing) s.Apply(d => d.SfxVolume = v); }, 560);
            UIFactory.Layout(_sfxSlider.GetComponent<RectTransform>(), 560, 34);

            Section(col, "DISPLAY");
            _fullscreenToggle = UIFactory.ToggleRow(col, "fullscreen", "Fullscreen  (borderless)",
                s.Current.Fullscreen, v =>
                {
                    if (_refreshing) return;
                    var cur = _gameRoot.Settings.CurrentMode();
                    BeginDisplayChange(() => s.SetDisplay(
                        s.Current.DisplayWidth > 0 ? s.Current.DisplayWidth : cur.W,
                        s.Current.DisplayHeight > 0 ? s.Current.DisplayHeight : cur.H, v, false));
                }, 560);
            UIFactory.Layout(_fullscreenToggle.GetComponent<RectTransform>(), 560, 32);

            var resRow = UIFactory.NewUI(col, "resrow");
            _resRow = resRow.gameObject;
            UIFactory.Layout(resRow, 560, 38);
            var resLab = UIFactory.Label(resRow, "lab", "Resolution", 13, UIFactory.InkFaint,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor(resLab.rectTransform, new Vector2(0f, 0.5f), new Vector2(120, 26),
                new Vector2(60, 0));
            for (int i = 0; i < _resButtons.Length; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(resRow, "ResButton_" + i, "", () => ChooseResolution(idx), 100, 30);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(100, 30),
                    new Vector2(186 + i * 108, 0));
                _resButtons[i] = b;
                _resLabels[i] = b.GetComponentInChildren<Text>();
                if (_resLabels[i] != null) UIFactory.SetBaseFontSize(_resLabels[i], 14);
            }

            _resNote = UIFactory.Label(col, "resnote", "", UIFactory.TypeSmall, UIFactory.InkDim,
                TextAnchor.MiddleLeft);
            _resNote.rectTransform.sizeDelta = new Vector2(560f, 16f);
            UIFactory.Layout(_resNote.rectTransform, 560, 16);
            _displayNoteRow = UIFactory.NewUI(sheet, "displaynote");
            UIFactory.Anchor(_displayNoteRow, new Vector2(0f, 0f), new Vector2(560, 30),
                new Vector2(320, 100));
            _displayNote = UIFactory.Label(_displayNoteRow, "txt", "", 14, UIFactory.AccentBright,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor(_displayNote.rectTransform, new Vector2(0f, 0.5f), new Vector2(300, 26),
                new Vector2(150, 0));
            var keepBtn = UIFactory.StandardButton(_displayNoteRow, "keep", "KEEP",
                () => ConfirmDisplay(), 90, 28);
            UIFactory.Anchor(keepBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(90, 28), new Vector2(355, 0));
            var revertBtn = UIFactory.StandardButton(_displayNoteRow, "revert", "REVERT",
                () => CancelDisplay(), 90, 28);
            UIFactory.Anchor(revertBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(90, 28), new Vector2(455, 0));
            _displayNoteRow.gameObject.SetActive(false);

            Section(col, "ACCESSIBILITY");
            var tsRow = UIFactory.NewUI(col, "textscale");
            UIFactory.Layout(tsRow, 560, 38);
            var tsLab = UIFactory.Label(tsRow, "lab", "Text size", 17, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(tsLab.rectTransform, new Vector2(0f, 0.5f), new Vector2(180, 30),
                new Vector2(90, 0));
            string[] names = { "SMALL", "NORMAL", "LARGE" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(tsRow, "TextScaleButton_" + i, names[i], () =>
                {
                    if (_refreshing) return;
                    s.Apply(d => d.TextScale = idx);
                    RefreshTextScaleButtons();
                }, 110, 34);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(110, 34),
                    new Vector2(285 + i * 118, 0));
                _textScaleButtons[i] = b;
            }
            _toggles[0] = Toggle(col, "Reduced motion  (no shake, no lunges, instant text)",
                s.Current.ReduceMotion, v => s.Apply(d => d.ReduceMotion = v));
            _toggles[1] = Toggle(col, "Photosensitivity safe  (no strobe, softened scares)",
                s.Current.PhotosensitivitySafe, v => s.Apply(d => d.PhotosensitivitySafe = v));
            _toggles[2] = Toggle(col, "Captions for significant sounds",
                s.Current.Captions, v => s.Apply(d => d.Captions = v));

            Section(col, "DATA");
            var dataRow = UIFactory.NewUI(col, "datarow");
            UIFactory.Layout(dataRow, 560, 40);
            var resetBtn = UIFactory.StandardButton(dataRow, "resetdata", "RESET DATA…", () =>
            {
                _gameRoot.RequestDataReset(() =>
                {
                    if (_dataHint != null)
                    {
                        _dataHint.text = "Save data erased. Settings kept.";
                        _dataHint.color = UIFactory.AccentBright;
                    }
                });
            }, 200, 40, true);
            UIFactory.Anchor(resetBtn.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
                new Vector2(200, 40), new Vector2(100, 0));
            _dataHint = UIFactory.Label(dataRow, "hint",
                "Progress, records, endings. Volumes, display and accessibility are kept.",
                13, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_dataHint.rectTransform, new Vector2(0f, 0.5f), new Vector2(340, 30),
                new Vector2(380, 0));

            UIFactory.SheetButton(sheet, "controls", "CONTROLS", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_open", 0.6f);
                _gameRoot.OpenControls();
            }, 140);
            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.6f);
                _gameRoot.CloseSettings();
            }, 360);

            gameObject.SetActive(false);
        }

        private static void Section(RectTransform col, string title)
        {
            var lab = UIFactory.Label(col, "sec_" + title, title, UIFactory.TypeSection, UIFactory.InkDim,
                TextAnchor.MiddleLeft);
            lab.rectTransform.sizeDelta = new Vector2(560f, 18f);
            lab.raycastTarget = false;
            UIFactory.Layout(lab.rectTransform, 560, 18);
        }

        private Toggle Toggle(RectTransform col, string label, bool value,
            UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var t = UIFactory.ToggleRow(col, label, label, value, v =>
            {
                if (!_refreshing) onChanged(v);
            }, 560);
            UIFactory.Layout(t.GetComponent<RectTransform>(), 560, 32);
            return t;
        }

        private void ChooseResolution(int idx)
        {
            if (_refreshing || idx < 0 || idx >= _choices.Count) return;
            var m = _choices[idx];
            BeginDisplayChange(() => _gameRoot.Settings.SetDisplay(m.W, m.H, _gameRoot.Settings.Current.Fullscreen, false));
        }

        private void BeginDisplayChange(UnityEngine.Events.UnityAction change)
        {
            if (_displayBefore == null) _displayBefore = _gameRoot.Settings.Current.Clone();
            change();
            _revertLeft = DisplayPolicy.RevertSeconds;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(true);
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = StartCoroutine(RevertCountdown());
            RefreshDisplayUI();
        }

        private IEnumerator RevertCountdown()
        {
            while (_revertLeft > 0)
            {
                if (_displayNote != null)
                    _displayNote.text = "Keep " + ModeText(_gameRoot.Settings.CurrentMode()) +
                                        "?  reverts in " + _revertLeft + "s";
                yield return new WaitForSecondsRealtime(1f);
                _revertLeft--;
            }
            CancelDisplay();
        }

        private void ConfirmDisplay()
        {
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = null;
            _gameRoot.Settings.ApplyCurrentDisplay(true);
            _displayBefore = null;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(false);
            RefreshDisplayUI();
        }

        private void CancelDisplay()
        {
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = null;
            _gameRoot.Settings.RestoreDisplaySnapshot(_displayBefore);
            _displayBefore = null;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(false);
            RefreshDisplayUI();
        }

        private static string ModeText(ModeSize m) => m.W + "×" + m.H;

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
            if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;   // open at the top
            _navOrder.Clear();
            foreach (var sel in new Selectable[]
            {
                _masterSlider, _musicSlider, _sfxSlider, _fullscreenToggle,
                _toggles[0], _toggles[1], _toggles[2],
                _textScaleButtons.Length > 0 ? _textScaleButtons[0] : null,
                _textScaleButtons.Length > 1 ? _textScaleButtons[1] : null,
                _textScaleButtons.Length > 2 ? _textScaleButtons[2] : null,
            })
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
            if (_displayBefore != null) CancelDisplay();
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            _refreshing = true;
            var s = _gameRoot.Settings.Current;
            if (_masterSlider != null) _masterSlider.value = s.MasterVolume;
            if (_musicSlider != null) _musicSlider.value = s.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = s.SfxVolume;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = s.Fullscreen;
            if (_toggles[0] != null) _toggles[0].isOn = s.ReduceMotion;
            if (_toggles[1] != null) _toggles[1].isOn = s.PhotosensitivitySafe;
            if (_toggles[2] != null) _toggles[2].isOn = s.Captions;
            _refreshing = false;
            RefreshTextScaleButtons();
            RefreshDisplayUI();
        }

        private void RefreshDisplayUI()
        {
            bool fs = _gameRoot.Settings.Current.Fullscreen;
            if (_resRow != null) _resRow.SetActive(!fs);   // borderless = desktop resolution
            if (_resNote != null) _resNote.text = fs ? "Borderless fullscreen uses the desktop resolution." : "";
            _choices = _gameRoot.Settings.MenuChoices();
            var cur = _gameRoot.Settings.CurrentMode();
            var stored = new ModeSize(_gameRoot.Settings.Current.DisplayWidth, _gameRoot.Settings.Current.DisplayHeight);
            var active = stored.W > 0 && stored.H > 0 ? stored : cur;
            for (int i = 0; i < _resButtons.Length; i++)
            {
                if (_resButtons[i] == null) continue;
                bool has = i < _choices.Count;
                _resButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var m = _choices[i];
                if (_resLabels[i] != null) _resLabels[i].text = ModeText(m);
                SetSelected(_resButtons[i], m.Equals(active));
            }
        }

        private static void SetSelected(Button b, bool sel)
        {
            if (b == null) return;
            var inner = b.GetComponentInChildren<Image>();
            var label = b.GetComponentInChildren<Text>();
            if (inner != null) inner.color = sel ? UIFactory.Ink : UIFactory.PanelAlt;
            if (label != null) label.color = sel ? new Color(0.06f, 0.06f, 0.07f) : UIFactory.Ink;
        }

        private void RefreshTextScaleButtons()
        {
            if (_textScaleButtons == null) return;
            int sel = _gameRoot.Settings.Current.TextScale;
            for (int i = 0; i < _textScaleButtons.Length; i++)
            {
                var b = _textScaleButtons[i];
                if (b == null) continue;
                SetSelected(b, i == sel);
            }
        }
    }
}
