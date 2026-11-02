# Pitch and Q&A study guide: how ShowMeYourID works

Written for: the person who has to pitch the game and answer questions about how its code works.
Read it in order once, then use the Q&A section (part 8) as flash cards. Every number below was
checked against the code, and the file names tell you where to look if someone asks you to show it.

## 1. The 30 second version

ShowMeYourID is a 2D horror game set in Hollowbrook College, where you are the night custodian.
You do three jobs, five nights in a row:

1. **Security desk.** Students arrive at your window. You check their ID, their file, and ask
   questions. You stamp APPROVE or DENY. Some of them are not students.
2. **Patrol.** Between visitors you walk the hallway with a flashlight, peek into rooms, and
   deal with things that roam.
3. **Survive the last night.** Night 5 is only the district administrator. You question him, deny
   him, and survive until dawn while he hunts you.

Finish the story and **Night 6** (the Challenge) opens: the same administrator, harder.

The one sentence about the code: **the rules of the game are plain C# that does not touch the screen,
and the screens are built on top of those rules.** That is why we can test the whole game without
opening Unity.

## 2. One night, step by step

1. Menu: NEW GAME. `GameRoot.StartNewGame` makes a fresh `GameSession` with a random seed (see `GameRules`) and loads
   the desk scene.
2. `BeginShift` builds the night: the roster of visitors comes from `RosterFactory`, the rules for
   that night come from `ShiftPlan`, and an intro plays.
3. A visitor arrives. `CheckpointQueue` decides when (a timer), and `CheckpointView` shows them.
4. You inspect. `IdRules` finds what is wrong with the papers. You may ask 2 of the 4 questions.
5. You stamp. The verdict is compared with the truth. Wrong calls cost sanity and count as
   breaches (let an anomaly in) or false rejections (turned a real student away).
6. Between visitors you can patrol (`GameRoot.Patrol`, `ViewManager`, `FlashlightRig`).
7. END SHIFT. The night is saved. Nights 1 to 4 go back to the menu, night 5 shows results and
   then one of three endings: Win, Lose or Secret.

## 3. The big idea: rules are separate from screens

This is the thing to understand best, because most questions come back to it.

| Layer | Folder | Knows about Unity? | Example |
|---|---|---|---|
| **Rules** (the brain) | `Scripts/GameLogic` | No. Plain C# | "Is this ID expired?", "Which ending is it?" |
| **Orchestrator** | `Scripts/Core` | Yes | `GameRoot` ties the rules to the screens |
| **Screens** (the body) | `Checkpoint`, `UI`, `World`, `Horror` | Yes | buttons, rooms, flashlight, scares |
| **Scene setup** | `Scripts/Scenes` | Yes | builds or finds the screens when a scene loads |
| **Placeholders** | `Scripts/Generated` | Yes | pictures and sounds painted by code when no file exists |

Why it matters, in plain words: the rules never say "draw a red box". They only answer questions.
The screens ask the rules and draw the answer. So you can run 63,000 rule checks in seconds on a
laptop with no game window, and you can change the art without touching the rules.

## 4. The files worth knowing (read in this order)

About 115 scripts and 20,000 lines of C#. You do not need all of them. These twelve explain the game.

| # | File | What to say about it |
|---|---|---|
| 1 | `Core/GameRoot.cs` (and its parts `.Shifts`, `.Patrol`, `.Endings`, `.Hunt`, `.Saving`) | The one object that lives across all scenes. It owns the run, the save, the audio, and a link to every screen. It is one class split into files by topic. |
| 2 | `GameLogic/GameSession.cs` | Everything that has happened this run: night, sanity, score, breaches, the administrator state. This is what gets saved. |
| 3 | `GameLogic/ShiftPlan.cs` | The five nights as data: visitors 4, 6, 7, 8 and 0, anomalies 2, 3, 4, 5 and 0, plus the rule note and what tricks are allowed each night. Night 6 is defined here too. |
| 4 | `GameLogic/RosterFactory.cs` | Makes the visitors for a night. Honest students come from 7 canon identities. Anomalies copy a student and then get "defects" (a wrong year, an expired ID, a swapped photo). |
| 5 | `GameLogic/IdRules.cs` | The rulebook for the papers. It checks 13 kinds of defect (expired, bad ID format, photo mismatch, duplicate ID and so on). |
| 6 | `GameLogic/CheckpointQueue.cs` | The waiting line: arrival timer, who is waiting, who is at the window, and what happens if the line overruns. |
| 7 | `GameLogic/EndingRules.cs` | Pure function from session to ending. Three breaches or three false rejections lose. The Secret needs ten findings in order plus surviving the administrator. |
| 8 | `GameLogic/AdminHunt.cs` and `World/AdminHuntDirector.cs` | Night 5 and 6 hunt. The first is the brain (a small state machine), the second shows it in the world. |
| 9 | `GameLogic/AdminTalk.cs` | The administrator's conversation: a script of questions and lies that catch him out. |
| 10 | `Checkpoint/CheckpointView.cs` (partial class) | The desk screen, split by area: `.Window`, `.Papers`, `.FileDrawer`, `.Decision`, `.AdminTalk`, `.Overlays` (rules note and the night log). |
| 11 | `World/ViewManager.cs` | Patrol: rooms, doors, the peek view. Also owns the flashlight and the peek lamp. |
| 12 | `Scenes/SceneBuild.cs` and `Scenes/SceneEdits.cs` | How the screens live in Unity scene files and still get connected by code (part 6). |

