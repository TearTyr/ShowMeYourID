# Architecture, in one page

**Services (built once by `GameCore.EnsureCore`, live across scenes):** `GameRoot` (the run:
session, shift flow, state, references to every view; split into `GameRoot.cs`, `.Shifts`,
`.Patrol`, `.Endings`, `.Saving`, `.Hunt`), `EventBus` (string-id pub/sub), `SettingsSystem`
(PlayerPrefs preferences + the one place Screen mode is written), `SaveSystem` (checksummed text
save), `AudioDirector` (all sound; captions fire from inside PlaySfx).

**Scenes:** each `.unity` holds its screens as real objects under a `SceneRoot_<scene>` object, plus one
start script (`MenuScene`, `DeskScene`, `PatrolScene`). The screen code is "find or make": when a
scene starts, `UIFactory.NewUI`, `GameCore.NewCanvas` and `GameCore.NewObject` first look for the object
in the scene (`SceneBuild.Reuse`) and only make it when it is missing, and `GetOrAdd<T>` does the same
for components. So an unbaked scene still works. `SceneEdits` (on the scene root) stores a baseline of
what the code made when the tool filled the scene; at load it compares the scene with the baseline,
lets the code run, then puts the designer's differences back (rect, colour, picture, text, font size, an
object hidden inside a screen). `Assets/_Project/Editor/ProjectSetup.cs` (editor only, the Hollowbrook
menu) fills the scenes by running the same code in the editor, saves code painted pictures as png files
in `SceneArt`, repairs Build Settings, art import settings and `GameConfig.asset`, and has the Check and
Rebuild From Scratch commands. Button clicks are wired in code, not in the Inspector. The patrol world is
not baked. Every `MonoBehaviour` that sits in a scene must live in a file named like the class.
`MenuScene.unity` -> menu + overlay sheets. `DeskScene.unity` -> the desk (CheckpointView) plus
HUD/dialogue/map/results/endings. `PatrolScene.unity` -> rooms, player, flashlight rig, anomalies, the
administrator hunt. The core survives loads via DontDestroyOnLoad; per-scene roots are destroyed
normally. The ending screens exist only in the desk scene, so `GameRoot.TriggerEnding` loads it
first when called from the patrol.

