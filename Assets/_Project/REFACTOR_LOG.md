# Refactor log — close-the-gaps pass (branch refactor/close-gaps)

## Audit (pre-edit) — raw output
    grep -rn "private GameRoot R;"            -> 18 hits (16 files; Interactables used protected)
    grep -rn "Part of the .* folder"          -> 60 hits
    grep -rn ".fontSize = " (excl UIFactory)  -> 2 hits (CheckpointView:195, SettingsView:107)
    grep -rn "SfxRequested|MusicContextRequested" -> 0 hits
    ls REFACTOR_LOG/ARCHITECTURE/STYLE        -> all three missing
    wc -l World/ViewManager.cs                -> 424 (prompt said ~490)
    grep -n "_index = -1;" CheckpointView     -> 2 hits (field init + BeginRun)
    MenuScreens.cs / HudAndCommon.cs          -> present as empty stubs (23 / 18 lines)
    comment baseline (clean tree)             -> added 0 / removed 0
Discrepancies vs the prompt, logged and worked around: MapView.cs / ShiftResultsView.cs /
EndingView.cs do not exist as files (those classes live in ProgressionScreens.cs, which was
renamed); ViewManager was 424 lines not ~490. Neither changes what any fix does.

## Symptom 0 — diagnostic log (headless FlowCheck harness; no Unity in this sandbox)
== NEW GAME flow, headless ==
[LOG] [Flow] SetState MainMenu
[LOG] [Flow] ApplyVisibility MainMenu
  OK   menu boots
[LOG] [Flow] NEW GAME clicked
[LOG] [Flow] loading scene: Hollowbrook
[LOG] [Flow] checkpoint bootstrap built (checkpoint=True loading=True dialogue=True)
[LOG] [Flow] queued action consumed in scene 'stub'
[LOG] [Flow] SetState ShiftIntro
[LOG] [Flow] ApplyVisibility ShiftIntro
[LOG] [Flow] intro shown (loading=True dialogue=True)
[LOG] [Flow] dialogue finished, invoking callback
[LOG] [Flow] intro callback fired
[LOG] [Flow] SetState Checkpoint
[LOG] [Flow] ApplyVisibility Checkpoint
[LOG] [Flow] BeginRun roster=4
[LOG] [Flow] NextVisitor index=-1
[LOG] [Flow] PresentVisitor Ana Delgado
[LOG] [Flow] intro done -> checkpoint state
  OK   state is Checkpoint after the intro
  OK   a visitor is at the window
== Symptom 0 structural proofs ==
  OK   B4: dialogue OUTER border rect is inactive after the intro (no grey fullscreen rect)
  OK   B3: windowframe sibling 122 is BEHIND visitor 138 and speech 140
== 1280x720 layout audit (same math as UIFactory.Anchor) ==
  OK   question column on screen (990,60)-(1270,260)
  OK   question 1 on screen (994,206)-(1266,246)
  OK   question 4 on screen (994,74)-(1266,114)
  OK   questions panel on screen (980,52)-(1280,302)
  OK   record sheet on screen (10,340)-(330,620)
  OK   record row NAME on screen (10,374)-(230,394)
  OK   record row EXPIRES on screen (10,518)-(230,538)
  OK   id card on screen (687,517)-(973,703)
  OK   camera monitor on screen (1014,535)-(1266,685)
  OK   allow circle on screen (1005,410)-(1125,530)
  OK   principal circle on screen (1135,410)-(1255,530)
  OK   visitor portrait on screen (565,53)-(715,243)
== B8 save/continue resume ==
[LOG] [Flow] NextVisitor index=0
[LOG] [Flow] PresentVisitor Carlo Mendoza
[LOG] [Flow] loading scene: MainMenu
[LOG] [Flow] loading scene: Hollowbrook
[LOG] [Flow] checkpoint bootstrap built (checkpoint=True loading=True dialogue=True)
[LOG] [Flow] queued action consumed in scene 'stub'
[LOG] [Flow] SetState ShiftIntro
[LOG] [Flow] ApplyVisibility ShiftIntro
[LOG] [Flow] intro shown (loading=True dialogue=True)
[LOG] [Flow] dialogue finished, invoking callback
[LOG] [Flow] intro callback fired
[LOG] [Flow] SetState Checkpoint
[LOG] [Flow] ApplyVisibility Checkpoint
[LOG] [Flow] BeginRun roster=4
[LOG] [Flow] NextVisitor index=0
[LOG] [Flow] PresentVisitor Carlo Mendoza
[LOG] [Flow] intro done -> checkpoint state
  first visitor before quit: Ana Delgado | first visitor after CONTINUE: Carlo Mendoza
  OK   B8: CONTINUE resumes at the next visitor, not the same one
== FLOW CHAIN COMPLETE ==

Classification: every transition line is present, so the state machine was never the failure.
The symptom was visual, in the order Part 0.4 predicts: B4 (full-screen grey border left active by
DialogueView.HideVisuals) then B3 (window frame drawn over the portrait) then B2 (question column
children offset +140 on a centre anchor => off the right edge; record rows inverted and half below
the sheet; monitor below the bottom edge).

