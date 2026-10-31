# Phase 0 — Project Audit: Show Me Your ID! (Twin Studio)

Date: 2026-10-18 · Environment: headless Linux sandbox (no Unity Editor available)

## 1. Repository state found

| Expected source of truth | Found? | Notes |
|---|---|---|
| `SHOW ME YOUR ID-GAME DEVELOPMENT.pdf` | ⚠ as text | Supplied as `uploads/showmeyourid.txt` (2,766 lines) — the full Executive Game Production / concept document text. Treated as the authoritative approved concept. |
| `ShowMeYourID_MasterPrompt.md` | ❌ | Not present in the workspace. Its technical requirements are quoted in the task brief (§5–§6) and are followed from there. |
| Unity project / RollCall source / assets | ❌ | Workspace contained **no** Unity project, scenes, prefabs, scripts, or assets. There is nothing to preserve or reuse. |

**Consequence:** the "existing project" cannot be audited because it does not exist in this
workspace. Per the brief's fallback, the project is created from scratch at
`/home/user/ShowMeYourID/` following the master-prompt technical spec (Unity 2D, Built-in RP,
`Assets/_Project/Scenes/Hollowbrook.unity`, code-driven bootstrap, EventBus/GameEvent/EventIds,
ScriptableObject data with procedural fallbacks, save/load, Windows-buildable layout).

## 2. Environment verification capability

- No Unity Editor / Unity Hub → **cannot** open the project, run Play mode, or produce a Windows
  build here. Those items will be reported **BLOCKED** with exact repro steps for the team.
- .NET SDK 8.0.426 installed at `/opt/dotnet` (invariant globalization) → all game C# is compiled
  against hand-written Unity API stubs (`Tools/Verification/UnityStubs`) as a genuine compile
  check, and pure-C# gameplay logic is **executed** by a headless test runner
  (`Tools/Verification/LogicTests`) producing real PASS/FAIL results for rules, progression,
  endings, and save/load.

## 3. Approved requirements extracted from the concept document (PDF text)

- Genre: 2D psychological horror, deduction, exploration. PC/Steam, mouse + keyboard. Monochrome
  high-contrast visual style; visual distortion for anomalies; red sparingly.
- Core loop: Security Room (fixed point-and-click checkpoint) → Hallway/Classrooms (2D
  side-scroll movement + point-and-click inspection) → decisions & clues affect endings.
- Checkpoint: Student ID + Student Record/Form; verify ID-number format, photograph vs. person,
  expiration, year level, course/program, regular/transferee status; questioning; clipboard
  checklist; paper-note rules; map; permanent Allow/Reject stamps; scan/database UI.
- Questioning: 4 question types (course, year, status, first classroom). **Count: see conflict C1.**
- Normal mode = 5 shifts: S1 4 students, S2 6, S3 7, S4 8 (investigation focus), S5 8 + district
  administrator revealed as anomalous impostor.
- Endless mode: continuous inspections, time limit, +time per correctly identified anomaly,
  rising anomaly rate, score. Challenge mode: faster sanity loss, unpredictable rule changes,
  inspection pressure, score multipliers.
- Consequences: False Rejection (legit rejected) and Anomaly Entry (anomaly allowed) tracked;
  sanity decreases on encounters/mistakes/disturbing events.
- Endings: **Win "Surviving Hollowbrook"** (all 5 shifts, <3 false rejections, <3 breaches,
  reject the administrator). **Lose "Another Custodian"** (3 breaches OR 3 false rejections).
  **Secret "The Truth Behind Hollowbrook"** (all ten environmental interactions, in the intended
  sequence, having investigated Hallway + Classroom 1 + Classroom 2).
- Exploration: 10 inspectable interaction types — Flickering Light, Chalkboard, Desk, Door,
  Window, Notice Board, lost Student ID, Photo, Cabinet, Clock — across Hallway + 2 classrooms.
  Wandering anomalies may appear anytime in hallway/classrooms.
- UI: main menu (New Game, Continue, Settings, Credits, Exit), mode select, pause + controls,
  loading screen, HUD (sanity bar, time, objective, interaction/notification indicators),
  checkpoint UI (ID display, photo, record, Scan, Accept, Reject, verification result, student
  database, anomaly record, Access Granted/Denied/System Error), dialogue UI, sanity effects,
  ending UI (title, description, decision summary, final result).
