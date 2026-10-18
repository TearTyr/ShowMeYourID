using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Objective document rules from the paper note / rulebook.
    /// Kept pure and side-effect free so they are trivially testable.</summary>
    public static class IdRules
    {
        public const string Prefix = "HB-";

        /// <summary>Required format: HB-YYYY-NNNN, YYYY in 2023..2027, NNNN in 0001..9999.</summary>
        public static bool IsFormatValid(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id.Length != 12) return false;                 // HB-YYYY-NNNN
            if (id[0] != 'H' || id[1] != 'B' || id[2] != '-') return false;
            if (id[7] != '-') return false;
            for (int i = 3; i <= 6; i++) if (!char.IsDigit(id[i])) return false;
            for (int i = 8; i <= 11; i++) if (!char.IsDigit(id[i])) return false;
            int year = int.Parse(id.Substring(3, 4));
            int serial = int.Parse(id.Substring(8, 4));
            if (year < 2023 || year > 2027) return false;      // plausible enrollment years only
            if (serial < 1 || serial > 9999) return false;     // 0000 is not issued
            return true;
        }

        public static int SerialOf(string id)
        {
            if (!IsFormatValid(id)) return -1;
            return int.Parse(id.Substring(8, 4));
        }

        public static bool IsExpired(VisitorData v, int shiftNumber)
        {
            return v.ExpirationDay < Content.ShiftDayNumber(shiftNumber);
        }

        /// <summary>Registry lookup ("Scan" button / student database).</summary>
        public static bool DatabaseLookup(string id, ISet<string> registry)
        {
            return registry != null && !string.IsNullOrEmpty(id) && registry.Contains(id);
        }

        /// <summary>Ground-truth evaluation of every objective discrepancy on a visitor.
        /// A visitor is "clean" when this returns an empty list — used by tests to assert
        /// fairness (legit visitors are always clean, anomalies never are).</summary>
        public static List<DefectType> EvaluateDocuments(VisitorData v, ISet<string> registry,
            PresentationLog playLog)
        {
            // Shift-5 administrator: staff badges are not student IDs; the objective check is
            // badge-vs-roster-memo (name + number) per the Night 5 rule addendum.
            if (v.IsAdministrator)
            {
                var adminDefects = new List<DefectType>();
                if (v.ClaimedName != v.RecordName) adminDefects.Add(DefectType.RecordNameMismatch);
                if (v.IdNumber != v.RecordIdNumber) adminDefects.Add(DefectType.NotInDatabase);
                return adminDefects;
            }

            var found = new List<DefectType>();
            if (!v.PhotoMatches)
                found.Add(WindowPortraitIsSubtlyOff(v) ? DefectType.PhotoSubtleMismatch : DefectType.PhotoMismatch);
            if (!IsFormatValid(v.IdNumber)) found.Add(DefectType.IdFormatInvalid);
            if (IsExpired(v, v.ShiftNumber)) found.Add(DefectType.ExpiredId);
            if (v.IdYearLevel != v.RecordYearLevel) found.Add(DefectType.YearMismatch);
            if (v.IdCourseIndex != v.RecordCourseIndex) found.Add(DefectType.CourseMismatch);
            if (v.IdIsTransferee != v.RecordIsTransferee) found.Add(DefectType.StatusMismatch);
            if (v.ClaimedName != v.RecordName) found.Add(DefectType.RecordNameMismatch);
            if (!DatabaseLookup(v.IdNumber, registry)) found.Add(DefectType.NotInDatabase);
            if (playLog != null && IsFormatValid(v.IdNumber) &&
                playLog.WasPresentedWithDifferentCard(v.IdNumber, v.IdPhoto))
                found.Add(DefectType.DuplicateId);
            return found;
        }

        private static bool WindowPortraitIsSubtlyOff(VisitorData v)
        {
            // Subtle = exactly one of hair/face/accessory differs (generated that way).
            int diffs = 0;
            if (v.WindowPortrait.HairStyle != v.IdPhoto.HairStyle) diffs++;
            if (v.WindowPortrait.FaceVariant != v.IdPhoto.FaceVariant) diffs++;
            if (v.WindowPortrait.Accessory != v.IdPhoto.Accessory) diffs++;
            return diffs == 1;
        }
    }
}
