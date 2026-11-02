namespace Hollowbrook.Logic
{
    /// <summary>Per-behavior movement/threat numbers. Timing differences between the four
    /// anomalies are explicit here - no variable named after one behavior is shared by all.</summary>
    public struct BehaviorTuning
    {
        public float BlinkSpeed;          // world units/second while creeping
        public float SlideSpeed;          // lateral glide speed
        public float SprintSpeed;         // the crawler's one speed: always fast, halved while a beam is on it
        public float SprintAggroRange;    // world distance inside which it runs straight at the player
        public float CoverHugSpeed;       // ceiling drift speed
        public float ThreatRange;         // distance at which the catch windup runs
        public float CatchGraceBlink;     // continuous threatening seconds before a catch
        public float CatchGraceSlide;
        public float CatchGraceSprint;
        public float CatchGraceCoverHug;
        public float RetreatSpeed;        // how fast a repelled anomaly runs backwards, away from the player
        public float BeamPace;            // 1 = normal, 0.5 = half speed while any flashlight beam is on it

        public static BehaviorTuning Default => new BehaviorTuning
        {
            BlinkSpeed = 1.15f,
            SlideSpeed = 0.9f,
            SprintSpeed = 4.5f,
            SprintAggroRange = 9f,
            CoverHugSpeed = 0.55f,
            ThreatRange = 1.6f,
            CatchGraceBlink = 2.2f,
            CatchGraceSlide = 2.4f,
            CatchGraceSprint = 1.8f,
            CatchGraceCoverHug = 2.6f,
            RetreatSpeed = 9f,   // fast, so the player SEES it run backwards off screen instead of vanishing
            BeamPace = 0.5f,
        };
    }

    /// <summary>The resolved rules for one anomaly kind. Y values are offsets above the room
    /// floor line (world y = floorY + offset), so spawn and clamp bands can never contradict.</summary>
    public struct BehaviorProfile
    {
        /// <summary>The picture key of an anomaly kind. Blink and Slide share the crouched body.</summary>
        public static string ArtKey(AnomalyBehaviorKind kind)
        {
            switch (kind)
            {
                case AnomalyBehaviorKind.Sprint: return "char_anomaly_sprint_a";
                case AnomalyBehaviorKind.CoverHug: return "char_anomaly_coverhug";
                default: return "char_anomaly";
            }
        }

        /// <summary>Where the hallway doors are along its length (FloorPlan HallX). The ceiling hanger drifts above them.</summary>
        public static readonly float[] DoorXs = { -13.8f, -8.8f, 0.4f, 8.6f, 11.0f, 13.3f };

        public static float NearestDoorX(float x)
        {
            float best = DoorXs[0];
            foreach (float d in DoorXs)
                if (System.Math.Abs(d - x) < System.Math.Abs(best - x)) best = d;
            return best;
        }

        // How tall each drawing is on screen, in world units (the picture sizes are set in ProceduralArt FilePpus).
        public const float CrouchedHeight = 3.0f;   // Blink and Slide: the crouched thing, a bit more than the guard
        public const float RunnerHeight = 2.0f;     // Sprint: the four legged crawler (it is about 3.6 wide)
        public const float HangerHeight = 1.9f;     // CoverHug: it hangs from the ceiling (about 4.6 wide)

        /// <summary>The top of the screen, as a height above the floor line: the camera shows 5 units above its
        /// middle and the floor line sits 3.1 below the middle. The hallway ceiling is up there.</summary>
        public const float TopOfScreenY = 8.1f;

        /// <summary>Height of a crouched anomaly's centre above the floor line: half its height puts its feet on the floor.</summary>
        public const float GroundCentreY = CrouchedHeight * 0.5f;
        public const float RunnerFloorY = RunnerHeight * 0.5f;                  // the runner on the floor
        public const float RunnerCeilingY = TopOfScreenY - RunnerHeight * 0.5f; // the runner upside down on the ceiling
        public const float HangerCentreY = TopOfScreenY - HangerHeight * 0.5f;  // hangs from the top edge of the screen

        public float MoveSpeed;
        public float SpawnY;              // floor-relative spawn height
        public float MinY, MaxY;          // floor-relative vertical band it may never leave
        public float ThreatRange;
        public float CatchGraceSeconds;
        public float AggroRange;
        public bool FreezesWhenObserved;  // Blink: the Focus beam or room light holds it still
        public bool CeilingLurker;        // CoverHug: only the Focus beam finds it clearly
        public float BeamPace;            // speed multiplier while any beam is on it: the player's time to reach for Focus
        public float RetreatSpeed;

        public static BehaviorProfile For(AnomalyBehaviorKind kind, BehaviorTuning t)
        {
            var p = new BehaviorProfile
            {
                ThreatRange = t.ThreatRange,
                RetreatSpeed = t.RetreatSpeed,
                BeamPace = t.BeamPace > 0f ? t.BeamPace : 1f,   // an unset tuning must never freeze an anomaly
                AggroRange = t.SprintAggroRange,
            };
            switch (kind)
            {
                case AnomalyBehaviorKind.Blink:
                    p.MoveSpeed = t.BlinkSpeed;
                    p.SpawnY = GroundCentreY; p.MinY = GroundCentreY; p.MaxY = GroundCentreY;
                    p.CatchGraceSeconds = t.CatchGraceBlink;
                    p.FreezesWhenObserved = true;
                    break;
                case AnomalyBehaviorKind.Slide:
                    p.MoveSpeed = t.SlideSpeed;
                    p.SpawnY = GroundCentreY; p.MinY = GroundCentreY; p.MaxY = GroundCentreY;
                    p.CatchGraceSeconds = t.CatchGraceSlide;
                    break;
                case AnomalyBehaviorKind.Sprint:
                    p.MoveSpeed = t.SprintSpeed;
                    p.SpawnY = RunnerFloorY; p.MinY = RunnerFloorY; p.MaxY = RunnerCeilingY;   // the floor or the ceiling
                    p.CatchGraceSeconds = t.CatchGraceSprint;
                    break;
                default: // CoverHug - ceiling lurker; spawn inside its own band, never clamped out
                    p.MoveSpeed = t.CoverHugSpeed;
                    p.SpawnY = HangerCentreY; p.MinY = HangerCentreY; p.MaxY = HangerCentreY;
                    p.CatchGraceSeconds = t.CatchGraceCoverHug;
                    p.CeilingLurker = true;
                    break;
            }
            return p;
        }
    }
}