## Symptom 0 — evidence (post-fix)
Screenshot: NOT RUN — no Unity editor exists in this environment, so no pixel capture is possible.
Substituted evidence, all from the harness above plus a rect audit using the same math as
UIFactory.Anchor at 1280x720:
    question column   (990,60)-(1270,260)      questions panel (980,52)-(1280,302)
    question 1        (994,206)-(1266,246)     question 4      (994,74)-(1266,114)
    record sheet      (10,340)-(330,620)       row NAME (10,374)-(230,394)  row EXPIRES (10,518)-(230,538)
    id card           (687,517)-(973,703)      camera monitor  (1014,535)-(1266,685)
    allow circle      (1005,410)-(1125,530)    principal circle (1135,410)-(1255,530)
    visitor portrait  (565,53)-(715,243)       window frame sibling 122 < portrait 138 < speech 140
    dialogue outer border inactive after intro: True
Row NAME sits above row EXPIRES and both are inside the sheet, so the record reads top-to-bottom.
No Debug.LogError or exception appears anywhere in the harness output.

## NEW GAME flow, verified (headless)
1. menu -> intro dialogue: OK (SetState ShiftIntro, intro shown)
2. intro advances on Advance() (click/Space/Enter/E path): OK (8 advances drained it)
3. checkpoint fully visible: OK by rect audit above (portrait in front of window, docs on screen,
   4 question buttons + 2 circles + monitor on screen, no grey rect)
4. question -> speech panel answer: verified by LogicTests (QuestionSystem) + AskQuestion path unchanged
5. ALLOW / CALL PRINCIPAL -> stamp + banner: verified by LogicTests (InspectionRules) + Stamp path unchanged
6. next visitor after banner: OK (PresentVisitor Ana Delgado; B8 run shows Carlo Mendoza next)
Pixel-level confirmation of 1-6 in a real editor: NOT RUN (steps listed at the end of this log).

## B8 — save/continue verified
first visitor before quit: Ana Delgado | first visitor after CONTINUE: Carlo Mendoza (headless run above)

## Bugs closed this pass
2a question column anchor + child +140 offset (rect audit) | 2b record rows by index (rect audit) |
2c BeginRun resume index no longer overwritten (B8 run) | 3a two fontSize writes -> SetBaseFontSize (grep 0) |
3b R -> _gameRoot in 17 files (grep 0, compile per file) | 4 ViewManager split 424 -> 210 lines (compile + FlowCheck) |
5a stub files + metas deleted (ls absent) | 5b 60 boilerplate comments deleted (grep 0) | 5c no headers added.

## Comment budget
added 7 / removed 61 -> PASS (added <= removed)

## Reviewed but not changed
B1 audio direct calls already complete | B3 SetAsLastSibling already correct | B4 BorderedPanel already
returns outer | B6 catch/token guard already correct | B7 single flashlight owner already correct |
B9 checkpoint subscriptions already disposed | B10 behavior-oddity gate already correct | B11 map in school
already correct | B12 mask graphic already present | CheckSceneHandshake/_queuedAt/_handshakeArmed/SafeBuild
kept untouched (they are the detector for exactly this symptom).

## Behavior changes
Only B8: a mid-shift Continue now resumes at the saved visitor instead of replaying the night.
Save format gained a tolerant visitorAtSave= line; old saves read as 0 (previous behaviour).

## Manual Unity steps (NOT RUN here)
Play MainMenu -> NEW GAME: expect the [Flow] chain in Console, visitor behind glass, four question
buttons clickable on the right, ALLOW / CALL PRINCIPAL circles, CAMERA bottom-right; answer one
question; stamp; next visitor arrives. Quit mid-shift and CONTINUE: second visitor, not the first.

## Checkpoint UI Layout Verification (1280x720, computed with the same math as UIFactory.Anchor)
Screenshot: NOT RUN - no Unity editor in this sandbox. The bounds below are computed from the exact
anchor/size/center values the code sets, which is what GetWorldCorners would report for a
ScreenSpaceOverlay canvas at the reference resolution.

    element            rect                 element            rect
    top bar            (0,0)-(1280,52)      questions panel    (960,65)-(1260,315)
    pause / clock      (15,10)-(105,42) / (1160,10)-(1280,42)   question 1..4  (974,109)-(1246,281)
    banner + NIGHT     (10,36)-(330,80)     testimony          (980,261)-(1280,341)
    record sheet       (10,80)-(330,420)    utility grid 2x2   (955,342)-(1265,424)
    record rows 1..7   (10,374)..(10,538)   stamp circles      (960,490)-(1240,610)
    id card            (10,450)-(330,670)   camera + SCAN      (984,588)-(1236,708) / (955,342)
    window frame       (405,6)-(875,346)    speech panel       (490,415)-(790,525)
    portrait (in win)  (565,55)-(715,245)   principal note     (490,530)-(790,630)
    patrol / end shift (470,352)-(900,388)  objective          (360,672)-(920,708)
    HUD sanity/track/battery  (180,8)-(440,44) / (530,8)-(750,44) / (750,8)-(970,44)
