using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // One grid for desk and HUD, in 1280x720 reference pixels, top-left origin, exactly as the
    // mockup reads. Regions never overlap; Place pins each region to the screen corner nearest
    // it so other aspect ratios grow the gutters instead of colliding columns.
    public static class CheckpointLayout
    {
        public const float RefW = 1280f;
        public const float RefH = 720f;
        public const float TopBarHeight = 52f;

        public struct Region
        {
            public float X, Y, W, H;
            public Region(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            public float Right => X + W;
            public float Bottom => Y + H;
        }

        public enum Pin { TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight, Center }

        public static readonly Region Banner = new Region(16, 4, 320, 44);
        public static readonly Region SanityMeter = new Region(348, 4, 280, 44);
        public static readonly Region LightMeter = new Region(636, 4, 220, 44);
        public static readonly Region TrackMeter = new Region(864, 4, 120, 44);
        public static readonly Region Clock = new Region(1000, 4, 150, 44);
        public static readonly Region Pause = new Region(1160, 8, 104, 36);

        public static readonly Region Objective = new Region(16, 56, 460, 24);
        public static readonly Region RunStats = new Region(490, 56, 340, 24);
        public static readonly Region Findings = new Region(1016, 56, 248, 24);
        public static readonly Region RoomInfo = new Region(1016, 84, 248, 22);
        public static readonly Region Mutation = new Region(320, 84, 640, 26);
        public static readonly Region Toast = new Region(280, 116, 720, 30);

        public static readonly Region Record = new Region(16, 64, 320, 290);
        public static readonly Region IdCard = new Region(16, 362, 320, 220);
        public static readonly Region Checklist = new Region(16, 590, 320, 122);

        public static readonly Region Window = new Region(405, 88, 470, 316);
        public static readonly Region Portrait = new Region(565, 104, 150, 180);
        public static readonly Region Glitch = new Region(560, 99, 160, 190);
        public static readonly Region Sill = new Region(405, 404, 470, 24);
        public static readonly Region Speech = new Region(490, 436, 300, 110);
        public static readonly Region Note = new Region(500, 554, 280, 110);
        public static readonly Region PatrolBtn = new Region(490, 672, 180, 36);
        public static readonly Region EndShiftBtn = new Region(680, 672, 200, 36);

        public static readonly Region Questions = new Region(968, 88, 296, 260);
        public static readonly Region UtilityScan = new Region(968, 356, 140, 36);
        public static readonly Region UtilityRecords = new Region(1124, 356, 140, 36);
        public static readonly Region UtilityMap = new Region(968, 396, 140, 36);
        public static readonly Region UtilityNotes = new Region(1124, 396, 140, 36);
        public static readonly Region StampReject = new Region(984, 440, 110, 110);
        public static readonly Region StampAllow = new Region(1148, 440, 110, 110);
        public static readonly Region Monitor = new Region(968, 558, 296, 146);

        public static readonly Region Prompt = new Region(320, 598, 640, 30);
        public static readonly Region ConfirmBox = new Region(430, 305, 420, 110);
        public static readonly Region VerdictBanner = new Region(320, 235, 640, 250);
        public static readonly Region PaperOverlay = new Region(260, 120, 760, 480);

        public static void Place(RectTransform rt, Region r, Pin pin)
        {
            Vector2 anchor;
            Vector2 pos;
            switch (pin)
            {
                case Pin.TopLeft:
                    anchor = new Vector2(0, 1); pos = new Vector2(r.X, -r.Y); break;
                case Pin.TopCenter:
                    anchor = new Vector2(0.5f, 1); pos = new Vector2(r.X + r.W / 2 - RefW / 2, -r.Y); break;
                case Pin.TopRight:
                    anchor = new Vector2(1, 1); pos = new Vector2(r.Right - RefW, -r.Y); break;
                case Pin.BottomLeft:
                    anchor = new Vector2(0, 0); pos = new Vector2(r.X, r.Bottom - RefH); break;
                case Pin.BottomCenter:
                    anchor = new Vector2(0.5f, 0); pos = new Vector2(r.X + r.W / 2 - RefW / 2, r.Bottom - RefH); break;
                case Pin.BottomRight:
                    anchor = new Vector2(1, 0); pos = new Vector2(r.Right - RefW, r.Bottom - RefH); break;
                default:
                    anchor = new Vector2(0.5f, 0.5f);
                    pos = new Vector2(r.X + r.W / 2 - RefW / 2, RefH / 2 - r.Y - r.H / 2); break;
            }
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = new Vector2(r.W, r.H);
            rt.anchoredPosition = pos;
        }

        // Children: x/y measured from the parent's top-left corner, y down, no centre maths.
        public static void PlaceLocal(RectTransform rt, float x, float y, float w, float h)
        {
            var top = new Vector2(0, 1);
            rt.anchorMin = top;
            rt.anchorMax = top;
            rt.pivot = top;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }
    }
}
