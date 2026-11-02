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
        public string IdNumber;        // registry ID, always valid format: HB-YYYY-NNN
        public int YearLevel;          // 1..4
        public int CourseIndex;        // index into Content.Courses
        public bool IsTransferee;
        public Portrait Portrait;
        public RoomId FirstClassroom;  // Room 101 (Classroom1) / Room 202 (Classroom2)
        public string FirstClassSubject;
        public string Greeting;        // flavor line at the window
        public AwkwardSpeech Awkward;  // how an anomaly wearing this student talks
        public string ScaredGreeting;  // what they say when they arrive frightened (student or anomaly alike)

        public bool IsFaculty;         // a professor: the year reads Professor, the status Faculty
        public string StatusString => Content.StatusName(IsTransferee, IsFaculty);
        public string YearString => Content.YearNames[YearLevel - 1];
        public string CourseString => Content.Courses[CourseIndex];
        // as printed on the file: "Room 101 · Data Structures"
        public string ClassroomString => Content.RoomName(FirstClassroom) + " · " + FirstClassSubject;
        // as the student says it at the window: "Room 101, Data Structures."
        public string ClassroomSpeech => Content.Speech(FirstClassroom, FirstClassSubject);
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
        public bool IsFaculty;                // a professor: Faculty on the card and the file alike
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
        public string SlipReason = "NIGHT CLASS - LATE LIST";
        // What the visitor answers to "Which classroom are you going to?". It matches the
        // file (RecordFirstClassroom) unless the visitor carries the ClassroomMismatch defect.
        public string ClassroomAnswer = "…";
        // What they say to each question (index = QuestionId). The card is what they claim, so an
        // honest answer repeats the card; a SpokenMismatch visitor lies about SpokenLieQuestion.
        public string[] Answers = new string[InterviewRules.Count];
        public int SpokenLieQuestion = -1;
        public bool SpeaksAwkwardly;          // this anomaly got its student's awkward lines
        public bool IsScared;                 // arrives frightened: the scared face and scared words, student or anomaly
        // Which student's face is shown at the window (a PhotoMismatch shows somebody else's).
        public int WindowFaceIndex = -1;
        // Canon roster position of the person this visitor CLAIMS to be (-1 = none, e.g. the
        // administrator). Set once by RosterFactory; drives the temporary face asset so the same
        // student always wears the same face on the card and at the window. Old saves load -1
        // and fall back to the procedural portraits.
        public int FaceAssetIndex = -1;

        public bool PhotoMatches => WindowPortrait == IdPhoto;

        /// <summary>Do the two faces AS DISPLAYED agree - portrait features AND the anomaly
        /// distortion overlay (the administrator's glitched window face is his photo tell, and
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
                    case DefectType.IdFormatInvalid: lines.Add("ID number '" + IdNumber + "' is not in HB-YYYY-NNN format."); break;
                    case DefectType.YearMismatch: lines.Add("Year level: ID says " + Content.YearNames[IdYearLevel - 1] + ", the file says " + Content.YearNames[RecordYearLevel - 1] + "."); break;
                    case DefectType.CourseMismatch: lines.Add("Course: ID says " + Content.Courses[IdCourseIndex] + ", the file says " + Content.Courses[RecordCourseIndex] + "."); break;
                    case DefectType.StatusMismatch: lines.Add("Status: ID says " + Content.StatusName(IdIsTransferee, IsFaculty) + ", the file says " + Content.StatusName(RecordIsTransferee, IsFaculty) + "."); break;
                    case DefectType.RecordNameMismatch: lines.Add("The ID says '" + ClaimedName + "' but the file for this ID number says '" + RecordName + "'."); break;
                    case DefectType.NotInDatabase: lines.Add("ID number not found in the student database."); break;
                    case DefectType.DuplicateId: lines.Add("ID number already used by a visitor from " + DuplicateOfShift + "."); break;
                    case DefectType.SpokenMismatch: lines.Add("Said '" + (SpokenLieQuestion >= 0 ? Answers[SpokenLieQuestion] : "") + "' when asked about their " + (SpokenLieQuestion >= 0 ? InterviewRules.Tag((QuestionId)SpokenLieQuestion).ToLowerInvariant() : "card") + ", which is not what the ID says."); break;
                    case DefectType.ClassroomMismatch: lines.Add("Said they were going to " + ClassroomAnswer.TrimEnd('.') + ", but the file says " + RecordFirstClassroom + "."); break;
                    case DefectType.SlipMismatch: lines.Add("The paper slip (" + SlipName + " / " + SlipIdNumber + ") does not match the ID card."); break;
                }
            }
            if (lines.Count == 0) lines.Add("No discrepancies - documents were consistent.");
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
        public int Number;           // set by ShiftPlan from the row's place in the table
        public string DateLabel;     // "Mon · Nov 2"
        public string LongDateLabel; // "MONDAY - NOVEMBER 2" (header board form)
        public int VisitorCount;     // how many come to the window
        public int AnomalyCount;     // how many of them are not students
        public int ScaredCount;      // how many of them arrive frightened (some students, some anomalies)
        public bool HasAdministrator;
        public string Title;
        public string Objective;
        public string RuleNoteAddendum;  // new paper-note rule introduced this shift
        public string IntroWarning;      // an extra line from "???" in the night's intro (optional)
        public int MaxDefectsPerAnomaly; // difficulty escalation
        public bool SubtleDefectsAllowed;
        public bool DuplicateIdsAllowed;
        public bool ClassroomLieAllowed;
        public bool SlipMismatchAllowed; // the paper slip can disagree with the card
        public bool NearExpiryAllowed;   // real students' IDs can be close to running out
        public bool ExplorationFocus;    // this night nudges investigation

        /// <summary>Its own copy, so nothing can change the table by accident.</summary>
        public ShiftDef Copy() => (ShiftDef)MemberwiseClone();
    }

    public static class Content
    {
        public static int TotalStudents => GameRules.Current.Students.Length;   // one per illustrated character

        /// <summary>"Room 101" / "Room 202" - the two classrooms of the school.</summary>
        public static string RoomName(RoomId room) => room == RoomId.Classroom1 ? "Room 101" : "Room 202";

        /// <summary>How a student says where they are going: "Room 101, Data Structures."</summary>
        public static string Speech(RoomId room, string subject) => RoomName(room) + ", " + subject + ".";

        public static readonly string[] Courses =
        {
            "BS Information Technology", "BS Computer Science", "BS Entertainment Multimedia Computing",
        };

        // year level 1 to 4 are the students, 5 is a professor
        public static readonly string[] YearNames = { "Freshman", "Sophomore", "Junior", "Senior", "Professor" };

        /// <summary>How a status reads on the card and the file: Faculty, Transferee or Regular.</summary>
        public static string StatusName(bool transferee, bool faculty)
        {
            if (faculty) return "Faculty";
            return transferee ? "Transferee" : "Regular";
        }

        /// <summary>The same status as a person says it: "faculty member", "transferee", "regular student".</summary>
        public static string StatusWord(bool transferee, bool faculty)
        {
            if (faculty) return "faculty member";
            return transferee ? "transferee" : "regular student";
        }

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
            var list = new List<StudentIdentity>();
            foreach (var row in GameRules.Current.Students)
            {
                list.Add(new StudentIdentity
                {
                    FullName = row.FullName,
                    IdNumber = row.IdNumber,
                    YearLevel = row.YearLevel,
                    CourseIndex = row.CourseIndex,
                    IsTransferee = row.IsTransferee,
                    IsFaculty = row.IsFaculty,
                    Portrait = new Portrait
                    {
                        HairStyle = row.HairStyle, FaceVariant = row.FaceVariant, Accessory = row.Accessory,
                        Distorted = false,
                    },
                    FirstClassroom = row.FirstClassroom,
                    FirstClassSubject = row.FirstClassSubject,
                    Greeting = row.Greeting,
                    Awkward = row.Awkward,
                    ScaredGreeting = row.ScaredGreeting,
                });
            }
            return list;
        }

        /// <summary>How EVERY scared visitor talks, real student or not: the same shaken words, so the
        /// way they speak never gives an anomaly away. The papers and the file still decide.</summary>
        public static readonly AwkwardSpeech ScaredSpeech = new AwkwardSpeech
        {
            Greeting = "P-please. Let me in. Something is out there.",
            Course = "{0}. Please, can I go in now?",
            Year = "{0}. Please, I just want to go inside.",
            Status = "A {0}. Please, it is not safe out here.",
            Classroom = "{0}. Please, can I go there? Quickly?",
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
            "Open the checkpoint. That is an order from the district.",
        };

        // Night 6: he is back, and he is polite about it.
        public static readonly string[] Night6AdminLines =
        {
            "Custodian. You remember me. I told you I do not give up.",
            "The district sent me back with the same papers and a better offer. Make the right decision this time.",
            "Open the checkpoint.",
        };
    }
}
