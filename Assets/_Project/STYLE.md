# Style contract

Naming: `_camelCase` private fields, `PascalCase` methods and constants, booleans as questions
(`_isWaiting`, `IsIlluminated`). No one-letter fields; the root reference is `_gameRoot`.
Methods do one job and stay under ~30 lines; `Update` reads as an outline. Early returns over nesting.
No nested ternaries, no tuple swaps, no LINQ in gameplay code, no generics in game code.
One public class per file. Members ordered: constants, public fields, private fields, Unity
lifecycle, public methods, private methods, helpers.
Magic numbers become named constants at the top of the file or in a layout class
(`CheckpointLayout` holds every desk position; `GameConfigSO` holds every tunable).

Comment discipline, in order:
1. Comments explain why, never what.
2. No "// Part of the X folder", "// Created by X", "// Helper for Y", nothing restating a path or class name.
3. No comment restating the line of code it sits on.
4. Method headers only when the step sequence is not obvious from the name plus first two lines.
5. Class purpose comments only when the class name alone does not say what it does.
6. When in doubt, no comment. Better code, fewer comments.

Audio: direct `_gameRoot.Audio.PlaySfx("key")` / `PlayMusic("context")`. Never publish sound events.
Events: EventBus only across systems that must not know each other; otherwise call the method.
Subscriptions: collect in `_subscriptions` at Init, dispose in OnDestroy.
Safety nets (`CheckSceneHandshake`, `SafeBuild`, `AspectMatchGuard`) stay; ask before removing any.
New rules go in GameLogic as pure C#; new tunables in GameConfigSO.
