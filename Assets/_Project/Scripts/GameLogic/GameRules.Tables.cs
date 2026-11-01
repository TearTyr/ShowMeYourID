using System;

namespace Hollowbrook.Logic
{
    /// <summary>One student: a row of the Students table.</summary>
    [Serializable]
    public class StudentRow
    {
        public string FullName;
        public string IdNumber;         // always valid: HB-YYYY-NNNN
        public int YearLevel;           // 1..4
        public int CourseIndex;         // index into Content.Courses
        public bool IsTransferee;
        public RoomId FirstClassroom;
        public string FirstClassSubject;
        public string Greeting;         // what the real student says at the window
        public int HairStyle, FaceVariant, Accessory;   // only for the code-drawn portrait if the art is missing
        public AwkwardSpeech Awkward;   // how the thing wearing this student speaks
    }

    /// <summary>How an anomaly wearing a student talks when it gets the words wrong. The facts are
    /// the same as normal speech (so the papers still decide); only the sound is off. {0} is the
    /// fact: the course, the year, "regular student" or "transferee", the room and class.</summary>
    [Serializable]
    public class AwkwardSpeech
    {
        public string Greeting;
        public string Course;
        public string Year;
        public string Status;
        public string Classroom;
    }

    public partial class GameRules
    {
        private static ShiftDef[] DefaultNights() => new[]
        {
            new ShiftDef
            {
                DateLabel = "Mon · Nov 2", LongDateLabel = "MONDAY - NOVEMBER 2",
                VisitorCount = 4, AnomalyCount = 2,
                Title = "Introduction",
                Objective = "Learn the checkpoint: inspect IDs, pull the student file, ask a question or two, and stamp Allow or Reject. Take a walk - learn how to leave the security room and come back.",
                RuleNoteAddendum = "NIGHT 1 - Welcome, custodian. Verify: ID number format (HB-YYYY-NNNN), photo vs. the person at the window, expiration date, year level, course, and Regular/Transferee status against the student file in the drawer. Your questions per student are limited. Not everyone tonight is a student.",
                MaxDefectsPerAnomaly = 1,
            },
            new ShiftDef
            {
                DateLabel = "Tue · Nov 3", LongDateLabel = "TUESDAY - NOVEMBER 3",
                VisitorCount = 6, AnomalyCount = 3,
                Title = "Initial Inconsistencies",
                Objective = "Small contradictions are appearing in documents, faces, and answers. Not everyone at the window is a student. The hallway is open between visitors - look around.",
                RuleNoteAddendum = "NIGHT 2 - District reminder: an expired ID must never be accepted, whatever the excuse. If documents disagree, the person does not enter. A student must name the room and class their file says.",
                MaxDefectsPerAnomaly = 1, ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
            },
            new ShiftDef
            {
                DateLabel = "Wed · Nov 4", LongDateLabel = "WEDNESDAY - NOVEMBER 4",
                VisitorCount = 7, AnomalyCount = 4,
                Title = "Increasing Anomalies",
                Objective = "Cases are harder tonight - subtle photo differences and recycled identities. Pull the floor files and cross-check everything twice.",
                RuleNoteAddendum = "NIGHT 3 - WARNING: duplicate ID numbers have been reported at other district sites. A valid ID already presented tonight with a different card is grounds for immediate rejection.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
            },
            new ShiftDef
            {
                DateLabel = "Thu · Nov 5", LongDateLabel = "THURSDAY - NOVEMBER 5",
                VisitorCount = 8, AnomalyCount = 5,
                Title = "Investigation",
                Objective = "Something happened in this school. Between visitors, investigate the hallway and both classrooms. Whatever the previous custodians found - find it too.",
                RuleNoteAddendum = "NIGHT 4 - The district has scheduled this facility for final closure. Custodial staff are instructed to document anything unusual found inside the building. Investigation is now part of your duties.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true, ExplorationFocus = true,
            },
            new ShiftDef
            {
                DateLabel = "Fri · Nov 6", LongDateLabel = "FRIDAY - NOVEMBER 6",
                VisitorCount = 0, AnomalyCount = 0, HasAdministrator = true,   // nobody else comes tonight
                Title = "Final Shift",
                Objective = "Last night. No students tonight: only a district administrator, here to take control of the school. Check his credentials like anyone else's.",
                RuleNoteAddendum = "NIGHT 5 - District personnel present a STAFF BADGE, not a student ID. The badge name and number must match the attached District Roster Memo. If they do not match, deny entry. No exceptions. No overrides by phone.",
                IntroWarning = "Tonight the district sends an administrator to 'take control'. Check the credentials like you would check anyone else's. Trust the evidence.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
            },
        };

        private static ShiftDef DefaultChallengeNight() => new ShiftDef
        {
            DateLabel = "Sat · Nov 7", LongDateLabel = "SATURDAY - NOVEMBER 7",
            VisitorCount = 0, AnomalyCount = 0, HasAdministrator = true,   // nobody else comes
            Title = "He Does Not Give Up",
            Objective = "The administrator is back. Same questions, new answers. Deny him again and survive until dawn: he is faster and more aggressive than ever.",
            RuleNoteAddendum = "NIGHT 6 - District personnel present a STAFF BADGE. Badge and memo must match. If they do not, deny entry. He will ask you to make the right decision. You already know what it is.",
            IntroWarning = "He said he would be back. He does not give up. Deny him again, and this time he will not be gentle. Only the light stops him.",
            MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
            ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
        };