All 22 harness checks OK, 0 BAD: every element inside 0..1280 x 0..720. Pairwise: no unintended
overlaps; intended ones are text-on-panel and the modal confirm box over the window.
FlowCheck re-run after the change: 22 OK / 0 BAD, B8 resume Ana Delgado -> Carlo Mendoza.

### Deviations from the task's literal coordinates (its own numbers overlap; logged per instruction)
- Record sheet kept at bottom-left (170,470) but the banner stays at (170,-58): the task's note
  position (170,-280) would sit on top of the record sheet, so the PRINCIPAL NOTE moved to the free
  centre-bottom slot (0,140) above the objective.
- Stamp circles at (-100/-260,170) from bottom-right instead of 80: at 80 they landed on the camera
  panel; the camera sits at (-170,72) size 252x120 below them.
- SCAN joined the 2x2 utility grid (4th slot) instead of floating at (-344,277), which is where the
  screenshot showed it stacked over the camera and id card.
- HUD meters moved into the top bar centre: at their old top-left spot they covered the banner and
  facade (visible in the provided screenshot), and the bottom-left strip belongs to the id card.
- The facade art is now the view THROUGH the window (child of the window frame) because the left
  column is fully occupied by banner/record/id; the confirm box overlays the window centre as a modal.

## Layout redo, from the ground up (branch fix/ui-layout-redo) — SUPERSEDES the table above

Root causes found behind "the UI is rotated invertedly" and the remaining overlaps:
1. ProceduralArt.Canvas2D draws row 0 as the TOP, but Texture2D.SetPixels reads row 0 as the
   BOTTOM of the texture. Every procedural sprite therefore rendered vertically mirrored:
   faces grew hair under the chin, the ID card wore its barcode on top and its header band at
   the bottom, the record sheet showed its stamp strip as a header. ToSprite now flips rows.
2. CheckpointLayout mixed centre-offset and edge-offset constants against UIFactory.Anchor's
   centre-pos convention, so panels drifted into each other (note over speech, camera under the
   stamps, meters inside each other, objective text bleeding over the ID card).

New rule: one grid, absolute 1280x720 regions, top-left origin, as the mockup reads.
CheckpointLayout.Region + Place(rt, region, pin) + PlaceLocal(rt, x, y, w, h) (children always
measured from the parent's top-left). Pins attach each region to the nearest screen corner so
other aspect ratios widen the gutters instead of colliding columns.

    top bar y4..48    banner 16..336 | sanity 348..628 | light 636..856 | track 864..984 |
                      clock 1000..1150 | pause 1160..1264
    row   y56..80     objective 16..476 (exploration) | run stats 490..830 | findings 1016..1264
    left  x16..336    record 64..354 | id card 362..582 | checklist 590..712
    centre            window 88..404 | sill 404..428 | speech 436..546 | note 554..664 |
                      patrol/end-shift 672..708
    right x968..1264  questions 88..348 | utilities 356..432 (2x2) | stamps 440..550 |
                      camera monitor 558..704

Checklist rows left the record sheet (its printed rules hold the seven data rows) and became
their own paper panel with paper-toned buttons; record/ID headers sit inside the sprites' dark
header bands; the monitor keeps caption and scanner output on separate rows.
FlowCheck's audit now replays the pin math for all 28 regions: placed == grid, on screen, and
every co-visible pair disjoint (state-tagged C/E/B). 27,125 logic assertions still pass.
Play-mode and screenshot checks: NOT RUN (no Unity editor in this sandbox).

## Desk == wireframe rebuild (branch fix/ui-layout-redo) — SUPERSEDES the pin system above

The corner-pin Place(rt, region, pin) still let panels drift: AspectMatchGuard keeps the canvas
at or above 1280x720, so on any non-16:9 view the canvas reference rect grows and regions pinned
to different corners moved apart (checklist/note toward the floor, right column toward the edge,
objective text across the record sheet). The pins are gone.

New placement contract (exactly as the approved wireframe script reads):
- ONE fixed 1280x720 DeskFrame per view (CheckpointView "DeskFrame", HUDView "HudFrame"),
  centred in the canvas, never edge-anchored. Non-16:9 screens grow the background gutters
  around the frame; nothing inside can drift any more.
- ONE placement helper: UIFactory.PlaceTL(rt, x, y, w, h) — anchor top-left, pivot (0,1),
  position (x, -y). CheckpointLayout.Place(rt, region) forwards to it; views contain no
  placement math and no magic numbers (the whole table, incl. panel-local sub-rects, lives in
  CheckpointLayout / CheckpointLayout.Inner).
- DeskLayoutAudit (new): every placed element is registered with its expected rect; a one-time
  editor-only Start() logs actual vs expected and warns on any difference over 2px. FlowCheck
  drives CheckAll() headlessly: DeskFrame 28/28, HudFrame 8/8, zero warnings.

Restored / upgraded to the wireframe spec:
- Speech box is a rolling two-sided log (490,436,300,110): visitor lines under AT THE WINDOW,
  player questions under YOU, answers below, newest at the bottom, last ~3 exchanges kept,
  RectMask2D-clipped so text can never overflow the panel.
- PATROL HALLWAY and END SHIFT stay visible per the wireframe; they dim (SetButtonEnabled)
  until usable instead of vanishing. END SHIFT lights up at roster end; exhibition mode clocks
  out into the curated ending, normal mode into shift results.
- Hint/objective line is exploration-only (wireframe note), findings right-aligned at x=1264,
  run stats + challenge bulletin got their own row-2 slots (400..810) clear of both.
- Challenge bulletin: strip shows a one-line annunciator; the FULL rule text goes to the speech
  log as a DISTRICT BULLETIN entry and remains printed in the (now scrollable) RULES NOTE.
- Flat wireframe art: record sheet and ID card are reprinted 1:1 at panel size (flat paper,
  2px flat border, header band, rules, stamp box, barcode — no speckle noise, no stretch);
  window frame flat #26262b with flat glass #3a3a40; ALLOW / CALL PRINCIPAL are flat circles
  (paper / dark, 2px rim); paper overlays use a plain flat sheet. rough_panel / rough_circle
  are no longer used at the desk.
- Principal note, RULES NOTE and TONIGHT'S LOG bodies are RectMask2D + ScrollRect (draggable
  when longer than the box); record/ID/checklist panels carry RectMask2D as a spill safety net.

