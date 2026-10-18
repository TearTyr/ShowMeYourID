using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Horror
{
    /// <summary>A wandering anomaly inside a room (approved: "may find wandering anomalies",
    /// "anomalies can appear anytime"). Behavior strategies implement the master-prompt set:
    /// Blink, Slide, Sprint, CoverHug. Anomalies never kill — the lose conditions are
    /// decision-based only; being caught costs sanity + a scare + repositioning (evasion is
    /// always possible via doors, lights and the flashlight).</summary>
    public class WanderingAnomaly : MonoBehaviour
    {
        private GameRoot R;
        private RoomInstance _room;
        private AnomalyBehavior _behavior;
        private SpriteRenderer _sr;
        private float _lifetime;
        private bool _sightedPenaltyApplied;
        private float _sanityTickTimer;
        public bool IsHallucination { get; private set; }
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

            switch (kind)
            {
                case AnomalyBehaviorKind.Blink: _behavior = new BlinkBehavior(); break;
                case AnomalyBehaviorKind.Slide: _behavior = new SlideBehavior(); break;
                case AnomalyBehaviorKind.Sprint: _behavior = new SprintBehavior(); break;
                default: _behavior = new CoverHugBehavior(); break;
            }
            _behavior.Attach(this, root, room, _sr);
            _lifetime = hallucination ? 6.5f : _behavior.Lifetime;

            if (!hallucination)
            {
                root.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.55f, null));
                root.Bus.Publish(GameEvent.Of(EventIds.MusicContextRequested, "anomaly"));
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

            if (!dialogueOpen) _behavior.Tick(dt, playerPos);

            TickVisibility();

            if (!IsHallucination)
            {
                // proximity dread: slow sanity drain when it is close
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
                // first clear sighting costs a chunk of sanity
                if (!_sightedPenaltyApplied && d < 5f)
                {
                    _sightedPenaltyApplied = true;
                    R.Session.ApplySanity(-6f);
                    R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
                    R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something is standing in the room with you*", 3f, null));
                }
            }
        }

        /// <summary>Alpha/order rules: silhouettes under darkness; CoverHug only truly revealed
        /// by the Focus beam; hallucinations are translucent.</summary>
        public void TickVisibility()
        {
            if (_sr == null) return;
            bool illuminated = R.Views != null && R.Views.IsIlluminated(transform.localPosition);
            float alpha = _behavior.BaseAlpha;
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

        /// <summary>Called by behaviors when they touch the player.</summary>
        public void CatchPlayer()
        {
            if (IsHallucination) { Despawn(true); return; }
            R.Session.ApplySanity(-15f);
            R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
            R.Bus.Publish(GameEvent.Of(EventIds.AnomalyCaught, Kind));
            if (R.Scares != null) R.Scares.Capture(Kind);
            // evasion recovery: the player ends up back at the room entrance, unharmed but shaken
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
    }

    // ------------------------------------------------------------------ behaviors

    public abstract class AnomalyBehavior
    {
        protected WanderingAnomaly Body;
        protected GameRoot R;
        protected RoomInstance Room;
        protected SpriteRenderer Sr;
        public virtual float Lifetime => 18f;
        public virtual float BaseAlpha => 0.92f;

        public void Attach(WanderingAnomaly body, GameRoot root, RoomInstance room, SpriteRenderer sr)
        {
            Body = body; R = root; Room = room; Sr = sr;
            OnInit();
        }
        protected virtual void OnInit() { }
        public abstract void Tick(float dt, Vector3 playerPos);

        protected float FloorY => -2.4f;
        protected bool Observed()
        {
            // "Observed" = room lights on (Sweep) or inside the flashlight cone (Focus).
            if (Room != null && Room.Light != null && Room.Light.Mode == LightMode.Sweep) return true;
            return R.Views != null && R.Views.IsIlluminated(Body.transform.localPosition);
        }
        protected void MoveTowards(float targetX, float speed, float dt)
        {
            var p = Body.transform.localPosition;
            p.x = Mathf.MoveTowards(p.x, targetX, speed * dt);
            Body.transform.localPosition = p;
        }
    }

    /// <summary>BLINK — motionless while observed; closes distance only in darkness
    /// (matches the approved teaser: "whenever the light goes out, the student moves closer").
    /// Contact in the dark = caught.</summary>
    public class BlinkBehavior : AnomalyBehavior
    {
        public override float Lifetime => 26f;
        private float _jitter;

        public override void Tick(float dt, Vector3 playerPos)
        {
            if (Observed())
            {
                // frozen — micro-jitter unless reduced motion
                _jitter += dt;
                if (R.Settings == null || !R.Settings.ReduceMotion)
                {
                    var p = Body.transform.localPosition;
                    p.y = FloorY + 2.4f + Mathf.Sin(_jitter * 30f) * 0.012f;
                    Body.transform.localPosition = p;
                }
                return;
            }
            MoveTowards(playerPos.x, 1.15f, dt);
            if (Mathf.Abs(playerPos.x - Body.transform.localPosition.x) < 0.9f &&
                Mathf.Abs(playerPos.y - Body.transform.localPosition.y) < 2.2f)
                Body.CatchPlayer();
        }
    }

    /// <summary>SLIDE — glides across the room ignoring observation and geometry; harmless on
    /// its own (proximity dread handled by the entity) but deeply wrong to watch.</summary>
    public class SlideBehavior : AnomalyBehavior
    {
        public override float Lifetime => 15f;
        public override float BaseAlpha => 0.60f;
        private float _dir;

        protected override void OnInit()
        {
            _dir = Random.value < 0.5f ? -1f : 1f;
        }

        public override void Tick(float dt, Vector3 playerPos)
        {
            var p = Body.transform.localPosition;
            p.x += _dir * 0.85f * dt;
            p.y = FloorY + 2.4f + Mathf.Sin(Time.time * 1.4f) * 0.05f;
            if (p.x > Room.HalfWidth - 0.8f || p.x < -Room.HalfWidth + 0.8f) _dir = -_dir; // glides "through" walls visually at the seam
            Body.transform.localPosition = p;
        }
    }

    /// <summary>SPRINT — stands far away, glitches (telegraph), then bursts past the player.
    /// Never catches; the pass-by is the scare (sanity handled here, respects Reduced Motion:
    /// no camera shake, slower burst).</summary>
    public class SprintBehavior : AnomalyBehavior
    {
        public override float Lifetime => 12f;
        private enum Phase { Wait, Dash, Done }
        private Phase _phase = Phase.Wait;
        private float _timer;
        private float _dashDir;
        private bool _passed;

        protected override void OnInit()
        {
            _timer = 1.0f + Random.value * 1.5f;
        }

        public override void Tick(float dt, Vector3 playerPos)
        {
            switch (_phase)
            {
                case Phase.Wait:
                    _timer -= dt;
                    if (_timer <= 0.35f && _timer + dt > 0.35f)
                        R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.8f, null)); // telegraph
                    if (_timer <= 0f)
                    {
                        _phase = Phase.Dash;
                        _dashDir = playerPos.x >= Body.transform.localPosition.x ? 1f : -1f;
                        R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_stinger", 0.9f, null));
                        if (R.Scares != null) R.Scares.Shake(0.35f);
                    }
                    break;
                case Phase.Dash:
                {
                    float speed = (R.Settings != null && R.Settings.ReduceMotion) ? 7f : 13.5f;
                    var dp = Body.transform.localPosition;
                    dp.x += _dashDir * speed * dt;
                    Body.transform.localPosition = dp;
                    if (!_passed && Mathf.Abs(playerPos.x - Body.transform.localPosition.x) < 1.4f)
                    {
                        _passed = true;
                        R.Session.ApplySanity(-10f);
                        R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
                        R.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, "sprint_pass"));
                        if (R.Scares != null) R.Scares.Shake(0.5f);
                    }
                    if (Mathf.Abs(Body.transform.localPosition.x) > Room.HalfWidth + 1f)
                    {
                        _phase = Phase.Done;
                        Body.Despawn(false);
                    }
                    break;
                }
            }
        }

    }

    /// <summary>COVERHUG — clings near the ceiling in dim spots, barely visible without the
    /// flashlight; standing under it too long = caught. Shining the Focus beam on it makes it
    /// recoil (light is the player's tool — approved evasion system).</summary>
    public class CoverHugBehavior : AnomalyBehavior
    {
        public override float Lifetime => 30f;
        public override float BaseAlpha => 0.16f;
        private float _underTimer;
        private float _recoilTimer;

        public override void Tick(float dt, Vector3 playerPos)
        {
            bool illuminated = R.Views != null && R.Views.IsIlluminated(Body.transform.localPosition);
            var p = Body.transform.localPosition;

            if (illuminated)
            {
                _recoilTimer += dt;
                _underTimer = Mathf.Max(0f, _underTimer - dt * 2f);
                if (_recoilTimer > 1.4f)
                {
                    // recoils away from the beam along the ceiling
                    float away = playerPos.x > p.x ? -1f : 1f;
                    p.x = Mathf.Clamp(p.x + away * 3.2f, -Room.HalfWidth + 1f, Room.HalfWidth - 1f);
                    Body.transform.localPosition = p;
                    _recoilTimer = 0f;
                    R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_glitch", 0.5f, null));
                }
                return;
            }
            _recoilTimer = 0f;

            bool under = Mathf.Abs(playerPos.x - p.x) < 1.4f;
            _underTimer = under ? _underTimer + dt : Mathf.Max(0f, _underTimer - dt * 0.7f);

            if (under && _underTimer > 0.4f && _underTimer < 0.4f + dt * 2f)
                R.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something above you is breathing*", 2.5f, null));

            float catchSeconds = R.Config != null ? R.Config.coverHugCatchSeconds : 2.6f;
            if (_underTimer >= catchSeconds) Body.CatchPlayer();

            // slow ceiling drift toward the player when the room is dark
            if (Room.Light != null && Room.Light.Mode == LightMode.Off)
            {
                p.x = Mathf.MoveTowards(p.x, playerPos.x, 0.5f * dt);
                Body.transform.localPosition = p;
            }
        }
    }
}
