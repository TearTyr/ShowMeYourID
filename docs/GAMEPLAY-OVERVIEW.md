# Show Me Your ID! - how the game plays right now

**Loop:** Main Menu -> NEW GAME -> black screen: the night title fades in and out -> the dialogue
box opens on black, then fades -> the black lifts off the shut booth -> **sticky note ("CLICK TO
BEGIN", on night 1 the how to play card)** -> the shutter opens for the first student ->
**checkpoint desk** <-> **patrol** (hallway + 2 classrooms) -> shift results -> next
night -> ending.

**Night 1 tutorial (parked, switched off with `TutorialSteps.Enabled = false`; night 1 plays like any night):** a new story opens with a guided night (`GameLogic/TutorialSteps`, the box is `UI/TutorialGuide`). It starts with a
keybind card and a CONTINUE button (the sticky note does nothing until then). Steps: a real
student, a student with an expired ID, the patrol (opens early, the line waits), a slow hallway anomaly that cannot catch you (beam slows
it, Focus drives it out), Room 101 (clean), Room 202 (a figure to clear), then GOT IT and the player finishes alone. The clock and sanity stand
still while it runs, only the room the lesson is about opens, and SKIP TUTORIAL is always shown. Night 1 always has Room 101 empty and Room 202
occupied, nobody goes missing, and visitor 1 and 2 are calm (the frightened one comes later). It is saved with the run (`tutorialStep`).

**The line:** an **arrival clock** (25-30 s on night 1, x0.8 each night, double speed while the
window is busy) walks the roster up to the booth. If the window is busy they queue in the yard.
Nothing on the desk says how many visitors are left tonight; the waiting line is only the hooded figures at the sides. After your verdict, the oldest waiter steps up
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

- **The night security log (nights 1 to 4):** the log paper has three boxes that fill themselves in, nobody
  counts: STUDENT ENCOUNTERS (everyone who reached the window), ANOMALIES DETECTED (how many you rejected, not
  how many really were anomalies, so it never gives a verdict away) and SECURITY INCIDENTS (students who never
  arrived plus things driven out of a peek room). Under the paper, the slip lists **everyone you let in** as
  buttons. Its job is a **second chance**: before END SHIFT you may **report one person** you doubt (click a
  name, click again to take it back, one at a time). END SHIFT is never held up by the log. At END SHIFT
  (`GameLogic/NightLog.cs`) a **right report** (they were an anomaly) cancels that one breach, so a night that
  would have failed does not, and a **wrong report** (a real student) costs 5 sanity at the start of the next
  night. You are never told on the spot: the next night's **Rules sheet** starts with LAST NIGHT'S LOG, which
  says how the report went. No report, no effect. Nights 5 and 6 have no log duty. The list, the report and the
  note are saved. The boxes are objects under `OverlayLog/paper` and the name buttons are `report_0` to
  `report_7` under `OverlayLog/slip` (box positions are fractions of the picture, `LogBoxCentres` in
  `CheckpointView.Overlays.cs`).

- **The four questions:** your course, your year, regular or transferee, first classroom (buttons
  or keys `1` to `4`). You can ask only **2 per student** (the panel shows `2/2`), so choose. Year
  unlocks once you have flipped the ID, status once you have opened their file; hearing an answer
  again is free. The answer shows in the bubble and stays on its button. Compare it with the card
  and the student's file. An anomaly lies about the room (the other room, a room the school does
  not have, or the wrong class) or about course, year or status. Lies start on night 2. About half
  of the anomalies also speak awkwardly: the same facts in wrong-sounding words (Raffy says everything
  twice, Maria Clara talks about "the girl"). Nothing marks it on screen; you have to hear it. A real
  student never speaks that way.
- **Scared visitors:** every night some visitors arrive frightened (1, 2, 2, 3, 3 on nights 1 to 5). A scared
  visitor, real student or anomaly, shows the same frightened face and talks in the same shaken words, so the
  monster face and the awkward speech cannot give them away. A scared anomaly is still wrong on the papers or
  in the file. Fear proves nothing.
- **The ID** flips only by its black spine (or `F`). Click it to inspect it enlarged; an open file
  stays visible next to it. The face at the window vs the ID photo is **your own eyes**.
