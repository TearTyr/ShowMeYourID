namespace Hollowbrook.Logic
{
    /// <summary>Battery tuning. Rates are percent-of-full per second (0..100), matching
    /// GameConfigSO's battery fields; RebootFraction is 0..1 of full charge.</summary>
    public struct BatteryParams
    {
        public float DrainSweepPerSecond;
        public float DrainFocusPerSecond;
        public float RecoverDarkPerSecond;
        public float RecoverLitPerSecond;
        public float RebootFraction;
    }

    public struct BatteryState
    {
        public float Charge;   // 0..1, never negative
        public bool Dead;
    }

    /// <summary>Drain/recover/death/reboot model for the handheld beam.
    ///  - Drain only while a beam is actually on (never while dead).
    ///  - Recover only while the beam is off; faster in a genuinely lit room (the caller
    ///    passes the ROOM's ambient state, never a flashlight mode).
    ///  - Hitting 0 sets Dead exactly once (justDied) and the beam stays off.
    ///  - Dead clears exactly once (justRebooted) at RebootFraction; the beam does NOT
    ///    switch back on by itself - input arbitration demands a deliberate press.</summary>
    public static class BatteryModel
    {
        public static BatteryState Tick(BatteryState s, LightMode beam, bool roomLit, float dt,
            BatteryParams p, out bool justDied, out bool justRebooted)
        {
            justDied = false; justRebooted = false;
            if (dt <= 0f) return s;

            bool on = beam != LightMode.Off && !s.Dead;
            float c = s.Charge;
            if (on)
            {
                float rate = beam == LightMode.Focus ? p.DrainFocusPerSecond : p.DrainSweepPerSecond;
                c -= rate / 100f * dt;
            }
            else
            {
                float rate = roomLit ? p.RecoverLitPerSecond : p.RecoverDarkPerSecond;
                c += rate / 100f * dt;
            }
            if (c < 0f) c = 0f; else if (c > 1f) c = 1f;

            bool dead = s.Dead;
            if (c <= 0f && !dead) { dead = true; justDied = true; }
            else if (dead && c >= p.RebootFraction) { dead = false; justRebooted = true; }

            return new BatteryState { Charge = c, Dead = dead };
        }
    }
}
