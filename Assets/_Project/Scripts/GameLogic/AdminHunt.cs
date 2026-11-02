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
    // He goes for the player, but never into the room the player is looking into: while you peek a room he
    // stops somewhere else, and he comes up behind you once you step out (a random 1 to 2 seconds later, so
    // you get a breath). When he is dealt with in the hallway he is off to a classroom within 2 seconds.
    // Only the light stops him. Too slow and he catches you. CatchesToLose catches and he walks to the server door.
    //
    // Doors are not safe. Look into his room and step out without sending him away, and that same door is a catch
    // the next time it opens (LeftHim). Leave him in his room until the room timer runs out with you in the hallway,
    // and he is Loose for a few seconds: ANY door you open is a catch.
    //
    // The hard rule: nobody can ever get stuck. The dawn clock always runs and every phase has
    // a time limit, so the hunt ends in a win or a loss whatever the player does or does not do.
    // AdminHuntTests runs random and idle input to prove it.

    public enum HuntPhase
    {
        Stalking,   // walking the halls: footsteps, no danger yet
        InRoom,     // in a peek room: find him with the lamp
        Behind,     // right behind the player in the hallway: flash him
        Loose,      // left in his room too long: any door the player opens is a catch, then he comes up behind them
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
        public float AmbushDelayMinSeconds;   // after you step out of a peek he is behind you this long after (shortest)
        public float AmbushDelayMaxSeconds;   // ...and longest, drawn at random so it cannot be timed
        public float SwapRetreatSeconds;      // harmless time after he is dealt with in the hallway
        public float SwapAfterRepelSeconds;   // retreat and walk together: the longest wait before he swaps to a classroom

        public static HuntTuning Default => new HuntTuning
        {
            CatchesToLose = 3, SurviveSeconds = 210f, StalkMinSeconds = 3f, StalkMaxSeconds = 6f,
            RoomSeconds = 9f, RoomHoldSeconds = 1.8f,
            HoldDecayPerSecond = GameRules.Current.HuntHoldDecayPerSecond, HeadRadius = 1.5f, BehindSeconds = 5f, BeamHitSeconds = 0.35f,
            RetreatSeconds = GameRules.Current.HuntRetreatSeconds,
            AmbushDelayMinSeconds = GameRules.Current.HuntAmbushDelayMinSeconds,
            AmbushDelayMaxSeconds = GameRules.Current.HuntAmbushDelayMaxSeconds,
            SwapRetreatSeconds = GameRules.Current.HuntSwapRetreatSeconds,
            SwapAfterRepelSeconds = GameRules.Current.HuntSwapAfterRepelSeconds,
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
        LeftHim = 512,      // the player stepped out of his room without sending him away: that door is a trap now
        Loose = 1024,       // his room timer ran out with the player in the hallway: every door is a trap for a while
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
        // He means to come up behind a player who is peeking: he waits for them to step out (then a random
        // short delay), and gives up and walks off if they stay in too long. He never goes into their room.
        private bool _ambushWaiting, _ambushAfterRepel;
        private float _ambushHallSeconds, _ambushWaited, _ambushDelay;
        private bool _wasPeeking, _leftPeek;   // the player stepped out of a peek this frame
        private bool _leftHim;                 // the player looked into his room and stepped out with him still there
        // Dealt with in the hallway: no extra wait, he swaps to a classroom within SwapAfterRepelSeconds.
        private bool _swapNext;
        private float _retreatSeconds;

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
        /// <summary>He is in a room the player looked into and left: opening that door again is a catch.</summary>
        public bool LeftHim => Phase == HuntPhase.InRoom && _leftHim;
        /// <summary>0 at the start of the night, 1 at dawn.</summary>
        public float DawnProgress => Tuning.SurviveSeconds <= 0f ? 1f : 1f - SurviveLeft / Tuning.SurviveSeconds;
        /// <summary>0..1 how close he has come while behind the player.</summary>
        public float BehindProgress => Tuning.BehindSeconds <= 0f ? 1f : Math.Min(1f, PhaseTimer / Tuning.BehindSeconds);

        public AdminHunt(HuntTuning tuning, int seed)
        {
            Tuning = tuning;
            _retreatSeconds = tuning.RetreatSeconds;
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
            _leftPeek = _wasPeeking && !input.Peeking && input.InHallway;
            _wasPeeking = input.Peeking;

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
                case HuntPhase.Loose: return TickLoose(input);
                default: return PhaseTimer >= _retreatSeconds ? BeginStalking() : HuntEvent.None;
            }
        }

        // ------------------------------------------------------------ phases
        private HuntEvent TickStalking(float dt, HuntInput input)
        {
            // stepping out of a peek can bring him up behind the player almost at once
            if (_leftPeek && !_ambushWaiting && _rng.Chance(GameRules.Current.HuntBehindChance)) ArmAmbush();
            if (_ambushWaiting) return TickAmbush(dt, input);
            _stalkLeft -= dt;
            if (_stalkLeft > 0f) return HuntEvent.None;
            return Stop(PickSpot(input));
        }

        // He waits for the player to be in the hallway, then shows up behind them after a random 1 to 2 seconds.
        // A player who stays in the peek is not followed in: after HuntAmbushPatienceSeconds of waiting in all he
        // gives up and walks off, so no phase can last for ever.
        private HuntEvent TickAmbush(float dt, HuntInput input)
        {
            _ambushWaited += dt;
            if (input.InHallway)
            {
                _ambushHallSeconds += dt;
                if (_ambushHallSeconds >= _ambushDelay) return Stop(BehindYou);
            }
            else _ambushHallSeconds = 0f;
            return _ambushWaited >= GameRules.Current.HuntAmbushPatienceSeconds ? BeginStalking() : HuntEvent.None;
        }

        private void ArmAmbush()
        {
            _ambushWaiting = true;
            _ambushHallSeconds = _ambushWaited = 0f;
            _ambushDelay = Tuning.AmbushDelayMinSeconds +
                           _rng.NextFloat() * Math.Max(0f, Tuning.AmbushDelayMaxSeconds - Tuning.AmbushDelayMinSeconds);
        }

        private HuntEvent TickInRoom(float dt, HuntInput input)
        {
            bool looking = input.Peeking && input.PlayerRoom == Room;
            // Looked, stepped out with him still there, opened the same door again: no second chance.
            if (looking && _leftHim) return Catch();
            var seen = HuntEvent.None;
            if (looking && !_spotted) { _spotted = true; seen = HuntEvent.Spotted; GiveFairChance(); }
            if (_spotted && !looking && !_leftHim) { _leftHim = true; seen |= HuntEvent.LeftHim; }
            UpdateHold(dt, looking && input.OnTarget, Tuning.RoomHoldSeconds);
            if (Hold >= 1f) return seen | Repel(true);
            if (PhaseTimer < Tuning.RoomSeconds) return seen;
            // He only catches a player who saw him and was too slow. A player looking elsewhere
            // is not caught by something never seen: he goes to wait behind them instead.
            return looking ? seen | Catch() : seen | FollowPlayer(input);
        }

        // The moment the player first looks into the room he is in, they get a full fair chance to find him:
        // the clock never leaves less than HuntFollowSeconds, so he can not catch them on the very first look.
        private void GiveFairChance()
        {
            float latest = Math.Max(0f, Tuning.RoomSeconds - GameRules.Current.HuntFollowSeconds);
            if (PhaseTimer > latest) PhaseTimer = latest;
        }

        // The player did not finish him: in the hallway he is Loose (every door is a catch for a while, then he is
        // behind them), or he waits for them to leave a peek. He never jumps into the room being peeked.
        private HuntEvent FollowPlayer(HuntInput input)
        {
            if (input.InHallway) return BeginLoose();
            var ev = BeginStalking();
            ArmAmbush();
            return ev;
        }

        private HuntEvent TickBehind(float dt, HuntInput input)
        {
            if (input.BeamOnFace) BeamHit += dt;
            if (BeamHit >= Tuning.BeamHitSeconds) return Repel(false);
            return PhaseTimer >= Tuning.BehindSeconds ? Catch() : HuntEvent.None;
        }

        private HuntEvent BeginLoose()
        {
            Enter(HuntPhase.Loose);
            Room = RoomId.Hallway;
            return HuntEvent.Loose;
        }

        // Any peek is a catch. Staying out of the doors for HuntLooseSeconds is the safe way; then he comes up behind.
        private HuntEvent TickLoose(HuntInput input)
        {
            if (input.Peeking) return Catch();
            if (PhaseTimer < GameRules.Current.HuntLooseSeconds) return HuntEvent.None;
            return input.InHallway ? Stop(BehindYou) : BeginStalking();
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
            _ambushHallSeconds = _ambushWaited = 0f;
            // he walks for less time as dawn gets closer
            float longest = Tuning.StalkMaxSeconds * (1f - 0.5f * DawnProgress);
            float top = Math.Max(Tuning.StalkMinSeconds, longest);
            _stalkLeft = Tuning.StalkMinSeconds + _rng.NextFloat() * (top - Tuning.StalkMinSeconds);
            if (_swapNext) _stalkLeft = SwapWalkSeconds();
            if (_ambushAfterRepel) { ArmAmbush(); _stalkLeft = 0f; }
            _ambushAfterRepel = false;
            return HuntEvent.StalkStart;
        }

        // After he is dealt with in the hallway the retreat and this walk together are at most SwapAfterRepelSeconds.
        private float SwapWalkSeconds()
        {
            float room = Math.Max(0.1f, Tuning.SwapAfterRepelSeconds - _retreatSeconds);
            return room * (0.3f + 0.7f * _rng.NextFloat());
        }

        private HuntEvent Stop(int spot)
        {
            _lastSpot = spot;
            _swapNext = false;
            if (spot == BehindYou)
            {
                Enter(HuntPhase.Behind);
                Room = RoomId.Hallway;
                return HuntEvent.AppearedBehind;
            }
            Enter(HuntPhase.InRoom);
            Room = Rooms[spot];
            _spotted = false;
            _leftHim = false;
            return HuntEvent.EnteredRoom;
        }

        // Lit in a peek, he does not give up: when the player steps out into the hallway he is there.
        private HuntEvent Repel(bool whilePeeking)
        {
            Repels++;
            _ambushAfterRepel = whilePeeking;
            _swapNext = !whilePeeking;   // dealt with in the hallway: no extra wait, a quick swap to a classroom
            _retreatSeconds = whilePeeking ? Tuning.RetreatSeconds : Tuning.SwapRetreatSeconds;
            Enter(HuntPhase.Retreat);
            return HuntEvent.Repelled;
        }

        private HuntEvent Catch()
        {
            Catches++;
            _swapNext = false;
            _retreatSeconds = Tuning.RetreatSeconds;
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

        // He goes for the player. In the hallway he comes up behind them more often than not, unless he was just dealt
        // with there (then a classroom). While the player peeks a room he is never in that room, and never behind
        // them. Never the same place twice in a row.
        private int PickSpot(HuntInput input)
        {
            int excluded = input.Peeking ? System.Array.IndexOf(Rooms, input.PlayerRoom) : -1;
            bool behind = !_swapNext && input.InHallway && _rng.Chance(GameRules.Current.HuntBehindChance);
            if (behind && _lastSpot != BehindYou) return BehindYou;
            return PickRoom(excluded);
        }

        private int PickRoom(int excluded)
        {
            var open = new System.Collections.Generic.List<int>();
            for (int i = 0; i < Rooms.Length; i++)
                if (i != excluded && i != _lastSpot) open.Add(i);
            if (open.Count == 0)
                for (int i = 0; i < Rooms.Length; i++)
                    if (i != excluded) open.Add(i);
            return open[_rng.NextInt(0, open.Count)];
        }
    }

    /// <summary>How his picture moves in a room. He wanders like the Rat in Five Nights at Candy's 3: he drifts
    /// to a spot, stands there a moment, then drifts to another one somewhere else, slowly and never in a circle.
    /// The spots come from the hunt's seed, so a run always wanders the same way. Each time he has been sent away
    /// he drifts a little faster, and the whole picture is only ever a few units from its anchor.</summary>
    public static class AdminHuntMotion
    {
        public const float MaxX = 2.6f;           // how far from the anchor a spot can be, sideways
        public const float MaxY = 0.7f;           // ...and up or down
        public const float MaxStep = 2.8f;        // the longest drift from one spot to the next
        private const int SpotCount = 16;         // the spots repeat after this many, nobody can tell
        private const float DriftShare = 0.7f;    // the rest of each stop is spent standing still

        /// <summary>Seconds one drift plus one pause takes: shorter after every visit, never frantic.</summary>
        public static float StopSeconds(int stuns) => Math.Max(2.6f, 3.8f - 0.3f * stuns);

        public static void HeadOffset(float t, int stuns, int seed, out float x, out float y)
        {
            var spots = SpotsFor(seed);
            float stop = StopSeconds(stuns);
            float clock = Math.Max(0f, t) / stop;
            int index = (int)clock;
            float within = clock - index;
            var from = spots[index % SpotCount];
            var to = spots[(index + 1) % SpotCount];
            float walk = within < DriftShare ? within / DriftShare : 1f;
            float ease = walk * walk * (3f - 2f * walk);   // starts and ends gently
            float shake = within < DriftShare ? 0.02f : 0.045f;   // a faint tremble, more when he stands still
            x = from[0] + (to[0] - from[0]) * ease + (float)Math.Sin(23f * t) * shake;
            y = from[1] + (to[1] - from[1]) * ease + (float)Math.Cos(29f * t) * shake;
        }

        // The spots he visits, one after another: each is a short drift from the one before.
        private static float[][] SpotsFor(int seed)
        {
            var rng = new GameRng(seed ^ 0x7A11);
            var spots = new float[SpotCount][];
            float x = 0f, y = 0f;
            for (int i = 0; i < SpotCount; i++)
            {
                x = Clamp(x + (rng.NextFloat() * 2f - 1f) * MaxStep, -MaxX, MaxX);
                y = Clamp(y + (rng.NextFloat() * 2f - 1f) * MaxY * 1.5f, -MaxY, MaxY);
                spots[i] = new[] { x, y };
            }
            return spots;
        }

        private static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
