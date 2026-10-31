using System.Collections.Generic;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Night 5, after the administrator is turned away: he does not leave, he hunts through the
    // school (rules in GameLogic/AdminHunt, scene driver in World/AdminHuntDirector). This file
    // is the flow around it: starting the hunt from the desk, ending it in a win or a loss, and
    // resuming it after Continue. One part of GameRoot, split out so each file stays readable.
    public partial class GameRoot
    {
        /// <summary>True from the DENY stamp until he is driven off or reaches the server door.</summary>
        public bool HuntActive => Session != null && Session.Hunt == HuntState.Active;

        /// <summary>Change the HUD objective line (and remember it for HUDs built later).</summary>
        public void SetObjective(string text)
        {
            ObjectiveText = text;
            Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, text));
        }

        /// <summary>The tuning numbers from GameConfig.asset (defaults when there is no asset).</summary>
        public HuntTuning BuildHuntTuning()
        {
            var t = HuntTuning.Default;
            if (Config == null) return t;
            t.StunsToWin = Config.huntStunsToWin;
            t.ServerSeconds = Config.huntServerSeconds;
            t.HideMinSeconds = Config.huntHideMinSeconds;
            t.HideMaxSeconds = Mathf.Max(Config.huntHideMinSeconds, Config.huntHideMaxSeconds);
            t.TrackMaxSeconds = Config.huntTrackMaxSeconds;
            t.HoldSeconds = Config.huntHoldSeconds;
            t.HeadRadius = Config.huntHeadRadius;
            t.ChargeSeconds = Config.huntChargeSeconds;
            t.ChargeHitSeconds = Config.huntChargeHitSeconds;
            return t;
        }

        /// <summary>The administrator was turned away: he forces the gate and the hunt begins.
        /// Called by the desk after the DENY stamp has landed.</summary>
        public void BeginAdminHunt()
        {
            if (Session.Hunt != HuntState.None) return;
            Session.Hunt = HuntState.Active;
            PersistRun();   // quitting now continues the hunt, never skips it
            Audio.PlaySfx("sfx_alarm", 0.8f);
            var lines = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "Then I will let myself in." },
                new DialogueLine { Speaker = "RADIO", Text = "The gate! He is through the gate — he is inside the school!" },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The server room. That is where he is going. Find him in the classrooms and stop him — the lamp, and the beam." },
            };
            if (Dialogue != null) Dialogue.Play(lines, () => EnterExploration(RoomId.Hallway));
            else EnterExploration(RoomId.Hallway);
        }

        /// <summary>Continue after a save made mid-hunt: straight back to the patrol.</summary>
        public void ResumeAdminHunt()
        {
            if (!HuntActive) return;
            EnterExploration(RoomId.Hallway);
        }

        /// <summary>He was stunned enough times and is driven off: the night closes normally.</summary>
        public void WinAdminHunt()
        {
            if (!HuntActive) return;
            Session.Hunt = HuntState.Won;
            if (Views != null) Views.StopAdminHunt();
            Audio.PlaySfx("sfx_bell", 0.8f);
            var lines = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "…This building remembers who opens its doors. It will remember you." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The shape folds into the dark. The hallway is only a hallway again. Dawn is close." },
            };
            if (Dialogue != null) Dialogue.Play(lines, FinishHuntedNight);
            else FinishHuntedNight();
        }

        // The results and ending screens live in the desk scene, so load it first.
        private void FinishHuntedNight()
        {
            QueueSceneAction(() => { Bus.Publish(GameEvent.Of(EventIds.PatrolEnded)); FinishShift(); });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        /// <summary>The server door meter ran out: he reached the server room.</summary>
        public void LoseAdminHunt()
        {
            if (!HuntActive) return;
            Session.Hunt = HuntState.Lost;
            if (Views != null) Views.StopAdminHunt();
            ShiftFailing = true;   // input and the sanity tick stand down while the scene plays
            System.Action then = () =>
            {
                ShiftFailing = false;
                TriggerEnding(Session.EvaluateEnding());
            };
            if (Scares != null) Scares.FailureSequence(AnomalyBehaviorKind.Blink, then);
            else then();
        }
    }
}
