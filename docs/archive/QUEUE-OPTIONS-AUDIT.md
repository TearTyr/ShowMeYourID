# Phase 9–11 baseline audit — checkpoint flow, settings, verification surface

Branch point: `f9bdffd` (feat/flashlight-anomaly-rework, clean tree).
Everything below was read out of the actual sources on that commit — no assumptions.

## 1. How a shift runs today (the code path being replaced)

1. `GameRoot.StartNewGame(Normal)` → seed `20261102` → `QueueSceneAction(BeginShift(1))` → load `Hollowbrook`.
2. `CheckpointSceneBootstrap.Build` creates every desk view, then `RunQueuedSceneAction` →
   `GameRoot.BeginShift(n, resumeFrom)`:
   - replays earlier shifts on a fresh seeded `RosterFactory` so the RNG lands identically,
   - `CurrentRoster = Roster.GenerateShift(CurrentShiftDef)` (4/6/7/8/8 visitors, +admin on night 5),
   - seeds `Session.Pool` with tonight's patrol anomalies,
   - `PlayIntroThenCheckpoint(intro, def, resumeFrom)` → intro dialogue → `Checkpoint.BeginRun(roster, def, resumeFrom)`.
3. `CheckpointView` owns the progression: `List<VisitorData> _roster`, a single `int _index`,
   `VisitorData _current`. `NextVisitor()` increments `_index`, presents `_roster[_index]`;
   past the end it sets `_rosterComplete` (END SHIFT lights up).
4. `Stamp(verdict)` locks the verdict, calls `GameRoot.OnVerdictResolved` (all counters, once),
   writes `_gameRoot.CurrentVisitorIndex = _index + 1` "to survive scene reloads", shows the
   banner, then `AfterBanner` waits `BannerSeconds`, plus **`BetweenSeconds + 2.2f`** in Normal
   mode ("Next one's crossing the yard. Patrol now if you need to.") before `NextVisitor()`.
5. Patrol: `GoPatrol()` refuses while an inspection is live (`!_verdictLocked`), else
   `EnterExploration()` → School scene → desk view **destroyed**. `ReturnToCheckpoint()` →
   `Checkpoint.ResumeRun()` re-reads `GameRoot.CurrentRoster` + `CurrentVisitorIndex` and calls
   `NextVisitor()` — the cursor survives, nothing else does.
6. Save/continue: `PersistRun()` stores `VisitorIndexAtSave` (a plain int). `ContinueGame()` →
   `BeginShift(shift, resumeFrom)` regenerates the identical roster (seeded) and `BeginRun`
   resumes with `_index = resumeFrom - 1`.
7. Endless/Challenge: `NextVisitor()` pulls `_gameRoot.NextRunVisitor()` per visitor (infinite
   generator on `RosterFactory`); pacing is the same `AfterBanner` coroutine; the run ends on
   `Session.RunTimer` in `GameRoot.Update` or on score/ending rules. (Menu-locked via
   `GameRoot.RunModesUnlocked = false`, but fully wired and headless-tested.)

## 2. Findings that shape Phase 9

