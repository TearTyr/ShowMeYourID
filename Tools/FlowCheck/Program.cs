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

        // The desk grid is absolute 1280x720 regions; the audit replays Place()'s pin math and
        // proves every panel lands on screen and that co-visible panels never overlap.
        // Tags: C = checkpoint desk, E = exploration, B = both.
        private struct AuditRow
        {
            public string Name;
            public Hollowbrook.Checkpoint.CheckpointLayout.Region Reg;
            public Hollowbrook.Checkpoint.CheckpointLayout.Pin Pin;
            public string Tags;
        }

        private static void LayoutAudit()
        {
            Console.WriteLine("== 1280x720 layout audit (single region grid) ==");
            var rows = new List<AuditRow>();
            Add(rows, "day banner", Hollowbrook.Checkpoint.CheckpointLayout.Banner, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopLeft, "B");
            Add(rows, "sanity meter", Hollowbrook.Checkpoint.CheckpointLayout.SanityMeter, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopLeft, "B");
            Add(rows, "light meter", Hollowbrook.Checkpoint.CheckpointLayout.LightMeter, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter, "B");
            Add(rows, "track meter", Hollowbrook.Checkpoint.CheckpointLayout.TrackMeter, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "B");
            Add(rows, "clock", Hollowbrook.Checkpoint.CheckpointLayout.Clock, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "B");
            Add(rows, "pause", Hollowbrook.Checkpoint.CheckpointLayout.Pause, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "B");
            Add(rows, "run stats", Hollowbrook.Checkpoint.CheckpointLayout.RunStats, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter, "B");
            Add(rows, "findings", Hollowbrook.Checkpoint.CheckpointLayout.Findings, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "B");
            Add(rows, "objective", Hollowbrook.Checkpoint.CheckpointLayout.Objective, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopLeft, "E");
            Add(rows, "room info", Hollowbrook.Checkpoint.CheckpointLayout.RoomInfo, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "E");
            Add(rows, "prompt", Hollowbrook.Checkpoint.CheckpointLayout.Prompt, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter, "E");
            Add(rows, "record sheet", Hollowbrook.Checkpoint.CheckpointLayout.Record, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopLeft, "C");
            Add(rows, "id card", Hollowbrook.Checkpoint.CheckpointLayout.IdCard, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomLeft, "C");
            Add(rows, "checklist", Hollowbrook.Checkpoint.CheckpointLayout.Checklist, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomLeft, "C");
            Add(rows, "window", Hollowbrook.Checkpoint.CheckpointLayout.Window, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter, "C");
            Add(rows, "sill", Hollowbrook.Checkpoint.CheckpointLayout.Sill, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter, "C");
            Add(rows, "speech", Hollowbrook.Checkpoint.CheckpointLayout.Speech, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter, "C");
            Add(rows, "principal note", Hollowbrook.Checkpoint.CheckpointLayout.Note, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter, "C");
            Add(rows, "patrol button", Hollowbrook.Checkpoint.CheckpointLayout.PatrolBtn, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter, "C");
            Add(rows, "end shift button", Hollowbrook.Checkpoint.CheckpointLayout.EndShiftBtn, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter, "C");
            Add(rows, "questions panel", Hollowbrook.Checkpoint.CheckpointLayout.Questions, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "C");
            Add(rows, "scan button", Hollowbrook.Checkpoint.CheckpointLayout.UtilityScan, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "C");
            Add(rows, "log button", Hollowbrook.Checkpoint.CheckpointLayout.UtilityRecords, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "C");
            Add(rows, "map button", Hollowbrook.Checkpoint.CheckpointLayout.UtilityMap, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "C");
            Add(rows, "rules button", Hollowbrook.Checkpoint.CheckpointLayout.UtilityNotes, Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight, "C");
            Add(rows, "call principal", Hollowbrook.Checkpoint.CheckpointLayout.StampReject, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomRight, "C");
            Add(rows, "allow", Hollowbrook.Checkpoint.CheckpointLayout.StampAllow, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomRight, "C");
            Add(rows, "camera monitor", Hollowbrook.Checkpoint.CheckpointLayout.Monitor, Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomRight, "C");

            foreach (var r in rows)
            {
                float l, t, w, h;
                Placed(r.Reg, r.Pin, out l, out t, out w, out h);
                Check(Mathf.Abs(l - r.Reg.X) < 0.01f && Mathf.Abs(t - r.Reg.Y) < 0.01f &&
                      Mathf.Abs(w - r.Reg.W) < 0.01f && Mathf.Abs(h - r.Reg.H) < 0.01f,
                    r.Name + " placed at (" + l.ToString("0") + "," + t.ToString("0") + ") " + w.ToString("0") + "x" + h.ToString("0"));
                Check(l >= -0.5f && t >= -0.5f && l + w <= 1280.5f && t + h <= 720.5f, r.Name + " on screen");
            }
            for (int i = 0; i < rows.Count; i++)
                for (int j = i + 1; j < rows.Count; j++)
                {
                    if (!ShareState(rows[i].Tags, rows[j].Tags)) continue;
                    var a = rows[i].Reg; var b = rows[j].Reg;
                    bool hit = a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
                    Check(!hit, rows[i].Name + " / " + rows[j].Name + " do not overlap");
                }
        }

        private static void Add(List<AuditRow> rows, string name,
            Hollowbrook.Checkpoint.CheckpointLayout.Region reg, Hollowbrook.Checkpoint.CheckpointLayout.Pin pin, string tags)
        {
            rows.Add(new AuditRow { Name = name, Reg = reg, Pin = pin, Tags = tags });
        }

        private static bool ShareState(string a, string b)
        {
            if (a.Contains("B") || b.Contains("B")) return true;
            foreach (char c in a) if (b.IndexOf(c) >= 0) return true;
            return false;
        }

        // same corner-pin math as CheckpointLayout.Place: anchor point + pos lands the pivot
        // corner, then the rect hangs off that corner per pin.
        private static void Placed(Hollowbrook.Checkpoint.CheckpointLayout.Region r,
            Hollowbrook.Checkpoint.CheckpointLayout.Pin pin, out float left, out float top, out float w, out float h)
        {
            w = r.W; h = r.H;
            float ax, ay, px, py;
            switch (pin)
            {
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopLeft:
                    ax = 0; ay = 0; px = r.X; py = r.Y; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter:
                    ax = 640; ay = 0; px = r.X + r.W / 2 - 640; py = r.Y; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight:
                    ax = 1280; ay = 0; px = r.Right - 1280; py = r.Y; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomLeft:
                    ax = 0; ay = 720; px = r.X; py = r.Bottom - 720; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter:
                    ax = 640; ay = 720; px = r.X + r.W / 2 - 640; py = r.Bottom - 720; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomRight:
                    ax = 1280; ay = 720; px = r.Right - 1280; py = r.Bottom - 720; break;
                default:
                    ax = 640; ay = 360; px = r.X + r.W / 2 - 640; py = r.Y + r.H / 2 - 360; break;
            }
            float cornerX = ax + px, cornerY = ay + py;
            switch (pin)
            {
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopCenter: left = cornerX - w / 2; top = cornerY; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.TopRight: left = cornerX - w; top = cornerY; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomLeft: left = cornerX; top = cornerY - h; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomCenter: left = cornerX - w / 2; top = cornerY - h; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.BottomRight: left = cornerX - w; top = cornerY - h; break;
                case Hollowbrook.Checkpoint.CheckpointLayout.Pin.Center: left = cornerX - w / 2; top = cornerY - h / 2; break;
                default: left = cornerX; top = cornerY; break;
            }
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
