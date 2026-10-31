namespace Hollowbrook.Logic
{
    // The four things the custodian can ask at the window. Each answer is worth something only
    // when it is compared with the card or the student's file, and a student may be asked only
    // Budget of them, so choosing which to ask is part of the job.
    public enum QuestionId { Course = 0, Year = 1, Status = 2, Classroom = 3 }

    public static class InterviewRules
    {
        public const int Count = 4;
        public const int Budget = 2;   // questions per student

        public static string Button(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "YOUR COURSE?";
                case QuestionId.Year: return "YOUR YEAR?";
                case QuestionId.Status: return "REGULAR OR TRANSFEREE?";
                default: return "FIRST CLASSROOM?";
            }
        }

        /// <summary>The short label the button wears once it has been answered.</summary>
        public static string Tag(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "COURSE";
                case QuestionId.Year: return "YEAR";
                case QuestionId.Status: return "STATUS";
                default: return "ROOM";
            }
        }

        public static string Spoken(QuestionId q)
        {
            switch (q)
            {
                case QuestionId.Course: return "What is your course?";
                case QuestionId.Year: return "What year are you in?";
                case QuestionId.Status: return "Are you a regular student or a transferee?";
                default: return "Which classroom are you going to?";
            }
        }

        // Year needs a look at the back of the ID, and the status needs their file, so those two
        // stay locked until the player has done that for this student.
        public static bool Unlocked(QuestionId q, bool idFlipped, bool fileOpened)
        {
            if (q == QuestionId.Year) return idFlipped;
            if (q == QuestionId.Status) return fileOpened;
            return true;
        }

        public static string LockedHint(QuestionId q)
        {
            if (q == QuestionId.Year) return "FLIP THE ID FIRST (F)";
            if (q == QuestionId.Status) return "OPEN THEIR FILE FIRST";
            return "";
        }

        /// <summary>The two or three words shown on a locked button.</summary>
        public static string LockedTag(QuestionId q) => q == QuestionId.Year ? "FLIP ID" : q == QuestionId.Status ? "FILE" : "";

        public static int Bit(QuestionId q) => 1 << (int)q;

        public static bool Asked(int mask, QuestionId q) => (mask & Bit(q)) != 0;

        public static int AskedCount(int mask)
        {
            int n = 0;
            for (int i = 0; i < Count; i++) if ((mask & (1 << i)) != 0) n++;
            return n;
        }

        public static int Remaining(int mask) => System.Math.Max(0, Budget - AskedCount(mask));

        /// <summary>A question already asked can be heard again for free; a new one needs budget.</summary>
        public static bool CanAsk(int mask, QuestionId q) => Asked(mask, q) || Remaining(mask) > 0;

        // What a student says. The honest answer is simply what their card says.
        public static string CourseSpeech(string course) => "I'm taking " + course + ".";
        public static string YearSpeech(string year) => "I'm in " + year + ".";
        public static string StatusSpeech(bool transferee) => transferee ? "I'm a transferee." : "I'm a regular student.";
    }
}
