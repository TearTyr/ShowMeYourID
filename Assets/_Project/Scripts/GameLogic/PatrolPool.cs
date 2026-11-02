using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public class PatrolPool
    {
        /// <summary>The kinds that wander the hallway on patrol: the crouched slider and creeper, the runner and the
        /// ceiling hanger. They are dealt out in turn each night.</summary>
        public static readonly AnomalyBehaviorKind[] HallwayKinds =
            { AnomalyBehaviorKind.Slide, AnomalyBehaviorKind.Sprint, AnomalyBehaviorKind.CoverHug, AnomalyBehaviorKind.Blink };

        /// <summary>What a hallucination looks like: the ceiling hanger drifting, harmless and fading on its own.</summary>
        public const AnomalyBehaviorKind PhantomKind = AnomalyBehaviorKind.CoverHug;

        public static bool IsHallwayKind(AnomalyBehaviorKind kind) => System.Array.IndexOf(HallwayKinds, kind) >= 0;

        private readonly List<AnomalyBehaviorKind> _pending = new List<AnomalyBehaviorKind>();
        private AnomalyBehaviorKind _lastMet = (AnomalyBehaviorKind)(-1);   // the kind PeekKind handed out last

        public int Pending => _pending.Count;
        public IReadOnlyList<AnomalyBehaviorKind> PendingList => _pending;

        public void Seed(IEnumerable<AnomalyBehaviorKind> kinds)
        {
            _pending.Clear();
            if (kinds != null) _pending.AddRange(kinds);
        }

        public AnomalyBehaviorKind PeekKind(GameRng rng)
        {
            if (_pending.Count == 0) return HallwayKinds[rng.NextInt(0, HallwayKinds.Length)];
            // a patrol meets a different kind each time: skip the kind it just met when another is waiting
            var choices = new List<AnomalyBehaviorKind>();
            foreach (var kind in _pending)
                if (kind != _lastMet) choices.Add(kind);
            if (choices.Count == 0) choices = _pending;
            _lastMet = choices[rng.NextInt(0, choices.Count)];
            return _lastMet;
        }

        public void Resolve(AnomalyBehaviorKind kind) => _pending.Remove(kind);

        public string Encode() => string.Join(",", _pending.ConvertAll(k => ((int)k).ToString()));

        public void Decode(string s)
        {
            _pending.Clear();
            if (string.IsNullOrEmpty(s)) return;
            foreach (var part in s.Split(','))
            {
                int v;
                if (int.TryParse(part, out v) && v >= 0 && v <= 3 && IsHallwayKind((AnomalyBehaviorKind)v)) _pending.Add((AnomalyBehaviorKind)v);
            }
        }
    }
}
