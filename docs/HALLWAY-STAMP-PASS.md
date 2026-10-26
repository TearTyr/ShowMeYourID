# Hallway limited visibility + smaller stamp asset — implementation report

Branch/repo: `github.com/TearTyr/ShowMeYourID`, cloned at `1ac4c79` ("JP Patch").
Companion baseline record: `docs/PHASE0-BASELINE.md`. Lighting sim: `docs/ui/hallway_lighting_sim.png`.
Stamp legibility preview: `Tools/stamp_preview.png` (regenerate: `python3 Tools/make_stamp_asset.py`).

---

## 0. A pre-existing build break had to be fixed first (documented gap)

At clone the project **did not compile** — in the Unity editor, in `verify.sh`, and in both live
test suites. `git log --all -S` proves the three missing types first appear as *references* in
`1ac4c79` and were never committed on any branch:

| Missing file | Reconstructed from |
|---|---|
| `Assets/_Project/Scripts/GameLogic/StampPlacement.cs` | its frozen expectations in `Tools/LogicTests` (`StampPlacementTests`) + `Tools/FlowCheck` placement proofs + `REFACTOR_LOG.md` §"stamp lands where you drop it" |
| `Assets/_Project/Scripts/GameLogic/SpeechBubble.cs` (`SpeechBubbleClock`, `SpeechBubbleLayout`) | `Tools/LogicTests` `SpeechBubbleTests` + `README` layout list + `CheckpointView.Speech` call sites |
| `Assets/_Project/Scripts/UI/HoverCursor.cs` | `REFACTOR_LOG.md` (Oct 2026 pass, "New: UI/HoverCursor.cs") + the UnityStubs `Cursor` swap stub + `FlowCheck` spine-handle proof |

All three were rebuilt **only** from those pinned contracts; nothing else was touched. After the
reconstruction the baseline is exactly the green state the project's own docs describe
(compile+audit clean, LogicTests all-pass, FlowCheck all-OK, frozen suite 663/663), which is what
makes the verification in §6 meaningful. `StampPlacement` is part of the stamp subsystem this task
owns; the other two are strictly required for the project (and therefore the harnesses) to build.

---

## 1. Phase 0 findings

**Engine / stack:** Unity `6000.3.0f1`, Built-in Render Pipeline, **no** 2D-lighting framework, no
post-processing volume, no shaders. Lighting is a hand-rolled **black-overlay + SpriteMask**
scheme; the desk is uGUI on a fixed 1280×720 grid (`CheckpointLayout`).

**Lighting framework (reused, not replaced):**
- per-room `darkness` quad, `Color.black`, sorting order **70**, `maskInteraction = VisibleOutsideMask`,
  alpha owned by `LightController` (`LitAlpha 0.12` / `DarkAlpha 0.62`);
- `FlashlightRig` handheld beam: cone `SpriteRenderer` (order 71, tint `(1, 0.96, 0.85, 0.30)`) +
  `SpriteMask` with custom range **60..70**, pure cone maths in `GameLogic/BeamGeometry`;
- `PeekLight` classroom lamp (mask range 60..80) — classroom peeks only, never the hallway.

**Hallway areas (one only):** `RoomId.Hallway` "MAIN HALLWAY" in `School.unity`, built by
`RoomBuilder` (`HalfWidth 11.4`, 3 doors, wall switch at −9.2). Classroom peeks frame classrooms;
`SecurityRoomView`/the desk are not hallways. Entries go through
`ViewManager.EnterRoomImmediate`, which already calls `LightController.ForceDarkOnEntry()` +
`FlashlightRig.ForceBeamOffOnEntry()` on **every** entry — the "never auto-activate on transition"
rule already held and is preserved.

**Flashlight system:** state = `FlashlightRig.Beam` + `FlashlightLatches`; input arbitration in
`GameLogic/FlashlightInput` (F = Sweep toggle, Q = Focus, L = off; mouse buttons are *not* beam
input by design); HUD sync through `EventIds.LightModeChanged` / `BatteryChanged` in `HUDView`
("MAIN HALLWAY · LIGHTS: … · BEAM: …"). Cursor-follow aiming already existed and is untouched.

**Stamp system:** `CheckpointView.Decision.cs` (`_stampOverlay` Image, child of the slip's
RectTransform; drop → `StampPlacement.DeskToLocal` → `ClampToPrintable`; `A`/`D` keyboard path;
approve = `stamp_allow`, reject = `stamp_reject`), art painted by `ProceduralArt.MakeStamp`,
tray regions in `CheckpointLayout` (frozen wireframe, left untouched).

