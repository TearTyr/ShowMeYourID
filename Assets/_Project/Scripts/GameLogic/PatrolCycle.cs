using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The patrol each normal night asks for: peek into Room 101, Room 202 and one of the
    /// comfort rooms. Either CR counts. The rooms are kept as bits in GameSession.ClearedRooms.</summary>
    public static class PatrolCycle
    {
        public static int Bit(RoomId room) => 1 << (int)room;

        // the four rooms the guard peeks into
        private static readonly RoomId[] PeekRooms =
            { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 };

        /// <summary>Does this peek room hold a standing anomaly tonight? Each room rolls for itself,
        /// from the run seed and the night, and at least one room always does. Standing anomalies
        /// never move: the player has to find them with the lamp. Same inputs, same answer.</summary>
        public static bool StandingIn(int seed, int shift, RoomId room)
        {
            int index = Array.IndexOf(PeekRooms, room);
            if (index < 0) return false;
            var rng = new GameRng(seed ^ (shift * 6151));
            bool any = false, mine = false;
            for (int i = 0; i < PeekRooms.Length; i++)
            {
                bool here = rng.Chance(GameRules.Current.StandingChance);
                any |= here;
                if (i == index) mine = here;
            }
            return any ? mine : index == rng.NextInt(0, PeekRooms.Length);
        }

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
