# Show Me Your ID! - Twin Studio

**2D psychological horror · deduction · exploration** - PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation, yet still open at night so its students can finish the
term. Students come to the window of your security booth. Check their ID and paper slip, ask
up to two questions, check the answers against their file in the cabinet, and stamp
**APPROVE** or **DECLINE**. Not everyone at the window is a student. Between visitors you patrol
the hallway and peek into the classrooms and the comfort rooms. On the last night eight students come first,
then a district administrator, and turning him away is only the start.

> “Everyone has an ID. Not everyone is a student.”

---

## Quick start

1. Install **Unity 6.3** (the project is stamped `6000.3.0f1`, Built-in Render Pipeline, no paid
   packages). It also opens on other Unity 6.x versions and 2022.3 LTS.
2. Open this folder as a Unity project and let it import.
   Then click **Hollowbrook > Set Up Project (one click)** in the menu bar. It fixes the scenes,
   the Build Settings list, the art import settings and `GameConfig.asset`, and only touches what is
   broken. **Hollowbrook > Check Project** just reports.
3. Press **Play**. The first scene is `Assets/_Project/Scenes/MainMenu.unity`. Each of the three
   scenes holds its screens as real objects (put there by the setup tool) that you can edit.
4. Windows build: `File > Build Settings > Windows > Build` (the three scenes are already listed).

The game ships painted art for the desk, the menu, the hallway, the classrooms, the comfort room
and the thirteen students. Anything without a picture is drawn in code (a short list, see the asset manifest). There is no
sound yet: nothing is generated, so the game is silent until real files are added. To add a picture or a sound, drop a file with the
same name in the matching folder under `Assets/_Project/Resources/Art/` or `Resources/Audio/`; the game uses it automatically.
Folder rules and naming are in [docs/ART-NAMING.md](docs/ART-NAMING.md), the full list is
[docs/ASSET-MANIFEST.md](docs/ASSET-MANIFEST.md).

**New to the code?** Read [docs/GUIDE.md](docs/GUIDE.md) first.

## How a night plays

1. **Sticky note** - click it to start the night.
2. **The window** - a student steps up. Their ID and slip are already on your desk.
3. **Ask** - up to 2 of the 4 questions (buttons, or keys `1` to `4`): course, year, regular or
   transferee, first classroom. Year unlocks after you flip the ID, status after you open their
   file. The answer stays on its button.
4. **Check** - compare the ID, the slip, the face and the answer with the student's **file**: click
   the cabinet (or `Tab`), pick the room folder, click the name. A wrong room, a room that does not
   exist, or the wrong class is a lie.
5. **Stamp** - drag a stamp onto the slip. Changed your mind? Stamp again before the next step.
6. **Hand back** - drag the ID back to the window. The decision counts now and is permanent.
7. **Patrol** (between visitors) - walk the hallway, peek into classrooms, find the ten findings.
   The hallway passes the rooms in the desk map's order: Security, Room 101, Room 202, CR-1, the
   sealed server room, CR-2. Every room is a peek (`E` at its door).
   Each night needs one patrol: peek into Room 101, Room 202 and a CR, then END SHIFT. Only the hallway has roaming anomalies. Some peek rooms hold a figure standing on the floor (the hooded figure, the crouched one or the ceiling hanger, in any room): hold the lamp on it for about three seconds (the Focus lamp, `Q`, is 1.3 times faster). It stays perfectly still meanwhile; then the lamp dies and when the light slowly returns it is gone. It never attacks. In the hallway the guard only has a weak light around him, so sweep the flashlight, up at the ceiling and down at the floor too.
8. **Night 5** - eight students (the usual checks), then the administrator last. The student questions do not apply to him:
   interrogate him (`T`) as often as you like, catch his lies, turn him away with the DENY stamp, and he
   forces his way in. He speaks only through the dialogue box. Survive until dawn (see below).

### The Night 1 tutorial (parked for now)

