using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // The approved desk grid: absolute rects in 1280x720 reference pixels, origin top-left,
    // y down — transcribed 1:1 from desk_layout_wireframe.svg. Everything on the desk and in
    // the HUD top bar is a child of one fixed, centred frame and is placed only through
    // Place() => UIFactory.PlaceTL. No corner pins, no per-element anchor choices, no layout
    // maths anywhere else.
    public static class CheckpointLayout
    {
        public const float RefW = 1280f;
        public const float RefH = 720f;

        public struct Region
        {
            public float X, Y, W, H;
            public Region(float x, float y, float w, float h) { X = x; Y = y; W = w; H = h; }
            public float Right => X + W;
            public float Bottom => Y + H;
        }

        // ---- top bar --------------------------------------------------------
        public static readonly Region TopBar = new Region(0, 0, 1280, 52);
        public static readonly Region Banner = new Region(16, 4, 320, 44);
        public static readonly Region SanityMeter = new Region(348, 15, 280, 22);   // slim
        public static readonly Region LightMeter = new Region(636, 4, 220, 44);      // patrol only
        public static readonly Region TrackMeter = new Region(864, 4, 120, 44);      // chase only
        public static readonly Region Clock = new Region(1000, 4, 150, 44);
        public static readonly Region Pause = new Region(1160, 8, 104, 36);

        // ---- row under the top bar (wireframe text baseline y=72) -----------
        public static readonly Region Hint = new Region(348, 56, 540, 24);   // free strip under the top bar
        public static readonly Region RunStats = new Region(20, 56, 320, 14);        // endless/challenge strip
        public static readonly Region Mutation = new Region(20, 70, 320, 14);        // challenge bulletin strip
        public static readonly Region Findings = new Region(894, 58, 370, 16);       // right edge x=1264
        public static readonly Region RoomInfo = new Region(894, 76, 370, 16);       // exploration only

        // ---- left column ----------------------------------------------------
        // records station slot: below the hint row, above the chat column
        public static readonly Region Station = new Region(16, 88, 320, 96);   // floor-files cabinet
        // the floor-files drawer: slides open under the station when a folder tab is
        // clicked, listing that floor's student files. Same left-column width, in the free
        // gap between the station (bottom 184) and the TL/RN row (top 466). The pulled
        // record sheet (home 16,168 320x290) lands on top of it — the drawer sits under the
        // papers layer.
        public static readonly Region FileDrawer = new Region(16, 188, 320, 270);
        // default slot for the held ID card; the card is draggable, so this is where it
        // rests, not where it stays (see DragPaper). The card is drawn slightly smaller
        // than the slot rect: DragPaper.BaseScale 0.82 (~18% under the old 1x display)
        public static readonly Region IdCard = new Region(438, 478, 304, 209);   // default desk slot
        // the offered ID during the hand-over step: same card size, centred over the window
        // counter edge: the student slides the ID here on arrival (root is scaled 0.6,
        // so the drawn card rests on the sill at ~192x132)
        public static readonly Region OfferedId = new Region(405, 208, 320, 220);  // rests on the counter, in front of the glass
        // the paper slip rides beside it on the counter
        public static readonly Region CounterSlip = new Region(755, 288, 200, 120);  // counter edge, clear of the dialogue strip

        // ---- centre column --------------------------------------------------
        public static readonly Region Window = new Region(405, 88, 470, 316);
        public static readonly Region Glass = new Region(420, 98, 440, 296);         // inside the frame
        public static readonly Region Portrait = new Region(565, 104, 150, 180);     // inside the glass
        public static readonly Region Glitch = new Region(560, 99, 160, 190);        // transient flash
        public static readonly Region Sill = new Region(405, 404, 470, 24);
        // the speech bubble: a transient overlay box beside the visitor's head (the
        // portrait's right edge is x=715), tail pointing at the face. The registered
        // region is the bubble's FULL box; the panel inside grows down from its top
        // edge with the line count (SpeechBubbleLayout), so short lines stay compact.
        public static readonly Region SpeechBubble = new Region(718, 100, 248, 210);
        public static readonly Region PatrolBtn = new Region(520, 690, 140, 24);
        public static readonly Region EndShiftBtn = new Region(672, 690, 150, 24);

        // ---- right column ---------------------------------------------------
        public static readonly Region Questions = new Region(968, 88, 296, 190);
        public static readonly Region UtilityRecords = new Region(16, 466, 156, 36);   // TL
        public static readonly Region UtilityMap = new Region(16, 510, 320, 96);        // MAP box
        public static readonly Region UtilityNotes = new Region(180, 466, 156, 36);    // RN
        // the verdict tray: two physical stamps, dragged onto the slip to commit. The
        // tray sits right under the questions panel (the retired appearance check's
        // slot) so the right column stays balanced: questions above, stamps below,
        // open desk underneath for spreading papers.
        public static readonly Region StampDeny = new Region(1128, 296, 152, 150);  // big D box
        public static readonly Region StampAllow = new Region(968, 296, 152, 150);  // big A box

        // ---- transient overlays (bounds-checked only; they cover the desk) ---
        public static readonly Region Toast = new Region(280, 116, 720, 30);
        public static readonly Region Prompt = new Region(320, 598, 640, 30);        // exploration only
        public static readonly Region ConfirmBox = new Region(430, 305, 420, 110);
        public static readonly Region PaperOverlay = new Region(260, 120, 760, 480);
        public static readonly Region FullDesk = new Region(0, 0, 1280, 720);

        // ---- checkpoint queue (Phase 9 additions to the approved grid) -------
        // Waiting-line silhouette strips stand in the free gaps either side of the booth:
        // left column ends at x=336, the window sill ends at x=875, END SHIFT at x=880.
        // The right strip carries the waiting-count badge above its figures. The full
        // QUEUE/CURRENT readout is the banner's second line (Inner.BannerQueue) — the
        // top bar's right side is fully claimed by the frozen wireframe (clock, pause,
        // findings), so no new top-level top-bar region exists to take it.
        public static readonly Region QueueStripL = new Region(340, 436, 60, 240);
        public static readonly Region QueueStripR = new Region(884, 428, 80, 244);
        // Shift-start sticky note: transient overlay, bounds-checked like the paper overlays.
        public static readonly Region StickyNote = new Region(390, 200, 500, 320);

        // Local sub-rects, measured from the owning panel's top-left corner.
        public static class Inner
        {
            // HUD meters
            public static readonly Region SanityCap = new Region(8, 4, 110, 16);
            public static readonly Region SanityStage = new Region(118, 4, 100, 16);
            public static readonly Region SanityVal = new Region(218, 4, 54, 16);
            public static readonly Region SanityTrack = new Region(8, 21, 264, 12);
            public static readonly Region LightCap = new Region(8, 4, 60, 16);
            public static readonly Region LightTrack = new Region(8, 21, 204, 12);
            public static readonly Region TrackCap = new Region(6, 4, 60, 14);
            public static readonly Region TrackTrack = new Region(6, 20, 108, 12);

            // floor-files station (panel-local): caption, two folder tabs, status line
            public static readonly Region StationCap = new Region(0, 3, 320, 15);
            public const float FolderTabX = 8f, FolderTabY0 = 20f, FolderTabStep = 26f,
                               FolderTabW = 304f, FolderTabH = 24f;
            public static readonly Region StationOut = new Region(8, 72, 304, 22);

            // floor-files drawer (panel-local): header + file rows (7 rows fit the region)
            public static readonly Region DrawerHead = new Region(10, 6, 300, 18);
            public const float FileRowX = 10f, FileRowY0 = 28f, FileRowStep = 32f,
                               FileRowW = 300f, FileRowH = 30f;

            // record sheet (sprite is printed 1:1 at 320x290)
            public static readonly Region RecordHeader = new Region(0, 2, 284, 24);  // clear of the X
            public const float RecordRowX = 14f, RecordRowW = 292f, RecordRowH = 14f;
            public static readonly float[] RecordRules = { 42f, 63f, 84f, 105f, 126f, 147f, 168f };
            public static readonly Region StampLocal = new Region(195, 165, 110, 110);

            // id card (sprite is printed 1:1 at 320x220)
            public static readonly Region IdHeader = new Region(0, 2, 320, 24);
            public static readonly Region IdPhoto = new Region(10, 46, 90, 118);
            public const float IdRowX = 112f, IdRowW = 200f, IdRowH = 16f;
            public static readonly float[] IdRules = { 52f, 75f, 99f, 123f, 146f, 179f };

            // speech bubble (container-local): the tail hangs off the panel's left edge,
            // tip toward the visitor's face; the text stretches inside the panel, which
            // the view sizes per line count (BubblePanelX/W are the static parts)
            public const float BubblePanelX = 0f, BubblePanelW = 248f;
            public const float BubblePadTop = 8f, BubblePadBottom = 8f,
                               BubbleTagH = 16f, BubbleLineH = 15f;
            public const int BubbleCharsPerLine = 36, BubbleMaxLines = 10;
            public static readonly Region BubbleTail = new Region(-16, 14, 20, 26);
            public static readonly Region NoteHead = new Region(10, 6, 260, 18);
            public static readonly Region NoteBody = new Region(10, 28, 260, 74);

            // the counter slip (panel-local): the written lines own the sheet. The verdict
            // mark lands WHERE the player drops the stamp (Papers, Please style), clamped
            // fully inside the paper by StampPlacement — no printed target box.
            public static readonly Region SlipReason = new Region(8, 52, 184, 56);

            // questions panel (buttons on the wireframe grid: x=980, y=118, step 42)
            public static readonly Region QHead = new Region(12, 6, 140, 20);
            public static readonly Region QBudget = new Region(146, 6, 138, 16);
            public const float QBtnX = 12f, QBtnY0 = 26f, QBtnStep = 32f, QBtnW = 272f, QBtnH = 30f;
            public static readonly Region QHint = new Region(12, 196, 272, 16);
            public static readonly Region QTestimony = new Region(12, 214, 272, 40);

            // confirmation popup
            public static readonly Region ConfirmText = new Region(20, 12, 380, 40);
            public static readonly Region ConfirmNo = new Region(150, 64, 90, 30);
            public static readonly Region ConfirmYes = new Region(250, 64, 90, 30);

            // verdict banner

            // paper overlays (rules note / tonight's log)
            public static readonly Region PaperHead = new Region(30, 22, 700, 32);
            public static readonly Region PaperBody = new Region(40, 62, 680, 344);
            public static readonly Region PaperClose = new Region(310, 416, 140, 38);

            // waiting-line strips (panel-local slots: 4 silhouettes each, 56px step)
            public const float QueueSlotW = 44f, QueueSlotH = 52f, QueueSlotStep = 56f;
            public const float QueueSlotLX = 8f, QueueSlotLY0 = 6f;    // inside the 60-wide left strip
            public const float QueueSlotRX = 18f, QueueSlotRY0 = 22f;  // below the right strip's badge
            public static readonly Region QueueBadge = new Region(0, 2, 80, 18);

            // day banner: night label on top, queue readout underneath
            public static readonly Region BannerTop = new Region(4, 2, 312, 20);
            public static readonly Region BannerQueue = new Region(4, 22, 312, 18);

            // sticky note (shift-start gate)
            public static readonly Region StickyTitle = new Region(30, 48, 440, 40);
            public static readonly Region StickyBody = new Region(40, 116, 420, 100);
            public static readonly Region StickyCta = new Region(40, 240, 420, 34);
        }

        // One fixed 1280x720 frame, centred in the canvas. Children use grid coordinates only.
        public static RectTransform DeskFrame(Transform parent, string name)
        {
            return UIFactory.FixedFrame(parent, name, RefW, RefH);
        }

        // The single placement call for grid elements and panel-local sub-rects alike.
        public static void Place(RectTransform rt, Region r)
        {
            UIFactory.PlaceTL(rt, r.X, r.Y, r.W, r.H);
        }
    }
}
