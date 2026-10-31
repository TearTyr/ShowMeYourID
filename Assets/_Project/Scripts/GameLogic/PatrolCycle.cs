using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The patrol each normal night asks for: peek into Room 101, Room 202 and one of the
    /// comfort rooms. Either CR counts. The rooms are kept as bits in GameSession.ClearedRooms.</summary>
    public static class PatrolCycle
    {
        public static int Bit(RoomId room) => 1 << (int)room;

        private static bool Has(int cleared, RoomId room) => (cleared & Bit(room)) != 0;

        private static bool HasComfort(int cleared) =>
            Has(cleared, RoomId.ComfortRoom) || Has(cleared, RoomId.ComfortRoom2);

        public static bool Done(int cleared) =>
            Has(cleared, RoomId.Classroom1) && Has(cleared, RoomId.Classroom2) && HasComfort(cleared);

        /// <summary>What is still to do, as plain text ("" when the cycle is done).</summary>
        public static string Left(int cleared)
        {
            var left = new List<string>();
            if (!Has(cleared, RoomId.Classroom1)) left.Add("Room 101");
            if (!Has(cleared, RoomId.Classroom2)) left.Add("Room 202");
            if (!HasComfort(cleared)) left.Add("a CR");
            return string.Join(", ", left);
        }
    }
}
