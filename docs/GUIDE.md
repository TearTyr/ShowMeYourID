# Guide for the one who makes the game

You know the Unity basics. This page tells you where things live and how to change them.

## How the game is put together (the one idea you need)

There is almost nothing set up by hand in the Unity Inspector. Each of the three scenes
(`MainMenu`, `Hollowbrook`, `School`) holds **one empty object with a bootstrap script**. When you
press Play, that script **builds the whole screen in code**. So when you want to change something,
you change a script (or `GameConfig.asset`), not a scene.

- **Hollowbrook.unity** = the security booth desk (the inspection).
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
6. `GameLogic/AdminHunt.cs` + `World/AdminHuntDirector.cs` - the final fight.

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
| Tune the hunt (stuns, seconds, head size, charge time) | `Resources/GameConfig.asset`, group **Administrator hunt** |
| Change how his picture shakes | `GameLogic/AdminHunt.cs` > `AdminHuntMotion` |
| Give him a real picture | put a PNG named `admin_hunt.png` in `Resources/Art/` |
| Make customers arrive faster or slower | `GameConfig.asset`, group **Checkpoint queue** |
| How long after a stamp until the next student | `GameConfig.asset` > `verdictBannerSeconds` |
| Move something on the desk | `Checkpoint/CheckpointLayout.cs` (all desk positions are numbers here) |
| Flashlight battery, beam size | `GameConfig.asset` (battery, beam), `World/FlashlightRig.cs` |
| Sanity drain and recovery | `GameLogic/ShiftPlan.cs` > `SanityModel` |
| Win and lose rules | `GameLogic/EndingRules.cs` |
| Replace art or sound | drop a file in `Resources/Art/<key>.png` or `Resources/Audio/<key>.wav`; names in `docs/ASSET-MANIFEST.md` |
| Add a clue | `GameLogic/ClueRegistry.cs` + an interactable in `World/RoomBuilder.cs` (search `ClueObject`) |

## After you change something

```bash
bash Tools/run_all.sh
```

It tells you PASS or FAIL for the rules, the whole desk/patrol/hunt flow, and the Unity compile.
If a test fails after a change you MEANT to make, the test describes the old behaviour: edit it
(`Tools/LogicTests/Program.cs` for rules, `Tools/FlowCheck/Program.cs` for screens).

It cannot tell you how it looks, sounds or feels. Press Play for that.

## Habits that keep this project easy

- One job per method, short methods, a comment that says **why**.
- Numbers that you might tune go in `GameConfig.asset`, not hidden in code.
- Do not use `GameObject.Find`. Ask `GameRoot` for the thing.
- Rules in `GameLogic/`, screens elsewhere.
- Old reports and history are in `docs/archive/`. You never need them to work.
- More detail in `ARCHITECTURE.md` and `STYLE.md`.
