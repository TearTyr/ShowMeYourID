using System;

namespace Hollowbrook.Logic
{
    // Night 5 after the administrator is refused and the player clicks Patrol (pure rules, no
    // UnityEngine).
    //
    // It plays like Nightmare Fredbear's night in Five Nights at Freddy's 4: he is the only
    // thing in the building, and you survive until dawn. Each time he stops he is somewhere new:
    //   - IN A ROOM (Room 101, Room 202, CR-1 or CR-2): peek in and keep the lamp on his head.
    //   - BEHIND YOU in the hallway: flash his face with the beam.
    // He goes for the player: he is likelier behind you in the hallway, and likelier in the room you
    // are peeking into. Only the light stops him. Too slow and he catches you. CatchesToLose catches and he walks
    // to the server door.
    //
    // The hard rule: nobody can ever get stuck. The dawn clock always runs and every phase has
    // a time limit, so the hunt ends in a win or a loss whatever the player does or does not do.
    // AdminHuntTests runs random and idle input to prove it.

    public enum HuntPhase
    {
        Stalking,   // walking the halls: footsteps, no danger yet
        InRoom,     // in a peek room: find him with the lamp
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
        public float RoomSeconds;        // time to find him in the room he stopped in
        public float RoomHoldSeconds;    // seconds of lamp on his head that send him away
        public float HoldDecayPerSecond; // how fast a hold drains when you let go
        public float HeadRadius;         // world units: how close the cursor must be to his head
        public float BehindSeconds;      // time to flash him when he is behind you
        public float BeamHitSeconds;     // seconds of beam on his face that count as a hit
        public float RetreatSeconds;     // harmless time after he leaves

        public static HuntTuning Default => new HuntTuning
        {
            CatchesToLose = 3, SurviveSeconds = 210f, StalkMinSeconds = 3f, StalkMaxSeconds = 6f,
            RoomSeconds = 9f, RoomHoldSeconds = 1.8f,
            HoldDecayPerSecond = GameRules.Current.HuntHoldDecayPerSecond, HeadRadius = 1.0f, BehindSeconds = 2.2f, BeamHitSeconds = 0.35f,
            RetreatSeconds = GameRules.Current.HuntRetreatSeconds,
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
    }

    [Flags]
    public enum HuntEvent
    {
        None = 0,
        StalkStart = 1,     // footsteps begin
        EnteredRoom = 4,    // he went into a peek room
        Spotted = 8,        // the player's lamp view found him in that room
        AppearedBehind = 16,// he is right behind the player
        Repelled = 32,      // the player sent him away
        Caught = 64,        // he reached the player
        Won = 128,
        Lost = 256,
    }

    public class AdminHunt
    {
        // where he can stop: these four rooms, or BehindYou
        private static readonly RoomId[] Rooms =
            { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 };
        private const int BehindYou = 4;

        private readonly GameRng _rng;
        private float _stalkLeft;
        private bool _spotted;
        private int _lastSpot = -1;
        // He means to come up behind a player who is peeking: he waits for them to step out, or
        // comes to their room if they stay in too long.
        private bool _ambushWaiting, _ambushAfterRepel;
        private float _ambushHallSeconds, _ambushPeekSeconds;

        public HuntTuning Tuning { get; }
        public HuntPhase Phase { get; private set; } = HuntPhase.Stalking;
        /// <summary>Where he stopped: a peek room, or the hallway when behind you.</summary>
        public RoomId Room { get; private set; } = RoomId.Hallway;
        public int Repels { get; private set; }
        public int Catches { get; private set; }
        public float SurviveLeft { get; private set; }
        /// <summary>0..1 progress of the lamp held on his head.</summary>
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

        /// <summary>He starts walking. A hunt that was left half way starts with the catches it had and
        /// the dawn clock where it stood, never with more time than a fresh one.</summary>
        public HuntEvent Start(int catches = 0, float surviveLeft = -1f)
        {
            Repels = 0;
            Catches = Math.Max(0, Math.Min(catches, Tuning.CatchesToLose - 1));
            SurviveLeft = surviveLeft > 0f ? Math.Min(surviveLeft, Tuning.SurviveSeconds) : Tuning.SurviveSeconds;
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
                case HuntPhase.InRoom: return TickInRoom(dt, input);
                case HuntPhase.Behind: return TickBehind(dt, input);
                default: return PhaseTimer >= Tuning.RetreatSeconds ? BeginStalking() : HuntEvent.None;
            }
        }

        // ------------------------------------------------------------ phases
        private HuntEvent TickStalking(float dt, HuntInput input)
        {
            _stalkLeft -= dt;
            if (_stalkLeft > 0f) return HuntEvent.None;
            if (_ambushWaiting) return TickAmbush(dt, input);
            int spot = PickSpot(input);
            if (spot == BehindYou && !input.InHallway) { _ambushWaiting = true; return HuntEvent.None; }
            return Stop(spot);
        }

