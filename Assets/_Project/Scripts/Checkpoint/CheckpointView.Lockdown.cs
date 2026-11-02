using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Lockdown: the clock on the face at the window, the bar that shows it, and what happens when it
    // runs out. One area of the inspection desk; see CheckpointView.cs for the state machine.
    public partial class CheckpointView
    {
        private static readonly L.Region LockdownBarSlot = new L.Region(470, 64, 349, 10);
        private static readonly L.Region LockdownInfoSlot = new L.Region(470, 76, 349, 16);

        private readonly LockdownClock _lockdownClock = new LockdownClock();
        private RectTransform _lockdownBar;
        private Image _lockdownFill;
        private Text _lockdownInfo;

        private bool LockdownOn => _gameRoot != null && _gameRoot.LockdownOn;

        private void BuildLockdownBar()
        {
            _lockdownBar = UIFactory.BorderedPanel(_desk, "lockdownbar", UIFactory.PanelBase, UIFactory.Border, 2);
            L.Place(_lockdownBar, LockdownBarSlot);
            _lockdownFill = UIFactory.Img(_lockdownBar, "fill", ProceduralArt.Get("solid_white"), UIFactory.Ink);
            var fill = _lockdownFill.rectTransform;
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
            _lockdownInfo = UIFactory.Label(_desk, "lockdowninfo", "", 13, UIFactory.Ink, TextAnchor.MiddleCenter);
            L.Place(_lockdownInfo.rectTransform, LockdownInfoSlot);
            ShowLockdownBar(false);
        }

        private void ShowLockdownBar(bool on)
        {
            _lockdownBar.gameObject.SetActive(on);
            _lockdownInfo.gameObject.SetActive(on);
        }

        // Once a frame while the queue runs: count down the face at the window, take them at zero.
        private void TickLockdown(float dt)
        {
            if (!LockdownOn) { if (_lockdownBar.gameObject.activeSelf) ShowLockdownBar(false); return; }
            bool faceAtWindow = _current != null && !_verdictLocked;
            if (!faceAtWindow) _lockdownClock.Stop();
            else if (_lockdownClock.Tick(dt)) { TakeAtWindow(); return; }
            RefreshLockdownBar();
        }

        private void RefreshLockdownBar()
        {
            ShowLockdownBar(true);
            float k = _lockdownClock.Running ? _lockdownClock.Fraction : 0f;
            _lockdownFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(k), 1f);
            _lockdownFill.color = _lockdownClock.Warning ? UIFactory.AccentBright : UIFactory.Ink;
            string seconds = _lockdownClock.Running ? "   " + Mathf.CeilToInt(_lockdownClock.SecondsLeft) + " S" : "";
            _lockdownInfo.text = "MISTAKES " + _gameRoot.Session.LockdownStrikes + "/" + LockdownRules.MaxStrikes + seconds;
        }

        // The time ran out: the thing outside takes whoever is at the window. A student lost is a mistake.
        private void TakeAtWindow()
        {
            var taken = _current;
            _verdictLocked = true;
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.CaptionUrgent, "*the thing outside reaches the glass and takes them*", 3f, null));
            _gameRoot.Audio.PlaySfx("sfx_alarm", 0.7f);
            _gameRoot.OnVisitorTaken(taken);
            if (_gameRoot.State == AppViewState.Ending) { Hide(); return; }
            if (Queue != null) Queue.CommitActiveResolution();   // booked now, like a stamp
            RefreshHud();
            RefreshLockdownBar();
            StartCoroutine(AfterStamp(taken));
        }
    }
}
