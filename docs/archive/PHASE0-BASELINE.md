# Phase 0 - baseline record (before Task 1 / Task 2 edits)

Repo: `github.com/TearTyr/ShowMeYourID` @ `1ac4c79` ("JP Patch"), cloned 2026-10-24.

## Engine / rendering stack
- **Unity 6.3** (`ProjectSettings/ProjectVersion.txt` = `6000.3.0f1`), **Built-in Render Pipeline**
  (no URP/HDRP, no 2D Light Renderer, no post-processing volume, no shaders in the project).
- 2D side-scroller patrol + uGUI desk. Orthographic main camera, `orthographicSize = 5`
  (`CoreBootstrap.cs:66`) → **17.78 world units of visible width at 16:9**, 10 units of height.
- All art/audio is generated procedurally at runtime (`ProceduralArt`, `ProceduralAudio`).
  Real files win if present: `Assets/_Project/Resources/Art/<key>.png` (README "Repository layout").

## Lighting framework already in use (this is what Task 1 must reuse)
Darkness is a **black overlay quad per room** + **SpriteMask holes** (no light objects at all):
- `RoomBuilder.BuildRoom` → `Quad("darkness", …, order 70, size (HalfWidth*2+4) × 11, Color.black)`,
  `maskInteraction = VisibleOutsideMask`; alpha owned by `LightController`.
- `LightController` (ambient ONLY): `LitAlpha = 0.12f`, `DarkAlpha = 0.62f`, flicker wobbles the
  darkness alpha, `EnterPeek` forces alpha 1, `ForceDarkOnEntry` on every entry.
- `FlashlightRig` (handheld ONLY): `SpriteMask` "FlashlightMask" (`isCustomRangeActive`,
  front 60 / back 70) + cone `SpriteRenderer` "beam" (order 71, colour `(1, 0.96, 0.85, 0.30)`),
  pure cone test in `GameLogic/BeamGeometry`, arbitration in `GameLogic/FlashlightInput`,
  `GameLogic/BatteryModel`.
- `PeekLight` - the classroom peek's cursor lamp (mask range 60..80 + a glow sprite at order 72).
- Sorting map: room art 0–10, anomalies 40, beam mask range 60–70, darkness 70, cone 71,
  player 73, light-switch plate 75. HUD is a Screen-Space-Overlay canvas (never masked).

## Hallway areas found (Task 1 scope)
| Area | Where | Notes |
|---|---|---|
| `RoomId.Hallway` "MAIN HALLWAY" | `School.unity` patrol, built by `RoomBuilder` | **the only hallway**. `HalfWidth = 11.4`, player spawn x −9.4, 3 doors (Security −10.6, Classroom1/2 via `RoomGraph.DoorX`), wall switch at −9.2, 5 clues |
| Classroom peek (`EnterPeek`) | shows Classroom1/2 only | never the hallway; `ExitPeek` returns to the hallway |
| `SecurityRoomView` / desk (`Hollowbrook.unity`) | office + checkpoint | not hallways, no patrol lighting |

Transitions: `ViewManager.EnterRoom → TransitionRoutine (fade) → EnterRoomImmediate` which calls
`room.Light.ForceDarkOnEntry()` + `Flashlight.ForceBeamOffOnEntry()`; patrol start
(`ShowWorld → EnterRoomImmediate(Hallway, null)`) and peek returns take the same path.

## Flashlight state (baseline - already manual)
- State lives on `FlashlightRig.Beam` (`LightMode Off/Sweep/Focus`) + `FlashlightLatches`
  (`internal Latches`), battery in `BatteryModel`/`BatteryState`.
- Input: **F** = Sweep toggle, **Q** = Focus toggle, **L** = off; mouse buttons are deliberately
  NOT beam input (`ReadInput` passes `false, false` for the hold params). `InputLocked` freezes
  input while paused / in dialogue / not Exploration / transitioning.
- `ForceBeamOffOnEntry()` clears latches + forces Off on **every** room entry (no auto-activate).
- `AimBeam()` follows the cursor on the z=0 plane every frame (even while Off, but cone+mask
  GameObjects are inactive then → nothing renders).
