using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>One room on the ground-floor plan. Pure data: the map view reads it, the
    /// headless proofs read it, and nothing in it touches the engine - so "is the sealed
    /// server room clickable?" and "do the comfort rooms overlap the hallway?" are questions
    /// that can be answered by a unit test instead of by eyeballing a screenshot.</summary>
    public struct FloorRoom
    {
        /// <summary>Stable hierarchy name ("room_hallway") - the map builds one node per room
        /// and the proofs look them up by this.</summary>
        public string Key;
        /// <summary>Hand-written label, \n separated.</summary>
        public string Label;
        /// <summary>Centre in the floor plan's own design space (see FloorPlan.PlanW/H).</summary>
        public float X, Y;
        public float W, H;
        /// <summary>The patrol room this square represents. The sealed server room carries null:
        /// it is on the plan but there is nothing to walk into.</summary>
        public RoomId? Room;
        /// <summary>The sealed server / identification room: restricted styling, never
        /// clickable - its locked behaviour is intentional (ClueRegistry "door").</summary>
        public bool Sealed;
        /// <summary>Comfort room (CR): peeked into from its door, like the classrooms.</summary>
        public bool Comfort;
        /// <summary>What the small map on the desk calls this room.</summary>
        public string MiniLabel;
        /// <summary>Where its door stands along the walkable hallway (world x). Null for the
        /// hallway itself. RoomBuilder and the maps both read this, so the patrol cannot
        /// disagree with them.</summary>
        public float? HallX;
        /// <summary>The name in the door's prompt.</summary>
        public string DoorName;
        /// <summary>The door's sprite. Null = the door is already painted in the corridor.</summary>
        public string DoorArt;
        /// <summary>Where the door to the hallway is drawn, in design space. Null = no door
        /// drawn (the sealed room's is drawn as a barred plate instead).</summary>
        public float? DoorX, DoorY;

        public float Left => X - W / 2f;
        public float Right => X + W / 2f;
        public float Top => Y - H / 2f;
        public float Bottom => Y + H / 2f;

        public bool Contains(float x, float y) => x >= Left && x <= Right && y >= Top && y <= Bottom;

        public bool Overlaps(FloorRoom o) =>
            Left < o.Right && o.Left < Right && Top < o.Bottom && o.Top < Bottom;
    }

    /// <summary>HOLLOWBROOK COLLEGE - GROUND FLOOR: the single source of truth for the school.
    /// The small map on the desk, the full plan and the walkable hallway are all built from this
    /// table. The hallway strip is the walkable hallway to scale: world x from -15 to 15 maps to
    /// design x from -390 to 390 (UnitsPerWorldX per world unit), and every room's door sits at
    /// its real HallX on that scale. Walking east you pass, in order: the Security Room (south),
    /// Room 101, Room 202, CR-1 (north), the sealed server room (south) and CR-2 (north). The
    /// plan therefore spreads over the whole width of the sheet.
    ///
    /// Everything is expressed in a fixed DESIGN SPACE with the origin at the sheet centre,
    /// y down. MapView scales that space uniformly to whatever the screen can show, and the desk
    /// map is the same plan drawn smaller (MiniBox).</summary>
    public static class FloorPlan
    {
        public static readonly float DesignW = 940f;
        public static readonly float DesignH = 440f;
        /// <summary>The drawn plan's own area (all the rooms with a little air), centred on the
        /// design origin. The plan is scaled to fill the content area of the sheet from THIS size, not
        /// from the whole sheet, so it uses the full width of the paper.</summary>
        public static readonly float PlanW = 900f;
        public static readonly float PlanH = 270f;
        /// <summary>Design units on the sheet per world unit along the hallway.</summary>
        public const float UnitsPerWorldX = 26f;
        public const string Title = "HOLLOWBROOK COLLEGE - GROUND FLOOR";

        /// <summary>The plan rooms, in draw order (hallway first so rooms sit on it).</summary>
        public static readonly FloorRoom[] Rooms =
        {
            new FloorRoom
            {
                Key = "room_hallway", Label = "MAIN HALLWAY", MiniLabel = "MAIN HALL",
                X = 0f, Y = 10f, W = 780f, H = 64f, Room = RoomId.Hallway,
            },
            new FloorRoom
            {
                Key = "room_security", Label = "SECURITY ROOM\nSecurity Checkpoint", MiniLabel = "SECURITY",
                X = -358.8f, Y = 95.5f, W = 160f, H = 70f, Room = RoomId.Security,
                DoorX = -358.8f, DoorY = 60.5f,
                HallX = -13.8f, DoorName = "Security", DoorArt = "prop_door_security",
            },
            new FloorRoom
            {
                Key = "room_classroom1", Label = "CLASSROOM 1\nRoom 101", MiniLabel = "ROOM 101",
                X = -228.8f, Y = -88f, W = 170f, H = 80f, Room = RoomId.Classroom1,
                DoorX = -228.8f, DoorY = -48f,
                HallX = -8.8f, DoorName = "Room 101",
            },
            new FloorRoom
            {
                Key = "room_classroom2", Label = "CLASSROOM 2\nRoom 202", MiniLabel = "ROOM 202",
                X = 10.4f, Y = -88f, W = 170f, H = 80f, Room = RoomId.Classroom2,
                DoorX = 10.4f, DoorY = -48f,
                HallX = 0.4f, DoorName = "Room 202",
            },
            new FloorRoom
            {
                Key = "room_cr1", Label = "CR-1", MiniLabel = "CR-1",
                X = 223.6f, Y = -76f, W = 100f, H = 56f, Room = RoomId.ComfortRoom, Comfort = true,
                DoorX = 223.6f, DoorY = -48f,
                HallX = 8.6f, DoorName = "CR-1", DoorArt = "prop_door_comfort",
            },
            new FloorRoom
            {
                Key = "room_server", Label = "SERVER - SEALED", MiniLabel = "SERVER - SEALED",
                X = 286f, Y = 95.5f, W = 140f, H = 70f, Room = null, Sealed = true,
                DoorX = 286f, DoorY = 60.5f,
                HallX = 11.0f, DoorArt = "prop_server_door",
            },
            new FloorRoom
            {
                Key = "room_cr2", Label = "CR-2", MiniLabel = "CR-2",
                X = 345.8f, Y = -76f, W = 100f, H = 56f, Room = RoomId.ComfortRoom2, Comfort = true,
                DoorX = 345.8f, DoorY = -48f,
                HallX = 13.3f, DoorName = "CR-2", DoorArt = "prop_door_comfort",
            },
        };

        /// <summary>Thin chalk runs joining each room door to the hallway wall (x0,y0 -> x1,y1). Decoration.</summary>
        public static readonly float[][] Corridors =
        {
            new[] { -358.8f, 60.5f, -358.8f, 42f },   // security door -> hallway south wall
            new[] { -228.8f, -48f, -228.8f, -22f },   // room 101 door -> hallway north wall
            new[] { 10.4f, -48f, 10.4f, -22f },       // room 202 door -> hallway north wall
            new[] { 223.6f, -48f, 223.6f, -22f },     // CR-1 door -> hallway north wall
            new[] { 286f, 60.5f, 286f, 42f },         // sealed server door -> hallway south wall
            new[] { 345.8f, -48f, 345.8f, -22f },     // CR-2 door -> hallway north wall
        };

        // The desk map is the same plan, drawn this many times smaller, around this point (the
        // plan centre is a little west of the sheet centre, so the point is a little east).
        private const float MiniScale = 3.3f, MiniCentreX = 150f, MiniCentreY = 99f;

        /// <summary>Where a room sits on the small desk map (top-left corner and size, in the
        /// map card own units).</summary>
        public static void MiniBox(FloorRoom r, out float x, out float y, out float w, out float h)
        {
            x = r.Left / MiniScale + MiniCentreX;
            y = r.Top / MiniScale + MiniCentreY;
            w = r.W / MiniScale;
            h = r.H / MiniScale;
        }

        /// <summary>Every room the player can reach must be on the plan - the proof that the
        /// map and the patrol never drift apart.</summary>
        public static FloorRoom? ForRoom(RoomId id)
        {
            foreach (var r in Rooms)
                if (r.Room.HasValue && r.Room.Value == id) return r;
            return null;
        }

        public static FloorRoom? ForKey(string key)
        {
            foreach (var r in Rooms)
                if (r.Key == key) return r;
            return null;
        }

        public static List<FloorRoom> ComfortRooms()
        {
            var list = new List<FloorRoom>();
            foreach (var r in Rooms) if (r.Comfort) list.Add(r);
            return list;
        }

        /// <summary>True when no two rooms overlap and the whole plan stays inside the plan area
        /// (which itself sits inside the sheet) - the layout invariant the reference drawing implies.</summary>
        public static bool LayoutIsSane(out string problem)
        {
            float halfW = PlanW / 2f, halfH = PlanH / 2f;
            for (int i = 0; i < Rooms.Length; i++)
            {
                var a = Rooms[i];
                if (a.Left < -halfW || a.Right > halfW || a.Top < -halfH || a.Bottom > halfH)
                {
                    problem = a.Key + " falls outside the plan area";
                    return false;
                }
                for (int j = i + 1; j < Rooms.Length; j++)
                    if (a.Overlaps(Rooms[j]))
                    {
                        problem = a.Key + " overlaps " + Rooms[j].Key;
                        return false;
                    }
            }
            problem = null;
            return true;
        }

        /// <summary>Uniform scale that fits the plan area inside (availW, availH) - the one
        /// number MapView multiplies every coordinate by, so the plan is centred and never
        /// clipped whatever the window size.</summary>
        public static float FitScale(float availW, float availH)
        {
            if (PlanW <= 0f || PlanH <= 0f) return 1f;
            float s = System.Math.Min(availW / PlanW, availH / PlanH);
            return s > 0f ? s : 1f;
        }

        /// <summary>Where the "YOU ARE HERE" marker and the finding dots sit for a room:
        /// its centre in design space.</summary>
        public static bool TryMarkerPos(RoomId id, out float x, out float y)
        {
            var r = ForRoom(id);
            if (r.HasValue) { x = r.Value.X; y = r.Value.Y; return true; }
            x = 0f; y = 0f;
            return false;
        }
    }
}
