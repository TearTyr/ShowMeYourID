# Show Me Your ID! — Twin Studio

**2D Psychological Horror · Deduction · Exploration** — PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation — yet still open at night so its students can finish
the term. Check IDs. Compare records. Ask up to **2 questions** — only after opening the interview.
Verdicts are physical and wordless: the student slides **ID + paper slip** to the counter; drag both to your desk (the ID rests slightly smaller than before and both papers keep their size while you scroll — `Z` is the one deliberate zoom; flip the ID by its **black spine** — the card body only drags — and the slip flips on its own, `F` flips whichever paper you touched last), pull the student's file from the **floor files** cabinet (browse the floor folders — a card whose number matches no filed record is itself a clue), compare the face at the window with the ID photo **with your own eyes** (the student's answers arrive in a **speech bubble beside their head**, fading 2 s after the line ends), then stamp **ACCEPT / DENY onto the slip** — *Papers, Please* style: drag a stamp onto the paper and the mark lands **where you dropped it** (always fully on the sheet; `A` / `D` stamp a corner default; one stamp per slip) — and **hand both papers back** at the window (`H`): the decision counts at hand-back, never at the drop. No confirm buttons, no post-decision text: the stamp mark, the thud, the subject walking off and the score tick are the whole feedback.
Walk the halls between visitors. Find the ten things the school is hiding. Keep them out.

> “Everyone has an ID. Not everyone is a student.”

---

## Quick start (for the team)

1. Install **Unity 6.3** (project stamped `6000.3.0f1`; Built-in Render Pipeline — no URP/HDRP,
   no paid packages). Also opens fine on any Unity 6.x or 2022.3 LTS — the code only uses APIs
   common to all of them (uGUI, legacy Input Manager, `LegacyRuntime.ttf`).
2. Open this folder as a Unity project and let it import (first import compiles ~100 scripts).
3. Press **Play** — the startup scene is `Assets/_Project/Scenes/MainMenu.unity` (build index 0).
   Each of the three scenes contains a single bootstrap object that builds everything in code:
   `MainMenu.unity` (menu), `Hollowbrook.unity` (Scene A — the inspection-desk loop),
   `School.unity` (Scene B — side-scroller + security office). Playing *any* of them boots the
   persistent core automatically.
4. Windows build: `File ▸ Build Settings ▸ Windows, Mono ▸ Build` (all three scenes are already
   in the build list via `ProjectSettings/EditorBuildSettings.asset`).

No external art or audio files are required: all sprites and sound are **generated
procedurally at runtime**. To upgrade any of them with real assets, drop files at
`Assets/_Project/Resources/Art/<key>.png` or `Resources/Audio/<key>.wav` — the game prefers
them automatically. **The naming contract for the art/audio pass is
[`docs/ASSET-MANIFEST.md`](docs/ASSET-MANIFEST.md)** (machine-readable twin:
`docs/asset-manifest.csv` — 126 rows with format, pixel spec, ppu, shift priority, status and
what each key is used by). The keys themselves live in `ProceduralArt.AllKeys` /
`ProceduralAudio.AllKeys` / `FaceAssets`, and `Tools/FlowCheck` proves manifest and code stay in
sync (`== asset manifest sync ==`).

## Controls

