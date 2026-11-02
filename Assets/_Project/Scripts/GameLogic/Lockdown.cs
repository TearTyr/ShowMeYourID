namespace Hollowbrook.Logic
{
    /// <summary>The Lockdown mode's rules: one finite night, every student at the gate at once, a clock on each
    /// face, and three mistakes before the gate gives. Pure numbers and decisions, no Unity.</summary>
    public static class LockdownRules
    {
        public const float SecondsPerVisitor = 35f;
        public const int MaxStrikes = 3;
        public const float ArrivalMinSeconds = 8f;    // they come fast
        public const float ArrivalMaxSeconds = 11f;
        public const float WarnSeconds = 10f;         // the bar turns red below this

        /// <summary>A stamp is a mistake when an anomaly is let in or a student is turned away.</summary>
        public static bool IsStrike(bool wasAnomaly, Verdict verdict)
        {
            if (verdict == Verdict.Allow) return wasAnomaly;
            if (verdict == Verdict.Reject) return !wasAnomaly;
            return false;
        }

        /// <summary>When the time runs out the thing outside takes whoever is at the window. Losing a real
        /// student is a mistake; an anomaly that is taken back out is not.</summary>
        public static bool TakenIsStrike(bool wasAnomaly) => !wasAnomaly;

        public static bool Lost(int strikes) => strikes >= MaxStrikes;

        public static string Rank(int strikes)
        {
            if (strikes <= 0) return "FLAWLESS";
            if (strikes == 1) return "STEADY";
            return "BARELY HELD";
        }

        public static string WinText(int strikes) =>
            "The last face left the window and the gate held. " + (strikes == 0
                ? "Not one mistake: thirteen calls, thirteen right."
                : "You made " + strikes + (strikes == 1 ? " mistake" : " mistakes") + " and the gate held anyway.");

        public static string LoseText =>
            "That was the third mistake. The gate gave way and the dark came in with the line still waiting. The district ended your contract before dawn.";
    }

    /// <summary>The thirty five seconds on the face at the window. Start it for each new visitor, tick it with
    /// the game's time, and it says once when the time is up.</summary>
    public class LockdownClock
    {
        public float SecondsLeft { get; private set; }
        public bool Running { get; private set; }

        public float Fraction => LockdownRules.SecondsPerVisitor <= 0f ? 0f : SecondsLeft / LockdownRules.SecondsPerVisitor;
        public bool Warning => Running && SecondsLeft <= LockdownRules.WarnSeconds;

        public void Start()
        {
            SecondsLeft = LockdownRules.SecondsPerVisitor;
            Running = true;
        }

        public void Stop() => Running = false;

        /// <summary>True on the one tick that runs the clock out.</summary>
        public bool Tick(float dt)
        {
            if (!Running || dt <= 0f) return false;
            SecondsLeft -= dt;
            if (SecondsLeft > 0f) return false;
            SecondsLeft = 0f;
            Running = false;
            return true;
        }
    }
}
