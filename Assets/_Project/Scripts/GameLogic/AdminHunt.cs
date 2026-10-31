using System;

namespace Hollowbrook.Logic
{
    // The rejected administrator's hunt (pure rules, no UnityEngine).
    //
    // After the DENY stamp he forces his way in and hides in the classrooms. You find him by
    // peeking into a room and holding the lamp on his head while his picture shakes and circles.
    // A full hold is a STUN: the lamp flickers out and he vanishes, then either hides in the
    // other classroom or charges down the hallway (flash him with your beam to stun him again).
    // Stun him StunsToWin times and he is driven off. If the SERVER DOOR meter runs out first,
    // he reaches the server room and you lose.
    //
    // The hard rule: nobody can ever get stuck. Every phase has a time limit, a held lamp only
    // pauses the server meter while he is actually being tracked (and tracking itself times out),
    // so the hunt ends in a win or a loss no matter what the player does or does not do.
    // AdminHuntTests runs random and idle input to prove it.

    public enum HuntPhase
    {
        Hiding,     // somewhere in a classroom, not seen yet (waiting for the player to peek in)
        Tracking,   // the player peeks into his room: hold the lamp on his head
        Flicker,    // the lamp died, he is gone for a moment
        Charging,   // he runs down the hallway at the player: flash his face
        Recoil,     // driven back, harmless for a moment
        Won,        // stunned enough times: driven off
        Lost,       // the server door meter ran out
    }

    public enum HuntOutcome { OtherRoom, HallwayCharge }

    /// <summary>Tunable numbers. Defaults are the shipped feel; GameConfigSO overrides them.</summary>
    public struct HuntTuning
    {
        public int StunsToWin;           // stuns needed to drive him off
        public float ServerSeconds;      // the server door meter: time until he reaches the server room
        public float HideMinSeconds;     // shortest wait before he moves on his own
        public float HideMaxSeconds;     // longest wait before he moves on his own
        public float TrackMaxSeconds;    // longest a single tracking phase can last
        public float HoldSeconds;        // seconds of lamp on his head for one stun
        public float HoldDecayPerSecond; // how fast the hold drains when the lamp slips off him
        public float HeadRadius;         // world units: how close the cursor must be to his head
        public float FlickerSeconds;     // lamp dead time after a stun
        public float ChargeSeconds;      // seconds for him to run down the hallway
        public float ChargeHitSeconds;   // seconds of beam on his face that count as a hit
        public float ChargeWaitSeconds;  // longest we wait for the player to be in the hallway
        public float RecoilSeconds;      // harmless time after a charge

        public static HuntTuning Default => new HuntTuning
        {
            StunsToWin = 4, ServerSeconds = 180f, HideMinSeconds = 6f, HideMaxSeconds = 14f,
            TrackMaxSeconds = 12f, HoldSeconds = 3.2f, HoldDecayPerSecond = 0.5f, HeadRadius = 1.0f,
            FlickerSeconds = 0.9f, ChargeSeconds = 2.6f, ChargeHitSeconds = 0.35f,
            ChargeWaitSeconds = 3f, RecoilSeconds = 1.2f,
        };
    }

    /// <summary>What the player is doing this frame (filled in by the director from the world).</summary>
    public struct HuntInput
    {
        public bool Peeking;        // the classroom peek view is open
        public RoomId PlayerRoom;   // the room on screen (the peeked classroom, or the hallway)
        public bool OnTarget;       // peeking, lamp lit, cursor on his head
        public bool InHallway;      // walking the hallway (not peeking, not mid-transition)
        public bool BeamOnFace;     // the hallway beam covers his face
    }

    [Flags]
    public enum HuntEvent
    {
        None = 0,
        Hid = 1,            // he is hiding in (a new) room: drop the door-rattle hint
        TrackStart = 2,     // the player found him
        Stun = 4,           // a stun landed (lamp hold or hallway flash)
        BrokeFree = 8,      // tracking timed out without a stun
        FlickerStart = 16,  // the lamp dies now
        ChargeStart = 32,   // he starts running down the hallway
        Caught = 64,        // he reached the player
        Won = 128,
        Lost = 256,
    }

