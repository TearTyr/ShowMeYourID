# Show Me Your ID! — Twin Studio

**2D Psychological Horror · Deduction · Exploration** — PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation — yet still open at night so its students can finish
the term. Check IDs. Compare records. Ask up to **2 questions** — only after opening the interview.
Stamp **ALLOW** or **REJECT**. The verdict banner never tells you whether you were right.
Walk the halls between visitors. Find the ten things the school is hiding. Keep them out.

> “Everyone has an ID. Not everyone is a student.”

---

## Quick start (for the team)

1. Install **Unity 6.3** (project stamped `6000.3.0f1`; Built-in Render Pipeline — no URP/HDRP,
   no paid packages). Also opens fine on any Unity 6.x or 2022.3 LTS — the code only uses APIs
   common to all of them (uGUI, legacy Input Manager, `LegacyRuntime.ttf`).
2. Open this folder as a Unity project and let it import (first import compiles ~35 scripts).
3. Press **Play** — the startup scene is `Assets/_Project/Scenes/MainMenu.unity` (build index 0).
   Each of the three scenes contains a single bootstrap object that builds everything in code:
   `MainMenu.unity` (menu), `Hollowbrook.unity` (Scene A — the inspection-desk loop),
   `School.unity` (Scene B — side-scroller + security office). Playing *any* of them boots the
   persistent core automatically.
4. Windows build: `File ▸ Build Settings ▸ Windows, Mono ▸ Build` (all three scenes are already
   in the build list via `ProjectSettings/EditorBuildSettings.asset`).

No external art or audio files are required: all sprites and sound are **generated
procedurally at runtime**. To upgrade any of them with real assets later, drop files at
`Assets/_Project/Resources/Art/<key>.png` or `Resources/Audio/<key>.wav` — the game prefers
them automatically (keys are listed in `ProceduralArt` / `ProceduralAudio`).

## Controls

| Context | Input |
|---|---|
| Checkpoint | Mouse: click the window to be offered the ID, click the card to take it, SCAN, QUESTION then questions, ALLOW / REJECT |
| Walk / Run | `A`/`D` or `◂`/`▸`, hold `Shift` to run |
| Interact | `E` (doors, clues, light switches) |
| Desk papers | drag the ID card / record sheet to arrange your desk (drop the ID on SCAN to scan); hover a question for what it cross-checks; `1`–`4` ask while the interview is open |
| Flashlight | `F` Sweep (wide) / `Q` Focus (tight) / `L` off — or hold `LMB` / `RMB` for a temporary beam |
| Map | `M` |
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
      AnomalyEncounter, AnomalyBehaviorRules, RoomGraph, PatrolPool
    Core/                         EventBus, GameEvent, EventIds, GameRoot, SaveSystem,
                                  SettingsSystem, GameConfigSO, IScreenView
    Bootstrap/                    GameBootstrap (builds the scene), UIFactory,
                                  ProceduralArt, ProceduralAudio
    Checkpoint/CheckpointView     security-room flow, documents, questions, scan, stamps
    World/                        ViewManager, PlayerController, Interactables, LightController
    Horror/                       WanderingAnomaly (Blink/Slide/Sprint/CoverHug),
                                  SanityController, ScareSystem, HallucinationSystem
    UI/                           menus, HUD, dialogue, map, results, endings, captions
    Audio/AudioDirector           event-driven music contexts + SFX pool + captions
Tools/
  generate_meta.py                regenerates stable .meta GUIDs
  Verification/                   HEADLESS test harness (not imported by Unity)
    UnityStubs/                   faithful UnityEngine/UnityEngine.UI API stubs
    LogicTests/                   runs the pure rules:   dotnet run --project Tools/Verification/LogicTests
    EngineSmoke/                  boots & plays the game: dotnet run --project Tools/Verification/EngineSmoke
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
  anomaly carries at least one defect discoverable with documents, the scanner, the photo,
  or ≤2 questions.
- **Saves**: versioned, checksum-guarded text format at `Application.persistentDataPath`
  (corrupt files degrade to “no save” instead of throwing). Settings persist separately via
  PlayerPrefs.

## Headless verification (no Unity required)

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
dotnet run --project Tools/Verification/LogicTests    # frozen suite: 663 checks
dotnet run --project Tools/LogicTests                 # independent suite: 27,336 checks
bash Tools/Verify/verify.sh                           # compile vs real Unity assemblies + static audit
dotnet run --project Tools/FlowCheck                  # button-level flow proofs: 830 OK / 0 BAD
```

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
- Independent suite: `Tools/LogicTests` — **27,336 assertions, 0 failures** (includes the
  checkpoint-queue groups: arrival clock, FIFO promotion, pressure edges, missing student,
  snapshot/legacy resume, budget mask).
- Flow harness: `Tools/FlowCheck` — **830 OK / 0 BAD across 5 consecutive runs** (sticky-note
  gate, queue proofs Q1–Q10, settings-modal proofs S1–S8, desk loop, patrol world loop W0–W9).
- Scene-load health in the editor: `[Flow]` breadcrumbs for every step of NEW GAME / CONTINUE,
  plus a 3.5 s handshake watchdog that names and recovers from any bootstrap that fails to run.
- Not verifiable without the editor: pixel layout, audio mix, Play-mode feel, Windows build.
