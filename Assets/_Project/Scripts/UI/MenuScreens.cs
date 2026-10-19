using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    public class MainMenuView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Button _continueBtn;
        private Button _newGameBtn;
        private Text _titleB;
        private Text _menuNote;
        private Image _shade, _vignette, _glint;
        private Coroutine _glitch, _ambient, _glintLoop;
        private int _f2Count;
        private float _f2Last = -99f;
        private float _noteUntil = -1f;

        private const float F2WindowSeconds = 1.5f;

        public void Init(GameRoot root)
        {
            R = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("menu_bg"));
            UIFactory.AnchorsFull(bg.rectTransform);
            bg.raycastTarget = true;

            _shade = UIFactory.Img(transform, "shade", ProceduralArt.Get("solid_white"), new Color(0, 0, 0, 0.28f));
            UIFactory.AnchorsFull(_shade.rectTransform);
            _shade.raycastTarget = false;
            _vignette = UIFactory.Img(transform, "vig", ProceduralArt.Get("vignette"), new Color(1, 1, 1, 0.35f));
            UIFactory.AnchorsFull(_vignette.rectTransform);
            _vignette.raycastTarget = false;

            var titleA = UIFactory.Label(transform, "titleA", "SHOW ME YOUR", 40, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(titleA.rectTransform, new Vector2(0f, 1f), new Vector2(760, 56), new Vector2(440, -84));
            _titleB = UIFactory.Label(transform, "titleB", "ID!", 108, UIFactory.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_titleB.rectTransform, new Vector2(0f, 1f), new Vector2(420, 120), new Vector2(270, -196));

            _glint = UIFactory.Img(transform, "glint", ProceduralArt.Get("solid_white"), new Color(0.9f, 0.9f, 0.88f, 0f));
            UIFactory.Anchor(_glint.rectTransform, new Vector2(0.5f, 1f), new Vector2(6, 3), new Vector2(1, -325));
            _glint.raycastTarget = false;

            var col = UIFactory.VStack(transform, "buttons", 8, TextAnchor.UpperLeft, false, false);
            var colRt = col.GetComponent<RectTransform>();
            UIFactory.Anchor(colRt, new Vector2(0f, 1f), new Vector2(360, 258), new Vector2(270, -419));

            MenuButtonLeft(col.transform, "NEW GAME", () =>
            {
                Debug.Log("[Flow] NEW GAME clicked");
                Click();
                R.StartNewGame(GameMode.Normal);   // the only active gameplay entry
            }, b => { Emphasize(b); _newGameBtn = b; }, 360, 50);
            MenuButtonLeft(col.transform, "CONTINUE", () => { Click(); R.ContinueGame(); }, b => _continueBtn = b, 360, 44);
            MenuButtonLeft(col.transform, "SETTINGS", () => { Click(); R.OpenSettings(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "CREDITS", () => { Click(); R.OpenCredits(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "EXIT", () =>
            {
                Click();
                R.Confirm.Ask("EXIT?", "Save and quit to desktop?", () => R.QuitApplication());
            }, null, 360, 44, danger: true);

            var footer = UIFactory.Label(transform, "footer",
                "Twin Studio · v1.0.6", 13, UIFactory.InkFaint, TextAnchor.LowerLeft);
            UIFactory.Anchor(footer.rectTransform, new Vector2(0f, 0f), new Vector2(900, 24), new Vector2(460, 14));

            _menuNote = UIFactory.Label(transform, "note", "", 14, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_menuNote.rectTransform, new Vector2(0f, 0f), new Vector2(900, 22), new Vector2(460, 46));

            gameObject.SetActive(false);
        }

        private void Click() => R.Audio.PlaySfx("sfx_click", 0.8f);

        private void MenuButtonLeft(Transform parent, string label, UnityEngine.Events.UnityAction onClick,
            System.Action<Button> created, int w = 360, int h = 44, bool danger = false)
        {
            var b = UIFactory.StandardButtonLeft(parent, label, label, () => onClick(), w, h, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), w, h);
            created?.Invoke(b);
        }

        private void Emphasize(Button b)
        {
            var border = b.GetComponent<Image>();
            if (border != null) border.color = new Color(0.55f, 0.55f, 0.58f);
            var lbl = b.GetComponentInChildren<Text>();
            if (lbl != null) lbl.fontStyle = FontStyle.Bold;
        }

        private void ShowNote(string msg)
        {
            if (_menuNote == null) return;
            _menuNote.text = msg;
            _noteUntil = Time.unscaledTime + 4.5f;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshContinue();
            _menuNote.text = "";
            _noteUntil = -1f;
            if (CoreBootstrap.CreatedEventSystem != null && _newGameBtn != null)
                CoreBootstrap.CreatedEventSystem.SetSelectedGameObject(_newGameBtn.gameObject);
            if (_glitch == null) _glitch = StartCoroutine(TitleGlitchLoop());
            if (_ambient == null) _ambient = StartCoroutine(AmbientLoop());
            if (_glintLoop == null) _glintLoop = StartCoroutine(GlintLoop());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            StopLoop(ref _glitch); StopLoop(ref _ambient); StopLoop(ref _glintLoop);
        }

        private void StopLoop(ref Coroutine c) { if (c != null) { StopCoroutine(c); c = null; } }

        public void RefreshContinue()
        {
            if (_continueBtn != null) UIFactory.SetButtonEnabled(_continueBtn, R.HasSave());
        }

        private void Update()
        {
            if (R == null) return;

            if (_noteUntil > 0f && Time.unscaledTime > _noteUntil)
            {
                _noteUntil = -1f;
                if (_menuNote != null) _menuNote.text = "";
            }

            if (_vignette != null)
            {
                bool reduce = R.Settings != null && R.Settings.ReduceMotion;
                float a = reduce ? 0.38f : 0.34f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.55f);
                var c = _vignette.color; c.a = a; _vignette.color = c;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Time.unscaledTime - _f2Last > F2WindowSeconds) _f2Count = 0;
                _f2Count++;
                _f2Last = Time.unscaledTime;
                R.Audio.PlaySfx("sfx_glitch", 0.22f);
                if (_f2Count < 3)
                {
                    ShowNote("Data reset armed — press F2 " + (3 - _f2Count) + " more time" + (3 - _f2Count == 1 ? "" : "s") + " (" + _f2Count + "/3).");
                }
                else
                {
                    _f2Count = 0;
                    R.RequestDataReset(() =>
                    {
                        RefreshContinue();
                        ShowNote("All save data erased.");
                    });
                }
            }
        }

        private IEnumerator TitleGlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(12f, 22f));
                if (!gameObject.activeInHierarchy) continue;
                string original = _titleB.text;
                _titleB.text = "1D?";
                _titleB.color = UIFactory.AccentBright;
                R.Audio.PlaySfx("sfx_glitch", 0.18f);
                yield return new WaitForSecondsRealtime(0.11f);
                _titleB.text = original;
                _titleB.color = UIFactory.Ink;
            }
        }

        private IEnumerator GlintLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(7f, 15f));
                if (!gameObject.activeInHierarchy || _glint == null) continue;
                if (R.Settings != null && R.Settings.ReduceMotion) continue;
                float peak = R.Settings != null && R.Settings.PhotosensitivitySafe ? 0.45f : 0.85f;
                var c = _glint.color; c.a = peak; _glint.color = c;
                yield return new WaitForSecondsRealtime(0.14f);
                c = _glint.color; c.a = 0f; _glint.color = c;
            }
        }

        private IEnumerator AmbientLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(24f, 40f));
                if (!gameObject.activeInHierarchy) continue;
                if (R.Settings != null && (R.Settings.PhotosensitivitySafe || R.Settings.ReduceMotion)) continue;
                R.Audio.PlaySfx("sfx_static", 0.06f);
                float t = 0f;
                while (t < 0.55f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
                    var c = _shade.color; c.a = 0.28f + 0.16f * k; _shade.color = c;
                    yield return null;
                }
                var c2 = _shade.color; c2.a = 0.28f; _shade.color = c2;
            }
        }
    }

    public class ModeSelectView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Text _endlessBest, _challengeBest;

        public void Init(GameRoot root)
        {
            R = root;
            UIFactory.Dimmer(transform, "dim", 0.88f);
            var sheet = UIFactory.LeftSheet(transform, "SELECT ASSIGNMENT", "The contract, or the long night.", out var col);

            BuildCard(sheet, new Vector2(320, -180), "NORMAL — THE CONTRACT",
                "Five nights at Hollowbrook. Verify students, keep anomalies out, investigate the school.",
                () => R.StartNewGame(GameMode.Normal), out _);
            BuildCard(sheet, new Vector2(320, -330), "ENDLESS WATCH",
                "One long night at the window. Correct calls buy seconds; the clock is the only boss.",
                () => R.StartNewGame(GameMode.Endless), out _endlessBest);
            BuildCard(sheet, new Vector2(320, -480), "CHALLENGE PROTOCOL",
                "Shifting district rules, doubled sanity drain, doubled score.",
                () => R.StartNewGame(GameMode.Challenge), out _challengeBest);

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Audio.PlaySfx("sfx_ui_close", 0.6f);
                R.SetState(AppViewState.MainMenu);
            }, 140);

            gameObject.SetActive(false);
        }

        private void BuildCard(RectTransform sheet, Vector2 pos, string title, string body,
            System.Action onSelect, out Text bestLabel)
        {
            Text best = null;
            var card = UIFactory.BorderedPanel(sheet, "card", new Color(0.06f, 0.06f, 0.07f, 0.97f), UIFactory.Border, 2);
            UIFactory.Anchor(card, new Vector2(0f, 1f), new Vector2(560, 130), pos);
            var t = UIFactory.Label(card, "title", title, 19, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(520, 30), new Vector2(280, -26));
            var b = UIFactory.Label(card, "body", body, 14, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(b.rectTransform, new Vector2(0f, 1f), new Vector2(380, 70), new Vector2(210, -60));
            best = UIFactory.Label(card, "best", "", 14, new Color(0.75f, 0.62f, 0.3f), TextAnchor.MiddleRight);
            UIFactory.Anchor(best.rectTransform, new Vector2(1f, 0f), new Vector2(180, 24), new Vector2(-110, 40));
            var sel = UIFactory.StandardButton(card, "select", "BEGIN", () =>
            {
                R.Audio.PlaySfx("sfx_bell", 0.7f);
                onSelect();
            }, 140, 40);
            UIFactory.Anchor(sel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(140, 40), new Vector2(-90, 0));
            bestLabel = best;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            var rec = R.Records;
            if (_endlessBest != null) _endlessBest.text = rec != null && rec.EndlessBest > 0 ? "BEST " + rec.EndlessBest : "";
            if (_challengeBest != null) _challengeBest.text = rec != null && rec.ChallengeBest > 0 ? "BEST " + rec.ChallengeBest : "";
        }

        public void Hide() => gameObject.SetActive(false);
    }

    public class SettingsView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
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
            R = root;
            var sheet = UIFactory.LeftSheet(transform, "OPTIONS",
                "Applies immediately. Remembered on this machine.", out var col);
            col.GetComponent<VerticalLayoutGroup>().spacing = UIFactory.SpaceUnit;
            col.sizeDelta = new Vector2(560f, 505f);
            col.anchoredPosition = new Vector2(320f, -364f);

            var viewport = UIFactory.NewUI(sheet, "optionsViewport");
            UIFactory.Anchor(viewport, new Vector2(0f, 1f), new Vector2(560f, 505f), new Vector2(320f, -364f));
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

            var s = R.Settings;

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
                    var cur = R.Settings.CurrentMode();
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
                if (_resLabels[i] != null) _resLabels[i].fontSize = 14;
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
                R.RequestDataReset(() =>
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
                R.Audio.PlaySfx("sfx_ui_open", 0.6f);
                R.OpenControls();
            }, 140);
            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Audio.PlaySfx("sfx_ui_close", 0.6f);
                R.CloseSettings();
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
            BeginDisplayChange(() => R.Settings.SetDisplay(m.W, m.H, R.Settings.Current.Fullscreen, false));
        }

        private void BeginDisplayChange(UnityEngine.Events.UnityAction change)
        {
            if (_displayBefore == null) _displayBefore = R.Settings.Current.Clone();
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
                    _displayNote.text = "Keep " + ModeText(R.Settings.CurrentMode()) +
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
            R.Settings.ApplyCurrentDisplay(true);
            _displayBefore = null;
            if (_displayNoteRow != null) _displayNoteRow.gameObject.SetActive(false);
            RefreshDisplayUI();
        }

        private void CancelDisplay()
        {
            if (_revertRoutine != null) StopCoroutine(_revertRoutine);
            _revertRoutine = null;
            R.Settings.RestoreDisplaySnapshot(_displayBefore);
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
            if (!gameObject.activeInHierarchy || R == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { R.CloseSettings(); return; }
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
            var s = R.Settings.Current;
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
            bool fs = R.Settings.Current.Fullscreen;
            if (_resRow != null) _resRow.SetActive(!fs);   // borderless = desktop resolution
            if (_resNote != null) _resNote.text = fs ? "Borderless fullscreen uses the desktop resolution." : "";
            _choices = R.Settings.MenuChoices();
            var cur = R.Settings.CurrentMode();
            var stored = new ModeSize(R.Settings.Current.DisplayWidth, R.Settings.Current.DisplayHeight);
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
            int sel = R.Settings.Current.TextScale;
            for (int i = 0; i < _textScaleButtons.Length; i++)
            {
                var b = _textScaleButtons[i];
                if (b == null) continue;
                SetSelected(b, i == sel);
            }
        }
    }

    public class ControlsView : MonoBehaviour, IScreenView
    {
        private GameRoot R;

        public void Init(GameRoot root)
        {
            R = root;
            var sheet = UIFactory.LeftSheet(transform, "CONTROLS", "Mouse at the desk. Keys in the dark.", out _);

            string body =
                "CHECKPOINT (mouse)\n" +
                "   Click documents, checklist rows, SCAN, questions, ALLOW / REJECT.\n" +
                "   At most 2 questions per visitor. Decisions are permanent.\n\n" +
                "EXPLORATION\n" +
                "   A / D  or  ← / →     walk left / right\n" +
                "   SHIFT                run\n" +
                "   E                    interact (doors, clues, switches)\n" +
                "   F                    flashlight (Focus light)\n" +
                "   M                    map          ESC   pause\n\n" +
                "SECURITY ROOM (School scene prototype, mouse)\n" +
                "   Click the door panel to admit or deny late night students.\n" +
                "   Click the marked spots to check the rooms for anomalies.\n\n" +
                "DIALOGUE\n" +
                "   CLICK / SPACE / ENTER / E     advance\n\n" +
                "SYSTEM\n" +
                "   F2 ×3 on the main menu, or OPTIONS → RESET DATA\n" +
                "      erases save data (settings are kept)";
            var txt = UIFactory.Label(sheet, "body", body, 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(txt.rectTransform, new Vector2(0f, 1f), new Vector2(560, 430), new Vector2(320, -360));

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Audio.PlaySfx("sfx_ui_close", 0.6f);
                R.CloseControls();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

    public class CreditsView : MonoBehaviour, IScreenView
    {
        private GameRoot R;

        public void Init(GameRoot root)
        {
            R = root;
            var sheet = UIFactory.LeftSheet(transform, "CREDITS", "A Twin Studio game.", out _);

            string body =
                "Project Director · Narrative — Jessie Mariel De Mesa\n" +
                "Project Assistants — Elijah Japeth Ramos · Clarence Manalo\n" +
                "Programming — Elijah Japeth Ramos · John Perilla\n" +
                "Art — Daniel Steven Solitaria · Jessie Mariel De Mesa\n" +
                "Audio — Clarence Manalo\n" +
                "Marketing & Branding — Diosdado Fabelico II · Daniella Garing\n" +
                "Storyboard — All members\n\n" +
                "This build generates all art and audio procedurally at runtime\n" +
                "(monochrome silhouettes, documents, and an E♭-minor score).\n" +
                "Drop real assets into Resources/Art and Resources/Audio to replace any of them.";
            var txt = UIFactory.Label(sheet, "body", body, 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(txt.rectTransform, new Vector2(0f, 1f), new Vector2(560, 430), new Vector2(320, -360));

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Audio.PlaySfx("sfx_ui_close", 0.6f);
                R.CloseCredits();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

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
