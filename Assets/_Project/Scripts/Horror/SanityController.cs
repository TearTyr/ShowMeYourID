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
    // Sanity-driven screen effects. Created by each scene bootstrap.
    public class SanityController : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private Image _vignette;
        private Image _distortion;
        private Text _warning;
        private SanityStage _lastStage = SanityStage.Stable;
        private float _acc;
        private float _heartbeatTimer;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();

        public void Init(GameRoot root, Canvas fxCanvas)
        {
            _gameRoot = root;

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

        public void Tick(bool inSecurity)
        {
            if (_gameRoot == null || _gameRoot.Session == null) return;
            _acc += Time.deltaTime;
            if (_acc < 0.5f) return;
            float dt = _acc; _acc = 0f;

            var session = _gameRoot.Session;
            if (_gameRoot.ShiftFailing) return;            // a failed night's scene owns the screen
            float before = session.Sanity;
            session.Stacks.Tick(dt);                       // a wrong call's extra drain wears off
            float refill = session.TakeRefill(dt);         // a correct call's refill, handed over gradually
            if (refill > 0f) session.ApplySanity(refill);
            if (inSecurity) TickDesk(session, dt);
            else if (_gameRoot.Views != null && _gameRoot.Views.Current != null)
            {
                var light = _gameRoot.Views.Current.Light;
                // sanity reads the room's REAL ambient state; the handheld beam does not
                // substitute for room light here (separation of concerns).
                if (light != null && !light.Lit && !_gameRoot.HuntActive)   // the hunt has its own clock
                    session.ApplySanity(-SanityModel.DarknessDrainPerSecond * dt);
            }

            if (!Mathf.Approximately(before, _gameRoot.Session.Sanity))
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));

            if (session.Sanity <= SanityModel.Min) { _gameRoot.OnSanityDepleted(); return; }

            var stage = SanityModel.StageFor(_gameRoot.Session.Sanity);
            if (stage != _lastStage)
            {
                _lastStage = stage;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityStageChanged, stage));
            }

            if (stage == SanityStage.Shaken || stage == SanityStage.Breaking)
            {
                _heartbeatTimer -= dt;
                if (_heartbeatTimer <= 0f)
                {
                    _heartbeatTimer = stage == SanityStage.Breaking ? 0.85f : 1.5f;
                    _gameRoot.Audio.PlaySfx("sfx_heartbeat", stage == SanityStage.Breaking ? 0.75f : 0.4f);
                }
            }
        }

        // At the desk: a student at the window wears sanity down slowly (a scary one a little
        // more), an empty window lets the custodian rest. Interactions are the same path - the
        // drain follows whoever is at the glass, whether they are a student or an anomaly.
        private void TickDesk(GameSession session, float dt)
        {
            var visitor = _gameRoot.Checkpoint != null ? _gameRoot.Checkpoint.CurrentVisitor : null;
            if (visitor == null)
            {
                float rate = session.Mode == GameMode.Normal
                    ? SanityModel.SecurityRoomRecoveryPerSecond
                    : SanityModel.SecurityRoomRecoveryPerSecond * 0.4f;
                session.ApplySanity(rate * dt);
                return;
            }
            session.ApplySanity(-(SanityModel.DeskDrainPerSecond + ScaryExtraDrain(visitor.ClaimedName)) * dt);
        }

        // The configured extra drain for a named student (0 for everyone else).
        private float ScaryExtraDrain(string name)
        {
            var cfg = _gameRoot.Config;
            if (cfg == null || cfg.scaryStudents == null || string.IsNullOrEmpty(name)) return 0f;
            foreach (var s in cfg.scaryStudents)
                if (s != null && s.fullName == name) return s.extraDrainPerSecond;
            return 0f;
        }

        private void OnStageChanged(GameEvent e)
        {
            if (e.Data is SanityStage stage) ApplyStageVisuals(stage, false);
        }

        private void ApplyStageVisuals(SanityStage stage, bool silent)
        {
            if (_vignette == null) return;
            bool reduce = _gameRoot != null && _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
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
