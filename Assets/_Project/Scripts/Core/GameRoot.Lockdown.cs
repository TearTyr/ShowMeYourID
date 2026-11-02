using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: the Lockdown mode. LockdownRules (pure) knows what a mistake is; this part counts
    // them, and ends the night when the third one lands or the last face has left the window.
    public partial class GameRoot
    {
        public bool LockdownOn => Session != null && Session.Mode == GameMode.Lockdown;

        // a stamped visitor: letting an anomaly in or turning a student away is a mistake
        private void NoteLockdownVerdict(InspectionRules.Outcome outcome)
        {
            if (LockdownRules.IsStrike(outcome.WasAnomaly, outcome.Verdict)) AddLockdownStrike();
            CheckLockdownEnd();
        }

        /// <summary>The time ran out on this visitor and the thing outside took them.</summary>
        public void OnVisitorTaken(VisitorData visitor)
        {
            if (!LockdownOn || visitor == null || !_resolvedVisitorIndices.Add(visitor.VisitorIndex)) return;
            if (LockdownRules.TakenIsStrike(visitor.IsAnomaly)) AddLockdownStrike();
            CheckLockdownEnd();
        }

        private void AddLockdownStrike()
        {
            Session.LockdownStrikes++;
            Bus.Publish(GameEvent.Of(EventIds.Notification,
                "MISTAKE " + Session.LockdownStrikes + " OF " + LockdownRules.MaxStrikes));
            Audio.PlaySfx("sfx_glitch", 0.6f);
        }

        /// <summary>Third mistake: the gate falls. Last face gone: the gate held. Returns true once the night has ended.</summary>
        public bool CheckLockdownEnd()
        {
            if (!LockdownOn) return false;
            if (State == AppViewState.Ending) return true;
            if (LockdownRules.Lost(Session.LockdownStrikes))
            {
                TriggerEnding(new EndingRules.Evaluation { Ending = EndingType.Lose, ReasonKey = "lockdown_gate", ReasonText = LockdownRules.LoseText });
                return true;
            }
            if (Queue != null && Queue.ShiftComplete)
            {
                TriggerEnding(new EndingRules.Evaluation { Ending = EndingType.Win, ReasonKey = "lockdown_held", ReasonText = LockdownRules.WinText(Session.LockdownStrikes) });
                return true;
            }
            return false;
        }
    }
}
