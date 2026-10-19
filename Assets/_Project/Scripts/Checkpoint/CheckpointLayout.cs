using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // Desk layout in 1280x720 reference pixels. UIFactory.Anchor treats pos as the CENTER of
    // the element measured from the anchor point named in each comment.
    // Grid: left column = banner/record/id, centre = window + speech + note + objective,
    // right column = questions, utilities, stamp circles, camera. HUD bars sit bottom-left.
    public static class CheckpointLayout
    {
        public const float TopBarHeight = 52f;
        public const float CheckRowStep = 14f;
        public const float CheckRowFirst = -226f;

        public static readonly Vector2 PauseCenter = new Vector2(60f, -26f);        // top-left
        public static readonly Vector2 ClockCenter = new Vector2(-60f, -26f);       // top-right
        public static readonly Vector2 BannerCenter = new Vector2(170f, -58f);      // top-left
        public static readonly Vector2 RecordCenter = new Vector2(170f, 470f);      // bottom-left
        public static readonly Vector2 IdCardCenter = new Vector2(170f, 160f);      // bottom-left
        public static readonly Vector2 WindowCenter = new Vector2(0f, -176f);       // top-center; facade + confirm overlay it
        public static readonly Vector2 PortraitCenter = new Vector2(0f, -150f);     // top-center
        public static readonly Vector2 PatrolCenter = new Vector2(-160f, -370f);    // top-center
        public static readonly Vector2 EndShiftCenter = new Vector2(160f, -370f);   // top-center
        public static readonly Vector2 SpeechCenter = new Vector2(0f, 250f);        // bottom-center
        public static readonly Vector2 NoteCenter = new Vector2(0f, 140f);          // bottom-center
        public static readonly Vector2 ObjectiveCenter = new Vector2(0f, 30f);      // bottom-center
        public static readonly Vector2 QuestionsPanelCenter = new Vector2(-170f, -190f); // top-right
        public static readonly Vector2 QuestionsColumnCenter = new Vector2(-170f, -195f);
        public static readonly Vector2 UtilityGridFirst = new Vector2(-250f, -360f);   // top-right, 2x2 grid
        public static readonly Vector2 UtilityGridStep = new Vector2(160f, -46f);
        public static readonly Vector2 AllowCenter = new Vector2(-100f, 170f);      // bottom-right
        public static readonly Vector2 PrincipalCenter = new Vector2(-260f, 170f);  // bottom-right
        public static readonly Vector2 CameraCenter = new Vector2(-170f, 72f);      // bottom-right
        public static readonly Vector2 ScanCenter = new Vector2(-170f, 150f);       // bottom-right, above camera
    }
}
