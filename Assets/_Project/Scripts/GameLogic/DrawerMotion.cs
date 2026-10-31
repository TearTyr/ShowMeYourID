using System;

namespace Hollowbrook.Logic
{
    /// <summary>The floor-files drawer's presentation maths: how far the body has come out,
    /// how far each file has emerged, and how deep the shadow under the lip is — all as pure
    /// functions of one normalized 0..1 pull value. Engine-free so the motion curve is
    /// unit-testable and so the headless harness can assert the exact same numbers the view
    /// renders with (CheckpointView.FileDrawer).
    ///
    /// Design rule (owner request): the drawer must answer the click almost instantly and
    /// read as a physical object being PULLED, not a panel fading in. So the travel curve is
    /// a fast decelerating ease-out with NO overshoot (no bounce), the files lag the body by
    /// a fraction of the travel so they visibly emerge from behind the drawer front, and the
    /// whole motion is over in well under a fifth of a second.</summary>
    public static class DrawerMotion
    {
        /// <summary>Seconds for a full pull-out (or push-in). Deliberately short: the drawer
        /// answers the click on the next frames, never after a wind-up.</summary>
        public static readonly float DurationSeconds = 0.14f;

        /// <summary>Px the drawer body travels between "shut inside the cabinet" and "pulled
        /// out": the mouth height minus the front-lip mask (CheckpointLayout.FileDrawer.H minus
        /// Inner.DrawerLipH), so a shut body hides completely behind the drawer front and a
        /// pulled one shows every file row with nothing left off the bottom of the mouth.</summary>
        public static float TravelPx { get; set; } = 150f;

        /// <summary>Extra px each file row slides up from as the drawer opens — the files
        /// emerge from the body rather than being pasted onto it.</summary>
        public const float RowEmergePx = 14f;

        /// <summary>How much a file row lags the drawer body (0 = rides with it, 1 = only
        /// starts moving when the body has finished).</summary>
        public const float RowLag = 0.35f;

        /// <summary>Scale the body grows through while coming out — a small "toward the
        /// player" cue that sells depth on a flat 2D desk.</summary>
        public const float NearScale = 1.035f;

        /// <summary>Deepest alpha of the shaded cavity at the top of the drawer body — the
        /// darkness under the front lip that the files slide out of.</summary>
        public const float CavityShadeAlpha = 0.55f;

        /// <summary>Decelerating ease-out: leaves fast, settles without bouncing.</summary>
        public static float EaseOut(float t)
        {
            float x = Clamp01(t);
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        /// <summary>Symmetric ease used for the push-back-in (mirror of the pull).</summary>
        public static float EaseInOut(float t)
        {
            float x = Clamp01(t);
            return x * x * (3f - 2f * x);
        }

        /// <summary>Advance one pull value by dt seconds. <paramref name="opening"/> picks the
        /// direction; the result is clamped to [0,1] so a held/repeated request can never
        /// run away (one drawer, one animation — see the view's guard).</summary>
        public static float Advance(float pull, float dt, bool opening)
        {
            if (DurationSeconds <= 0f) return opening ? 1f : 0f;
            float step = Math.Max(0f, dt) / DurationSeconds;
            return Clamp01(pull + (opening ? step : -step));
        }

        /// <summary>Pixels the drawer body is out of the cabinet at this pull (0..TravelPx).</summary>
        public static float BodyOffset(float pull) => EaseOut(pull) * TravelPx;

        /// <summary>The body's scale at this pull: 1 while shut, NearScale fully out.</summary>
        public static float BodyScale(float pull) => 1f + (NearScale - 1f) * EaseOut(pull);

        /// <summary>0..1 visibility of the files inside the body — they lag the body so they
        /// read as emerging from it instead of appearing with it.</summary>
        public static float RowReveal(float pull)
        {
            float e = EaseOut(pull);
            return Clamp01((e - RowLag) / Math.Max(1e-4f, 1f - RowLag));
        }

        /// <summary>Pixels each file row still has to slide up from (0 = settled).</summary>
        public static float RowOffset(float pull) => (1f - EaseOut(RowReveal(pull))) * RowEmergePx;

        /// <summary>Cavity-shade alpha under the lip: deepens as the body comes out.</summary>
        public static float CavityShadeAt(float pull) => CavityShadeAlpha * EaseInOut(pull);

        /// <summary>True when the motion has finished in the given direction — the view uses
        /// it to snap to the exact end state (no sub-pixel drift left behind).</summary>
        public static bool Finished(float pull, bool opening) => opening ? pull >= 1f : pull <= 0f;

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
