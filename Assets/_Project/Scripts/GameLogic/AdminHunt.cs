using System;

namespace Hollowbrook.Logic
{
    // Night 5 after the administrator is refused (pure rules, no UnityEngine).
    //
    // It plays like Nightmare Fredbear's night in Five Nights at Freddy's 4: he is the only
    // thing in the building, you listen for where his footsteps stop, and you survive until
    // dawn. Each time he stops, he is in one of three kinds of place:
    //   - AT A CLASSROOM DOOR (the game's left and right halls): stand at that door and hold E
    //     until he gives up.
    //   - IN A COMFORT ROOM (the closet): peek in and keep the lamp on his head.
    //   - BEHIND YOU in the hallway (the bed): flash his face with the beam.
    // Too slow and he catches you. CatchesToLose catches and he walks to the server door.
    //
    // The hard rule: nobody can ever get stuck. The dawn clock always runs and every phase has
    // a time limit, so the hunt ends in a win or a loss whatever the player does or does not do.
    // AdminHuntTests runs random and idle input to prove it.

    public enum HuntPhase
    {
        Stalking,   // walking the halls: footsteps, no danger yet
        AtDoor,     // behind a classroom door: hold it shut
        Closet,     // in CR-1 or CR-2: find him with the lamp
        Behind,     // right behind the player in the hallway: flash him
        Retreat,    // driven back or done catching you: harmless for a moment
        Won,        // dawn came
        Lost,       // caught too many times
    }

    /// <summary>Tunable numbers. Defaults are the shipped feel; GameConfigSO overrides them.</summary>
    public struct HuntTuning
    {
        public int CatchesToLose;        // catches until he reaches the server door
        public float SurviveSeconds;     // the dawn clock
        public float StalkMinSeconds;    // shortest walk between stops
        public float StalkMaxSeconds;    // longest walk between stops (halves by dawn)
        public float DoorSeconds;        // time to reach the door he stopped at and hold it
        public float DoorHoldSeconds;    // seconds of holding the door that send him away
        public float ClosetSeconds;      // time to find him in the comfort room
        public float ClosetHoldSeconds;  // seconds of lamp on his head that send him away
        public float HoldDecayPerSecond; // how fast a hold drains when you let go
        public float HeadRadius;         // world units: how close the cursor must be to his head
        public float BehindSeconds;      // time to flash him when he is behind you
        public float BeamHitSeconds;     // seconds of beam on his face that count as a hit
        public float RetreatSeconds;     // harmless time after he leaves

        public static HuntTuning Default => new HuntTuning
        {
            CatchesToLose = 3, SurviveSeconds = 210f, StalkMinSeconds = 5f, StalkMaxSeconds = 10f,
            DoorSeconds = 10f, DoorHoldSeconds = 2f, ClosetSeconds = 12f, ClosetHoldSeconds = 1.8f,
            HoldDecayPerSecond = 0.5f, HeadRadius = 1.0f, BehindSeconds = 2.6f, BeamHitSeconds = 0.35f,
            RetreatSeconds = 1.5f,
        };
    }

    /// <summary>What the player is doing this frame (filled in by the director from the world).</summary>
    public struct HuntInput
    {
        public bool Peeking;        // a peek view is open
        public RoomId PlayerRoom;   // the room on screen (the peeked room, or the hallway)
        public bool OnTarget;       // peeking, lamp lit, cursor on his head
        public bool InHallway;      // walking the hallway (not peeking, not mid-transition)
        public bool BeamOnFace;     // the hallway beam covers his face
        public bool HoldingDoor;    // standing at the door he is behind, holding E
    }

    [Flags]
    public enum HuntEvent
    {
        None = 0,
        StalkStart = 1,     // footsteps begin
        ReachedDoor = 2,    // the footsteps stopped at a classroom door
        EnteredCloset = 4,  // he went into the comfort room
        Spotted = 8,        // the player's lamp view found him in the comfort room
        AppearedBehind = 16,// he is right behind the player
        Repelled = 32,      // the player sent him away
        Caught = 64,        // he reached the player
        Won = 128,
        Lost = 256,
    }

