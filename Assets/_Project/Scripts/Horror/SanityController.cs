using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    /// <summary>Drives sanity over time per context and renders its atmospheric effects
    /// (vignette, distortion, low-sanity warning, heartbeat). Sanity is perception and
    /// atmosphere — never a health bar: it cannot end the run by itself. All distortion
    /// lives on the FX canvas BELOW the UI canvas, so documents/HUD stay readable at every
    /// stage (approved requirement). Subscribes/disposes through the EventBus properly.</summary>
    public class SanityController : MonoBehaviour
    {
        private GameRoot R;
        private Image _vignette;
        private Image _distortion;
        private Text _warning;
        private SanityStage _lastStage = SanityStage.Stable;
        private float _acc;
        private float _heartbeatTimer;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root, Canvas fxCanvas)
        {
            R = root;

            var vigRt = UIFactory.NewUI(fxCanvas.transform, "Vignette");
            UIFactory.AnchorsFull(vigRt);
            _vignette = vigRt.gameObject.AddComponent<Image>();
            _vignette.sprite = ProceduralArt.Get("vignette");
            _vignette.color = new Color(1, 1, 1, 0.25f);
            _vignette.raycastTarget = false;

            var disRt = UIFactory.NewUI(fxCanvas.transform, "Distortion");
            UIFactory.AnchorsFull(disRt);
            _distortion = disRt.gameObject.AddComponent<Image>();
            _distortion.sprite = ProceduralArt.Get("noise_overlay");
            _distortion.color = new Color(0.7f, 0.7f, 0.72f, 0f);
            _distortion.raycastTarget = false;

            var warnRt = UIFactory.NewUI(fxCanvas.transform, "LowSanityWarning");
            UIFactory.Anchor(warnRt, new Vector2(0.5f, 1f), new Vector2(560, 40), new Vector2(0, -64));
            _warning = UIFactory.Label(warnRt, "text", "", 20, new Color(0.75f, 0.2f, 0.24f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_warning.rectTransform);
            _warning.gameObject.SetActive(false);

            _subs.Add(root.Bus.Subscribe(EventIds.SanityStageChanged, OnStageChanged));
            _subs.Add(root.Bus.Subscribe(EventIds.StateChanged, e => ApplyStageVisuals(_lastStage, true)));
            ApplyStageVisuals(SanityStage.Stable, true);
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        /// <summary>Called every frame by GameRoot while in a run; accumulates whole seconds.</summary>
        public void Tick(bool inSecurity)
        {
            if (R == null || R.Session == null) return;
            _acc += Time.deltaTime;
            if (_acc < 0.5f) return;
            float dt = _acc; _acc = 0f;

            float before = R.Session.Sanity;
            if (inSecurity)
            {
                // the lit security booth is the safe space: slow recovery (approved)
                float rate = R.Session.Mode == GameMode.Normal
                    ? SanityModel.SecurityRoomRecoveryPerSecond
                    : SanityModel.SecurityRoomRecoveryPerSecond * 0.4f;
                R.Session.ApplySanity(rate * dt);
            }
            else if (R.Views != null && R.Views.Current != null)
            {
                var light = R.Views.Current.Light;
                if (light != null && light.Mode == LightMode.Off)
                    R.Session.ApplySanity(-SanityModel.DarknessDrainPerSecond * dt);
                else if (light != null && light.Mode == LightMode.Focus)
                    R.Session.ApplySanity(-SanityModel.DarknessDrainPerSecond * 0.25f * dt);
            }

            if (!Mathf.Approximately(before, R.Session.Sanity))
                R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));

            var stage = SanityModel.StageFor(R.Session.Sanity);
            if (stage != _lastStage)
            {
                _lastStage = stage;
                R.Bus.Publish(GameEvent.Of(EventIds.SanityStageChanged, stage));
            }

            // heartbeat at Shaken/Breaking
            if (stage == SanityStage.Shaken || stage == SanityStage.Breaking)
            {
                _heartbeatTimer -= dt;
                if (_heartbeatTimer <= 0f)
                {
                    _heartbeatTimer = stage == SanityStage.Breaking ? 0.85f : 1.5f;
                    R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_heartbeat",
                        stage == SanityStage.Breaking ? 0.75f : 0.4f, null));
                }
            }
        }

        private void OnStageChanged(GameEvent e)
        {
            if (e.Data is SanityStage stage) ApplyStageVisuals(stage, false);
        }

        private void ApplyStageVisuals(SanityStage stage, bool silent)
        {
            if (_vignette == null) return;
            bool reduce = R != null && R.Settings != null && R.Settings.ReduceMotion;
            float vig, dis;
            string warn;
            switch (stage)
            {
                case SanityStage.Uneasy: vig = 0.40f; dis = 0.030f; warn = ""; break;
                case SanityStage.Shaken: vig = 0.55f; dis = reduce ? 0.05f : 0.075f; warn = "YOUR HANDS WON'T STOP SHAKING"; break;
                case SanityStage.Breaking: vig = 0.70f; dis = reduce ? 0.08f : 0.130f; warn = "YOU CAN FEEL IT WATCHING BACK"; break;
                default: vig = 0.25f; dis = 0f; warn = ""; break;
            }
            var vc = _vignette.color; vc.a = vig; _vignette.color = vc;
            var dc = _distortion.color; dc.a = dis; _distortion.color = dc;
            if (_warning != null)
            {
                _warning.text = warn;
                _warning.gameObject.SetActive(!string.IsNullOrEmpty(warn));
            }
            _lastStage = stage;
        }
    }
}
