using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Registry access for the STUDENT RECORDS folder on the desk: canon students are
    /// filed by cohort (the year they enrolled, shown as "S2024-2028") and the player browses
    /// the cohort folders instead of typing. A presented card whose number matches no filed
    /// record is itself a clue — ghost IDs are not in the registry.
    /// Pure and deterministic: same canon = same folders, always.</summary>
    public static class RecordSearch
    {
        /// <summary>The school year the game is set in: a student's cohort is this minus their
        /// year level, and it is the year printed in their ID number (HB-2025-0286).</summary>
        public const int CurrentSchoolYear = 2027;
        /// <summary>First cohort on file (a 3rd-year in <see cref="CurrentSchoolYear"/>).</summary>
        public const int FirstCohort = 2024;
        /// <summary>How many cohorts are on file: 1st, 2nd and 3rd year.</summary>
        public const int CohortCount = 3;
        /// <summary>Years a cohort's programme spans, as written on its folder (S2024-2028).</summary>
        public const int CohortSpanYears = 4;

        /// <summary>One cohort folder: its enrolment year, display label, and the canon indices
        /// filed under it (roster order).</summary>
        public class CohortFolder
        {
            public int Cohort;                                 // enrolment year, e.g. 2025
            public string Label = "";                          // e.g. "S2025-2029"
            public List<int> Indices = new List<int>();        // indices into canon, roster order
        }

        /// <summary>The enrolment year a student is filed under.</summary>
        public static int CohortOf(StudentIdentity s) => s == null ? 0 : CurrentSchoolYear - s.YearLevel;

        /// <summary>The enrolment year of the folder tab at <paramref name="tab"/> (0 = oldest).</summary>
        public static int CohortOfTab(int tab) => FirstCohort + tab;

        /// <summary>Display label of a cohort folder: "S2025-2029".</summary>
        public static string CohortLabel(int cohort) => "S" + cohort + "-" + (cohort + CohortSpanYears);

        /// <summary>Files the canon roster into cohort folders: one folder per cohort present,
        /// oldest first, students in roster order inside their folder. A null or empty canon
        /// files nothing.</summary>
        public static List<CohortFolder> BuildCohortFolders(IReadOnlyList<StudentIdentity> canon)
        {
            var folders = new List<CohortFolder>();
            if (canon == null) return folders;
            for (int i = 0; i < canon.Count; i++)
            {
                var s = canon[i];
                if (s == null) continue;
                int cohort = CohortOf(s);
                CohortFolder f = null;
                foreach (var cand in folders)
                    if (cand.Cohort == cohort) { f = cand; break; }
                if (f == null)
                {
                    f = new CohortFolder { Cohort = cohort, Label = CohortLabel(cohort) };
                    folders.Add(f);
                }
                f.Indices.Add(i);
            }
            folders.Sort((a, b) => a.Cohort.CompareTo(b.Cohort));
            return folders;
        }
    }
}
