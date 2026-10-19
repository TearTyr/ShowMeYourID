# Architecture, one page

Services are built once in `CoreBootstrap.EnsureCore()` and survive scene loads (DontDestroyOnLoad):
`GameRoot` owns the run (session counters, current shift, state enum, references to every view, the
queued scene action and the handshake watchdog); `EventBus` is a string-keyed pub/sub list for things
that genuinely do not know each other; `SettingsSystem` owns PlayerPrefs preferences and is the only
code that writes Screen mode; `SaveSystem` reads/writes the checksummed text save; `AudioDirector`
plays every sound and emits captions from inside PlaySfx.

Each `.unity` scene holds one bootstrap component that builds the scene in code. `MenuSceneBootstrap`
creates the menu, mode select, settings/controls/credits sheets, confirm popup and captions.
`CheckpointSceneBootstrap` creates the desk (CheckpointView), HUD, dialogue, map, results, endings,
loading, pause, sanity/scare FX. `SchoolSceneBootstrap` creates ViewManager (rooms, player,
flashlight rig, anomaly spawner), hallucinations, sanity/scare FX, HUD, dialogue, pause, captions,
confirm, security office and map. The checkpoint scene is a fixed desk with no world; the school
scene is the side-scrolling world with no desk.

One visitor, event flow: `VisitorArrived` (desk presents papers) -> `DocumentsShown` ->
`QuestionAsked` / `ScanPerformed` (evidence gathered, testimony line appended) -> `VerdictStamped`
(InspectionRules scores the decision, GameSession counts complaints/breaches) -> `OnVerdictResolved`
(banner reveals ground truth) -> `NextVisitor` or `ShiftEnded`.

Intro to checkpoint, the Symptom 0 path: NEW GAME queues `BeginShift(1)` on GameRoot and asks
SceneRouter for Hollowbrook. The new scene's bootstrap Awake builds the views and calls
`RunQueuedSceneAction`, which runs BeginShift: it shows the Loading card, sets ShiftIntro and plays
the intro dialogue. The last dialogue click invokes the callback that hides the card, sets Checkpoint
and calls `CheckpointView.BeginRun`. If the bootstrap never runs, `CheckSceneHandshake` logs the
likely cause after 3.5 s and returns to the menu instead of leaving a dead screen.

Add a clue: add a ClueDef to `ClueRegistry.Build` (order, room, text) and an `AddClue` call for that
room in `RoomBuilder.BuildRoom`; discovery flows through `GameSession.DiscoverClue`.
Add an anomaly behavior: add an `AnomalyBehaviorKind` value and one branch in
`WanderingAnomaly.MoveBehavior`; spawn cadence lives in `RoomSpawner`.
Add a view: write a class implementing `IScreenView`, create it with `CoreBootstrap.CreateView<T>` in
the bootstrap that should own it, and store it in a GameRoot field.
