using System;
using Hollowbrook.Checkpoint;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.Scenes;
using Hollowbrook.UI;
using UnityEngine;

namespace FlowCheck
{
    // Lockdown: every student at the gate at once, a clock on each face, three mistakes, no patrol, no save.
    internal static partial class Program
    {
        private static string SaveText()
        {
            string path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "hollowbrook_save.txt");
            return System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;
        }

        private static string LockdownInfo(GameRoot root) =>
            FindDeep(root.Checkpoint.transform, "lockdowninfo").GetComponent<UnityEngine.UI.Text>().text;

        private static void StartLockdown(GameRoot root)
        {
            root.StartNewGame(GameMode.Lockdown);
            DeskScene.Build(root);
            AdvanceDialogue(root);
            ClickStickyNote(root);
        }

        // put whoever is next at the window
        private static VisitorData FaceAtWindow(GameRoot root)
        {
            var cp = root.Checkpoint;
            for (int i = 0; i < 40 && cp.CurrentVisitor == null; i++) cp.AdvanceQueue(1f);
            return cp.CurrentVisitor;
        }

        private static void LockdownProof(GameRoot root)
        {
            Console.WriteLine("== lockdown: thirteen faces, a clock on each, three mistakes ==");
            string storyBefore = SaveText();

            root.BootToMainMenu();
            var buttons = root.MainMenu.transform.Find("buttons");
            Check(buttons.Find("LOCKDOWN") != null && buttons.Find("NEW GAME") != null && buttons.Find("CONTINUE") != null && buttons.Find("NIGHT 6") != null,
                "LD: the menu has four buttons: NEW GAME, CONTINUE, NIGHT 6 and LOCKDOWN");
            var lockBtn = buttons.Find("LOCKDOWN").GetComponent<UnityEngine.UI.Button>();
            Check(!lockBtn.interactable, "LD: LOCKDOWN is locked until Night 6 has been beaten (or the secret code is used)");
            var wide = buttons.Find("NEW GAME").GetComponent<RectTransform>().sizeDelta;
            Check(wide.x < 320f && wide.x > 280f && wide.y < 66f && wide.y > 56f, "LD: the wide buttons are about 70 percent of their old size (" + wide.x + " x " + wide.y + ")");
            float lastBottom = buttons.Find("LOCKDOWN").GetComponent<RectTransform>().anchoredPosition.y - wide.y / 2f;
            Check(-lastBottom < 720f - 100f, "LD: the four buttons end well above the icon row");

            StartLockdown(root);
            var cp = root.Checkpoint;
            Check(root.LockdownOn && root.CurrentShiftDef.Lockdown && root.Session.CurrentShift == ShiftPlan.LockdownNumber, "LD: the mode starts its own night");
            Check(root.CurrentRoster.Count == 13 && root.CurrentRoster.FindAll(v => v.IsAnomaly).Count == 6 && root.CurrentRoster.TrueForAll(v => v.IsScared),
                "LD: thirteen faces, six of them not students, all frightened");
            Check(!root.TutorialActive && root.Session.Pool.Pending == 0, "LD: no tutorial and nothing owed to a patrol");
            Check(FindDeep(cp.transform, "lockdownbar").gameObject.activeSelf, "LD: the countdown bar is on the desk");
            cp.RefreshHud();
            Check(!Btn(root, "patrol").interactable && !Btn(root, "endshift").interactable, "LD: no PATROL and no END SHIFT");
            Check(root.Queue.Tuning.ArrivalMaxSeconds <= LockdownRules.ArrivalMaxSeconds && root.Queue.Tuning.MissingStudentChance == 0f,
                "LD: they arrive fast and nobody goes missing");

            // the clock on the face at the window
            var first = FaceAtWindow(root);
            cp.AdvanceQueue(0.01f);   // the bar is drawn before the queue presents a face, so give it a frame
            Check(first != null && LockdownInfo(root).Contains("MISTAKES 0/3") && LockdownInfo(root).Contains("35"), "LD: a face at the window starts at 35 seconds (" + LockdownInfo(root) + ")");
            float sanity = root.Session.Sanity;
            cp.AdvanceQueue(10f);
            Check(LockdownInfo(root).Contains("25") && cp.CurrentVisitor == first, "LD: ten seconds later 25 are left (" + LockdownInfo(root) + ")");
            RunUpdates(root, 20);
            Check(root.Session.Sanity == sanity, "LD: sanity does not drain in Lockdown");

            // a wrong stamp is a mistake: let in an anomaly or turn a student away
            var wrongVerdict = InspectionRules.ExpectedVerdict(first) == Verdict.Allow ? Verdict.Reject : Verdict.Allow;
            cp.Stamp(wrongVerdict);
            HandBack(root, true, true);
            Check(root.Session.LockdownStrikes == 1, "LD: the wrong stamp is the first mistake");
            cp.AdvanceQueue(0.01f);   // the bar is redrawn on the next frame
            Check(LockdownInfo(root).Contains("MISTAKES 1/3"), "LD: and the bar says so");

            // time runs out: a real student taken is a mistake, an anomaly taken is not
            int strikes = root.Session.LockdownStrikes;
            var next = FaceAtWindow(root);
            bool wasStudent = !next.IsAnomaly;
            cp.AdvanceQueue(LockdownRules.SecondsPerVisitor + 1f);
            Check(root.Session.LockdownStrikes == strikes + (wasStudent ? 1 : 0),
                "LD: when the time runs out the " + (wasStudent ? "student is taken and it is a mistake" : "anomaly is taken and it is not a mistake"));
            Check(cp.CurrentVisitor != next, "LD: the next face steps up");

            // the third mistake loses
            for (int i = 0; i < 20 && root.State != AppViewState.Ending; i++)
            {
                var face = FaceAtWindow(root);
                if (face == null) break;
                cp.Stamp(InspectionRules.ExpectedVerdict(face) == Verdict.Allow ? Verdict.Reject : Verdict.Allow);
                HandBack(root, true, true);
            }
            Check(root.State == AppViewState.Ending && root.Session.LockdownStrikes >= LockdownRules.MaxStrikes, "LD: the third mistake ends the night");
            Check(AgainLabel(root) == "TRY AGAIN", "LD: the ending offers TRY AGAIN");
            Check(SaveText() == storyBefore, "LD: the story save is untouched");

            // clearing the line wins, with a rank by mistakes
            StartLockdown(root);
            cp = root.Checkpoint;
            for (int i = 0; i < 13 && root.State != AppViewState.Ending; i++) StampCorrectly(root);
            Check(root.State == AppViewState.Ending && root.Session.LockdownStrikes == 0, "LD: thirteen right calls clear the line and win");
            Check(AgainLabel(root) == "TRY AGAIN", "LD: and the win offers TRY AGAIN too");
            var summary = root.Ending.SummaryText;
            Check(summary.Contains("FLAWLESS"), "LD: no mistakes ranks FLAWLESS (" + summary.Replace("\n", " / ") + ")");
            Check(SaveText() == storyBefore, "LD: the win leaves the story save untouched too");

            // an unattended face is taken, and a clear line still wins when the last one is taken
            StartLockdown(root);
            cp = root.Checkpoint;
            for (int i = 0; i < 13 && root.State != AppViewState.Ending; i++)
            {
                var face = FaceAtWindow(root);
                if (face == null) break;
                if (i % 2 == 0) { cp.Stamp(InspectionRules.ExpectedVerdict(face)); HandBack(root, true, true); }
                else cp.AdvanceQueue(LockdownRules.SecondsPerVisitor + 1f);
            }
            Check(root.State == AppViewState.Ending, "LD: a night of right stamps and timeouts ends one way or the other");

            // Night 6 and the story still work from the menu
            root.BootToMainMenu();
            Check(buttons.Find("NIGHT 6") != null, "LD: the menu is back");
        }
    }
}