    public class AdminHunt
    {
        private enum Spot { Classroom1, Classroom2, Closet, Behind }

        private readonly GameRng _rng;
        private float _stalkLeft;
        private bool _spotted;
        private int _lastSpot = -1;

        public HuntTuning Tuning { get; }
        public HuntPhase Phase { get; private set; } = HuntPhase.Stalking;
        /// <summary>Where he stopped: a classroom, the comfort room, or the hallway when behind you.</summary>
        public RoomId Room { get; private set; } = RoomId.Hallway;
        public int Repels { get; private set; }
        public int Catches { get; private set; }
        public float SurviveLeft { get; private set; }
        /// <summary>0..1 progress of the current hold (door or lamp).</summary>
        public float Hold { get; private set; }
        public float PhaseTimer { get; private set; }
        public float BeamHit { get; private set; }
        public bool Finished => Phase == HuntPhase.Won || Phase == HuntPhase.Lost;
        /// <summary>0 at the start of the night, 1 at dawn.</summary>
        public float DawnProgress => Tuning.SurviveSeconds <= 0f ? 1f : 1f - SurviveLeft / Tuning.SurviveSeconds;
        /// <summary>0..1 how close he has come while behind the player.</summary>
        public float BehindProgress => Tuning.BehindSeconds <= 0f ? 1f : Math.Min(1f, PhaseTimer / Tuning.BehindSeconds);

        public AdminHunt(HuntTuning tuning, int seed)
        {
            Tuning = tuning;
            _rng = new GameRng(seed);
            SurviveLeft = tuning.SurviveSeconds;
        }

        /// <summary>He starts walking.</summary>
        public HuntEvent Start()
        {
            Repels = 0;
            Catches = 0;
            SurviveLeft = Tuning.SurviveSeconds;
            return BeginStalking();
        }

        /// <summary>One frame. Returns what happened so the director can react.</summary>
        public HuntEvent Tick(float dt, HuntInput input)
        {
            if (Finished || dt <= 0f) return HuntEvent.None;
            PhaseTimer += dt;

            SurviveLeft -= dt;
            if (SurviveLeft <= 0f)
            {
                SurviveLeft = 0f;
                Phase = HuntPhase.Won;
                return HuntEvent.Won;
            }

            switch (Phase)
            {
                case HuntPhase.Stalking: return TickStalking(dt, input);
                case HuntPhase.AtDoor: return TickAtDoor(dt, input);
                case HuntPhase.Closet: return TickCloset(dt, input);
                case HuntPhase.Behind: return TickBehind(dt, input);
                default: return PhaseTimer >= Tuning.RetreatSeconds ? BeginStalking() : HuntEvent.None;
            }
        }

        // ------------------------------------------------------------ phases
        private HuntEvent TickStalking(float dt, HuntInput input)
        {
            _stalkLeft -= dt;
            return _stalkLeft > 0f ? HuntEvent.None : Stop(PickSpot(input));
        }

        private HuntEvent TickAtDoor(float dt, HuntInput input)
        {
            UpdateHold(dt, input.HoldingDoor, Tuning.DoorHoldSeconds);
            if (Hold >= 1f) return Repel();
            return PhaseTimer >= Tuning.DoorSeconds ? Catch() : HuntEvent.None;
        }

        private HuntEvent TickCloset(float dt, HuntInput input)
        {
            bool looking = input.Peeking && input.PlayerRoom == Room;
            var seen = HuntEvent.None;
            if (looking && !_spotted) { _spotted = true; seen = HuntEvent.Spotted; }
            UpdateHold(dt, looking && input.OnTarget, Tuning.ClosetHoldSeconds);
            if (Hold >= 1f) return seen | Repel();
            return PhaseTimer >= Tuning.ClosetSeconds ? seen | Catch() : seen;
        }

        private HuntEvent TickBehind(float dt, HuntInput input)
        {
            if (input.BeamOnFace) BeamHit += dt;
            if (BeamHit >= Tuning.BeamHitSeconds) return Repel();
            return PhaseTimer >= Tuning.BehindSeconds ? Catch() : HuntEvent.None;
        }

