using System;

namespace Hollowbrook.Logic
{
    /// <summary>The ground-floor map sheet's layout, computed in pure floats.
    ///
    /// This is the piece that used to be hand-tuned pixel offsets inside the view (and is why
    /// the sheet sat off-centre with CLOSE colliding into the hint line). Given the reference
    /// space the canvas actually shows, it answers: how big is the sheet, how far does the
    /// drawing inside it scale, where does the CLOSE row go, and how big is the type — all
    /// centred by construction, so no resolution needs its own magic numbers.
    ///
    /// Engine-free on purpose (see the GameLogic folder's rule): MapView applies these numbers
    /// and nothing else, LogicTests proves them, and Tools/Preview mirrors them for the SVG
    /// contact sheet. One formula, three consumers, no drift.</summary>
    public struct MapSheetLayout
    {
        /// <summary>Sheet size, in canvas reference units.</summary>
        public float SheetW, SheetH;
        /// <summary>Uniform scale from FloorPlan design units to sheet units.</summary>
        public float PaperScale;
        /// <summary>Uniform scale from design units to on-screen units for the drawn plan.</summary>
        public float PlanScale;
        /// <summary>The plan container's centre, in sheet-local units (y up, origin = sheet centre).</summary>
        public float PlanCentreY;
        /// <summary>Clamped type scales: the drawing's labels and the sheet's own chrome.</summary>
        public float PlanTextScale, ChromeTextScale;
        /// <summary>CLOSE row: size, plus its centre Y measured UP from the bottom of the
        /// visible area — exactly the number a bottom-edge anchor wants.</summary>
        public float CloseW, CloseH, CloseFromBottom;
        /// <summary>Width of the title/legend/hint lines inside the sheet.</summary>
        public float ContentW;
        // ---- the sheet's composition, in FloorPlan design units ----
        public const float Margin = 22f;         // paper edge -> content
        public const float TitleBandH = 80f;     // hand-written title + rule + subtitle
        public const float ControlBandH = 54f;   // legend + hint, inside the bottom band
        public const float PlanPad = 8f;         // breathing room around the drawn plan
        public const float CloseGap = 14f;       // sheet bottom edge -> CLOSE row
        public const float ChromeH = 132f;       // CLOSE row under the sheet + a little air
        public const float CloseRowW = 190f, CloseRowH = 38f;
        public const float PaperTiltDeg = -0.35f;    // pinned up by hand, not by machine

        // Type scales are clamped, and separately for the drawing and the sheet's chrome, so
        // shrinking the plan to fit a small window can never reduce the type to specks.
        public const float PlanTextMin = 0.85f, PlanTextMax = 1.15f;
        public const float ChromeTextMin = 0.90f, ChromeTextMax = 1.10f;

        // design-space sizes of the drawn furniture
        public const float DoorLongWallW = 34f, DoorLongWallH = 16f;   // a gap in a long wall
        public const float DoorEndWallW = 16f, DoorEndWallH = 34f;     // a gap in an end wall
        public const float MarkerSize = 26f;

        /// <summary>Fit the sheet into the reference space the canvas actually shows.
        /// <paramref name="visibleW"/>/<paramref name="visibleH"/> come from
        /// UIFactory.VisibleReferenceSize (AspectMatchGuard decides which axis is matched).</summary>
        public static MapSheetLayout Fit(float visibleW, float visibleH)
        {
            var l = new MapSheetLayout();
            float availW = Max(240f, visibleW - 24f);
            float availH = Max(200f, visibleH - ChromeH);
            l.PaperScale = Min(availW / FloorPlan.DesignW, availH / FloorPlan.DesignH);
            l.SheetW = FloorPlan.DesignW * l.PaperScale;
            l.SheetH = FloorPlan.DesignH * l.PaperScale;

            l.ContentW = FloorPlan.DesignW - Margin * 2f;
            float contentH = FloorPlan.DesignH - Margin * 2f - TitleBandH - ControlBandH;
            l.PlanScale = FloorPlan.FitScale(l.ContentW - PlanPad * 2f, contentH - PlanPad * 2f) * l.PaperScale;
            // the title band is taller than the control band, so the drawing sits this far up
            l.PlanCentreY = (TitleBandH - ControlBandH) / 2f;

            l.PlanTextScale = Clamp(l.PlanScale, PlanTextMin, PlanTextMax);
            l.ChromeTextScale = Clamp(l.PaperScale, ChromeTextMin, ChromeTextMax);

            l.CloseW = CloseRowW;
            l.CloseH = CloseRowH;
            // bottom-edge anchor + centre pivot: measured up from the bottom of the visible
            // area to the row's CENTRE, so half the row's height belongs in the offset
            l.CloseFromBottom = visibleH / 2f - l.SheetH / 2f - CloseGap - l.CloseH / 2f;
            return l;
        }

