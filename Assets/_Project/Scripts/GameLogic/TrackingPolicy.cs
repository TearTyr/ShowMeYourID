namespace Hollowbrook.Logic
{
    /// <summary>Master-prompt §3: an anomaly held in the light <i>solidifies from haze to ink</i>;
    /// fully tracking one reveals/dispels it and leaves a collectible token. A hallucination
    /// given the same truth-check shimmers and vanishes instead. Pure math on purpose: the
    /// feel-tuning below is asserted by Tools/LogicTests, not by eyeballing Play mode.</summary>
    public static class TrackingPolicy
    {
        /// <summary>Meter runs 0..1; reaching this reveals the anomaly.</summary>
        public const float RevealThreshold = 1f;

        /// <summary>Focus (flashlight) is the tight truth-beam: ~1.8 s of hold to reveal.</summary>
        public const float FocusRatePerSecond = 0.55f;

        /// <summary>A lit room (Sweep) observes slowly — tracking is deliberate, not passive.</summary>
        public const float LitRoomRatePerSecond = 0.22f;

        /// <summary>Look away and the haze closes back up.</summary>
        public const float DecayPerSecond = 0.35f;

        /// <summary>Spec §4: half a second of Focus truth-checks a hallucination.</summary>
        public const float HallucinationTruthSeconds = 0.5f;

        /// <summary>How long the solidified silhouette holds before it dispels (leaving the token).</summary>
        public const float RevealBeatSeconds = 1.2f;

        public static float RatePerSecond(bool focus, bool litRoom)
        {
            if (focus) return FocusRatePerSecond;
            if (litRoom) return LitRoomRatePerSecond;
            return -DecayPerSecond;
        }

        public static float Advance(float meter, float dt, bool focus, bool litRoom)
        {
            if (dt <= 0f) return Clamp01(meter);
            return Clamp01(meter + RatePerSecond(focus, litRoom) * dt);
        }

        public static bool IsRevealed(float meter) => meter >= RevealThreshold - 1e-4f;

        /// <summary>Haze→ink factor for the silhouette (0 = haze, 1 = solid ink).</summary>
        public static float Solidify(float meter) => Clamp01(meter);

        public static bool HallucinationDispelled(float focusSeconds) =>
            focusSeconds >= HallucinationTruthSeconds;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
