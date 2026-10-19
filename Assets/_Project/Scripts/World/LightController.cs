using System.Collections;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Per-room light switch state and flicker. Created by ViewManager.
    public class LightController : MonoBehaviour
    {
        public LightMode Mode { get; private set; } = LightMode.Sweep;
        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _darkness;
        private float _flickerTimer;
        private Coroutine _flickerRoutine;

        public void Init(GameRoot root, RoomInstance room, SpriteRenderer darkness, LightMode startMode)
        {
            _gameRoot = root; _room = room; _darkness = darkness;
            _flickerTimer = Random.Range(6f, 14f);
            ApplyVisual();
            Mode = startMode;
            ApplyVisual();
        }

        public void SetMode(LightMode m)
        {
            if (Mode == m) return;
            if (m != LightMode.Focus) _lastNonFocus = m;
            Mode = m;
            ApplyVisual();
            _gameRoot?.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, m));
            if (_gameRoot != null && _gameRoot.HUD != null)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    m == LightMode.Off ? "Lights off. Something prefers it that way." :
                    m == LightMode.Focus ? "Flashlight on." : "Lights on."));
        }

        private LightMode _lastNonFocus = LightMode.Sweep;

        public void ToggleFocus(bool on)
        {
            SetMode(on ? LightMode.Focus : _lastNonFocus);
        }

        private void ApplyVisual()
        {
            if (_darkness == null) return;
            float alpha = Mode == LightMode.Off ? 0.62f : Mode == LightMode.Focus ? 0.52f : 0.12f;
            var c = _darkness.color; c.a = alpha; _darkness.color = c;
        }

        private void Update()
        {
            if (_gameRoot == null || _gameRoot.Paused || _room == null) return;
            if (Mode != LightMode.Sweep) return;
            _flickerTimer -= Time.deltaTime;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = Random.Range(9f, 20f);
                if (_flickerRoutine == null && _darkness != null)
                    _flickerRoutine = StartCoroutine(Flicker());
            }
        }

        private IEnumerator Flicker()
        {
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            int pulses = photoSafe ? 2 : 4;
            float baseAlpha = 0.12f;
            _gameRoot.Audio.PlaySfx("sfx_flicker", 0.5f);
            for (int i = 0; i < pulses; i++)
            {
                // gentle dim (never a hard strobe; photosensitivity guard caps contrast)
                var c = _darkness.color; c.a = photoSafe ? 0.30f : 0.55f; _darkness.color = c;
                yield return new WaitForSeconds(0.07f);
                c = _darkness.color; c.a = baseAlpha; _darkness.color = c;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
            }
            var cf = _darkness.color; cf.a = baseAlpha; _darkness.color = cf;
            _flickerRoutine = null;
        }
    }
}
