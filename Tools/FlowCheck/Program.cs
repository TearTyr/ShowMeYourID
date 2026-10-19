using System;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
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

            // what Unity does after SceneManager.LoadScene: the new scene's bootstrap Awake
            CheckpointSceneBootstrap.Build(root);

            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++)
                root.Dialogue.Advance();

            Check(root.State == AppViewState.Checkpoint, "state is Checkpoint after the intro");
            Check(root.Checkpoint != null && root.Checkpoint.CurrentVisitor != null, "a visitor is at the window");

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
            SaveContinueResume();
            DeskLoop(root);

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
                "record", "idcard", "checklist_border",
                "windowframe", "glass", "facade", "visitor", "glitch", "sill",
                "speech_border", "speechlog", "principalnote_border", "notebody", "patrol", "endshift",
                "qpanel_border", "q0", "q1", "q2", "q3", "testimony",
                "scan", "records", "map", "notes", "reject", "allow", "monitor_border", "out",
                "confirmbox_border", "Banner", "OverlayRules", "OverlayLog",
            };
            foreach (var n in deskNames)
                Check(FindDeep(root.Checkpoint.transform, n) != null, "desk element '" + n + "'");

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
            ExpectRegion("record sheet", L.Record, 16, 64, 320, 290);
            ExpectRegion("id card", L.IdCard, 16, 362, 320, 220);
            ExpectRegion("checklist", L.Checklist, 16, 590, 320, 122);
            ExpectRegion("window frame", L.Window, 405, 88, 470, 316);
            ExpectRegion("glass", L.Glass, 420, 98, 440, 296);
            ExpectRegion("portrait", L.Portrait, 565, 104, 150, 180);
            ExpectRegion("sill", L.Sill, 405, 404, 470, 24);
            ExpectRegion("speech", L.Speech, 490, 436, 300, 110);
            ExpectRegion("principal note", L.Note, 500, 554, 280, 110);
            ExpectRegion("patrol button", L.PatrolBtn, 490, 672, 180, 36);
            ExpectRegion("end shift", L.EndShiftBtn, 680, 672, 200, 36);
            ExpectRegion("questions", L.Questions, 968, 88, 296, 260);
            ExpectRegion("scan id", L.UtilityScan, 968, 356, 140, 36);
            ExpectRegion("tonight's log", L.UtilityRecords, 1124, 356, 140, 36);
            ExpectRegion("map", L.UtilityMap, 968, 396, 140, 36);
            ExpectRegion("rules note", L.UtilityNotes, 1124, 396, 140, 36);
            ExpectRegion("call principal", L.StampReject, 984, 440, 110, 110);
            ExpectRegion("allow", L.StampAllow, 1148, 440, 110, 110);
            ExpectRegion("camera", L.Monitor, 968, 558, 296, 146);
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
            Add("record sheet", L.Record, "C");
            Add("id card", L.IdCard, "C");
            Add("checklist", L.Checklist, "C");
            Add("window", L.Window, "C");
            Add("sill", L.Sill, "C");
            Add("speech", L.Speech, "C");
            Add("principal note", L.Note, "C");
            Add("patrol button", L.PatrolBtn, "C");
            Add("end shift button", L.EndShiftBtn, "C");
            Add("questions panel", L.Questions, "C");
            Add("scan button", L.UtilityScan, "C");
            Add("log button", L.UtilityRecords, "C");
            Add("map button", L.UtilityMap, "C");
            Add("rules button", L.UtilityNotes, "C");
            Add("call principal", L.StampReject, "C");
            Add("allow", L.StampAllow, "C");
            Add("camera monitor", L.Monitor, "C");
            Add("toast", L.Toast, "T");
            Add("confirm box", L.ConfirmBox, "T");
            Add("verdict banner", L.VerdictBanner, "T");
            Add("paper overlay", L.PaperOverlay, "T");

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
        private static void SpeechProof(GameRoot root)
        {
            Console.WriteLine("== speech box carries both sides ==");
            var panel = FindDeep(root.Checkpoint.transform, "speech_border");
            Check(panel != null && panel.gameObject.activeSelf, "speech panel visible while a visitor is at the window");
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

            Check(live("allow") && live("reject"), "ALLOW / CALL PRINCIPAL live while a visitor is at the window");
            Check(!live("patrol"), "PATROL HALLWAY dimmed mid-inspection");
            Check(!live("endshift"), "END SHIFT dimmed before the roster is done");

            cp.DoScan();
            Check(monOut() != "SCANNER READY" && monOut().Length > 0,
                "SCAN ID writes the scanner readout (" + monOut().Split('\n')[0] + ")");

            var confirm = FindDeep(desk, "confirmbox_border");
            btn("reject").onClick.Invoke();
            Check(confirm.gameObject.activeSelf, "CALL PRINCIPAL opens the confirmation popup");
            FindDeep(confirm, "no").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!confirm.gameObject.activeSelf, "popup 'No' closes it without stamping");
            Check(live("allow"), "ALLOW still live after cancelling the popup");

            cp.GoPatrol();
            Check(root.State == AppViewState.Checkpoint, "PATROL refused mid-inspection (state stays Checkpoint)");

            string beforeYes = cp.CurrentVisitor != null ? cp.CurrentVisitor.ClaimedName : "?";
            btn("reject").onClick.Invoke();
            FindDeep(confirm, "yes").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!confirm.gameObject.activeSelf, "popup 'Yes' closes and stamps");
            // the stub runs the banner coroutine synchronously, so by the time the click
            // returns the loop has already moved on: the stamped visitor is the proof
            Check(cp.CurrentVisitor == null || cp.CurrentVisitor.ClaimedName != beforeYes,
                "verdict registered: " + beforeYes + " was processed and the loop moved on");
            Check(cp.CurrentVisitor != null, "next visitor arrives after the verdict banner");

            int guard = 0;
            while (!live("endshift") && guard++ < 12)
            {
                if (cp.CurrentVisitor == null) break;
                if (InspectionRules.ExpectedVerdict(cp.CurrentVisitor, null) == Verdict.Allow)
                    btn("allow").onClick.Invoke();          // exercises the ALLOW click path
                else
                    cp.Stamp(Verdict.Reject);
            }
            Check(guard < 12, "roster walked to the end without runaway (" + guard + " verdicts)");
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
            root.Checkpoint.Stamp(InspectionRules.ExpectedVerdict(root.Checkpoint.CurrentVisitor, null));
            root.PersistRun();
            root.QuitToMenu(true);
            root.ContinueGame();
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            string resumed = root.Checkpoint.CurrentVisitor != null ? root.Checkpoint.CurrentVisitor.ClaimedName : "?";
            Console.WriteLine("  first visitor before quit: " + first + " | first visitor after CONTINUE: " + resumed);
            Check(first != resumed, "B8: CONTINUE resumes at the next visitor, not the same one");
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
    }
}
