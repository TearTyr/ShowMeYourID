using System.Collections;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    /// <summary>Main menu — minimal, left-anchored, horror-restrained.
    /// Game name top-left, options as a FNAF-style vertical list below it, everything else
    /// is negative space with the hallway art breathing in it. No tagline, no logos, no
    /// locked entries: NEW GAME / CONTINUE / SETTINGS / CREDITS / EXIT only.
    /// F2 ×3 (≤1.5 s apart) erases save data behind a confirmation.</summary>
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

            // deepened negative space + breathing vignette (atmosphere, not decoration)
            _shade = UIFactory.Img(transform, "shade", ProceduralArt.Get("solid_white"), new Color(0, 0, 0, 0.28f));
            UIFactory.AnchorsFull(_shade.rectTransform);
            _shade.raycastTarget = false;
            _vignette = UIFactory.Img(transform, "vig", ProceduralArt.Get("vignette"), new Color(1, 1, 1, 0.35f));
            UIFactory.AnchorsFull(_vignette.rectTransform);
            _vignette.raycastTarget = false;

            // game name — top-left, nothing else
            var titleA = UIFactory.Label(transform, "titleA", "SHOW ME YOUR", 40, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(titleA.rectTransform, new Vector2(0f, 1f), new Vector2(760, 56), new Vector2(440, -84));
            _titleB = UIFactory.Label(transform, "titleB", "ID!", 108, UIFactory.Ink, TextAnchor.UpperLeft);
            UIFactory.Anchor(_titleB.rectTransform, new Vector2(0f, 1f), new Vector2(420, 120), new Vector2(270, -196));

            // the distant figure's eye-glint (menu_bg figure sits mid-frame, right of the list)
            _glint = UIFactory.Img(transform, "glint", ProceduralArt.Get("solid_white"), new Color(0.9f, 0.9f, 0.88f, 0f));
            UIFactory.Anchor(_glint.rectTransform, new Vector2(0.5f, 1f), new Vector2(6, 3), new Vector2(1, -325));
            _glint.raycastTarget = false;

            // options — vertical list anchored left, below the title block
            var col = UIFactory.VStack(transform, "buttons", 8, TextAnchor.UpperLeft, false, false);
            var colRt = col.GetComponent<RectTransform>();
            UIFactory.Anchor(colRt, new Vector2(0f, 1f), new Vector2(360, 258), new Vector2(270, -419));

            MenuButtonLeft(col.transform, "NEW GAME", () =>
            {
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

            // feedback line (F2 reset prompts / confirmations)
            _menuNote = UIFactory.Label(transform, "note", "", 14, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_menuNote.rectTransform, new Vector2(0f, 0f), new Vector2(900, 22), new Vector2(460, 46));

            gameObject.SetActive(false);
        }

        private void Click() => R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_click", 0.8f, null));

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

            // F2 ×3 data reset
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Time.unscaledTime - _f2Last > F2WindowSeconds) _f2Count = 0;
                _f2Count++;
                _f2Last = Time.unscaledTime;
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.22f, null));
                if (_f2Count < 3)
                {
                    ShowNote("Data reset armed — press F2 " + (3 - _f2Count) + " more time" + (3 - _f2Count == 1 ? "" : "s") + " (" + _f2Count + "/3).");
                }
                else
                {
                    _f2Count = 0;
                    R.Confirm.Ask("ERASE ALL DATA?",
                        "Deletes your save: shift progress, records and unlocked endings.\nAccessibility settings are kept.",
                        () =>
                        {
                            R.ResetAllData();
                            RefreshContinue();
                            ShowNote("All save data erased.");
                            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_static", 0.3f, null));
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.18f, null));
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_static", 0.06f, null));
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

    /// <summary>Mode selection (future use — not reachable from the v1.0.6 menu; kept so the
    /// structure accommodates additional modes later without menu rework).</summary>
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_ui_close", 0.6f, null));
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_bell", 0.7f, null));
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

    /// <summary>Options — overhauled onto the shared left-sheet look. Every control still
    /// changes real behavior and applies immediately.</summary>
    public class SettingsView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Button[] _textScaleButtons;

        public void Init(GameRoot root)
        {
            R = root;
            var sheet = UIFactory.LeftSheet(transform, "OPTIONS", "Applies immediately. Remembered on this machine.", out var col);

            var s = R.Settings;
            AddSlider(col, "Master volume", s.Current.MasterVolume, v => s.Apply(d => d.MasterVolume = v));
            AddSlider(col, "Music volume", s.Current.MusicVolume, v => s.Apply(d => d.MusicVolume = v));
            AddSlider(col, "Effects volume", s.Current.SfxVolume, v => s.Apply(d => d.SfxVolume = v));

            var tsRow = UIFactory.NewUI(col, "textscale");
            UIFactory.Layout(tsRow, 560, 40);
            var tsLab = UIFactory.Label(tsRow, "lab", "Text size", 17, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.Anchor(tsLab.rectTransform, new Vector2(0f, 0.5f), new Vector2(180, 30), new Vector2(90, 0));
            string[] names = { "SMALL", "NORMAL", "LARGE" };
            _textScaleButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = UIFactory.StandardButton(tsRow, "TextScaleButton_" + i, names[i], () =>
                {
                    s.Apply(d => d.TextScale = idx);
                    RefreshTextScaleButtons();
                }, 110, 34);
                UIFactory.Anchor(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(110, 34),
                    new Vector2(230 + i * 118, 0));
                _textScaleButtons[i] = b;
            }

            AddToggle(col, "Reduced motion  (no shake, no lunges, instant text)", s.Current.ReduceMotion,
                v => s.Apply(d => d.ReduceMotion = v));
            AddToggle(col, "Photosensitivity safe  (no strobe, softened scares)", s.Current.PhotosensitivitySafe,
                v => s.Apply(d => d.PhotosensitivitySafe = v));
            AddToggle(col, "Captions for significant sounds", s.Current.Captions,
                v => s.Apply(d => d.Captions = v));

            UIFactory.SheetButton(sheet, "controls", "CONTROLS", () =>
            {
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_ui_open", 0.6f, null));
                R.OpenControls();
            }, 140);
            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_ui_close", 0.6f, null));
                R.CloseSettings();
            }, 360);

            gameObject.SetActive(false);
        }

        private void RefreshTextScaleButtons()
        {
            if (_textScaleButtons == null) return;
            int sel = R.Settings.Current.TextScale;
            for (int i = 0; i < _textScaleButtons.Length; i++)
            {
                var b = _textScaleButtons[i];
                if (b == null) continue;
                var inner = b.GetComponentInChildren<Image>();
                if (inner != null) inner.color = i == sel ? new Color(0.30f, 0.30f, 0.33f) : UIFactory.PanelAlt;
            }
        }

        private void AddSlider(RectTransform parent, string label, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var row = UIFactory.SliderRow(parent, label, label, value, v => onChanged(v), 560);
            UIFactory.Layout(row.GetComponent<RectTransform>(), 560, 40);
        }

        private void AddToggle(RectTransform parent, string label, bool value, UnityEngine.Events.UnityAction<bool> onChanged)
        {
            var row = UIFactory.ToggleRow(parent, label, label, value, v => onChanged(v), 560);
            UIFactory.Layout(row.GetComponent<RectTransform>(), 560, 36);
        }

        public void Show() { gameObject.SetActive(true); RefreshTextScaleButtons(); }
        public void Hide() { gameObject.SetActive(false); }
    }

    /// <summary>Controls reference — same left-sheet family.</summary>
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
                "   Questions: at most 2 per visitor. Decisions are permanent.\n\n" +
                "EXPLORATION\n" +
                "   A / D  or  ◂ /         walk left / right\n" +
                "   SHIFT                  run\n" +
                "   E                      interact (doors, clues, switches)\n" +
                "   F                      flashlight (Focus light)\n" +
                "   M                      map\n" +
                "   ESC                    pause\n\n" +
                "DIALOGUE\n" +
                "   CLICK / SPACE / ENTER / E     advance\n\n" +
                "SYSTEM\n" +
                "   F2 ×3 (main menu)      erase save data";
            var txt = UIFactory.Label(sheet, "body", body, 16, UIFactory.InkDim, TextAnchor.UpperLeft);
            UIFactory.Anchor(txt.rectTransform, new Vector2(0f, 1f), new Vector2(560, 430), new Vector2(320, -360));

            UIFactory.SheetButton(sheet, "back", "◂ BACK", () =>
            {
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_ui_close", 0.6f, null));
                R.CloseControls();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

    /// <summary>Credits — same left-sheet family.</summary>
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_ui_close", 0.6f, null));
                R.CloseCredits();
            }, 140);

            gameObject.SetActive(false);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

    /// <summary>Pause — same left-sheet family (slides over whichever scene is running).</summary>
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
                R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_click", 0.7f, null));
                onClick();
            }, 420, 46, danger);
            UIFactory.Layout(b.GetComponent<RectTransform>(), 420, 46);
        }

        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
