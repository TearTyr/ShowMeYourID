using System;

namespace Hollowbrook.Logic
{
    /// <summary>The corridor's LIMITED VISIBILITY model: how dark the hallway ambient is and how
    /// the extra "depth" darkness grades in with distance from the custodian, so with the beam
    /// OUT he only sees a small pool around himself and the corridor beyond it stays black.
    /// Pure (no engine) so the numbers are unit-testable and so one module owns the geometry the
    /// painted shroud sprite, the room quad and the ambient alpha all have to agree on.
    /// Consumers: RoomBuilder (builds the shroud quad), LightController (drives its alpha and
    /// follows the player), ProceduralArt (paints the radial falloff), GameConfigSO (defaults).</summary>
    public static class HallwayDarkness
    {
        // ---- ambient levels -------------------------------------------------------------
        // The guard carries a weak light. Right around him the painting keeps 45% of its
        // brightness (BaseAlpha 0.55): enough to see his own feet and the wall beside him.
        public const float BaseAlpha = 0.55f;
        // What the depth layer ADDS once the falloff has run out: 45% * (1 - 0.90) = 4.5% brightness
        // left beyond the pool, so whatever lies ahead of him is black until the flashlight finds it.
        public const float DepthAlpha = 0.90f;

        // ---- the pool (world units from the custodian) -----------------------------------
        // Fully clear out to ClearRadius, fully deep from DepthRadius, smoothstep between - a
        // gradual radial falloff with zero slope at both ends, never a hard circular edge. The pool
        // is small on purpose: it is his weak light, the flashlight is what does the looking.
        public const float ClearRadius = 1.3f;
        public const float DepthRadius = 3.8f;

        // ---- shroud coverage --------------------------------------------------------------
        // The depth layer is ONE quad that follows the player, so it has to cover the room's whole
        // darkness region from either end of the corridor. Inputs are the hallway's own layout
        // numbers (RoomBuilder/PlayerController read them from here, so they cannot drift):
        public const float HallwayHalfWidth = 14.6f;    // RoomBuilder: room.HalfWidth (the painted corridor is 30 wide)
        public const float RoomDarkMargin = 2f;         // the darkness quad is HalfWidth*2 + 4 wide
        public const float RoomDarkHalfHeight = 5.5f;   // ...and 11 tall, centred on y = 0
        public const float PlayerClampInset = 0.6f;     // PlayerController clamps x to ±(HalfWidth-0.6)
        public const float PlayerBodyLift = 1.7f;       // the player stands at FloorY + 1.7
        public const float FloorY = -3.1f;              // RoomInstance.FloorY (one shared floor line)
        public const float CoverMargin = 1f;            // slack so the shroud edge is never on screen

        /// <summary>How far the custodian can walk from the room's centre line.</summary>
        public const float PlayerTravel = HallwayHalfWidth - PlayerClampInset;          // 14.0
        /// <summary>The player's fixed height on the shared plane (the pool is centred on him).</summary>
        public const float PlayerY = FloorY + PlayerBodyLift;                           // -1.4
        public const float PlayerYOffset = -PlayerY;                                    // 1.4 (const maths)

        /// <summary>World size of the player-following depth quad: room half-extent + how far the
        /// player can be from the centre + margin, per axis. 63.2 x 14.4 for the hallway.</summary>
        public const float ShroudWorldW = 2f * (HallwayHalfWidth + RoomDarkMargin + PlayerTravel + CoverMargin);
        public const float ShroudWorldH = 2f * (RoomDarkHalfHeight + PlayerYOffset + CoverMargin);

        /// <summary>0 at the custodian, 1 once the corridor is fully deep - smoothstep, so the
        /// falloff is gradual and has no visible band or hard edge.</summary>
        public static float DepthAt(float worldDistance)
        {
            if (!(worldDistance > ClearRadius)) return 0f;      // also catches NaN
            if (worldDistance >= DepthRadius) return 1f;
            float t = (worldDistance - ClearRadius) / (DepthRadius - ClearRadius);
            return t * t * (3f - 2f * t);
        }

        /// <summary>How dark the corridor is at a distance: the two black layers composited
        /// (base ambient, then the depth layer scaled by the falloff). 0.55 near, 0.955 far.</summary>
        public static float AlphaAt(float worldDistance) =>
            1f - (1f - BaseAlpha) * (1f - DepthAlpha * DepthAt(worldDistance));

        /// <summary>Fraction of the art's brightness still reaching the eye at a distance.</summary>
        public static float VisibleFractionAt(float worldDistance) => 1f - AlphaAt(worldDistance);

        /// <summary>The pool's share of the on-screen width - the "how much can he see" number
        /// (1.3u radius in a 17.78u-wide view = 15%).</summary>
        public static float PoolShareOfView(float cameraHalfWidth) =>
            cameraHalfWidth > 0f ? ClearRadius / cameraHalfWidth : 0f;

        /// <summary>How much darker the corridor's base ambient is than a normally lit room
        /// (lit alpha 0.12 → 0.489, i.e. 49% darker).</summary>
        public static float DarkerThanLitRoom(float litAlpha)
        {
            float litVisible = 1f - litAlpha;
            if (litVisible <= 0f) return 0f;
            return 1f - (1f - BaseAlpha) / litVisible;
        }
    }
}