Support files you may be asked about: `Core/EventBus.cs` (messages between parts),
`Core/GameConfigSO.cs` (every tuning number in one asset), `Core/SaveSystem.cs` plus
`GameLogic/SaveFormat.cs` (saving), `Generated/ProceduralArt.cs` (placeholder art).

## 5. How each system works (the explanations to practice)

### The inspection (the heart of the game)
Every visitor is a `VisitorData`: an ID card, a record in the drawer, a face, and four answers. A
legit student has everything matching. An anomaly has at least one thing wrong, and **every anomaly
has at least one tell that the tests prove is findable**. The tells are the 13 `DefectType` values.
Some are on the papers (expired, year mismatch). Some are only in what they say (the classroom
answer disagrees with the file). You have 2 questions per student out of 4, so you must choose.
Year unlocks after you flip the ID, status after you open the file.

### Fairness and repeatability
A new game gets a random seed, saved with the run (the `Random Seed` setting in `GameConfig` can be
turned off for a repeatable run). `RosterFactory` replays earlier nights to reach
the same state, so a saved game can rebuild the exact same visitors without storing them. That is
how Continue works mid-night.

### Sanity
Sanity runs 0 to 100 with four stages (Stable 70 and up, Uneasy 40, Shaken 15, Breaking below).
Lower stages cause more hallucinations. Wrong decisions add "drain stacks" that make every loss
bigger. A correct decision refills a bit. At 0, nights 1 to 4 black you out and you replay that
night with a penalty. On night 5 it is the Lose ending.

### Failed nights
Clocking out with an anomaly let in finishes the night, and the next night starts worse (less
sanity, a drain stack, an anomaly already roaming), and the intro says why. A blackout means you
did not finish, so Continue puts you back at the start of that same night with the same penalty.
Losing night 5 or night 6 offers RETRY on the ending screen. The start of every night is also kept in
its own file (`hollowbrook_nightstart.txt`), so a retry is always the true start of the night, even
after quitting and Continuing half way through.

### Where the numbers live
Nothing about the rules is buried in the code. `GameLogic/GameRules.cs` holds each number once, and
`GameRules.Tables.cs` holds the five nights and the thirteen students as rows. The `GameConfig` asset
owns the live copy, so a designer changes them in the Inspector. Adding a night or a student is
adding a row.

### Patrol and the flashlight
The hallway is dark. The flashlight is a cone: `BeamGeometry` (pure maths) decides what the beam
covers, and `BeamCover` paints the darkness with that cone cleared out, so the room's own art
shows through. Sweep is wide and short, Focus is tight and long, and Focus is what repels
anomalies. A battery drains and recovers.

### Peeking and the hooded figure
Rooms are never entered, only peeked from the door. The peek lamp is a hole following the cursor.
Some rooms hold a **hooded figure** (`PeekAnomaly`) that stands still. Put the lit lamp fully on
him and a fixed sequence plays (a small state machine): he flickers, the lamp dims, black, a slow
fade back, and he is gone. It cannot be interrupted.

