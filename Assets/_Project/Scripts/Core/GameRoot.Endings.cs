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

        /// <summary>The END SHIFT button. Clocking out with an anomaly let in fails the night (black
        /// screen, a face, the jumpscare) on nights 1-4; anything else just ends the shift.</summary>
        public void RequestEndShift()
        {
            bool failed = Session.Mode == GameMode.Normal && Session.CurrentShift < ShiftPlan.TotalShifts &&
                          Session.ShiftBreaches > 0;
            if (failed) FailShift(ShiftFailure.Breach);
            else FinishShift();
        }

        /// <summary>The sanity bar hit zero. Nights 1-4: the custodian blacks out and the shift ends.
        /// The final night is the unique one: a blackout there is the Lose result.</summary>
        public void OnSanityDepleted()
        {
            if (ShiftFailing || Session.Mode != GameMode.Normal) return;
            if (Session.CurrentShift < ShiftPlan.TotalShifts) { FailShift(ShiftFailure.Blackout); return; }
            ShiftFailing = true;
            var lose = new EndingRules.Evaluation
            {
                Ending = EndingType.Lose, ReasonKey = "blackout",
                ReasonText = "Your sanity gave out in the dark. The last thing you remember is the lights going — and the corridor was not empty.",
            };
            PlayFailure(() => { ShiftFailing = false; TriggerEnding(lose); });
        }

        // A failed night (nights 1-4): record how and what it will cost, play the scene, then close
        // the shift out. From the patrol the desk scene has to load first — the results screen lives there.
        private void FailShift(ShiftFailure kind)
        {
            if (ShiftFailing) return;
            ShiftFailing = true;
            Session.RecordShiftFailure(kind);
            PlayFailure(() =>
            {
                ShiftFailing = false;
                if (State == AppViewState.Exploration || State == AppViewState.RoomPeek)
                {
                    QueueSceneAction(() => { Bus.Publish(GameEvent.Of(EventIds.PatrolEnded)); FinishShift(); });
                    Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
                }
                else FinishShift();
            });
        }

        private void PlayFailure(System.Action then)
        {
            if (Scares != null) Scares.FailureSequence(AnomalyBehaviorKind.Blink, then);
            else then();
        }

        public void FinishShift()
        {
            Bus.Publish(GameEvent.Of(EventIds.ShiftEnded, CurrentShiftDef));
            Session.CompleteShift();
            // the finished shift's line must never leak into the next one: the queue
            // resets here, so the save written below resumes as "next shift, not started"
            Queue = new CheckpointQueue();
            Session.QueueState = null;
            PersistRun();

            var eval = Session.EvaluateEnding();
            if (eval.Ending == EndingType.None && Config != null && Config.exhibitionMode && Session.CompletedShifts >= 1)
            {
                Session.Breaches += Session.Pool.Pending;         // booth night closes its books too
                eval = EndingRules.EvaluateExhibition(Session);   // booth cut closes after one night
            }
            if (eval.Ending != EndingType.None) { TriggerEnding(eval); return; }

            SetState(AppViewState.ShiftResults);
            if (ShiftResults != null) ShiftResults.Populate(Session, CurrentShiftDef);
            Audio.PlayMusic("");
        }

        public void AdvanceToNextShift()
        {
            if (Session.CurrentShift > ShiftPlan.TotalShifts) { BootToMainMenu(); return; }
            BeginShift(Session.CurrentShift);
        }

        public void FinishRun() // endless/challenge time-out
        {
            if (Session.Mode == GameMode.Endless && Session.Score > Records.EndlessBest) Records.EndlessBest = Session.Score;
            if (Session.Mode == GameMode.Challenge && Session.Score > Records.ChallengeBest) Records.ChallengeBest = Session.Score;
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.EndlessResults);
            if (Ending != null) Ending.PopulateRunResults(Session, Records);
            Audio.PlayMusic(Session.Score > 0 ? "end_win" : "end_lose");
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
            if (eval.ReasonKey != "exhibition" && !Records.EndingsSeen.Contains(key)) Records.EndingsSeen.Add(key);
            Session.RunFinished = true;
            PersistRun();
            SetState(AppViewState.Ending);
            if (Ending != null) Ending.Populate(eval, Session, Records);
            Bus.Publish(GameEvent.Of(EventIds.EndingReached, eval));
            Audio.PlayMusic("end_" + key);
            Audio.PlaySfx(eval.Ending == EndingType.Lose ? "sfx_alarm" : "sfx_bell");
        }
    }
}
