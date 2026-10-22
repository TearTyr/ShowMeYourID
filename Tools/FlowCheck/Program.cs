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
            int speech = DeepIndex(checkpointRoot, "speech_border");
            Check(window >= 0 && visitor > window && speech > window,
                $"B3: windowframe sibling {window} is BEHIND visitor {visitor} and speech {speech}");

            RequiredElements(root);
            LayoutAudit(root);
            SpeechProof(root);
            PapersProof(root);
            SaveContinueResume();
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
                "record", "station", "find", "closefile", "idcard",
                "windowframe", "glass", "facade", "visitor", "glitch", "sill",
                "speech_border", "speechlog", "offeredid", "interview", "patrol", "endshift",
                "qpanel_border", "q0", "q1", "q2", "q3", "testimony",
                "scan", "records", "map", "notes", "stamp_allow", "stamp_deny",
                "monitor_border", "out",
                "OverlayRules", "OverlayLog",
                "StickyNoteOverlay", "stickynote_border",
            };
            foreach (var n in deskNames)
                Check(FindDeep(root.Checkpoint.transform, n) != null, "desk element '" + n + "'");

            // presentation cuts: the checklist widget is gone for good, and the waiting-line
            // display is built exactly when ShowcaseBuild.LineDisplay says so (never built
            // then hidden, never referenced from anywhere else)
            Check(FindDeep(root.Checkpoint.transform, "checklist_border") == null,
                "the checklist widget is removed from the desk");
            string[] lineNames =
            {
                "queuestatus", "queuestripL", "queuestripR", "qsil0", "qsil7", "qoverflow", "qbadge",
            };
            foreach (var n in lineNames)
            {
                bool present = FindDeep(root.Checkpoint.transform, n) != null;
                Check(present == ShowcaseBuild.LineDisplay,
                    "line display element '" + n + "' is built exactly when LineDisplay is on");
            }

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
            ExpectRegion("top bar", L.TopBar, 0, 0, 1280, 52);
            ExpectRegion("banner", L.Banner, 16, 4, 320, 44);
            ExpectRegion("sanity", L.SanityMeter, 348, 4, 280, 44);
            ExpectRegion("light", L.LightMeter, 636, 4, 220, 44);
            ExpectRegion("track", L.TrackMeter, 864, 4, 120, 44);
            ExpectRegion("clock", L.Clock, 1000, 4, 150, 44);
            ExpectRegion("pause", L.Pause, 1160, 8, 104, 36);
            ExpectRegion("records station", L.Station, 16, 100, 320, 262);
            ExpectRegion("id card", L.IdCard, 405, 436, 320, 220);
            ExpectRegion("window frame", L.Window, 405, 88, 470, 316);
            ExpectRegion("glass", L.Glass, 420, 98, 440, 296);
            ExpectRegion("portrait", L.Portrait, 565, 104, 150, 180);
            ExpectRegion("sill", L.Sill, 405, 404, 470, 24);
            ExpectRegion("speech", L.Speech, 16, 362, 320, 342);
            ExpectRegion("patrol button", L.PatrolBtn, 490, 672, 180, 36);
            ExpectRegion("end shift", L.EndShiftBtn, 680, 672, 200, 36);
            ExpectRegion("questions", L.Questions, 968, 88, 296, 260);
            ExpectRegion("scan id", L.UtilityScan, 968, 356, 140, 36);
            ExpectRegion("tonight's log", L.UtilityRecords, 1124, 356, 140, 36);
            ExpectRegion("map", L.UtilityMap, 968, 396, 140, 36);
            ExpectRegion("rules note", L.UtilityNotes, 1124, 396, 140, 36);
            ExpectRegion("deny stamp", L.StampDeny, 984, 440, 110, 110);
            ExpectRegion("allow stamp", L.StampAllow, 1148, 440, 110, 110);
            ExpectRegion("camera", L.Monitor, 968, 558, 296, 146);
            // Phase 9/10 grid extensions (queue strips + sticky note; the queue readout
            // is the banner's second line, inside the existing registered Banner region)
            ExpectRegion("queue strip L", L.QueueStripL, 340, 436, 60, 240);
            ExpectRegion("queue strip R", L.QueueStripR, 884, 428, 80, 244);
            ExpectRegion("sticky note", L.StickyNote, 390, 200, 500, 320);
            // question buttons: wireframe x=980, y=118, 272x38, step 42 (panel-local grid)
            Check(Math.Abs(L.Inner.QBtnX - (980 - 968)) < 0.01f && Math.Abs(L.Inner.QBtnY0 - (118 - 88)) < 0.01f &&
                  Math.Abs(L.Inner.QBtnStep - 42) < 0.01f && Math.Abs(L.Inner.QBtnW - 272) < 0.01f &&
                  Math.Abs(L.Inner.QBtnH - 38) < 0.01f,
                "question buttons == wireframe grid (980,118 + i*42, 272x38)");
            // hint/findings row: wireframe baseline y=72, findings right edge x=1264
            Check(Math.Abs(L.Findings.Right - 1264) < 0.01f, "findings right-aligned at x=1264");
            Check(Math.Abs(L.Hint.X - 20) < 0.01f, "hint starts at x=20");
            Check(L.Hint.Y >= 52 && L.Hint.Bottom <= 96 && L.Findings.Y >= 52 && L.Findings.Bottom <= 96,
                "hint/findings live in the row under the top bar (52..96), clear of the record sheet");

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
            Add("id card", L.IdCard, "C");
            Add("window", L.Window, "C");
            Add("sill", L.Sill, "C");
            Add("speech", L.Speech, "C");
            Add("patrol button", L.PatrolBtn, "C");
            Add("end shift button", L.EndShiftBtn, "C");
            Add("questions panel", L.Questions, "C");
            Add("scan button", L.UtilityScan, "C");
            Add("log button", L.UtilityRecords, "C");
            Add("map button", L.UtilityMap, "C");
            Add("rules button", L.UtilityNotes, "C");
            Add("deny stamp", L.StampDeny, "C");
            Add("allow stamp", L.StampAllow, "C");
            Add("camera monitor", L.Monitor, "C");
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

        // ------------------------------------------------------------- speech proof
        // The speech box must show BOTH sides: the visitor's line, the player's question
        // under "YOU", and the visitor's answer — newest at the bottom.

        // ------------------------------------------------- papers pass (drag, pips, hints)
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
            Console.WriteLine("== papers pass: chat column, draggable ID, question panel ==");
            var cp = root.Checkpoint;

            // chat lives bottom-left and speaks in three inks
            var speech = FindDeep(cp.transform, "speech_border");
            var srt = speech.GetComponent<RectTransform>();
            Check(Mathf.Abs(srt.anchoredPosition.x - L.Speech.X) < 0.5f &&
                  Mathf.Abs(-srt.anchoredPosition.y - L.Speech.Y) < 0.5f &&
                  L.Speech.X == 16 && L.Speech.Y == 362,
                  "the chat log sits in the bottom-left column (16,362)");
            var logText = FindDeep(cp.transform, "speechlog").GetChild(0).GetComponent<UnityEngine.UI.Text>().text;
            Check(logText.Contains("<color=#4a5a49>YOU</color>") && logText.Contains("<color=#6f6d68>AT THE WINDOW</color>"),
                  "chat lines carry per-speaker colours");

            // both papers are physical
            var idDrag = FindDeep(cp.transform, "idcard").GetComponent<DragPaper>();
            var recDrag = FindDeep(cp.transform, "record").GetComponent<DragPaper>();
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
            idDrag.DragBy(new Vector2(120f, -40f));        // a normal desk move
            idDrag.EndDrag();
            c = idDrag.CenterInBounds();
            Check(Mathf.Abs(c.x - (home.x + 120f)) < 0.5f && Mathf.Abs(c.y - (home.y + 40f)) < 0.5f,
                  "a paper dropped on the desk stays where the player put it");
            idDrag.BeginDrag();
            idDrag.PlaceCenter(home);                      // tidy back for the next checks
            idDrag.EndDrag();

            // dropping the ID on the scanner scans it — exactly one scan, same rules path
            int scans = 0;
            var sub = root.Bus.Subscribe(EventIds.ScanPerformed, e => scans++);
            idDrag.BeginDrag();
            idDrag.PlaceAt(new Vector2(L.UtilityScan.X + L.UtilityScan.W / 2f - 160f,
                                       L.UtilityScan.Y + L.UtilityScan.H / 2f - 110f));
            idDrag.EndDrag();
            Check(scans == 1, "drop-on-scanner triggers exactly one scan");
            idDrag.BeginDrag();
            idDrag.PlaceAt(new Vector2(600f - 160f, 500f - 110f));
            idDrag.EndDrag();
            Check(scans == 1, "dropping elsewhere is just repositioning (no scan)");
            sub.Dispose();

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
            var hint = FindDeep(cp.transform, "qhint").GetComponent<UnityEngine.UI.Text>();
            Check(hint.text == "No more questions — they're getting nervous.",
                  "budget-spent reason line replaces the hover hint");
            UnityEngine.Input.KeysDown.Add(UnityEngine.KeyCode.Alpha3);
            cp.TickHotkeys();
            UnityEngine.Input.KeysDown.Clear();
            Check(!FindDeep(cp.transform, "q2").GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("ASKED"),
                  "hotkeys respect the spent budget (no third question)");
            var testimonyHost = FindDeep(cp.transform, "testimony");
            Check(testimonyHost.GetComponentInChildren<UnityEngine.UI.ScrollRect>() != null,
                  "testimony scrolls instead of clipping");

            // records station: the file opens only when the player opens it
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "the student file stays shut until the player searches for it");
            cp.SearchRecords(cp.CurrentVisitor.IdNumber);
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "searching the claimed ID opens the matching file");
            cp.SearchRecords("HB-1999-0000");
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf,
                  "a ghost ID answers NO RECORD and shuts the window (a clue in itself)");
            cp.ToggleFileWindow();
            Check(FindDeep(cp.transform, "record").gameObject.activeSelf, "the station tap reopens the file");
            cp.ToggleFileWindow();
            Check(!FindDeep(cp.transform, "record").gameObject.activeSelf, "and closes it again");

            // the slip: counter -> desk, stamp target, one stamp, pending until hand-back
            var slipGo = FindDeep(cp.transform, "slip");
            var slipDrag = slipGo.GetComponent<DragPaper>();
            slipDrag.BeginDrag();
            slipDrag.PlaceCenter(new Vector2(760f, 500f));
            slipDrag.EndDrag();
            Check(cp.PendingVerdict == Verdict.None, "an unstamped slip commits nothing");
            DropStamp(root, Verdict.Reject);          // stamp dropped on the ID, not the slip
            Check(!cp.SlipStamped, "a stamp dropped off the slip returns to the rack (no mark)");
            StampSlip(root, Verdict.Reject);
            Check(cp.SlipStamped && cp.PendingVerdict == Verdict.Reject,
                  "the stamp lands on the slip and records a pending verdict");
            StampSlip(root, Verdict.Allow);
            Check(cp.PendingVerdict == Verdict.Reject, "one stamp per slip: the second returns to the rack");

            // flip + zoom on the held ID (desk is live here): tap retargets to the ID
            var idGo = FindDeep(cp.transform, "idcard");
            var idDrag2 = idGo.GetComponent<DragPaper>();
            idDrag2.BeginDrag(); idDrag2.EndDrag();      // a tap: flips the ID itself
            Check(idGo.Find("idback").gameObject.activeSelf, "tapping the held ID flips it to the back face");
            cp.FlipId();
            Check(!idGo.Find("idback").gameObject.activeSelf, "F flips back to the face");
            cp.DebugZoom(9f);
            Check(Mathf.Abs(idGo.localScale.x - 1.8f) < 0.01f, "zoom clamps at 1.8");
            cp.DebugZoom(0.1f);
            Check(Mathf.Abs(idGo.localScale.x - 1f) < 0.01f, "zoom clamps at 1.0");
            cp.DebugZoom(1f);
        }

        private static void SpeechProof(GameRoot root)
        {
            Console.WriteLine("== speech box carries both sides ==");
            var panel = FindDeep(root.Checkpoint.transform, "speech_border");
            Check(panel != null && panel.gameObject.activeSelf, "speech panel visible while a visitor is at the window");
            root.Checkpoint.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            root.Checkpoint.OpenInterview();
            root.Checkpoint.AskQuestion(QuestionId.Course);
            var host = FindDeep(root.Checkpoint.transform, "speechlog");
            Check(host != null && host.childCount == 1, "speech log exists");
            if (host == null || host.childCount != 1) return;
            string text = host.GetChild(0).GetComponent<UnityEngine.UI.Text>().text;
            Console.WriteLine("  ..speech log now: " + text.Replace("\n", " | "));
            Check(text.Contains("AT THE WINDOW"), "visitor side present (AT THE WINDOW)");
            Check(text.Contains("YOU"), "player side present (YOU)");
            Check(text.Contains("Your course?"), "the asked question is quoted in the log");
            int youIdx = text.IndexOf("YOU", StringComparison.Ordinal);
            int firstIdx = text.IndexOf("AT THE WINDOW", StringComparison.Ordinal);
            int lastIdx = text.LastIndexOf("AT THE WINDOW", StringComparison.Ordinal);
            Check(firstIdx < youIdx && youIdx < lastIdx, "order: visitor line, then YOU, then the answer (newest at bottom)");
        }

        // ------------------------------------------------- desk interaction loop
        // Button-level proof of the full visitor loop on the rebuilt desk: gating,
        // SCAN readout, CALL PRINCIPAL popup (No cancels / Yes stamps), PATROL guard,
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
            Func<string> monOut = () => FindDeep(desk, "out").GetChild(0).GetComponent<UnityEngine.UI.Text>().text;

            // earlier proofs may have left an inspection mid-way: close it like a player
            // would, then make sure a fresh visitor (documents NOT taken) is at the window
            if (FindDeep(desk, "idcard").gameObject.activeSelf && cp.CurrentVisitor != null)
                cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            int freshGuard = 0;
            while (cp.CurrentVisitor == null && freshGuard++ < 8) cp.AdvanceQueue(6f);
            Check(cp.CurrentVisitor != null && !FindDeep(desk, "idcard").gameObject.activeSelf,
                  "a fresh visitor waits with documents still untaken");
            Check(StampAlpha(root, "stamp_allow") < 0.5f, "the stamp tray rests dim before the ID is taken");
            int procBefore = root.Queue.ProcessedCount;
            DropStamp(root, Verdict.Reject);
            Check(root.Queue.ProcessedCount == procBefore && cp.CurrentVisitor != null,
                  "a stamp drop with no documents in hand is a no-op");
            Check(FindDeep(desk, "offeredid").gameObject.activeInHierarchy,
                  "the ID slides to the counter edge on arrival, unasked");
            var offerDrag = FindDeep(desk, "offeredid").GetComponent<DragPaper>();
            offerDrag.BeginDrag();
            offerDrag.PlaceCenter(new Vector2(600f, 520f));
            offerDrag.EndDrag();
            Check(FindDeep(desk, "idcard").gameObject.activeSelf,
                  "dragging the ID onto the desk takes it (drop where you let go)");
            Check(!live("patrol"), "PATROL HALLWAY dimmed mid-inspection");
            Check(!live("endshift"), "END SHIFT dimmed before the roster is done");

            Check(StampAlpha(root, "stamp_allow") > 0.9f, "stamps ink up once the ID is in hand");
            cp.DoScan();
            Check(monOut() != "SCANNER READY" && monOut().Length > 0,
                "SCAN ID writes the scanner readout (" + monOut().Split('\n')[0] + ")");

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
            Check(Silhouettes(root) == 0, "Q1: the line display is off for the showcase (no silhouettes)");
            Check(StatusText(root) == "", "Q1: the queue readout is hidden for the showcase");

            // ---- Q2 the oldest waiter steps up after the banner, with no second wait
            cp.DebugTakeDocuments();   // hand-over gate: take the ID like a click would
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null));
            Check(cp.CurrentVisitor == roster[1], "Q2: oldest waiting visitor stepped up immediately after the banner");
            Check(root.Queue.WaitingCount == 2 && Silhouettes(root) == 0, "Q2: the line queues invisibly (showcase)");
            Check(StatusText(root) == "", "Q2: the readout stays hidden ('" + StatusText(root) + "')");
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
            Check(StatusText(root) == "", "Q5: the readout stays hidden for the showcase");
            string speech = FindDeep(cp.transform, "speechlog").GetChild(0).GetComponent<UnityEngine.UI.Text>().text;
            Check(speech.Contains("Roll call"), "Q5: subtle radio feedback, no anomaly revealed");
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
            cp.OpenInterview();
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
            Check(root.Queue.WaitingCount == 1 && Silhouettes(root) == 0, "Q8: the waiting line was restored, not duplicated (silhouettes off)");
            Check(StatusText(root) == "", "Q8: the readout stays hidden after restore");
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
            Check(!StatusText(root).Contains("CURRENT:"), "Q9: no finite counter for infinite sources ('" + StatusText(root) + "')");
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

        private static int Silhouettes(GameRoot root)
        {
            int n = 0;
            for (int i = 0; i < 8; i++)
            {
                var fig = FindDeep(root.Checkpoint.transform, "qsil" + i);
                if (fig != null && fig.gameObject.activeSelf) n++;
            }
            return n;
        }

        private static string StatusText(GameRoot root)
        {
            // the readout label only exists when ShowcaseBuild.LineDisplay is on
            var go = FindDeep(root.Checkpoint.transform, "queuestatus");
            return go == null ? "" : go.GetComponent<UnityEngine.UI.Text>().text;
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

        private static void WorldLoop(GameRoot root)
        {
            Console.WriteLine("== PATROL world loop (flashlight + roaming anomaly), headless frames ==");

            // a fresh normal run, then patrol — through the real scene handoffs the game uses
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

            // ---- W1: input arbitration (F/Q/L toggles + mouse holds) through the real rig
            Console.WriteLine("== W1 flashlight input arbitration ==");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep, "W1: F latches Sweep");
            PressKey(root, KeyCode.Q);
            Check(rig.Beam == LightMode.Focus && !rig.Latches.Sweep && rig.Latches.Focus,
                "W1: Q takes over — the keyboard toggles are mutually exclusive");
            PressKey(root, KeyCode.F);
            Check(rig.Beam == LightMode.Sweep && rig.Latches.Sweep && !rig.Latches.Focus,
                "W1: F while Focus-latched switches back to Sweep (no dead toggle)");
            PressKey(root, KeyCode.L);
            Check(rig.Beam == LightMode.Off && !rig.Latches.Sweep && !rig.Latches.Focus,
                "W1: L switches off and clears conflicting latches");
            PressKey(root, KeyCode.F);
            Input.MouseHeld.Add(1);
            WorldFrame(root);
            Check(rig.Beam == LightMode.Focus && rig.Latches.Sweep, "W1: held RMB temporarily overrides the latch without changing it");
            Input.MouseHeld.Remove(1);
            WorldFrame(root);
            Check(rig.Beam == LightMode.Sweep, "W1: releasing the mouse returns to the latched mode");
            Input.MouseHeld.Add(0);
            WorldFrame(root);
            Check(rig.Beam == LightMode.Sweep, "W1: held LMB is a temporary Sweep");
            Input.MouseHeld.Remove(0);
            PressKey(root, KeyCode.L);
            Check(rig.Beam == LightMode.Off, "W1: back to Off");

            // ---- W2: room lighting and the beam are independent systems
            Console.WriteLine("== W2 room lights vs handheld beam ==");
            var sw = FindInteractable<LightSwitchObject>(hall);
            Check(sw != null, "W2: the hallway has a wall switch");
            Check(hall.Light != null && hall.Light.Lit, "W2: hallway lights start on");
            PressKey(root, KeyCode.Q);
            Check(hall.Light.Lit && rig.Beam == LightMode.Focus, "W2: switching the beam to Focus does NOT touch the room lights");
            if (sw != null) sw.Interact();
            Check(!hall.Light.Lit && rig.Beam == LightMode.Focus, "W2: the wall switch darkens the room, beam stays Focus");
            if (sw != null) sw.Interact();
            Check(hall.Light.Lit, "W2: and lights it again");
            PressKey(root, KeyCode.L);
            Check(rig.Beam == LightMode.Off && hall.Light.Lit, "W2: beam Off leaves no stale state on the room light");

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
                AimAt(0f, 4f); WorldFrame(root);
                Check(Mathf.Abs(cone.localEulerAngles.z - 90f) < 0.5f, $"W3: aiming up rotates the cone to ~90° (got {cone.localEulerAngles.z:F1})");
                AimAt(-5f, -0.5f); WorldFrame(root);
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
            Check(rig.Beam == LightMode.Off, "W4: held mouse buttons cannot relight a dead battery");
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
                SetBeam(root, rig, LightMode.Off);
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