### The administrator hunt (night 5)
Deny him and the Patrol click starts a hunt. `AdminHunt` is a state machine with phases:
Stalking (walking, no danger), InRoom (he is in a peek room, keep the lamp on his head for 1.8 s),
Behind (he is behind you in the hallway, flash his face with the beam within 2.6 s), Retreat,
Won, Lost. Three catches lose (two on Night 6), 210 seconds to dawn. He goes for you: more often
behind you in the hallway, and often in the room you are peeking into. **The hunt can never get
stuck**: the dawn clock always runs and every phase has a time limit, and a test runs random and
idle players to prove it.

### The night security log
A second chance, not a chore. The log paper has three boxes (student encounters, anomalies
detected, security incidents) that fill themselves in. The slip lists everyone you let in, and
before END SHIFT you may report one of them. A right report cancels that anomaly, so the night
does not fail. A wrong report costs 5 sanity next night. It is saved with the night and cleared
each night.

### Saving
`SaveFormat` turns the session into a text file (header `SMYIDSAVE`, version 3, with a checksum).
`SaveSystem` writes it to a temp file, then swaps it in, keeping a backup, so a crash mid save does
not corrupt it. A corrupt or older save is ignored instead of crashing. Settings (volumes and
accessibility) are stored separately in PlayerPrefs.

### Messages between parts (EventBus)
Instead of every class calling every other class, parts publish an event with a string id
(`Bus.Publish(...)`) and anything interested subscribes. Example: the audio director subscribes to
settings changes, the HUD to notifications, the caption bar to caption requests.

## 6. Scenes and the "no bootstrap" change

There are three scenes: `MainMenu`, `Gameplay` (the desk) and `School` (the patrol).

Originally each scene held one empty object with a "bootstrap" script that built the whole
screen from code when you pressed Play. That is hard to edit, because nothing was visible in the
Unity editor. The team needed to edit screens in the scenes, so we changed it:

- The screen code became **find or make**. When a scene starts, the code first looks for the
  object already in the scene (`SceneBuild.Reuse`) and only makes it if it is missing.
- **Hollowbrook > Set Up Project** runs that same code inside the editor and saves the result into
  the scene files. Now every canvas, button and label is a normal Unity object.
- `SceneEdits` keeps your changes: it stores what the code made as a baseline, and when the scene
  loads it finds what you moved, recoloured, re-texted or hid, lets the code run, then puts your
  changes back on top. Things you did not touch keep following the code.
- Clicks are still wired in code, and things that only exist while playing (anomalies, the patrol
  world) are still built in code.
- Pictures painted by code cannot live in a scene file, so the tool saves them as png files in
  `SceneArt`. If a scene was never set up, the game still runs because missing objects get built.

## 7. How we know it works (the testing story)

We have three automatic checks, run with one command (`bash Tools/run_all.sh`):

1. **Rules tests** (`Tools/LogicTests`): about 63,000 checks of the pure rules (IDs, endings, queue,
   hunt, saves, credits, the night log).
2. **Flow check** (`Tools/FlowCheck`): about 1,200 checks that press the real buttons headlessly:
   play the desk, patrol, the whole hunt, night 6, saves and retries.
3. **Compile check** (`Tools/Verify`): compiles every script against the real Unity assemblies, so
   if it passes it compiles in the editor.

Be honest about the edge: these cannot judge how it **looks, sounds or feels**, and the scene
setup tool only runs inside Unity. Those parts were checked by playing in the editor.

## 8. Likely questions and short answers

**What is the core idea of your code?**
Rules and screens are separate. The rules are plain C# that answers questions, the screens draw the
answers. That makes the game testable and the art swappable.

**Is the game random, or the same every time?**
Every new game is different: the seed is random. But the seed is saved with the run, so a night can
be rebuilt exactly. Saving only needs a few numbers, not the whole roster, and tests can fix the seed
to replay the same night and get the same result. `RosterFactory` also makes every student take a turn
as an anomaly (whoever has been one the fewest times goes next), so nobody is skipped in a run.

**How does the game decide someone is an anomaly?**
`RosterFactory` picks which visitors are anomalies for the night and gives them defects. `IdRules`
then finds those defects from the papers and answers. The verdict you stamp is compared with what
the factory made.

**How do you stop an unwinnable night?**
Every anomaly has a tell that tests prove can be found, the hunt has time limits in every phase,
and the queue has an overwhelmed limit. A test runs a random player and an idle player and checks
the hunt always ends.

**What is GameRoot?**
The single object that survives scene changes. It holds the session, saves, audio, settings, the
queue and links to every screen. Gameplay code never searches for objects by name, it asks GameRoot.

