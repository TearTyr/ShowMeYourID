using System;

namespace Hollowbrook.Logic
{
    /// <summary>The ONE authoritative progression path for a real-anomaly encounter.
    /// Documented transition rules (a tick may change at most one state):
    ///   Roaming   - dark and untracked: ordinary behavior movement, no attack intent.
    ///   Stalking  - meter > 0.05 without light: it presses in; movement/cues communicate it.
    ///   Detected  - any light covers it (beam or lit room): readable silhouette, NOT victory.
    ///   Exposed   - accurate Focus + meter > ExposeMeter: corrupted identity visible, repel
    ///               meter advancing. Losing Focus (or battery) falls back to Detected - the
    ///               state is recoverable; the meter then decays per TrackingPolicy after the
    ///               grace window, it is not wiped.
    ///   Repelled  - meter >= RepelMeter: threat behavior and catch timers STOP (Threatening is
    ///               judged on the new state); the entity physically retreats for
    ///               ExposedBeatSeconds, then leaves the stage.
    ///   Recovering- off-stage: the spawner's return timer owns the RecoverSeconds countdown
    ///               and re-enters the world as Roaming through a plausible location.
    /// Hallucinations never enter this machine; they use the Focus-seconds truth-check.</summary>
    public enum AnomalyState
    {
        Roaming,     // moving between waypoints, minding its own business
        Stalking,    // unlit/unwatched: closes distance or reroutes toward the player
        Detected,    // inside the illuminated area: silhouette/movement readable
        Exposed,     // held in Focus: distorted face/identity visible, repel meter filling
        Repelled,    // repel condition met: threat interrupted, it flees
        Recovering,  // off-stage: repositions through connected rooms, may return
    }

    [Serializable]
    public struct EncounterTuning
    {
        public float RepelMeter;          // 0..1 tracking meter that constitutes "sustained light"
        public float ExposeMeter;         // meter at which accurate Focus first exposes the identity
        public float ExposedBeatSeconds;  // how long a repelled anomaly visibly retreats before leaving
        public float RecoverSeconds;      // off-stage time before it may return
        public float ThreatRange;         // world units at which it can lunge/catch
        public static EncounterTuning Default => new EncounterTuning
        {
            RepelMeter = 1f, ExposeMeter = 0.55f, ExposedBeatSeconds = 0.9f, RecoverSeconds = 14f, ThreatRange = 1.6f,
        };
    }

    public struct EncounterStep
    {
        public AnomalyState State;
        public bool JustExposed;    // transition into Exposed this tick (face-reveal feedback)
        public bool JustRepelled;   // transition into Repelled this tick (retreat + token)
        public bool JustRecovered;  // Recovering finished: eligible to roam again
        public bool Threatening;    // inside threat range while not repelled/recovering
    }

    public static class AnomalyEncounter
    {
        public static EncounterStep Step(AnomalyState state, float meter, float stateTimer,
            bool illuminated, bool focused, bool batteryOk, float distanceToPlayer,
            float dt, EncounterTuning t)
        {
            var r = new EncounterStep { State = state };
            switch (state)
            {
                case AnomalyState.Repelled:
                    if (stateTimer >= t.ExposedBeatSeconds) { r.State = AnomalyState.Recovering; stateTimer = 0f; }
                    break;
                case AnomalyState.Recovering:
                    if (stateTimer >= t.RecoverSeconds) { r.State = AnomalyState.Roaming; r.JustRecovered = true; stateTimer = 0f; }
                    break;
                case AnomalyState.Exposed:
                    if (meter >= t.RepelMeter) { r.State = AnomalyState.Repelled; r.JustRepelled = true; stateTimer = 0f; }
                    else if (!focused || !batteryOk) { r.State = AnomalyState.Detected; stateTimer = 0f; }
                    break;
                default: // Roaming / Stalking / Detected
                    if (focused && batteryOk && illuminated && meter > t.ExposeMeter)
                    { r.State = AnomalyState.Exposed; r.JustExposed = true; stateTimer = 0f; }
                    else if (illuminated) r.State = AnomalyState.Detected;
                    else if (meter > 0.05f) r.State = AnomalyState.Stalking;  // half-lit: it presses in
                    else r.State = AnomalyState.Roaming;                       // dark and untracked
                    break;
            }
            // threatening is judged on the NEW state: the tick that repels it can no longer
            // also count as a catch frame.
            r.Threatening = (r.State == AnomalyState.Roaming || r.State == AnomalyState.Stalking ||
                             r.State == AnomalyState.Detected) && distanceToPlayer <= t.ThreatRange;
            return r;
        }
    }
}
