# Guide for the one who makes the game

You know the Unity basics. This page tells you where things live and how to change them.

## How the game is put together (the one idea you need)

The three scenes (`MainMenu`, `Gameplay`, `School`) hold the **real objects** of the game: the
canvases, every screen, every button and label. Open a scene, look in the Hierarchy under
`SceneRoot_...`, and move, resize, recolour or re-text things in the Inspector like any Unity object.

**One click setup:** the menu bar has **Hollowbrook > Set Up Project (one click)**. It fills the three
scenes with those objects, lists them in Build Settings (MainMenu first), sets the art to Sprite at the
size the game draws it, creates `GameConfig.asset` if it is missing, and makes Play always start at the
main menu. It only adds what is missing and **never overwrites a change you made**, so click it any time
something looks off. **Hollowbrook > Check Project** only reports. **Hollowbrook > Rebuild Scenes From
Scratch** (asks first) throws a scene's contents away and fills it again, for a scene that is badly broken.
The code is `Assets/_Project/Editor/ProjectSetup.cs`.

Pictures the code paints (no file in `Resources/Art`) are saved by the tool as png files in
`Assets/_Project/SceneArt`, so the scenes can point at real files.

**What is kept when you press Play:** the game still runs its screen code (it connects the buttons and
fills in the data), then puts back what you changed in the scene. Kept: position, size, colour, picture
and text of an object, and hiding an object that sits on a screen. Not kept: renaming an object (the
code finds things by name), and showing or hiding a whole screen (the game decides that). If you
never ran the setup, the game still works: it builds the missing objects in code.

**Not in the scene files:** things that only exist while playing (anomalies, papers on the desk, the
administrator hunt) and the patrol world (rooms, doors, items), which is data built from code.

- **Gameplay.unity** = the security booth desk (the inspection).
- **School.unity** = the patrol (hallway, classrooms, flashlight, the administrator hunt).
- **GameRoot** (in `Scripts/Core`) is the one object that lives across scenes. It holds the run,
  the night, the save, and links to every screen. Start there if you are lost.

## Read these files first, in this order

1. `Core/GameRoot.cs` - the central object. Its other parts: `GameRoot.Shifts.cs` (starting a
   night), `.Patrol.cs`, `.Endings.cs`, `.Saving.cs`, `.Hunt.cs` (the administrator hunt flow).
2. `GameLogic/RosterFactory.cs` - makes the visitors for a night. This is where students lie.
3. `GameLogic/InspectionRules.cs` and `GameLogic/IdRules.cs` - what is right and wrong.
4. `Checkpoint/CheckpointView.cs` - the desk. It is one class split into files by area
   (`.Window`, `.Papers`, `.FileDrawer`, `.Decision` = questions and stamps, `.AdminTalk`...).
5. `World/ViewManager.cs` - rooms, doors, the peek view.
6. `GameLogic/AdminHunt.cs` + `World/AdminHuntDirector.cs` - night 5, surviving him until dawn.

**The folder rule:** `GameLogic/` is plain C# with no Unity inside. That is why it can be tested
without opening Unity. Rules go there. Anything that touches the screen goes elsewhere.

## Where do I change...?

