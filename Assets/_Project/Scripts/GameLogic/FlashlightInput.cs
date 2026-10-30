namespace Hollowbrook.Logic
{
    /// <summary>The flashlight's latched state: on in the wide sweep, on in the narrow focus,
    /// or off (both false).</summary>
    public struct FlashlightLatches
    {
        public bool Sweep;
        public bool Focus;
    }

    /// <summary>One frame of flashlight controls -> latches + desired beam. Rules (all
    /// unit-testable, no engine):
    ///  - The ON/OFF toggle (F or left click) turns the light on in the wide sweep, or off
    ///    whatever mode it was in.
    ///  - The NARROW toggle (Q or right click) switches between the wide sweep and the narrow
    ///    focus, and ONLY while the light is on — it never turns the light on by itself.
    ///  - L puts the light out.
    ///  - A dead battery forces Off and clears the latches, so revival needs a deliberate
    ///    press — the beam never switches itself back on when charge returns.
    ///  - If both toggles arrive in one frame, ON/OFF wins.</summary>
    public static class FlashlightInput
    {
        public static LightMode Resolve(FlashlightLatches latches,
            bool toggleOnOff, bool toggleNarrow, bool offKey,
            bool batteryDead, out FlashlightLatches newLatches)
        {
            newLatches = latches;
            if (batteryDead)
            {
                newLatches = default;
                return LightMode.Off;
            }

            bool on = latches.Sweep || latches.Focus;
            if (offKey) newLatches = default;
            else if (toggleOnOff) newLatches = on ? default : new FlashlightLatches { Sweep = true };
            else if (toggleNarrow && on)
                newLatches = new FlashlightLatches { Sweep = latches.Focus, Focus = !latches.Focus };

            if (newLatches.Focus) return LightMode.Focus;
            if (newLatches.Sweep) return LightMode.Sweep;
            return LightMode.Off;
        }
    }
}
