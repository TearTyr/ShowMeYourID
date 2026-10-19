using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public struct ModeSize : IEquatable<ModeSize>
    {
        public int W;
        public int H;

        public ModeSize(int w, int h) { W = w; H = h; }

        public bool Equals(ModeSize o) => W == o.W && H == o.H;
        public override bool Equals(object obj) => obj is ModeSize m && Equals(m);
        public override int GetHashCode() => W * 397 ^ H;
        public override string ToString() => W + "×" + H;

        public float Aspect => H > 0 ? (float)W / H : 0f;
    }

    public static class DisplayPolicy
    {
        public const int RevertSeconds = 15;

        public const int MinWidth = 1024;
        public const int MinHeight = 600;

        public static readonly int[] PreferredHeights = { 720, 900, 1080, 1440 };

        public static List<ModeSize> Supported(IEnumerable<ModeSize> all)
        {
            var seen = new HashSet<ModeSize>();
            var list = new List<ModeSize>();
            if (all == null) return list;
            foreach (var m in all)
            {
                if (m.W < MinWidth || m.H < MinHeight) continue;
                if (seen.Add(m)) list.Add(m);
            }
            list.Sort((a, b) =>
            {
                int byArea = (a.W * a.H).CompareTo(b.W * b.H);
                return byArea != 0 ? byArea : a.W.CompareTo(b.W);
            });
            return list;
        }

        public static List<ModeSize> MenuChoices(IReadOnlyList<ModeSize> supported, ModeSize current, int max = 5)
        {
            var outList = new List<ModeSize>();
            if (supported == null) return outList;

            foreach (int h in PreferredHeights)
            {
                ModeSize best = default;
                bool found = false;
                foreach (var m in supported)
                {
                    if (m.H != h) continue;
                    if (!found || m.W > best.W) { best = m; found = true; }
                }
                if (found && !outList.Contains(best)) outList.Add(best);
            }

            if (current.H >= MinHeight && current.W >= MinWidth && !outList.Contains(current))
                outList.Add(current);

            outList.Sort((a, b) => (a.W * a.H).CompareTo(b.W * b.H));
            while (outList.Count > max) outList.RemoveAt(outList.Count - 1);
            return outList;
        }

        public static bool IsSupported(IEnumerable<ModeSize> supported, int w, int h)
        {
            if (supported == null) return false;
            foreach (var m in supported) if (m.W == w && m.H == h) return true;
            return false;
        }

        public static ModeSize Fallback(IReadOnlyList<ModeSize> supported, ModeSize preferred)
        {
            if (supported == null || supported.Count == 0) return preferred;
            ModeSize best = supported[0];
            float bestScore = float.MaxValue;
            foreach (var m in supported)
            {
                float aspectPenalty = Math.Abs(m.Aspect - preferred.Aspect) * 10000f;
                float areaPenalty = Math.Abs(m.W * m.H - preferred.W * preferred.H) / 100000f;
                float score = aspectPenalty + areaPenalty;
                if (score < bestScore) { bestScore = score; best = m; }
            }
            return best;
        }
    }
}