**Desk vs patrol:** the desk is a state machine over an **arrival queue** (`GameLogic/CheckpointQueue`,
pure): a seeded clock pulls from the roster source (Normal: 25-30 s on night 1, x0.8 per night,
double-speed while the window is busy); window free -> present, busy ->
FIFO line; verdict -> short pause -> `CompleteActive()` promotes the oldest waiter with no second
wait; completion only when source dry + line empty + window free. `GameRoot` owns the queue so it
survives scene reloads - patrol pauses the clock (the desk view, the only ticker, is destroyed) and
snapshots (waiting line, cursor, timer, asked flag) ride in the save. Anomaly arrivals can vanish
one legitimate student (seeded roll, once per shift, line-first, permanent). Patrol is a
side-scroller: ViewManager owns rooms (hallway, two peek-only classrooms, the walkable comfort room), transitions and the combined illumination queries
(BeamCovers / IsFocusLit / RoomLit); each room's LightController owns ONLY ambient light (wall
switch + flicker); FlashlightRig owns ONLY the handheld beam (pure `BeamGeometry` cone; `BeamCover` paints the room dark
as one picture with the beam cleared out of it, while the room's own dark layers step aside;
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
student's art; every anomaly wears its `_anomaly` art at the window and a `PhotoMismatch` shows another
student's face (`GameLogic/FaceAssets`, `Generated/FaceArt`).

**Scared visitors:** each night row has a `ScaredCount`. `RosterFactory.PickScared` picks them before the
defects, alternating real students and anomalies, never the administrator. A scared visitor wears the
student's `_scared` art whether they are real or an anomaly (`FaceAssets.WindowKey` with the scared flag),
says that student's `ScaredGreeting` and answers in the shared `Content.ScaredSpeech`, and a scared anomaly
cannot get awkward speech or a subtle photo change. So only the papers and the file can catch one.

**Intro -> checkpoint:** NEW GAME queues `BeginShift` then loads DeskScene; the new scene's
scene script's Awake sets the views up and calls `RunQueuedSceneAction`; `BeginShift` shows the Loading card,
plays intro dialogue, and on the last click sets state Checkpoint and calls `BeginRun`, which
attaches the desk to `GameRoot.Queue`. If the scene script never runs, `CheckSceneHandshake` logs and
returns to the menu after 3.5 s.

**Night 1 tutorial (PARKED: `TutorialSteps.Enabled = false`, so `GameRoot.TutorialStep` always reads `Done`; set it to true to bring it back):** `GameLogic/TutorialSteps` (pure) holds the order (it opens with the `Controls` step, a keybind card that waits for its CONTINUE button), the texts and what each step holds still;
`GameSession.TutorialStep` is saved (`Done` = none, which is also every old save and every other mode). `GameRoot.Tutorial.cs` feeds it
facts once a frame (`TickTutorial`), pauses the clock and sanity in `Update` while a step is active, holds the line
(`CheckpointQueue.HoldLine`), opens the patrol early (`CheckpointView.PatrolOpen`), guards the peek doors (`Doorway.Interact`) and
makes `RoomSpawner` spawn one Slide in the hallway and nothing else; `WanderingAnomaly.TickCatch` cannot catch while it runs. The box is
`UI/TutorialGuide`, built inside the HUD frame. `RosterFactory` makes visitor 1 a calm real student and visitor 2 an expired ID anomaly when
`ShiftDef.TutorialOpening` is set (night 1), and `PatrolCycle.StandingIn` fixes night 1 to Room 202 only.

**Night 5 - the administrator:** eight students first (the normal desk), then he comes last. Sanity drains all the time only once he is at the window (`GameRoot.AdministratorDrainOn`). At the window, `GameLogic/AdminTalk` (pure) is a short topic list
shown by `CheckpointView.AdminTalk`; it can never stamp or award clues, and he can be questioned without limit (a lie counts once).
Only the DENY stamp turns him away, and his words go through the dialogue box, never the bubble. A DENY on the administrator calls
`GameRoot.BeginAdminHunt`: `Session.Hunt` becomes Active, the patrol scene loads and
`ViewManager.StartAdminHunt` creates `World/AdminHuntDirector`. The RULES are the pure
`GameLogic/AdminHunt` state machine (Stalking -> InRoom, Behind or Loose -> Retreat;
every phase has a time limit and the dawn clock always runs down, so a hunt always ends in
`Won` or `Lost`; `AdminHuntTests` proves it with idle, perfect, dodging, spamming and random
players). The director reads the world (peek open, cursor on his head, beam on his face), feeds
the rules, and turns events into sound, captions, the shaking picture and `PeekLight.Flicker`.
Win -> `FinishShift` (normal Win/Secret ending); loss -> the `server_door` ending. The state is
saved, so Continue resumes the hunt.

**Lockdown:** `GameMode.Lockdown`, `ShiftPlan.LockdownDef()` (numbered after Night 6, row `GameRules.LockdownNight`, flag
`ShiftDef.Lockdown`). `GameLogic/Lockdown` holds `LockdownRules` (what a mistake is, the rank, the texts) and `LockdownClock`
(35 s per face). `CheckpointView.Lockdown` ticks the clock from `AdvanceQueue`, draws the bar and takes the visitor when it runs
out (`GameRoot.OnVisitorTaken`); `GameRoot.Lockdown.cs` counts the mistakes and ends the night (`CheckLockdownEnd`, called after a
verdict and after each face leaves). `PersistRun` writes nothing for any mode but the story, the sanity controller does not drain,
`PatrolOpen` is false, `EndingView` has its own result lines and always offers TRY AGAIN.

**Night 6 (the Challenge):** `GameMode.Challenge`, `ShiftPlan.Night6Def()`. Unlocked when the story
save holds a Win or Secret in `Records.EndingsSeen` (`GameRoot.Night6Unlocked`). It reuses the Night 5
flow end to end: the same desk, DENY, hunt and endings, with a second script in `GameLogic/AdminTalk`
(`n6_hub`, chosen by `AdminTalkContext.Night6`) and the "Night 6" numbers in `GameConfigSO` picked
by `GameRoot.BuildHuntTuning`. `PersistRun` returns early in this mode so the story save is never
overwritten, and `TriggerEnding` does not add to the story's endings seen.

**Desk furniture that moves** (the FLOOR FILES pull drawer) keeps its numbers in a pure module
(`GameLogic/DrawerMotion`) and one float of state in the view (`CheckpointView.FileDrawer`),
advanced from `TickDrawer` on unscaled time, so a click is answered on its own frame and repeated
clicks reverse the same animation. Files in a drawer are listed A-Z (`RecordSearch`).

**The map** works the same way: `GameLogic/FloorPlan` owns the rooms in a fixed design space,
`GameLogic/MapSheetLayout.Fit` turns the visible reference area into sheet size / plan scale / type
scales, and `UI/MapView` only applies those numbers. The game is a side scroller, so the plan draws
ALL six rooms in ONE row along the top, evenly spaced and in the order you walk past their doors
(`HallX`), with the hallway under them and nothing below it. The plan is fitted to the sheet from `PlanW` x `PlanH` (the rooms plus
a little air), not from the whole sheet.

**Add a clue:** add a `ClueDef` to `ClueRegistry.Build` (order, room, text) and an interactable in
`RoomBuilder` that calls `Session.DiscoverClue(key)`.
**Add an anomaly behavior:** add an `AnomalyBehaviorKind` value and one branch in
`WanderingAnomaly.MoveBehavior`.
**Add a view:** create the class implementing `IScreenView`, then `GameCore.CreateView<T>` in
the scene script that should own it and a field on GameRoot.
