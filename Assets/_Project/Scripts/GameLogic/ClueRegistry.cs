using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class ClueRegistry
    {
        public static List<ClueDef> Build()
        {
            return new List<ClueDef>
            {
                new ClueDef { Order = 1, Key = "light", Display = "Flickering Light", Room = RoomId.Hallway,
                    InspectText = "The hallway lights stutter in a slow rhythm — three dark beats, then a bright one. A maintenance log is taped beneath the fixture: “Zone 3 flicker since the server room was sealed. District says ignore it.”",
                    ChainHint = "Someone scratched an arrow under the log, pointing into Classroom 1.",
                    SanityDelta = -2f, IsScare = false },
                new ClueDef { Order = 2, Key = "chalkboard", Display = "Chalkboard", Room = RoomId.Classroom1,
                    InspectText = "Old lesson notes have been half-erased. Underneath, in fresh chalk: “HB-2024-0212 WAS NEVER A REAL STUDENT.” The writing is not on any lesson plan.",
                    ChainHint = "Chalk dust trails from the board to the front desk.",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 3, Key = "desk", Display = "Front Desk", Room = RoomId.Classroom1,
                    InspectText = "The teacher's desk drawer is stuck, then gives. Inside: a folded note on district letterhead. “They wear our students' faces. Don't let them in. — E.R., night custodian (resigned).”",
                    ChainHint = "The note ends with: “Start with the sealed door at the end of the hall.”",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 4, Key = "door", Display = "Sealed Door", Room = RoomId.Hallway,
                    InspectText = "The door at the end of the hall is chained from the outside. A plate reads “SERVER / IDENTIFICATION ROOM”. The wood around the frame is scarred with scratch marks — from the inside.",
                    ChainHint = "Through the door's small wired glass you can see Classroom 2's far window glowing faintly.",
                    SanityDelta = -6f, IsScare = true },
                new ClueDef { Order = 5, Key = "window", Display = "Classroom Window", Room = RoomId.Classroom2,
                    InspectText = "Outside, under the streetlight, three figures stand motionless in the yard, faces up toward this window. You blink. The yard is empty. The glass is warm.",
                    ChainHint = "When you step back, your reflection is a beat late — and it looks toward the hallway notice board.",
                    SanityDelta = -8f, IsScare = true },
                new ClueDef { Order = 6, Key = "board", Display = "Notice Board", Room = RoomId.Hallway,
                    InspectText = "Faded announcements: “STUDENT IDENTIFICATION PROJECT — photos, records and personal information will be combined into one system. Volunteer sign-up inside.” Someone added in pen: “It was never voluntary.”",
                    ChainHint = "A pin holds a torn corner of a class photo. The rest of it hangs near the lost-and-found.",
                    SanityDelta = -4f, IsScare = false },
                new ClueDef { Order = 7, Key = "lostid", Display = "Lost Student ID", Room = RoomId.Classroom1,
                    InspectText = "A student ID under the lost-and-found shelf. The name field has been scratched out. The photo is the previous custodian — the one who “resigned”. Status: REGULAR. Year: 1st.",
                    ChainHint = "On the back, in pencil: “They put me in the system too. Look at the old picture in the hall.”",
                    SanityDelta = -6f, IsScare = true },
                new ClueDef { Order = 8, Key = "photo", Display = "Old School Photo", Room = RoomId.Hallway,
                    InspectText = "A framed school picture, decades old. Rows of identical faces. In the back corner stands a staff member in a formal coat who has not aged a day — the same face as the district administrator on tonight's roster memo.",
                    ChainHint = "Under the frame: a cabinet key labeled “C-2”, for the filing cabinet in Classroom 2.",
                    SanityDelta = -6f, IsScare = false },
                new ClueDef { Order = 9, Key = "cabinet", Display = "Filing Cabinet", Room = RoomId.Classroom2,
                    InspectText = "The key turns. The folder is stamped “CONFIDENTIAL — STUDENT IDENTIFICATION PROJECT — APPROVED”. Inside: the system began generating identities that belonged to no real student. The district sealed the server room and closed the school mid-term. A custodian's note is taped inside the cover: “Don't let them in.”",
                    ChainHint = "The last page is a work order: “Replace hallway clock — stopped 1:47 AM.”",
                    SanityDelta = -6f, IsScare = false },
                new ClueDef { Order = 10, Key = "clock", Display = "Stopped Clock", Room = RoomId.Hallway,
                    InspectText = "The hallway clock is frozen at 1:47 AM — the minute the closure order was signed, according to the folder. As you watch, the minute hand twitches backward. Somewhere behind the walls, a server fan spins up.",
                    ChainHint = "You now know where they are all trying to go. Keep them out.",
                    SanityDelta = -10f, IsScare = true },
            };
        }
    }

    public class ClueDiscoveryResult
    {
        public bool FirstTime;
        public bool AdvancedSequence;  // this was the next clue in the intended order
        public bool BrokeSequence;     // found out of order — secret ending no longer available
        public ClueDef Def;
    }

    public class ClueTracker
    {
        private readonly Dictionary<string, ClueDef> _byKey = new Dictionary<string, ClueDef>();
        private readonly HashSet<string> _found = new HashSet<string>();
        private readonly HashSet<RoomId> _investigatedRooms = new HashSet<RoomId>();
        private int _nextInSequence = 1;

        public bool OrderIntact { get; private set; } = true;
        public int FoundCount => _found.Count;
        public int TotalCount => _byKey.Count;
        public IReadOnlyCollection<string> FoundKeys => _found;
        public IReadOnlyCollection<RoomId> InvestigatedRooms => _investigatedRooms;
        public int NextExpectedOrder => _nextInSequence;

        public ClueTracker()
        {
            foreach (var c in ClueRegistry.Build()) _byKey[c.Key] = c;
        }

        public ClueDef Get(string key) => _byKey.TryGetValue(key, out var c) ? c : null;

        public ClueDiscoveryResult Discover(string key)
        {
            var res = new ClueDiscoveryResult();
            if (!_byKey.TryGetValue(key, out var def)) return res;
            res.Def = def;
            MarkRoomInvestigated(def.Room);
            if (_found.Contains(key)) { res.FirstTime = false; return res; }
            _found.Add(key);
            res.FirstTime = true;
            if (def.Order == _nextInSequence)
            {
                res.AdvancedSequence = true;
                _nextInSequence++;
            }
            else
            {
                res.BrokeSequence = OrderIntact; // first out-of-order find breaks it
                OrderIntact = false;
            }
            return res;
        }

        public void MarkRoomInvestigated(RoomId room)
        {
            if (room != RoomId.Security) _investigatedRooms.Add(room);
        }

        public bool AllFound => _found.Count >= _byKey.Count;

        public bool SecretConditionsMet =>
            AllFound && OrderIntact &&
            _investigatedRooms.Contains(RoomId.Hallway) &&
            _investigatedRooms.Contains(RoomId.Classroom1) &&
            _investigatedRooms.Contains(RoomId.Classroom2);

        public List<string> SaveFound() { var l = new List<string>(_found); l.Sort(System.StringComparer.Ordinal); return l; }
        public void LoadFound(IEnumerable<string> keys)
        {
            _found.Clear(); _nextInSequence = 1; OrderIntact = true;
            if (keys == null) return;
            foreach (var k in keys)
            {
                if (!_byKey.ContainsKey(k)) continue;
                _found.Add(k);
                if (_byKey[k].Order == _nextInSequence) _nextInSequence++;
            }
        }
        public List<string> SaveRooms()
        {
            var l = new List<string>();
            foreach (var r in _investigatedRooms) l.Add(r.ToString());
            l.Sort(System.StringComparer.Ordinal);
            return l;
        }
        public void LoadRooms(IEnumerable<string> rooms)
        {
            _investigatedRooms.Clear();
            if (rooms == null) return;
            foreach (var r in rooms)
                if (System.Enum.TryParse(r, out RoomId room)) _investigatedRooms.Add(room);
        }
        public void LoadOrderIntact(bool intact, int nextOrder)
        {
            OrderIntact = intact;
            _nextInSequence = nextOrder;
        }
    }
}
