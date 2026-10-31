using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.UI
{
    // Sanity, light battery, tracking meter, findings, prompts. Created by each scene bootstrap.
    // Lives in its own fixed 1280x720 frame so the top-bar meters sit in the same grid slots
    // as the desk underneath — at every aspect ratio.
    public class HUDView : MonoBehaviour, IScreenView
    {
        private GameRoot _gameRoot;
        private RectTransform _frame;
        private Image _sanityFill;
        private Text _sanityLabel;
        private Text _sanityCapLabel;
        private Text _objective;
        private string _lastPrompt = "";   // the desk hint strip mirrors the interaction prompt
        private Text _prompt;
        private Text _toast;
        private Text _findings;
        private Text _roomInfo;
        private Text _beamLabel;
        private GameObject _trackBar;
        private Image _trackFill;
        // Under the SecurityRoom kit the sanity furniture is a WHITE pill on the board: the
        // HUD's own box retires, the track goes invisible and the fill/caption/value flip to
        // dark ink so the live meter reads exactly like the concept art's bar.
        // The flag is sticky because SetSanity repaints the fill on every sanity event — a
        // one-shot tint used to be clobbered by the next SetSanity call.
        private bool _kitSanity;
        internal void ApplyKitSanity(bool kit)
        {
            _kitSanity = kit;
            var track = _sanityFill != null ? _sanityFill.rectTransform.parent.GetComponent<UnityEngine.UI.Image>() : null;
            if (track != null)
            {
                // the kit pill carries a DARK recessed bed (the concept art's bar); the live
                // fill is paper-white inside it, so full reads full and empty reads empty
                track.color = kit ? UIFactory.NsInk : UIFactory.PanelAlt;
            }
            if (_sanityFill != null)
            {
                // 1-unit inset: the bed keeps a keyline around the fill at every value
                var frt = _sanityFill.rectTransform;
                frt.offsetMin = kit ? new Vector2(1f, 1f) : Vector2.zero;
                frt.offsetMax = kit ? new Vector2(-1f, -1f) : Vector2.zero;
            }
            if (_sanityLabel != null) _sanityLabel.color = kit ? UIFactory.NsInk : UIFactory.Ink;
            if (_sanityCapLabel != null) _sanityCapLabel.color = kit ? UIFactory.NsInk : UIFactory.InkDim;
            // fill + stage caption are stage-driven: route the repaint through SetSanity so
            // the kit inks are what lands, and any later sanity event keeps them.
            SetSanity(_gameRoot != null && _gameRoot.Session != null ? _gameRoot.Session.Sanity : 100f);
        }

        private Text _stageLabel;
        private GameObject _batteryBar;
        private Image _batteryFill;
        private GameObject _peekExitBand;   // bottom band: click to step back from a peek
        private float _toastUntil;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _frame = L.DeskFrame(transform, "HudFrame");
            var audit = _frame.gameObject.AddComponent<DeskLayoutAudit>();

            var barBg = UIFactory.BorderedPanel(_frame, "sanitybg", UIFactory.PanelBase, UIFactory.Border, 2);
            // the SecurityRoom kit paints the sanity bar's furniture on the desk canvas; the HUD
            // keeps the live track/fill/text and retires its own box (both stay registered)
            foreach (var kitImg in barBg.gameObject.GetComponentsInChildren<Image>(true))
            { var kc = kitImg.color; kc.a = 0f; kitImg.color = kc; }
            Reg(audit, "sanity", barBg, L.SanityMeter);
            _sanityCapLabel = UIFactory.Label(barBg, "cap", "SANITY", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            L.Place(_sanityCapLabel.rectTransform, L.Inner.SanityCap);
            var track = UIFactory.Panel(barBg, "track", UIFactory.PanelAlt).rectTransform;
            L.Place(track, L.Inner.SanityTrack);
            _sanityFill = UIFactory.Img(track, "fill", ProceduralArt.Get("solid_white"), UIFactory.Ink);
            StretchFill(_sanityFill.rectTransform);
            _sanityLabel = UIFactory.Label(barBg, "val", "100", 12, UIFactory.Ink, TextAnchor.MiddleRight);
            L.Place(_sanityLabel.rectTransform, L.Inner.SanityVal);
            _stageLabel = UIFactory.Label(barBg, "stage", "STABLE", 10, UIFactory.InkFaint, TextAnchor.MiddleCenter);
            L.Place(_stageLabel.rectTransform, L.Inner.SanityStage);

            var tbg = UIFactory.BorderedPanel(_frame, "trackbg", UIFactory.PanelBase, UIFactory.Border, 2);
            Reg(audit, "track", tbg, L.TrackMeter);
            var tcap = UIFactory.Label(tbg, "cap", "TRACK", 10, UIFactory.InkDim, TextAnchor.MiddleLeft);
            L.Place(tcap.rectTransform, L.Inner.TrackCap);
            var ttrack = UIFactory.Panel(tbg, "track", UIFactory.PanelAlt).rectTransform;
            L.Place(ttrack, L.Inner.TrackTrack);
            _trackFill = UIFactory.Img(ttrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.AccentBright);
            StretchFill(_trackFill.rectTransform);
            _trackBar = tbg.gameObject;
            _trackBar.SetActive(false);

            var bbg = UIFactory.BorderedPanel(_frame, "batterybg", UIFactory.PanelBase, UIFactory.Border, 2);
            Reg(audit, "light", bbg, L.LightMeter);
            var bcap = UIFactory.Label(bbg, "cap", "LIGHT", 11, UIFactory.InkDim, TextAnchor.MiddleLeft);
            L.Place(bcap.rectTransform, L.Inner.LightCap);
            var btrack = UIFactory.Panel(bbg, "track", UIFactory.PanelAlt).rectTransform;
            L.Place(btrack, L.Inner.LightTrack);
            _batteryFill = UIFactory.Img(btrack, "fill", ProceduralArt.Get("solid_white"), UIFactory.InkDim);
            StretchFill(_batteryFill.rectTransform);
            _beamLabel = UIFactory.Label(bbg, "beamstate", "", 11, UIFactory.Ink, TextAnchor.MiddleRight);
            L.Place(_beamLabel.rectTransform, L.Inner.LightState);
            _batteryBar = bbg.gameObject;

            _findings = UIFactory.Label(_frame, "findings", "FINDINGS 0/10", 13, UIFactory.InkDim, TextAnchor.MiddleRight);
            Reg(audit, "findings", _findings.rectTransform, L.Findings);

            _roomInfo = UIFactory.Label(_frame, "room", "", 13, UIFactory.InkFaint, TextAnchor.MiddleRight);
            Reg(audit, "roominfo", _roomInfo.rectTransform, L.RoomInfo);

            _objective = UIFactory.Label(_frame, "objective", "", 14, UIFactory.InkDim);
            Reg(audit, "hint", _objective.rectTransform, L.Hint);

            _prompt = UIFactory.Label(_frame, "prompt", "", 15, UIFactory.AccentBright, TextAnchor.MiddleCenter);
            Reg(audit, "prompt", _prompt.rectTransform, L.Prompt);

            _toast = UIFactory.Label(_frame, "toast", "", 16, UIFactory.Ink, TextAnchor.MiddleCenter);
            Reg(audit, "toast", _toast.rectTransform, L.Toast);
            _toast.gameObject.SetActive(false);

            // Peek step-back band: click anywhere along the bottom edge to return to the
            // hallway. Shown only while peeking; built outside the desk layout table so the
            // existing slot audit stays untouched.
            var bandRt = UIFactory.NewUI(_frame, "peekexitband");
            bandRt.anchorMin = new Vector2(0f, 0f);
            bandRt.anchorMax = new Vector2(1f, 0f);
            bandRt.pivot = new Vector2(0.5f, 0f);
            bandRt.offsetMin = new Vector2(0f, 0f);
            bandRt.offsetMax = new Vector2(0f, 44f);
            var bandImg = bandRt.gameObject.AddComponent<Image>();
            bandImg.color = new Color(1f, 1f, 1f, 0.05f);
            var bandBtn = bandRt.gameObject.AddComponent<Button>();
            bandBtn.targetGraphic = bandImg;
            bandBtn.onClick.AddListener(() => _gameRoot.ExitRoomPeek());
            var bandCap = UIFactory.Label(bandRt, "cap", "STEP BACK TO THE HALLWAY", 13,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(bandCap.rectTransform);
            _peekExitBand = bandRt.gameObject;
            _peekExitBand.SetActive(false);

            _subs.Add(root.Bus.Subscribe(EventIds.SanityChanged, e => SetSanity(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.SanityStageChanged, e =>
            {
                if (e.Data is SanityStage st && st == SanityStage.Breaking)
                    Notify("YOUR GRIP IS SLIPPING — GET BACK TO THE BOOTH");
            }));
            _subs.Add(root.Bus.Subscribe(EventIds.ObjectiveChanged, e => _objective.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.InteractionPrompt, e =>
            {
                _lastPrompt = e.Text ?? "";
                // ONE instruction per band: the red strip owns the moment-to-moment hint.
                // The grey strip keeps the shift objective — mirroring the prompt into both
                // read as a duplicated, conflicting instruction (owner UI report #2A).
                _prompt.text = _lastPrompt;
            }));
            _subs.Add(root.Bus.Subscribe(EventIds.Notification, e => Notify(e.Text)));
            _subs.Add(root.Bus.Subscribe(EventIds.ClueDiscovered, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.TrackingChanged, e => SetTracking(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.AnomalyRevealed, e => SetTracking(0f)));
            _subs.Add(root.Bus.Subscribe(EventIds.TokenCollected, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.BatteryChanged, e => { SetBattery(e.Value); RefreshRoom(); }));
            _subs.Add(root.Bus.Subscribe(EventIds.RoomEntered, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.LightModeChanged, e => RefreshRoom()));
            _subs.Add(root.Bus.Subscribe(EventIds.StateChanged, e => OnState(e.Payload<AppViewState>())));
            _objective.text = root.ObjectiveText ?? "";   // HUD may be built after the publish
            OnState(root.State);                          // correct slot visibility before the first event

            gameObject.SetActive(false);
        }

        private static void Reg(DeskLayoutAudit audit, string name, RectTransform rt, L.Region r)
        {
            L.Place(rt, r);
            audit.Register(name, rt, r);
        }

        // meter fill: stretches with the track, anchored left so anchorMax.x can act as 0..1
        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        private void OnState(AppViewState s)
        {
            bool exploring = s == AppViewState.Exploration;
            bool peeking = s == AppViewState.RoomPeek;
            bool atDesk = s == AppViewState.Checkpoint;
            _prompt.gameObject.SetActive(true);
            _roomInfo.gameObject.SetActive(exploring || peeking);
            // the desk keeps only night/clock/pause/sanity: battery and track are patrol tools
            if (_batteryBar != null) _batteryBar.SetActive(exploring || peeking);
            if (_trackBar != null) _trackBar.SetActive(exploring || peeking);
            _objective.gameObject.SetActive(exploring || peeking);   // the desk header keeps
            // night/clock/pause/sanity only: the reference board has no objective strip, and
            // at the desk the red prompt strip already owns the moment-to-moment instruction
            if (!exploring && !peeking) _prompt.text = "";
            if (!exploring && !peeking && _batteryBar != null) _batteryBar.SetActive(false); // desk has no flashlight
            if (s == AppViewState.Checkpoint) _objective.text = _gameRoot.ObjectiveText ?? "";
            if (peeking) _objective.text = _gameRoot.HuntActive
                ? _gameRoot.ObjectiveText
                : "Press F to switch the flashlight on — sweep it across the room; click the bottom edge to step back.";
            if (exploring) _objective.text = _gameRoot.ObjectiveText;   // restore after a peek overwrote the hint
            if (_peekExitBand != null) _peekExitBand.SetActive(peeking);
            RefreshFindings();
            SetSanity(_gameRoot.Session != null ? _gameRoot.Session.Sanity : 100f);
        }

        public void SetSanity(float v)
        {
            float k = Mathf.Clamp01(v / SanityModel.Max);
            var rt = _sanityFill.rectTransform;
            rt.anchorMax = new Vector2(k, 1);
            var stage = SanityModel.StageFor(v);
            _sanityFill.color = FillInk(stage);
            _sanityLabel.text = ((int)v).ToString();
            if (_stageLabel != null)
            {
                _stageLabel.text = stage.ToString().ToUpperInvariant();
                var c = stage == SanityStage.Breaking ? UIFactory.AccentBright :
                        stage == SanityStage.Shaken ? new Color(0.75f, 0.45f, 0.2f) : UIFactory.InkFaint;
                if (_kitSanity) c.a = 0f;   // the kit pill has no room for the stage caption
                _stageLabel.color = c;
            }
        }

        // Stage language for the fill. Under the kit the bar sits on a WHITE pill, so the
        // healthy ink flips to dark; the alarm reds/oranges read on either bed and stay.
        private Color FillInk(SanityStage stage)
        {
            if (stage == SanityStage.Breaking) return UIFactory.AccentBright;
            if (stage == SanityStage.Shaken) return new Color(0.75f, 0.45f, 0.2f);
            if (stage == SanityStage.Uneasy)
                return _kitSanity ? new Color(0.93f, 0.86f, 0.72f) : UIFactory.InkDim;
            // kit bed is dark ink: the healthy fill is the paper white of the pill family
            return _kitSanity ? new Color(0.93f, 0.92f, 0.89f) : UIFactory.Ink;
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
            var peek = _gameRoot.Views.PeekLight;
            bool peeking = _gameRoot.State == AppViewState.RoomPeek && peek != null && peek.Active;
            // the flashlight is the only light, so the line reads HERE, live: it must never say
            // OFF while the light is visibly on. While peeking, the cursor light IS the
            // flashlight (the beam rests OFF for the whole peek); every toggle republishes
            // LightModeChanged, and every beam change republishes the battery — so this
            // line refreshes with both.
            var rig = _gameRoot.Views.Flashlight;
            string handheld = peeking
                ? "FLASHLIGHT: " + (peek.Lit ? (peek.Focused ? "ON · FOCUS" : "ON") : "OFF")
                : "FLASHLIGHT: " + (rig == null || rig.Beam == LightMode.Off ? "OFF"
                    : rig.Beam == LightMode.Focus ? "FOCUS"
                    : "SWEEP");
            _roomInfo.text = room.DisplayName + "  ·  " + handheld;
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
