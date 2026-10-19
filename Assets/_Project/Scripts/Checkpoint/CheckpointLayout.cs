using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // Where every desk panel sits, in 1280x720 reference pixels.
    // UIFactory.Anchor(parent, anchor, size, pos) treats pos as the CENTER of the element,
    // measured from the anchor point named in each comment below. Keeping them here means
    // the layout can be read and adjusted in one place instead of 20 call sites.
    public static class CheckpointLayout
    {
        public const float CheckRowStep = 16f;

        public static readonly Vector2 BannerCenter = new Vector2(170f, -56f);      // from top-left
        public static readonly Vector2 FacadeCenter = new Vector2(170f, -160f);    // from top-left
        public static readonly Vector2 ConfirmCenter = new Vector2(170f, -282f);   // from top-left
        public static readonly Vector2 RecordCenter = new Vector2(170f, -480f);    // from top-left
        public static readonly Vector2 WindowCenter = new Vector2(0f, -176f);     // from top-center
        public static readonly Vector2 PortraitCenter = new Vector2(0f, -148f);   // from top-center
        public static readonly Vector2 SpeechCenter = new Vector2(220f, -370f);   // from top-center
        public static readonly Vector2 QuestionsPanelCenter = new Vector2(-150f, -177f); // top-right; clears the 52px topbar
        public static readonly Vector2 QuestionsColumnCenter = new Vector2(-150f, -160f); // top-right; button stack centers on the panel
        public static readonly Vector2 UtilityCenterFirst = new Vector2(-225f, -340f);  // from top-right
        public const float UtilityStep = 90f;
        public static readonly Vector2 PatrolCenter = new Vector2(100f, 40f);     // from bottom-left
        public static readonly Vector2 EndShiftCenter = new Vector2(295f, 40f);   // from bottom-left
        public static readonly Vector2 NoteCenter = new Vector2(-80f, 120f);       // from bottom-CENTER (x is an offset)
        public static readonly Vector2 IdCardCenter = new Vector2(830f, 110f);     // from bottom-LEFT
        public static readonly Vector2 CameraCenter = new Vector2(-140f, 110f);   // from bottom-right
        public static readonly Vector2 AllowCenter = new Vector2(-215f, 250f);    // from bottom-right
        public static readonly Vector2 PrincipalCenter = new Vector2(-85f, 250f); // from bottom-right
        public static readonly Vector2 ScanCenter = new Vector2(-344f, 277f);     // from bottom-right
    }
}
