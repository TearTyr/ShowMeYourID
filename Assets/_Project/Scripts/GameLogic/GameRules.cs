using System;

namespace Hollowbrook.Logic
{
    /// <summary>Every rule of the game that a designer may want to change, in one place. The rules
    /// code reads GameRules.Current and never holds a number of its own. The Game Config asset owns
    /// the live copy (GameConfigSO.rules), so a value is changed in the Inspector, with no code
    /// touched. The values here are only the defaults. Nights and students are tables, see
    /// GameRules.Tables.cs: adding a night or a student is adding a row.
    /// Numbers that describe the art or the engine (picture sizes, sorting layers, desk layout)
    /// are not rules, so they stay where the art is drawn.</summary>
    [Serializable]
    public partial class GameRules
    {
        /// <summary>The rules in use. GameRoot points this at the Game Config asset at boot.</summary>
        public static GameRules Current = new GameRules();

        // ---- a run ----
        public bool RandomSeed = true;          // every new game is different; off = always FixedSeed
        public int FixedSeed = 20261102;        // the run testers get when RandomSeed is off

        /// <summary>The seed for a new game. It is saved, so Continue rebuilds the same nights.</summary>
        public int NewRunSeed() => RandomSeed ? new Random().Next() : FixedSeed;

        // ---- the desk ----
        public int LoseThreshold = 3;           // breaches or false rejections that lose the run
        public int QuestionBudget = 2;          // questions per student
        public float AwkwardSpeechChance = 0.5f;    // an anomaly speaks awkwardly (see StudentRow.Awkward)
        public float NearExpiryChance = 0.25f;  // a real student's ID runs out soon (it is still valid)

        // ---- sanity ----
        public float StableFrom = 70f;          // sanity stages: at or above this number
        public float UneasyFrom = 40f;
        public float ShakenFrom = 15f;
        public float HallucinationUneasy = 0.010f;      // chance per second at each stage
        public float HallucinationShaken = 0.035f;
        public float HallucinationBreaking = 0.080f;
        public float SecurityRoomRecoveryPerSecond = 0.5f;  // resting at the desk with the window empty (nights 1 to 4 only)
        public float DeskDrainPerSecond = 0.25f;        // a student at the window
        public float DarknessDrainPerSecond = 0.3f;     // walking the dark hallway, or peeking into a room
        public float RefillOnCorrect = 8f;              // a correct decision gives this much...
        public float RefillPerSecond = 1.5f;            // ...at this speed
        public float FinalNightDrainPerSecond = 0.3f;   // night 5 and 6: sanity always drains, wherever you are
        public float ChallengeDrainScale = 1.5f;        // ...and faster on night 6

        // ---- what a failed night costs the next one ----
        public int MaxSeverity = 5;
        public int BlackoutSeverity = 2;                // a blackout counts as this many anomalies let in
        public float SanityLossPerSeverity = 8f;        // at the first night
        public float FailedNightScale = 0.15f;          // bigger by this much per night after the first
        public float MaxStartSanityLoss = 45f;          // a night never starts lower than this

        // ---- patrol ----
        public float StandingChance = 0.75f;            // a peek room holds a standing anomaly tonight
        public float AnomalyLifetimeSeconds = 18f;      // a roaming anomaly gives up after this
        public float PhantomLifetimeSeconds = 6.5f;     // a hallucination fades after this
        public float NearSanityPerSecond = 1.5f;        // lost while one is close
        public float NearRange = 3.2f;                  // world units
        public float SightedSanity = 6f;                // lost once, the first time one comes this close:
        public float SightedRange = 5f;                 // world units
        public float CaughtSanity = 15f;                // lost when a roaming anomaly catches the player
        public float SpawnRollSeconds = 4f;             // how often the spawner rolls for another one
        public float SpawnNightBase = 0.6f;             // spawn chance scale: this...
        public float SpawnPerNight = 0.2f;              // ...plus this per night...
        public float SpawnPerInside = 0.35f;            // ...more per anomaly inside the school...
        public float SpawnPerBreach = 0.3f;             // ...more per breach this run...
        public float SpawnDarkScale = 1.6f;             // ...and this many times more in the dark
        public int PatrolMinAnomalies = 2;              // every patrol meets at least this many, dice or no dice
        public float PatrolNextAnomalySeconds = 3f;     // the next guaranteed one comes this long after the last is gone
        public float SpawnMinDistance = 4f;             // never appears closer to the player than this
        public float ReturnRetrySeconds = 4f;           // a returning anomaly waits this long for space

        // ---- leaving a peek ----
        public float PeekExitGraceSeconds = 3f;         // nothing can catch you just after stepping out
        public float PeekExitPushDistance = 4f;         // anything closer to the door is pushed back to this

        // ---- the administrator hunt (the rest of its numbers are in the Game Config groups above) ----
        public float HuntBehindChance = 0.6f;           // in the hallway: he comes up behind the player
        public float HuntSameRoomChance = 0.5f;         // peeking: he is in the room being looked into
        public float HuntRetreatSeconds = 1.5f;         // harmless after he leaves
        public float HuntHoldDecayPerSecond = 0.5f;     // the lamp hold drains this fast when you let go
        public float HuntFollowSeconds = 5f;            // he followed you into this room: time to find him
        public float HuntAmbushDelaySeconds = 1.2f;     // after you light him in a peek, he is behind you this long after you step out
        public float HuntAmbushPatienceSeconds = 5f;    // ...and if you stay in the peek, he comes into your room after this long
        // ---- marathon: nights follow each other without the menu ----
        public float NightDimSeconds = 1.4f;            // the screen dims this long between one night and the next
        // ---- the peek exit ----
        public float PeekBandAlpha = 0.45f;             // how see-through the step-back button is (0 = invisible, 1 = solid)

        /// <summary>Rows of the Night table, as used by ShiftPlan.</summary>
        public ShiftDef[] Nights = DefaultNights();
        /// <summary>The Challenge night that opens after the story.</summary>
        public ShiftDef ChallengeNight = DefaultChallengeNight();
        /// <summary>The Lockdown mode's one night: every student and six anomalies at the gate at once.</summary>
        public ShiftDef LockdownNight = DefaultLockdownNight();
        /// <summary>One row per student, in face-art order: row N wears char_student_NN.</summary>
        public StudentRow[] Students = DefaultStudents();
    }
}
