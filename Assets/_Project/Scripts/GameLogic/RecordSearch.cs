using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Registry access for the records station. The desk UI is the FLOOR FILES
    /// cabinet: canon students are filed by the floor of their first classroom (Room 101 =
    /// 1st floor, Room 202 = 2nd floor) and the player browses folders instead of typing.
    /// A presented card whose number matches no filed record is itself a clue — ghost IDs
    /// are not in the registry.
    /// Pure and deterministic: same canon = same folders and same answers, always.</summary>
    public static class RecordSearch
    {
        /// <summary>One drawer folder: a floor, its display label, and the canon indices
        /// filed under it (roster order).</summary>
        public class FloorFolder
        {
            public int Floor;                                  // 1-based floor number
            public string Label = "";                          // e.g. "1ST FLOOR · ROOM 101"
            public List<int> Indices = new List<int>();        // indices into canon, roster order
        }

        /// <summary>The floor a classroom is on: Room 101 sits on 1, Room 202 on 2.</summary>
        public static int FloorOf(RoomId room) => room == RoomId.Classroom1 ? 1 : 2;

        /// <summary>Display label of a floor folder. Room numbers follow the building:
        /// floor N is Room N0N (101, 202 — the rooms StudentIdentity.ClassroomString names).</summary>
        public static string FloorLabel(int floor)
        {
            string ord = floor == 1 ? "1ST" : floor == 2 ? "2ND" : floor == 3 ? "3RD" : floor + "TH";
            return ord + " FLOOR · ROOM " + (floor * 100 + floor);
        }

        /// <summary>Files the canon roster into floor folders: one folder per floor present,
        /// floors ascending, students in roster order inside their folder. A null or empty
        /// canon files nothing.</summary>
        public static List<FloorFolder> BuildFloorFolders(IReadOnlyList<StudentIdentity> canon)
        {
            var folders = new List<FloorFolder>();
            if (canon == null) return folders;
            for (int i = 0; i < canon.Count; i++)
            {
                var s = canon[i];
                if (s == null) continue;
                int floor = FloorOf(s.FirstClassroom);
                FloorFolder f = null;
                foreach (var cand in folders)
                    if (cand.Floor == floor) { f = cand; break; }
                if (f == null)
                {
                    f = new FloorFolder { Floor = floor, Label = FloorLabel(floor) };
                    folders.Add(f);
                }
                f.Indices.Add(i);
            }
            folders.Sort((a, b) => a.Floor.CompareTo(b.Floor));
            return folders;
        }
    }
}
