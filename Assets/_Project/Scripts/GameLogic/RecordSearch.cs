using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Registry lookup for the records station: the player searches by ID number or
    /// by name and gets the matching canon student (the ground-truth file), or nothing.
    /// "No record" is itself a clue — ghost IDs are not in the registry. Pure and
    /// deterministic: same canon + same query = same answer, always.</summary>
    public static class RecordSearch
    {
        // the registry rows are StudentIdentity (canon); visitors are claimed paperwork
        /// <summary>Index into <paramref name="canon"/>, or -1 when nothing matches.
        /// Exact (case-insensitive) ID match wins; otherwise the first name that contains
        /// the query (case-insensitive, queries of 3+ chars so "a" never matches all).</summary>
        public static int FindInCanon(IReadOnlyList<StudentIdentity> canon, string query)
        {
            if (canon == null || string.IsNullOrEmpty(query)) return -1;
            string q = query.Trim();
            if (q.Length == 0) return -1;
            string qu = q.ToUpperInvariant();
            for (int i = 0; i < canon.Count; i++)
                if (canon[i] != null && canon[i].IdNumber != null &&
                    canon[i].IdNumber.ToUpperInvariant() == qu) return i;
            if (qu.Length < 3) return -1;
            for (int i = 0; i < canon.Count; i++)
                if (canon[i] != null && canon[i].FullName != null &&
                    canon[i].FullName.ToUpperInvariant().Contains(qu)) return i;
            return -1;
        }
    }
}