    public class AdminHunt
    {
        private readonly GameRng _rng;
        private HuntOutcome _lastOutcome = HuntOutcome.OtherRoom;
        private int _sameOutcomeRun;
        private float _hideLeft;
        private float _chargeLeft;
        private float _chargeWait;

        public HuntTuning Tuning { get; }
        public HuntPhase Phase { get; private set; } = HuntPhase.Hiding;
        /// <summary>Where he is: a classroom while hiding/tracking, the hallway while charging.</summary>
        public RoomId Room { get; private set; } = RoomId.Classroom1;
        public int Stuns { get; private set; }
        public float ServerLeft { get; private set; }
        /// <summary>0..1 progress of the current lamp hold (1 = stun).</summary>
        public float Hold { get; private set; }
        public float PhaseTimer { get; private set; }
        public float ChargeHit { get; private set; }
        public HuntOutcome PendingOutcome { get; private set; }
        public bool Finished => Phase == HuntPhase.Won || Phase == HuntPhase.Lost;
        public float ServerFraction => Tuning.ServerSeconds <= 0f ? 0f : ServerLeft / Tuning.ServerSeconds;
        /// <summary>0..1 how far down the hallway he has run.</summary>
        public float ChargeProgress => Tuning.ChargeSeconds <= 0f ? 1f : 1f - _chargeLeft / Tuning.ChargeSeconds;

        public AdminHunt(HuntTuning tuning, int seed)
        {
            Tuning = tuning;
            _rng = new GameRng(seed);
            ServerLeft = tuning.ServerSeconds;
        }

        /// <summary>Start hiding in a random classroom, with the shortest wait.</summary>
        public HuntEvent Start()
        {
            Stuns = 0;
            ServerLeft = Tuning.ServerSeconds;
            return BeginHiding(RandomClassroom(), Tuning.HideMinSeconds);
        }

        /// <summary>One frame. Returns what happened so the director can react.</summary>
        public HuntEvent Tick(float dt, HuntInput input)
        {
            if (Finished || dt <= 0f) return HuntEvent.None;
            PhaseTimer += dt;

            // the server door clock runs all the time, except while he is held under the lamp
            bool held = Phase == HuntPhase.Tracking && input.OnTarget && input.Peeking;
            if (!held) ServerLeft -= dt;
            if (ServerLeft <= 0f)
            {
                ServerLeft = 0f;
                Phase = HuntPhase.Lost;
                return HuntEvent.Lost;
            }

            switch (Phase)
            {
                case HuntPhase.Hiding: return TickHiding(dt, input);
                case HuntPhase.Tracking: return TickTracking(dt, input);
                case HuntPhase.Flicker: return TickFlicker();
                case HuntPhase.Charging: return TickCharging(dt, input);
                default: return TickRecoil();
            }
        }

        // ------------------------------------------------------------ phases
        private HuntEvent TickHiding(float dt, HuntInput input)
        {
            _hideLeft -= dt;
            if (input.Peeking && input.PlayerRoom == Room)
            {
                Phase = HuntPhase.Tracking;
                PhaseTimer = 0f;
                Hold = 0f;
                return HuntEvent.TrackStart;
            }
            if (_hideLeft <= 0f) return BeginHiding(OtherClassroom(Room), RandomHideSeconds());
            return HuntEvent.None;
        }

        private HuntEvent TickTracking(float dt, HuntInput input)
        {
            if (!input.Peeking || input.PlayerRoom != Room)
            {
                // the player stepped back: he stays where he is and goes quiet again
                Hold = 0f;
                Phase = HuntPhase.Hiding;
                PhaseTimer = 0f;
                _hideLeft = Tuning.HideMinSeconds;
                return HuntEvent.None;
            }
            if (input.OnTarget) Hold += dt / Tuning.HoldSeconds;
            else Hold = Math.Max(0f, Hold - dt * Tuning.HoldDecayPerSecond);

            if (Hold >= 1f) return LandStun(HuntEvent.None);
            if (PhaseTimer >= Tuning.TrackMaxSeconds)
            {
                // he slips away without a stun, so nobody can be pinned in front of him forever
                StartFlicker();
                return HuntEvent.BrokeFree | HuntEvent.FlickerStart;
            }
            return HuntEvent.None;
        }

