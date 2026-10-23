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
            _topLeft = UIFactory.Label(banner, "left", "", 14, UIFactory.PaperInk, TextAnchor.MiddleCenter);
            L.Place(_topLeft.rectTransform, L.Inner.BannerTop);
            // second banner line: the live queue readout (Phase 9.7 top-bar format)


            _topClock = UIFactory.Label(_desk, "clock", "22:00", 20, UIFactory.InkDim, TextAnchor.MiddleCenter);
            Reg("clock", _topClock.rectTransform, L.Clock);


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
        // rule text in the speech log. A null or textless mutation hides the strip again.
        private void ShowBulletin(RuleMutationDef mutation)
        {
            bool active = mutation != null && !string.IsNullOrEmpty(mutation.RuleText);
            _mutationStrip.gameObject.SetActive(active);
            if (!active) return;
            _mutationStrip.text = BulletinShort;         // full rule text: speech entry + RULES NOTE
            SayLine("DISTRICT BULLETIN", mutation.RuleText);
        }

        // Night/clock line in Normal mode; mode/score line in endless and challenge.
        private void ApplyModeGates()
        {
            bool runMode = _gameRoot != null && _gameRoot.Session != null &&
                           _gameRoot.Session.Mode != GameMode.Normal;
            if (_runStats != null) _runStats.gameObject.SetActive(runMode);
            if (_mutationStrip != null) _mutationStrip.gameObject.SetActive(runMode);
        }

        private void RefreshTopBar()
        { 
            ApplyModeGates();
            if (_gameRoot.Session.Mode == GameMode.Normal)
            {
                _topLeft.text = "NIGHT " + _gameRoot.Session.CurrentShift + " · " + (_shift != null ? _shift.DateLabel : "");
                _topClock.text = _gameRoot.ClockString;
            }
            else
            {
                _topLeft.text = (_gameRoot.Session.Mode == GameMode.Endless ? "ENDLESS WATCH" : "CHALLENGE PROTOCOL") +
                                "   ·   PROCESSED " + _gameRoot.Session.RunProcessed;
                _topClock.text = _gameRoot.ClockString;
                float mult = EndlessRules.StreakMultiplier(_gameRoot.Session.Streak) *
                             (_gameRoot.Session.Mode == GameMode.Challenge ? ChallengeRules.ScoreMultiplier : 1f);
                _runStats.text = "SCORE " + _gameRoot.Session.Score + "   ·   STREAK " + _gameRoot.Session.Streak + " (×" + mult.ToString("0.0") + ")";
            }
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
