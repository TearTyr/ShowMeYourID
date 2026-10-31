using System.Collections.Generic;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Night 5, after the administrator is turned away: he does not leave, he stalks the school
    // until dawn (rules in GameLogic/AdminHunt, scene driver in World/AdminHuntDirector). This file
    // is the flow around it: starting the hunt from the desk, ending it in a win or a loss, and
    // resuming it after Continue. One part of GameRoot, split out so each file stays readable.
    public partial class GameRoot
    {
        /// <summary>True from the DENY stamp until dawn or until he reaches the server door.</summary>
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
            t.CatchesToLose = Config.huntCatchesToLose;
            // every lie exposed at the window makes the night shorter
            float lies = AdminTalk.LiesExposed(Session.AdminTalk);
            t.SurviveSeconds = Mathf.Max(Config.huntMinSurviveSeconds,
                Config.huntSurviveSeconds - lies * Config.huntSecondsPerLie);
            t.StalkMinSeconds = Config.huntStalkMinSeconds;
            t.StalkMaxSeconds = Mathf.Max(Config.huntStalkMinSeconds, Config.huntStalkMaxSeconds);
            t.DoorSeconds = Config.huntDoorSeconds;
            t.DoorHoldSeconds = Config.huntDoorHoldSeconds;
            t.ClosetSeconds = Config.huntClosetSeconds;
            t.ClosetHoldSeconds = Config.huntClosetHoldSeconds;
            t.HeadRadius = Config.huntHeadRadius;
            t.BehindSeconds = Config.huntBehindSeconds;
            t.BeamHitSeconds = Config.huntBeamHitSeconds;
            return t;
        }

        /// <summary>The administrator was turned away: he says he will be back and leaves, then
        /// the hunt begins. Called by the desk after the DENY stamp has landed; left runs when he
        /// walks away from the window.</summary>
        public void BeginAdminHunt(System.Action left = null)
        {
            if (Session.Hunt != HuntState.None) return;
            Session.Hunt = HuntState.Active;
            PersistRun();   // quitting now continues the hunt, never skips it
            var parting = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "Very well, custodian. I'll be back." },
            };
            var after = new List<DialogueLine>
            {
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "He walks off into the dark. The gate is still swinging. He never went through it." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "He is inside. Watch where he stops. Hold the doors, check the comfort rooms, watch your back. Dawn is the only way out." },
            };
            System.Action thenHunt = () =>
            {
                if (left != null) left();
                if (Dialogue != null) Dialogue.Play(after, () => EnterExploration(RoomId.Hallway));
                else EnterExploration(RoomId.Hallway);
            };
            if (Dialogue != null) Dialogue.Play(parting, thenHunt);
            else thenHunt();
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
