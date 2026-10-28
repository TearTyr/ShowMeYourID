# UI reference match — the live desk re-grid to `uploads/UI Reference.jpg`

The owner supplied a UI reference screenshot (640x360, treated as the 1280x720 desk space) as
the layout source of truth and asked that the live UI be modified until a grid comparison of a
re-rendered screenshot reproduces the reference's layout, proportions, typography and layering.
Rule of the pass: the reference decides GEOMETRY and TYPE; nothing is faked with static images,
gameplay and controls keep their behaviour, and elements the current desk has but the reference
does not survive only where they are essential gameplay (talk dock, red prompt strip, findings
counter). The 3-line grey objective strip was redundant at the desk and is hidden there.

Method: `gridwork/grid.py` normalises reference / current / ours to 1280x720 and draws one 80px
grid over each; every rect below was measured off `grid_ref.png` before editing and re-measured
off a fresh render after. Annotated grids live outside the repo (`"$ARENA_WORKSPACE"/gridwork`) and never
ship in the UI.

## Measured reference targets (1280x720) and what moved

| element | reference rect | was | now |
|---|---|---|---|
| night banner (one condensed line) | (20,25)-(395,55) | (25,22,380,34) small/abbrev | (20,25,375,30), 34px squeezed 0.72 |
| clock | (1013,25)-(1072,55) | (985,22,120,34) 22px | (1000,25,85,30), 30px squeezed 0.75 |
| sanity pill / pause | unchanged | ok | ok |
| FLOOR FILES station | (40,85,305,160) | (44,103,272,144) | retargeted; tabs (50,40)+i*35, 230x34 |
| pulled drawer sheet | (55,255,275,255) + bracket (120,520,140,25) | mouth (47,253,266,253), front at TOP | mouth (55,255,275,290); front bracket rides BELOW the sheet; rows (15,50)+i*27, 250x22, dark 160px name pill + registry id on the paper |
| window stack | lintel (395,160,485,25), frame (410,185,455,265), interior (430,200,415,235), sill (375,450,520,18), base (350,468,580,22) | photo filled 187-537 | kit bands re-solved: top (0,-10,1280,207), glass (0,185,1280,265), base (0,447,1280,272); portrait = the bust column (530,205,200,230) |
| speech bubble | (735,300,180,65) | region ok, panel/tag misdrawn | kit panel hangs its 48u painted tail border OUTSIDE the box so the white lands on the reference rect; tag line retired under the kit |
| questions panel | (950,105,305,215), head cap 25, counter "2/2" at (1200,130) | (950,116,309,215), "QUESTIONS: 2 LEFT ●●" | retargeted; head 24px condensed; counter = remaining/max; rows (15,60)+i*37, 275x28, unnumbered, label left after the bullet arrow |
| inline map | torn paper (955,330,290,170) w/ hand boxes | chalk room list | paper composite: light sheet + tape tabs + title/floor line + 5 room boxes (sealed one red) |
| desk papers | log (80,555,160,165) -4°, rules (240,570,160,150) +3° | (97,540,...)/(231,548,...) | retargeted, tilts kept |
| ID card / slip | (460,535,190,~120) / (665,530,155,90) | (456,500,200,137)/(669,531,150,90) | (462,533,182,122)/(665,530,155,90) |
| stamps + caption pills | art (900,520,140,125)/(1055,520,140,125); pills (900,655,140,40)/(1055,655,140,40) | 10px higher, ~10px pills | retargeted; pills carry the verdict disc + word |
| patrol / end shift | (450,655,180,40)/(650,655,180,40) | (453,640,172,47)/... | retargeted; kit pills keep paper-white when disabled (word+icon dim) |
| talk dock | absent (essential, kept) | (440,690,420,24) | slimmed to (440,696,420,22) under the new button row |
| grey objective strip | absent (redundant at desk) | always visible | hidden at the desk (exploration/peek keep it) |

## Cause -> change -> fix -> verification, per defect

1. **Every region above** — `Checkpoint/CheckpointLayout.cs` is the single grid table; all rects
   re-targeted in one edit, inner grids (tabs, rows, question buttons, dock, banner) with them.
   FlowCheck's `ExpectRegion` literals, inner-grid pins, the stamp-spacing pin (23 -> 15) and the
   overlap matrix were updated in the same commit-shaped step; the matrix re-proves all pairs
   disjoint (station left the always-on "B" tag for the checkpoint "C" tag once the hint strip and
   the cabinet would otherwise have shared pixels).
2. **Kit band geometry** — the three shutter bands are stretched full-frame layers; their painted
   furniture lands where the band rect puts it. The old rects were solved against the concept art,
   not the reference: lintel drew at 187-212, frame at 187-537, sill at 538. Measuring the furniture
   in the OLD render gave each feature's band-fraction; solving the linear map for the reference
   targets gave top (0,-10,1280,207), glass (0,185,1280,265), base (0,447,1280,272). Re-render put
   lintel/frame/interior/sill/base on the reference rows within a pixel.
3. **Header typography** — the reference board line is a condensed stencil bold; the shipped face
   is wide, so the line wrapped ("NIGHT 1 - MONDAY -/ NOVEMBER 2") at any size that fit 375px.
   Fix: `ContentData.ShiftDatesLong` + `ShiftDef.LongDateLabel` supply the full
   "MONDAY - NOVEMBER 2" form, the label draws at 34px with `horizontalOverflow = Overflow` and a
   0.72 horizontal squeeze (clock 30px/0.75); QUESTIONS head likewise 24px/0.72. Two harness bugs
   surfaced here and were fixed: `WalkScreen` multiplied a node's OWN localScale into its
   anchoredPosition (Unity scales about the pivot, after placement — the clock dumped at x=750),
   and the preview renderer applied the squeeze to the font SIZE instead of the glyph width (caps
   shrank); it now lays lines out unscaled and squeezes the surface, fed by a new `sc` field in the
   dump.
4. **Drawer read/front/rows** — the reference pulls the worn front BELOW the row sheet (handle
   bracket at 520-545), prints the registry readout where a pulled sheet can never cover it, and
   reads "ROOM 202" + dark ink on light paper. The front now rides at body-bottom +10 (its
   travel-fraction offset fades out as the pull settles, so a settled drawer lands exactly on the
   reference bracket), the readout moved to the cabinet front's free strip, rows became a dark
   160px name pill plus the registry number on the paper beside it, tabs and head carry the room
   number alone, and the kit pass inks head/front/handle in the paper language. `RefreshHandle`
   kept overwriting the kit handle colour every frame — it now branches on `_kitOn`.
5. **Question rows born inactive** — the kit pass restyles row labels via
   `GetComponentInChildren<Text>()`, which skips the rows while they wait for a visitor (they are
   built hidden), so the reference's left-aligned-after-the-arrow labels never applied and rows
   rendered centred. The lookup now searches inactive children (and the `bg/label` path).
6. **Bubble** — the kit bubble's sliced left border (48u) carries its painted tail, which pushed
   the white panel 53px right of the reference box and left the speaker tag bleeding over the
   wall. The panel now hangs that border outside the registered box (white lands exactly on
   737..919), the tag line is retired under the kit (the tail points at the speaker), and the text
   rect clears the border. FlowCheck's panel-growth pin now asserts the white zone equals the box.
7. **Map** — `map_paper` is the night overlay's DARK sheet; the reference desk map is light torn
   paper. The composite now stretches `paper_flat`, pins two tape tabs, and draws title, floor
   line, five room boxes (server room sealed in red) and the open-plan hint; click still opens the
   full overlay, and the overlay remains the single source of room truth.
8. **Proofs that asserted the old look** — budget pips ("1 LEFT ●○"), the drawer-head floor
   wording, the taken-slip rest x (it encodes 740 - slipW/2), the bubble tag/marker checks and the
   panel-width pin all described the pre-reference UI. Each was rewritten to the new contract
   ("1/2", "ROOM 101 — 7 FILES", tag-less kit bubble, tail-border-outside panel) rather than
   weakened.

## Verification

- `bash Tools/Verify/verify.sh` — compile clean + audit clean (107 scripts).
- `dotnet run --project Tools/FlowCheck` — **1127 OK / 0 BAD** (layout pins, overlap matrix,
  drawer/bubble/budget proofs, teardown proof).
- `dotnet run --project Tools/LogicTests` — **27,773 PASS / 0 FAIL**.
- `dotnet run --project Tools/Verification/LogicTests` (frozen) — **663 / 663**.
- Renders: `preview/ref_occupied`, `preview/ref_empty` (1920x1080), `preview/ref_occ_1024`
  (1024x768, narrower), `preview/ref_occ_1280x600` (1280x600, shorter) — grid-compared against
  `gridwork/grid_ref.png`; the narrower/shorter viewports letterbox the fixed 1280x720 frame with
  nothing clipped and nothing reflowed.

## Residual differences (disclosed, not hidden)

- The window portrait is the temp face asset: a bust on a WHITE photo ground, placed in the
  reference's bust column (530,205,200,230) so the dark kit interior shows either side of it. The
  reference paints its bust straight onto a dark interior; matching that exactly means replacing
  the placeholder face art, which is content work outside this pass.
- The kit's painted frame side pillars are ~40px wide where the reference draws ~20px; that is the
  owner's kit art stretched full-frame, not a layout choice.
- The desk map's room boxes are filled chips (the overlay's room ink) where the reference draws
  outlined hand boxes; the sealed room still reads red.
- Stamps keep their verdict tint (green/red) — the established allow/deny language — where the
  reference mockup is monochrome.
- Kept although absent from the reference, because they are essential gameplay: the talk dock
  (slimmed to 22px under the button row), the red prompt strip, the findings counter.
- Disabled kit pills dim word+icon only (paper stays white, per the reference); the pre-kit desk
  keeps the old whole-button dim.

Tree left UNCOMMITTED by standing instruction.

# Teardown fix — "Destroying assets is not permitted" floods the editor console on exit

The owner's real Unity editor session (first Play Mode run after the UI bug-fix pass) ended with
~38 copies of this error on exiting play:

    Destroying assets is not permitted to avoid data loss.
    If you really want to remove an asset use DestroyImmediate (theObject, true);
    UnityEngine.Object:Destroy (UnityEngine.Object)
    Hollowbrook.Bootstrap.ProceduralArt:ClearCache () (at Assets/_Project/Scripts/Bootstrap/ProceduralArt.cs:438)
    Hollowbrook.Core.GameRoot:OnDestroy () (at Assets/_Project/Scripts/Core/GameRoot.cs:906)

- **Cause:** `ProceduralArt.Cache` mixes two kinds of sprites. Procedural ones own their
  `Texture2D` (painted in `Canvas2D.ToSprite`); file-backed ones (drop-in faces/stamps and the
  38 SecurityRoom kit layers) are `Sprite.Create` wrappers around textures **imported from
  Resources** — assets on disk. `ClearCache()` destroyed `sprite.texture` indiscriminately, so
  every editor exit attempted one forbidden asset-destroy per cached kit layer. The engine
  refuses (no-op + error log), so nothing was corrupted — but the console flooded. The headless
  harness could not catch it: the stub `Resources` probes the real art tree, yet the stub
  `Object.Destroy` had no asset guard — another editor-vs-stub divergence (same species as the
  blank question-row labels). `ProceduralAudio.Get` carried the identical latent bug for clips
  loaded from `Resources/Audio`.
- **Change:**
  - `Bootstrap/ProceduralArt.cs` — new `FileBacked` key set, registered by `Get()`'s file
    branch; `ClearCache()` destroys only the runtime `Sprite` wrapper for file-backed entries
    (never the borrowed imported texture) and frees texture+wrapper for procedural entries.
  - `Bootstrap/ProceduralAudio.cs` — same split: loaded clips are dropped by reference only,
    synthesised clips are destroyed.
  - `Tools/Verification/UnityStubs/UnityEngine.Core.cs` — the stubs now mirror the engine guard:
    `Resources` probes mark their stand-ins `isPersistentAsset`; `Object.Destroy` and
    `DestroyImmediate(obj)` refuse persistent assets with Unity's exact error text (the new
    `DestroyImmediate(obj, true)` overload permits it, as in the engine); `Debug` gained an
    `ErrorCount` so proofs can diff around risky calls.
  - `Tools/FlowCheck/Program.cs` — new `TeardownProof` (8 checks) invoking both `ClearCache()`s
    exactly as `GameRoot.OnDestroy` does: zero engine errors logged, the imported kit texture
    survives, procedural texture and both sprite wrappers are freed, the cache is empty, and
    the next `Get()` rebuilds fresh around the surviving asset.