        /// <summary>Design-space point -> the plan container's anchored space, origin at the
        /// container's centre. FloorPlan's y axis points DOWN (like a drawing) while a
        /// RectTransform's points UP, so the y is negated HERE and nowhere else.</summary>
        public static float ToAnchoredX(float designX, float planScale) => designX * planScale;
        public static float ToAnchoredY(float designY, float planScale) => -designY * planScale;

        /// <summary>True when the whole sheet AND its CLOSE row fit inside the visible area,
        /// with CLOSE clear of the sheet — the "centred, never clipped, never overflowing"
        /// invariant, provable at any resolution. CloseFromBottom is measured UP from the
        /// bottom edge, while the sheet is centred, so the sheet's bottom edge sits at
        /// SheetH/2 above that same bottom edge.</summary>
        public bool FitsIn(float visibleW, float visibleH)
        {
            float closeTop = CloseFromBottom + CloseH / 2f;
            float closeBottom = CloseFromBottom - CloseH / 2f;
            float sheetBottom = SheetH / 2f;
            return SheetW <= visibleW + 0.01f && SheetH <= visibleH + 0.01f &&
                   closeTop <= visibleH + 0.01f && closeBottom >= -0.01f &&
                   closeTop <= sheetBottom - CloseGap + 0.01f;
        }

        // CloseFromBottom is an anchoredPosition for a BOTTOM-edge anchor with a CENTRE pivot,
        // so the row's edges in that same bottom-up space are +/- CloseH/2 around it. The sheet
        // is centred instead, hence its own conversion — the two only become comparable once
        // both are expressed from the bottom of the visible area.
        /// <summary>The CLOSE row's bottom edge, measured up from the bottom of the visible
        /// area — its on-screen bottom margin. Proved non-negative at every resolution, so the
        /// row can never be pushed off the bottom of the window.</summary>
        public float CloseBottomMargin => CloseFromBottom - CloseH / 2f;

        /// <summary>The CLOSE row's top edge, in the same bottom-up space.</summary>
        public float CloseTopFromBottom => CloseFromBottom + CloseH / 2f;

        /// <summary>The sheet's bottom edge in that same bottom-up space.</summary>
        public float SheetBottomFromBottom(float visibleH) => (visibleH - SheetH) / 2f;

        /// <summary>The gap left between the sheet's bottom edge and the CLOSE row. Always
        /// CloseGap, at every resolution — which is the point: nothing here is a fixed pixel
        /// offset that only works on one screen.</summary>
        public float GapUnderSheet(float visibleH) =>
            SheetBottomFromBottom(visibleH) - CloseTopFromBottom;

        /// <summary>A design-space type size, on screen: the fit scale, clamped, times the
        /// player's text-scale setting.</summary>
        public static int TypeSize(int designSize, float fitScale, float playerTextScale) =>
            (int)Math.Round(Math.Max(9f, designSize * fitScale * Max(playerTextScale, 0.5f)));

        private static float Min(float a, float b) => a < b ? a : b;
        private static float Max(float a, float b) => a > b ? a : b;
        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
