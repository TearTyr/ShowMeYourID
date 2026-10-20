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
    // The left-column main menu, F2 reset and its ambience. Created by MenuSceneBootstrap.
    public class MainMenuView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
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
            _gameRoot = root;
            var bg = UIFactory.Img(transform, "bg", ProceduralArt.Get("menu_bg"));
            bg.color = Color.black;   // showcase build: black backdrop behind the menu
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
                _gameRoot.StartNewGame(GameMode.Normal);   // the only active gameplay entry
            }, b => { Emphasize(b); _newGameBtn = b; }, 360, 50);
            MenuButtonLeft(col.transform, "CONTINUE", () => { Click(); _gameRoot.ContinueGame(); }, b => _continueBtn = b, 360, 44);
            MenuButtonLeft(col.transform, "SETTINGS", () => { Click(); _gameRoot.OpenSettings(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "CREDITS", () => { Click(); _gameRoot.OpenCredits(); }, null, 360, 44);
            MenuButtonLeft(col.transform, "EXIT", () =>
            {
                Click();
                _gameRoot.Confirm.Ask("EXIT?", "Save and quit to desktop?", () => _gameRoot.QuitApplication());
            }, null, 360, 44, danger: true);

            var footer = UIFactory.Label(transform, "footer",
                "Twin Studio · v1.0.6", 13, UIFactory.InkFaint, TextAnchor.LowerLeft);
            UIFactory.Anchor(footer.rectTransform, new Vector2(0f, 0f), new Vector2(900, 24), new Vector2(460, 14));

            _menuNote = UIFactory.Label(transform, "note", "", 14, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_menuNote.rectTransform, new Vector2(0f, 0f), new Vector2(900, 22), new Vector2(460, 46));

            gameObject.SetActive(false);
        }

        private void Click() => _gameRoot.Audio.PlaySfx("sfx_click", 0.8f);

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
            if (_continueBtn != null) UIFactory.SetButtonEnabled(_continueBtn, _gameRoot.HasSave());
        }

        private void Update()
        {
            if (_gameRoot == null) return;

            if (_noteUntil > 0f && Time.unscaledTime > _noteUntil)
            {
                _noteUntil = -1f;
                if (_menuNote != null) _menuNote.text = "";
            }

            if (_vignette != null)
            {
                bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
                float a = reduce ? 0.38f : 0.34f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.55f);
                var c = _vignette.color; c.a = a; _vignette.color = c;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (Time.unscaledTime - _f2Last > F2WindowSeconds) _f2Count = 0;
                _f2Count++;
                _f2Last = Time.unscaledTime;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.22f);
                if (_f2Count < 3)
                {
                    ShowNote("Data reset armed — press F2 " + (3 - _f2Count) + " more time" + (3 - _f2Count == 1 ? "" : "s") + " (" + _f2Count + "/3).");
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

        private IEnumerator TitleGlitchLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(12f, 22f));
                if (!gameObject.activeInHierarchy) continue;
                string original = _titleB.text;
                _titleB.text = "1D?";
                _titleB.color = UIFactory.AccentBright;
                _gameRoot.Audio.PlaySfx("sfx_glitch", 0.18f);
                yield return new WaitForSecondsRealtime(0.11f);
                _titleB.text = original;
                _titleB.color = UIFactory.Ink;
            }
        }

        private IEnumerator GlintLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(7f, 15f));
                if (!gameObject.activeInHierarchy || _glint == null) continue;
                if (_gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion) continue;
                float peak = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe ? 0.45f : 0.85f;
                var c = _glint.color; c.a = peak; _glint.color = c;
                yield return new WaitForSecondsRealtime(0.14f);
                c = _glint.color; c.a = 0f; _glint.color = c;
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
                    var c = _shade.color; c.a = 0.28f + 0.16f * k; _shade.color = c;
                    yield return null;
                }
                var c2 = _shade.color; c2.a = 0.28f; _shade.color = c2;
            }
        }
    }
}
