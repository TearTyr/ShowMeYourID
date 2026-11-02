using System;

namespace Hollowbrook.Logic
{
    /// <summary>One student: a row of the Students table.</summary>
    [Serializable]
    public class StudentRow
    {
        public string FullName;
        public string IdNumber;         // always valid: HB-YYYY-NNN
        public int YearLevel;           // 1..4
        public int CourseIndex;         // index into Content.Courses
        public bool IsTransferee;
        public RoomId FirstClassroom;
        public string FirstClassSubject;
        public string Greeting;         // what the real student says at the window
        public int HairStyle, FaceVariant, Accessory;   // only for the code-drawn portrait if the art is missing
        public AwkwardSpeech Awkward;   // how the thing wearing this student speaks
        public string ScaredGreeting;   // what they say when they arrive frightened (student or anomaly)
        public bool IsFaculty;          // a professor: status prints Faculty, the year is Professor
        public bool Off;                // true = not in the school yet (its art is missing); DefaultStudents leaves it out
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
                VisitorCount = 4, AnomalyCount = 2, ScaredCount = 1,
                Title = "Introduction",
                Objective = "Learn the checkpoint: inspect IDs, pull the student file, ask a question or two, and stamp Allow or Reject. Take a walk - learn how to leave the security room and come back.",
                RuleNoteAddendum = "NIGHT 1 - Welcome, custodian. Verify: ID number format (HB-YYYY-NNN), photo vs. the person at the window, expiration date, year level, course, and Regular/Transferee status against the student file in the drawer. Your questions per student are limited. Not everyone tonight is a student. Some visitors arrive frightened: fear proves nothing, a student can be scared and so can the thing wearing their face. Trust the papers.",
                MaxDefectsPerAnomaly = 1,
                TutorialOpening = true,
            },
            new ShiftDef
            {
                DateLabel = "Tue · Nov 3", LongDateLabel = "TUESDAY - NOVEMBER 3",
                VisitorCount = 6, AnomalyCount = 3, ScaredCount = 2,
                Title = "Initial Inconsistencies",
                Objective = "Small contradictions are appearing in documents, faces, and answers. Not everyone at the window is a student. The hallway is open between visitors - look around.",
                RuleNoteAddendum = "NIGHT 2 - District reminder: an expired ID must never be accepted, whatever the excuse. If documents disagree, the person does not enter. A student must name the room and class their file says.",
                MaxDefectsPerAnomaly = 1, ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
            },
            new ShiftDef
            {
                DateLabel = "Wed · Nov 4", LongDateLabel = "WEDNESDAY - NOVEMBER 4",
                VisitorCount = 7, AnomalyCount = 4, ScaredCount = 2,
                Title = "Increasing Anomalies",
                Objective = "Cases are harder tonight - subtle photo differences and recycled identities. Pull the floor files and cross-check everything twice.",
                RuleNoteAddendum = "NIGHT 3 - WARNING: duplicate ID numbers have been reported at other district sites. A valid ID already presented tonight with a different card is grounds for immediate rejection.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
            },
            new ShiftDef
            {
                DateLabel = "Thu · Nov 5", LongDateLabel = "THURSDAY - NOVEMBER 5",
                VisitorCount = 8, AnomalyCount = 5, ScaredCount = 3,
                Title = "Investigation",
                Objective = "Something happened in this school. Between visitors, investigate the hallway and both classrooms. Whatever the previous custodians found - find it too.",
                RuleNoteAddendum = "NIGHT 4 - The district has scheduled this facility for final closure. Custodial staff are instructed to document anything unusual found inside the building. Investigation is now part of your duties.",
                MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
                ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true, ExplorationFocus = true,
            },
            new ShiftDef
            {
                DateLabel = "Fri · Nov 6", LongDateLabel = "FRIDAY - NOVEMBER 6",
                VisitorCount = 8, AnomalyCount = 5, ScaredCount = 3, HasAdministrator = true,   // eight students, then the administrator last
                Title = "Final Shift",
                Objective = "Last night. The students come first, then a district administrator arrives to take control of the school. Check everyone's credentials the same way, his included.",
                RuleNoteAddendum = "NIGHT 5 - Students still show a student ID: everything you learned this week still applies. The district administrator comes last and presents a STAFF BADGE, not a student ID. The badge name and number must match the attached District Roster Memo. If they do not match, deny entry. No exceptions. No overrides by phone.",
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

        private static ShiftDef DefaultLockdownNight() => new ShiftDef
        {
            DateLabel = "All at the gate", LongDateLabel = "ALL AT THE GATE",
            VisitorCount = 13, AnomalyCount = 6, ScaredCount = 13, Lockdown = true,   // every student, six of them not themselves, all frightened
            Title = "Lockdown",
            Objective = "Everyone is at the gate at once and the thing outside is closing in. Thirteen faces, thirty five seconds each. Three mistakes and the gate gives.",
            RuleNoteAddendum = "LOCKDOWN - The same checks as every night. Approve a student, decline anything else. When the time on a face runs out, the thing outside takes them. Let an anomaly in, turn a student away, or lose a student, and that is a mistake. Three and the gate falls.",
            IntroWarning = "There is no patrol tonight. Nobody is leaving this booth.",
            MaxDefectsPerAnomaly = 2, SubtleDefectsAllowed = true, DuplicateIdsAllowed = true,
            ClassroomLieAllowed = true, SlipMismatchAllowed = true, NearExpiryAllowed = true,
        };

        // The ones marked Off (art still missing) are dropped here, so turning one on is one word.
        private static StudentRow[] DefaultStudents() => Array.FindAll(AllStudents(), s => !s.Off);

        private static StudentRow[] AllStudents() => new[]
        {
            new StudentRow
            {
                FullName = "Mark Angelo Mendoza", IdNumber = "HB-2024-101", YearLevel = 3, CourseIndex = 0,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Networking",
                Greeting = "Good evening. Night class.",
                ScaredGreeting = "P-please. Is it safe to come in? Something was behind me.",
                HairStyle = 0, FaceVariant = 0, Accessory = 1,
                Awkward = new AwkwardSpeech   // too many words
                {
                    Greeting = "Good evening to you and to the evening.",
                    Course = "The course that I am taking is the course of {0}.",
                    Year = "The year that I am is the year that is called {0}.",
                    Status = "The kind of student that I am is the kind that is {0}.",
                    Classroom = "The room that I am going to is the room of {0}.",
                },
            },
            new StudentRow
            {
                FullName = "Nathaniel Aquino", IdNumber = "HB-2026-153", YearLevel = 1, CourseIndex = 0, IsTransferee = true,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Programming Basics",
                Greeting = "Hi… I'm running a bit late.",
                ScaredGreeting = "H-hi... did you hear that? It followed me from the gate.",
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
                FullName = "Maria Clara Santos", IdNumber = "HB-2023-183", YearLevel = 4, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Algorithms",
                Greeting = "Evening! Cold out there.",
                ScaredGreeting = "Please, let me in. Something was walking the hall behind me.",
                HairStyle = 2, FaceVariant = 2, Accessory = 1,
                Awkward = new AwkwardSpeech   // talks about herself as somebody else
                {
                    Greeting = "Evening. The girl is cold. The girl has come inside.",
                    Course = "The girl is taking {0}.",
                    Year = "The girl is a {0}.",
                    Status = "The girl is a {0}. That is what the girl is.",
                    Classroom = "The girl goes to {0}. She has been told this.",
                },
            },
            new StudentRow
            {
                FullName = "Liza May Ramos", IdNumber = "HB-2024-341", YearLevel = 3, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Data Structures",
                Greeting = "Let me in quick, please.",
                ScaredGreeting = "Quick, please, quick. Don't look at the window. Let me in.",
                HairStyle = 3, FaceVariant = 3, Accessory = 3,
                Awkward = new AwkwardSpeech   // the words arrive in the wrong order
                {
                    Greeting = "Quick me let in. Please, the please.",
                    Course = "Taking am I, {0}, the course.",
                    Year = "A {0} I am, the year, yes.",
                    Status = "Student I am, a {0}, yes the student.",
                    Classroom = "Going I am, {0}, to there.",
                },
            },
            new StudentRow
            {
                FullName = "Raffy Isedru", IdNumber = "HB-2001-129", YearLevel = 5, CourseIndex = 2, IsFaculty = true,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Media Ethics",
                Greeting = "Good evening, officer… I mean, custodian.",
                ScaredGreeting = "Officer... custodian... there is someone out there. Please.",
                HairStyle = 4, FaceVariant = 4, Accessory = 2,
                Awkward = new AwkwardSpeech   // says everything twice
                {
                    Greeting = "Good evening, custodian. Good evening, custodian.",
                    Course = "I'm taking {0}. I'm taking {0}.",
                    Year = "I'm a {0}. I'm a {0}.",
                    Status = "I'm a {0}. I'm a {0}.",
                    Classroom = "{0}. {0}.",
                },
            },
            new StudentRow
            {
                FullName = "Ranielle Waterbonia", IdNumber = "HB-2023-667", YearLevel = 4, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Discrete Math",
                Greeting = "Here's my ID.",
                ScaredGreeting = "Here is my ID. Please, please just take it and let me in.",
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
                FullName = "Rhevster Estrellita", IdNumber = "HB-2025-053", YearLevel = 2, CourseIndex = 2,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Digital Sketching",
                Greeting = "Evening. Same routine as always.",
                ScaredGreeting = "Evening. N-not the same routine tonight. Something is wrong.",
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
            new StudentRow
            {
                FullName = "Paolo Garcia", IdNumber = "HB-2024-134", YearLevel = 3, CourseIndex = 1, IsTransferee = true,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Cybersecurity Basics",
                Greeting = "Evening. Long shift tonight?",
                ScaredGreeting = "Evening. Did the lights just go out for you too?",
                HairStyle = 1, FaceVariant = 2, Accessory = 0,
                Awkward = new AwkwardSpeech   // answers everything with a question
                {
                    Greeting = "Is it evening? It is evening, is it not?",
                    Course = "Am I taking {0}? I think I am taking {0}?",
                    Year = "Am I a {0}? Is that what you are asking?",
                    Status = "Am I a {0}? Would you say I am?",
                    Classroom = "Am I going to {0}? Is that where I go?",
                },
            },
            new StudentRow
            {
                FullName = "Angelica Cruz", IdNumber = "HB-2023-190", YearLevel = 4, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Data Visualization",
                Greeting = "Hello! I stayed late to finish a poster.",
                ScaredGreeting = "H-hello. I stayed too late. I should not have stayed.",
                HairStyle = 2, FaceVariant = 4, Accessory = 1,
                Awkward = new AwkwardSpeech   // speaks as if it already happened
                {
                    Greeting = "Hello. I stayed late. I was always staying late.",
                    Course = "I was taking {0}. I have always been taking it.",
                    Year = "I was a {0}. It was a long time ago.",
                    Status = "I was a {0}. I was that for a while.",
                    Classroom = "I was going to {0}. I had gone there.",
                },
            },
            new StudentRow
            {
                FullName = "Mark Anthony Villanueva", IdNumber = "HB-2026-167", YearLevel = 1, CourseIndex = 0, IsTransferee = true,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Intro to Computing",
                Greeting = "Hey, sorry I'm so late.",
                ScaredGreeting = "Sorry, sorry. Can I come in? I do not want to be out there.",
                HairStyle = 3, FaceVariant = 1, Accessory = 2,
                Awkward = new AwkwardSpeech   // apologises for everything
                {
                    Greeting = "Sorry. Sorry I am here. Sorry for the evening.",
                    Course = "Sorry. {0}. Sorry for the course.",
                    Year = "Sorry. {0}. Sorry for the year.",
                    Status = "Sorry. A {0}. Sorry for being one.",
                    Classroom = "Sorry. {0}. Sorry for the room.",
                },
            },
            new StudentRow
            {
                FullName = "Bea Dela Cruz", IdNumber = "HB-2026-158", YearLevel = 1, CourseIndex = 0, IsTransferee = true,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Web Fundamentals",
                Greeting = "Hi. Is this the right gate?",
                ScaredGreeting = "Hi. Is this the right gate? Please say it is the right gate.",
                HairStyle = 4, FaceVariant = 0, Accessory = 0,
                Awkward = new AwkwardSpeech   // far too slow, with long stops
                {
                    Greeting = "Hi. . . . This. . . is. . . the gate. . .",
                    Course = "I. . . am taking. . . {0}. . . yes.",
                    Year = "I. . . am a. . . {0}. . . yes.",
                    Status = "I. . . am a. . . {0}. . . yes.",
                    Classroom = "I. . . am going to. . . {0}. . . yes.",
                },
            },
            new StudentRow
            {
                FullName = "Giones Acirfa", IdNumber = "HB-2025-143", YearLevel = 2, CourseIndex = 0, IsTransferee = true,
                FirstClassroom = RoomId.Classroom1, FirstClassSubject = "Systems Administration",
                Greeting = "Good evening. Rehearsal ran long.",
                ScaredGreeting = "Good evening. The rehearsal room was empty and something answered.",
                HairStyle = 5, FaceVariant = 2, Accessory = 3,
                Awkward = new AwkwardSpeech   // reads it off a wall
                {
                    Greeting = "It is written: good evening. It is written: rehearsal ran long.",
                    Course = "It is written that I take {0}.",
                    Year = "It is written that I am a {0}.",
                    Status = "It is written that I am a {0}.",
                    Classroom = "It is written that I go to {0}.",
                },
            },
            new StudentRow
            {
                FullName = "Kristine Joy Bautista", IdNumber = "HB-2024-342", YearLevel = 3, CourseIndex = 1,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Probability",
                Greeting = "Hi, I'm only here for my notes.",
                ScaredGreeting = "I only came for my notes. I want to go home now. Please.",
                HairStyle = 0, FaceVariant = 5, Accessory = 1,
                Awkward = new AwkwardSpeech   // counts down after every answer
                {
                    Greeting = "Hi. I am only here for the notes. Three. Two. One.",
                    Course = "{0}. Three. Two. One.",
                    Year = "{0}. Three. Two. One.",
                    Status = "A {0}. Three. Two. One.",
                    Classroom = "{0}. Three. Two. One.",
                },
            },
            new StudentRow   // Off: Art/Characters/Izzy_Neckerman only holds copies of Anthony's pictures
            {
                FullName = "Izzy Neckerman", IdNumber = "HB-2025-156", YearLevel = 2, CourseIndex = 1, Off = true,
                FirstClassroom = RoomId.Classroom2, FirstClassSubject = "Software Engineering",
                Greeting = "Evening. Capstone defence is tomorrow.",
                ScaredGreeting = "Evening. Capstone defence tomorrow. I cannot be here. Please.",
                HairStyle = 2, FaceVariant = 3, Accessory = 0,
                Awkward = new AwkwardSpeech   // says the same sentence with one word swapped each time
                {
                    Greeting = "Evening. The defence is tomorrow. The tomorrow is defence.",
                    Course = "I take {0}. {0} takes me.",
                    Year = "I am a {0}. A {0} is in me.",
                    Status = "I am a {0}. A {0} is me.",
                    Classroom = "I go to {0}. {0} goes to me.",
                },
            },
        };
    }
}
