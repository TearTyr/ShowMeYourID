namespace Hollowbrook.Logic
{
    /// <summary>Which face art goes where. The ID card ALWAYS shows the real student's face.
    /// A real student at the window shows that same face. An anomaly ALWAYS wears the "_anomaly"
    /// art (the owner's rule: like That's Not My Neighbor, the monster is in plain sight). A
    /// PhotoMismatch anomaly wears somebody else's (VisitorData.WindowFaceIndex differs). The one
    /// exception is a SCARED visitor, real student or anomaly: both wear the "_scared" art, so the
    /// monster face cannot give them away.
    /// Keys resolve through ProceduralArt
    /// (Resources/Art/<key>.png first); with no file the caller falls back to the procedural
    /// portrait painter, so a missing asset never shows a broken image.</summary>
    public static class FaceAssets
    {
        // The illustrated busts (one folder per student in Art/), built by
        // Tools/prep_characters.py into Assets/_Project/Resources/Art/. Add a character there
        // and add a row to the Students table; the mapping does not change.
        public static int StudentFaceCount => GameRules.Current.Students.Length;

        /// <summary>Zero-padded key for one ordinary student face: "char_student_03".</summary>
        public static string StudentKey(int faceIndex) =>
            "char_student_" + Pad(Mod(faceIndex, StudentFaceCount) + 1);

        /// <summary>The anomaly variant OF ONE STUDENT: "char_student_03_anomaly" - the same
        /// person as StudentKey(index), visibly altered. Paired assets keep the reference photo
        /// and the in-world face tied to one identity.</summary>
        public static string AnomalyKey(int faceIndex) =>
            StudentKey(faceIndex) + "_anomaly";

        /// <summary>The same student, frightened: "char_student_03_scared". A scared visitor shows this at
        /// the window whether they are a real student or an anomaly, so the monster art cannot tell.</summary>
        public static string ScaredKey(int faceIndex) =>
            StudentKey(faceIndex) + "_scared";

        /// <summary>The face standing at the window: the student's art, or that student's
        /// "_anomaly" art when <paramref name="anomalyVariant"/> is set. null = no asset index,
        /// use the procedural painter.</summary>
        public static string WindowKey(int faceIndex, bool anomalyVariant)
        {
            if (faceIndex < 0) return null;
            return anomalyVariant ? AnomalyKey(faceIndex) : StudentKey(faceIndex);
        }

        /// <summary>The window face for a visitor who may be scared: scared art wins over the monster art.</summary>
        public static string WindowKey(int faceIndex, bool anomalyVariant, bool scared)
        {
            if (faceIndex >= 0 && scared) return ScaredKey(faceIndex);
            return WindowKey(faceIndex, anomalyVariant);
        }

        /// <summary>The photo printed on the ID card: always the real student's face.</summary>
        public static string IdKey(int faceIndex) => faceIndex < 0 ? null : StudentKey(faceIndex);

        /// <summary>The Night-5 district administrator's badge photo. He has no canon roster
        /// row, so he gets his own pair instead of an index: the formal portrait on the badge,
        /// and the window variant that carries her tell.</summary>
        public const string AdminKey = "char_admin";

        /// <summary>The face at the window during the administrator encounter: the same person,
        /// a half-second wrong. Paired with <see cref="AdminKey"/> exactly like the student
        /// anomaly variants are paired with their students.</summary>
        public const string AdminWindowKey = "char_admin_glitch";

        private static int Mod(int v, int n) { int r = v % n; return r < 0 ? r + n : r; }

        private static string Pad(int v) => v < 10 ? "0" + v : v.ToString();
    }
}