- **Fix:** exiting play is silent — the forbidden destroys are gone, not suppressed; every
  runtime allocation is still released with the core.
- **Verification:** `bash Tools/Verify/verify.sh` compile+audit clean (107 scripts, 19,726
  lines); `dotnet run --project Tools/FlowCheck` — **1139 OK / 0 BAD**, zero `[ERR]` on stderr.
  **Negative control:** with the old indiscriminate destroy temporarily reintroduced, FlowCheck
  goes **BAD** ("teardown logs no engine error", exit 1) and logs the owner's exact message 51×
  (every file-backed texture cached across the walk) — the regression net now catches this bug
  class headlessly. `Tools/LogicTests` 27,773/0; frozen `Tools/Verification/LogicTests` 663/0.
  Re-rendered the occupied-desk dump is **pixel-identical** to
  `preview/after_occupied/screen_1920x1080.png` (0 differing pixels) — zero visual impact.
  Unity Play Mode re-run by the owner remains the final confirmation (no editor in this
  environment); repro: exit play and check the console stays empty.

# UI bug-fix pass — the seven rendering defects from the owner's screenshots

Four owner screenshots (`uploads/Screenshot 2026-10-27 231239.png` intro, `231303.png` +
`231347.png` occupied desk, `231600.png` empty booth) showed seven distinct rendering defects
introduced or exposed by the kit-integration + overhaul passes. This pass fixes each one with no
change to mechanics, ending priority, or the black/white distressed look. Every fix was verified
against REAL RENDERED OUTPUT: `dotnet run --project Tools/FlowCheck -- --dump-state=…` writes a
hierarchy dump, `Tools/Preview/render_screen.py` rasterises it at 1920×1080, and the sheets in
`preview/` show concept / before / after side by side.

## 1 — White shape obscuring the intro dialogue

- **Cause:** `DialogueView.Init` skinned the 1000×170 narration panel with the kit's white torn
  speech bubble `sec_bubble` (9-slice). Its white centre tile painted over the narration and the
  white ink on white paper made the text invisible.
- **Change:** `UI/DialogueView.cs` — removed the `sec_bubble` panel-skin block; the dark cinematic
  panel returns.
- **Fix:** narration reads light-on-dark with the red heading; no white shape anywhere in frame.
- **Verification:** `preview/after_intro/screen_1920x1080.png` (`--dump-state=intro`), row 1 of
  `preview/compare_after_fix.png`.

## 2 — Red prompt overlapping the sanity pill (and duplicating the objective)

- **Cause:** `L.Prompt` was (450,62,400,18) carrying a 20 pt label → the line wrapped to two rows
  that collided with the sanity pill; the grey `_objective` strip mirrored the same prompt text,
  so the player read it twice.
- **Change:** `UI/HUDView.cs` — prompt font 20→15 and the prompt is no longer mirrored into
  `_objective` (the strip keeps `root.ObjectiveText` from the checkpoint); `CheckpointLayout.cs`
  — Prompt (450,62,440,20).
- **Fix:** one-line red prompt tucked under the pill; objective strip shows only the checkpoint
  objective. FlowCheck asserts prompt/objective duplication is gone.
- **Verification:** `preview/after_occupied`, rows 2/4/5 of `compare_after_fix.png`, FlowCheck
  duplication assertions.

## 3 — Questions counter wrapping vertically off the panel

- **Cause:** `Inner.QBudget` was (272,18,**30**,22) — a 30 px box for "QUESTIONS: 2 LEFT ● ●", so
  the words stacked vertically and bled over the panel edge.
- **Change:** `CheckpointLayout.cs` — QHead (16,18,120,22), QBudget (140,18,153,22);
  `CheckpointView.Decision.cs` — `_budgetLabel.horizontalOverflow = Overflow` as belt-and-braces.
- **Fix:** the counter sits on one line inside the questions panel at every budget value.
- **Verification:** FlowCheck `ExpectRegion` literals + rendered rows 2/4/5.

## 4 — Sanity pill reading as an all-black slab

- **Cause:** the kit tint set the FILL to `NsInk` (near-black) on the white pill, so at sanity 100
  the whole track was a black slab; the track bed was alpha-0, so filled and unfilled were
  indistinguishable at any value.
- **Change:** `UI/HUDView.cs` `ApplyKitSanity` — track bed = `NsInk` alpha 1 (a dark recess;
  non-kit builds keep `PanelAlt`), fill inset `offsetMin(1,1)/offsetMax(-1,-1)` as a keyline;
  kit `FillInk`: Stable = paper white (0.93,0.92,0.89), Uneasy = warm paper (0.93,0.86,0.72),
  Breaking/Shaken alarm colours unchanged.
- **Fix:** filled vs unfilled is unmistakable at every value and the number agrees with the bar.
- **Verification:** `preview/sanity_after_sheet.png` (45 = warm paper at 45 % over dark bed,
  25 = orange at 25 %), `--dump-state=occ45/occ25`, FlowCheck kit-sanity proof now asserts dark
  bed (alpha > 0.9, r < 0.2) + paper-white stable fill (r > 0.8).

## 5 — Question labels invisible in the real editor (stub-vs-Unity divergence)

- **Cause:** `StandardButton` labels use a point anchor (0.5,0.5); `KitSkin` assigned
  `offsetMin/offsetMax`, which real Unity resolves to a NEGATIVE sizeDelta (−34,−4) → the text
  draws nothing. The verification stub tolerated the negative delta, so only the owner's editor
  caught it.
- **Change:** `CheckpointView.KitSkin.cs` — question labels get `anchorMin=zero / anchorMax=one`
  BEFORE the offsets are assigned, so stretch semantics apply and the offsets mean what they say.
- **Fix:** labels render on their pills in the editor and headless alike.
- **Verification:** rendered rows 2/4/5 (labels visible on pills); `verify.sh` audit clean; the
  divergence is documented in the stub comment from the talk-dock fix entry.

## 6 — Empty booth: grey slabs, squeezed blinds, no empty state

- **Cause:** `sec_window_*_glass` sprites carry art only in the centred ~36 % of their 2880 px
  export; `KitRect` stretched them into `L.Glass` (422 wide), squeezing the blinds to a third of
  the frame, and the unfaded `tint` child of `glass` painted grey side panels. After hand-back the
  visitor silhouette also lingered with no explanation.
- **Change:** `CheckpointView.KitSkin.cs` — glass band region → `new L.Region(0,187,1280,350)`
  (full-width stretch) and `"tint"` added to the fade list; `CheckpointView.Decision.cs` — new
  `_qEmpty` label "NO VISITOR AT THE WINDOW" (16,104,277,20) shown while `!atWindow`;
  `CheckpointView.Window.cs` — `SetVisitorVisible` made `internal` so the harness can land the
  post-verdict beat (stub coroutines are no-ops headless).
- **Fix:** full-width blinds, no grey slabs, an explicit empty-state line, visitor gone after
  hand-back.
- **Verification:** `preview/after_empty/screen_1920x1080.png` (`--dump-state=empty`), row 3 of
  `compare_after_fix.png`.

## 7 — Stamp legibility, document crowding, bottom-row clipping

- **Cause:** `SkinStamp` pasted a 46 px black disc (`sec_icon_accept/decline`) over the stamp
  handle silhouette, burying it; the log/rules papers' regions ended exactly at y=720 so their
  tilt pushed corners off-screen; the ID card rest slot overlapped the patrol/end-shift row.
- **Change:** stamp badge 46 px → 26 px at offset (0,40) on the head knob; `CheckpointLayout.cs`
  — IdCard (456,500,200,137), PatrolBtn (453,640,172,47), EndShiftBtn (653,640,181,47),
  TalkDock (440,690,420,24), UtilityRecords (97,540,134,164), UtilityNotes (231,548,181,152).
- **Fix:** stamps read as stamps with a small head badge; documents sit inside their frames; the
  whole bottom row is on screen at every tested breakpoint.
- **Verification:** rows 2–5 of `compare_after_fix.png`; FlowCheck `ExpectRegion` literals for the
  moved regions; `preview/resp_occ/{1280x720,1024x768,1280x600}` and
  `preview/resp_empty/{1280x600,1024x768}` show no clipping or collisions (letterboxed centring
  per `AspectMatchGuard`).

## Suites after the pass (re-run for real, this turn)

- `bash Tools/Verify/verify.sh` — compile clean vs both Unity assembly sets + audit clean
  (107 scripts, 19,694 lines).
- `dotnet run --project Tools/FlowCheck` — **1131 OK / 0 BAD** (includes the new
  `--dump-state=intro|empty|occupied|occ45|occ25` proofs, the prompt/objective duplication
  assertions and the kit-sanity bed/fill assertions).
- `dotnet run --project Tools/LogicTests` — **27,773 / 27,773**.
- `dotnet run --project Tools/Verification/LogicTests` (frozen) — **663 / 663**.
- Rendered evidence: `preview/compare_after_fix.png` (concept + five before/after rows),
  `preview/after_*/screen_1920x1080.png`, `preview/sanity_after_sheet.png`,
  `preview/breakpoints_after_sheet.png`, `preview/resp_occ/`, `preview/resp_empty/`.
- **NOT RUN:** Unity Play Mode in the real editor and the Windows build (no editor in this
  environment) — issue 5 was exactly a stub-vs-editor divergence, so an editor smoke pass remains
  the one check this pass cannot give itself.

# Editor compile fix — the talk dock used a stub-only InputField API

The owner's Unity editor flagged two CS1061 errors the headless harness could not see:
`CheckpointView.TalkDock.cs` called `InputField.Focus()` — a member the Verification stub had
invented. Real uGUI has no `Focus()`; the equivalent is `ActivateInputField()` (which also makes
the field the EventSystem's selected object, exactly the semantics the stub's Focus had, so
`UIFactory.TypingInField()` behaves identically).

- `CheckpointView.TalkDock.cs`: both call sites (post-send refocus, `FocusTalk()` for `V`) now
  call `ActivateInputField()`.
- `UnityStubs/UnityEngine.UI.cs`: the invented `Focus()` is REMOVED, not aliased — the stub now
  exposes only the real uGUI surface (plus the explicitly test-only `SimulateEnter`/
  `SimulateTyping` helpers, which Assets never calls), so the next stub-vs-editor divergence
  fails `verify.sh` instead of the owner's build. A comment documents the rule.
- `Tools/FlowCheck/Program.cs`: the two `TalkDockProof` focus calls switched to match.
- Re-run green after the change: verify.sh compile+audit clean (107 scripts); FlowCheck
  1130 OK / 0 BAD; LogicTests 27,773 / 0; frozen Verification 663 / 0.

# Complete UI overhaul & dialogue input integration — the desk speaks, the concept leads

Two stacked owner directives in one uncommitted pass: make the long-dead dialogue text box a
REAL input wired into the interaction loop, and redesign the desk to the concept art
(`uploads/1_image.png`, measured and re-based as the source of truth for proportions and
placement). Everything below is on top of the kit-integration pass; contracts kept: two-question
cap, physical stamp + hand-back verdict, Lose > Secret > Win ending priority, sanity =
atmosphere, no scanner/checklist restoration.

## The dialogue text box (P0) — typed window talk, not a prop

- `GameLogic/SpokenChat.cs` (new, pure): Normalize / TryMatchQuestion / Classify / Route plus a
  capped 24-line WindowTranscript. No Unity types — LogicTests covers it with 36 checks.
- `CheckpointView.TalkDock.cs` (new): a dock strip under the tray (440,692,420,26) holding a real
  `UIFactory.TextField`, and a transcript sheet (440,462,420,226) behind a LOG toggle. `V`
  focuses, `Esc` blurs; Enter submits, Shift+Enter inserts a newline (the stub InputField's
  MultiLineSubmit semantics match uGUI, and the EventSystem focus sync is exercised headless).
