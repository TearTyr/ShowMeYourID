using System.Collections.Generic;

namespace Hollowbrook.Logic
{
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
                    Objective = "Cases are harder tonight — subtle photo differences and recycled identities. Pull the floor files and cross-check everything twice.",
                    RuleNoteAddendum = "NIGHT 3 — WARNING: duplicate ID numbers have been reported at other district sites. A valid ID already presented tonight with a different card is grounds for immediate rejection.",
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

        public static ShiftDef ExhibitionDef()
        {
            return new ShiftDef
            {
                Number = 1,
                DateLabel = Content.ShiftDates[0],
                VisitorCount = 4, AnomalyCount = 1, HasAdministrator = false,
                Title = "Exhibition Night",
                Objective = "One short night: check the papers, ask when in doubt, stamp once. " +
                            "One of the four is not a student - the papers or the voice will give it away.",
                RuleNoteAddendum = "EXHIBITION - Verify: ID format (HB-YYYY-NNNN), photo vs the face at the " +
                            "window, expiration, year, course, Regular/Transferee. Up to 2 questions. " +
                            "A answer marked 'papers disagree' or an odd voice is grounds to call the principal.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = false,
                DuplicateIdsAllowed = false, BehaviorOddityAllowed = true, ExplorationFocus = false,
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
            new RuleMutationDef("strict_expiry",
                "TEMPORARY DISTRICT RULE: IDs expiring within 60 days are not accepted during audits. Reject them."),
            new RuleMutationDef("odd_serials",
                "TEMPORARY DISTRICT RULE: For tonight's serial audit, IDs with an ODD serial number are suspended. Reject them."),
        };
    }

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

        // Resting at the desk (the window is empty) recovers sanity; a student at the window
        // wears it down slowly; patrol in the dark wears it down a little faster. Wrong
        // decisions do not cost sanity directly: they add a SanityStacks drain stack.
        public const float SecurityRoomRecoveryPerSecond = 1.2f;
        public const float DeskDrainPerSecond = 0.25f;
        public const float DarknessDrainPerSecond = 0.3f;
        // A correct decision refills this much, handed over gradually (the bar fills in
        // RefillOnCorrect / RefillPerSecond = 6 s) rather than in one jump.
        public const float RefillOnCorrect = 15f;
        public const float RefillPerSecond = 2.5f;

        public static float Clamp(float v) => v < Min ? Min : (v > Max ? Max : v);

        public static float DrainMultiplier(GameMode mode) => mode == GameMode.Challenge ? 2f : 1f;
    }
}
