using System;
using System.Reflection;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.Scenes;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace FlowCheck
{
    // The Night 1 tutorial played the way a new player does: the desk, the hallway, two peeks, then alone.
    internal static partial class Program
    {
        private static string GuideTitle(GameRoot root) =>
            FindDeep(FindDeep(root.HUD.transform, "tutorialguide_border"), "title").GetComponent<UnityEngine.UI.Text>().text;

        private static bool GuideShown(GameRoot root) =>
            FindDeep(root.HUD.transform, "tutorialguide_border").gameObject.activeSelf;

        // GameRoot.Update never runs headless: call it by hand, a big step at a time
        private static void RunUpdates(GameRoot root, int frames)
        {
            var update = typeof(GameRoot).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Time.deltaTime = 0.5f;
            for (int i = 0; i < frames; i++) update.Invoke(root, null);
        }

        private static Doorway HallDoor(GameRoot root, RoomId target)
        {
            foreach (var it in root.Views.Room(RoomId.Hallway).Interactables)
            {
                var d = it as Doorway;
                if (d != null && d.Target == target) return d;
            }
            return null;
        }

        // stamp whoever is at the window the right way, ticking the tutorial the way a frame would
        private static VisitorData StampLessonVisitor(GameRoot root)
        {
            var cp = root.Checkpoint;
            for (int i = 0; i < 20 && cp.CurrentVisitor == null; i++) cp.AdvanceQueue(6f);
            var who = cp.CurrentVisitor;
            cp.Stamp(InspectionRules.ExpectedVerdict(who));
            root.TickTutorial();
            HandBack(root, true, true);
            root.TickTutorial();
            return who;
        }

        private static void TutorialProof(GameRoot root)
        {
            Console.WriteLine("== night 1 tutorial: the desk, the hallway, two peeks, then alone ==");
            root.StartNewGame(GameMode.Normal);
            DeskScene.Build(root);
            AdvanceDialogue(root);
            var cp = root.Checkpoint;
            Check(root.TutorialStep == TutorialStep.FirstStudent && root.TutorialActive, "TUT: a new story starts with the lesson");
            Check(GuideShown(root) && GuideTitle(root) == TutorialSteps.Title(TutorialStep.FirstStudent), "TUT: the guide box shows the first step");
            Check(FindDeep(root.HUD.transform, "skip").gameObject.activeSelf, "TUT: SKIP TUTORIAL is there");
            Check(!FindDeep(root.HUD.transform, "gotit").gameObject.activeSelf, "TUT: GOT IT waits for the last step");

            // the clock and the sanity wait while a lesson is showing
            ClickStickyNote(root);
            string clock = root.ClockString;
            float sanity = root.Session.Sanity;
            RunUpdates(root, 60);
            Check(root.ClockString == clock && root.Session.Sanity == sanity, "TUT: the clock and sanity stand still during the lesson (" + root.ClockString + ")");

            var first = StampLessonVisitor(root);
            Check(!first.IsAnomaly && !first.IsScared, "TUT: visitor 1 is a calm real student");
            Check(root.TutorialStep == TutorialStep.FirstAnomaly && GuideTitle(root) == TutorialSteps.Title(TutorialStep.FirstAnomaly),
                "TUT: one stamp moves the guide to the second visitor");
            Check(!root.TutorialHoldsLine, "TUT: the second visitor still arrives normally");

            var second = StampLessonVisitor(root);
            Check(second.IsAnomaly && !second.IsScared && second.Defects.Count == 1 && second.Defects[0] == DefectType.ExpiredId,
                "TUT: visitor 2 has one clear broken rule, the expired ID");
            cp.AdvanceQueue(6f);
            Check(root.TutorialStep == TutorialStep.GoPatrol && root.TutorialHoldsLine && root.Queue.HoldLine, "TUT: two stamps open the patrol lesson and hold the line");
            Check(cp.CurrentVisitor == null || cp.CurrentVisitor == second, "TUT: nobody new steps up to the window while the lesson walks");
            cp.RefreshHud();
            Check(Btn(root, "patrol").interactable, "TUT: PATROL is open although the line is not done");

            // the hallway: one slow anomaly, nothing can catch the player, other doors stay shut
            cp.GoPatrol();
            PatrolScene.Build(root);
            var views = root.Views;
            root.TickTutorial();
            Check(root.State == AppViewState.Exploration && root.TutorialStep == TutorialStep.HallAnomaly, "TUT: leaving the booth starts the hallway lesson");
            Check(GuideShown(root) && GuideTitle(root) == TutorialSteps.Title(TutorialStep.HallAnomaly), "TUT: the guide follows to the hallway");
            WorldFrames(root, 3);
            Check(views.Anomalies.Count == 1 && views.Anomalies[0].Kind == AnomalyBehaviorKind.Slide, "TUT: one slow Slide anomaly waits in the hallway");
            var lesson = views.Anomalies.Count > 0 ? views.Anomalies[0] : null;
            if (lesson != null)
            {
                var spot = lesson.transform.localPosition; spot.x = views.Player.transform.localPosition.x; lesson.transform.localPosition = spot;
                float hurt = root.Session.Sanity;
                WorldFrames(root, 600);
                Check(!lesson.WasCaught && root.Session.Sanity == hurt, "TUT: it cannot catch the player during training");
            }
            var door101 = HallDoor(root, RoomId.Classroom1);
            door101.Interact();
            Check(root.State == AppViewState.Exploration, "TUT: Room 101 does not open before its lesson");
            if (lesson != null) lesson.Despawn(true);   // the Focus light drove it out
            root.TickTutorial();
            Check(root.TutorialStep == TutorialStep.PeekClean, "TUT: driving it out moves the guide on to Room 101");
            WorldFrames(root, 700);
            Check(views.Anomalies.Count == 0, "TUT: no other hallway anomaly shows up during training");

            // Room 101 is clean, Room 202 holds the lesson, nothing else opens
            HallDoor(root, RoomId.Classroom2).Interact();
            Check(root.State == AppViewState.Exploration, "TUT: Room 202 stays shut until Room 101 is done");
            HallDoor(root, RoomId.ComfortRoom).Interact();
            Check(root.State == AppViewState.Exploration, "TUT: the CR stays shut too");
            door101.Interact();
            Check(root.State == AppViewState.RoomPeek && views.PeekedAnomaly == null, "TUT: Room 101 opens and is clean");
            root.TickTutorial();
            Check(root.TutorialStep == TutorialStep.PeekClean, "TUT: the guide waits for the step back");
            root.ExitRoomPeek();
            for (int f = 0; f < 600 && root.State == AppViewState.RoomPeek; f++) WorldFrame(root);
            root.TickTutorial();
            Check(root.TutorialStep == TutorialStep.PeekAnomaly, "TUT: stepping back from Room 101 moves the guide on to Room 202");
            door101.Interact();
            Check(root.State == AppViewState.Exploration, "TUT: Room 101 is not the lesson any more");

            HallDoor(root, RoomId.Classroom2).Interact();
            WorldFrames(root, 3);
            Check(root.State == AppViewState.RoomPeek && views.PeekedAnomaly != null, "TUT: Room 202 always holds one");
            if (views.PeekedAnomaly != null)
            {
                var at = views.PeekedAnomaly.transform.localPosition;
                AimAt(at.x, at.y);
                for (int f = 0; f < 500 && (root.Session.PeekAnomaliesGone & PatrolCycle.Bit(RoomId.Classroom2)) == 0; f++) WorldFrame(root);
            }
            Check((root.Session.PeekAnomaliesGone & PatrolCycle.Bit(RoomId.Classroom2)) != 0, "TUT: the lamp on the figure clears it");
            WorldFrames(root, 600);
            root.ExitRoomPeek();
            for (int f = 0; f < 600 && root.State == AppViewState.RoomPeek; f++) WorldFrame(root);
            root.TickTutorial();
            Check(root.TutorialStep == TutorialStep.Finish && GuideTitle(root) == TutorialSteps.Title(TutorialStep.Finish), "TUT: the last step says training is over");
            Check(FindDeep(root.HUD.transform, "gotit").gameObject.activeSelf, "TUT: GOT IT shows on the last step");
            HallDoor(root, RoomId.ComfortRoom).Interact();
            Check(root.State == AppViewState.RoomPeek, "TUT: every door opens on the last step");
            root.ExitRoomPeek();
            for (int f = 0; f < 600 && root.State == AppViewState.RoomPeek; f++) WorldFrame(root);
            root.TutorialGotIt();
            root.TickTutorial();
            Check(!root.TutorialActive && !GuideShown(root) && !root.Queue.HoldLine, "TUT: GOT IT ends the tutorial, hides the box and frees the line");

            // alone: back to the desk, the line carries on, the log is the last job
            root.ReturnToCheckpoint();
            DeskScene.Build(root);
            for (int i = 0; i < 20 && root.Checkpoint.CurrentVisitor == null; i++) root.Checkpoint.AdvanceQueue(6f);
            cp = root.Checkpoint;
            Check(cp.CurrentVisitor != null, "TUT: back at the desk the next visitor steps up");
            for (int i = 0; i < 6 && !root.Queue.ShiftComplete; i++) StampCorrectly(root);
            Check(root.Queue.ShiftComplete, "TUT: the rest of the night plays through");
            cp.RefreshHud();
            Check(!Btn(root, "endshift").interactable, "TUT: END SHIFT still waits for the signed log");
            SignLog(root);
            cp.RefreshHud();
            Check(Btn(root, "endshift").interactable, "TUT: once the log is signed the night can end");
            RunUpdates(root, 60);
            Check(root.ClockString != clock, "TUT: the clock runs again after the tutorial");

            // SKIP at any time
            root.StartNewGame(GameMode.Normal);
            DeskScene.Build(root);
            AdvanceDialogue(root);
            Check(GuideShown(root), "TUT: a second new story has the lesson again");
            FindDeep(root.HUD.transform, "skip").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(!root.TutorialActive && !GuideShown(root), "TUT: SKIP TUTORIAL ends it at once");
            Check(root.Session.TutorialStep == TutorialStep.Done, "TUT: and the save says so");

            // quit and Continue in the middle of a lesson
            root.StartNewGame(GameMode.Normal);
            DeskScene.Build(root);
            AdvanceDialogue(root);
            ClickStickyNote(root);
            StampLessonVisitor(root);
            StampLessonVisitor(root);
            root.PersistRun();
            Check(root.Saves.Read().Session.TutorialStep == TutorialStep.GoPatrol, "TUT: the step is in the save");
            ContinueToDesk(root);
            root.TickTutorial();
            Check(root.TutorialStep == TutorialStep.GoPatrol && GuideShown(root) && root.Queue.HoldLine, "TUT: Continue comes back to the same lesson with the line held");

            // a game that never had a tutorial: Continue from an old save has none
            root.Session.TutorialStep = TutorialStep.Done;
            root.PersistRun();
            ContinueToDesk(root);
            Check(!root.TutorialActive && !GuideShown(root), "TUT: a save without a tutorial stays without one");
        }
    }
}
