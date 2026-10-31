namespace Hollowbrook.Logic
{
    /// <summary>Which face art goes where. The ID card ALWAYS shows the real student's face.
    /// The window shows that same face, unless the visitor has a photo defect:
    ///  - PhotoMismatch: somebody else's face (VisitorData.WindowFaceIndex differs),
    ///  - PhotoSubtleMismatch: the same student's "_anomaly" art (one feature is off).
    /// A visitor with any other defect looks completely normal, so only the papers and the
    /// classroom answer can give them away. Keys resolve through ProceduralArt
    /// (Resources/Art/<key>.png first); with no file the caller falls back to the procedural
    /// portrait painter, so a missing asset never shows a broken image.</summary>
    public static class FaceAssets
    {
        // Temporary set shipped under Assets/_Project/Resources/Art/ (see Tools/make_face_assets.py
        // / the pass notes). Swap the files for final art later; the mapping does not change.
        public const int StudentFaceCount = 6;

        /// <summary>Zero-padded key for one ordinary student face: "student_face_03".</summary>
        public static string StudentKey(int faceIndex) =>
            "student_face_" + Pad(Mod(faceIndex, StudentFaceCount) + 1);

        /// <summary>The anomaly variant OF ONE STUDENT: "student_face_03_anomaly" — the same
        /// person as StudentKey(index), visibly altered. Paired assets keep the reference photo
        /// and the in-world face tied to one identity.</summary>
        public static string AnomalyKey(int faceIndex) =>
            StudentKey(faceIndex) + "_anomaly";

        /// <summary>The face standing at the window: the student's art, or that student's
        /// "_anomaly" art when <paramref name="anomalyVariant"/> is set. null = no asset index,
        /// use the procedural painter.</summary>
        public static string WindowKey(int faceIndex, bool anomalyVariant)
        {
            if (faceIndex < 0) return null;
            return anomalyVariant ? AnomalyKey(faceIndex) : StudentKey(faceIndex);
        }

        /// <summary>The photo printed on the ID card: always the real student's face.</summary>
        public static string IdKey(int faceIndex) => faceIndex < 0 ? null : StudentKey(faceIndex);

        /// <summary>The Night-5 district administrator's badge photo. He has no canon roster
        /// row, so he gets his own pair instead of an index: the formal portrait on the badge,
        /// and the window variant that carries her tell.</summary>
        public const string AdminKey = "admin_face";

        /// <summary>The face at the window during the administrator encounter: the same person,
        /// a half-second wrong. Paired with <see cref="AdminKey"/> exactly like the student
        /// anomaly variants are paired with their students.</summary>
        public const string AdminWindowKey = "admin_face_glitch";

        private static int Mod(int v, int n) { int r = v % n; return r < 0 ? r + n : r; }

        private static string Pad(int v) => v < 10 ? "0" + v : v.ToString();
    }
}
