using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class InspectionRules
    {
        public class Outcome
        {
            public Verdict Verdict;
            public Verdict Expected;
            public bool Correct;
            public bool FalseRejection;   // legit rejected (not via an official rule mutation)
            public bool AnomalyBreach;    // anomaly allowed in
            public bool WasAnomaly;
            public bool WasAdministrator;
            public string Id;              // admitted-student bookkeeping for the classroom
            public string Name;
            public int VisitorIndex = -1;
            public List<string> RevealedDefects = new List<string>();
        }

        public static Verdict ExpectedVerdict(VisitorData v, RuleMutationDef activeMutation)
        {
            if (activeMutation != null && activeMutation.ForcesReject(v)) return Verdict.Reject;
            return v.IsAnomaly ? Verdict.Reject : Verdict.Allow;
        }

        public static Outcome Apply(VisitorData v, Verdict verdict, RuleMutationDef activeMutation)
        {
            var expected = ExpectedVerdict(v, activeMutation);
            bool forcedByRule = activeMutation != null && activeMutation.ForcesReject(v);
            var o = new Outcome
            {
                Verdict = verdict,
                Expected = expected,
                Correct = verdict == expected,
                FalseRejection = verdict == Verdict.Reject && !v.IsAnomaly && !forcedByRule,
                AnomalyBreach = verdict == Verdict.Allow && v.IsAnomaly,
                WasAnomaly = v.IsAnomaly,
                WasAdministrator = v.IsAdministrator,
                Id = v.IdNumber,
                Name = v.ClaimedName,
                VisitorIndex = v.VisitorIndex,
                RevealedDefects = v.DescribeDefects(),
            };
            return o;
        }
    }
}
