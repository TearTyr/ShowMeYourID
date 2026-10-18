# Show Me Your ID! — Twin Studio

**2D Psychological Horror · Deduction · Exploration** — PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation — yet still open at night so its students can finish
the term. Check IDs. Compare records. Ask up to **2 questions**. Stamp **ALLOW** or **REJECT**.
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
| Checkpoint | Mouse: documents, checklist rows, SCAN, questions, ALLOW / REJECT |
| Walk / Run | `A`/`D` or `◂`/`▸`, hold `Shift` to run |
| Interact | `E` (doors, clues, light switches) |
| Flashlight (Focus light) | `F` |
| Map | `M` |
| Pause | `Esc` |
| Dialogue | Click / `Space` / `Enter` / `E` |
| Erase save data | `F2` ×3 on the main menu (each press ≤1.5 s apart, then confirm) |
| Security office (Scene B) | enter it from the office door inside Classroom 1 while patrolling |

## Game structure

- **Main menu** is focused for the current milestone: **NEW GAME** starts the five-night story
  directly; **CHALLENGE MODE** and **EXTRAS** sit in the menu as *locked placeholders* for
  later. The Endless/Challenge systems are fully implemented and headless-tested behind
  `GameRoot.RunModesUnlocked = false` — flip that one flag (or wire an Extras screen) to
  re-expose them without touching any other code.
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
      QuestionSystem, InspectionRules, ClueRegistry, ShiftPlan, SanityModel,
      EndingRules, GameSession, SaveFormat
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
dotnet run --project Tools/Verification/LogicTests    # 185 rules/progression/ending/save tests
dotnet run --project Tools/Verification/EngineSmoke   # 123 engine tests: boots the real
                                                      # bootstrap and plays full runs
```

`docs/HANDOFF.md` contains the full test matrix (PASS / FAIL / NOT RUN / BLOCKED) and the
documented conflict resolutions (question limit, administrator-breach ending priority,
clue sequence).
