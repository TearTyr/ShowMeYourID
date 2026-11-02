using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Scenes;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: how a night ends - clocking out, a failed night, blackout, the end of a
    // shift or a run, and the ending screens.
    public partial class GameRoot
    {
        // ---- shift / run completion ----

        /// <summary>True while a failed night's scene is playing (input and the sanity tick stand down).</summary>
        public bool ShiftFailing { get; private set; }

        private EndingRules.Evaluation _endingAfterResults;

        /// <summary>The END SHIFT button. Clocking out with an anomaly let in fails the night on nights
        /// 1-4: it is recorded and paid for the next night (and said in that night's intro), but there
        /// is no scare. Scares belong to patrol and peeking.</summary>
        public void RequestEndShift()
        {
            if (!NightResolved) return;
            if (LogRequired) Session.GradeNight(NightIncidents());   // the log is checked now, tomorrow's Rules sheet tells
            bool failed = Session.Mode == GameMode.Normal && Session.CurrentShift < ShiftPlan.TotalShifts &&
                          Session.ShiftBreaches > 0;
            if (failed) Session.RecordShiftFailure(ShiftFailure.Breach);
            FinishShift();
        }

        /// <summary>Nights 1 to 4 end with a signed Security Log. Night 5, night 6 and other modes have no log duty.</summary>
        public bool LogRequired => Session != null && Session.Mode == GameMode.Normal && Session.CurrentShift < ShiftPlan.TotalShifts;

        public bool LogReady => !LogRequired || Session.LogSigned;

        // what the third box counts: students who never reached the window, and things driven out of the peek rooms
        private int NightIncidents()
        {
            int driven = 0;
            for (int bits = Session.PeekAnomaliesGone; bits != 0; bits &= bits - 1) driven++;
            return (Queue != null ? Queue.MissingIncidents : 0) + driven;
        }

        /// <summary>A night with the administrator closes only once he has been refused and survived.
        /// Every other night has nothing to resolve.</summary>
        public bool NightResolved => CurrentShiftDef == null || !CurrentShiftDef.HasAdministrator ||
                                     (Session.AdminRejected && Session.Hunt == HuntState.Won);

        /// <summary>The sanity bar hit zero. Nights 1-4: the custodian blacks out and the shift ends.
        /// The final night is the unique one: a blackout there is the Lose result.</summary>
        public void OnSanityDepleted()
        {
            if (ShiftFailing) return;
            if (Session.Mode == GameMode.Normal && Session.CurrentShift < ShiftPlan.TotalShifts)
            { FailShift(ShiftFailure.Blackout); return; }
            ShiftFailing = true;
            var lose = new EndingRules.Evaluation
            {
                Ending = EndingType.Lose, ReasonKey = "blackout",
                ReasonText = "Your sanity gave out in the dark. The last thing you remember is the lights going - and the corridor was not empty.",
            };
            PlayFailure(() => { ShiftFailing = false; TriggerEnding(lose); });
        }

        // A blackout (nights 1-4): record it, play the scene, then close the shift out, which goes
        // straight back to the main menu.
        private void FailShift(ShiftFailure kind)
        {
            if (ShiftFailing) return;
            ShiftFailing = true;
            PlayFailure(() =>
            {
                ShiftFailing = false;
                ReplayFailedNight(kind);
            });
        }

        // A night that was not finished is not over: the save goes back to the start of that night,
        // with its penalty booked, and Continue plays it again.
        private void ReplayFailedNight(ShiftFailure kind)
        {
            int breaches = Session.ShiftBreaches;
            if (_nightStart != null) Session = _nightStart.Session;
            else Session.RewindNight();
            Session.ShiftBreaches = breaches;      // the penalty counts what was let in
            Session.RecordShiftFailure(kind);
            Session.ShiftBreaches = 0;
            Session.QueueState = null;
            _shiftMinutes = 0f;
            Queue = new CheckpointQueue();
            PersistRun();
            SetState(AppViewState.MainMenu);
            QuitToMenu(false);
        }

        // Called when a night begins. A fresh night is saved at once and kept in its own file, so a
        // failed attempt always goes back to the true start, even after a quit and Continue half way
        // through. A resumed night reads that file back; a copy from another run or night is ignored.
        private void RememberNightStart(bool isResume)
        {
            if (Session.Mode != GameMode.Normal) return;
            if (isResume)
            {
                _nightStart = Saves.ReadNightStart();
                bool sameNight = _nightStart != null && _nightStart.Session.Seed == Session.Seed &&
                                 _nightStart.Session.CurrentShift == Session.CurrentShift;
                if (!sameNight) _nightStart = null;
                return;
            }
            PersistRun();
            _nightStart = Saves.Read();
            Saves.WriteNightStart(_nightStart);
        }

        // The run is lost, but the night is not: the save goes back to the start of the night that was
        // lost (with the records from now), so Continue offers that same night again. A run that began
        // the night already over the lose limit could only lose again, so it stays finished instead:
        // the ending then offers a new game, not a retry that cannot be won.
        private void SaveLostNight()
        {
            PersistRun();
            if (Session.Mode != GameMode.Normal) return;
            var night = _nightStart;
            if (night == null)
            {
                night = Saves.Read();
                if (night == null) return;
                night.Session.RewindNight();
            }
            if (night.Session.LoseThresholdReached) return;
            night.Records = Records;
            night.Settings = Settings.Current.Clone();
            Saves.Write(night);
        }

        // The scare only plays out on patrol or in a peek. Fainting at the desk just ends the night.
        private void PlayFailure(System.Action then)
        {
            bool onPatrol = State == AppViewState.Exploration || State == AppViewState.RoomPeek;
            if (Scares != null && onPatrol) Scares.FailureSequence(BehaviorProfile.ArtKey(AnomalyBehaviorKind.Blink), then);
            else then();
        }

        public void FinishShift()
        {
            Session.CompleteShift();
            // the finished shift's line must never leak into the next one: the queue
            // resets here, so the save written below resumes as "next shift, not started"
            Queue = new CheckpointQueue();
            Session.QueueState = null;
            PersistRun();

            var eval = Session.EvaluateEnding();

            // night 6 is one night: straight to its ending
            if (Session.Mode == GameMode.Challenge) { TriggerEnding(eval); return; }

            // nights 1 to 4 run on without the menu: the screen dims and the next night starts. The save
            // above already holds the next night, so quitting during the dim and Continuing is safe.
            if (Session.Mode == GameMode.Normal && Session.CurrentShift <= ShiftPlan.TotalShifts)
            {
                int next = Session.CurrentShift;
                System.Action begin = () => BeginShift(next);
                if (Loading != null) Loading.DimThen(GameRules.Current.NightDimSeconds, begin);
                else begin();
                return;
            }

            // the final night: the results first, and their button leads to the ending
            _endingAfterResults = eval.Ending != EndingType.None ? eval : null;
            SetState(AppViewState.ShiftResults);
            if (ShiftResults != null) ShiftResults.Populate(Session, CurrentShiftDef);
            Audio.PlayMusic("");
        }

        /// <summary>Night 6 opens once the story has been finished (a Win or the Secret ending), read
        /// from the save so it is right on the main menu too, or when the secret code has opened it.</summary>
        public bool Night6Unlocked
        {
            get
            {
                if (UnlockCodeStage >= 1) return true;
                var save = Saves != null ? Saves.Read() : null;
                var seen = save != null && save.Records != null ? save.Records.EndingsSeen : null;
                return seen != null && (seen.Contains("win") || seen.Contains("secret"));
            }
        }

        public void AdvanceToNextShift()
        {
            if (_endingAfterResults != null)
            {
                var ending = _endingAfterResults;
                _endingAfterResults = null;
                TriggerEnding(ending);
                return;
            }
            // the run is over with no ending to show: load the menu scene (BootToMainMenu alone would
            // stay in this scene, which has no menu, and strand the player)
            if (Session.CurrentShift > ShiftPlan.TotalShifts) { QuitToMenu(false); return; }
            BeginShift(Session.CurrentShift);
        }

        public void TriggerEnding(EndingRules.Evaluation eval)
        {
            if (Ending == null)
            {
                // the ending screen lives in the desk scene: go there first (e.g. a blackout on patrol)
                if (Views != null) Views.StopAdminHunt();
                QueueSceneAction(() => TriggerEnding(eval));
                Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Checkpoint);
                return;
            }
            string key = eval.Ending == EndingType.Secret ? "secret" : (eval.Ending == EndingType.Win ? "win" : "lose");
            if (Session.Mode == GameMode.Normal && !Records.EndingsSeen.Contains(key)) Records.EndingsSeen.Add(key);
            if (Session.Mode == GameMode.Challenge && eval.Ending == EndingType.Win) MarkNight6Beaten();   // opens Lockdown
            Session.RunFinished = true;
            if (eval.Ending == EndingType.Lose) SaveLostNight(); else PersistRun();
            SetState(AppViewState.Ending);
            if (Ending != null) Ending.Populate(eval, Session, Records);
            Audio.PlayMusic("end_" + key);
            Audio.PlaySfx(eval.Ending == EndingType.Lose ? "sfx_alarm" : "sfx_bell");
        }
    }
}