        private HuntEvent TickAmbush(float dt, HuntInput input)
        {
            var rules = GameRules.Current;
            if (input.InHallway)
            {
                _ambushPeekSeconds = 0f;
                _ambushHallSeconds += dt;
                return _ambushHallSeconds >= rules.HuntAmbushDelaySeconds ? Stop(BehindYou) : HuntEvent.None;
            }
            _ambushHallSeconds = 0f;
            _ambushPeekSeconds += dt;
            if (_ambushPeekSeconds < rules.HuntAmbushPatienceSeconds) return HuntEvent.None;
            int here = input.Peeking ? System.Array.IndexOf(Rooms, input.PlayerRoom) : -1;
            if (here < 0) here = _rng.NextInt(0, Rooms.Length);   // not in the hallway and not peeking: he picks a room
            var ev = Stop(here);   // a player who will not come out gets him in the room
            PhaseTimer = Math.Max(0f, Tuning.RoomSeconds - rules.HuntFollowSeconds);
            return ev;
        }

        private HuntEvent TickInRoom(float dt, HuntInput input)
        {
            bool looking = input.Peeking && input.PlayerRoom == Room;
            var seen = HuntEvent.None;
            if (looking && !_spotted) { _spotted = true; seen = HuntEvent.Spotted; }
            UpdateHold(dt, looking && input.OnTarget, Tuning.RoomHoldSeconds);
            if (Hold >= 1f) return seen | Repel(true);
            if (PhaseTimer < Tuning.RoomSeconds) return seen;
            // He only catches a player who saw him and was too slow. A player looking elsewhere
            // is not caught by something never seen: he comes to where they are instead.
            return looking ? seen | Catch() : seen | FollowPlayer(input);
        }

        // He goes to the room being peeked, with a short time to find him, or comes up behind
        // a player in the hallway.
        private HuntEvent FollowPlayer(HuntInput input)
        {
            int here = input.Peeking ? System.Array.IndexOf(Rooms, input.PlayerRoom) : -1;
            if (here < 0) return Stop(BehindYou);
            var ev = Stop(here);
            PhaseTimer = Math.Max(0f, Tuning.RoomSeconds - GameRules.Current.HuntFollowSeconds);
            return ev;
        }

        private HuntEvent TickBehind(float dt, HuntInput input)
        {
            if (input.BeamOnFace) BeamHit += dt;
            if (BeamHit >= Tuning.BeamHitSeconds) return Repel(false);
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
            _ambushWaiting = false;
            _ambushHallSeconds = _ambushPeekSeconds = 0f;
            // he walks for less time as dawn gets closer
            float longest = Tuning.StalkMaxSeconds * (1f - 0.5f * DawnProgress);
            float top = Math.Max(Tuning.StalkMinSeconds, longest);
            _stalkLeft = Tuning.StalkMinSeconds + _rng.NextFloat() * (top - Tuning.StalkMinSeconds);
            if (_ambushAfterRepel) { _ambushWaiting = true; _stalkLeft = 0f; }
            _ambushAfterRepel = false;
            return HuntEvent.StalkStart;
        }

        private HuntEvent Stop(int spot)
        {
            _lastSpot = spot;
            if (spot == BehindYou)
            {
                Enter(HuntPhase.Behind);
                Room = RoomId.Hallway;
                return HuntEvent.AppearedBehind;
            }
            Enter(HuntPhase.InRoom);
            Room = Rooms[spot];
            _spotted = false;
            return HuntEvent.EnteredRoom;
        }

        // Lit in a peek, he does not give up: when the player steps out into the hallway he is there.
        private HuntEvent Repel(bool whilePeeking)
        {
            Repels++;
            _ambushAfterRepel = whilePeeking;
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

        // He goes for the player. In the hallway he comes up behind them more often than not; while the
        // player peeks into a room he is as likely to be in that very room as anywhere else. Otherwise
        // any of the four rooms. Never the same place twice in a row, and never behind you during a peek.
        private int PickSpot(HuntInput input)
        {
            int pick = ChooseSpot(input);
            if (pick != _lastSpot) return pick;
            return pick == BehindYou ? _rng.NextInt(0, Rooms.Length) : (pick + 1) % Rooms.Length;
        }

        private int ChooseSpot(HuntInput input)
        {
            if (input.InHallway && _rng.Chance(GameRules.Current.HuntBehindChance)) return BehindYou;
            if (input.Peeking && _rng.Chance(GameRules.Current.HuntSameRoomChance))
            {
                int here = System.Array.IndexOf(Rooms, input.PlayerRoom);
                if (here >= 0) return here;
            }
            return _rng.NextInt(0, Rooms.Length);
        }
    }

    /// <summary>How his picture moves in a room: the head circles an anchor point while
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
