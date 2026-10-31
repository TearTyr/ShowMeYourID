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
        // Paper tilts off the concept art (degrees, +z = counter-clockwise on screen): the
        // documents lie on the desk, they are not pinned to the grid.
        public const float TiltCard = -2f, TiltSlip = -2f, TiltLog = -4f, TiltRules = 3f;
        // The printed card / slip art keeps its design space inside a scaled art layer.
        public const float CardArtW = 320f, CardArtH = 220f, SlipArtW = 200f, SlipArtH = 120f;
        // Desk footprint of the two held papers = art design space x these scales, and the
        // container rect IS the drawn rect (one number drives art and interaction area, so
        // they can never drift). Raised ~10% over the old 0.625 / 0.75 display: the documents
        // read easier at arm's length while keeping their proportions and desk slots.
        public const float IdScale = 0.6875f;
        public const float SlipScale = 0.825f;

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
        // Header grid, re-baselined to the owner's SecurityRoom kit: the board is the kit's
        // Upper Panel (measured off the 3021x1690 export), and the sanity bar / pause block sit
        // in the cut-outs the board paints for them (measured off the Sanity & Pause layer).
        public static readonly Region TopBar = new Region(0, 0, 1280, 80);
        public static readonly Region Banner = new Region(20, 25, 375, 30);
        public static readonly Region SanityMeter = new Region(470, 22, 349, 38);   // kit pill, concept-measured
        public static readonly Region LightMeter = new Region(348, 96, 200, 40);   // patrol only, under the board
        public static readonly Region TrackMeter = new Region(568, 96, 110, 40);   // chase only
        public static readonly Region Clock = new Region(1000, 25, 85, 30);
        public static readonly Region NightCounter = new Region(885, 25, 110, 30);   // "NIGHT 1/5", right beside the clock
        public static readonly Region Pause = new Region(1119, 22, 125, 38);        // kit pill, concept-measured

        // ---- row under the top bar (wireframe text baseline y=72) -----------
        // the strip under the kit board (board bottom = 107): hint, run stats, findings
        public static readonly Region Hint = new Region(25, 82, 300, 20);
        public static readonly Region RunStats = new Region(700, 84, 250, 10);        // endless/challenge strip
        public static readonly Region Mutation = new Region(700, 95, 250, 10);       // challenge bulletin strip
        public static readonly Region Findings = new Region(955, 84, 309, 12);        // right edge x=1264
        public static readonly Region RoomInfo = new Region(955, 96, 309, 12);       // exploration only

        // ---- left column ----------------------------------------------------
        // records station slot: below the hint row, above the chat column
        public static readonly Region Station = new Region(40, 85, 305, 160);   // FLOOR FILES registry
        // the floor-files drawer: the cabinet MOUTH, a fixed grid rect under the station.
        // It never moves (the layout audit pins it); the drawer body inside it slides out on
        // a fast ease-out — see CheckpointView.FileDrawer + DrawerMotion. Same left-column
        // width, in the free gap between the station (bottom 259) and the queue strip
        // (top 482). The pulled record sheet (home 16,168 320x290) lands on top of it — the
        // mouth is built before the papers layer, so a pulled file always covers the drawer.
        // 202px tall: it stops clear of the waiting-line strip at y=482 (bottom 516).
        public static readonly Region FileDrawer = new Region(55, 255, 275, 290);
        // default slot for the held ID card; the card is draggable, so this is where it
        // rests, not where it stays (see DragPaper). The container IS the drawn card:
        // CardArt x IdScale, so the pointer surface and the ink can never disagree.
        public static readonly Region IdCard = new Region(450, 500, CardArtW * IdScale, CardArtH * IdScale);
        // the offered ID during the hand-over step: same card, centred over the window
        // counter edge: the student slides the ID here on arrival (root is scaled 0.6,
        // so the drawn card rests on the sill at ~132x99)
        public static readonly Region OfferedId = new Region(405, 208, 320, 220);  // rests on the counter, in front of the glass
        // the paper slip rides beside it on the counter — container == drawn slip
        // (SlipArt x SlipScale), clear of the stamps and the button row
        public static readonly Region CounterSlip = new Region(678, 505, SlipArtW * SlipScale, SlipArtH * SlipScale);

        // ---- centre column --------------------------------------------------
        public static readonly Region Window = new Region(375, 160, 520, 330);
        public static readonly Region Glass = new Region(430, 200, 415, 235);        // portrait wrap
        public static readonly Region Portrait = new Region(530, 205, 200, 230);     // the bust column, centred in the wrap
        public static readonly Region Glitch = new Region(425, 195, 425, 245);       // transient flash
        public static readonly Region Sill = new Region(375, 450, 520, 18);          // under-frame glow strip
        // the speech bubble: a transient overlay box beside the visitor's head (the
        // portrait's right edge is x=715), tail pointing at the face. The registered
        // region is the bubble's FULL box; the panel inside grows down from its top
        // edge with the line count (SpeechBubbleLayout), so short lines stay compact. 132 tall
        // holds six lines of the kit bubble's 15pt text without ever truncating a clue.
        public static readonly Region SpeechBubble = new Region(737, 297, 182, 132);  // clear of face hitbox AND questions
        // booth dressing from the night-shift mockup: hood bands over the frame, the under-frame
        // glow and the two counter bases the window sits on
        public static readonly Region Hood1 = new Region(395, 160, 485, 25);
        public static readonly Region Hood2 = new Region(410, 185, 455, 265);
        public static readonly Region Base1 = new Region(375, 450, 520, 18);
        public static readonly Region Base2 = new Region(350, 468, 580, 22);
        // stamp block captions (the .slbl strip under each APPROVE / DECLINE block)
        public static readonly Region Base3 = new Region(347, 609, 591, 22);
        public static readonly Region PatrolBtn = new Region(450, 655, 180, 40);
        public static readonly Region EndShiftBtn = new Region(650, 655, 180, 40);

        // ---- right column ---------------------------------------------------
        public static readonly Region Questions = new Region(950, 105, 305, 215);
        public static readonly Region UtilityRecords = new Region(80, 555, 160, 165);   // tonight's log paper prop
        public static readonly Region UtilityMap = new Region(955, 330, 290, 170);       // inline map sheet
        public static readonly Region UtilityNotes = new Region(240, 570, 160, 150);   // rules paper prop
        // the verdict tray: two physical stamps, dragged onto the slip to commit. The
        // tray sits right under the questions panel (the retired appearance check's
        // slot) so the right column stays balanced: questions above, stamps below,
        // open desk underneath for spreading papers. Sized at ~78% of the original 152x150
        // boxes (a 22% linear cut) and centred in the same 296..446 band: 8px apart, the D
        // box's right edge flush with the old one, both rows aligned — same colours, labels
        // and typography, a smaller footprint with a comfortable 118x117 grab area.
        // Re-baselined to the owner's SecurityRoom kit: the tray circles sit where the kit's
        // Stamp BG layer paints them (measured from the 2880x1620 export), enlarged to keep the
        // >=96px grab area the audit demands. Allow left, Deny right, one aligned row.
        public static readonly Region StampAllow = new Region(900, 520, 140, 125);   // APPROVE stamp block
        public static readonly Region StampDeny = new Region(1055, 520, 140, 125);  // DECLINE stamp block

        // ---- the Shift-5 conversation (tag A: mutually exclusive with the questions panel
        // and the inline map, which it borrows the column from while it is open) ----------
        // While the administrator talks, the desk puts the question row and the inline map
        // away and gives the whole right column above the stamps to the conversation; when it
        // closes, both come back exactly where they were. 14 option rows fit under the header.
        public static readonly Region AdminTalk = new Region(950, 116, 309, 373);
        // the slim re-open bar shown once the player steps back out of the conversation
        public static readonly Region AdminTalkBar = new Region(950, 492, 309, 20);
        public static class InnerAdminTalk
        {
            public static readonly Region Head = new Region(8, 4, 282, 18);
            public static readonly Region Sub = new Region(8, 22, 282, 14);
            public const float OptY0 = 40f, OptStep = 24f, OptH = 22f;
            public static readonly Region Opt = new Region(8, 0, 282, OptH);   // y set per row
        }

        // ---- transient overlays (bounds-checked only; they cover the desk) ---
        public static readonly Region Toast = new Region(280, 94, 720, 30);
        public static readonly Region Prompt = new Region(450, 62, 440, 20);         // hand-over guidance strip
        // the reading sheet for the rules note / tonight's log: centred over the desk with
        // room for crest, hand title, the hand-written block AND the printed live text
        // the reading sheet: the kit's photographed paper at its OWN aspect (789x879),
        // centred — the art carries crest, title, the hand-written block and the baked
        // tilt, so nothing is re-typeset or rotated over it
        public static readonly Region PaperOverlay = new Region(362, 24, 556, 620);
        public static readonly Region FullDesk = new Region(0, 0, 1280, 720);

        // ---- checkpoint queue (Phase 9 additions to the approved grid) -------
        // Waiting-line silhouette strips stand in the free gaps either side of the booth:
        // left column ends at x=336, the window sill ends at x=875, END SHIFT at x=880.
        // The right strip carries the waiting-count badge above its figures. The full
        // QUEUE/CURRENT readout is the banner's second line (Inner.BannerQueue) — the
        // top bar's right side is fully claimed by the frozen wireframe (clock, pause,
        // findings), so no new top-level top-bar region exists to take it.
        public static readonly Region QueueStripL = new Region(350, 108, 123, 34);
        public static readonly Region QueueStripR = new Region(896, 110, 50, 26);
        // Shift-start sticky note: transient overlay, bounds-checked like the paper overlays.
        public static readonly Region StickyNote = new Region(390, 200, 500, 320);

        // Local sub-rects, measured from the owning panel's top-left corner.
        public static class Inner
        {
            // HUD meters
            public static readonly Region SanityCap = new Region(10, 8, 50, 22);
            public static readonly Region SanityStage = new Region(68, 12, 228, 14);
            public static readonly Region SanityVal = new Region(304, 6, 36, 26);
            public static readonly Region SanityTrack = new Region(66, 8, 232, 22);
            public static readonly Region LightCap = new Region(8, 4, 60, 16);
            public static readonly Region LightTrack = new Region(8, 21, 120, 12);
            public static readonly Region LightState = new Region(134, 8, 62, 24);
            public static readonly Region TrackCap = new Region(6, 4, 60, 14);
            public static readonly Region TrackTrack = new Region(6, 20, 108, 12);

            // floor-files station (panel-local): caption and the two folder tabs — all
            // centred in the cabinet width so the front reads as one symmetric face
            public static readonly Region StationCap = new Region(22.5f, 15, 260, 20);
            public const float FolderTabX = 37.5f, FolderTabY0 = 40f, FolderTabStep = 35f,
                               FolderTabW = 230f, FolderTabH = 34f;
            // floor-files drawer (body-local): the drawer front lip covers the top of the
            // mouth while shut, the header rides just under it, and the file rows start below
            // both — 7 rows fit the pulled drawer (see DrawerRowCapacity)
            public const float DrawerFrontH = 25f;                           // the pulled front bracket
            public const float DrawerLipH = 28f;                   // the mask the body slides behind
            public const float DrawerBodyH = 255f;                 // the row sheet inside the mouth
            public const float DrawerHeadH = 18f;
            public const float DrawerFrontTravelRatio = 0.06f;     // the front rides out a little
            public static readonly Region DrawerHead = new Region(15, 30, 245, DrawerHeadH);
            public static readonly Region DrawerHandle = new Region(30, 9, 80, 7);
            public static readonly Region DrawerFrontLabel = new Region(0, 6, 266, 14);
            public const float FileRowX = 15f, FileRowY0 = 50f, FileRowStep = 27f,
                               FileRowW = 250f, FileRowH = 22f;
            public const float FileRowPillW = 160f;                // the dark name pill on the sheet
            // file-row sub-columns (row-local): the name takes the manila body, the registry
            // number sits right-aligned on the same line
            public const float FileRowNameX = 6f, FileRowNameW = 148f,
                               FileRowIdX = 160f, FileRowIdW = 75f;

            // record sheet (sprite is printed 1:1 at 320x290)
            public static readonly Region RecordHeader = new Region(0, 2, 284, 24);  // clear of the X
            public const float RecordRowX = 14f, RecordRowW = 292f, RecordRowH = 14f;
            public static readonly float[] RecordRules = { 42f, 63f, 84f, 105f, 126f, 147f, 168f };

            // id card (sprite is printed 1:1 at 320x220)
            public static readonly Region IdHeader = new Region(0, 2, 320, 24);
            public static readonly Region IdPhoto = new Region(10, 46, 90, 118);
            public const float IdRowX = 112f, IdRowW = 200f, IdRowH = 16f;
            public static readonly float[] IdRules = { 52f, 75f, 99f, 123f, 146f, 179f };

            // speech bubble (container-local): the tail hangs off the panel's left edge,
            // tip toward the visitor's face; the text stretches inside the panel, which
            // the view sizes per line count (BubblePanelX/W are the static parts)
            public const float BubblePanelX = 0f, BubblePanelW = 182f;
            public const float BubblePadTop = 6f, BubblePadBottom = 6f,
                               BubbleTagH = 14f, BubbleLineH = 14f;
            public const int BubbleCharsPerLine = 24, BubbleMaxLines = 3;
            // the kit bubble carries no speaker tag and is read at a glance, so it uses bigger
            // type than the no-kit bubble. Its art is a sliced sprite: a 48u painted-tail border
            // on the left, 14u on the other sides, and the white sits 6u inside the outer edge
            // (jagged ink), so the text keeps a clear inset inside that white (never on the ink)
            public const int BubbleFontKit = 15, BubbleCharsPerLineKit = 21;
            public const float BubbleLineHKit = 18f;
            public const float BubbleKitTailW = 48f, BubbleKitEdgeW = 14f, BubbleKitWhiteMargin = 6f;
            public const float BubbleKitInsetX = 10f, BubbleKitInsetY = 12f;
            // the sec_bubble master's pixels per layout unit (ProceduralArt.FilePpus) — 480px tail = 48u
            public const float BubbleKitArtPxPerUnit = 10f;
            public static readonly Region BubbleTail = new Region(-34, 26, 44, 36);

            // the counter slip (panel-local): the written lines own the sheet. The verdict
            // mark lands WHERE the player drops the stamp (Papers, Please style), clamped
            // fully inside the paper by StampPlacement — no printed target box.
            public static readonly Region SlipReason = new Region(8, 52, 184, 56);

            // questions panel (panel-local): the header strip, then ONE content box the
            // choice column is divided from (LayoutQuestions). The box stops above the
            // kit panel's painted bottom border (measured at local y=198 on the 305x215
            // rect), so no choice can ever land on — or through — the panel frame.
            public static readonly Region QHead = new Region(16, 14, 150, 24);
            public static readonly Region QBudget = new Region(234, 14, 48, 24);
            public const float QBtnX = 16f, QBtnW = 266f;
            public const float QContentY0 = 44f, QContentY1 = 193f, QBtnGap = 7f;

            // verdict banner

            // paper overlays (rules note / tonight's log): the photographed sheet owns
            // the printed ink; the LIVE typed body rides on an addendum slip pinned over
            // the sheet's blank bottom band (the paper keeps its aspect and its art),
            // and the close pill sits on the sheet's blank top-right corner
            public static readonly Region PaperSlip = new Region(405, 596, 470, 116);
            public static readonly Region PaperSlipBody = new Region(14, 10, 442, 96);
            public static readonly Region PaperClose = new Region(688, 42, 140, 36);

            // day banner: night label on top, queue readout underneath
            public static readonly Region BannerTop = new Region(0, 0, 700, 30);   // pre-squeeze width
            public static readonly Region BannerQueue = new Region(0, 18, 380, 16);

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
