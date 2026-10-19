using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    // Part of the gamelogic folder.
    public static class IdRules
    {
        public const string Prefix = "HB-";

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

        public static bool DatabaseLookup(string id, ISet<string> registry)
        {
            return registry != null && !string.IsNullOrEmpty(id) && registry.Contains(id);
        }

        public static List<DefectType> EvaluateDocuments(VisitorData v, ISet<string> registry,
            PresentationLog playLog)
        {
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
            int diffs = 0;
            if (v.WindowPortrait.HairStyle != v.IdPhoto.HairStyle) diffs++;
            if (v.WindowPortrait.FaceVariant != v.IdPhoto.FaceVariant) diffs++;
            if (v.WindowPortrait.Accessory != v.IdPhoto.Accessory) diffs++;
            return diffs == 1;
        }
    }
}