| # | Finding | Consequence for the rework |
|---|---------|---------------------------|
| A1 | **One `_index` field conflates five concepts** (roster cursor, arrival count, processed count, "current", completion). `CurrentVisitorIndex` doubles as the save cursor. | Replace with an explicit queue controller separating roster source / active / waiting line / arrival count / processed count / effective total / completion (§9.2). |
| A2 | **`AfterBanner` hard-codes a second waiting period** (`BetweenSeconds + 2.2f`) even in run modes. | Must go: promotion from the line is immediate after the banner (§9.4); the arrival clock is the only inter-visitor pacing. `betweenVisitorsSeconds` / `exhibitionBetweenVisitors` become dead config — removed with their only reader. |
| A3 | **Nothing survives patrol except the cursor int.** `_tonightsLog`, speech, banner state are view-local; the roster list itself lives in `GameRoot.CurrentRoster`. | Waiting line / cursor / arrival timer / started flag / vanished students / question budget must live in persistent run state owned by `GameRoot` (§9.3). Chosen rule: **the arrival clock runs only while the desk is the active screen; patrol explicitly pauses the line** (the desk view — the only ticker — is destroyed in the School scene; state rides in `GameRoot.Queue`). |
| A4 | `ResumeAfterExploration()`/`DelayedNext()` are **dead code** (no callers — `ReturnToCheckpoint` calls `ResumeRun`). | Deleted in the rewrite, not carried over. |
| A5 | **No queue pressure or overwhelmed concept exists.** Sanity hits come only from verdict mistakes (`RecordVerdict`); `EndingRules` lose routes are admin breach / 3 breaches / 3 false rejections. Sanity 0 has no fail state (only stage effects). | Overwhelmed cannot ride an existing lose route *accurately* → new one-shot session flag `QueueOverwhelmed` + additive `EndingRules` lose reason `"overwhelmed"` (evaluated after the existing three, so the frozen 663-check suite is untouched). Pressure warning at 5 is a crossed-threshold edge, not per-frame. |
| A6 | **No missing-student concept.** `RosterFactory.GenerateShift` pre-records every generated visitor in its generation log (duplicate-ID defect source); the play-side `Session.TonightLog` records only at verdict time (`OnVerdictResolved`). | Vanishing a student at runtime must not touch either log: a vanished student is never "presented" (scan/duplicate semantics stay honest) and never "inspected". Victim eligibility = legit, not active, not processed, not admin, not already vanished; prefer the waiting line, else the un-arrived source (§9.6). |
| A7 | `VisitorIndex` is unique per shift roster (0..count-1, admin = count); run-mode visitors carry `-1`. | Snapshot serialization of the line uses `VisitorIndex`; missing-student is a Normal-mode (finite roster) mechanic — run modes get the queue but never vanish anyone. |
| A8 | Admin encounter: admin is appended last in the roster, presented through the same `NextVisitor` path; `AfterBanner` special-cases `IsAdministrator && Session.AdminRejected` (outro, then `_rosterComplete = true`). State `AdminEncounter` exists in the enum and visibility tables but is never entered — the encounter runs in `Checkpoint` state. | The admin simply becomes the last arrival of the clock; the outro branch is preserved, completion then falls out of the queue invariants (source exhausted + line empty + window free). |
| A9 | Pause: `TogglePause` sets `Time.timeScale = 0` and both scene `Update`s keep running. `SettingsView.Update` handles Escape → `CloseSettings`; **`CheckpointView.Update` and `PlayerController.Tick` also handle Escape in the same frame** → Esc while Options is open over the pause menu closes Options *and* unpauses. | Real bug against §10.4. Fix: `GameRoot.BlockingOverlayOpen` (Settings/Controls/Credits visible) gates the Esc-unpause paths. The arrival clock ticks on **scaled** `Time.deltaTime` from the desk view's `Update`, so `timeScale = 0` (pause, and pause→Options) freezes it with no unscaled-time leak. |
| A10 | Settings sheet: Master/Music/SFX sliders + fullscreen + 4 resolution buttons + revert-countdown row + text-scale + 3 accessibility toggles + data reset + CONTROLS + BACK, in a scrolling `LeftSheet` (dimmer already blocks clicks behind: `UIFactory.Panel` sets `raycastTarget = true`). `OpenSettings/CloseSettings` already hide/re-show the pause sheet correctly and never touch `timeScale`. | §10.3 = strip the sheet down to the three audio rows + BACK (keep `LeftSheet`, Esc, Tab order, `SettingsSystem.Apply` immediate-write path). `SettingsSystem`/`SettingsData`/`DisplayPolicy`/F2×3 reset stay — other systems use them (`ApplyStoredDisplay` at boot, `AdoptFromSaveIfFresh`, `RequestDataReset` from the menu). The non-frozen LogicTests "Options sheet row budget" group must be updated to the new row set. |
| A11 | `AudioDirector.ApplyVolumes` (on `SettingsChanged`) only refreshes **playing** music sources; `Crossfade` captures `musicVol` once at fade start, so a volume change mid-crossfade gets overwritten by the stale value at fade end. SFX volume is applied per-`Play()` (correct). | Repair inside the existing path: read `_settings.MusicVolume` live per crossfade frame; keep `ApplyVolumes` for the static case. No second audio manager (§10.5). |
| A12 | Save format: flat `key=value` lines + checksum, `Version = 1`; frozen suite asserts round-trip by field (not by exact text) and that `SMYIDSAVE 1` parses. Unknown keys are ignored by `Deserialize`; missing keys fall back to defaults. | Queue metadata is added as new additive keys (`queueStarted`, `queueCursor`, `queueArrived`, `queueProcessed`, `queueTimer`, `queueWaiting`, `queueActive`, `queueVanished`, `queueMissing`, `queueBudget`, `queueOverwhelmed`) — old saves still load; a new save with no queue keys present (pre-queue save) falls back to the legacy `visitorAtSave` resume. |
| A13 | Desk placement discipline: every element goes through `CheckpointLayout` regions + `Reg()` → `DeskLayoutAudit`; FlowCheck pins the table to the wireframe digit-for-digit and proves registered rects match, co-visible regions disjoint, all on-screen. Top-bar slots are tagged `B` (both scenes); `Findings`(894,58,370×16) is B; `RoomInfo`(894,76,370×16) is exploration-only (`E`). | New regions (all additions to the table + FlowCheck rows): `QueueStatus` (894,76,370×16, `C` — the desk-side twin of the exploration `RoomInfo` slot, never co-visible with it), `QueueStripL` (340,436,60×240, `C` — the free gap between the left column and the speech panel), `QueueStripR` (884,432,80×240, `C` — the free gap between the window sill and the right column, clear of END SHIFT which ends at x=880), `StickyNote` (390,200,500×320, `T` — transient overlay, bounds-checked like the paper overlays). |
| A14 | Exhibition mode is the shipping default (`exhibitionMode = true`, one 4-visitor night with 1 anomaly, `EvaluateExhibition` closes the booth cut after clock-out). FlowCheck's DeskLoop walks the whole roster through button clicks and expects END SHIFT → Ending. | The queue must not break the exhibition arc: 4 arrivals at 3–5 s, one anomaly (the missing-student roll has something to bite on), banner 2.2 s. FlowCheck keeps walking the roster, now through note-click + arrival ticks. |
| A15 | `GameRoot.Update` runs the Endless/Challenge `RunTimer` whenever state is Checkpoint and no dialogue is active. | With the sticky-note gate the run timer would drain before the run starts → gate it on `Queue.Started` too. |
| A16 | Verification surface (as of this commit, re-run for real): `bash Tools/Verify/verify.sh` → compile clean vs both Unity assembly sets + audit clean; `Tools/LogicTests` → 27,225/27,225; frozen `Tools/Verification/LogicTests` → 663/663; `Tools/FlowCheck` → 647 OK / 0 BAD. `Tools/Verification/CompileCheck` is a csproj (compiles the same sources); `Tools/Verification/EngineSmoke` is frozen-broken by design (references the deleted `SceneBootstrapRegistry.SimulateLoad`) — left untouched per standing instruction, reported NOT FIXED. | Phase 11 reports these numbers as actually re-run, plus the new queue/sticky/settings proofs; Unity-editor checks stay NOT RUN with repro steps. |