**The tutorial is switched off** because it was unstable: night 1 plays like any other night (the first click on the sticky note brings the
first visitor, and every door is open). All of its code is still in the project. To bring it back, set `TutorialSteps.Enabled = true` in
`GameLogic/TutorialSteps.cs` and playtest it. What it does when it is on:

A new story starts with a guided Night 1 that teaches the core ideas once and then leaves you alone. It opens with a card of the
keybinds (desk and patrol) and a CONTINUE button: nobody arrives until you press it. After that a box at the top of the screen
says what to do; the clock and your sanity wait while it does. Visitor 1 is a real student (the whole desk routine). Visitor 2 has an
expired ID (find the broken rule, DECLINE, and meet the Security Log). Then the patrol opens early: a slow anomaly walks the hallway
(the plain light slows it, the Focus light `Q` drives it out and it cannot catch you), Room 101 is a clean peek, Room 202 holds a figure to
clear, and other doors stay shut until their turn. Then GOT IT, and you finish the night alone: a CR, the rest of the line, report anyone you doubt in the
Security Log, END SHIFT. SKIP TUTORIAL is always on the box. Night 2 onward has none.

### Night 5: surviving the administrator

After the eight students you question him at the window. When you refuse him the window closes, the
booth dims and flickers, and he says he'll be back and leaves. The security game is over. When you
press **Patrol** it plays like Nightmare Fredbear's night in *Five Nights at Freddy's 4*: he is the
only anomaly, and he can be anywhere. There is no audio yet, so captions say where he stops:

- **In Room 101, Room 202, CR-1 or CR-2:** peek in and keep the lamp on his head.
- **Right behind you:** aim at him and flash his face (`F` / `Q`).

Only the light stops him, and each success gives sanity back. Too slow and he catches you.
**Three catches** and he reaches the server door: you lose. Survive until **dawn** to win. Every lie
you exposed at the window makes the night shorter. The security door is locked while he is inside.
The numbers are in `GameConfig.asset` under "Administrator hunt".

## Controls

| Where | Keys |
|---|---|
| Desk | mouse to drag papers and stamps · `1` to `4` ask the questions · `Tab` file drawer · `F` flip the ID (or click its black spine) · `T` talk to the administrator · `X` or `Esc` close the rules/log sheet · `M` map · `Esc` pause |
| Patrol | `A`/`D` or arrows walk · mouse aims the flashlight · `Shift` run · `E` interact with what the guard stands at · `F` flashlight on/off · `Q` focus (repels anomalies) · `L` off · `Esc` pause |
| Peeking | mouse moves the lamp · `F` lamp on/off · `Q` wide/tight · `E` selects what the lit lamp is on · `S` or the bottom edge steps back · `Esc` pause |
| Dialogue | click / `Space` / `Enter` / `E` |
| Menu | `F2` ×3 erases save data (then confirm) |

## Night 6: the Challenge

Finish the story (the Win or the Secret ending) and the **NIGHT 6** button on the main menu opens.
It is Night 5 again, harder: he comes back, says he does not give up and asks you to make the
right decision. He has a new set of questions. Deny him again and the pursuit starts, with only 2
catches allowed, shorter walks between stops, less time to find him and a longer night. It is one
night: quitting it ends it, and it never touches your story save. The numbers are under "Night 6" in
`GameConfig.asset`.

## Lockdown

The **LOCKDOWN** button on the main menu is locked until Night 6 has been beaten. A secret code typed on the main menu (`wwssaaddggez`, one key after another) opens the hidden modes one at a time: the first time it opens Night 6, the next time Lockdown. It is one finite night with every student at the gate at
once: all 13 students, 6 of them anomalies, all frightened, arriving fast (8 to 11 seconds apart). There is no patrol, no Security
Log and no sanity drain. Instead there is a clock on every face: **35 seconds** at the window, then the thing outside takes them.
Three **mistakes** and the gate falls (you lose): an anomaly let in, a student turned away, or a real student taken when the
time ran out (an anomaly taken is not a mistake). Clear the line to win; the rank is by mistakes (FLAWLESS, STEADY, BARELY HELD).
A bar at the top of the desk shows the seconds and your mistakes. It is one sitting: it never touches your story save.

