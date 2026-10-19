# Baseline audit — flashlight & roaming anomaly (Phase 0)

Audited at `64dd4ab` (branch `fix/ui-layout-redo`), before any edits. Every line below was
traced in the working tree, not in the task's reference dump. Headless suites at baseline:
verify.sh compile+audit clean, FlowCheck 504 OK / 0 BAD, LogicTests 27125/27125,
frozen Verification/LogicTests 663/663.

Interaction path traced:
`FlashlightRig.ReadInput → ApplyBeam → LightController.SetMode → ViewManager.IsIlluminated →
WanderingAnomaly.TickTracking / TickStates → AnomalyEncounter.Step`, plus
`RoomSpawner.UpdateSpawner / SpawnAnomaly / ScheduleReturn / OnAnomalyGone / DiscardLive`,
room transitions (`ViewManager.EnterRoomImmediate`), pause (`GameRoot.TogglePause`),
battery (`FlashlightRig.TickBattery`).

## A. Beam geometry & coordinate consistency — CONFIRMED BROKEN

- `FlashlightRig.ConeCovers()` is a **rectangle test** (`|dx|<7 && |dy|<1.7` Focus,
  `6.2/3.4` Sweep) with a `Mathf.Sign(dx)==_side` side check. There is no direction,
  no angle, no range cone: aiming up/down never tilts coverage, and the visible sprite
  (flipped, scaled) disagrees with the logical box everywhere except pure-horizontal aim.
- It compares a **world point** against `_cone.transform.localPosition` (rig-local).
  It only works because every parent in the chain happens to be identity.
- `AimBeam()` never rotates the beam toward the cursor: it offsets x by `±0.6`, damps y
  (`dy*0.75`, clamped ±2.6) and flips `localScale.x`. The mask is **not flipped**
  (`w*1.25` always positive) → aiming left desyncs mask from cone.
- `ScreenToWorldPoint(Input.mousePosition)` with mouse `z=0` does not convert to the
  `z=0` world plane under the orthographic camera at `z=-10` (needs `mouse.z = -cam.z`).
- Callers mix spaces: `WanderingAnomaly` passes `transform.localPosition`,
  `ViewManager.IsIlluminated` forwards to `PlayerController.ConeCovers` → rig box test.

## B. Flashlight input conflicts — CONFIRMED

- **F and Q latches are not mutually exclusive**: `ReadInput()` toggles `_latchSweep` and
  `_latchFocus` independently; the resolver prefers Focus, so pressing F while Q is latched
  sets a sweep latch that is silently ignored — the mode "doesn't respond".
- **Battery death auto-reactivates**: on depletion `ApplyBeam(Off)` runs but latches are
  *kept*. Once charge recovers past `batteryRebootFraction`, the latched beam switches
  itself back on with no player input.
- `Update()` gates only on `_gameRoot.Paused` — input, aiming and battery keep running
  during blocking dialogue, the map overlay, transitions and non-exploration states.
- Headless: `Input.GetMouseButton()` is hard-`false` in the stubs, so hold-modes were
  never testable (stub needs one additive `MouseHeld` set).

## C. Ambient lighting ↔ flashlight shared state — CONFIRMED

- `ApplyBeam()` **writes the room's `LightController`**: Focus beam → `room.SetMode(Focus)`;
  Sweep in a dark room → `room.SetMode(Sweep)`. Switching the flashlight on silently
  switches the room lights on.
- `ViewManager.IsIlluminated()` only queries the beam when `room.Light.Mode == Focus` —
  a state that only the flashlight itself ever sets. Circular coupling.
- `PlayerController.PlaceInRoom()` calls `room.Light.ToggleFocus(false)` on every room
  entry — walking through a door mutates room lighting.
- Battery recovery reads `Light.Mode == Sweep` as "lit room": after the beam wrote `Focus`
  into the room, a genuinely lit room reads as unlit → wrong recovery rate.
- `SanityController.Tick` has a `Mode == Focus` branch (0.25× darkness drain) for a room
  state that should not exist.
- `HallucinationSystem` phantom flicker: `SetMode(Off)` then `Invoke(RestoreLights)` →
  `SetMode(Sweep)` **unconditionally** — restores lights-on even if the player had them off.
- `LightController.SetMode` user-facing text says "Flashlight on." for a *room* mode.
- Flicker itself only animates darkness alpha (does not corrupt beam state) — OK.

## D. Competing tracking & encounter logic — CONFIRMED (state machine is dead code)

- `Update()` runs `TickTracking` **before** `TickStates`. `TrackingPolicy.IsRevealed`
  fires at meter 1.0 → sets `_revealed`, drops the token, publishes `AnomalyRevealed`,
  schedules an in-place despawn after `RevealBeatSeconds`.
- `TickStates()` early-returns forever once `_revealed` → `AnomalyEncounter.Step` can
  **never** reach Repelled/Recovering in real play (`EncounterTuning.RepelMeter` is the
  same 1.0 the reveal path consumes first). `JustRepelled`, retreat, `ScheduleReturn`
  never execute. The encounter state machine is decorative.

## E. Repelled state vs actual movement — CONFIRMED

- Even if Repelled were reachable: `MoveBehavior()` is gated only on `_revealed`, never on
  `_state` — a "repelled" anomaly would keep running its ordinary kind movement
  (Blink still chases when unseen, Sprint still dashes). No retreat movement exists.
- `EncounterStep.Threatening` is computed from the **old** state — on the tick that
  transitions into Repelled, a close-range catch could still fire.
