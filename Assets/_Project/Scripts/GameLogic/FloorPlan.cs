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
        /// <summary>The patrol room this square represents. Comfort rooms are map-only and
        /// carry null: they are on the plan, they are not part of the patrol graph.</summary>
        public RoomId? Room;
        /// <summary>The sealed server / identification room: restricted styling, never
        /// clickable - its locked behaviour is intentional (ClueRegistry "door").</summary>
        public bool Sealed;
        /// <summary>Comfort room (CR): labelled on the plan, no patrol mechanics.</summary>
        public bool Comfort;
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

    /// <summary>HOLLOWBROOK COLLEGE - GROUND FLOOR: the single source of truth for the map's
    /// layout, rebuilt to match the owner's hand-drawn reference plan: one long central
    /// hallway; BOTH classrooms (101 west of 202) along its north wall; the security
    /// checkpoint at the south-west, under the hallway's west end; the sealed server /
    /// identification room capping the east end with the two comfort rooms north and south
    /// of it; and the reference's stair hatch ("2ND FLOOR??") under the corridor's
    /// east-centre long wall.
    ///
    /// Everything is expressed in a fixed DESIGN SPACE with the origin at the sheet's centre,
    /// y down. MapView scales that space uniformly to whatever the screen can show, so the
    /// plan stays centred and fully visible at any resolution instead of being pinned to
    /// hand-tuned pixel offsets.</summary>
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
                Key = "room_hallway", Label = "MAIN HALLWAY",
                X = 10f, Y = 0f, W = 600f, H = 70f, Room = RoomId.Hallway,
            },
            new FloorRoom
            {
                // the reference's south-west corner: the checkpoint hangs under the
                // hallway's west end, its door in its own north wall
                Key = "room_security", Label = "SECURITY ROOM\nSecurity Checkpoint",
                X = -380f, Y = 110f, W = 170f, H = 120f, Room = RoomId.Security,
                DoorX = -295f, DoorY = 110f,   // its own east wall, into the return
            },
            new FloorRoom
            {
                // both classrooms on the NORTH wall, 101 west of 202, exactly as the
                // reference draws them; each door in its own south wall
                Key = "room_classroom1", Label = "CLASSROOM 1\nRoom 101",
                X = -160f, Y = -115f, W = 210f, H = 120f, Room = RoomId.Classroom1,
                DoorX = -160f, DoorY = -55f,
            },
            new FloorRoom
            {
                Key = "room_classroom2", Label = "CLASSROOM 2\nRoom 202",
                X = 70f, Y = -115f, W = 210f, H = 120f, Room = RoomId.Classroom2,
                DoorX = 70f, DoorY = -55f,
            },
            new FloorRoom
            {
                // the sealed room caps the hallway's east end: the corridor runs to it and
                // stops, which is exactly what the chain and the scratch marks say
                Key = "room_server", Label = "SERVER / \nIDENTIFICATION ROOM\n- SEALED -",
                X = 390f, Y = 0f, W = 150f, H = 130f, Room = null, Sealed = true,
                DoorX = 315f, DoorY = 0f,      // its own west wall, where the corridor ends
            },
            new FloorRoom
            {
                // the comfort rooms hang off the east end north and south of the sealed
                // room (the reference's small north room), off a short chalk return
                Key = "room_comfort_north", Label = "COMFORT ROOM\nCR-1",
                X = 390f, Y = -115f, W = 150f, H = 70f, Room = null, Comfort = true,
                DoorX = 315f, DoorY = -115f,   // on the room's own west wall (X - W/2 = 315)
            },
            new FloorRoom
            {
                Key = "room_comfort_south", Label = "COMFORT ROOM\nCR-2",
                X = 390f, Y = 115f, W = 150f, H = 70f, Room = null, Comfort = true,
                DoorX = 315f, DoorY = 115f,    // on the room's own west wall
            },
        };

        /// <summary>Corridor stubs drawn as thin chalk runs between a room and the hallway
        /// (design-space segments, x0,y0 -> x1,y1). Purely decorative: they show that every
        /// room hangs off the one central passage - the classrooms and the checkpoint
        /// straight into its long walls, the comfort rooms round an elbow return at the
        /// east end rather than blocking the corridor.</summary>
        public static readonly float[][] Corridors =
        {
            new[] { -295f, 110f, -270f, 110f },  // checkpoint door -> the west return
            new[] { -270f, 110f, -270f, 35f },   // return -> hallway south wall
            new[] { -160f, -55f, -160f, -35f },  // classroom 1 door -> hallway north wall
            new[] { 70f, -55f, 70f, -35f },      // classroom 2 door -> hallway north wall
            new[] { 310f, 0f, 315f, 0f },        // hallway east end -> the sealed plate
            new[] { 315f, -115f, 300f, -115f },  // CR-1 door -> the return
            new[] { 300f, -115f, 300f, -35f },   // return -> hallway north wall
            new[] { 315f, 115f, 300f, 115f },    // CR-2 door -> the return
            new[] { 300f, 115f, 300f, 35f },     // return -> hallway south wall
        };

        /// <summary>The reference's stair hatch: short chalk ticks under the corridor's
        /// east-centre long wall, with the custodian's scratch beside it. Decoration only -
        /// no room, no door, no mechanic; the patrol graph never learned about stairs.</summary>
        public static readonly float[][] StairsRuns =
        {
            new[] { 180f, 35f, 180f, 53f },
            new[] { 192f, 35f, 192f, 53f },
            new[] { 204f, 35f, 204f, 53f },
            new[] { 216f, 35f, 216f, 53f },
            new[] { 228f, 35f, 228f, 53f },
            new[] { 240f, 35f, 240f, 53f },
            new[] { 254f, 68f, 242f, 56f },     // the scratch's little arrow at the hatch
        };
        public const string StairsNote = "2ND FLOOR??";
        public const float StairsNoteX = 292f, StairsNoteY = 82f;

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
