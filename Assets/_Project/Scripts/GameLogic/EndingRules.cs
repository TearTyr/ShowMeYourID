namespace Hollowbrook.Logic
{
    public static class EndingRules
    {
        public static int LoseThreshold => GameRules.Current.LoseThreshold;

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
            bool losesCount = s.CurrentShift >= ShiftPlan.TotalShifts;
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
                    "You turned the administrator away, but your light failed you too many times. He found the sealed server door, and the identification system woke behind it. By morning the district had replaced you.");
            // these two can land after dawn: surviving him does not undo the nights before
            string survived = s.Hunt == HuntState.Won ? "You lasted until dawn, and it was not enough. " : "";
            if (s.Breaches >= LoseThreshold)
                return Result(EndingType.Lose, "breaches",
                    survived + "Too many anomalies entered Hollowbrook on your watch. The district pulled your contract.");
            if (s.FalseRejections >= LoseThreshold)
                return Result(EndingType.Lose, "false_rejections",
                    survived + "Too many legitimate students were turned away at your window. The complaints reached the district before your explanation did.");
            if (s.QueueOverwhelmed)
                return Result(EndingType.Lose, "overwhelmed",
                    "The line overran the checkpoint. Too many faces at the glass, too little time to hold them - the gate gave way, and the yard poured into Hollowbrook unchecked. The district ended your contract before dawn.");
            return null;
        }

        private static Evaluation EvaluateOutcome(GameSession s)
        {

            if (s.Mode == GameMode.Challenge && s.CompletedShifts >= 1)
            {
                // Night 6 is one night: he must be turned away and survived until dawn
                if (!s.AdminRejected || s.Hunt != HuntState.Won)
                    return Result(EndingType.None, "admin_unresolved", "He is still in the building.");
                return Result(EndingType.Win, "night6",
                    "Night 6. He came back, asked politely, and you said no a second time. The building keeps its door shut until morning, and nobody on the district's list is watching it.");
            }
            if (s.Mode == GameMode.Normal && s.CompletedShifts >= ShiftPlan.TotalShifts)
            {
                // both good endings need him turned away and survived until dawn
                if (!s.AdminRejected || s.Hunt != HuntState.Won)
                    return Result(EndingType.None, "admin_unresolved",
                        "The books close with the district's administrator neither admitted nor turned away. Some nights refuse to be filed.");
                // the Secret is the clue chain's own prize (ten findings, in order) and outranks the Win
                if (s.Clues.SecretConditionsMet)
                    return Result(EndingType.Secret, "secret",
                        "Every finding, in the order it was meant to be found. You leave Hollowbrook carrying what the official explanation never included - and what no custodian before you ever discovered.");
                return Result(EndingType.Win, "win",
                    "Every night of it. You trusted your own judgment, kept the checkpoint, and drove the thing wearing the administrator's face back into the dark. You walk out of Hollowbrook relieved - and alive.");
            }
            return Result(EndingType.None, "continue", "");
        }

        private static Evaluation Result(EndingType t, string key, string text) =>
            new Evaluation { Ending = t, ReasonKey = key, ReasonText = text };
    }
}
