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
            // night 6 is the same hunt with its own, harder numbers
            bool n6 = Session.Mode == GameMode.Challenge;
            t.CatchesToLose = n6 ? Config.night6CatchesToLose : Config.huntCatchesToLose;
            // every lie exposed at the window makes the night shorter
            float lies = AdminTalk.LiesExposed(Session.AdminTalk);
            t.SurviveSeconds = n6
                ? Mathf.Max(Config.night6MinSurviveSeconds, Config.night6SurviveSeconds - lies * Config.night6SecondsPerLie)
                : Mathf.Max(Config.huntMinSurviveSeconds, Config.huntSurviveSeconds - lies * Config.huntSecondsPerLie);
            float stalkMin = n6 ? Config.night6StalkMinSeconds : Config.huntStalkMinSeconds;
            float stalkMax = n6 ? Config.night6StalkMaxSeconds : Config.huntStalkMaxSeconds;
            t.StalkMinSeconds = stalkMin;
            t.StalkMaxSeconds = Mathf.Max(stalkMin, stalkMax);
            t.RoomSeconds = n6 ? Config.night6RoomSeconds : Config.huntRoomSeconds;
            t.RoomHoldSeconds = n6 ? Config.night6RoomHoldSeconds : Config.huntRoomHoldSeconds;
            t.HeadRadius = Config.huntHeadRadius;
            t.BehindSeconds = n6 ? Config.night6BehindSeconds : Config.huntBehindSeconds;
            t.BeamHitSeconds = n6 ? Config.night6BeamHitSeconds : Config.huntBeamHitSeconds;
            t.DodgesLamp = n6;   // night 6: he circles and dodges the lamp in a room
            if (n6) ScaleSwapWaits(ref t, GameRules.Current.Night6SwapScale);
            return t;
        }

        // Night 6: every wait between his swaps is shorter (the 1 to 2 s breath after a peek, the hallway swap).
        private static void ScaleSwapWaits(ref HuntTuning t, float scale)
        {
            t.AmbushDelayMinSeconds *= scale;
            t.AmbushDelayMaxSeconds *= scale;
            t.SwapRetreatSeconds *= scale;
            t.SwapAfterRepelSeconds *= scale;
            t.RetreatSeconds *= scale;
        }

        /// <summary>The administrator was turned away: the security game is over for good, and
        /// the hunt waits for the Patrol button. Saved now, so quitting cannot skip it.</summary>
        public void PrepareAdminHunt()
        {
            if (Session.Hunt != HuntState.None) return;
            Session.Hunt = HuntState.Ready;
            Session.HuntCatches = 0;
            Session.HuntSecondsLeft = -1f;
            SetObjective("He is inside the school. Press PATROL when you are ready.");
            PersistRun();
        }

        /// <summary>He says he will be back and goes. Called once the window has shut on him.</summary>
        public void PlayAdminExit()
        {
            if (Dialogue == null) return;
            if (Session.Mode == GameMode.Challenge)
            {
                Dialogue.Play(new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = "Then we do this the hard way, custodian. I will not stop until dawn." },
                    new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "He is faster tonight, and he is not pretending any more. Take the flashlight and go on patrol when you are ready. Only the light stops him." },
                }, null);
                return;
            }
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
                new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Session.Mode == GameMode.Challenge
                    ? "…Dawn. Again. You are the first who ever said no twice."
                    : "…This building remembers who opens its doors. It will remember you." },
                new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "The shape folds into the dark. The hallway is only a hallway again. Dawn is close." },
            };
            if (Dialogue != null) Dialogue.Play(lines, FinishHuntedNight);
            else FinishHuntedNight();
        }

        // The results and ending screens live in the desk scene, so load it first.
        private void FinishHuntedNight()
        {
            QueueSceneAction(FinishShift);
            Hollowbrook.Scenes.SceneRouter.Load(Hollowbrook.Scenes.SceneRouter.Checkpoint);
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
            if (Scares != null) Scares.FailureSequence(FaceAssets.AdminWindowKey, then);
            else then();
        }
    }
}