- Wiring is real: a typed line that matches an unasked question goes through the SAME Interview
  path as the numbered buttons (budget, bubble answer, testimony, transcript); anything else
  draws an in-character desk refusal into the transcript. Submitted lines render in the sheet;
  the text is available to game logic via SpokenChat.Route's return, not a string sink.
- States: disabled + explanatory placeholder with nobody at the window; locked while the send
  beat runs; success clears the box; a closed window KEEPS the typed text (failed send); empty
  and duplicate submits are blocked; the dock survives visitor transitions (ResetTalkDock per
  visitor) and scene transitions (state lives in the view, transcript in the session-less
  pure model rebuilt per shift).
- FlowCheck `TalkDockProof` (~35 checks) drives the box through typing, focus, submit, failure,
  duplicate/empty blocks and the dead-window placeholder; LogicTests `SpokenChatTests` covers
  the router.

## Concept rebase — one grid, measured from the art

- `CheckpointLayout` regions re-based to the concept measurements: header board, booth glass,
  questions column, cabinet + drawer, papers, tray, dock. The board layer rides at (0,-31) so
  its painted cut-outs meet the re-baselined meters (its own white strips are painted out in
  prep: `sec_upper_panel_clean`); the kit window is shorter than the concept booth, so the
  shutter is three bands (`sec_window_*_{top,glass,base}`) — hood/base keep painted proportions,
  only glass stretches.
- ID card and slip became containers (audited rect + invisible raycast image) with scaled art
  children (`cardart` 320x220 @0.625, `slipart` 200x120 @0.75): concept footprints coexist with
  the design-space children; flips walk the art child, stamp marks parent to `slipart`.
- Fonts: prep unpacks the kit zips to `Resources/Fonts/kit_*`; UIFactory loads kit-first with
  the bundled open-licence faces as fallback; the headless stub probes the real font files so
  proofs see the same selection the editor resolves.
- Screen-dump pipeline: `FlowCheck --dump-screen=` walks the REAL hierarchy (desk + HUD frame,
  cumulative scales, input focus), `--dump-sprites=` exports procedural art as RGBA, and
  `Tools/Preview/render_screen.py` turns one dump into pixel screenshots at any breakpoint with
  the real fonts and 9-slice maths. Breakpoint shots: `preview/shots_after/` (7 resolutions),
  comparisons `preview/compare_1280x720.png`, `preview/compare_1920x1080.png`, contact sheet
  `preview/breakpoints_after_sheet.png`.

## Final hardening (this turn)

- Sanity pill: the HUD is built AFTER the desk, so `ApplyKitSanity(true)` never ran at Init;
  and even when called, `SetSanity` repaints the fill on every sanity event and clobbered the
  one-shot tint. Fixed with a sticky `_kitSanity` flag (fill/value/stage inks are kit-aware in
  SetSanity; alarm reds/oranges survive on the white pill) plus a lazy once-call in
  `CheckpointView.RefreshHud`. FlowCheck now proves track bed retired, dark inks, hidden stage
  caption and stage-correct fill.
- Speech bubble 9-slice: the kit master shipped padded with transparent margins and was loaded
  at the default UI PPU, so the 430px tail slice drew as a 4-unit sliver (and the preview
  renderer, fed raw texture pixels as reference units, drew nothing). Fixed on all three
  layers: prep trims `sec_bubble` to its alpha box (2879x1031), `ProceduralArt.FileBorders`
  measures the real art (480/140/140/140) with a PPU-10 override, the dump emits
  `pixelsPerUnit`, and the renderer converts borders through it — source crops stay in texture
  pixels, destinations in screen pixels, exactly like uGUI.
- Stale proofs un-retired: the shutter proof referenced the pre-band `sec_window` node and
  silently skipped; it now checks all three bands swap together, every kit-layer click-through
  probe asserts presence (no silent no-ops), and the tail slice is asserted >=20 reference
  units.

## Intentional deviations from the concept (with reasons)

- Talk dock + transcript sheet: new P0 furniture the concept (a still) has no slot for; placed
  in the free strip under the tray, outside every audited rect.
- Patrol map widget keeps the dark chalk list: the kit ships no map paper and inventing torn
  art would be fabricated owner art; the list is the live clickable plan index.
- The interaction prompt strip renders in accent red under the sanity pill: it is the live
  hand-over hint and must out-shout the board; the concept has no prompt because it is static.
- Patrol / End Shift pills dim while disabled: state language (the concept shows them live).
- Sanity stage caption hides under the kit pill: no room on the pill; the alarm colours and the
  horror FX carry the stage instead.

## Suites after the pass

FlowCheck 1130 OK / 0 BAD; LogicTests 27,773 / 0; frozen Verification suite 663 / 0;
verify.sh compile + audit clean (107 scripts). NOT RUN (no Unity editor here): Play Mode,
Windows build, on-screen feel of flicker/tilt — geometry verified by the dump/render pipeline
instead.

# SecurityRoom art-kit integration pass — the booth wears the owner's UI

Kit (`origin/main` commit f68b0f8, `Art/SecurityRoom/`, 8 numbered export folders + 2 font
zips) integrated end-to-end without moving a single audited interaction:

- `Tools/prep_securityroom_art.py` slices the export into 32 `sec_*` sprites under
  `Resources/Art/SecurityRoom/` (full-frame layers copied as-is; pills/tray cards/icons/folder
  tabs/sanity+pause widgets cut at measured alpha boxes) and unpacks Bahnschrift + Chiller into
  `Resources/Fonts/kit_*`; `Tools/generate_meta.py` keeps Unity's metas in step.
- `CheckpointView.KitSkin.cs` (new partial) applies the skin in one flat pass at the end of
  Init: booth base + lamp breath + shutter state (open glass while a visitor stands, closed
  blinds while the booth waits), header boards, question panel/pills with the arrow as the
  asked-mark, patrol/end-shift pills with footprint + moon icons, cabinet + folder tabs +
  drawer skins, tray circles under the stamps, kit stamp handles with check/cross badges,
  caption bars, dark-paper TL/RN buttons, tilted-paper overlays, and the 9-sliced speech box
  (`sec_bubble`, border 430/90/90/90) on both the desk bubble and the intro dialogue panel.
  `LoadingView` and the shift summary wear the shutter composites as backdrops.
- Geometry: the header grid was re-baselined ONCE, to the kit board — TopBar (30,30,1220,77),
  Banner (34,38,300,32), Clock (940,38,140,32), SanityMeter (478,50,331,36) and Pause
  (1098,50,132,36) in the board's painted cut-outs, the hint/findings/run-stats strip at
  y90..114, patrol meters at y112; the verdict tray was re-baselined to the kit's circles
  (allow 900,561,142,100 / deny 1056,561,142,100, 14px apart, grab areas >=96px). FlowCheck's
  wireframe table and overlap matrix pin every new number; the HUD's sanity box retires its own
  furniture and draws the live track/fill/text on the kit bar.
- Fonts: `UIFactory` tries the kit families first (Bahnschrift regular/bold for UI, Chiller for
  the display face) with the bundled open-licence faces as fallbacks; headless keeps GameFont.
- Headless verification gained teeth: the Unity stubs' `Resources.Load<Texture2D>` now probes
  the real `Resources` tree (PNG header only — no pixel memory), so drop-in art is exercised by
  the proofs exactly as the editor resolves it; stubs also grew `Sprite.Create`'s 9-slice
  overload, `Transform.SetSiblingIndex` and `Vector4.magnitude`.
- FlowCheck: new `SecurityRoom art kit` proof (32 sprites ship + resolve to their own names,
  every kit layer click-through, shutter follows the visitor, sliced bubble keeps its tail,
  tray circles draw under their stamps, fonts never null) and the manifest-sync proof now
  covers the kit rows; `docs/asset-manifest.csv` grows to 162 rows (36 shipped-kit).
- Suites after the pass: FlowCheck 1151 OK / 0 BAD, LogicTests 27,737 / 0, verify.sh compile +
  audit clean (105 scripts, 18,795 lines). NOT RUN (no Unity editor in this environment):
  Play Mode, Windows build, on-screen feel of the flicker/tilt — alignment was verified with
  the PIL composite `preview/kit_alignment2.png` instead.

# Refactor log — readability pass (branch refactor/readability)

## Issue #1 review pass — verified bugs, design gaps, cleanup