| Context | Input |
|---|---|
| Checkpoint | Mouse/touch: the ID + slip are placed on your desk when the student steps up — drag to arrange them (the scroll wheel never resizes papers), flip the ID by its black spine, open a floor folder and pull the student's file, drag a stamp onto the slip — the mark lands where you drop it — and drag both papers back to the window. Student dialogue appears in a speech bubble beside the visitor's head (gone 2 s after the line ends; **hover the bubble** to keep reading it). The face at the window and the ID photo are **yours to compare** — there is no click-to-match widget, no selection frame and no result cue anywhere on the desk. Keys: `1`–`4` questions, `A`/`D` stamp, `H` hand back, `F` flip (last paper touched), `Z` zoom (toggle, last paper touched), `Tab` pull the file drawer |
| Window talk | `V` focuses the talk box at the foot of the desk — type and press `Enter` to speak through the glass (`Shift+Enter` = newline, `Esc` = blur). A typed line that matches an unasked question spends one of the two question slots and is answered in the bubble exactly like the numbered buttons; anything else draws an in-character refusal. The `LOG` button opens the transcript sheet (last 24 lines). The box is disabled (with a saying-so placeholder) when nobody is at the window, locks during the send beat, clears on success and keeps your text on a failed send |
| Walk / Run | `A`/`D` or `◂`/`▸`, hold `Shift` to run |
| Interact | `E` (doors, clues, light switches) |
| Desk papers | drag the ID card / record sheet to arrange your desk; hover a question for what it cross-checks; `1`–`4` ask while the interview is open. There is no ID scanner, no face-check form and no face-matching click — verification is manual: documents, floor files, questions and your own eyes |
| Floor files | click the **FLOOR FILES** cabinet front (or `Tab`) to **pull the drawer out**; it answers on the click frame and is fully out in ~0.14 s. Pick a floor folder, then click a file to pull the record sheet onto the desk (it lands *on top* of the open drawer). Click the front again, or `Tab`, to push it back in; the drawer stays out while you browse, and a fresh visitor shuts it |
| Flashlight | `F` toggles the beam ON (wide Sweep) / OFF, `Q` Focus (tight, hotter core), `L` off — the mouse buttons are **not** beam input. Switching it on is **instant**: the cone and its stencil hole are aimed at the cursor, at the mode's full length and width and at full brightness, on the very frame the key goes down (only a *held* beam glides, and only a Sweep↔Focus change while lit morphs). The beam's reveal is graded: environment brightens by distance/angle inside the cone and fades back to the room's ambient at its rim. The classroom **peek lamp** uses the same language: `F` on/off, `Q` wide ↔ focus | Rooms are entered **dark** and every entry also puts the beam **out** (it relights only on a deliberate `F`/`Q`); the wall switch (`E`) lights the room. Classroom **peeks** start dark too: the cursor lamp is OFF until `F` (the same single toggle — mouse movement never lights it), and the exit works in the dark. The HUD reads both lights: `LIGHTS` (room) and `BEAM` / `LAMP` (handheld) |
| Map | `M` — the hand-drawn **HOLLOWBROOK COLLEGE — GROUND FLOOR** plan: security checkpoint on the left, the long central hallway, Classroom 1 (Room 101) above it, Classroom 2 (Room 202) below it, the **sealed** server / identification room (red hatch, not clickable) at the corridor's east end, and the two **comfort rooms** (CR-1 / CR-2) off that same end. Your position and every logged finding are marked on it. The sheet is centred and scaled to the window at every resolution; `M`/`Esc` or CLOSE dismisses it |
| Pause | `Esc` |
| Dialogue | Click / `Space` / `Enter` / `E` |
| Track an anomaly | hold the **Focus** beam on it to fill TRACK (Sweep or room lights only *hold* the meter); a full meter exposes and repels it, leaving a token |
| Collect a token | `E` while standing at it (count persists with the run) |
| Erase save data | `F2` ×3 on the main menu (each press ≤1.5 s apart, then confirm) |
| Security office (Scene B) | enter it from the office door inside Classroom 1 while patrolling |

## Options & persistence

- **Options** (`SETTINGS` in the main menu, and from Pause) is a compact **audio-only modal**:
  MASTER / MUSIC / SFX with live % read-outs and BACK. It blocks the screen behind it, applies
  instantly (even while paused), and closing it returns exactly where you were — pause stays
  paused. Display mode, text size, accessibility flags and the data reset still exist in
  `SettingsSystem` and their other entry points (boot-time display apply; `F2`×3 on the menu).
- Settings live in **PlayerPrefs** (`smyid.settings.*`) and survive a data reset; progression lives
  in `hollowbrook_save.txt` under `Application.persistentDataPath` (versioned, checksum-guarded —
  including the queue state: waiting line, arrival timer, window visitor and spent question budget).
- `F2` ×3 resets through `GameRoot.RequestDataReset` (one shared confirmation path).

## Game structure

- **Booth demo vs full game:** the `exhibitionMode` key in `Resources/GameConfig.asset` decides whether NEW GAME plays the one-night exhibition cut or the five-night story. Shipping default: **false = full story**.
- **Main menu** is focused for the current milestone: **NEW GAME** starts the five-night story
  directly. **CHALLENGE MODE** and **EXTRAS** are *not shown at all* (v1.0.6+ decision); the
  Endless/Challenge systems remain fully implemented and headless-tested behind
  `GameRoot.RunModesUnlocked = true` — flip that one flag to re-expose them without touching
  any other code.
- **Anomaly tracking** (master-prompt §3): hold the tight **Focus** beam on a wandering anomaly
  and the TRACK meter climbs; any lesser observation (wide Sweep beam, a lit room, a 0.35 s grace
  window) only *holds* it, and darkness decays it — ambient light can never resolve an encounter.
  Past 0.55 the corrupted identity is **exposed** (distorted face); a full meter **repels** it: the
  anomaly physically retreats, drops a collectible token exactly once, and may return later through
  another room. Sweep still matters: it freezes a Blink mid-stalk and interrupts a Sprint's windup.
  Half a second of Focus on a hallucination proves it was never there. The state machine lives in
  `GameLogic/AnomalyEncounter.cs`, the rates in `GameLogic/TrackingPolicy.cs` — both engine-free
  and unit-tested; the beam is a true cone (`GameLogic/BeamGeometry.cs`).
