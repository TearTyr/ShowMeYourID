using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public static class PatrolRules
    {
        public const int LateArrivals = 3;

        public static readonly RuleMutationDef OfficeRule = new RuleMutationDef(
            "office_late_list",
            "OFFICE RULE: admit only students on tonight's late list whose ID is NOT expired.");

        public static Verdict ExpectedOfficeVerdict(VisitorData v) =>
            InspectionRules.ExpectedVerdict(v, OfficeRule);

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