        private void UpdateHold(float dt, bool holding, float seconds)
        {
            if (holding) Hold += dt / seconds;
            else Hold = Math.Max(0f, Hold - dt * Tuning.HoldDecayPerSecond);
        }

        // ------------------------------------------------------------ transitions
        private HuntEvent BeginStalking()
        {
            Enter(HuntPhase.Stalking);
            Room = RoomId.Hallway;
            // he walks for less time as dawn gets closer
            float longest = Tuning.StalkMaxSeconds * (1f - 0.5f * DawnProgress);
            float top = Math.Max(Tuning.StalkMinSeconds, longest);
            _stalkLeft = Tuning.StalkMinSeconds + _rng.NextFloat() * (top - Tuning.StalkMinSeconds);
            return HuntEvent.StalkStart;
        }

        private HuntEvent Stop(Spot spot)
        {
            _lastSpot = (int)spot;
            switch (spot)
            {
                case Spot.Classroom1:
                case Spot.Classroom2:
                    Enter(HuntPhase.AtDoor);
                    Room = spot == Spot.Classroom1 ? RoomId.Classroom1 : RoomId.Classroom2;
                    return HuntEvent.ReachedDoor;
                case Spot.Closet:
                    Enter(HuntPhase.Closet);
                    Room = _rng.Chance(0.5f) ? RoomId.ComfortRoom : RoomId.ComfortRoom2;
                    _spotted = false;
                    return HuntEvent.EnteredCloset;
                default:
                    Enter(HuntPhase.Behind);
                    Room = RoomId.Hallway;
                    return HuntEvent.AppearedBehind;
            }
        }

        private HuntEvent Repel()
        {
            Repels++;
            Enter(HuntPhase.Retreat);
            return HuntEvent.Repelled;
        }

        private HuntEvent Catch()
        {
            Catches++;
            if (Catches >= Tuning.CatchesToLose)
            {
                Phase = HuntPhase.Lost;
                return HuntEvent.Caught | HuntEvent.Lost;
            }
            Enter(HuntPhase.Retreat);
            return HuntEvent.Caught;
        }

        private void Enter(HuntPhase phase)
        {
            Phase = phase;
            PhaseTimer = 0f;
            Hold = 0f;
            BeamHit = 0f;
        }

        // Half the stops are at the two doors, the rest split between the comfort room and right
        // behind you. Never the same place twice in a row, and never behind you during a peek.
        private Spot PickSpot(HuntInput input)
        {
            float roll = _rng.NextFloat();
            int pick = roll < 0.25f ? 0 : roll < 0.5f ? 1 : roll < 0.75f ? 2 : 3;
            if (pick == 3 && !input.InHallway) pick = _rng.Chance(0.5f) ? 0 : 1;
            if (pick == _lastSpot) pick = (pick + 1) % 3;
            return (Spot)pick;
        }
    }

    /// <summary>How his picture moves in the comfort room: the head circles an anchor point while
    /// the whole image jitters. Faster and wider each time he has been sent away.</summary>
    public static class AdminHuntMotion
    {
        public static void HeadOffset(float t, int stuns, int seed, out float x, out float y)
        {
            float phase = (seed % 628) / 100f;
            float radius = 1.0f + 0.15f * stuns;
            float speed = 2.2f + 0.6f * stuns;
            float drift = (float)Math.Sin(0.9f * t + phase) * 1.1f;            // slow wander left/right
            float shakeX = (float)(Math.Sin(37f * t + phase * 3f) * 0.07 + Math.Sin(53f * t) * 0.05);
            float shakeY = (float)(Math.Sin(41f * t + phase) * 0.07 + Math.Sin(61f * t) * 0.05);
            x = (float)Math.Cos(speed * t + phase) * radius + drift + shakeX;
            y = (float)Math.Sin(speed * t + phase) * radius * 0.6f + shakeY;
        }
    }
}