- **Faces:** the ID always shows the real student. The window shows the same person unless the
  visitor has a photo defect (somebody else's face, or the same person with one feature off).
  Anomalies with other defects look completely normal.
- **Stamps:** drag **APPROVE** or **DECLINE** onto the slip. The slip glows while you hold a stamp,
  and a missed drop tells you why. The mark lands where you dropped it. Stamp again to replace it.
- **Hand back:** the window sill lights up; drag the ID back. The decision counts now and is
  permanent. Wrong calls feed complaints (false rejects) and breaches (anomalies let in); 3 of
  either ends the run on the last night.
- The prompt line at the top always names the next step.

**The window:** the next student steps up within 3 seconds of an empty window, and the window stays
open from the first student to the last. Then it closes and Patrol unlocks.

**Patrol (A/D or arrows, Shift run, E interact):** the mouse only aims the flashlight, it selects
nothing. Doors and items go by where the guard stands. The corridor is one painted image, always
dark. The doors follow the desk map: Security, Room 101, Room 202, CR-1, the sealed server room, CR-2
(FloorPlan.cs draws the hallway and both maps, with every room in one row above the hallway because it is a side scroller). The **flashlight** points at the cursor (`F` sweep
on/off, `Q` focus, `L` off). While it is lit it turns the guard to face it; while it is off he faces
the way he walks. **One patrol cycle** is required before END SHIFT: peek into Room 101, Room 202 and
a CR (either one). **Anomalies** (the four legged **crawler** that runs on the floor or upside down on the ceiling, the crouched one, and one that hangs from the top of the hallway above the doors) roam the hallway, from night 1, with one already waiting when each patrol starts: hold the Focus beam on it to repel it, and it is gone for a while. The crawler is small and always runs fast (`anomalySpeedSprint`), straight at you once you are within 9 units. The wide Sweep beam only shows what is there and slows any anomaly, the crawler included, to half speed while it is on it (`beamPace`), so there is time to switch to Focus (`Q`); it never repels or freezes anything. The guard carries only a weak light, so what is ahead of him is black until the flashlight finds it: sweep it along the walls, up at the ceiling and down at the floor. Every room is a
**peek** from its door: a black cover with a hole that follows the mouse shows the room behind it
(on when the room opens; `F` on/off, `Q` wide/tight). `E` selects what the lit lamp is on, nothing else; `S` or the bottom
edge steps back. Some peek rooms hold something that stands on the painted floor (never on furniture), smaller the further back it stands, in some rooms each night and always at least one. Any peek room can hold the **hooded figure**, the **crouched one** or the **ceiling hanger** (the crawler never stands in a peek room, it only patrols the hallway). Hold the lamp on its body (the cursor on it): about 3 seconds with the plain wide lamp, 1.3 times faster with the Focus lamp (`Q`), and the bar drains back if the light leaves. While it fills, every one of them **stays perfectly still**: nothing shakes, grows or moves, and the lamp only dims a little more. Then the lamp goes black, it is gone, and a slow fade brings the room back. From the black on, the lamp, `E` and stepping back are locked. It never attacks. Reduce Motion keeps a steadier, deeper dim. The patrol flashlight no longer paints a yellow cone: it clears the darkness, so the room shows its own art, like the peek lamp. A room does not count for the cycle until its anomaly is gone,. **Step out of a peek with its figure still standing and that door becomes a jumpscare**: the next time you open it the figure jumps you (the sanity cost of a catch, `CaughtSanity`) and the door never opens. The scare uses the figure up, so the room then counts as cleared. The mark is saved with the night (`PeekLeftBehind`). Besides the clues there
are things to inspect: the patrol clipboard, the lockers, and each CR's mirror, sinks and stalls.
The pocket watch token on Room 101's lost-and-found board is one of the ten: it is picked up when
inspected (once) and is gone for good, with no other use. Ten clues, found
in order, open the secret ending.

**Night 5 - the administrator:** eight students first (five of them anomalies), then him last, with a staff badge
and a roster memo that do not match. The four student questions are gone. `T` opens the
interrogation: his badge name, the number, his face, his first stop, and any evidence you found.
Each answer has a follow-up that catches him out, and every **lie exposed** takes 15 seconds off the
night (it can never be shorter than 120 seconds). Talking never stamps, and you can ask him anything again and again. Only the DENY stamp turns him
away (there is no refuse button). He speaks only through the dialogue box. Allowing him in loses at once.

**The hunt (after the DENY):** the window closes, the booth dims and flickers, and he says he'll be
back and leaves. The security game is over and only Patrol works; the hunt starts when it is clicked.
Then it plays like Nightmare Fredbear's night in FNaF 4, with captions instead of audio. He is the
only anomaly and can be anywhere: in **Room 101, Room 202, CR-1 or CR-2** (peek in and hold the lamp
on his head within 9 s) or **right behind you** (aim at him and flash his face within 2.8 s: he **runs** up the hallway at your back, about 3.9 units a second). He goes for you: in the hallway he
comes up behind you more often than not, and while you peek into a room he is often in that very room. Only
the light stops him, and each success gives 8 sanity. Too slow and he catches you: sanity drops and
you are thrown back. **3 catches** = he reaches the server door (lose). Survive **210 s** to dawn.
**Doors are not safe.** Look into his room and step out without sending him away, and **that same door is a
catch** the next time it opens (the HUD says DO NOT OPEN THAT DOOR). Leave him in his room until its timer runs
out while you stand in the hallway, and he is **loose** for 6 s (`HuntLooseSeconds`): **any door** you open is a
catch. Staying away from the doors for those 6 s is safe, and then he comes up behind you as usual.
The security door is locked and every phase has a time limit, so it cannot get stuck. Quit + CONTINUE
restarts the hunt, or returns to the desk if it has not started.

**Lockdown:** the third mode, locked until Night 6 is beaten or the secret code `wwssaaddggez` is typed on the main menu (the code opens Night 6 first, then Lockdown; `GameRoot.Unlocks.cs`) (`GameMode.Lockdown`, `ShiftPlan.LockdownDef()`, rules in `GameLogic/Lockdown`). One
night, all 13 students (6 anomalies, all frightened) arriving 8 to 11 s apart, no patrol, no log, no sanity drain. Each face has
35 s at the window (the bar at the top of the desk), then it is taken. Mistakes: an anomaly let in, a student turned away, a student
taken. Three lose; clearing the line wins, ranked FLAWLESS, STEADY or BARELY HELD. Never saved.

**Night 6 (the Challenge):** opens on the main menu after the story's Win or Secret ending. He is back
and says he does not give up; he asks for the right decision and has a new set of questions. DENY him
again and the pursuit starts, harder: 2 catches, shorter stalks, less time to react, a longer night. He runs up the hallway faster (1.8 s, as fast as you run), and in a room he does not wander: like Monster Vinnie in FNaC 3 he **circles about his spot and dodges the lamp**, faster while the light is on him, turning round at random (`AdminHuntDodge`). A lamp parked in the middle never finds him.
Surviving to dawn is its own ending. It is one night and never touches the story save.

**End of a night:** nights 1 to 4 run on without the menu: the screen dims, then the next night starts (a marathon). Clocking out with an anomaly let in
still finishes the night: the next night starts by itself and its intro says what it costs. CONTINUE still resumes a game you quit. A night you
do not finish (a blackout) is **played again**: CONTINUE stays on that night, with the same cost.
Night 5 shows the results, then the ending. Losing night 5 keeps night 5 in the save: CONTINUE offers
it again and the ending screen has RETRY NIGHT 5. Losing night 6 offers RETRY NIGHT 6. There is no scare
at the desk; scares only happen on patrol and in peeks. A retry always restarts the night empty, and
stepping out of a peek never lands a catch you did not see coming (a short grace, and anything by the
door is pushed back).

**Endings:** Surviving Hollowbrook (5 nights + dawn on the last) / Another Custodian
(3 breaches, 3 false rejections, the line overruns, the administrator let in, or three catches on night 5) /
The Truth Behind Hollowbrook (10 clues in order, dawn reached).

**Main menu:** CONTINUE is dark grey and dead when there is no save. With a save, hovering it shows
a large `NIGHT n` under the button.

**Options:** SETTINGS is an audio-only modal (MASTER / MUSIC / SFX), usable from the menu and the
pause menu. Display, accessibility and the data reset (`F2` x3) live in `SettingsSystem`.

**Checked here:** rules tests, a button-level flow check of the desk, patrol and the whole hunt, and
a compile against real Unity assemblies. **Not checked here:** pixels, audio, feel - play it in the
editor (see the playtest list in the final report).
