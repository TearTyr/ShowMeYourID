using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>ONE definition of "anomalies still outstanding tonight" (master-prompt §3).
    /// Seeded at shift start with the shift's anomaly count; patrol spawns draw from it
    /// without consuming (an anomaly left uncaught stays outstanding and can knock at the
    /// office door); dispelling or being caught resolves it; the security office consumes
    /// outstanding kinds for its late arrivals. Serialised with the run so the pool
    /// survives scene loads AND save/continue.</summary>
    [Serializable]
    public class PatrolPool
    {
        private readonly List<AnomalyBehaviorKind> _pending = new List<AnomalyBehaviorKind>();

        public int Pending => _pending.Count;
        public IReadOnlyList<AnomalyBehaviorKind> PendingList => _pending;

        public void Seed(IEnumerable<AnomalyBehaviorKind> kinds)
        {
            _pending.Clear();
            if (kinds != null) _pending.AddRange(kinds);
        }

        /// <summary>Kind for the next patrol spawn, without consuming it.</summary>
        public AnomalyBehaviorKind PeekKind(GameRng rng)
        {
            if (_pending.Count == 0) return (AnomalyBehaviorKind)rng.NextInt(0, 4);
            return _pending[rng.NextInt(0, _pending.Count)];
        }

        /// <summary>Resolved = dispelled by light or caught the player: no longer outstanding.</summary>
        public void Resolve(AnomalyBehaviorKind kind) => _pending.Remove(kind);

        /// <summary>The office admits an outstanding anomaly as a late arrival: consumes it.</summary>
        public bool TryTake(GameRng rng, out AnomalyBehaviorKind kind)
        {
            kind = AnomalyBehaviorKind.Blink;
            if (_pending.Count == 0) return false;
            int i = rng.NextInt(0, _pending.Count);
            kind = _pending[i];
            _pending.RemoveAt(i);
            return true;
        }

        public string Encode() => string.Join(",", _pending.ConvertAll(k => ((int)k).ToString()));

        public void Decode(string s)
        {
            _pending.Clear();
            if (string.IsNullOrEmpty(s)) return;
            foreach (var part in s.Split(','))
            {
                int v;
                if (int.TryParse(part, out v) && v >= 0 && v <= 3) _pending.Add((AnomalyBehaviorKind)v);
            }
        }
    }
}
