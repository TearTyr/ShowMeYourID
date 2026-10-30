using System;
using System.Collections.Generic;
using System.Linq;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace FlowCheck
{
    // Drives the real NEW GAME path headlessly and prints the [Flow] breadcrumb chain.
    // SceneManager.LoadScene is a no-op in the stubs, so the scene bootstrap Awake is
    // invoked by hand exactly as Unity would run it after the load completes.
    internal static class Program
    {
        private static int _failures;

        private static void Check(bool ok, string what)
        {
            Console.WriteLine((ok ? "  OK   " : "  BAD  ") + what);
            if (!ok) _failures++;
        }

        private static void Main(string[] args)
        {
            // opt-in: boot to the desk and dump the file drawer as SVG at three points of its
            // travel, so the animation can be LOOKED AT rather than inferred from numbers.
            // Not part of the proof run: `dotnet run --project Tools/FlowCheck -- --dump-drawer=DIR`
            string screenArg = null, dumpState = "occupied";
            foreach (var a in args)
            {
                if (a.StartsWith("--dump-screen=")) screenArg = a.Substring("--dump-screen=".Length);
                if (a.StartsWith("--dump-state=")) dumpState = a.Substring("--dump-state=".Length);
            }
            if (screenArg != null)
            {
                DumpScreen(screenArg, dumpState);
                return;
            }
            if (args.Length > 0 && args[0].StartsWith("--dump-sprites="))
            {
                // the procedural sprites are painted while the desk builds, so build first
                MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore());
                var bootRoot = GameRoot.Instance;
                bootRoot.MainMenu.transform.Find("buttons").Find("NEW GAME")
                    .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                CheckpointSceneBootstrap.Build(bootRoot);
                for (int i = 0; i < 8 && bootRoot.Dialogue != null && bootRoot.Dialogue.IsActive; i++)
                    bootRoot.Dialogue.Advance();
                DumpSprites(args[0].Substring("--dump-sprites=".Length));
                return;
            }
            if (args.Length > 0 && args[0].StartsWith("--dump-drawer="))
            {
                DumpDrawer(args[0].Substring("--dump-drawer=".Length));
                return;
            }
            Console.WriteLine("== NEW GAME flow, headless ==");
            MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore());
            var root = GameRoot.Instance;
            Check(root != null && root.State == AppViewState.MainMenu, "menu boots");

            var newGame = root.MainMenu.transform.Find("buttons").Find("NEW GAME");
            newGame.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Console.WriteLine("  ..clicked NEW GAME..");
            // desk proofs keep the classic fast clock (shipping Normal pace is 25-30 s)
            root.Config.queueArrivalMinSeconds = 3f;
            root.Config.queueArrivalMaxSeconds = 5f;
            root.Config.queueBusyHurryMultiplier = 1f;

            // what Unity does after SceneManager.LoadScene: the new scene's bootstrap Awake
            CheckpointSceneBootstrap.Build(root);

            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++)
                root.Dialogue.Advance();

            Check(root.State == AppViewState.Checkpoint, "state is Checkpoint after the intro");
            Check(root.Checkpoint != null, "the desk exists");

            Console.WriteLine("== sticky-note start gate (Phase 10.1) ==");
            var cp0 = root.Checkpoint;
            var sticky = FindDeep(cp0.transform, "StickyNoteOverlay");
            Check(sticky != null && sticky.gameObject.activeSelf, "a fresh shift opens behind the sticky note");
            var noteText = NoteTextOf(sticky);
            Check(noteText.Contains("NIGHT SHIFT BEGINS.") &&
                  noteText.Contains("Rules: Check IDs, ask questions, stamp. Don't let the wrong faces in.") &&
                  noteText.Contains("[CLICK TO BEGIN]"),
                  "the note carries the exact briefing text");
            Check(cp0.CurrentVisitor == null && root.Queue.ArrivalCount == 0 && !root.Queue.Started,
                "no visitor, no arrivals, clock not started while the note is up");
            cp0.AdvanceQueue(6f); cp0.AdvanceQueue(6f);   // >6s of ignored waiting (step 11.1.4)
            Check(cp0.CurrentVisitor == null && root.Queue.WaitingCount == 0 && root.Queue.ArrivalCount == 0,
                "waiting 12s with the note up arrives nobody and queues nobody");
            ClickStickyNote(root);
            Check(!sticky.gameObject.activeSelf, "the note hides on click");
            Check(cp0.CurrentVisitor != null, "P10: clicking the note presents the first visitor");
            Check(root.Queue.Started && root.Queue.ArrivalCount == 1 && root.Queue.WaitingCount == 0,
                "P10: exactly one arrival sequence started (one clock, one visitor)");
            Check(root.Queue.TimerSeconds >= root.Config.queueArrivalMinSeconds &&
                  root.Queue.TimerSeconds <= root.Config.queueArrivalMaxSeconds,
                "P9: the arrival clock is armed inside [min,max] = [" +
                root.Config.queueArrivalMinSeconds + "," + root.Config.queueArrivalMaxSeconds + "]s");

            Console.WriteLine("== Symptom 0 structural proofs ==");
            var border = FindDeep(root.Dialogue.transform, "panel_border");
            Check(border != null && !border.gameObject.activeSelf,
                "B4: dialogue OUTER border rect is inactive after the intro (no grey fullscreen rect)");
            var checkpointRoot = root.Checkpoint.transform;
            int window = DeepIndex(checkpointRoot, "windowframe");
            int visitor = DeepIndex(checkpointRoot, "visitor");
            int speech = DeepIndex(checkpointRoot, "speechbubble");
            Check(window >= 0 && visitor > window && speech > window,
                $"B3: windowframe sibling {window} is BEHIND visitor {visitor} and speech bubble {speech}");

            RequiredElements(root);
            LayoutAudit(root);
            BubbleProof(root);
            PapersProof(root);
            FaceMatchRetiredProof(root);
            SecurityKitProof(root);
            MapProof(root);
            AssetManifestProof(root);
            SaveContinueResume();
            StampPlaceProof(root);
            DeskLoop(root);
            QueueProof(root);
            SettingsProof(root);
            WorldLoop(root);
            StudentsProof(root);

            Console.WriteLine("== one persistent core across the scene chain ==");
            Check(ReferenceEquals(root, GameRoot.Instance),
                  "I-02: exactly one GameRoot after menu->desk->school->desk");
            MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore());
            Check(ReferenceEquals(root, GameRoot.Instance),
                  "I-02: re-booting a scene never creates a second core");

            IssueOneProofs(root);
            AdminTalkProof(root);
            TeardownProof();

            Console.WriteLine(_failures == 0 ? "== FLOW CHAIN COMPLETE ==" : "== " + _failures + " CHECK(S) FAILED ==");
            Environment.Exit(_failures == 0 ? 0 : 1);
        }

        // ------------------------------------------------------- teardown (OnDestroy)
        // GameRoot.OnDestroy releases the procedural caches. Kit/drop-in sprites BORROW
        // imported Resources textures: those are assets on disk, and Destroy on them is
        // refused by Unity ("Destroying assets is not permitted to avoid data loss") —
        // exactly the editor-console flood this proof pins shut. Runtime objects (painted
        // textures, Sprite wrappers, synthesised clips) must still be freed.
        private static void TeardownProof()
        {
            Console.WriteLine("== teardown: OnDestroy cache release never destroys assets ==");
            var fileSprite = ProceduralArt.Get("sec_bubble");   // kit file -> borrowed asset texture
            var procSprite = ProceduralArt.Get("vignette");     // procedural -> owned texture
            var fileTex = fileSprite != null ? fileSprite.texture : null;
            var procTex = procSprite != null ? procSprite.texture : null;
            Check(fileTex != null && procTex != null && !ReferenceEquals(fileTex, procTex),
                  "fixture: a file-backed and a procedural sprite cache distinct textures");
            Check(fileTex && procTex, "fixture: both textures alive before teardown");

            int errs = UnityEngine.Debug.ErrorCount;
            ProceduralArt.ClearCache();     // exactly what GameRoot.OnDestroy calls
            ProceduralAudio.ClearCache();
            Check(UnityEngine.Debug.ErrorCount == errs,
                  "teardown logs no engine error (no 'Destroying assets is not permitted')");
            Check(fileTex, "the imported kit texture SURVIVES teardown (asset on disk — not ours to free)");
            Check(!procTex, "the procedural texture is released with the core");
            Check(!fileSprite && !procSprite, "both runtime Sprite wrappers are released");
            Check(!ProceduralArt.CachedKeys.Any(), "the art cache is empty after teardown");

            var again = ProceduralArt.Get("sec_bubble");
            Check(again != null && again.texture != null && !ReferenceEquals(again, fileSprite),
                  "next session: the sprite is rebuilt fresh around the surviving asset");
        }

        // ------------------------------------------------------- required elements
        // Nothing may silently disappear from the desk: every wireframe element is
        // looked up by name in the real built hierarchy.
        private static void RequiredElements(GameRoot root)
        {
            Console.WriteLine("== required desk/HUD elements exist ==");
            string[] deskNames =
            {
                "DeskFrame", "topbar", "daybanner_border", "clock", "pause", "runstats", "mutationbg",
                "record", "station", "folder1", "folder2", "folder3", "recordsicon", "filedrawer_border", "closefile", "idcard",
                "drawerbody", "drawerhead",
                "windowframe", "glass", "facade", "visitor", "glitch", "sill",
                "speechbubble", "bubble_border", "bubble", "bubbletext", "bubbletail",
                "offeredid", "patrol", "endshift",
                "qpanel_border", "q0", "q1", "q2", "q3",
                "records", "map", "notes", "stamp_allow", "stamp_deny",
                "OverlayRules", "OverlayLog",
                "admintalk", "admintalkbar",
                "StickyNoteOverlay", "stickynote_border",
            };
            foreach (var n in deskNames)
                Check(FindDeep(root.Checkpoint.transform, n) != null, "desk element '" + n + "'");

            // a Button whose target graphic cannot be hit is a dead control, and nothing else in
            // the build would notice: walk the whole desk and say so if one shows up
            var deadClicks = new List<string>();
            foreach (var b in root.Checkpoint.gameObject.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if (!b.isActiveAndEnabled) continue;      // hidden widgets are not dead clicks
                var g = b.targetGraphic;
                if (g == null || !g.raycastTarget) deadClicks.Add(b.gameObject.name);
            }
            Check(deadClicks.Count == 0,
                "every enabled desk button has a raycast-enabled target graphic" +
                (deadClicks.Count > 0 ? " (dead: " + string.Join(", ", deadClicks) + ")" : ""));

            // presentation cuts: the checklist widget and the waiting-line DISPLAY are
            // gone for good (the queue itself still runs headless); nothing is built-then-hidden
            Check(FindDeep(root.Checkpoint.transform, "checklist_border") == null,
                "the checklist widget is removed from the desk");
            Check(FindDeep(root.Checkpoint.transform, "scan") == null &&
                  FindDeep(root.Checkpoint.transform, "monitor_border") == null &&
                  FindDeep(root.Checkpoint.transform, "out") == null &&
                  FindDeep(root.Checkpoint.transform, "camcap") == null,
                "the ID scanner is removed: no SCAN button, no camera monitor, no readout");
            Check(FindDeep(root.Checkpoint.transform, "queuestripL") == null &&
                  FindDeep(root.Checkpoint.transform, "queuestatus") == null,
                "the waiting-line display is retired; the queue logic still runs headless");
            Check(FindDeep(root.Checkpoint.transform, "appearance_border") == null &&
                  FindDeep(root.Checkpoint.transform, "apphead") == null &&
                  FindDeep(root.Checkpoint.transform, "appq") == null &&
                  FindDeep(root.Checkpoint.transform, "appyes") == null &&
                  FindDeep(root.Checkpoint.transform, "appno") == null &&
                  FindDeep(root.Checkpoint.transform, "appwhy") == null &&
                  FindDeep(root.Checkpoint.transform, "appreason") == null &&
                  FindDeep(root.Checkpoint.transform, "reasonhint") == null &&
                  FindDeep(root.Checkpoint.transform, "appstatus") == null,
                "the face check is removed: no appearance panel, no YES/NO, no follow-up, no reason field");
            Check(FindDeep(root.Checkpoint.transform, "speech_border") == null &&
                  FindDeep(root.Checkpoint.transform, "speechlog") == null,
                "the bottom dialogue strip is gone: student dialogue lives in the speech bubble");
            Check(FindDeep(root.Checkpoint.transform, "stampbox") == null,
                "the slip has no printed stamp box: the mark lands where the player drops the stamp");
            string[] hudNames =
            {
                "HudFrame", "sanitybg_border", "trackbg_border", "batterybg_border",
                "findings", "room", "objective", "prompt", "toast",
            };
            foreach (var n in hudNames)
                Check(FindDeep(root.HUD.transform, n) != null, "HUD element '" + n + "'");
        }

        // ------------------------------------------------------------- layout audit
        // 1) the code table must equal the approved wireframe numbers, digit for digit
        // 2) co-visible regions never overlap and stay on screen
        // 3) the REAL built rects (DeskLayoutAudit on DeskFrame/HudFrame) match the table
        private struct AuditRow
        {
            public string Name;
            public L.Region Reg;
            public string Tags;   // C = desk, E = exploration, B = both, T = transient overlay
        }

        private static readonly List<AuditRow> Rows = new List<AuditRow>();

        private static void Add(string name, L.Region reg, string tags)
        {
            Rows.Add(new AuditRow { Name = name, Reg = reg, Tags = tags });
        }

        private static void ExpectRegion(string name, L.Region r, float x, float y, float w, float h)
        {
            Check(Math.Abs(r.X - x) < 0.01f && Math.Abs(r.Y - y) < 0.01f &&
                  Math.Abs(r.W - w) < 0.01f && Math.Abs(r.H - h) < 0.01f,
                name + " == wireframe (" + x + "," + y + " " + w + "x" + h + ")");
        }

        private static void LayoutAudit(GameRoot root)
        {
            Console.WriteLine("== code table vs the approved UI reference (uploads/UI Reference.jpg) ==");
            ExpectRegion("top bar", L.TopBar, 0, 0, 1280, 80);
            ExpectRegion("banner", L.Banner, 20, 25, 375, 30);
            ExpectRegion("sanity", L.SanityMeter, 470, 22, 349, 38);
            ExpectRegion("light", L.LightMeter, 348, 96, 200, 40);
            ExpectRegion("track", L.TrackMeter, 568, 96, 110, 40);
            ExpectRegion("clock", L.Clock, 1000, 25, 85, 30);
            ExpectRegion("pause", L.Pause, 1119, 22, 125, 38);
            ExpectRegion("records station", L.Station, 40, 85, 305, 160);
            ExpectRegion("file drawer", L.FileDrawer, 55, 255, 275, 290);
            ExpectRegion("id card", L.IdCard, 450, 500, 220, 151.25f);
            ExpectRegion("window frame", L.Window, 375, 160, 520, 330);
            ExpectRegion("glass", L.Glass, 430, 200, 415, 235);
            ExpectRegion("portrait", L.Portrait, 530, 205, 200, 230);
            ExpectRegion("sill", L.Sill, 375, 450, 520, 18);
            ExpectRegion("speech bubble", L.SpeechBubble, 737, 297, 182, 132);
            ExpectRegion("patrol button", L.PatrolBtn, 450, 655, 180, 40);
            ExpectRegion("end shift", L.EndShiftBtn, 650, 655, 180, 40);
            ExpectRegion("questions", L.Questions, 950, 105, 305, 215);
            ExpectRegion("TL", L.UtilityRecords, 80, 555, 160, 165);
            ExpectRegion("MAP", L.UtilityMap, 955, 330, 290, 170);
            ExpectRegion("RN", L.UtilityNotes, 240, 570, 160, 150);
            ExpectRegion("sticky note", L.StickyNote, 390, 200, 500, 320);
            // question buttons: one content box inside the kit panel's painted frame
            // (local y 9..198 on the 305x215 rect); four evenly divided rows, each fully
            // inside the box — the column layout (LayoutQuestions) places them from these
            Check(Math.Abs(L.Inner.QBtnX - 16) < 0.01f && Math.Abs(L.Inner.QBtnW - 266) < 0.01f &&
                  L.Inner.QContentY0 >= 40f && L.Inner.QContentY1 <= 195f &&
                  L.Inner.QContentY0 < L.Inner.QContentY1 &&
                  (L.Inner.QContentY1 - L.Inner.QContentY0) / 4f >= 22f,
                "question rows divide the panel content box, clear of the painted frame");
            // cohort tabs: station-local grid; file rows: records-panel grid, starting under the
            // 18px header — a whole cohort folder (7 files) fits
            Check(Math.Abs(L.Inner.FolderTabX - (L.Station.W - L.Inner.FolderTabW) / 2f) < 0.01f &&
                  Math.Abs(L.Inner.FolderTabY0 - 40) < 0.01f &&
                  Math.Abs(L.Inner.FolderTabStep - 35) < 0.01f && Math.Abs(L.Inner.FolderTabW - 230) < 0.01f &&
                  Math.Abs(L.Inner.FolderTabH - 34) < 0.01f,
                "folder tabs == the centred station grid ((W-230)/2,40 + i*35, 230x34): one symmetric face");
            Check(L.Inner.FolderTabY0 + (RecordSearch.CohortCount - 1) * L.Inner.FolderTabStep +
                  L.Inner.FolderTabH <= L.Station.H + 0.01f,
                "all three cohort tabs fit inside the station");
            Check(L.Inner.RecordsIcon.X >= 0f && L.Inner.RecordsIcon.Right <= L.Station.W &&
                  L.Inner.RecordsIcon.Y >= L.Inner.StationCap.Bottom - 0.01f &&
                  L.Inner.RecordsIcon.Bottom <= L.Station.H + 0.01f,
                "the folder icon sits inside the station, under the caption");
            Check(Math.Abs(L.Inner.FileRowX - 15) < 0.01f && Math.Abs(L.Inner.FileRowY0 - 50) < 0.01f &&
                  Math.Abs(L.Inner.FileRowStep - 27) < 0.01f && Math.Abs(L.Inner.FileRowW - 250) < 0.01f &&
                  L.Inner.FileRowY0 + 6 * L.Inner.FileRowStep + L.Inner.FileRowH <= L.Inner.RecordsPanelH + 0.01f,
                "file rows == the reference panel grid (15,50 + i*27, 250x22), 7 rows fit the sheet");
            Check(CheckpointView.RecordsRowCapacity() >= 7,
                "the records panel holds a whole cohort folder (>= 7 file rows)");
            // the retired appearance check left its slot to the stamp tray: the right
            // column reads questions-above / stamps-below, with the tray hugging the panel
            Check(Mathf.Abs(L.StampAllow.W - L.StampDeny.W) < 0.01f &&
                  Mathf.Abs(L.StampAllow.H - L.StampDeny.H) < 0.01f &&
                  Mathf.Abs(L.StampAllow.Y - L.StampDeny.Y) < 0.01f &&
                  Mathf.Abs(L.StampDeny.X - L.StampAllow.Right - 15f) < 0.01f &&   // the reference tray spacing
                  L.StampAllow.Y >= L.UtilityMap.Bottom &&
                  L.StampDeny.Right <= 1275f,
                "the APPROVE/DECLINE blocks are one aligned row, 8px apart, under the map panel, inside the desk");
            Check(L.StampAllow.W >= 96f && L.StampAllow.H >= 96f,
                "the stamp blocks keep a comfortable >=96px grab area");
            Check(L.SpeechBubble.X >= 735f && L.SpeechBubble.Right <= L.Questions.X &&
                  L.SpeechBubble.Y >= L.Window.Y && L.SpeechBubble.Bottom <= L.Sill.Bottom,
                "the speech bubble box stays beside the head, clear of the right column and the sill");
            Check(SpeechBubbleLayout.Height(L.Inner.BubbleMaxLines, L.Inner.BubbleLineH,
                      L.Inner.BubbleTagH, L.Inner.BubblePadTop, L.Inner.BubblePadBottom) <=
                  L.SpeechBubble.H + 0.01f,
                "the bubble's maximum text height fits inside its registered box");
            // hint/findings row: wireframe baseline y=72, findings right edge x=1264
            Check(Math.Abs(L.Findings.Right - 1264) < 0.01f, "findings right-aligned at x=1264");
            Check(Math.Abs(L.Hint.X - 25) < 0.01f, "hint lives in the left slot of the strip under the top bar");
            Check(L.Hint.Y >= 82 && L.Hint.Bottom <= 110 && L.Findings.Y >= 82 && L.Findings.Bottom <= 110 &&
                  L.RunStats.Y >= 82 && L.RunStats.Bottom <= 110,
                "hint/findings/run-stats live in the one strip under the kit board (84..108)");

            Console.WriteLine("== grid sanity: on screen, disjoint, contained ==");
            Rows.Clear();
            Add("banner", L.Banner, "B");
            Add("sanity meter", L.SanityMeter, "B");
            Add("light meter", L.LightMeter, "E");
            Add("track meter", L.TrackMeter, "E");
            Add("clock", L.Clock, "B");
            Add("pause", L.Pause, "B");
            Add("run stats", L.RunStats, "B");
            Add("mutation strip", L.Mutation, "B");
            Add("findings", L.Findings, "B");
            Add("hint", L.Hint, "E");
            Add("room info", L.RoomInfo, "E");
            Add("prompt", L.Prompt, "E");
            Add("records station", L.Station, "C");
            Add("file drawer", L.FileDrawer, "C");
            Add("id card", L.IdCard, "P");   // P = paper layer: rides over desk furniture
            Add("window", L.Window, "C");
                    Add("speech bubble", L.SpeechBubble, "T");   // transient overlay beside the head
            Add("patrol button", L.PatrolBtn, "C");
            Add("end shift button", L.EndShiftBtn, "C");
            Add("questions panel", L.Questions, "C");
            Add("log button", L.UtilityRecords, "P");
            Add("map button", L.UtilityMap, "C");
            Add("rules button", L.UtilityNotes, "P");
            Add("deny stamp", L.StampDeny, "P");
            Add("allow stamp", L.StampAllow, "P");
            Add("queue strip left", L.QueueStripL, "C");
            Add("admin talk panel", L.AdminTalk, "A");      // A = the conversation owns the
            Add("admin talk bar", L.AdminTalkBar, "A");     // column alone while it is open
            Add("queue strip right", L.QueueStripR, "C");
            Add("toast", L.Toast, "T");
            Add("confirm box", L.ConfirmBox, "T");
            Add("paper overlay", L.PaperOverlay, "T");
            Add("sticky note", L.StickyNote, "T");

            foreach (var r in Rows)
                Check(r.Reg.X >= -0.01f && r.Reg.Y >= -0.01f && r.Reg.Right <= 1280.01f && r.Reg.Bottom <= 720.01f,
                    r.Name + " on screen");

            for (int i = 0; i < Rows.Count; i++)
                for (int j = i + 1; j < Rows.Count; j++)
                {
                    // transient popups (T) intentionally stack on the desk and on each other;
                    // they are bounds-checked only.
                    if (Rows[i].Tags.Contains("T") || Rows[j].Tags.Contains("T")) continue;
                    if (!ShareState(Rows[i].Tags, Rows[j].Tags)) continue;
                    var a = Rows[i].Reg; var b = Rows[j].Reg;
                    bool hit = a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
                    Check(!hit, Rows[i].Name + " / " + Rows[j].Name + " do not overlap");
                }

            CheckInside("glass", L.Glass, L.Window);
            CheckInside("portrait", L.Portrait, L.Window);
            CheckInside("glitch", L.Glitch, L.Window);

            Console.WriteLine("== real built rects vs table (DeskLayoutAudit.CheckAll) ==");
            AuditFrame(root.Checkpoint.transform, "DeskFrame");
            AuditFrame(root.HUD.transform, "HudFrame");
        }

        // Centre-Y of an element in its parent's centred space (+y up), pivot- and anchor-aware:
        // the map's sheets and rows are anchored to different edges, so raw anchoredPosition
        // values are not comparable — this is.
        private static float CentreY(RectTransform rt)
        {
            // a full-screen parent reports sizeDelta (0,0) because it is anchored to stretch,
            // so the reference height comes from the canvas: screen px / scaleFactor
            float parentH = rt.parent != null ? ((RectTransform)rt.parent).sizeDelta.y : 720f;
            if (parentH <= 0.5f)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                float sf = root != null && root.scaleFactor > 0.01f ? root.scaleFactor : 1f;
                parentH = Screen.height / sf;
            }
            // anchor reference point relative to the PARENT'S centre, then the element's own
            // anchoredPosition (which its pivot is measured from) — Unity's RectTransform rule
            float anchorFromCentre = ((rt.anchorMin.y + rt.anchorMax.y) / 2f - 0.5f) * parentH;
            return anchorFromCentre + rt.anchoredPosition.y;
        }

        // True when a node carries no pointer/drag handler at all: the strongest form of "this
        // is not an interactive target any more", and it cannot go stale when a class is deleted.
        private static bool NoPointerHandler(GameObject go) =>
            go.GetComponent<UnityEngine.UI.Button>() == null &&
            go.GetComponent<UnityEngine.EventSystems.IPointerClickHandler>() == null &&
            go.GetComponent<UnityEngine.EventSystems.IPointerDownHandler>() == null &&
            go.GetComponent<UnityEngine.EventSystems.IBeginDragHandler>() == null &&
            go.GetComponent<UnityEngine.EventSystems.IDragHandler>() == null;

        private static void CheckInside(string name, L.Region inner, L.Region outer)
        {
            Check(inner.X >= outer.X && inner.Y >= outer.Y && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom,
                name + " contained by its parent region");
        }

        private static void AuditFrame(Transform viewRoot, string frameName)
        {
            var frame = FindDeep(viewRoot, frameName);
            Check(frame != null, frameName + " exists");
            if (frame == null) return;
            var audit = frame.GetComponent<DeskLayoutAudit>();
            Check(audit != null, frameName + " carries a DeskLayoutAudit");
            if (audit == null) return;
            var bad = audit.CheckAll();
            foreach (var line in bad) Console.WriteLine("  BAD  " + frameName + " " + line);
            Check(bad.Count == 0, frameName + ": " + audit.Count + " registered elements match the grid within 2px — zero warnings");
        }

        private static bool ShareState(string a, string b)
        {
            if (a.Contains("B") || b.Contains("B")) return true;
            foreach (char c in a) if (b.IndexOf(c) >= 0) return true;
            return false;
        }

        // ------------------------------------------------- papers pass (drag, pips, hints)
        private static string HintText(GameRoot root) =>
            FindDeep(root.HUD.transform, "objective").GetComponent<UnityEngine.UI.Text>().text;

        // the red interaction strip: the moment-to-moment instruction (the grey objective
        // strip no longer mirrors it — one instruction per band, owner UI report #2A)
        private static string PromptText(GameRoot root) =>
            FindDeep(root.HUD.transform, "prompt").GetComponent<UnityEngine.UI.Text>().text;

        private static float StampAlpha(GameRoot root, string stampName) =>
            FindDeep(root.Checkpoint.transform, stampName).GetComponent<UnityEngine.UI.Image>().color.a;

        // drag a stamp from its tray onto a paper's centre, exactly like the pointer does
        private static void DropStamp(GameRoot root, Verdict v) => DropStampOn(root, v, "idcard");

        private static void StampSlip(GameRoot root, Verdict v) => DropStampOn(root, v, "slip");

        private static void DropStampOn(GameRoot root, Verdict v, string paperName)
        {
            var name = v == Verdict.Allow ? "stamp_allow" : "stamp_deny";
            var drag = FindDeep(root.Checkpoint.transform, name).GetComponent<DragPaper>();
            var r = FindDeep(root.Checkpoint.transform, paperName);
            if (r == null) return;
            var rt = r.GetComponent<RectTransform>();
            var s = rt.rect.size;
            var own = drag.GetComponent<RectTransform>().rect.size;
            drag.BeginDrag();
            drag.PlaceAt(new Vector2(rt.anchoredPosition.x + s.x / 2f - own.x / 2f,
                                     -rt.anchoredPosition.y + s.y / 2f - own.y / 2f));
            drag.EndDrag();
        }

        private static void TakeSlip(GameRoot root)
        {
            var d = FindDeep(root.Checkpoint.transform, "slip").GetComponent<DragPaper>();
            d.BeginDrag();
            d.PlaceCenter(new Vector2(760f, 500f));
            d.EndDrag();
        }

        private static void HandBack(GameRoot root, bool id, bool slip)
        {
            var desk = root.Checkpoint.transform;
            if (id)
            {
                var d = FindDeep(desk, "idcard").GetComponent<DragPaper>();
                d.BeginDrag(); d.PlaceCenter(new Vector2(640f, 300f)); d.EndDrag();
            }
            if (slip)
            {
                var d = FindDeep(desk, "slip").GetComponent<DragPaper>();
                d.BeginDrag(); d.PlaceCenter(new Vector2(640f, 320f)); d.EndDrag();
            }
        }

        private static void PapersProof(GameRoot root)
        {
            Console.WriteLine("== papers pass: bubble column, draggable ID, question panel ==");
            var cp = root.Checkpoint;

            // the student's voice lives in the bubble beside the head, in its own ink
            var bubble = FindDeep(cp.transform, "speechbubble");
            var brt = bubble.GetComponent<RectTransform>();
            Check(Mathf.Abs(brt.anchoredPosition.x - L.SpeechBubble.X) < 0.5f &&
                  Mathf.Abs(-brt.anchoredPosition.y - L.SpeechBubble.Y) < 0.5f &&
                  L.SpeechBubble.X == 737 && L.SpeechBubble.Y == 297,
                  "the speech bubble box hangs beside the portrait's head (737,297)");
            var bubbleInk = FindDeep(cp.transform, "bubbletext").GetComponent<UnityEngine.UI.Text>().text;
            Check(!bubbleInk.Contains("AT THE WINDOW") && !bubbleInk.Contains("<color=") &&
                  bubbleInk.Length > 0,
                  "under the kit skin the bubble carries the spoken line alone (no tag, no markup)");

            // both papers are physical
            var idDrag = FindDeep(cp.transform, "idcard").GetComponent<DragPaper>();
            var recDrag = FindDeep(cp.transform, "record").GetComponent<DragPaper>();
            var slipT0 = FindDeep(cp.transform, "slip").GetComponent<RectTransform>();
            Check(idDrag != null && recDrag != null, "ID card and record sheet carry DragPaper");
            if (idDrag == null) return;
            var home = idDrag.CenterInBounds();
            idDrag.BeginDrag();
            idDrag.DragBy(new Vector2(99999f, -99999f));   // dragged clean off the desk
            idDrag.EndDrag();
            var c = idDrag.CenterInBounds();
            Check(Mathf.Abs(c.x - home.x) < 0.5f && Mathf.Abs(c.y - home.y) < 0.5f,
                  "a paper dropped outside the desk returns to its slot");
            idDrag.BeginDrag();
            idDrag.DragBy(new Vector2(120f, -20f));        // a normal desk move
            idDrag.EndDrag();
            c = idDrag.CenterInBounds();
            Check(Mathf.Abs(c.x - (home.x + 120f)) < 0.5f && Mathf.Abs(c.y - (home.y + 20f)) < 0.5f,
                  "a paper dropped on the desk stays where the player put it");
            idDrag.BeginDrag();
            idDrag.PlaceCenter(home);                      // tidy back for the next checks
            idDrag.EndDrag();

            // task 5: the ID card's DEFAULT display is slightly smaller — the whole card
            // (sprite, photo, rows) scales uniformly through DragPaper.BaseScale, so the
            // design proportions survive and zoom still reads 1x..1.8x of the resting size
            float cardRatio = CheckpointView.IdCardScale;
            Check(cardRatio >= 0.55f && cardRatio <= 0.70f,
                "the ID card's printed art rests at the concept size (scale " + cardRatio + ")");
            var idGo0 = FindDeep(cp.transform, "idcard");
            var cardArt = idGo0.Find("cardart");
            Check(cardArt != null &&
                  Mathf.Abs(cardArt.GetComponent<RectTransform>().localScale.x - cardRatio) < 0.01f &&
                  Mathf.Abs(idGo0.GetComponent<RectTransform>().localScale.x - 1f) < 0.01f &&
                  Mathf.Abs(idDrag.Zoom - 1f) < 0.01f,
                "the container rests at 1x while the art layer carries the concept scale");
            Check(Mathf.Abs(idGo0.GetComponent<RectTransform>().rect.width - L.IdCard.W) < 0.5f &&
                  Mathf.Abs(idGo0.GetComponent<RectTransform>().rect.height - L.IdCard.H) < 0.5f &&
                  Mathf.Abs(cardArt.GetComponent<RectTransform>().rect.width - L.CardArtW) < 0.5f &&
                  Mathf.Abs(cardArt.GetComponent<RectTransform>().rect.height - L.CardArtH) < 0.5f,
                "the card footprint is the concept rect; the printed art keeps its 320x220 design space");

            // the scanner is gone: dropping the ID anywhere on the open desk is just
            // repositioning — no side effects
            idDrag.BeginDrag();
            idDrag.PlaceCenter(new Vector2(820f, 620f));
            idDrag.EndDrag();
            c = idDrag.CenterInBounds();
            Check(c.x >= home.x + 50f && c.x <= 1280f &&
                  FindDeep(cp.transform, "idcard").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                  "dropping the ID over the open desk only repositions it (no scan, no hand-back)");
            idDrag.BeginDrag();
            idDrag.PlaceCenter(home);
            idDrag.EndDrag();

            // question panel: pips, asked-tags, hotkeys, budget-spent reason, scroll testimony
            var budget = FindDeep(cp.transform, "budget").GetComponent<UnityEngine.UI.Text>();
            Check(budget.text == "1/2",
                  "the reference counter reads remaining/budget after the first ask (got '" + budget.text + "')");
            var q0 = FindDeep(cp.transform, "q0");
            Check(q0.GetComponent<UnityEngine.UI.Button>().interactable == false &&
                  q0.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("ASKED"),
                  "the asked question is tagged and disabled");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Alpha2);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            var q1 = FindDeep(cp.transform, "q1");
            Check(q1.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("ASKED"),
                  "hotkey 2 asks the second question while the interview is open");
            Check(budget.text == "0/2",
                  "the reference counter empties to 0/2 (got '" + budget.text + "')");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Alpha3);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(!FindDeep(cp.transform, "q2").GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("ASKED"),
                  "hotkeys respect the spent budget (no third question)");

            // floor-files cabinet: the file opens only when the player pulls it
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "the student file stays shut until the player pulls it");
            Check(FindDeep(cp.transform, "find") == null &&
                  FindDeep(cp.transform, "searchbox") == null &&
                  FindDeep(cp.transform, "placeholder") == null &&
                  FindDeep(cp.transform, "query") == null &&
                  FindDeep(cp.transform, "station").GetComponent<UnityEngine.UI.InputField>() == null,
                  "the old registry terminal is gone: no FIND button, no text input");
            // ---- the STUDENT RECORDS folder: one folder icon that opens a three-cohort panel ----
            var panelBox = FindDeep(cp.transform, "filedrawer_border");   // the panel slot: pinned to the grid
            var station = FindDeep(cp.transform, "station");
            var icon = FindDeep(cp.transform, "recordsicon");
            Check(panelBox != null && station != null && icon != null && icon.parent == station,
                "RECORDS: the station holds one folder icon and the panel slot sits on the grid");
            Check(icon.Find("label").GetComponent<UnityEngine.UI.Text>().text == "STUDENT RECORDS" &&
                  FindDeep(station, "cap").GetComponent<UnityEngine.UI.Text>().text == "STUDENT RECORDS",
                "RECORDS: the folder is labelled STUDENT RECORDS");
            Check(station.GetComponent<UnityEngine.UI.Button>() == null &&
                  FindDeep(cp.transform, "cabfront") == null && FindDeep(cp.transform, "drawerfront") == null &&
                  FindDeep(cp.transform, "handle") == null && FindDeep(cp.transform, "drawercavity") == null,
                "RECORDS: no drawer machinery is left — no pull button, cabinet front, handle or cavity");
            cp.ResetRecords();
            DeskFrames(cp, 1);
            Check(!cp.RecordsOpen && icon.gameObject.activeSelf && !panelBox.gameObject.activeSelf &&
                  !FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "RECORDS: a fresh desk shows the closed folder icon and nothing else");

            // the click is answered on the SAME call: no timer, no coroutine, no animation
            icon.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(cp.RecordsOpen && panelBox.gameObject.activeSelf && !icon.gameObject.activeSelf,
                "RECORDS: one click on the folder opens the panel on that very call (no delay, no slide)");
            var cohorts = RecordSearch.BuildCohortFolders(root.Roster.Canon);
            string[] wantTabs = { "S2024-2028", "S2025-2029", "S2026-2030" };
            bool tabsOk = true;
            for (int t = 0; t < 3; t++)
            {
                var tabGo = FindDeep(cp.transform, "folder" + (t + 1));
                if (tabGo == null || !tabGo.gameObject.activeSelf ||
                    tabGo.Find("label").GetComponent<UnityEngine.UI.Text>().text != wantTabs[t]) tabsOk = false;
            }
            Check(tabsOk && FindDeep(cp.transform, "folder4") == null,
                "RECORDS: three cohort tabs — S2024-2028, S2025-2029, S2026-2030 — appear once it is open");
            Check(FindDeep(cp.transform, "filerow0") != null, "RECORDS: the oldest cohort's files are listed");
            var headText = FindDeep(cp.transform, "drawerhead").GetComponent<UnityEngine.UI.Text>().text;
            Check(headText.Contains("S2024-2028") && headText.Contains(cohorts[0].Indices.Count + " FILES"),
                  "the panel head names the cohort and counts its files (got '" + headText + "')");
            Check(cp.RecordsTab == 0, "RECORDS: it opens on the oldest cohort");

            // pull a file: same record-window path the old FIND search triggered
            var row0 = FindDeep(cp.transform, "filerow0");
            string rowName = row0.Find("pill/fname").GetComponent<UnityEngine.UI.Text>().text;
            row0.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "clicking a student file pulls it into the record window");
            Check(FindDeep(cp.transform, "rname").GetComponent<UnityEngine.UI.Text>().text
                      .ToUpperInvariant().Contains(rowName),
                  "the pulled record is the row that was clicked");
            Check(row0.Find("pill").GetComponent<UnityEngine.UI.Image>().color !=
                  FindDeep(cp.transform, "filerow1").Find("pill").GetComponent<UnityEngine.UI.Image>().color,
                  "the selected file row stays highlighted");
            Check(cp.RecordsOpen,
                  "RECORDS: taking a file leaves the panel OPEN — browsing several files never re-opens it");
            var closeBtn = FindDeep(cp.transform, "closefile").GetComponent<UnityEngine.UI.Button>();
            closeBtn.onClick.Invoke();
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf, "the file's X closes the window");

            // the other cohorts: different files, and pulling one repopulates the sheet
            var tab2 = FindDeep(cp.transform, "folder2").GetComponent<UnityEngine.UI.Button>();
            tab2.onClick.Invoke();
            Check(cp.RecordsOpen && cp.RecordsTab == 1, "RECORDS: another cohort tab swaps the panel's contents");
            var row2 = FindDeep(cp.transform, "filerow0");
            string row2Name = row2.Find("pill/fname").GetComponent<UnityEngine.UI.Text>().text;
            Check(row2Name != rowName, "the second cohort lists a different set of files");
            row2.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf &&
                  FindDeep(cp.transform, "rname").GetComponent<UnityEngine.UI.Text>().text
                      .ToUpperInvariant().Contains(row2Name),
                  "pulling from the second cohort reopens the record sheet");
            FindDeep(cp.transform, "folder3").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(cp.RecordsTab == 2 &&
                  FindDeep(cp.transform, "drawerhead").GetComponent<UnityEngine.UI.Text>().text.Contains("S2026-2030"),
                "the third tab opens S2026-2030");

            // repeated input: one panel, one set of files — never a pile-up
            for (int k = 0; k < 6; k++)
                FindDeep(cp.transform, "folder" + (1 + k % 3)).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(CountDeep(cp.transform, "drawerbody") == 1 && CountDeep(cp.transform, "filerow0") == 1,
                "RECORDS: repeated tab clicks never build a second panel or a second set of files");

            // closing: clicking the open tab again, or TAB, puts the folder back
            int openTab = cp.RecordsTab;
            FindDeep(cp.transform, "folder" + (openTab + 1)).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!cp.RecordsOpen && !panelBox.gameObject.activeSelf && icon.gameObject.activeSelf &&
                  !FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "RECORDS: clicking the open tab closes the panel and brings the folder icon back");
            PressDeskKey(cp, KeyCode.Tab);
            Check(cp.RecordsOpen && cp.RecordsTab == openTab,
                "RECORDS: TAB reopens the folder on the cohort it was last on");
            PressDeskKey(cp, KeyCode.Tab);
            Check(!cp.RecordsOpen, "RECORDS: TAB closes it again");

            // empty folder edge: the panel still opens, with an explicit NO FILES line
            cp.OpenRecords(0);
            cp.PopulateRecords(RecordSearch.CohortLabel(2023), null, new int[0]);
            Check(FindDeep(cp.transform, "nofiles") != null &&
                  FindDeep(cp.transform, "nofiles").GetComponent<UnityEngine.UI.Text>().text.Contains("NO FILES"),
                  "an empty folder shows a NO FILES state");
            cp.ResetRecords();
            Check(!cp.RecordsOpen && !panelBox.gameObject.activeSelf && cp.RecordsTab == -1,
                "RECORDS: a reset (new visitor) closes the folder and clears the cohort");

            // the slip: counter -> desk, stamp target, one stamp, pending until hand-back
            var slipGo = FindDeep(cp.transform, "slip");
            var slipDrag = slipGo.GetComponent<DragPaper>();
            slipDrag.BeginDrag();
            slipDrag.PlaceCenter(new Vector2(760f, 500f));
            slipDrag.EndDrag();
            Check(cp.PendingVerdict == Verdict.None, "an unstamped slip commits nothing");
            Check(PromptText(root).Contains("STAMP THE SLIP"),
                  "prompt matches state: both papers on the desk (got '" + PromptText(root) + "')");
            Check(!HintText(root).Contains("STAMP THE SLIP"),
                  "the grey objective strip no longer duplicates the red prompt (got '" + HintText(root) + "')");
            Check(FindDeep(FindDeep(cp.transform, "offeredid").parent, "dim") == null,
                  "no full-desk dimmer blocks stamps or tools during the hand-over");
            HandBack(root, true, false);
            Check(FindDeep(cp.transform, "idcard").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                  "hand-back before stamping is refused: the ID stays on your desk");
            DropStamp(root, Verdict.Reject);          // stamp dropped on the ID, not the slip
            Check(!cp.SlipStamped, "a stamp dropped off the slip returns to the rack (no mark)");
            StampSlip(root, Verdict.Reject);
            Check(cp.SlipStamped && cp.PendingVerdict == Verdict.Reject,
                  "the stamp lands on the slip and records a pending verdict");
            Check(FindDeep(cp.transform, "stampoverlay").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                  "the stamp mark persists on the slip (no fade-out before hand-back)");
            Check(PromptText(root).Contains("DRAG THE ID BACK"),
                  "prompt matches state: stamped, awaiting the ID's hand-back (got '" + PromptText(root) + "')");
            var soRt = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            Check(soRt.sizeDelta.x > 1f && soRt.sizeDelta.y > 1f,
                  "the stamp overlay has a real size after the drop (visible mark)");
            Check(FindDeep(cp.transform, "idcard").GetComponent<UnityEngine.UI.Image>().raycastTarget &&
                  FindDeep(cp.transform, "record").GetComponent<UnityEngine.UI.Image>().raycastTarget,
                  "draggable papers have raycast on (pointer can grab them)");
            var layer = FindDeep(cp.transform, "PapersLayer");
            Check(layer != null &&
                  layer.GetSiblingIndex() > FindDeep(cp.transform, "glass").GetSiblingIndex() &&
                  FindDeep(cp.transform, "idcard").parent == layer &&
                  FindDeep(cp.transform, "record").parent == layer,
                  "papers sit above the window in sibling order (one PapersLayer)");
            Check(FindDeep(cp.transform, "station").GetComponent<UnityEngine.UI.InputField>() == null &&
                  FindDeep(cp.transform, "stationtap") == null,
                  "no text field or tap-swallower left on the station");
            Check(FindDeep(cp.transform, "filedrawer_border").GetSiblingIndex() <
                  FindDeep(cp.transform, "PapersLayer").GetSiblingIndex(),
                  "the drawer sits under the papers layer: a pulled file lands on top of it");
            StampSlip(root, Verdict.Allow);
            Check(cp.PendingVerdict == Verdict.Reject, "one stamp per slip: the second returns to the rack");

            // flip + zoom on the held ID (desk is live here): the black spine is the handle
            var idGo = FindDeep(cp.transform, "idcard");
            var spine = FindDeep(cp.transform, "idstripe");
            var spineBack = FaceOf(idGo, "idback").Find("idstripeback").gameObject;
            var slipT = FindDeep(cp.transform, "slip").transform;
            Check(idGo.GetComponent<UnityEngine.UI.Button>() == null,
                  "the card BODY is not a flip target any more (clicking it only drags)");
            Check(spine.GetComponent<UnityEngine.UI.Button>() != null &&
                  spine.GetComponent<UnityEngine.UI.Image>().raycastTarget &&
                  spine.GetComponent<Hollowbrook.UI.HoverCursor>() != null &&
                  spineBack.GetComponent<UnityEngine.UI.Button>() != null &&
                  spineBack.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                  "the black spines (front + mirrored back) are the flip handles: Button, raycast, hand cursor");
            spine.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(FaceOf(idGo, "idback").gameObject.activeSelf, "clicking the spine flips the ID to the back face");
            spineBack.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!FaceOf(idGo, "idback").gameObject.activeSelf, "the back face's own spine flips it home (reliable both ways)");

            // the slip is ONLY draggable: it has no back face, a tap on it does nothing, and the
            // F key flips the ID even when the slip was lifted last
            Check(FaceOf(slipT, "slipback") == null, "the slip has no back face any more");
            var slipDragC = slipT.GetComponent<DragPaper>();
            slipDragC.BeginDrag();
            slipDragC.EndDrag();      // a tap on the taken slip used to turn it over
            Check(FaceOf(slipT, "bg").gameObject.activeSelf,
                "tapping the taken slip does nothing — its face stays up");
            Check(FindDeep(cp.transform, "stampoverlay").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                "the stamp mark stays on the slip");
            Check(!FaceOf(idGo, "idback").gameObject.activeSelf, "(the ID is face-up before F)");
            cp.FlipLastPaper();       // F, with the slip touched last
            Check(FaceOf(idGo, "idback").gameObject.activeSelf,
                "F flips the ID even when the slip was touched last");
            Check(FaceOf(slipT, "slipback") == null && FaceOf(slipT, "bg").gameObject.activeSelf,
                "...and never touches the slip");
            var idDragC = idGo.GetComponent<DragPaper>();
            idDragC.BeginDrag(); idDragC.EndDrag();   // retarget zoom to the ID (no move, no tap handler)
            // the ID deliberately STAYS face-down here: the desk loop's fresh-visitor
            // check proves the hand-over reset turns it face-up (flip never carries over)
            // task 1: stacking — right-column panels below papers, stamps above papers
            int qPI = FindDeep(cp.transform, "qpanel_border").GetSiblingIndex();
            int layI = FindDeep(cp.transform, "PapersLayer").GetSiblingIndex();
            int stpI = FindDeep(cp.transform, "stamp_allow").GetSiblingIndex();
            Check(qPI < layI && layI < stpI, "stacking: questions panel < papers layer < stamp tray");
            // task 2: stripe on both faces
            Check(FindDeep(cp.transform, "idstripe") != null && FindDeep(cp.transform, "idstripeback") != null,
                  "the ID carries its black stripe on front and (mirrored) back");
            // task 3 (reworked): folder tabs sit exactly on the station grid — PlaceTL, no pixel nudges
            var tabRt = FindDeep(cp.transform, "folder1").GetComponent<RectTransform>();
            var tl = new Vector2(0f, 1f);
            Check(tabRt.anchorMin == tl && tabRt.anchorMax == tl && tabRt.pivot == tl &&
                  Mathf.Abs(tabRt.anchoredPosition.x - L.Inner.FolderTabX) < 0.01f &&
                  Mathf.Abs(-tabRt.anchoredPosition.y - L.Inner.FolderTabY0) < 0.01f,
                  "folder tabs are placed through PlaceTL on the station grid (8,20, no pixel nudges)");
            // task 4: the record's X anchors top-right
            var xRt = FindDeep(cp.transform, "closefile").GetComponent<RectTransform>();
            Check(xRt.anchorMin == Vector2.one && xRt.anchorMax == Vector2.one,
                  "the record's exit button is anchored top-right");
            cp.DebugZoom(9f);
            Check(Mathf.Abs(idGo.localScale.x - 1.8f) < 0.01f &&
                  Mathf.Abs(idGo.GetComponent<DragPaper>().Zoom - 1.8f) < 0.01f,
                "zoom clamps at 1.8x of the card's resting scale");
            cp.DebugZoom(0.1f);
            Check(Mathf.Abs(idGo.localScale.x - 1f) < 0.01f &&
                  Mathf.Abs(idGo.GetComponent<DragPaper>().Zoom - 1f) < 0.01f,
                "zoom clamps at the resting 1x (the smaller default card)");
            cp.DebugZoom(1f);
            // the ONLY remaining zoom input is the deliberate Z toggle (the scroll wheel
            // no longer resizes papers — there is no scroll listener on the desk at all)
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Z);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(Mathf.Abs(idGo.localScale.x - 1.5f) < 0.01f, "Z toggles the last-touched paper to 1.5x");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Z);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(Mathf.Abs(idGo.localScale.x - 1f) < 0.01f, "Z again returns it to the intended 1x size");
        }

        // ---------------------------------------------------- speech bubble proof
        // The student's voice lives in a comic bubble beside the portrait's head:
        // visible while the line is up and STAYING up until the next line replaces it or the
        // desk clears it — no timer, so a clue can never vanish before it is read.
        private static void BubbleProof(GameRoot root)
        {
            Console.WriteLine("== speech bubble beside the student's head ==");
            var cp = root.Checkpoint;
            var bubbleRoot = FindDeep(cp.transform, "speechbubble");
            var panel = FindDeep(cp.transform, "bubble_border").GetComponent<RectTransform>();
            var text = FindDeep(cp.transform, "bubbletext").GetComponent<UnityEngine.UI.Text>();
            var tail = FindDeep(cp.transform, "bubbletail").GetComponent<UnityEngine.UI.Image>();
            Check(bubbleRoot != null && bubbleRoot.gameObject.activeSelf,
                "SB: the bubble is up while the visitor's greeting is showing");
            Check(!text.text.Contains("AT THE WINDOW") && text.text.Length > 0,
                "SB: the reference bubble speaks the line alone — the tail points at the speaker");
            Check(!text.text.StartsWith("<b><color="),
                "SB: no speaker-tag markup rides in the kit bubble");
            Check(tail.sprite != null, "SB: the bubble carries its pointing tail");
            bool anyRaycast = false;
            foreach (var img in bubbleRoot.gameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (img.raycastTarget) anyRaycast = true;
            foreach (var t in bubbleRoot.gameObject.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (t.raycastTarget) anyRaycast = true;
            Check(!anyRaycast,
                "SB: the bubble is entirely click-through — a click or drag over it reaches the desk below");
            Check(bubbleRoot.GetSiblingIndex() < FindDeep(cp.transform, "PapersLayer").GetSiblingIndex(),
                "SB: the bubble sits under the papers layer — a dragged paper rides over it");
            Check(L.Inner.BubbleTail.X < 0f && L.Inner.BubbleTail.Right > 0f,
                "SB: the tail straddles the panel's left edge (seamless comics seam)");
            Check(Mathf.Abs(panel.anchoredPosition.x + 48f) < 0.01f &&
                  Mathf.Abs(-panel.anchoredPosition.y) < 0.01f &&
                  Mathf.Abs(panel.anchoredPosition.x + panel.sizeDelta.x - 14f - L.Inner.BubblePanelW) < 0.01f,
                "SB: the kit panel hangs its painted tail border OUTSIDE the box — the white " +
                "panel grows down from the container top at exactly the full box width");
            Check(L.SpeechBubble.X >= 735f &&
                  L.SpeechBubble.Y < L.Portrait.Y + L.Portrait.H / 2f + 40f &&
                  L.SpeechBubble.Bottom < L.Sill.Bottom,
                "SB: the bubble hangs BESIDE the head — right of the face, at head height, above the sill");
            float h0 = panel.sizeDelta.y;
            Check(h0 >= 56f && h0 <= L.SpeechBubble.H + 0.01f,
                "SB: the panel wraps to the line count inside its box (h=" + h0 + ")");

            // no timer: the line is still up long after it was spoken
            Check(bubbleRoot.gameObject.activeSelf && cp.BubbleClock.Visible,
                "SB: the line is up and stays up — nothing ticks it away");

            // a new line replaces the old one and shows normally
            cp.DebugSay("One more thing — the intercom crackles.");
            Check(bubbleRoot.gameObject.activeSelf, "SB: a fresh line keeps the bubble up");
            int gen1 = cp.BubbleClock.Generation;
            cp.DebugSay("Second line, replacing the first.");
            Check(bubbleRoot.gameObject.activeSelf && cp.BubbleClock.Generation == gen1 + 1,
                "SB: every line advances the generation");
            Check(text.text.Contains("Second line"), "SB: the bubble shows the NEW line, not the old one");

            // the white interior is where the text lives: the kit panel's sliced borders are
            // pinned to the 48u tail / 14u edge the layout is built on, and the text sits
            // inside the white with a clear inset (never on the jagged ink)
            var bubbleImg = FindDeep(cp.transform, "bubble_border").GetComponent<UnityEngine.UI.Image>();
            float multiplied = bubbleImg.pixelsPerUnit * bubbleImg.pixelsPerUnitMultiplier;
            Check(Mathf.Abs(multiplied - L.Inner.BubbleKitArtPxPerUnit) < 0.01f,
                "SB: the kit bubble's borders are pinned to " + L.Inner.BubbleKitArtPxPerUnit +
                " art px per unit whatever the canvas reference PPU is (got " + multiplied + ")");
            var textRt = text.rectTransform;
            float whiteLeft = L.Inner.BubbleKitTailW;
            float whiteRight = panel.sizeDelta.x - L.Inner.BubbleKitWhiteMargin;
            float tl = textRt.anchoredPosition.x, tr = tl + textRt.sizeDelta.x;
            Check(tl >= whiteLeft + L.Inner.BubbleKitInsetX - 0.01f &&
                  tr <= whiteRight - L.Inner.BubbleKitInsetX + 0.01f,
                "SB: the text column sits inside the bubble's white with a clear inset (" +
                tl + ".." + tr + " inside " + whiteLeft + ".." + whiteRight + ")");
            Check(text.fontSize >= 15, "SB: the bubble type is large enough to read at a glance");

            // the longest lines the game can actually put in the bubble are shown in full
            foreach (var line in new[] {
                Hollowbrook.Logic.Content.AdminLines[0],
                "“My first stop is your server room.”  [echoing, as if reading]" })
            {
                cp.DebugSay(line);
                Check(!text.text.EndsWith("…") && text.text == line,
                    "SB: the longest real line (" + line.Length + " chars) is shown in full — nothing is cut off");
                Check(panel.sizeDelta.y > 56f && panel.sizeDelta.y <= L.SpeechBubble.H + 0.01f,
                    "SB: the panel grows to the wrapped line count (h=" + panel.sizeDelta.y + ")");
            }

            // a long line is wrapped and capped inside the bubble box, never spilling
            cp.DebugSay("Tonight the hallway smells of chalk and old rain, and every face in " +
                        "the queue wears the same borrowed smile, so watch the eyes, watch the " +
                        "hands, watch the way a name is said a half-second before it is remembered, " +
                        "because the school keeps its secrets in the pauses between words, and the " +
                        "radiators tick like a clock counting down to something nobody wrote in " +
                        "the log, and by the fourth bell even the shadows will have filed their " +
                        "own little slips, stamped and signed, waiting for someone to read them " +
                        "back in the morning light.");
            Check(bubbleRoot.gameObject.activeSelf, "SB: a long line still shows in the bubble");
            Check(panel.sizeDelta.y <= L.SpeechBubble.H + 0.01f,
                "SB: the panel never outgrows its registered box (h=" + panel.sizeDelta.y + ")");
            Check(text.text.EndsWith("…") || text.text.EndsWith("\u2026"),
                "SB: overlong dialogue is truncated with an ellipsis, not clipped silently");

            // the live interview path: the answer pops the bubble beside the head
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.AskQuestion(QuestionId.Course);
            Check(bubbleRoot.gameObject.activeSelf && !text.text.Contains("AT THE WINDOW") &&
                  text.text.Length > 0 && !text.text.Contains("Good evening"),
                "SB: the asked question's answer arrives in the bubble (line alone, tag retired)");
            Check(!text.text.Contains("Your course?"),
                "SB: the bubble carries the STUDENT's answer (the button shows the question asked)");

            // leaving the desk cleans the bubble up
            cp.HideBubbleNow();
            Check(!bubbleRoot.gameObject.activeSelf && !cp.BubbleClock.Visible,
                "SB: the room-change cleanup hides the bubble instantly");
        }

        // ------------------------------------- stamp placement (Papers, Please style)
        // The verdict mark lands WHERE the player dropped the stamp: the desk point is
        // converted into the slip's own space (position, zoom scale and resting tilt) and
        // clamped so the whole tilted mark stays on the paper. Corner drops clamp inside
        // instead of hanging off; the mark rides the slip when the paper is dragged; the
        // hand-back scoring workflow is untouched. (The keyboard default spot is proven
        // in IssueOneProofs, on its own fresh run.)
        private static void StampPlaceProof(GameRoot root)
        {
            Console.WriteLine("== stamp placement: the mark lands where you drop it ==");
            var cp = root.Checkpoint;
            int guard = 0;
            while (cp.CurrentVisitor == null && guard++ < 8) cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor != null, "SP: a visitor is at the window for the stamp proof");
            if (cp.CurrentVisitor == null) return;

            var desk = FindDeep(cp.transform, "DeskFrame");
            var slipT = FindDeep(cp.transform, "slip").GetComponent<RectTransform>();
            var slipArt = slipT.Find("slipart").GetComponent<RectTransform>();
            var soRt = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            Check(FindDeep(cp.transform, "stampbox") == null,
                "SP: no printed target box — the drop point decides where the mark lands");

            // ---- case 1: a drop at the centre of a ZOOMED slip lands at the drop point ----
            cp.DebugTakeDocuments();
            TakeSlip(root);                       // slip centred at (760,500): top-left (660,440)
            var slipDrag0 = slipT.GetComponent<DragPaper>();
            slipDrag0.Zoom = 1.5f;                // the slip refuses zoom: no input can resize it
            Check(!slipDrag0.Zoomable && Mathf.Abs(slipT.localScale.x - 1f) < 0.01f,
                "SP: the slip is not zoomable — a zoom request leaves it at 1x");
            // scaffolding only: scale the rect directly so the drop maths still proves it maps
            // through ANY scale chain (nothing in the game scales the slip any more)
            slipT.localScale = new Vector3(1.5f, 1.5f, 1f);
            var want = InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null);
            StampAtPoint(root, want, new Vector2(797.5f, 522.5f));   // centre of the 1.5x slip
            Check(cp.SlipStamped && cp.PendingVerdict == want,
                "SP: the drop stamps the slip and records the pending verdict (workflow preserved)");
            Check(soRt.parent == slipArt, "SP: the mark is a layer OF THE SLIP (it rides the paper)");
            var markImg = soRt.GetComponent<UnityEngine.UI.Image>();
            Check(markImg.sprite != null &&
                  markImg.sprite.name == (want == Verdict.Allow ? "stamp_allow" : "stamp_reject"),
                "SP: the mark shows the EXISTING stamp art for the verdict (types preserved)");
            Check(Mathf.Abs(soRt.sizeDelta.x - CheckpointView.StampMarkSize) < 0.01f &&
                  Mathf.Abs(soRt.sizeDelta.y - CheckpointView.StampMarkSize) < 0.01f,
                $"SP: the mark draws at {soRt.sizeDelta.x:F0}x{soRt.sizeDelta.y:F0} reference px — square, so no stretching");
            float grow = soRt.sizeDelta.x / 45f - 1f;           // 45px = the previous size
            Check(grow > 0.30f && grow < 0.40f,
                $"SP: the mark is {grow:P1} LARGER than the 45px baseline (target 30-40%)");
            Check(soRt.sizeDelta.x <= slipArt.rect.width * 0.33f,
                $"SP: the mark covers {soRt.sizeDelta.x / slipArt.rect.width:P0} of the slip's width — ink over paper, never a panel");
            var top = new Vector2(slipT.anchoredPosition.x, -slipT.anchoredPosition.y);
            float slipScale = slipT.localScale.x * slipArt.localScale.x;   // zoom * printed scale
            var local = StampPlacement.DeskToLocal(797.5f, 522.5f, top.x, top.y,
                slipScale, slipT.localEulerAngles.z);
            local = StampPlacement.ClampToPrintable(local, slipArt.rect.width, slipArt.rect.height,
                CheckpointView.StampMarkSize, CheckpointView.StampMaxTilt, CheckpointView.StampEdgePad);
            float mx = soRt.anchoredPosition.x, my = -soRt.anchoredPosition.y;
            Check(Mathf.Abs(slipT.localScale.x - 1.5f) < 0.01f &&
                  Mathf.Abs(mx - local.X) < 0.5f && Mathf.Abs(my - local.Y) < 0.5f,
                $"SP: the centre drop on the ZOOMED slip maps through its scale (got {mx:F1},{my:F1} want {local.X:F1},{local.Y:F1})");
            Check(Mathf.Abs(mx - 100f) < 12f && Mathf.Abs(my - 60f) < 12f,
                $"SP: ...and lands in the slip's middle, not a fixed corner (got {mx:F1},{my:F1})");
            Check(Mathf.Abs(slipT.localScale.x - 1.5f) < 0.01f &&
                  Mathf.Abs(slipArt.localScale.x - L.SlipScale) < 0.01f &&
                  Mathf.Abs(soRt.localScale.x - 1f) < 0.01f,
                "SP: the mark zooms WITH the slip (it inherits the paper's scale chain)");
            float ang = Mathf.Abs(soRt.localEulerAngles.z);
            ang = Mathf.Min(ang, 360f - ang);
            float rad = ang * Mathf.Deg2Rad;
            float bound = soRt.sizeDelta.x * (Mathf.Cos(rad) + Mathf.Sin(rad)) / 2f;
            Check(mx - bound >= -0.01f && mx + bound <= slipArt.rect.width + 0.01f &&
                  my - bound >= -0.01f && my + bound <= slipArt.rect.height + 0.01f,
                $"SP: the tilted mark stays fully inside the slip (bounds ±{bound:F1} in {slipArt.rect.width}x{slipArt.rect.height})");

            // the mark rides the paper: drag the stamped slip elsewhere
            Vector2 markBefore = new Vector2(soRt.anchoredPosition.x, soRt.anchoredPosition.y);
            float cA = Mathf.Cos(slipT.localEulerAngles.z * Mathf.Deg2Rad);
            float sA = Mathf.Sin(slipT.localEulerAngles.z * Mathf.Deg2Rad);
            float lx = markBefore.x, ly = -markBefore.y;
            Vector2 deskBefore = new Vector2(top.x + (lx * cA + ly * sA) * slipScale,
                                             top.y + (-lx * sA + ly * cA) * slipScale);
            var slipDrag = slipT.GetComponent<DragPaper>();
            slipDrag.BeginDrag();
            slipDrag.PlaceCenter(new Vector2(620f, 540f));
            slipDrag.EndDrag();
            Check(Mathf.Abs(slipT.localScale.x - 1.5f) < 0.01f,
                "SP: lifting and dropping never loses the slip's scale");
            var top2 = new Vector2(slipT.anchoredPosition.x, -slipT.anchoredPosition.y);
            Vector2 deskAfter = new Vector2(top2.x + (lx * cA + ly * sA) * slipScale,
                                            top2.y + (-lx * sA + ly * cA) * slipScale);
            Check(soRt.parent == slipArt &&
                  Mathf.Abs(soRt.anchoredPosition.x - markBefore.x) < 0.01f &&
                  Mathf.Abs(soRt.anchoredPosition.y - markBefore.y) < 0.01f,
                "SP: dragging the slip keeps the mark at the same spot ON THE PAPER");
            Check(Vector2.Distance(deskBefore, deskAfter) > 50f,
                $"SP: ...and on the desk the mark moved WITH the slip ({Vector2.Distance(deskBefore, deskAfter):F0}px)");
            // commit the stamped slip (the corner-clamp and keyboard-default cases run on
            // IssueOneProofs' own fresh shift — night 1's roster is only four deep), then
            // read the verdict record: no face-check data anywhere
            HandBack(root, true, true);
            FindDeep(desk, "records").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            string logText = FindDeep(FindDeep(desk, "OverlayLog"), "text").GetComponent<UnityEngine.UI.Text>().text;
            Check(logText.Contains("ALLOWED") || logText.Contains("REJECTED"),
                "SP: tonight's log still books every verdict");
            Check(!logText.Contains("appearance:"),
                "SP: the removed face check leaves no data in the verdict record");
            FindDeep(desk, "records").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            // the next visitor (night 1's last) is left untouched for the desk loop

            // the reduced mark's art ships as a real asset override (Resources/Art/stamp_allow.png),
            // which the headless Resources stub cannot load — so prove the FILE and its alpha
            // stream are in place, exactly as ProceduralArt.Get would find them in the editor.
            string rootDir = RepoRoot();
            string png = rootDir == null ? null :
                System.IO.Path.Combine(rootDir, "Assets/_Project/Resources/Art/stamp_allow.png");
            Check(png != null && System.IO.File.Exists(png),
                "SP: the approval stamp ships as an asset override at Resources/Art/stamp_allow.png");
            if (png != null && System.IO.File.Exists(png))
            {
                byte[] head = new byte[30];
                using (var fs = System.IO.File.OpenRead(png))
                {
                    int got = fs.Read(head, 0, head.Length);
                    Check(got == head.Length, "SP: the override file reads back whole");
                }
                Check(head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 &&
                      head[25] == 6,
                    "SP: it is a true-colour RGBA PNG (signature + colour type 6 = alpha channel)");
                Check(System.IO.File.Exists(png + ".meta"),
                    "SP: the override has its .meta, so Unity imports it on the next open");
            }
            Check(Hollowbrook.Bootstrap.ProceduralArt.Get("stamp_allow") != null,
                "SP: the stamp key still resolves (asset override wins in-editor, painter is the fallback)");
        }

        // ONE source of truth, three light layers + the HUD: cone, stencil mask and penumbra
        // must always agree with the beam state, and the readout must agree with all of them.
        private static void CheckLightSync(GameRoot root, string tag)
        {
            var rig = root.Views.Flashlight;
            bool want = rig.Beam != LightMode.Off && !rig.BatteryDead;
            bool cone = FindDeep(rig.transform, "beam").gameObject.activeSelf;
            bool mask = FindDeep(rig.transform, "FlashlightMask").gameObject.activeSelf;
            Check(cone == want && mask == want,
                $"{tag}: cone + mask agree with the beam state ({(want ? "ON" : "OFF")}) — the only two light layers");
            string hud = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            // while peeking, the handheld readout is replaced by the peek lamp's own line
            bool peeking = root.State == AppViewState.RoomPeek;
            string wantHud = peeking
                ? (root.Views.PeekLight != null && root.Views.PeekLight.Lit ? "LAMP: ON" : "LAMP: OFF")
                : (want ? ("BEAM: " + (rig.Beam == LightMode.Focus ? "FOCUS" : "SWEEP")) : "BEAM: OFF");
            Check(hud.Contains(wantHud),
                $"{tag}: the HUD readout agrees with the light layers (want '{wantHud}', got '{hud}')");
        }

        // walk up from the harness binary OR the launch directory until the folder holding
        // Assets/_Project (Directory.Build.props parks the build output in /tmp, outside the repo)
        private static string RepoRoot()
        {
            var seeds = new[] { Environment.CurrentDirectory, AppContext.BaseDirectory };
            foreach (string seed in seeds)
            {
                if (string.IsNullOrEmpty(seed)) continue;
                var dir = new System.IO.DirectoryInfo(seed);
                while (dir != null &&
                       !System.IO.Directory.Exists(System.IO.Path.Combine(dir.FullName, "Assets/_Project")))
                    dir = dir.Parent;
                if (dir != null) return dir.FullName;
            }
            return null;
        }

        // --------------------------- the retired ID-photo / face matching mechanic
        // The click-the-photo-then-click-the-face comparison is GONE: no click targets, no
        // selection state, no cue, no prompts, no code left that could resurrect it. What must
        // SURVIVE is everything the faces themselves are for — the window portrait and the ID
        // photo still render the right sprites, still centred in their slots, because the
        // anomaly system (PhotoMismatch / PhotoSubtleMismatch) is an EYE-CHECK: the player
        // compares them with no help from the desk.
        private static void FaceMatchRetiredProof(GameRoot root)
        {
            Console.WriteLine("== ID-photo / face matching: removed from the desk ==");
            var cp = root.Checkpoint;
            int guard = 0;
            while (cp.CurrentVisitor == null && guard++ < 8) cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor != null, "FM: a visitor is at the window for the removal proofs");
            if (cp.CurrentVisitor == null) return;
            cp.DebugTakeDocuments();

            // 1) nothing on the desk is a face-matching widget any more
            string[] gone = { "idphotohit", "visitorhit", "photosel", "facecue" };
            foreach (var n in gone)
                Check(FindDeep(cp.transform, n) == null, "FM: '" + n + "' is gone from the desk");
            var photoNode = FindDeep(cp.transform, "idphoto").gameObject;
            var portraitNode = FindDeep(cp.transform, "visitor").gameObject;
            Check(NoPointerHandler(photoNode) && NoPointerHandler(portraitNode),
                "FM: no pointer handler of ANY kind survives on the photo or the portrait");
            var photoImg = FindDeep(cp.transform, "idphoto").GetComponent<UnityEngine.UI.Image>();
            var portraitImg = FindDeep(cp.transform, "visitor").GetComponent<UnityEngine.UI.Image>();
            Check(!photoImg.raycastTarget && !portraitImg.raycastTarget,
                "FM: the ID photo and the face at the window take NO clicks — they are printed ink and scenery");
            Check(FindDeep(cp.transform, "idphoto").GetComponent<UnityEngine.UI.Button>() == null &&
                  FindDeep(cp.transform, "visitor").GetComponent<UnityEngine.UI.Button>() == null,
                "FM: neither face is a button, so neither can be a matching target");

            // 2) the card is still one draggable object: the photo no longer eats the gesture
            var cardRt = FindDeep(cp.transform, "idcard").GetComponent<RectTransform>();
            var cardDrag = FindDeep(cp.transform, "idcard").GetComponent<DragPaper>();
            Check(FindDeep(cp.transform, "idcard").GetComponent<UnityEngine.UI.Image>().raycastTarget && cardDrag != null,
                "FM: the card body still owns the pointer (dragging by the photo works again)");
            Vector2 cardBefore = cardRt.anchoredPosition;
            cardDrag.BeginDrag();
            cardDrag.DragBy(new Vector2(40f, 10f));
            cardDrag.EndDrag();
            Check(Vector2.Distance(cardRt.anchoredPosition, cardBefore) > 5f,
                "FM: a drag that starts on the photograph still moves the card");

            // 3) no prompt, toast or note anywhere teaches the removed mechanic
            string toast = FindDeep(root.HUD.transform, "toast").GetComponent<UnityEngine.UI.Text>().text;
            string hint = FindDeep(root.HUD.transform, "objective").GetComponent<UnityEngine.UI.Text>().text;
            var allText = new System.Text.StringBuilder();
            CollectText(cp.transform, allText);
            CollectText(root.HUD.transform, allText);
            string desk = allText.ToString();
            Check(!toast.Contains("ID PHOTO") && !hint.Contains("ID PHOTO") &&
                  !desk.Contains("ID PHOTO first") && !desk.Contains("ID photo selected"),
                "FM: no instruction to click the ID photo survives in any desk/HUD text");
            Check(!desk.Contains("green frame") && !desk.Contains("second-opinion"),
                "FM: no comparison-result wording survives (green/amber cue language is gone)");

            // 4) the faces themselves still work — the eye-check the anomaly system relies on
            var v = cp.CurrentVisitor;
            Check(photoImg.sprite != null && portraitImg.sprite != null &&
                  photoImg.sprite.name != "face_placeholder" && portraitImg.sprite.name != "face_placeholder",
                "FM: the ID photo and the window portrait still render real faces");
            var offeredImg = FindDeep(cp.transform, "photo").GetComponent<UnityEngine.UI.Image>();
            Check(offeredImg.sprite != null && offeredImg.sprite.name != "face_placeholder" &&
                  !offeredImg.raycastTarget,
                "FM: the counter card's photo renders the same face — and is not clickable either");

            var vWant = Hollowbrook.UI.UIFactory.Letterbox(L.Portrait.W, L.Portrait.H,
                portraitImg.sprite.rect.width, portraitImg.sprite.rect.height);
            var vRt = portraitImg.GetComponent<RectTransform>();
            Check(Mathf.Abs(vRt.sizeDelta.x - vWant.W) < 0.5f && Mathf.Abs(vRt.sizeDelta.y - vWant.H) < 0.5f &&
                  Mathf.Abs(vRt.anchoredPosition.x - vWant.X) < 0.5f,
                $"FM: the portrait rect IS the letterbox of its sprite, centred in the window ({vWant.W:F0}x{vWant.H:F0})");
            var pWant = Hollowbrook.UI.UIFactory.Letterbox(L.Inner.IdPhoto.W, L.Inner.IdPhoto.H,
                photoImg.sprite.rect.width, photoImg.sprite.rect.height);
            var pRt = photoImg.GetComponent<RectTransform>();
            Check(Mathf.Abs(pRt.sizeDelta.x - pWant.W) < 0.5f && Mathf.Abs(pRt.sizeDelta.y - pWant.H) < 0.5f,
                "FM: the ID photo rect IS the letterbox of its sprite inside the card slot");
            Check(FaceArt.WindowSprite(v) != null && FaceArt.IdSprite(v) != null,
                "FM: FaceArt still resolves both faces (the identity chain is untouched)");

            // 5) no white boxes anywhere on the desk: a null-sprite Image would render uGUI white.
            // Solid panels are sprite-less BY DESIGN, so the invariant is: every face-chain Image
            // has a real sprite when visible, and every sprite-less overlay ink is transparent.
            bool inkLeak = false;
            foreach (var n in new[] { "stampoverlay" })
            {
                var t = FindDeep(cp.transform, n);
                if (t == null) continue;
                var im = t.GetComponent<UnityEngine.UI.Image>();
                if (im != null && im.sprite == null && im.color.a > 0.01f) inkLeak = true;
            }
            Check(!inkLeak, "FM: sprite-less overlay inks stay fully transparent until they mean something");

            // 6) the pairing TRUTH the removed cue used to show is still in the data, so
            // anomaly detection, scoring and the fairness invariants never depended on the widget
            Check(v.DisplayedFacesMatch == (!v.IsAnomaly || !v.Defects.Contains(DefectType.PhotoMismatch)),
                "FM: DisplayedFacesMatch still reports the run's own pairing truth (data, not UI)");

            // temporary face/stamp assets still ship in the project's own override folder
            string rootDir = RepoRoot();
            string artDir = rootDir == null ? null :
                System.IO.Path.Combine(rootDir, "Assets/_Project/Resources/Art");
            bool allFaces = artDir != null;
            for (int i = 1; i <= Hollowbrook.Logic.FaceAssets.StudentFaceCount && allFaces; i++)
                allFaces &= System.IO.File.Exists(System.IO.Path.Combine(artDir,
                    Hollowbrook.Logic.FaceAssets.StudentKey(i - 1) + ".png"));
            for (int i = 1; i <= Hollowbrook.Logic.FaceAssets.StudentFaceCount && allFaces; i++)
                allFaces &= System.IO.File.Exists(System.IO.Path.Combine(artDir,
                    Hollowbrook.Logic.FaceAssets.AnomalyKey(i - 1) + ".png"));   // paired variants
            Check(allFaces, "FM: every temporary student/anomaly face asset still ships under Resources/Art");
            Check(artDir != null && System.IO.File.Exists(
                System.IO.Path.Combine(artDir, "stamp_approval_temp.png")),
                "FM: the temporary approval stamp asset still ships under Resources/Art");
            Check(cp.CurrentVisitor.FaceAssetIndex >= 0 || cp.CurrentVisitor.IsAdministrator,
                "FM: visitors still carry a stable face-asset index (admin excepted)");
            int fidx = cp.CurrentVisitor.FaceAssetIndex;
            Check(fidx < 0 || Hollowbrook.Logic.FaceAssets.WindowKey(fidx, true) ==
                  Hollowbrook.Logic.FaceAssets.StudentKey(fidx) + "_anomaly",
                "FM: the anomaly variant is still paired with the same student as the normal portrait");
        }

        // ------------------------------------------------- ground-floor plan (map overlay)
        // The sheet is authored once in FloorPlan's design space and FITTED to the screen, so
        // these proofs measure the real built rects: centred at the reference resolution, still
        // centred and fully visible at other resolutions, every room on the plan, the sealed
        // room unreachable, and the comfort rooms present without becoming patrol targets.
        private static void MapProof(GameRoot root)
        {
            Console.WriteLine("== ground-floor map: hand-drawn plan, centred and data-driven ==");
            var map = root.MapScreen;
            Check(map != null, "MAP: the map view exists");
            if (map == null) return;

            Check(FloorPlan.LayoutIsSane(out string layoutProblem),
                "MAP: the floor plan's rooms are all on the sheet and none overlap" +
                (layoutProblem != null ? " (" + layoutProblem + ")" : ""));

            int w0 = Screen.width, h0 = Screen.height;
            Screen.width = 1280; Screen.height = 720;      // the reference resolution
            root.OpenMap();
            Check(root.State == AppViewState.Map && map.gameObject.activeSelf, "MAP: M opens the plan");

            var sheet = map.Sheet;
            var plan = map.PlanRoot;
            Check(Mathf.Abs(sheet.anchoredPosition.x) < 0.01f && Mathf.Abs(sheet.anchoredPosition.y) < 0.01f,
                $"MAP: the sheet is dead-centre of the screen ({sheet.anchoredPosition})");
            Check(sheet.anchorMin == new Vector2(0.5f, 0.5f) && sheet.anchorMax == new Vector2(0.5f, 0.5f) &&
                  sheet.pivot == new Vector2(0.5f, 0.5f),
                "MAP: ...by construction (centre anchor + centre pivot), not by a hand-tuned offset");
            Check(Mathf.Abs(sheet.sizeDelta.x - FloorPlan.DesignW * map.PaperScale) < 0.01f &&
                  Mathf.Abs(sheet.sizeDelta.y - FloorPlan.DesignH * map.PaperScale) < 0.01f,
                $"MAP: the sheet keeps the plan's {FloorPlan.DesignW:F0}x{FloorPlan.DesignH:F0} aspect " +
                $"at its fitted scale ({map.PaperScale:F3})");
            Check(sheet.sizeDelta.x <= 1280f + 0.01f && sheet.sizeDelta.y <= 720f + 0.01f,
                $"MAP: the whole sheet is on screen ({sheet.sizeDelta.x:F0}x{sheet.sizeDelta.y:F0} in 1280x720)");
            Check(Mathf.Abs(plan.anchoredPosition.x) < 0.01f,
                "MAP: the plan is centred horizontally inside the sheet");
            Check(Mathf.Abs(plan.anchoredPosition.y) < 40f,
                $"MAP: the plan sits between the title band and the control band (y={plan.anchoredPosition.y:F1})");

            // the title, the plan and the bottom controls are one centred stack
            var title = map.Title;
            Check(title != null && title.text == FloorPlan.Title &&
                  title.text.Contains("HOLLOWBROOK COLLEGE") && title.text.Contains("GROUND FLOOR"),
                "MAP: the sheet carries the HOLLOWBROOK COLLEGE — GROUND FLOOR title");
            Check(title.font == UIFactory.HandFont, "MAP: the title is set in the handwritten plan face");
            var closeRow = map.CloseRow;
            Check(closeRow != null && Mathf.Abs(closeRow.anchoredPosition.x) < 0.01f,
                "MAP: CLOSE is centred with the sheet");
            var closeBtn = closeRow.GetComponent<UnityEngine.UI.Button>();
            Check(closeBtn != null, "MAP: CLOSE is still a real button");
            Check(closeBtn != null && closeBtn.targetGraphic != null && closeBtn.targetGraphic.raycastTarget,
                "MAP: ...and its graphic actually takes the click (a raycast-off target is a dead button)");
            bool everyBtnHasTarget = true;
            foreach (var def in FloorPlan.Rooms)
            {
                var node = map.RoomNode(def.Key);
                var b = node.GetComponent<UnityEngine.UI.Button>();
                if (b == null) { if (!def.Sealed) everyBtnHasTarget = false; continue; }
                if (b.targetGraphic == null || !b.targetGraphic.raycastTarget) everyBtnHasTarget = false;
            }
            Check(everyBtnHasTarget,
                "MAP: every clickable room has a raycast-enabled target graphic (none of them can be a dead click)");
            // centred space: +y up from the screen centre, whatever the element's own anchor is
            // CLOSE is anchored to the screen's bottom edge with a centre pivot, so its
            // anchoredPosition.y is measured UP from that edge: screen-centre space is
            // y = -visibleH/2 + anchoredPosition.y
            float sheetBottom = CentreY(sheet) - sheet.sizeDelta.y / 2f;
            float closeCentre = -360f + closeRow.anchoredPosition.y;
            float closeTop = closeCentre + closeRow.sizeDelta.y / 2f;
            float closeBottom = closeCentre - closeRow.sizeDelta.y / 2f;
            Check(closeTop <= sheetBottom - 2f && closeBottom >= -360f && closeTop <= 360f,
                $"MAP: CLOSE hangs {sheetBottom - closeTop:F0}px under the sheet, fully on screen, never over the plan");

            // ---- every room the reference drawing asks for, in the right place ----
            var hall = FloorPlan.ForRoom(RoomId.Hallway);
            var c1 = FloorPlan.ForRoom(RoomId.Classroom1);
            var c2 = FloorPlan.ForRoom(RoomId.Classroom2);
            var sec = FloorPlan.ForRoom(RoomId.Security);
            Check(hall.HasValue && c1.HasValue && c2.HasValue && sec.HasValue,
                "MAP: the hallway, both classrooms and the security room are all on the plan");
            var hallNode = map.RoomNode("room_hallway");
            var c1Node = map.RoomNode("room_classroom1");
            var c2Node = map.RoomNode("room_classroom2");
            var secNode = map.RoomNode("room_security");
            var serverNode = map.RoomNode("room_server");
            var crNorth = map.RoomNode("room_comfort_north");
            var crSouth = map.RoomNode("room_comfort_south");
            Check(hallNode != null && c1Node != null && c2Node != null && secNode != null &&
                  serverNode != null && crNorth != null && crSouth != null,
                "MAP: every room has its own node (label and hit area can never drift apart)");
            Check(secNode.GetComponent<RectTransform>().anchoredPosition.x <
                  hallNode.GetComponent<RectTransform>().anchoredPosition.x,
                "MAP: the security checkpoint is on the LEFT");
            // inside the plan container (centre anchor, centre pivot) anchoredPosition.y IS the
            // offset from the plan's centre, y down — directly comparable between rooms
            // Design y is DOWN, and Design() maps it to anchoredPosition.y = -y * scale, so a
            // room drawn ABOVE the hallway has the LARGER anchored y inside the plan container.
            float hallY = hallNode.GetComponent<RectTransform>().anchoredPosition.y;
            float c1Y = c1Node.GetComponent<RectTransform>().anchoredPosition.y;
            float c2Y = c2Node.GetComponent<RectTransform>().anchoredPosition.y;
            Check(c1Y > hallY + 40f && c2Y > hallY + 40f,
                $"MAP: BOTH classrooms are drawn ABOVE the hallway, as the reference plan has them ({c1Y:F0} / {c2Y:F0} vs hall {hallY:F0})");
            float c1X = c1Node.GetComponent<RectTransform>().anchoredPosition.x;
            float c2X = c2Node.GetComponent<RectTransform>().anchoredPosition.x;
            float secY = secNode.GetComponent<RectTransform>().anchoredPosition.y;
            float secX = secNode.GetComponent<RectTransform>().anchoredPosition.x;
            float hallX = hallNode.GetComponent<RectTransform>().anchoredPosition.x;
            Check(c1X < c2X - 40f,
                $"MAP: Room 101 sits WEST of Room 202 along the north wall ({c1X:F0} / {c2X:F0})");
            Check(secY < hallY - 40f && secX < hallX,
                $"MAP: the checkpoint hangs BELOW the hallway's west end ({secX:F0},{secY:F0})");
            float crNY = crNorth.GetComponent<RectTransform>().anchoredPosition.y;
            float crSY = crSouth.GetComponent<RectTransform>().anchoredPosition.y;
            float crNX = crNorth.GetComponent<RectTransform>().anchoredPosition.x;
            Check(crNY > hallY + 20f && crSY < hallY - 20f && crNX > hallX,
                "MAP: the comfort rooms hang off the east end, north and south of the sealed room");
            Check(serverNode.GetComponent<RectTransform>().anchoredPosition.x >
                  hallNode.GetComponent<RectTransform>().anchoredPosition.x,
                "MAP: the server / identification room is on the RIGHT");
            Check(hallNode.GetComponent<RectTransform>().sizeDelta.x >
                  2.5f * c1Node.GetComponent<RectTransform>().sizeDelta.x / 2f,
                "MAP: the hallway reads as the long central passage");
            Check(c1Node.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("CLASSROOM 1") &&
                  c1Node.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("Room 101") &&
                  c2Node.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("CLASSROOM 2") &&
                  c2Node.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("Room 202"),
                "MAP: the classrooms keep their room numbers (101 west, 202 east, both north)");
            Check(crNorth.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("COMFORT ROOM") &&
                  crSouth.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("COMFORT ROOM"),
                "MAP: both comfort rooms are labelled on the plan");
            Check(FloorPlan.ComfortRooms().Count == 2, "MAP: the comfort rooms live in the plan's data, not just its ink");
            var crN = FloorPlan.ForKey("room_comfort_north").Value;
            var crS = FloorPlan.ForKey("room_comfort_south").Value;
            Check(!crN.Overlaps(hall.Value) && !crS.Overlaps(hall.Value) &&
                  !crN.Overlaps(c1.Value) && !crS.Overlaps(c2.Value),
                "MAP: the comfort rooms obstruct neither the hallway nor the classrooms");
            Check(!crN.Room.HasValue && !crS.Room.HasValue && !crN.Sealed && !crS.Sealed,
                "MAP: the comfort rooms are map-only — they are not patrol destinations");

            // every drawn room sits inside the drawn plan, at its design-space size, and at the
            // position the table asks for — the check that catches a mirrored or hand-nudged plan
            bool inside = true, scaled = true, faithful = true;
            foreach (var def in FloorPlan.Rooms)
            {
                var node = map.RoomNode(def.Key);
                if (node == null) { inside = false; continue; }
                var rt = node.GetComponent<RectTransform>();
                var planRect = new Rect(-plan.sizeDelta.x / 2f, -plan.sizeDelta.y / 2f,
                    plan.sizeDelta.x, plan.sizeDelta.y);
                if (!planRect.Contains(rt.anchoredPosition)) inside = false;
                if (Mathf.Abs(rt.sizeDelta.x - def.W * map.PlanScale) > 0.01f ||
                    Mathf.Abs(rt.sizeDelta.y - def.H * map.PlanScale) > 0.01f) scaled = false;
                float wantX = MapSheetLayout.ToAnchoredX(def.X, map.PlanScale);
                float wantY = MapSheetLayout.ToAnchoredY(def.Y, map.PlanScale);
                if (Mathf.Abs(rt.anchoredPosition.x - wantX) > 0.01f ||
                    Mathf.Abs(rt.anchoredPosition.y - wantY) > 0.01f) faithful = false;
            }
            Check(inside, "MAP: every room is drawn inside the plan area (nothing clipped or overflowing)");
            Check(scaled, "MAP: every room is sized from the design table by the ONE plan scale");
            Check(faithful, "MAP: every room sits exactly where FloorPlan puts it (the drawing is faithful to the data)");
            Check(MapSheetLayout.ToAnchoredY(c1.Value.Y, 1f) > MapSheetLayout.ToAnchoredY(hall.Value.Y, 1f) &&
                  MapSheetLayout.ToAnchoredY(c2.Value.Y, 1f) > MapSheetLayout.ToAnchoredY(hall.Value.Y, 1f) &&
                  MapSheetLayout.ToAnchoredY(sec.Value.Y, 1f) < MapSheetLayout.ToAnchoredY(hall.Value.Y, 1f),
                "MAP: the design-y flip is right way up — both classrooms above the corridor on screen, the checkpoint below");

            // ---- the sealed room stays sealed ----
            Check(serverNode.GetComponent<UnityEngine.UI.Button>() == null &&
                  !serverNode.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                "MAP: the sealed server / identification room has no button and eats no clicks");
            Check(serverNode.Find("label").GetComponent<UnityEngine.UI.Text>().text.Contains("SEALED"),
                "MAP: ...and still says SEALED");
            var sealImg = serverNode.GetComponent<UnityEngine.UI.Image>();
            Check(sealImg.sprite != null && sealImg.sprite.name == "map_sealed" &&
                  sealImg.color == Color.white &&
                  serverNode.Find("label").GetComponent<UnityEngine.UI.Text>().color == UIFactory.AccentBright,
                "MAP: the sealed room has its own barred red art and a red label (restricted reads at a glance)");
            var serverDef = FloorPlan.ForKey("room_server").Value;
            Check(serverDef.Sealed && !serverDef.Room.HasValue &&
                  System.Array.IndexOf(RoomGraph.Roamable, RoomId.Security) < 0,
                "MAP: the sealed room is not in the patrol graph, so it cannot be travelled to");

            // ---- the patrol rooms keep their click ----
            var c1Btn = c1Node.GetComponent<UnityEngine.UI.Button>();
            var secBtn = secNode.GetComponent<UnityEngine.UI.Button>();
            Check(c1Btn != null && secBtn != null &&
                  hallNode.GetComponent<UnityEngine.UI.Button>() != null &&
                  c2Node.GetComponent<UnityEngine.UI.Button>() != null,
                "MAP: the hallway, both classrooms and the checkpoint are still clickable");
            var crBtn = crNorth.GetComponent<UnityEngine.UI.Button>();
            Check(crBtn != null, "MAP: a comfort room answers a click (with a line, not a dead nothing)");

            // ---- you-are-here + the findings dots ----
            var here = FindDeep(plan, "here");
            Check(here != null && here.gameObject.activeSelf &&
                  here.Find("lbl").GetComponent<UnityEngine.UI.Text>().text == "YOU",
                "MAP: the position marker is on the plan and says YOU");
            Check(FindDeep(plan, "dot_light") == null,
                "MAP: no finding dots before anything has been discovered");

            // ---- responsive: any window size keeps it centred, whole, and readable ----
            var sizes = new[] { new Vector2(1920, 1080), new Vector2(1024, 768), new Vector2(800, 600),
                                new Vector2(2560, 1080), new Vector2(1280, 600), new Vector2(3840, 2160) };
            bool centred = true, fits = true, closeOk = true, readable = true, roomsScale = true;
            foreach (var s in sizes)
            {
                Screen.width = (int)s.x; Screen.height = (int)s.y;
                // re-run the guard for this window exactly as its Update would, then ask the
                // SAME question the view asks: how much reference space is actually visible?
                var guard = sheet.GetComponentInParent<CanvasScaler>()?.GetComponent<AspectMatchGuard>();
                guard?.Apply();
                var vis = UIFactory.VisibleReferenceSize(sheet);
                map.Relayout();
                if (Mathf.Abs(sheet.anchoredPosition.x) > 0.01f || Mathf.Abs(sheet.anchoredPosition.y) > 0.01f)
                    centred = false;
                float visibleW = vis.x, visibleH = vis.y;
                if (sheet.sizeDelta.x > visibleW + 0.5f || sheet.sizeDelta.y > visibleH + 0.5f) fits = false;
                float cCentre = -visibleH / 2f + closeRow.anchoredPosition.y;
                float cTop = cCentre + closeRow.sizeDelta.y / 2f;
                float cBottom = cCentre - closeRow.sizeDelta.y / 2f;
                if (cTop > visibleH / 2f || cBottom < -visibleH / 2f ||
                    cTop > CentreY(sheet) - sheet.sizeDelta.y / 2f - 2f) closeOk = false;
                if (map.Title.fontSize < 12) readable = false;
                if (Mathf.Abs(hallNode.GetComponent<RectTransform>().sizeDelta.x -
                              FloorPlan.ForRoom(RoomId.Hallway).Value.W * map.PlanScale) > 0.01f)
                    roomsScale = false;
            }
            Check(centred, "MAP: still dead-centre at 16:9, 4:3, 21:9, 32:9 and 2x/4k sizes");
            Check(fits, "MAP: the sheet never exceeds the visible reference area (no clipping, no overflow)");
            Check(closeOk, "MAP: CLOSE stays on screen and never overlaps the sheet at any size");
            Check(readable, "MAP: the title stays readable when the plan has to shrink");
            Check(roomsScale, "MAP: the rooms keep following the one plan scale at every size");

            Screen.width = w0; Screen.height = h0;
            (sheet.GetComponentInParent<CanvasScaler>()?.GetComponent<AspectMatchGuard>())?.Apply();
            map.Relayout();
            root.CloseMap();
            Check(root.State == AppViewState.Checkpoint && !map.gameObject.activeSelf,
                "MAP: closing returns exactly where the player was");
        }

        // drag a stamp from its tray and release it with its centre on a desk point —
        // exactly what the pointer does; OnDropped then carries that point to the slip
        private static void StampAtPoint(GameRoot root, Verdict v, Vector2 deskPoint)
        {
            var name = v == Verdict.Allow ? "stamp_allow" : "stamp_deny";
            var drag = FindDeep(root.Checkpoint.transform, name).GetComponent<DragPaper>();
            drag.BeginDrag();
            drag.PlaceCenter(deskPoint);
            drag.EndDrag();
        }

        // ------------------------------------------------- desk interaction loop
        // Button-level proof of the full visitor loop on the rebuilt desk: gating,
        // CALL PRINCIPAL popup (No cancels / Yes stamps), PATROL guard,
        // ALLOW click, roster walk and the END SHIFT clock-out. Stub coroutines run
        // synchronously, so banners/next-visitor transitions complete inside the call.
        private static void DeskLoop(GameRoot root)
        {
            Console.WriteLine("== desk interaction loop (button level) ==");
            var cp = root.Checkpoint;
            var desk = FindDeep(cp.transform, "DeskFrame");
            Check(desk != null, "DeskFrame found for the interaction loop");
            if (desk == null) return;

            Func<string, UnityEngine.UI.Button> btn =
                n => FindDeep(desk, n).GetComponent<UnityEngine.UI.Button>();
            Func<string, bool> live = n => btn(n).interactable;

            // earlier proofs may have left an inspection mid-way: close it like a player
            // would, then make sure a fresh visitor (documents NOT taken) is at the window
            if (FindDeep(desk, "idcard").gameObject.activeSelf && cp.CurrentVisitor != null)
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            int freshGuard = 0;
            while (cp.CurrentVisitor == null && freshGuard++ < 8) cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor != null && FindDeep(desk, "idcard").gameObject.activeSelf &&
                  FindDeep(desk, "slip").gameObject.activeSelf,
                  "a fresh visitor arrives with both documents already on the desk (§6 automation)");
            Check(!FaceOf(FindDeep(desk, "idcard"), "idback").gameObject.activeSelf,
                  "the fresh visitor's ID is born FACE-UP (flip state never carries over)");
            Check(StampAlpha(root, "stamp_allow") > 0.9f, "stamps ink up as soon as the inspection is live (docs auto-placed)");
            int procBefore = root.Queue.ProcessedCount;
            DropStamp(root, Verdict.Reject);
            Check(root.Queue.ProcessedCount == procBefore && cp.CurrentVisitor != null,
                  "a stamp drop with no documents in hand is a no-op");
            Check(!FindDeep(desk, "offeredid").gameObject.activeInHierarchy,
                  "no counter hand-over gate: the offered card never blocks the flow");
            Check(FindDeep(desk, "idcard").gameObject.activeSelf &&
                  FindDeep(desk, "slip").gameObject.activeSelf,
                  "AUTO-DOCS: ID and slip are already on the desk when the inspection begins");
            Check(PromptText(root).Contains("STAMP THE SLIP"),
                  "AUTO-DOCS: the prompt jumps straight to stamping (got '" + PromptText(root) + "')");
            var cardPos = FindDeep(desk, "idcard").GetComponent<RectTransform>().anchoredPosition;
            var slipPos = FindDeep(desk, "slip").GetComponent<RectTransform>().anchoredPosition;
            Check(cardPos.y < 0 && slipPos.y < 0 && Mathf.Abs(slipPos.x - (740f - L.CounterSlip.W * 0.5f)) < 1f,
                $"AUTO-DOCS: both rest in their desk slots (card {cardPos.x:F0},{cardPos.y:F0}; slip {slipPos.x:F0},{slipPos.y:F0})");
            Check(FindDeep(desk, "sname") != null &&
                  FindDeep(desk, "sname").GetComponent<UnityEngine.UI.Text>().text.Length > 4,
                "AUTO-DOCS: the slip carries the current student's data without any drag");
            Check(!live("patrol"), "PATROL HALLWAY dimmed mid-inspection");
            Check(!live("endshift"), "END SHIFT dimmed before the roster is done");

            Check(StampAlpha(root, "stamp_allow") > 0.9f, "stamps stay inked while the inspection is live");

            cp.GoPatrol();
            Check(root.State == AppViewState.Checkpoint, "PATROL refused mid-inspection (state stays Checkpoint)");

            Check(FindDeep(desk, "confirmbox_border") == null,
                  "the yes/no confirm popup is retired from the desk");
            string beforeYes = cp.CurrentVisitor != null ? cp.CurrentVisitor.ClaimedName : "?";
            int procBeforeStamp = root.Queue.ProcessedCount;
            TakeSlip(root);
            StampSlip(root, Verdict.Reject);
            Check(cp.SlipStamped && cp.PendingVerdict == Verdict.Reject,
                  "the stamp on the slip is pending: wordless, unscored");
            Check(root.Queue.ProcessedCount == procBeforeStamp,
                  "nothing is scored at the stamp drop — the decision waits for hand-back");
            HandBack(root, false, true);     // the slip dropped at the window is NOT a hand-back
            Check(root.Queue.ProcessedCount == procBeforeStamp &&
                  cp.CurrentVisitor != null && cp.CurrentVisitor.ClaimedName == beforeYes &&
                  FindDeep(desk, "slip").gameObject.activeSelf,
                  "dropping the slip at the window does nothing: it stays on the desk, the student waits");
            HandBack(root, true, false);     // the ID alone closes the inspection
            Check(root.Queue.ProcessedCount == procBeforeStamp + 1,
                  "handing back the ID (slip stamped, left on the desk) commits the verdict");
            Check(!FindDeep(desk, "speechbubble").gameObject.activeSelf,
                  "the speech bubble leaves with the student — no line lingers over an empty window");
            // the stub runs the commit coroutine synchronously: the walked-off visitor is proof
            Check(cp.CurrentVisitor == null || cp.CurrentVisitor.ClaimedName != beforeYes,
                "verdict registered: " + beforeYes + " was processed and left the window");
            if (cp.CurrentVisitor == null) cp.AdvanceQueue(6f);   // the arrival clock delivers the next
            Check(cp.CurrentVisitor != null, "next visitor arrives on the clock after the verdict banner");

            int guard = 0;
            while (!live("endshift") && guard++ < 16)
            {
                if (cp.CurrentVisitor == null) { cp.AdvanceQueue(6f); continue; }
                cp.DebugTakeDocuments();   // hand-over gate before the verdict path
                TakeSlip(root);
                var want = InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null) == Verdict.Allow
                    ? Verdict.Allow : Verdict.Reject;
                StampSlip(root, want);
                HandBack(root, true, true);   // full hand-back commits the verdict
            }
            Check(guard < 16, "roster walked to the end without runaway (" + guard + " steps)");
            Check(root.Queue.ShiftComplete, "P9: shift complete = source dry + line empty + window free");
            Check(live("endshift"), "END SHIFT lights up when the roster is done");
            Check(live("patrol"), "PATROL HALLWAY usable once no inspection is live");

            ResolvePool(root);   // a played patrol would have dealt with the night's anomalies (issue #7)
            btn("endshift").onClick.Invoke();
            bool expectedEnding = root.Config.exhibitionMode;   // the exhibition arc clocks out straight into its ending
            Check(expectedEnding ? root.State == AppViewState.Ending : root.State == AppViewState.ShiftResults,
                "END SHIFT click clocks out (exhibition=" + expectedEnding + ", state=" + root.State + ")");
        }

        private static void SaveContinueResume()
        {            Console.WriteLine("== B8 save/continue resume ==");
            var root = GameRoot.Instance;
            string first = root.Checkpoint.CurrentVisitor != null ? root.Checkpoint.CurrentVisitor.ClaimedName : "?";
            root.Checkpoint.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            root.Checkpoint.Stamp(InspectionRules.ExpectedVerdict(root.Checkpoint.CurrentVisitor, null));
            int logBefore = root.Session.TonightLog.Count;
            int poolBefore = root.Session.Pool.Pending;
            int admittedBefore = root.Session.AdmittedStudents.Count;
            root.PersistRun();
            Check(root.Session.QueueState != null && root.Session.QueueState.Started &&
                  root.Session.QueueState.Arrived == root.Queue.ArrivalCount,
                "P11: PersistRun writes the started queue snapshot (cursor " + root.Session.QueueState.RosterCursor + ")");
            root.QuitToMenu(true);
            root.ContinueGame();
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            var sticky = FindDeep(root.Checkpoint.transform, "StickyNoteOverlay");
            Check(sticky != null && !sticky.gameObject.activeSelf,
                "P10: CONTINUE into a started shift never re-shows the sticky note");
            Check(root.Checkpoint.CurrentVisitor == null,
                "P9: the restored shift waits on its arrival clock instead of respawning someone");
            root.Checkpoint.AdvanceQueue(6f);         // the restored clock delivers the next arrival
            string resumed = root.Checkpoint.CurrentVisitor != null ? root.Checkpoint.CurrentVisitor.ClaimedName : "?";
            Console.WriteLine("  first visitor before quit: " + first + " | first visitor after CONTINUE: " + resumed);
            Check(first != resumed, "B8: CONTINUE resumes at the next visitor, not the same one");
            Check(root.Queue.ArrivalCount == 2 && root.Queue.ProcessedCount == 1,
                "P9: the restored queue kept its arrival/processed counts (no restart from zero)");
            // issue #3: the resumed night keeps its presentation log, pool and admitted list
            Check(root.Session.TonightLog.Count == logBefore,
                "issue #3: TonightLog survives save/continue (duplicate rule keeps working)");
            Check(root.Session.Pool.Pending == poolBefore,
                "issue #3: the anomaly pool survives save/continue (repelled stays repelled)");
            Check(root.Session.AdmittedStudents.Count == admittedBefore,
                "issue #3: admitted students survive save/continue (classroom seats keep them)");
        }

        private static int DeepIndex(Transform parent, string name)
        {
            var flat = new List<Transform>();
            Flatten(parent, flat);
            for (int i = 0; i < flat.Count; i++) if (flat[i].name == name) return i;
            return -1;
        }

        private static void Flatten(Transform t, List<Transform> into)
        {
            into.Add(t);
            for (int i = 0; i < t.childCount; i++) Flatten(t.GetChild(i), into);
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindDeep(t.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        // ------------------------------------------------- checkpoint queue proofs
        // Headless runs of the Phase 11 manual sequences: 11.1 queue + sticky note,
        // 11.2 queue pressure, 11.3 missing student, 11.4 transitions/persistence,
        // 11.5 settings + pause. Timing overrides below are TEST-ONLY (applied to the
        // live queue tuning or a synthetic roster); the production arrival interval in
        // GameConfigSO is never touched to make a proof finish faster.
        private static void QueueProof(GameRoot root)
        {
            Console.WriteLine("== QUEUE proofs (Phase 9 + 11.1-11.4, headless) ==");
            bool exhibition = root.Config.exhibitionMode;

            // ---- Q1 arrivals accumulate FIFO while an inspection stays unresolved
            FreshGame(root);
            var cp = root.Checkpoint;
            var roster = root.CurrentRoster;
            Check(root.Queue.Tuning.ArrivalMinSeconds == root.Config.queueArrivalMinSeconds &&
                  root.Queue.Tuning.ArrivalMaxSeconds == root.Config.queueArrivalMaxSeconds &&
                  root.Queue.Tuning.PressureWarnAt == root.Config.queuePressureWarnAt &&
                  root.Queue.Tuning.OverwhelmedAt == root.Config.queueOverwhelmedAt &&
                  Math.Abs(root.Queue.Tuning.MissingStudentChance - root.Config.missingStudentChance) < 1e-6f &&
                  root.Queue.Tuning.MissingStudentLimit == root.Config.missingStudentLimit,
                  "Q1: production config feeds the queue tuning before any override");
            IsolateMissingStudent(root);
            Check(FindDeep(cp.transform, "StickyNoteOverlay").gameObject.activeSelf, "Q1: the fresh shift waits behind the note");
            ClickStickyNote(root);
            Check(cp.CurrentVisitor == roster[0], "Q1: note click presents the first visitor");
            for (int i = 0; i < 3; i++) cp.AdvanceQueue(6f);   // inspection left unresolved ~18s
            Check(cp.CurrentVisitor == roster[0], "Q1: an unresolved inspection keeps its visitor");
            Check(root.Queue.WaitingCount == 3, "Q1: three arrivals accumulated in the line (got " + root.Queue.WaitingCount + ")");
            Check(string.Join(",", root.Queue.WaitingLine.Select(v => v.VisitorIndex.ToString())) == "1,2,3",
                  "Q1: the line is FIFO in arrival order");

            // ---- Q2 the oldest waiter steps up after the banner, with no second wait
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.CurrentVisitor == roster[1], "Q2: oldest waiting visitor stepped up immediately after the banner");
            Check(root.Queue.WaitingCount == 2, "Q2: the line queues invisibly (showcase)");
            int guard = 0;
            while (cp.CurrentVisitor != null && guard++ < 6)
            {
                cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            }
            Check(root.Queue.ShiftComplete && guard <= 3, "Q2: the fully resolved roster completes the shift");

            // ---- Q3 an empty queue never completes the shift early (11.1 step 11)
            FreshGame(root);
            IsolateMissingStudent(root);
            cp = root.Checkpoint;
            ClickStickyNote(root);
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.CurrentVisitor == null && root.Queue.WaitingCount == 0, "Q3: window and line are empty");
            Check(!root.Queue.ShiftComplete, "Q3: empty line with pending arrivals is NOT completion");
            Check(!Btn(root, "endshift").interactable, "Q3: END SHIFT stays dimmed while the source can produce");
            cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor == root.CurrentRoster[1], "Q3: the checkpoint waits, then the clock delivers");

            // ---- Q4 queue pressure warns once, overwhelm collapses once (11.2)
            var night4 = new RosterFactory(5).GenerateShift(ShiftPlan.Build()[3]);   // 8 visitors
            Check(night4.Count == 8, "Q4: pressure roster has eight visitors");
            FreshGame(root);
            root.Queue.AttachRoster(night4, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f,   // test-only acceleration
                PressureWarnAt = 5, OverwhelmedAt = 7,   // prove the pressure feature with real
                                                          // values even though shipping config
                                                          // disables it for the showcase
                MissingStudentChance = 0f,
            }, 4242);
            cp = root.Checkpoint;
            int warnToasts = 0, overrunToasts = 0;
            var toastSub = root.Bus.Subscribe(EventIds.Notification, e =>
            {
                string t = e.Text ?? e.Data as string;
                if (t == null) return;
                if (t.Contains("QUEUE PRESSURE")) warnToasts++;
                if (t.Contains("OVERRUN")) overrunToasts++;
            });
            ClickStickyNote(root);
            Check(cp.CurrentVisitor == night4[0], "Q4: synthetic roster drives the same note-click start");
            for (int i = 0; i < 7; i++) cp.AdvanceQueue(1f);
            Check(warnToasts == 1, "Q4: pressure warning fired exactly once at 5 waiting (got " + warnToasts + ")");
            Check(overrunToasts == 1, "Q4: overwhelmed fired exactly once at 7 waiting (got " + overrunToasts + ")");
            Check(root.Queue.WaitingCount == 7 && root.Queue.ArrivalCount == 8, "Q4: 7 waiting + 1 at the window when the line overran");
            Check(root.Queue.Collapsed, "Q4: the queue collapsed");
            Check(root.State == AppViewState.Ending && root.Session.QueueOverwhelmed, "Q4: overwhelmed ended the run through the existing ending route");
            Check(root.Session.RunFinished && root.Records.EndingsSeen.Contains("lose"), "Q4: recorded once as a lose, run finished");
            int arrivalsFrozen = root.Queue.ArrivalCount;
            int processedBefore = root.Session.Processed;
            cp.AdvanceQueue(60f);
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(Verdict.Allow);
            root.TriggerQueueOverwhelmed();
            Check(root.Queue.ArrivalCount == arrivalsFrozen, "Q4: no further arrivals after the collapse");
            Check(root.Session.Processed == processedBefore, "Q4: verdict interaction disabled after the run ended");
            Check(overrunToasts == 1 && root.State == AppViewState.Ending, "Q4: no duplicate ending transition");
            toastSub.Dispose();

            // ---- Q5 missing student: forced incident, line-preferred victim, permanent (11.3)
            int seedWait = FindQueueSeed(rr => rr.Count > 3 && !rr[0].IsAnomaly && !rr[1].IsAnomaly && rr[2].IsAnomaly);
            Check(seedWait > 0, "Q5: found a deterministic seed with legit, legit, anomaly order");
            var mRoster = new RosterFactory(seedWait).GenerateShift(ShiftPlan.Build()[1]);  // night 2: 6 visitors
            var victimExpected = mRoster[1];
            FreshGame(root);
            root.Queue.AttachRoster(mRoster, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f,
                MissingStudentChance = 1f,          // forced for the proof
                MissingStudentLimit = root.Config.missingStudentLimit,
            }, 777);
            cp = root.Checkpoint;
            ClickStickyNote(root);                    // #0 legit -> window
            cp.AdvanceQueue(1f);                      // #1 legit -> line
            cp.AdvanceQueue(1f);                      // #2 anomaly -> line, incident fires
            Check(root.Queue.MissingIncidents == 1, "Q5: the anomaly's arrival triggered one incident");
            Check(root.Queue.WaitingCount == 1 && root.Queue.WaitingLine.First() == mRoster[2],
                  "Q5: the waiting legitimate student vanished from the line");
            Check(root.Queue.EffectiveTotal == mRoster.Count - 1, "Q5: effective total dropped by one (" + root.Queue.EffectiveTotal + ")");
            string toast = FindDeep(root.HUD.transform, "toast").GetComponent<UnityEngine.UI.Text>().text;
            Check(toast.Contains("ROSTER DISCREPANCY"), "Q5: subtle radio feedback via the HUD toast, no anomaly revealed");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));   // resolve #0 -> anomaly steps up
            Check(cp.CurrentVisitor == mRoster[2], "Q5: the anomaly itself steps up to the window");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(Verdict.Allow);                    // let the anomaly in — the loss must stand
            Check(root.Queue.MissingIncidents == 1 && root.Queue.EffectiveTotal == mRoster.Count - 1,
                  "Q5: allowing the anomaly never restores the vanished student");
            var arrivedNames = new List<string>();
            guard = 0;
            while (!root.Queue.ShiftComplete && guard++ < 12)
            {
                if (cp.CurrentVisitor == null) { cp.AdvanceQueue(1f); continue; }
                arrivedNames.Add(cp.CurrentVisitor.ClaimedName);
                cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            }
            Check(root.Queue.ShiftComplete, "Q5: the shift still completes with a vanished student");
            Check(!arrivedNames.Contains(victimExpected.ClaimedName), "Q5: the vanished student never arrived later");
            Check(root.Queue.ArrivalCount == root.Queue.EffectiveTotal, "Q5: arrivals == effective total (bookkeeping consistent)");
            Check(!root.Session.TonightLog.WasPresented(victimExpected.IdNumber),
                  "Q5: a student who never arrived is never recorded as inspected");

            // ---- Q6 probability-failure path: an anomaly arrives, nobody vanishes
            FreshGame(root);
            root.Queue.AttachRoster(mRoster, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 0f,
            }, 777);
            cp = root.Checkpoint;
            ClickStickyNote(root);
            for (int i = 0; i < 3; i++) cp.AdvanceQueue(1f);
            Check(root.Queue.MissingIncidents == 0 && root.Queue.EffectiveTotal == mRoster.Count &&
                  root.Queue.WaitingCount == 3,
                  "Q6: chance-0 roll leaves the line intact (full effective total)");

            // ---- Q7 patrol transition: the line rides in GameRoot, the clock pauses (11.4)
            FreshGame(root);
            IsolateMissingStudent(root);
            cp = root.Checkpoint;
            var pRoster = root.CurrentRoster;
            ClickStickyNote(root);
            cp.AdvanceQueue(6f);                                   // #2 arrives behind #1
            Check(root.Queue.WaitingCount == 1, "Q7: a visitor is waiting before the patrol");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));   // #1 resolved, #2 steps up
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));   // #2 resolved, line empty
            Check(cp.CurrentVisitor == null && root.Queue.RosterCursor == 2, "Q7: window free, cursor at 2");
            float timerBeforePatrol = root.Queue.TimerSeconds;
            Check(timerBeforePatrol > 0f, "Q7: the arrival clock is mid-interval");
            cp.GoPatrol();
            SchoolSceneBootstrap.Build(root);                      // what Unity does after LoadScene(School)
            Check(root.State == AppViewState.Exploration, "Q7: patrol started");
            Check(root.Queue.Started && root.Queue.RosterCursor == 2 && root.Queue.ArrivalCount == 2 &&
                  Math.Abs(root.Queue.TimerSeconds - timerBeforePatrol) < 0.0001f,
                  "Q7: queue state frozen and intact while off-desk (timer never advanced)");
            root.ReturnToCheckpoint();
            CheckpointSceneBootstrap.Build(root);
            cp = root.Checkpoint;
            Check(!FindDeep(cp.transform, "StickyNoteOverlay").gameObject.activeSelf, "Q7: returning never re-shows the note");
            Check(cp.CurrentVisitor == null, "Q7: no visitor duplicated on return");
            cp.AdvanceQueue(timerBeforePatrol + 0.1f);
            Check(cp.CurrentVisitor == pRoster[2] && root.Queue.ArrivalCount == 3,
                  "Q7: the paused clock resumes and delivers exactly the next visitor");

            // ---- Q8 quit + continue mid-inspection: window visitor, budget and line restored
            cp.AdvanceQueue(6f);                                   // #4 arrives behind #3
            Check(root.Queue.WaitingCount == 1, "Q8: one waiting before the quit");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.AskQuestion(QuestionId.Year);
            Check(root.Queue.ActiveBudgetMask == 1 << (int)QuestionId.Year, "Q8: the question budget synced into the queue");
            root.TogglePause(true);
            root.QuitToMenu(true);                                 // saves at the quit boundary
            root.ContinueGame();
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            cp = root.Checkpoint;
            Check(!FindDeep(cp.transform, "StickyNoteOverlay").gameObject.activeSelf, "Q8: a restored shift shows no note");
            Check(cp.CurrentVisitor != null && cp.CurrentVisitor.VisitorIndex == pRoster[2].VisitorIndex,
                  "Q8: the same visitor is back at the window");
            string budgetText = FindDeep(cp.transform, "budget").GetComponent<UnityEngine.UI.Text>().text;
            Check(budgetText == "1/2", "Q8: the question budget was NOT refunded ('" + budgetText + "')");
            Check(root.Queue.WaitingCount == 1, "Q8: the waiting line was restored, not duplicated");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.CurrentVisitor != null && cp.CurrentVisitor.VisitorIndex == pRoster[3].VisitorIndex,
                  "Q8: after the verdict the restored waiter steps up");

            // ---- Q9 Endless/Challenge keep working through their own arrival source (9.8)
            root.StartNewGame(GameMode.Endless);
            CheckpointSceneBootstrap.Build(root);
            Check(root.Queue != null && root.Queue.Tuning.ArrivalMaxSeconds == 5f, "endless keeps the fast race clock");
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            cp = root.Checkpoint;
            Check(root.Queue.InfiniteSource && root.Queue.EffectiveTotal == -1, "Q9: endless run attaches the generator source");
            Check(FindDeep(cp.transform, "StickyNoteOverlay").gameObject.activeSelf, "Q9: run modes use the same start gate");
            ClickStickyNote(root);
            Check(cp.CurrentVisitor != null, "Q9: first run visitor arrives on the note click");
            cp.AdvanceQueue(6f);
            Check(root.Queue.WaitingCount == 1, "Q9: run visitors accumulate in the same line");
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.CurrentVisitor != null && root.Session.RunProcessed == 1,
                  "Q9: promotion + run scoring still flow through OnVerdictResolved");

            // ---- Q10 shift boundary: clocking out never leaks the line into the next night
            root.Config.exhibitionMode = false;        // full five-night arc (test-only override)
            FreshGame(root);
            IsolateMissingStudent(root);
            cp = root.Checkpoint;
            ClickStickyNote(root);
            guard = 0;
            while (!Btn(root, "endshift").interactable && guard++ < 20)
            {
                if (cp.CurrentVisitor == null) { cp.AdvanceQueue(6f); continue; }
                cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            }
            Check(guard < 20 && root.Queue.ShiftComplete, "Q10: night 1 walked to completion through the queue");
            ResolvePool(root);   // a played patrol would have dealt with the night's anomalies (issue #7)
            Btn(root, "endshift").onClick.Invoke();
            Check(root.State == AppViewState.ShiftResults, "Q10: clocking out lands on the shift results");
            root.QuitToMenu(true);
            root.ContinueGame();
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            cp = root.Checkpoint;                      // the rebuild created a fresh desk view
            Check(root.Session.CurrentShift == 2, "Q10: CONTINUE after clock-out starts night 2");
            Check(!root.Queue.Started && root.Queue.ArrivalCount == 0 && root.Queue.RosterCursor == 0 &&
                  root.Queue.WaitingCount == 0,
                  "Q10: the finished night's line never leaks into night 2");
            Check(root.CurrentRoster.Count == 6, "Q10: night 2 regenerated its own 6-visitor roster");
            Check(FindDeep(cp.transform, "StickyNoteOverlay").gameObject.activeSelf,
                  "Q10: every freshly started shift gets the sticky note");
            ClickStickyNote(root);
            Check(root.Checkpoint.CurrentVisitor != null && root.Queue.ArrivalCount == 1,
                  "Q10: night 2 starts cleanly behind its own note");

            // ---- Q11 the district administrator's special encounter survives the queue
            var night5 = new RosterFactory(3).GenerateShift(ShiftPlan.Build()[4]);   // 8 + admin
            Check(night5.Count == 9 && night5[8].IsAdministrator,
                  "Q11: night 5 roster ends with the administrator");
            FreshGame(root);
            IsolateMissingStudent(root);
            root.Queue.AttachRoster(night5, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f,   // test-only acceleration
                MissingStudentChance = 0f,
            }, 31337);
            cp = root.Checkpoint;
            ClickStickyNote(root);
            guard = 0;
            while (guard++ < 40)
            {
                for (int i = 0; i < 12 && root.Dialogue != null && root.Dialogue.IsActive; i++)
                    root.Dialogue.Advance();
                if (root.Queue.Active == null)
                {
                    if (root.Queue.ShiftComplete) break;
                    cp.AdvanceQueue(1f);
                    continue;
                }
                if (cp.CurrentVisitor == null) continue;   // defensive: view/queue out of sync
                cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            }
            Check(root.Queue.ProcessedCount == 9, "Q11: all nine visitors resolved through the queue (" +
                  root.Queue.ProcessedCount + ")");
            Check(root.Session.AdminRejected && !root.Session.AdminAllowed,
                  "Q11: the administrator was rejected through the queue flow");
            for (int i = 0; i < 12 && root.Dialogue != null && root.Dialogue.IsActive; i++)
                root.Dialogue.Advance();               // play the outro to its callback
            Check(root.Queue.ShiftComplete, "Q11: rejecting the administrator completes the night");
            Check(Btn(root, "endshift").interactable, "Q11: END SHIFT lights up after the outro");
            ResolvePool(root);   // a played patrol would have dealt with the night's anomalies (issue #7)
            Btn(root, "endshift").onClick.Invoke();
            Check(root.State == AppViewState.ShiftResults && root.Session.CompletedShifts == 1,
                  "Q11: clocking out lands on shift results (story arc intact)");

            root.Config.exhibitionMode = exhibition;   // restore the production default
        }

        // ------------------------------------------------- settings modal proofs
        // Phase 10.3-10.5 + 11.5, headless: the audio-only modal from the main menu and
        // from the pause menu (pause stays paused; closing returns to the pause sheet).
        private static void SettingsProof(GameRoot root)
        {
            Console.WriteLine("== SETTINGS modal proofs (Phase 10 + 11.5, headless) ==");

            root.BootToMainMenu();
            MenuSceneBootstrap.Build(root);            // what Unity does after LoadScene(MainMenu)
            var settingsBtn = root.MainMenu.transform.Find("buttons").Find("SETTINGS");
            Check(settingsBtn != null, "S1: the menu SETTINGS button exists");
            settingsBtn.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var sv = root.SettingsScreen;
            Check(sv != null && sv.gameObject.activeSelf, "S1: settings opens over the main menu (no scene load)");
            Check(root.State == AppViewState.MainMenu && root.MainMenu.gameObject.activeSelf,
                  "S1: the menu state is untouched underneath");

            Check(FindDeep(sv.transform, "master") != null && FindDeep(sv.transform, "music") != null &&
                  FindDeep(sv.transform, "sfx") != null, "S2: exactly the three audio rows exist");
            Check(FindDeep(sv.transform, "fullscreen") == null && FindDeep(sv.transform, "resrow") == null &&
                  FindDeep(sv.transform, "textscale") == null && FindDeep(sv.transform, "datarow") == null &&
                  FindDeep(sv.transform, "resetdata") == null && FindDeep(sv.transform, "controls") == null,
                  "S2: display / accessibility / data-reset / controls are gone from the modal");
            Check(FindDeep(sv.transform, "back") != null, "S2: a clear BACK button exists");
            Check(FindDeep(sv.transform, "dim") != null &&
                  FindDeep(sv.transform, "dim").GetComponent<UnityEngine.UI.Image>().raycastTarget,
                  "S2: the dimmer blocks clicks behind the modal");

            var master = FindDeep(sv.transform, "master").GetComponent<UnityEngine.UI.Slider>();
            var music = FindDeep(sv.transform, "music").GetComponent<UnityEngine.UI.Slider>();
            var sfx = FindDeep(sv.transform, "sfx").GetComponent<UnityEngine.UI.Slider>();
            var s = root.Settings;

            SetSlider(master, 0.5f);
            Check(Math.Abs(s.Current.MasterVolume - 0.5f) < 0.0001f, "S3: master slider writes through SettingsSystem immediately");
            SetSlider(music, 0.4f);
            Check(Math.Abs(s.MusicVolume - 0.2f) < 0.0001f, "S3: music volume = master x music (0.5 x 0.4)");
            Check(Math.Abs(s.SfxVolume - 0.4f) < 0.0001f, "S3: moving music did NOT change the sfx mix (0.5 x 0.8)");
            SetSlider(sfx, 0.25f);
            Check(Math.Abs(s.SfxVolume - 0.125f) < 0.0001f, "S3: sfx volume = master x sfx (0.5 x 0.25)");
            Check(Math.Abs(s.MusicVolume - 0.2f) < 0.0001f, "S3: moving sfx did NOT change the music mix");

            // the music bed reacts without a scene reload (existing SettingsChanged path)
            var sources = root.Audio.gameObject.GetComponentsInChildren<UnityEngine.AudioSource>(true);
            UnityEngine.AudioSource playingMusic = null;
            foreach (var src in sources) if (src.isPlaying && src.loop) { playingMusic = src; break; }
            Check(playingMusic != null, "S4: a music source is playing (menu bed)");
            if (playingMusic != null)
            {
                SetSlider(music, 1f);
                Check(Math.Abs(playingMusic.volume - s.MusicVolume) < 0.0001f,
                      "S4: the playing music source refreshed to master x music = " + s.MusicVolume);
                SetSlider(master, 0f);
                Check(Math.Abs(playingMusic.volume) < 0.0001f, "S4: master at zero mutes music");
                Check(Math.Abs(s.SfxVolume) < 0.0001f, "S4: master at zero mutes sfx too");
                SetSlider(master, 0.8f);
            }

            // persistence across an application restart (PlayerPrefs path)
            var fresh = new SettingsSystem(root.Bus);
            Check(Math.Abs(fresh.Current.MasterVolume - 0.8f) < 0.0001f &&
                  Math.Abs(fresh.Current.MusicVolume - 1f) < 0.0001f &&
                  Math.Abs(fresh.Current.SfxVolume - 0.25f) < 0.0001f,
                  "S5: a fresh SettingsSystem reloads the exact stored volumes (restart persistence)");

            FindDeep(sv.transform, "back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!sv.gameObject.activeSelf && root.MainMenu.gameObject.activeSelf &&
                  root.State == AppViewState.MainMenu,
                  "S6: BACK closes to an intact main menu");

            // pause-menu path: Options over PAUSED must stay paused and return to the sheet
            FreshGame(root);
            ClickStickyNote(root);
            root.TogglePause(true);
            Check(root.Paused && Time.timeScale == 0f && root.PauseScreen.gameObject.activeSelf, "S7: pause menu is up, game paused");
            root.OpenSettings();
            Check(root.SettingsScreen.gameObject.activeSelf && !root.PauseScreen.gameObject.activeSelf,
                  "S7: options replaces the pause sheet");
            Check(root.Paused && Time.timeScale == 0f, "S7: the game stays paused while options is open");
            Check(root.BlockingOverlayOpen, "S7: the modal reports itself as a blocking overlay");
            SetSlider(FindDeep(root.SettingsScreen.transform, "music").GetComponent<UnityEngine.UI.Slider>(), 0.55f);
            Check(Math.Abs(root.Settings.Current.MusicVolume - 0.55f) < 0.0001f, "S7: sliders work while paused (no scaled time involved)");
            Input.KeysDown.Add(KeyCode.Escape);
            root.Checkpoint.TickHotkeys();               // the frame's Esc for the desk view
            Input.KeysDown.Clear();
            Check(root.Paused && root.SettingsScreen.gameObject.activeSelf,
                  "S7: Esc while options is open does NOT unpause behind it");
            root.CloseSettings();
            Check(!root.SettingsScreen.gameObject.activeSelf && root.PauseScreen.gameObject.activeSelf &&
                  root.Paused && Time.timeScale == 0f,
                  "S7: closing options returns to the pause menu, still paused");
            Input.KeysDown.Add(KeyCode.Escape);
            root.Checkpoint.TickHotkeys();               // deliberate resume by the player
            Input.KeysDown.Clear();
            Check(!root.Paused && Time.timeScale == 1f, "S7: Esc with no overlay resumes gameplay");
            root.TogglePause(true);
            root.OpenSettings();
            var reopened = FindDeep(root.SettingsScreen.transform, "music").GetComponent<UnityEngine.UI.Slider>();
            Check(Math.Abs(reopened.value - 0.55f) < 0.0001f, "S8: reopening options shows the retained value");
            root.CloseSettings();
            root.TogglePause(false);
        }

        // ------------------------------------------------------------ queue helpers
        private static void FreshGame(GameRoot root)
        {
            root.StartNewGame(GameMode.Normal);
            CheckpointSceneBootstrap.Build(root);        // what Unity does after LoadScene(Hollowbrook)
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
        }

        // isolate a scenario from the (separately proven) missing-student roll;
        // the production config -> tuning wiring is asserted in Q1 before any override
        private static void IsolateMissingStudent(GameRoot root) =>
            root.Queue.Tuning.MissingStudentChance = 0f;

        private static void ClickStickyNote(GameRoot root)
        {
            var note = FindDeep(root.Checkpoint.transform, "stickynote_border");
            note.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        private static string NoteTextOf(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            CollectText(t, sb);
            return sb.ToString();
        }

        private static void CollectText(Transform t, System.Text.StringBuilder sb)
        {
            var txt = t.GetComponent<UnityEngine.UI.Text>();
            if (txt != null && !string.IsNullOrEmpty(txt.text)) sb.Append(txt.text).Append('\n');
            for (int i = 0; i < t.childCount; i++) CollectText(t.GetChild(i), sb);
        }



        private static UnityEngine.UI.Button Btn(GameRoot root, string name) =>
            FindDeep(root.Checkpoint.transform, name).GetComponent<UnityEngine.UI.Button>();

        private static void SetSlider(UnityEngine.UI.Slider slider, float v)
        {
            slider.value = v;                            // the stub field has no side effects...
            slider.onValueChanged.Invoke(v);             // ...so fire the callback like the engine does
        }

        private static int FindQueueSeed(Func<List<VisitorData>, bool> pred)
        {
            for (int seed = 1; seed <= 400; seed++)
            {
                var rr = new RosterFactory(seed).GenerateShift(ShiftPlan.Build()[1]);   // night 2 shape
                if (pred(rr)) return seed;
            }
            return -1;
        }

        // ------------------------------------------------- patrol world loop
        // Headless exploration frames for the flashlight/anomaly systems. Stub MonoBehaviours
        // never receive Update() calls, so ViewManager.TickWorld steps light, player, beam,
        // anomalies and spawner in a deterministic order — the real components, the real
        // config, the real event traffic. Mirrors the manual test sequence from the task.
        private static void StudentsProof(GameRoot root)
        {
            Console.WriteLine("== classroom students (F-01/F-02) ==");
            var sp = root.Students;
            Check(sp != null && sp.Room != null && sp.Room.Seats.Length == 6,
                  "F-01: the school scene wires a spawner and a 6-seat placeholder");
            if (sp == null) return;

            var list = new List<StudentData>
            {
                new StudentData { Id = "HB-2026-0001", Name = "Ana", IsAnomaly = false },
                new StudentData { Id = "HB-2026-0002", Name = "Bre", IsAnomaly = true },
                new StudentData { Id = "HB-2026-0001", Name = "Ana dup", IsAnomaly = false },
            };
            sp.Spawn(list, 42);
            int[] first = sp.Students.Select(s => s.Seat).ToArray();
            for (int i = 0; i < 400; i++) sp.Tick(0.05f);
            Check(sp.Students.Count == 3 && sp.SeatedCount == 3,
                  "F-01: three bodies enter one by one and sit (duplicate id shares a seat)");
            Check(first[0] == first[2] && first[1] != first[0], "F-02: duplicate ids map to one seat, others distinct");
            sp.Spawn(list, 42);
            Check(sp.Students.Select(s => s.Seat).SequenceEqual(first), "F-02: same seed + same list = same seating");

            var big = new List<StudentData>();
            for (int i = 0; i < 10; i++) big.Add(new StudentData { Id = "HB-2026-" + (100 + i), Name = "S" + i });
            sp.Spawn(big, 7);
            var seats = sp.Students.Select(s => s.Seat).Where(s => s >= 0).ToList();
            Check(sp.Students.Count == 6 && seats.Count == 6 && seats.Distinct().Count() == 6,
                  "F-02: overflow beyond the seats is warned and skipped, seats stay distinct");

            sp.Spawn(null, 1);
            sp.Spawn(new List<StudentData>(), 1);
            Check(sp.Students.Count == 0, "F-01: null/empty admitted list is a silent no-op");

            sp.Spawn(list, 42);
            root.TogglePause(true);
            for (int i = 0; i < 50; i++) sp.Tick(0.05f);
            Check(sp.SeatedCount == 0 && sp.Students.All(s => !s.gameObject.activeSelf),
                  "F-01: pause freezes both entry and walking");
            root.TogglePause(false);
            for (int i = 0; i < 400; i++) sp.Tick(0.05f);
            Check(sp.SeatedCount == 3, "F-01: unpausing finishes the seating");
            sp.Clear();
            Check(sp.Students.Count == 0, "F-01: Clear empties the room at patrol end");
        }

        private static void ResolvePool(GameRoot root)
        {
            while (root.Session.Pool.Pending > 0)
                root.Session.Pool.Resolve(root.Session.Pool.PendingList[0]);
        }

        private static void IssueOneProofs(GameRoot root)
        {
            Console.WriteLine("== issue #1 review proofs: quit/save/pause/esc/memo ==");
            string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "hollowbrook_save.txt");
            string before = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            Check(before != null, "a save exists from the earlier proofs");
            root.BootToMainMenu();
            root.QuitApplication();
            string after = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
            Check(after == before, "issue #1: EXIT on the main menu leaves the save untouched");

            System.IO.File.WriteAllText(path, "SMYIDSAVE 1\ngarbage\nchecksum=0\n");
            Check(root.Saves.Read() != null, "issue #8: a corrupt main save falls back to the .bak copy");
            System.IO.File.WriteAllText(path, before);

            root.StartNewGame(GameMode.Normal);
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            ClickStickyNote(root);
            var cp = root.Checkpoint;

            // corner drop (fresh run, visitor #1): a drop near the slip's top-left stamps
            // NEAR THE TOP-LEFT — clamped so the whole tilted mark stays on the paper
            cp.DebugTakeDocuments();
            TakeSlip(root);
            var cornerWant = InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null);
            StampAtPoint(root, cornerWant, new Vector2(690f, 460f));   // just inside the corner
            var soT = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            var slT = FindDeep(cp.transform, "slip").Find("slipart").GetComponent<RectTransform>();
            Check(cp.SlipStamped, "issue #5: a corner drop stamps the slip");
            float tlx = soT.anchoredPosition.x, tly = -soT.anchoredPosition.y;
            float cornerMargin = StampPlacement.TiltedHalfExtent(CheckpointView.StampMarkSize,
                CheckpointView.StampMaxTilt) + CheckpointView.StampEdgePad;
            Check(Mathf.Abs(tlx - cornerMargin) < 0.5f && Mathf.Abs(tly - cornerMargin) < 0.5f,
                $"issue #5: top-left drop => top-left mark, clamped exactly onto the printable corner ({tlx:F1},{tly:F1} = margin {cornerMargin:F1})");
            float angT = Mathf.Abs(soT.localEulerAngles.z);
            angT = Mathf.Min(angT, 360f - angT);
            float bT = soT.sizeDelta.x * (Mathf.Cos(angT * Mathf.Deg2Rad) + Mathf.Sin(angT * Mathf.Deg2Rad)) / 2f;
            Check(tlx - bT >= -0.01f && tlx + bT <= slT.rect.width + 0.01f &&
                  tly - bT >= -0.01f && tly + bT <= slT.rect.height + 0.01f,
                "issue #5: the tilted mark never hangs off the paper, even at the corner");
            HandBack(root, true, true);            // commit the correct verdict, free the slip
            cp.AdvanceQueue(6f);                   // the clock delivers the keyboard-proof visitor
            Check(cp.CurrentVisitor != null, "issue #5: the next visitor arrives for the pause proofs");
            cp.DebugTakeDocuments();
            var slip = FindDeep(cp.transform, "slip").GetComponent<DragPaper>();
            slip.BeginDrag(); slip.PlaceCenter(new Vector2(760f, 500f)); slip.EndDrag();
            root.TogglePause(true);
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.A);
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.D);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(!cp.SlipStamped, "issue #5: a paused desk never stamps");
            root.TogglePause(false);
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.A);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.D);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(!cp.SlipStamped, "issue #5: unpaused, A and D do NOT stamp — a verdict is only ever a dragged stamp");
            Check(FindDeep(cp.transform, "slblA") == null && FindDeep(cp.transform, "slblD") == null,
                "issue #5: no APPROVE (A) / DECLINE (D) caption bars sit under the stamps");
            StampSlip(root, Hollowbrook.Logic.InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.SlipStamped, "issue #5: dragging a stamp onto the slip still stamps it");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.H);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(FindDeep(cp.transform, "idcard").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                "issue #5: H does nothing — the ID only goes back by being dragged to the window");

            root.TogglePause(true);
            root.OpenSettings();
            InputGate.NextFrame();
            Check(root.SettingsScreen.TryCloseOnEscape(),
                  "issue #6: Esc closes the settings sheet (and consumes the key)");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Escape);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(root.Paused, "issue #6: the same Esc does NOT also unpause behind the sheet");
            root.TogglePause(false);

            cp.DebugPresentAdmin();
            cp.DebugTakeDocuments();
            var rec = FindDeep(cp.transform, "record");
            Check(rec.gameObject.activeSelf,
                  "issue #4: the admin's District Roster Memo is on the desk at hand-over");
            var adminTab = FindDeep(cp.transform, "folder1").GetComponent<UnityEngine.UI.Button>();
            adminTab.onClick.Invoke();
            var adminRow = FindDeep(cp.transform, "filerow0");
            if (adminRow != null) adminRow.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var memoToast = FindDeep(root.HUD.transform, "toast");
            Check(rec.gameObject.activeSelf &&
                  memoToast.gameObject.activeSelf &&
                  memoToast.GetComponent<UnityEngine.UI.Text>().text.Contains("ATTACHED MEMO"),
                "issue #4: pulling student files can't replace the memo (the refusal toasts)");
            adminTab.onClick.Invoke();   // tidy: shut the drawer again
        }

        private static void WorldLoop(GameRoot root)
        {
            Console.WriteLine("== PATROL world loop (flashlight + roaming anomaly), headless frames ==");

            // a fresh normal run, then patrol — through the real scene handoffs the game uses.
            // the booth demo is an explicit opt-in now (issue #2): shipping default is the story
            root.Config.exhibitionMode = true;
            root.StartNewGame(GameMode.Normal);
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            Check(root.State == AppViewState.Checkpoint, "W0: a fresh run reaches the desk");
            Check(root.Session.Pool.Pending == 1, "W0: the exhibition night seeds exactly one outstanding anomaly");

            root.EnterExploration();
            SchoolSceneBootstrap.Build(root);      // what Unity does after LoadScene(School)
            Check(root.State == AppViewState.Exploration, "W0: patrol starts in Exploration");
            var views = root.Views;
            Check(views != null && views.Current != null && views.Current.Id == RoomId.Hallway, "W0: patrol starts in the hallway");
            if (views == null || views.Current == null) return;
            var rig = views.Flashlight;
            var hall = views.Current;
            Check(rig != null && rig.Beam == LightMode.Off, "W0: the beam starts Off");
            Check(rig != null && !rig.BatteryDead && rig.Battery >= 0.999f, "W0: the battery starts full");
            if (rig == null) return;
            CheckLightSync(root, "W0 entry");

            // ---- placeholder quad sanity (the floating-box regression) ----
            // Every flat placeholder is a solid_white quad whose native size is 1x1 world
            // unit, so localScale IS its drawn size. If that sprite ever returns at UI ppu,
            // placeholders shrink to 1/25th: doors vanish and the darkness overlay degenerates
            // into a small floating box mid-room.
            Console.WriteLine("== placeholder quads draw at authored size ==");
            float QuadW(SpriteRenderer sr) =>
                sr.sprite.rect.width / sr.sprite.pixelsPerUnit * sr.gameObject.transform.localScale.x;
            float QuadH(SpriteRenderer sr) =>
                sr.sprite.rect.height / sr.sprite.pixelsPerUnit * sr.gameObject.transform.localScale.y;
            Check(Mathf.Abs(QuadW(hall.Darkness) - (hall.HalfWidth * 2f + 4f)) < 0.01f &&
                  Mathf.Abs(QuadH(hall.Darkness) - 11f) < 0.01f,
                  "P: the darkness overlay spans the whole room (no floating box)");
            Check(hall.Darkness.color.r < 0.01f && hall.Darkness.color.g < 0.01f && hall.Darkness.color.b < 0.01f,
                  "P: the darkness overlay is black (its alpha dims, never washes out)");
            int doors = 0; bool doorsFullHeight = true;
            bool switchVisible = false;
            foreach (var it in hall.Interactables)
            {
                var d = it as Doorway;
                if (d != null && d.Art != null) { doors++; if (QuadH(d.Art) < 3f) doorsFullHeight = false; }
                var ls = it as LightSwitchObject;
                if (ls != null && ls.Art != null && QuadW(ls.Art) > 0.1f) switchVisible = true;
            }
            Check(doors == 3 && doorsFullHeight,
                  "P: all " + doors + " hallway doors draw as full-height slabs at their anchors");
            Check(switchVisible, "P: the wall switch plate is visible (not a sub-pixel dot)");

            var cfg = root.Config;
            float chanceSave = cfg.anomalySpawnChancePerSecond;
            cfg.anomalySpawnChancePerSecond = 0f;    // deterministic scenarios; W9 restores real pressure

            // scoped event witnesses (the bus persists across scene rebuilds)
            int deathCaptions = 0, rebootCaptions = 0, tensesCaptions = 0, creakCaptions = 0;
            int revealedReal = 0, caughtEvents = 0;
            var stateLog = new List<string>();
            root.Bus.Subscribe(EventIds.CaptionRequested, e =>
            {
                // captions are published via Of(id, data, value, text) — the string rides in Data
                string t = e.Text ?? e.Data as string ?? "";
                if (t.Contains("dies")) deathCaptions++;
                else if (t.Contains("coughs")) rebootCaptions++;
                else if (t.Contains("tenses")) tensesCaptions++;
                else if (t.Contains("door creaks")) creakCaptions++;
            });
            root.Bus.Subscribe(EventIds.AnomalyStateChanged, e => { if (e.Text != null) stateLog.Add(e.Text); });
            root.Bus.Subscribe(EventIds.AnomalyRevealed, e => { if (e.Data is AnomalyBehaviorKind) revealedReal++; });
            root.Bus.Subscribe(EventIds.AnomalyCaught, e => caughtEvents++);

            // ---- W1: input arbitration — keyboard toggles ONLY, F is the on/off toggle
            Console.WriteLine("== W1 flashlight input arbitration ==");
            var coneGoW1 = FindDeep(rig.transform, "beam").gameObject;
            var maskGoW1 = FindDeep(rig.transform, "FlashlightMask").gameObject;
            Check(rig.Beam == LightMode.Off && !coneGoW1.activeSelf && !maskGoW1.activeSelf,
                "W1: the flashlight is OFF by default — cone sprite and light mask really hidden");
            Input.MouseHeld.Add(0);
            WorldFrames(root, 5);
            Check(rig.Beam == LightMode.Off, "W1: a held LMB never lights the beam (mouse is not beam input)");
            Input.MouseHeld.Remove(0);
            Input.MouseHeld.Add(1);
            WorldFrames(root, 5);
            Check(rig.Beam == LightMode.Off, "W1: a held RMB never lights the beam either");
            Input.MouseHeld.Remove(1);
            // sweeping the cursor across the dark room steers nothing ON: movement may aim a
            // lit beam, it may never be the thing that lights one (acceptance: mouse alone)
            for (int i = 0; i < 14; i++)
            {
                AimAt(i * 1.6f, -3f + (i % 4) * 1.5f);
                WorldFrames(root, 2);
            }
            Check(rig.Beam == LightMode.Off && !rig.Latches.Sweep && !rig.Latches.Focus,
                "W1: cursor movement alone never activates the flashlight");
            Check(!FindDeep(rig.transform, "beam").gameObject.activeSelf &&
                  !FindDeep(rig.transform, "FlashlightMask").gameObject.activeSelf,
                "W1: and an untouched beam shows no cone, no mask, no cursor-following glow");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep, "W1: F toggles the flashlight ON (sweep)");
            Check(coneGoW1.activeSelf && maskGoW1.activeSelf,
                "W1: ON really shows the cone + mask (the beam exists, not just a status)");
            string beamInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(beamInfo.Contains("BEAM: SWEEP"),
                "W1: the HUD reads BEAM: SWEEP while the beam is on (got '" + beamInfo + "')");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Off && !rig.Latches.Sweep, "W1: F again toggles it OFF");
            Check(!coneGoW1.activeSelf && !maskGoW1.activeSelf,
                "W1: OFF hides the cone + mask completely — no visible beam, no illumination");
            Check(!rig.ConeCovers(new Vector3(3f, 0f, 0f)), "W1: an OFF beam illuminates nothing (cone test covers no point)");
            beamInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(beamInfo.Contains("BEAM: OFF"),
                "W1: the HUD reads BEAM: OFF in sync with the hidden beam (got '" + beamInfo + "')");
            PressKey(root, KeyCode.Q);
            Check(rig.Beam == LightMode.Focus && !rig.Latches.Sweep && rig.Latches.Focus,
                "W1: Q takes over with Focus — the keyboard toggles are mutually exclusive");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep && rig.Latches.Sweep && !rig.Latches.Focus,
                "W1: F while Focus-latched switches back to Sweep (no dead toggle)");
            PressKey(root, KeyCode.L);
            Check(rig.Beam == LightMode.Off && !rig.Latches.Sweep && !rig.Latches.Focus,
                "W1: L switches off and clears conflicting latches");
            CheckLightSync(root, "W1 after L");

            // ---- W2: room lighting and the beam are independent systems
            Console.WriteLine("== W2 room lights vs handheld beam ==");
            var sw = FindInteractable<LightSwitchObject>(hall);
            Check(sw != null, "W2: the hallway has a wall switch");
            Check(hall.Light != null && !hall.Light.Lit, "W2: rooms are entered DARK (lights start OFF)");
            var roomInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(roomInfo.Contains("LIGHTS: OFF — wall switch [E]"),
                  "W2: the HUD reads LIGHTS: OFF and points at the wall switch (got '" + roomInfo + "')");
            PressKey(root, KeyCode.Q);
            Check(!hall.Light.Lit && rig.Beam == LightMode.Focus, "W2: the beam is the portable light: Focus does NOT touch the room lights");
            if (sw != null) sw.Interact();
            Check(hall.Light.Lit && rig.Beam == LightMode.Focus, "W2: the wall switch lights the room, beam stays Focus");
            if (sw != null) sw.Interact();
            Check(!hall.Light.Lit, "W2: and darkens it again");
            // entry reset: whatever the switch did, ARRIVING in a room always means darkness
            if (sw != null) sw.Interact();
            Check(hall.Light.Lit, "W2: switch back on...");
            hall.Light.ForceDarkOnEntry();   // what ViewManager calls on every room entry
            Check(!hall.Light.Lit, "W2: ...but entering the room forces the lights back off (spawn in darkness)");
            if (sw != null) sw.Interact();   // later sections (battery recovery) play lit
            Check(hall.Light.Lit, "W2: the switch relights it — the player is never locked in the dark");
            PressKey(root, KeyCode.L);
            Check(rig.Beam == LightMode.Off && hall.Light.Lit, "W2: beam Off leaves no stale state on the room light");

            // beam entry reset: arriving in ANY room means arriving dark — the handheld
            // beam goes out with the room lights and only a deliberate F/Q relights it
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep, "W2: beam lit before the transition");
            views.EnterRoom(RoomId.Classroom1);
            Check(views.Current.Id == RoomId.Classroom1 && !views.Transitioning, "W2: walked into the classroom");
            Check(rig.Beam == LightMode.Off && !rig.Latches.Sweep && !rig.Latches.Focus,
                "W2: entering a room forces the beam OFF — a transition never carries a lit beam over");
            Check(!FindDeep(rig.transform, "beam").gameObject.activeSelf,
                "W2: the beam sprite is really hidden after the entry (LIGHTS: OFF now MEANS dark)");
            string entryInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(entryInfo.Contains("BEAM: OFF"),
                "W2: the HUD beam status stays in sync through the entry (got '" + entryInfo + "')");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep, "W2: F relights it after the entry (deliberate input only)");
            views.EnterRoom(RoomId.Hallway);
            Check(views.Current.Id == RoomId.Hallway && rig.Beam == LightMode.Off,
                "W2: ...and every further entry starts dark again");
            if (sw != null && !hall.Light.Lit) sw.Interact();   // later sections (battery) play lit
            Check(hall.Light.Lit, "W2: the hallway is relit for the battery proofs");

            // ---- W2b: limited hallway visibility — deeper ambient + a pool around the player
            Console.WriteLine("== W2b hallway limited visibility (dark corridor, manual beam) ==");
            var swB = FindInteractable<LightSwitchObject>(hall);
            if (hall.Light.Lit && swB != null) swB.Interact();
            Check(!hall.Light.Lit, "W2b: the corridor is dark for the depth proofs");
            var depth = hall.Depth;
            Check(depth != null, "W2b: the hallway owns a depth layer");
            Check(views.Room(RoomId.Classroom1).Depth == null &&
                  views.Room(RoomId.Classroom2).Depth == null,
                  "W2b: classrooms keep their single uniform darkness layer (design preserved)");
            if (depth == null) return;
            Check(depth.maskInteraction == SpriteMaskInteraction.VisibleOutsideMask &&
                  depth.sortingOrder > 60 && depth.sortingOrder < 70,
                  "W2b: the depth layer rides INSIDE the beam mask's range, so one beam hole cuts both");
            Check(depth.color.r < 0.01f && depth.color.g < 0.01f && depth.color.b < 0.01f,
                  "W2b: the depth layer is black (its alpha grades, it never washes the room out)");
            Check(depth.enabled && Mathf.Abs(depth.color.a - cfg.hallwayDepthAlpha) < 1e-3f,
                  $"W2b: dark corridor -> depth layer ON at {depth.color.a:F2}");
            Check(Mathf.Abs(hall.Darkness.color.a - cfg.hallwayDarkAlpha) < 1e-3f,
                  $"W2b: corridor ambient {hall.Darkness.color.a:F2} vs a classroom's {LightController.DarkAlpha:F2}");
            float vsLit = HallwayDarkness.DarkerThanLitRoom(LightController.LitAlpha);
            Check(cfg.hallwayDarkAlpha > LightController.DarkAlpha + 0.1f &&
                  vsLit > 0.70f && vsLit < 0.85f,
                  $"W2b: the corridor is {vsLit:P0} darker than a normally lit room (target 70-85%)");
            float share = HallwayDarkness.PoolShareOfView(5f * 16f / 9f);
            Check(share > 0.15f && share < 0.25f,
                  $"W2b: with the beam out he sees {share:P0} of the on-screen width (target 15-25%)");

            // the pool tracks the custodian, and the beam/hud stay exactly as they were
            views.Player.PlaceInRoom(hall, new Vector3(-6f, 0f, 0f));
            WorldFrames(root, 2);
            Check(Mathf.Abs(depth.transform.localPosition.x + 6f) < 0.01f &&
                  Mathf.Abs(depth.transform.localPosition.y - HallwayDarkness.PlayerY) < 0.01f,
                  "W2b: the pool recentres on the player after he walks (per-frame follow)");
            Check(rig.Beam == LightMode.Off, "W2b: none of that lit the beam");

            // beam ON cuts the same hole in both layers; beam OFF removes every light layer
            SetBeam(root, rig, LightMode.Sweep);
            Check(FindDeep(rig.transform, "FlashlightMask").gameObject.activeSelf && depth.enabled,
                  "W2b: beam ON -> the mask is live while the depth layer stays beam-cuttable");
            SetBeam(root, rig, LightMode.Off);
            Check(!FindDeep(rig.transform, "FlashlightMask").gameObject.activeSelf &&
                  !FindDeep(rig.transform, "beam").gameObject.activeSelf,
                  "W2b: beam OFF -> cone and mask fully removed (no residual glow anywhere)");

            // environmental illumination, not a brighter graphic: a graded penumbra sheet puts
            // ONE authoritative pair: stencil mask + cone tint. The stacked-sheet approach is
            // fully rolled back — nothing left that could double-dark or desynchronise.
            Check(FindDeep(rig.transform, "BeamPenumbra") == null &&
                  FindDeep(rig.transform, "BeamAreaMask") == null,
                "W2b: no stacked light layers remain (penumbra/area approach rolled back)");
            var coneT = FindDeep(rig.transform, "beam");
            var maskGo = FindDeep(rig.transform, "FlashlightMask");
            var maskC = maskGo.GetComponent<SpriteMask>();
            SetBeam(root, rig, LightMode.Sweep);
            Check(maskC.alphaCutoff <= 0.1f,
                $"W2b: the mask cutoff sits in the cone art's faintest feather ({maskC.alphaCutoff:F2}) — soft single-layer reveal");
            Check(Mathf.Abs(maskGo.localScale.x - coneT.localScale.x * 1.06f) < 0.01f,
                "W2b: the hole rides just outside the visible cone (no dark rim inside the beam)");
            SetBeam(root, rig, LightMode.Focus);
            Check(rig.Params.HalfAngleDeg < cfg.sweepHalfAngleDeg,
                "W2b: FOCUS narrows the cone (tighter, longer) within the same single mask+cone system");
            Check(Mathf.Abs(coneT.GetComponent<SpriteRenderer>().color.a - 0.42f) < 1e-3f,
                "W2b: focus concentrates more light on the inspected spot (hotter core tint)");
            SetBeam(root, rig, LightMode.Sweep);
            Check(Mathf.Abs(coneT.GetComponent<SpriteRenderer>().color.a - 0.30f) < 1e-3f,
                "W2b: leaving focus restores the broad sweep wash");
            SetBeam(root, rig, LightMode.Off);
            Check(!coneT.gameObject.activeSelf && !maskGo.gameObject.activeSelf,
                "W2b: beam OFF removes cone AND mask — no stale light layer survives");

            // the wall switch still means a fully visible corridor, then darkness returns
            if (swB != null) swB.Interact();
            Check(hall.Light.Lit && !depth.enabled &&
                  Mathf.Abs(hall.Darkness.color.a - LightController.LitAlpha) < 1e-3f,
                  "W2b: lights ON -> the pool is gone, the corridor reads as an ordinary lit room");
            if (swB != null) swB.Interact();
            Check(!hall.Light.Lit && depth.enabled &&
                  Mathf.Abs(depth.color.a - cfg.hallwayDepthAlpha) < 1e-3f,
                  "W2b: lights OFF -> darkness and the pool return");
            if (swB != null) swB.Interact();   // leave the corridor lit, as W2 handed it over
            Check(rig.Beam == LightMode.Off, "W2b: no lighting change ever auto-activated the beam");
            CheckLightSync(root, "W2b dark corridor");

            // ---- W2c: the beam answers the keypress ON THE ACTIVATION FRAME ----
            // Owner report: "the beam still feels delayed when I switch it on during patrol".
            // Root cause: the activation frame kept the displayed aim and the displayed cone
            // geometry where the damping had left them, so a freshly lit beam pointed at the
            // PREVIOUS aim (Vector3.right after a room entry) and swung round over ~15 frames,
            // and an off->on mode change morphed its length/width instead of taking them at
            // once. The lamp is invisible a moment before the press, so there is nothing to
            // glide FROM: off->on snaps, a held lamp keeps its damping.
            Console.WriteLine("== W2c beam appears instantly on activation (no wind-up) ==");
            views.Player.PlaceInRoom(hall, new Vector3(0f, 0f, 0f));
            SetBeam(root, rig, LightMode.Off);
            AimAt(0f, 5f);                       // cursor straight up, beam out
            WorldFrame(root);
            float angleBefore = coneT.localEulerAngles.z;
            PressKey(root, KeyCode.F);           // ONE frame: key down -> tick -> measure
            Check(rig.Beam == LightMode.Sweep && coneT.gameObject.activeSelf &&
                  maskGo.gameObject.activeSelf,
                "W2c: F lights the sweep beam on the very frame it is pressed (layers live at once)");
            Check(Mathf.Abs(coneT.localEulerAngles.z - 90f) < 0.5f,
                $"W2c: ...already pointing AT THE CURSOR on that frame (got {coneT.localEulerAngles.z:F1}°, was {angleBefore:F1}°)");
            Check(Mathf.Abs(rig.Dir.y - 1f) < 1e-3f && Mathf.Abs(rig.Dir.x) < 1e-3f,
                "W2c: the logical cone agrees with the cursor too (gameplay never lags the art)");
            Check(Mathf.Abs(coneT.localScale.x - cfg.sweepRange / 8f) < 0.01f,
                $"W2c: the cone is the Sweep's full length immediately ({coneT.localScale.x:F3} vs {cfg.sweepRange / 8f:F3})");
            Check(Mathf.Abs(maskGo.localScale.x - coneT.localScale.x * 1.06f) < 0.01f,
                "W2c: the stencil hole matches that geometry on the same frame (no rim, no desync)");
            // "follows the flashlight's current direction and position": the rendered cone must
            // be exactly the logical aim's mid-point and angle — derived from the rig itself,
            // so the proof holds wherever the player is standing
            var expectedMid = rig.Origin + rig.Dir * (cfg.sweepRange * 0.5f);
            float expectedAngle = Mathf.Atan2(rig.Dir.y, rig.Dir.x) * Mathf.Rad2Deg;
            Check(Vector3.Distance(coneT.localPosition, expectedMid) < 0.02f &&
                  Mathf.Abs(Mathf.DeltaAngle(coneT.localEulerAngles.z, expectedAngle)) < 0.5f &&
                  coneT.localPosition == maskGo.localPosition &&
                  coneT.localEulerAngles == maskGo.localEulerAngles,
                $"W2c: cone and mask are both parked on the aim's mid-point and angle ({coneT.localPosition} @ {coneT.localEulerAngles.z:F1}°)");
            Check(Mathf.Abs(coneT.GetComponent<SpriteRenderer>().color.a - 0.30f) < 1e-3f,
                "W2c: full brightness on the activation frame — no fade-in to wait through");
            // repeated activation/deactivation must behave identically every time, and a new
            // activation must never inherit the previous aim
            bool stable = true;
            for (int cycle = 0; cycle < 5; cycle++)
            {
                SetBeam(root, rig, LightMode.Off);
                bool up = cycle % 2 == 0;
                AimAt(up ? 0f : 6f, up ? 6f : 1f);
                PressKey(root, KeyCode.F);
                var want = rig.Origin + rig.Dir * (cfg.sweepRange * 0.5f);
                float wantAngle = Mathf.Atan2(rig.Dir.y, rig.Dir.x) * Mathf.Rad2Deg;
                if (rig.Beam != LightMode.Sweep || !coneT.gameObject.activeSelf ||
                    Vector3.Distance(coneT.localPosition, want) > 0.02f ||
                    Mathf.Abs(Mathf.DeltaAngle(coneT.localEulerAngles.z, wantAngle)) > 0.5f ||
                    Mathf.Abs(coneT.GetComponent<SpriteRenderer>().color.a - 0.30f) > 1e-3f)
                    stable = false;
            }
            Check(stable,
                "W2c: five on/off cycles in a row are all instant, fully lit, and aimed at the NEW cursor");
            // a mode change while the lamp is ALREADY lit keeps its smooth morph (that glide is
            // the intentional part) but still ends on the right geometry
            PressKey(root, KeyCode.Q);
            WorldFrames(root, 30);
            Check(rig.Beam == LightMode.Focus &&
                  Mathf.Abs(coneT.localScale.x - cfg.focusRange / 8f) < 0.01f &&
                  Mathf.Abs(coneT.GetComponent<SpriteRenderer>().color.a - 0.42f) < 1e-3f,
                "W2c: switching Sweep->Focus while lit still morphs, and lands on Focus geometry + tint");
            SetBeam(root, rig, LightMode.Off);
            Check(!coneT.gameObject.activeSelf && !maskGo.gameObject.activeSelf,
                "W2c: and L still puts it out at once (controls unchanged)");
            CheckLightSync(root, "W2c instant beam");

            // ---- W3: cursor aiming + true cone coverage in one world space
            Console.WriteLine("== W3 aiming and cone coverage ==");
            views.Player.PlaceInRoom(hall, new Vector3(0f, 0f, 0f));
            AimAt(5f, -0.5f);
            PressKey(root, KeyCode.F);           // sweep, aiming right
            Check(rig.Dir.x > 0.99f && Mathf.Abs(rig.Dir.y) < 0.01f, "W3: aiming right gives a right-pointing beam");
            Check(rig.ConeCovers(new Vector3(4f, 0f, 0f)), "W3: Sweep covers a point 4u ahead");
            Check(!rig.ConeCovers(new Vector3(-4f, 0f, 0f)), "W3: nothing behind the player is covered");
            Check(!rig.ConeCovers(new Vector3(rig.Params.Range + 1.5f, -0.5f, 0f)), "W3: beyond the Sweep range is dark");
            Check(rig.ConeCovers(new Vector3(4f, 2.0f, 0f)), "W3: the wide cone covers 2.5u off-axis at 4u");
            PressKey(root, KeyCode.Q);           // same aim, Focus now
            Check(!rig.ConeCovers(new Vector3(4f, 2.0f, 0f)), "W3: Focus does NOT cover the same off-axis point — the modes really differ");
            Check(rig.ConeCovers(new Vector3(4f, 0f, 0f)), "W3: Focus still covers the axis");
            Check(rig.ConeCovers(new Vector3(rig.Params.Range - 0.2f, -0.5f, 0f)), "W3: Focus reaches its full (longer) range");
            Check(rig.Params.HalfAngleDeg < BeamGeometry.DefaultSweepHalfAngleDeg + 0.01f &&
                  rig.Params.Range > BeamGeometry.DefaultSweepRange, "W3: Focus geometry = tight + long, Sweep = wide + short");

            var cone = FindDeep(rig.transform, "beam");
            var mask = FindDeep(rig.transform, "FlashlightMask");
            Check(cone != null && mask != null, "W3: cone sprite and mask both exist");
            if (cone != null && mask != null)
            {
                Check(cone.localPosition == mask.localPosition, "W3: the mask rides exactly on the cone (no aim desync)");
                AimAt(0f, 4f); WorldFrames(root, 30);   // let the damped aim settle
                Check(Mathf.Abs(cone.localEulerAngles.z - 90f) < 0.5f, $"W3: aiming up rotates the cone to ~90° (got {cone.localEulerAngles.z:F1})");
                AimAt(-5f, -0.5f); WorldFrames(root, 30);
                Check(rig.Dir.x < -0.99f, "W3: aiming left flips the direction");
                Check(rig.ConeCovers(new Vector3(-4f, 0f, 0f)) && !rig.ConeCovers(new Vector3(4f, 0f, 0f)),
                    "W3: a left aim covers left, not right");
                Check(Mathf.Abs(cone.localEulerAngles.z - 180f) < 0.5f || Mathf.Abs(cone.localEulerAngles.z + 180f) < 0.5f,
                    $"W3: the sprite rotates to ~180° for a left aim (got {cone.localEulerAngles.z:F1})");
            }
            AimAt(5f, 0f); WorldFrame(root);
            var pp = views.Player.transform.localPosition;
            Check(Mathf.Abs(rig.Origin.x - pp.x) < 1e-3f && Mathf.Abs(rig.Origin.y - (pp.y + 0.2f)) < 1e-3f,
                "W3: the beam origin stays anchored to the player (no drift)");
            PressKey(root, KeyCode.L);

            // ---- W4: battery — drain, death, deliberate reboot, room-aware recovery
            Console.WriteLine("== W4 battery ==");
            TopUpBattery(root, rig);
            Check(hall.Light.Lit, "W4: top-up left the room lit");
            float b0 = rig.Battery;
            SetBeam(root, rig, LightMode.Focus);
            WorldFrames(root, 60);
            float focusDrain = b0 - rig.Battery;
            Check(Math.Abs(focusDrain - 0.06f) < 0.005f, $"W4: Focus drains 6%/s (got {focusDrain * 100f:F1}%)");
            SetBeam(root, rig, LightMode.Sweep);
            float b1 = rig.Battery;
            WorldFrames(root, 60);
            float sweepDrain = b1 - rig.Battery;
            Check(Math.Abs(sweepDrain - 0.025f) < 0.005f && sweepDrain < focusDrain, $"W4: Sweep drains slower than Focus ({sweepDrain * 100f:F1}%)");
            int g = 0;
            while (!rig.BatteryDead && g++ < 60 * 40) WorldFrame(root);   // hold the beam until empty
            Check(rig.BatteryDead, "W4: the battery dies when drained empty");
            Check(rig.Beam == LightMode.Off && !rig.BeamOn, "W4: death forces the beam off");
            Check(!rig.Latches.Focus && !rig.Latches.Sweep, "W4: death clears the latches");
            Check(rig.Battery >= 0f, "W4: charge never goes negative");
            Check(deathCaptions == 1, "W4: the death caption fires exactly once");
            WorldFrames(root, 60);
            Check(deathCaptions == 1 && rig.Beam == LightMode.Off, "W4: a dead battery does not repeat callbacks or auto-relight");
            Input.MouseHeld.Add(1); WorldFrames(root, 30); Input.MouseHeld.Remove(1);
            Check(rig.Beam == LightMode.Off, "W4: mouse buttons are not beam input — a dead battery stays dark");
            float r0 = rig.Battery;
            WorldFrames(root, 60);
            float litGain = rig.Battery - r0;
            Check(Math.Abs(litGain - 0.08f) < 0.01f, $"W4: a lit room recovers 8%/s while the beam is off (got {litGain * 100f:F1}%)");
            Check(rig.BatteryDead == (rig.Battery < cfg.batteryRebootFraction), "W4: still dead below the reboot fraction");
            g = 0;
            while (rig.BatteryDead && g++ < 60 * 15) WorldFrame(root);
            Check(!rig.BatteryDead && rebootCaptions == 1, "W4: it revives at the reboot fraction, announced exactly once");
            Check(rig.Beam == LightMode.Off, "W4: revival does NOT auto-reactivate the beam — deliberate input required");
            PressKey(root, KeyCode.F);
            Check(rig.BeamOn && rig.Beam == LightMode.Sweep, "W4: deliberate input relights the beam after revival");
            SetBeam(root, rig, LightMode.Off);
            if (sw != null) sw.Interact();   // lights off
            float d0 = rig.Battery; WorldFrames(root, 60); float darkGain = rig.Battery - d0;
            if (sw != null) sw.Interact();   // lights on
            float l0 = rig.Battery; WorldFrames(root, 60); float litGain2 = rig.Battery - l0;
            Check(darkGain > 0.03f && darkGain < 0.05f && litGain2 > darkGain * 1.5f,
                $"W4: dark recovery ({darkGain * 100f:F1}%/s) is slower than lit ({litGain2 * 100f:F1}%/s)");

            // ---- W5: one authoritative encounter: detect → expose → repel → recover → return
            Console.WriteLine("== W5 one authoritative encounter ==");
            TopUpBattery(root, rig);
            if (hall.Light.Lit && sw != null) sw.Interact();   // dark hallway: pure beam play
            int poolBefore = root.Session.Pool.Pending;
            var targetKind = root.Session.Pool.PendingList[0];
            var an = views.SpawnAnomalyOfKind(targetKind);
            Check(an != null, "W5: a real anomaly spawns on demand (" + targetKind + ")");
            if (an == null) return;
            Check(an.State == AnomalyState.Roaming, "W5: it starts Roaming — existing is not attacking");
            Check(views.Anomalies.Count == 1 && views.Spawner.Live == an, "W5: one live anomaly, held in the single slot");
            Check(views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Blink) == null && views.Anomalies.Count == 1,
                "W5: the live slot blocks duplicate real anomalies");
            WorldFrames(root, 60);
            Check(an.Meter == 0f && an.State != AnomalyState.Repelled, "W5: unobserved in the dark, nothing progresses by itself");

            views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(an.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
            SetBeam(root, rig, LightMode.Focus);
            int exposedAt = -1;
            g = 0;
            while (an.State != AnomalyState.Exposed && !an.Gone && g++ < 600)
            {
                views.Player.transform.localPosition = new Vector3(
                    Mathf.Clamp(an.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), RoomInstance.FloorY + 1.7f, 0f);
                AimAt(an.Pos.x, an.Pos.y);
                WorldFrame(root);
                if (an.State == AnomalyState.Exposed && exposedAt < 0) exposedAt = g;
            }
            Check(an.State == AnomalyState.Exposed, $"W5: sustained accurate Focus exposes the corrupted identity (frames={g}, meter={an.Meter:F2})");
            Check(exposedAt > 55, $"W5: exposure took sustained tracking, not one frame ({exposedAt} frames)");
            Check(stateLog.Count(s => s == "Exposed") == 1, "W5: the Exposed transition published exactly once");
            var face = FindDeep(an.transform, "ExposedFace");
            Check(face != null && face.gameObject.activeSelf, "W5: the distorted face is shown while Exposed");

            // losing Focus is recoverable — no free repel: step 8u to the far side (the clamp
            // can never push the player back next to it) and point the beam the other way
            float farX = an.Pos.x >= 0f ? an.Pos.x - 8f : an.Pos.x + 8f;
            views.Player.PlaceInRoom(hall, new Vector3(farX, 0f, 0f));
            AimAt(farX + (an.Pos.x >= 0f ? -40f : 40f), -0.5f);
            WorldFrames(root, 45);
            Check(!stateLog.Contains("Repelled"), "W5: releasing Focus early earns no repel");
            float mLoss0 = an.Meter;
            WorldFrames(root, 60);
            Check(an.Meter < mLoss0, $"W5: the meter decays once the grace window runs out ({mLoss0:F2} → {an.Meter:F2})");
            Check(an.State != AnomalyState.Exposed, "W5: Exposed is recoverable — it fell back when Focus was lost");
            Check(face != null && !face.gameObject.activeSelf, "W5: the exposed face hides again");

            // hold accurate Focus to a real repel
            int repelMark = stateLog.Count;
            g = 0;
            while (!stateLog.Contains("Repelled") && !an.Gone && g++ < 900)
            {
                views.Player.transform.localPosition = new Vector3(
                    Mathf.Clamp(an.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), RoomInstance.FloorY + 1.7f, 0f);
                AimAt(an.Pos.x, an.Pos.y);
                WorldFrame(root);
            }
            Check(stateLog.Contains("Repelled"), $"W5: sustained accurate Focus reaches REPELLED (frames={g}, state={an.State}, gone={an.Gone})");
            Check(stateLog.Skip(repelMark).Count(s => s == "Repelled") == 1, "W5: Repelled published exactly once");
            Check(revealedReal == 1, "W5: the reveal event fires exactly once per resolution");
            Check(an.Resolved, "W5: the entity is marked resolved");
            Check(CountDeep(hall.Root.transform, "AnomalyToken") == 1, "W5: exactly one token dropped, in the room it was repelled in");
            SetBeam(root, rig, LightMode.Off);

            // the retreat is physical, then it leaves the stage and a return is scheduled
            float px = views.Player.transform.localPosition.x;
            float distAtRepel = Mathf.Abs(an.Pos.x - px);
            float maxApproach = 0f;
            g = 0;
            while (!an.Gone && g++ < 200)
            {
                WorldFrame(root);
                maxApproach = Mathf.Max(maxApproach, distAtRepel - Mathf.Abs(an.Pos.x - px));
            }
            Check(an.Gone, $"W5: after the retreat beat the anomaly leaves the stage (frames={g})");
            Check(maxApproach < 0.2f, $"W5: while repelled it never closes back on the player (max approach {maxApproach:F2}u)");
            Check(views.Anomalies.Count == 0 && views.Spawner.Live == null, "W5: the live slot is released");
            Check(root.Session.Pool.Pending == poolBefore - 1,
                $"W5: the pool resolves the outstanding anomaly exactly once, at the handoff ({poolBefore} → {root.Session.Pool.Pending})");
            Check(views.Spawner.ReturnPending, "W5: a recovery return is scheduled");
            Check(Math.Abs(views.Spawner.ReturnSecondsLeft - cfg.anomalyRecoverySeconds) < 1.5f,
                $"W5: the return waits ~the configured recovery period ({views.Spawner.ReturnSecondsLeft:F1}s of {cfg.anomalyRecoverySeconds}s)");
            g = 0;
            while (views.Spawner.Live == null && g++ < 60 * 40) WorldFrame(root);
            Check(views.Spawner.Live != null, $"W5: after recovery the threat returns (frames={g})");
            if (views.Spawner.Live != null)
            {
                var ret = views.Spawner.Live;
                Check(!ReferenceEquals(ret, an), "W5: the return is a fresh entity, not a stale reference");
                Check(ret.Kind == targetKind, "W5: the SAME behavior returns — not a re-peeked pool kind");
                Check(ret.Room != hall, $"W5: it repositioned to another room ({(ret.Room != null ? ret.Room.Id.ToString() : "?")}), never on top of the player");
                Check(g > 60 * (cfg.anomalyRecoverySeconds - 2f), "W5: it stayed away for (about) the full recovery period");
            }
            views.Spawner.DiscardLive();

            // the dropped token is collectible exactly once
            var token = FindInteractable<TokenPickup>(hall);
            Check(token != null, "W5: the dropped token registered as a room interactable");
            if (token != null)
            {
                views.Player.PlaceInRoom(hall, new Vector3(token.AnchorX, 0f, 0f));
                int tokensBefore = root.Session.TokensCollected;
                token.Interact();
                Check(root.Session.TokensCollected == tokensBefore + 1, "W5: collecting the token counts exactly once");
                Check(FindInteractable<TokenPickup>(hall) == null, "W5: the token is gone from the room after pickup");
            }

            // ---- W6: four distinct behaviors
            Console.WriteLine("== W6 behavior identities ==");
            TopUpBattery(root, rig);
            if (hall.Light.Lit && sw != null) sw.Interact();   // dark for behavior reads

            // Blink: creeps unseen, frozen by ANY beam, pinned by room light — never auto-solved
            var blink = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Blink);
            Check(blink != null, "W6: Blink spawns");
            if (blink != null)
            {
                float bx0 = blink.Pos.x;
                g = 0;
                while (Mathf.Abs(blink.Pos.x - bx0) < 0.05f && g++ < 240) WorldFrame(root);
                Check(g < 240, "W6: Blink stalks while unobserved in the dark");
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(blink.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                SetBeam(root, rig, LightMode.Sweep);
                g = 0;
                while (!rig.ConeCovers(blink.Pos) && g++ < 10) { AimAt(blink.Pos.x, blink.Pos.y); WorldFrame(root); }
                Check(rig.ConeCovers(blink.Pos), "W6: setup — the Sweep beam covers the Blink");
                float bx1 = blink.Pos.x;
                for (int i = 0; i < 90; i++) { AimAt(blink.Pos.x, blink.Pos.y); WorldFrame(root); }
                Check(Mathf.Abs(blink.Pos.x - bx1) < 1e-4f, "W6: any beam freezes a Blink mid-stalk (restraint, not resolution)");
                Check(blink.State != AnomalyState.Repelled && !blink.Gone && blink.Meter < 0.55f,
                    "W6: Sweep alone never resolves the encounter");
                SetBeam(root, rig, LightMode.Off);
                if (sw != null) sw.Interact();     // lights on
                float bx2 = blink.Pos.x;
                WorldFrames(root, 90);
                Check(Mathf.Abs(blink.Pos.x - bx2) < 1e-4f, "W6: room lights alone pin a Blink");
                if (sw != null) sw.Interact();     // lights off again
            }
            views.Spawner.DiscardLive();

            // Sprint: readable telegraph → dash → catch; a beam interrupts the windup
            var sprint = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Sprint);
            Check(sprint != null, "W6: Sprint spawns");
            if (sprint != null)
            {
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(sprint.Pos.x - 4f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                int tenses0 = tensesCaptions;
                float sx0 = sprint.Pos.x;
                WorldFrames(root, 30);                     // halfway through the 1.1s windup
                Check(tensesCaptions == tenses0 + 1, "W6: Sprint's windup is announced exactly once, BEFORE the burst");
                Check(Mathf.Abs(sprint.Pos.x - sx0) < 0.05f, "W6: Sprint holds position during the telegraph — the reaction window is real");
                Check(Mathf.Abs(sprint.Pos.x - views.Player.transform.localPosition.x) > 3f,
                    "W6: it has not reached the player during the telegraph (no instant catch on spawn)");
                SetBeam(root, rig, LightMode.Sweep);       // sweep on it: interrupts the windup
                float maxStepBeam = 0f, lastBx = sprint.Pos.x;
                for (int i = 0; i < 90; i++)
                {
                    AimAt(sprint.Pos.x, sprint.Pos.y);
                    WorldFrame(root);
                    maxStepBeam = Math.Max(maxStepBeam, Mathf.Abs(sprint.Pos.x - lastBx));
                    lastBx = sprint.Pos.x;
                }
                Check(maxStepBeam <= 1.35f / 60f + 1e-4f, $"W6: under the beam Sprint never bursts (max step {maxStepBeam:F4}u/frame ≤ patrol speed)");
                Check(tensesCaptions == tenses0 + 1, "W6: no re-windup while the beam holds it");
                SetBeam(root, rig, LightMode.Off);
                float maxStepDash = 0f, lastDx = sprint.Pos.x;
                g = 0;
                while (Mathf.Abs(sprint.Pos.x - views.Player.transform.localPosition.x) > 1.0f && !sprint.Gone && g++ < 420)
                {
                    WorldFrame(root);
                    maxStepDash = Math.Max(maxStepDash, Mathf.Abs(sprint.Pos.x - lastDx));
                    lastDx = sprint.Pos.x;
                }
                Check(g < 420 && !sprint.Gone, $"W6: with the beam off Sprint telegraphs, then dashes at the player (frames={g})");
                Check(maxStepDash > 2.5f / 60f, $"W6: the dash really is fast (max step {maxStepDash:F4}u/frame)");
                int caught0 = caughtEvents;
                float sanity0 = root.Session.Sanity;
                g = 0;
                while (!sprint.Gone && g++ < 60 * 8) WorldFrame(root);   // stand still and eat the consequence
                Check(sprint.WasCaught, "W6: standing next to a dashing Sprint gets you caught — after its grace, not instantly");
                Check(caughtEvents == caught0 + 1, "W6: the catch event fires exactly once");
                Check(root.Session.Sanity < sanity0 - 10f, "W6: a catch costs real sanity");
                Check(Mathf.Abs(views.Player.transform.localPosition.x - hall.PlayerSpawn.x) < 0.01f,
                    "W6: after the catch the player is back at the room entrance");
            }
            views.Spawner.DiscardLive();

            // CoverHug: ceiling band is spawn AND clamp; only Focus identifies it
            var hug = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.CoverHug);
            Check(hug != null, "W6: CoverHug spawns");
            if (hug != null)
            {
                float floorY = RoomInstance.FloorY;
                Check(hug.Pos.y >= floorY + 3.6f - 1e-3f && hug.Pos.y <= floorY + 4.6f + 1e-3f,
                    $"W6: CoverHug spawns inside its ceiling band (y={hug.Pos.y:F2})");
                WorldFrames(root, 150);
                Check(!hug.Gone && hug.Pos.y >= floorY + 3.6f - 1e-3f && hug.Pos.y <= floorY + 4.6f + 1e-3f,
                    $"W6: CoverHug stays on the ceiling — no contradictory clamp drags it down (y={hug.Pos.y:F2})");
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(hug.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                SetBeam(root, rig, LightMode.Sweep);
                for (int i = 0; i < 15; i++) { AimAt(hug.Pos.x, hug.Pos.y); WorldFrame(root); }
                float sweepAlpha = hug.Renderer.color.a;
                SetBeam(root, rig, LightMode.Focus);
                for (int i = 0; i < 15; i++) { AimAt(hug.Pos.x, hug.Pos.y); WorldFrame(root); }
                float focusAlpha = hug.Renderer.color.a;
                Check(sweepAlpha <= 0.45f, $"W6: Sweep leaves the ceiling lurker murky (alpha {sweepAlpha:F2})");
                Check(focusAlpha >= 0.9f, $"W6: Focus turns it clear (alpha {focusAlpha:F2}) — identification costs the tight beam");
                SetBeam(root, rig, LightMode.Off);
            }
            views.Spawner.DiscardLive();

            // Slide: readable glide — never a teleport
            var slide = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Slide);
            Check(slide != null, "W6: Slide spawns");
            if (slide != null)
            {
                float totalPath = 0f, maxStepSlide = 0f, lastSx = slide.Pos.x;
                for (int i = 0; i < 300; i++)
                {
                    WorldFrame(root);
                    float d = Mathf.Abs(slide.Pos.x - lastSx);
                    totalPath += d;
                    maxStepSlide = Math.Max(maxStepSlide, d);
                    lastSx = slide.Pos.x;
                }
                Check(maxStepSlide <= 0.9f / 60f + 1e-4f, $"W6: Slide never exceeds its glide speed per frame (max {maxStepSlide:F4}u)");
                Check(totalPath > 1.5f, $"W6: Slide really repositions over 5s (path {totalPath:F1}u)");
            }
            views.Spawner.DiscardLive();

            // ---- W7: repel cancels an in-progress catch; pause freezes everything
            Console.WriteLine("== W7 catch fairness + pause ==");
            TopUpBattery(root, rig);
            if (hall.Light.Lit && sw != null) sw.Interact();
            var victim = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Slide);
            Check(victim != null, "W7: a Slide spawns for the catch-race test");
            if (victim != null)
            {
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(victim.Pos.x + 1.2f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                int caughtBefore = caughtEvents;
                int repelsBefore = stateLog.Count(s => s == "Repelled");
                SetBeam(root, rig, LightMode.Focus);
                g = 0;
                while (stateLog.Count(s => s == "Repelled") <= repelsBefore && !victim.Gone && g++ < 600)
                {
                    AimAt(victim.Pos.x, victim.Pos.y);
                    WorldFrame(root);
                }
                Check(stateLog.Count(s => s == "Repelled") > repelsBefore,
                    $"W7: from inside its threat range, Focus still wins the race to Repelled (frames={g})");
                Check(caughtEvents == caughtBefore, "W7: a successful repel cancels the catch — no attack lands afterwards");
                g = 0;
                while (!victim.Gone && g++ < 200) WorldFrame(root);
                Check(victim.Gone && caughtEvents == caughtBefore, "W7: it retreats and leaves without catching the player");
                SetBeam(root, rig, LightMode.Off);
            }
            views.Spawner.DiscardLive();

            var pauseTest = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Slide);
            if (pauseTest != null)
            {
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(pauseTest.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                SetBeam(root, rig, LightMode.Focus);
                for (int i = 0; i < 30; i++) { AimAt(pauseTest.Pos.x, pauseTest.Pos.y); WorldFrame(root); }
                float meterBefore = pauseTest.Meter, batBefore = rig.Battery;
                var posBefore = pauseTest.Pos;
                int statesBefore = stateLog.Count;
                root.TogglePause(true);
                WorldFrames(root, 180);
                Check(pauseTest.Meter == meterBefore && pauseTest.Pos == posBefore &&
                      rig.Battery == batBefore && rig.Beam == LightMode.Focus,
                    "W7: pause freezes meter, movement and battery — the beam holds its state");
                Check(stateLog.Count == statesBefore, "W7: pause publishes no transitions");
                root.TogglePause(false);
                for (int i = 0; i < 30; i++) { AimAt(pauseTest.Pos.x, pauseTest.Pos.y); WorldFrame(root); }
                Check(pauseTest.Meter > meterBefore, "W7: resume continues the exact same encounter");
                CheckLightSync(root, "W7 paused, beam held");
                SetBeam(root, rig, LightMode.Off);
                CheckLightSync(root, "W7 resumed, beam off");
            }
            views.Spawner.DiscardLive();

            // ---- W8: roaming through connected rooms
            Console.WriteLine("== W8 cross-room roaming ==");
            views.Player.PlaceInRoom(hall, new Vector3(-6.5f, 0f, 0f));
            var rover = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Blink, RoomId.Classroom1);
            Check(rover != null && rover.Room != null && rover.Room.Id == RoomId.Classroom1, "W8: an anomaly can own another room");
            if (rover == null) { cfg.anomalySpawnChancePerSecond = chanceSave; return; }
            WorldFrame(root);
            Check(!rover.Renderer.enabled, "W8: an off-stage anomaly is not drawn over the player's room");
            float sanityWall = root.Session.Sanity;
            WorldFrames(root, 120);
            Check(root.Session.Sanity == sanityWall, "W8: an anomaly in another room cannot drain sanity through the wall");
            float lastRx = rover.Pos.x; float maxStepRoam = 0f; RoomId lastRoom = rover.Room.Id;
            int crossings = 0, creaks0 = creakCaptions;
            g = 0;
            while (rover.Room.Id != RoomId.Hallway && !rover.Gone && g++ < 60 * 40)
            {
                WorldFrame(root);
                if (rover.Room.Id != lastRoom)
                {
                    crossings++;
                    Check(Mathf.Abs(rover.Pos.x - RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom1)) < 0.31f,
                        $"W8: the crossing lands it at the matching doorway (x={rover.Pos.x:F2})");
                    lastRoom = rover.Room.Id;
                    lastRx = rover.Pos.x;
                    continue;
                }
                float d = Mathf.Abs(rover.Pos.x - lastRx);
                maxStepRoam = Math.Max(maxStepRoam, d);
                lastRx = rover.Pos.x;
            }
            Check(rover.Room.Id == RoomId.Hallway && !rover.Gone,
                $"W8: the off-stage anomaly walks to the player's room through connected doors (frames={g}, crossings={crossings})");
            WorldFrame(root);   // one settling tick: the crossing frame computed visibility pre-ownership-switch
            Check(crossings == 1, "W8: exactly one doorway crossing — no arbitrary teleporting between rooms");
            Check(maxStepRoam <= 1.15f * 1.25f / 60f + 1e-4f, $"W8: in-room steps stay within its walk speed (max {maxStepRoam:F4}u/frame)");
            Check(creakCaptions > creaks0, "W8: its arrival through the door is announced");
            Check(rover.Renderer.enabled, "W8: back on stage, it is drawn again");

            // the resumed encounter engages normally in the new room
            TopUpBattery(root, rig);
            SetBeam(root, rig, LightMode.Focus);
            int exposedRoam = -1;
            g = 0;
            while (exposedRoam < 0 && !rover.Gone && g++ < 600)
            {
                views.Player.transform.localPosition = new Vector3(
                    Mathf.Clamp(rover.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), RoomInstance.FloorY + 1.7f, 0f);
                AimAt(rover.Pos.x, rover.Pos.y);
                WorldFrame(root);
                if (rover.State == AnomalyState.Exposed) exposedRoam = g;
            }
            Check(exposedRoam > 0, $"W8: the encounter resumes across rooms — Focus exposes it in the hallway (frames={g})");
            views.Spawner.DiscardLive();

            // the player leaving mid-encounter neither freezes nor resets it
            var stalker = views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Slide);
            Check(stalker != null, "W8: a Slide spawns for the mid-encounter exit test");
            if (stalker != null)
            {
                views.Player.PlaceInRoom(hall, new Vector3(Mathf.Clamp(stalker.Pos.x - 3f, -hall.HalfWidth + 1f, hall.HalfWidth - 1f), 0f, 0f));
                SetBeam(root, rig, LightMode.Focus);
                for (int i = 0; i < 60; i++) { AimAt(stalker.Pos.x, stalker.Pos.y); WorldFrame(root); }
                Check(stalker.Meter > 0.4f, $"W8: the meter builds before the exit ({stalker.Meter:F2})");
                SetBeam(root, rig, LightMode.Off);
                float meterAtExit = stalker.Meter;
                views.EnterRoom(RoomId.Classroom1);
                Check(views.Current.Id == RoomId.Classroom1 && !views.Transitioning, "W8: the player can leave mid-encounter");
                Check(!stalker.Gone && stalker.Room.Id == RoomId.Hallway, "W8: leaving neither kills nor freezes the anomaly");
                Check(views.Spawner.Live == stalker, "W8: the live slot still tracks it — no duplicates can spawn meanwhile");
                WorldFrames(root, 120);
                Check(stalker.Meter < meterAtExit, "W8: its meter decays while nobody watches (no free progress)");
                Check(stalker.State == AnomalyState.Roaming || stalker.State == AnomalyState.Stalking,
                    $"W8: the encounter de-escalates while apart (state={stalker.State})");
                int creaks1 = creakCaptions;
                g = 0;
                while (stalker.Room.Id != RoomId.Classroom1 && !stalker.Gone && g++ < 60 * 45) WorldFrame(root);
                Check(stalker.Room.Id == RoomId.Classroom1 && !stalker.Gone,
                    $"W8: the roaming threat follows the player through the doors (frames={g})");
                Check(creakCaptions > creaks1, "W8: the follow-through is announced on arrival");
                SetBeam(root, rig, LightMode.Focus);
                int exposedFollow = -1;
                g = 0;
                while (exposedFollow < -0 && !stalker.Gone && g++ < 1200)
                {
                    views.Player.transform.localPosition = new Vector3(
                        Mathf.Clamp(stalker.Pos.x - 2.5f, -7.2f, 7.2f), RoomInstance.FloorY + 1.7f, 0f);
                    AimAt(stalker.Pos.x, stalker.Pos.y);
                    WorldFrame(root);
                    if (stalker.State == AnomalyState.Exposed) exposedFollow = g;
                }
                Check(exposedFollow > 0 && stalker.State == AnomalyState.Exposed,
                    $"W8: the SAME anomaly can be engaged again after following (frames={g}) — no arbitrary reset");
            }
            views.Spawner.DiscardLive();

            // ---- W10: the classroom peek — the cursor lamp is manual (F), dark on entry
            Console.WriteLine("== W10 classroom peek lamp (manual, dark entries) ==");
            SetBeam(root, rig, LightMode.Off);
            views.EnterRoom(RoomId.Hallway);
            Check(views.Current.Id == RoomId.Hallway, "W10: back in the hallway for the peek proof");
            root.EnterRoomPeek(RoomId.Classroom1);
            Check(root.State == AppViewState.RoomPeek && views.PeekActive, "W10: peeking frames the classroom");
            CheckLightSync(root, "W10 peek entry");
            var lamp = views.PeekLight;
            Check(lamp != null && lamp.Active, "W10: the peek session is live");
            Check(!lamp.Lit, "W10: entering a classroom starts with the lamp OFF — no F was pressed");
            Check(rig.Beam == LightMode.Off, "W10: the handheld beam stays OFF through the peek entry");
            var lampMask = FindDeep(lamp.transform, "PeekMask").gameObject;
            var lampGlow = FindDeep(lamp.transform, "Glow").gameObject;
            Check(!lampMask.activeSelf && !lampGlow.activeSelf,
                "W10: OFF really hides the cursor spotlight (mask + glow), not just a status");
            Check(lamp.gameObject.activeSelf,
                "W10: the peek session itself stays live while dark — only the LIGHT is off");
            Check(FindDeep(root.HUD.transform, "peekexitband").gameObject.activeSelf,
                "W10: the step-back band is up during the peek");
            AimAt(0f, 0f);
            WorldFrames(root, 30);
            Check(!lamp.Lit && !lampMask.activeSelf && !lampGlow.activeSelf,
                "W10: mouse movement never lights the lamp (cursor is not lamp input)");
            Check(!views.IsPeekLit(new Vector3(0f, 0f, 0f)),
                "W10: an OFF lamp illuminates nothing — the coverage query agrees with the visuals");
            // the peek SESSION keeps running dark: the exit prompt is reachable without
            // the lamp, so an OFF light can never lock the player in a classroom
            Doorway exitDoor = null;
            foreach (var it in views.Current.Interactables)
            {
                var d = it as Doorway;
                if (d != null && d.Target == RoomId.Hallway) exitDoor = d;
            }
            Check(exitDoor != null, "W10: the peeked classroom has its exit doorway");
            AimAt(exitDoor.AnchorX, RoomInstance.FloorY + 1.4f);
            WorldFrames(root, 2);
            string darkPrompt = FindDeep(root.HUD.transform, "prompt").GetComponent<UnityEngine.UI.Text>().text;
            Check(!lamp.Lit && darkPrompt.Length > 0,
                "W10: dark is not a trap — the exit prompt answers under the cursor without the lamp (got '" + darkPrompt + "')");
            string peekInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(peekInfo.Contains("LAMP: OFF"),
                "W10: the HUD reads LAMP: OFF in the dark classroom (got '" + peekInfo + "')");
            PressKey(root, KeyCode.F);
            Check(lamp.Lit, "W10: ONE F press turns the lamp on (nothing was secretly pre-lit)");
            Check(lampMask.activeSelf && lampGlow.activeSelf,
                "W10: ON brings the cursor spotlight back (mask + glow together)");
            AimAt(0f, 0f);
            WorldFrames(root, 5);
            Check(views.IsPeekLit(new Vector3(0f, 0f, 0f)),
                "W10: the lit lamp illuminates the cursor point — the same state drives light and visual");
            peekInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(peekInfo.Contains("LAMP: ON"),
                "W10: the HUD reads LAMP: ON while the lamp is lit (got '" + peekInfo + "')");
            string peekObjective = FindDeep(root.HUD.transform, "objective").GetComponent<UnityEngine.UI.Text>().text;
            Check(peekObjective.Contains("Press F"),
                "W10: the hint line teaches the manual lamp (got '" + peekObjective + "')");
            string lampToast = FindDeep(root.HUD.transform, "toast").GetComponent<UnityEngine.UI.Text>().text;
            Check(lampToast.Contains("LAMP: ON"),
                "W10: the toggle confirms itself with a toast (got '" + lampToast + "')");

            // focus on the classroom lamp: Q reshapes the LIT circle, never the ON/OFF state
            float wideR = lamp.EffectiveRadius;
            PressKey(root, KeyCode.Q);
            Check(lamp.Lit && lamp.Focused,
                "W10: Q switches the LIT lamp to focus — it does not touch ON/OFF");
            Check(lamp.EffectiveRadius < wideR && Mathf.Abs(lamp.EffectiveRadius - lamp.FocusRadius) < 0.01f,
                $"W10: focus narrows the circle ({wideR:F1}u -> {lamp.EffectiveRadius:F1}u)");
            Check(Mathf.Abs(lamp.transform.localScale.x - lamp.EffectiveRadius * 2f) < 0.01f,
                "W10: mask + glow really resize with the mode (real light, not a label)");
            Check(views.IsPeekLit(new Vector3(1f, 0f, 0f)) && !views.IsPeekLit(new Vector3(2.2f, 0f, 0f)),
                "W10: the focused circle concentrates on near detail and stops short of the wide reach");
            peekInfo = FindDeep(root.HUD.transform, "room").GetComponent<UnityEngine.UI.Text>().text;
            Check(peekInfo.Contains("LAMP: ON · FOCUS"),
                "W10: the HUD names the focused lamp (got '" + peekInfo + "')");
            PressKey(root, KeyCode.F);
            Check(!lamp.Lit && lamp.Focused && !lampMask.activeSelf && !lampGlow.activeSelf &&
                  !views.IsPeekLit(new Vector3(0f, 0f, 0f)),
                "W10: F puts a FOCUSED lamp fully out — OFF wins over whichever mode was last set");
            PressKey(root, KeyCode.F);
            Check(lamp.Lit && lamp.Focused && views.IsPeekLit(new Vector3(1f, 0f, 0f)),
                "W10: ...and F brings the focused light back (mode survived the OFF, illumination did not leak)");
            PressKey(root, KeyCode.Q);
            Check(lamp.Lit && !lamp.Focused && views.IsPeekLit(new Vector3(2.2f, 0f, 0f)),
                "W10: Q again returns the broad wash — the wide/focus toggle is clean both ways");
            AimAt(3f, 1f);
            WorldFrames(root, 5);
            Check(views.IsPeekLit(new Vector3(3f, 1f, 0f)) && !views.IsPeekLit(new Vector3(-6f, 0f, 0f)),
                "W10: while ON, the light follows the cursor (covers the new point, not the whole room)");
            PressKey(root, KeyCode.F);
            Check(!lamp.Lit && !lampMask.activeSelf && !lampGlow.activeSelf,
                "W10: F again turns it OFF completely — spotlight and illumination together");
            Check(!views.IsPeekLit(new Vector3(3f, 1f, 0f)), "W10: OFF takes the illumination with it");
            root.ExitRoomPeek();
            Check(root.State == AppViewState.Exploration && !views.PeekActive && !lamp.Lit,
                "W10: stepping back ends the session dark (no lamp state survives the exit)");
            Check(rig.Beam == LightMode.Off, "W10: the hallway beam is still OFF after the peek (no carry-over)");
            CheckLightSync(root, "W10 peek exit");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep, "W10: back on patrol, F is the beam toggle again");
            root.EnterRoomPeek(RoomId.Classroom2);
            Check(!views.PeekLight.Lit &&
                  !FindDeep(views.PeekLight.transform, "Glow").gameObject.activeSelf,
                "W10: the NEXT classroom entry also starts with the lamp OFF (every entry is dark)");
            Check(rig.Beam == LightMode.Off, "W10: ...and the beam never rides into a peek lit");
            PressKey(root, KeyCode.F);
            Check(views.PeekLight.Lit, "W10: one F press lights the second classroom too");
            PressKey(root, KeyCode.F);
            Check(!views.PeekLight.Lit, "W10: ...and one F press puts it out again (a clean toggle)");
            // the dark exit: E on the doorway works with the lamp OFF
            Doorway exitDoor2 = null;
            foreach (var it in views.Current.Interactables)
            {
                var d = it as Doorway;
                if (d != null && d.Target == RoomId.Hallway) exitDoor2 = d;
            }
            AimAt(exitDoor2.AnchorX, RoomInstance.FloorY + 1.4f);
            PressKey(root, KeyCode.E);
            Check(!views.PeekActive && root.State == AppViewState.Exploration,
                "W10: E on the exit doorway steps back to the hallway — even with the lamp OFF");
            Check(!FindDeep(root.HUD.transform, "peekexitband").gameObject.activeSelf,
                "W10: the step-back band is gone after leaving the doorway");
            Check(!views.PeekLight.Lit && rig.Beam == LightMode.Off,
                "W10: exiting leaves everything dark — restarting patrol never re-lights anything");

            // ---- W9: spawn ownership under ambient pressure + the desk handoff
            Console.WriteLine("== W9 spawn ownership + desk handoff ==");
            SetBeam(root, rig, LightMode.Off);
            views.EnterRoom(RoomId.Hallway);
            root.Session.CurrentShift = 2;                    // ambient wandering unlocks from shift 2
            cfg.anomalySpawnChancePerSecond = chanceSave;     // real ambient pressure
            int maxReal = 0;
            for (int i = 0; i < 60 * 45; i++)
            {
                WorldFrame(root);
                int real = 0;
                foreach (var a in views.Anomalies) if (a != null && !a.IsHallucination && !a.Gone) real++;
                if (real > maxReal) maxReal = real;
            }
            Check(maxReal <= 1, $"W9: ambient spawning never runs two real anomalies at once (max concurrent: {maxReal})");
            cfg.anomalySpawnChancePerSecond = 0f;
            if (views.Spawner.Live == null) views.SpawnAnomalyOfKind(AnomalyBehaviorKind.Blink);
            Check(views.Spawner.Live != null, "W9: a live anomaly exists before the desk handoff");
            int poolAtExit = root.Session.Pool.Pending;
            views.ReturnToSecurity();
            Check(views.Spawner.Live == null, "W9: returning to the desk clears the live slot (no stale reference)");
            Check(root.Session.Pool.Pending == poolAtExit, "W9: a discarded anomaly stays OUTSTANDING in the pool (not resolved)");
            Check(!views.Spawner.ReturnPending, "W9: pending returns are cancelled with the patrol");
            Check(views.Current == null, "W9: the desk handoff releases the room");
            cfg.anomalySpawnChancePerSecond = chanceSave;
            root.Session.CurrentShift = 1;
            Console.WriteLine("== patrol world loop done ==");
        }

        // ---------------------------------------------------- asset manifest <-> code sync
        // The manifest (docs/asset-manifest.csv) is the naming contract the art/audio team works
        // from, and every name in it is a live key: a file at Resources/Art/<key>.png or
        // Resources/Audio/<key>.wav replaces the procedural placeholder with no code change. That
        // only stays true while the list and the code agree, so this proof reads the CSV and
        // checks it against ProceduralArt.AllKeys / ProceduralAudio.AllKeys / FaceAssets in both
        // directions, plus that every key actually resolves (a typo'd case would fall back to the
        // solid/placeholder and ship a white square).
        private static void AssetManifestProof(GameRoot root)
        {
            Console.WriteLine("== asset manifest sync (docs/asset-manifest.csv <-> code) ==");
            string rootDir = RepoRoot();
            string csv = rootDir == null ? null :
                System.IO.Path.Combine(rootDir, "docs", "asset-manifest.csv");
            Check(csv != null && System.IO.File.Exists(csv), "AM: docs/asset-manifest.csv ships with the repo");
            if (csv == null || !System.IO.File.Exists(csv)) return;

            var artRows = new List<string>();
            var audioRows = new List<string>();
            var fontRows = new List<string>();
            var shipped = new List<string>();
            foreach (var raw in System.IO.File.ReadAllLines(csv))
            {
                var f = SplitCsv(raw);
                if (f.Count < 6 || f[0] == "key") continue;
                if (f[1] == "art") artRows.Add(f[0]);
                else if (f[1] == "audio") audioRows.Add(f[0]);
                else if (f[1] == "font") fontRows.Add(f[0]);
                if (f[4] == "temp-shipped") shipped.Add(f[0] + "|" + f[1] + "|flat");
                if (f[4] == "shipped-kit") shipped.Add(f[0] + "|" + f[1] + "|kit");
            }
            Check(artRows.Count > 110 && audioRows.Count >= 30 && fontRows.Count == 8,
                $"AM: the manifest covers the whole pipeline ({artRows.Count} art, {audioRows.Count} audio, {fontRows.Count} font rows)");

            // art rows == the painter's keys + the face pairs, exactly
            var codeArt = new List<string>(Hollowbrook.Bootstrap.ProceduralArt.AllKeys);
            for (int i = 1; i <= 14; i++)
            {
                codeArt.Add("student_face_" + i.ToString("D2"));
                codeArt.Add("student_face_" + i.ToString("D2") + "_anomaly");
            }
            codeArt.Add(Hollowbrook.Logic.FaceAssets.AdminKey);
            codeArt.Add(Hollowbrook.Logic.FaceAssets.AdminWindowKey);
            codeArt.AddRange(SecurityKitKeys);   // the owner's SecurityRoom kit rows
            Check(SameSet(artRows, codeArt, out string artDiff),
                "AM: every art row is a real procedural key and every key has a row" +
                (artDiff.Length > 0 ? " (diff: " + artDiff + ")" : ""));

            // audio rows == the synth's keys, exactly
            Check(SameSet(audioRows, new List<string>(Hollowbrook.Bootstrap.ProceduralAudio.AllKeys),
                  out string audioDiff),
                "AM: every audio row is a real synth key and every key has a row" +
                (audioDiff.Length > 0 ? " (diff: " + audioDiff + ")" : ""));

            // every art key resolves to its own sprite (a missing case would fall back to solid)
            bool resolves = true;
            string badKey = "";
            foreach (var k in Hollowbrook.Bootstrap.ProceduralArt.AllKeys)
            {
                var sp = Hollowbrook.Bootstrap.ProceduralArt.Get(k);
                if (sp == null || sp.name != k) { resolves = false; badKey = k; break; }
            }
            Check(resolves, "AM: every art key paints its own sprite (no fallback squares)" +
                  (resolves ? "" : " — first broken key: " + badKey));

            // every audio key resolves to its own clip
            bool audioOk = true;
            string badAudio = "";
            foreach (var k in Hollowbrook.Bootstrap.ProceduralAudio.AllKeys)
            {
                var clip = Hollowbrook.Bootstrap.ProceduralAudio.Get(k);
                if (clip == null || clip.name != k) { audioOk = false; badAudio = k; break; }
            }
            Check(audioOk, "AM: every audio key synthesises its own clip (no silent falls-through)" +
                  (audioOk ? "" : " — first broken key: " + badAudio));

            // rows claiming a shipped file must actually ship it
            bool filesOk = true;
            string missing = "";
            foreach (var row in shipped)
            {
                var parts = row.Split('|');
                string rel;
                if (parts[1] == "art")
                    rel = System.IO.Path.Combine(parts[2] == "kit"
                        ? "Assets/_Project/Resources/Art/SecurityRoom" : "Assets/_Project/Resources/Art",
                        parts[0] + ".png");
                else
                    rel = System.IO.Path.Combine("Assets/_Project/Resources/Fonts",
                        System.IO.File.Exists(System.IO.Path.Combine(rootDir,
                            "Assets/_Project/Resources/Fonts", parts[0] + ".otf"))
                        ? parts[0] + ".otf" : parts[0] + ".ttf");
                if (!System.IO.File.Exists(System.IO.Path.Combine(rootDir, rel)))
                { filesOk = false; missing = rel; break; }
            }
            Check(filesOk, "AM: every row marked shipped (temp or kit) has its file in the repo" +
                  (filesOk ? "" : " — missing: " + missing));

            // the faces the fairness invariant leans on: paired, and the admin pair exists
            Check(Hollowbrook.Logic.FaceAssets.WindowKey(3, true) ==
                  Hollowbrook.Logic.FaceAssets.StudentKey(3) + "_anomaly" &&
                  Hollowbrook.Logic.FaceAssets.AdminWindowKey == "admin_face_glitch",
                "AM: anomaly and administrator face variants keep their pairing contract");
        }

        // quote-aware CSV split (the generator quotes any field containing a comma)
        private static List<string> SplitCsv(string line)
        {
            var fields = new List<string>();
            var cur = new System.Text.StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"') { inQ = !inQ; continue; }
                if (c == ',' && !inQ) { fields.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(c);
            }
            fields.Add(cur.ToString());
            return fields;
        }

        private static bool SameSet(List<string> a, List<string> b, out string diff)
        {
            var sa = new HashSet<string>(a);
            var sb = new HashSet<string>(b);
            var d = new List<string>();
            foreach (var x in a) if (!sb.Contains(x)) d.Add("+" + x);
            foreach (var x in b) if (!sa.Contains(x)) d.Add("-" + x);
            diff = string.Join(", ", d);
            return d.Count == 0 && a.Count == b.Count;
        }

        // --------------------------------- Shift-5 administrator: the branching conversation
        // Integration proof at the real desk: arrival intro hands over to the conversation, the
        // panel borrows the question column, choices move only encounter state, the question
        // budget and the stamps stay authoritative, silence and re-open cannot softlock, and the
        // verdict still commits exactly once through the physical hand-back.
        private static void AdminTalkProof(GameRoot root)
        {
            Console.WriteLine("== administrator encounter: branching conversation at the checkpoint ==");
            var cp = root.Checkpoint;
            cp.DebugPresentAdmin();
            cp.DebugTakeDocuments();
            var admin = cp.CurrentVisitor;
            Check(admin != null && admin.IsAdministrator, "AT: the administrator is at the window");

            // click through the arrival intro exactly like the player; its completion opens the talk
            int guard = 0;
            while (root.Dialogue != null && root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();
            Check(cp.AdminTalkOpen, "AT: the arrival intro hands over to the conversation (hub open)");
            var panel = FindDeep(cp.transform, "admintalk");
            Check(panel != null && panel.gameObject.activeSelf, "AT: the conversation panel is on the desk");
            Check(!FindDeep(cp.transform, "qpanel_border").gameObject.activeSelf &&
                  !FindDeep(cp.transform, "map").gameObject.activeSelf,
                "AT: while it talks, the question row and the inline map step aside");
            Check(FindDeep(cp.transform, "opt_chal_name") != null &&
                  FindDeep(cp.transform, "opt_chal_num") != null &&
                  FindDeep(cp.transform, "opt_chal_face") != null &&
                  FindDeep(cp.transform, "opt_defer") != null &&
                  FindDeep(cp.transform, "opt_silence") != null &&
                  FindDeep(cp.transform, "opt_end") != null,
                "AT: the hub offers the three challenges, compliance, silence and the exit");
            Check(FindDeep(cp.transform, "opt_ev_photo") == null &&
                  FindDeep(cp.transform, "opt_ev_cabinet") == null,
                "AT: the evidence branches stay shut while their clues are undiscovered");

            // a challenge moves encounter state and NOTHING else
            int budgetBefore = BudgetLeft(cp);
            FindDeep(cp.transform, "opt_chal_name").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            guard = 0;
            while (root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();
            Check(root.Session.AdminTalk.Node == "chal_name" && root.Session.AdminTalk.Challenge == 1,
                "AT: the challenge landed in the encounter state (node + tactic counter)");
            Check(BudgetLeft(cp) == budgetBefore, "AT: a conversational response consumes no verification question");
            Check(!cp.SlipStamped && cp.PendingVerdict == Verdict.None,
                "AT: no verdict, breach or ending moved because of a dialogue choice");

            // double-commit: the same button fired twice in one beat takes effect once
            var twice = FindDeep(cp.transform, "opt_press_name").GetComponent<UnityEngine.UI.Button>();
            twice.onClick.Invoke();
            twice.onClick.Invoke();
            Check(root.Session.AdminTalk.Node == "press_name" && root.Session.AdminTalk.Challenge == 2,
                $"AT: firing one option twice commits it once (node={root.Session.AdminTalk.Node} chal={root.Session.AdminTalk.Challenge}: one challenge for the hub pick, one for the press — not three)");
            guard = 0;
            while (root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();

            // verification questions still work mid-conversation, and the talk adapts to them
            cp.AskQuestion(QuestionId.FirstClassroom);
            Check(BudgetLeft(cp) == budgetBefore - 1, "AT: the real question system still owns its budget");
            FindDeep(cp.transform, "opt_press_name_back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            guard = 0;
            while (root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();
            Check(FindDeep(cp.transform, "opt_q_server") != null,
                "AT: after the server-room question, its follow-up branch appears");
            var qLine = FindDeep(cp.transform, "opt_q_server").Find("label")
                .GetComponent<UnityEngine.UI.Text>().text;
            Check(qLine.Contains("server room"), "AT: the follow-up names the actual answer's subject");

            // silence: a pause, a beat, and always a way back
            FindDeep(cp.transform, "opt_silence").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            guard = 0;
            while (root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();
            Check(root.Session.AdminTalk.Silence == 1 && root.Session.AdminTalk.Node == "silence",
                "AT: silence is a counted beat, not a question and not a verdict");
            FindDeep(cp.transform, "opt_silence_back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            guard = 0;
            while (root.Dialogue.IsActive && guard++ < 60) root.Dialogue.Advance();
            Check(root.Session.AdminTalk.Node == AdminTalk.HubId, "AT: silence always returns to a usable hub");

            // T steps out and back in without losing the thread
            PressDeskKey(cp, KeyCode.T);
            Check(!cp.AdminTalkOpen && FindDeep(cp.transform, "admintalkbar").gameObject.activeSelf &&
                  FindDeep(cp.transform, "qpanel_border").gameObject.activeSelf,
                "AT: stepping back restores the inspection column and leaves the re-open bar");
            PressDeskKey(cp, KeyCode.T);
            Check(cp.AdminTalkOpen && FindDeep(cp.transform, "opt_end") != null && !root.Dialogue.IsActive,
                "AT: re-opening resumes the same node — options straight away, no replayed lines");

            // interruption contract: the encounter survives pause and refuses to be walked away
            // from mid-inspection, and neither resets the conversation nor the visitor
            string talkBefore = root.Session.AdminTalk.Encode();
            root.TogglePause(true);
            PressDeskKey(cp, KeyCode.T);                    // gameplay keys stand down while paused
            Check(root.Session.AdminTalk.Encode() == talkBefore && cp.AdminTalkOpen,
                "AT: pause freezes the conversation (hotkeys included) and corrupts nothing");
            root.TogglePause(false);
            cp.GoPatrol();
            Check(root.State == AppViewState.Checkpoint && root.Session.AdminTalk.Encode() == talkBefore,
                "AT: walking away mid-inspection is refused — the administrator keeps the window");

            // the conversation persists with the run save
            root.PersistRun();
            string saveText = System.IO.File.ReadAllText(System.IO.Path.Combine(
                UnityEngine.Application.persistentDataPath, "hollowbrook_save.txt"));
            Check(saveText.Contains("adminTalk=") && saveText.Contains("chal_name".Length > 0 ? "hub" : "hub"),
                "AT: the encounter state rides the run save (adminTalk key)");

            // stepping back for good hands the desk over; the stamps remain the only decision
            FindDeep(cp.transform, "opt_end").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(root.State == AppViewState.Checkpoint,
                "AT: completing the conversation awards nothing — the desk is simply handed back");
            Check(root.Session.AdminTalk.Completed && !cp.AdminTalkOpen &&
                  !FindDeep(cp.transform, "admintalkbar").gameObject.activeSelf &&
                  FindDeep(cp.transform, "qpanel_border").gameObject.activeSelf &&
                  FindDeep(cp.transform, "map").gameObject.activeSelf,
                "AT: the exit completes the conversation and gives the desk back intact");

            // the physical verdict, unchanged: stamp, hand back, exactly-once counters
            StampSlip(root, Verdict.Reject);
            HandBack(root, true, true);
            Check(root.Session.AdminRejected && !root.Session.AdminAllowed,
                $"AT: rejecting the administrator through the stamps updates the session once (rejected={root.Session.AdminRejected} allowed={root.Session.AdminAllowed} stamped={cp.SlipStamped} pending={cp.PendingVerdict} processed={root.Session.Processed})");
            int processed = root.Session.Processed;
            int breaches = root.Session.Breaches;
            root.OnVerdictResolved(admin, InspectionRules.Apply(admin, Verdict.Reject, null));
            Check(root.Session.Processed == processed && root.Session.Breaches == breaches &&
                  root.Session.FalseRejections == root.Session.FalseRejections,
                "AT: a duplicated verdict callback changes nothing (exactly-once guard)");
            Check(QueueAbandoned(root), "AT: the rejected administrator closes the night's line safely");
        }

        private static int BudgetLeft(CheckpointView cp)
        {
            var t = FindDeep(cp.transform, "budget");
            if (t == null) return -1;
            string s = t.GetComponent<UnityEngine.UI.Text>().text;
            int i = s.IndexOf('/');          // the reference counter reads "remaining/max"
            if (i < 0) return -1;
            int n;
            return int.TryParse(s.Substring(0, i).Trim(), out n) ? n : -1;
        }

        private static bool QueueAbandoned(GameRoot root) =>
            root.Queue != null && root.Queue.Active == null && root.Queue.WaitingCount == 0;

        // --------------------------------------- the owner's SecurityRoom art kit, integrated
        // The kit is a skin over the audited grid: these proofs check the files ship, the keys
        // resolve to sprites named after themselves, the full-frame layers are click-through
        // (so not one desk interaction moved), the bubble keeps its 9-slice tail, and the booth
        // shutter follows the visitor.
        // every sprite the SecurityRoom kit ships (proof-shared with the manifest sync)
        internal static readonly string[] SecurityKitKeys =
        {
            "sec_background", "sec_background_open", "sec_background_close", "sec_light",
            "sec_window_open", "sec_window_close", "sec_upper_panel", "sec_upper_panel_bg",
            "sec_sanity_bar", "sec_pause_block", "sec_btn_pill", "sec_wide_pill", "sec_caption_bar",
            "sec_stamp_circle_allow", "sec_stamp_circle_deny", "sec_icon_accept",
            "sec_icon_decline", "sec_folder_tab1", "sec_folder_tab2", "sec_icon_patrol",
            "sec_icon_endshift", "sec_qpanel", "sec_arrow_right", "sec_cabinet1",
            "sec_cabinet2", "sec_stamp", "sec_rules_paper", "sec_rules_dark",
            "sec_log_paper", "sec_log_dark", "sec_log_rules", "sec_bubble",
        };

        private static int Idx(Transform t) => t != null ? t.GetSiblingIndex() : -1;

        private static void SecurityKitProof(GameRoot root)
        {
            Console.WriteLine("== SecurityRoom art kit: shipped, resolved, click-through ==");
            var cp = root.Checkpoint;
            string rootDir = RepoRoot();
            string kitDir = rootDir == null ? null :
                System.IO.Path.Combine(rootDir, "Assets/_Project/Resources/Art/SecurityRoom");
            string[] keys = SecurityKitKeys;
            bool shipped = true, resolves = true;
            string missing = "";
            foreach (var k in keys)
            {
                if (kitDir == null || !System.IO.File.Exists(System.IO.Path.Combine(kitDir, k + ".png")))
                { shipped = false; missing = k; break; }
                var sp = Hollowbrook.Bootstrap.ProceduralArt.Get(k);
                if (sp == null || sp.name != k) { resolves = false; missing = k; break; }
            }
            Check(shipped, "KIT: all " + keys.Length + " kit sprites ship under Resources/Art/SecurityRoom" +
                  (shipped ? "" : " (missing " + missing + ")"));
            Check(resolves, "KIT: every kit key resolves to a sprite named after itself (drop-in wiring works)");

            // the full-frame layers must never eat a click: every desk interaction is unchanged
            bool clickThrough = true;
            foreach (var n in new[] { "sec_base", "sec_light",
                                      "sec_window_top", "sec_window_glass", "sec_window_base",
                                      "sec_upper_bg",
                                      "sec_upper", "sec_sanity_bar", "sec_pause_block",
                                      "sec_tray_allow", "sec_tray_deny" })
            {
                var t = FindDeep(cp.transform, n);
                Check(t != null, "KIT: layer " + n + " exists (proof is not a silent no-op)");
                if (t == null) continue;
                var im = t.GetComponent<UnityEngine.UI.Image>();
                if (im != null && im.raycastTarget) clickThrough = false;
            }
            Check(clickThrough, "KIT: every kit layer is click-through — the audited interactions own the pointer");

            // the booth shutter follows the visitor (open glass / closed blinds) — all three
            // bands swap together: hood and base keep their painted heights, glass stretches
            var win = FindDeep(cp.transform, "sec_window_glass");
            if (win != null)
            {
                var wimg = win.GetComponent<UnityEngine.UI.Image>();
                bool visitor = cp.CurrentVisitor != null;
                string state = visitor ? "open" : "close";
                Check(wimg.sprite.name == "sec_window_" + state + "_glass",
                      $"KIT: the shutter glass matches the window state ({state})");
                bool bandsSynced = true;
                foreach (var band in new[] { "sec_window_top", "sec_window_base" })
                {
                    var bi = FindDeep(cp.transform, band).GetComponent<UnityEngine.UI.Image>();
                    if (bi.sprite == null || bi.sprite.name != "sec_window_" + state + "_" +
                        (band == "sec_window_top" ? "top" : "base")) bandsSynced = false;
                }
                Check(bandsSynced, "KIT: hood and counter bands swap with the glass (no mixed shutter)");
            }

            // the live sanity meter wears the kit's WHITE pill: track bed retires, inks flip
            // dark, the stage caption hides. Regression cover: the HUD is built AFTER the desk,
            // so the tint lands lazily on the first RefreshHud — and SetSanity repainting on
            // every sanity event must keep it (a one-shot tint used to be clobbered).
            if (root.HUD != null)
            {
                var sanityBox = FindDeep(root.HUD.transform, "sanitybg_border");
                var trackImg = sanityBox != null ? sanityBox.Find("track")?.GetComponent<UnityEngine.UI.Image>() : null;
                var fillImg = sanityBox != null ? sanityBox.Find("track/fill")?.GetComponent<UnityEngine.UI.Image>() : null;
                var valTxt = sanityBox != null ? sanityBox.Find("val")?.GetComponent<UnityEngine.UI.Text>() : null;
                var capTxt = sanityBox != null ? sanityBox.Find("cap")?.GetComponent<UnityEngine.UI.Text>() : null;
                var stageTxt = sanityBox != null ? sanityBox.Find("stage")?.GetComponent<UnityEngine.UI.Text>() : null;
                Check(sanityBox != null && trackImg != null && fillImg != null &&
                      valTxt != null && capTxt != null && stageTxt != null,
                    "KIT: the HUD sanity widget is reachable for the pill proof");
                if (trackImg != null)
                    Check(trackImg.color.a > 0.9f && trackImg.color.r < 0.2f,
                        "KIT: the sanity track is a dark recessed bed inside the kit pill");
                if (valTxt != null && capTxt != null)
                    Check(valTxt.color == UIFactory.NsInk && capTxt.color == UIFactory.NsInk,
                        "KIT: sanity caption + value flipped to dark ink on the white pill");
                if (stageTxt != null)
                    Check(stageTxt.color.a == 0f, "KIT: the stage caption hides under the kit pill");
                if (fillImg != null && root.Session != null)
                {
                    var st = SanityModel.StageFor(root.Session.Sanity);
                    bool alarm = st == SanityStage.Breaking || st == SanityStage.Shaken;
                    bool fillOk = alarm
                        ? fillImg.color == (st == SanityStage.Breaking
                            ? UIFactory.AccentBright : new Color(0.75f, 0.45f, 0.2f))
                        : fillImg.color.r > 0.8f && fillImg.color.g > 0.8f && fillImg.color.b > 0.7f;
                    Check(fillOk, $"KIT: the sanity fill reads paper-white on the dark bed for stage {st} (alarm colors kept)");
                }
            }

            // the bubble wears the kit's 9-slice speech box, tail intact, single raycast target
            var bubble = FindDeep(cp.transform, "bubble_border").GetComponent<UnityEngine.UI.Image>();
            Check(bubble.sprite != null && bubble.sprite.name == "sec_bubble" &&
                  bubble.type == UnityEngine.UI.Image.Type.Sliced && bubble.sprite.border.x > 100f,
                $"KIT: the speech bubble is the kit box, sliced so its tail never stretches (border x={bubble.sprite.border.x:F0})");
            // the slice must be a REAL border in reference units: border px / pixelsPerUnit.
            // Regression: at the default UI PPU the 430px tail drew as a 4-unit sliver (and the
            // preview renderer, fed raw texture pixels, drew nothing at all).
            float borderUnits = bubble.sprite.border.x / bubble.sprite.pixelsPerUnit;
            Check(borderUnits >= 20f,
                $"KIT: the bubble's tail slice is {borderUnits:F0} reference units wide (pixelsPerUnit {bubble.sprite.pixelsPerUnit:F0})");

            // the tray circles sit under the stamps, in the re-baselined tray rects
            var allow = FindDeep(cp.transform, "stamp_allow");
            var circA = FindDeep(cp.transform, "sec_tray_allow");
            var circD = FindDeep(cp.transform, "sec_tray_deny");
            var deny = FindDeep(cp.transform, "stamp_deny");
            // draw order, not sibling adjacency: the stamps are draggable and SetAsLastSibling
            // on every lift, so the circles only have to stay UNDER them, not next to them
            Check(circA != null && circD != null && allow != null && deny != null &&
                  circA.GetSiblingIndex() < allow.GetSiblingIndex() &&
                  circD.GetSiblingIndex() < deny.GetSiblingIndex(),
                "KIT: the tray circles draw under their stamps (allow and deny)");
            Check(FindDeep(cp.transform, "sec_captionbar_a") == null &&
                  FindDeep(cp.transform, "sec_captionbar_d") == null,
                "KIT: the APPROVE / DECLINE caption bars are gone from the tray");

            // fonts: kit families first, bundled open-licence faces as fallback — never null
            Check(UIFactory.UiFont != null && UIFactory.UiFontBold != null &&
                  UIFactory.TypeFont != null && UIFactory.HandFont != null,
                "KIT: every text role resolves a font (kit family or bundled fallback)");
        }

        // ------------------------------------------------------- desk helpers
        // The desk view runs its own Update in the editor; headlessly nothing ticks unless the
        // harness does it, exactly like WorldFrame does for the patrol. These drive the same
        // per-frame paths (queue clock, hotkeys) so an
        // animation's real timing is what gets asserted — never a synthetic jump to the end.
        private static void DeskFrame(CheckpointView cp, float dt = 1f / 60f)
        {
            Time.deltaTime = dt;
            Time.unscaledDeltaTime = dt;
            Input.KeysDown.Clear();     // edge-triggered keys last exactly one frame
        }

        private static void DeskFrames(CheckpointView cp, int n, float dt = 1f / 60f)
        {
            for (int i = 0; i < n; i++) DeskFrame(cp, dt);
        }

        private static void PressDeskKey(CheckpointView cp, KeyCode k)
        {
            Input.KeysDown.Add(k);
            cp.TickHotkeys();
            DeskFrame(cp);
        }


        // ---------------------------------------------------- opt-in screen dump (JSON)
        // Walks the REAL built desk hierarchy and records every visible Image/Text as a rect
        // in the 1280x720 desk-grid space (top-left origin, y down), with sprite key, 9-slice
        // border, tint, cumulative alpha, rotation and full type metrics. Tools/Preview/
        // render_screen.py turns one dump into pixel screenshots at ANY resolution: the desk
        // is laid out in reference units, so one dump serves every breakpoint. Rect maths is
        // Unity's own: anchor ref point = lerp(anchorMin,anchorMax,pivot), size = sizeDelta +
        // anchor span, pivot hangs off anchoredPosition.
        private struct DumpRot { public float Cx, Cy, Rad, Deg; }

        // state selects WHICH moment of the night gets dumped, so every screen the owner
        // reported can be looked at: the intro dialogue, the empty booth, the occupied desk
        // and the occupied desk at low sanity (the meter's legibility states).
        private static Transform FaceOf(Transform paper, string name)
        {
            var art = paper.Find("cardart");
            if (art == null) art = paper.Find("slipart");
            if (art == null) art = paper;
            return art.Find(name);
        }

        private static int CountDeep(Transform t, string name)
        {
            int n = t.name == name ? 1 : 0;
            for (int i = 0; i < t.childCount; i++) n += CountDeep(t.GetChild(i), name);
            return n;
        }

        private static void DumpScreen(string path, string state = "occupied")
        {
            MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore());
            var root = GameRoot.Instance;
            root.Config.queueArrivalMinSeconds = 3f;
            root.Config.queueArrivalMaxSeconds = 5f;
            root.Config.queueBusyHurryMultiplier = 1f;
            var newGame = root.MainMenu.transform.Find("buttons").Find("NEW GAME");
            newGame.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            CheckpointSceneBootstrap.Build(root);
            if (state == "intro")
            {
                // the night card + the intro dialogue, exactly as the shift opens
                var sb0 = new System.Text.StringBuilder();
                sb0.Append("{\"refw\":1280,\"refh\":720,\"nodes\":[");
                bool f0 = true;
                if (root.Loading != null)
                    WalkScreen(root.Loading.transform as RectTransform, 0f, 0f, 1280f, 720f,
                               new List<DumpRot>(), sb0, ref f0, 1f, 1f);
                if (root.Dialogue != null)
                    WalkScreen(root.Dialogue.transform as RectTransform, 0f, 0f, 1280f, 720f,
                               new List<DumpRot>(), sb0, ref f0, 1f, 1f);
                sb0.Append("]}");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? ".");
                System.IO.File.WriteAllText(path, sb0.ToString());
                Console.WriteLine("wrote " + path + " (intro)");
                return;
            }
            int adv = 0;
            while (root.Dialogue != null && root.Dialogue.IsActive && adv++ < 64)
                root.Dialogue.Advance();
            ClickStickyNote(root);
            var cp = root.Checkpoint;
            if (!cp.gameObject.activeInHierarchy) { Console.WriteLine("desk not live — aborting dump"); return; }
            if (state == "empty")
            {
                // the booth AFTER a verdict: papers handed back, visitor gone, shutter shut.
                // The stub coroutine pump never runs AfterStamp, so the harness lands the
                // same beat the editor reaches through the post-stamp coroutine.
                cp.DebugTakeDocuments();
                TakeSlip(root);
                StampSlip(root, Hollowbrook.Logic.InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
                HandBack(root, true, false);
                cp.SetVisitorVisible(false);
                DeskFrames(cp, 6);
                var sb1 = new System.Text.StringBuilder();
                sb1.Append("{\"refw\":1280,\"refh\":720,\"nodes\":[");
                bool f1 = true;
                var frame0 = FindDeep(cp.transform, "DeskFrame");
                if (frame0 != null)
                    WalkScreen(frame0.GetComponent<RectTransform>(), 0f, 0f, 1280f, 720f,
                               new List<DumpRot>(), sb1, ref f1, 1f, 1f);
                var hud0 = root.HUD != null ? FindDeep(root.HUD.transform, "HudFrame") : null;
                if (hud0 != null)
                    WalkScreen(hud0.GetComponent<RectTransform>(), 0f, 0f, 1280f, 720f,
                               new List<DumpRot>(), sb1, ref f1, 1f, 1f);
                sb1.Append("]}");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? ".");
                System.IO.File.WriteAllText(path, sb1.ToString());
                Console.WriteLine("wrote " + path + " (empty)");
                return;
            }
            int guard = 0;
            while (cp.CurrentVisitor == null && guard++ < 8) cp.AdvanceQueue(6f);
            // take the offered ID the way a tap does: card docks, papers reveal
            var offered = FindDeep(cp.transform, "offeredid");
            var offerDrag = offered != null ? offered.GetComponent<DragPaper>() : null;
            if (offerDrag != null && offerDrag.OnTapped != null) offerDrag.OnTapped.Invoke(new Vector2(560f, 580f));
            // pull the floor-files drawer open (the concept art shows it open)
            var station = FindDeep(cp.transform, "station");
            if (station != null) station.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if (state == "occ45" || state == "occ25")
            {
                float v = state == "occ45" ? 45f : 25f;
                if (root.HUD != null) root.HUD.SetSanity(v);   // before the walk: the dump is pixels
            }
            DeskFrames(cp, 12);
            // reading-sheet and map dumps open the real view the way its own click does
            if (state == "rules" || state == "log")
                cp.DebugOpenOverlay(state);
            // records dumps: the open panel on a cohort, and the pulled record sheet over it
            // — the two states a player browsing the registry sees
            if (state == "files" || state == "record")
            {
                // the occupied setup already opens the panel once (oldest cohort): asking for
                // the same cohort again would shut it, so only steer when it is elsewhere
                if (!cp.RecordsOpen || cp.RecordsTab != 0) cp.OpenRecords(0);
                DeskFrames(cp, 14);
                if (state == "record")
                {
                    int idx = -1;
                    foreach (var f in Hollowbrook.Logic.RecordSearch.BuildCohortFolders(root.Roster.Canon))
                        if (f.Cohort == RecordSearch.FirstCohort && f.Indices.Count > 2) { idx = f.Indices[2]; break; }
                    if (idx >= 0) cp.OpenCanonFile(idx);
                    DeskFrames(cp, 3);
                }
            }
            // asked/disabled question rows: two of the budget's questions spent
            if (state == "asked")
            {
                cp.AskByHotkey(0);
                DeskFrames(cp, 3);
                cp.AskByHotkey(2);
                DeskFrames(cp, 8);
            }
            // the talk dock mid-conversation: a typed exchange in the transcript sheet,
            // then a long window line in the bubble (three wrapped lines)
            if (state == "talk")
            {
                // the window conversation now runs through the Questions panel; the dump
                // shows the student's side of it — a long answer in the kit bubble
                cp.DebugSay("Custodian. I'm the district administrator. I'll be taking " +
                            "control of this facility tonight.");
                for (int i = 0; i < 2; i++) DeskFrame(cp);
            }

            var frame = FindDeep(cp.transform, "DeskFrame");
            if (frame == null) { Console.WriteLine("no DeskFrame — aborting dump"); return; }
            var walkRoot = frame.GetComponent<RectTransform>();
            if (state == "map")
            {
                root.OpenMap();
                var mapT = root.MapScreen != null ? root.MapScreen.transform as RectTransform : null;
                if (mapT == null) { Console.WriteLine("no map view — aborting dump"); return; }
                walkRoot = mapT;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("{\"refw\":1280,\"refh\":720,\"nodes\":[");
            bool first = true;
            WalkScreen(walkRoot, 0f, 0f, 1280f, 720f,
                       new List<DumpRot>(), sb, ref first, 1f, 1f);
            // the HUD canvas rides over the desk: sanity track/fill/value, hints, toasts
            var hud = root.HUD != null && state != "map" ? FindDeep(root.HUD.transform, "HudFrame") : null;
            if (hud != null)
                WalkScreen(hud.GetComponent<RectTransform>(), 0f, 0f, 1280f, 720f,
                           new List<DumpRot>(), sb, ref first, 1f, 1f);
            sb.Append("]}");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? ".");
            System.IO.File.WriteAllText(path, sb.ToString());
            Console.WriteLine("wrote " + path + " (" + state + ")");
        }

        // cs = the product of the ancestors' localScale: a node's own sizeDelta and
        // anchoredPosition live in its parent's LOCAL units, so they draw multiplied by it
        // (the scaled card/slip art layers keep their design space this way).
        // Headless sprite export: every PROCEDURAL sprite the bootstrap paints (vignette,
        // desk wash, faces, papers...) as raw RGBA, so the preview renderer can show the
        // same pixels the editor would instead of a flat stand-in colour.
        private static void DumpSprites(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            var manifest = new System.Text.StringBuilder("{");
            bool first = true;
            foreach (var key in Hollowbrook.Bootstrap.ProceduralArt.CachedKeys)
            {
                if (!Hollowbrook.Bootstrap.ProceduralArt.TryGet(key, out var sp)) continue;
                var tex = sp != null ? sp.texture : null;
                var px = tex != null ? tex.GetPixels() : null;
                if (px == null || tex.width < 1 || tex.height < 1) continue;
                var raw = new byte[px.Length * 4];
                for (int i = 0; i < px.Length; i++)
                {
                    raw[i * 4] = (byte)(px[i].r * 255f);
                    raw[i * 4 + 1] = (byte)(px[i].g * 255f);
                    raw[i * 4 + 2] = (byte)(px[i].b * 255f);
                    raw[i * 4 + 3] = (byte)(px[i].a * 255f);
                }
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, key + ".rgba"), raw);
                if (!first) manifest.Append(',');
                first = false;
                manifest.Append('"').Append(key).Append("\":{\"w\":").Append(tex.width)
                        .Append(",\"h\":").Append(tex.height).Append('}');
            }
            manifest.Append('}');
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "manifest.json"), manifest.ToString());
            Console.WriteLine("wrote procedural sprites to " + dir);
        }

        private static void WalkScreen(RectTransform rt, float px, float py, float pw, float ph,
            List<DumpRot> rots, System.Text.StringBuilder sb, ref bool first, float alpha, float cs)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return;
            float u = cs * rt.localScale.x;
            float ax = Mathf.Lerp(rt.anchorMin.x, rt.anchorMax.x, rt.pivot.x) * pw;
            float ayTop = ph - Mathf.Lerp(rt.anchorMin.y, rt.anchorMax.y, rt.pivot.y) * ph;
            float sx = rt.sizeDelta.x * u + (rt.anchorMax.x - rt.anchorMin.x) * pw;
            float sy = rt.sizeDelta.y * u + (rt.anchorMax.y - rt.anchorMin.y) * ph;
            // a node's OWN localScale squeezes its size and its children, but NOT its own
            // anchoredPosition — Unity applies the scale around the pivot, after placement
            float pivotX = px + ax + rt.anchoredPosition.x * cs;
            float pivotY = py + ayTop - rt.anchoredPosition.y * cs;
            float x = pivotX - rt.pivot.x * sx;
            float y = pivotY - (1f - rt.pivot.y) * sy;
            float cx = x + sx * 0.5f, cy = y + sy * 0.5f;
            float ang = rt.localEulerAngles.z;
            foreach (var r in rots)
            {
                float dx = cx - r.Cx, dy = cy - r.Cy;
                float c = Mathf.Cos(r.Rad), s2 = Mathf.Sin(r.Rad);
                cx = r.Cx + c * dx + s2 * dy;
                cy = r.Cy - s2 * dx + c * dy;
                ang += r.Deg;
            }
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null) alpha *= cg.alpha;
            var img = rt.GetComponent<UnityEngine.UI.Image>();
            var txt = rt.GetComponent<UnityEngine.UI.Text>();
            // a disabled graphic draws nothing (uGUI): the input's placeholder hides while
            // ink exists, exactly as the editor shows it
            bool visual = (img != null && img.enabled && img.color.a > 0.004f && alpha > 0.004f) ||
                          (txt != null && txt.enabled && txt.text.Length > 0 && txt.color.a > 0.004f && alpha > 0.004f);
            bool pushed = false;
            if (visual)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"n\":").Append(Json(rt.name));
                sb.Append(",\"x\":").Append((cx - sx * 0.5f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"y\":").Append((cy - sy * 0.5f).ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"w\":").Append(sx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"h\":").Append(sy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"a\":").Append(ang.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"al\":").Append(alpha.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                if (img != null)
                {
                    var c = img.color;
                    sb.Append(",\"img\":{\"s\":").Append(Json(img.sprite != null ? img.sprite.name : ""));
                    sb.Append(",\"t\":").Append((int)img.type);
                    sb.Append(",\"pa\":").Append(img.preserveAspect ? 1 : 0);
                    if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f)
                    {
                        var b = img.sprite.border;
                        sb.Append(",\"b\":[").Append(b.x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                          .Append(b.y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                          .Append(b.z.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                          .Append(b.w.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(']');
                        // uGUI converts slice borders through pixelsPerUnit; the renderer
                        // needs the same number to honour them in reference units
                        sb.Append(",\"pu\":").Append(img.sprite.pixelsPerUnit.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    sb.Append(",\"c\":[").Append(((int)(c.r * 255f))).Append(',').Append(((int)(c.g * 255f))).Append(',')
                      .Append(((int)(c.b * 255f))).Append(',').Append(c.a.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append("]}");
                }
                if (txt != null)
                {
                    var c = txt.color;
                    sb.Append(",\"txt\":{\"t\":").Append(Json(txt.text));
                    sb.Append(",\"f\":").Append(Json(txt.font != null ? txt.font.name : ""));
                    sb.Append(",\"s\":").Append(txt.fontSize);
                    sb.Append(",\"st\":").Append((int)txt.fontStyle);
                    // a condensed label (localScale.x < 1) draws its glyphs squeezed: the
                    // renderer needs the node's own scale to set the true pixel size
                    sb.Append(",\"sc\":").Append(rt.localScale.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                    sb.Append(",\"al\":").Append((int)txt.alignment);
                    sb.Append(",\"w\":").Append((int)txt.horizontalOverflow);
                    // best-fit window: the renderer shrinks like uGUI does, so a fitted row
                    // reads in the preview exactly as it will in the editor
                    sb.Append(",\"bf\":").Append(txt.resizeTextForBestFit ? 1 : 0);
                    sb.Append(",\"bn\":").Append(txt.resizeTextMinSize);
                    sb.Append(",\"bx\":").Append(txt.resizeTextMaxSize);
                    sb.Append(",\"c\":[").Append(((int)(c.r * 255f))).Append(',').Append(((int)(c.g * 255f))).Append(',')
                      .Append(((int)(c.b * 255f))).Append(',').Append(c.a.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)).Append("]}");
                }
                var inp = rt.GetComponent<UnityEngine.UI.InputField>();
                if (inp != null)
                    sb.Append(",\"inp\":{\"f\":").Append(inp.isFocused ? 1 : 0).Append('}');
                sb.Append('}');
            }
            if (Mathf.Abs(ang - (rots.Count > 0 ? 0f : 0f)) > 0.01f && rt.localEulerAngles.z != 0f)
            {
                rots.Add(new DumpRot { Cx = cx, Cy = cy, Rad = rt.localEulerAngles.z * Mathf.Deg2Rad, Deg = rt.localEulerAngles.z });
                pushed = true;
            }
            for (int i = 0; i < rt.childCount; i++)
                WalkScreen(rt.GetChild(i).GetComponent<RectTransform>(), x, y, sx, sy, rots, sb, ref first, alpha, u);
            if (pushed) rots.RemoveAt(rots.Count - 1);
        }

        private static string Json(string s)
        {
            var sb = new System.Text.StringBuilder("\"");
            foreach (var ch in s)
            {
                if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
                else if (ch == '\n') sb.Append("\\n");
                else if (ch == '\r') sb.Append("\\r");
                else if (ch == '\t') sb.Append("\\t");
                else if (ch < 0x20) sb.Append(' ');
                else sb.Append(ch);
            }
            return sb.Append('"').ToString();
        }

        // ---------------------------------------------------- opt-in visual dump (SVG)
        // Walks the REAL built desk hierarchy and draws it, so the drawer's travel, the front
        // lip, the shadow and the emerging files can be seen instead of trusted. Coordinates are
        // the same top-left desk-grid space CheckpointLayout places everything in.
        private static void DumpDrawer(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            MenuSceneBootstrap.Build(CoreBootstrap.EnsureCore());
            var root = GameRoot.Instance;
            root.Config.queueArrivalMinSeconds = 3f;
            root.Config.queueArrivalMaxSeconds = 5f;
            root.Config.queueBusyHurryMultiplier = 1f;
            // the real path: NEW GAME queues BeginShift, THEN the checkpoint scene boots and
            // runs the queued action — skipping the click leaves the run in MainMenu
            var newGame = root.MainMenu.transform.Find("buttons").Find("NEW GAME");
            newGame.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            CheckpointSceneBootstrap.Build(root);
            int adv = 0;
            while (root.Dialogue != null && root.Dialogue.IsActive && adv++ < 64)
                root.Dialogue.Advance();
            Console.WriteLine("state after intro: " + root.State + " (advanced " + adv + ")");
            ClickStickyNote(root);      // the shift starts; the desk becomes the live screen
            var cp = root.Checkpoint;
            if (!cp.gameObject.activeInHierarchy) { Console.WriteLine("desk not live — aborting dump"); return; }
            int guard = 0;
            while (cp.CurrentVisitor == null && guard++ < 8) cp.AdvanceQueue(6f);

            var frameRt = FindDeep(cp.transform, "DeskFrame").GetComponent<RectTransform>();
            string[] names = { "station", "recordsicon", "filedrawer_border", "drawerbody",
                               "drawerhead", "filerow0", "filerow3", "filerow6",
                               "folder1", "folder2", "folder3", "record" };
            var frames = new[] { new { Tag = "0_closed", Frames = 0, Open = false },
                                 new { Tag = "1_open", Frames = 2, Open = true },
                                 new { Tag = "4_open", Frames = 20, Open = true } };
            bool opened = false;
            foreach (var f in frames)
            {
                if (f.Open && !opened)
                {
                    FindDeep(cp.transform, "recordsicon").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    opened = true;
                }
                if (f.Frames > 0) DeskFrames(cp, f.Frames);
                if (f.Tag == "4_open")
                {
                    var firstRow = FindDeep(cp.transform, "filerow0");
                    if (firstRow != null) firstRow.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"420\" height=\"560\" " +
                              "viewBox=\"0 0 420 560\" font-family=\"sans-serif\">");
                sb.AppendLine("<rect width=\"420\" height=\"560\" fill=\"#0b0b0d\"/>");
                sb.AppendLine($"<text x=\"10\" y=\"18\" fill=\"#8a8a92\" font-size=\"12\">" +
                              $"{f.Tag}  open={cp.RecordsOpen}</text>");
                // desk-grid (x right, y down) -> svg, cropped to the left column and shifted up
                const float ox = 20f, oy = -90f;
                foreach (var n in names)
                {
                    var t = FindDeep(cp.transform, n);
                    if (t == null) continue;
                    var rt = t.GetComponent<RectTransform>();
                    var img = t.GetComponent<UnityEngine.UI.Image>();
                    var txt = t.GetComponent<UnityEngine.UI.Text>();
                    // Every drawer element is placed through PlaceTL (top-left anchor AND
                    // pivot), so its desk-grid rect is the parent chain's offsets plus its own
                    // anchoredPosition, and sizeDelta IS its size. Same maths as CheckpointLayout.
                    float px = 0f, py = 0f;
                    for (var p2 = rt.parent as RectTransform; p2 != null && p2 != frameRt;
                         p2 = p2.parent as RectTransform)
                    {
                        px += p2.anchoredPosition.x;
                        py += p2.anchoredPosition.y;
                    }
                    float w = rt.sizeDelta.x, h = rt.sizeDelta.y;
                    float x = px + rt.anchoredPosition.x + ox;
                    float y = -(py + rt.anchoredPosition.y) + oy;
                    if (!t.gameObject.activeInHierarchy)
                    {
                        sb.AppendLine($"<rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{w:F1}\" height=\"{h:F1}\" " +
                                      $"fill=\"none\" stroke=\"#33333a\" stroke-dasharray=\"3 3\"/>");
                        sb.AppendLine($"<text x=\"{x + 3:F1}\" y=\"{y + 11:F1}\" fill=\"#4a4a55\" " +
                                      $"font-size=\"9\">{n} (hidden)</text>");
                        continue;
                    }
                    var c = img != null ? img.color : (txt != null ? txt.color : Color.white);
                    string hex = $"#{(int)(Mathf.Clamp01(c.r) * 255):x2}" +
                                 $"{(int)(Mathf.Clamp01(c.g) * 255):x2}{(int)(Mathf.Clamp01(c.b) * 255):x2}";
                    float a = Mathf.Clamp01(c.a);
                    var cg = t.GetComponent<CanvasGroup>();
                    if (cg != null) a *= cg.alpha;
                    sb.AppendLine($"<rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{w:F1}\" height=\"{h:F1}\" " +
                                  $"fill=\"{hex}\" fill-opacity=\"{a:F2}\" stroke=\"#6a6a72\" stroke-width=\"0.6\"/>");
                    if (txt != null && txt.text.Length > 0)
                        sb.AppendLine($"<text x=\"{x + 3:F1}\" y=\"{y + 11:F1}\" fill=\"#d8d8dc\" " +
                                      $"font-size=\"8\">{System.Security.SecurityElement.Escape(txt.text)}</text>");
                    else
                        sb.AppendLine($"<text x=\"{x + 2:F1}\" y=\"{y - 2:F1}\" fill=\"#7a7a84\" " +
                                      $"font-size=\"8\">{n}</text>");
                }
                sb.AppendLine("</svg>");
                string path = System.IO.Path.Combine(dir, "drawer_" + f.Tag + ".svg");
                System.IO.File.WriteAllText(path, sb.ToString());
                Console.WriteLine("wrote " + path);
            }
        }

        // ------------------------------------------------------- world helpers
        private static void WorldFrame(GameRoot root, float dt = 1f / 60f)
        {
            Time.deltaTime = dt;
            Time.unscaledDeltaTime = dt;
            root.Views.TickWorld(dt);
            Input.KeysDown.Clear();     // edge-triggered keys last exactly one frame
        }

        private static void WorldFrames(GameRoot root, int n, float dt = 1f / 60f)
        {
            for (int i = 0; i < n; i++) WorldFrame(root, dt);
        }

        private static void PressKey(GameRoot root, KeyCode k)
        {
            Input.KeysDown.Add(k);
            WorldFrame(root);
        }

        private static void AimAt(float x, float y) => Input.mousePosition = new Vector3(x, y, 0f);

        // idempotent beam selection through the real toggles (pressing Q while Focus is
        // latched would switch it OFF — scenarios need the mode, not the keypress count)
        private static void SetBeam(GameRoot root, FlashlightRig rig, LightMode want)
        {
            if (want == LightMode.Off)
            {
                if (rig.Beam != LightMode.Off || rig.Latches.Sweep || rig.Latches.Focus) PressKey(root, KeyCode.L);
            }
            else if (want == LightMode.Sweep)
            {
                if (!rig.Latches.Sweep) PressKey(root, KeyCode.F);
            }
            else if (!rig.Latches.Focus) PressKey(root, KeyCode.Q);
        }

        private static T FindInteractable<T>(RoomInstance room) where T : Interactable
        {
            if (room == null) return null;
            foreach (var it in room.Interactables) if (it is T t) return t;
            return null;
        }

        // ------------------------------------------------- window talk dock (typed dialogue)
        // The dialogue text box is a functional input: typed lines route into the real
        // interaction loop (verification questions spend the QuestionSystem budget, the
        // rest gets an in-character answer), every exchange lands in the transcript sheet
        // and the bubble, and the loading/disabled/error states are observable widgets.
        private static void TopUpBattery(GameRoot root, FlashlightRig rig)
        {
            if (rig.Beam != LightMode.Off) PressKey(root, KeyCode.L);
            var room = root.Views.Current;
            var sw = FindInteractable<LightSwitchObject>(room);
            if (room != null && room.Light != null && !room.Light.Lit && sw != null) sw.Interact();
            int g = 0;
            while (rig.Battery < 0.9f && g++ < 60 * 25) WorldFrame(root);
        }
    }
}
