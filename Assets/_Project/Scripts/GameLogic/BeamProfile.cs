using System;

namespace Hollowbrook.Logic
{
    /// <summary>The flashlight's light distribution in beam space: t = distance along the aim
    /// (0 at the lamp, 1 at the mode's range), uNorm = offset from the axis as a fraction of the
    /// wedge half-width at that distance (0 on axis, 1 at the wedge edge). ONE profile feeds every
    /// layer — the cone sprite's painted light, the stencil reveal and the penumbra sheet — so the
    /// visible beam, the hole it cuts and the graded falloff can never disagree. Pure so the
    /// curves are unit-testable; ProceduralArt bakes them, FlashlightRig scales them.</summary>
    public static class BeamProfile
    {
        // Cross-section: full on axis, feathered to nothing just OUTSIDE the wedge edge — a soft
        // tapered shoulder instead of a hard triangle side.
        public static float Side(float uNorm) => 1f - Smooth(0.55f, 1.15f, Math.Abs(uNorm));

        // Along the beam: a hot pool near the lamp, then a long exponential-ish decay that is
        // completely gone at the reach (no bright wall at the end of the cone).
        public static float Reach(float t) =>
            t <= 0f ? 1f : (1f - Smooth(0.45f, 1.0f, t)) * (1f - 0.35f * Math.Min(t, 1f));

        // The faint wide lift the cone sprite paints OUTSIDE the wedge (dust in the air, a whisper
        // of spill on nearby surfaces). Kept under the stencil cutoff so it never cuts the mask.
        public static float Halo(float t, float uNorm) =>
            0.5f * (1f - Smooth(0.9f, 1.9f, Math.Abs(uNorm))) * (1f - Smooth(0.35f, 1.0f, t));

        /// <summary>The light the cone sprite paints (0..1): wedge light plus the faint halo.</summary>
        public static float Light(float t, float uNorm) =>
            Math.Min(1f, Reach(t) * Side(uNorm) + 0.30f * Halo(t, uNorm));

        public static float Smooth(float lo, float hi, float v)
        {
            if (hi <= lo) return v >= hi ? 1f : 0f;
            float t = (v - lo) / (hi - lo);
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return t * t * (3f - 2f * t);
        }
    }
}
