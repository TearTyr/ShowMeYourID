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
        public bool Lit { get; private set; } = false;   // rooms are entered dark (see ForceDarkOnEntry)

        private const float LitAlpha = 0.12f;
        private const float DarkAlpha = 0.62f;

        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _darkness;
        private float _flickerTimer;
        private Coroutine _flickerRoutine;
        private bool _peekOverride;   // peeking: darkness is a solid-black presentation layer

        /// <summary>Peek presentation override: while on, the darkness is fully black and no
        /// flicker or wall-switch visual may touch it (the real Lit state stays unchanged).
        /// Turning it off restores the visual for the real Lit state.</summary>
        public void EnterPeek(bool on)
        {
            _peekOverride = on;
            if (on)
            {
                if (_flickerRoutine != null) { StopCoroutine(_flickerRoutine); _flickerRoutine = null; }
                if (_darkness != null) { var c = _darkness.color; c.a = 1f; _darkness.color = c; }
            }
            else ApplyVisual();
        }

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
            if (!_peekOverride) ApplyVisual();   // peek keeps the room black whatever the switch does
            if (_gameRoot == null) return;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, on));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                on ? "Lights on." : "Lights off. Something prefers it that way."));
        }

        public void Toggle() => SetLit(!Lit);

        /// <summary>Room-entry reset: EVERY room is entered in darkness — the wall switch (E)
        /// and the handheld beam are the only lights. Called by ViewManager on each entry
        /// (peeks included). Silent by design: no toast, no "Lights off" line; the HUD
        /// refreshes on RoomEntered. A peek override still owns the visual while it is on.</summary>
        public void ForceDarkOnEntry()
        {
            bool wasLit = Lit;
            Lit = false;
            if (!_peekOverride) ApplyVisual();
            if (wasLit && _gameRoot != null)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, false));
        }

        private void ApplyVisual()
        {
            if (_darkness == null) return;
            var c = _darkness.color; c.a = Lit ? LitAlpha : DarkAlpha; _darkness.color = c;
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float dt)
        {
            if (_gameRoot == null || _gameRoot.Paused || _room == null) return;
            if (_peekOverride) return;   // no flicker while the player only peeks in
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