FlowCheck now also proves: table == wireframe digit-for-digit (independent transcription),
co-visible regions disjoint, glass/portrait/glitch contained by the window, all 40 required
desk/HUD elements exist by name, and a button-level desk loop — gating (allow/reject live,
patrol/end-shift dimmed mid-inspection), SCAN readout, CALL PRINCIPAL popup No/Yes paths,
PATROL refused mid-inspection, ALLOW click path, roster walk, END SHIFT clock-out.
Verification: compile 0 errors/0 warnings (minimal + full Unity assembly sets), FlowCheck
504 OK / 0 BAD, LogicTests 27,125/27,125.
UnityStubs gained RectMask2D (one-line stub) so FlowCheck keeps compiling the real view code.

Resolution math (fixed centred frame under AspectMatchGuard):
- 1280x720 and 1920x1080 are exactly 16:9 → canvas reference == 1280x720 → the frame fills
  the screen; geometry is pixel-identical to the wireframe at both.
- 1366x768 (aspect 1.7786) → canvas reference 1280.6x720 → frame centred with 0.3px side
  gutters; sub-pixel, layout unchanged.
- Any other aspect: desk stays whole and centred, background fills the gutters.

Play-mode runs and screenshots at 1280x720 / 1920x1080 / 1366x768: NOT RUN (no Unity editor
in this sandbox) — the rect audit above is the headless substitute; visual sign-off needs one
editor session against desk_layout_wireframe.svg.

---

# Flashlight + roaming anomaly rework (branch feat/flashlight-anomaly-rework)

Task: turn the FNAC3-inspired flashlight/anomaly prototype into one coherent, playable, testable
horror mechanic without new art, packages or parallel systems. Phase-0 baseline audit (every
defect traced in the working tree, not the reference dump): `docs/FLASHLIGHT-ANOMALY-AUDIT.md`.

## What was broken (verified at 64dd4ab before touching anything)
- `ConeCovers` was a rectangle test in mixed local/world spaces; the sprite flipped but never
  rotated toward the cursor, and the mask never flipped with it — visible beam and logical
  coverage disagreed everywhere except pure-horizontal aim.
- F/Q latches were not mutually exclusive (F looked dead while Q was latched); battery death
  kept the latches, so the beam AUTO-RELIT itself at the reboot threshold; input ran during
  dialogue/overlays/transitions.
- `ApplyBeam` wrote the room's `LightController` (Sweep silently switched room lights on),
  `IsIlluminated` only worked while the room mode was the flashlight's own Focus, `PlaceInRoom`
  reset room lighting on every door, battery recovery and sanity drain read that tangled state,
  and the phantom flicker restored lights-on unconditionally.
- `TickTracking`'s reveal at meter 1.0 set `_revealed` BEFORE `TickStates` ran, so
  `AnomalyEncounter` could never reach Repelled/Recovering — the whole state machine, retreat,
  `ScheduleReturn` and `JustRepelled` were dead code. A "repelled" anomaly froze in place,
  dropped its token and faded next to the player; movement never consulted `_state`.
- `coverHugCatchSeconds` was the catch grace for ALL four kinds; Sprint telegraphed once at
  spawn then dashed forever with no warning cue; CoverHug spawned at the ceiling and was
  immediately clamped to the floor line by contradictory coordinates.
- Anomalies were parented to room roots: leaving froze them (lifetime stopped too) while the
  stale `_liveAnomaly` slot blocked ALL future spawns; `ScheduleReturn(kind)` ignored its kind
  and re-peeked the pool; opening the MAP called `HideWorld` → `DiscardLive`, wiping the live
  encounter; cross-room x-distance fed sanity drain and catch checks.

## The rebuild
Pure engine-free logic (GameLogic, unit-tested in LogicTests):
- `BeamGeometry` — true cone test (forward distance + perpendicular offset + body radius),
  per-mode range/half-angle; ONE coordinate space (world-root plane) for every query.
- `FlashlightInput` — latch/hold arbitration table: F/Q exclusive, L clears, mouse holds are
  momentary overrides that release back to the latch, dead battery forces Off AND clears
  latches (revival requires deliberate input).