**Asset conventions:** no binary art in the repo; `ProceduralArt.Get(key)` prefers a real file at
`Assets/_Project/Resources/Art/<key>.png` (README), `.meta` GUIDs from `Tools/generate_meta.py`
(TextureImporter block for `.png`).

**Baselines (before edits):** hallway ambient alpha `0.62` — identical to every room (0 % darker
than a dark classroom, 57 % darker than a lit one); visibility with the beam OFF: **unlimited**
(whole 22.8 u corridor at 38 % brightness); stamp mark `56×56` reference px on a 200×120 slip
(28 % of its width), clamp margin 37.28 px; no files under `Resources/Art/`.

---

## 2. Files modified

| Path | Change |
|---|---|
| `Assets/_Project/Scripts/GameLogic/HallwayDarkness.cs` | **new** pure module: corridor ambient levels, pool radii, smoothstep falloff, shroud world size, acceptance-ratio helpers |
| `Assets/_Project/Scripts/GameLogic/StampPlacement.cs` | **restored** (missing at HEAD): drop-point → slip-local mapping + printable-area clamp |
| `Assets/_Project/Scripts/GameLogic/SpeechBubble.cs` | **restored** (missing at HEAD): `SpeechBubbleClock` + `SpeechBubbleLayout` |
| `Assets/_Project/Scripts/UI/HoverCursor.cs` | **restored** (missing at HEAD): hand cursor on the ID's spine handles |
| `Assets/_Project/Scripts/World/LightController.cs` | per-room dark alpha; owns the depth layer (alpha + enable + per-frame player follow); flicker restore uses the per-room alpha |
| `Assets/_Project/Scripts/World/RoomBuilder.cs` | hallway builds the `depth_darkness` shroud (order 65, beam-mask range, `VisibleOutsideMask`) and passes the darker ambient; hallway extent now sourced from `HallwayDarkness` |
| `Assets/_Project/Scripts/World/RoomInstance.cs` | new `Depth` field (hallway-only layer handle) |
| `Assets/_Project/Scripts/Bootstrap/ProceduralArt.Rooms.cs` | new painter `MakeHallDepth()` — radial falloff baked in **world units** |
| `Assets/_Project/Scripts/Bootstrap/ProceduralArt.cs` | registers `hall_depth`; the Resources override path now names its sprite after the key (so a dropped-in PNG behaves exactly like procedural art) |
| `Assets/_Project/Scripts/Core/GameConfigSO.cs` | two new tunables `hallwayDarkAlpha` / `hallwayDepthAlpha` (defaults from `HallwayDarkness`), per the project's "new tunables go in GameConfigSO" rule |
| `Assets/_Project/Scripts/Checkpoint/CheckpointView.Decision.cs` | `StampMarkSize 56 → 39` (and the three constants became `internal` so the harnesses measure the real numbers) |
| `Tools/FlowCheck/Program.cs` | new W1 cursor-sweep proofs, new **W2b** hallway-visibility section, stamp-size proofs, corner-clamp now derived from the real constants, PNG-on-disk proof, `RepoRoot()` helper |
| `Tools/LogicTests/Program.cs` | new `HallwayDarknessTests()` group (pure-logic acceptance numbers) |
| `Tools/make_stamp_asset.py` | **new** deterministic generator for the stamp placeholder + legibility preview |
| `Assets/_Project/Resources/Art/stamp_allow.png` (+`.meta`) | **new asset** |
| `docs/PHASE0-BASELINE.md`, `docs/ui/hallway_lighting_sim.png`, `Tools/stamp_preview.png` | evidence / baseline records |

Nothing else was touched: anomalies, sanity, battery, spawn odds, save format, desk grid, peek,
HUD wiring and all controls are byte-identical.

---

## 3. Asset created

`Assets/_Project/Resources/Art/stamp_allow.png` — **PNG, RGBA (colour type 6), 128×128 px**,
transparent background (corner alpha 0), ink `rgb(62,71,62)` @ α 0.94.

- **Generation:** rendered deterministically by `Tools/make_stamp_asset.py` (PIL, 512 px
  supersample → LANCZOS to 128). AI raster generation was deliberately *not* used: the mark needs
  a true alpha channel, a muted flat ink, and the project's lo-fi worn-ink language — all of which
  a generator guarantees and reproduces (`--seed` re-rolls the wear).
- **Art direction:** double-ring stamped emblem + heavy check; muted low-saturation green-grey ink
  (the ACCEPT tray tint pulled dark) that sits with the warm paper stock and cool UI ink; worn
  press = low-frequency alpha mottle, ink-starved specks, one chip per ring.
