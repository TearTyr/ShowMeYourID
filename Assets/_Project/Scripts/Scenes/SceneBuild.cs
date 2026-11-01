using System.Collections.Generic;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // The scene files are the editable copy of the game's screens (Hollowbrook > Set Up Project
    // saves them there). While a scene is being set up (between Begin and End) the helpers that
    // make UI objects, UIFactory.NewUI and GameCore.NewCanvas, first look for an object that is
    // already in the scene with that name under that parent, and use it instead of making a new
    // one. The same code therefore either fills an empty scene (the first time) or finds what is
    // already there (every time after). Objects made after End (rows made while playing) are
    // always new.
    public static class SceneBuild
    {
        // Objects the code makes in one place and then moves to another (the papers into their layer,
        // the grain and vignette on top of the desk). They are looked for below the parent too.
        private static readonly HashSet<string> MovedNames = new HashSet<string>
        {
            "record", "idcard", "slip", "grain", "vignette",
        };

        private static readonly HashSet<Transform> Taken = new HashSet<Transform>();
        private static readonly List<GameObject> SwitchedOn = new List<GameObject>();

        /// <summary>True while a scene's screens are being set up.</summary>
        public static bool Active { get; private set; }

        public static void Begin()
        {
            Active = true;
            Taken.Clear();
            SwitchedOn.Clear();
        }

        /// <summary>Finish: everything that was switched off in the scene file is switched off again.</summary>
        public static void End()
        {
            Active = false;
            foreach (var go in SwitchedOn)
                if (go != null) go.SetActive(false);
            SwitchedOn.Clear();
            Taken.Clear();
        }

        /// <summary>The next child of <paramref name="parent"/> with this name that was not handed out
        /// yet, or null. Two siblings with one name are handed out in order. An object that is switched
        /// off in the scene file (every screen the code hides) is switched on while the code sets it up,
        /// so that searches through its children find everything, and off again at the end.</summary>
        public static Transform Reuse(Transform parent, string name)
        {
            if (!Active || parent == null) return null;
            Transform found = null;
            for (int i = 0; i < parent.childCount && found == null; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name && !Taken.Contains(child)) found = child;
            }
            if (found == null && MovedNames.Contains(name)) found = FindBelow(parent, name);
            if (found == null) return null;

            Taken.Add(found);
            if (!found.gameObject.activeSelf)
            {
                found.gameObject.SetActive(true);
                SwitchedOn.Add(found.gameObject);
            }
            return found;
        }

        private static Transform FindBelow(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name && !Taken.Contains(child)) return child;
                var deeper = FindBelow(child, name);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}

namespace Hollowbrook
{
    // In the parent namespace so every Hollowbrook.* file sees it without a using line.
    public static class GameObjectUtil
    {
        /// <summary>The component if the object already has one, otherwise a new one. A scene that was
        /// set up in the editor already holds its components; a scene built from nothing does not.</summary>
        public static T GetOrAdd<T>(this GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
