namespace Hollowbrook.Logic
{
    public static class EndingRules
    {
        public const int LoseThreshold = 3;

        public class Evaluation
        {
            public EndingType Ending;
            public string ReasonKey;   // stable key for UI/captions/tests
            public string ReasonText;
        }

        public static Evaluation Evaluate(GameSession s)
        {
            // Normal mode: nights 1-4 never end the game (a bad night just fails the shift and
            // costs the next one); the lose conditions below only apply on the final night.
            // Endless and Challenge keep them throughout.
            bool losesCount = s.Mode != GameMode.Normal || s.CurrentShift >= ShiftPlan.TotalShifts;
            if (losesCount) { var lose = EvaluateLosses(s); if (lose != null) return lose; }
            return EvaluateOutcome(s);
        }

        private static Evaluation EvaluateLosses(GameSession s)
        {
            if (s.AdminAllowed)
                return Result(EndingType.Lose, "admin_breach",
                    "You let the administrator through. It walked straight to the server room, and the identification system came online. By morning the district had already replaced you.");
            if (s.Hunt == HuntState.Lost)
                return Result(EndingType.Lose, "server_door",
                    "You turned the administrator away, but he did not leave. He found the sealed server door, and the identification system woke behind it. By morning the district had replaced you.");
            if (s.Breaches >= LoseThreshold)
                return Result(EndingType.Lose, "breaches",
                    "Three anomalies entered Hollowbrook on your watch. The district pulled your contract before dawn.");
            if (s.FalseRejections >= LoseThreshold)
                return Result(EndingType.Lose, "false_rejections",
                    "Three legitimate students were turned away at your window. The complaints reached the district before your explanation did.");
            if (s.QueueOverwhelmed)
                return Result(EndingType.Lose, "overwhelmed",
                    "The line overran the checkpoint. Too many faces at the glass, too little time to hold them - the gate gave way, and the yard poured into Hollowbrook unchecked. The district ended your contract before dawn.");
            return null;
        }

        private static Evaluation EvaluateOutcome(GameSession s)
        {

            if (s.Mode == GameMode.Normal && s.CompletedShifts >= ShiftPlan.TotalShifts)
            {
                // The Secret ending is the clue chain's own prize (ten findings, in order, in
                // the right rooms) and outranks the ordinary Win; a hard Lose outranks both.
                if (s.Clues.SecretConditionsMet)
                    return Result(EndingType.Secret, "secret",
                        "Ten findings, in the order they were meant to be found. You leave Hollowbrook carrying what the official explanation never included - and what no custodian before you ever discovered.");
                // The design's Win definition names the administrator's rejection explicitly,
                // so the evaluator checks it explicitly: a completed fifth shift with the
                // administrator neither rejected nor allowed is an unfinished night, not a win.
                if (!s.AdminRejected || s.Hunt == HuntState.Active)
                    return Result(EndingType.None, "admin_unresolved",
                        "The books close with the district's administrator neither admitted nor turned away. Some nights refuse to be filed.");
                return Result(EndingType.Win, "win",
                    "Five nights. You trusted your own judgment, kept the checkpoint, and drove the thing wearing the administrator's face back into the dark. You walk out of Hollowbrook relieved - and alive.");
            }
            return Result(EndingType.None, "continue", "");
        }

        public static Evaluation EvaluateExhibition(GameSession s)
        {
            var lose = Evaluate(s);
            if (lose.Ending == EndingType.Lose) return lose;
            if (s.Clues.SecretConditionsMet)
                return Result(EndingType.Secret, "secret", lose.ReasonText);
            return Result(EndingType.Win, "exhibition",
                "The bell rings. Four faces, one of them wrong, and the wrong one did not get in. " +
                "Hollowbrook keeps its secrets one more night - demo complete, thank you for playing.");
        }

        private static Evaluation Result(EndingType t, string key, string text) =>
            new Evaluation { Ending = t, ReasonKey = key, ReasonText = text };
    }

    public static class EndlessRules
    {
        public const float StartSeconds = 150f;
        public const float TimePerAnomalyRejected = 20f;
        public const float TimePerLegitAllowed = 8f;
        public const float TimePenaltyForMistake = 12f;
        public const float HardCapSeconds = 420f;

        public static float AnomalyChance(int processedSoFar)
        {
            float c = 0.15f + processedSoFar * 0.02f;
            return c > 0.6f ? 0.6f : c;
        }

        public static int ScoreFor(InspectionRules.Outcome o, int streakAfter)
        {
            int baseScore = o.WasAnomaly ? 100 : 50;
            return o.Correct ? (int)(baseScore * StreakMultiplier(streakAfter)) : 0;
        }

        public static float StreakMultiplier(int streak)
        {
            float m = 1f + (streak / 3) * 0.5f; // integer division: +0.5x every 3 streak
            return m > 4f ? 4f : m;
        }
    }

    public static class ChallengeRules
    {
        public const int VisitorsPerRuleWave = 4;
        public const float ScoreMultiplier = 2f;
        public const float StartSeconds = EndlessRules.StartSeconds;

        public static RuleMutationDef RollMutation(int waveIndex, GameRng rng)
        {
            if (waveIndex <= 0) return ShiftPlan.ChallengeMutations[0];
            int idx = rng.NextInt(1, ShiftPlan.ChallengeMutations.Length);
            return ShiftPlan.ChallengeMutations[idx];
        }
    }
}
