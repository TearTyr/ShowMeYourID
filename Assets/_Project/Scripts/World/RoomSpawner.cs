using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Decides when a roaming anomaly appears and where it goes when it leaves.
    // Keeps the live-anomaly slot, the spawn cooldown and the breach counter.
    public class RoomSpawner
    {
        private readonly GameRoot _root;
        private readonly ViewManager _views;
        private GameRng _rng;
        private WanderingAnomaly _liveAnomaly;
        private float _spawnCooldown = 6f;
        private int _anomaliesInside;

        public RoomSpawner(GameRoot root, ViewManager views, GameRng rng)
        {
            _root = root;
            _views = views;
            _rng = rng;
        }

        public void NoteBreach() { _anomaliesInside++; }

        public void CoolDown(float seconds) { _spawnCooldown = seconds; }

        // Leaving for the desk discards the live anomaly without resolving it: it is still outstanding.
        public void DiscardLive()
        {
            if (_liveAnomaly != null) { Object.Destroy(_liveAnomaly.gameObject); _liveAnomaly = null; }
        }

        public void UpdateSpawner(float dt)
        {
            int shift = _root.Session.Mode == GameMode.Normal ? _root.Session.CurrentShift : 3;
            if (_root.Session.Mode == GameMode.Normal && shift < 2) return; // anomalies wander from shift 2
            if (_liveAnomaly != null) return;
            if (_root.Dialogue != null && _root.Dialogue.IsActive) { _spawnCooldown = Mathf.Max(_spawnCooldown, 2f); return; }

            _spawnCooldown -= dt;
            if (_spawnCooldown > 0f) return;

            float chance = _root.Config.anomalySpawnChancePerSecond * (0.6f + 0.2f * shift) *
                           (1f + 0.35f * _anomaliesInside + 0.3f * (int)_root.Session.Breaches) *
                           (_views.Current.Light != null && _views.Current.Light.Mode == LightMode.Off ? 1.6f : 1f);
            _spawnCooldown = 4f; // re-roll interval; chance is per-second so scale by 4
            if (UnityEngine.Random.value > chance * 4f) return;

            SpawnAnomaly(false);
        }

        public void SpawnAnomaly(bool hallucination)
        {
            if (_views.Current == null) return;
            if (!hallucination && _liveAnomaly != null) return;
            var kinds = new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Slide,
                                AnomalyBehaviorKind.Sprint, AnomalyBehaviorKind.CoverHug };
            if (_rng == null) _rng = new GameRng(_root.Session.Seed + 31 + _root.Session.CurrentShift);
            var kind = hallucination
                ? (_rng.Chance(0.5f) ? AnomalyBehaviorKind.Slide : AnomalyBehaviorKind.Blink)
                : _root.Session.Pool.PeekKind(_rng);   // shared pool: uncaught == outstanding
            if (hallucination && kind == AnomalyBehaviorKind.Blink) kind = AnomalyBehaviorKind.Slide; // phantoms never grab
            var spawn = _views.Current.AnomalySpawnPoints.Count > 0
                ? _views.Current.AnomalySpawnPoints[_rng.NextInt(0, _views.Current.AnomalySpawnPoints.Count)]
                : new Vector3(_views.Current.HalfWidth - 1.5f, 0, 0);
            if (Mathf.Abs(spawn.x - _views.Player.transform.position.x) < 4f) spawn.x = -spawn.x;

            var go = new GameObject(hallucination ? "Phantom" : "Anomaly_" + kind);
            go.transform.SetParent(_views.Current.Root.transform, false);
            var an = go.AddComponent<WanderingAnomaly>();
            an.Init(_root, _views.Current, kind, spawn, hallucination);
            if (hallucination) return;
            _liveAnomaly = an;
            _root.Bus.Publish(GameEvent.Of(EventIds.AnomalySighted, kind));
        }

        public void ScheduleReturn(AnomalyBehaviorKind kind)
        {
            _views.StartCoroutine(ReturnRoutine(kind));
        }

        private System.Collections.IEnumerator ReturnRoutine(AnomalyBehaviorKind kind)
        {
            yield return new WaitForSeconds(EncounterTuning.Default.RecoverSeconds);
            if (_root != null && !_root.Paused && _views.Current != null) SpawnAnomaly(false);
        }

        public void OnAnomalyGone(WanderingAnomaly a)
        {
            if (_liveAnomaly == a) _liveAnomaly = null;
            if (!a.IsHallucination && (a.WasRevealed || a.WasCaught))
                _root.Session.Pool.Resolve(a.Kind);
        }
    }
}
