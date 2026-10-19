using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Horror
{
    public class WanderingAnomaly : MonoBehaviour
    {
        private GameRoot R;
        private RoomInstance _room;
        private SpriteRenderer _sr;
        private float _lifetime;
        private bool _sightedPenaltyApplied;
        private float _sanityTickTimer;
        private float _track;             // 0..1 tracking meter (TrackingPolicy)
        private AnomalyState _state = AnomalyState.Roaming;
        private float _stateTimer;
        private bool _threatTold;
        private SpriteRenderer _face;
        public AnomalyState State => _state;
        private float _trackThrottle;     // HUD event throttle
        private bool _revealed;           // fully tracked: solid, inert, about to dispel
        private float _revealBeat;
        public bool IsHallucination { get; private set; }
        public bool WasRevealed { get; private set; }
        public bool WasCaught { get; private set; }
        public AnomalyBehaviorKind Kind { get; private set; }
        public Vector3 Pos => transform.localPosition;

        public void Init(GameRoot root, RoomInstance room, AnomalyBehaviorKind kind, Vector3 spawn, bool hallucination)
        {
            R = root;
            _room = room;
            Kind = kind;
            IsHallucination = hallucination;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralArt.Get("anomaly_" + kind.ToString().ToLowerInvariant());
            _sr.sortingOrder = 40; // under the darkness overlay => silhouette in dim light

            float floorY = -2.4f;
            transform.localPosition = kind == AnomalyBehaviorKind.CoverHug
                ? new Vector3(spawn.x, floorY + 4.6f, 0)
                : new Vector3(spawn.x, floorY + 2.4f, 0);

            _telegraph = R.Config.sprintTelegraphSeconds;
            PickTarget(Vector3.zero);
            _lifetime = hallucination ? 6.5f : 18f;

            if (!hallucination)
            {
                root.Audio.PlaySfx("sfx_glitch", 0.55f);
                root.Audio.PlayMusic("anomaly");
            }
        }

        internal GameRoot Root => R;
        internal RoomInstance Room => _room;
        internal SpriteRenderer Renderer => _sr;

        private void Update()
        {
            if (R == null || R.Paused || _sr == null) return;
            bool dialogueOpen = R.Dialogue != null && R.Dialogue.IsActive;
            float dt = Time.deltaTime;

            _lifetime -= dt;
            if (_lifetime <= 0f) { Despawn(false); return; }

            Vector3 playerPos = R.Views != null && R.Views.Player != null
                ? R.Views.Player.transform.localPosition : Vector3.zero;

            if (!dialogueOpen && !_revealed) MoveBehavior(dt, playerPos);

            TickTracking(dt, dialogueOpen);
            TickStates(dt);

            TickVisibility();

            if (!IsHallucination)
            {
                float d = Mathf.Abs(playerPos.x - transform.localPosition.x);
                if (d < 3.2f)
                {
                    _sanityTickTimer -= dt;
                    if (_sanityTickTimer <= 0f)
                    {
                        _sanityTickTimer = 1f;
                        R.Session.ApplySanity(-1.5f);
                        R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
                    }
                }
                if (!_sightedPenaltyApplied && d < 5f)
                {
                    _sightedPenaltyApplied = true;
                    R.Session.ApplySanity(-6f);
                    R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
                    R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something is standing in the room with you*", 3f, null));
                }
            }
        }

        private void TickTracking(float dt, bool dialogueOpen)
        {
            if (dialogueOpen || R == null || R.Views == null) return;
            bool focus = R.Views.IsIlluminated(transform.localPosition);
            bool litRoom = _room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep;

            if (IsHallucination)
            {
                _track = focus ? _track + dt : 0f;
                if (TrackingPolicy.HallucinationDispelled(_track))
                {
                    R.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, "hallucination"));
                    R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it was never there*", 2.2f, null));
                    Despawn(true);
                }
                return;
            }

            if (_revealed)
            {
                _revealBeat -= dt;
                if (_revealBeat <= 0f) Despawn(false);   // dispelled: the token stays behind
                return;
            }

            float before = _track;
            _track = TrackingPolicy.Advance(_track, dt, focus, litRoom);
            _trackThrottle -= dt;
            if (Mathf.Abs(_track - before) > 1e-4f && _trackThrottle <= 0f)
            {
                _trackThrottle = 0.1f;
                R.Bus.Publish(GameEvent.Of(EventIds.TrackingChanged, _track));
            }

            if (TrackingPolicy.IsRevealed(_track))
            {
                _revealed = true;
                WasRevealed = true;
                _revealBeat = TrackingPolicy.RevealBeatSeconds;
                R.Bus.Publish(GameEvent.Of(EventIds.TrackingChanged, 0f));
                R.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, Kind));
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested,
                    "*the light holds it still - it is solid now*", 2.6f, null));
                R.Audio.PlaySfx("sfx_stinger", 0.5f);
                DropToken();
            }
        }

        private void DropToken()
        {
            if (_room == null || transform.parent == null) return;
            var go = new GameObject("AnomalyToken");
            go.transform.SetParent(transform.parent, false);
            var p = transform.localPosition;
            go.transform.localPosition = new Vector3(p.x, -2.2f, p.z - 0.2f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("token");
            sr.color = new Color(0.85f, 0.85f, 0.82f);
            sr.sortingOrder = 75;
            var pick = go.AddComponent<TokenPickup>();
            pick.Init(R, _room, p.x);
            _room.Interactables.Add(pick);
        }

        private void TickStates(float dt)
        {
            if (IsHallucination || R == null || R.Views == null) return;
            var rig = R.Views.Flashlight;
            bool focused = rig != null && rig.Beam == LightMode.Focus && rig.BeamOn && rig.ConeCovers(transform.localPosition);
            bool cone = rig != null && rig.BeamOn && rig.ConeCovers(transform.localPosition);
            bool litRoom = _room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep;
            var player = R.Views.Player != null ? R.Views.Player.transform.localPosition : Vector3.zero;
            float dist = Vector3.Distance(player, transform.localPosition);
            var tuning = EncounterTuning.Default;
            var step = AnomalyEncounter.Step(_state, _track, _stateTimer, cone || litRoom, focused,
                rig == null || !rig.BatteryDead, dist, dt, tuning);
            _stateTimer = step.State == _state ? _stateTimer + dt : 0f;
            _state = step.State;

            if (step.JustExposed)
            {
                ShowFace();
                R.Bus.Publish(GameEvent.Of(EventIds.AnomalyStateChanged, _state.ToString()));
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*under the beam the face is WRONG*", 2.6f, null));
                R.Audio.PlaySfx("sfx_glitch", 0.6f);
            }
            if (step.JustRepelled)
            {
                WasRevealed = true;          // pool: resolved; token drops below
                DropToken();
                R.Bus.Publish(GameEvent.Of(EventIds.AnomalyStateChanged, _state.ToString()));
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it tears itself away from the light*", 2.6f, null));
                R.Audio.PlaySfx("sfx_stinger", 0.7f);
                if (R.Views != null) R.Views.ScheduleReturn(Kind);
            }
            if (step.Threatening && !_threatTold)
            {
                _threatTold = true;
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*footsteps, close, behind you*", 2.2f, null));
                R.Audio.PlaySfx("sfx_heartbeat", 0.7f);
            }
            if (!step.Threatening) _threatTold = false;
        }

        private void ShowFace()
        {
            if (_face != null || _sr == null) return;
            var go = new GameObject("ExposedFace");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            _face = go.AddComponent<SpriteRenderer>();
            _face.sprite = ProceduralArt.Portrait("anom", new Portrait { HairStyle = 2, FaceVariant = 4, Accessory = 0, Distorted = true }, 96, 120);
            _face.color = new Color(0.9f, 0.9f, 0.88f);
            _face.sortingOrder = 81;
        }

        public void TickVisibility()
        {
            if (_sr == null) return;
            if (_revealed)
            {
                var rc = _sr.color; rc.a = 1f; _sr.color = rc;   // solid ink, no haze
                _sr.sortingOrder = 80;
                return;
            }
            bool illuminated = R.Views != null && R.Views.IsIlluminated(transform.localPosition);
            float alpha = Kind == AnomalyBehaviorKind.CoverHug ? 0.35f : 0.92f;
            int order = 40;
            if (IsHallucination) alpha = 0.45f;
            if (illuminated) { alpha = Mathf.Max(alpha, 0.95f); order = 71; }
            if (_room != null && _room.Light != null && _room.Light.Mode == LightMode.Off && !illuminated)
                alpha *= 0.6f;
            var c = _sr.color;
            if (!Mathf.Approximately(c.a, alpha) || _sr.sortingOrder != order)
            {
                c.a = alpha; _sr.color = c; _sr.sortingOrder = order;
            }
        }

        public void CatchPlayer()
        {
            if (IsHallucination) { Despawn(true); return; }
            WasCaught = true;
            R.Session.ApplySanity(-15f);
            R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
            R.Bus.Publish(GameEvent.Of(EventIds.AnomalyCaught, Kind));
            if (R.Scares != null) R.Scares.Capture(Kind);
            if (R.Views != null && R.Views.Player != null)
                R.Views.Player.PlaceInRoom(_room, _room.PlayerSpawn);
            Despawn(true);
        }

        public void Despawn(bool wasCaught)
        {
            if (R != null && R.Views != null) R.Views.OnAnomalyGone(this);
            if (R != null && !IsHallucination)
                R.Bus.Publish(GameEvent.Of(EventIds.MusicContextRequested,
                    GameRoot.MusicForRoom(_room != null ? _room.Id : RoomId.Hallway)));
            Destroy(gameObject);
        }

    // one script, one switch: the four evasion patterns (was four strategy classes)
    private float _targetX;
    private float _telegraph;

    private void MoveBehavior(float dt, Vector3 playerPos)
    {
        var p = transform.localPosition;
        float speed = 1.1f;
        if (Kind == AnomalyBehaviorKind.Blink)
        {
            if (!Observed()) p.x = Mathf.MoveTowards(p.x, playerPos.x, speed * dt);   // moves only unseen
        }
        else if (Kind == AnomalyBehaviorKind.Slide)
        {
            p.x = Mathf.MoveTowards(p.x, _targetX, speed * 0.8f * dt);
            if (Mathf.Abs(p.x - _targetX) < 0.2f) PickTarget(playerPos);
        }
        else if (Kind == AnomalyBehaviorKind.Sprint)
        {
            _telegraph -= dt;
            if (_telegraph <= 0f) p.x = Mathf.MoveTowards(p.x, playerPos.x, 4.5f * dt);
        }
        else // CoverHug: ceiling lurker, only the Focus beam finds it
        {
            p.y = Mathf.MoveTowards(p.y, -0.6f, 0.4f * dt);
            p.x = Mathf.MoveTowards(p.x, _targetX, speed * 0.5f * dt);
            if (Mathf.Abs(p.x - _targetX) < 0.2f) PickTarget(playerPos);
        }
        p.y = Mathf.Clamp(p.y, -2.6f, -0.4f);
        transform.localPosition = p;
    }

    private void PickTarget(Vector3 playerPos)
    {
        float half = _room != null ? _room.HalfWidth : 10f;
        _targetX = UnityEngine.Random.Range(-half + 1f, half - 1f);
        if (Mathf.Abs(_targetX - playerPos.x) < 2f) _targetX = -_targetX;
        if (Kind == AnomalyBehaviorKind.Sprint) _telegraph = R.Config.sprintTelegraphSeconds;
    }

    private bool Observed()
    {
        if (_room != null && _room.Light != null && _room.Light.Mode == LightMode.Sweep) return true;
        return R.Views != null && R.Views.IsIlluminated(transform.localPosition);
    }
}
}
