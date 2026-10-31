# Peek-In Flashlight Room Inspection — implementation notes

Classrooms 1 and 2 are **peek-only**: the player never walks inside. Standing at a classroom
doorway and pressing `E` fades to black, frames the room, and hands the reveal to a
cursor-locked circle of light. This file documents how it works and what is knowingly rough.

## State machine

```
Exploration --(E at classroom door: Doorway.Interact -> GameRoot.EnterRoomPeek)--> RoomPeek
RoomPeek    --(E at the room's hallway door, or click the bottom HUD band:
              GameRoot.ExitRoomPeek -> ViewManager.ExitPeek)---------------------> Exploration
```

* `AppViewState.RoomPeek` (appended to `Enums.cs`) is a full "night is happening" state: the
  shift clock advances, the HUD stays up, and pause is allowed (`GameRoot.TogglePause`
  allow-list includes it).
* `GameRoot.PeekRoom` (`RoomId?`) names the room being peeked; `ViewManager.ExitPeek` clears
  it and calls `SetState(Exploration)` itself.
* `ViewManager` owns the two coroutines:
  * `PeekTransitionRoutine`: force the corridor beam Off (one `Flashlight.Tick(0)` for
    headless parity) -> fade to black (0.28 s) -> `EnterRoomImmediate(classroom)` -> hide the
    player sprite -> `LightController.EnterPeek(true)` -> lazily create `PeekLight` ->
    `PeekLight.Enter(room)` -> freeze the camera at (0,0,-10) -> fade out (0.34 s).
  * `ExitPeekRoutine`: fade to black -> `PeekLight.Exit()` -> `LightController.EnterPeek(false)`
    -> `EnterRoomImmediate(Hallway, fromRoom)` -> show the player -> clear `PeekRoom`,
    `SetState(Exploration)` -> fade out.
  Both set `Transitioning` while running, so doorways and beam input stay locked mid-fade
  exactly like normal room transitions. They are idempotent (`_peeking` / `_peekExiting`).
* While `_peeking`, `ViewManager` skips the spawner (`Update`), skips the camera clamp
  (`LateUpdate`), and `TickWorld` ticks **only** `PeekLight.Tick(dt)` — no Player, no
  Flashlight, no anomalies, no spawner (acceptance 10). In the editor each component's own
  `Update` still runs; every one of them is peek-aware (see below).

## The camera/mask trick

* The room's darkness overlay is a `solid_white` SpriteRenderer at `sortingOrder = 70` with
  `maskInteraction = VisibleOutsideMask`. A `SpriteMask` anywhere therefore cuts a lit hole.
* `PeekLight` carries that mask (`lamp_glow`, custom range 60–80 so only the darkness layer is
  masked) plus a tinted `Glow` child (`sortingOrder = 72`) as the visible lamp.
* `LightController.EnterPeek(true)` is a *presentation override*: darkness alpha 1, flicker
  stopped, and `SetLit`/`Tick` may not touch the visual while the override is on. The real
  `Lit` state is unchanged — `EnterPeek(false)` restores it via `ApplyVisual()`.
* `FlashlightRig.Tick` early-returns in `RoomPeek`: beam forced Off, latches cleared (so
  stepping back never re-lights it by itself — revival needs a deliberate F/Q), battery
  neither drains nor recovers.
* Anomalies freeze: `WanderingAnomaly.Tick` in `RoomPeek` runs only `TickVisibility`, and the
  peek branch there shows the silhouette **only** while `ViewManager.IsPeekLit(position)`
  (alpha 1/0, `sortingOrder = 80` so it reads above the black). No movement, timers, threats,
  catches, or sanity ticks.

## Interaction cursor radius

* `PeekLight.Tick` projects `Input.mousePosition` onto the z=0 plane and moves the circle
  there. An interactable is "under the light" when its anchor point
  `(AnchorX, RoomInstance.FloorY + 1.4f, 0)` is within `CursorHitRadius` (default **1.3
  world units**) of the cursor — squared-distance scan over `RoomInstance.Interactables`,
  no raycasts, no per-frame allocation.
* The prompt is published on `EventIds.InteractionPrompt` only when it changes (same contract
  as the hallway player). `E` interacts; `E` on the room's hallway doorway exits the peek.
