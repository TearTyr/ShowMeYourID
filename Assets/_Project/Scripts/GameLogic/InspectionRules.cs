using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The questioning mechanic (§2.3). Four question types are available;
    /// at most <see cref="Content.MaxQuestionsPerInspection"/> may be asked per inspection
    /// (conflict C1 resolution — the approved gameplay section and both testing checklists
    /// specify 1–2 questions).</summary>
    public static class QuestionSystem
    {
        public static readonly QuestionId[] All =
        {
            QuestionId.Course, QuestionId.Year, QuestionId.Status, QuestionId.FirstClassroom,
        };

        public static string QuestionText(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "What's your course?";
                case QuestionId.Year: return "What year are you in?";
                case QuestionId.Status: return "Are you a regular student or a transferee?";
                default: return "What classroom is your first class in?";
            }
        }

        public static Answer Resolve(VisitorData v, QuestionId q)
        {
            if (v.Answers != null && v.Answers.TryGetValue(q, out var a)) return a;
            return new Answer { Text = "…", Tone = ToneTag.Flat, ContradictsDocuments = false };
        }

        /// <summary>Per-inspection budget tracker.</summary>
        public class Budget
        {
            private readonly HashSet<QuestionId> _asked = new HashSet<QuestionId>();
            public int Max { get; }
            public int Asked => _asked.Count;
            public int Remaining => Max - _asked.Count;
            public bool HasAsked(QuestionId q) => _asked.Contains(q);

            public Budget(int max = Content.MaxQuestionsPerInspection) { Max = max; }

            public bool TryAsk(VisitorData v, QuestionId q, out Answer answer)
            {
                answer = default;
                if (_asked.Contains(q)) return false;      // no repeats
                if (_asked.Count >= Max) return false;     // budget spent
                _asked.Add(q);
                answer = Resolve(v, q);
                return true;
            }

            public void Reset() => _asked.Clear();
        }
    }

    /// <summary>Evaluates the player's Allow/Reject decision against ground truth.
    /// Decisions are permanent (§2.4).</summary>
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
            public List<string> RevealedDefects = new List<string>();
        }

        /// <summary>The verdict the rules require, given any active Challenge rule mutation.</summary>
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
                RevealedDefects = v.DescribeDefects(),
            };
            return o;
        }
    }
}
