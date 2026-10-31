# Show Me Your ID! - how the game plays right now

**Loop:** Main Menu -> NEW GAME -> night intro (click to advance) -> **sticky note ("CLICK TO
BEGIN")** -> **checkpoint desk** <-> **patrol** (hallway + 2 classrooms) -> shift results -> next
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

**Patrol (A/D or arrows, Shift run, E interact):** the corridor, both classrooms and the comfort room
are painted (the hallway art changes with the lights), and inspecting a clue shows its close up
beside the text. every room is entered **dark**. Each has a wall
switch (E). The handheld **flashlight** is separate: `F` sweep on/off, `Q` focus, `L` off. Rooms
1 and 2 are inspected by **peeking** from the doorway with a cursor lamp (`F` on/off, `Q` wide/tight);
the exit is the bottom edge or `E` on the doorway. The hallway's east end has a door to the
**comfort room** (press `E`, next to the sealed server door): a room you walk into, dimmed far
more than the others, so only the flashlight shows anything. The same door takes you back out. The beam runs on a battery. Holding Focus on a
roaming anomaly fills TRACK: roaming -> stalking -> detected -> exposed -> repelled (it retreats and
drops a token, and may return). Ten clues across three rooms, found in order, open the secret ending.

**Night 5 - the administrator:** he arrives last with a staff badge and a roster memo that do not
match. `T` opens the conversation (a short list of questions about the badge, the number, his face,
his answer, and any evidence you found). Talking never stamps. **REFUSE ENTRY** = the DENY stamp.
Allowing him in loses at once.

**The hunt (after the DENY):** he forces the gate and hides in Classroom 1 or 2 (a knock and a
caption say which). Peek in with the lamp on: his picture shakes and circles; keep the cursor on his
head until the hold fills (**stun**). The lamp flickers out and he vanishes, then he hides in the
other room or **charges down the hallway**: flash his face with the beam. **4 stuns** = driven off
(normal Win/Secret ending). **SERVER DOOR** meter (180 s, paused only while he is held under the
lamp) reaching zero = he reaches the server room (lose). Nothing else roams during the hunt, the
security door is locked, and every phase has a time limit, so it cannot get stuck. Quit + CONTINUE
resumes the hunt.

**Endings:** Surviving Hollowbrook (5 nights + the administrator driven off) / Another Custodian
(3 breaches, 3 false rejections, the line overruns, the administrator let in, or the server door) /
The Truth Behind Hollowbrook (10 clues in order, administrator driven off).

**Options:** SETTINGS is an audio-only modal (MASTER / MUSIC / SFX), usable from the menu and the
pause menu. Display, accessibility and the data reset (`F2` x3) live in `SettingsSystem`.

**Checked here:** rules tests, a button-level flow check of the desk, patrol and the whole hunt, and
a compile against real Unity assemblies. **Not checked here:** pixels, audio, feel - play it in the
editor (see the playtest list in the final report).
