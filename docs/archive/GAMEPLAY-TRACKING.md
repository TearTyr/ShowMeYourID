# Gameplay increment - anomaly tracking, reveal & tokens (master-prompt §3/§4)

> **UPDATED by the flashlight/anomaly rework (`feat/flashlight-anomaly-rework`)**: the meter no longer
> "reveals/dispels" on its own. `AnomalyEncounter` is the single authority - a full meter means
> **Repelled** (physical retreat, token once, spawner-owned recovery return), and ambient light only
> **holds** the meter instead of advancing it. See `docs/FLASHLIGHT-ANOMALY-AUDIT.md` for the baseline
> defects this replaced.

Closes deviation **D10** from `docs/SPEC-CONFORMANCE.md`: the spec's tracking layer did not exist.
Design goal: the light beam becomes a *verb* - hold it and the haze solidifies into ink.

## Rules (`GameLogic/TrackingPolicy.cs`, engine-free, unit-tested)

| Input | Effect on the 0..1 meter |
|---|---|
| Focus beam (tight cone) on the anomaly | +0.55 /s → full in ~1.8 s (the ONLY way to progress) |
| Sweep beam or room lights on it | 0 /s → *holds* the meter; detection is not victory |
| 0.35 s after Focus is lost | grace window: still holds, then decays |
| Neither (unobserved) | −0.35 /s → the haze closes back up |
| Meter > 0.55 under Focus | **Exposed**: distorted face shown; losing Focus falls back (recoverable) |
| Meter reaches 1.0 | **Repelled**: it physically retreats for 0.9 s, drops a token once, leaves the stage; the spawner returns the same kind elsewhere after ~14 s |
| Focus held 0.5 s on a *hallucination* | it shimmers out ("*it was never there*") - spec §4 truth-check; never touches pool/token |

## Wiring

- `Horror/WanderingAnomaly.cs` - `TickTracking` (meter) feeds `TickStates` (`AnomalyEncounter.Step`,
  the one authoritative path); publishes `EventIds.TrackingChanged` (throttled to 10 Hz),
  `AnomalyStateChanged` per transition, `AnomalyRevealed` once per repulsion, caption + stinger.
  Repelled anomalies retreat physically (hunting and catch timers stop); `TickVisibility` renders
  exposed ones at full alpha / sorting order 80 and hides off-stage roamers.
- `World/Interactables.cs` - `TokenPickup : Interactable` ("Collect token [E]") registers itself in
  `RoomInstance.Interactables`, increments `GameSession.TokensCollected`, publishes `TokenCollected`,
  removes itself from the room list and destroys its GameObject.
- `Bootstrap/ProceduralArt.cs` - new `token` key (28 px pocket-watch motif; still zero image files).
- `UI/HudAndCommon.cs` - a TRACK meter under the sanity bar, visible only while 0 < meter < 1;
  hidden on reveal/decay/collection. No new screen, no clutter.
- `GameLogic/GameSession.cs` + `SaveFormat.cs` - `TokensCollected` persists with the run
  (`tokens=` line; old saves without it load as 0).

## Verification

| Check | Result |
|---|---|
| Compile vs real Unity 2022.3.62f1 assemblies (minimal + full sets) | PASS, 0 errors / 0 warnings |
| `Tools/Verification/LogicTests` (the team's suite) | PASS 185 / 185 |
| `Tools/Verification/EngineSmoke` (the team's suite) | PASS 159 / 159 |
| `Tools/LogicTests` (independent suite, incl. 19 new tracking/token assertions) | PASS 27,125 / 27,125 |
| Static audit (`Tools/Verify/audit.py`, 44 scripts) | clean |
| Feel/pixels in Play mode | **NOT TESTED** - needs the Unity editor |

## Manual test (2 minutes, in the editor)

1. Play → NEW GAME → Night 1 has no anomalies, so jump to Night 3 via a save or press on; enter the
   hallway (`PATROL HALLWAY`) and wait for a wandering anomaly.
2. Lights **Off**: silhouette faint, meter hidden. Press **F** and hold the cone on it: TRACK bar
   fills, silhouette darkens toward solid ink.
3. Release **F**: bar drains. Hold again to 100 %: stinger + caption, silhouette goes solid,
   dispels, a small watch-token remains; walk to it, press **E** → caption + FINDINGS-adjacent toast
   state; `tokens=` in the save file increments.
4. At low sanity a hallucination appears: 0.5 s of Focus makes it vanish without a token.
5. Pause/resume mid-track: the meter freezes (Update early-returns while `R.Paused`).
