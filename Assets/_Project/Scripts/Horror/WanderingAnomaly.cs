using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Horror
{
    // One roaming anomaly: its behavior pattern plus the light encounter. Spawned by RoomSpawner
    // under the persistent AnomalyRoot, so it keeps living while the player is
    // peeking into a room. Anomalies roam the hallway only (the peek rooms have their own, see PeekAnomaly).
    //
    // Authority: AnomalyEncounter is the ONE progression path for real anomalies. The old
    // tracking-threshold reveal that bypassed Repelled/Recovering is gone; TrackingPolicy only
    // supplies meter rates. Hallucinations keep their separate Focus-seconds truth-check and
    // never touch the pool or catch logic.
    //
    // Coordinate space: the shared world/room plane (world root is identity), via localPosition.
    public class WanderingAnomaly : MonoBehaviour
    {
        private GameRoot _gameRoot;
        private RoomInstance _room;              // room ownership - roaming updates this at doors
        private SpriteRenderer _sr;
        private GameObject _faceGo;
        private SpriteRenderer _face;
        private float _lifetime;
        private bool _sightedPenaltyApplied;
        private float _sanityTickTimer;

        private float _meter;                    // repel meter (TrackingPolicy rates)
        private float _graceLeft;                // hold window right after Focus is lost
        private AnomalyState _state = AnomalyState.Roaming;
        private float _stateTimer;
        private float _threatSeconds;            // continuous threatening time (fair catch)
        private float _catchGrace;               // seconds after a peek in which nothing can catch the player
        private bool _threatTold;
        private bool _resolved;                  // pool resolution, exactly once
        private bool _gone;                      // despawn handoff, exactly once
        private float _lastX;

        private BehaviorProfile _profile;
        private BehaviorTuning _behavior;
        private EncounterTuning _tuning;
        private float _targetX;                  // wander/slide waypoint in the owned room
        private float _telegraph = -1f;          // >= 0 while Sprint winds up (readable warning)
        private float _dashTimer;                // > 0 while Sprint bursts
        private GameRng _moveRng;                  // per-spawn seed for wander/retreat rolls
        private GameRng MoveRng => _moveRng ?? (_moveRng = new GameRng(0x5EED));
        private float _dashRecover;              // > 0 while Sprint catches its breath
        private float _retreatTargetX;           // where Repelled movement withdraws to

        public AnomalyState State => _state;
        public float Meter => _meter;
        public bool Resolved => _resolved;
        public bool IsHallucination { get; private set; }
        public bool WasRevealed { get; private set; }
        public bool WasCaught { get; private set; }
        public AnomalyBehaviorKind Kind { get; private set; }
        public Vector3 Pos => transform.localPosition;

        internal GameRoot Root => _gameRoot;
        internal RoomInstance Room => _room;
        internal SpriteRenderer Renderer => _sr;
        internal bool Gone => _gone;

        public void Init(GameRoot root, RoomInstance room, AnomalyBehaviorKind kind, Vector3 spawn, bool hallucination)
        {
            _gameRoot = root;
            _room = room;
            Kind = kind;
            IsHallucination = hallucination;

            _behavior = BehaviorTuningFrom(root.Config);
            _profile = BehaviorProfile.For(kind, _behavior);
            var cfg = root.Config;
            _tuning = new EncounterTuning
            {
                RepelMeter = EncounterTuning.Default.RepelMeter,
                ExposeMeter = cfg != null ? cfg.exposeMeterThreshold : 0.55f,
                ExposedBeatSeconds = cfg != null ? cfg.repelBeatSeconds : 0.9f,
                RecoverSeconds = cfg != null ? cfg.anomalyRecoverySeconds : 14f,
                ThreatRange = _profile.ThreatRange,
            };

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralArt.Get(BehaviorProfile.ArtKey(kind));
            if (kind == AnomalyBehaviorKind.Sprint) LoadRunFrames();
            _sr.sortingOrder = 40; // under the darkness overlay => silhouette in dim light

            // spawn INSIDE the behavior's own vertical band - no contradictory coordinates
            float startX = kind == AnomalyBehaviorKind.CoverHug ? BehaviorProfile.NearestDoorX(spawn.x) : spawn.x;   // the hanger starts above a door
            transform.localPosition = new Vector3(startX, RoomInstance.FloorY + _profile.SpawnY, 0);
            _lastX = spawn.x;
            _lifetime = hallucination ? GameRules.Current.PhantomLifetimeSeconds : GameRules.Current.AnomalyLifetimeSeconds;
            PickWanderTarget(PlayerPos());

            if (!hallucination)
            {
                root.Audio.PlaySfx("sfx_glitch", 0.55f);
                root.Audio.PlayMusic("anomaly");
            }
        }

        private float _pace = 1f;   // speed multiplier for this frame (see MoveBehavior)

        private static BehaviorTuning BehaviorTuningFrom(GameConfigSO c)
        {
            if (c == null) return BehaviorTuning.Default;
            return new BehaviorTuning
            {
                BlinkSpeed = c.anomalySpeedBlink,
                SlideSpeed = c.anomalySpeedSlide,
                SprintSpeed = c.anomalySpeedSprint,
                SprintDashSpeed = c.sprintDashSpeed,
                SprintTelegraphSeconds = c.sprintTelegraphSeconds,
                SprintDashSeconds = c.sprintDashSeconds,
                SprintRecoverSeconds = c.sprintRecoverSeconds,
                SprintAggroRange = c.sprintAggroRange,
                CoverHugSpeed = c.anomalySpeedCoverHug,
                ThreatRange = c.anomalyThreatRange,
                CatchGraceBlink = c.catchGraceBlink,
                CatchGraceSlide = c.catchGraceSlide,
                CatchGraceSprint = c.catchGraceSprint,
                CatchGraceCoverHug = c.coverHugCatchSeconds,
                RetreatSpeed = c.anomalyRetreatSpeed,
                BeamPace = c.beamPace,
            };
        }

        private void Update()
        {
            if (_gameRoot == null || _sr == null || _gone || _gameRoot.Paused) return;
            Tick(Time.deltaTime);
        }

        // One frame of anomaly logic. internal so the headless FlowCheck can step frames
        // deterministically (stub MonoBehaviours never get Update calls).
        internal void Tick(float dt)
        {
            if (_gameRoot == null || _sr == null || _gone || dt <= 0f) return;
            var views = _gameRoot.Views;
            if (views == null || views.Current == null) return;

            bool dialogueOpen = _gameRoot.Dialogue != null && _gameRoot.Dialogue.IsActive;
            bool sameRoom = _room != null && views.Current.Id == _room.Id;
            // peek: nobody inside moves, escalates or despawns - but the cursor light may
            // still reveal them, so visibility keeps ticking for the peeked room only.
            if (views.PeekActive)
            {
                bool inPeekedRoom = _room != null && views.Current.Id == _room.Id;
                TickVisibility(views, inPeekedRoom);
                return;
            }
            bool roaming = !dialogueOpen && !views.Transitioning;
            // confrontation inputs (beam, threat, catch, proximity) only exist while the player
            // shares the room; the meter/state still tick off-stage so a threat that follows
            // you de-escalates (decays toward Roaming) instead of freezing mid-encounter.
            bool active = sameRoom && roaming;

            if (IsHallucination) { TickPhantom(dt, views, active); return; }

            // lifetime runs only while the confrontation could progress: it pauses during the
            // deterministic repel/recovery resolution and while the anomaly travels off-stage.
            if (active && _state != AnomalyState.Repelled && _state != AnomalyState.Recovering)
            {
                _lifetime -= dt;
                if (_lifetime <= 0f) { Despawn(false); return; }
            }

            if (roaming)
            {
                TickTracking(dt, views, sameRoom);
                TickStates(dt, views, sameRoom, active);
                if (_gone) return;   // recovery handoff despawned mid-tick
            }
            MoveBehavior(dt, views, sameRoom, active);
            FaceTheWayItMoves();
            AnimateRun(dt);
            TickVisibility(views, sameRoom);
            if (active) ProximityEffects(dt, views);
        }

        // ---------------------------------------------------------------- hallucination

        private void TickPhantom(float dt, ViewManager views, bool active)
        {
            _lifetime -= dt;   // phantoms always fade out on their own
            if (_lifetime <= 0f) { Despawn(false); return; }
            if (active)
            {
                var p = transform.localPosition;
                p.x = Mathf.MoveTowards(p.x, _targetX, _profile.MoveSpeed * _pace * dt);
                if (Mathf.Abs(p.x - _targetX) < 0.2f) PickWanderTarget(PlayerPos());
                transform.localPosition = p;

                // separate truth-check: only the tight Focus beam sees through a phantom,
                // and dispelling one never touches the real-anomaly pool.
                _meter = views.IsFocusLit(p) ? _meter + dt : 0f;
                if (TrackingPolicy.HallucinationDispelled(_meter))
                {
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, "hallucination"));
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it was never there*", 2.2f, null));
                    Despawn(false);
                    return;
                }
            }
            TickVisibility(views, active);
        }

        // ---------------------------------------------------------------- tracking meter

        private void TickTracking(float dt, ViewManager views, bool sameRoom)
        {
            if (_resolved || _state == AnomalyState.Repelled || _state == AnomalyState.Recovering)
                return;   // meter frozen once the resolution started

            var pos = transform.localPosition;
            bool focus = sameRoom && views.IsFocusLit(pos);
            float grace = _gameRoot.Config != null ? _gameRoot.Config.focusGraceSeconds : TrackingPolicy.GraceSeconds;
            if (focus) _graceLeft = grace;
            else _graceLeft = Mathf.Max(0f, _graceLeft - dt);

            // Focus ADVANCES; a lit room or the short grace window only HOLDS - ambient light can
            // never resolve an encounter by itself. The plain Sweep beam only slows it (see _pace).
            bool observed = sameRoom && views.RoomLit;
            var cfg = _gameRoot.Config;
            float rate = TrackingPolicy.RatePerSecond(focus, observed || _graceLeft > 0f,
                cfg != null ? cfg.focusProgressPerSecond : TrackingPolicy.FocusRatePerSecond,
                TrackingPolicy.HoldRatePerSecond,
                cfg != null ? cfg.trackingDecayPerSecond : TrackingPolicy.DecayPerSecond);

            _meter = TrackingPolicy.Advance(_meter, dt, rate);
        }

        // ---------------------------------------------------------------- encounter states

        private void TickStates(float dt, ViewManager views, bool sameRoom, bool active)
        {
            var rig = views.Flashlight;
            var pos = transform.localPosition;
            bool roomLit = sameRoom && views.RoomLit;
            bool focused = sameRoom && rig != null && rig.Beam == LightMode.Focus && rig.ConeCovers(pos);
            // horizontal distance: this is a side-view school - "next to you" means next to you
            // on the floor plane, including directly under a ceiling lurker.
            float dist = sameRoom && views.Player != null
                ? Mathf.Abs(views.Player.transform.localPosition.x - pos.x) : 999f;

            // only the Focus beam or a lit room counts as light on it: the plain beam changes nothing
            var step = AnomalyEncounter.Step(_state, _meter, _stateTimer,
                focused || roomLit, focused, rig == null || !rig.BatteryDead, dist, dt, _tuning);

            bool changed = ApplyStateStep(step, dt);
            if (step.JustExposed) AnnounceExposure();
            if (step.JustRepelled) AnnounceRepel(views);
            if (changed && _state == AnomalyState.Recovering)
            {
                // leaves the stage; the spawner owns the recovery countdown and re-enters it
                // through a plausible location. No coroutine races, no duplicate returns.
                views.ScheduleReturn(Kind);
                Despawn(true);
                return;
            }
            TickCatch(dt, step.Threatening, focused || roomLit, active);
        }

        // Lands the pure state machine's verdict in the live object; true when the state moved.
        private bool ApplyStateStep(EncounterStep step, float dt)
        {
            bool changed = step.State != _state;
            _stateTimer = changed ? 0f : _stateTimer + dt;
            var prev = _state;
            _state = step.State;
            if (!changed) return false;
            if (prev == AnomalyState.Exposed && _state != AnomalyState.Repelled) HideFace();
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyStateChanged, _state.ToString()));
            return true;
        }

        private void AnnounceExposure()
        {
            ShowFace();
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*under the beam the face is WRONG*", 2.6f, null));
            _gameRoot.Audio.PlaySfx("sfx_glitch", 0.6f);
        }

        private void AnnounceRepel(ViewManager views)
        {
            ResolveOnce();       // pool marker, exactly once
            BeginRetreat(views); // physical withdrawal starts NOW; hunting is over
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyRevealed, Kind));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it tears itself away from the light*", 2.6f, null));
            _gameRoot.Audio.PlaySfx("sfx_stinger", 0.7f);
        }

        // Fair catch: continuously threatening for this behavior's OWN grace seconds, only
        // while sharing the room with the player, and never while pinned (a frozen Blink
        // under the beam cannot also maul you).
        private void TickCatch(float dt, bool threatening, bool observed, bool active)
        {
            bool pinned = _profile.FreezesWhenObserved && observed;
            if (_catchGrace > 0f) { _catchGrace -= dt; threatening = false; }
            if (!threatening || !active || pinned)
            {
                _threatSeconds = 0f;
                if (!threatening) _threatTold = false;
                return;
            }
            if (!_threatTold)
            {
                _threatTold = true;
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*footsteps, close, behind you*", 2.2f, null));
                _gameRoot.Audio.PlaySfx("sfx_heartbeat", 0.7f);
            }
            _threatSeconds += dt;
            if (_threatSeconds >= _profile.CatchGraceSeconds) CatchPlayer();
        }

        /// <summary>The player just stepped out of a peek. Whatever it was doing, it cannot finish a catch the
        /// player never saw coming: its threat timer is cleared, a short grace runs, and if it stands too
        /// close it is pushed back so there is room to react.</summary>
        public void CalmAfterPeek()
        {
            if (_gone || IsHallucination) return;
            var rules = GameRules.Current;
            _threatSeconds = 0f;
            _threatTold = false;
            _catchGrace = rules.PeekExitGraceSeconds;
            var views = _gameRoot != null ? _gameRoot.Views : null;
            if (views == null || views.Player == null) return;
            var p = transform.localPosition;
            float px = views.Player.transform.localPosition.x;
            if (Mathf.Abs(p.x - px) >= rules.PeekExitPushDistance) return;
            float side = p.x >= px ? 1f : -1f;
            p.x = px + side * rules.PeekExitPushDistance;
            transform.localPosition = ClampToBand(p);
        }

        /// <summary>The spawner hands each anomaly a seed derived from the run rng, so
        /// wander targets and retreat sides replay identically for the same run.</summary>
        public void SeedMovement(int seed)
        {
            _moveRng = new GameRng(seed);
            // the runner is on the floor or upside down on the ceiling, rolled once per spawn
            if (Kind == AnomalyBehaviorKind.Sprint && !IsHallucination && _moveRng.Chance(0.5f)) RunOnCeiling();
        }

        public bool OnCeiling { get; private set; }

        private void RunOnCeiling()
        {
            OnCeiling = true;
            var p = transform.localPosition;
            p.y = RoomInstance.FloorY + _profile.MaxY;
            transform.localPosition = p;
            _sr.flipY = true;
        }

        // The runner is three drawn frames: slow steps on patrol, fast on the dash, held still while it winds up.
        private Sprite[] _runFrames;
        private float _frameClock;

        private void LoadRunFrames()
        {
            _runFrames = new[] { ProceduralArt.Get("char_anomaly_sprint_a"), ProceduralArt.Get("char_anomaly_sprint_b"), ProceduralArt.Get("char_anomaly_sprint_c") };
        }

        private void AnimateRun(float dt)
        {
            if (_runFrames == null || _sr == null) return;
            float fps = 5f;
            if (_dashTimer > 0f) fps = 14f;
            else if (_telegraph >= 0f || _dashRecover > 0f) fps = 0f;
            _frameClock += fps * dt;
            _sr.sprite = _runFrames[(int)_frameClock % _runFrames.Length];
        }

        private void BeginRetreat(ViewManager views)
        {
            float px = views.Player != null ? views.Player.transform.localPosition.x : 0f;
            float x = transform.localPosition.x;
            float half = _room != null ? _room.HalfWidth - 0.5f : 10f;
            float sign = x >= px ? 1f : -1f;
            _retreatTargetX = Mathf.Clamp(x + sign * 6f, -half, half);
        }

        // ---------------------------------------------------------------- movement

        private void MoveBehavior(float dt, ViewManager views, bool sameRoom, bool active)
        {
            var p = transform.localPosition;

            if (_state == AnomalyState.Repelled || _state == AnomalyState.Recovering)
            {
                // physical retreat: ordinary movement and threat behavior are both over
                p.x = Mathf.MoveTowards(p.x, _retreatTargetX, _profile.RetreatSpeed * dt);
                transform.localPosition = ClampToBand(p);
                return;
            }

            if (!sameRoom || !active) return;   // away or mid-dialogue: hold position   // dialogue/transition: hold position, no hunting

            var player = PlayerPos();
            bool stalking = _state == AnomalyState.Stalking;
            float dist = Mathf.Abs(player.x - p.x);

            // any flashlight beam on it slows it down: the player's time to reach for Focus. Only the
            // speed changes, never the warning and recovery timers (a Sprint still telegraphs in full).
            _pace = views.BeamCovers(p) ? _profile.BeamPace : 1f;

            // one method per behavior kind: each reads as its own little rule sheet
            switch (Kind)
            {
                case AnomalyBehaviorKind.Blink: p = MoveBlink(dt, views, player, stalking, p); break;
                case AnomalyBehaviorKind.Slide: p = MoveSlide(dt, player, stalking, p); break;
                case AnomalyBehaviorKind.Sprint: p = MoveSprint(dt, views, player, dist, p); break;
                default: p = MoveCoverHug(dt, player, stalking, p); break;   // CoverHug
            }
            transform.localPosition = ClampToBand(p);
        }

        // Blink: moves only when not effectively observed - the Focus beam or the room lights
        // hold it still. The plain beam does not. Focus is still needed to repel.
        private Vector3 MoveBlink(float dt, ViewManager views, Vector3 player, bool stalking, Vector3 p)
        {
            if (views.IsFocusLit(p) || views.RoomLit) return p;
            float tx = stalking ? player.x : _targetX;
            p.x = Mathf.MoveTowards(p.x, tx, _profile.MoveSpeed * _pace * dt);
            if (!stalking && Mathf.Abs(p.x - _targetX) < 0.2f) PickWanderTarget(player);
            return p;
        }

        // Slide: readable lateral glide - commits to a point, re-picks on arrival; while
        // stalking it picks flank points near the player. No teleporting.
        private Vector3 MoveSlide(float dt, Vector3 player, bool stalking, Vector3 p)
        {
            p.x = Mathf.MoveTowards(p.x, _targetX, _profile.MoveSpeed * _pace * dt);
            if (Mathf.Abs(p.x - _targetX) < 0.2f) PickSlideTarget(player, stalking);
            return p;
        }

        // Sprint: telegraphed burst - windup under darkness, dash, recover; the Focus beam on it
        // interrupts the windup.
        private Vector3 MoveSprint(float dt, ViewManager views, Vector3 player, float dist, Vector3 p)
        {
            if (_dashTimer > 0f)
            {
                _dashTimer -= dt;
                p.x = Mathf.MoveTowards(p.x, player.x, _profile.DashSpeed * _pace * dt);
                if (Mathf.Abs(p.x - player.x) < 0.15f || _dashTimer <= 0f)
                { _dashTimer = 0f; _dashRecover = _profile.DashRecoverSeconds; }
                return p;
            }
            if (_dashRecover > 0f) { _dashRecover -= dt; return p; }

            if (dist < _profile.AggroRange && !views.IsFocusLit(p))
            {
                if (_telegraph < 0f)
                {
                    // understandable warning BEFORE the burst - the reaction window
                    _telegraph = _profile.TelegraphSeconds;
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it tenses - MOVE*", 1.6f, null));
                    _gameRoot.Audio.PlaySfx("sfx_heartbeat", 0.8f);
                }
                _telegraph -= dt;
                if (_telegraph <= 0f)
                {
                    _telegraph = -1f;
                    _dashTimer = _profile.DashSeconds;
                    _gameRoot.Audio.PlaySfx("sfx_glitch", 0.8f);
                }
            }
            else
            {
                _telegraph = -1f;
                p.x = Mathf.MoveTowards(p.x, _targetX, _profile.MoveSpeed * _pace * dt);
                if (Mathf.Abs(p.x - _targetX) < 0.2f) PickWanderTarget(player);
            }
            return p;
        }

        // CoverHug: ceiling lurker - slow drift inside its own high band.
        private Vector3 MoveCoverHug(float dt, Vector3 player, bool stalking, Vector3 p)
        {
            float tx = stalking ? player.x : _targetX;
            p.x = Mathf.MoveTowards(p.x, tx, _profile.MoveSpeed * _pace * dt);
            if (!stalking && Mathf.Abs(p.x - _targetX) < 0.2f) PickWanderTarget(player);
            return p;
        }

        private Vector3 ClampToBand(Vector3 p)
        {
            p.y = Mathf.Clamp(p.y, RoomInstance.FloorY + _profile.MinY, RoomInstance.FloorY + _profile.MaxY);
            float half = _room != null ? _room.HalfWidth - 0.5f : 10f;
            p.x = Mathf.Clamp(p.x, -half, half);
            return p;
        }

        private void PickWanderTarget(Vector3 player)
        {
            if (Kind == AnomalyBehaviorKind.CoverHug) { PickDoorTarget(); return; }
            float half = _room != null ? _room.HalfWidth : 10f;
            _targetX = -half + 1f + MoveRng.NextFloat() * Mathf.Max(0.1f, (half - 1f) * 2f);
            if (Mathf.Abs(_targetX - player.x) < 2f) _targetX = -_targetX;
        }

        // The ceiling hanger drifts from door to door along the top of the hallway.
        private void PickDoorTarget()
        {
            float here = transform.localPosition.x;
            float door = here;
            for (int tries = 0; tries < 6 && Mathf.Abs(door - here) < 1f; tries++)
                door = BehaviorProfile.DoorXs[MoveRng.NextInt(0, BehaviorProfile.DoorXs.Length)];
            _targetX = door;
        }

        private void PickSlideTarget(Vector3 player, bool stalking)
        {
            if (!stalking) { PickWanderTarget(player); return; }
            float half = _room != null ? _room.HalfWidth - 1f : 9f;
            float side = MoveRng.NextFloat() < 0.5f ? -1f : 1f;
            float off = 2.5f + MoveRng.NextFloat() * 2.5f;
            _targetX = Mathf.Clamp(player.x + side * off, -half, half);
        }

        // ---------------------------------------------------------------- feedback / visuals

        private void ShowFace()
        {
            if (_faceGo != null) { _faceGo.SetActive(true); return; }
            // where the head is in each drawing, and how big the shown face is
            var head = new Vector2(0.3f, 1.2f);   // the crouched thing: its bowed head
            float size = 0.87f;
            if (Kind == AnomalyBehaviorKind.Sprint) { head = new Vector2(1.7f, OnCeiling ? -0.45f : 0.45f); size = 0.7f; }
            else if (Kind == AnomalyBehaviorKind.CoverHug) { head = new Vector2(-0.3f, -0.55f); size = 0.6f; }
            if (_sr.flipX) head.x = -head.x;
            _faceGo = new GameObject("ExposedFace");
            _faceGo.transform.SetParent(transform, false);
            _faceGo.transform.localPosition = new Vector3(head.x, head.y, -0.1f);
            _faceGo.transform.localScale = new Vector3(size, size, 1f);
            _face = _faceGo.AddComponent<SpriteRenderer>();
            _face.sprite = ProceduralArt.Portrait("anom",
                new Portrait { HairStyle = 2, FaceVariant = 4, Accessory = 0, Distorted = true }, 96, 120);
            _face.color = new Color(0.9f, 0.9f, 0.88f);
            _face.sortingOrder = 81;
        }

        private void HideFace() => _faceGo?.SetActive(false);

        private void TickVisibility(ViewManager views, bool sameRoom)
        {
            if (_sr == null) return;
            if (!sameRoom)
            {
                _sr.enabled = false;                       // off-stage: never drawn over another room
                if (_faceGo != null && _faceGo.activeSelf) _faceGo.SetActive(false);
                return;
            }
            _sr.enabled = true;
            var pos = transform.localPosition;

            // peek: the room light is off; only the cursor circle reveals. Exposed
            // anomalies are revealed only by the cursor, not by the room light.
            if (views.PeekActive)
            {
                bool peekLit = views.IsPeekLit(pos);
                var pc = _sr.color; pc.a = peekLit ? 0.95f : 0f; _sr.color = pc;
                _sr.sortingOrder = 80;
                if (_faceGo != null && _faceGo.activeSelf) _faceGo.SetActive(false);
                return;
            }

            if (_state == AnomalyState.Exposed || _state == AnomalyState.Repelled)
            {
                var rc = _sr.color; rc.a = 1f; _sr.color = rc;   // solid ink while exposed
                _sr.sortingOrder = 80;
                return;
            }

            bool beam = views.BeamCovers(pos);
            bool lit = views.RoomLit;
            float alpha = _profile.CeilingLurker ? 0.30f : 0.92f;
            int order = 40;
            // one that is closing in is drawn above the darkness: the player always sees what catches them
            bool closingIn = _state == AnomalyState.Stalking && !IsHallucination;
            if (IsHallucination) alpha = 0.45f;
            // any beam reads silhouettes - but the ceiling lurker only turns clear under Focus
            if (beam && (!_profile.CeilingLurker || views.IsFocusLit(pos)))
            { alpha = Mathf.Max(alpha, 0.95f); order = 71; }
            if (!lit && !beam) alpha *= 0.6f;
            if (closingIn) { alpha = Mathf.Max(alpha, 0.95f); order = 71; }
            var c = _sr.color;
            if (!Mathf.Approximately(c.a, alpha) || _sr.sortingOrder != order)
            { c.a = alpha; _sr.color = c; _sr.sortingOrder = order; }
        }

        // The art faces right, so moving left is a flip.
        private void FaceTheWayItMoves()
        {
            float x = transform.localPosition.x;
            if (Mathf.Abs(x - _lastX) > 0.001f) _sr.flipX = x < _lastX;
            _lastX = x;
        }

        // ---------------------------------------------------------------- resolution

        private void ProximityEffects(float dt, ViewManager views)
        {
            if (views.Player == null) return;
            float d = Mathf.Abs(views.Player.transform.localPosition.x - transform.localPosition.x);
            var rules = GameRules.Current;
            if (d < rules.NearRange)
            {
                _sanityTickTimer -= dt;
                if (_sanityTickTimer <= 0f)
                {
                    _sanityTickTimer = 1f;
                    _gameRoot.Session.ApplySanity(-rules.NearSanityPerSecond);
                    _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                }
            }
            if (!_sightedPenaltyApplied && d < rules.SightedRange)
            {
                _sightedPenaltyApplied = true;
                _gameRoot.Session.ApplySanity(-rules.SightedSanity);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*something is standing in the room with you*", 3f, null));
            }
        }

        private void ResolveOnce()
        {
            if (_resolved || IsHallucination) return;
            _resolved = true;
            WasRevealed = true;
        }

        public void CatchPlayer()
        {
            if (_gone) return;
            if (IsHallucination) { Despawn(false); return; }
            WasCaught = true;
            _gameRoot.Session.ApplySanity(-GameRules.Current.CaughtSanity);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.AnomalyCaught, Kind));
            if (_gameRoot.Scares != null) _gameRoot.Scares.Capture(Kind);
            var views = _gameRoot.Views;
            if (views != null && views.Player != null && views.Current != null)
                views.Player.PlaceInRoom(views.Current, views.Current.PlayerSpawn);
            // caught != repelled: the anomaly slips away UNRESOLVED (pool keeps it) and
            // comes back later - patrol now has real stakes 
            if (views != null) views.ScheduleReturn(Kind);
            Despawn(false);
        }

        /// <summary>End this entity. `resolved` marks it as dealt with (pool clears in
        /// OnAnomalyGone). Runs exactly once - no duplicate handoffs or callbacks.</summary>
        public void Despawn(bool resolved)
        {
            if (_gone) return;
            _gone = true;
            if (resolved && !IsHallucination) WasRevealed = true;
            if (_gameRoot != null)
            {
                var views = _gameRoot.Views;
                if (views != null)
                {
                    views.OnAnomalyGone(this);
                    if (!IsHallucination && views.Current != null && _room != null && views.Current.Id == _room.Id)
                        _gameRoot.Audio.PlayMusic(GameRoot.MusicForRoom(_room.Id));
                }
            }
            Destroy(gameObject);
        }

        private Vector3 PlayerPos() =>
            _gameRoot != null && _gameRoot.Views != null && _gameRoot.Views.Player != null
                ? _gameRoot.Views.Player.transform.localPosition : Vector3.zero;
    }
}
