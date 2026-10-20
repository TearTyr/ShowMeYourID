using System.Collections.Generic;
using UnityEngine;

namespace Hollowbrook.Checkpoint
{
    // One-time proof that the built hierarchy matches the approved grid: every registered
    // element's actual rect (in DeskFrame coordinates) is compared against the table and any
    // difference over the tolerance is warned about in the editor console. FlowCheck drives
    // CheckAll() headlessly; in a player build this component stays silent.
    public class DeskLayoutAudit : MonoBehaviour
    {
        public struct Entry
        {
            public string Name;
            public RectTransform Rt;
            public CheckpointLayout.Region Expected;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public int Count => _entries.Count;

        public void Register(string name, RectTransform rt, CheckpointLayout.Region expected)
        {
            _entries.Add(new Entry { Name = name, Rt = rt, Expected = expected });
        }

        public List<string> CheckAll(float tolerance = 2f)
        {
            var bad = new List<string>();
            foreach (var e in _entries)
            {
                string problem = CheckEntry(e, tolerance);
                if (problem != null) bad.Add(problem);
            }
            return bad;
        }

        // One entry vs the approved grid: returns what is wrong, or null when it matches.
        private string CheckEntry(Entry e, float tolerance)
        {
            if (e.Rt == null) return e.Name + ": element missing";
            if (!PlacedTopLeft(e.Rt))
                return e.Name + ": not placed through PlaceTL (anchorMin=" + e.Rt.anchorMin +
                       " anchorMax=" + e.Rt.anchorMax + " pivot=" + e.Rt.pivot + ")";
            float x = e.Rt.anchoredPosition.x;
            float y = -e.Rt.anchoredPosition.y;
            float w = e.Rt.sizeDelta.x;
            float h = e.Rt.sizeDelta.y;
            var r = e.Expected;
            if (Mathf.Abs(x - r.X) > tolerance || Mathf.Abs(y - r.Y) > tolerance ||
                Mathf.Abs(w - r.W) > tolerance || Mathf.Abs(h - r.H) > tolerance)
            {
                return e.Name + ": expected (" + r.X + "," + r.Y + " " + r.W + "x" + r.H +
                       ") actual (" + x + "," + y + " " + w + "x" + h + ")";
            }
            return null;
        }

        // Every grid element shares one anchor/pivot setup: top-left, so the table's
        // (x, y, w, h) numbers map 1:1 onto anchoredPosition/sizeDelta.
        private static bool PlacedTopLeft(RectTransform rt)
        {
            var tl = new Vector2(0f, 1f);
            return rt.anchorMin == tl && rt.anchorMax == tl && rt.pivot == tl;
        }

        private void Start()
        {
#if UNITY_EDITOR
            var bad = CheckAll();
            foreach (var line in bad) Debug.LogWarning("[LayoutAudit] " + line);
            Debug.Log("[LayoutAudit] " + gameObject.name + ": " + (_entries.Count - bad.Count) + "/" +
                      _entries.Count + " elements match the approved grid" +
                      (bad.Count == 0 ? " — zero warnings" : " — " + bad.Count + " WARNING(S)"));
#endif
        }
    }
}