- **The line** (Phase 9): every shift starts behind a **sticky note** ("CLICK TO BEGIN"); from that
  click the booth runs a real **arrival queue** — a randomized 25–30 s clock walks the roster up
  on Night 1 (x0.8 per later night, double-speed while you are still interviewing, so dawdling
  grows the line; Endless/Challenge keep the fast 3–5 s race clock); the oldest waiter steps up
  right after your verdict banner. The line's desk display (silhouettes + `QUEUE: n | CURRENT: a/t`
  readout) is owned by one flag, `ShowcaseBuild.LineDisplay` (off in the current build: the UI is
  simply never built, while the queue itself still paces arrivals behind the scenes; the pressure
  knobs live in `GameConfigSO`). Anomalies entering the line can make one legitimate student
  **vanish** (seeded chance, one per shift, permanent, line-first). The queue is engine-free pure
  logic (`GameLogic/CheckpointQueue.cs`) owned by `GameRoot`, so patrol pauses it and saves
  restore it exactly.
- **Normal mode** — five shifts (4/6/7/8 students, then 8 + the district administrator on
  Night 5). Three endings: **Surviving Hollowbrook** (win), **Another Custodian** (lose at
  3 breaches or 3 false rejections), **The Truth Behind Hollowbrook** (secret: all ten
  environmental findings in the intended sequence, having investigated the hallway and both
  classrooms).
- **Endless mode** — beat the clock; correct calls buy seconds (anomaly rejects +20 s).
- **Challenge mode** — Endless with mid-run district rule mutations, ×2 sanity drain, ×2 score.
- Sanity is atmosphere and perception (vignette, distortion, hallucinations, heartbeats) —
  never a health bar. Accessibility: volumes, text size, **Reduced Motion**,
  **Photosensitivity-Safe** (no strobe flashes), and **Captions** for significant sounds.

## Repository layout

```
Assets/_Project/
  Scenes/Hollowbrook.unity        entry scene (bootstrap object only)
  Resources/GameConfig.asset      designer-tunable pacing/horror knobs (ScriptableObject)
  Scripts/
    GameLogic/                    PURE C# rules — zero UnityEngine dependency
      Enums, ContentData, GameRng, IdRules, RosterFactory, PresentationLog,
      QuestionSystem, InspectionRules, CheckpointQueue (arrival line + missing
      students), ClueRegistry, ShiftPlan, SanityModel, EndingRules, GameSession,
      SaveFormat, TrackingPolicy, BeamGeometry, FlashlightInput, BatteryModel,
      AnomalyEncounter, AnomalyBehaviorRules, RoomGraph, PatrolPool,
      SpeechBubble (bubble clock + text fitting), StampPlacement (drop-point maths),
      DrawerMotion (the file drawer's pull curve), FloorPlan (the ground-floor rooms),
      MapSheetLayout (the map sheet's fit/centre maths)
    Core/                         EventBus, GameEvent, EventIds, GameRoot, SaveSystem,
                                  SettingsSystem, GameConfigSO, IScreenView
    Bootstrap/                    GameBootstrap (builds the scene), UIFactory,
                                  ProceduralArt, ProceduralAudio
    Checkpoint/CheckpointView     security-room flow, documents, questions, speech bubble, stamps,
                                  the floor-files pull drawer (CheckpointView.FileDrawer.cs)
    World/                        ViewManager, PlayerController, Interactables, LightController
    Horror/                       WanderingAnomaly (Blink/Slide/Sprint/CoverHug),
                                  SanityController, ScareSystem, HallucinationSystem
    UI/                           menus, HUD, dialogue, map, results, endings, captions
    Audio/AudioDirector           event-driven music contexts + SFX pool + captions
Tools/
  generate_meta.py                regenerates stable .meta GUIDs
  Preview/render_map_preview.py   draws the map sheet to SVG from the real FloorPlan/MapSheetLayout
                                  numbers at several resolutions (centring + clipping contact sheet)
  Verification/                   HEADLESS test harness (not imported by Unity)
    UnityStubs/                   faithful UnityEngine/UnityEngine.UI API stubs
    LogicTests/                   runs the pure rules:   dotnet run --project Tools/Verification/LogicTests
    CompileCheck/                 compiles every script at C#9/Unity-2022.3 level
docs/                             audit, handoff report
```

