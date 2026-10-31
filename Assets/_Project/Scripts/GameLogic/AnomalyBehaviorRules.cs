namespace Hollowbrook.Logic
{
    /// <summary>Per-behavior movement/threat numbers. Timing differences between the four
    /// anomalies are explicit here - no variable named after one behavior is shared by all.</summary>
    public struct BehaviorTuning
    {
        public float BlinkSpeed;          // world units/second while creeping
        public float SlideSpeed;          // lateral glide speed
        public float SprintSpeed;         // patrol speed between bursts
        public float SprintDashSpeed;     // burst speed
        public float SprintTelegraphSeconds; // readable warning before the burst
        public float SprintDashSeconds;   // burst duration
        public float SprintRecoverSeconds;   // pause after a burst before re-telegraphing
        public float SprintAggroRange;    // world distance inside which it winds up
        public float CoverHugSpeed;       // ceiling drift speed
        public float ThreatRange;         // distance at which the catch windup runs
        public float CatchGraceBlink;     // continuous threatening seconds before a catch
        public float CatchGraceSlide;
        public float CatchGraceSprint;
        public float CatchGraceCoverHug;
        public float RetreatSpeed;        // how fast a repelled anomaly withdraws

        public static BehaviorTuning Default => new BehaviorTuning
        {
            BlinkSpeed = 1.15f,
            SlideSpeed = 0.9f,
            SprintSpeed = 1.3f,
            SprintDashSpeed = 4.5f,
            SprintTelegraphSeconds = 1.1f,
            SprintDashSeconds = 1.4f,
            SprintRecoverSeconds = 1.4f,
            SprintAggroRange = 9f,
            CoverHugSpeed = 0.55f,
            ThreatRange = 1.6f,
            CatchGraceBlink = 2.2f,
            CatchGraceSlide = 2.4f,
            CatchGraceSprint = 1.8f,
            CatchGraceCoverHug = 2.6f,
            RetreatSpeed = 3.2f,
        };
    }

    /// <summary>The resolved rules for one anomaly kind. Y values are offsets above the room
    /// floor line (world y = floorY + offset), so spawn and clamp bands can never contradict.</summary>
    public struct BehaviorProfile
    {
        public float MoveSpeed;
        public float SpawnY;              // floor-relative spawn height
        public float MinY, MaxY;          // floor-relative vertical band it may never leave
        public float ThreatRange;
        public float CatchGraceSeconds;
        public float TelegraphSeconds;    // 0 = behavior has no windup burst
        public float DashSpeed;           // 0 = behavior never bursts
        public float DashSeconds;
        public float DashRecoverSeconds;
        public float AggroRange;
        public bool FreezesWhenObserved;  // Blink: any beam or room light holds it still
        public bool CeilingLurker;        // CoverHug: only the Focus beam finds it clearly
        public bool BeamInterruptsBurst;  // Sprint: light on it cancels the windup
        public float RetreatSpeed;

        public static BehaviorProfile For(AnomalyBehaviorKind kind, BehaviorTuning t)
        {
            var p = new BehaviorProfile
            {
                ThreatRange = t.ThreatRange,
                RetreatSpeed = t.RetreatSpeed,
                AggroRange = t.SprintAggroRange,
            };
            switch (kind)
            {
                case AnomalyBehaviorKind.Blink:
                    p.MoveSpeed = t.BlinkSpeed;
                    p.SpawnY = 2.4f; p.MinY = 2.4f; p.MaxY = 2.4f;
                    p.CatchGraceSeconds = t.CatchGraceBlink;
                    p.FreezesWhenObserved = true;
                    break;
                case AnomalyBehaviorKind.Slide:
                    p.MoveSpeed = t.SlideSpeed;
                    p.SpawnY = 2.4f; p.MinY = 2.4f; p.MaxY = 2.4f;
                    p.CatchGraceSeconds = t.CatchGraceSlide;
                    break;
                case AnomalyBehaviorKind.Sprint:
                    p.MoveSpeed = t.SprintSpeed;
                    p.SpawnY = 2.4f; p.MinY = 2.4f; p.MaxY = 2.4f;
                    p.CatchGraceSeconds = t.CatchGraceSprint;
                    p.TelegraphSeconds = t.SprintTelegraphSeconds;
                    p.DashSpeed = t.SprintDashSpeed;
                    p.DashSeconds = t.SprintDashSeconds;
                    p.DashRecoverSeconds = t.SprintRecoverSeconds;
                    p.BeamInterruptsBurst = true;
                    break;
                default: // CoverHug - ceiling lurker; spawn inside its own band, never clamped out
                    p.MoveSpeed = t.CoverHugSpeed;
                    p.SpawnY = 4.4f; p.MinY = 3.6f; p.MaxY = 4.6f;
                    p.CatchGraceSeconds = t.CatchGraceCoverHug;
                    p.CeilingLurker = true;
                    break;
            }
            return p;
        }
    }
}
