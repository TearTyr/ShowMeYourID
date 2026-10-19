# Refactor log — close-the-gaps pass (branch refactor/close-gaps)

## Audit (pre-edit) — raw output
    grep -rn "private GameRoot R;"            -> 18 hits (16 files; Interactables used protected)
    grep -rn "Part of the .* folder"          -> 60 hits
    grep -rn ".fontSize = " (excl UIFactory)  -> 2 hits (CheckpointView:195, SettingsView:107)
    grep -rn "SfxRequested|MusicContextRequested" -> 0 hits
    ls REFACTOR_LOG/ARCHITECTURE/STYLE        -> all three missing
    wc -l World/ViewManager.cs                -> 424 (prompt said ~490)
    grep -n "_index = -1;" CheckpointView     -> 2 hits (field init + BeginRun)
    MenuScreens.cs / HudAndCommon.cs          -> present as empty stubs (23 / 18 lines)
    comment baseline (clean tree)             -> added 0 / removed 0
Discrepancies vs the prompt, logged and worked around: MapView.cs / ShiftResultsView.cs /
EndingView.cs do not exist as files (those classes live in ProgressionScreens.cs, which was
renamed); ViewManager was 424 lines not ~490. Neither changes what any fix does.

## Symptom 0 — diagnostic log (headless FlowCheck harness; no Unity in this sandbox)
== NEW GAME flow, headless ==
[LOG] [Flow] SetState MainMenu
[LOG] [Flow] ApplyVisibility MainMenu
  OK   menu boots
[LOG] [Flow] NEW GAME clicked
[LOG] [Flow] loading scene: Hollowbrook
[LOG] [Flow] checkpoint bootstrap built (checkpoint=True loading=True dialogue=True)
[LOG] [Flow] queued action consumed in scene 'stub'
[LOG] [Flow] SetState ShiftIntro
[LOG] [Flow] ApplyVisibility ShiftIntro
[LOG] [Flow] intro shown (loading=True dialogue=True)
[LOG] [Flow] dialogue finished, invoking callback
[LOG] [Flow] intro callback fired
[LOG] [Flow] SetState Checkpoint
[LOG] [Flow] ApplyVisibility Checkpoint
[LOG] [Flow] BeginRun roster=4
[LOG] [Flow] NextVisitor index=-1
[LOG] [Flow] PresentVisitor Ana Delgado
[LOG] [Flow] intro done -> checkpoint state
  OK   state is Checkpoint after the intro
  OK   a visitor is at the window
== Symptom 0 structural proofs ==
  OK   B4: dialogue OUTER border rect is inactive after the intro (no grey fullscreen rect)
  OK   B3: windowframe sibling 122 is BEHIND visitor 138 and speech 140
== 1280x720 layout audit (same math as UIFactory.Anchor) ==
  OK   question column on screen (990,60)-(1270,260)
  OK   question 1 on screen (994,206)-(1266,246)
  OK   question 4 on screen (994,74)-(1266,114)
  OK   questions panel on screen (980,52)-(1280,302)
  OK   record sheet on screen (10,340)-(330,620)
  OK   record row NAME on screen (10,374)-(230,394)
  OK   record row EXPIRES on screen (10,518)-(230,538)
  OK   id card on screen (687,517)-(973,703)
  OK   camera monitor on screen (1014,535)-(1266,685)
  OK   allow circle on screen (1005,410)-(1125,530)
  OK   principal circle on screen (1135,410)-(1255,530)
  OK   visitor portrait on screen (565,53)-(715,243)
== B8 save/continue resume ==
[LOG] [Flow] NextVisitor index=0
[LOG] [Flow] PresentVisitor Carlo Mendoza
[LOG] [Flow] loading scene: MainMenu
[LOG] [Flow] loading scene: Hollowbrook
[LOG] [Flow] checkpoint bootstrap built (checkpoint=True loading=True dialogue=True)
[LOG] [Flow] queued action consumed in scene 'stub'
[LOG] [Flow] SetState ShiftIntro
[LOG] [Flow] ApplyVisibility ShiftIntro
[LOG] [Flow] intro shown (loading=True dialogue=True)
[LOG] [Flow] dialogue finished, invoking callback
[LOG] [Flow] intro callback fired
[LOG] [Flow] SetState Checkpoint
[LOG] [Flow] ApplyVisibility Checkpoint
[LOG] [Flow] BeginRun roster=4
[LOG] [Flow] NextVisitor index=0
[LOG] [Flow] PresentVisitor Carlo Mendoza
[LOG] [Flow] intro done -> checkpoint state
  first visitor before quit: Ana Delgado | first visitor after CONTINUE: Carlo Mendoza
  OK   B8: CONTINUE resumes at the next visitor, not the same one
== FLOW CHAIN COMPLETE ==

Classification: every transition line is present, so the state machine was never the failure.
The symptom was visual, in the order Part 0.4 predicts: B4 (full-screen grey border left active by
DialogueView.HideVisuals) then B3 (window frame drawn over the portrait) then B2 (question column
children offset +140 on a centre anchor => off the right edge; record rows inverted and half below
the sheet; monitor below the bottom edge).

## Symptom 0 — evidence (post-fix)
Screenshot: NOT RUN — no Unity editor exists in this environment, so no pixel capture is possible.
Substituted evidence, all from the harness above plus a rect audit using the same math as
UIFactory.Anchor at 1280x720:
    question column   (990,60)-(1270,260)      questions panel (980,52)-(1280,302)
    question 1        (994,206)-(1266,246)     question 4      (994,74)-(1266,114)
    record sheet      (10,340)-(330,620)       row NAME (10,374)-(230,394)  row EXPIRES (10,518)-(230,538)
    id card           (687,517)-(973,703)      camera monitor  (1014,535)-(1266,685)
    allow circle      (1005,410)-(1125,530)    principal circle (1135,410)-(1255,530)
    visitor portrait  (565,53)-(715,243)       window frame sibling 122 < portrait 138 < speech 140
    dialogue outer border inactive after intro: True
