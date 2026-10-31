namespace Hollowbrook.Logic
{
    /// <summary>School floor connectivity: the hallway hub plus two classrooms, and where each
    /// door sits in its room's local x. Single source of truth for both the physical doors
    /// RoomBuilder places and the routes anomalies patrol - roamers walk through doors, they
    /// never teleport between rooms.</summary>
    public static class RoomGraph
    {
        public static readonly RoomId[] Roamable = { RoomId.Hallway, RoomId.Classroom1, RoomId.Classroom2 };

        public static bool IsRoamable(RoomId r) =>
            r == RoomId.Hallway || r == RoomId.Classroom1 || r == RoomId.Classroom2;

        /// <summary>Anchor x of the door inside room <paramref name="from"/> that leads to
        /// <paramref name="to"/>; NaN when the rooms are not directly connected. The security
        /// door is an exit, not a patrol route, so it is intentionally absent.</summary>
        public static float DoorX(RoomId from, RoomId to)
        {
            if (from == RoomId.Hallway && to != RoomId.Security)
            {
                var room = FloorPlan.ForRoom(to);
                return room.HasValue && room.Value.HallX.HasValue ? room.Value.HallX.Value : float.NaN;
            }
            if (from == RoomId.Classroom1 && to == RoomId.Hallway) return 7.6f;   // the painted door at the right edge of the room (inside the roaming limit)
            if (from == RoomId.Classroom2 && to == RoomId.Hallway) return 7.6f;
            if ((from == RoomId.ComfortRoom || from == RoomId.ComfortRoom2) && to == RoomId.Hallway) return 0f;
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
