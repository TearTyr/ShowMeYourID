# Show Me Your ID! - v1.0.6 hotfix 1 + full-source verification

**Date:** 2026-10-18 · **Unity:** 6.3 (6000.3.x), Built-in RP, uGUI legacy, legacy Input Manager
**Source of truth used:** your `source_report.md` scan of `C:/Users/Tinapay/Downloads/ShowMeYourID/Assets`
(107 files, 42 scripts dumped) - the project is now rebuilt here byte-for-byte, so we are looking at the same code.

---

## 1. Replication - proven, not asserted

Your `source_report.md` was parsed and all **42 scripts (9,183 lines)** were written back out to
`ShowMeYourID/Assets/…` at their exact paths. Then a real compiler was set up against the **actual
Unity 2022.3.62f1 assemblies** (`UnityEngine.CoreModule.dll`, `UnityEngine.UIModule.dll`,
`UnityEngine.UI.dll`, … from a public redistributable on nuget.org) - *not* hand-written stubs,
which is precisely what caused this bug.

**Before the fix, the harness reproduced your editor output exactly:**

```
Assets/_Project/Scripts/Core/AspectMatchGuard.cs(22,26): error CS0246: The type or namespace name 'CanvasScaler' could not be found (are you missing a using directive or an assembly reference?)
Assets/_Project/Scripts/Core/AspectMatchGuard.cs(13,17): error CS0246: The type or namespace name 'CanvasScaler' could not be found (are you missing a using directive or an assembly reference?)
    2 Error(s)
```

Same files, same codes, same line **and column** numbers as your Unity console. From here on, anything
I say about compilation is measured, not guessed.

---

## 2. Root cause

`CanvasScaler` is in the **`UnityEngine.UI`** namespace / `UnityEngine.UI.dll` assembly (package
`com.unity.ugui`) - *not* in `UnityEngine`. Confirmed against the real assembly and Unity's docs
([`UnityEngine.UI.CanvasScaler`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.CanvasScaler.html)).

The earlier headless test stubs placed `CanvasScaler` in `UnityEngine`, so `AspectMatchGuard.cs` - the
one new file in v1.0.6 - compiled in the sandbox and passed all 159 smoke tests, then failed in your editor.
Note the same stub-fidelity trap produced the v1.0.1 `Canvas.RenderMode` bug; that one is fixed and
stays fixed (audit verifies `renderMode = RenderMode.…` in `UIFactory.cs` and `ViewManager.cs`).

---

## 3. There were **two** broken files, not one - Unity only showed you the first

Fixing `AspectMatchGuard.cs` alone immediately produces a **new** error:

```
Assets/_Project/Scripts/Bootstrap/CoreBootstrap.cs(90,55): error CS0246: The type or namespace name 'CanvasScaler' could not be found
```

```csharp
// CoreBootstrap.cs:90 - inside NewCanvas()
guard.Init(canvas.gameObject.GetComponent<CanvasScaler>());   // <-- CanvasScaler, no using UnityEngine.UI
```

Why you didn't see it yet: line 90 is an *argument to `AspectMatchGuard.Init(…)`*, and `Init`'s parameter
type was itself the unresolved `CanvasScaler`. Roslyn suppresses cascading errors inside an expression
whose method group already failed to bind - so the second error stays invisible until the first is fixed.
The static audit predicts it independently of the compiler (`uses CanvasScaler @L90 … missing 'using UnityEngine.UI;'`).

**So the fix is two added lines:**

```diff
--- a/Assets/_Project/Scripts/Core/AspectMatchGuard.cs
+++ b/Assets/_Project/Scripts/Core/AspectMatchGuard.cs
@@ -1,5 +1,6 @@
 using System.Collections;
 using UnityEngine;
+using UnityEngine.UI;   // CanvasScaler lives in UnityEngine.UI (com.unity.ugui), not UnityEngine

 namespace Hollowbrook.Core
 {
--- a/Assets/_Project/Scripts/Bootstrap/CoreBootstrap.cs
+++ b/Assets/_Project/Scripts/Bootstrap/CoreBootstrap.cs
@@ -5,6 +5,7 @@
 using UnityEngine;
 using UnityEngine.EventSystems;
 using UnityEngine.SceneManagement;
+using UnityEngine.UI;   // CanvasScaler (used in NewCanvas) lives in UnityEngine.UI

 namespace Hollowbrook.Bootstrap
 {
```

