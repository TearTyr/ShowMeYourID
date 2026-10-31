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
    // The main menu, F2 reset and its ambience. Created by MenuSceneBootstrap.
    //
    // Visual spec: Art/MainMenu/reference/main-menu-reference.png (2880x1620). The whole menu
    // is built from the owner's kit (Assets/_Project/Resources/Art/MainMenu/menu_*.png, cut by
    // Tools/prep_mainmenu_art.py): the corridor backdrop, the "SHOW ME YOUR ID!" logo plate,
    // the two outlined wide buttons (NEW GAME / CONTINUE) and the three outlined icon squares
    // (SETTINGS / EXIT / CREDITS). Every rect below is the reference composite measured in
    // 1280x720 canvas units (reference px / 2.25), so the menu lands pixel-on the artwork at
    // the intended resolution and stays edge-anchored (top-left column, top-right logo,
    // bottom-left icon row) everywhere else.
    public class MainMenuView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Button _continueBtn;
        private Button _newGameBtn;
        private Image _shade, _bg, _logo;
        private Coroutine _glitch, _ambient;
        private int _f2Count;
        private float _f2Last = -99f;
        private float _noteUntil = -1f;
        private Vector2 _bgFittedFor = Vector2.zero;

        private const float F2WindowSeconds = 1.5f;

        // ---- reference geometry (1280x720 canvas units; reference px / 2.25) ----
        private const float WideW = 434.22f, WideH = 87.56f;          // 977x197 frame
        private const float WideX = 49.33f;                           // left edge of the column
        private const float Wide1Y = 68.44f;                          // NEW GAME top
        private const float Wide2Y = 179.56f;                         // CONTINUE top
        private const float SqW = 71.11f, SqH = 72f;                  // 160x162 frame
        private const float SqY = 591.56f;                            // icon row top
        private const float Sq1X = 48.44f, Sq2X = 134.67f, Sq3X = 218.67f;
        private const float LogoW = 543.11f, LogoH = 350.22f;         // 1222x788 plate
        private const float LogoRight = 322.22f;                      // centre offset from right
        private const float LogoTop = 223.11f;                        // centre offset from top
        private const float IconPadLeft = 29.8f;                      // icon left pad in wide btn
        private const float LabelPadLeft = 91.1f;                       // label left pad in wide btn
        private const int LabelSize = 46;                             // chiller cap band ≈ ref raster
        private const float LabelSqueeze = 1.042f;                    // match the kit's tracking
        private const float BgAspect = 16f / 9f;                      // backdrop native aspect

        public void Init(GameRoot root)
        {
            _gameRoot = root;

            // corridor backdrop, cover-cropped (FitBackground) so it never stretches
            _bg = UIFactory.Img(transform, "bg", Kit("menu_background"));
            _bg.raycastTarget = true;
            var bgRt = _bg.rectTransform;
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            FitBackground();

            // ambience: the rare static flicker only. The corridor art ships with its own
            // vignette and grain baked in - an extra wash layer only dulls the reference.
            _shade = UIFactory.Img(transform, "shade", ProceduralArt.Get("solid_white"), new Color(0, 0, 0, 0f));
            UIFactory.AnchorsFull(_shade.rectTransform);
            _shade.raycastTarget = false;

            // logo plate, top right
            _logo = UIFactory.Img(transform, "logo", Kit("menu_logo"));
            UIFactory.Anchor(_logo.rectTransform, new Vector2(1f, 1f), new Vector2(LogoW, LogoH),
                new Vector2(-LogoRight, -LogoTop));
            _logo.raycastTarget = false;

            // every button lives under one named column so headless proofs (and keyboard
            // order) keep the old addresses: buttons/NEW GAME ... buttons/EXIT
            var col = UIFactory.NewUI(transform, "buttons");
            UIFactory.AnchorsFull(col);

            WideButton(col, "NEW GAME", "NEW GAME", "menu_icon_newgame",
                new Vector2(44.89f, 52f), new Vector2(0f, 0.9f), () =>
                {
                    Debug.Log("[Flow] NEW GAME clicked");
                    Click();
                    _gameRoot.StartNewGame(GameMode.Normal);   // the only active gameplay entry
                }, b => _newGameBtn = b, WideX, Wide1Y);
            WideButton(col, "CONTINUE", "CONTINUE", "menu_icon_continue",
                new Vector2(45.33f, 45.33f), Vector2.zero, () => { Click(); _gameRoot.ContinueGame(); },
                b => _continueBtn = b, WideX, Wide2Y);
            SquareButton(col, "SETTINGS", "menu_icon_settings", new Vector2(49.33f, 49.33f),
                new Vector2(0.9f, 0f), () => { Click(); _gameRoot.OpenSettings(); }, Sq1X, SqY);
            SquareButton(col, "EXIT", "menu_icon_exit", new Vector2(49.33f, 49.33f),
                new Vector2(1.56f, 0f), () =>
                {
                    Click();
                    _gameRoot.Confirm.Ask("EXIT?", "Quit to desktop?", () => _gameRoot.QuitApplication());
                }, Sq2X, SqY);
            SquareButton(col, "CREDITS", "menu_icon_credits", new Vector2(49.33f, 33.78f),
                new Vector2(0f, 1.78f), () => { Click(); _gameRoot.OpenCredits(); }, Sq3X, SqY);

            // transient messages (F2 reset arming / save erased) - empty means invisible
            _menuNote = UIFactory.Label(transform, "note", "", 14, new Color(1f, 1f, 1f, 0.75f),
                TextAnchor.MiddleLeft);
            var noteRt = _menuNote.rectTransform;
            noteRt.anchorMin = new Vector2(0f, 0f);
            noteRt.anchorMax = new Vector2(0f, 0f);
            noteRt.pivot = new Vector2(0f, 0f);
            noteRt.sizeDelta = new Vector2(760f, 22f);
            noteRt.anchoredPosition = new Vector2(Sq1X, 720f - SqY + 6f);
            _menuNote.raycastTarget = false;

            gameObject.SetActive(false);
        }

        private Text _menuNote;

        private static Sprite Kit(string key) => ProceduralArt.Get(key);

        // ---- button builders -------------------------------------------------
        private Button WideButton(Transform parent, string name, string label, string iconKey,
            Vector2 iconSize, Vector2 iconOffset, Action onClick, Action<Button> created, float x, float y)
        {
            var b = BuildButton(parent, name, "menu_frame_wide", new Vector2(WideW, WideH),
                new Vector2(x + WideW / 2f, -(y + WideH / 2f)), onClick, created);
            var rt = b.GetComponent<RectTransform>();

            var icon = UIFactory.Img(rt, "icon", Kit(iconKey));
            UIFactory.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), iconSize,
                new Vector2(IconPadLeft + iconSize.x / 2f, iconOffset.y));
            icon.raycastTarget = false;

            var txt = UIFactory.Label(rt, "label", label, LabelSize, UIFactory.Ink, TextAnchor.MiddleLeft);
            txt.font = UIFactory.TypeFont;   // the kit's horror display face (Chiller)
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            var lrt = txt.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(WideW - LabelPadLeft - 6f, WideH);
            lrt.anchoredPosition = new Vector2(LabelPadLeft, -2.67f);
            lrt.localScale = new Vector3(LabelSqueeze, 1f, 1f);   // the kit's condensed tracking
            txt.raycastTarget = false;
            return b;
        }

        private Button SquareButton(Transform parent, string name, string iconKey, Vector2 iconSize,
            Vector2 iconOffset, Action onClick, float x, float y)
        {
            var b = BuildButton(parent, name, "menu_frame_square", new Vector2(SqW, SqH),
                new Vector2(x + SqW / 2f, -(y + SqH / 2f)), onClick, null);
            var rt = b.GetComponent<RectTransform>();
            var icon = UIFactory.Img(rt, "icon", Kit(iconKey));
            UIFactory.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), iconSize, iconOffset);
            icon.raycastTarget = false;
            return b;
        }

        private Button BuildButton(Transform parent, string name, string frameKey, Vector2 size,
            Vector2 centre, Action onClick, Action<Button> created)
        {
            var rt = UIFactory.NewUI(parent, name);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = centre;

            // hover/press wash sits BEHIND the outline so the frame stays crisp white
            var fill = UIFactory.Img(rt, "fill", ProceduralArt.Get("solid_white"), new Color(1f, 1f, 1f, 0f));
            var frt = fill.rectTransform;
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(-3f, -3f);
            fill.raycastTarget = false;

            var frame = rt.gameObject.AddComponent<Image>();
            frame.sprite = Kit(frameKey);
            frame.color = Color.white;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = frame;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.pressedColor = Color.white;
            cb.selectedColor = Color.white;
            cb.disabledColor = Color.white;   // dimming happens through the CanvasGroup
            cb.fadeDuration = 0.1f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            Skin(btn, fill);
            created?.Invoke(btn);
            return btn;
        }

        // hover / pressed / keyboard-focus wash; a disabled button never reacts
        private static void Skin(Button btn, Image fill)
        {
            bool hover = false, sel = false, down = false;
            Action apply = () =>
            {
                float a = !btn.interactable ? 0f : down ? 0.22f : hover ? 0.12f : sel ? 0.08f : 0f;
                var c = fill.color; c.a = a; fill.color = c;
            };
            var et = btn.gameObject.AddComponent<EventTrigger>();
            void On(EventTriggerType t, Action fn)
            {
                var e = new EventTrigger.Entry { eventID = t };
                e.callback.AddListener(_ => { fn(); apply(); });
                et.triggers.Add(e);
            }
            On(EventTriggerType.PointerEnter, () => hover = true);
            On(EventTriggerType.PointerExit, () => { hover = false; down = false; });
            On(EventTriggerType.PointerDown, () => down = true);
            On(EventTriggerType.PointerUp, () => down = false);
            On(EventTriggerType.Select, () => sel = true);
            On(EventTriggerType.Deselect, () => { sel = false; hover = false; down = false; });
        }

        // ---- backdrop cover-crop ----------------------------------------------
        private void FitBackground()
        {
            if (_bg == null) return;
            var vis = UIFactory.VisibleReferenceSize(transform as RectTransform);
            if (vis.x <= 0f || vis.y <= 0f) vis = new Vector2(1280f, 720f);
            if (vis == _bgFittedFor) return;
            _bgFittedFor = vis;
            float w, h;
            if (vis.x / vis.y > BgAspect) { w = vis.x; h = vis.x / BgAspect; }
            else { h = vis.y; w = vis.y * BgAspect; }
            _bg.rectTransform.sizeDelta = new Vector2(w, h);
        }

        private void Click() => _gameRoot.Audio.PlaySfx("sfx_click", 0.8f);

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
            FitBackground();
            if (CoreBootstrap.CreatedEventSystem != null && _newGameBtn != null)
                CoreBootstrap.CreatedEventSystem.SetSelectedGameObject(_newGameBtn.gameObject);
            if (_glitch == null) _glitch = StartCoroutine(LogoGlitchLoop());
            if (_ambient == null) _ambient = StartCoroutine(AmbientLoop());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            StopLoop(ref _glitch); StopLoop(ref _ambient);
        }

        private void StopLoop(ref Coroutine c) { if (c != null) { StopCoroutine(c); c = null; } }

        public void RefreshContinue()
        {
            if (_continueBtn != null) UIFactory.SetButtonEnabled(_continueBtn, _gameRoot.HasSave());
        }

        private void Update()
        {
            if (_gameRoot == null) return;
            FitBackground();

            if (_noteUntil > 0f && Time.unscaledTime > _noteUntil)
            {
                _noteUntil = -1f;
                if (_menuNote != null) _menuNote.text = "";
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Time.unscaledTime - _f2Last > F2WindowSeconds) _f2Count = 0;
                _f2Count++;
                _f2Last = Time.unscaledTime;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.22f);
                if (_f2Count < 3)
                {
                    ShowNote("Data reset armed - press F2 " + (3 - _f2Count) + " more time" + (3 - _f2Count == 1 ? "" : "s") + " (" + _f2Count + "/3).");
                }
                else
                {
                    _f2Count = 0;
                    _gameRoot.RequestDataReset(() =>
                    {
                        RefreshContinue();
                        ShowNote("All save data erased.");
                    });
                }
            }
        }

        // the old text title glitched "ID!" into "1D?"; the logo plate carries the same beat
        // as a brief red flash so the menu keeps its signature scare without redrawing art.
        private IEnumerator LogoGlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(12f, 22f));
                if (!gameObject.activeInHierarchy || _logo == null) continue;
                if (_gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe) continue;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.18f);
                _logo.color = UIFactory.AccentBright;
                yield return new WaitForSecondsRealtime(0.11f);
                _logo.color = Color.white;
            }
        }

        private IEnumerator AmbientLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(24f, 40f));
                if (!gameObject.activeInHierarchy) continue;
                if (_gameRoot.Settings != null && (_gameRoot.Settings.PhotosensitivitySafe || _gameRoot.Settings.ReduceMotion)) continue;
                _gameRoot.Audio.PlaySfx("sfx_static", 0.06f);
                float t = 0f;
                while (t < 0.55f)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI);
                    var c = _shade.color; c.a = 0.12f * k; _shade.color = c;
                    yield return null;
                }
                var c2 = _shade.color; c2.a = 0f; _shade.color = c2;
            }
        }
    }
}
