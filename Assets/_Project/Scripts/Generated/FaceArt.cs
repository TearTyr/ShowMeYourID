using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // Chooses the sprites for the window face and the ID photo: the face art when the visitor
    // maps to one, otherwise the empty-photo placeholder. One decision point, so the window
    // and the card can never disagree about who is displayed. Created by CheckpointView.Window.
    public static class FaceArt
    {
        /// <summary>The face at the window: an anomaly always wears its monster art (see FaceAssets).</summary>
        public static Sprite WindowSprite(VisitorData v)
        {
            if (v == null) return null;
            string key = v.IsAdministrator ? FaceAssets.AdminWindowKey : WindowKeyFor(v);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            return ProceduralArt.Get("face_placeholder");   // only if a face file is missing
        }

        /// <summary>The photo printed on the ID: always the real student's face art.</summary>
        public static Sprite IdSprite(VisitorData v)
        {
            if (v == null) return null;
            string key = v.IsAdministrator ? FaceAssets.AdminKey : FaceAssets.IdKey(v.FaceAssetIndex);
            Sprite asset;
            if (key != null && ProceduralArt.TryGet(key, out asset)) return asset;
            return ProceduralArt.Get("face_placeholder");   // only if a face file is missing
        }

        private static string WindowKeyFor(VisitorData v)
        {
            int face = v.WindowFaceIndex >= 0 ? v.WindowFaceIndex : v.FaceAssetIndex;
            return FaceAssets.WindowKey(face, v.IsAnomaly, v.IsScared);
        }
    }
}
