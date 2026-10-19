using System;
using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

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
            Console.WriteLine("  diag dialogue children: " + string.Join(",", Names(root.Dialogue.transform)));
            Console.WriteLine("  diag checkpoint children: " + string.Join(",", Names(root.Checkpoint.transform)));
            Console.WriteLine("  diag: border found=" + (border != null) +
                              " active=" + (border != null && border.gameObject.activeSelf) +
                              " inner active=" + FindDeep(root.Dialogue.transform, "panel")?.gameObject.activeSelf);
            Check(border != null && !border.gameObject.activeSelf,
                "B4: dialogue OUTER border rect is inactive after the intro (no grey fullscreen rect)");
            var checkpointRoot = root.Checkpoint.transform;
            int window = DeepIndex(checkpointRoot, "windowframe");
            int visitor = DeepIndex(checkpointRoot, "visitor");
            int speech = DeepIndex(checkpointRoot, "speech");
            Check(window >= 0 && visitor > window && speech > window,
                $"B3: windowframe sibling {window} is BEHIND visitor {visitor} and speech {speech}");

            LayoutAudit();
            SaveContinueResume();

            Console.WriteLine(_failures == 0 ? "== FLOW CHAIN COMPLETE ==" : "== " + _failures + " CHECK(S) FAILED ==");
            Environment.Exit(_failures == 0 ? 0 : 1);
        }

        // UIFactory.Anchor math, replicated so on-screen placement is provable headlessly.
        private static void Rect(float ax, float ay, float w, float h, float px, float py,
            out float left, out float top, out float right, out float bottom)
        {
            float cx = ax * 1280f + px;
            float cy = (1f - ay) * 720f - py;
            left = cx - w / 2f; top = cy - h / 2f; right = cx + w / 2f; bottom = cy + h / 2f;
        }

        private static void OnScreen(string what, float ax, float ay, float w, float h, float px, float py)
        {
            float l, t, r, b;
            Rect(ax, ay, w, h, px, py, out l, out t, out r, out b);
            Check(l >= -0.5f && t >= -0.5f && r <= 1280.5f && b <= 720.5f,
                what + " on screen (" + l.ToString("0") + "," + t.ToString("0") + ")-(" + r.ToString("0") + "," + b.ToString("0") + ")");
        }

        private static void LayoutAudit()
        {
            Console.WriteLine("== 1280x720 layout audit (same math as UIFactory.Anchor) ==");
            var L = Hollowbrook.Checkpoint.CheckpointLayout.QuestionsColumnCenter;
            OnScreen("question column", 1f, 1f, 280f, 200f, L.x, L.y);
            OnScreen("question 1", 1f, 1f, 272f, 40f, L.x, L.y - 100f + 34f);
            OnScreen("question 4", 1f, 1f, 272f, 40f, L.x, L.y - 100f + 34f + 3f * 44f);
            OnScreen("questions panel", 1f, 1f, 300f, 250f,
                Hollowbrook.Checkpoint.CheckpointLayout.QuestionsPanelCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.QuestionsPanelCenter.y);
            OnScreen("record sheet", 0f, 1f, 320f, 280f,
                Hollowbrook.Checkpoint.CheckpointLayout.RecordCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.RecordCenter.y);
            // record sheet top edge sits at 480-140 = 340; rows hang 44+24*i below it
            OnScreen("record row NAME", 0f, 1f, 220f, 20f, 120f, -(340f + 44f));
            OnScreen("record row EXPIRES", 0f, 1f, 220f, 20f, 120f, -(340f + 44f + 6f * 24f));
            OnScreen("id card", 0.5f, 0f, 286f, 186f,
                Hollowbrook.Checkpoint.CheckpointLayout.IdCardCenter.x - 640f,
                Hollowbrook.Checkpoint.CheckpointLayout.IdCardCenter.y);
            OnScreen("camera monitor", 1f, 0f, 252f, 150f,
                Hollowbrook.Checkpoint.CheckpointLayout.CameraCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.CameraCenter.y);
            OnScreen("allow circle", 1f, 0f, 120f, 120f,
                Hollowbrook.Checkpoint.CheckpointLayout.AllowCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.AllowCenter.y);
            OnScreen("principal circle", 1f, 0f, 120f, 120f,
                Hollowbrook.Checkpoint.CheckpointLayout.PrincipalCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.PrincipalCenter.y);
            OnScreen("visitor portrait", 0.5f, 1f, 150f, 190f,
                Hollowbrook.Checkpoint.CheckpointLayout.PortraitCenter.x,
                Hollowbrook.Checkpoint.CheckpointLayout.PortraitCenter.y);
        }

        private static void SaveContinueResume()
        {
            Console.WriteLine("== B8 save/continue resume ==");
            var root = GameRoot.Instance;
            string first = root.Checkpoint.CurrentVisitor != null ? root.Checkpoint.CurrentVisitor.ClaimedName : "?";
            root.Checkpoint.Stamp(Hollowbrook.Logic.InspectionRules.ExpectedVerdict(
                root.Checkpoint.CurrentVisitor, null));
            root.PersistRun();
            root.QuitToMenu(true);
            root.ContinueGame();
            CheckpointSceneBootstrap.Build(root);
            for (int i = 0; i < 8 && root.Dialogue != null && root.Dialogue.IsActive; i++) root.Dialogue.Advance();
            string resumed = root.Checkpoint.CurrentVisitor != null ? root.Checkpoint.CurrentVisitor.ClaimedName : "?";
            Console.WriteLine("  first visitor before quit: " + first + " | first visitor after CONTINUE: " + resumed);
            Check(first != resumed, "B8: CONTINUE resumes at the next visitor, not the same one");
        }

        private static List<string> Names(Transform t)
        {
            var list = new List<string>();
            for (int i = 0; i < t.childCount; i++) list.Add(t.GetChild(i).name);
            return list;
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

        private static int IndexOfChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return i;
            return -1;
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
