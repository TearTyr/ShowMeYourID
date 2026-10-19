using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
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

        public AnomalyBehaviorKind PeekKind(GameRng rng)
        {
            if (_pending.Count == 0) return (AnomalyBehaviorKind)rng.NextInt(0, 4);
            return _pending[rng.NextInt(0, _pending.Count)];
        }

        public void Resolve(AnomalyBehaviorKind kind) => _pending.Remove(kind);

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
