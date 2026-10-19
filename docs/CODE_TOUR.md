# Code tour — read these 10 files in this order

1. `Assets/_Project/Scripts/Core/GameRoot.cs` — the one object that lives across scenes: holds the
   session, the current shift, the state enum, and opens/closes overlay screens. Start here.
2. `Assets/_Project/Scripts/Bootstrap/MenuSceneBootstrap.cs` — how a scene builds itself in code
   (no Inspector wiring anywhere in this project).
3. `Assets/_Project/Scripts/UI/MenuScreens.cs` — main menu, Options, Controls, Credits, Pause.
4. `Assets/_Project/Scripts/UI/UIFactory.cs` — every button/label/slider/panel is made here, so the
   whole game shares one look and one type scale.
5. `Assets/_Project/Scripts/Checkpoint/CheckpointView.cs` — the desk: visitor window, papers,
   questions, ALLOW / CALL PRINCIPAL, stamps and banners.
6. `Assets/_Project/Scripts/GameLogic/RosterFactory.cs` — seeded generation of visitors; the fairness
   rule "real students are clean, anomalies are always catchable" lives here.
7. `Assets/_Project/Scripts/GameLogic/InspectionRules.cs` — who should be allowed, and what counts as
   a complaint or a breach.
8. `Assets/_Project/Scripts/World/ViewManager.cs` — rooms, doors, transitions, anomaly spawning.
9. `Assets/_Project/Scripts/World/FlashlightRig.cs` — cursor beam, darkness mask, battery.
10. `Assets/_Project/Scripts/Horror/WanderingAnomaly.cs` — one script, one switch: four evasion
    patterns plus the detect/expose/repel/recover encounter.

## What each folder does
- `Core/` — long-lived services: root, events, saves, settings, resolution guard.
- `GameLogic/` — pure C# rules (no Unity): rosters, ID rules, endings, shifts, saves format, tracking.
- `Bootstrap/` — one script per scene that builds that scene, plus all procedural art/audio.
- `UI/` — menus, HUD, dialogue, captions, the UI factory.
- `World/` — player, rooms, lights, flashlight, security office.
- `Horror/` — anomaly, sanity, scares, hallucinations.
- `Checkpoint/` — the desk screen.
- `Tools/` — verification harnesses (not part of the game build; `Tools/Verification` is frozen).
