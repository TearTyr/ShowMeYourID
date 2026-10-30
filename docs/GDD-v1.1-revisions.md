# Show Me Your ID! — GDD v1.1 revision sheet

**What this is.** The GDD ("Game Design Document Show Me Your ID! (2026)-1.pdf") describes the game as it was
before the recent changes. This sheet lists, section by section, **only** the passages that now disagree
with the game or with the owner's decisions: what to find, and what to replace it with. Wording follows the
GDD's own style. Nothing else in the document changes.

**What is deliberately NOT touched** (owner's instruction): sections 2.16–2.22 and every other piece of the
Super Mario Bros. template text, including the team-table note, the "Time: reaching zero costs a life" row and the
"Player rewards" points table in 2.7, and the references. Part 1 (research) is untouched too.

**Planned vs built.** The GDD's own provenance note says unfinished features are "treated as planned features
rather than completed implementations". Everything below marked **(Planned)** is decided but not yet in the
game; everything else is in the game now. Numbers marked *tuning* are first-pass values that still need playtesting.

**The flowchart (p. 11)** is an image and cannot be patched in text; the changes it needs are listed in section F.

---

## A. 2.1 Document control

**Add** a line under *Design history*:

> v1.1, October 30, 2026: updated to match the implemented game and the production decisions made since v1.0
> (patrol and peek changes, shift failure and the final night, sanity, desk interface).

---

## B. 2.2 – 2.3 Overview, features, definitions

**2.2 Scope** — replace
> "...across the security room, hallways, and classrooms."

with
> "...across the security room, hallways, classrooms, and the comfort rooms."

**2.3 Game elements** — replace
> "Moving through the hallway and peeking into classrooms with a flashlight cursor."

with
> "Moving through the hallway and peeking into classrooms and comfort rooms with a flashlight cursor."

**2.3 Key features** — replace
> "Hallway exploration with wandering anomalies, and classroom peek-in inspection with a circular flashlight cursor."

with
> "Hallway exploration with wandering anomalies, and classroom and comfort-room peek-in inspection with a circular flashlight cursor."

and replace
> "A sanity system, three endings (Win, Lose, Secret), plus Endless and Challenge modes."

with
> "A sanity system with drain stacks and gradual recovery, shift failure on nights 1 to 4, three endings (Win, Lose, Secret) decided on the final night, plus Endless and Challenge modes."

**2.3 Design guideline 3** — replace
> "Keep the security room stationary and the classrooms peek-only, so the hallway remains the place where the player can be caught."

with
> "Keep the security room stationary and the classrooms and comfort rooms peek-only, so the hallway remains the place where the player can be caught."

**2.3 Game design definitions** — replace the **Lose** line
> "Lose: three anomalies breach the building, or three legitimate students are falsely rejected."

with
> "Lose: on the final night, three anomalies breach the building, or three legitimate students are falsely rejected (counted over the whole run), or the custodian's sanity reaches 0%. On nights 1 to 4 these events do not end the game; they fail the shift (see Shift failure)."

and **add** after *Shift transition*:
> "Shift failure: on nights 1 to 4 a shift fails if the custodian blacks out (sanity reaches 0%) or clocks out with an anomaly let in that night. A failed shift ends with a jumpscare and returns the player to the title menu, and the next night starts with less sanity, drain stacks already active, and an anomaly already roaming. The game continues."

**2.3 Game design definitions** — replace the **Secret ending** line
> "Secret ending: discover all ten environmental interactions across the hallway, Classroom 1 and Classroom 2."

with
> "Secret ending: discover all ten environmental interactions across the hallway, Classroom 1 and Classroom 2. (Planned) Each night has a fixed set of interactable objects, each with a subtle hint that it can be interacted with, so the full set is found progressively across the nights."

---

## C. 2.5 Game flow summary and gameplay outline

**2.5 step 4** — replace
> "Using the map, the player can leave the window, walk the hallway and peek into Classroom 1 and Classroom 2 to look for clues and wandering anomalies."

with
> "Using PATROL HALLWAY, the player can leave the window, walk the hallway and the comfort hallway, and peek into Classroom 1, Classroom 2, CR-1 and CR-2 to look for clues and anomalies. The map is a reference kept at the security desk; it does not move the player."

**2.5 step 6** — replace
> "Wrong decisions raise the risk of the Lose ending and lower sanity."

with
> "A wrong decision adds a sanity drain stack, which speeds up the loss of sanity for a short time; a correct decision restores sanity gradually."

**2.5 step 7** — replace
> "After Shift 5 the ending is decided: Win or Secret. The Lose ending can end the run at any point."

with
> "After Shift 5 the ending is decided: Win or Secret. The Lose ending can end the run only on the final night; a failed shift on nights 1 to 4 does not end the run."

**Gameplay outline 2** — replace
> "Game options: Settings, plus a Pause menu with a Controls screen and a Back button."

with (no change needed) — *kept as is.*

**Gameplay outline 9** — replace
> "Losing: three anomaly breaches or three false rejections."

with
> "Losing: on the final night, three anomaly breaches, three false rejections, or sanity reaching 0%. On nights 1 to 4 a bad night fails the shift instead."

**Gameplay outline 10** — replace
> "End: an ending screen shows the ending title, description, decision summary and final result."

with
> "End: an ending screen shows the ending title, description, decision summary and final result. Finishing a night on nights 1 to 4 flickers the screen to black and returns the player straight to the title menu, with no results screen and no continue prompt; Continue resumes at the next night. The full results appear only after the final night."

---

## D. 2.6 – 2.9 Mechanics, player, rules, progression

**2.6 Controls** — replace
> "Interaction key or mouse click: interact with environmental objects."

with
> "Interaction key (E): interact with doors and environmental objects. Flashlight: F or left click turns it on and off; Q or right click narrows the beam while it is on; L turns it off."

**2.6 Player actions → Exploration** — replace
> "Exploration: Investigate hallways and classrooms to discover clues and anomalies."

with
> "Exploration: Investigate hallways, classrooms and comfort rooms to discover clues and anomalies."

**2.6 Items → Student ID** — replace
> "Student ID: Displays the student's identity and photograph."

with
> "Student ID: Displays the student's identity and photograph. Clicking it lifts it into a close-up inspect view; clicking anywhere else puts it back on the desk."

**2.6 Items → add:**
> "Slip: The paper slip each visitor brings. It is dragged and stamped, and stays on the desk."
> "Student Records folder: A folder icon on the desk that opens the registry, filed by cohort (S2024-2028, S2025-2029, S2026-2030)."

**2.6 Items → School Map** — replace
> "School Map: Shows the school's layout and may help unlock the secret ending."

with
> "School Map: Shows the school's layout. It is pinned up in the security office and can only be opened at the desk."

**2.6 Failure conditions** — replace
> "Failure conditions (the player loses if): sanity reaches 0%, three anomaly breaches occur, or three legitimate students are incorrectly rejected."

with
> "Failure conditions: on nights 1 to 4, sanity reaching 0% or clocking out with an anomaly let in fails the shift. On the final night, sanity reaching 0%, three anomaly breaches, or three legitimate students incorrectly rejected is the Lose result."

**2.6 Enemies and hazards → add (Planned):**
> "Room anomalies (Planned): an anomaly may already be present in a room the player peeks into. It drains sanity while the player stays; flashing it with the light makes it flicker briefly and move; if the player lingers without finding it, a random 5 to 12 second timer ends in a jumpscare, and the timer runs 1.8 times slower while the flashlight is on (applied once). Leaving the room before it triggers avoids the jumpscare but carries a penalty."

**2.6 Items → Clipboard** — append (Planned):
> "(Planned) During a peek the clipboard shows a photo of the room and two tick boxes: the room is clean, or something unexpected happened. A correct tick restores sanity (clean: +5; unexpected and survived: an additional +10); a wrong tick costs sanity (-15, growing with each repeat, capped at -20)."

**2.7 Player definitions → Sanity** — replace
> "Sanity: The player's resource: it falls as the player meets anomalies, makes mistakes and sees disturbing events."

with
> "Sanity: The player's resource. It drains slowly while a visitor is at the window and faster on patrol in the dark, falls when the player meets anomalies and sees disturbing events, and recovers while the window is empty. A correct decision restores 15 sanity gradually over about 6 seconds. A wrong decision adds a drain stack."

**2.7 Player definitions → Tools** — replace
> "...and the flashlights (hallway flashlight with battery, and the separate peek light)."

with
> "...and the flashlight (one on/off control with a narrow beam, used in the hallway and when peeking, with a battery on patrol)."

**2.7 Player properties table → Sanity row, "Rule"** — replace
> "Decreases with anomalies, mistakes and disturbing events"

with
> "Drains while a visitor is at the window and on patrol; wrong decisions add drain stacks; correct decisions and an empty window restore it"

**2.7 Player properties table → False rejections / Anomaly breaches rows, "Feedback on change"** — replace
> "Verification result; decision summary at the ending"

with
> "Counted during play; shown on the results and ending screens (not on the desk)"

**2.8 Rules 4** — replace
> "A false rejection and an anomaly breach are both mistakes. Three of either ends the run in the Lose ending."

with
> "A false rejection and an anomaly breach are both mistakes. They add a sanity drain stack. Over the whole run, three of either is the Lose result when the final night is played."

**2.8 Rules 7** — replace
> "Sanity falls with anomalies, mistakes, and disturbing events."

with
> "Sanity drains with time at the window, anomalies and disturbing events. A wrong decision adds a drain stack (baseline drain 0.85x; 1.25x, 1.75x and 2.25x with one, two and three stacks; each level lasts about 10, 20 and 30 seconds — *tuning*). A correct decision restores 15 sanity over about 6 seconds. Sanity reaching 0% blacks the custodian out."

**2.8 Rules → add:**
> "8. On nights 1 to 4, blacking out or clocking out with an anomaly let in fails the shift. The next night starts with less sanity, drain stacks already active and an anomaly already roaming, scaled by how bad the failed night was (*tuning*). The final night is not affected by this rule."

**2.8 Theory and game balance** — replace
> "Two-sided risk. Breaches and false rejections both lead to the Lose ending, so neither stamping Allow nor stamping Reject by default can work."

with
> "Two-sided risk. Breaches and false rejections both add drain stacks and both count toward the final night's Lose result, so neither stamping Allow nor stamping Reject by default can work."

and
> "Failure limits. The limit of three for each mistake type is the current design value and still needs playtesting."

stays as is; **add** under it:
> "Recovery. Sanity is restored by correct decisions and by an empty window, so the pace of arrivals leaves short waits in which the custodian can recover."

**2.9 Progression and difficulty → Structure** — append:
> "Each night is introduced by a title card (NIGHT n) that fades in before the opening dialogue, and the desk shows the current night (n/5) beside the clock. The fifth night is unique: (Planned) the district administrator does everything to get in, the screen darkens and flickers, and the result depends on the player's choice: if the administrator is let in they roam the school and can catch the player (an instant jumpscare, the Lose path); if refused they stop roaming (the Win path, or the Secret ending when all ten interactions were found)."

**2.9 Exploration** — replace
> "Hallways and classrooms provide clues that help the player identify anomalies and uncover hidden information."

with
> "Hallways, classrooms and comfort rooms provide clues that help the player identify anomalies and uncover hidden information."

---

## E. 2.11, 2.14, 2.15 World, interface, technical

**2.11 Level-building method** — replace
> "The game uses a security room for student inspections, side-scrolling hallways for exploration, and classrooms for cursor-based investigation."

with
> "The game uses a security room for student inspections, side-scrolling hallways for exploration, and classrooms and comfort rooms for cursor-based investigation. The comfort hallway branches off the east end of the main hallway through a doorway (E); its two doors, CR-1 and CR-2, are peek-only, and clicking the bottom of the screen is the only way back."

**2.14 HUD** — replace
> "HUD: Displays relevant gameplay information, including the current shift, sanity level, and anomaly breach and false rejection counts."

with
> "HUD: Displays relevant gameplay information, including the current night (n/5, beside the clock) and the sanity level. Anomaly breach and false rejection counts are shown on the results and ending screens, not on the desk."

**2.14 Screens** — replace
> "Screens: Title screen, shift start screen, gameplay screen, settings screen, credits screen, and game over screen."

with
> "Screens: Title screen, night title card and opening dialogue, gameplay screen, settings screen, credits screen and the ending screen, which carries the full results after the final night. Nights 1 to 4 end at the title screen."

**2.14 Controls table** — replace the rows:

| Original row | Replace with |
|---|---|
| "Inspect documents, checklist rows, SCAN, questions, stamps — Mouse" | "Inspect documents, checklist rows, questions; drag stamps onto the slip; click the ID to inspect it — Mouse" |
| "Interact (doors, clues, light switches, tokens, peek entry/exit) — E" | "Interact (doors, clues, tokens, peek entry) — E. The only way back from a peeked room is clicking the bottom of the screen" |
| "Flashlight: wide Sweep / tight Focus / off — F / Q / L — or hold LMB / RMB for a temporary beam" | "Flashlight: on / off — F or left click; narrow beam (while on) — Q or right click; off — L. A click on UI (such as the bottom step-back strip) does not toggle the light" |
| "Map (checkpoint overlay) — M" | "Map (security desk only) — M" |

**2.14 HUD bullets** — replace
> "Show the night number, date, queue count, current visitor, objective, in-game clock (22:00–02:00), sanity stage, flashlight mode, token count, and low-sanity warning."

with
> "Show the night number and counter (n/5), date, queue count, current visitor, objective, in-game clock (22:00–02:00), sanity stage, flashlight state, token count, and low-sanity warning."

**2.14 Checkpoint Desk UI** — replace/remove:

| Original bullet | Change |
|---|---|
| "Show the student's ID card." | Append: "Clicking the ID enlarges and straightens it; clicking elsewhere returns it to the desk." |
| "Add the CAMERA/SCAN panel to show registry results and whether the visitor has already appeared that night." | **Remove** (the scan panel is retired). |
| "Add two chalk circles: ALLOW and CALL PRINCIPAL. Show a No/Yes confirmation before rejecting a visitor." | Replace with: "Add the green (Allow) and red (Reject) stamps. A stamp is dragged onto the slip and leaves its mark where it is dropped; there are no keyboard shortcuts and no confirmation step." |
| "When a verdict is made, stamp the record sheet and show a result banner revealing the truth." | Replace with: "When a verdict is made, the stamp mark stays on the slip, and the player drags the ID back to the window to close the inspection. The slip is not handed back." |
| (add) | "Add the STUDENT RECORDS folder: a folder icon that opens a panel with three cohort tabs; picking a student lays their record on the desk." |
| (add) | "Speech bubble: the student's reply stays on screen, in large dark type on the white bubble, until the next line replaces it or the visitor leaves." |

**2.14 Screen flow and menus, item 5 and 6** — replace
> "5. Game over screen: Appears when the player reaches a failure condition."

with
> "5. Results: nights 1 to 4 return to the title screen with no results screen; after the final night the ending screen shows the full results. The Lose ending appears only on the final night."

**2.15 Functional data model** — add to the list:
> "● Drain stacks, pending sanity refill, this shift's breaches, and any failed-night penalty carried to the next night."

---

## F. Flowchart (p. 11) — what to redraw

- **Remove** the *SCAN (optional)* box and the *Security Office (hallway door)* late-list box.
- **Remove** "wall light switches (E)" from the HALLWAY box.
- **STAMP — PERMANENT** box: replace "ALLOW or CALL PRINCIPAL (reject asks No/Yes)" with "drag the Allow or Reject stamp onto the slip".
- **RESULT BANNER** box: the banner is retired; replace with "stamp mark stays on the slip; drag the ID back to the window".
- Add **comfort hallway · CR-1 · CR-2** beside the PEEK box.
- Replace the dashed red LOSE interrupt: it now applies on the **final night only**. Add a **FAILED SHIFT** branch for nights 1 to 4 (blackout or an anomaly let in at clock-out → jumpscare → results marked FAILED → next night starts with a penalty) that rejoins at *NEXT NIGHT (N+1)*.
- Add the **night title card** before NIGHT INTRO's dialogue.
- **SHIFT RESULTS** box: remove for nights 1 to 4. A finished night (clean or failed) goes straight to **MAIN MENU**; the results appear only inside the **ENDING SCREEN** after night 5.

---

## G. Items found in the GDD that I could not verify (left alone, flagged)

- The **"8 clickable checklist rows"** on the STUDENT DATA record (p. 19) and the **"PRINCIPAL NOTE"** — not checked against the code.
- The **"Time: Countdown per stage; reaching zero costs a life"** row and the **"Player rewards"** points (200 / 1,000) in 2.7 come from the Mario template and match nothing in the game; left as instructed.
- The **line-overrun loss at 7 waiting** (p. 11) is switched off in the current configuration (the threshold is set to 9999).