- `BatteryModel` — drain/recover/death/reboot with exactly-once transitions, clamped 0..1.
- `AnomalyBehaviorRules` — per-kind profiles: speeds, vertical bands (spawn INSIDE the clamp
  band), threat range, per-kind catch graces, Sprint telegraph/dash/recover, flags
  (FreezesWhenObserved, CeilingLurker, BeamInterruptsBurst), retreat speed.
- `RoomGraph` — hallway-hub connectivity + door anchors; RoomBuilder now places doors from it
  (single source), and off-stage anomalies patrol through it (no teleports).
- `TrackingPolicy` reduced to the rate model: Focus ADVANCES, any observation (Sweep beam, lit
  room, 0.35s grace) only HOLDS, darkness decays. Ambient light can never resolve an encounter.
- `AnomalyEncounter` stays the frozen-suite-compatible authority (additive only): `ExposeMeter`
  moved into tuning; `Threatening` now judges the NEW state, so the repel tick can never also
  catch you; dark+untracked settles to Roaming.

Unity glue:
- `FlashlightRig` — mouse projected onto the z=0 plane, cone sprite AND mask rotated/scaled
  onto the aim vector (scale maps the fixed sprite onto the mode's logical geometry), input
  locked while paused/dialogue/transitioning/non-exploration, battery via BatteryModel reading
  the room's REAL `Lit` state. It never writes room lighting; `SyncRoom` is gone.
- `LightController` — room-only: `Lit` bool, wall switch (`Toggle`), visual-only flicker that
  can never reach beam state; `LightModeChanged` keeps its event id (payload now the bool).
- `ViewManager` — the only place beam+ambient combine: `BeamCovers` / `IsFocusLit` /
  `IsIlluminated` / `RoomLit`; persistent `AnomalyRoot`; `HideWorld` no longer discards the
  live anomaly (map/security overlays pause an encounter instead of wiping it); `TickWorld`
  gives headless frames a deterministic step order.
- `WanderingAnomaly` — the `_revealed` bypass is deleted; the encounter state machine is the
  single path. Repelled = physical retreat to the far wall (movement, threat and catch timers
  all stop), then Recovering hands off to the spawner and despawns; token + pool resolution
  exactly once per entity (`_resolved`/`_gone`). Off-stage: patrols toward the player's room
  through doors (announced on arrival, never drawn over another room), meter/state keep
  ticking so a followed threat de-escalates; lifetime pauses off-stage and mid-resolution;
  proximity sanity/threat are room-gated. Behaviors: Blink creeps only unobserved (any beam or
  room light pins it, and a pinned Blink cannot catch you); Sprint telegraphs (announced once)
  → dashes → recovers, beam coverage interrupts the windup; Slide glides to committed points
  and flanks while stalking; CoverHug lives in its ceiling band and only Focus turns it clear.
  Hallucinations keep the separate Focus-seconds truth-check — never pool/tokens/catch.
- `RoomSpawner` — single live slot (duplicate spawns return null), recovery returns are a
  plain-field timer ticked in `UpdateSpawner` (no coroutine races; slot-busy re-arms instead
  of double-spawning), returns bring back the SAME kind into a plausible OTHER room,
  `DiscardLive` (desk exit) stays unresolved by design and cancels pending returns.
- `SanityController` reads ambient `Lit` (the room-Focus branch is gone); `HallucinationSystem`
  restores the player's own switch position after a phantom flicker; HUD room line shows
  LIGHTS: ON/OFF + the F/Q/L beam legend; ControlsView lists the real controls.
- `GameConfigSO` gained the beam geometry, encounter rates/thresholds and per-behavior
  numbers (defaults sourced from the logic constants; `Resources/GameConfig.asset` updated in
  lockstep; `coverHugCatchSeconds` now genuinely only applies to CoverHug; Sprint telegraph
  0.9s → 1.1s).

## Behavior changes (documented)
- A lit room / Sweep beam now HOLDS the tracking meter instead of slowly filling it — only
  Focus can expose and repel (the task's "detection is not victory").
- Dark rooms drain sanity at the full rate again even with the beam on (the old 0.25× branch
  existed only because the flashlight wrote Focus into the room light).
- Repelled anomalies retreat and leave, returning as the same behavior elsewhere after
  `anomalyRecoverySeconds`; previously they froze and faded in place.
- Opening the map/security overlay mid-encounter pauses it instead of destroying it.
- A returning threat can drop its own token if repelled again (one token per successful
  repulsion; pool resolution is idempotent per kind instance).

## Verification (all green at this commit)
- `Tools/Verify/verify.sh`: compile 0 errors/0 warnings (minimal + full Unity assembly sets),
  static audit clean.
- `Tools/LogicTests`: 27,225/27,225 (was 27,125) — new groups: BeamGeometry (center/edge/
  behind/range/radius/mode-difference/diagonal), FlashlightInput arbitration table,
  BatteryModel (rates, clamps, exactly-once death/reboot, deliberate relight), BehaviorRules
  (four distinct profiles, band consistency, per-kind graces), RoomGraph (connectivity, hops,
  door digits == RoomBuilder), AnomalyEncounter addenda (ExposeMeter tunable, repel tick not
  threatening, recoverable Exposed); TrackingPolicy group rewritten to the hold/decay model.
- `Tools/FlowCheck`: 647 OK / 0 BAD across 5 consecutive runs (was 504) — the desk suites are
  untouched, plus the new headless WorldLoop (W0–W9) driving the REAL components through
  `ViewManager.TickWorld` frames: input arbitration; light/beam independence; aim + cone
  coverage (incl. cone/mask alignment and ~90°/~180° sprite rotation); battery drain rates,
  death once, no auto-relight, deliberate reboot, lit-vs-dark recovery; the full
  spawn→detect→expose→lose-focus→re-expose→repel→retreat→recover→same-kind-return loop with
  token + pool exactly-once; all four behavior identities; repel-beats-catch race; pause
  freeze; cross-room roaming (door-bounded steps, announced arrivals, through-wall sanity
  immunity, mid-encounter exit + follow + re-engage); single-slot invariant under 45s of
  ambient spawn pressure; desk handoff discard semantics.
- Frozen `Tools/Verification/LogicTests`: 663/663, untouched (`AnomalyEncounter.Step`
  signature and its whole transition table preserved; only additive changes).
- UnityStubs: one additive change (`Input.MouseHeld` set + `GetMouseButton` reads it; default
  empty == old behavior) so mouse-hold modes are testable headlessly.

## NOT RUN (no Unity editor in this sandbox)
- Play-mode verification of the rendered beam: real `SpriteMask` reveal through the darkness
  overlay, sprite rotation/scale visuals, `ScreenToWorldPoint` against the live camera, and
  photosensitivity flicker. Repro: open `School.unity`, press Play, patrol (F/Q/L + mouse
  holds), watch the cone follow the cursor with the mask hole aligned, then run the W-list
  from `Tools/FlowCheck` manually (each check maps 1:1 to an editor action).
- Manual 12-step test sequence from the task: the headless WorldLoop covers steps 1–12
  behaviorally; the visual half (what the beam LOOKS like) needs one editor session.

# Checkpoint queue, missing students, sticky note + audio-only Options (branch feat/queue-options-rework)

Baseline audited at `f9bdffd`; findings and decisions in `docs/QUEUE-OPTIONS-AUDIT.md` (A1–A16).
Phases 9–11 of the additional agentic development task.

## What was replaced (verified in the baseline audit)

- `CheckpointView` walked the roster with ONE `_index` field that conflated roster cursor, arrival
  count, processed count, "current" and completion; `AfterBanner` hard-coded a second waiting
  period (`BetweenSeconds + 2.2f`) after every verdict; nothing but the cursor survived patrol;
  saves carried a single `visitorAtSave` int. No queue pressure, no overwhelmed state, no
  missing students, no shift-start gate.
- `SettingsView` mixed audio, display (fullscreen/resolution/revert countdown), accessibility and
  data reset in one scrolling sheet; Escape while Options was open over the pause menu closed
  Options AND unpaused in the same frame (`CheckpointView.Update` / `PlayerController.Tick` both
  handled Esc unconditionally).
- `AudioDirector.Crossfade` captured the music volume once at fade start, so slider moves during a
  crossfade were overwritten by the stale value at fade end.

## The rebuild

**`GameLogic/CheckpointQueue.cs` (new, engine-free, unit-tested).** The single authority for the
arrival flow, with the concepts the task demands kept separate: roster source (finite list in
Normal, `GameRoot.NextRunVisitor` generator in Endless/Challenge), waiting line (real FIFO
`Queue<VisitorData>`), active window visitor, arrival clock (seeded random 3–5 s from
`QueueTuning`, at most one arrival per tick, overshoot carries), arrival/processed counters,
effective total (roster minus vanished; −1 when infinite), and completion (started + source dry +
line empty + window free — never "line temporarily empty"). `Tick`/`ForceNextArrival`/
`ResumePresentation` return a `QueueTickResult` struct (ToWindow/ToLine/Vanished/LineChanged/
PressureWarning/Overwhelmed) — the pure layer reports, the desk view presents and sounds.
Pressure edges are crossed-threshold events (warning once at 5; collapse once at 7 — the task's
initial thresholds, all in `GameConfigSO`). `AbandonLine()` closes a night on a story beat (the
administrator outro) even if waiters remain — the promoted-next window closes with it, so the
admin rejection can never soft-lock the END SHIFT gate. Missing students: when an eligible (non-admin)
anomaly first enters the line, one seeded roll against `missingStudentChance` (limit
`missingStudentLimit` = 1/shift); the victim is a random legitimate visitor, preferred from the
waiting line, else from the un-arrived source (cursor skips vanished entries — no negative
indices, no double removal, never active/processed/admin/anomaly). A line victim also decrements
the arrival progression, so `CURRENT: a/t` and the end-of-shift invariant `arrivals == effective
total` stay honest. Run modes never vanish anyone (finite-roster story mechanic).

**Ownership + persistence.** `GameRoot.Queue` owns the instance (persistent run state). Chosen
patrol rule (applied consistently): the clock runs only while the desk is the active screen — the
desk view is the only ticker (`AdvanceQueue(Time.deltaTime)` from `Update`, so `timeScale = 0`
pauses it with no unscaled-time leak), patrol destroys the view and freezes the line, `ResumeRun`
rebuilds silhouettes/counters/window from the same object. `QueueSnapshot` (started, cursor,
arrived, processed, timer, waiting/vanished as `VisitorIndex` lists, active, incidents, budget
mask, collapsed, resolution-pending) rides in `GameSession` and serializes as additive `queue*` save
keys; pre-queue saves fall back to the legacy `visitorAtSave` cursor. Resolution is two-phase —
`CommitActiveResolution` books the processed count and consumes the budget mask at the stamp,
`CompleteActive` frees the window and promotes after the banner — so a pause→quit *during the
banner* restores as a finished resolution instead of a stampable visitor (found by tracing the
banner coroutine against the pause menu; nine pure-logic checks pin it). `FinishShift` resets the
queue before persisting so a finished night's line can never leak into the next one.
`QuestionSystem.Budget` gained `Mask`/`RestoreFromMask` so a mid-inspection quit/continue cannot
refund questions.

**Desk view.** `NextVisitor`/`ResumeAfterExploration`/`DelayedNext` and the `_index`/`_roster`/
`_rosterComplete`/`_waitingReturnFromPatrol` fields are gone (replaced coherently, not wrapped).
`BeginRun(shift)` shows the sticky note for unstarted shifts or restores the presentation from
queue state; `AfterBanner` → `CompleteActive()` promotes the oldest waiter immediately (no second
wait) or frees the window for the clock; `Stamp` gates on collapse; verdicts still run through
`GameRoot.OnVerdictResolved` exactly once; the administrator outro branch is intact. Queue UI on
the approved grid discipline: two silhouette strips in the free yard gaps either side of the
booth (`QueueStripL/R`, 4 identical `custodian_0` figures each + `+n` overflow — legit and
anomaly indistinguishable), a `LINE n` badge above the right strip, and the
`QUEUE: n | CURRENT: a/t` readout as the day banner's second line (`Inner.BannerQueue`; the
top bar's right side is fully claimed by the frozen wireframe's clock/pause/findings, so no new
top-level top-bar region exists). Amber at pressure, red `OVERRUN` at collapse. All new elements
are registered in `CheckpointLayout` + `DeskLayoutAudit` and pinned by the FlowCheck layout audit.

**Overwhelmed consequence.** No existing lose route represents "the line overran the checkpoint"
accurately (breaches/false rejections would misrepresent it), so per the task's fallback the
consequence is a one-shot run failure through the EXISTING ending route: `Session.QueueOverwhelmed`
+ additive `EndingRules` lose reason `"overwhelmed"` (evaluated after admin/breaches/false-rejects,
so frozen tests and priorities are untouched) → `GameRoot.TriggerQueueOverwhelmed` (alarm, caption,
toast, `TriggerEnding`). The queue collapses first, so arrivals stop, `Stamp` is dead, and
re-entry is guarded (`RunFinished`/state checks) — no duplicate endings, no repeated penalties.

**Sticky note (Phase 10.1).** Centered paper panel over a click-blocking full-desk dimmer with the
exact required text ("NIGHT SHIFT BEGINS." / "Rules: Check IDs, ask questions, stamp. Don't let the
wrong faces in." / "[CLICK TO BEGIN]"). A genuine gate: before the click the queue is unstarted —
no first visitor, no clock, no enqueues, verdict/question controls dimmed, patrol refused — and
there is exactly one clock to start. The click plays `sfx_ui_open`, hides the note, marks the shift
started in persistent run state, presents the first eligible visitor immediately and arms the
arrival clock. Resume paths (patrol return, CONTINUE) never re-show it, never double-arm the
clock, never re-spawn the first visitor, never erase the line, never refund the budget.

**Options modal (Phase 10.3–10.5).** `SettingsView` refined (not duplicated) into the compact
audio-only modal on the existing `LeftSheet` + `OpenSettings`/`CloseSettings` paths: AUDIO header +
MASTER/MUSIC/SFX `SliderRow`s (live % read-out built in) + `◂ BACK`; dimmer blocks clicks behind;
Escape closes; Tab cycles the three sliders. Display/fullscreen/resolution/text-size/
accessibility/CONTROLS/RESET-DATA rows removed from the panel — `SettingsSystem`, `SettingsData`
persistence, `DisplayPolicy`, boot-time `ApplyStoredDisplay` and the menu's `F2`×3 reset are
untouched. `AudioDirector`: crossfade now reads `_settings.MusicVolume` live per frame;
`ApplyVolumes` on `SettingsChanged` refreshes playing music sources; SFX volume applies per
`PlaySfx` as before. Master multiplies both mixes; zero mutes; nothing depends on scaled time.
`GameRoot.BlockingOverlayOpen` (Settings/Controls/Credits visible) gates the Esc-unpause hotkeys in
`CheckpointView.TickHotkeys` and `PlayerController.Tick` — pause stays paused while Options is
open, and closing Options returns to the pause sheet.

**Config.** `GameConfigSO` + `GameConfig.asset` in lockstep: `queueArrivalMinSeconds` 3,
`queueArrivalMaxSeconds` 5, `queuePressureWarnAt` 5, `queueOverwhelmedAt` 7,
`missingStudentChance` 0.25, `missingStudentLimit` 1. The dead `betweenVisitorsSeconds` /
`exhibitionBetweenVisitors` knobs (only reader was the deleted `AfterBanner` wait) were removed.

## Behavior changes (documented)

- Visitors arrive on a clock instead of one-at-a-time-on-demand; an unresolved inspection lets the
  yard fill up. Exhibition nights are playable at booth pace but the 5/7 thresholds are live —
  all knobs are in `GameConfig` for tuning.
- Patrol explicitly pauses the line (radio says so: "The line starts again when you're back.").
- The desk top bar's `VISITOR n/m` suffix became the banner's second queue line; night/date label
  unchanged.
- Options no longer exposes display/accessibility/reset (still available via boot apply + F2×3);
  Pause → CONTROLS still opens the controls sheet.
- New lose ending reason `overwhelmed` (same ending screen, records it under the `lose` key).
- Saves gained additive `queue*` keys (version still 1; old saves load and resume via the legacy
  cursor; new saves restore the exact line, timer and question budget).

## Verification (all green at this commit)

- `Tools/Verify/verify.sh`: compile 0 errors / 0 warnings vs BOTH the minimal and full Unity
  assembly sets; static audit clean.
- `Tools/LogicTests`: **27,336/27,336** (was 27,225) — new groups: CheckpointQueue start gate /
  interval bounds + randomization / FIFO promotion / completion invariants / pressure + collapse
  one-shot edges / missing-student eligibility, preference, limit, permanence, bookkeeping /
  snapshot + save round-trip + legacy resume + run-mode source / budget mask; overwhelmed ending
  priority; options-sheet row budget updated for the audio-only modal.
- `Tools/FlowCheck`: **830 OK / 0 BAD across 5 consecutive runs** (was 647) — sticky-note gate +
  exact text in Main; queue proofs Q1–Q11 (FIFO accumulation, immediate promotion, empty≠complete,
  pressure/overwhelm ending once with arrivals stopped and verdicts dead, forced missing-student
  with line-preferred victim + radio feedback + permanence under ALLOW, chance-0 path, patrol
  round trip with frozen timer, mid-inspection quit+continue restoring visitor/budget/line,
  endless generator source, shift-boundary leak guard, night-5 administrator walk: intro dialogue,
  rejection, outro, completion and clock-out through the queue); settings proofs S1–S8 (menu +
  pause paths,
  audio-only rows, dimmer, slider independence, live music refresh, mute-at-zero, restart
  persistence, Esc guard); layout audit extended for the new regions; existing desk/save/world
  suites intact.
- Frozen `Tools/Verification/LogicTests`: **663/663**, untouched.
- `Tools/Verification/CompileCheck`: builds, 0 errors.
- `Tools/Verification/EngineSmoke`: still frozen-broken by decision (deleted `SimulateLoad`,
  pre-rework `LightController.Mode`) — not fixed, not deleted, not relied on.
- UnityStubs: no changes this pass (existing stubs covered sliders/buttons/PlayerPrefs).

## NOT RUN (no Unity editor in this sandbox)

Editor-only items, with exact repro:
1. **Play-mode pass of the Phase 11.1 sequence** — open `Assets/_Project/Scenes/Hollowbrook.unity`
   (or any scene; bootstraps self-build), Play → NEW GAME → advance the intro → the sticky note
   must appear; wait ≥6 s (nobody arrives); click it (first visitor + clock); leave the inspection
   ≥6 s (a second visitor's silhouette appears in the yard strip and `QUEUE:` counts it); keep
   waiting (FIFO accumulation); stamp (oldest waiter steps up after the banner, counters update);
   let the yard empty before the next arrival (desk waits — END SHIFT stays dim).
2. **11.2 pressure/overwhelm in-editor** — temporarily set `GameConfig.queueArrivalMin/Max` to
   ~1 s (test-only; restore 3/5 afterwards) or simply idle: at 5 waiting the amber banner line +
   intercom + radio warning fire once; at 7 the alarm + caption + "overwhelmed" lose ending fire
   once (no second ending, stamps dead).
3. **11.3 missing student in-editor** — set `missingStudentChance = 1` temporarily: on the first
   anomaly joining the line, one silhouette disappears (or a future visitor never comes), the
   radio roll-call line plays, `CURRENT: a/t` total drops by 1; allow the anomaly in and confirm
   the student stays gone. Restore 0.25.
4. **11.5 settings in-editor** — menu SETTINGS → only three sliders + BACK; move sliders while a
   music bed plays (immediate); pause in-game → SETTINGS (pause sheet hides, game stays paused) →
   BACK returns to the pause sheet → RESUME; Esc over Options must not unpause.
5. **Visual checks** — silhouette strips readable against the yard at 1280×720 / 1920×1080 /
   1366×768; sticky-note paper panel centered over the dimmer; banner second line legible;
   LayoutAudit console line reports zero warnings.