- `Config.coverHugCatchSeconds` is used as the catch windup for **every** kind
  (`TickStates` line: `_threatSeconds >= _gameRoot.Config.coverHugCatchSeconds`).
- Sprint: `_telegraph` counts down once from `Init`, then it dashes at 4.5 u/s forever
  (`PickTarget` — the only place that resets telegraph — is never called for Sprint).
  No warning cue at telegraph start; no dash/recover cycle.
- CoverHug: spawns at `floorY+4.6` (ceiling) but `MoveBehavior` clamps y to
  `[-2.6,-0.4]` and steers to `-0.6` — contradictory coordinates drag it off its hiding
  position on frame one.

## F. Room transitions & anomaly lifecycle — CONFIRMED

- Anomalies are parented to `room.Root`; leaving the room `SetActive(false)` → the
  anomaly **freezes** (Update stops, lifetime stops ticking).
- `RoomSpawner._liveAnomaly` still references the frozen entity → `UpdateSpawner`
  returns on `if (_liveAnomaly != null)` forever: **no new encounter can spawn anywhere**
  until the player happens to walk back. Stale reference blocks the system.
- `ScheduleReturn(kind)` ignores `kind`: after `RecoverSeconds` it calls `SpawnAnomaly(false)`
  which peeks the pool again — the "returned" threat can be a different behavior, and it
  spawns into whatever room the player currently occupies.
- `ViewManager.HideWorld()` calls `_spawner.DiscardLive()` — merely opening the **map
  overlay** (State != Exploration → HideWorld) destroys the live anomaly without resolution.
- Distance-based sanity drain, sighted penalty and catch use raw x-distance **without
  room ownership** — a player in Classroom 1 at x≈4 is "next to" a hallway anomaly at x≈4
  (only masked today because cross-room anomalies are frozen).
- `DiscardLive` on returning to the desk (unresolved → stays in pool) — correct, keep.
- Pool/token: `OnAnomalyGone` resolves the pool on `WasRevealed || WasCaught`; the token
  drops from *two* call sites (reveal path and the dead `JustRepelled` path) guarded only
  by `_tokenDropped`; no single exactly-once resolution flag.

## Verified-OK items (do not break)

- Hallucinations never touch the pool, never drop tokens, never catch (`TickStates` and
  `OnAnomalyGone` guard on `IsHallucination`; phantom kinds are Slide/Blink only).
- `PatrolPool` semantics ("uncaught == outstanding", consumed by the security-office late
  queue via `PatrolRules.BuildLateQueue`) are coherent; keep.
- Frozen `Verification/LogicTests` pins `AnomalyEncounter.Step(...)` signature and its
  transition table (incl. `EncounterTuning.Default` field names/values) — extend additively
  only: new tuning fields with behavior-preserving defaults, `Threatening` switched to the
  NEW state (frozen expectations still hold; verified by hand before editing).
- Battery *rates* live in `GameConfigSO` and are sane (Sweep 40 s, Focus ~17 s,
  recover 25 s/12.5 s, reboot at 20 %); reveal takes ~1.8 s of Focus. Keep magnitudes.
- Event ids (`BatteryChanged`, `TrackingChanged`, `AnomalyStateChanged`, `AnomalyRevealed`,
  `AnomalyCaught`, `TokenCollected`, `LightModeChanged`) exist and are throttled where
  per-frame; keep ids, fix emission points.

## Cannot verify headlessly (no Unity editor in this sandbox)

Real `SpriteMask` reveal under the darkness overlay, actual `ScreenToWorldPoint` with the
live camera, `WaitForSeconds` pacing, sorting-order visuals, photosensitivity flicker.
These get an explicit NOT RUN + reproduction steps in the final report; the geometry math
itself moves into pure, unit-tested logic (`BeamGeometry`) so the runtime glue is thin.

## Remediation plan (implemented on `feat/flashlight-anomaly-rework`)

1. Pure logic in `GameLogic` (engine-free, unit-testable): `BeamGeometry` (true cone test,
   one world space), `FlashlightInput` (latch/hold arbitration), `BatteryModel`
   (drain/recover/death/reboot), `AnomalyBehaviorRules` (per-kind profiles), `RoomGraph`
   (connectivity + door anchors, single source for `RoomBuilder`).
2. `LightController` becomes room-only (`Lit` bool); `FlashlightRig` never writes it;
   `ViewManager` owns illumination queries (`BeamCovers` / `RoomLit` / `IsIlluminated` /
   `IsFocusLit`); battery & sanity read the real ambient state.
3. `AnomalyEncounter` becomes the single authority (`_revealed` bypass deleted);
   `TrackingPolicy` reduced to the meter-rate model (Focus advances, observation holds,
   darkness decays + short grace); Repelled retreats physically; Recovering hands off to
   the spawner's return timer (same kind, plausible room, exactly-once token/pool resolve).
4. Behaviors get real rules: Blink freezes when observed, Sprint telegraph→dash→recover
   with a warning cue and beam-interrupt, CoverHug keeps its ceiling band (spawn/clamp
   consistent) and only Focus finds it, Slide glides readably.
5. Anomalies live under a persistent `AnomalyRoot`, own a `RoomId`, patrol through doors
   when off-stage (no freeze, no teleport, room-gated threat/sanity), one live slot,
   return timer inside the spawner (no coroutine races), map overlay no longer discards.
6. Tuning into `GameConfigSO` (defaults sourced from the logic constants), tests in
   LogicTests + a headless FlowCheck `WorldLoop`, frozen suites untouched.
