using System;
using System.Collections.Generic;
using System.Text;

namespace Hollowbrook.Logic
{
    /// <summary>One person who reached the window tonight: what they really were, what the player did, and the
    /// first thing that was wrong with them. Written by GameSession.RecordVerdict, saved with the run.</summary>
    [Serializable]
    public class NightRecord
    {
        public string Name;
        public bool WasAnomaly;
        public bool Allowed;
        public string Why;   // the first defect in words, "" for a real student
    }

    /// <summary>The Security Log answer sheet (pure, no Unity). The player counts three things by hand during
    /// a night; at END SHIFT the game compares them with what really happened. A wrong box costs sanity at the
    /// start of the next night, and the only place the player learns what they missed is the Rules sheet of that
    /// next night (the note this class writes).</summary>
    public static class NightLogRules
    {
        public const int SanityLossPerMistake = 5;

        public class Grade
        {
            public bool[] Right = new bool[GameSession.LogEntries];
            public int Mistakes;
            public int SanityLoss;
            public string Note = "";
        }

        /// <param name="counts">the three boxes the player filled in</param>
        /// <param name="records">everyone who reached the window tonight</param>
        /// <param name="incidents">students reported missing plus things driven out of the peek rooms</param>
        public static Grade Check(int[] counts, IList<NightRecord> records, int incidents)
        {
            var grade = new Grade();
            int anomalies = 0;
            foreach (var r in records) if (r.WasAnomaly) anomalies++;
            int[] truth = { records.Count, anomalies, incidents };

            var lines = new StringBuilder();
            for (int i = 0; i < truth.Length; i++)
            {
                int wrote = counts != null && i < counts.Length ? counts[i] : 0;
                grade.Right[i] = wrote == truth[i];
                if (grade.Right[i]) continue;
                grade.Mistakes++;
                lines.Append(MistakeLine(i, wrote, truth[i], records)).Append('\n');
            }

            grade.SanityLoss = grade.Mistakes * SanityLossPerMistake;
            if (grade.Mistakes == 0)
            {
                grade.Note = "Your three counts matched the night.";
                return grade;
            }
            lines.Append("Penalty: ").Append(grade.SanityLoss).Append(" sanity at the start of the night.");
            grade.Note = lines.ToString();
            return grade;
        }

        private static string MistakeLine(int box, int wrote, int truth, IList<NightRecord> records)
        {
            string said = "you wrote " + wrote + ", there " + (truth == 1 ? "was " : "were ") + truth;
            switch (box)
            {
                case 0:
                    return "Student encounters: " + said + ". Everyone who reached the window counts, whatever you stamped.";
                case 1:
                    return "Anomalies detected: " + said + AnomalyList(records) + ". Count what they were, not what you stamped.";
                default:
                    return "Security incidents: " + said + ". A student who never reached the window counts, and so does anything driven out of a room.";
            }
        }

        // " (Name: the first thing wrong; Name: ...)" so the player can see what they missed
        private static string AnomalyList(IList<NightRecord> records)
        {
            var parts = new List<string>();
            foreach (var r in records)
            {
                if (!r.WasAnomaly) continue;
                parts.Add(string.IsNullOrEmpty(r.Why) ? r.Name : r.Name + ": " + r.Why.TrimEnd('.'));
            }
            return parts.Count == 0 ? "" : " (" + string.Join("; ", parts) + ")";
        }
    }
}
