# Tools/Verify — editor-equivalent compile harness

This folder exists because of one bug: `AspectMatchGuard.cs` compiled fine against
hand-written Unity API **stubs** but failed in the real editor with

```
error CS0246: The type or namespace name 'CanvasScaler' could not be found
```

The stubs had put `CanvasScaler` in `UnityEngine`. In real Unity it is in
**`UnityEngine.UI`** (assembly `UnityEngine.UI.dll`, package `com.unity.ugui`).
Stubs can never be trusted for this; real assemblies can.

So this harness compiles every script under `Assets/` against the **actual Unity
assemblies** (`UnityEngine.CoreModule.dll`, `UnityEngine.UIModule.dll`,
`UnityEngine.UI.dll`, …) pulled from a public redistributable of Unity
**2022.3.62f1** — the version this project originally targeted, and API-identical
for everything it uses.

Result: *if it compiles here, it compiles in your editor.* Verified both ways —
the harness reproduces the original two CS0246 errors at the exact same
line/column positions Unity reported, and reports 0 errors / 0 warnings after the fix.

> These files live outside `Assets/`, so Unity ignores them completely:
> no `.meta` files, no compilation, no import cost.

## Run it

```bash
bash Tools/Verify/verify.sh                    # uses ./Assets
bash Tools/Verify/verify.sh /path/to/Assets    # or point it anywhere
```

Requirements: python3, and any .NET SDK (6/8/9) purely as a Roslyn host — it does
**not** replace Unity. `verify.sh` prints the one-time install command if missing.

## What each step does

| Step | File | Purpose |
|---|---|---|
| fetch | `fetch_unity_libs.py` | downloads the real Unity assemblies to a scratch dir (`/tmp/unitylibs*`). Nothing is committed — Unity's assemblies must not be redistributed |
| compile A | `Assembly-CSharp.csproj` | `-p:UnityLibs=/tmp/unitylibs_min` → **minimal** reference set: default Unity modules + uGUI. Proves the project needs no extra package (legacy-only claim) |
| compile B | `Assembly-CSharp.csproj` | full 76-assembly set → widest coverage, catches anything the minimal set would miss |
| audit | `audit.py` | heuristic net for the things a compiler can't judge: using-directive fidelity, nested-type misuse (`Canvas.RenderMode`), Unity 6 API removals, legacy-only compliance, and the canvas / CanvasScaler / EventSystem / font / scene-name wiring |

`audit.py` was cross-validated against the compiler on a synthetic bad file: it
flagged `CanvasScaler`, `Text`, `Image` and the compiler rejected exactly those
three — and it does **not** false-positive on members that merely share a name
(`public string Text;`, `new DialogueLine { Text = ... }`).

### Manual invocation

```bash
export DOTNET_ROOT=/tmp/dotnet PATH=/tmp/dotnet:$PATH
python3 Tools/Verify/fetch_unity_libs.py /tmp/unitylibs_min
dotnet build Tools/Verify/Assembly-CSharp.csproj -v q \
    -p:GameSrc=$PWD/Assets -p:UnityLibs=/tmp/unitylibs_min
python3 Tools/Verify/audit.py Assets
```

## Running the logic suite (executable, no Unity)

`Assets/_Project/Scripts/GameLogic/` has no engine dependency, so those rules can be **executed** on
plain .NET — see `Tools/LogicTests/`:

```bash
dotnet run --project Tools/LogicTests/LogicTests.csproj
#  PASS 27075   FAIL 0   TOTAL 27075
```

It generates 2,040 visitors over 60 seeds × 5 shifts and asserts the fairness invariants (legit =
zero defects ⇒ Allow; anomaly = ≥1 defect and always detectable ⇒ Reject), the 4/6/7/8/8+admin
progression, shift-gated defect unlocks, the ≤2 question cap, the administrator encounter, ending
priority (Lose > Secret > Win), save round-trip + tamper rejection, clue-sequence gating and the
sanity/endless/challenge maths. Add `-- --debug-dup` for a focused dump of duplicate-ID evidence.

Point it at another tree with `-p:GameLogicSrc=/path/to/Assets/_Project/Scripts/GameLogic`.

## Known limits (be honest about these)

1. **Compile-only.** No Unity runtime here, so Play-mode behaviour, layout pixels
   and builds are still verified in the editor. This harness replaces *stub*
   guessing, not the editor.
2. **Assemblies are 2022.3.62f1**, not 6000.3.x — no public redistributable of
   Unity 6 managed assemblies exists. Unity 6 removals are covered by `audit.py`'s
   `UNITY6_RISK` rules instead (Arial.ttf, FindObjectOfType, WWW, …).
3. **Only `Assets/**.cs` is compiled.** Editor scripts, asmdefs and the `Tools/`
   test suites are separate compilation units; add a second `.csproj` if you want
   them covered.
4. Scenes, `ProjectSettings/`, `Packages/manifest.json` and `.meta` files are not
   checked — they aren't source. `audit.py` verifies `SceneRouter`'s scene names
   against `Assets/_Project/Scenes/*.unity` when those files are present.
