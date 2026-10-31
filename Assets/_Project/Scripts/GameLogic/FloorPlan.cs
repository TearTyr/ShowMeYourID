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
        /// <summary>Centre in the floor plan's own design space (see FloorPlan.DesignW/H).</summary>
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
        /// hallway itself. RoomGraph and RoomBuilder both read this, so the patrol cannot
        /// disagree with the maps.</summary>
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
    /// table. It follows the desk map: the security room at the south-west, Room 101 and Room 202
    /// along the north of the main hall, and CR-1, the sealed server room and CR-2 down the east
    /// end. Walking east along the hallway you pass them in that order.
    ///
    /// Everything is expressed in a fixed DESIGN SPACE with the origin at the sheet's centre,
    /// y down. MapView scales that space uniformly to whatever the screen can show, and the desk
    /// map is the same plan drawn smaller (MiniBox).</summary>
    public static class FloorPlan
    {
        public static readonly float DesignW = 940f;
        public static readonly float DesignH = 440f;
        public const string Title = "HOLLOWBROOK COLLEGE - GROUND FLOOR";

        /// <summary>The plan's own rooms, in draw order (hallway first so rooms sit on it).</summary>
        public static readonly FloorRoom[] Rooms =
        {
            new FloorRoom
            {
                Key = "room_hallway", Label = "MAIN HALLWAY", MiniLabel = "MAIN HALL",
                X = -29f, Y = 10f, W = 518f, H = 64f, Room = RoomId.Hallway,
            },
            new FloorRoom
            {
                Key = "room_security", Label = "SECURITY ROOM\nSecurity Checkpoint", MiniLabel = "SECURITY",
                X = -317f, Y = 99f, W = 160f, H = 77f, Room = RoomId.Security,
                DoorX = -262f, DoorY = 60.5f,
                HallX = -13.8f, DoorName = "Security", DoorArt = "prop_door_security",
            },
            new FloorRoom
            {
                Key = "room_classroom1", Label = "CLASSROOM 1\nRoom 101", MiniLabel = "ROOM 101",
                X = -150f, Y = -93f, W = 186f, H = 90f, Room = RoomId.Classroom1,
                DoorX = -150f, DoorY = -48f,
                HallX = -8.8f, DoorName = "Room 101",
            },
            new FloorRoom
            {
                Key = "room_classroom2", Label = "CLASSROOM 2\nRoom 202", MiniLabel = "ROOM 202",
                X = 58f, Y = -93f, W = 186f, H = 90f, Room = RoomId.Classroom2,
                DoorX = 58f, DoorY = -48f,
                HallX = 0.4f, DoorName = "Room 202",
            },
            new FloorRoom
            {
                Key = "room_cr1", Label = "CR-1", MiniLabel = "CR-1",
                X = 294f, Y = -106f, W = 128f, H = 64f, Room = RoomId.ComfortRoom, Comfort = true,
                DoorX = 230f, DoorY = -106f,
                HallX = 8.6f, DoorName = "CR-1", DoorArt = "prop_door_comfort",
            },
            new FloorRoom
            {
                Key = "room_server", Label = "SERVER - SEALED", MiniLabel = "SERVER - SEALED",
                X = 314f, Y = -6f, W = 166f, H = 83f, Room = null, Sealed = true,
                DoorX = 231f, DoorY = -6f,
                HallX = 11.0f, DoorArt = "prop_server_door",
            },
            new FloorRoom
            {
                Key = "room_cr2", Label = "CR-2", MiniLabel = "CR-2",
                X = 294f, Y = 93f, W = 128f, H = 64f, Room = RoomId.ComfortRoom2, Comfort = true,
                DoorX = 230f, DoorY = 93f,
                HallX = 13.3f, DoorName = "CR-2", DoorArt = "prop_door_comfort",
            },
        };

        /// <summary>Thin chalk runs joining each room to the hallway (x0,y0 -> x1,y1). Decoration.</summary>
        public static readonly float[][] Corridors =
        {
            new[] { -262f, 60.5f, -262f, 42f },   // security door -> hallway south wall
            new[] { -150f, -48f, -150f, -22f },   // room 101 door -> hallway north wall
            new[] { 58f, -48f, 58f, -22f },       // room 202 door -> hallway north wall
            new[] { 225f, -22f, 225f, -106f },    // hallway east end -> up to CR-1
            new[] { 225f, -106f, 230f, -106f },
            new[] { 225f, 42f, 225f, 93f },       // hallway east end -> down to CR-2
            new[] { 225f, 93f, 230f, 93f },
        };

        // The desk map is the same plan, drawn this many times smaller, around this point.
        private const float MiniScale = 3.2f, MiniCentreX = 142f, MiniCentreY = 99f;

        /// <summary>Where a room sits on the small desk map (top-left corner and size, in the
        /// map card's own units).</summary>
        public static void MiniBox(FloorRoom r, out float x, out float y, out float w, out float h)
        {
            x = r.Left / MiniScale + MiniCentreX;
            y = r.Top / MiniScale + MiniCentreY;
            w = r.W / MiniScale;
            h = r.H / MiniScale;
        }

        /// <summary>Every room that is part of the patrol graph must be on the plan - the
        /// proof that the map and RoomGraph never drift apart.</summary>
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

        /// <summary>True when no two rooms overlap and the whole plan stays inside the design
        /// sheet - the layout invariant the reference drawing implies.</summary>
        public static bool LayoutIsSane(out string problem)
        {
            float halfW = DesignW / 2f, halfH = DesignH / 2f;
            for (int i = 0; i < Rooms.Length; i++)
            {
                var a = Rooms[i];
                if (a.Left < -halfW || a.Right > halfW || a.Top < -halfH || a.Bottom > halfH)
                {
                    problem = a.Key + " falls outside the sheet";
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

        /// <summary>Uniform scale that fits the design space inside (availW, availH) - the one
        /// number MapView multiplies every coordinate by, so the plan is centred and never
        /// clipped whatever the window size.</summary>
        public static float FitScale(float availW, float availH)
        {
            if (DesignW <= 0f || DesignH <= 0f) return 1f;
            float s = System.Math.Min(availW / DesignW, availH / DesignH);
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
