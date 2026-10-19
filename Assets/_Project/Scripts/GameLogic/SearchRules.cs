using System;

namespace Hollowbrook.Logic
{
    // How long a lamp has to rest on a dark spot before it counts as searched,
    // and how quickly an abandoned search fades back into the dark.
    public static class SearchRules
    {
        public const float SearchSeconds = 1.0f;

        public static float Advance(float progress, float dt, bool lampOverSpot)
        {
            if (lampOverSpot) return Math.Min(1f, progress + dt / SearchSeconds);
            return Math.Max(0f, progress - dt / (SearchSeconds * 2f));
        }

        public static bool IsSearched(float progress) => progress >= 1f - 1e-4f;
    }
}
