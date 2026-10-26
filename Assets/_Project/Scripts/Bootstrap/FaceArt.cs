using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Chooses the sprites for the window face and the ID photo: the temporary face assets when
    // the run's data maps this visitor to one, otherwise the procedural portrait painter of the
    // exact portrait triple. One decision point, so the window, the card and the face-compare
    // cue can never disagree about who is displayed. Created by CheckpointView.Window.
    public static class FaceArt
    {
        /// <summary>The face at the window: the student's asset, an anomaly face when the run
        /// says the window portrait is distorted, else the procedural painter.</summary>
        public static Sprite WindowSprite(VisitorData v)
        {
            if (v == null) return null;
            bool altered = v.IsAnomaly || v.WindowPortrait.Distorted;
            string key = FaceAssets.WindowKey(v.FaceAssetIndex, altered);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            var drawn = ProceduralArt.Portrait("window", v.WindowPortrait, 240, 296);
            return drawn != null ? drawn : ProceduralArt.Get("face_placeholder");
        }

        /// <summary>The photo printed on the ID: the SAME asset as the window when the run says
        /// the two agree; a mismatched card photo renders its own portrait triple procedurally,
        /// so a mismatch always shows two visibly different people.</summary>
        public static Sprite IdSprite(VisitorData v)
        {
            if (v == null) return null;
            string key = FaceAssets.IdKey(v.FaceAssetIndex, v.PhotoMatches,
                v.IsAnomaly || v.WindowPortrait.Distorted);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            var drawn = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            return drawn != null ? drawn : ProceduralArt.Get("face_placeholder");
        }
    }
}
