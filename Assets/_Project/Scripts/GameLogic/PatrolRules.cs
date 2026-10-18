using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The security office (Scene B) expressed as rules, not as a view: tonight's late
    /// list is an OFFICIAL rule, so office verdicts are scored by the very same engine as the
    /// checkpoint (InspectionRules/IdRules) and feed the same story counters.</summary>
    public static class PatrolRules
    {
        public const int LateArrivals = 3;

        /// <summary>Printed on the office door panel AND used for scoring — one source of truth.</summary>
        public static readonly RuleMutationDef OfficeRule = new RuleMutationDef(
            "office_late_list",
            "OFFICE RULE: admit only students on tonight's late list whose ID is NOT expired.");

        public static Verdict ExpectedOfficeVerdict(VisitorData v) =>
            InspectionRules.ExpectedVerdict(v, OfficeRule);

        /// <summary>Tonight's late queue: outstanding anomalies from the shared patrol pool first
        /// (an uncaught anomaly can knock), then real late students — most clean and on the list
        /// (Allow), some off the list (Reject by the office rule, never a "false rejection").
        /// Fairness invariant holds: every anomaly is detectable, every real student is clean.</summary>
        public static List<VisitorData> BuildLateQueue(RosterFactory roster, ShiftDef shift,
            PatrolPool pool, GameRng rng)
        {
            var q = new List<VisitorData>();
            if (roster == null || shift == null) return q;

            int fromPool = System.Math.Min(pool != null ? pool.Pending : 0, rng.NextInt(0, 2));
            for (int i = 0; i < fromPool; i++)
            {
                AnomalyBehaviorKind kind;
                if (pool == null || !pool.TryTake(rng, out kind)) break;
                var v = roster.BuildLateStudent(roster.PickIdentity(rng), shift, true);
                q.Add(v);
            }

            int legit = LateArrivals - q.Count;
            for (int i = 0; i < legit; i++)
            {
                var v = roster.BuildLateStudent(roster.PickIdentity(rng), shift, false);
                if (i == legit - 1 && legit > 1) v.OnLateList = false;  // the rule has a case to act on
                q.Add(v);
            }

            rng.Shuffle(q);
            return q;
        }
    }
}
