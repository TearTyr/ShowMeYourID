using System.Collections;
using Hollowbrook.Core;
using UnityEngine;

namespace Hollowbrook.World
{
    // Per-room ambient lighting ONLY: the wall switch state, darkness alpha and flicker.
    // The handheld beam is FlashlightRig's business — this class never sees a beam mode and
    // the rig never writes here. Battery recovery, sanity drain and spawn odds all read the
    // real ambient state (Lit) from this class. Created by RoomBuilder.
    public class LightController : MonoBehaviour
    {
        public bool Lit { get; private set; } = true;

        private const float LitAlpha = 0.12f;
        private const float DarkAlpha = 0.62f;

        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _darkness;
        private float _flickerTimer;
        private Coroutine _flickerRoutine;

        public void Init(GameRoot root, RoomInstance room, SpriteRenderer darkness, bool startLit)
        {
            _gameRoot = root; _room = room; _darkness = darkness;
            _flickerTimer = Random.Range(6f, 14f);
            Lit = startLit;
            ApplyVisual();
        }

        /// <summary>The wall switch (and scripted events) are the only ways room light changes.</summary>
        public void SetLit(bool on)
        {
            if (Lit == on) return;
            Lit = on;
            ApplyVisual();
            if (_gameRoot == null) return;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, on));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                on ? "Lights on." : "Lights off. Something prefers it that way."));
        }

        public void Toggle() => SetLit(!Lit);

        private void ApplyVisual()
        {
            if (_darkness == null) return;
            var c = _darkness.color; c.a = Lit ? LitAlpha : DarkAlpha; _darkness.color = c;
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float dt)
        {
            if (_gameRoot == null || _gameRoot.Paused || _room == null) return;
            if (!Lit) return;
            _flickerTimer -= dt;
            if (_flickerTimer <= 0f)
            {
                _flickerTimer = Random.Range(9f, 20f);
                if (_flickerRoutine == null && _darkness != null)
                    _flickerRoutine = StartCoroutine(Flicker());
            }
        }

        // Flicker is a pure visual wobble of the darkness alpha: it never touches Lit, and it
        // can never reach the flashlight's beam state (the rig owns that).
        private IEnumerator Flicker()
        {
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            int pulses = photoSafe ? 2 : 4;
            _gameRoot.Audio.PlaySfx("sfx_flicker", 0.5f);
            for (int i = 0; i < pulses; i++)
            {
                // gentle dim (never a hard strobe; photosensitivity guard caps contrast)
                var c = _darkness.color; c.a = photoSafe ? 0.30f : 0.55f; _darkness.color = c;
                yield return new WaitForSeconds(0.07f);
                c = _darkness.color; c.a = LitAlpha; _darkness.color = c;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
            }
            var cf = _darkness.color; cf.a = Lit ? LitAlpha : DarkAlpha; _darkness.color = cf;
            _flickerRoutine = null;
        }
    }
}