**What is a partial class and why use it?**
One class written across several files. `GameRoot` and `CheckpointView` are big, so each file holds
one topic (`.Hunt`, `.Saving`, `.Papers`), but the compiler treats it as one class.

**What is the EventBus for?**
So parts do not need to know each other. One publishes, others subscribe by id.

**How does saving work and what if it breaks?**
Text file with a header and checksum, written to a temp file and swapped in with a backup. If the
file is corrupt or from an old version it is ignored, so the game starts fresh instead of crashing.

**How did you make the screens editable if they were built in code?**
Find or make, plus a baseline of what the code made, so your edits survive (part 6).

**What does the flashlight actually do?**
Maths decides which points are inside a cone; the same cone is used to clear the darkness over the
hallway and to decide what the beam repels. One shape drives both, so what you see is what counts.

**How is Night 6 different from Night 5?**
Same flow, but a different conversation, harder hunt numbers (two catches to lose, shorter stalks),
and it never touches your story save. It unlocks after the Win or Secret ending.

**What is "procedural art"?**
Placeholder pictures and sounds painted by code when no file exists, so the game never shows a
hole while art is still arriving. A real file with the same name always wins. We removed the
placeholders nothing used so the game relies on the team's real assets.

**Hardest bug you found?**
Good answer: the captions. Every caption showed as an empty black strip at the bottom of the screen,
because the game sent the words in one field of the event and the caption bar read another. Fixed
by reading both and drawing text only, with a test so it cannot come back.

**What are the limits?**
The look and feel needs human playtesting, the scene tool needs Unity to run, the patrol world is
not stored in the scene files, and the audio is still placeholder.

**What would you do next?**
Balance the numbers with playtesters (they all live in `GameConfig`), finish the log art, replace
placeholder audio, and move more of the patrol world into scenes.

**Who made what?** (Use the credits: Project Director and Narrative Jessie Mariel De Mesa,
Programming Elijah Japeth Ramos and John Perilla, Art Daniel Steven Solitaria and Jessie Mariel De
Mesa, Audio Clarence Manalo, Marketing and Branding Diosdado Fabelico II and Daniella Garing,
Storyboard Jessie Mariel De Mesa, Daniel Steven Solitaria, Daniella Garing and Diosdado Fabelico II.)

## 9. The numbers to memorize

| Thing | Value |
|---|---|
| Nights | 5 (story) plus Night 6 (Challenge) |
| Visitors per night | 4, 6, 7, 8, then only the administrator |
| Anomalies at the window | 2, 3, 4, 5 (of 4, 6, 7, 8) |
| Questions | 4 exist, 2 allowed per student |
| Defect types | 13 |
| Students | 7 illustrated canon identities |
| Lose threshold | 3 breaches or 3 false rejections |
| Findings for the Secret | 10, in order |
| Sanity stages | Stable 70+, Uneasy 40+, Shaken 15+, Breaking below |
| Hunt | 3 catches (2 on Night 6), 210 s to dawn, lamp hold 1.8 s, flash within 2.6 s |
| Flashlight | Sweep range 12, Focus range 15, all in `GameConfig` |
| Seed | random for each new game (`GameRules.RandomSeed`; fixed value 20261102 when off) |
| Tests | about 63,000 rules checks, about 1,200 flow checks, plus the real Unity compile |

## 10. A study plan (about 4 short sessions)

1. **Session 1 (30 min):** read parts 1 to 3 out loud. Draw the layers table from memory.
2. **Session 2 (45 min):** open `GameSession.cs`, `ShiftPlan.cs`, `IdRules.cs` and `EndingRules.cs`.
   They are short and read like plain English. Be able to explain each in two sentences.
3. **Session 3 (45 min):** open `GameRoot.cs` and `GameRoot.Shifts.cs`, follow `StartNewGame` to
   `BeginShift` with your finger. Then read `AdminHunt.cs` and say the phases without looking.
4. **Session 4 (30 min):** run `bash Tools/run_all.sh` and watch it pass, read part 8 as flash cards,
   and practise the two stories: how the scenes became editable, and the caption bug.

Tip for the Q&A: if you are asked about something you do not know, say what layer it lives in
and which file, then say "I would need to open it to be exact". That is true, and it shows you
understand the structure, which is the thing they are really testing.
