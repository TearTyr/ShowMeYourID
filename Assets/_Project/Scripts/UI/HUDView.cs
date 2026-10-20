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
        private GameObject _peekExitBand;   // bottom band: click to step back from a peek
        private float _toastUntil;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root)
        {
            _gameRoot = root;
            _frame = L.DeskFrame(transform, "HudFrame");
            var audit = _frame.gameObject.AddComponent<DeskLayoutAudit>();

            var barBg = UIFactory.BorderedPanel(_frame, "sanitybg", UIFactory.PanelBase, UIFactory.Border, 2);
            Reg(audit, "sanity", barBg, L.SanityMeter);
            var label = UIFactory.Label(barBg, "cap", "SANITY", 12, UIFactory.InkDim, TextAnchor.MiddleLeft);
            L.Place(label.rectTransform, L.Inner.SanityCap);
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
            _batteryBar = bbg.gameObject;

            _findings = UIFactory.Label(_frame, "findings", "FINDINGS 0/10", 13, UIFactory.InkDim, TextAnchor.MiddleRight);
            Reg(audit, "findings", _findings.rectTransform, L.Findings);

            _roomInfo = UIFactory.Label(_frame, "room", "", 13, UIFactory.InkFaint, TextAnchor.MiddleRight);
            Reg(audit, "roominfo", _roomInfo.rectTransform, L.RoomInfo);

            _objective = UIFactory.Label(_frame, "objective", "", 14, UIFactory.InkDim);
            Reg(audit, "hint", _objective.rectTransform, L.Hint);

            _prompt = UIFactory.Label(_frame, "prompt", "", 20, UIFactory.AccentBright, TextAnchor.MiddleCenter);
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
            _subs.Add(root.Bus.Subscribe(EventIds.InteractionPrompt, e => _prompt.text = e.Text ?? ""));
            _subs.Add(root.Bus.Subscribe(EventIds.Notification, e => Notify(e.Text)));
            _subs.Add(root.Bus.Subscribe(EventIds.ClueDiscovered, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.TrackingChanged, e => SetTracking(e.Value)));
            _subs.Add(root.Bus.Subscribe(EventIds.AnomalyRevealed, e => SetTracking(0f)));
            _subs.Add(root.Bus.Subscribe(EventIds.TokenCollected, e => RefreshFindings()));
            _subs.Add(root.Bus.Subscribe(EventIds.BatteryChanged, e => SetBattery(e.Value)));
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
            bool atDesk = s == AppViewState.Checkpoint || s == AppViewState.AdminEncounter;
            _prompt.gameObject.SetActive(true);
            _roomInfo.gameObject.SetActive(exploring || peeking);
            _objective.gameObject.SetActive(!atDesk);   // the hint line belongs to exploration; the desk grid keeps its own row
            if (!exploring && !peeking) _prompt.text = "";
            if (!exploring && !peeking && _batteryBar != null) _batteryBar.SetActive(false); // desk has no flashlight
            if (s == AppViewState.Checkpoint) _objective.text = "Inspect every visitor. Permanent decisions.";
            if (peeking) _objective.text = "Sweep the light — click the bottom edge to step back.";
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
            string light = room.Light != null ? (room.Light.Lit ? "ON" : "OFF") : "—";
            _roomInfo.text = room.DisplayName + "  ·  LIGHTS: " + light + "  ·  [F]sweep [Q]focus [L]off";
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
