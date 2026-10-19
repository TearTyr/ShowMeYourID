# Style contract (short version)

- One public class per file. One-line `//` purpose comment above every class, saying who creates it.
- Names: `_camelCase` fields, `PascalCase` methods, booleans read as questions (`_isWaiting`).
  No one-letter fields (`R` is gone; use `_gameRoot`).
- Methods <= ~30 lines, one job each; `Update` reads as an outline. Early returns over nesting.
- No nested ternaries, no tuple swaps, no LINQ in gameplay code; `foreach` and temp variables.
- Magic numbers: named constants at the top of the file or in a layout class (see CheckpointLayout).
- Comments say WHY in one line; method header lists steps when the method is over ~15 lines.
- Audio: direct `_gameRoot.Audio.PlaySfx("key")` / `PlayMusic("context")`. Never event-publish sound.
- Events: EventBus only when two systems genuinely do not know each other; otherwise call the method.
  Subscribe in Init, dispose in OnDestroy, keep the list in `_subscriptions`.
- Safety nets stay: `CheckSceneHandshake`, `SafeBuild`, AspectMatchGuard. Ask before removing any.
- New tunables go in GameConfigSO; new rules go in GameLogic (pure C#, no UnityEngine).
