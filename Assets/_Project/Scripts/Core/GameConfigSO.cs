using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ShowMeYourID/Game Config")]
    // Tuning knobs editable in one asset. Loaded by CoreBootstrap.
    public class GameConfigSO : ScriptableObject
    {
        [Header("Checkpoint pacing")]
        [Tooltip("Seconds the stamped paper stays before the next student steps up.")]
        public float verdictBannerSeconds = 1.2f;

        [Header("Checkpoint queue")]
        [Tooltip("Lower bound of the randomized visitor arrival interval while a shift runs (Normal mode, night 1).")][Range(0.5f, 60f)]
        public float queueArrivalMinSeconds = 25f;
        [Tooltip("Upper bound of the randomized visitor arrival interval (Normal mode, night 1).")][Range(0.5f, 120f)]
        public float queueArrivalMaxSeconds = 30f;
        [Tooltip("Endless/Challenge arrival interval bounds - those modes are score races and keep the fast clock.")][Range(0.5f, 60f)]
        public float queueRunMinSeconds = 3f;
        [Range(0.5f, 120f)]
        public float queueRunMaxSeconds = 5f;
        [Tooltip("Normal mode: the arrival clock runs this much faster while the window is busy, so dawdling grows the line.")][Range(1f, 4f)]
        public float queueBusyHurryMultiplier = 2f;
        [Tooltip("Normal mode: base arrival interval is multiplied by this per night after night 1.")][Range(0.3f, 1f)]
        public float queueNightScale = 0.8f;
        // Showcase build: the line pressure rules are switched off (set sane values such as
        // 5 / 7 to bring the feature back). The waiting line itself is hidden in the view.
        [Tooltip("Waiting visitors (window excluded) at which the queue-pressure warning fires once.")][Range(1, 9999)]
        public int queuePressureWarnAt = 9999;
        [Tooltip("Waiting visitors at which the line overruns the checkpoint - a one-shot run failure.")][Range(2, 9999)]
        public int queueOverwhelmedAt = 9999;
        [Tooltip("Chance that an arriving anomaly (not the administrator) makes one legitimate student vanish. Normal mode only.")][Range(0f, 1f)]
        public float missingStudentChance = 0.25f;
        [Tooltip("Missing-student incidents allowed per shift.")][Range(0, 5)]
        public int missingStudentLimit = 1;

        [Header("Sanity")]
        [Tooltip("Students who unsettle the custodian: while one of these is at the window, sanity drains an extra amount per second (progressively, for as long as they stand there). Match the name exactly as it reads on their ID. Empty = nobody is scary.")]
        public ScaryStudent[] scaryStudents = new ScaryStudent[0];

        [System.Serializable]
        public class ScaryStudent
        {
            [Tooltip("Full name as on the student's ID, e.g. Carlo Mendoza.")]
            public string fullName = "";
            [Tooltip("Extra sanity lost per second while they are at the window (before the drain stacks).")][Range(0f, 2f)]
            public float extraDrainPerSecond = 0.3f;
        }

        [Header("Exploration")]
        public float walkSpeed = 3.4f;
        public float runSpeed = 6.0f;
        public float interactRange = 1.35f;

        [Header("Flashlight battery")]
        [Tooltip("Percent per second while the wide Sweep beam is on.")]
        public float batteryDrainSweepPerSecond = 2.5f;
        [Tooltip("Percent per second while the tight Focus beam is on.")]
        public float batteryDrainFocusPerSecond = 6f;
        [Tooltip("Percent per second recovered in darkness with the beam off.")]
        public float batteryRecoverOffPerSecond = 4f;
        [Tooltip("Percent per second recovered with the beam off in a lit (safe) room.")]
        public float batteryRecoverLitPerSecond = 8f;
        [Tooltip("Charge needed before a dead beam can switch on again (no soft-lock).")][Range(0f, 1f)]
        public float batteryRebootFraction = 0.2f;

        [Header("Hallway darkness (limited visibility)")]
        [Tooltip("Ambient darkness of the MAIN HALLWAY while its lights are off. Rooms use 0.62, a lit room 0.12; 0.76 leaves the corridor 73% darker than a lit room. Applied to the room darkness layer's alpha.")][Range(0f, 1f)]
        public float hallwayDarkAlpha = HallwayDarkness.BaseAlpha;
        [Tooltip("Extra darkness the corridor's depth layer adds beyond the pool around the custodian, while the lights are off. 0 = the whole corridor is equally visible.")][Range(0f, 1f)]
        public float hallwayDepthAlpha = HallwayDarkness.DepthAlpha;
        // NOTE: the pool's radii are NOT here on purpose - they are baked into the painted
        // hall_depth sprite, so they live with the rest of that geometry in HallwayDarkness
        // (ClearRadius / DepthRadius). Only the two alphas are runtime-mutable.

        [Header("Flashlight beam geometry")]        [Tooltip("World units the wide Sweep beam reaches.")][Range(1f, 20f)]
        public float sweepRange = BeamGeometry.DefaultSweepRange;
        [Tooltip("Half-angle of the Sweep cone, degrees.")][Range(2f, 60f)]
        public float sweepHalfAngleDeg = BeamGeometry.DefaultSweepHalfAngleDeg;
        [Tooltip("World units the tight Focus beam reaches.")][Range(1f, 24f)]
        public float focusRange = BeamGeometry.DefaultFocusRange;
        [Tooltip("Half-angle of the Focus cone, degrees. Narrow = precise tracking tool.")][Range(1f, 30f)]
        public float focusHalfAngleDeg = BeamGeometry.DefaultFocusHalfAngleDeg;
        [Tooltip("Target body radius added to the cone test - grazing a silhouette still counts.")][Range(0f, 2f)]
        public float beamTargetRadius = BeamGeometry.DefaultTargetRadius;

        [Header("Patrol")]
        [Tooltip("Roaming anomalies on patrol. Off: the only anomalies are the ones at the security window.")]
        public bool patrolAnomalies = false;

        [Header("Administrator hunt (night 5, after the DENY stamp)")]
        [Tooltip("Times he can catch you before he reaches the server door and you lose.")][Range(1, 10)]
        public int huntCatchesToLose = 3;
        [Tooltip("Seconds to survive until dawn.")][Range(30f, 900f)]
        public float huntSurviveSeconds = 210f;
        [Tooltip("Seconds taken off the night for every lie exposed in his interrogation.")][Range(0f, 60f)]
        public float huntSecondsPerLie = 15f;
        [Tooltip("The night is never shorter than this, however many lies were exposed.")][Range(30f, 600f)]
        public float huntMinSurviveSeconds = 120f;
        [Tooltip("How long he walks between stops (min / max). The max halves by dawn.")][Range(1f, 30f)]
        public float huntStalkMinSeconds = 5f;
        [Range(1f, 40f)]
        public float huntStalkMaxSeconds = 10f;
        [Tooltip("Seconds to reach the door he stopped at and hold it before he gets through.")][Range(3f, 30f)]
        public float huntDoorSeconds = 10f;
        [Tooltip("Seconds of holding E at the door that send him away.")][Range(0.3f, 10f)]
        public float huntDoorHoldSeconds = 2f;
        [Tooltip("Seconds to find him in the comfort room.")][Range(3f, 30f)]
        public float huntClosetSeconds = 12f;
        [Tooltip("Seconds the lamp must stay on his head in the comfort room.")][Range(0.3f, 10f)]
        public float huntClosetHoldSeconds = 1.8f;
        [Tooltip("How close (world units) the cursor must be to his head.")][Range(0.3f, 3f)]
        public float huntHeadRadius = 1.0f;
        [Tooltip("Seconds to flash him when he appears behind you.")][Range(1f, 8f)]
        public float huntBehindSeconds = 2.6f;
        [Tooltip("Seconds of beam on his face that count as a hit.")][Range(0.1f, 2f)]
        public float huntBeamHitSeconds = 0.35f;
        [Tooltip("Sanity lost each time he catches you.")][Range(0f, 40f)]
        public float huntCaughtSanity = 15f;

        [Header("Encounter - repel meter")]
        [Tooltip("Meter per second while Focus holds the anomaly's body (the only way to progress).")][Range(0.05f, 2f)]
        public float focusProgressPerSecond = TrackingPolicy.FocusRatePerSecond;
        [Tooltip("Meter lost per second in darkness, once the grace window has run out.")][Range(0.05f, 2f)]
        public float trackingDecayPerSecond = TrackingPolicy.DecayPerSecond;
        [Tooltip("Seconds the meter holds after Focus is lost before it starts decaying (fairness window).")][Range(0f, 2f)]
        public float focusGraceSeconds = TrackingPolicy.GraceSeconds;
        [Tooltip("Meter needed before accurate Focus exposes the corrupted identity.")][Range(0.05f, 0.95f)]
        public float exposeMeterThreshold = 0.55f;
        [Tooltip("Seconds a repelled anomaly visibly retreats before it slips away.")][Range(0.2f, 5f)]
        public float repelBeatSeconds = 0.9f;
        [Tooltip("Off-stage seconds before a repelled anomaly may return elsewhere.")][Range(2f, 60f)]
        public float anomalyRecoverySeconds = 14f;

        [Header("Anomaly behavior")]
        [Tooltip("World distance at which an active anomaly starts its catch windup.")][Range(0.5f, 4f)]
        public float anomalyThreatRange = 1.6f;
        [Tooltip("Creep speed of the Blink anomaly (only moves when unobserved).")][Range(0.2f, 4f)]
        public float anomalySpeedBlink = 1.15f;
        [Tooltip("Lateral glide speed of the Slide anomaly.")][Range(0.2f, 4f)]
        public float anomalySpeedSlide = 0.9f;
        [Tooltip("Patrol speed of the Sprint anomaly between bursts.")][Range(0.2f, 4f)]
        public float anomalySpeedSprint = 1.3f;
        [Tooltip("Ceiling drift speed of the CoverHug anomaly.")][Range(0.1f, 3f)]
        public float anomalySpeedCoverHug = 0.55f;
        [Tooltip("Burst speed of the Sprint anomaly after its telegraph.")][Range(1f, 10f)]
        public float sprintDashSpeed = 4.5f;
        [Tooltip("Readable warning before a Sprint burst; any beam coverage interrupts it.")][Range(0.3f, 4f)]
        public float sprintTelegraphSeconds = 1.1f;
        [Tooltip("Sprint burst duration before it must recover.")][Range(0.2f, 5f)]
        public float sprintDashSeconds = 1.4f;
        [Tooltip("Sprint pause after a burst before it can wind up again.")][Range(0.2f, 5f)]
        public float sprintRecoverSeconds = 1.4f;
        [Tooltip("Distance inside which Sprint winds up.")][Range(2f, 16f)]
        public float sprintAggroRange = 9f;
        [Tooltip("How fast a repelled anomaly withdraws from the player.")][Range(1f, 8f)]
        public float anomalyRetreatSpeed = 3.2f;
        [Tooltip("Continuous threatening seconds before each behavior catches the player. Fair warning cue plays first.")][Range(0.5f, 6f)]
        public float catchGraceBlink = 2.2f;
        [Range(0.5f, 6f)] public float catchGraceSlide = 2.4f;
        [Range(0.5f, 6f)] public float catchGraceSprint = 1.8f;
        [Tooltip("CoverHug's own catch windup - this grace is CoverHug-specific by name and by use.")][Range(0.5f, 6f)]
        public float coverHugCatchSeconds = 2.6f;

        [Header("Horror pacing")]
        [Tooltip("Base chance per second that a wandering anomaly spawns in an entered room (shift-scaled).")]
        public float anomalySpawnChancePerSecond = 0.05f;

        [Header("Exhibition (5-10 minute booth cut)")]
        [Tooltip("One curated night (4 visitors, 1 anomaly) then the demo closes. Off = full five-shift story.")]
        // shipping default is the FIVE-NIGHT story; the booth demo opts in via
        // GameConfig.asset exhibitionMode: 1 or this flag in headless proofs
        public bool exhibitionMode = false;
        public float exhibitionBannerSeconds = 2.2f;

        [Header("Shift clock")]
        [Tooltip("In-story minutes per real second while a shift is running, in ANY scene.")]
        public float clockMinutesPerSecond = 0.25f;

        [Header("Presentation")]
        public int canvasReferenceWidth = 1280;
        public int canvasReferenceHeight = 720;
        public float dialogueCharsPerSecond = 42f;

        public static GameConfigSO CreateDefault()
        {
            var c = CreateInstance<GameConfigSO>();
            c.name = "GameConfig (procedural default)";
            return c;
        }

        public static GameConfigSO LoadOrDefault()
        {
            GameConfigSO loaded = null;
            try { loaded = Resources.Load<GameConfigSO>("GameConfig"); }
            catch (System.Exception e) { Debug.LogWarning("[GameConfig] Resources.Load failed: " + e.Message); }
            if (loaded == null)
            {
                loaded = CreateDefault();
                Debug.Log("[GameConfig] No asset found - using procedural defaults.");
            }
            return loaded;
        }
    }
}