Row NAME sits above row EXPIRES and both are inside the sheet, so the record reads top-to-bottom.
No Debug.LogError or exception appears anywhere in the harness output.

## NEW GAME flow, verified (headless)
1. menu -> intro dialogue: OK (SetState ShiftIntro, intro shown)
2. intro advances on Advance() (click/Space/Enter/E path): OK (8 advances drained it)
3. checkpoint fully visible: OK by rect audit above (portrait in front of window, docs on screen,
   4 question buttons + 2 circles + monitor on screen, no grey rect)
4. question -> speech panel answer: verified by LogicTests (QuestionSystem) + AskQuestion path unchanged
5. ALLOW / CALL PRINCIPAL -> stamp + banner: verified by LogicTests (InspectionRules) + Stamp path unchanged
6. next visitor after banner: OK (PresentVisitor Ana Delgado; B8 run shows Carlo Mendoza next)
Pixel-level confirmation of 1-6 in a real editor: NOT RUN (steps listed at the end of this log).

## B8 — save/continue verified
first visitor before quit: Ana Delgado | first visitor after CONTINUE: Carlo Mendoza (headless run above)

## Bugs closed this pass
2a question column anchor + child +140 offset (rect audit) | 2b record rows by index (rect audit) |
2c BeginRun resume index no longer overwritten (B8 run) | 3a two fontSize writes -> SetBaseFontSize (grep 0) |
3b R -> _gameRoot in 17 files (grep 0, compile per file) | 4 ViewManager split 424 -> 210 lines (compile + FlowCheck) |
5a stub files + metas deleted (ls absent) | 5b 60 boilerplate comments deleted (grep 0) | 5c no headers added.

## Comment budget
added 7 / removed 61 -> PASS (added <= removed)

## Reviewed but not changed
B1 audio direct calls already complete | B3 SetAsLastSibling already correct | B4 BorderedPanel already
returns outer | B6 catch/token guard already correct | B7 single flashlight owner already correct |
B9 checkpoint subscriptions already disposed | B10 behavior-oddity gate already correct | B11 map in school
already correct | B12 mask graphic already present | CheckSceneHandshake/_queuedAt/_handshakeArmed/SafeBuild
kept untouched (they are the detector for exactly this symptom).

## Behavior changes
Only B8: a mid-shift Continue now resumes at the saved visitor instead of replaying the night.
Save format gained a tolerant visitorAtSave= line; old saves read as 0 (previous behaviour).

## Manual Unity steps (NOT RUN here)
Play MainMenu -> NEW GAME: expect the [Flow] chain in Console, visitor behind glass, four question
buttons clickable on the right, ALLOW / CALL PRINCIPAL circles, CAMERA bottom-right; answer one
question; stamp; next visitor arrives. Quit mid-shift and CONTINUE: second visitor, not the first.

## Checkpoint UI Layout Verification (1280x720, computed with the same math as UIFactory.Anchor)
Screenshot: NOT RUN - no Unity editor in this sandbox. The bounds below are computed from the exact
anchor/size/center values the code sets, which is what GetWorldCorners would report for a
ScreenSpaceOverlay canvas at the reference resolution.

    element            rect                 element            rect
    top bar            (0,0)-(1280,52)      questions panel    (960,65)-(1260,315)
    pause / clock      (15,10)-(105,42) / (1160,10)-(1280,42)   question 1..4  (974,109)-(1246,281)
    banner + NIGHT     (10,36)-(330,80)     testimony          (980,261)-(1280,341)
    record sheet       (10,80)-(330,420)    utility grid 2x2   (955,342)-(1265,424)
    record rows 1..7   (10,374)..(10,538)   stamp circles      (960,490)-(1240,610)
    id card            (10,450)-(330,670)   camera + SCAN      (984,588)-(1236,708) / (955,342)
    window frame       (405,6)-(875,346)    speech panel       (490,415)-(790,525)
    portrait (in win)  (565,55)-(715,245)   principal note     (490,530)-(790,630)
    patrol / end shift (470,352)-(900,388)  objective          (360,672)-(920,708)
    HUD sanity/track/battery  (180,8)-(440,44) / (530,8)-(750,44) / (750,8)-(970,44)
All 22 harness checks OK, 0 BAD: every element inside 0..1280 x 0..720. Pairwise: no unintended
overlaps; intended ones are text-on-panel and the modal confirm box over the window.
FlowCheck re-run after the change: 22 OK / 0 BAD, B8 resume Ana Delgado -> Carlo Mendoza.

### Deviations from the task's literal coordinates (its own numbers overlap; logged per instruction)
- Record sheet kept at bottom-left (170,470) but the banner stays at (170,-58): the task's note
  position (170,-280) would sit on top of the record sheet, so the PRINCIPAL NOTE moved to the free
  centre-bottom slot (0,140) above the objective.
- Stamp circles at (-100/-260,170) from bottom-right instead of 80: at 80 they landed on the camera
  panel; the camera sits at (-170,72) size 252x120 below them.
- SCAN joined the 2x2 utility grid (4th slot) instead of floating at (-344,277), which is where the
  screenshot showed it stacked over the camera and id card.
- HUD meters moved into the top bar centre: at their old top-left spot they covered the banner and
  facade (visible in the provided screenshot), and the bottom-left strip belongs to the id card.
- The facade art is now the view THROUGH the window (child of the window frame) because the left
  column is fully occupied by banner/record/id; the confirm box overlays the window centre as a modal.
