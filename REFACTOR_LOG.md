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
