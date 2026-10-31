namespace Hollowbrook.Logic
{
    /// <summary>Rate model for the repel meter - nothing else. Focus illumination ADVANCES the
    /// meter; any lesser observation (Sweep beam, lit room, the short grace window after Focus
    /// is lost) only HOLDS it, so ambient light can never resolve an encounter by itself;
    /// darkness decays it. Whether a full meter means "exposed" or "repelled" is decided by
    /// AnomalyEncounter - the single authoritative progression path. Hallucinations keep their
    /// separate Focus-seconds truth-check.</summary>
    public static class TrackingPolicy
    {
        public const float FocusRatePerSecond = 0.55f;
        public const float HoldRatePerSecond = 0f;
        public const float DecayPerSecond = 0.35f;
        public const float GraceSeconds = 0.35f;   // meter holds this long right after Focus is lost
        public const float HallucinationTruthSeconds = 0.5f;

        public static float RatePerSecond(bool focus, bool observed) =>
            RatePerSecond(focus, observed, FocusRatePerSecond, HoldRatePerSecond, DecayPerSecond);

        public static float RatePerSecond(bool focus, bool observed,
            float focusRate, float holdRate, float decayRate)
        {
            if (focus) return focusRate;
            if (observed) return holdRate;
            return -decayRate;
        }

        public static float Advance(float meter, float dt, float ratePerSecond)
        {
            if (dt <= 0f) return Clamp01(meter);
            return Clamp01(meter + ratePerSecond * dt);
        }

        public static float Advance(float meter, float dt, bool focus, bool observed) =>
            Advance(meter, dt, RatePerSecond(focus, observed));

        public static bool IsFull(float meter) => meter >= 1f - 1e-4f;

        public static float Solidify(float meter) => Clamp01(meter);

        public static bool HallucinationDispelled(float focusSeconds) =>
            focusSeconds >= HallucinationTruthSeconds;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
