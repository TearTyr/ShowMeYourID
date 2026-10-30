namespace Hollowbrook.Logic
{
    /// <summary>School floor connectivity: the hallway hub, two classrooms and the comfort
    /// hallway with its two comfort rooms, and where each door sits in its room's local x.
    /// Single source of truth for both the physical doors RoomBuilder places and the routes
    /// anomalies patrol — roamers walk through doors, they never teleport between rooms.
    /// The rooms form a tree rooted at the hallway: classrooms and the comfort hallway hang
    /// off it, the comfort rooms hang off the comfort hallway. Only the hallway and the
    /// classrooms are Roamable (anomalies walk between them); the comfort hallway and rooms
    /// can still hold an anomaly where it spawns, it just does not roam out.</summary>
    public static class RoomGraph
    {
        public static readonly RoomId[] Roamable = { RoomId.Hallway, RoomId.Classroom1, RoomId.Classroom2 };

        public static bool IsRoamable(RoomId r) =>
            r == RoomId.Hallway || r == RoomId.Classroom1 || r == RoomId.Classroom2;

        /// <summary>Peek-only rooms: the player looks in from the doorway and never walks inside
        /// (the two classrooms and the two comfort rooms).</summary>
        public static bool IsPeekRoom(RoomId r) =>
            r == RoomId.Classroom1 || r == RoomId.Classroom2 || r == RoomId.Comfort1 || r == RoomId.Comfort2;

        /// <summary>The room a room hangs off — where a peek steps back to and where its door
        /// leads. The hallway (and the off-graph security room) are their own root.</summary>
        public static RoomId ParentOf(RoomId r)
        {
            switch (r)
            {
                case RoomId.Classroom1:
                case RoomId.Classroom2:
                case RoomId.ComfortHall: return RoomId.Hallway;
                case RoomId.Comfort1:
                case RoomId.Comfort2: return RoomId.ComfortHall;
                default: return r;
            }
        }

        /// <summary>Anchor x of the door inside room <paramref name="from"/> that leads to
        /// <paramref name="to"/>; NaN when the rooms are not directly connected. The security
        /// door is an exit, not a patrol route, so it is intentionally absent.</summary>
        public static float DoorX(RoomId from, RoomId to)
        {
            if (from == RoomId.Hallway && to == RoomId.Classroom1) return -4.0f;
            if (from == RoomId.Hallway && to == RoomId.Classroom2) return 4.6f;
            if (from == RoomId.Classroom1 && to == RoomId.Hallway) return 4.9f;
            if (from == RoomId.Classroom2 && to == RoomId.Hallway) return 4.9f;
            // the comfort hallway: its entrance sits at the hallway's east end, its own way
            // back on the east side so a player arriving from the hallway stands just inside
            if (from == RoomId.Hallway && to == RoomId.ComfortHall) return 8.7f;
            if (from == RoomId.ComfortHall && to == RoomId.Hallway) return 5.2f;
            if (from == RoomId.ComfortHall && to == RoomId.Comfort1) return 0.6f;
            if (from == RoomId.ComfortHall && to == RoomId.Comfort2) return -3.2f;
            if (from == RoomId.Comfort1 && to == RoomId.ComfortHall) return 4.9f;
            if (from == RoomId.Comfort2 && to == RoomId.ComfortHall) return 4.9f;
            return float.NaN;
        }

        public static bool Connected(RoomId a, RoomId b) => a != b && !float.IsNaN(DoorX(a, b));

        /// <summary>Next room on the shortest patrol route; <paramref name="from"/> itself when
        /// already there or when either end is off-graph.</summary>
        public static RoomId NextHop(RoomId from, RoomId to)
        {
            if (from == to || !IsRoamable(from) || !IsRoamable(to)) return from;
            if (Connected(from, to)) return to;
            return RoomId.Hallway;   // classrooms connect only through the hallway
        }
    }
}