        private HuntEvent TickFlicker()
        {
            if (PhaseTimer < Tuning.FlickerSeconds) return HuntEvent.None;
            if (PendingOutcome == HuntOutcome.OtherRoom)
                return BeginHiding(OtherClassroom(Room), RandomHideSeconds());
            Phase = HuntPhase.Charging;
            PhaseTimer = 0f;
            Room = RoomId.Hallway;
            _chargeLeft = Tuning.ChargeSeconds;
            _chargeWait = 0f;
            ChargeHit = 0f;
            return HuntEvent.ChargeStart;
        }

        private HuntEvent TickCharging(float dt, HuntInput input)
        {
            if (!input.InHallway)
            {
                // the charge waits for the player to be in the hallway, but not forever
                _chargeWait += dt;
                if (_chargeWait >= Tuning.ChargeWaitSeconds)
                    return BeginHiding(RandomClassroom(), RandomHideSeconds());
                return HuntEvent.None;
            }
            _chargeLeft -= dt;
            if (input.BeamOnFace) ChargeHit += dt;
            if (ChargeHit >= Tuning.ChargeHitSeconds) return LandStun(HuntEvent.None);
            if (_chargeLeft > 0f) return HuntEvent.None;
            StartRecoil();
            return HuntEvent.Caught;
        }

        private HuntEvent TickRecoil()
        {
            if (PhaseTimer < Tuning.RecoilSeconds) return HuntEvent.None;
            return BeginHiding(RandomClassroom(), RandomHideSeconds());
        }

        // ------------------------------------------------------------ transitions
        private HuntEvent LandStun(HuntEvent extra)
        {
            Stuns++;
            Hold = 0f;
            if (Stuns >= Tuning.StunsToWin)
            {
                Phase = HuntPhase.Won;
                return extra | HuntEvent.Stun | HuntEvent.Won;
            }
            if (Phase == HuntPhase.Charging) { StartRecoil(); return extra | HuntEvent.Stun; }
            StartFlicker();
            return extra | HuntEvent.Stun | HuntEvent.FlickerStart;
        }

        private void StartFlicker()
        {
            Phase = HuntPhase.Flicker;
            PhaseTimer = 0f;
            PendingOutcome = PickOutcome();
        }

        private void StartRecoil()
        {
            Phase = HuntPhase.Recoil;
            PhaseTimer = 0f;
        }

        private HuntEvent BeginHiding(RoomId room, float hideSeconds)
        {
            Phase = HuntPhase.Hiding;
            PhaseTimer = 0f;
            Hold = 0f;
            Room = room;
            _hideLeft = hideSeconds;
            return HuntEvent.Hid;
        }

        // Random, but never the same outcome three times in a row.
        private HuntOutcome PickOutcome()
        {
            var pick = _rng.Chance(0.45f) ? HuntOutcome.HallwayCharge : HuntOutcome.OtherRoom;
            if (pick == _lastOutcome && _sameOutcomeRun >= 2)
                pick = pick == HuntOutcome.OtherRoom ? HuntOutcome.HallwayCharge : HuntOutcome.OtherRoom;
            _sameOutcomeRun = pick == _lastOutcome ? _sameOutcomeRun + 1 : 1;
            _lastOutcome = pick;
            return pick;
        }

        private float RandomHideSeconds() => Tuning.HideMinSeconds +
            _rng.NextFloat() * (Tuning.HideMaxSeconds - Tuning.HideMinSeconds);

        private RoomId RandomClassroom() => _rng.Chance(0.5f) ? RoomId.Classroom1 : RoomId.Classroom2;

        private static RoomId OtherClassroom(RoomId room) =>
            room == RoomId.Classroom1 ? RoomId.Classroom2 : RoomId.Classroom1;
    }

    /// <summary>How his picture moves while you track him: the head circles an anchor point
    /// while the whole image jitters. Faster and wider after every stun.</summary>
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
