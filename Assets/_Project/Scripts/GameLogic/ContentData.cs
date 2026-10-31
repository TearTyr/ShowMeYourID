using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
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

        public Portrait Swapped() => new Portrait
        {
            HairStyle = (HairStyle + 3) % 6,
            FaceVariant = (FaceVariant + 2) % 6,
            Accessory = (Accessory + 1) % 4,
            Distorted = Distorted,
        };

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

    [Serializable]
    public class StudentIdentity
    {
        public string FullName;
        public string IdNumber;        // registry ID, always valid format: HB-YYYY-NNNN
        public int YearLevel;          // 1..3
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

    [Serializable]
    public struct Answer
    {
        public string Text;
        public ToneTag Tone;
        public bool ContradictsDocuments;
    }

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
        // the paper slip (ticket) the student carries with the ID: checked against the card
        // and the file. On legitimate visitors it always agrees (fairness invariant).
        public string SlipName;
        public string SlipIdNumber;
        public string SlipReason = "NIGHT CLASS — LATE LIST";
        public Dictionary<QuestionId, Answer> Answers = new Dictionary<QuestionId, Answer>();
        public bool BehaviorGlitchVisible;    // rare visual micro-glitch hint for BehaviorOddity
        // Canon roster position of the person this visitor CLAIMS to be (-1 = none, e.g. the
        // administrator). Set once by RosterFactory; drives the temporary face asset so the same
        // student always wears the same face on the card and at the window. Old saves load -1
        // and fall back to the procedural portraits.
        public int FaceAssetIndex = -1;
        public bool OnLateList = true;        // office rule: on tonight's late list

        public bool PhotoMatches => WindowPortrait == IdPhoto;

        /// <summary>Do the two faces AS DISPLAYED agree — portrait features AND the anomaly
        /// distortion overlay (the administrator's glitched window face is her photo tell, and
        /// Portrait equality ignores Distorted on purpose for the scoring rules). This is the
        /// truth the desk's face-comparison cue shows; it never contradicts the rendered sprites.
        /// Informational only: verdicts stay with InspectionRules.</summary>
        public bool DisplayedFacesMatch =>
            PhotoMatches && !IsAnomaly && WindowPortrait.Distorted == IdPhoto.Distorted;

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
                    case DefectType.SlipMismatch: lines.Add("The paper slip (" + SlipName + " / " + SlipIdNumber + ") does not match the ID card."); break;
                }
            }
            if (lines.Count == 0) lines.Add("No discrepancies — documents were consistent.");
            return lines;
        }
    }

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

    [Serializable]
    public class ShiftDef
    {
        public int Number;           // 1..5
        public string DateLabel;     // "Mon · Nov 2"
        public string LongDateLabel  // "MONDAY - NOVEMBER 2" (header board form)
        {
            get
            {
                int i = System.Array.IndexOf(Content.ShiftDates, DateLabel);
                return i < 0 || i >= Content.ShiftDatesLong.Length
                    ? (DateLabel ?? "") : Content.ShiftDatesLong[i];
            }
        }
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

    [Serializable]
    public class RuleMutationDef
    {
        public string Key;
        public string RuleText;       // printed on the paper note while active

        public RuleMutationDef(string key, string text) { Key = key; RuleText = text; }

        public bool ForcesReject(VisitorData v)
        {
            switch (Key)
            {
                case "no_transferees": return !v.IsAnomaly && v.IdIsTransferee;
                case "strict_expiry": return !v.IsAnomaly && v.ExpirationDay - Content.ShiftDayNumber(v.ShiftNumber) <= 60;
                case "odd_serials": return !v.IsAnomaly && IdRules.SerialOf(v.IdNumber) % 2 == 1;
                case "office_late_list": return !v.IsAnomaly &&
                    (!v.OnLateList || IdRules.IsExpired(v, v.ShiftNumber));
                default: return false;
            }
        }
    }

    public static class Content
    {
        public const int TotalStudents = 14;          // 8 core + 6 variations (approved asset plan)
        public const int MaxQuestionsPerInspection = 2; // conflict C1 resolution: §2.3 + checklists say 1–2

        public static readonly string[] Courses =
        {
            "BS Information Technology", "BS Accountancy", "AB Communication", "BS Nursing",
            "BS Criminology", "BS Psychology", "BS Biology", "BA Multimedia Arts",
        };

        public static readonly string[] YearNames = { "1st Year", "2nd Year", "3rd Year" };

        public static readonly string[] ShiftDates =
        {
            "Mon · Nov 2", "Tue · Nov 3", "Wed · Nov 4", "Thu · Nov 5", "Fri · Nov 6",
        };

        // The header board spells the night out in full (the reference header line):
        // "NIGHT 1 - MONDAY - NOVEMBER 2". ShiftDates stays the short form for papers.
        public static readonly string[] ShiftDatesLong =
        {
            "MONDAY - NOVEMBER 2", "TUESDAY - NOVEMBER 3", "WEDNESDAY - NOVEMBER 4",
            "THURSDAY - NOVEMBER 5", "FRIDAY - NOVEMBER 6",
        };

        public static int ShiftDayNumber(int shift) => 1000 + shift; // arbitrary monotonic base

        public static int DayNumberOfDate(int year, int month, int day)
        {
            int y = year - 2026, m = month, d = day;
            return 1001 + (y * 360 + (m - 11) * 30 + (d - 2));
        }

        public static string DisplayDate(int year, int month, int day) =>
            year.ToString("D4") + "-" + month.ToString("D2") + "-" + day.ToString("D2");

        public static List<StudentIdentity> BuildStudentRoster()
        {
            string[,] core =
            {
                { "Ana Delgado",     "3", "0", "0", "0", "0", "1", "1", "Data Structures" },
                { "Marco Reyes",     "2", "4", "0", "1", "1", "0", "2", "Criminal Law" },
                { "Liza Santos",     "1", "1", "0", "2", "2", "1", "1", "Taxation Review" },
                { "Jerome Cruz",     "1", "7", "0", "3", "3", "3", "2", "Digital Sketching" },
                { "Bea Villanueva",  "3", "3", "1", "4", "4", "2", "1", "Anatomy" },
                { "Carlo Mendoza",   "2", "0", "0", "5", "5", "0", "2", "Networking" },
                { "Trisha Ramos",    "3", "2", "0", "0", "3", "2", "1", "Media Ethics" },
                { "Miguel Bautista", "1", "5", "1", "1", "4", "0", "2", "Intro Psychology" },
                { "Grace Lim",       "3", "6", "0", "2", "5", "1", "1", "Genetics" },
                { "Paolo Soriano",   "2", "1", "1", "3", "0", "0", "2", "Bookkeeping" },
                { "Kim Alonzo",      "2", "4", "0", "4", "1", "3", "1", "Forensics" },
                { "Rica Fernandez",  "1", "2", "0", "5", "2", "2", "2", "Public Speaking" },
                { "Diego Navarro",   "3", "5", "0", "0", "4", "0", "1", "Abnormal Psych" },
                { "Faith Gonzales",  "2", "3", "1", "1", "5", "1", "2", "Pharmacology" },
            };

            var list = new List<StudentIdentity>();
            for (int i = 0; i < TotalStudents; i++)
            {
                int year = int.Parse(core[i, 1]);
                int enrollYear = RecordSearch.CurrentSchoolYear - year;
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
