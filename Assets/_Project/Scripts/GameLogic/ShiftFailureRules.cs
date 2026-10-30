namespace Hollowbrook.Logic
{
    /// <summary>How a night can fail without ending the game (nights 1-4): the custodian blacks out
    /// at 0 sanity, or clocks out after letting an anomaly in.</summary>
    public enum ShiftFailure { None, Breach, Blackout }

    /// <summary>What a failed night costs the NEXT night. Severity grows with the anomalies let in that
    /// night (a blackout counts as two more), and the penalty scales with it and with how late in
    /// the run the failure was: less sanity at the start, a drain stack already up, and an anomaly
    /// already roaming. Pure: unit-tested, nothing here knows about the engine.</summary>
    public static class ShiftFailureRules
    {
        public const int MaxSeverity = 5;
        public const int BlackoutSeverity = 2;          // a blackout is worth two anomalies let in
        public const float SanityLossPerSeverity = 8f;  // at the first night
        public const float NightScale = 0.15f;          // +15% per night after the first
        public const float MaxStartSanityLoss = 45f;    // the next night never starts below 55

        /// <summary>How bad the failed night was: anomalies let in that night, plus the blackout.</summary>
        public static int Severity(int shiftBreaches, ShiftFailure kind)
        {
            int s = (shiftBreaches < 0 ? 0 : shiftBreaches) + (kind == ShiftFailure.Blackout ? BlackoutSeverity : 0);
            if (kind == ShiftFailure.None) return 0;
            if (s < 1) s = 1;                            // a failed night is never "free"
            return s > MaxSeverity ? MaxSeverity : s;
        }

        /// <summary>How much sanity the next night starts without (from a full bar).</summary>
        public static float StartSanityLoss(int severity, int failedNight)
        {
            if (severity <= 0) return 0f;
            float night = failedNight < 1 ? 1f : failedNight;
            float loss = severity * SanityLossPerSeverity * (1f + NightScale * (night - 1f));
            return loss > MaxStartSanityLoss ? MaxStartSanityLoss : loss;
        }

        /// <summary>Drain stacks already up when the next night begins (capped at the stack cap).</summary>
        public static int StartStacks(int severity) =>
            severity < 0 ? 0 : (severity > SanityStacks.MaxStacks ? SanityStacks.MaxStacks : severity);

        /// <summary>Whether an anomaly is already roaming when the next patrol starts.</summary>
        public static bool RoamingAtStart(int severity) => severity >= 1;
    }
}
