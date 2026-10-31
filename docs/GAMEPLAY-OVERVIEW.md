# Show Me Your ID! - how the game plays right now

**Loop:** Main Menu -> NEW GAME -> black screen: the night title fades in and out -> the dialogue
box opens on black, then fades -> the black lifts off the shut booth -> **sticky note ("CLICK TO
BEGIN", on night 1 the how to play card)** -> the shutter opens for the first student ->
**checkpoint desk** <-> **patrol** (hallway + 2 classrooms) -> shift results -> next
night -> ending. Exhibition build (`exhibitionMode` in GameConfig) = one curated night.

**The line:** an **arrival clock** (25-30 s on night 1, x0.8 each night, double speed while the
window is busy) walks the roster up to the booth. If the window is busy they queue in the yard.
The top bar reads `WAITING n  ·  n TO GO TONIGHT`. After your verdict, the oldest waiter steps up
right away. Patrol **pauses** the clock and nothing is lost across the scene switch; quit +
CONTINUE restores the window visitor, the line order and the arrival timer. Line pressure
(warning at 5, overrun at 7) is switched off in the current build (`queuePressureWarnAt` /
`queueOverwhelmedAt` = 9999); set sane values in GameConfig to bring it back.

**Missing students:** when an anomaly first enters the line it may (seeded, **max 1 per shift**)
make one legitimate student disappear before reaching you. The night's expected total drops, the
radio reports a roll-call discrepancy, and the absence is permanent.

**The window (mouse):** a student reaches the window *behind glass*. Their words appear in a
**speech bubble beside their head** and stay until the next line. On the desk: the FLOOR FILES
cabinet (pick Room 101 or Room 202, A-Z files), the **ID card** and **paper slip** (already on your
desk), the **rules note** and **tonight's log** (with ID numbers), and the **questions panel**.

- **The four questions:** your course, your year, regular or transferee, first classroom (buttons
  or keys `1` to `4`). You can ask only **2 per student** (the panel shows `2/2`), so choose. Year
  unlocks once you have flipped the ID, status once you have opened their file; hearing an answer
  again is free. The answer shows in the bubble and stays on its button. Compare it with the card
  and the student's file. An anomaly lies about the room (the other room, a room the school does
  not have, or the wrong class) or about course, year or status. Lies start on night 2, and a
  strange tone of voice tells you something is off before you spend the second question.
- **The ID** flips only by its black spine (or `F`). Click it to inspect it enlarged; an open file
  stays visible next to it. The face at the window vs the ID photo is **your own eyes**.
- **Faces:** the ID always shows the real student. The window shows the same person unless the
  visitor has a photo defect (somebody else's face, or the same person with one feature off).
  Anomalies with other defects look completely normal.
- **Stamps:** drag **ACCEPT** or **DECLINE** onto the slip. The slip glows while you hold a stamp,
  and a missed drop tells you why. The mark lands where you dropped it. Stamp again to replace it.
- **Hand back:** the window sill lights up; drag the ID back. The decision counts now and is
  permanent. Wrong calls feed complaints (false rejects) and breaches (anomalies let in); 3 of
  either ends the run on the last night.
- The prompt line at the top always names the next step.

**Patrol (A/D or arrows, Shift run, E interact):** keyboard only, there is no cursor. The corridor
is one painted image, always dark, and **no anomalies roam the school**: they only come to the
security window. The doors follow the desk map: Security, Room 101, Room 202, CR-1, the sealed server
room, CR-2 (FloorPlan.cs draws the hallway and both maps). The guard faces the way he walks, and the
**flashlight** points the way he faces: `F` sweep on/off, `Q` focus, `L` off. Every room is a
**peek** from its door: a black cover with a hole that follows the mouse shows the room behind it
(`F` on/off, `Q` wide/tight). `E` selects what the lit lamp is on, nothing else; `S` or the bottom
edge steps back. Besides the clues there are things to inspect: the patrol clipboard, a dead switch,
the lockers, and each CR's mirror, sinks and stalls. One token lies in the hallway on each of nights
1 to 4; tokens are banked between runs and 3 open Endless and Challenge on the main menu. Ten clues,
found in order, open the secret ending.

**Night 5 - the administrator:** no students and no roaming anomalies, only him, with a staff badge
and a roster memo that do not match. The four student questions are gone. `T` opens the
interrogation: his badge name, the number, his face, his first stop, and any evidence you found.
Each answer has a follow-up that catches him out, and every **lie exposed** takes 15 seconds off the
night (it can never be shorter than 120 seconds). Talking never stamps. **REFUSE ENTRY** = the DENY
stamp. Allowing him in loses at once.

**The hunt (after the DENY):** he says he'll be back and leaves the window. Then it plays like
Nightmare Fredbear's night in FNaF 4, with captions instead of audio: he walks the halls. When they stop he is at one of three places: **a
classroom door** (stand there and hold `E` for 2 s within 10 s), **CR-1 or CR-2** (peek in and
hold the lamp on his head within 12 s), or **right behind you** (turn and flash his face within 2.6 s). Too
slow and he catches you: sanity drops and you are thrown back. **3 catches** = he reaches the server
door (lose). Survive **210 s** to dawn (normal Win/Secret ending). The security door is locked and
every phase has a time limit, so it cannot get stuck. Quit + CONTINUE restarts the hunt.

**Endings:** Surviving Hollowbrook (5 nights + dawn on the last) / Another Custodian
(3 breaches, 3 false rejections, the line overruns, the administrator let in, or three catches on night 5) /
The Truth Behind Hollowbrook (10 clues in order, dawn reached).

**Options:** SETTINGS is an audio-only modal (MASTER / MUSIC / SFX), usable from the menu and the
pause menu. Display, accessibility and the data reset (`F2` x3) live in `SettingsSystem`.

**Checked here:** rules tests, a button-level flow check of the desk, patrol and the whole hunt, and
a compile against real Unity assemblies. **Not checked here:** pixels, audio, feel - play it in the
editor (see the playtest list in the final report).
