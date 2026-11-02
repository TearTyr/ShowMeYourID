using System;

namespace Hollowbrook.Logic
{
    /// <summary>What stands in a peek room. Any room can hold any of the four: the hooded figure, the crouched
    /// slider, the ceiling hanger, or the runner low on the floor.</summary>
    public enum PeekKind { Hood, Slide, Coverhug, Runner }

    public struct PeekSpot
    {
        public float X;       // where it stands, along the room
        public float FeetY;   // where its feet are on the painted floor
    }

    /// <summary>Where things stand in a peek room, and how big they are there (pure, no Unity).
    /// The rooms are single flat pictures, so every spot was picked by eye on clear painted floor (never on a
    /// chair, a desk or a sink) and the size follows the picture's perspective: the further back the feet,
    /// the closer to the room's horizon, the smaller the figure. Rooms 202 and CR-2 are drawn mirrored, so
    /// their spots are in game coordinates already.</summary>
    public static class PeekSpots
    {
        public const float FrontFeetY = -3.1f;   // the floor line at the front edge of the picture
        public const float ScreenTopY = 5f;      // the top of the screen

        private static readonly PeekSpot[] Classroom1 =
            { new PeekSpot { X = 0.9f, FeetY = -2.9f }, new PeekSpot { X = 2.6f, FeetY = -2.0f }, new PeekSpot { X = -0.6f, FeetY = -1.4f } };
        private static readonly PeekSpot[] Classroom2 =
            { new PeekSpot { X = -2.4f, FeetY = -2.8f }, new PeekSpot { X = -0.8f, FeetY = -2.2f }, new PeekSpot { X = -2.8f, FeetY = -1.5f } };
        private static readonly PeekSpot[] ComfortRoom =
            { new PeekSpot { X = -2.2f, FeetY = -2.6f }, new PeekSpot { X = -1.2f, FeetY = -3.0f }, new PeekSpot { X = -3.6f, FeetY = -3.0f } };

        // where the ceiling hanger can hang in a comfort room (above the corridor and its light)
        private static readonly float[] ComfortCeilingXs = { -2.5f, -0.5f, 2.0f };
        // ...and in a classroom (above the desks, clear of the board and the door)
        private static readonly float[] ClassroomCeilingXs = { -3.5f, 0.5f, 3.5f };

        private static bool IsClassroom(RoomId room) => room == RoomId.Classroom1 || room == RoomId.Classroom2;

        private static bool Mirrored(RoomId room) => room == RoomId.ComfortRoom2;

        /// <summary>The floor spots of a room: three each, in game coordinates.</summary>
        public static PeekSpot[] For(RoomId room)
        {
            switch (room)
            {
                case RoomId.Classroom1: return (PeekSpot[])Classroom1.Clone();
                case RoomId.Classroom2: return (PeekSpot[])Classroom2.Clone();
                default:
                {
                    var spots = (PeekSpot[])ComfortRoom.Clone();
                    if (Mirrored(room)) for (int i = 0; i < spots.Length; i++) spots[i].X = -spots[i].X;
                    return spots;
                }
            }
        }

        /// <summary>The x positions a ceiling hanger can hang at in this room.</summary>
        public static float[] CeilingXs(RoomId room)
        {
            if (IsClassroom(room)) return (float[])ClassroomCeilingXs.Clone();
            var xs = (float[])ComfortCeilingXs.Clone();
            if (Mirrored(room)) for (int i = 0; i < xs.Length; i++) xs[i] = -xs[i];
            return xs;
        }

        /// <summary>The height (above the floor line) where the picture's floor and walls seem to meet the eye level.</summary>
        public static float Horizon(RoomId room)
        {
            return IsClassroom(room) ? 1.7f : 0.2f;
        }

        /// <summary>How tall a kind stands at the front edge of the floor, in world units.</summary>
        public static float FrontHeight(PeekKind kind)
        {
            switch (kind)
            {
                case PeekKind.Hood: return 4.4f;
                case PeekKind.Slide: return 3.4f;
                case PeekKind.Runner: return 2.5f;   // it lies low on the floor, about 4.5 wide
                default: return 1.9f;   // the hanger does not stand: it hangs, at this height
            }
        }

        /// <summary>How tall it is with its feet at feetY: full size at the front, smaller towards the horizon.</summary>
        public static float HeightAt(PeekKind kind, RoomId room, float feetY)
        {
            if (kind == PeekKind.Coverhug) return FrontHeight(kind);
            float horizon = Horizon(room);
            return FrontHeight(kind) * (horizon - feetY) / (horizon - FrontFeetY);
        }

        public static string ArtKey(PeekKind kind)
        {
            switch (kind)
            {
                case PeekKind.Slide: return "char_anomaly";
                case PeekKind.Coverhug: return "char_anomaly_coverhug";
                case PeekKind.Runner: return "char_anomaly_sprint_a";
                default: return "char_hood";
            }
        }

        /// <summary>Half the width and half the height of the part of the figure the lamp must be on, from its height.</summary>
        public static float BodyHalfWidth(PeekKind kind, float height)
        {
            if (kind == PeekKind.Coverhug || kind == PeekKind.Runner) return height * 0.9f;   // they are wide and low
            return height * (kind == PeekKind.Slide ? 0.30f : 0.25f);
        }

        public static float BodyHalfHeight(PeekKind kind, float height) => height * (kind == PeekKind.Coverhug || kind == PeekKind.Runner ? 0.5f : 0.45f);

        // ---- the night's rolls: the same room and the same night always give the same answer

        private static GameRng Roll(int seed, int shift, RoomId room, int salt) => new GameRng(seed ^ (shift * 6151) ^ ((int)room * 977) ^ salt);

        // the four peek rooms, in the order the kinds are dealt out
        private static int Slot(RoomId room)
        {
            switch (room)
            {
                case RoomId.Classroom1: return 0;
                case RoomId.Classroom2: return 1;
                case RoomId.ComfortRoom: return 2;
                default: return 3;
            }
        }

        /// <summary>Any room can hold any kind. The night picks a starting kind and the four rooms take the four
        /// kinds in turn, so a night with several figures shows different ones, and no night is all hoods.</summary>
        public static PeekKind KindIn(int seed, int shift, RoomId room)
        {
            int kinds = 4;
            int start = Roll(seed, shift, RoomId.Hallway, 0x11).NextInt(0, kinds);
            return (PeekKind)((start + Slot(room)) % kinds);
        }

        /// <summary>Which of the room's three spots it stands at tonight.</summary>
        public static int SpotIndexIn(int seed, int shift, RoomId room) => Roll(seed, shift, room, 0x22).NextInt(0, 3);
    }

    /// <summary>How fast the lamp clears a figure it is held on (pure). The plain wide lamp works, it is just
    /// slow: held on the body it fills a bar in the clear time, and Focus is 1.3 times faster. Off the body
    /// the bar drains back.</summary>
    public static class PeekClear
    {
        public const float FocusSpeed = 1.3f;
        public const float DrainPerSecond = 0.5f;   // a share of the whole bar per second

        public static float SecondsToClear(bool focused, float clearSeconds) => clearSeconds / (focused ? FocusSpeed : 1f);

        public static float Advance(float progress, float dt, bool onBody, bool focused, float clearSeconds)
        {
            if (dt <= 0f) return progress;
            if (onBody)
                return Math.Min(1f, progress + dt / SecondsToClear(focused, clearSeconds));
            return Math.Max(0f, progress - DrainPerSecond * dt);
        }
    }
}
