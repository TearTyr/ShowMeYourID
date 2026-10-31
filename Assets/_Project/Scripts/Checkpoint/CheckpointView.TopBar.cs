using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
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
            // horizontally — tall glyphs, narrow advance, exactly the reference read.
            _topLeft.rectTransform.localScale = new Vector3(0.72f, 1f, 1f);
            // second banner line: the live queue readout (Phase 9.7 top-bar format)


            _topClock = UIFactory.Label(_desk, "clock", "22:00", 30, UIFactory.NsChalk, TextAnchor.MiddleCenter);
            _topClock.font = UIFactory.UiFontBold;
            _topClock.horizontalOverflow = HorizontalWrapMode.Overflow;
            _topClock.rectTransform.localScale = new Vector3(0.75f, 1f, 1f);   // condensed digits
            Reg("clock", _topClock.rectTransform, L.Clock);

            // the night counter sits right beside the clock: which of the five nights this is
            _topNight = UIFactory.Label(_desk, "nightcounter", "", 20, UIFactory.NsChalk, TextAnchor.MiddleRight);
            _topNight.font = UIFactory.UiFontBold;
            _topNight.horizontalOverflow = HorizontalWrapMode.Overflow;
            _topNight.raycastTarget = false;
            Reg("nightcounter", _topNight.rectTransform, L.NightCounter);


            // under the night banner: how many are waiting and how many are still to go tonight
            _topQueue = UIFactory.Label(_desk, "queuecount", "", 13, UIFactory.NsChalk, TextAnchor.MiddleLeft);
            _topQueue.font = UIFactory.UiFontBold;
            _topQueue.horizontalOverflow = HorizontalWrapMode.Overflow;
            _topQueue.raycastTarget = false;
            L.Place(_topQueue.rectTransform, L.QueueCount);

            var pause = GridButton(_desk, "pause", "II  PAUSE", () => _gameRoot.TogglePause(true), L.Pause, 12);
            Reg("pause", pause, L.Pause);

            _runStats = UIFactory.Label(_desk, "runstats", "", 13, UIFactory.Ink, TextAnchor.MiddleCenter);
            Reg("runstats", _runStats.rectTransform, L.RunStats);
            _runStats.gameObject.SetActive(false);

            var mutBg = UIFactory.Panel(_desk, "mutationbg", new Color(0.04f, 0.04f, 0.05f, 0.95f));
            Reg("mutation", mutBg.rectTransform, L.Mutation);
            mutBg.raycastTarget = false;
            _mutationStrip = UIFactory.Label(mutBg.rectTransform, "mutation", "", 13,
                new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(_mutationStrip.rectTransform);
            _mutationStrip.gameObject.SetActive(false);
        }

        // The challenge-mode district bulletin: a short strip on the desk plus the full
        // rule text as a HUD toast (the speech bubble belongs to the student alone).
        // A null or textless mutation hides the strip again.
        private void ShowBulletin(RuleMutationDef mutation)
        {
            bool active = mutation != null && !string.IsNullOrEmpty(mutation.RuleText);
            _mutationStrip.gameObject.SetActive(active);
            if (!active) return;
            _mutationStrip.text = BulletinShort;         // full rule text: toast + RULES NOTE
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                "DISTRICT BULLETIN — " + mutation.RuleText));
        }

        // Night/clock line in Normal mode; mode/score line in endless and challenge.
        private void ApplyModeGates()
        {
            bool runMode = _gameRoot != null && _gameRoot.Session != null &&
                           _gameRoot.Session.Mode != GameMode.Normal;
            if (_runStats != null) _runStats.gameObject.SetActive(runMode);
            if (_topNight != null) _topNight.gameObject.SetActive(!runMode);   // run modes have no nights
            if (_mutationStrip != null) _mutationStrip.gameObject.SetActive(runMode);
        }

        private void RefreshTopBar()
        { 
            ApplyModeGates();
            RefreshQueueCount();
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                _topLeft.text = "NIGHT " + _gameRoot.Session.CurrentShift + " - " + (_shift != null ? _shift.LongDateLabel : "");
                if (_topNight != null)
                    _topNight.text = "NIGHT " + _gameRoot.Session.CurrentShift + "/" + ShiftPlan.TotalShifts;
                TickClockText();
            }
            else
            {
                _topLeft.text = (_gameRoot.Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL") +
                                "   ·   PROCESSED " + _gameRoot.Session.RunProcessed;
                TickClockText();
                float mult = EndlessRules.StreakMultiplier(_gameRoot.Session.Streak) *
                             (_gameRoot.Session.Mode == GameMode.Challenge ? ChallengeRules.ScoreMultiplier : 1f);
                _runStats.text = "SCORE " + _gameRoot.Session.Score + "   ·   STREAK " + _gameRoot.Session.Streak + " (×" + mult.ToString("0.0") + ")";
            }
        }

        // "WAITING 2   ·   4 TO GO TONIGHT" - blank until the shift has started and in run modes.
        private void RefreshQueueCount()
        {
            if (_topQueue == null) return;
            bool show = Queue != null && Queue.Started && !Queue.InfiniteSource &&
                        _gameRoot.Session.Mode == GameMode.Normal;
            _topQueue.gameObject.SetActive(show);
            if (!show) return;
            int toGo = Mathf.Max(0, Queue.EffectiveTotal - Queue.ProcessedCount);
            _topQueue.text = "WAITING " + Queue.WaitingCount + "   ·   " + toGo + " TO GO TONIGHT";
        }

        // The shift clock model ticks every frame in GameRoot.Update; the label that shows
        // it must tick with it. RefreshHud() (visitor arrivals, verdicts, dialogue) used to
        // be the ONLY writer of _topClock.text, so between events the header kept showing a
        // stale time — the clock looked frozen even though the model ran. One cached string
        // compare per frame keeps this allocation-free.
        private void TickClockText()
        {
            if (_topClock == null || _gameRoot == null) return;
            string s = _gameRoot.ClockString;
            if (s != _lastClockText) { _lastClockText = s; _topClock.text = s; }
        }

        public void OnRunTimerTick(float secondsLeft)
        {
            if (_runStats == null || !_runStats.gameObject.activeInHierarchy) return;
            int m = (int)(secondsLeft / 60f), s = (int)(secondsLeft % 60f);
            _runStats.text = "TIME " + m + ":" + s.ToString("D2") + "   ·   SCORE " + _gameRoot.Session.Score +
                             "   ·   STREAK ×" + EndlessRules.StreakMultiplier(_gameRoot.Session.Streak).ToString("0.0");
            _runStats.color = secondsLeft < 30f ? UIFactory.AccentBright : UIFactory.Ink;
        }
    }
}
