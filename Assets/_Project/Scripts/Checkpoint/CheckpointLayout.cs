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
        // edge with the line count (SpeechBubbleLayout), so short lines stay compact.
        public static readonly Region SpeechBubble = new Region(737, 297, 182, 72);  // clear of face hitbox AND questions
        // booth dressing from the night-shift mockup: hood bands over the frame, the under-frame
        // glow and the two counter bases the window sits on
        public static readonly Region Hood1 = new Region(395, 160, 485, 25);
        public static readonly Region Hood2 = new Region(410, 185, 455, 265);
        public static readonly Region Base1 = new Region(375, 450, 520, 18);
        public static readonly Region Base2 = new Region(350, 468, 580, 22);
        // stamp block captions (the .slbl strip under each APPROVE / DECLINE block)
        public static readonly Region Base3 = new Region(347, 609, 591, 22);
        public static readonly Region StampLabelAllow = new Region(900, 655, 140, 40);
        public static readonly Region StampLabelDeny = new Region(1055, 655, 140, 40);
        public static readonly Region PatrolBtn = new Region(450, 655, 180, 40);
        public static readonly Region EndShiftBtn = new Region(650, 655, 180, 40);

        // ---- the window talk dock (dialogue panel: history + the typed text box) ----
        // A slim always-visible strip in the free bottom margin under the patrol/end-shift
        // row: the text box the window conversation runs through. It claims no concept-art
        // furniture (the strip sits in empty desk margin) and never covers a control:
        // y 692..718 is below every button's bottom edge (691).
        public static readonly Region TalkDock = new Region(440, 696, 420, 22);
        // the transcript sheet: expands UP over the paper spread while open (pinned by the
        // LOG button or breathing open for a few seconds after each reply), collapsed by
        // default so the idle desk stays exactly as the concept art paints it.
        public static readonly Region TalkLog = new Region(440, 462, 420, 226);

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
        public static readonly Region ConfirmBox = new Region(430, 305, 420, 110);
        // the reading sheet for the rules note / tonight's log: centred over the desk with
        // room for crest, hand title, the hand-written block AND the printed live text
        public static readonly Region PaperOverlay = new Region(240, 60, 800, 600);
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
            // front-local, beside the handle: the registry readout rides out with the drawer
            public static readonly Region DrawerFrontOut = new Region(170, 10, 90, 14);
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
            public static readonly Region StampLocal = new Region(195, 165, 110, 110);

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
            public static readonly Region BubbleTail = new Region(-34, 26, 44, 36);
            public static readonly Region NoteHead = new Region(10, 6, 260, 18);
            public static readonly Region NoteBody = new Region(10, 28, 260, 74);

            // the counter slip (panel-local): the written lines own the sheet. The verdict
            // mark lands WHERE the player drops the stamp (Papers, Please style), clamped
            // fully inside the paper by StampPlacement — no printed target box.
            public static readonly Region SlipReason = new Region(8, 52, 184, 56);

            // questions panel (buttons on the wireframe grid: x=980, y=118, step 42)
            public static readonly Region QHead = new Region(15, 25, 150, 25);   // pre-squeeze width
            public static readonly Region QBudget = new Region(238, 25, 44, 25);
            public const float QBtnX = 15f, QBtnY0 = 60f, QBtnStep = 37f, QBtnW = 275f, QBtnH = 28f;
            public static readonly Region QHint = new Region(16, 199, 277, 14);
            public static readonly Region QTestimony = new Region(12, 218, 285, 40);

            // confirmation popup
            public static readonly Region ConfirmText = new Region(20, 12, 380, 40);
            public static readonly Region ConfirmNo = new Region(150, 64, 90, 30);
            public static readonly Region ConfirmYes = new Region(250, 64, 90, 30);

            // verdict banner

            // paper overlays (rules note / tonight's log): one centred reading sheet,
            // top to bottom — kit crest, hand title, the hand-written static block
            // (rules lines, or the log checklist with its boxes), then the printed
            // live body in its own masked column, and the close pill under it
            public static readonly Region PaperCrest = new Region(345, 14, 110, 113);
            public static readonly Region PaperHead = new Region(60, 132, 680, 46);
            public static readonly Region PaperHand = new Region(70, 180, 660, 170);
            public const float PaperCheckX = 70f, PaperCheckY0 = 196f, PaperCheckStep = 44f,
                               PaperCheckW = 420f, PaperCheckH = 32f,
                               PaperCheckBoxX = 640f, PaperCheckBoxS = 34f;
            public static readonly Region PaperBody = new Region(70, 356, 660, 182);
            public static readonly Region PaperClose = new Region(330, 548, 140, 38);

            // waiting-line strips (panel-local slots: 4 silhouettes each, 56px step)
            public const float QueueSlotW = 44f, QueueSlotH = 52f, QueueSlotStep = 56f;
            public const float QueueSlotLX = 8f, QueueSlotLY0 = 6f;    // inside the 60-wide left strip
            public const float QueueSlotRX = 18f, QueueSlotRY0 = 22f;  // below the right strip's badge
            public static readonly Region QueueBadge = new Region(0, -16, 123, 16);

            // day banner: night label on top, queue readout underneath
            public static readonly Region BannerTop = new Region(0, 0, 700, 30);   // pre-squeeze width
            public static readonly Region BannerQueue = new Region(0, 18, 380, 16);

            // window talk dock (dock-local): LOG toggle · the text box · SEND
            public static readonly Region TalkLogToggle = new Region(0, 0, 28, 22);
            public static readonly Region TalkInput = new Region(32, 0, 330, 22);
            public static readonly Region TalkSend = new Region(366, 0, 54, 22);
            // transcript sheet (sheet-local): header, eight rows, footer hint
            public static readonly Region TalkLogHead = new Region(8, 4, 404, 16);
            public const float TalkLogRowX = 8f, TalkLogRowY0 = 24f, TalkLogRowStep = 22f,
                               TalkLogRowW = 404f, TalkLogRowH = 20f;
            public const int TalkLogRows = 8;
            public static readonly Region TalkLogHint = new Region(8, 202, 404, 18);

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