* `Radius` (light circle, default **2.6**) and `CursorHitRadius` are public fields on
  `PeekLight` for tuning.

## Input arbitration (documented deviation from the task spec)

The original spec had `Esc` exit the peek (acceptance 5) *and* open the pause menu
(acceptance 6) — mutually exclusive on one press, and in this scene the pause menu had no
other entry point while peeking (the player controller does not tick). Decision taken with
the game owner during implementation:

* **Esc shows the pause menu** (hallway rule mirrored in `PeekLight.TickEscape`: overlay
  sheet first, then pause toggle; dialogue suppresses it). Acceptance 5's "Esc exits" is
  overridden by this decision.
* **Exiting a peek** is done by clicking the **bottom screen band** (HUDView `peekexitband`
  button, visible only in `RoomPeek`) or by `E` at the room's hallway doorway.
* Consequently the HUD peek objective reads "Sweep the light — click the bottom edge to step
  back." instead of the spec's "Esc to step back."

Other small, deliberate additions beyond the spec bullet list (all to satisfy the spec's own
hard requirements/acceptance tests):

* `GameRoot.TogglePause` allow-list includes `RoomPeek` (requirement: pause must be reachable
  and safe during peek).
* `FlashlightRig` clears its input latches when forcing Off (acceptance 7: deliberate relight).
* `ViewManager.PeekTransitionRoutine` calls `Flashlight.Tick(0)` once (headless parity with
  the editor, where the rig's own `Update` forces Off on the first peek frame).
* `LightController.SetLit` ignores visuals while the peek override is on (hard requirement 2:
  flipping the wall switch mid-peek must not light the room).
* `Doorway.Interact` sends only classroom targets to peek; a `Hallway` target (inside a room)
  keeps the plain `EnterRoom` path.

## Known limitations

1. **Anomaly silhouette legibility under the circle.** The reveal is binary (alpha 1 inside
   the circle, 0 outside) at `sortingOrder = 80`; the sprite's own shading was tuned for the
   dim-room layers, so under the lamp it can read flatter than intended. Needs an art/editor
   pass, not a code change.
2. **Dialogue-pause interplay.** Dialogue suppresses all peek input (cursor, E, Esc) per the
   hard requirements, and `PeekLight.Update` also skips while paused or while an overlay
   sheet is open. If a clue dialogue and the pause menu are opened in quick succession the
   peek simply freezes until both are closed — correct, but it can feel like a hiccup.
3. **Clues have no sprite in the showcase build.** The placeholder `visual` child was
   removed (it read as a floating white block in lit rooms); peek reveals clues through the
   prompt + E only, until real clue art exists. Already-discovered clues keep their red
   `found_marker` at `sortingOrder = 76`, visible over the peek black on purpose.
4. **Design doc drift.** The `showmeyourid.txt` copy in this workspace still describes
   classrooms as enter-and-walk (§1.3, §2.5). The doc-side patch describing peek mode is
   pending from the design side; this implementation follows the peek spec, not those
   paragraphs.

## Verification performed (headless, no Unity editor in this environment)

* `Tools/Verification/CompileCheck` and `Tools/Verify/verify.sh` (real Unity 2022.3
  assemblies, min + full) — clean; `audit.py` clean.
* `Tools/Verification/LogicTests` 663/663 and `Tools/LogicTests` 27,336/27,336 — unchanged.
* `Tools/FlowCheck` 830 OK / 0 BAD — unchanged (desk loop, queue proofs, patrol world loop).
* Scratch harness (outside the repo, not committed): boots menu → NEW GAME → patrol →
  peek headlessly and proves: peek entry/exit paths, hidden player, beam Off + battery
  frozen, player/anomaly/spawner freeze, cursor prompt + E inspect, anomaly reveal only
  under the circle, Esc pause arbitration, bottom-band exit, deliberate relight after peek,
  and return-to-checkpoint — 32/32 checks pass.
* **Not testable here:** everything visual/audible (fade feel, mask edge softness, glow tint,
  silhouette legibility), real-input feel, and the editor-only `DeskLayoutAudit` warnings.
  Acceptance items 1–12 were verified headlessly where logic allows; the purely visual ones
  need an editor playtest.
