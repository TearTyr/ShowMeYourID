namespace Hollowbrook.Logic
{
    /// <summary>Stable mapping from a student identity to its temporary face asset. The index
    /// rides on VisitorData (set once by RosterFactory from the canon roster position), so the
    /// same student always shows the same face on the card AND at the window — and an anomaly is
    /// never "the odd-looking portrait", it is explicit game data (IsAnomaly / Distorted /
    /// PhotoMatches) that decides which representation renders. Keys resolve through
    /// ProceduralArt (Resources/Art/<key>.png first); when no file ships, the caller falls back
    /// to the procedural portrait painter, so missing assets can never show a broken image.</summary>
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

        /// <summary>The face standing at the window: the student's own asset — or that same
        /// student's ANOMALY variant when the run's data says this visitor is anomalous (or its
        /// window portrait is distorted). Both flags are explicit game data, never a guess about
        /// how a portrait looks. null = no asset index, use the procedural painter.</summary>
        public static string WindowKey(int faceIndex, bool anomalyVariant)
        {
            if (faceIndex < 0) return null;
            return anomalyVariant ? AnomalyKey(faceIndex) : StudentKey(faceIndex);
        }

        /// <summary>The photo printed on the ID card. It uses the SAME asset as the window only
        /// when the run says the two agree (PhotoMatches); a swapped/subtle-variant card photo
        /// falls back to the procedural painter of its own portrait triple, so a mismatched pair
        /// always LOOKS like two different people — the cue never contradicts the pixels.</summary>
        public static string IdKey(int faceIndex, bool photoMatches, bool anomalyWindow)
        {
            if (faceIndex < 0 || !photoMatches || anomalyWindow) return null;
            return StudentKey(faceIndex);   // the card always keeps the NORMAL reference face
        }

        private static int Mod(int v, int n) { int r = v % n; return r < 0 ? r + n : r; }

        private static string Pad(int v) => v < 10 ? "0" + v : v.ToString();
    }
}
