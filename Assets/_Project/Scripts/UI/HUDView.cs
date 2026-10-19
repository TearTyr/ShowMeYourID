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
    // Sanity, light battery, tracking meter, findings, prompts. Created by each scene bootstrap.
    public class HUDView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private Image _sanityFill;
        private Text _sanityLabel;
        private Text _objective;
        private Text _prompt;
        private Text _toast;
        private Text _findings;
        private Text _roomInfo;
        private GameObject _trackBar;
        private Image _trackFill;
        private Text _stageLabel;
        private GameObject _batteryBar;
        private Image _batteryFill;
        private float _toastUntil;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;

            var barBg = UIFactory.Panel(transform, "sanitybg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(barBg, new Vector2(0.5f, 1f), new Vector2(260, 36), new Vector2(-160, -26));
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
            _stageLabel = UIFactory.Label(barBg, "stage", "STABLE", 11, UIFactory.InkFaint, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_stageLabel.rectTransform, new Vector2(0, 1), new Vector2(150, 16), new Vector2(120, -28));

            var tbg = UIFactory.Panel(transform, "trackbg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(tbg, new Vector2(0.5f, 1f), new Vector2(220, 36), new Vector2(90, -26));
            var tcap = UIFactory.Label(tbg, "cap", "TRACK", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(tcap.rectTransform, new Vector2(0, 1), new Vector2(80, 16), new Vector2(46, -7));
            var ttrack = UIFactory.Panel(tbg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(ttrack, new Vector2(0, 0), new Vector2(216, 10), new Vector2(120, 9));
            _trackFill = UIFactory.Img(ttrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.AccentBright);
            _trackFill.rectTransform.anchorMin = new Vector2(0, 0);
            _trackFill.rectTransform.anchorMax = new Vector2(0, 1);
            _trackFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _trackFill.rectTransform.offsetMin = Vector2.zero;
            _trackFill.rectTransform.offsetMax = Vector2.zero;
            _trackBar = tbg.gameObject;
            _trackBar.SetActive(false);

            var bbg = UIFactory.Panel(transform, "batterybg", new Color(0.05f, 0.05f, 0.06f, 0.85f)).rectTransform;
            UIFactory.Anchor(bbg, new Vector2(0.5f, 1f), new Vector2(220, 36), new Vector2(300, -26));
            var bcap = UIFactory.Label(bbg, "cap", "LIGHT", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            UIFactory.Anchor(bcap.rectTransform, new Vector2(0, 1), new Vector2(80, 16), new Vector2(46, -5));
            var btrack = UIFactory.Panel(bbg, "track", new Color(0.12f, 0.12f, 0.13f)).rectTransform;
            UIFactory.Anchor(btrack, new Vector2(0, 0), new Vector2(216, 8), new Vector2(120, 9));
            _batteryFill = UIFactory.Img(btrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.InkDim);
            _batteryFill.rectTransform.anchorMin = new Vector2(0, 0);
            _batteryFill.rectTransform.anchorMax = new Vector2(1, 1);
            _batteryFill.rectTransform.pivot = new Vector2(0, 0.5f);
            _batteryFill.rectTransform.offsetMin = Vector2.zero;
            _batteryFill.rectTransform.offsetMax = Vector2.zero;
            _batteryBar = bbg.gameObject;

            _findings = UIFactory.Label(transform, "findings", "FINDINGS 0/10", 15, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.Anchor(_findings.rectTransform, new Vector2(1, 1), new Vector2(240, 24), new Vector2(-150, -64));

            _roomInfo = UIFactory.Label(transform, "room", "", 14, UIFactory.InkFaint, TextAnchor.MiddleRight);
            UIFactory.Anchor(_roomInfo.rectTransform, new Vector2(1, 1), new Vector2(300, 22), new Vector2(-180, -88));

            _objective = UIFactory.Label(transform, "objective", "", 15, UIFactory.InkDim);
            var ort = _objective.rectTransform;
            ort.anchorMin = new Vector2(0, 0); ort.anchorMax = new Vector2(0, 0);
            ort.pivot = new Vector2(0, 0);
            ort.sizeDelta = new Vector2(560, 66);
            ort.anchoredPosition = new Vector2(20, 16);

            _prompt = UIFactory.Label(transform, "prompt", "", 20, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(640, 30), new Vector2(0, 92));

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
            _subs.Add(root.Bus.Subscribe(EventIds.TrackingChanged, e => SetTracking(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.AnomalyRevealed, e => SetTracking(0f)));
            _subs.Add(root.Bus.Subscribe(EventIds.TokenCollected, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.BatteryChanged, e => SetBattery(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.RoomEntered, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.LightModeChanged, e => RefreshRoom()));
            _objective.text = root.ObjectiveText ?? "";   // HUD may be built after the publish
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
            SetSanity(_gameRoot.Session != null ? _gameRoot.Session.Sanity : 100f);
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
            if (_stageLabel != null)
            {
                _stageLabel.text = stage.ToString().ToUpperInvariant();
                _stageLabel.color = stage == SanityStage.Breaking ? UIFactory.AccentBright :
                                    stage == SanityStage.Shaken ? new Color(0.75f, 0.45f, 0.2f) : UIFactory.InkFaint;
            }
        }

        private void SetBattery(float v)
        {
            if (_batteryBar == null) return;
            _batteryBar.SetActive(true);
            if (_batteryFill != null)
            {
                _batteryFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
                _batteryFill.color = v <= 0.2f ? UIFactory.AccentBright : UIFactory.InkDim;
            }
        }

        private void SetTracking(float v)
        {
            if (_trackBar == null) return;
            bool on = v > 0.001f && v < 0.999f;
            _trackBar.SetActive(on);
            if (on && _trackFill != null)
                _trackFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        }

        public void RefreshFindings()
        {
            if (_gameRoot.Session == null) return;
            _findings.text = "FINDINGS " + _gameRoot.Session.Clues.FoundCount + "/10" +
                             (_gameRoot.Session.Clues.OrderIntact ? "" : "  (order broken)") +
                             (_gameRoot.Session.TokensCollected > 0 ? "  ·  TOKENS " + _gameRoot.Session.TokensCollected : "");
        }

        public void RefreshRoom()
        {
            if (_gameRoot.Views == null || _gameRoot.Views.Current == null) { _roomInfo.text = ""; return; }
            var room = _gameRoot.Views.Current;
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
            if (_gameRoot != null && _gameRoot.Session != null) { SetSanity(_gameRoot.Session.Sanity); RefreshFindings(); RefreshRoom(); }
        }

        public void Show() { gameObject.SetActive(true); Refresh(); }
        public void Hide() { gameObject.SetActive(false); }
    }
}
