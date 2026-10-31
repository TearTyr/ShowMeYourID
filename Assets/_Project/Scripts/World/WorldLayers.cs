namespace Hollowbrook.World
{
    // Sorting orders of the layers that the flashlight masks cut holes in. One place, because
    // a sprite mask only affects sprites inside its range and a range that does not include
    // the darkness layer leaves the room under a solid black cover.
    public static class WorldLayers
    {
        // The black cover over a room (and the hallway's follow-the-player shroud) both draw here.
        // They are plain black sheets with an alpha, so drawing them on the same order is safe.
        public const int Darkness = 70;
        /// <summary>The peek's black cover, drawn over everything in the room.</summary>
        public const int PeekCover = 72;
    }
}
