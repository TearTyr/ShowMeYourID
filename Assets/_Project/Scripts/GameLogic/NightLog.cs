using System;
using System.Collections.Generic;

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

    /// <summary>The Security Log rules (pure, no Unity). The game fills in the three boxes by itself, so nobody
    /// has to count. What the log is FOR is a second chance: before END SHIFT the player may REPORT one person
    /// they let in. A right report (that person was an anomaly) cancels that one breach, so a night that would
    /// have failed does not. A wrong report (a real student) costs sanity at the start of the next night, and
    /// the next night's Rules sheet explains what happened (the note this class writes).</summary>
    public static class NightLogRules
    {
        public const int SanityLossPerWrongReport = 5;

        public const int BoxEncounters = 0, BoxRejected = 1, BoxIncidents = 2;

        /// <summary>The three numbers on the sheet. The middle one counts what the player rejected, not what was
        /// really an anomaly, so the sheet never gives a verdict away.</summary>
        /// <param name="records">everyone who reached the window tonight</param>
        /// <param name="incidents">students reported missing plus things driven out of the peek rooms</param>
        public static int[] Boxes(IList<NightRecord> records, int incidents)
        {
            int rejected = 0;
            foreach (var r in records) if (!r.Allowed) rejected++;
            return new[] { records.Count, rejected, incidents };
        }

        /// <summary>Only someone who was let in can be reported.</summary>
        public static bool CanReport(IList<NightRecord> records, int index) =>
            index >= 0 && index < records.Count && records[index].Allowed;

        public class Outcome
        {
            public bool Reported;       // the player filed a report
            public bool Cancels;        // it was right: that anomaly no longer counts as let in
            public int SanityLoss;      // it was wrong: paid at the start of the next night
            public string Note = "";    // for the Rules sheet of the next night
        }

        public static Outcome Resolve(IList<NightRecord> records, int index)
        {
            var o = new Outcome();
            if (!CanReport(records, index)) return o;
            var r = records[index];
            o.Reported = true;
            if (r.WasAnomaly)
            {
                o.Cancels = true;
                string why = string.IsNullOrEmpty(r.Why) ? "" : " (" + r.Why.TrimEnd('.') + ")";
                o.Note = "You reported " + r.Name + " and you were right" + why + ". That anomaly was dealt with, so it did not count against you.";
                return o;
            }
            o.SanityLoss = SanityLossPerWrongReport;
            o.Note = "You reported " + r.Name + ", but they were a real student. Penalty: " + o.SanityLoss + " sanity at the start of the night.";
            return o;
        }
    }
}
