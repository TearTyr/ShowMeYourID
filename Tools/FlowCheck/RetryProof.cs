using System;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.Scenes;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlowCheck
{
    // The owner's playtest bugs, replayed the way a player hits them: a lost night must come back
    // truly empty, and the end of a run must never strand the player on a screen with no buttons.
    internal static partial class Program
    {
        // Clock out of every night before this one, through the same save and Continue a player uses.
        private static void ReachNight(GameRoot root, int night)
        {
            FreshGame(root);
            for (int n = 1; n < night; n++)
            {
                ClickStickyNote(root);
                root.RequestEndShift();
                ContinueToDesk(root);
            }
        }

        private static void ContinueToDesk(GameRoot root)
        {
            root.ContinueGame();
            DeskScene.Build(root);
            AdvanceDialogue(root);
        }

        private static void AdvanceDialogue(GameRoot root)
        {
            for (int i = 0; i < 60 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
        }

        private static void FaintAtDesk(GameRoot root)
        {
            float dt = Time.deltaTime;
            root.Session.Sanity = 0f;
            Time.deltaTime = 0.5f;
            root.SanityCtl.Tick(true);
            Time.deltaTime = dt;
        }

        // A stamped verdict at the window, then the next one steps up.
        private static void StampCorrectly(GameRoot root)
        {
            var cp = root.Checkpoint;
            for (int i = 0; i < 20 && cp.CurrentVisitor == null; i++) cp.AdvanceQueue(6f);
            cp.Stamp(InspectionRules.ExpectedVerdict(cp.CurrentVisitor));
            HandBack(root, true, true);
            cp.AdvanceQueue(6f);
        }

        private static string AgainLabel(GameRoot root) =>
            FindDeep(root.Ending.transform, "again").GetComponentInChildren<UnityEngine.UI.Text>().text;

        private static void CheckFreshNight(GameRoot root, int night, string tag)
        {
            var s = root.Session;
            Check(s.CurrentShift == night && !s.RunFinished, tag + ": the same night again");
            Check(root.Queue.IsAttached && !root.Queue.Started && root.Queue.ProcessedCount == 0 &&
                  root.Queue.ArrivalCount == 0, tag + ": the line is empty, nobody has been processed");
            Check(!root.Queue.ShiftComplete, tag + ": the queue is not complete");
            Check(s.QueueState == null || !s.QueueState.HasProgress, tag + ": nothing of the old attempt is in the save");
            root.Checkpoint.RefreshHud();
            Check(!Btn(root, "endshift").interactable, tag + ": END SHIFT is locked");
        }

        private static void RetryProof(GameRoot root)
        {
            Console.WriteLine("== retry: a lost night comes back empty ==");

            // the tester's report: lose night 5 in the hunt, press RETRY, END SHIFT is open
            ReachNight(root, ShiftPlan.TotalShifts);
            ClickStickyNote(root);
            int nightFive = root.Session.CurrentShift;
            Check(nightFive == ShiftPlan.TotalShifts, "RETRY: (the bot clocked out of nights 1 to 4)");
            root.Checkpoint.Stamp(Verdict.Reject);
            HandBack(root, true, true);
            AdvanceDialogue(root);
            Check(root.HuntReady, "RETRY: (the administrator was refused)");
            root.Checkpoint.GoPatrol();
            PatrolScene.Build(root);
            for (int frame = 0; frame < 6000 && root.State == AppViewState.Exploration; frame++) WorldFrame(root, 0.1f);
            Check(root.Session.Hunt == HuntState.Lost, "RETRY: (doing nothing lost the hunt)");
            for (int frame = 0; frame < 200 && root.ShiftFailing; frame++) WorldFrame(root, 0.1f);
            if (root.Ending == null || root.State != AppViewState.Ending)
            {
                DeskScene.Build(root);   // the ending lives in the desk scene
            }
            Check(root.State == AppViewState.Ending, "RETRY: the lost hunt reaches the ending screen");
            Check(AgainLabel(root) == "RETRY NIGHT 5", "RETRY: the button offers night 5 (" + AgainLabel(root) + ")");
            FindDeep(root.Ending.transform, "again").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            DeskScene.Build(root);
            AdvanceDialogue(root);
            CheckFreshNight(root, nightFive, "RETRY night 5");
            Check(root.Session.Hunt == HuntState.None && !root.Session.AdminRejected, "RETRY night 5: the hunt and the refusal are gone");
            ClickStickyNote(root);
            Check(root.Checkpoint.CurrentVisitor != null && root.Checkpoint.CurrentVisitor.IsAdministrator,
                "RETRY night 5: the administrator is back at the window");

            // ...and the second attempt ends properly, it does not fall out of the game
            root.Checkpoint.RefreshHud();
            Check(!Btn(root, "endshift").interactable, "RETRY night 5: END SHIFT stays locked with him at the window");
            root.RequestEndShift();
            Check(root.Session.CurrentShift == nightFive && root.Session.CompletedShifts == nightFive - 1,
                "RETRY night 5: asking to clock out with him unresolved does nothing");

            // losing a second time goes back to the same start
            FaintAtDesk(root);
            Check(root.State == AppViewState.Ending, "RETRY: fainting on the second attempt is the Lose ending again");
            root.ContinueGame();
            DeskScene.Build(root);
            AdvanceDialogue(root);
            CheckFreshNight(root, nightFive, "RETRY night 5, twice");

            // a blackout after a quit and Continue half way through the night
            ReachNight(root, 3);
            ClickStickyNote(root);
            int processedAtStart = root.Session.Processed;
            StampCorrectly(root);
            StampCorrectly(root);
            Check(root.Session.Processed == processedAtStart + 2, "RETRY: (two students were stamped)");
            root.QuitToMenu(true);
            ContinueToDesk(root);
            Check(root.Queue.ProcessedCount == 2, "RETRY: (Continue resumed half way through the night)");
            FaintAtDesk(root);
            Check(root.Session.Processed == processedAtStart, "RETRY: the blackout goes back to the start of the night, not to the last save");
            ContinueToDesk(root);
            CheckFreshNight(root, 3, "RETRY after a mid-night Continue");

            // the same when the game was closed in between: the start of the night is in its own file
            ReachNight(root, 2);
            ClickStickyNote(root);
            StampCorrectly(root);
            root.QuitToMenu(true);
            Check(root.Saves.ReadNightStart() != null && root.Saves.ReadNightStart().Session.CurrentShift == 2,
                "RETRY: the start of the night is on disk, so it survives closing the game");
            ContinueToDesk(root);
            FaintAtDesk(root);
            ContinueToDesk(root);
            CheckFreshNight(root, 2, "RETRY after a restart");

            // the file was removed by hand: the night still restarts cleanly
            ReachNight(root, 2);
            ClickStickyNote(root);
            StampCorrectly(root);
            root.QuitToMenu(true);
            System.IO.File.Delete(root.Saves.NightStartPath);
            ContinueToDesk(root);
            FaintAtDesk(root);
            ContinueToDesk(root);
            CheckFreshNight(root, 2, "RETRY with the night start file gone");

            // a night start left by an older run is never used for this one
            ReachNight(root, 2);
            var foreign = root.Saves.ReadNightStart();
            foreign.Session.Seed++;
            root.Saves.WriteNightStart(foreign);
            ClickStickyNote(root);
            StampCorrectly(root);
            root.QuitToMenu(true);
            ContinueToDesk(root);
            FaintAtDesk(root);
            Check(root.Saves.Read().Session.Seed == foreign.Session.Seed - 1, "RETRY: a night start from another run is ignored");

            // the game closed while the lost-hunt scene played: Continue must not land at a dead desk
            ReachNight(root, ShiftPlan.TotalShifts);
            ClickStickyNote(root);
            root.Session.Hunt = HuntState.Lost;
            root.PersistRun();
            ContinueToDesk(root);
            CheckFreshNight(root, ShiftPlan.TotalShifts, "RETRY after closing during the lost hunt");
            Check(root.Session.Hunt == HuntState.None, "RETRY: ...and the lost hunt is forgotten");

            // a run already over the lose limit at the start of the night cannot be won: no retry trap
            ReachNight(root, ShiftPlan.TotalShifts);
            root.Session.Breaches = EndingRules.LoseThreshold;
            root.PersistRun();
            root.QuitToMenu(false);
            root.Saves.WriteNightStart(root.Saves.Read());
            ContinueToDesk(root);
            ClickStickyNote(root);
            root.Checkpoint.Stamp(Verdict.Reject);
            Check(root.State == AppViewState.Ending && !root.HasSave(),
                "RETRY: a run lost for good does not offer Continue");
            Check(AgainLabel(root) == "PLAY AGAIN", "RETRY: its button is PLAY AGAIN (" + AgainLabel(root) + ")");

            Console.WriteLine("== soft lock: the end of a run always reaches a menu ==");
            string loaded = null;
            SceneLoadBridge.SceneLoadHook = name => loaded = name;
            ReachNight(root, ShiftPlan.TotalShifts);
            loaded = null;
            root.FinishShift();      // the path the tester hit: a night closed with him unresolved
            Check(root.State == AppViewState.ShiftResults, "SOFTLOCK: (the results screen is up)");
            root.AdvanceToNextShift();
            Check(loaded == SceneRouter.Menu, "SOFTLOCK: its button loads the menu scene (got '" + loaded + "'), the desk scene has no menu");
            SceneLoadBridge.SceneLoadHook = null;

            // NEW GAME and erase data leave nothing of the night start behind
            FreshGame(root);
            Check(System.IO.File.Exists(root.Saves.NightStartPath), "SAVE: a new night writes its night start file");
            root.Saves.Delete();
            Check(!System.IO.File.Exists(root.Saves.NightStartPath) && root.Saves.ReadNightStart() == null,
                "SAVE: erasing the data removes the night start too");
            FreshGame(root);   // leave a live game for whatever runs next
        }

        private static float SanityAfterTick(GameRoot root, bool atDesk, int ticks = 4)
        {
            float dt = Time.deltaTime;
            Time.deltaTime = 0.5f;
            for (int i = 0; i < ticks; i++) root.SanityCtl.Tick(atDesk);
            Time.deltaTime = dt;
            return root.Session.Sanity;
        }

        // The playtest report: the bar never drained on nights 5 and 6.
        private static void SanityProof(GameRoot root)
        {
            Console.WriteLine("== sanity: always draining on the administrator's nights ==");
            ReachNight(root, 2);
            root.Session.Sanity = 60f; root.Session.Stacks.Clear();
            Check(root.Checkpoint.CurrentVisitor == null, "SANITY: (the window is empty)");
            Check(SanityAfterTick(root, true) > 60f, "SANITY: at the desk on night 2, an empty window still rests the custodian");
            Check(root.Session.Sanity - 60f < 4 * 0.5f * GameRules.Current.SecurityRoomRecoveryPerSecond + 0.01f,
                "SANITY: ...at the rate the rules say, no faster");
            ReachNight(root, ShiftPlan.TotalShifts);
            Check(root.AdministratorNight, "SANITY: (night 5 is an administrator night)");
            root.Session.Sanity = 80f; root.Session.Stacks.Clear();
            float before = root.Session.Sanity;
            Check(SanityAfterTick(root, true) < before, "SANITY: night 5 at the desk with an empty window drains instead of resting");
            before = root.Session.Sanity;
            Check(SanityAfterTick(root, false) < before, "SANITY: night 5 on patrol (hallway) drains");
            before = root.Session.Sanity;
            root.Session.Hunt = HuntState.Active;
            Check(SanityAfterTick(root, false) < before, "SANITY: night 5 during the hunt drains (it used to be exempt)");
            root.Session.Hunt = HuntState.None;
            before = root.Session.Sanity;
            root.SetState(AppViewState.RoomPeek);
            Check(SanityAfterTick(root, false) < before, "SANITY: night 5 inside a peek drains");
            root.SetState(AppViewState.Checkpoint);
            root.Session.Sanity = 90f;
            SanityAfterTick(root, true, 1);
            float one = 90f - root.Session.Sanity;
            Check(Mathf.Abs(one - 0.5f * GameRules.Current.FinalNightDrainPerSecond * 0.85f) < 0.01f,
                "SANITY: the rate is the rules' final night rate (with the 0.85 baseline), one number, not two stacked (" + one + ")");
            // night 6 is harder
            root.Session.Mode = GameMode.Challenge;
            root.Session.Sanity = 90f;
            SanityAfterTick(root, true, 1);
            Check(90f - root.Session.Sanity > one * 1.4f, "SANITY: night 6 drains faster than night 5");
            root.Session.Mode = GameMode.Normal;
            var r = GameRules.Current;
            Check(r.RefillOnCorrect <= 8f && r.RefillPerSecond <= 1.5f && r.SecurityRoomRecoveryPerSecond <= 0.5f,
                "SANITY: the passive gains are the nerfed values");
            // a stall (tab-out) never drains in one jump
            root.Session.Sanity = 90f;
            float spike = Time.deltaTime;
            Time.deltaTime = 60f;
            root.SanityCtl.Tick(true);
            Time.deltaTime = spike;
            Check(root.Session.Sanity > 90f - 0.5f * 0.5f, "SANITY: a one minute stall costs no more than half a second of drain");
            FreshGame(root);
        }

        // Other leaks found while playing the game through: a click that erased a run, and a quit that
        // erased a hunt's catches.
        private static void MoreFlawsProof(GameRoot root)
        {
            Console.WriteLine("== more flaws: NEW GAME asks first, a hunt cannot be escaped by quitting ==");
            string loaded = null;
            SceneLoadBridge.SceneLoadHook = name => loaded = name;
            FreshGame(root);
            ClickStickyNote(root);
            root.Session.Score = 777;
            root.PersistRun();
            MenuScene.Build(root);
            var newGame = root.MainMenu.transform.Find("buttons").Find("NEW GAME").GetComponent<UnityEngine.UI.Button>();
            loaded = null;
            newGame.onClick.Invoke();
            Check(root.Confirm.IsOpen && loaded == null, "NEWGAME: with a game in progress, the button asks first and loads nothing");
            Check(root.Saves.Read().Session.Score == 777, "NEWGAME: ...and the save is untouched");
            FindDeep(root.Confirm.transform, "no").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!root.Confirm.IsOpen && loaded == null && root.Saves.Read().Session.Score == 777, "NEWGAME: NO changes nothing");
            newGame.onClick.Invoke();
            FindDeep(root.Confirm.transform, "yes").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(loaded == SceneRouter.Checkpoint && root.Session.Score == 0, "NEWGAME: YES starts the new game");
            DeskScene.Build(root);
            AdvanceDialogue(root);
            root.Saves.Delete();
            MenuScene.Build(root);
            newGame = root.MainMenu.transform.Find("buttons").Find("NEW GAME").GetComponent<UnityEngine.UI.Button>();
            loaded = null;
            newGame.onClick.Invoke();
            Check(!root.Confirm.IsOpen && loaded == SceneRouter.Checkpoint, "NEWGAME: with nothing to lose it starts straight away");
            DeskScene.Build(root);
            AdvanceDialogue(root);
            SceneLoadBridge.SceneLoadHook = null;

            // quit in the middle of the hunt and Continue: the catches and the clock are where they were
            ReachNight(root, ShiftPlan.TotalShifts);
            ClickStickyNote(root);
            root.Checkpoint.Stamp(Verdict.Reject);
            HandBack(root, true, true);
            AdvanceDialogue(root);
            root.Checkpoint.GoPatrol();
            PatrolScene.Build(root);
            var hunt = root.Views.Hunt.Model;
            for (int frame = 0; frame < 6000 && hunt.Catches < 1 && !hunt.Finished; frame++) WorldFrame(root, 0.1f);
            Check(hunt.Catches == 1 && !hunt.Finished, "HUNTSAVE: (he caught the player once)");
            float left = hunt.SurviveLeft;
            root.QuitToMenu(true);
            var saved = root.Saves.Read();
            Check(saved.Session.HuntCatches == 1 && Mathf.Abs(saved.Session.HuntSecondsLeft - left) < 0.01f,
                "HUNTSAVE: quitting saves the catches and the dawn clock");
            root.ContinueGame();
            DeskScene.Build(root);
            AdvanceDialogue(root);
            PatrolScene.Build(root);
            hunt = root.Views.Hunt.Model;
            Check(hunt.Catches == 1, "HUNTSAVE: Continue brings the catch back, it is no free escape (" + hunt.Catches + ")");
            Check(hunt.SurviveLeft <= left + 0.01f && hunt.SurviveLeft > left - 5f, "HUNTSAVE: ...and the dawn clock where it stood");
            Check(hunt.Phase == HuntPhase.Stalking, "HUNTSAVE: he starts walking again, nothing instant");
            for (int frame = 0; frame < 20000 && !hunt.Finished; frame++) WorldFrame(root, 0.1f);
            Check(hunt.Phase == HuntPhase.Lost && hunt.Catches == hunt.Tuning.CatchesToLose,
                "HUNTSAVE: doing nothing now loses after the remaining catches only");
            FreshGame(root);
        }
    }
}
