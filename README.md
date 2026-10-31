# Show Me Your ID! - Twin Studio

**2D psychological horror · deduction · exploration** - PC (Windows), mouse + keyboard.

You are the newest temporary night custodian of **Hollowbrook College**, a school shut down
mid-year without an official explanation, yet still open at night so its students can finish the
term. Students come to the window of your security booth. Check their ID and paper slip, ask
which classroom they are going to, check the answer against their file in the cabinet, and stamp
**ACCEPT** or **DECLINE**. Not everyone at the window is a student. Between visitors you patrol
the hallway and peek into the classrooms with a flashlight. On the last night a district
administrator arrives, and turning him away is only the start.

> “Everyone has an ID. Not everyone is a student.”

---

## Quick start

1. Install **Unity 6.3** (the project is stamped `6000.3.0f1`, Built-in Render Pipeline, no paid
   packages). It also opens on other Unity 6.x versions and 2022.3 LTS.
2. Open this folder as a Unity project and let it import.
3. Press **Play**. The first scene is `Assets/_Project/Scenes/MainMenu.unity`. Each of the three
   scenes holds one bootstrap object that builds everything in code.
4. Windows build: `File > Build Settings > Windows > Build` (the three scenes are already listed).

All sprites and sounds are generated at runtime. To replace one with real art or audio, drop a
file at `Assets/_Project/Resources/Art/<key>.png` or `Resources/Audio/<key>.wav`; the game prefers
it automatically. The naming list is [docs/ASSET-MANIFEST.md](docs/ASSET-MANIFEST.md).

**New to the code?** Read [docs/GUIDE.md](docs/GUIDE.md) first.

## How a night plays

1. **Sticky note** - click it to start the night.
2. **The window** - a student steps up. Their ID and slip are already on your desk.
3. **Ask** - press the question button (or `1`): *Which classroom are you going to?* It is free.
   The answer stays in the questions panel.
4. **Check** - compare the ID, the slip, the face and the answer with the student's **file**: click
   the cabinet (or `Tab`), pick the room folder, click the name. A wrong room, a room that does not
   exist, or the wrong class is a lie.
5. **Stamp** - drag a stamp onto the slip. Changed your mind? Stamp again before the next step.
6. **Hand back** - drag the ID back to the window. The decision counts now and is permanent.
7. **Patrol** (between visitors) - walk the hallway, peek into classrooms, find the ten findings.
8. **Night 5** - the administrator arrives. Talk to him (`T`), refuse him (**REFUSE ENTRY** or a
   DENY stamp), and he forces his way in. Hunt him through the classrooms (see below).

### The administrator hunt

After the DENY, he hides in Classroom 1 or 2. A knock and a caption tell you which door. Peek in
(`E` at the door): his picture shakes and circles in the dark, and only your lamp shows it. Keep
the cursor on his **head** to fill the hold; a full hold is a **stun**. The lamp flickers out, he
vanishes, then he either hides in the other room or **charges down the hallway**: flash his face
with your beam (`F` / `Q`). **Four stuns** drive him off. If the **SERVER DOOR** meter reaches zero
first, he reaches the server room and you lose. The security door is locked while he is inside.
The numbers (stuns, seconds, head size…) are in `GameConfig.asset` under “Administrator hunt”.

## Controls

| Where | Keys |
|---|---|
| Desk | mouse to drag papers and stamps · `1` ask the classroom question · `Tab` file drawer · `F` flip the ID (or click its black spine) · `T` talk to the administrator · `X` or `Esc` close the rules/log sheet · `M` map · `Esc` pause |
| Patrol | `A`/`D` or arrows walk · `Shift` run · `E` interact (doors, clues, switches) · `F` flashlight on/off · `Q` focus beam · `L` beam off · `Esc` pause |
| Peeking | mouse moves the lamp · `F` lamp on/off · `Q` wide/tight · `E` at the doorway or click the bottom edge to step back |
| Dialogue | click / `Space` / `Enter` / `E` |
| Menu | `F2` ×3 erases save data (then confirm) |

## Endings

- **Surviving Hollowbrook** - five nights, the administrator driven off.
- **Another Custodian** - 3 anomalies let in, 3 students wrongly turned away, the line overruns
  the checkpoint, the administrator let in, or he reaches the server door.
- **The Truth Behind Hollowbrook** - all ten findings, found in order.

## Repository layout

```
Assets/_Project/
  Scenes/                 MainMenu, Hollowbrook (the desk), School (patrol) - one bootstrap each
  Resources/              GameConfig.asset (tuning), Art/ and Audio/ drop-in files
  Scripts/
    GameLogic/            PURE C# rules, no UnityEngine (rosters, questions, queue, endings, hunt...)
    Core/                 GameRoot (split in parts), EventBus, saves, settings, config
    Checkpoint/           the desk (CheckpointView, split by area), papers, layout grid
    World/                patrol: rooms, player, flashlight, peek lamp, anomalies, the hunt
    Horror/               anomalies, sanity, scares, hallucinations
    UI/                   menus, HUD, dialogue, map, results, endings
    Bootstrap/            one script per scene that builds it, plus procedural art and audio
    Audio/                music and effects
Tools/                    test and check tools (not part of the game build)
docs/                     GUIDE.md, GAMEPLAY-OVERVIEW.md, ASSET-MANIFEST.md, archive/ (old reports)
```

## Checking your changes (no Unity needed)

```bash
bash Tools/run_all.sh
```

That runs, in order: the rules tests (`Tools/LogicTests`), the button-level flow check
(`Tools/FlowCheck`, which plays the desk, patrol and the hunt headlessly), and the compile against
the **real Unity assemblies** (`Tools/Verify`, downloads them once). Each prints PASS/FAIL.

What these cannot tell you: how it **looks**, **sounds** and **feels**. That needs the editor.

## The owner's art kit (`Art/SecurityRoom`)

The layered SecurityRoom UI kit ships in `Art/UI/SecurityDesk/`. Run
`python Tools/prep_securityroom_art.py` after changing it; it slices the export into
`Resources/Art/UI/SecurityDesk/` (`sec_*` keys). The desk wears the kit as a skin over the layout
grid (`CheckpointView.KitSkin.cs`); if a file is missing the procedural look is used instead.

## Saves

`hollowbrook_save.txt` in `Application.persistentDataPath` (versioned, checksum-guarded; a bad
file counts as “no save”). Settings live in PlayerPrefs. **Save version 2** changed the questions:
old saves are ignored, start a New Game.