## Endings

- **Surviving Hollowbrook** - five nights, and dawn reached on the last one.
- **Another Custodian** - 3 anomalies let in, 3 students wrongly turned away, the line overruns
  the checkpoint, the administrator let in, or he catches you three times on night 5.
- **The Truth Behind Hollowbrook** - all ten findings, found in order, and dawn reached.

## Repository layout

```
Assets/_Project/
  Scenes/                 MainMenu, Gameplay (the desk), School (patrol) - hold their screens
  Editor/                 ProjectSetup: the Hollowbrook menu (one click setup and check), editor only
  Resources/              GameConfig.asset (tuning), Audio/ drop-in files
    Art/                  Characters/ (Students, Anomalies, Staff), UI/ (SecurityDesk, MainMenu) and Environment/ (Hallway, Rooms,
                          Props, Inspect), named in snake_case
  Scripts/
    GameLogic/            PURE C# rules, no UnityEngine (rosters, questions, queue, endings, hunt...)
    Core/                 GameRoot (split in parts), EventBus, saves, settings, config
    Checkpoint/           the desk (CheckpointView, split by area), papers, layout grid
    World/                patrol: rooms, player, flashlight, peek lamp, anomalies, the hunt
    Horror/               anomalies, sanity, scares, hallucinations
    UI/                   menus, HUD, dialogue, map, results, endings
    Scenes/               one start script per scene, plus the find or make helpers (SceneBuild, SceneEdits)
    Generated/            procedural art (painted in code when no file exists)
    Audio/                AudioDirector and AudioLibrary (plays real files only)
Art/                      source art for the prep scripts (the current kit is "UI and Design Assets")
Tools/                    test, check and art prep tools (not part of the game build)
docs/                     GUIDE.md, GAMEPLAY-OVERVIEW.md, ART-NAMING.md, ASSET-MANIFEST.md
```

## Checking your changes (no Unity needed)

```bash
bash Tools/run_all.sh
```

That runs, in order: the rules tests (`Tools/LogicTests`), the button-level flow check
(`Tools/FlowCheck`, which plays the desk, patrol and the hunt headlessly), and the compile against
the **real Unity assemblies** (`Tools/Verify`, downloads them once), and a check that no file contains an em dash
(`Tools/check_no_em_dashes.py`; the project never uses them). Each prints PASS/FAIL.

What these cannot tell you: how it **looks**, **sounds** and **feels**. That needs the editor.

## Source art and the prep scripts

The top level `Art/UI and Design Assets/` folder holds the artists' original files (Characters,
Comfort Room, Hallway, ID, Main Menu, Rooms, Security). The game never reads it. The scripts in
`Tools/` cut it into the small game-ready pictures under `Resources/Art/`:

| Script | Makes |
|---|---|
| `Tools/prep_securityroom_art.py` | the desk kit, `UI/SecurityDesk/` (`sec_*`) |
| `Tools/prep_id_card.py` | the ID card template (needs `pip install psd-tools aggdraw pillow`) |
| `Tools/prep_characters.py` | students, administrator and guard, `Characters/Students`, `Staff`, `Anomalies` |
| `Tools/prep_environment_art.py` | hallway, classrooms, comfort room, props and clue close ups, `Environment/` |
| `Tools/prep_mainmenu_art.py` | the main menu, `UI/MainMenu/` (reads the old kit in `Art/Old/`, already cut) |

After a script adds new files, run `python Tools/generate_meta.py` for their Unity `.meta` files and
`python Tools/gen_asset_manifest.py` to refresh the asset list. `Art/Old/` is the replaced art kit.
It is ignored by git and still available in the history.

## Saves

`hollowbrook_save.txt` in `Application.persistentDataPath` (versioned, checksum-guarded; a bad
file counts as “no save”). Settings live in PlayerPrefs. **Save version 3** changed the student
registry and the questions: old saves are ignored, start a New Game.
