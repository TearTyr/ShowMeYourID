namespace Hollowbrook.Logic
{
    /// <summary>Latched keyboard beam selections. Mouse buttons are momentary and never latch.</summary>
    public struct FlashlightLatches
    {
        public bool Sweep;
        public bool Focus;
    }

    /// <summary>One frame of flashlight controls → latches + desired beam.
    /// Rules (all unit-testable, no engine):
    ///  - F/Q keyboard toggles are mutually exclusive — selecting one clears the other.
    ///  - L clears both latches.
    ///  - A held mouse button temporarily overrides the latched mode; releasing returns to it.
    ///  - Both buttons held: Focus wins (precise intent beats broad intent).
    ///  - A dead battery forces Off and clears the latches, so revival needs a deliberate
    ///    press — the beam never switches itself back on when charge returns.</summary>
    public static class FlashlightInput
    {
        public static LightMode Resolve(FlashlightLatches latches,
            bool toggleSweepKey, bool toggleFocusKey, bool offKey,
            bool holdSweep, bool holdFocus, bool batteryDead,
            out FlashlightLatches newLatches)
        {
            newLatches = latches;
            if (batteryDead)
            {
                newLatches = default;
                return LightMode.Off;
            }

            if (offKey) newLatches = default;
            else if (toggleFocusKey) { newLatches.Focus = !latches.Focus; newLatches.Sweep = false; }
            else if (toggleSweepKey) { newLatches.Sweep = !latches.Sweep; newLatches.Focus = false; }

            if (holdFocus) return LightMode.Focus;
            if (holdSweep) return LightMode.Sweep;
            if (newLatches.Focus) return LightMode.Focus;
            if (newLatches.Sweep) return LightMode.Sweep;
            return LightMode.Off;
        }
    }
}