### Architecture notes

- **Event-driven**: systems communicate through `EventBus` + `GameEvent` + `EventIds`
  (subscriptions are disposed by owners; the bus isolates handler exceptions).
- **Explicit references only**: `GameBootstrap` wires every view into `GameRoot`; gameplay
  code never calls `GameObject.Find`.
- **Deterministic content**: rosters regenerate from the run seed, so a save resumed at
  Shift N replays identical visitors (generation of earlier shifts advances the RNG).
- **Fairness invariant** (enforced by tests): legitimate visitors always have clean
  documents, truthful Normal-tone answers and are never false-flagged as duplicates; every
  anomaly carries at least one defect discoverable with documents, the floor files, the
  photo, or ≤2 questions.
- **Saves**: versioned, checksum-guarded text format at `Application.persistentDataPath`
  (corrupt files degrade to “no save” instead of throwing). Settings persist separately via
  PlayerPrefs.

## The owner's art kit (`Art/SecurityRoom`)

The layered SecurityRoom UI kit ships verbatim in `Art/SecurityRoom/`. Run
`python3 Tools/prep_securityroom_art.py` after any kit update: it slices and copies the export
into `Resources/Art/SecurityRoom/` (`sec_*` keys) and unpacks the Bahnschrift/Chiller font
families into `Resources/Fonts/kit_*`. The desk wears the kit as a **skin over the audited
grid** (`CheckpointView.KitSkin.cs`): geometry stays owned by `CheckpointLayout`, every kit
layer is click-through, and each skin call falls back to the procedural look if a file is
missing. `docs/ASSET-MANIFEST.md` §9 documents the contract; FlowCheck's
`SecurityRoom art kit` proof checks that all 32 sprites ship, resolve and stay click-through.

## Headless verification (no Unity required)

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
dotnet run --project Tools/Verification/LogicTests    # frozen suite: 663 checks
dotnet run --project Tools/LogicTests                 # independent suite: 27,773 checks
bash Tools/Verify/verify.sh                           # compile vs real Unity assemblies + static audit
dotnet run --project Tools/FlowCheck                  # button-level flow proofs: 1127 OK / 0 BAD

# opt-in visual checks (no Unity needed)
python3 Tools/Preview/render_map_preview.py out/      # map sheet at 7 resolutions: centring/clipping
dotnet run --project Tools/FlowCheck -- --dump-drawer=out/   # the file drawer at 5 points of its pull
dotnet run --project Tools/FlowCheck -- --dump-screen=out/screen.json --dump-sprites=out/procsprites/
python3 Tools/Preview/render_screen.py --dump out/screen.json --out out/shots --res 1280x720 1920x1080
```

`render_screen.py` replays a real hierarchy dump at any resolution with the real fonts and
9-slice maths (borders converted through the sprite's pixelsPerUnit, like uGUI) — the desk is
laid out in 1280x720 reference units, so one dump serves every breakpoint.

(`Tools/Verification/EngineSmoke` is frozen-broken by decision — it references a bootstrap API
removed in an earlier refactor; it is kept as-is, not run, and not fixed.)

`docs/HANDOFF.md` contains the full test matrix (PASS / FAIL / NOT RUN / BLOCKED) and the
documented conflict resolutions (question limit, administrator-breach ending priority,
clue sequence).

## Verification

- Every script compiles against **real Unity assemblies** (`Tools/Verify`, minimal + full sets):
  0 errors, 0 warnings, static audit clean.
- Frozen team suite: `Tools/Verification/LogicTests` **663 passing** (additive changes only —
  `AnomalyEncounter.Step`, save format and ending priority untouched semantically).
- Independent suite: `Tools/LogicTests` — **27,773 assertions, 0 failures** (includes the
  checkpoint-queue groups: arrival clock, FIFO promotion, pressure edges, missing student,
  snapshot/legacy resume, budget mask; plus `DrawerMotion`, `FloorPlan` and `MapSheetLayout`).
- Flow harness: `Tools/FlowCheck` — **1143 OK / 0 BAD** (sticky-note gate, queue proofs Q1–Q10,
  settings-modal proofs S1–S8, desk loop incl. the drawer pull, patrol world loop W0–W9 incl.
  the instant-beam proofs W2c, the ground-floor map proofs, and the face-match removal proofs).
- Scene-load health in the editor: `[Flow]` breadcrumbs for every step of NEW GAME / CONTINUE,
  plus a 3.5 s handshake watchdog that names and recovers from any bootstrap that fails to run.
- Not verifiable without the editor: pixel layout, audio mix, Play-mode feel, Windows build.
