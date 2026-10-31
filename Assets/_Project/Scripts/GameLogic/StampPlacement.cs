using System;

namespace Hollowbrook.Logic
{
    /// <summary>Papers, Please stamping maths: a desk drop point mapped into the slip's own
    /// space (position, zoom scale and resting tilt included), plus the printable-area clamp
    /// that keeps the WHOLE tilted mark on the paper. Pure so the placement rules are
    /// unit-testable without an engine; CheckpointView.Decision is the only caller and the
    /// rendered mark is a visual echo of these numbers.</summary>
    public static class StampPlacement
    {
        /// <summary>A spot in the slip's own space: X right, Y DOWN from its top-left corner
        /// (the desk grid's orientation), so the caller negates Y for anchoredPosition.</summary>
        public struct Point
        {
            public float X, Y;
            public Point(float x, float y) { X = x; Y = y; }
            public override string ToString() => X.ToString("0.##") + "," + Y.ToString("0.##");
        }

        private const float Deg2Rad = (float)(Math.PI / 180.0);
        private const float MinScale = 1e-4f;   // a zero zoom must never divide into NaN/inf

        /// <summary>Desk point -> slip-local point. Undoes, in order: the slip's top-left
        /// offset, its zoom scale, then its resting tilt (Unity's clockwise z rotation with
        /// y down, so the inverse rotates by -tilt).</summary>
        public static Point DeskToLocal(float deskX, float deskY, float slipTopX, float slipTopY,
            float scale, float tiltDeg)
        {
            float s = GuardScale(scale);
            float ux = (deskX - slipTopX) / s;
            float uy = (deskY - slipTopY) / s;
            double rad = tiltDeg * Deg2Rad;
            float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
            return new Point(ux * cos - uy * sin, ux * sin + uy * cos);
        }

        /// <summary>Half extent of the mark's axis-aligned bounds once tilted: a square of side
        /// s rotated by t spans s/2 * (|cos t| + |sin t|) each way - worst case (45°) is the
        /// half diagonal. The clamp budgets this so no corner ever hangs off the paper.</summary>
        public static float TiltedHalfExtent(float markSize, float tiltDeg)
        {
            double rad = tiltDeg * Deg2Rad;
            float half = markSize * 0.5f;
            return half * ((float)Math.Abs(Math.Cos(rad)) + (float)Math.Abs(Math.Sin(rad)));
        }

        /// <summary>Pulls a local spot in until the whole tilted mark (plus <paramref name="edgePad"/>)
        /// fits the sheet. A sheet smaller than the mark clamps to its centre - the range is
        /// never allowed to invert.</summary>
        public static Point ClampToPrintable(Point local, float paperW, float paperH,
            float markSize, float maxTiltDeg, float edgePad)
        {
            float margin = TiltedHalfExtent(markSize, maxTiltDeg) + edgePad;
            return new Point(ClampAxis(local.X, paperW, margin), ClampAxis(local.Y, paperH, margin));
        }

        private static float ClampAxis(float v, float paper, float margin)
        {
            float lo = margin, hi = paper - margin;
            if (hi < lo) return paper * 0.5f;   // mark bigger than the sheet: centre it
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        private static float GuardScale(float scale)
        {
            if (Math.Abs(scale) >= MinScale) return scale;
            return scale < 0f ? -MinScale : MinScale;
        }
    }
}
