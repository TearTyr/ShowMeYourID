# Show Me Your ID! — how the game plays *right now* (verified state)

**Loop:** Main Menu → NEW GAME → night intro (click to advance) → **sticky note ("CLICK TO BEGIN")** →
**checkpoint desk** ⇄ **patrol** (hallway + 2 classrooms) → shift results → next night → ending.
Exhibition build = one curated night.

**The line (Phase 9):** visitors no longer politely wait for you to finish. An **arrival clock**
(randomized **3–5 s**, `GameConfig.queueArrival*`) walks the roster up to the booth: if the window is
busy they **queue in the yard** — one identical silhouette per waiting visitor in the strips either
side of the booth (legit and anomaly look the same). The day banner's second line reads
`QUEUE: n | CURRENT: arrived/expected`. After your verdict banner, the **oldest waiter steps up
immediately**. Let the line reach **5** and the radio warns you (once); reach **7** and the line
**overruns the checkpoint** — a one-shot lose ending (arrivals stop, stamps die). The queue lives in
`GameRoot` (`GameLogic/CheckpointQueue.cs`, engine-free and unit-tested): patrol **pauses** the clock
and nothing is lost across the scene switch; quit + CONTINUE restores the window visitor, your spent
question budget, the line order and the arrival timer. An empty yard is **never** mistaken for a
finished shift — END SHIFT only lights when the whole roster is resolved.

**Missing students:** when an anomaly first enters the line it may (configured chance, seeded roll,
**max 1 per shift**) make one legitimate student disappear before reaching you — preferentially
someone already waiting. The night's expected total drops, the radio reports a roll-call discrepancy,
and the absence is **permanent** (even allowing the anomaly in). Nobody is ever recorded as inspected
who never arrived; the line never reveals who the anomaly is.

**Checkpoint (mouse):** a student reaches the window *behind glass*. On the desk: STUDENT DATA (record
+ 8-row checklist), the student ID card, PRINCIPAL NOTE (tonight's rule), CAMERA/SCAN (registry +
duplicate-tonight). Right panel: four questions (max **2** per visitor, permanent budget) and two chalk
circles: **ALLOW** and **CALL PRINCIPAL** (= reject, with a No/Yes confirmation). Verdicts are permanent;
wrong calls feed complaints (false rejects) and breaches (anomalies admitted); 3 of either ends the run.
**Smart questioning:** each answer posts a testimony line stating whether it *matches the papers* or
*papers disagree*, plus an odd-voice flag — the desk does the clerical comparison, the player keeps judgment.

**Patrol (A/D or arrows, Shift run, E interact):** each room has its own on/off lights (wall switch, E);
the handheld **flashlight** is a separate system — cursor-aimed as a true cone: LMB or F = wide Sweep,
RMB or Q = tight Focus, L = off; the beam punches real darkness (sprite mask) and runs on a battery
(Focus drains 2.4x Sweep; off recovers, faster in genuinely lit rooms; a dead beam reboots at 20% and
needs a deliberate press — it never relights itself). Holding Focus on a roaming anomaly fills TRACK:
roaming → stalking → detected (any light) → exposed (distorted face, meter > 0.55) → repelled (it
physically retreats, drops a token once, then may return later through another room). Sweep is not a
weaker Focus — it freezes a Blink mid-stalk and interrupts a Sprint's telegraphed dash. Anomalies roam
through the connected doors (Hallway ↔ Classroom 1/2); leaving a room neither freezes nor resets them. Ten clues across three rooms, in a discoverable order, gate the
secret ending. Security office (Classroom 1 door): late-list verdicts count toward the story.

**Endings:** Surviving Hollowbrook (5 clean nights + reject the administrator) / Another Custodian
(3 breaches or 3 complaints — or letting the line overrun the checkpoint) / The Truth Behind
Hollowbrook (10 clues in order). Exhibition night closes with its own demo card and never touches
story records.

**Options (Phase 10):** the SETTINGS sheet is a compact **audio-only modal** — MASTER / MUSIC / SFX
with live % read-outs and BACK — usable from the main menu and the pause menu (pause stays paused;
closing returns to the pause sheet; Escape never unpauses behind an open sheet). Volumes apply
immediately (music crossfades read the mix live) and persist through PlayerPrefs. Display,
accessibility and data-reset settings remain in `SettingsSystem` and their other entry points
(boot-time display apply, the menu's `F2`×3 reset).

**Exhibition mode (default ON, `GameConfig.exhibitionMode`):** one night, 4 visitors, 1 anomaly with an
honest tell and faster banners (2.2 s), one coaching hint → ~5-8 minutes. Set it false for
the full five-shift campaign.

**Verified here:** compile vs real Unity assemblies 0/0 (minimal + full sets); frozen LogicTests 663;
independent suite 27,336 (fairness, endings, saves, encounter states, display policy, checkpoint
queue); FlowCheck 830 OK / 0 BAD ×5 (button-level desk + queue + settings proofs, patrol world loop).
`Tools/Verification/EngineSmoke` remains frozen-broken by decision. **Not verified here:** pixels,
audio, feel — editor playthrough required (steps in the refactor log).
