using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Bootstrap;
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
            bool failed = Session.Mode == GameMode.Normal && Session.CurrentShift < ShiftPlan.TotalShifts &&
                          Session.ShiftBreaches > 0;
            if (failed) Session.RecordShiftFailure(ShiftFailure.Breach);
            FinishShift();
        }

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
            Session.RecordShiftFailure(kind);
            PlayFailure(() =>
            {
                ShiftFailing = false;
                FinishShift();
            });
        }

        // The scare only plays out on patrol or in a peek. Fainting at the desk just ends the night.
        private void PlayFailure(System.Action then)
        {
            bool onPatrol = State == AppViewState.Exploration || State == AppViewState.RoomPeek;
            if (Scares != null && onPatrol) Scares.FailureSequence(AnomalyBehaviorKind.Blink, then);
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

            // nights 1 to 4 end straight at the main menu; Continue starts the next one
            if (Session.Mode == GameMode.Normal && Session.CurrentShift <= ShiftPlan.TotalShifts)
            {
                SetState(AppViewState.MainMenu);
                QuitToMenu(false);
                return;
            }

            // the final night: the results first, and their button leads to the ending
            _endingAfterResults = eval.Ending != EndingType.None ? eval : null;
            SetState(AppViewState.ShiftResults);
            if (ShiftResults != null) ShiftResults.Populate(Session, CurrentShiftDef);
            Audio.PlayMusic("");
        }

        /// <summary>Night 6 opens once the story has been finished (a Win or the Secret ending), read
        /// from the save so it is right on the main menu too.</summary>
        public bool Night6Unlocked
        {
            get
            {
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
            if (Session.CurrentShift > ShiftPlan.TotalShifts) { BootToMainMenu(); return; }
            BeginShift(Session.CurrentShift);
        }

        public void TriggerEnding(EndingRules.Evaluation eval)
        {
            if (Ending == null)
            {
                // the ending screen lives in the desk scene: go there first (e.g. a blackout on patrol)
                if (Views != null) Views.StopAdminHunt();
                QueueSceneAction(() => TriggerEnding(eval));
                Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
                return;
            }
            string key = eval.Ending == EndingType.Secret ? "secret" : (eval.Ending == EndingType.Win ? "win" : "lose");
            if (Session.Mode == GameMode.Normal && !Records.EndingsSeen.Contains(key)) Records.EndingsSeen.Add(key);
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.Ending);
            if (Ending != null) Ending.Populate(eval, Session, Records);
            Audio.PlayMusic("end_" + key);
            Audio.PlaySfx(eval.Ending == EndingType.Lose ? "sfx_alarm" : "sfx_bell");
        }
    }
}
