using System;
using System.Collections.Generic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Scenes
{
    /// <summary>One screen object as the code made it: where it sits, its colour, picture and text.</summary>
    [Serializable]
    public class SceneEditEntry
    {
        public string Path;                 // "UICanvas/MainMenuView/bg" (a second sibling of one name gets "#1")
        public bool Active;
        public Vector2 AnchorMin, AnchorMax, Pivot, Position, Size;
        public Vector3 Scale;
        public bool HasColor;
        public Color Color;
        public string Sprite;               // the picture's name ("" = none)
        public bool HasText;
        public string Text;
        public int FontSize;
    }

    // Sits on the scene's SceneRoot object. Two jobs:
    //  1. When the scene is set up in the editor (Hollowbrook > Set Up Project), it stores what the code
    //     made, the BASELINE.
    //  2. When the scene loads, it compares the objects in the scene file with that baseline. Whatever
    //     differs is something the designer changed in the editor. The code then runs and sets up the
    //     screens as usual, and afterwards the designer's changes are put back on top. So the scene
    //     wins where it was edited, and everything else keeps following the code (screen sizes, text
    //     scale, data). Moving, resizing, recolouring, re-texting or swapping the picture of an
    //     object, or hiding one inside a screen, is kept. Renaming objects is not: the match is by name.
    public class SceneEdits : MonoBehaviour
    {
        public List<SceneEditEntry> Baseline = new List<SceneEditEntry>();

        /// <summary>The scene was set up in the editor (the baseline is stored).</summary>
        public bool IsBaked => Baseline != null && Baseline.Count > 0;

        // Whole screens (canvas children) are shown and hidden by the code; a tick left on or off
        // while looking at them in the editor must never leak into the game.
        private const int FirstDepthWhereActiveCounts = 3;
        private const float Tolerance = 0.01f;

        public void StoreBaseline(Transform sceneRoot)
        {
            Baseline = new List<SceneEditEntry>();
            foreach (var c in Capture(sceneRoot)) Baseline.Add(c.Entry);
        }

        /// <summary>Editor, when a scene that was set up before is set up again: objects the designer
        /// changed keep their old baseline (so their changes are still seen as changes), every other
        /// object gets a fresh one (so new code values are not mistaken for changes).</summary>
        public void RefreshBaseline(Transform sceneRoot, Plan keep)
        {
            var old = new Dictionary<string, SceneEditEntry>();
            foreach (var e in Baseline) old[e.Path] = e;
            Baseline = new List<SceneEditEntry>();
            foreach (var c in Capture(sceneRoot))
            {
                SceneEditEntry before;
                bool keepOld = keep != null && keep.Contains(c.Entry.Path) && old.TryGetValue(c.Entry.Path, out before);
                Baseline.Add(keepOld ? old[c.Entry.Path] : c.Entry);
            }
        }

        // ---------------------------------------------------------------- capture

        public struct Captured
        {
            public Transform Node;
            public int Depth;               // the canvases are 1, the screens 2, what is on them 3 and up
            public SceneEditEntry Entry;
            public Sprite Sprite;           // the picture itself (not stored, put back as is)
        }

        public static List<Captured> Capture(Transform sceneRoot)
        {
            var list = new List<Captured>();
            Collect(sceneRoot, "", 1, list);
            return list;
        }

        private static void Collect(Transform parent, string parentPath, int depth, List<Captured> into)
        {
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                int before;
                seen.TryGetValue(child.name, out before);
                seen[child.name] = before + 1;
                string path = (parentPath.Length == 0 ? "" : parentPath + "/") + child.name + (before > 0 ? "#" + before : "");
                var rt = child as RectTransform;
                if (rt != null) into.Add(Read(rt, path, depth));
                Collect(child, path, depth + 1, into);
            }
        }

        private static Captured Read(RectTransform rt, string path, int depth)
        {
            var e = new SceneEditEntry
            {
                Path = path, Active = rt.gameObject.activeSelf,
                AnchorMin = rt.anchorMin, AnchorMax = rt.anchorMax, Pivot = rt.pivot,
                Position = rt.anchoredPosition, Size = rt.sizeDelta, Scale = rt.localScale,
            };
            var c = new Captured { Node = rt, Depth = depth, Entry = e };
            var graphic = rt.GetComponent<Graphic>();
            if (graphic != null) { e.HasColor = true; e.Color = graphic.color; }
            var img = rt.GetComponent<Image>();
            if (img != null && img.sprite != null) { e.Sprite = img.sprite.name; c.Sprite = img.sprite; }
            var txt = rt.GetComponent<Text>();
            if (txt != null) { e.HasText = true; e.Text = txt.text; e.FontSize = txt.fontSize; }
            c.Entry = e;
            return c;
        }

        // ---------------------------------------------------------------- the plan

        /// <summary>The designer's changes found in a scene, ready to be put back after the code ran.</summary>
        public class Plan
        {
            private struct Item
            {
                public Captured Now;
                public bool Rect, Active, Color, Sprite, Text, FontSize;
            }

            private readonly List<Item> _items = new List<Item>();
            private readonly HashSet<string> _paths = new HashSet<string>();

            public int Count => _items.Count;

            /// <summary>True when the object at this path was changed by the designer.</summary>
            public bool Contains(string path) => _paths.Contains(path);

            internal void Add(Captured now, SceneEditEntry baseline)
            {
                var n = now.Entry;
                var item = new Item { Now = now };
                item.Rect = Differs(n.AnchorMin, baseline.AnchorMin) || Differs(n.AnchorMax, baseline.AnchorMax) ||
                            Differs(n.Pivot, baseline.Pivot) || Differs(n.Position, baseline.Position) ||
                            Differs(n.Size, baseline.Size) || Differs(n.Scale, baseline.Scale);
                item.Active = now.Depth >= FirstDepthWhereActiveCounts && n.Active != baseline.Active;
                item.Color = n.HasColor && baseline.HasColor && ColorDiffers(n.Color, baseline.Color);
                item.Sprite = (n.Sprite ?? "") != (baseline.Sprite ?? "") && now.Sprite != null;
                item.Text = n.HasText && baseline.HasText && n.Text != baseline.Text;
                item.FontSize = n.HasText && baseline.HasText && n.FontSize != baseline.FontSize;
                if (item.Rect || item.Active || item.Color || item.Sprite || item.Text || item.FontSize)
                {
                    _items.Add(item);
                    _paths.Add(n.Path);
                }
            }

            /// <summary>Put the designer's changes back. Call after the code has set the screens up.</summary>
            public void Apply()
            {
                foreach (var item in _items)
                {
                    var rt = item.Now.Node as RectTransform;
                    if (rt == null) continue;
                    var n = item.Now.Entry;
                    if (item.Rect)
                    {
                        rt.anchorMin = n.AnchorMin; rt.anchorMax = n.AnchorMax; rt.pivot = n.Pivot;
                        rt.anchoredPosition = n.Position; rt.sizeDelta = n.Size; rt.localScale = n.Scale;
                    }
                    if (item.Active) rt.gameObject.SetActive(n.Active);
                    if (item.Color)
                    {
                        var g = rt.GetComponent<Graphic>();
                        if (g != null) g.color = n.Color;
                    }
                    if (item.Sprite)
                    {
                        var img = rt.GetComponent<Image>();
                        if (img != null) img.sprite = item.Now.Sprite;
                    }
                    var txt = rt.GetComponent<Text>();
                    if (txt != null && item.Text) txt.text = n.Text;
                    if (txt != null && item.FontSize) UIFactory.SetBaseFontSize(txt, n.FontSize);
                }
            }

            private static bool Differs(Vector2 a, Vector2 b) =>
                Mathf.Abs(a.x - b.x) > Tolerance || Mathf.Abs(a.y - b.y) > Tolerance;

            private static bool Differs(Vector3 a, Vector3 b) =>
                Mathf.Abs(a.x - b.x) > Tolerance || Mathf.Abs(a.y - b.y) > Tolerance || Mathf.Abs(a.z - b.z) > Tolerance;

            private static bool ColorDiffers(Color a, Color b) =>
                Mathf.Abs(a.r - b.r) > 0.004f || Mathf.Abs(a.g - b.g) > 0.004f ||
                Mathf.Abs(a.b - b.b) > 0.004f || Mathf.Abs(a.a - b.a) > 0.004f;
        }

        /// <summary>Read the scene as it is now and find what the designer changed. Call BEFORE the code
        /// sets the screens up. An empty plan when the scene was never set up in the editor.</summary>
        public static Plan Begin(GameObject sceneRoot)
        {
            var plan = new Plan();
            if (sceneRoot == null) return plan;
            var edits = sceneRoot.GetComponent<SceneEdits>();
            if (edits == null || !edits.IsBaked) return plan;

            var baseline = new Dictionary<string, SceneEditEntry>();
            foreach (var e in edits.Baseline) baseline[e.Path] = e;
            foreach (var now in Capture(sceneRoot.transform))
            {
                SceneEditEntry before;
                if (baseline.TryGetValue(now.Entry.Path, out before)) plan.Add(now, before);
            }
            if (plan.Count > 0) Debug.Log("[Scene] " + plan.Count + " object(s) changed in the scene file, kept on top of the code.");
            return plan;
        }
    }
}
