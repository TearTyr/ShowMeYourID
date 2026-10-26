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

        private static void Main()
        {
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
            FaceCompareProof(root);
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

            Console.WriteLine(_failures == 0 ? "== FLOW CHAIN COMPLETE ==" : "== " + _failures + " CHECK(S) FAILED ==");
            Environment.Exit(_failures == 0 ? 0 : 1);
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
                "record", "station", "folder1", "folder2", "filedrawer_border", "closefile", "idcard",
                "windowframe", "glass", "facade", "visitor", "glitch", "sill",
                "speechbubble", "bubble_border", "bubble", "bubbletext", "bubbletail",
                "offeredid", "patrol", "endshift",
                "qpanel_border", "q0", "q1", "q2", "q3",
                "records", "map", "notes", "stamp_allow", "stamp_deny",
                "OverlayRules", "OverlayLog",
                "StickyNoteOverlay", "stickynote_border",
            };
            foreach (var n in deskNames)
                Check(FindDeep(root.Checkpoint.transform, n) != null, "desk element '" + n + "'");

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
            Console.WriteLine("== code table vs approved wireframe (desk_layout_wireframe.svg) ==");
            ExpectRegion("top bar", L.TopBar, 0, 0, 1280, 81);
            ExpectRegion("banner", L.Banner, 22, 23, 320, 34);
            ExpectRegion("sanity", L.SanityMeter, 471, 23, 347, 34);
            ExpectRegion("light", L.LightMeter, 348, 84, 200, 40);
            ExpectRegion("track", L.TrackMeter, 568, 84, 110, 40);
            ExpectRegion("clock", L.Clock, 975, 23, 140, 34);
            ExpectRegion("pause", L.Pause, 1121, 23, 138, 34);
            ExpectRegion("records station", L.Station, 42, 119, 268, 140);
            ExpectRegion("file drawer", L.FileDrawer, 55, 278, 243, 200);
            ExpectRegion("id card", L.IdCard, 428, 506, 304, 209);
            ExpectRegion("window frame", L.Window, 412, 182, 455, 260);
            ExpectRegion("glass", L.Glass, 431, 199, 420, 241);
            ExpectRegion("portrait", L.Portrait, 431, 199, 420, 241);
            ExpectRegion("sill", L.Sill, 412, 442, 455, 14);
            ExpectRegion("speech bubble", L.SpeechBubble, 740, 288, 215, 83);
            ExpectRegion("patrol button", L.PatrolBtn, 26, 688, 180, 28);
            ExpectRegion("end shift", L.EndShiftBtn, 1074, 688, 180, 28);
            ExpectRegion("questions", L.Questions, 961, 110, 298, 201);
            ExpectRegion("TL", L.UtilityRecords, 23, 552, 150, 20);
            ExpectRegion("MAP", L.UtilityMap, 977, 337, 268, 146);
            ExpectRegion("RN", L.UtilityNotes, 181, 552, 150, 20);
            ExpectRegion("sticky note", L.StickyNote, 390, 200, 500, 320);
            // question buttons: wireframe x=980, y=118, 272x38, step 42 (panel-local grid)
            Check(Math.Abs(L.Inner.QBtnX - 18) < 0.01f && Math.Abs(L.Inner.QBtnY0 - 45) < 0.01f &&
                  Math.Abs(L.Inner.QBtnStep - 34) < 0.01f && Math.Abs(L.Inner.QBtnW - 266) < 0.01f &&
                  Math.Abs(L.Inner.QBtnH - 26) < 0.01f &&
                  L.Inner.QBtnY0 + 4 * L.Inner.QBtnStep <= L.Questions.H + 0.01f,
                "question buttons == the night-shift panel grid (18,45 + i*34, 266x26), 4 fit");
            // folder tabs: station-local grid (x=8, y=20, step 26, 304x24) + file rows:
            // drawer-local grid (x=10, y=28, step 32, 300x30) — 7 rows fit the region
            Check(Math.Abs(L.Inner.FolderTabX - 21) < 0.01f && Math.Abs(L.Inner.FolderTabY0 - 26) < 0.01f &&
                  Math.Abs(L.Inner.FolderTabStep - 40) < 0.01f && Math.Abs(L.Inner.FolderTabW - 224) < 0.01f &&
                  Math.Abs(L.Inner.FolderTabH - 38) < 0.01f,
                "folder tabs == the registry grid (21,26 + i*40, 224x38)");
            Check(Math.Abs(L.Inner.FileRowX - 8) < 0.01f && Math.Abs(L.Inner.FileRowY0 - 28) < 0.01f &&
                  Math.Abs(L.Inner.FileRowStep - 24) < 0.01f && Math.Abs(L.Inner.FileRowW - 225) < 0.01f &&
                  L.Inner.FileRowY0 + 7 * L.Inner.FileRowStep <= L.FileDrawer.H + 0.01f,
                "file rows == drawer grid (8,28 + i*24, 225x22) and 7 rows fit");
            // the retired appearance check left its slot to the stamp tray: the right
            // column reads questions-above / stamps-below, with the tray hugging the panel
            Check(Mathf.Abs(L.StampAllow.W - L.StampDeny.W) < 0.01f &&
                  Mathf.Abs(L.StampAllow.H - L.StampDeny.H) < 0.01f &&
                  Mathf.Abs(L.StampAllow.Y - L.StampDeny.Y) < 0.01f &&
                  Mathf.Abs(L.StampDeny.X - L.StampAllow.Right - 8f) < 0.01f &&
                  L.StampAllow.Y >= L.UtilityMap.Bottom &&
                  L.StampDeny.Right <= 1275f,
                "the APPROVE/DECLINE blocks are one aligned row, 8px apart, under the map panel, inside the desk");
            Check(L.StampAllow.W >= 96f && L.StampAllow.H >= 96f,
                "the stamp blocks keep a comfortable >=96px grab area");
            Check(L.SpeechBubble.X >= 738f && L.SpeechBubble.Right <= L.Questions.X &&
                  L.SpeechBubble.Y >= L.Window.Y && L.SpeechBubble.Bottom <= L.Sill.Bottom,
                "the speech bubble box stays beside the head, clear of the right column and the sill");
            Check(SpeechBubbleLayout.Height(L.Inner.BubbleMaxLines, L.Inner.BubbleLineH,
                      L.Inner.BubbleTagH, L.Inner.BubblePadTop, L.Inner.BubblePadBottom) <=
                  L.SpeechBubble.H + 0.01f,
                "the bubble's maximum text height fits inside its registered box");
            // hint/findings row: wireframe baseline y=72, findings right edge x=1264
            Check(Math.Abs(L.Findings.Right - 1264) < 0.01f, "findings right-aligned at x=1264");
            Check(Math.Abs(L.Hint.X - 26) < 0.01f, "hint lives in the left slot of the strip under the top bar");
            Check(L.Hint.Y >= 81 && L.Hint.Bottom <= 108 && L.Findings.Y >= 81 && L.Findings.Bottom <= 108 &&
                  L.RunStats.Y >= 81 && L.RunStats.Bottom <= 108,
                "hint/findings/run-stats live in the one row under the 81px top bar (84..105)");

            Console.WriteLine("== grid sanity: on screen, disjoint, contained ==");
            Rows.Clear();
            Add("banner", L.Banner, "B");
            Add("sanity meter", L.SanityMeter, "B");
            Add("light meter", L.LightMeter, "B");
            Add("track meter", L.TrackMeter, "B");
            Add("clock", L.Clock, "B");
            Add("pause", L.Pause, "B");
            Add("run stats", L.RunStats, "B");
            Add("mutation strip", L.Mutation, "B");
            Add("findings", L.Findings, "B");
            Add("hint", L.Hint, "E");
            Add("room info", L.RoomInfo, "E");
            Add("prompt", L.Prompt, "E");
            Add("records station", L.Station, "B");
            Add("file drawer", L.FileDrawer, "C");
            Add("id card", L.IdCard, "C");
            Add("window", L.Window, "C");
            Add("sill", L.Sill, "C");
            Add("speech bubble", L.SpeechBubble, "T");   // transient overlay beside the head
            Add("patrol button", L.PatrolBtn, "C");
            Add("end shift button", L.EndShiftBtn, "C");
            Add("questions panel", L.Questions, "C");
            Add("log button", L.UtilityRecords, "C");
            Add("map button", L.UtilityMap, "C");
            Add("rules button", L.UtilityNotes, "C");
            Add("deny stamp", L.StampDeny, "C");
            Add("allow stamp", L.StampAllow, "C");
            Add("queue strip left", L.QueueStripL, "C");
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
                  L.SpeechBubble.X == 740 && L.SpeechBubble.Y == 288,
                  "the speech bubble box hangs beside the portrait's head (740,288)");
            var bubbleInk = FindDeep(cp.transform, "bubbletext").GetComponent<UnityEngine.UI.Text>().text;
            Check(bubbleInk.Contains("<color=#6f6d68>AT THE WINDOW</color>"),
                  "the bubble line carries the student's speaker colour");

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
            Check(cardRatio >= 0.80f && cardRatio <= 0.85f,
                "the ID card's resting scale is 15-20% under the old default (got " + cardRatio + ")");
            var idGo0 = FindDeep(cp.transform, "idcard");
            Check(Mathf.Abs(idGo0.GetComponent<RectTransform>().localScale.x - cardRatio) < 0.01f &&
                  Mathf.Abs(idDrag.Zoom - 1f) < 0.01f,
                "the card rests at the smaller default while Zoom reads 1x");
            Check(Mathf.Abs(idGo0.GetComponent<RectTransform>().rect.width - 304f) < 0.5f &&
                  Mathf.Abs(idGo0.GetComponent<RectTransform>().rect.height - 209f) < 0.5f,
                "the card rect keeps the designed 304x209 slot (aspect untouched, scale does the shrink)");

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
            Check(budget.text.Contains("1 LEFT") && budget.text.Contains("●") && budget.text.Contains("○"),
                  "budget pips show one question left after the first ask (got '" + budget.text + "')");
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
            Check(budget.text.Contains("0 LEFT") && !budget.text.Contains("●"),
                  "budget pips empty out (got '" + budget.text + "')");
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
            var drawer = FindDeep(cp.transform, "filedrawer_border");
            var zone = FindDeep(cp.transform, "drawerzone");
            var zoneHover = zone.GetComponent<Hollowbrook.UI.HoverSense>();
            Check(!FindDeep(cp.transform, "folder1").gameObject.activeSelf && !drawer.gameObject.activeSelf,
                "DRAWER: floor files start HIDDEN inside the closed drawer (front label + handle only)");
            zoneHover.OnPointerEnter(null);
            Check(FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "DRAWER: hovering the shared area opens it (tabs revealed)");
            zoneHover.OnPointerEnter(null);
            Check(FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "DRAWER: re-entering (handle -> contents) never flickers it shut");
            var tab1 = FindDeep(cp.transform, "folder1").GetComponent<UnityEngine.UI.Button>();
            tab1.onClick.Invoke();
            Check(!FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "DRAWER: selecting a floor file closes the drawer AFTER the selection registers");
            zoneHover.OnPointerEnter(null);
            Check(drawer.gameObject.activeSelf && FindDeep(cp.transform, "filerow0") != null,
                "DRAWER: re-opening shows the selected floor's files");
            var headText = FindDeep(cp.transform, "drawerhead").GetComponent<UnityEngine.UI.Text>().text;
            Check(headText.Contains("1ST FLOOR") && headText.Contains("7 FILES"),
                  "the drawer head names the floor and counts its files (got '" + headText + "')");
            zoneHover.OnPointerExit(null);
            Check(!drawer.gameObject.activeSelf && !FindDeep(cp.transform, "folder1").gameObject.activeSelf,
                "DRAWER: leaving the whole interaction area closes it");
            zoneHover.OnPointerEnter(null);

            // pull a file: same record-window path the old FIND search triggered
            var row0 = FindDeep(cp.transform, "filerow0");
            string rowName = row0.Find("fname").GetComponent<UnityEngine.UI.Text>().text;
            row0.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "clicking a student file pulls it into the record window");
            Check(FindDeep(cp.transform, "rname").GetComponent<UnityEngine.UI.Text>().text
                      .ToUpperInvariant().Contains(rowName),
                  "the pulled record is the row that was clicked");
            Check(FindDeep(cp.transform, "stationout").GetComponent<UnityEngine.UI.Text>().text.Contains("FOUND:"),
                  "the station readout confirms the pulled file");
            Check(row0.GetComponent<UnityEngine.UI.Image>().color !=
                  FindDeep(cp.transform, "filerow1").GetComponent<UnityEngine.UI.Image>().color,
                  "the selected file row stays highlighted");
            var closeBtn = FindDeep(cp.transform, "closefile").GetComponent<UnityEngine.UI.Button>();
            closeBtn.onClick.Invoke();
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf, "the file's X closes the window");

            // the second floor folder: different files, and pulling one repopulates the sheet
            var tab2 = FindDeep(cp.transform, "folder2").GetComponent<UnityEngine.UI.Button>();
            tab2.onClick.Invoke();
            zoneHover.OnPointerEnter(null);   // selecting closed the drawer; reach back in
            var row2 = FindDeep(cp.transform, "filerow0");
            string row2Name = row2.Find("fname").GetComponent<UnityEngine.UI.Text>().text;
            Check(row2Name != rowName, "the second floor folder lists a different set of files");
            row2.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf &&
                  FindDeep(cp.transform, "rname").GetComponent<UnityEngine.UI.Text>().text
                      .ToUpperInvariant().Contains(row2Name),
                  "pulling from the second folder reopens the record sheet");

            // empty folder edge: the drawer still opens, with an explicit NO FILES line
            cp.PopulateDrawer(RecordSearch.FloorLabel(3), null, new int[0]);
            Check(drawer.gameObject.activeSelf && FindDeep(cp.transform, "nofiles") != null &&
                  FindDeep(cp.transform, "nofiles").GetComponent<UnityEngine.UI.Text>().text.Contains("NO FILES"),
                  "an empty folder shows a NO FILES state");
            tab1.onClick.Invoke();      // back to the real 1st-floor folder...
            tab1.onClick.Invoke();      // ...then shut the drawer: clean desk for the next checks
            Check(!drawer.gameObject.activeSelf, "the drawer closes again after browsing");

            // the slip: counter -> desk, stamp target, one stamp, pending until hand-back
            var slipGo = FindDeep(cp.transform, "slip");
            var slipDrag = slipGo.GetComponent<DragPaper>();
            slipDrag.BeginDrag();
            slipDrag.PlaceCenter(new Vector2(760f, 500f));
            slipDrag.EndDrag();
            Check(cp.PendingVerdict == Verdict.None, "an unstamped slip commits nothing");
            Check(HintText(root).Contains("STAMP THE SLIP"),
                  "hint matches state: both papers on the desk (got '" + HintText(root) + "')");
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
            Check(HintText(root).Contains("HAND BACK"),
                  "hint matches state: stamped, awaiting hand-back (got '" + HintText(root) + "')");
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
            var spineBack = idGo.Find("idback").Find("idstripeback").gameObject;
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
            Check(idGo.Find("idback").gameObject.activeSelf, "clicking the spine flips the ID to the back face");
            spineBack.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!idGo.Find("idback").gameObject.activeSelf, "the back face's own spine flips it home (reliable both ways)");

            // decoupled papers: lift the SLIP last, then flip the ID — the slip must not ride along
            var slipDragC = slipT.GetComponent<DragPaper>();
            slipDragC.BeginDrag();
            slipDragC.EndDrag();      // a tap on the taken slip is the SLIP's own flip
            Check(slipT.Find("slipback").gameObject.activeSelf, "tapping the taken slip turns the slip over by itself");
            Check(FindDeep(cp.transform, "stampoverlay").GetComponent<UnityEngine.UI.Image>().color.a > 0.9f,
                  "the stamp mark rides the slip's flip (it stays on the paper)");
            cp.FlipSlip();            // face-up again
            Check(!slipT.Find("slipback").gameObject.activeSelf && slipT.Find("bg").gameObject.activeSelf,
                  "and flips the slip back");
            spine.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();   // the slip was touched last...
            Check(idGo.Find("idback").gameObject.activeSelf,
                  "...yet clicking the ID's spine flips the ID — never the last-touched paper");
            Check(!slipT.Find("slipback").gameObject.activeSelf && slipT.Find("bg").gameObject.activeSelf,
                  "the slip stays exactly as it was: no mirroring, no hidden face");
            cp.FlipLastPaper();       // the F key routes to the last-touched paper — here the slip
            Check(slipT.Find("slipback").gameObject.activeSelf, "F flips whichever paper was lifted last (here: the slip)");
            var idDragC = idGo.GetComponent<DragPaper>();
            idDragC.BeginDrag(); idDragC.EndDrag();   // retarget zoom to the ID (no move, no tap handler)
            // both papers deliberately STAY face-down here: the desk loop's fresh-visitor
            // check proves the hand-over reset turns them face-up (flip never carries over)
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
            Check(Mathf.Abs(idGo.localScale.x - 1.8f * cardRatio) < 0.01f &&
                  Mathf.Abs(idGo.GetComponent<DragPaper>().Zoom - 1.8f) < 0.01f,
                "zoom clamps at 1.8x of the card's resting scale");
            cp.DebugZoom(0.1f);
            Check(Mathf.Abs(idGo.localScale.x - cardRatio) < 0.01f &&
                  Mathf.Abs(idGo.GetComponent<DragPaper>().Zoom - 1f) < 0.01f,
                "zoom clamps at the resting 1x (the smaller default card)");
            cp.DebugZoom(1f);
            // the ONLY remaining zoom input is the deliberate Z toggle (the scroll wheel
            // no longer resizes papers — there is no scroll listener on the desk at all)
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Z);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(Mathf.Abs(idGo.localScale.x - 1.5f * cardRatio) < 0.01f, "Z toggles the last-touched paper to 1.5x");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Z);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(Mathf.Abs(idGo.localScale.x - cardRatio) < 0.01f, "Z again returns it to the intended 1x size");
        }

        // ---------------------------------------------------- speech bubble proof
        // The student's voice lives in a comic bubble beside the portrait's head:
        // visible while the line is up, removed exactly 2 s after the dialogue ends,
        // re-shown (with a RESTARTED countdown) when a new line arrives mid-cooldown —
        // one clock, generation-guarded, so no stale timer can hide a newer dialogue.
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
            Check(text.text.Contains("AT THE WINDOW"), "SB: the bubble speaks with the student's tag");
            Check(text.text.StartsWith("<b><color="),
                "SB: the speaker tag renders bold, in the speaker's own ink");
            Check(tail.sprite != null, "SB: the bubble carries its pointing tail");
            bool borderRaycast = false, otherRaycast = false;
            foreach (var img in bubbleRoot.gameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (!img.raycastTarget) continue;
                if (img.gameObject.name == "bubble_border") borderRaycast = true; else otherRaycast = true;
            }
            foreach (var t in bubbleRoot.gameObject.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (t.raycastTarget) otherRaycast = true;
            Check(borderRaycast && !otherRaycast,
                "SB: exactly one raycast target — the bubble panel, for hover-to-read; text/fill stay click-through");
            Check(bubbleRoot.GetSiblingIndex() < FindDeep(cp.transform, "PapersLayer").GetSiblingIndex(),
                "SB: the bubble sits under the papers layer — a dragged paper rides over it");
            Check(L.Inner.BubbleTail.X < 0f && L.Inner.BubbleTail.Right > 0f,
                "SB: the tail straddles the panel's left edge (seamless comics seam)");
            Check(Mathf.Abs(panel.anchoredPosition.x - L.Inner.BubblePanelX) < 0.01f &&
                  Mathf.Abs(-panel.anchoredPosition.y) < 0.01f &&
                  Mathf.Abs(panel.sizeDelta.x - L.Inner.BubblePanelW) < 0.01f,
                "SB: the panel grows DOWN from the container top at full box width");
            Check(L.SpeechBubble.X >= 738f &&
                  L.SpeechBubble.Y < L.Portrait.Y + L.Portrait.H / 2f + 40f &&
                  L.SpeechBubble.Bottom < L.Sill.Bottom,
                "SB: the bubble hangs BESIDE the head — right of the face, at head height, above the sill");
            float h0 = panel.sizeDelta.y;
            Check(h0 >= 56f && h0 <= L.SpeechBubble.H + 0.01f,
                "SB: the panel wraps to the line count inside its box (h=" + h0 + ")");

            // the 2-second cooldown starts when the dialogue ENDS (the line is instant here)
            cp.TickBubble(1.9f);
            Check(bubbleRoot.gameObject.activeSelf && cp.BubbleClock.Visible,
                "SB: still up 1.9 s after the line — no premature hide while it is showing");
            cp.TickBubble(0.2f);
            Check(!cp.BubbleClock.Visible && !bubbleRoot.gameObject.activeSelf,
                "SB: removed exactly 2 s after the dialogue ended");
            cp.TickBubble(5f);
            Check(!bubbleRoot.gameObject.activeSelf, "SB: ticks after the hide change nothing");

            // hover-to-read through the REAL wiring: pointer enter suspends the countdown
            var hover = FindDeep(cp.transform, "bubble_border").GetComponent<Hollowbrook.UI.HoverSense>();
            Check(hover != null, "SB: the bubble panel carries the hover sensor");
            if (hover != null)
            {
                cp.DebugSay("Hold still — the player is reading this line.");
                cp.TickBubble(1.5f);
                hover.OnPointerEnter(null);
                Check(cp.BubbleClock.Hovering && bubbleRoot.gameObject.activeSelf,
                    "SB: the cursor entering the bubble registers as a hover");
                cp.TickBubble(12f);
                Check(bubbleRoot.gameObject.activeSelf && cp.BubbleClock.Visible,
                    "SB: the bubble stays up while hovered, long past the 2 s cooldown");
                hover.OnPointerExit(null);
                Check(!cp.BubbleClock.Hovering && cp.BubbleClock.SecondsSinceLine < 0.01f &&
                      bubbleRoot.gameObject.activeSelf,
                    "SB: leaving restarts the 2 s cooldown (dialogue over) — bubble still up at leave");
                cp.TickBubble(1.9f);
                Check(bubbleRoot.gameObject.activeSelf, "SB: still up 1.9 s after the cursor left");
                cp.TickBubble(0.2f);
                Check(!bubbleRoot.gameObject.activeSelf, "SB: gone 2 s after the cursor left");
                // hover while the line is fresh: leaving must not start the countdown early
                cp.DebugSay("Talking line — do not start the clock yet.");
                hover.OnPointerEnter(null);
                hover.OnPointerExit(null);
                Check(cp.BubbleClock.SecondsSinceLine < 0.01f,
                    "SB: leaving while the dialogue still runs keeps the countdown unstarted");
                cp.TickBubble(1.9f);
                Check(bubbleRoot.gameObject.activeSelf,
                    "SB: ...so the bubble waits for the dialogue to end before counting");
                cp.TickBubble(0.2f);
                Check(!bubbleRoot.gameObject.activeSelf, "SB: ...then hides 2 s after that");
                // a hidden bubble cannot be hovered (it stops raycasting when inactive)
                hover.OnPointerEnter(null);
                Check(!cp.BubbleClock.Hovering && !bubbleRoot.gameObject.activeSelf,
                    "SB: an enter while the bubble is hidden is ignored — it can never suspend a countdown");
            }

            // a new line during the cooldown cancels the pending hide and shows normally
            cp.DebugSay("One more thing — the intercom crackles.");
            Check(bubbleRoot.gameObject.activeSelf, "SB: a fresh line brings the bubble back");
            int gen1 = cp.BubbleClock.Generation;
            cp.TickBubble(1.5f);
            cp.DebugSay("Second line, mid-cooldown.");
            Check(bubbleRoot.gameObject.activeSelf && cp.BubbleClock.SecondsSinceLine < 0.01f,
                "SB: a new line mid-cooldown RESTARTS the countdown (pending hide cancelled)");
            Check(cp.BubbleClock.Generation == gen1 + 1,
                "SB: every line advances the generation — a stale timer could never hide it");
            Check(text.text.Contains("Second line"), "SB: the bubble shows the NEW line, not the old one");
            cp.TickBubble(1.9f);
            Check(bubbleRoot.gameObject.activeSelf, "SB: the newer dialogue survives past the OLD timer's 2 s");
            cp.TickBubble(0.2f);
            Check(!bubbleRoot.gameObject.activeSelf, "SB: ...and hides 2 s after the LAST line");

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
            Check(bubbleRoot.gameObject.activeSelf && text.text.Contains("AT THE WINDOW"),
                "SB: the asked question's answer arrives in the bubble");
            Check(!text.text.Contains("Your course?"),
                "SB: the bubble carries the STUDENT's answer (the button shows the question asked)");

            // leaving the desk cleans the bubble (and its countdown) up
            cp.HideBubbleNow();
            Check(!bubbleRoot.gameObject.activeSelf && !cp.BubbleClock.Visible,
                "SB: the room-change cleanup hides the bubble instantly — no timer survives");
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
            var soRt = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            Check(FindDeep(cp.transform, "stampbox") == null,
                "SP: no printed target box — the drop point decides where the mark lands");

            // ---- case 1: a drop at the centre of a ZOOMED slip lands at the drop point ----
            cp.DebugTakeDocuments();
            TakeSlip(root);                       // slip centred at (760,500): top-left (660,440)
            var slipDrag0 = slipT.GetComponent<DragPaper>();
            slipDrag0.Zoom = 1.5f;                // the Z-zoom path: the drop must map through the scale
            Check(Mathf.Abs(slipDrag0.Zoom - 1.5f) < 0.01f, "SP: the slip is zoomed to 1.5x for the drop");
            var want = InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null);
            StampAtPoint(root, want, new Vector2(810f, 530f));   // centre of the 1.5x slip
            Check(cp.SlipStamped && cp.PendingVerdict == want,
                "SP: the drop stamps the slip and records the pending verdict (workflow preserved)");
            Check(soRt.parent == slipT, "SP: the mark is a layer OF THE SLIP (it rides the paper)");
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
            Check(soRt.sizeDelta.x <= slipT.rect.width * 0.33f,
                $"SP: the mark covers {soRt.sizeDelta.x / slipT.rect.width:P0} of the slip's width — ink over paper, never a panel");
            var top = new Vector2(slipT.anchoredPosition.x, -slipT.anchoredPosition.y);
            var local = StampPlacement.DeskToLocal(810f, 530f, top.x, top.y,
                slipT.localScale.x, slipT.localEulerAngles.z);
            local = StampPlacement.ClampToPrintable(local, slipT.rect.width, slipT.rect.height,
                CheckpointView.StampMarkSize, CheckpointView.StampMaxTilt, CheckpointView.StampEdgePad);
            float mx = soRt.anchoredPosition.x, my = -soRt.anchoredPosition.y;
            Check(Mathf.Abs(slipT.localScale.x - 1.5f) < 0.01f &&
                  Mathf.Abs(mx - local.X) < 0.5f && Mathf.Abs(my - local.Y) < 0.5f,
                $"SP: the centre drop on the ZOOMED slip maps through its scale (got {mx:F1},{my:F1} want {local.X:F1},{local.Y:F1})");
            Check(Mathf.Abs(mx - 100f) < 12f && Mathf.Abs(my - 60f) < 12f,
                $"SP: ...and lands in the slip's middle, not a fixed corner (got {mx:F1},{my:F1})");
            Check(Mathf.Abs(slipT.localScale.x * soRt.localScale.x - 1.5f) < 0.01f,
                "SP: the mark zooms WITH the slip (it inherits the paper's scale)");
            float ang = Mathf.Abs(soRt.localEulerAngles.z);
            ang = Mathf.Min(ang, 360f - ang);
            float rad = ang * Mathf.Deg2Rad;
            float bound = soRt.sizeDelta.x * (Mathf.Cos(rad) + Mathf.Sin(rad)) / 2f;
            Check(mx - bound >= -0.01f && mx + bound <= slipT.rect.width + 0.01f &&
                  my - bound >= -0.01f && my + bound <= slipT.rect.height + 0.01f,
                $"SP: the tilted mark stays fully inside the slip (bounds ±{bound:F1} in {slipT.rect.width}x{slipT.rect.height})");

            // the mark rides the paper: drag the stamped slip elsewhere
            Vector2 markBefore = new Vector2(soRt.anchoredPosition.x, soRt.anchoredPosition.y);
            float cA = Mathf.Cos(slipT.localEulerAngles.z * Mathf.Deg2Rad);
            float sA = Mathf.Sin(slipT.localEulerAngles.z * Mathf.Deg2Rad);
            float lx = markBefore.x, ly = -markBefore.y;
            Vector2 deskBefore = new Vector2(top.x + (lx * cA + ly * sA) * slipT.localScale.x,
                                             top.y + (-lx * sA + ly * cA) * slipT.localScale.x);
            var slipDrag = slipT.GetComponent<DragPaper>();
            slipDrag.BeginDrag();
            slipDrag.PlaceCenter(new Vector2(620f, 540f));
            slipDrag.EndDrag();
            Check(Mathf.Abs(slipDrag.Zoom - 1.5f) < 0.01f,
                "SP: lifting and dropping never loses the slip's zoom");
            var top2 = new Vector2(slipT.anchoredPosition.x, -slipT.anchoredPosition.y);
            Vector2 deskAfter = new Vector2(top2.x + (lx * cA + ly * sA) * slipT.localScale.x,
                                            top2.y + (-lx * sA + ly * cA) * slipT.localScale.x);
            Check(soRt.parent == slipT &&
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

        // --------------------------------------- face comparison (ID photo -> window face)
        // The mechanic is informational and data-truthful: the cue follows VisitorData.
        // PhotoMatches (window portrait vs the photo printed on the card — the very structs
        // RosterFactory swaps for PhotoMismatch / PhotoSubtleMismatch), never a coin flip.
        private static void FaceCompareProof(GameRoot root)
        {
            Console.WriteLine("== face comparison: ID photo, then the face at the window ==");
            var cp = root.Checkpoint;
            int guard = 0;
            while (cp.CurrentVisitor == null && guard++ < 8) cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor != null, "FC: a visitor is at the window for the face proof");
            if (cp.CurrentVisitor == null) return;
            cp.DebugTakeDocuments();
            var photo = FindDeep(cp.transform, "idphoto");
            var photoHit = FindDeep(cp.transform, "idphotohit");
            var sel = photoHit != null ? photoHit.GetComponent<IdPhotoSelect>() : null;
            var portrait = FindDeep(cp.transform, "visitor");
            var portraitHit = FindDeep(cp.transform, "visitorhit");
            var click = portraitHit != null ? portraitHit.GetComponent<ClickSense>() : null;
            Check(sel != null && photoHit.GetComponent<UnityEngine.UI.Image>().raycastTarget &&
                  !photo.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                "FC: the ID photograph has its own click-catcher; the letterboxed Image stays click-through");
            Check(click != null && portraitHit.GetComponent<UnityEngine.UI.Image>().raycastTarget &&
                  !portrait.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                "FC: the student at the window is clickable through a hitbox fitted to the displayed sprite");
            // the hitboxes match the PIXELS, not the slot: preserveAspect letterboxes inside it
            var pImg = photo.GetComponent<UnityEngine.UI.Image>();
            var prRect = photo.GetComponent<RectTransform>().rect;
            var wantBox = Hollowbrook.UI.UIFactory.Letterbox(prRect.width, prRect.height,
                pImg.sprite.rect.width, pImg.sprite.rect.height);
            var hitRt = photoHit.GetComponent<RectTransform>();
            Check(Mathf.Abs(hitRt.sizeDelta.x - wantBox.W) < 0.01f &&
                  Mathf.Abs(hitRt.sizeDelta.y - wantBox.H) < 0.01f &&
                  Mathf.Abs(hitRt.anchoredPosition.x - wantBox.X) < 0.01f &&
                  Mathf.Abs(-hitRt.anchoredPosition.y - wantBox.Y) < 0.01f,
                $"FC: the photo hitbox is the letterboxed sprite rect ({wantBox.W:F0}x{wantBox.H:F0} in {prRect.width:F0}x{prRect.height:F0}), at any card zoom or position");
            var vImg = portrait.GetComponent<UnityEngine.UI.Image>();
            var vRect = portrait.GetComponent<RectTransform>().rect;
            var wantV = Hollowbrook.UI.UIFactory.Letterbox(vRect.width, vRect.height,
                vImg.sprite.rect.width, vImg.sprite.rect.height);
            var vHitRt = portraitHit.GetComponent<RectTransform>();
            Check(Mathf.Abs(vHitRt.sizeDelta.x - wantV.W) < 0.01f &&
                  Mathf.Abs(vHitRt.sizeDelta.y - wantV.H) < 0.01f,
                "FC: the student hitbox matches the displayed portrait, scaled or not");
            var selBorder = FindDeep(cp.transform, "photosel").GetComponent<UnityEngine.UI.Image>();
            var cueBorder = FindDeep(cp.transform, "facecue").GetComponent<UnityEngine.UI.Image>();
            Check(selBorder.sprite != null && selBorder.sprite.name == "outline_frame" &&
                  cueBorder.sprite != null && cueBorder.sprite.name == "outline_frame",
                "FC: selection/cue inks are HOLLOW frames — they outline, never cover, the photo");
            Check(Mathf.Abs(selBorder.color.a) < 0.01f && Mathf.Abs(cueBorder.color.a) < 0.01f,
                "FC: born unselected, with no cue playing");

            // wrong order first: the student click asks for the photo instead of guessing
            cp.DebugClickWindowStudent();
            Check(!cp.FacePhotoSelected && Mathf.Abs(cueBorder.color.a) < 0.01f,
                "FC: clicking the student first compares nothing — it asks for the ID photo");
            string toast = FindDeep(root.HUD.transform, "toast").GetComponent<UnityEngine.UI.Text>().text;
            Check(toast.Contains("ID PHOTO first"),
                "FC: ...with a one-line instruction (got '" + toast + "')");

            // a clean click on the photo selects it (thin outline on the PHOTO, not the card)
            sel.OnEndDrag(new UnityEngine.EventSystems.PointerEventData());
            Check(cp.FacePhotoSelected && selBorder.color.a > 0.5f,
                "FC: a clean click on the photograph selects it");
            var photoRt = photo.GetComponent<RectTransform>();
            var cardRt0 = FindDeep(cp.transform, "idcard").GetComponent<RectTransform>();
            Check(selBorder.rectTransform.parent == photoRt && cardRt0 != photoRt &&
                  selBorder.rectTransform.anchorMin == Vector2.zero &&
                  selBorder.rectTransform.anchorMax == Vector2.one &&
                  selBorder.rectTransform.offsetMin == Vector2.zero &&
                  selBorder.rectTransform.offsetMax == Vector2.zero &&
                  !selBorder.raycastTarget,
                "FC: the hollow selection frame is stretched over the PHOTOGRAPH alone and eats no clicks");

            // a real drag that STARTS on the photo still drags the card (forwarded past 6px)
            var cardRt = FindDeep(cp.transform, "idcard").GetComponent<RectTransform>();
            Vector2 before = cardRt.anchoredPosition;
            sel.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData());
            sel.OnDrag(new UnityEngine.EventSystems.PointerEventData { delta = new Vector2(40f, 10f) });
            sel.OnEndDrag(new UnityEngine.EventSystems.PointerEventData());
            Check(Vector2.Distance(cardRt.anchoredPosition, before) > 5f && cp.FacePhotoSelected,
                "FC: dragging from the photo still moves the card — and does not toggle the selection");

            // compare: the cue follows the run's own pairing truth, briefly, then clears
            bool stampedBefore = cp.SlipStamped;
            var pendBefore = cp.PendingVerdict;
            bool want = cp.CurrentVisitor.PhotoMatches;
            bool got = cp.DebugCompareFaces();
            Check(got == want, $"FC: the comparison follows the pairing truth (PhotoMatches={want})");
            Check(!cp.FacePhotoSelected, "FC: the comparison consumes the selection");
            Check(cp.FaceCueCount == 1, "FC: exactly one cue played for one comparison (no pile-up)");
            Check(cp.LastFaceCueMatch == want, "FC: the cue that played matches the pairing truth");
            Check(Mathf.Abs(cueBorder.color.a) < 0.01f,
                "FC: the cue clears itself when the pulse ends (brief, never lingering)");
            Check(cp.SlipStamped == stampedBefore && cp.PendingVerdict == pendBefore,
                "FC: the cue changes no verdict state — stamping stays the player's call");

            // temporary face/stamp assets ship in the project's own override folder
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
            Check(allFaces, "FC: every temporary student/anomaly face asset ships under Resources/Art");
            Check(artDir != null && System.IO.File.Exists(
                System.IO.Path.Combine(artDir, "stamp_approval_temp.png")),
                "FC: the temporary approval stamp asset ships under Resources/Art");
            Check(cp.CurrentVisitor.FaceAssetIndex >= 0 || cp.CurrentVisitor.IsAdministrator,
                "FC: visitors carry a stable face-asset index (admin excepted)");
            int fidx = cp.CurrentVisitor.FaceAssetIndex;
            Check(fidx < 0 || Hollowbrook.Logic.FaceAssets.WindowKey(fidx, true) ==
                  Hollowbrook.Logic.FaceAssets.StudentKey(fidx) + "_anomaly",
                "FC: the anomaly variant is paired with the same student as the normal portrait");
            // no white boxes anywhere on the desk: a null-sprite Image would render uGUI white
            // the bug class: a face slot (or any sprite-carrying slot) showing uGUI's default
            // white texture. Solid panels are sprite-less BY DESIGN, so the invariant is: every
            // face-chain Image has a real sprite when visible, and every sprite-less overlay ink
            // (selection frame, cue frame, stamp mark pre-stamp) is fully transparent.
            var offeredImg = FindDeep(cp.transform, "photo").GetComponent<UnityEngine.UI.Image>();
            Check(pImg.sprite != null && vImg.sprite != null && offeredImg.sprite != null,
                "FC: photo, window portrait and counter photo all carry real sprites (no white texture)");
            var vWant = Hollowbrook.UI.UIFactory.Letterbox(L.Portrait.W, L.Portrait.H,
                vImg.sprite.rect.width, vImg.sprite.rect.height);
            var vRt = portrait.GetComponent<RectTransform>();
            Check(Mathf.Abs(vRt.sizeDelta.x - vWant.W) < 0.5f && Mathf.Abs(vRt.sizeDelta.y - vWant.H) < 0.5f &&
                  Mathf.Abs(vRt.anchoredPosition.x - vWant.X) < 0.5f,
                $"FC: the portrait Image rect IS the letterbox of its sprite, centred in the window ({vWant.W:F0}x{vWant.H:F0})");
            var pWant = Hollowbrook.UI.UIFactory.Letterbox(L.Inner.IdPhoto.W, L.Inner.IdPhoto.H,
                pImg.sprite.rect.width, pImg.sprite.rect.height);
            var pRt = photo.GetComponent<RectTransform>();
            Check(Mathf.Abs(pRt.sizeDelta.x - pWant.W) < 0.5f && Mathf.Abs(pRt.sizeDelta.y - pWant.H) < 0.5f,
                "FC: the ID photo Image rect IS the letterbox of its sprite inside the card slot");
            bool inkLeak = false;
            foreach (var n in new[] { "photosel", "facecue", "stampoverlay", "idphotohit", "visitorhit" })
            {
                var t = FindDeep(cp.transform, n);
                if (t == null) continue;
                var im = t.GetComponent<UnityEngine.UI.Image>();
                if (im != null && im.sprite == null && im.color.a > 0.01f) inkLeak = true;
            }
            Check(!inkLeak, "FC: sprite-less overlay inks stay fully transparent until they mean something");
            Check(pImg.sprite != null && pImg.sprite.name != "face_placeholder",
                "FC: the ID photo shows a real face sprite once the visitor is presented");
            int idx = cp.CurrentVisitor.FaceAssetIndex;
            Check(Hollowbrook.Logic.FaceAssets.WindowKey(idx, false) ==
                  Hollowbrook.Logic.FaceAssets.WindowKey(idx, false),
                "FC: the same student always maps to the same face key (stable identity)");
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
            Check(!FindDeep(desk, "idcard").Find("idback").gameObject.activeSelf &&
                  !FindDeep(desk, "slip").Find("slipback").gameObject.activeSelf,
                  "the fresh visitor's papers are born FACE-UP (flip state never carries over)");
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
            Check(HintText(root).Contains("STAMP THE SLIP"),
                  "AUTO-DOCS: the hint jumps straight to stamping (got '" + HintText(root) + "')");
            var cardPos = FindDeep(desk, "idcard").GetComponent<RectTransform>().anchoredPosition;
            var slipPos = FindDeep(desk, "slip").GetComponent<RectTransform>().anchoredPosition;
            Check(cardPos.y < 0 && slipPos.y < 0 && Mathf.Abs(slipPos.x - 750f) < 1f,
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
            HandBack(root, true, false);
            Check(root.Queue.ProcessedCount == procBeforeStamp &&
                  cp.CurrentVisitor != null && cp.CurrentVisitor.ClaimedName == beforeYes,
                  "the student waits while one paper is missing");
            HandBack(root, false, true);
            Check(root.Queue.ProcessedCount == procBeforeStamp + 1,
                  "hand-back of ID + stamped slip commits the verdict");
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
            Check(budgetText.Contains("1 LEFT"), "Q8: the question budget was NOT refunded ('" + budgetText + "')");
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
            StampAtPoint(root, cornerWant, new Vector2(672f, 448f));   // just inside the corner
            var soT = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            var slT = FindDeep(cp.transform, "slip").GetComponent<RectTransform>();
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
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(!cp.SlipStamped, "issue #5: a paused desk never stamps from hotkeys");
            root.TogglePause(false);
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.A);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(cp.SlipStamped, "issue #5: unpaused, the same key stamps the slip");
            var kbMark = FindDeep(cp.transform, "stampoverlay").GetComponent<RectTransform>();
            var kbSlip = FindDeep(cp.transform, "slip").GetComponent<RectTransform>();
            var kbLocal = StampPlacement.ClampToPrintable(new StampPlacement.Point(
                kbSlip.rect.width * 0.8f, kbSlip.rect.height * 0.68f),
                kbSlip.rect.width, kbSlip.rect.height,
                CheckpointView.StampMarkSize, CheckpointView.StampMaxTilt, CheckpointView.StampEdgePad);
            Check(kbMark.parent == kbSlip &&
                  Mathf.Abs(kbMark.anchoredPosition.x - kbLocal.X) < 0.5f &&
                  Mathf.Abs(-kbMark.anchoredPosition.y - kbLocal.Y) < 0.5f,
                "issue #5: the keyboard stamp lands on the corner default spot, fully on the paper");
            Check(Mathf.Abs(kbMark.anchoredPosition.x - tlx) +
                  Mathf.Abs(kbMark.anchoredPosition.y + tly) > 20f,
                "issue #5: keyboard default and corner drop are DIFFERENT spots (no single fixed position)");

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
            Check(rec.gameObject.activeSelf &&
                  FindDeep(cp.transform, "stationout").GetComponent<UnityEngine.UI.Text>().text.Contains("ATTACHED MEMO"),
                "issue #4: pulling student files can't replace the memo (NO RECORD — see memo)");
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

        private static int CountDeep(Transform t, string name)
        {
            int n = t.name == name ? 1 : 0;
            for (int i = 0; i < t.childCount; i++) n += CountDeep(t.GetChild(i), name);
            return n;
        }

        // charge the battery back up (lit room, beam off) so long Focus scenarios never
        // die mid-assertion; keeps the room lit on exit.
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
