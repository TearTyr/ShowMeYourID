using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>A screen mode as plain numbers so the selection policy stays engine-free
    /// and unit-testable (the Unity-side caller converts Screen.resolutions into these).</summary>
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

        /// <summary>Width / height, guarded against a zero height.</summary>
        public float Aspect => H > 0 ? (float)W / H : 0f;
    }

    /// <summary>Single source of truth for WHICH resolutions the Options menu may offer and
    /// which one to fall back to. Pure math — no UnityEngine — so every rule is testable.
    /// The actual Screen.SetResolution call lives in exactly one place
    /// (Hollowbrook.Core.SettingsSystem) to avoid competing owners of the display state.</summary>
    public static class DisplayPolicy
    {
        /// <summary>Seconds the player gets to confirm a display change before it reverts.</summary>
        public const int RevertSeconds = 15;

        /// <summary>Anything below this is not offered and not restored from prefs.</summary>
        public const int MinWidth = 1024;
        public const int MinHeight = 600;

        /// <summary>Heights we prefer to show as buttons, in presentation order.</summary>
        public static readonly int[] PreferredHeights = { 720, 900, 1080, 1440 };

        /// <summary>De-duplicate, drop anything too small, sort by area then width.
        /// "avoid duplicate entries where practical" — one button per distinct WxH.</summary>
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

        /// <summary>The resolutions shown as buttons in Options: the preferred heights that the
        /// display actually supports, always including the native/current mode, capped at
        /// <paramref name="max"/> so the sheet never overflows.</summary>
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
                    // prefer the widest (most common) mode at that height, e.g. 1920x1080 over 1080x1080
                    if (!found || m.W > best.W) { best = m; found = true; }
                }
                if (found && !outList.Contains(best)) outList.Add(best);
            }

            // the mode the player is actually on must always be selectable/visible
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

        /// <summary>Nearest usable mode to <paramref name="preferred"/>: same aspect first,
        /// then closest area. Used when a stored preference is gone from the supported list
        /// (monitor changed, window moved) so launch never lands on an unusable display.</summary>
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
