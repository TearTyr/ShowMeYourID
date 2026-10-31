using System;

namespace Hollowbrook.Logic
{
    /// <summary>Geometry of one handheld beam mode: how far it reaches and how wide it opens.</summary>
    public struct BeamParams
    {
        public float Range;          // world units from the origin along the beam axis
        public float HalfAngleDeg;   // half of the full cone angle, degrees

        private const float Deg2Rad = (float)(Math.PI / 180.0);
        public float HalfTan => (float)Math.Tan(HalfAngleDeg * Deg2Rad);
    }

    /// <summary>True cone coverage for the flashlight. One coordinate space (the world/room
    /// plane); every caller passes world x/y. Pure so the geometry is unit-testable without
    /// an engine - the rendered cone sprite is only a visual echo of this math.</summary>
    public static class BeamGeometry
    {
        public const float DefaultSweepRange = 6.2f;
        public const float DefaultSweepHalfAngleDeg = 28f;
        public const float DefaultFocusRange = 7.5f;
        public const float DefaultFocusHalfAngleDeg = 10f;
        public const float DefaultTargetRadius = 0.55f;

        public static BeamParams ParamsFor(LightMode mode,
            float sweepRange, float sweepHalfDeg, float focusRange, float focusHalfDeg)
        {
            switch (mode)
            {
                case LightMode.Sweep: return new BeamParams { Range = sweepRange, HalfAngleDeg = sweepHalfDeg };
                case LightMode.Focus: return new BeamParams { Range = focusRange, HalfAngleDeg = focusHalfDeg };
                default: return new BeamParams { Range = 0f, HalfAngleDeg = 0f };
            }
        }

        public static BeamParams DefaultParamsFor(LightMode mode) =>
            ParamsFor(mode, DefaultSweepRange, DefaultSweepHalfAngleDeg, DefaultFocusRange, DefaultFocusHalfAngleDeg);

        /// <summary>Cone test by forward distance + perpendicular offset. The target counts as a
        /// disc of <paramref name="targetRadius"/> so grazing a silhouette's body still covers it.
        /// (dirX,dirY) need not be normalized.</summary>
        public static bool Covers(float originX, float originY, float dirX, float dirY,
            BeamParams p, float targetX, float targetY, float targetRadius)
        {
            if (p.Range <= 0f) return false;                    // Off: no illumination at all
            float len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            if (len < 1e-6f) return false;                      // no aim yet
            dirX /= len; dirY /= len;

            float rx = targetX - originX, ry = targetY - originY;
            float fwd = rx * dirX + ry * dirY;                  // distance along the beam axis
            if (fwd < -targetRadius) return false;              // behind the origin
            if (fwd > p.Range + targetRadius) return false;     // beyond the reach

            float perp = Math.Abs(rx * dirY - ry * dirX);       // offset from the axis
            float limit = Math.Max(0f, fwd) * p.HalfTan + targetRadius;
            return perp <= limit;
        }
    }
}
