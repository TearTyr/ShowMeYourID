using System.Collections;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Per-room ambient lighting ONLY: the lit state, darkness alpha and flicker.
    // The handheld beam is FlashlightRig's business - this class never sees a beam mode and
    // the rig never writes here. Battery recovery, sanity drain and spawn odds all read the
    // real ambient state (Lit) from this class. Created by RoomBuilder.
    //
    // The corridor additionally owns a DEPTH layer (RoomBuilder builds it for the hallway only):
    // a second black sheet whose alpha is a radial falloff around the custodian, so with the
    // lights and the beam both out he sees a small pool and the rest of the hallway stays black.
    // It is ambient darkness, not a light - nothing glows, it only grades how much is hidden -
    // and it follows the player every frame. HallwayDarkness owns the geometry.
    public class LightController : MonoBehaviour
    {
        public bool Lit { get; private set; } = false;   // rooms are entered dark (see ForceDarkOnEntry)

        public const float LitAlpha = 0.12f;
        public const float DarkAlpha = 0.62f;

        private GameRoot _gameRoot;
        private RoomInstance _room;
        private SpriteRenderer _darkness;
        private SpriteRenderer _depth;          // hallway only: the player-following falloff
        private float _darkAlpha = DarkAlpha;   // per-room ambient darkness (the corridor is deeper)
        private float _depthAlpha = HallwayDarkness.DepthAlpha;
        private float _flickerTimer;
        private Coroutine _flickerRoutine;
        private bool _peekOverride;   // peeking: darkness is a solid-black presentation layer

        /// <summary>Peek presentation override: while on, the room's own darkness steps aside for
        /// the lamp's cover (PeekLight), and no flicker or scripted light change may touch it.
        /// Turning it off restores the visual for the real Lit state.</summary>
        public void EnterPeek(bool on)
        {
            _peekOverride = on;
            if (on)
            {
                if (_flickerRoutine != null) { StopCoroutine(_flickerRoutine); _flickerRoutine = null; }
                ApplyDepth();      // no pool, no falloff: the lamp's cover is the only dark in a peek
            }
            else ApplyVisual();
            if (_darkness != null) _darkness.enabled = !on;
        }

        public void Init(GameRoot root, RoomInstance room, SpriteRenderer darkness, bool startLit,
            SpriteRenderer depthShroud = null, float darkAlpha = DarkAlpha,
            float depthAlpha = HallwayDarkness.DepthAlpha)
        {
            _gameRoot = root; _room = room; _darkness = darkness;
            _depth = depthShroud;
            _darkAlpha = Mathf.Clamp01(darkAlpha);
            _depthAlpha = Mathf.Clamp01(depthAlpha);
            _flickerTimer = Random.Range(6f, 14f);
            Lit = startLit;
            ApplyVisual();
        }

        /// <summary>The room's current ambient darkness as one alpha: what a light's penumbra
        /// has to fade back to at its rim (lit room, dark room, or dark corridor + depth pool).
        /// Read by FlashlightRig so the beam's graded falloff lands on the real ambient.</summary>
        public float AmbientAlpha =>
            Lit ? LitAlpha : (_depth != null
                ? 1f - (1f - _darkAlpha) * (1f - _depthAlpha)
                : _darkAlpha);

        /// <summary>Scripted events (the hallucination flicker) are the only ways room light changes -
        /// there are no wall switches; the flashlight is the player's only light.</summary>
        public void SetLit(bool on)
        {
            if (Lit == on) return;
            Lit = on;
            if (!_peekOverride) ApplyVisual();   // peek keeps the room black whatever the scripted light does
            if (_gameRoot == null) return;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.LightModeChanged, on));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                on ? "Lights on." : "Lights off. Something prefers it that way."));
        }

        /// <summary>Room-entry reset: EVERY room is entered in darkness - the handheld
        /// flashlight is the only light. Called by ViewManager on each entry
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
            if (_darkness != null)
            {
                var c = _darkness.color; c.a = Lit ? LitAlpha : _darkAlpha; _darkness.color = c;
            }
            ApplyDepth();
        }

        // The depth layer is a DARK-room feature: a lit corridor is visible end to end, and a
        // peek keeps its own solid-black presentation. Disabled (not merely faded) so a lit room
        // pays no draw call for it.
        private void ApplyDepth()
        {
            if (_depth == null) return;
            bool on = !Lit && !_peekOverride;
            _depth.enabled = on;
            if (!on) return;
            var c = _depth.color; c.a = _depthAlpha; _depth.color = c;
            FollowPlayer();   // re-centre at once: the pool is never a frame behind the room entry
        }

        // Keeps the pool centred on the custodian. Struct maths only - no allocations, so the
        // lighting loop stays inside the frame budget. Room-local == world here (the world root
        // and every room root are at identity), the same space the darkness quad is built in.
        private void FollowPlayer()
        {
            if (_depth == null || !_depth.enabled) return;
            var views = _gameRoot.Views;
            if (views == null || views.Player == null) return;
            var p = views.Player.transform.localPosition;
            _depth.transform.localPosition = new Vector3(p.x, p.y, 0f);
        }

        private void Update() => Tick(Time.deltaTime);

        internal void Tick(float dt)
        {
            if (_gameRoot == null || _gameRoot.Paused || _room == null) return;
            FollowPlayer();         // the pool tracks him in the dark too - no flicker gate
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
            var cf = _darkness.color; cf.a = Lit ? LitAlpha : _darkAlpha; _darkness.color = cf;
            _flickerRoutine = null;
        }
    }
}
