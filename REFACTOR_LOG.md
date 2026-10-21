# Refactor log — readability pass (branch refactor/readability)

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
- `Tools/Verification`: frozen; its EngineSmoke no longer compiles (it called the removed SimulateLoad).
  Left untouched on purpose; `Tools/FlowCheck` is the replacement diagnostic.
- `ProceduralArt`/`ProceduralAudio` internals: self-contained and readable; only dead keys were candidates
  and their use is concat-built at call sites, so nothing was deleted.
- Full A1 renaming across all 48 files: deferred; renaming untouched files adds diff noise without
  playtest coverage.

## Extra bugs found (not in Part B)
- `SettingsView` scroll viewport had a Mask with no Graphic (B12).
- `UIFactory.Label` base-size bookkeeping ignored manual fontSize writes (B5).
