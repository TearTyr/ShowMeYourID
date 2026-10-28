# Architecture, in one page

**Services (built once by `CoreBootstrap.EnsureCore`, live across scenes):** `GameRoot` (the run:
session, shift flow, state, references to every view), `EventBus` (string-id pub/sub),
`SettingsSystem` (PlayerPrefs preferences + the one place Screen mode is written), `SaveSystem`
(checksummed text save), `AudioDirector` (all sound; captions fire from inside PlaySfx).

**Scenes:** each `.unity` holds one bootstrap component that builds everything in code.
`MainMenu.unity` -> menu + overlay sheets. `Hollowbrook.unity` -> the desk (CheckpointView) plus
HUD/dialogue/map/results/endings. `School.unity` -> rooms, player, flashlight rig, anomalies,
security office. The core survives loads via DontDestroyOnLoad; per-scene roots are destroyed normally.

**Desk vs patrol:** the desk is a state machine over an **arrival queue** (`GameLogic/CheckpointQueue`,
pure): a seeded clock pulls from the roster source (Normal: 25–30 s on night 1, x0.8 per night,
 double-speed while the window is busy; Endless/Challenge: 3–5 s); window free → present, busy → FIFO line
(one identical silhouette per waiter — the desk display of that line is owned by
`ShowcaseBuild.LineDisplay`, off in the current build; the queue itself always runs); verdict → banner → `CompleteActive()` promotes the oldest
waiter with no second wait; completion only when source dry + line empty + window free. `GameRoot`
owns the queue so it survives scene reloads — patrol pauses the clock (the desk view, the only
ticker, is destroyed) and snapshots (waiting line, cursor, timer, budget mask) ride in the save.
Anomaly arrivals can vanish one legitimate student (seeded roll, once per shift, line-first,
permanent). Patrol is a side-scroller: ViewManager owns rooms,
transitions and the combined illumination queries (BeamCovers / IsFocusLit / RoomLit); each room's
LightController owns ONLY ambient light (wall switch + flicker); FlashlightRig owns ONLY the handheld
beam (pure `BeamGeometry` cone, `FlashlightInput` arbitration, `BatteryModel`; an off->on switch
snaps the displayed aim and geometry and places the cone in the same call, so the beam exists on
the activation frame — only a HELD beam is damped); RoomSpawner owns the
single live-threat slot, ambient spawn odds and recovery returns; WanderingAnomaly owns one encounter
driven by the `AnomalyEncounter` state machine, and roams between rooms through `RoomGraph` door
anchors under a persistent AnomalyRoot (never frozen by room deactivation).

**One visitor, event flow:** sticky note gates the shift start (`Queue.Started` — no arrivals, no
clock, no verdict controls before the click) → `VisitorArrived` (desk presents from the queue) ->
`QuestionAsked` (answer + testimony line; the budget mask syncs into the queue for saves) ->
`ScanPerformed` -> `VerdictStamped` (InspectionRules scores, `GameRoot.OnVerdictResolved` counts,
exactly once) -> banner -> `CompleteActive` promotes the oldest waiter, or the arrival clock runs,
or `ShiftEnded` when the queue reports completion.

**Intro -> checkpoint (Symptom 0):** NEW GAME queues `BeginShift` then loads Hollowbrook; the new
scene's bootstrap Awake builds views and calls `RunQueuedSceneAction`; `BeginShift` shows the Loading
card, plays intro dialogue, and on the last click sets state Checkpoint and calls `BeginRun` — which
attaches the desk to `GameRoot.Queue`: a fresh shift waits behind the sticky-note start gate, a
resumed one restores its presentation from the persistent queue state.
If the bootstrap never runs, `CheckSceneHandshake` logs and returns to the menu after 3.5 s.

**Desk furniture that moves** (the FLOOR FILES pull drawer) keeps its numbers in a pure module
(`GameLogic/DrawerMotion`: 0.14 s ease-out, travel, row lag, shadow) and one float of state in the
view (`CheckpointView.FileDrawer`), advanced from `TickDrawer` on unscaled time — so a click is
answered on its own frame, repeated clicks reverse the same animation, and the curve is unit-tested.
**Typed window talk** follows the same split: `GameLogic/SpokenChat` (pure) normalizes a typed
line, matches it against the visitor's unasked questions and classifies refusals, while
`CheckpointView.TalkDock` only owns the widgets (input strip, transcript sheet, focus/lock
states). A matching typed line enters the SAME Interview path as the numbered question buttons —
one two-slot budget, one bubble, one transcript — so the box can never fake an answer. Input
semantics (Enter submits, Shift+Enter newline) live once, in the uGUI InputField, and the stub
reimplements them so headless proofs and the editor agree.
**The map** works the same way: `GameLogic/FloorPlan` owns the rooms in a fixed design space,
`GameLogic/MapSheetLayout.Fit` turns the visible reference area (`UIFactory.VisibleReferenceSize`,
which is what AspectMatchGuard leaves) into sheet size / plan scale / type scales / CLOSE offset,
and `UI/MapView` only applies those numbers and re-applies them on show and on resize — that is why
the sheet is centred and unclipped at any resolution instead of at one.
**Add a clue:** add a `ClueDef` to `ClueRegistry.Build` (order, room, text) and an interactable in
`ViewManager`'s room build that calls `Session.DiscoverClue(key)`.
**Add an anomaly behavior:** add an `AnomalyBehaviorKind` value and one branch in
`WanderingAnomaly.MoveBehavior`.
**Add a view:** create the class implementing `IScreenView`, then `CoreBootstrap.CreateView<T>` in the
scene bootstrap that should own it and a field on GameRoot.
