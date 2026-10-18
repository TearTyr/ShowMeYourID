using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Procedural portrait descriptor (rendered by ProceduralArt on the Unity side).
    /// Monochrome silhouette style per the approved art direction.</summary>
    [Serializable]
    public struct Portrait
    {
        public int HairStyle;    // 0..5
        public int FaceVariant;  // 0..5
        public int Accessory;    // 0 none, 1 glasses, 2 earrings, 3 cap
        public bool Distorted;   // anomaly glitch overlay (static patches / shifted eyes)

        public static bool operator ==(Portrait a, Portrait b) =>
            a.HairStyle == b.HairStyle && a.FaceVariant == b.FaceVariant && a.Accessory == b.Accessory;
        public static bool operator !=(Portrait a, Portrait b) => !(a == b);
        public override bool Equals(object obj) => obj is Portrait p && p == this;
        public override int GetHashCode() => (HairStyle * 31 + FaceVariant) * 31 + Accessory;

        /// <summary>Obvious mismatch: everything differs. Used for PhotoMismatch.</summary>
        public Portrait Swapped() => new Portrait
        {
            HairStyle = (HairStyle + 3) % 6,
            FaceVariant = (FaceVariant + 2) % 6,
            Accessory = (Accessory + 1) % 4,
            Distorted = Distorted,
        };

        /// <summary>Single-feature mismatch. Used for PhotoSubtleMismatch (shift 3+).</summary>
        public Portrait SubtleVariant(GameRng rng)
        {
            var p = this;
            switch (rng.NextInt(0, 2))
            {
                case 0: p.HairStyle = (p.HairStyle + 1) % 6; break;
                default: p.FaceVariant = (p.FaceVariant + 1) % 6; break;
            }
            return p;
        }
    }

    /// <summary>One of the 14 unique students (8 core + 6 variations) from the asset plan.
    /// Identities are reused across shifts; anomalies wear existing identities.</summary>
    [Serializable]
    public class StudentIdentity
    {
        public string FullName;
        public string IdNumber;        // registry ID, always valid format: HB-YYYY-NNNN
        public int YearLevel;          // 1..4
        public int CourseIndex;        // index into Content.Courses
        public bool IsTransferee;
        public Portrait Portrait;
        public RoomId FirstClassroom;  // Room 101 (Classroom1) / Room 202 (Classroom2)
        public string FirstClassSubject;
        public string Greeting;        // flavor line at the window

        public string StatusString => IsTransferee ? "Transferee" : "Regular";
        public string YearString => Content.YearNames[YearLevel - 1];
        public string CourseString => Content.Courses[CourseIndex];
        public string ClassroomString =>
            (FirstClassroom == RoomId.Classroom1 ? "Room 101" : "Room 202") + " · " + FirstClassSubject;
    }

    /// <summary>Answer to one question, with delivery tone.</summary>
    [Serializable]
    public struct Answer
    {
        public string Text;
        public ToneTag Tone;
        /// <summary>True when the answer contradicts the visitor's presented documents
        /// (objective ground truth used by tests and the results summary).</summary>
        public bool ContradictsDocuments;
    }

    /// <summary>A person at the window. Legitimate visitors exactly match a StudentIdentity
    /// (zero defects). Anomalies carry 1+ defects; <see cref="IsAnomaly"/> is ground truth
    /// and must never be exposed to UI code directly.</summary>
    [Serializable]
    public class VisitorData
    {
        public int VisitorIndex;              // index within the whole run
        public int ShiftNumber;               // 1..5 (0 in endless/challenge)
        public bool IsAnomaly;
        public bool IsAdministrator;          // Shift-5 special encounter
        public string ClaimedName;            // name printed on the ID card
        public string RecordName;             // name printed on the record form
        public string IdNumber;               // printed on ID card
        public string RecordIdNumber;         // printed on record form
        public int IdYearLevel;               // year level printed on ID
        public int RecordYearLevel;           // year level printed on record
        public int IdCourseIndex;             // course printed on ID
        public int RecordCourseIndex;         // course printed on record
        public bool IdIsTransferee;           // status printed on ID
        public bool RecordIsTransferee;       // status printed on record
        public int ExpirationDay;             // absolute day number (see Content.DayNumber)
        public string ExpirationDisplay;      // e.g. "2027-03-31"
        public Portrait WindowPortrait;       // the face at the window (what player sees)
        public Portrait IdPhoto;              // photo printed on the ID card
        public bool InDatabase;               // scan result: registry lookup
        public string DuplicateOfShift;       // non-empty when DuplicateId ("previously seen Shift N")
        public string RecordFirstClassroom;   // schedule line on the record form
        public List<DefectType> Defects = new List<DefectType>();
        public string Greeting = "...";
        public Dictionary<QuestionId, Answer> Answers = new Dictionary<QuestionId, Answer>();
        public bool BehaviorGlitchVisible;    // rare visual micro-glitch hint for BehaviorOddity

        public bool PhotoMatches => WindowPortrait == IdPhoto;

        /// <summary>Ground-truth discrepancy list shown on the results banner after the verdict.</summary>
        public List<string> DescribeDefects()
        {
            var lines = new List<string>();
            foreach (var d in Defects)
            {
                switch (d)
                {
                    case DefectType.PhotoMismatch: lines.Add("The face at the window does not match the ID photo."); break;
                    case DefectType.PhotoSubtleMismatch: lines.Add("The ID photo differs subtly from the person (one feature)."); break;
                    case DefectType.ExpiredId: lines.Add("The ID expired on " + ExpirationDisplay + "."); break;
                    case DefectType.IdFormatInvalid: lines.Add("ID number '" + IdNumber + "' is not in HB-YYYY-NNNN format."); break;
                    case DefectType.YearMismatch: lines.Add("Year level: ID says " + Content.YearNames[IdYearLevel - 1] + ", record says " + Content.YearNames[RecordYearLevel - 1] + "."); break;
                    case DefectType.CourseMismatch: lines.Add("Course: ID says " + Content.Courses[IdCourseIndex] + ", record says " + Content.Courses[RecordCourseIndex] + "."); break;
                    case DefectType.StatusMismatch: lines.Add("Status: ID says " + (IdIsTransferee ? "Transferee" : "Regular") + ", record says " + (RecordIsTransferee ? "Transferee" : "Regular") + "."); break;
                    case DefectType.RecordNameMismatch: lines.Add("Record name '" + RecordName + "' does not match ID name '" + ClaimedName + "'."); break;
                    case DefectType.NotInDatabase: lines.Add("ID number not found in the student database."); break;
                    case DefectType.DuplicateId: lines.Add("ID number already used by a visitor from " + DuplicateOfShift + "."); break;
                    case DefectType.BehaviorOddity: lines.Add("Behavior and answers did not match the class schedule."); break;
                }
            }
            if (lines.Count == 0) lines.Add("No discrepancies — documents were consistent.");
            return lines;
        }
    }

    /// <summary>One of the ten environmental interactions (§2.6). The listing order in the
    /// source document is the intended sequence for the secret ending (conflict C5).</summary>
    [Serializable]
    public class ClueDef
    {
        public int Order;            // 1..10, intended sequence
        public string Key;           // stable save key, e.g. "light"
        public string Display;       // "Flickering Light"
        public RoomId Room;
        public string InspectText;   // what the player reads
        public string ChainHint;     // points to the next clue in the intended sequence
        public float SanityDelta;    // disturbing events cost sanity
        public bool IsScare;         // triggers a scare/stinger event
    }

    /// <summary>One night of Normal mode.</summary>
    [Serializable]
    public class ShiftDef
    {
        public int Number;           // 1..5
        public string DateLabel;     // "Mon · Nov 2"
        public int VisitorCount;     // 4/6/7/8/8 per the approved progression
        public int AnomalyCount;     // 0/2/3/4/4
        public bool HasAdministrator;
        public string Title;
        public string Objective;
        public string RuleNoteAddendum;  // new paper-note rule introduced this shift
        public int MaxDefectsPerAnomaly; // difficulty escalation
        public bool SubtleDefectsAllowed;
        public bool DuplicateIdsAllowed;
        public bool BehaviorOddityAllowed;
        public bool ExplorationFocus;    // shift 4 nudges investigation
    }

    /// <summary>Challenge-mode rule mutation: an official temporary policy change that
    /// flips the correct verdict for some legitimate visitors ("unpredictable rule changes").</summary>
    [Serializable]
    public class RuleMutationDef
    {
        public string Key;
        public string RuleText;       // printed on the paper note while active

        public RuleMutationDef(string key, string text) { Key = key; RuleText = text; }

        /// <summary>True when this mutation forces rejection of an otherwise-legit visitor.</summary>
        public bool ForcesReject(VisitorData v)
        {
            switch (Key)
            {
                case "no_transferees": return !v.IsAnomaly && v.IdIsTransferee;
                case "fourth_year_curfew": return !v.IsAnomaly && v.IdYearLevel == 4;
                case "strict_expiry": return !v.IsAnomaly && v.ExpirationDay - Content.ShiftDayNumber(v.ShiftNumber) <= 60;
                case "odd_serials": return !v.IsAnomaly && IdRules.SerialOf(v.IdNumber) % 2 == 1;
                default: return false;
            }
        }
    }

    /// <summary>Static content pools shared by generation, UI and tests.</summary>
    public static class Content
    {
        public const int TotalStudents = 14;          // 8 core + 6 variations (approved asset plan)
        public const int MaxQuestionsPerInspection = 2; // conflict C1 resolution: §2.3 + checklists say 1–2

        public static readonly string[] Courses =
        {
            "BS Information Technology", "BS Accountancy", "AB Communication", "BS Nursing",
            "BS Criminology", "BS Psychology", "BS Biology", "BA Multimedia Arts",
        };

        public static readonly string[] YearNames = { "1st Year", "2nd Year", "3rd Year", "4th Year" };

        // Five consecutive nights. The school is finishing the term before permanent closure.
        public static readonly string[] ShiftDates =
        {
            "Mon · Nov 2", "Tue · Nov 3", "Wed · Nov 4", "Thu · Nov 5", "Fri · Nov 6",
        };

        /// <summary>Absolute day number for the given shift night (1..5). Day 1 = 2026-11-02.
        /// Used for expiration comparisons without pulling in DateTime parsing.</summary>
        public static int ShiftDayNumber(int shift) => 1000 + shift; // arbitrary monotonic base

        public static int DayNumberOfDate(int year, int month, int day)
        {
            // Only dates in the story window matter; map them onto the same monotonic axis.
            // 2026-11-02 == ShiftDayNumber(1) == 1001. 30-day months approximation is fine
            // because all comparisons are order-based and dates come from this function.
            int y = year - 2026, m = month, d = day;
            return 1001 + (y * 360 + (m - 11) * 30 + (d - 2));
        }

        public static string DisplayDate(int year, int month, int day) =>
            year.ToString("D4") + "-" + month.ToString("D2") + "-" + day.ToString("D2");

        /// <summary>The 14 unique students. Deterministic (no RNG) so all systems share one canon.</summary>
        public static List<StudentIdentity> BuildStudentRoster()
        {
            string[,] core =
            {
                // name,            year, course, transferee, hair, face, acc, room(1/2), subject
                { "Ana Delgado",     "3", "0", "0", "0", "0", "1", "1", "Data Structures" },
                { "Marco Reyes",     "2", "4", "0", "1", "1", "0", "2", "Criminal Law" },
                { "Liza Santos",     "4", "1", "0", "2", "2", "1", "1", "Taxation Review" },
                { "Jerome Cruz",     "1", "7", "0", "3", "3", "3", "2", "Digital Sketching" },
                { "Bea Villanueva",  "3", "3", "1", "4", "4", "2", "1", "Anatomy" },
                { "Carlo Mendoza",   "2", "0", "0", "5", "5", "0", "2", "Networking" },
                { "Trisha Ramos",    "4", "2", "0", "0", "3", "2", "1", "Media Ethics" },
                { "Miguel Bautista", "1", "5", "1", "1", "4", "0", "2", "Intro Psychology" },
                // 6 extra variations
                { "Grace Lim",       "3", "6", "0", "2", "5", "1", "1", "Genetics" },
                { "Paolo Soriano",   "2", "1", "1", "3", "0", "0", "2", "Bookkeeping" },
                { "Kim Alonzo",      "4", "4", "0", "4", "1", "3", "1", "Forensics" },
                { "Rica Fernandez",  "1", "2", "0", "5", "2", "2", "2", "Public Speaking" },
                { "Diego Navarro",   "3", "5", "0", "0", "4", "0", "1", "Abnormal Psych" },
                { "Faith Gonzales",  "2", "3", "1", "1", "5", "1", "2", "Pharmacology" },
            };

            var list = new List<StudentIdentity>();
            for (int i = 0; i < TotalStudents; i++)
            {
                int year = int.Parse(core[i, 1]);
                // Enrollment year is consistent with year level for the 2026–27 term (flavor rule).
                int enrollYear = 2027 - year;
                var id = new StudentIdentity
                {
                    FullName = core[i, 0],
                    IdNumber = "HB-" + enrollYear + "-" + (101 + i * 37).ToString("D4"),
                    YearLevel = year,
                    CourseIndex = int.Parse(core[i, 2]),
                    IsTransferee = core[i, 3] == "1",
                    Portrait = new Portrait
                    {
                        HairStyle = int.Parse(core[i, 4]),
                        FaceVariant = int.Parse(core[i, 5]),
                        Accessory = int.Parse(core[i, 6]),
                        Distorted = false,
                    },
                    FirstClassroom = core[i, 7] == "1" ? RoomId.Classroom1 : RoomId.Classroom2,
                    FirstClassSubject = core[i, 8],
                    Greeting = Greetings[i % Greetings.Length],
                };
                list.Add(id);
            }
            return list;
        }

        public static readonly string[] Greetings =
        {
            "Good evening. Night class.",
            "Hi… I'm running a bit late.",
            "Evening! Cold out there.",
            "Let me in quick, please.",
            "Good evening, officer… I mean, custodian.",
            "Here's my ID.",
            "Evening. Same routine as always.",
        };

        public static readonly string[] AnomalyGreetings =
        {
            "Good evening. I am a student of this school.",
            "…Evening.",
            "Let me through. I belong here.",
            "My documents are in order.",
            "You will let me in tonight.",
            "I attend classes here. Everyone does.",
        };

        public static readonly string[] AdminLines =
        {
            "Custodian. I'm the district administrator. I'll be taking control of this facility tonight.",
            "My credentials are in order. Badge and roster memo, as your instructions require.",
            "There have been… complaints about your performance. Let's not add to them.",
            "Open the checkpoint. That is an order from the district.",
        };
    }
}
