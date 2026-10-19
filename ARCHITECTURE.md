# Architecture, in one page

**Services (built once by `CoreBootstrap.EnsureCore`, live across scenes):** `GameRoot` (the run:
session, shift flow, state, references to every view), `EventBus` (string-id pub/sub),
`SettingsSystem` (PlayerPrefs preferences + the one place Screen mode is written), `SaveSystem`
(checksummed text save), `AudioDirector` (all sound; captions fire from inside PlaySfx).

**Scenes:** each `.unity` holds one bootstrap component that builds everything in code.
`MainMenu.unity` -> menu + overlay sheets. `Hollowbrook.unity` -> the desk (CheckpointView) plus
HUD/dialogue/map/results/endings. `School.unity` -> rooms, player, flashlight rig, anomalies,
security office. The core survives loads via DontDestroyOnLoad; per-scene roots are destroyed normally.

**Desk vs patrol:** the desk is a state machine over one visitor at a time (present -> inspect ->
question <=2 -> stamp -> banner -> next). Patrol is a side-scroller: ViewManager owns rooms,
transitions and the combined illumination queries (BeamCovers / IsFocusLit / RoomLit); each room's
LightController owns ONLY ambient light (wall switch + flicker); FlashlightRig owns ONLY the handheld
beam (pure `BeamGeometry` cone, `FlashlightInput` arbitration, `BatteryModel`); RoomSpawner owns the
single live-threat slot, ambient spawn odds and recovery returns; WanderingAnomaly owns one encounter
driven by the `AnomalyEncounter` state machine, and roams between rooms through `RoomGraph` door
anchors under a persistent AnomalyRoot (never frozen by room deactivation).

**One visitor, event flow:** `VisitorArrived` (desk presents) -> `QuestionAsked` (answer + testimony
line) -> `ScanPerformed` -> `VerdictStamped` (InspectionRules scores, GameSession counts) ->
`VisitorResolved` (banner reveals ground truth) -> next visitor or `ShiftEnded`.

**Intro -> checkpoint (Symptom 0):** NEW GAME queues `BeginShift` then loads Hollowbrook; the new
scene's bootstrap Awake builds views and calls `RunQueuedSceneAction`; `BeginShift` shows the Loading
card, plays intro dialogue, and on the last click sets state Checkpoint and calls `BeginRun`.
If the bootstrap never runs, `CheckSceneHandshake` logs and returns to the menu after 3.5 s.

**Add a clue:** add a `ClueDef` to `ClueRegistry.Build` (order, room, text) and an interactable in
`ViewManager`'s room build that calls `Session.DiscoverClue(key)`.
**Add an anomaly behavior:** add an `AnomalyBehaviorKind` value and one branch in
`WanderingAnomaly.MoveBehavior`.
**Add a view:** create the class implementing `IScreenView`, then `CoreBootstrap.CreateView<T>` in the
scene bootstrap that should own it and a field on GameRoot.
