using System;

namespace Hollowbrook.Logic
{
    /// <summary>What stands in a peek room.</summary>
    public enum PeekKind { Hood, Slide, Coverhug }

    /// <summary>What it does while the lamp is on it: stays where it stands, or rushes at the camera.</summary>
    public enum PeekReaction { Still, Rush }

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
            var xs = (float[])ComfortCeilingXs.Clone();
            if (Mirrored(room)) for (int i = 0; i < xs.Length; i++) xs[i] = -xs[i];
            return xs;
        }

        /// <summary>The height (above the floor line) where the picture's floor and walls seem to meet the eye level.</summary>
        public static float Horizon(RoomId room)
        {
            return room == RoomId.Classroom1 || room == RoomId.Classroom2 ? 1.7f : 0.2f;
        }

        /// <summary>How tall a kind stands at the front edge of the floor, in world units.</summary>
        public static float FrontHeight(PeekKind kind)
        {
            switch (kind)
            {
                case PeekKind.Hood: return 4.4f;
                case PeekKind.Slide: return 3.4f;
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
                default: return "char_hood";
            }
        }

        /// <summary>Half the width and half the height of the part of the figure the lamp must be on, from its height.</summary>
        public static float BodyHalfWidth(PeekKind kind, float height)
        {
            if (kind == PeekKind.Coverhug) return height * 0.9f;   // it is wide and low
            return height * (kind == PeekKind.Slide ? 0.30f : 0.25f);
        }

        public static float BodyHalfHeight(PeekKind kind, float height) => height * (kind == PeekKind.Coverhug ? 0.5f : 0.45f);

        // ---- the night's rolls: the same room and the same night always give the same answer

        private static GameRng Roll(int seed, int shift, RoomId room, int salt) => new GameRng(seed ^ (shift * 6151) ^ ((int)room * 977) ^ salt);

        /// <summary>Classrooms hold the hooded figure. A comfort room holds the crouched one or the ceiling hanger.</summary>
        public static PeekKind KindIn(int seed, int shift, RoomId room)
        {
            if (room == RoomId.Classroom1 || room == RoomId.Classroom2) return PeekKind.Hood;
            return Roll(seed, shift, room, 0x11).Chance(0.5f) ? PeekKind.Slide : PeekKind.Coverhug;
        }

        /// <summary>Which of the room's three spots it stands at tonight.</summary>
        public static int SpotIndexIn(int seed, int shift, RoomId room) => Roll(seed, shift, room, 0x22).NextInt(0, 3);

        /// <summary>Some rush at the camera while the lamp is on them, some just stand there. The hanger never moves.</summary>
        public static PeekReaction ReactionIn(int seed, int shift, RoomId room)
        {
            if (KindIn(seed, shift, room) == PeekKind.Coverhug) return PeekReaction.Still;
            return Roll(seed, shift, room, 0x33).Chance(0.5f) ? PeekReaction.Rush : PeekReaction.Still;
        }
    }

    /// <summary>How fast the lamp clears a figure it is held on (pure). Held on the body it fills a bar in
    /// the clear time (Focus is 1.3 times faster); off the body the bar drains back.</summary>
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