- **Sized for the reduced display:** authored strokes are chunky (outer ring 8 px, check 14 px at
  128) so at the new **39 px** display the ring reads ≈2.4 px and the check ≈4.3 px
  (`Tools/stamp_preview.png` shows 39 / 56 / 58 px side by side).
- **Integration:** no code reference needed — `ProceduralArt.Get("stamp_allow")` prefers
  `Resources/Art/stamp_allow.png` automatically (the project's documented override path); the one
  added line (`spr.name = key`) makes the override indistinguishable from procedural art for
  name-based lookups and the headless proofs. `.meta` = `generate_meta.py` output plus two
  deliberate keys (`alphaIsTransparency: 1`, `textureCompression: 0`) for a transparency- and
  legibility-critical UI mark.
- **Backup:** no existing file was replaced — `Resources/Art/` did not exist and the procedural
  `MakeStamp(false)` remains in place as the runtime fallback (a strictly better backup than a
  `.bak` copy). `stamp_reject` keeps its procedural art, so the pair stays stylistically matched.

---

## 4. Lighting changes (before → after)

| Value | Before | After |
|---|---|---|
| hallway ambient darkness alpha | 0.62 (same as rooms) | **0.76** (`hallwayDarkAlpha`) |
| …as a ratio to a lit room (0.12) | 57 % darker | **73 % darker** (band 70–85 %) |
| …vs a dark classroom | identical | 37 % darker (24 % vs 38 % brightness) |
| corridor beyond the pool | 38 % brightness (whole corridor visible) | **6.7 % brightness** (black) |
| visibility with beam OFF | unlimited | pool of **2·1.9 u = 3.8 u ≈ 21 %** of the 17.78 u view (band 15–25 %) |
| falloff | none | smoothstep 1.9 u → 6.0 u, zero slope at both ends (no hard edge) |
| wall switch ON | 0.12 | 0.12, **and the pool is removed** (corridor fully visible, as before) |
| beam tint / cursor-follow / HUD | unchanged | unchanged |

Mechanics of the change, reusing the existing framework only:
- the hallway's base darkness quad keeps its existing role/size/order (70) but takes a deeper
  per-room alpha;
- a **second black layer** (`depth_darkness`, order 65, inside the beam mask's 60..70 range, so one
  beam hole cuts both) carries a radial alpha gradient **baked in world units** and follows the
  player each frame (`LightController.FollowPlayer`, struct maths only — no per-frame allocations);
- because the layer *adds darkness* instead of adding light, the OFF state shows **no light
  circle, no beam, no cursor glow anywhere** — `FlashlightRig.ApplyBeam(Off)` still deactivates
  cone + mask, verified;
- the gradient is a hole-free smoothstep, so the pool has a gradual radial falloff with no rim;
- classrooms keep their single uniform layer (asserted), peek stays solid black, sanity drain /
  spawn odds / battery recovery still read only `Light.Lit` (unchanged semantics).

---

## 5. Stamp changes

- `StampMarkSize` **56 → 39** reference px = **−30.4 %** (band 25–35 %). Square `sizeDelta` +
  `preserveAspect` → no stretching; slip-width coverage **28 % → 19.5 %**, so it hides *less* of
  the REASON line than before; still legible at 39 px (preview above).
- Clamp margin follows automatically: `TiltedHalfExtent(39, 18°) + 2 = 27.9 px`, so the tilted mark
  stays fully on the 200×120 sheet (sweep proof in LogicTests/FlowCheck).
- Placement preserved exactly: drop-point → slip-local mapping, one stamp per slip, mark is a child
  layer of the slip (rides drag/flip/zoom), keyboard default (80 %/68 %) unchanged, tray regions
  untouched (frozen wireframe).
- Integration points: `PlaceStampMark` (sizeDelta), `TryStampOnDrop` / `KeyboardStampSpot` (clamp),
  `ProceduralArt` (art override), `Tools/FlowCheck` (harness now reads `CheckpointView.StampMarkSize`
  instead of hardcoded 56/18/2 literals, and the corner-clamp expectation is derived from
  `TiltedHalfExtent`).

---

## 6. Verification results

Suites (all run in-sandbox; .NET SDK 8.0.404 + real Unity 2022.3.62f1 assemblies fetched):

| Suite | Before task edits (post-reconstruction) | After |
|---|---|---|
| `verify.sh` compile (minimal + full 76-asm) + `audit.py` | clean | **clean — 0 errors, 0 warnings, audit clean (93 scripts, 15,119 lines)** |
| `Tools/LogicTests` | 27 519 / 0 | **27 533 PASS / 0 FAIL** |
| `Tools/FlowCheck` | 910 OK / 0 BAD | **936 OK / 0 BAD, exit 0** |
| `Tools/Verification/LogicTests` (frozen) | 663 / 663 | **663 passed / 0 failed** |

Per acceptance item (measured):

**Task 1**
- Entering hallway → flashlight OFF, hallway dark: OK (W0/W2: `Beam == Off`, latches cleared; hallway ambient **0.76** vs classroom 0.62).
- OFF → no visible light circle / beam / cursor glow: OK (W1 "untouched beam shows no cone, no mask, no cursor-following glow"; the depth layer only *adds* darkness).
- F → limited illumination, soft falloff: OK (W1 cone+mask active; `docs/ui/hallway_lighting_sim.png` shows the smoothstep pool; LogicTests asserts monotone, no single step > 0.12).
- F again → illumination fully removed: OK (cone + mask `activeSelf == false`; `ConeCovers` false everywhere).
- Repeated entries → never auto-activates: OK (W2 entry-reset arc ×3 rooms + W2b "no lighting change ever auto-activated the beam").
- Mouse movement alone never activates: OK (W1: held LMB, held RMB, **plus a 28-frame cursor sweep** — beam stays Off).
- Movement/interaction/HUD work in darkness: OK (interaction is distance-based, prompts unaffected; HUD room line asserted in sync; player + switch plate render above the darkness by design).
- Cursor-follow still works while ON: OK (W3 aiming/cone proofs unchanged and passing).
- Hallway 70–85 % darker than a lit room: **73 %** (asserted in LogicTests + FlowCheck W2b).
- Pool 15–25 % of visible width: **21 %** (asserted both suites).

**Task 2**
- ~30 % smaller: **39×39 px, −30.4 %**, square (FlowCheck SP).
- New asset integrated & consistent: PNG on disk proven by FlowCheck (signature + colour type 6 + `.meta` present); preview shows the same worn-ink language as the reject stamp.
- Readable at the new size: `Tools/stamp_preview.png` (39/56/58 px on the paper stock); ink coverage 35 % of the 39 px downscale.
- Stamps at the click location: OK (SP centre drop on a 1.5×-zoomed slip → (98.7, 62.1) = the mapped drop, not a corner).
- Moves with the slip when dragged: OK (SP drag proof: mark's slip-local spot unchanged, desk position moved > 50 px).
- Stays within the paper bounds: OK (tilted-extent bounds proof at the corner: clamped exactly to margin **27.9**).
- Approve/reject both preserved: OK (SP runs both verdicts across the suite; tray untouched).
- Original asset backed up: N/A — nothing was replaced; the procedural painter remains as fallback.

---

## 7. Not implemented / could not verify headlessly

- No Unity editor in this sandbox, so the **rendered** SpriteMask stencil behaviour, real
  bilinear/mipmap filtering of the PNG, and Play-mode frame timing are editor-side checks.
  Mitigations: the layer orders/mask range were chosen strictly inside the shipped beam-mask
  range (65 ∈ [60,70]); the PNG mirrors the project's existing sprite settings (RGBA32-equivalent,
  bilinear, no mip chain) and its legibility was measured by downscaling, not guessed; the lighting
  loop adds one `localPosition` write per frame for one room.
- `Resources.Load` is stubbed to `null` headlessly, so FlowCheck proves the PNG by file inspection
  (signature, colour type, meta) rather than by loading it; in-editor the override path is the
  documented, already-shipped mechanism.
- Photosensitivity flicker interaction with the new alpha was verified by code path only (the
  flicker coroutine now restores the per-room alpha; it only runs while `Lit`).

## 8. Risks / follow-ups

- The depth layer's baked radii live in `HallwayDarkness` (they are painted into `hall_depth`);
  the two alphas are the only runtime tunables (`GameConfigSO`). Changing `HallwayHalfWidth` or the
  player clamp now automatically resizes the shroud through the shared constants, but repainting
  the sprite still requires a code edit — noted in the config tooltip.
- With the shipped near-black placeholder art the pool is subtle by eye (measured 8/255 near vs
  2/255 far on walls); the mid-tone row of `docs/ui/hallway_lighting_sim.png` shows how it reads
  once real art lands. If the team wants a brighter pool, `hallwayDarkAlpha` is a slider in
  `GameConfig.asset` (keep it ≥ 0.736 to stay inside the 70–85 % band).
- The three reconstructed files (§0) should be reviewed against the team's own copies if they
  exist off-repo; they are pinned to the frozen test contracts, which all pass.
