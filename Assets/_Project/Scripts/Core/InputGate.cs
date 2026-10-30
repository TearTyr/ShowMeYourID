using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowbrook.Core
{
    /// <summary>One owner per key per frame. Esc and M are listened to by several views
    /// (sheets, map, pause, desk, patrol) and Unity does not guarantee their Update order,
    /// so the first handler to run consumes the key for that frame and the rest stand down.
    /// Fixes the "one Esc closes Settings AND unpauses the game" class of bug (issue #6).</summary>
    public static class InputGate
    {
        private static int _escFrame = -1;
        private static int _mFrame = -1;

        public static bool TryEscape()
        {
            int f = Time.frameCount;
            if (f == _escFrame) return false;
            _escFrame = f;
            return true;
        }

        public static bool TryM()
        {
            int f = Time.frameCount;
            if (f == _mFrame) return false;
            _mFrame = f;
            return true;
        }

        /// <summary>True while the pointer is over any UI that takes clicks (the pause button,
        /// the peek's step-back strip, a sheet): those clicks keep their own meaning.</summary>
        public static bool PointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        /// <summary>A mouse press that may drive the flashlight (0 = left: on/off, 1 = right:
        /// narrow beam): the press itself, and never one that lands on UI.</summary>
        public static bool FlashlightClick(int button) =>
            Input.GetMouseButtonDown(button) && !PointerOverUi();

        /// <summary>Headless harnesses drive handlers by hand inside one "frame"; bump the
        /// frame between simulated key presses so each gets a fresh gate.</summary>
        public static void NextFrame() { _escFrame = -1; _mFrame = -1; }
    }
}
