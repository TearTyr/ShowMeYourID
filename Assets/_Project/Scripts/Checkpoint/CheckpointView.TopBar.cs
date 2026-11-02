using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Top bar: day banner, clock, pause, run stats, mutation bulletin.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        private void BuildTopBar()
        {
            var bar = UIFactory.Panel(_desk, "topbar", UIFactory.PanelAlt);
            Reg("topbar", bar.rectTransform, L.TopBar);
            bar.raycastTarget = false;

            var banner = UIFactory.BorderedPanel(_desk, "daybanner", UIFactory.Paper, UIFactory.Border, 2);
            Reg("banner", banner, L.Banner);
            _topLeft = UIFactory.Label(banner, "left", "", 34, UIFactory.NsChalk, TextAnchor.MiddleLeft);
            _topLeft.font = UIFactory.UiFontBold;
            _topLeft.horizontalOverflow = HorizontalWrapMode.Overflow;   // one line, always
            L.Place(_topLeft.rectTransform, L.Inner.BannerTop);
            // the reference board line is a CONDENSED bold: "NIGHT 1 - MONDAY - NOVEMBER 2"
            // on one 375px line. The shipped face is wide, so the line is squeezed ~72%
            // horizontally - tall glyphs, narrow advance, exactly the reference read.
            _topLeft.rectTransform.localScale = new Vector3(0.72f, 1f, 1f);
            // (no counter of how many visitors are left tonight: the player should not know how long it lasts)


            _topClock = UIFactory.Label(_desk, "clock", "22:00", 34, UIFactory.NsChalk, TextAnchor.MiddleCenter);
            _topClock.font = UIFactory.UiFontBold;
            _topClock.horizontalOverflow = HorizontalWrapMode.Overflow;
            _topClock.rectTransform.localScale = new Vector3(0.75f, 1f, 1f);   // condensed digits
            Reg("clock", _topClock.rectTransform, L.Clock);


            var pause = GridButton(_desk, "pause", "II  PAUSE", () => _gameRoot.TogglePause(true), L.Pause, 12);
            Reg("pause", pause, L.Pause);
        }

        private void RefreshTopBar()
        {
            _topLeft.text = _shift != null && _shift.Lockdown
                ? _shift.NightLabel
                : "NIGHT " + _gameRoot.Session.CurrentShift + " - " + (_shift != null ? _shift.LongDateLabel : "");
            TickClockText();
        }

        // The shift clock model ticks every frame in GameRoot.Update; the label that shows
        // it must tick with it. RefreshHud() (visitor arrivals, verdicts, dialogue) used to
        // be the ONLY writer of _topClock.text, so between events the header kept showing a
        // stale time - the clock looked frozen even though the model ran. One cached string
        // compare per frame keeps this allocation-free.
        private void TickClockText()
        {
            if (_topClock == null || _gameRoot == null) return;
            string s = _gameRoot.ClockString;
            if (s != _lastClockText) { _lastClockText = s; _topClock.text = s; }
        }
    }
}
