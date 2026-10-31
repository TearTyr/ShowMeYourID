# Architecture, in one page

**Services (built once by `CoreBootstrap.EnsureCore`, live across scenes):** `GameRoot` (the run:
session, shift flow, state, references to every view; split into `GameRoot.cs`, `.Shifts`,
`.Patrol`, `.Endings`, `.Saving`, `.Hunt`), `EventBus` (string-id pub/sub), `SettingsSystem`
(PlayerPrefs preferences + the one place Screen mode is written), `SaveSystem` (checksummed text
save), `AudioDirector` (all sound; captions fire from inside PlaySfx).

**Scenes:** each `.unity` holds one bootstrap component that builds everything in code.
`MainMenu.unity` -> menu + overlay sheets. `Hollowbrook.unity` -> the desk (CheckpointView) plus
HUD/dialogue/map/results/endings. `School.unity` -> rooms, player, flashlight rig, anomalies, the
administrator hunt. The core survives loads via DontDestroyOnLoad; per-scene roots are destroyed
normally. The ending screens exist only in the desk scene, so `GameRoot.TriggerEnding` loads it
first when called from the patrol.

**Desk vs patrol:** the desk is a state machine over an **arrival queue** (`GameLogic/CheckpointQueue`,
pure): a seeded clock pulls from the roster source (Normal: 25-30 s on night 1, x0.8 per night,
double-speed while the window is busy; Endless/Challenge: 3-5 s); window free -> present, busy ->
FIFO line; verdict -> short pause -> `CompleteActive()` promotes the oldest waiter with no second
wait; completion only when source dry + line empty + window free. `GameRoot` owns the queue so it
survives scene reloads - patrol pauses the clock (the desk view, the only ticker, is destroyed) and
snapshots (waiting line, cursor, timer, asked flag) ride in the save. Anomaly arrivals can vanish
one legitimate student (seeded roll, once per shift, line-first, permanent). Patrol is a
side-scroller: ViewManager owns rooms (hallway, two peek-only classrooms, the walkable comfort room), transitions and the combined illumination queries
(BeamCovers / IsFocusLit / RoomLit); each room's LightController owns ONLY ambient light (wall
switch + flicker); FlashlightRig owns ONLY the handheld beam (pure `BeamGeometry` cone,
`FlashlightInput` arbitration, `BatteryModel`); RoomSpawner owns the single live-threat slot,
ambient spawn odds and recovery returns; WanderingAnomaly owns one encounter driven by the
`AnomalyEncounter` state machine, and roams between rooms through `RoomGraph` door anchors.

**One visitor, event flow:** sticky note gates the shift start (`Queue.Started`) -> `VisitorArrived`
(desk presents from the queue; ID and slip are already on the desk) -> `QuestionAsked` (optional,
up to 2 of 4 questions per student; the answer goes to the bubble and stays on its button) -> stamp dropped on the slip
(a later stamp replaces it) -> ID dragged back to the window -> `VerdictStamped` (InspectionRules
scores, `GameRoot.OnVerdictResolved` counts, exactly once) -> short pause -> `CompleteActive`
promotes the oldest waiter, or the arrival clock runs, or `ShiftEnded` when the queue reports
completion.

**What makes a visitor wrong (`RosterFactory`):** the student FILE (the `Record*` fields and the
canon roster behind the cabinet) is always the truth. A defect changes the CARD, the slip, the face
or the spoken classroom answer so it disagrees with the file. Legitimate students always match
exactly; every anomaly has at least one tell the tests prove. Faces: the ID always shows the real
student's art; the window shows another student's face only on a `PhotoMismatch`, and the
`_anomaly` art only on a `PhotoSubtleMismatch` (`GameLogic/FaceAssets`, `Bootstrap/FaceArt`).

**Intro -> checkpoint:** NEW GAME queues `BeginShift` then loads Hollowbrook; the new scene's
bootstrap Awake builds views and calls `RunQueuedSceneAction`; `BeginShift` shows the Loading card,
plays intro dialogue, and on the last click sets state Checkpoint and calls `BeginRun`, which
attaches the desk to `GameRoot.Queue`. If the bootstrap never runs, `CheckSceneHandshake` logs and
returns to the menu after 3.5 s.

**Night 5 - the administrator:** at the window, `GameLogic/AdminTalk` (pure) is a short topic list
shown by `CheckpointView.AdminTalk`; it can never stamp or award clues. **REFUSE ENTRY** goes
through the same `FinalizeIfReady` path as a physical DENY. A DENY on the administrator calls
`GameRoot.BeginAdminHunt`: `Session.Hunt` becomes Active, the patrol scene loads and
`ViewManager.StartAdminHunt` creates `World/AdminHuntDirector`. The RULES are the pure
`GameLogic/AdminHunt` state machine (Stalking -> AtDoor, Closet or Behind -> Retreat;
every phase has a time limit and the dawn clock always runs down, so a hunt always ends in
`Won` or `Lost`; `AdminHuntTests` proves it with idle, perfect, dodging, spamming and random
players). The director reads the world (peek open, cursor on his head, beam on his face), feeds
the rules, and turns events into sound, captions, the shaking picture and `PeekLight.Flicker`.
Win -> `FinishShift` (normal Win/Secret ending); loss -> the `server_door` ending. The state is
saved, so Continue resumes the hunt.

**Desk furniture that moves** (the FLOOR FILES pull drawer) keeps its numbers in a pure module
(`GameLogic/DrawerMotion`) and one float of state in the view (`CheckpointView.FileDrawer`),
advanced from `TickDrawer` on unscaled time, so a click is answered on its own frame and repeated
clicks reverse the same animation. Files in a drawer are listed A-Z (`RecordSearch`).

**The map** works the same way: `GameLogic/FloorPlan` owns the rooms in a fixed design space,
`GameLogic/MapSheetLayout.Fit` turns the visible reference area into sheet size / plan scale / type
scales, and `UI/MapView` only applies those numbers.

**Add a clue:** add a `ClueDef` to `ClueRegistry.Build` (order, room, text) and an interactable in
`RoomBuilder` that calls `Session.DiscoverClue(key)`.
**Add an anomaly behavior:** add an `AnomalyBehaviorKind` value and one branch in
`WanderingAnomaly.MoveBehavior`.
**Add a view:** create the class implementing `IScreenView`, then `CoreBootstrap.CreateView<T>` in
the scene bootstrap that should own it and a field on GameRoot.
