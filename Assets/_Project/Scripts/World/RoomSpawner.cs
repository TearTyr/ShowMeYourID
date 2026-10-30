using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Decides when a roaming anomaly appears, where a repelled one returns, and owns the
    // single live-anomaly slot, the spawn cooldown and the breach counter.
    //
    // Lifecycle contract (one authoritative active threat):
    //  - a real anomaly occupies the live slot from spawn until OnAnomalyGone clears it —
    //    no second real anomaly can spawn while the slot is held, so overlapping routines
    //    can never duplicate it;
    //  - a repelled anomaly schedules its own kind's return here; the countdown is a plain
    //    timer ticked by UpdateSpawner (no coroutines that could fire mid-resolution), and
    //    it re-arms instead of spawning while the slot is busy;
    //  - returns reposition through a plausible room (never the player's current one when
    //    another is available) and never on top of the player;
    //  - DiscardLive (leaving for the desk) destroys WITHOUT resolving: the pool keeps it
    //    outstanding, and it also cancels any pending return.
    public class RoomSpawner
    {
        private readonly GameRoot _root;
        private readonly ViewManager _views;
        private GameRng _rng;
        private WanderingAnomaly _liveAnomaly;
        private float _spawnCooldown = 6f;
        private int _anomaliesInside;
        private float _returnTimer = -1f;
        private AnomalyBehaviorKind _returnKind;

        public WanderingAnomaly Live => _liveAnomaly;
        public bool ReturnPending => _returnTimer >= 0f;
        public float ReturnSecondsLeft => _returnTimer;

        public RoomSpawner(GameRoot root, ViewManager views, GameRng rng)
        {
            _root = root;
            _views = views;
            _rng = rng;
        }

        public void NoteBreach() { _anomaliesInside++; }

        public void CoolDown(float seconds) { _spawnCooldown = seconds; }

        public void DiscardLive()
        {
            _returnTimer = -1f;
            if (_liveAnomaly != null)
            {
                var a = _liveAnomaly;
                _liveAnomaly = null;
                Object.Destroy(a.gameObject);
                _views.Unregister(a);
            }
        }

        public void UpdateSpawner(float dt)
        {
            if (_root.State != AppViewState.Exploration) return;
            if (_views.Current == null || _views.Transitioning) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) { _spawnCooldown = Mathf.Max(_spawnCooldown, 2f); return; }

            // a scheduled recovery is the continuation of an EXISTING threat, not ambient
            // spawning — it runs regardless of the shift gate below.
            TickReturn(dt);

            // a failed night's penalty: an anomaly is already roaming when this night's patrol starts
            if (_root.Session.RoamingAtStart && _liveAnomaly == null)
            {
                _root.Session.RoamingAtStart = false;
                SpawnAnomaly(false);
                return;
            }

            int shift = _root.Session.Mode == GameMode.Normal ? _root.Session.CurrentShift : 3;
            if (_root.Session.Mode == GameMode.Normal && shift < 2) return; // anomalies wander from shift 2
            if (_liveAnomaly != null) return;

            _spawnCooldown -= dt;
            if (_spawnCooldown > 0f) return;

            float chance = _root.Config.anomalySpawnChancePerSecond * (0.6f + 0.2f * shift) *
                           (1f + 0.35f * _anomaliesInside + 0.3f * (int)_root.Session.Breaches) *
                           (_views.Current.Light != null && !_views.Current.Light.Lit ? 1.6f : 1f);
            _spawnCooldown = 4f; // re-roll interval; chance is per-second so scale by 4
            if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
            if (_rng.NextFloat() > chance * 4f) return;

            SpawnAnomaly(false);
        }

        /// <summary>A repelled anomaly handed us its kind: count down the recovery period, then
        /// bring it back through a plausible location. Slot-busy re-arms; never duplicates.</summary>
        public void ScheduleReturn(AnomalyBehaviorKind kind)
        {
            _returnKind = kind;
            _returnTimer = _root.Config != null ? _root.Config.anomalyRecoverySeconds : 14f;
        }

        private void TickReturn(float dt)
        {
            if (_returnTimer < 0f) return;
            _returnTimer -= dt;
            if (_returnTimer > 0f) return;
            if (_liveAnomaly != null) { _returnTimer = 4f; return; }  // wait for the stage to clear
            _returnTimer = -1f;

            // plausible new location: another roamable room when one exists, so it never
            // materializes inside the player's space; it walks in through a door from there.
            RoomId? room = null;
            var options = new List<RoomId>();
            foreach (var r in RoomGraph.Roamable)
                if (_views.Current == null || r != _views.Current.Id) options.Add(r);
            if (options.Count > 0)
            {
                if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
                room = options[_rng.NextInt(0, options.Count)];
            }
            SpawnKind(_returnKind, room);
            _spawnCooldown = Mathf.Max(_spawnCooldown, 3f);
        }

        /// <summary>Ambient spawn entry: real anomalies take a kind from the shared pool
        /// (uncaught == outstanding); phantoms are always the gliding shape (they never grab).</summary>
        public void SpawnAnomaly(bool hallucination)
        {
            if (_views.Current == null) return;
            if (!hallucination && _liveAnomaly != null) return;
            if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
            var kind = hallucination ? AnomalyBehaviorKind.Slide : _root.Session.Pool.PeekKind(_rng);
            Spawn(kind, _views.Current, hallucination);
        }

        /// <summary>Spawn a specific behavior, optionally in a specific room (recovery returns,
        /// tests). Real spawns respect the single live slot and return null when it is held.</summary>
        public WanderingAnomaly SpawnKind(AnomalyBehaviorKind kind, RoomId? roomId)
        {
            if (_liveAnomaly != null) return null;
            var room = roomId.HasValue ? _views.Room(roomId.Value) : _views.Current;
            if (room == null || room.Root == null) return null;
            return Spawn(kind, room, false);
        }

        private WanderingAnomaly Spawn(AnomalyBehaviorKind kind, RoomInstance room, bool hallucination)
        {
            if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
            var spawn = PickSpawnPoint(room);

            var go = new GameObject(hallucination ? "Phantom" : "Anomaly_" + kind);
            go.transform.SetParent(_views.AnomalyRoot, false);   // persistent layer: rooms can
            var an = go.AddComponent<WanderingAnomaly>();        // deactivate without freezing it
            _views.Register(an);
            an.Init(_root, room, kind, spawn, hallucination);
            an.SeedMovement(_rng.NextInt(1, 1 << 30));   // movement rolls ride the run seed
            if (hallucination) return an;
            _liveAnomaly = an;
            _root.Bus.Publish(GameEvent.Of(EventIds.AnomalySighted, kind));
            return an;
        }

        // Prefer a spawn point well away from the player (never materialize on top of them);
        // fall back to the farthest available point.
        private Vector3 PickSpawnPoint(RoomInstance room)
        {
            var points = room.AnomalySpawnPoints;
            if (points.Count == 0) return new Vector3(room.HalfWidth - 1.5f, 0, 0);
            float playerX = _views.Player != null && _views.Current == room
                ? _views.Player.transform.localPosition.x : float.NaN;
            Vector3 best = points[0];
            float bestD = -1f;
            foreach (var p in points)
            {
                float d = float.IsNaN(playerX) ? 99f : Mathf.Abs(p.x - playerX);
                if (d > bestD) { bestD = d; best = p; }
                if (d >= 4f) return p;   // first comfortably distant point (rng-stable order)
            }
            return best;
        }

        public void OnAnomalyGone(WanderingAnomaly a)
        {
            if (a == null) return;
            if (_liveAnomaly == a) _liveAnomaly = null;
            _views.Unregister(a);
            // pool resolution exactly once per entity (the anomaly guards its own despawn):
            // only REPELLED anomalies are dealt with; caught ones stay outstanding and return.
            if (!a.IsHallucination && a.WasRevealed)
                _root.Session.Pool.Resolve(a.Kind);
        }
    }
}