- Audio: checkpoint SFX (paper, stamps, UI, map, intercom static), exploration SFX (footsteps,
  doors, flicker), horror SFX (distortion, static, alarms), 9 music contexts (menu, checkpoint
  radio noise, investigation, hallway, dark room, normal classroom, anomaly encounter, final,
  endings). Documented musical direction: key of **E♭ minor over B♭**, progressions
  `(Ebm/Bb)(Db/Ab)(Cb/G)(Db/Ab)` etc.
- Cast: 14 unique students (8 core + 6 variations) reusable across shifts; anomaly variants built
  from students (face/eye/expression/movement distortion); custodian; district administrator.

## 4. Conflicts identified & resolutions

| # | Conflict | Resolution (documented, not silent) |
|---|---|---|
| C1 | Questions per inspection: tool list (doc line ~285) says "1–3", while §2.3, inspection step 5, and both testing checklists say "1–2". | **Max 2 questions per inspection** — the specific gameplay section and checklists (3 places) override the single summary bullet. Configurable in `GameConfig` if the team disagrees. |
| C2 | Master-prompt anomaly behaviors (Blink/Slide/Sprint/CoverHug) & light modes (Off/Sweep/Focus) are not named in the PDF; PDF only says "wandering anomalies… may appear anytime" and the teaser shows "when the light goes out, the student moves closer". | Compatible, not contradictory: the four behaviors are the technical implementation of "wandering anomalies"; Blink matches the teaser's light-linked movement. Implemented as specified in the master prompt. |
| C3 | Lose condition vs. Shift-5 administrator: win requires rejecting the administrator, but the strict lose trigger is "3 breaches". Allowing the administrator with <3 total breaches would leave the ending undefined. | Allowing the administrator = immediate Lose ("Another Custodian") — narratively mandated: the anomalies reach the identification system and the player "loses control of the situation". Documented in `EndingRules`. |
| C4 | Secret ending vs. Lose ending overlap (all clues found but 3 breaches/false rejects). | Lose overrides Secret (lose text: player leaves "without discovering the truth"). Secret overrides Win when both qualify. Priority: Lose > Secret > Win. |
| C5 | "Intended sequence" for the 10 clues is not enumerated in the doc. | The document's own listing order in §2.6 is used as the intended sequence, and an in-game chain (each clue's text points to the next) makes the order discoverable and fair. Out-of-order finds still count as discovered but break the sequence (tracked separately). |
| C6 | Target age "13+" (project info) vs "15+" (concept paper). | Non-gameplay; noted for marketing/ESRB paperwork. Game targets 13+ presentation (no gore), photosensitivity safeguards included. |
| C7 | Master prompt asks for "1–3 questions", "ten clues", "three endings", "five shifts" — all match the PDF except C1. Visitor counts confirmed against PDF: 4/6/7/8/8+admin. | — |

## 5. Prioritized work plan (defects = "missing" since project is empty)

1. **P1 Core:** project skeleton, bootstrap, EventBus/GameEvent/EventIds, config, save/load,
   settings, procedural art/audio factories. (Blocks everything.)
2. **P2 Checkpoint:** visitor model + seeded generation, ID/record verification rules, question
   system (≤2), checklist, scan/database, Allow/Reject stamps, feedback banners, mistake tracking.
3. **P3 Exploration:** side-scroll movement (walk/run), Security/Hallway/Classroom1/Classroom2
   views, transitions + recovery, doors, 10 interactables/clue chain, map screen.
4. **P4 Horror:** sanity model + stages + recovery, hallucinations, scare system (with
   photosensitivity guard), wandering anomalies (Blink/Slide/Sprint/CoverHug), lights
   (Off/Sweep/Focus).
5. **P5 Progression:** shift manager (5 shifts, rosters), shift results, administrator encounter,
   ending evaluator (Win/Lose/Secret), Endless mode, Challenge mode + rule mutations.
6. **P6 Systems:** menus (main/mode/pause/controls/settings/credits), HUD, dialogue, loading
   screen, Continue-from-save, ending screens & records.
7. **P7 Integration:** procedural monochrome art pass, synthesized audio (E♭m menu theme, radio
   noise checkpoint, stingers), captions, accessibility settings wired to real behavior, balance.
8. **P8 Verification:** stub-compile every script, run logic test suite, hand off with
   PASS/FAIL/NOT RUN/BLOCKED matrix (Unity Play-mode & Windows build = BLOCKED here, with steps).
