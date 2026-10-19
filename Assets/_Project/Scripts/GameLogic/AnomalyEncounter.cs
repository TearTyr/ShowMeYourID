using System;

namespace Hollowbrook.Logic
{
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
        public float ExposedBeatSeconds;  // how long the face stays visible before it flees
        public float RecoverSeconds;      // off-stage time before it may return
        public float ThreatRange;         // world units at which it can lunge/catch
        public static EncounterTuning Default => new EncounterTuning
        {
            RepelMeter = 1f, ExposedBeatSeconds = 0.9f, RecoverSeconds = 14f, ThreatRange = 1.6f,
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
                    if (focused && batteryOk && illuminated && meter > 0.55f)
                    { r.State = AnomalyState.Exposed; r.JustExposed = true; stateTimer = 0f; }
                    else if (illuminated) r.State = AnomalyState.Detected;
                    else if (meter > 0.05f) r.State = AnomalyState.Stalking;  // half-lit: it presses in
                    else r.State = meter <= 0.05f && !illuminated ? AnomalyState.Roaming : state;
                    break;
            }
            r.Threatening = (state == AnomalyState.Roaming || state == AnomalyState.Stalking ||
                             state == AnomalyState.Detected) && distanceToPlayer <= t.ThreatRange;
            return r;
        }
    }
}