## 3. Decisions taken from the audit

- **One controller, pure logic**: `GameLogic/CheckpointQueue.cs` (engine-free like the rest of
  GameLogic) owns roster source, waiting line, active window, arrival timer, counts, pressure
  edges, missing-student incidents and snapshotting. `GameRoot` owns the instance (`GameRoot.Queue`)
  so it survives scene reloads; `CheckpointView` becomes its presenter and the only ticker
  (scaled `Time.deltaTime`, desk-screen-only). `Tick`/`ForceNextArrival` return a small result
  struct — no engine callbacks inside the pure layer; LogicTests drive it directly, FlowCheck
  drives the real view.
- **Arrival rule**: first visitor on sticky-note click (`MarkStarted` + `ForceNextArrival`),
  afterwards a seeded random 3–5 s clock (config `queueArrivalMinSeconds/MaxSeconds`); at most one
  arrival per tick, overshoot carries. Window free → present; occupied → FIFO enqueue.
- **Patrol rule (chosen, §9.3)**: clock paused off-desk, queue state preserved; returning rebuilds
  silhouettes/counters from `GameRoot.Queue`; the mid-inspection patrol guard stays, so the window
  is always free when the desk is left.
- **Verdict rule**: after the existing banner, `CompleteActive()` promotes the oldest waiting
  visitor immediately (no second wait); empty line → hide visitor and let the clock run;
  `ShiftComplete` only when started + source exhausted + line empty + window free.
- **Pressure**: warning once when waiting crosses 5 (config); overwhelmed once at 7 (config) →
  collapse the queue (no further arrivals), disable verdict controls, `Session.QueueOverwhelmed`
  → additive `EndingRules` lose `"overwhelmed"` → existing `TriggerEnding` route (no duplicate
  endings, no repeated penalties, not misrepresented as false rejection/breach).
- **Missing student**: rolled with the run-seeded queue RNG when an eligible (non-admin) anomaly
  first enters the line; `missingStudentChance` / `missingStudentLimit = 1` per shift in config;
  victim = random legit from the waiting line, else random legit not yet arrived from the source
  (marked vanished, cursor skips it); feedback = one ambiguous RADIO line + roster-discrepancy
  notification; never reveals which visitor is the anomaly; permanent (allowing the anomaly
  restores nobody).
- **Sticky note**: desk overlay (dimmer blocks the whole desk), exact spec text, genuine start
  gate (`Queue.Started` in persistent run state) — shown only when the shift has not started;
  scene reloads with a started queue never re-show it, never double-arm the clock, never re-spawn
  the first visitor.
- **Options modal**: existing `SettingsView` stripped to MASTER/MUSIC/SFX + BACK on the existing
  `LeftSheet` + `OpenSettings/CloseSettings` paths; audio path repaired inside `AudioDirector`
  (live crossfade volume); Esc-unpause collision fixed via `GameRoot.BlockingOverlayOpen`.