Blocking/high: (1) menu EXIT no longer overwrites the save (InRunProgress guard, also on
OnApplicationQuit); menu prompt now "Quit to desktop?". (2) exhibitionMode default false +
asset key added — NEW GAME is the five-night story; FlowCheck opts into the booth explicitly.
(3) CONTINUE keeps the night: BeginShift(isResume) skips TonightLog/Admitted/pool resets,
pool kinds derive from seed^shift, admitted list rides the save as roster indices and is
remapped on resume; FlowCheck asserts all three survive. (4) the Night-5 memo is handed over
with the badge and registry searches can't replace it. (5) gameplay hotkeys/zoom stand down
while paused, in overlays, in dialogue or off-desk. (6) InputGate gives Esc/M one owner per
frame across sheets, map, pause, desk, patrol, confirm popup (Confirm now closes on Esc and
counts as a blocking overlay).
Design: (7) catching an anomaly no longer resolves it — it stays outstanding and returns;
outstanding anomalies count as breaches when the run's books close, so patrol feeds endings.
Robustness: (8) crash-safe save write (File.Replace + .bak, Read falls back to .bak/.tmp),
settings persist once on sheet close/quit instead of per slider tick, RoomSpawner and
HallucinationSystem randomness seeded from the run seed.
Hygiene: (9) EngineSmoke removed (stale frozen harness; docs aligned), duplicate root/Assets
docs de-duplicated, loose docs/*.patch deleted, GameBootstrap comment fixed, dead code gone
(QuestionTextShort, _interviewOpen, the retired waiting-line DISPLAY incl. ShowcaseBuild),
blank-line runs collapsed.
FlowCheck: +issue proofs (menu-EXIT save guard, .bak fallback, pause gate, Esc single-owner,
admin memo, resume fidelity) and pool-resolve before story clock-outs; 837 OK.


## Owner-feedback pass — desk order & sizes (Messenger notes + redlined screenshot)

- Owner asks (chat): questions already out, press the question itself (no gate button); the ID
  slides out automatically per student (no click); the dialogue box was far too big for its
  content; TL / RN as two compact buttons; MAP as its own box; A/D as two big stamp boxes; and
  the CAMERA is the scan device — you put the ID on it to scan.
- New desk grid: centre = window, counter papers on the sill, compact DIALOGUE strip under the
  window (470x44, three lines), ID default slot below, patrol/end-shift at the very bottom;
  left = terminal, file window, TL + RN buttons, MAP box; right = questions, SCAN, CAMERA panel
  (drop-the-ID-to-scan, readout inside), then the two big stamps A—ACCEPT / D—DENY.
- FlowCheck table + proofs follow the new grid (837 OK), including "drop the ID on the CAMERA
  scans it" and the dialogue-strip position.


## Desk bug-fix + simplification pass (post-slip rework)

Bugs (root causes from the desk-flow audit):
1. Papers un-draggable in real Unity: `UIFactory.Img` defaults raycast off — idcard/record
   frames now set `raycastTarget = true` (labels stay off; back faces are Panels = already on).
2. Invisible stamp mark: `PlaceStampMark` re-anchored the overlay to a point anchor without a
   size — now `sizeDelta 90x90`, `preserveAspect`, raycast off. Check/X marks show on the slip.
3. Papers drew behind the window: new `PapersLayer` (created after the window, before the
   stamps) parents ID, file and slip; opened/taken papers rise via SetAsLastSibling.
4. Terminal unusable: the full-size tap button swallowed clicks — rebuilt as one visible
   search box (own background, "ID or name" placeholder, box is the InputField targetGraphic so
   click focuses), Enter/FIND searches, one result line; body-tap toggle removed; file opens in
   PapersLayer with its X close.
5. Hint line: state-driven (nothing taken / slip on counter / unstamped / awaiting hand-back)
   and moved into the free strip under the top bar (HUD objective label mirrors the prompt at
   the desk); run-stats/mutation bulletin moved to a left strip, gated to Endless/Challenge.
6. Counter slip readable and clear of the window frame (scale 1 at 890,308).

Simplification: top bar = night/date + clock + pause + slim sanity; battery/track patrol-only;
left = compact terminal (96px) + 3-line speech log; right = no QUESTION gate / hint / testimony
(four compact question buttons + pips, answers live in the speech log); tools = one small row
(SCAN/LOG/MAP/RULES) + one scan-result strip (drop the ID on it to scan); CAMERA panel removed;
stamps stay large bottom-right; centre-bottom is open desk; patrol/end-shift smaller.

FlowCheck grew the audit's requested asserts: raycast-on papers, stamp overlay size > 0 after a
drop, PapersLayer sibling order above the window, hint text per hand-over state, search-box
focus wiring (targetGraphic + placeholder, no tap-swallower). 840 OK.


## Desk flow rework — slips, stamps, records station (Papers-Please pass)

- Detain retired: Accept / Deny only. Post-decision text/popups retired: the verdict banner,
  the admin outro dialogue and the post-verdict radio lines are gone; feedback is the stamp
  mark on the paper, the thud, the subject walking off, the score/streak tick.
- New desk flow: ID **and paper slip** slide to the counter on arrival; both are dragged to the
  desk (move/flip/zoom/stack, drop-outside returns). The slip carries name/ID/reason and can
  hold the new `SlipMismatch` defect (capped pool defect, shift 2+, seeded, fairness-tested).
- The stamp lands ON THE SLIP where dropped (child layer, seeded ±rotation), one per slip,
  recording a PENDING verdict; the decision counts when ID + stamped slip are handed back
  (window zone, or `H`). Missing paper = the student waits.
- Records station replaces the auto-opening record sheet: search by ID/name via new pure
  `RecordSearch` (canon = StudentIdentity); NO RECORD is itself a clue; the file window is
  draggable, tap/X-closable, clamped left of the stamp tray.
- Latent bug found & fixed en route: booked slip defects could lose their slip name to
  baseline ordering; unbooked slips resync after ID-rewriting defects (duplicate/format),
  keeping "slip disagrees iff defect booked" invariant-tested.
- FlowCheck: hand-back proofs, pending-vs-scored proofs, slip/flip/zoom/station checks
  (825 OK). LogicTests +RecordSearchTests +SlipFairnessTests (27,396 / 27,398 de-DE).


## Inspection-table rework, slice A — verdicts become physical (Papers, Please pass)

- Verdicts are stamps, not buttons: APPROVE / DENY / DETAIN live in a tray and commit when
  dragged onto the subject's ID (DropStamp hit-test with padding); keyboard path A / D / T.
  The round ALLOW / CALL PRINCIPAL buttons and the yes/no confirm popup are retired —
  stamping IS confirming (task: remove plain confirm-button steps).
- New `Verdict.Detain` in pure rules: detaining an anomaly = correct stop; detaining a
  legitimate student = serious false rejection. Mistakes now book warnings; every third
  warning draws a fine (score penalty) — penalties, never instant game over. Both persist
  in the save (tolerant keys).
- Stamps reuse DragPaper with ReturnsHome; the tray dims until an inspection is live.
- FlowCheck: DeskLoop now stamps by drag (DropStamp helper), popup checks retired, stamp
  tray regions in the wireframe table; +8 stamp checks. LogicTests +8 (StampRulesTests).
- Rules, fairness, saves otherwise untouched; QuestionSystem budget unchanged here.


## Papers pass — desk chat, physical papers, question-panel upgrade

- Chat (speech log) moved to the bottom-left column (16,362,320,342) with per-speaker inks
  (YOU / window / RADIO); the retired principal-note slot left the layout tables.
- Papers are physical: new `DragPaper` (drag with lift + tilt settle, clamped to the desk
  frame, headless-drivable) on the ID card and record sheet; dropping the ID on the scanner
  scans through the same rules path as the SCAN button. New `HoverHint` feeds the panel hint.
- Question panel: budget pips, ASKED tags + disabled state, per-question cross-check hints,
  hotkeys 1-4, budget-spent reason line, scrolling testimony.
- Layout contract updated in one place (CheckpointLayout) — DeskLayoutAudit and FlowCheck's
  wireframe table follow; FlowCheck gained a 14-check PapersProof (drag clamps, drop-scan
  exactly-once, pips, asked-state, hotkeys, reason line, scroll, chat position + colours).
- Rules, fairness and saves untouched: dragging is positional only; scanning path unchanged.


## Readability pass for a beginner team (five committed steps)

1. **Showcase-cut cleanup** — `ShowcaseBuild` owns presentation cuts; checklist cut for good
   (see the entry below).
2. **`ProceduralArt` split by art family** — one `partial class` over five files (core cache +
   key switch + Canvas2D, then `.Rooms / .Props / .Faces / .Documents`). 1042 lines became
   338 + four ~200-line files; zero call-site changes.
3. **One helper vocabulary** — `UIFactoryLocal` (RoomBuilder) merged into `UIFactory`;
   `NewRect` / `FullImage` live with the rest of the rect vocabulary.
4. **`WanderingAnomaly` long methods split** — `MoveBehavior` is a 5-line dispatch over
   `MoveBlink / MoveSlide / MoveSprint / MoveCoverHug` (one rule sheet per behavior; adding a
   behavior = adding one method, as ARCHITECTURE promises); `TickStates` lands reactions via
   `ApplyStateStep / AnnounceExposure / AnnounceRepel / TickCatch`.
5. **`CheckpointView` split by desk area** — same class over nine partial files
   (`TopBar, Papers, Window, Speech, Interview, Decision, Overlays, QueueLine, Sticky`);
   the core file keeps the state machine, shared fields and orchestration (1371 → 427 lines).
   Chosen over extracting widget classes on purpose: the desk is one tightly-coupled state
   machine, and partial files give per-area navigation without callback/context plumbing —
   the friendlier read for a beginner team. Promoting an area to a real class later stays
   possible, one area at a time, with FlowCheck as the safety net.

Every step: one commit, then FlowCheck + LogicTests + Verification/LogicTests +
verify.sh (compile vs real Unity assemblies + static audit). All green at every step.

## Showcase-cut cleanup (one owner per presentation decision)

- `ShowcaseBuild` (Core) now owns the build's presentation cuts; first flag: `LineDisplay`
  (waiting-line silhouettes + banner readout). The strips are built **only when the flag is
  on** — nothing is ever built-then-hidden — and refresh paths null-guard, so flag off means
  the code paths simply do not exist at runtime. FlowCheck asserts each line element is
  present exactly when the flag says so.
- The checklist clipboard was cut for good (it was built hidden with a commented-out show
  line): widget, row model, `CycleCheckRow`, `StylePaperRow`, layout regions, wireframe-table
  rows and the README/Controls mentions are gone. Git holds the restore path.
- Remaining "showcase" comments reworded into permanent art-direction notes (black backdrops,
  clue sprites, dark security office) so no reader mistakes a decision for a temporary hack.

## Floating box + vanishing doors (placeholder quad scale, post-simplification)

**Symptom:** a small translucent box floats at the centre of every patrol room (grey when lit,
brighter when dark, solid white while peeking); rooms sit on pure black; doors and wall-switch
plates draw as invisible dots even though their prompts and interactions still work.

**Root cause:** `ProceduralArt.MakeSolid` built `solid_white` — the one flat quad sprite every
world placeholder is scaled from — as a 4x4 texture at `UiPpu` (100 px/unit), i.e. a native size
of 0.04x0.04 world units. Placeholders set `localScale` in world units assuming a 1x1-unit quad,
so every one of them drew at 1/25th of its authored size: the room darkness overlay (scale
26.8x11) degenerated into the 1.07x0.44 floating box, door slabs into 4x8 px dots, switch plates
into sub-pixel specks. Headless proof: the FlowCheck dump printed
`darkness ... size=(1.07x0.44) screen=(632..692, 268..293)` — exactly the box in the screenshot.

**Fixes:**
- `solid_white` is now natively 1x1 world unit (`QuadPpu = 4`); uGUI Images ignore pixelsPerUnit,
  so every UI use of the sprite is untouched.
- `RoomBuilder.Quad(parent, name, pos, order, size, color)` is the single flat-placeholder
  constructor (size in units == localScale). Wall/floor bands, door slabs, server-door art,
  switch plates, the darkness overlay and the clue found-marker all route through it; the
  marker's hand-rolled duplicate in `Interactables` was removed.
- The darkness overlay is tinted BLACK (LightController only varies its alpha): it dims rooms
  instead of washing them white, peek holds it solid black, and the PeekLight SpriteMask hole
  (VisibleOutsideMask) finally works at full size, as documented.
- The custodian draws above the darkness (order 73 > 70) exactly as his comment always claimed
  ("lit by design"); anomalies remain under it (40) as silhouettes.

**Proof:** FlowCheck section `== placeholder quads draw at authored size ==` (darkness spans the
room and is black; all 3 hallway doors full-height at their RoomGraph anchors; switch plate
visible) plus the existing W-chain (door transitions, roaming crossings) and LogicTests.

## Symptom 0 diagnostic (ordered [Flow] chain, headless FlowCheck harness)
Unity is not available in this sandbox, so the chain below was produced by
`Tools/FlowCheck` (boots the real menu, clicks the real NEW GAME button, runs the
checkpoint bootstrap the way Awake would, clicks through the intro):

    [Flow] SetState MainMenu / ApplyVisibility MainMenu
    [Flow] NEW GAME clicked
    [Flow] loading scene: Hollowbrook
    [Flow] checkpoint bootstrap built (checkpoint=True loading=True dialogue=True)
    [Flow] queued action consumed in scene 'stub'
    [Flow] SetState ShiftIntro / ApplyVisibility ShiftIntro
    [Flow] intro shown (loading=True dialogue=True)
    [Flow] dialogue finished, invoking callback
    [Flow] intro callback fired
    [Flow] SetState Checkpoint / ApplyVisibility Checkpoint
    [Flow] BeginRun roster=4 -> NextVisitor index=-1 -> PresentVisitor Ana Delgado

Every expected line is present, so the state machine was never the failure: the symptom was
visual. Confirmed causes, in the order Part 0.4 prescribes:
1. **B4 (immediate cause):** `BorderedPanel` returned the INNER rect while the OUTER border stayed
   full-screen and active; `DialogueView.HideVisuals` hid only the inner, leaving an opaque grey
   rectangle over the checkpoint. Proof now: `panel_border` inactive after the intro (FlowCheck OK).
2. **B3:** `BuildDecor` created the window frame after the portrait, so it drew on top. Proof now:
   windowframe sibling index 122 < visitor 138 < speech 140 (FlowCheck OK).
3. **B2:** question column, record sheet and monitor were anchored partly off-screen; re-anchored
   onto `CheckpointLayout` named constants.

## Symptom 0 evidence (post-fix)
Screenshot: NOT POSSIBLE headless. Substitutes attached in this log: the full ordered chain above,
the two structural proofs (border inactive; sibling order), and the layout grid in
`CheckpointLayout.cs` (every panel center/size listed in one place for manual eyeballing).
No Debug.LogError and no exception appears in the FlowCheck output.

## Phases
- Phase 0: handshake safety net REINSTATED (it had been deleted by an earlier simplify pass):
  `_queuedAt`, `_handshakeArmed`, `CheckSceneHandshake()` + call at top of `Update()`; `[Flow]`
  diagnostics gated behind `GameRoot.VerboseFlow`. B4, B3, B2, B5, B12 fixed (commits B4/B3/B2).
- Phase 1: naming (`R` -> `_gameRoot` in touched files), subscription disposal pattern (B9).
- Phase 2 (B1): grep for SfxRequested/MusicContextRequested = 0 hits; direct `Audio.PlaySfx/PlayMusic` only.
- Phase 3: MenuScreens -> 6 files, HudAndCommon -> 4 files, RoomInstance split out; behaviour unchanged.
- Phase 5: B6 catch grace + single token; B7 single flashlight owner; B8 mid-shift continue index;
  B10 behavior-oddity gating; B11 map in school scene.
- Phase 6: one-line purpose comment on all 94 public types; magic numbers into CheckpointLayout/GameConfig.

## Behavior changes (documented)
- B8: Continue now resumes at the saved visitor instead of replaying the night from visitor 1.
  Save format gained a tolerant `visitorAtSave=` line; old saves read as 0 (old behaviour).
- B10: exhibition nights can now generate voice-only anomalies (the objective always claimed they could).
- B6: standing next to a threatening anomaly for `coverHugCatchSeconds` now catches you (scare + sanity
  + reposition). Previously it could never trigger.

## Reviewed but left alone
- Handshake safety net: considered dead code once, deleted once, caused exactly the bug it exists for.
  Reinstated and kept forever.
- `Tools/Verification/EngineSmoke`: was frozen-broken (renamed APIs); removed in the issue #9
  cleanup together with its stale doc claims. `Tools/FlowCheck` is the replacement diagnostic.
- `ProceduralArt`/`ProceduralAudio` internals: self-contained and readable; only dead keys were candidates
  and their use is concat-built at call sites, so nothing was deleted.
- Full A1 renaming across all 48 files: deferred; renaming untouched files adds diff noise without
  playtest coverage.

## Extra bugs found (not in Part B)
- `SettingsView` scroll viewport had a Mask with no Graphic (B12).
- `UIFactory.Label` base-size bookkeeping ignored manual fontSize writes (B5).

## Desk UI Fixes Pass — scanner stacking, ID stripe & flip, FIND alignment, record X (October 2026)
Four targeted desk fixes (task list from the owner's web-style report, mapped to Unity):
1. **ID over scanner (stacking):** root cause was build order — the right column (camera/tools) was
   built after `PapersLayer`, so the scan device drew OVER dragged papers. `Init` now builds in
   explicit stacking order: window → questions → tools/camera → **papers layer** → stamp tray →
   overlays; `BuildRightColumn` split into `BuildQuestionsPanel` / `BuildToolsAndCamera` /
   `BuildStampTray`. The slip is built inside the papers layer (`BuildSlip`). FlowCheck asserts
   `camera < PapersLayer < stamp tray` sibling order.
2. **Black spine + reliable flip:** the held ID card carries a 14-unit black stripe — left edge on
   the front, mirrored right on the back. The whole card is now a `Button` (transition None, no
   tint): pointer click flips, and because it's a Selectable the EventSystem fires it on
   Enter/Space when focused. Tap reliability root cause: `DragPaper._moved` accumulated raw screen
   pixels against a 6-unit threshold — at canvas scale >1 every small drag exceeded it and clicks
   never counted as taps; movement now accumulates in canvas units with a 10-unit threshold.
3. **FIND alignment:** the station's box and button now share a `searchrow` container — the input
   stretches to fill and FIND is right-pinned (anchor 1, full height): flush at any scale, no
   pixel nudges (root cause was independent `PlaceTL` coordinates that could drift).
4. **Record exit button:** `closefile` is anchored top-right (anchor/pivot 1,1 at −4,−4) instead of
   an absolute placement, and `Inner.RecordHeader` narrowed to 284 so the X never collides with the
   header text.
Also: `PresentVisitor` ends with `RefreshHud()` and stamps are built dim (alpha .35) so tray state
can never lag presentation; a duplicate `BuildStamp` pair left by the column split was removed
(caught by FlowCheck reading the stale first instance).
FlowCheck: 842 checks green. LogicTests invariant + ICU/de-DE green. Frozen suite 663/663. verify.sh clean.

## Floor Files pass — registry terminal replaced by a folder cabinet (October 2026)
The typed **REGISTRY TERMINAL** (header, "ID or name" input, FIND button) is gone. The station is
now the **FLOOR FILES** cabinet: two manila folder tabs (procedural `folder_tab` sprite, dark
manila, hover brighten) for **1ST FLOOR · ROOM 101** and **2ND FLOOR · ROOM 202**.
1. **Folders → drawer (accordion):** clicking a tab slides the drawer open at the new grid region
   `FileDrawer (16,188 320x270)` — under the station, above TL/RN, under the papers layer — and
   lists that floor's files (name + registry number per row, manila left edge); clicking the open
   tab again collapses it. The open tab stays lit (`FolderOpen` tint).
2. **Direct selection replaces search:** clicking a file row runs the same record-window path the
   old FIND triggered (`OpenCanonFile` → `FillRecordSheet(StudentIdentity)` → record rises above
   the drawer, `FOUND: <NAME>` readout, paper sfx); the pulled row keeps a highlighted tint. The
   admin-night rule is unchanged: no file replaces the attached memo.
3. **Grouping is pure logic:** `RecordSearch.BuildFloorFolders` files canon students by
   `FloorOf(FirstClassroom)` (Room 101→1, Room 202→2), floors ascending, roster order inside a
   folder, deterministic; empty/null canon files nothing. An empty floor still opens the drawer
   with an explicit **NO FILES** row; a roster taller than the drawer shows what fits + a
   "+N MORE FILES" line instead of overflowing. `FindInCanon` stays (LogicTests cover it) but the
   desk no longer exposes a text box, so the `_searchField` focus guard in `TickHotkeys` is gone.
4. **Fresh desk per visitor:** `ResetHandover` closes the drawer and clears the selection instead
   of clearing the old query text.
Docs: README loop + controls table, ControlsView line, GAMEPLAY-OVERVIEW desk paragraph updated.
FlowCheck: 882 checks green (new: element presence, grid tables for tabs/rows/drawer, open /
collapse / pull / second-folder / highlight / NO FILES / admin-memo / stacking-under-papers
proofs; retired: FIND-alignment and typing-focus blocks). LogicTests 27,409 invariant + 27,411
ICU/de-DE green (+13 floor-folder checks). Frozen suite 663/663. verify.sh clean.

## Dark entry, spine-only flip, slip decoupling pass (October 2026)
Three owner-facing fixes, one per system:
1. **Rooms are entered dark (patrol):** `RoomBuilder` builds every room `startLit: false` (the
   Classroom-2 exception is retired) and `ViewManager.EnterRoomImmediate` calls the new silent
   `LightController.ForceDarkOnEntry()` on EVERY entry — peek returns included. No toast fires on
   entry; the HUD reads `LIGHTS: OFF — wall switch [E]` until the switch is thrown. The wall
   switch still toggles freely (the player is never locked in the dark) and the beam keys are
   untouched: `F` sweeps, `Q` focuses, `L` off — the handheld beam stays the portable light.
   Balance reads (battery recovery, dark sanity drain, dark 1.6x spawn odds, anomaly observation)
   all consume the same `Lit` state as before — darkness is now simply the starting condition.
2. **The ID flips by its black spine only (desk):** the whole-card `Button` is gone — the card
   body only drags. Both spines (front-left `idstripe`, mirrored back-right `idstripeback`) carry
   raycast + a subtle brighten Button (multiplied up; the spine is near-black) + the new
   `HoverCursor` component (procedural `cursor_hand` sprite; hand cursor while hovering the
   handle only, restored on exit/disable). Flip works both ways from the spines; `F` still flips.
3. **The slip never flips with the ID:** the flip used to follow the shared "last touched paper"
   state, so clicking the ID while the slip was last lifted flipped the SLIP. `FlipId`/`FlipSlip`
   now each target their own paper through one `FlipPaper` helper; the `F` hotkey routes through
   `FlipLastPaper` (keyboard parity keeps the slip's radio-note back reachable); tapping the
   taken slip flips the slip alone; the stamp mark rides the slip's flip (never the ID's).
   `ResetHandover` also turns both papers face-up, so flip state never carries across visitors.
New: `UI/HoverCursor.cs` (+ .meta), `Cursor`/`CursorMode` stubs in UnityStubs, `cursor_hand` art.
Docs: README loop + controls table, ControlsView desk/exploration lines, GAMEPLAY-OVERVIEW
checkpoint + patrol paragraphs.
FlowCheck: 894 checks green (new: W2 dark-entry arc, HUD readout, entry reset, relight guarantee;
spine-handle presence, body-not-a-button, both-ways spine flips, slip tap-flip, stamp-rides-slip,
decoupling proof, F routing, face-up carry-over). LogicTests 27,409 invariant + 27,411 ICU/de-DE
green (counts unchanged — no pure-logic change). Frozen suite 663/663. verify.sh clean.

## Verification & patrol audit pass: scroll-resize, scanner removal, flashlight, stamp box, appearance check (October 2026)
Six-part owner audit, one commit-sized change per task:
1. **Papers keep their size while scrolling (desk):** the wheel-zoom listener in
   `CheckpointView.Update` is gone — scrolling no longer resizes the ID or the slip anywhere.
   The deliberate `Z` toggle survives and moved into `TickHotkeys` (same desk-live gating,
   now headless-testable). Drag-lift (1.04x while held) and the 1..1.8 `DragPaper.Zoom`
   clamp are unchanged; overlay text scrolling (ScrollRect) untouched.
2. **The ID scanner is removed:** SCAN button, the CAMERA monitor panel + readout,
   `DoScan()`, the drop-ID-on-scanner/camera zones, `EventIds.ScanPerformed` and the
   "SCANNER READY" init are all gone; the layout grid retires `UtilityScan`/`Monitor`
   (their slot now carries the appearance check — no dead space). Question-budget refusals
   ("ALREADY ASKED THAT." / "NO MORE QUESTIONS") surface as HUD toasts instead of the dead
   readout. `RosterFactory.Scan`/`ScanResult`/`PresentationLog` stay in GameLogic — the
   frozen verification suite and LogicTests still exercise the pure duplicate-ID rule, and
   the verdict path still records presentations; only the desk UI stopped calling it.
   ID display, photo and manual verification (documents, floor files, questions) preserved.
3. **The patrol flashlight behaves (world):** rooms already entered dark — now the handheld
   beam arrives OFF too. `ViewManager.EnterRoomImmediate` calls the new silent
   `FlashlightRig.ForceBeamOffOnEntry()` on every entry (patrol start, doors, peek returns):
   latches clear, cone sprite + light mask deactivate, so "LIGHTS: OFF" finally means dark.
   `F` toggles the beam ON (sweep) / OFF; `Q`/`L` stay. Mouse buttons are no longer beam
   input — a held LMB/RMB (doors, switches, dialogue) used to light the beam without any key
   press, which is what read as "the flashlight turns itself on". The HUD room line now
   carries the live beam state (`BEAM: OFF/SWEEP/FOCUS`, refreshed via the battery publish
   every `ApplyBeam` already fires), so status text and real illumination can never disagree.
4. **The stamp mark respects the slip (desk):** the verdict check/X used to land 90x90 at the
   raw drop point — smothering the written lines or hanging off the paper edge. The slip now
   prints a `stampbox` (130,52 60x60; REASON narrowed to 116 wide, clear of it) and
   `PlaceStampMark` snaps a 56x56 mark into that box with the seeded tilt — provably inside
   the paper and off the text at every zoom and viewport. Drop-to-stamp, A/D keys, the punch
   animation, one-stamp-per-slip and pending-until-hand-back all unchanged.
5. **Conditional appearance question (desk):** new `GameLogic/AppearanceCheck.cs` (pure) +
   `CheckpointView.Appearance.cs` panel in the retired camera's slot: "Does the student's
   current appearance differ from their ID photo?" — YES reveals "Why does the student look
   different…?" with a one-line reason field (the field's GO carries the InputField alone —
   never two Selectables on one object); NO hides it AND clears the previous explanation, so
   stale text can never be submitted. YES without a reason refuses BOTH stamp paths with a
   toast (the stamp is the submit). While the field has focus every desk hotkey stands down
   (a typed "a" never stamps) and Esc returns focus. The answered check rides into TONIGHT'S
   LOG with the verdict (unanswered = excluded), syncs to `CheckpointQueue.ActiveAppearance*`
   and persists through the queue snapshot (`queueAppear`/`queueAppearWhy`; newline-flattened,
   '=' safe; pre-feature saves load unanswered). No face recognition anywhere — the player's
   Yes/No IS the data.
Docs: README (loop, controls, fairness, save note), ControlsView sheet, GAMEPLAY-OVERVIEW
(checkpoint + patrol paragraphs).
FlowCheck: 906 checks green (new: scanner-removal proofs, appearance-form arc incl. stamp
gate/focus guard/save keys/log line/per-visitor reset, stamp-box geometry, Z-toggle proofs,
W1 keyboard-only arbitration + cone/mask visibility + HUD BEAM sync, W2 beam entry-reset arc;
retired: scan-drop, camera readout and mouse-hold beam blocks). LogicTests 27,443 invariant +
27,445 ICU/de-DE green (+34 appearance/queue/save checks). Frozen suite 663/663. verify.sh
compile + audit clean.

## Owner audit pass 2: manual peek lamp, drop-position stamping, face-check removal, speech bubble, smaller ID (October 2026)
Seven-part owner prompt, one scoped change per task:
1. **The flashlight stays off until F (world):** the beam side was already manual (previous
   pass); the remaining auto-on light was the classroom PEEK itself: `PeekLight` — the
   cursor-locked lamp — switched on the moment the room was entered, without any key press.
   The peek SESSION and the LAMP are now separate states: `Active` (the room is framed;
   cursor tracking, prompts and the E exit keep working, so the dark can never trap the
   player) and `Lit` (the light itself — born OFF on EVERY entry, toggled by `F` alone;
   mask + glow + the `Covers`/`IsPeekLit` illumination query all answer to the same flag).
   Mouse movement never lights anything. Every toggle publishes `LightModeChanged` + a
   toast, and the HUD room line reads `LAMP: ON/OFF` while peeking (`BEAM:` on patrol), so
   the indicator cannot disagree with the real light. The handheld beam still forces off on
   every room entry / peek / patrol restart (one press on, one press off — no double-F).
2. **The stamp lands where you drop it (desk):** Papers, Please style. The printed
   `stampbox` is gone (the REASON line is full width again) and the new pure
   `GameLogic/StampPlacement.cs` maps the drop point from desk space into the slip's own
   space — position, zoom scale and resting tilt included — then clamps so the whole tilted
   56px mark stays inside the printable area (2px edge padding): a centre drop marks the
   centre, a corner drop marks the corner (clamped in, never overhanging). The mark stays a
   child layer of the slip, so it rides drags, flips and zooms. `A`/`D` keyboard verdicts
   land on the familiar lower-right default. One-stamp-per-slip, pending-until-hand-back,
   the punch animation, the per-inspection seeded tilt and both stamp arts are unchanged.
3. **The face check is removed completely:** `CheckpointView.Appearance.cs` +
   `GameLogic/AppearanceCheck.cs` (and their .meta files) deleted; the `L.Appearance` region
   and every `Inner.App*` sub-rect retired; the YES/NO buttons, the follow-up question, the
   reason InputField + typing-focus hotkey guard, the stamp gate, the per-visitor reset, the
   queue sync (`ActiveAppearance*`) and the snapshot/save keys (`queueAppear`/
   `queueAppearWhy`) are all gone — old saves still load (unknown keys are ignored). The
   RULES NOTE text no longer teaches the form. The ID card, photo, student info and the four
   existing questions are untouched; no replacement face/appearance system was added. The
   verdict stamp tray moved UP into the vacated slot so the right column stays balanced
   (questions above, stamps below, open desk underneath). Night 3's objective/rule note were
   also cleaned — they still told the player to "Use SCAN" (scanner retired last pass).
4. **Student dialogue is a speech bubble beside the head (desk):** the rectangular DIALOGUE
   strip under the window is gone; `CheckpointView.Speech.cs` now builds a comic-style
   bubble at `L.SpeechBubble (718,100,248,210)` — right of the portrait's head, never over
   the face — in the desk's paper stock + border, with a new procedural `bubble_tail`
   sprite pointing at the student (borderless base buried under the panel for a seamless
   comics seam). The bubble grows DOWN with the wrapped line count (pure
   `SpeechBubbleLayout`: greedy wrap estimate, word-boundary truncation with an ellipsis,
   height formula), capped at its registered box. Timing lives in the pure
   `SpeechBubbleClock`: the bubble is removed exactly `HideDelaySeconds = 2` after the LAST
   line (the dialogue end), a new line cancels the pending hide and restarts the countdown,
   and a generation counter makes stale delayed hides impossible (one model, no coroutine
   pile-up). The countdown freezes while paused, behind blocking sheets and while the
   scripted `DialogueView` sequence is still talking (the admin's typed lines end first,
   then the 2 s start). Leaving the desk (patrol), a new shift, or the visitor walking off
   hides the bubble instantly — no timer survives the room change. The bubble only speaks
   with the student's voice: radio warnings and district bulletins go to HUD toasts. The
   bubble is raycast-transparent and renders under the papers layer.
5. **The ID card's default display is slightly smaller (desk):** `DragPaper.BaseScale`
   (new, default 1) — `Zoom` now reads/writes RELATIVE to it (clamp still 1..1.8). The held
   ID is born and rests at `IdCardScale = 0.82` (~18% smaller), the whole card scaling
   uniformly: the 304x209 slot rect, the aspect, the photo, every row and the spine flip
   handle stay exactly as designed and readable; drag/lift/clamp honour the smaller size.
   The Z-zoom toggle, the no-scroll-resize guarantee and the take/dock paths are unchanged
   (docking now lands the card's top-left precisely on its slot corner). The slip, the
   record sheet and the counter-offered card (0.6) keep their sizes.
6. **The scanner stays removed:** no scanner code, UI or logic reintroduced; FlowCheck's
   negative proofs (no SCAN button / camera monitor / readout) still run, and the manual
   verification workflow (documents, floor files, questions) is preserved.
Docs: README (loop, controls, layout tree), ControlsView sheet, GAMEPLAY-OVERVIEW
(checkpoint + patrol paragraphs — including the stale "two chalk circles" sentence),
ShiftPlan night-3 text.
FlowCheck: 910 checks green (new: W10 peek-lamp arc — dark entry, mouse-never-lights, one-F
toggle both ways, mask/glow hidden, coverage query agreement, HUD LAMP sync + toast + hint
line, dark prompt/E exit, second-classroom reset; BubbleProof timing/geometry/cleanup;
StampPlaceProof drop-point mapping on a zoomed slip + mark-rides-slip + verdict art;
corner clamp + keyboard default in IssueOneProofs; smaller-ID geometry, lift/drop base and
corner-clamp proofs; bubble/stamp/layout negative proofs. Retired: the appearance-form arc,
the stamp-box geometry and the speech-strip checks). LogicTests 27,519 invariant green
(-35 appearance/queue/save checks; +111 SpeechBubbleClock / SpeechBubbleLayout /
StampPlacement checks). Frozen suite 663/663. verify.sh compile + audit clean.

## Readable bubble, compact tray, graded beam light, focus everywhere, face check pass (October 2026)
Six owner requests, one pass; every change rides the existing systems.
1. **Hover-to-read speech bubble:** `SpeechBubbleClock` gains `Hovering`/`SetHover` — entering the
   bubble suspends the 2 s countdown, leaving RESTARTS it from zero (and changes nothing while the
   dialogue is still talking, because the countdown only begins once it ends). The bubble's border
   panel becomes the single raycast target in the bubble (`UI/HoverSense.cs`, fill/text/tail stay
   click-through; the box hangs over empty glass beside the head). One clock, generation-guarded:
   a new line still cancels any pending hide, and a hidden bubble cannot be hovered.
2. **Compact verdict tray:** `L.StampAllow/StampDeny` 152x150 -> 118x117 (-22% linear), one aligned
   row centred in the old 296..446 band, 8 px apart, D box right-flush; colours, labels (18 px) and
   the A/D hotkeys untouched; >=96 px grab area kept.
3. **Stamp readability:** `StampMarkSize` 39 -> 45 px (+15.4%, still 20% under the original 56):
   square + preserveAspect, clamp margin 30.4 px keeps the tilted mark fully on the 200x120 slip,
   drop-point placement and slip-riding unchanged.
4. **The beam lights the WORLD, gradually:** the stencil hole was binary, so the reveal popped out
   of a flat cut-out. New `beam_penumbra` sheet (ProceduralArt, cone-space gradient: full reveal in
   the core, soft side falloff at the wedge edge, distance fade at the reach, exactly the room's
   ambient alpha at the rim) rides at order 72 over the hole, scaled 1.35x with the cone (mask
   overscale 1.06 -> 1.35 so the fade lands on the ambient, no seam). `LightController.AmbientAlpha`
   / `ViewManager.AmbientAlpha` feed its tint, so lit rooms penumbra gently and dark corridors fade
   to their own black. OFF removes cone + mask + penumbra together.
5. **Focus, everywhere:** patrol Focus (Q) was already the arbitration; it now also concentrates
   (core tint 0.30 -> 0.42) while Sweep stays the wide wash. The classroom peek lamp gains the same
   language: Q toggles wide (2.6 u) <-> focus (1.3 u, hotter glow) — reshapes real coverage
   (`EffectiveRadius` drives mask, glow AND `Covers`), never lights the lamp, and OFF wins over
   whichever mode was last set. HUD reads `LAMP: ON · FOCUS`.
6. **Face check returns, interactive and truthful:** click the ID PHOTO (`Checkpoint/IdPhotoSelect`
   — a clean click selects with a thin pale frame on the photo alone; a real drag is forwarded to
   the card past 6 px, so selecting never yanks the card), then click the face at the window
   (`UI/ClickSense`). The answer is the run's own data: `VisitorData.DisplayedFacesMatch` (window
   portrait vs printed photo, features AND the anomaly distortion — the administrator's glitched
   window face is her tell), i.e. exactly what the sprites render and what PhotoMismatch/
   PhotoSubtleMismatch book. A brief frame answers: muted green = same face, muted amber = not; it
   informs only (no verdict, no deny). Wrong order asks for the photo first; selection resets on a
   new visitor and on leaving the desk; one cue coroutine at a time.
New: `UI/HoverSense.cs`, `UI/ClickSense.cs`, `Checkpoint/IdPhotoSelect.cs`,
`Checkpoint/CheckpointView.FaceCompare.cs` (+ metas). Changed: `SpeechBubble.cs`,
`CheckpointView.Speech.cs`, `CheckpointLayout.cs`, `CheckpointView.Decision.cs`,
`CheckpointView.Papers.cs` (photo renamed `idphoto`), `CheckpointView.Window.cs` (`SetPaperHeld`
skips the cue inks), `CheckpointView.cs`, `ProceduralArt(.Props).cs`, `FlashlightRig.cs`,
`LightController.cs`, `ViewManager.cs`, `PeekLight.cs`, `HUDView.cs`, `ContentData.cs`,
`ControlsView.cs`, README. FlowCheck: +SB hover arc, +W2b penumbra/focus arc, +W10 lamp-focus arc,
+FaceCompareProof, tray/stamp expectations follow the constants. LogicTests: +hover clock proofs,
+face-truth group (a booked photo defect can never show a matching cue; the cue equals the faces as
rendered). Frozen suite untouched. verify.sh compile+audit clean (97 scripts, 15,601 lines);
LogicTests 27,548/27,548; FlowCheck 982 OK / 0 BAD; frozen 663/663.

## Face-hitbox, lighting sync audit, bigger stamp, temporary asset set pass (October 2026)
1. **Face clicks match pixels:** `preserveAspect` Images raycast their WHOLE slot, so photo/student
   clicks landed on letterbox margins. New `UIFactory.Letterbox` (pure) + `AspectHitbox`: an
   invisible child click-catcher fitted to the sprite actually displayed, re-synced on every face
   swap (`SyncFaceHitbox`), so hits are correct at any card zoom, position or viewport scale. The
   photo's catcher owns `IdPhotoSelect` (clean click selects; >6 px of movement forwards the gesture
   to the card's DragPaper, so selecting never yanks the card and dragging still works from the
   photo); the portrait's catcher owns `ClickSense`.
2. **Lighting state audit:** new FlowCheck `CheckLightSync` walks entry, L-off, dark corridor, peek
   entry/exit and pause/resume asserting cone + stencil mask + penumbra activity AND the HUD readout
   all agree with the single beam state (peek swaps the readout to the lamp line). All green: no
   desync exists — the beam graphic, its illumination and the indicator derive from one state
   (`FlashlightRig.Beam`), and OFF deactivates all three layers together.
3. **Stamp:** `StampMarkSize` 45 -> 60 px (+33.3%, inside the requested 30-40%), square +
   preserveAspect, clamp margin 39.8 px keeps the tilted mark on the 200x120 slip, drop-point
   placement and slip-riding unchanged; the temp ink asset's tight margins keep NAME/ID readable.
4. **Temporary asset set (AI-generated, post-processed, integrated):**
   `Resources/Art/student_face_01..06.png` (muted grayscale, one coherent framing, grain),
   `anomaly_face_01..03.png` (restrained uncanny: wide unblinking eyes, static patches, tilt),
   `stamp_approval_temp.png` (worn double-ring check, white keyed out to alpha, trimmed tight).
   Mapping is pure game data: `VisitorData.FaceAssetIndex` (canon position, set by RosterFactory,
   saved, -1 fallback) + `GameLogic/FaceAssets` keys; `Bootstrap/FaceArt` picks asset-vs-procedural
   per face: window = student asset (anomaly asset when the run says the window portrait is
   distorted), card = the SAME asset only when `PhotoMatches`, else the procedural painter of the
   card's own portrait triple — so a mismatch always SHOWS two different people and the cue never
   contradicts the pixels. `ProceduralArt.TryGet` + a `stamp_allow -> stamp_approval_temp` alias make
   missing files fall back gracefully (no broken images); deleting a file restores the old art.
   Previews: `Tools/face_asset_preview.png`, raws kept in `Tools/faces_raw/`.
FlowCheck: +CheckLightSync at six transition points, +hitbox/letterbox proofs, +face-asset file and
stability proofs. LogicTests: +FaceAssetMappingTests (stable wrap, same-face-on-match, no borrowed
faces, distorted => anomaly key, procedural fallback). verify.sh compile+audit clean (99 scripts,
15,793 lines); LogicTests 27,555/27,555; FlowCheck 1001 OK / 0 BAD; frozen 663/663.

## Flashlight overhaul, white-square fix, paired anomaly faces pass (October 2026)
1. **White square over the ID photo — root cause:** five portrait Images (`idphoto`, the counter
   `photo`, `visitor`, the security-room and dialogue portraits) were built with `sprite = null`
   and uGUI's default `Color.white`; a null-sprite Image renders the engine's white texture, i.e. a
   solid white rectangle over the photo slot until a sprite lands. Fix: a designed
   `face_placeholder` sprite (dark photo-gray backdrop + muted silhouette bust, same language as the
   real portraits) is now the build-time sprite everywhere, `FaceArt` can never return null, and a
   FlowCheck proof asserts no visible white sprite-less Image exists on the desk.
2. **Flashlight overhaul:** one pure profile (`GameLogic/BeamProfile`) now feeds every layer —
   `light_cone` repainted as a soft tapered beam (hot pool at the lamp, feathered shoulders,
   long decay at the reach, faint sub-cutoff halo that lifts nearby surfaces without cutting the
   stencil), `beam_penumbra` repainted as its exact inverse so the graded reveal lands on the room's
   ambient alpha with no seam and no flat cut-out. The rig damps displayed aim (1-exp(-14dt)) and
   displayed geometry (1-exp(-12dt)) so the beam glides under mouse jitter and morphs across
   Sweep<->Focus instead of popping; logical cone tests stay exact. Focus keeps Q, narrows (7.5u/10°)
   and burns hotter (tint 0.42 vs 0.30); OFF deactivates cone + mask + penumbra together; the
   classroom lamp's independent F/Q controls are untouched. New proofs: cross-section shoulder,
   along-beam decay, sub-cutoff halo, plus the six-point `CheckLightSync` state audit.
   (A first bake forgot the wedge's u/t normalisation and produced a cylinder — caught by the sim
   and the profile proofs, fixed in both bakers.)
3. **Paired anomaly faces:** six AI-derived variants of the shipped student portraits
   (`student_face_01_anomaly.png` .. `_06_anomaly.png`, same framing/grain, restrained uncanny
   tells: milky mis-set pupil, too-wide unblinking eyes, over-symmetric smile, blacked cap shadow,
   double-exposure ghost edge, opaque lenses). `FaceAssets.AnomalyKey` is now the PAIRED key, and
   `FaceArt` renders the window face with the student's own variant whenever the run's data says
   `IsAnomaly` (or distorted) while the ID card keeps the NORMAL reference — so the mismatch is
   visible by eye and `DisplayedFacesMatch` (now `PhotoMatches && !IsAnomaly && distortion-equal`)
   still agrees with the pixels. The three generic `anomaly_face_0*.png` files were retired.
Docs: `docs/ui/hallway_lighting_sim.png` (v4), `Tools/anomaly_pair_preview.png` (pairs at 48/96 px).
verify.sh compile+audit clean (100 scripts, 15,869 lines); LogicTests 27,556/27,556; FlowCheck
1007 OK / 0 BAD; frozen suite 663/663.

## Night-shift desk UI pass (HTML mockup -> uGUI, October 2026)
The approved `Hollowbrook Night Shift.html` mockup (fixed 2576x1449 stage) was converted into the
desk grid at exactly stage/2.0125 = the 1280x720 reference: `CheckpointLayout` re-tabled (top bar
81px with sanity number + clock + pause; registry station + drawer; booth hoods, frame, glow and
counter bases; bubble beside the head; questions + inline college map + APPROVE/DECLINE blocks with
caption strips; corner patrol/end-shift; stands for the waiting line). Palette swapped globally to
the mockup grayscale (`UIFactory.Ns*`: ink #0c0c0c, desk #161616, paper #ececec/#c9c9c9, chalk,
mid #8a8a8a) and the three mockup type roles are bundled open-licence fonts under
`Resources/Fonts/` (Barlow Condensed UI, Special Elite paper type, Caveat Brush hand) with a safe
fallback to the built-in font. New stage backdrop (`desk_bg` radial wash + grain + vignette),
hooded queue figures (`hood_figure`) on the stands, inline map panel (still opens the full overlay),
sheet titles per mockup (NIGHT SECURITY LOG / NIGHT SECURITY RULES). Mechanics untouched: drag,
spine flip, zoom, drop-point stamping, questions, bubble hover + 2s cooldown, face compare,
shortcuts. Documented deviations for readability/interaction: ID card and slip keep their readable
sizes (repositioned to the mockup's counter cluster), bubble capped at 3 lines with ellipsis and
shifted to x=740 so it never covers the face hitbox, patrol/end-shift in the bottom corners.
FlowCheck layout audit re-baselined to the new table (ExpectRegion + internal grids + overlap
matrix); the white-box proof is now targeted (face-chain sprites real, sprite-less overlay inks
transparent). verify.sh compile+audit clean (100 scripts, 16,025 lines); LogicTests 27,556/27,556;
FlowCheck 1003 OK / 0 BAD; frozen 663/663.

## Screenshot-driven Unity hotfix pass (October 2026)
Four defects visible in the owner's editor screenshots, all root-caused and fixed:
1. **White square over the ID photo:** the selection/comparison cues were `BorderedPanel`s — whose
   OUTER Image is a FILLED rect — so a selected photo (or a playing cue) painted a pale solid square
   over the photograph. Both cues now use a new hollow `outline_frame` sprite (opaque border,
   transparent centre): they outline, never cover.
2. **Dark trapezoids flanking the beam:** the penumbra sheet added its darkness ON TOP of the room
   darkness outside the wedge but inside its rect (double-dark corners). New `BeamAreaMask`
   (SpriteMask, range 60..70, 0.98x the penumbra sheet) cuts the base+pool layers exactly where the
   penumbra takes over, so the penumbra REPLACES the ambient inside the beam area and equals it at
   the boundary — one coherent beam, no geometric shadows.
3. **HUD collisions under the top bar:** LightMeter/TrackMeter moved to (348,84,200,40) /
   (568,84,110,40), clear of Findings/RoomInfo and of each other's captions.
4. **Portrait framing:** `ShowVisitorPortrait` re-asserts the portrait rect + preserveAspect on
   every present, so no stale rect/anchor can leave the face off-centre.
5. **§6 automation:** `ResetHandover` now offers AND auto-takes both papers (TakeId + DockIdToSlot +
   TakeSlipAt) — the inspection never gates on dragging; dragging still arranges. The earlier
   auto-take attempt had deleted the Offer calls (which fill + activate the slip); restored before
   the take. ControlsView/README wording updated.
FlowCheck proofs rewired (auto-docs, hollow frames, area mask, HUD row). verify.sh compile+audit
clean (100 scripts, 16,078 lines); LogicTests 27,556/27,556; FlowCheck 1008 OK / 0 BAD; frozen
663/663. The single-file browser game was NOT modified this pass; its 50/50 Playwright result from
the previous pass still applies to its unchanged bytes.

## Owner-feedback pass: face-match removal, instant beam, pull drawer, new ground-floor plan
Five requested changes, each root-caused before editing, each with a proof that runs headlessly.

**1. The ID-photo ↔ face matching mechanic is gone (deleted, not hidden).**
Root cause of its existence: it was bolted on as an "informational" cue, but it put a clickable
widget where the design wants the player's own eyes. Removed the whole feature, not its entry
points: deleted `Checkpoint/CheckpointView.FaceCompare.cs`, `Checkpoint/IdPhotoSelect.cs` and
`UI/ClickSense.cs` (+ metas), the `BuildFaceCompare()` build step, the `ResetFaceCompare()` /
`SyncFaceHitboxes()` calls, the `_cueInks` exemption in `SetPaperHeld`, the `idphotohit` /
`visitorhit` click-catchers, the `photosel` / `facecue` hollow frames, the `outline_frame` sprite
key and its painter, and `UIFactory.AspectHitbox` / `SyncAspectHitbox` (only that feature used
them). The ID photo, the window portrait and the counter photo are now `raycastTarget = false`
with **no** component but their `Image`, so no handler can fire from them and a drag that starts
on the photograph reaches the card's `DragPaper` again. Deliberately KEPT: `FaceArt`,
`VisitorData.PhotoMatches` / `DisplayedFacesMatch`, `DefectType.PhotoMismatch` /
`PhotoSubtleMismatch` and the face assets — anomaly detection, scoring and the fairness invariant
all read those, and the eye-check is still a real way to catch an impostor. `FrameToSlot` (the
letterbox centring) moved to `CheckpointView.Window.cs`; it is portrait framing, not the mechanic.
Proof: `FaceMatchRetiredProof` — nodes gone, no pointer handler of ANY kind on either face, no
"click the ID photo" wording anywhere in the desk/HUD text, card still draggable from the photo,
and both faces still render real sprites at their exact letterbox rects.

**2. The handheld beam now appears on the activation frame.**
Root cause: the displayed aim (`_aimSmooth`, damped at 14/s) and the displayed geometry
(`_dispRange` / `_dispHalfTan`, damped at 12/s) were *only* moved from `AimBeam`, so on the frame
F went down the cone appeared with the previous aim (still `Vector3.right` after a room entry) and
the previous mode's shape, then swung/stretched into place over ~15 frames. Damping a HELD lamp is
right; damping the press that creates it is lag. Fix: `ApplyBeam` detects off→on, snaps
`_aimSmooth` to the cursor and the displayed geometry to the mode's `BeamParams`, then calls the
new `PlaceBeam()` so the cone and its stencil mask are positioned, rotated and scaled in the same
call. No timer, no coroutine, no fade: full tint on frame one. A held beam keeps its glide and a
Sweep↔Focus change while lit keeps its morph. `AimFromCursor` is now the single source for the aim
used by both paths, so the art and the logical cone cannot disagree. Proof: W2c — one
`PressKey(F)` frame and the cone is at the cursor's angle, at `sweepRange` length, mask riding
1.06x on it, both on the aim mid-point, alpha already 0.30; five on/off cycles all instant and
each aimed at the NEW cursor; `L` still puts it out at once.

**3. The FLOOR FILES cabinet is a physical pull drawer.**
Root cause: the "drawer" was a hover-gated panel (`drawerzone` + `SetDrawerBody`) that appeared
under the cabinet and vanished when the pointer left, so it read as a pop-up, and it was built
*after* `PapersLayer`, contradicting its own comment that a pulled file lands on top of it.
Rebuilt in a new `CheckpointView.FileDrawer.cs`, built BEFORE the papers so the record sheet really
does land above it. The mouth (`filedrawer_border`) stays pinned to its approved grid rect so
`DeskLayoutAudit` still passes; inside it a `RectMask2D` hides the body while it is pushed up into
the cabinet, the body slides DOWN/out on a fast ease-out with a 3.5% scale cue, the cavity behind
the front lip darkens, the worn front + handle ride out a few px with it, and the files fade and
slide in staggered behind the body. Every number lives in the pure `GameLogic/DrawerMotion`
(0.14 s, no overshoot, no bounce). The registry readout moved ONTO the drawer front, beside the
handle: the pulled drawer covers the foot of the cabinet front, so the old `Inner.StationOut` slot
would have hidden "FOUND: ..." exactly while the player was using it (that region is retired).
One float drives the whole motion, so repeated clicks reverse the same animation instead of
stacking: duplicate drawers and duplicate file sets are impossible by construction. `OpenDrawer` /
`CloseDrawer` step the motion on the input frame itself (one frame's worth, flagged so `TickDrawer`
does not double-step). The hover gate is gone: the cabinet front is a `Button` (handle brightens on hover, stays lit while open), folder tabs only exist while
the drawer is out, taking a file leaves it out for browsing, and `Tab` is the keyboard equivalent.
A fresh visitor still shuts it (`CloseFileDrawer`, immediate when the desk itself hides).
Proofs: the DRAWER block (born shut and clipped, click-frame travel, monotonic settle, flush open,
7 rows fit, files clickable only once arrived, click-storm builds nothing twice, `Tab` both ways,
body parks back exactly where it started) plus `DrawerMotionTests` in LogicTests.

**4 & 5. The ground-floor map is a new hand-drawn plan, centred by construction.**
Root cause of the off-centre sheet: five hard-coded pixel offsets (`(0,10)` sheet, `(0,-64)` title,
`(0,74)` hint, `(0,34)` CLOSE) that only balanced at 1280x720 — the hint and CLOSE actually
overlapped in the y band 74..76, and the room boxes were asymmetric inside the sheet (left edge
-445, right edge +420), so the drawing looked pushed left even when the panel was centred.
Replaced with data + one fit: `GameLogic/FloorPlan` owns the rooms in a fixed 940x440 design space
(security checkpoint left, long central hallway, Classroom 1 above, Classroom 2 below, sealed
server/ID room at the corridor's east end, comfort rooms CR-1/CR-2 off that same end, doorways and
corridor stubs included), and `GameLogic/MapSheetLayout.Fit` turns the visible reference size into
sheet size, plan scale, plan centre, two clamped type scales and the CLOSE offset. `MapView` only
applies those numbers and re-applies them on `Show()` and on any `Screen` size change, so centring
and "never clipped" hold at every resolution instead of at one. `UIFactory.VisibleReferenceSize`
is the one place that asks the canvas what is actually visible (AspectMatchGuard widens/tallens the
reference by aspect). Styling: new procedural `map_paper` (aged, stained, folded, dog-eared, red
district stamp), `map_room` (wobbly double-stroked outline over a soft fill), `map_sealed` (red
hatch + chain + padlock) and `map_door`, chalk corridor runs, handwritten `CaveatBrush` title,
dark paper under a night lamp so the horror UI is never blown out. Sealed stays sealed: no `Button`
and `raycastTarget = false`, and it is not in `RoomGraph`. Comfort rooms are map-only by design —
no `RoomId`, no patrol mechanics — but they answer a click with a radio line instead of nothing.
Patrol rooms keep `TravelViaMap`, findings keep their red dots, YOU keeps its marker. The desk's
inline map widget now lists the same rooms in the same words.
Proofs: MAP block (dead-centre at 1280x720, sheet keeps the plan's aspect, every room inside the
plan area and exactly where the table puts it, 101 above / 202 below / sealed right, sealed has no
button and no raycast, comfort rooms present and inert, CLOSE under the sheet and on screen, and
all of it re-asserted at 1920x1080, 1024x768, 800x600, 2560x1080, 1280x600 and 4k) plus
`FloorPlanTests` and `MapSheetLayoutTests` in LogicTests.

**Visual verification without the editor.** Two opt-in SVG dumps were added so layout claims can be
looked at, not just asserted. A desk-wide sweep was added with them: every enabled `Button` must
have a raycast-enabled target graphic, which is how the CLOSE row on the new map was caught being
built as a dead control. `Tools/Preview/render_map_preview.py` (parses `FloorPlan` +
`MapSheetLayout` and draws the sheet at 7 resolutions with margins, gap-under-sheet and a clipping
column) and `dotnet run --project Tools/FlowCheck -- --dump-drawer=DIR` (boots to the desk through
the real NEW GAME path and draws the built hierarchy at shut / click-frame / +2 / +5 / open).
Looking at the first dumps caught four things the numeric proofs had not: the title rule was drawn
through the title's descenders; the cavity shade was 0px wide (a stretched anchor makes `sizeDelta`
a delta, not a size — every drawer rect now goes through `PlaceTL`, the desk's one placement rule);
the registry readout would have been hidden behind the pulled drawer front; and the y flip between
design space and RectTransform space needed pinning in ONE function (`MapSheetLayout.ToAnchoredY`)
with a test on it, because getting it backwards silently puts Classroom 1 under the hallway.

verify.sh compile+audit clean (102 scripts, 17,097 lines, minimal AND full Unity assembly sets);
LogicTests 27,657/27,657; FlowCheck 1093 OK / 0 BAD; frozen 663/663.

## Asset-manifest pass (naming contract for the art/audio team)
The owner asked for the asset list to start production against the design doc (interaction
objects, five shifts of 4/6/7/8/8+admin students, modes, endings). Delivered
`docs/ASSET-MANIFEST.md` + `docs/asset-manifest.csv` (126 rows: 90 art/face, 32 audio, 4 fonts)
with per-key format, pixel spec, ppu, priority tier mapped to the shifts, and current status.
To keep the list honest:
1. `ProceduralArt.AllKeys` / `ProceduralAudio.AllKeys` now expose the exact key sets, and a new
   FlowCheck section (`== asset manifest sync ==`) reads the CSV and proves both directions —
   every row is a real key, every key has a row, every key resolves to a sprite/clip named after
   itself, and every "temp-shipped" row has its file. That proof immediately caught two real
   fallback bugs: `token` and `rough_panel_dark` were painted by shared painters that named the
   sprite after the FIRST key, so a dropped-in file for either would have kept showing the
   other's art.
2. Faces: the approved 14-student plan is now named in full — `student_face_01..14` plus
   `_anomaly` variants (01-06 ship as temps) and `admin_face` / `admin_face_glitch`, the latter
   wired into `FaceArt` for the Night-5 badge photo and window tell. Manifest note: bump
   `FaceAssets.StudentFaceCount` 6->14 on the day 07-14 ship.
3. Several design-doc interaction objects were listed in code but never DRAWN: doors and the
   sealed server door were flat quads (the spriteKey sat unused in the signature), the wall switch
   was an anonymous white quad, and the clue objects (notice board, photo, clock, chalkboard,
   window, lost ID, light fixture) had no visual at all. `RoomBuilder` now draws the real keys
   standing on the floor line (new key `prop_lightswitch`), and `AddClue` hangs an optional sprite
   off each clue anchor — so every name in the manifest is visible in-game today as a placeholder
   and becomes final art the moment a PNG lands.
verify.sh compile+audit clean (102 scripts, 17,194 lines).

## Shift-5 administrator: dynamic branching conversation + ending-evaluator audit
Owner brief: extend the existing administrator encounter into a choice-driven conversation while
preserving every established rule (physical stamps, 2-question cap, clue order, save/queue
contracts, Lose > Secret > Win). Audit first, then the smallest architecture-compatible build.

**Audit findings fixed (proven gaps, not style):**
1. `EndingRules.Evaluate` awarded the ordinary Win on "five shifts completed" alone — the design's
   Win definition names the administrator's rejection explicitly. The Secret branch keeps its
   clue-chain gate (it is the clue chain's own prize); the Win branch now requires
   `Session.AdminRejected`, and a completed run with the administrator neither rejected nor
   allowed returns `None/admin_unresolved` (no ending invented, no Win leaked). Regression tests in
   `EndingAuditSuite`.
2. `GameSession.EvaluateEnding` folded `Pool.Pending` into `Breaches` on EVERY call — a repeated
   evaluation (ending screen, summary, resumed save) could count the same outstanding anomalies
   twice and flip a Win into a Lose. The fold now happens exactly once (`PoolFolded`, pool closed
   with the books) with regression tests calling the evaluator three times.
3. `GameRoot.OnVerdictResolved` had no re-entrance guard: a duplicated callback would double-count
   session counters. It now keeps a per-run set of resolved visitor indices (cleared on new game
   and on continue). That guard immediately exposed a fourth bug:
4. `RosterFactory.BuildAdministrator` never set `VisitorIndex` (only the roster-generation call
   site did), so a harness-presented administrator carried index 0 and collided with visitor 0.
   The index assignment moved into the builder — the administrator IS the extra final entry
   (`shift.VisitorCount`), by construction, on every path.

**The conversation (new, pure + thin view):**
- `GameLogic/AdminTalk.cs`: engine-free node graph (hub + 3 challenge branches with second beats,
  compliance branch with a refuse beat, two evidence branches gated on the REAL clue keys `photo`
  and `cabinet`, three question-follow-up branches gated on the REAL `QuestionSystem` asked-state
  and quoting the visitor's ACTUAL answers via tokens, a three-beat silence branch, and an exit on
  every node). Moods pick state-aware lines (after challenge / compliance / evidence / silence,
  hub returns); counters are the administrator's memory. `ValidateScript()` proves unique ids, real
  destinations, reachability, escapability from every node, and token sanity; LogicTests walks every
  branch for real.
- Contract enforced by tests: choices move ONLY `Session.AdminTalk` (node, used-mask, counters) —
  never the question budget, never clues, never a verdict, never an ending; the stamp + hand-back
  transaction is untouched and remains the single decision point; silence is counted, pausing is
  presentation-only, and always returns to a usable hub.
- `Checkpoint/CheckpointView.AdminTalk.cs`: compact uGUI choice panel that borrows the question
  column while open (question row + inline map step aside and return), re-open bar + `T` hotkey,
  commits BEFORE speaking (`_talkCommitting`) so a double click can never take one option twice,
  lines through the existing `DialogueView` (typewriter, Reduced Motion, pause-gated), silence
  beats through `WaitUnpaused`, and a `VerdictStamped` subscription scoped to THE ADMINISTRATOR so
  an ordinary student's stamp can never mark the encounter completed. State lives in
  `Session.AdminTalk`, saved as one `adminTalk=` KV (forward-compatible: old saves decode as an
  unopened conversation; a torn node id falls back to the hub).
- Location decision (brief §5): the encounter stays in the `Checkpoint` state — `AppViewState.
  AdminEncounter` remains reserved exactly as `docs/HANDOFF.md` A8 documents; one state machine
  (CheckpointView) owns the interaction, no parallel encounter, nothing moved into the School
  scene's late-arrival prototype.

**Verification:** LogicTests 27,737/27,737 (new AdminTalkSuite, EndingAuditSuite,
AdminRosterSuite); FlowCheck 1143 OK / 0 BAD (new AdminTalkProof: intro hands over to the hub,
column borrow/restore, budget untouched, double-commit blocked, question-adaptive branch quotes the
real answer, silence beats, T out-and-back without replay, save carries `adminTalk=`, pause and
walk-away contracts, stamp/hand-back still the decision, exactly-once counters, line abandoned);
frozen suite 663/663; verify.sh compile+audit clean (104 scripts, 18,400 lines). Unity Play Mode,
the Windows build and the audio/visual feel remain NOT RUN in this environment (no editor).