| I want to... | Go to |
|---|---|
| Add or edit a student (name, year, course, room, class) | `GameLogic/ContentData.cs` > `BuildStudentRoster` |
| Change what students say when they arrive | `ContentData.cs` > `Greetings`, `AnomalyGreetings` |
| Change how an anomaly lies about the classroom | `RosterFactory.cs` > `LieAboutClassroom` |
| Change which tricks appear on which night | `GameLogic/ShiftPlan.cs` (`...Allowed` flags, counts, the rule note text) |
| Add a new kind of trick (defect) | `Enums.cs` (`DefectType`), `RosterFactory.cs` (`ApplyDefect`, `BuildDefectPool`), `IdRules.cs` if it shows on the papers |
| Change what the administrator says | `GameLogic/AdminTalk.cs` > `BuildScript` (plain text, copy a `Topic(...)` line) |
| Change any rule number, a night or a student (their lines, even how they speak awkwardly) | `Resources/GameConfig.asset`, group **Game rules** (defaults in `GameLogic/GameRules.cs` and `GameRules.Tables.cs`) |
| Tune night 5 (catches, seconds to dawn, door and lamp holds, seconds per lie) | `Resources/GameConfig.asset`, group **Administrator hunt** |
| Change how he wanders in a room (speed, how far, how long he pauses) | `GameLogic/AdminHunt.cs` > `AdminHuntMotion`; how fast he walks up behind you: `World/AdminHuntDirector.cs` > `BehindStartDistance` |
| Give him a real picture | `char_admin.png` / `char_admin_glitch.png` come from the Director art (`Tools/prep_characters.py`); drop `char_admin_hunt.png` in `Resources/Art/` for a special hunt picture |
| Add or change a student picture | put the busts in `Art/UI and Design Assets/Characters`, edit the list in `Tools/prep_characters.py`, run it, then add a row to the Students table in `GameRules.Tables.cs` |
| Change the student ID card art | edit `Art/UI and Design Assets/ID/SMYI-ID.psd`, run `python Tools/prep_id_card.py` (needs `pip install psd-tools aggdraw pillow`); text positions are in `CheckpointView.Papers.cs` > `LayoutIdCard` |
| Make customers arrive faster or slower | `GameConfig.asset`, group **Checkpoint queue** |
| How long after a stamp until the next student | `GameConfig.asset` > `verdictBannerSeconds` |
| Move something on the desk | `Checkpoint/CheckpointLayout.cs` (all desk positions are numbers here) |
| Flashlight battery, beam size (the peek lamp drains the same battery) | `GameConfig.asset` (battery, beam), `World/FlashlightRig.cs` |
| How often the white caption text may show, and which captions always show | `GameLogic/CaptionPolicy.cs`; urgent ones are sent as `EventIds.CaptionUrgent` |
| How often small wrong noises and lamp stutters happen on patrol | `Horror/HallucinationSystem.cs` > `TickDread` |
| The peek lamp size | `World/PeekLight.cs` (`Radius`, `FocusRadius`) |
| The figures in the peek rooms (where they stand, how big, how long the lamp must stay on them, who rushes, the black, the fade back) | `GameLogic/PeekSpots.cs`, `World/PeekAnomaly.cs`, `GameConfig.asset` > `peekClearSeconds` |
| How much a plain flashlight beam slows a patrol anomaly | `GameConfig.asset` > `beamPace` (0.5 = half speed) |
| Turn the (parked) Night 1 tutorial back on | `GameLogic/TutorialSteps.cs` > `Enabled` (set it to `true`, then playtest) |
| The Night 1 tutorial (the steps, their words, what each holds still) | `GameLogic/TutorialSteps.cs`; the box is `UI/TutorialGuide.cs`; the glue is `Core/GameRoot.Tutorial.cs` |
| Lockdown (seconds per face, mistakes allowed, arrival speed, who comes) | `GameLogic/Lockdown.cs` (`LockdownRules`), the night row `LockdownNight` in `GameRules.Tables.cs` |
| How big the main menu buttons are | `UI/MainMenuView.cs` > `MenuScale` |
| What a wrong Security Log box costs, and what the Rules sheet says about it | `GameLogic/NightLog.cs` (`SanityLossPerMistake`) |
| How big the sliding anomaly is and where it sits on the floor | `GameLogic/AnomalyBehaviorRules.cs` (`GroundCentreY`), `FilePpus` in `Generated/ProceduralArt.cs` |
| Night 6 (the Challenge): his questions, how hard the hunt is | `GameLogic/AdminTalk.cs` (`n6_hub`), `GameConfig.asset` group **Night 6** |
| The ten findings (the token is number 7) | `GameLogic/ClueRegistry.cs`, `World/RoomBuilder.cs` |
| Move a button or change a label on a screen | open the scene, find it under `SceneRoot_...` and change it in the Inspector |
| Sanity drain and recovery | `GameLogic/ShiftPlan.cs` > `SanityModel` |
| Win and lose rules | `GameLogic/EndingRules.cs` |
| Replace art or sound | drop a file in `Resources/Art/<key>.png` or `Resources/Audio/<key>.wav`; names in `docs/ASSET-MANIFEST.md` |
| Change the credits | `GameLogic/CreditsData.cs` (roles and names) |
| Add a clue | `GameLogic/ClueRegistry.cs` + an interactable in `World/RoomBuilder.cs` (search `ClueObject`) |

## After you change something

```bash
bash Tools/run_all.sh
```

It tells you PASS or FAIL for the rules, the whole desk/patrol/hunt flow, the Unity compile, and that no file contains an em dash
(the project never uses them: write a hyphen, a colon or a new sentence).
If a test fails after a change you MEANT to make, the test describes the old behaviour: edit it
(`Tools/LogicTests/Program.cs` for rules, `Tools/FlowCheck/Program.cs` for screens).

It cannot tell you how it looks, sounds or feels. Press Play for that.

## Habits that keep this project easy

- One job per method, short methods, a comment that says **why**.
- Numbers that you might tune go in `GameConfig.asset`, not hidden in code.
- Do not use `GameObject.Find`. Ask `GameRoot` for the thing.
- Rules in `GameLogic/`, screens elsewhere.
- More detail in `ARCHITECTURE.md` and `STYLE.md`.
