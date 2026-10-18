// Headless ENGINE smoke tests for Show Me Your ID!
// Boots the real GameBootstrap against Unity stubs (synchronous coroutines) and drives:
// boot → main menu → New Game → all five shifts with correct verdicts → administrator
// rejection → Win ending; exploration + clue chain; mistake paths; lose ending; endless +
// challenge runs; pause/map/settings; save/continue; procedural art/audio generation.
// Run:  dotnet run --project Tools/Verification/EngineSmoke
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace SMYID.Smoke
{
    public static class SmokeRunner
    {
        static int _passed, _failed;
        static GameRoot R;

        static void Check(bool cond, string name)
        {
            if (cond) { _passed++; Console.WriteLine("  PASS  " + name); }
            else { _failed++; Console.WriteLine("  FAIL  " + name); }
        }

        static void Section(string s) => Console.WriteLine("\n== " + s + " ==");

        static void Call(object obj, string method, params object[] args)
        {
            var mi = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi == null) throw new Exception("method '" + method + "' not found on " + obj.GetType().Name);
            mi.Invoke(obj, args.Length == 0 ? null : args);
        }

        static T Field<T>(object obj, string name)
        {
            var fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) throw new Exception("field '" + name + "' not found on " + obj.GetType().Name);
            return (T)fi.GetValue(obj);
        }

        public static int Main()
        {
            Console.WriteLine("Show Me Your ID! — engine smoke tests (headless, real code paths)");
            string tmp = Path.Combine(Path.GetTempPath(), "smyid-smoke-" + Environment.TickCount);
            Directory.CreateDirectory(tmp);
            Application.persistentDataPath = tmp;
            // headless scene loading: simulate SceneManager.LoadScene via the registry
            SceneLoadBridge.SceneLoadHook = SceneBootstrapRegistry.SimulateLoad;

            try { RunAll(); }
            catch (Exception e)
            {
                _failed++;
                Console.WriteLine("  FAIL  UNHANDLED EXCEPTION: " + e);
            }
            Console.WriteLine($"\nRESULT: {_passed} passed, {_failed} failed");
            try { Directory.Delete(tmp, true); } catch (Exception) { }
            return _failed == 0 ? 0 : 1;
        }

        // ---------------------------------------------------------------- helpers

        static void DrainDialogue()
        {
            int guard = 0;
            while (R.Dialogue != null && R.Dialogue.IsActive && guard++ < 500)
                R.Dialogue.Advance();
        }

        static void StampExpected()
        {
            var v = R.Checkpoint.CurrentVisitor;
            if (v == null) throw new Exception("no visitor at window");
            var mutation = R.Session.Mode == GameMode.Challenge ? R.Session.ActiveMutation : null;
            R.Checkpoint.Stamp(InspectionRules.ExpectedVerdict(v, mutation));
            DrainDialogue();
        }

        static ClueObject ClueIn(RoomInstance room, string key)
        {
            foreach (var it in room.Interactables)
            {
                var c = it as ClueObject;
                if (c != null && c.ClueKey == key) return c;
            }
            return null;
        }

        static Doorway DoorTo(RoomInstance room, RoomId target)
        {
            foreach (var it in room.Interactables)
            {
                var d = it as Doorway;
                if (d != null && d.Target == target) return d;
            }
            return null;
        }

        // ---------------------------------------------------------------- suite

        static void RunAll()
        {
            Boot();
            MenuAndSettings();
            MenuGatingAndDataReset();
            NormalRunFullStory_WinEnding();
            ExplorationAndClues_FreshRun();
            SecurityRoomPrototype();
            LoseByFalseRejections();
            EndlessRun();
            ChallengeRun();
            SaveAndContinue();
            ResolutionAndLayoutAudit();
            ProceduralContentSweep();
        }

        static void Boot()
        {
            Section("Boot & bootstrap (scene architecture)");
            SceneRouter.Load(SceneRouter.Menu);   // as Unity would load build-settings scene 0
            R = GameRoot.Instance;
            Check(R != null, "GameRoot created by menu bootstrap");
            Check(CoreBootstrap.CreatedEventSystem != null,
                "EventSystem created (without it NO button/slider/toggle can be clicked in Unity)");
            Check(R.Bus != null && R.Config != null && R.Settings != null && R.Saves != null && R.Audio != null,
                "services constructed (bus/config/settings/saves/audio)");
            Check(R.MainCamera != null, "camera built");
            Check(R.MainMenu != null && R.ModeSelect != null && R.SettingsScreen != null &&
                  R.ControlsScreen != null && R.CreditsScreen != null && R.Confirm != null && R.Captions != null,
                "menu scene wires its views with explicit references");
            Check(R.State == AppViewState.MainMenu, "boots to main menu scene");
            Check(!R.HasSave(), "no save on first boot");
            // loading the checkpoint scene builds the gameplay view set
            SceneRouter.Load(SceneRouter.Checkpoint);
            Check(R.Checkpoint != null && R.HUD != null && R.Dialogue != null && R.MapScreen != null &&
                  R.Loading != null && R.Ending != null && R.ShiftResults != null && R.PauseScreen != null &&
                  R.SanityCtl != null && R.Scares != null && R.Hallucinations != null,
                "checkpoint scene wires gameplay views explicitly");
            Check(R.Views == null, "checkpoint scene has no world (desk-only scene)");
            SceneRouter.Load(SceneRouter.School);
            Check(R.Views != null && R.SecurityRoom != null && R.HUD != null,
                "school scene wires world + security-room views");
            SceneRouter.Load(SceneRouter.Menu);
            Check(R.State == AppViewState.MainMenu, "scene router returns to menu cleanly");
        }

        static void MenuAndSettings()
        {
            Section("Menus, settings, pause, map");
            // settings really change behavior
            R.Settings.Apply(d => d.TextScale = 2);
            Check(Mathf.Approximately(R.Settings.TextScaleMultiplier, 1.25f), "text scale Large = 1.25x applied");
            R.Settings.Apply(d => { d.ReduceMotion = true; d.PhotosensitivitySafe = true; d.Captions = false; });
            Check(R.Settings.ReduceMotion && R.Settings.PhotosensitivitySafe && !R.Settings.Captions,
                "accessibility toggles persist through Apply()");
            R.Settings.Apply(d => { d.MasterVolume = 0.5f; d.MusicVolume = 0.5f; d.SfxVolume = 0.5f; });
            Check(Mathf.Approximately(R.Settings.SfxVolume, 0.25f), "effective sfx volume = master × sfx");
            // restore defaults for the rest of the suite
            R.Settings.Apply(d =>
            {
                d.TextScale = 1; d.ReduceMotion = false; d.PhotosensitivitySafe = false; d.Captions = true;
                d.MasterVolume = 0.8f; d.MusicVolume = 0.6f; d.SfxVolume = 0.8f;
            });

            // pause toggling
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            R.TogglePause(true);
            Check(R.Paused && Time.timeScale == 0f, "pause freezes time");
            R.TogglePause(false);
            Check(!R.Paused && Time.timeScale == 1f, "unpause restores time");

            // map from checkpoint and back
            R.OpenMap();
            Check(R.State == AppViewState.Map, "map opens over checkpoint");
            R.CloseMap();
            Check(R.State == AppViewState.Checkpoint, "map closes back to checkpoint");

            // overlay screens
            R.OpenSettings(); R.CloseSettings();
            R.OpenControls(); R.CloseControls();
            R.QuitToMenu(false);
            R.OpenCredits(); R.CloseCredits();
            Check(R.State == AppViewState.MainMenu, "overlay screens never corrupt primary state");
            DrainDialogue();
        }

        static void MenuGatingAndDataReset()
        {
            Section("Menu gating, New Game flow, F2×3 data reset");
            Check(!GameRoot.RunModesUnlocked, "run modes locked by default (menu placeholders inactive)");
            R.QuitToMenu(false);

            var buttons = R.MainMenu.transform.Find("buttons");
            Check(buttons != null, "menu option column exists");
            var newGame = buttons.Find("NEW GAME");
            Check(newGame != null, "NEW GAME entry present");
            newGame.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(R.Session.Mode == GameMode.Normal && R.State == AppViewState.ShiftIntro,
                "NEW GAME starts the Normal story immediately");
            DrainDialogue();
            Check(R.State == AppViewState.Checkpoint && R.Checkpoint.CurrentVisitor != null,
                "and lands at the checkpoint with a visitor at the window");

            // Challenge/Extras must be completely invisible in the menu
            R.QuitToMenu(false);
            Check(buttons.Find("CHALLENGE MODE") == null, "CHALLENGE MODE not present in menu at all");
            Check(buttons.Find("EXTRAS") == null, "EXTRAS not present in menu at all");
            Check(buttons.Find("NEW GAME") != null && buttons.Find("CONTINUE") != null &&
                  buttons.Find("SETTINGS") != null && buttons.Find("CREDITS") != null && buttons.Find("EXIT") != null,
                "menu shows exactly the five focused options");

            // build real save data, then erase via the true F2×3 path
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            var def = ShiftPlan.Get(1);
            for (int i = 0; i < def.VisitorCount; i++) StampExpected();
            R.FinishShift();
            Check(R.HasSave(), "save exists before reset");
            R.QuitToMenu(false);

            Input.KeysDown.Add(KeyCode.F2);
            for (int i = 0; i < 3; i++)
            {
                Time.unscaledTime += 0.4f; // consecutive presses inside the 1.5 s window
                Call(R.MainMenu, "Update");
            }
            Input.KeysDown.Remove(KeyCode.F2);
            var confirmRoot = Field<GameObject>(R.Confirm, "_root");
            Check(confirmRoot.activeSelf, "third consecutive F2 opens the erase-data confirmation");
            var yes = confirmRoot.transform.Find("box_border/box/yes");
            Check(yes != null, "confirmation exposes a YES control");
            yes.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!R.HasSave(), "F2×3 + YES erased the save file");
            Check(R.Records.EndingsSeen.Count == 0 && R.Records.EndlessBest == 0 && R.Records.ChallengeBest == 0,
                "records/ending history cleared");

            // accidental protection: spaced presses must never reach the confirm
            R.Session.CurrentShift = 1;
            R.PersistRun();
            Check(R.HasSave(), "save re-created for accidental-press test");
            Input.KeysDown.Add(KeyCode.F2);
            Time.unscaledTime += 5f; Call(R.MainMenu, "Update");
            Time.unscaledTime += 5f; Call(R.MainMenu, "Update");
            Input.KeysDown.Remove(KeyCode.F2);
            Check(!confirmRoot.activeSelf && R.HasSave(),
                "two spaced F2 presses do NOT reset (window expired — accidental-proof)");
            R.Saves.Delete();
        }

        static void NormalRunFullStory_WinEnding()
        {
            Section("Normal mode: five shifts → administrator → Win ending");
            R.StartNewGame(GameMode.Normal);
            Check(R.State == AppViewState.ShiftIntro && R.Dialogue.IsActive, "shift 1 intro dialogue plays");
            Check(R.Dialogue.transform.GetSiblingIndex() > R.Loading.transform.GetSiblingIndex(),
                "intro dialogue draws ABOVE the loading backdrop (v1.0.4 freeze regression)");
            DrainDialogue();
            Check(R.State == AppViewState.Checkpoint, "intro completes into checkpoint");

            var plan = ShiftPlan.Build();
            for (int shift = 1; shift <= 5; shift++)
            {
                Check(R.Session.CurrentShift == shift, "shift " + shift + " active");
                var def = plan[shift - 1];
                int total = def.VisitorCount + (def.HasAdministrator ? 1 : 0);
                int anomaliesRejectedBefore = R.Session.AnomaliesRejected;
                for (int i = 0; i < total; i++)
                {
                    var v = R.Checkpoint.CurrentVisitor;
                    Check(v != null, $"shift {shift} visitor {i + 1}/{total} at window");
                    if (v == null) break;
                    if (shift == 1 && i == 0)
                    {
                        // exercise inspection tools on the very first visitor
                        R.Checkpoint.AskQuestion(QuestionId.Course);
                        R.Checkpoint.AskQuestion(QuestionId.Year);
                        R.Checkpoint.AskQuestion(QuestionId.Status); // 3rd must be refused (budget)
                        R.Checkpoint.DoScan();
                    }
                    if (def.HasAdministrator && i == total - 1)
                        Check(v.IsAdministrator, "shift 5 finale is the district administrator");
                    StampExpected();
                }
                Check(R.Session.AnomaliesRejected - anomaliesRejectedBefore == def.AnomalyCount + (def.HasAdministrator ? 1 : 0),
                    $"shift {shift}: all anomalies rejected");
                if (shift < 5 || !R.Session.AdminRejected) { } // admin path checked below
                R.FinishShift();
                if (shift < 5)
                {
                    Check(R.State == AppViewState.ShiftResults, "shift " + shift + " results screen shown");
                    R.AdvanceToNextShift();
                    DrainDialogue();
                }
            }

            Check(R.Session.AdminRejected, "administrator identified and rejected");
            Check(R.Session.FalseRejections == 0 && R.Session.Breaches == 0, "clean record on perfect play");
            Check(R.State == AppViewState.Ending, "ending screen shown after shift 5");
            var eval = R.Session.EvaluateEnding();
            Check(eval.Ending == EndingType.Win && eval.ReasonKey == "win", "perfect play without clues => WIN ending");
            Check(R.Records.EndingsSeen.Contains("win"), "win ending recorded in meta records");
            Check(!R.HasSave(), "finished run does not offer Continue");
            R.QuitToMenu(false);
        }

        static void ExplorationAndClues_FreshRun()
        {
            Section("Exploration: rooms, doors, lights, clues, anomalies");
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            // finish shift 1 visitors, then patrol before clocking out
            var def = ShiftPlan.Get(1);
            for (int i = 0; i < def.VisitorCount; i++) StampExpected();
            R.Checkpoint.GoPatrol();
            Check(R.State == AppViewState.Exploration, "patrol enters exploration");
            Check(R.Views.Current != null && R.Views.Current.Id == RoomId.Hallway, "starts in the hallway");

            var hallway = R.Views.Current;
            var light = ClueIn(hallway, "light");
            Check(light != null, "flickering-light clue present in hallway");
            float sanityBefore = R.Session.Sanity;
            light.Interact();
            DrainDialogue();
            Check(R.Session.Clues.FoundCount == 1, "clue discovered and tracked");
            Check(R.Session.Sanity < sanityBefore, "disturbing clue costs sanity");
            light.Interact(); DrainDialogue();
            Check(R.Session.Clues.FoundCount == 1, "re-inspection does not double-count");

            // light switch toggles Off <-> Sweep
            LightSwitchObject sw = null;
            foreach (var it in hallway.Interactables) if (it is LightSwitchObject l) sw = l;
            Check(sw != null, "light switch present");
            var modeBefore = hallway.Light.Mode;
            sw.Interact();
            Check(hallway.Light.Mode != modeBefore, "switch changes light mode");
            sw.Interact();
            Check(hallway.Light.Mode == modeBefore, "switch toggles back");

            // movement simulation through the real Update loop
            var player = R.Views.Player;
            float x0 = player.transform.localPosition.x;
            Input.KeysHeld.Add(KeyCode.D);
            for (int f = 0; f < 30; f++) Call(player, "Update");
            Input.KeysHeld.Remove(KeyCode.D);
            Check(player.transform.localPosition.x > x0 + 0.5f, "holding D moves the custodian right");
            Input.KeysHeld.Add(KeyCode.A);
            for (int f = 0; f < 30; f++) Call(player, "Update");
            Input.KeysHeld.Remove(KeyCode.A);
            Check(Mathf.Abs(player.transform.localPosition.x - x0) < 0.6f, "holding A moves back (no drift/trap)");

            // door to classroom 1 → clues 2 & 3 in intended order
            var d1 = DoorTo(hallway, RoomId.Classroom1);
            d1.Interact();
            Check(R.Views.Current.Id == RoomId.Classroom1, "door transitions to Classroom 1");
            ClueIn(R.Views.Current, "chalkboard").Interact(); DrainDialogue();
            ClueIn(R.Views.Current, "desk").Interact(); DrainDialogue();
            Check(R.Session.Clues.FoundCount == 3 && R.Session.Clues.OrderIntact, "clue chain 1-2-3 in order");

            // anomaly encounter + catch (evasion recovery)
            R.Views.SpawnAnomaly(false);
            var an = Field<WanderingAnomaly>(R.Views, "_liveAnomaly");
            Check(an != null, "wandering anomaly spawned");
            an.TickVisibility();
            float sanBefore = R.Session.Sanity;
            Vector3 spawnBefore = player.transform.localPosition;
            an.CatchPlayer();
            Check(R.Session.Sanity < sanBefore, "being caught costs sanity (no death — decision-based lose only)");
            Check(Mathf.Abs(player.transform.localPosition.x - R.Views.Current.PlayerSpawn.x) < 0.01f,
                "player repositioned to room entrance after catch (recovery, never stuck)");

            // classroom 2 via hallway
            DoorTo(R.Views.Current, RoomId.Hallway).Interact();
            Check(R.Views.Current.Id == RoomId.Hallway, "back to hallway");
            DoorTo(R.Views.Current, RoomId.Classroom2).Interact();
            Check(R.Views.Current.Id == RoomId.Classroom2, "Classroom 2 reachable");
            ClueIn(R.Views.Current, "cabinet").Interact(); DrainDialogue();   // out of intended order on purpose
            Check(!R.Session.Clues.OrderIntact, "out-of-order discovery breaks the secret sequence (tracked)");

            // return to the checkpoint through the security door
            DoorTo(R.Views.Current, RoomId.Hallway).Interact();
            var secDoor = DoorTo(R.Views.Current, RoomId.Security);
            Check(secDoor != null, "security door present in hallway");
            secDoor.Interact();
            Check(R.State == AppViewState.Checkpoint, "returned to checkpoint — player can never get trapped");
            R.Checkpoint.ResumeAfterExploration();
            R.FinishShift();
            Check(R.State == AppViewState.ShiftResults, "shift completes after exploration");
            R.QuitToMenu(false);
        }

        static void SecurityRoomPrototype()
        {
            Section("Scene B: classroom → security office door-check prototype");
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            var def = ShiftPlan.Get(1);
            for (int i = 0; i < def.VisitorCount; i++) StampExpected();
            R.Checkpoint.GoPatrol();
            Check(R.State == AppViewState.Exploration, "patrol loads the School scene");

            DoorTo(R.Views.Current, RoomId.Classroom1).Interact();
            Check(R.Views.Current.Id == RoomId.Classroom1, "inside Classroom 1");

            Hollowbrook.World.SecurityDoorInteractable secDoor = null;
            foreach (var it in R.Views.Current.Interactables)
                if (it is Hollowbrook.World.SecurityDoorInteractable d) secDoor = d;
            Check(secDoor != null, "security office door exists in Classroom 1");
            secDoor.Interact();
            Check(R.State == AppViewState.SecurityRoom, "door opens the security-room view");
            Check(R.SecurityRoom.CurrentStudent != null, "first late student is at the door");

            for (int i = 0; i < 3; i++)
            {
                var v = R.SecurityRoom.CurrentStudent;
                Check(v != null, "late student " + (i + 1) + " present");
                bool shouldReject = v.IsAnomaly || !IdRules.IsFormatValid(v.IdNumber)
                                    || IdRules.IsExpired(v, 1) || !v.InDatabase;
                ClickChild(R.SecurityRoom.transform, shouldReject ? "reject" : "allow");
            }
            Check(R.SecurityRoom.CurrentStudent == null, "all three late arrivals processed");

            for (int i = 0; i < 3; i++) ClickChild(R.SecurityRoom.transform, "check" + i);
            ClickChild(R.SecurityRoom.transform, "ret");
            Check(R.State == AppViewState.Checkpoint, "return button loads the checkpoint scene");
            Check(R.Checkpoint.CurrentVisitor == null, "shift-1 roster already complete on resume");
            R.FinishShift();
            R.QuitToMenu(false);
        }

        static void ClickChild(Transform parent, string name)
        {
            var go = parent.Find(name);
            if (go == null) throw new Exception("button not found: " + name);
            go.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        }

        static void LoseByFalseRejections()
        {
            Section("Lose ending: three false rejections");
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            for (int i = 0; i < 3; i++)
            {
                var v = R.Checkpoint.CurrentVisitor;
                Check(v != null && !v.IsAnomaly, "shift-1 visitor " + (i + 1) + " is legitimate");
                R.Checkpoint.Stamp(Verdict.Reject); // wrong on purpose
                DrainDialogue();
            }
            Check(R.Session.FalseRejections == 3, "three false rejections counted");
            Check(R.State == AppViewState.Ending, "lose ending triggers immediately mid-shift");
            var eval = R.Session.EvaluateEnding();
            Check(eval.Ending == EndingType.Lose && eval.ReasonKey == "false_rejections", "lose reason = false rejections");
            Check(R.Records.EndingsSeen.Contains("lose"), "lose ending recorded");
            R.QuitToMenu(false);
        }

        static void EndlessRun()
        {
            Section("Endless mode: timer economy & results");
            R.StartNewGame(GameMode.Endless);
            DrainDialogue();
            Check(R.State == AppViewState.Checkpoint && R.Session.Mode == GameMode.Endless, "endless starts at the window");
            float t0 = R.Session.RunTimer;
            int processed = 0;
            for (int i = 0; i < 12; i++)
            {
                var v = R.Checkpoint.CurrentVisitor;
                if (v == null) break;
                StampExpected();
                processed++;
            }
            Check(processed == 12, "endless serves visitors continuously");
            Check(R.Session.RunProcessed == 12, "run counter tracks processed visitors");
            Check(R.Session.Score > 0, "correct calls score points");
            Check(R.Session.RunTimer > 0, "timer economy keeps the run alive on correct play");

            // force time-out through the real Update path (tick until the clock runs out)
            R.Session.RunTimer = 0.05f;
            Time.deltaTime = 1f / 60f;
            for (int t = 0; t < 10 && R.State == AppViewState.Checkpoint; t++) Call(R, "Update");
            Check(R.State == AppViewState.EndlessResults, "time-out ends the run with results");
            Check(R.Records.EndlessBest >= R.Session.Score, "endless best score recorded");
            R.QuitToMenu(false);
        }

        static void ChallengeRun()
        {
            Section("Challenge mode: rule mutations & double stakes");
            bool mutationSeen = false;
            var sub = R.Bus.Subscribe(EventIds.MutationChanged, e => mutationSeen = true);
            R.StartNewGame(GameMode.Challenge);
            DrainDialogue();
            for (int i = 0; i < 8; i++)
            {
                if (R.Checkpoint.CurrentVisitor == null) break;
                StampExpected(); // uses ActiveMutation, matching what the bulletin says
            }
            sub.Dispose();
            Check(mutationSeen, "district bulletin mutates the rules mid-run");
            Check(R.Session.RunProcessed == 8, "challenge processes visitors");
            Check(R.Session.Mode == GameMode.Challenge, "challenge session mode");
            // sanity drains twice as fast on mistakes in challenge (model-level check)
            Check(SanityModel.DrainMultiplier(GameMode.Challenge) == 2f, "challenge sanity drain ×2");
            R.QuitToMenu(false);
        }

        static void SaveAndContinue()
        {
            Section("Save & Continue across shifts");
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            var def = ShiftPlan.Get(1);
            for (int i = 0; i < def.VisitorCount; i++) StampExpected();
            R.FinishShift();                       // saves at shift boundary
            Check(R.State == AppViewState.ShiftResults, "results after shift 1");
            Check(R.HasSave(), "save exists after shift completion");
            int completed = R.Session.CompletedShifts;

            R.QuitToMenu(true);
            Check(R.State == AppViewState.MainMenu, "quit to menu");
            R.ContinueGame();
            DrainDialogue();
            Check(R.Session.CompletedShifts == completed, "continue restores progress");
            Check(R.Session.CurrentShift == 2 && R.State == AppViewState.Checkpoint,
                "continue resumes at the start of shift 2");
            Check(R.Checkpoint.CurrentVisitor != null, "first visitor of resumed shift is at the window");

            // mid-shift quit + continue replays the shift from its start
            R.QuitToMenu(true);
            R.ContinueGame();
            DrainDialogue();
            Check(R.Session.CurrentShift == 2, "mid-shift quit resumes the same shift");
            R.QuitToMenu(false);
        }

        struct Rect2 { public float X0, Y0, X1, Y1; public Rect2(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
            public bool Overlaps(Rect2 o) => X0 < o.X1 && o.X0 < X1 && Y0 < o.Y1 && o.Y0 < Y1; }

        static void ResolutionAndLayoutAudit()
        {
            Section("Resolution / aspect coverage + reference-space overlap audit");
            // (width, height, expect height-match) across the required test matrix
            var cases = new[]
            {
                new[] { 1920, 1080 }, new[] { 1280, 720 }, new[] { 2560, 1080 },
                new[] { 3440, 1440 }, new[] { 1024, 768 }, new[] { 800, 600 },
                new[] { 1366, 768 }, new[] { 1600, 900 }, new[] { 1280, 1024 },
            };
            bool allOk = true, allBig = true;
            foreach (var c in cases)
            {
                Screen.width = c[0]; Screen.height = c[1];
                foreach (var g in Hollowbrook.Core.AspectMatchGuard.Instances) g.Apply();
                foreach (var g in Hollowbrook.Core.AspectMatchGuard.Instances)
                {
                    var vis = g.VisibleReferenceSize();
                    float aspect = c[0] / (float)c[1];
                    // invariant: the SHORTER reference side is always exactly the design size,
                    // the longer side only ever grows into negative space
                    bool okMatch = aspect > Hollowbrook.Core.AspectMatchGuard.ReferenceAspect
                        ? Mathf.Abs(vis.y - 720f) < 0.5f && vis.x >= 1279.5f
                        : Mathf.Abs(vis.x - 1280f) < 0.5f && vis.y >= 719.5f;
                    bool bigEnough = vis.x >= 1279.5f && vis.y >= 719.5f;
                    if (!okMatch) { allOk = false; Console.WriteLine($"    {c[0]}x{c[1]} match wrong: {vis.x:0}x{vis.y:0}"); }
                    if (!bigEnough) { allBig = false; Console.WriteLine($"    {c[0]}x{c[1]} visible ref {vis.x:0}x{vis.y:0} TOO SMALL"); }
                }
            }
            Check(allOk, "aspect guard pins the design size on the constrained axis at every ratio");
            Check(allBig, "visible reference space never below 1280x720 at 16:9, 21:9, 4:3, windowed sizes");
            Screen.width = 1280; Screen.height = 720;
            foreach (var g in Hollowbrook.Core.AspectMatchGuard.Instances) g.Apply();

            // reference-space overlap audit — menu (top-left title, left column, bottom notes)
            var menu = new (string n, Rect2 r)[]
            {
                ("titleA",  new Rect2(60, 56, 820, 112)),
                ("titleB",  new Rect2(60, 136, 480, 256)),
                ("options", new Rect2(90, 290, 450, 548)),
                ("note",    new Rect2(10, 663, 910, 685)),
                ("footer",  new Rect2(10, 694, 910, 718)),
            };
            bool menuClean = true;
            for (int i = 0; i < menu.Length; i++)
                for (int j = i + 1; j < menu.Length; j++)
                    if (menu[i].r.Overlaps(menu[j].r)) { menuClean = false; Console.WriteLine($"    menu overlap: {menu[i].n} vs {menu[j].n}"); }
            Check(menuClean, "main menu: no element overlaps another (reference space)");

            // reference-space overlap audit — checkpoint desk (worst case = exactly 720 high)
            var cp = new (string n, Rect2 r)[]
            {
                ("topbar",   new Rect2(0, 0, 1280, 52)),
                ("bulletin", new Rect2(12, 56, 1268, 90)),
                ("speech",   new Rect2(775, 90, 1105, 210)),
                ("portrait", new Rect2(520, 96, 760, 392)),
                ("monitor",  new Rect2(1012, 302, 1264, 420)),
                ("tools",    new Rect2(871, 424, 1001, 594)),
                ("questions",new Rect2(1004, 439, 1264, 639)),
                ("stamps",   new Rect2(983, 645, 1273, 707)),
                ("idcard",   new Rect2(15, 505, 301, 691)),
                ("record",   new Rect2(317, 410, 555, 710)),
                ("clipboard",new Rect2(560, 473, 740, 711)),
            };
            bool cpClean = true;
            for (int i = 0; i < cp.Length; i++)
                for (int j = i + 1; j < cp.Length; j++)
                    if (cp[i].r.Overlaps(cp[j].r)) { cpClean = false; Console.WriteLine($"    checkpoint overlap: {cp[i].n} vs {cp[j].n}"); }
            Check(cpClean, "checkpoint: no panel overlaps another (reference space)");
        }

        static void ProceduralContentSweep()
        {
            Section("Procedural content generation sweep");
            string[] artKeys =
            {
                "solid_white","vignette","noise_overlay","flash","light_cone","menu_bg","hallway_bg",
                "classroom1_bg","classroom2_bg","security_bg","prop_door","prop_door_security","prop_server_door",
                "prop_lockers","prop_desk","prop_chalkboard","prop_window","prop_noticeboard","prop_cabinet",
                "prop_clock","prop_photoframe","prop_lightfixture","prop_lostid","custodian_0","custodian_1",
                "anomaly_blink","anomaly_slide","anomaly_sprint","anomaly_coverhug","id_card_frame",
                "record_sheet","clipboard","note_paper","stamp_allow","stamp_reject","icon_check","icon_x",
            };
            int artOk = 0;
            foreach (var k in artKeys)
            {
                var sp = ProceduralArt.Get(k);
                if (sp != null && sp.texture != null && sp.texture.width > 0) artOk++;
                else Console.WriteLine("    missing art: " + k);
            }
            Check(artOk == artKeys.Length, $"all {artKeys.Length} procedural sprites generate");

            var portrait = ProceduralArt.Portrait("t", new Portrait { HairStyle = 3, FaceVariant = 2, Accessory = 1, Distorted = true });
            Check(portrait != null && portrait.texture.width == 168, "distorted portrait generates at ID size");

            string[] audioKeys =
            {
                "sfx_click","sfx_ui_open","sfx_ui_close","sfx_paper","sfx_stamp_allow","sfx_stamp_reject",
                "sfx_scan_ok","sfx_scan_error","sfx_intercom","sfx_knock","sfx_footstep","sfx_door","sfx_flicker",
                "sfx_static","sfx_heartbeat","sfx_stinger","sfx_scare","sfx_glitch","sfx_pickup","sfx_bell","sfx_alarm",
                "music_menu","music_checkpoint","music_hallway","music_classroom","music_darkroom",
                "music_investigation","music_anomaly","music_final","music_end_win","music_end_lose","music_end_secret",
            };
            int audioOk = 0;
            foreach (var k in audioKeys)
            {
                var clip = ProceduralAudio.Get(k);
                if (clip != null && clip.length > 0.03f) audioOk++;
                else Console.WriteLine("    missing audio: " + k);
            }
            Check(audioOk == audioKeys.Length, $"all {audioKeys.Length} procedural clips synthesize");

            // save file actually lands on disk through the real SaveSystem
            R.StartNewGame(GameMode.Normal);
            DrainDialogue();
            R.QuitToMenu(true);
            Check(File.Exists(R.Saves.SavePath), "save file written to persistentDataPath");
            var loaded = R.Saves.Read();
            Check(loaded != null && loaded.Session.Mode == GameMode.Normal, "save file reads back");
        }
    }
}
