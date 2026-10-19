namespace Hollowbrook.Logic
{
    public static class TrackingPolicy
    {
        public const float RevealThreshold = 1f;

        public const float FocusRatePerSecond = 0.55f;

        public const float LitRoomRatePerSecond = 0.22f;

        public const float DecayPerSecond = 0.35f;

        public const float HallucinationTruthSeconds = 0.5f;

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

        public static float Solidify(float meter) => Clamp01(meter);

        public static bool HallucinationDispelled(float focusSeconds) =>
            focusSeconds >= HallucinationTruthSeconds;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