        private static StudentRow[] DefaultStudents() => new[]
        {
            new StudentRow
            {
                FullName = "Carlo Mendoza", IdNumber = "HB-2024-0101", YearLevel = 3, CourseIndex = 0,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Networking",
                Greeting = "Good evening. Night class.",
                HairStyle = 0, FaceVariant = 0, Accessory = 1,
                Awkward = new AwkwardSpeech   // too many words
                {
                    Greeting = "Good evening to you and to the evening.",
                    Course = "The course that I am taking is the course of {0}.",
                    Year = "The year that I am in is the year of {0}.",
                    Status = "The kind of student that I am is the kind that is {0}.",
                    Classroom = "The room that I am going to is the room of {0}.",
                },
            },
            new StudentRow
            {
                FullName = "Nathaniel Aquino", IdNumber = "HB-2026-0153", YearLevel = 1, CourseIndex = 0, IsTransferee = true,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Programming Basics",
                Greeting = "Hi… I'm running a bit late.",
                HairStyle = 1, FaceVariant = 1, Accessory = 0,
                Awkward = new AwkwardSpeech   // says the right thing about the wrong feeling
                {
                    Greeting = "Hello. I am late. I enjoy being late. Do you enjoy it.",
                    Course = "{0}. I enjoy it. I enjoy having it.",
                    Year = "{0}. I enjoy it. It is a good amount of year.",
                    Status = "I am a {0}. I enjoy it. Everyone enjoys it.",
                    Classroom = "{0}. I enjoy that room. I enjoy the walls.",
                },
            },
            new StudentRow
            {
                FullName = "Maria Clara Santos", IdNumber = "HB-2023-0143", YearLevel = 4, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Algorithms",
                Greeting = "Evening! Cold out there.",
                HairStyle = 2, FaceVariant = 2, Accessory = 1,
                Awkward = new AwkwardSpeech   // talks about herself as somebody else
                {
                    Greeting = "Evening. The girl is cold. The girl has come inside.",
                    Course = "The girl is taking {0}.",
                    Year = "The girl is in {0}.",
                    Status = "The girl is a {0}. That is what the girl is.",
                    Classroom = "The girl goes to {0}. She has been told this.",
                },
            },
            new StudentRow
            {
                FullName = "Liza May Ramos", IdNumber = "HB-2024-0341", YearLevel = 3, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Data Structures",
                Greeting = "Let me in quick, please.",
                HairStyle = 3, FaceVariant = 3, Accessory = 3,
                Awkward = new AwkwardSpeech   // the words arrive in the wrong order
                {
                    Greeting = "Quick me let in. Please, the please.",
                    Course = "Taking am I, {0}, the course.",
                    Year = "In I am, {0}, the year.",
                    Status = "Student I am, a {0}, yes the student.",
                    Classroom = "Going I am, {0}, to there.",
                },
            },
            new StudentRow
            {
                FullName = "Raffy Isidro", IdNumber = "HB-2023-0129", YearLevel = 4, CourseIndex = 2,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Media Ethics",
                Greeting = "Good evening, officer… I mean, custodian.",
                HairStyle = 4, FaceVariant = 4, Accessory = 2,
                Awkward = new AwkwardSpeech   // says everything twice
                {
                    Greeting = "Good evening, custodian. Good evening, custodian.",
                    Course = "I'm taking {0}. I'm taking {0}.",
                    Year = "I'm in {0}. I'm in {0}.",
                    Status = "I'm a {0}. I'm a {0}.",
                    Classroom = "{0}. {0}.",
                },
            },
            new StudentRow
            {
                FullName = "Renielle Waterbonia", IdNumber = "HB-2025-0187", YearLevel = 2, CourseIndex = 1, IsTransferee = true,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Discrete Math",
                Greeting = "Here's my ID.",
                HairStyle = 5, FaceVariant = 5, Accessory = 0,
                Awkward = new AwkwardSpeech   // answers like a form being read out
                {
                    Greeting = "Here is my identification. Identification is here.",
                    Course = "Course, colon, {0}, end of course.",
                    Year = "Year, colon, {0}, end of year.",
                    Status = "Status, colon, {0}, end of status.",
                    Classroom = "Destination, colon, {0}, end of destination.",
                },
            },
            new StudentRow
            {
                FullName = "Rhevster Estrellita", IdNumber = "HB-2025-0053", YearLevel = 2, CourseIndex = 2,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Digital Sketching",
                Greeting = "Evening. Same routine as always.",
                HairStyle = 0, FaceVariant = 3, Accessory = 2,
                Awkward = new AwkwardSpeech   // keeps thanking you for things you did not do
                {
                    Greeting = "Evening. Thank you for the routine. Thank you for always.",
                    Course = "Thank you for asking me. {0}. Thank you for the course.",
                    Year = "Thank you for asking me. {0}. Thank you for the year.",
                    Status = "Thank you for asking me. I am a {0}. Thank you for me.",
                    Classroom = "Thank you for asking me. {0}. Thank you for the door.",
                },
            },
        };
    }
}
