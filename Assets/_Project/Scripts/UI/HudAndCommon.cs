using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.UI
{
    /// <summary>Gameplay HUD (approved list): sanity bar, current time, objective display,
    /// interaction indicator, notification toasts, findings counter, room/light readout.
    /// Lives above the FX canvas so essential info is always readable (sanity effects can
    /// never obscure it).</summary>
    public class HUDView : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Image _sanityFill;
        private Text _sanityLabel;
        private Text _objective;
        private Text _prompt;
        private Text _toast;
        private Text _findings;
        private Text _roomInfo;
        private float _toastUntil;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root)
        {
            R = root;

            // sanity bar (top-left)
            var barBg = UIFactory.Panel(transform, "sanitybg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(barBg, new Vector2(0, 1), new Vector2(240, 46), new Vector2(132, -86));
            var label = UIFactory.Label(barBg, "cap", "SANITY", 13, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(80, 18), new Vector2(46, -10));
            var track = UIFactory.Panel(barBg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(track, new Vector2(0, 0), new Vector2(216, 14), new Vector2(120, 12));
            _sanityFill = UIFactory.Img(track, "fill", ProceduralArt.Get("solid_white"), UIFactory.Ink);
            _sanityFill.rectTransform.anchorMin = new Vector2(0, 0);
            _sanityFill.rectTransform.anchorMax = new Vector2(1, 1);
            _sanityFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _sanityFill.rectTransform.offsetMin = Vector2.zero;
            _sanityFill.rectTransform.offsetMax = Vector2.zero;
            _sanityLabel = UIFactory.Label(barBg, "val", "100", 13, UIFactory.Ink, TextAnchor.MiddleRight);
            UIFactory.Anchor(_sanityLabel.rectTransform, new Vector2(1, 1), new Vector2(48, 18), new Vector2(-30, -10));

            // findings counter (top-right under the clock)
            _findings = UIFactory.Label(transform, "findings", "FINDINGS 0/10", 15, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_findings.rectTransform, new Vector2(1, 1), new Vector2(240, 24), new Vector2(-150, -64));

            // room + light info (right, under findings)
            _roomInfo = UIFactory.Label(transform, "room", "", 14, UIFactory.InkFaint, TextAnchor.MiddleRight);
            UIFactory.Anchor(_roomInfo.rectTransform, new Vector2(1, 1), new Vector2(300, 22), new Vector2(-180, -88));

            // objective (bottom-left)
            _objective = UIFactory.Label(transform, "objective", "", 15, UIFactory.InkDim);
            var ort = _objective.rectTransform;
            ort.anchorMin = new Vector2(0, 0); ort.anchorMax = new Vector2(0, 0);
            ort.pivot = new Vector2(0, 0);
            ort.sizeDelta = new Vector2(560, 66);
            ort.anchoredPosition = new Vector2(20, 16);

            // interaction prompt (bottom-center)
            _prompt = UIFactory.Label(transform, "prompt", "", 20, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(640, 30), new Vector2(0, 92));

            // toast (top-center)
            _toast = UIFactory.Label(transform, "toast", "", 18, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_toast.rectTransform, new Vector2(0.5f, 1), new Vector2(720, 30), new Vector2(0, -64));
            _toast.gameObject.SetActive(false);

            _subs.Add(root.Bus.Subscribe(EventIds.SanityChanged, e => SetSanity(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.SanityStageChanged, e =>
            {
                if (e.Data is SanityStage st && st == SanityStage.Breaking)
                    Notify("YOUR GRIP IS SLIPPING — GET BACK TO THE BOOTH");
            }));
            _subs.Add(root.Bus.Subscribe(EventIds.ObjectiveChanged, e => _objective.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.InteractionPrompt, e => _prompt.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.Notification, e => Notify(e.Text)));
            _subs.Add(root.Bus.Subscribe(EventIds.ClueDiscovered, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.RoomEntered, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.LightModeChanged, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.StateChanged, e => OnState(e.Payload<AppViewState>())));

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        private void OnState(AppViewState s)
        {
            bool exploring = s == AppViewState.Exploration;
            _prompt.gameObject.SetActive(true);
            _roomInfo.gameObject.SetActive(exploring);
            if (!exploring) _prompt.text = "";
            if (s == AppViewState.Checkpoint) _objective.text = "Inspect every visitor. Permanent decisions.";
            RefreshFindings();
            SetSanity(R.Session != null ? R.Session.Sanity : 100f);
        }

        public void SetSanity(float v)
        {
            float k = Mathf.Clamp01(v / SanityModel.Max);
            var rt = _sanityFill.rectTransform;
            rt.anchorMax = new Vector2(k, 1);
            var stage = SanityModel.StageFor(v);
            _sanityFill.color = stage == SanityStage.Breaking ? UIFactory.AccentBright :
                                stage == SanityStage.Shaken ? new Color(0.75f, 0.45f, 0.2f) :
                                stage == SanityStage.Uneasy ? UIFactory.InkDim : UIFactory.Ink;
            _sanityLabel.text = ((int)v).ToString();
        }

        public void RefreshFindings()
        {
            if (R.Session == null) return;
            _findings.text = "FINDINGS " + R.Session.Clues.FoundCount + "/10" +
                             (R.Session.Clues.OrderIntact ? "" : "  (order broken)");
        }

        public void RefreshRoom()
        {
            if (R.Views == null || R.Views.Current == null) { _roomInfo.text = ""; return; }
            var room = R.Views.Current;
            string light = room.Light != null ? room.Light.Mode.ToString().ToUpperInvariant() : "—";
            _roomInfo.text = room.DisplayName + "  ·  LIGHTS: " + light + "  ·  [F] flashlight";
        }

        public void Notify(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            _toast.text = msg;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 3.2f;
        }

        private void Update()
        {
            if (_toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (R != null && R.Session != null) { SetSanity(R.Session.Sanity); RefreshFindings(); RefreshRoom(); }
        }

        public void Show() { gameObject.SetActive(true); Refresh(); }
        public void Hide() { gameObject.SetActive(false); }
    }

    /// <summary>Accessibility caption bar: shows text for significant audio events when
    /// Captions are enabled in settings.</summary>
    public class CaptionBar : MonoBehaviour, IScreenView
    {
        private GameRoot R;
        private Text _text;
        private GameObject _bg;
        private float _until;
        private EventSubscription _sub;

        public void Init(GameRoot root)
        {
            R = root;
            _bg = UIFactory.Panel(transform, "bg", new Color(0, 0, 0, 0.72f)).gameObject;
            _bg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; // display only, never blocks buttons
            UIFactory.Anchor(_bg.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(880, 34), new Vector2(0, 54));
            _text = UIFactory.Label(_bg.transform, "t", "", 16, new Color(0.85f, 0.85f, 0.8f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_text.rectTransform);
            _bg.SetActive(false);
            _sub = root.Bus.Subscribe(EventIds.CaptionRequested, e =>
            {
                if (R.Settings == null || !R.Settings.Captions) return;
                _text.text = e.Text ?? "";
                _until = Time.unscaledTime + (e.Value > 0 ? e.Value : 2.5f);
                _bg.SetActive(true);
            });
        }

        private void Update()
        {
            if (_bg.activeSelf && Time.unscaledTime > _until) _bg.SetActive(false);
        }

        private void OnDestroy() { _sub?.Dispose(); }
        public void Show() { gameObject.SetActive(true); }
        public void Hide() { gameObject.SetActive(false); }
    }

    /// <summary>Loading / shift-title flash screen.</summary>
    public class LoadingView : MonoBehaviour, IScreenView
    {
        private Text _label;
        private GameObject _bg;
        private Coroutine _routine;

        public void Init(GameRoot root)
        {
            _bg = UIFactory.Panel(transform, "bg", Color.black).gameObject;
            UIFactory.AnchorsFull(_bg.GetComponent<RectTransform>());
            _label = UIFactory.Label(_bg.transform, "label", "", 40, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 80), new Vector2(0, 30));
            var sub = UIFactory.Label(_bg.transform, "sub", "HOLLOWBROOK COLLEGE — NIGHT CUSTODIAN PROGRAM", 15, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            UIFactory.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900, 30), new Vector2(0, -30));
            _bg.SetActive(false);
        }

        public void Flash(string label)
        {
            _label.text = label ?? "";
            _bg.SetActive(true);
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(HideAfter(1.35f));
        }

        private IEnumerator HideAfter(float s)
        {
            yield return new WaitForSecondsRealtime(s);
            _bg.SetActive(false);
            _routine = null;
        }

        /// <summary>Persistent backdrop with a title (used while shift intros play over it).</summary>
        public void Show(string label)
        {
            _label.text = label ?? "";
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(true);
        }

        public void Show() { _bg.SetActive(true); }
        public void Hide()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            _bg.SetActive(false);
        }
    }

    /// <summary>Modal yes/no confirmation (used for quitting, destructive choices).</summary>
    public class ConfirmationPopup : MonoBehaviour, IScreenView
    {
        private GameObject _root;
        private Text _title, _body;
        private System.Action _onYes;
        private GameRoot R;

        public void Init(GameRoot root)
        {
            R = root;
            _root = UIFactory.Panel(transform, "panel", new Color(0, 0, 0, 0.55f)).gameObject;
            UIFactory.AnchorsFull(_root.GetComponent<RectTransform>());
            var panel = UIFactory.BorderedPanel(_root.transform, "box", new Color(0.07f, 0.07f, 0.08f, 0.98f), UIFactory.Border);
            UIFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(560, 240), Vector2.zero);
            _title = UIFactory.Label(panel, "title", "", 24, UIFactory.Ink, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_title.rectTransform, new Vector2(0.5f, 1), new Vector2(520, 36), new Vector2(0, -34));
            _body = UIFactory.Label(panel, "body", "", 16, UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_body.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 70), new Vector2(0, -92));
            var yes = UIFactory.StandardButton(panel, "yes", "YES", () =>
            {
                // capture BEFORE Hide() — Hide() clears _onYes (order bug caught by smoke tests:
                // without this, every confirmation in the game silently did nothing)
                var act = _onYes;
                Hide();
                act?.Invoke();
            }, 160, 44, true);
            UIFactory.Anchor(yes.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(-100, 40));
            var no = UIFactory.StandardButton(panel, "no", "NO", Hide, 160, 44);
            UIFactory.Anchor(no.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(160, 44), new Vector2(100, 40));
            _root.SetActive(false);
        }

        public void Ask(string title, string body, System.Action onYes)
        {
            _title.text = title;
            _body.text = body;
            _onYes = onYes;
            _root.SetActive(true);
        }

        public void Show() { }
        public void Hide() { _root.SetActive(false); _onYes = null; }
    }
}