Nothing else in either file was touched - the aspect-ratio logic is correct as shipped (see §5).

---

## 4. Verification after the fix

| Check | Result |
|---|---|
| Compile vs **minimal** reference set (default Unity modules + `UnityEngine.UI` only, 19 assemblies) | **PASS** - 0 errors, 0 warnings |
| Compile vs **full** Unity assembly set (76 assemblies) | **PASS** - 0 errors, 0 warnings |
| Compile with **all warnings enabled** (`NoWarn=` empty, so Unity's console would show them) | **PASS** - 0 warnings |
| Static audit (`Tools/Verify/audit.py`, 42 scripts) | **PASS** - 0 error-risks, 2 informational notes |
| **Executable logic suite** (`Tools/LogicTests`, real `GameLogic` code on .NET) | **PASS - 27,075 / 27,075 assertions**, 1 known display gap (§6) |

The minimal-set build is the meaningful one for your **legacy-only restriction**: it proves no script
needs TextMeshPro, the new Input System, URP, UIElements, Burst, Collections or Mathematics - only
built-in modules + uGUI, exactly what a default Unity 6.3 project has.

The audit was cross-validated against the compiler on a synthetic bad file: it flagged `CanvasScaler`,
`Text` and `Image`, and the real compiler rejected exactly those three (same lines). It also does not
false-positive on members that share a name (`public string Text;`, `new DialogueLine { Text = … }`),
which is why `EventBus.cs`, `GameRoot.cs`, `ContentData.cs`, `InspectionRules.cs`, `RosterFactory.cs`
and `Interactables.cs` are correctly reported clean.

---

## 5. Whole-project check (all 42 scripts, 9,185 lines)

**Wiring the whole UI depends on - all OK:**

| Check | Where | Status |
|---|---|---|
| `EventSystem` created (else every button is dead) | `CoreBootstrap.cs` | OK |
| `StandaloneInputModule` (legacy Input Manager, matches your restriction) | `CoreBootstrap.cs` | OK |
| Canvas built with `Canvas` + `CanvasScaler` + `GraphicRaycaster` | `UIFactory.CreateRootCanvas` | OK |
| `uiScaleMode = ScaleWithScreenSize`, `referenceResolution` set | `UIFactory.cs:55-56` | OK |
| `AspectMatchGuard` attached to every canvas created through `CoreBootstrap.NewCanvas` | `CoreBootstrap.cs:89-90` | OK - and `NewCanvas` is the *only* caller of `CreateRootCanvas`, so there is no un-guarded canvas |
| `renderMode = RenderMode.…` (top-level enum, the v1.0.1 bug) | `UIFactory.cs`, `ViewManager.cs` | OK |
| `[RuntimeInitializeOnLoadMethod]` safety net → routes a bootstrap-less scene to the menu | `CoreBootstrap.cs:97` | OK |
| Font: `LegacyRuntime.ttf` tried **first**, `Arial.ttf` only as fallback | `UIFactory.GameFont` | OK for Unity 6 |
| `DontDestroyOnLoad` on core/audio/EventSystem/camera | `CoreBootstrap.cs`, `GameRoot.cs` | OK |
| `SceneRouter` names (`MainMenu`, `Hollowbrook`, `School`) vs. the 3 scene assets in your tree | `CoreBootstrap.cs:16-18` | OK - all three exist |
| Reference-resolution consistency: `GameConfigSO.canvasReference 1280×720` vs `AspectMatchGuard.ReferenceAspect 1280/720 = 1.7778` | - | OK - guard picks the right match axis |
| Removed/obsolete-in-Unity-6 APIs (`WWW`, `MovieTexture`, `GUIText`, `Application.LoadLevel`, `FindObjectOfType(s)`, `Handheld`) | all scripts | none used |
| Forbidden by your constraints (TMPro, new Input System, URP, UIElements, `GameObject.Find`) | all scripts | none used |

**Aspect-ratio logic reviewed line by line** (`AspectMatchGuard.cs`, 54 lines) - it is correct:
`matchWidthOrHeight = aspect > 16:9 ? 1 : 0` with `ScaleWithScreenSize` means the constrained axis lands
exactly on the reference and the other axis expands (1920×1080 → 1280×720; 2560×1080 → 1680×720;
1024×768 → 1280×960). `Update()` re-applies only when `Screen.width/height` change, and `VisibleReferenceSize()`
matches that math. No change needed beyond the `using`.

**Two informational notes (not bugs, no action required):**
1. `UIFactory.CreateRootCanvas` never sets `scaler.screenMatchMode`, so it relies on Unity's default
   (`MatchWidthOrHeight`) - which is exactly what `AspectMatchGuard` assumes. Setting it explicitly would
   be self-documenting; behaviour is identical today.
2. `Resources.GetBuiltinResource<Font>("Arial.ttf")` remains as a *fallback*. `Arial.ttf` no longer ships
   in Unity 6, so that line can log an error **only if** `LegacyRuntime.ttf` also failed - on Unity 6.3 it
   never reaches it.

**Housekeeping observations from your tree (cosmetic):**
- `AspectMatchGuard.cs` is the only script **without a `.meta`** - consistent with it being the file added
  in v1.0.6 and the report being generated before Unity imported it. Unity generates one on next import; harmless.
- Three **empty script folders** still carry `.meta` files: `Scripts/Data`, `Scripts/Exploration`,
  `Scripts/Progression` - leftovers from the earlier refactor. Safe to delete in the editor.
- `Assets/cs.py` (your source-dump scanner) sits **inside `Assets/`**. Unity imports it as a text asset -
  harmless, but it belongs in `Tools/` so it never ships or slows an import.
- Your tree accounts for exactly 107 files: 42 `.cs` + 41 `.cs.meta` + 3 scenes + 3 scene `.meta` +
  `GameConfig.asset` + its `.meta` + 15 folder/`_Project` `.meta` + `cs.py`. Nothing is missing or orphaned.

---

## 6. Executable logic suite - 27,075 assertions, actually run

`Assets/_Project/Scripts/GameLogic/` is engine-free by design (its own `Enums.cs` header says so), so it
can be **executed** on plain .NET - no Unity, no stubs. `Tools/LogicTests/` does exactly that:

```
dotnet run --project Tools/LogicTests/LogicTests.csproj
```

```
== IdRules - HB-YYYY-NNNN format ==                        == EndingRules - Win / Lose / Secret priority ==
== Content - approved constants & roster ==                == GameSession - counters, sanity, shifts ==
== ShiftPlan - 4/6/7/8/8+admin progression ==              == ClueTracker - sequence gates the secret ==
== ClueRegistry - the ten environmental clues ==           == SaveFormat - round-trip + tamper detection ==
== RosterFactory - fairness over 60 seeds x 5 shifts ==    == SanityModel / Endless / Challenge maths ==
== Shift-5 administrator encounter ==                      ============================================
== QuestionSystem - max 2 questions ==                       PASS 27075   FAIL 0   TOTAL 27075
== InspectionRules - verdict matrix ==                        known display gaps (not failures): 5
```

2,040 visitors were generated (1,200 legitimate, 780 anomalies) and every one was checked against the
rules the game itself uses. Highlights of what is now machine-verified on your actual code:

- **Fairness:** every legitimate visitor has zero defects, zero objective discrepancies, matching
  ID/record fields, non-contradicting answers, `Tone == Normal`, a valid registered ID, and is never
  expired → always `Allow`. Every anomaly carries ≥1 defect, respects the shift's defect cap, never uses
  a defect type before its shift unlocks it, and is **always detectable** (documents, duplicate evidence,
  contradicting answer, or non-Normal tone) → always `Reject`.
- **Progression:** 5 shifts, 4/6/7/8/8 visitors, 0/2/3/4/4 anomalies, administrator only on Night 5
  (appended → 9 visitors), subtle/duplicate/behavior defects unlock on Night 3, only Night 4 is
  investigation-focused.
- **Determinism:** same seed → byte-identical roster (saves regenerate correctly); different seed →
  different roster; `GameRng` stream reproducible; shuffle preserves every element.
- **Administrator (conflict C3):** badge fails the student-ID format, is not in the registry, name and
  number disagree with the memo; `EvaluateDocuments` uses the admin special case; allowing it sets
  `AdminAllowed` → immediate Lose; rejecting it recovers 10 sanity.
- **Endings (conflict C4):** Lose > Secret > Win. 3 breaches or 3 false rejections or an allowed
  administrator ⇒ Lose, and Lose outranks a fully-completed secret run. Five clean shifts ⇒ Win; all ten
  clues in order ⇒ Secret; out-of-order discovery permanently breaks the secret.
- **Question cap (conflict C1):** exactly 2, repeats refused without consuming budget, reset works.
- **Verdict matrix:** correct / false-rejection / breach flags, plus Challenge rule mutations - and a
  rejection forced by an official mutation is *not* counted as a false rejection.
- **Save integrity:** full round-trip of session, settings (incl. accessibility toggles), records,
  endings seen, shift history, clue discoveries and the presentation log; tampering with one value,
  dropping the checksum, changing the header or the version ⇒ rejected.
- **Maths:** sanity stage boundaries (70/40/15), clamping, Challenge 2× drain, hallucination escalation,
  endless anomaly ramp capped at 0.6, streak multiplier capped at 4×, timer hard cap.

### The one gap it found (real, low severity, optional fix)

`RosterFactory.Scan()` returns early when an ID is **not in the student registry**, *before* computing
`DuplicateTonight`:

```csharp
if (!r.FoundInDatabase)
{
    r.Display = "NOT FOUND - NO STUDENT RECORD FOR " + v.IdNumber;
    return r;                     // <-- DuplicateTonight never computed
}
```

So when a "ghost" ID (unregistered) is presented twice in one night with different cards, the scanner
shows only `NOT FOUND` and omits the `⚠ ALREADY PRESENTED … WITH A DIFFERENT CARD` warning - while
`IdRules.EvaluateDocuments` and the post-verdict results banner *do* report the duplicate.

- **Impact:** informational only. The visitor is still trivially catchable (`NOT FOUND` is sufficient
  evidence), the verdict and all counters are correct, and no player is misled into a wrong call.
- **Frequency:** 5 occurrences in 2,040 generated visitors (0.25 %) - Nights 4–5 only, where duplicate
  IDs are unlocked.
- **Optional fix:** `docs/optional-scanner-duplicate-fix.patch` moves the duplicate/returner computation
  above the early return and appends the warning to the `NOT FOUND` line. Verified: compiles clean
  against the real assemblies, logic suite stays **27,075 / 27,075**, and the known-gap count drops
  **5 → 0**. Not applied by default, so the hotfix you install stays exactly two lines.

### Two latent notes (no action needed)

1. `Content.DayNumberOfDate` uses a 360-day-year approximation, so `2026-12-31` and `2027-01-01` both map
   to day 1060. No generated content lands in that window (expired IDs are Jun–Oct 2026, near-expiry IDs
   display Nov 20 – Dec 9 2026, far expiry is 2027-03-31), and `DisplayOf` re-derives real calendar dates,
   so no impossible date is ever shown. The suite asserts the collision explicitly so anyone extending the
   date range sees it immediately.
2. `strict_expiry` was verified to actually fire: endless/challenge runs do produce near-expiry legitimate
   IDs (14–38 days out, inside the 60-day window), so the mutation is not dead code.

---

## 7. What I still cannot replicate (and how to close the gap)

Your scan covered `Assets/` and only `.cs`/`.hlsl`. Not available here, therefore **not verified**:

| Missing | Why it matters |
|---|---|
| `Tools/` (your logic + engine smoke suites: 185 / 159 tests) | I cannot re-run *your* suites. I wrote an independent one for the engine-free logic layer (27,075 assertions, §6), but the **engine smoke tests** (bootstrap, view order, overlap audit) need Unity or those stubs |
| `Packages/manifest.json` | Cannot confirm the exact package set / `activeInputHandler` |
| `ProjectSettings/*` (esp. `EditorBuildSettings.asset`) | Cannot confirm the leftover `Scenes/SampleScene` is gone or the scene order |
| `Assets/_Project/Scenes/*.unity` | Cannot inspect the bootstrap objects actually placed in each scene |
| `Assets/_Project/Resources/GameConfig.asset` | Only the C# defaults (1280×720) were visible, not the serialized overrides |
| `.meta` files / GUIDs | Irrelevant for code, relevant if a scene references a script by GUID |

**To make me 100% equivalent to your machine**, re-run your scanner from the project root instead of
`Assets/`, and add `.asset`, `.unity`, `.json`, `.txt`, `.asmdef` to the dumped extensions:

```
scan root : C:/Users/Tinapay/Downloads/ShowMeYourID      (not ...\Assets)
extensions: .cs .hlsl .unity .asset .json .asmdef .txt .md
skip      : Library/ Temp/ obj/ Logs/ .git/ UserSettings/
```

With `Tools/` included I can re-run your suites here and report 185/185 · 159/159 instead of only
"compiles + logic verified".

---

## 8. Deliverables

| File | What it is |
|---|---|
| `Assets/_Project/Scripts/Core/AspectMatchGuard.cs` | **fixed** (1 line added) |
| `Assets/_Project/Scripts/Bootstrap/CoreBootstrap.cs` | **fixed** (1 line added) |
| `Tools/Verify/` | replication harness - `README.md`, `verify.sh`, `Assembly-CSharp.csproj`, `fetch_unity_libs.py`, `audit.py`. Compiles every script against **real Unity assemblies** |
| `Tools/LogicTests/` | executable logic suite - `LogicTests.csproj`, `Program.cs` (27,075 assertions). Run with `dotnet run` |
| `Tools/Directory.Build.props` | keeps build artifacts out of the repo |
| `docs/HOTFIX-v1.0.6.1.md` | this report |
| `docs/optional-scanner-duplicate-fix.patch` | the optional §6 scanner fix (verified, not applied) |
| `docs/v1.0.6-hotfix1.patch` | the 2-line unified diff |
| `ShowMeYourID_Unity6.3_v1.0.6-hotfix1.zip` | everything above, at real paths - extract over your project root |

Everything under `Tools/` and `docs/` is **outside `Assets/`**, so Unity never imports, compiles or ships it.

### How to apply (pick one)

**A. Two-line edit (fastest, keeps your `.meta` files and GUIDs):** add
`using UnityEngine.UI;` to the top of `Assets/_Project/Scripts/Core/AspectMatchGuard.cs` **and**
`Assets/_Project/Scripts/Bootstrap/CoreBootstrap.cs`. Save → Unity recompiles → console clean.

**B. Hotfix zip:** extract over `…/ShowMeYourID/` and overwrite. Only the two scripts plus the new
`Tools/` and `docs/` folders are touched; no `.meta`, scene or setting is replaced.

**C. Patch:** `git apply docs/v1.0.6-hotfix1.patch` from the project root (the file is inside the zip).

Then: delete/uncheck `Scenes/SampleScene` in Build Settings (order `MainMenu` 0, `Hollowbrook` 1,
`School` 2), press Play in `MainMenu.unity`, and run the aspect check from the previous report.

**Expected in the editor after the fix:** 0 errors. If Unity reports anything else, paste it verbatim -
I can now reproduce it here against real assemblies before answering.
