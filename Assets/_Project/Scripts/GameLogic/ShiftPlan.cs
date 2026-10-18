using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The approved five-shift Normal progression (4/6/7/8/8+admin).</summary>
    public static class ShiftPlan
    {
        public const int TotalShifts = 5;

        public static List<ShiftDef> Build()
        {
            return new List<ShiftDef>
            {
                new ShiftDef
                {
                    Number = 1, DateLabel = Content.ShiftDates[0],
                    VisitorCount = 4, AnomalyCount = 0, HasAdministrator = false,
                    Title = "Introduction",
                    Objective = "Learn the checkpoint: inspect IDs and records, ask up to 2 questions, and stamp Allow or Reject. Take a walk — learn how to leave the security room and come back.",
                    RuleNoteAddendum = "NIGHT 1 — Welcome, custodian. Verify: ID number format (HB-YYYY-NNNN), photo vs. the person at the window, expiration date, year level, course, and Regular/Transferee status against the student record. You may ask up to 2 questions.",
                    MaxDefectsPerAnomaly = 1, SubtleDefectsAllowed = false, DuplicateIdsAllowed = false,
                    BehaviorOddityAllowed = false, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 2, DateLabel = Content.ShiftDates[1],
                    VisitorCount = 6, AnomalyCount = 2, HasAdministrator = false,
                    Title = "Initial Inconsistencies",
                    Objective = "Small contradictions are appearing in documents, faces, and answers. Not everyone at the window is a student. The hallway is open between visitors — look around.",
                    RuleNoteAddendum = "NIGHT 2 — District reminder: an expired ID must never be accepted, whatever the excuse. If documents disagree, the person does not enter.",
                    MaxDefectsPerAnomaly = 1, SubtleDefectsAllowed = false, DuplicateIdsAllowed = false,
                    BehaviorOddityAllowed = false, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 3, DateLabel = Content.ShiftDates[2],
                    VisitorCount = 7, AnomalyCount = 3, HasAdministrator = false,
                    Title = "Increasing Anomalies",
                    Objective = "Cases are harder tonight — subtle photo differences and recycled identities. Use SCAN against the student database and cross-check everything twice.",
                    RuleNoteAddendum = "NIGHT 3 — WARNING: duplicate ID numbers have been reported at other district sites. Use the scanner: a valid ID already presented tonight with a different card is grounds for immediate rejection.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = false,
                },
                new ShiftDef
                {
                    Number = 4, DateLabel = Content.ShiftDates[3],
                    VisitorCount = 8, AnomalyCount = 4, HasAdministrator = false,
                    Title = "Investigation",
                    Objective = "Something happened in this school. Between visitors, investigate the hallway and both classrooms. Whatever the previous custodians found — find it too.",
                    RuleNoteAddendum = "NIGHT 4 — The district has scheduled this facility for final closure. Custodial staff are instructed to document anything unusual found inside the building. Investigation is now part of your duties.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = true,
                },
                new ShiftDef
                {
                    Number = 5, DateLabel = Content.ShiftDates[4],
                    VisitorCount = 8, AnomalyCount = 4, HasAdministrator = true,
                    Title = "Final Shift",
                    Objective = "Last night. A district administrator will arrive to take control of the school. Check the credentials like you would check anyone else's. Trust the evidence.",
                    RuleNoteAddendum = "NIGHT 5 — District personnel present a STAFF BADGE, not a student ID. The badge name and number must match the attached District Roster Memo. If they do not match, deny entry. No exceptions. No overrides by phone.",
                    MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                    BehaviorOddityAllowed = true, ExplorationFocus = false,
                },
            };
        }

        public static ShiftDef Get(int number)
        {
            var all = Build();
            foreach (var s in all) if (s.Number == number) return s;
            return null;
        }

        public static readonly RuleMutationDef[] ChallengeMutations =
        {
            new RuleMutationDef("none", null),
            new RuleMutationDef("no_transferees",
                "TEMPORARY DISTRICT RULE: Transferees are not admitted on nights with active system audits. Reject all transferees."),
            new RuleMutationDef("fourth_year_curfew",
                "TEMPORARY DISTRICT RULE: Fourth-year students are under district curfew tonight. Reject all 4th Years."),
            new RuleMutationDef("strict_expiry",
                "TEMPORARY DISTRICT RULE: IDs expiring within 60 days are not accepted during audits. Reject them."),
            new RuleMutationDef("odd_serials",
                "TEMPORARY DISTRICT RULE: For tonight's serial audit, IDs with an ODD serial number are suspended. Reject them."),
        };
    }

    /// <summary>Pure sanity model. Sanity is atmosphere/perception, not a health bar:
    /// it never directly ends the run (per the approved concept), but low sanity drives
    /// hallucinations, distortion, and scare frequency.</summary>
    public static class SanityModel
    {
        public const float Max = 100f;
        public const float Min = 0f;

        public static SanityStage StageFor(float sanity)
        {
            if (sanity >= 70f) return SanityStage.Stable;
            if (sanity >= 40f) return SanityStage.Uneasy;
            if (sanity >= 15f) return SanityStage.Shaken;
            return SanityStage.Breaking;
        }

        /// <summary>Chance per ambient tick (1s) that a hallucination fires at this stage.</summary>
        public static float HallucinationChancePerSecond(SanityStage stage)
        {
            switch (stage)
            {
                case SanityStage.Uneasy: return 0.010f;
                case SanityStage.Shaken: return 0.035f;
                case SanityStage.Breaking: return 0.080f;
                default: return 0f;
            }
        }

        /// <summary>Recovery while stationed in the lit security room (per second).</summary>
        public const float SecurityRoomRecoveryPerSecond = 1.2f;
        /// <summary>Drain while inside an unlit room (per second).</summary>
        public const float DarknessDrainPerSecond = 0.6f;

        public static float Clamp(float v) => v < Min ? Min : (v > Max ? Max : v);

        /// <summary>Mode multipliers: Challenge mode drains sanity faster (approved spec).</summary>
        public static float DrainMultiplier(GameMode mode) => mode == GameMode.Challenge ? 2f : 1f;
    }
}