- HUD: `HUDView` room line = `MAIN HALLWAY · LIGHTS: OFF - wall switch [E] · BEAM: OFF`,
  refreshed on `LightModeChanged` / `RoomEntered` / `BatteryChanged`.

## BASELINE VALUES to compare against at the end
**Hallway ambient (identical to every other room today):**
- lit alpha `0.12` (88 % brightness), dark alpha `0.62` (38 % brightness)
- → hallway is currently **0 % darker than a dark classroom** and only 57 % darker than a lit room
- visibility with the beam OFF: **unlimited** - the whole 22.8-unit corridor (and all 3 doors,
  lockers, clues) sits at a uniform 38 % brightness. No near-field pool exists.

**Stamp mark (the verdict mark stamped onto the slip):**
- `CheckpointView.Decision.cs`: `StampMarkSize = 56f` px, `StampMaxTilt = 18f`, `StampEdgePad = 2f`
  → `StampPlacement` clamp margin `TiltedHalfExtent(56,18)+2 = 37.28 px`
- slip sheet = 200 × 120 px (`L.CounterSlip`), mark/slip-width ratio = **28.0 %**
- `_stampOverlay` Image: `sizeDelta (56,56)`, `preserveAspect = true`, child of the slip's
  RectTransform (rides drag/flip/zoom), `raycastTarget = false`, seeded tilt `−12 ± 6°`
- art: procedural `stamp_allow` / `stamp_reject`, 168 × 168 canvas @ `UiPpu = 100`,
  double ring (r 74/64/62) + 12 px check or cross + 90 worn-ink gaps; allow ink = grey
  `(0.2,0.2,0.2,0.9)`, reject ink = `(0.72,0.12,0.16,0.92)`
- tray (the draggable tools, NOT the mark): `L.StampAllow (968,296,152,150)`,
  `L.StampDeny (1128,296,152,150)` - frozen wireframe grid, audited by FlowCheck
- no files in `Assets/_Project/Resources/Art/` (directory does not exist) → the procedural
  painter is the only stamp art today

## Test / lint / build commands
| Command | What |
|---|---|
| `bash Tools/Verify/verify.sh` | compile all `Assets/**.cs` vs REAL Unity 2022.3.62f1 assemblies (minimal + full 76-asm set) + `audit.py` static audit |
| `dotnet run --project Tools/LogicTests/LogicTests.csproj` | live pure-logic suite |
| `dotnet run --project Tools/FlowCheck/FlowCheck.csproj` | headless full-game flow + world loop (drives `ViewManager.TickWorld`, `PressKey`, `AimAt`) |
| `dotnet run --project Tools/Verification/LogicTests/LogicTests.csproj` | **frozen** suite (additive changes only) |
| `python3 Tools/generate_meta.py` | deterministic `.meta` GUIDs for new assets |

Sandbox needs: no dotnet/mono preinstalled → .NET SDK 8.0.404 unpacked to `/tmp/dotnet`,
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; Unity assemblies fetched to `/tmp/unitylibs{,_min}`.

## PRE-EXISTING BUILD BREAK FOUND (not caused by these tasks)
`1ac4c79` ("JP Patch") references three files that were **never committed on any branch**
(`git log --all -S` shows the strings appear for the first time in that commit):
- `Assets/_Project/Scripts/GameLogic/StampPlacement.cs`
- `Assets/_Project/Scripts/GameLogic/SpeechBubble.cs` (`SpeechBubbleClock` + `SpeechBubbleLayout`)
- `Assets/_Project/Scripts/UI/HoverCursor.cs`

At clone the project did **not compile** (Unity editor, `verify.sh`, LogicTests and FlowCheck all
failed). All three were reconstructed from their pinned contracts (README layout list,
REFACTOR_LOG entries, LogicTests/FlowCheck expectations) before any task work; see
`docs/HALLWAY-STAMP-PASS.md`.

### Baseline AFTER the reconstruction, BEFORE task edits
- `verify.sh`: compile clean (minimal + full) · audit clean (92 scripts, 14,911 lines)
- LogicTests: **27519 PASS / 0 FAIL**
- FlowCheck: **910 OK / 0 BAD**, exit 0
- frozen Verification/LogicTests: **663 passed / 0 failed**
