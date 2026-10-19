# Show Me Your ID! — how the game plays *right now* (verified state)

**Loop:** Main Menu → NEW GAME → night intro (click to advance) → **checkpoint desk** ⇄ **patrol**
(hallway + 2 classrooms) → shift results → next night → ending. Exhibition build = one curated night.

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
(3 breaches or 3 complaints) / The Truth Behind Hollowbrook (10 clues in order). Exhibition night closes
with its own demo card and never touches story records.

**Exhibition mode (default ON, `GameConfig.exhibitionMode`):** one night, 4 visitors, 1 anomaly with an
honest tell, faster banners (2.2 s) and gaps (1.0 s), one coaching hint → ~5-8 minutes. Set it false for
the full five-shift campaign.

**Verified here:** compile vs real Unity assemblies 0/0; LogicTests 663; EngineSmoke 201; independent
suite 27,125 (fairness, endings, saves, encounter states, display policy). **Not verified here:** pixels,
audio, feel — editor playthrough required (steps in the last report).
