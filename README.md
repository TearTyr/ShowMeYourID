# Show Me Your ID! - Twin Studio

**2D psychological horror · deduction · exploration** - PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation, yet still open at night so its students can finish the
term. Students come to the window of your security booth. Check their ID and paper slip, ask
up to two questions, check the answers against their file in the cabinet, and stamp
**ACCEPT** or **DECLINE**. Not everyone at the window is a student. Between visitors you patrol
the hallway and peek into the classrooms and the comfort rooms. On the last night nobody comes but
a district administrator, and turning him away is only the start.

> “Everyone has an ID. Not everyone is a student.”

---

## Quick start

1. Install **Unity 6.3** (the project is stamped `6000.3.0f1`, Built-in Render Pipeline, no paid
   packages). It also opens on other Unity 6.x versions and 2022.3 LTS.
2. Open this folder as a Unity project and let it import.
3. Press **Play**. The first scene is `Assets/_Project/Scenes/MainMenu.unity`. Each of the three
   scenes holds one bootstrap object that builds everything in code.
4. Windows build: `File > Build Settings > Windows > Build` (the three scenes are already listed).

The game ships painted art for the desk, the menu, the hallway, the classrooms, the comfort room
and the seven students. Anything without a picture is drawn in code, and sounds are generated at
runtime. To replace a picture or a sound, drop a file with the same name in the matching folder
under `Assets/_Project/Resources/Art/` or `Resources/Audio/`; the game prefers it automatically.
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
   sealed server room, CR-2. Every room is a peek (`E` at its door). No anomalies roam the school.
8. **Night 5** - no students, only the administrator. The student questions do not apply to him:
   interrogate him (`T`), catch his lies, refuse him (**REFUSE ENTRY** or a DENY stamp), and he
   forces his way in. Survive until dawn (see below).

### Night 5: surviving the administrator

After you refuse him he says he'll be back and leaves. Then it plays like Nightmare Fredbear's night
in *Five Nights at Freddy's 4*: he is the only thing in the building. There is no audio yet, so
captions tell you when he is walking and where he stops:

- **At a classroom door:** stand at that door and hold `E` until he gives up.
- **In CR-1 or CR-2:** peek in and keep the lamp on his head.
- **Right behind you:** turn to him and flash his face (`F` / `Q`).

Too slow and he catches you. **Three catches** and he reaches the server door: you lose. Survive
until **dawn** to win. Every lie you exposed at the window makes the night shorter. The security
door is locked while he is inside. The numbers are in `GameConfig.asset` under "Administrator hunt".

## Controls

| Where | Keys |
|---|---|
| Desk | mouse to drag papers and stamps · `1` to `4` ask the questions · `Tab` file drawer · `F` flip the ID (or click its black spine) · `T` talk to the administrator · `X` or `Esc` close the rules/log sheet · `M` map · `Esc` pause |
| Patrol | no mouse · `A`/`D` or arrows walk and turn the guard and his flashlight · `Shift` run · `E` interact · `F` flashlight on/off · `Q` focus · `L` off · `Esc` pause · night 5: hold `E` at the door he is behind |
| Peeking | mouse moves the lamp · `F` lamp on/off · `Q` wide/tight · `E` selects what the lit lamp is on · `S` or the bottom edge steps back · `Esc` pause |
| Dialogue | click / `Space` / `Enter` / `E` |
| Menu | `F2` ×3 erases save data (then confirm) |

## Tokens

One token lies somewhere in the hallway on each of nights 1 to 4. They are kept between runs and
shown on the main menu.
Three tokens open Endless and Challenge from the TOKENS button.

## Endings

- **Surviving Hollowbrook** - five nights, and dawn reached on the last one.
- **Another Custodian** - 3 anomalies let in, 3 students wrongly turned away, the line overruns
  the checkpoint, the administrator let in, or he catches you three times on night 5.
- **The Truth Behind Hollowbrook** - all ten findings, found in order, and dawn reached.

## Repository layout

```
Assets/_Project/
  Scenes/                 MainMenu, Hollowbrook (the desk), School (patrol) - one bootstrap each
  Resources/              GameConfig.asset (tuning), Audio/ drop-in files
    Art/                  Characters/, UI/ (SecurityDesk, MainMenu) and Environment/ (Hallway, Rooms,
                          Props, Inspect), named in snake_case
  Scripts/
    GameLogic/            PURE C# rules, no UnityEngine (rosters, questions, queue, endings, hunt...)
    Core/                 GameRoot (split in parts), EventBus, saves, settings, config
    Checkpoint/           the desk (CheckpointView, split by area), papers, layout grid
    World/                patrol: rooms, player, flashlight, peek lamp, anomalies, the hunt
    Horror/               anomalies, sanity, scares, hallucinations
    UI/                   menus, HUD, dialogue, map, results, endings
    Bootstrap/            one script per scene that builds it, plus procedural art and audio
    Audio/                music and effects
Art/                      source art for the prep scripts (the current kit is "UI and Design Assets")
Tools/                    test, check and art prep tools (not part of the game build)
docs/                     GUIDE.md, GAMEPLAY-OVERVIEW.md, ART-NAMING.md, ASSET-MANIFEST.md, archive/
```

## Checking your changes (no Unity needed)

```bash
bash Tools/run_all.sh
```

That runs, in order: the rules tests (`Tools/LogicTests`), the button-level flow check
(`Tools/FlowCheck`, which plays the desk, patrol and the hunt headlessly), and the compile against
the **real Unity assemblies** (`Tools/Verify`, downloads them once). Each prints PASS/FAIL.

What these cannot tell you: how it **looks**, **sounds** and **feels**. That needs the editor.

## Source art and the prep scripts

The top level `Art/UI and Design Assets/` folder holds the artists' original files (Characters,
Comfort Room, Hallway, ID, Main Menu, Rooms, Security). The game never reads it. The scripts in
`Tools/` cut it into the small game-ready pictures under `Resources/Art/`:

| Script | Makes |
|---|---|
| `Tools/prep_securityroom_art.py` | the desk kit, `UI/SecurityDesk/` (`sec_*`) |
| `Tools/prep_id_card.py` | the ID card template (needs `pip install psd-tools aggdraw pillow`) |
| `Tools/prep_characters.py` | students, administrator and guard, `Characters/` |
| `Tools/prep_environment_art.py` | hallway, classrooms, comfort room, props and clue close ups, `Environment/` |
| `Tools/prep_mainmenu_art.py` | the main menu, `UI/MainMenu/` (reads the old kit in `Art/Old/`, already cut) |

After a script adds new files, run `python Tools/generate_meta.py` for their Unity `.meta` files and
`python Tools/gen_asset_manifest.py` to refresh the asset list. `Art/Old/` is the replaced art kit.
It is ignored by git and still available in the history.

## Saves

`hollowbrook_save.txt` in `Application.persistentDataPath` (versioned, checksum-guarded; a bad
file counts as “no save”). Settings live in PlayerPrefs. **Save version 3** changed the student
registry and the questions: old saves are ignored, start a New Game.
