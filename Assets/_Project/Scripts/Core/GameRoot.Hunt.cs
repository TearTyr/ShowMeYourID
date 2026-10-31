using System.Collections.Generic;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Night 5: after the DENY the window closes and he leaves, and the hunt starts when the player
    // clicks Patrol. He then stalks the school until dawn (rules in GameLogic/AdminHunt, scene
    // driver in World/AdminHuntDirector). This file is the flow around it: getting ready at the
    // desk, ending it in a win or a loss, and resuming after Continue.
    public partial class GameRoot
    {
        /// <summary>True from the Patrol click until dawn or until he reaches the server door.</summary>
        public bool HuntActive => Session != null && Session.Hunt == HuntState.Active;

        /// <summary>He has been refused and has left the window: only Patrol is left at the desk.</summary>
        public bool HuntReady => Session != null && Session.Hunt == HuntState.Ready;

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
            t.CatchesToLose = Config.huntCatchesToLose;
            // every lie exposed at the window makes the night shorter
            float lies = AdminTalk.LiesExposed(Session.AdminTalk);
            t.SurviveSeconds = Mathf.Max(Config.huntMinSurviveSeconds,
                Config.huntSurviveSeconds - lies * Config.huntSecondsPerLie);
            t.StalkMinSeconds = Config.huntStalkMinSeconds;
            t.StalkMaxSeconds = Mathf.Max(Config.huntStalkMinSeconds, Config.huntStalkMaxSeconds);
            t.RoomSeconds = Config.huntRoomSeconds;
            t.RoomHoldSeconds = Config.huntRoomHoldSeconds;
            t.HeadRadius = Config.huntHeadRadius;
            t.BehindSeconds = Config.huntBehindSeconds;
            t.BeamHitSeconds = Config.huntBeamHitSeconds;
            return t;
        }

        /// <summary>The administrator was turned away: the security game is over for good, and
        /// the hunt waits for the Patrol button. Saved now, so quitting cannot skip it.</summary>
        public void PrepareAdminHunt()
        {
            if (Session.Hunt != HuntState.None) return;
            Session.Hunt = HuntState.Ready;
            SetObjective("He is inside the school. Press PATROL when you are ready.");
            PersistRun();
        }

        /// <summary>He says he will be back and goes. Called once the window has shut on him.</summary>
        public void PlayAdminExit()
        {
            if (Dialogue == null) return;
            Dialogue.Play(new List<DialogueLine>
            {
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "Very well, custodian. I'll be back." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "He walks off into the dark. The gate is still swinging. He never went through it." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "He is inside. Nobody else is coming tonight. Take the flashlight and go on patrol when you are ready. Only the light stops him, and dawn is the only way out." },
            }, null);
        }

        /// <summary>Continue after a save made mid-hunt: straight back to the patrol.</summary>
        public void ResumeAdminHunt()
        {
            if (!HuntActive) return;
            EnterExploration(RoomId.Hallway);
        }

        /// <summary>Dawn came and he is gone: the night closes normally.</summary>
        public void WinAdminHunt()
        {
            if (!HuntActive) return;
            Session.Hunt = HuntState.Won;
            if (Views != null) Views.StopAdminHunt();
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

        /// <summary>He caught the player too many times and reached the server room.</summary>
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
