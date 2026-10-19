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

            Console.WriteLine(_failures == 0 ? "== FLOW CHAIN COMPLETE ==" : "== " + _failures + " CHECK(S) FAILED ==");
            Environment.Exit(_failures == 0 ? 0 : 1);
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
