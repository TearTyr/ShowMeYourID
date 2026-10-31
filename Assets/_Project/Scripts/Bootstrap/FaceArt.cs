using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Chooses the sprites for the window face and the ID photo: the face art when the visitor
    // maps to one, otherwise the procedural portrait painter. One decision point, so the window
    // and the card can never disagree about who is displayed. Created by CheckpointView.Window.
    public static class FaceArt
    {
        /// <summary>The face at the window. Only a photo defect changes it (see FaceAssets).</summary>
        public static Sprite WindowSprite(VisitorData v)
        {
            if (v == null) return null;
            string key = v.IsAdministrator ? FaceAssets.AdminWindowKey : WindowKeyFor(v);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            var drawn = ProceduralArt.Portrait("window", v.WindowPortrait, 240, 296);
            return drawn != null ? drawn : ProceduralArt.Get("face_placeholder");
        }

        /// <summary>The photo printed on the ID: always the real student's face art.</summary>
        public static Sprite IdSprite(VisitorData v)
        {
            if (v == null) return null;
            string key = v.IsAdministrator ? FaceAssets.AdminKey : FaceAssets.IdKey(v.FaceAssetIndex);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            var drawn = ProceduralArt.Portrait("card", v.IdPhoto, 168, 208);
            return drawn != null ? drawn : ProceduralArt.Get("face_placeholder");
        }

        private static string WindowKeyFor(VisitorData v)
        {
            int face = v.WindowFaceIndex >= 0 ? v.WindowFaceIndex : v.FaceAssetIndex;
            bool subtle = v.Defects.Contains(DefectType.PhotoSubtleMismatch) || v.WindowPortrait.Distorted;
            return FaceAssets.WindowKey(face, subtle);
        }
    }
}
