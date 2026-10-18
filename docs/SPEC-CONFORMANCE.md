# Show Me Your ID! — Master-prompt conformance report

**Spec:** `ShowMeYourID_MasterPrompt.md` (the game direction / technical spec)
**Code:** all 42 scripts from your `source_report.md` scan (9,185 lines), compiled and partially executed here
**Method:** every row below was checked by grepping/compiling/running the real code — file:line evidence, no memory.

**Headline:** the *gameplay contract* in the spec is implemented and machine-verified (5 shifts, 4/6/7/8/8+admin,
10 clues in order, 3 endings with the exact titles and conditions, ≤2 questions, permanent decisions,
false-reject/breach counters, seeded determinism). The *architecture* deviates in four deliberate ways
(3 scenes instead of 1, code-generated data instead of ScriptableObjects, no custom shaders, no
tracking/token layer). Those are listed in the deviation register with a decision for each.

---

## 1. Hard technical constraints (§HARD TECHNICAL CONSTRAINTS)

| # | Constraint | Status | Evidence / note |
|---|---|---|---|
| 1 | Unity 2D, Built-in RP, 16:9, C# .NET Standard 2.1 | ✅ (one number differs) | Built-in RP, no URP/TMP/new Input System anywhere (audit-verified). Compiles at C# 9 / netstandard2.1. **Reference resolution is 1280×720, not 1920×1080** → **D1** |
| 2 | ONE scene only; never `SceneManager` for gameplay | ❌ deliberately | 3 scenes (`MainMenu`, `Hollowbrook`, `School`) + `SceneRouter`/`SceneManager.LoadScene` (`CoreBootstrap.cs:14-25`) → **D2** (you asked for Scene A/Scene B in v1.0.4) |
| 3 | Everything wired by a bootstrap; no hand-wiring; no `GameObject.Find` | ✅ | Per-scene bootstraps + `CoreBootstrap`; audit finds **zero** `GameObject.Find` calls |
| 4 | Decouple with an EventBus (`EventIds` + payload) | ✅ (shape differs) | `EventBus.Publish/Subscribe` + 33 `EventIds` constants; `GameEvent` is a plain payload, not a ScriptableObject channel → **D3** |
| 5 | Fallback chains everywhere; never crash or magenta | ✅ | `GameConfigSO.LoadOrDefault` (`GameConfigSO.cs:44-56`), `ProceduralArt.cs:25`, `ProceduralAudio.cs:21`, `UIFactory.GameFont` |
| 6 | Zero required image files; procedural `Texture2D` | ✅ | `ProceduralArt.cs` (922 lines, ~37 keys), `ProceduralAudio.cs` (356 lines) |
| 7 | Data lives in ScriptableObjects (`VisitorSO`, `AnomalySO`, `NightConfig`, `CueSO`) | ❌ | Only **one** SO exists: `GameConfigSO` (`[CreateAssetMenu]`). Visitors/nights/cues are code-generated → **D5** |
| 8 | No magic strings, `[SerializeField]`+tooltips, `///` summaries, seeded determinism | ✅ mostly | `EventIds` constants, `[Tooltip]` on `GameConfigSO` tunables, XML summaries throughout, `GameRng(seed)` (determinism test-verified). Most tunables live in `GameConfigSO` rather than per-component `[SerializeField]` |

---

## 2. Feature spec — all 12 areas

| § | Feature | Status | Evidence |
|---|---|---|---|
| 1 | **Security checkpoint (TNMN core)** | ✅ 95% | `CheckpointView.cs` (883 lines): ID + record cross-check, photo compare, SCAN, checklist, stamps (`StampPunch` L721), result banner (`ShowBanner` L702), portrait fade-in, glitch hint (L552). Verification fields all present: idNumber+format, photo, expiration, yearLevel, course, status Regular/Transferee (`IdRules.EvaluateDocuments`). 4 standing questions, 1–2 cap (`QuestionSystem.All`, `Content.MaxQuestionsPerInspection = 2`) — **test-verified**. Permanent decisions + `FalseRejections`/`Breaches` — **test-verified**. Flow is coroutine-driven rather than a named FSM enum → **D19**; the "1 discrepancy = doubt, 2+ = reject" scoring rule is not used → **D9** |
| 2 | **Hallway + classrooms (side-scroll)** | ✅ | `PlayerController.cs` walk/run/interact (`GameConfigSO.walkSpeed 3.4 / runSpeed 6.0 / interactRange 1.35`), `ViewManager.cs` rooms + transitions + **watchdog** (L27 comment cites master-prompt §6B), `Interactables.cs` doors/inspectables. `ShutterTransition` is a fade (`ViewManager.cs:52 TransitionFade`) → cosmetic rename |
| 3 | **Anomalies + evasion + tracking** | ⚠️ partial | `WanderingAnomaly.cs` (344 lines) with all **four** evade behaviors (Blink/Slide/Sprint/CoverHug), `LightController.cs` Off/Sweep/Focus + F-toggle (L11,36,49), Focus reveals CoverHug (L296,316), "observed = lights on or inside the cone" (L172). **Missing:** `TrackingSystem`/`TrackParamsSO` tracking meter, silhouette haze→ink solidification, `TokenManager`/`TokenView` → **D10** (the biggest gameplay gap vs spec) |
| 4 | **Sanity + dread + hallucination** | ✅ (numbers differ) | `SanityController.cs` stage visuals + heartbeat + vignette/distortion (L101-128), `HallucinationSystem.cs`, `ScareSystem.cs` with reduced-motion/photosensitivity guards. Stage names/thresholds are Stable/Uneasy/Shaken/Breaking at 70/40/15 vs spec Calm/Neutral/Danger/Hopeless at 76/41/16, and there is no "composure shield" → **D11** |
| 5 | **5 shifts + escalation** | ✅ **test-verified** | `ShiftPlan.Build()`: 4/6/7/8/8 visitors, 0/2/3/4/4 anomalies, admin only on Night 5, defect cap 1→2, subtle/duplicate/behavior defects unlock Night 3, Night 4 investigation-focused |
| 6 | **10 clues + secret ending** | ✅ **test-verified, exact** | `ClueRegistry.Build()` keys in order: `light, chalkboard, desk, door, window, board, lostid, photo, cabinet, clock` — the spec's ten, in the spec's order, spanning Hallway + Classroom 1 + Classroom 2 |
| 7 | **3 endings** | ✅ **test-verified** | Titles verbatim at `ProgressionScreens.cs:230-232`: `SURVIVING HOLLOWBROOK` / `ANOTHER CUSTODIAN` / `THE TRUTH BEHIND HOLLOWBROOK`. Conditions in `EndingRules.Evaluate`: Win = 5 shifts + admin rejected + <3 false rejects + <3 breaches; Lose = 3 breaches OR 3 false rejections OR admin allowed; Secret = 10 clues in order + all three rooms. Priority Lose > Secret > Win |
| 8 | **Administrator boss (Shift 5)** | ✅ **test-verified** | `RosterFactory.BuildAdministrator` (L356): `IsAnomaly=true`, staff badge `DA-0233` fails student-ID format, not in registry, name/number disagree with the memo, distorted window portrait, TooSmooth/Flat/Echoing tones. Rejecting is required for Win; allowing = immediate Lose. Twist beat = `GlitchHint` coroutine (no shader invert-pulse → **D4**) |
| 9 | **Modes (Normal/Endless/Challenge)** | ✅ (menu-hidden by request) | `GameMode` enum, `EndlessRules` (clock, +time per correct call, anomaly ramp to 0.6, score, streak ×4 cap), `ChallengeRules` (4 mutations, ×2 score, ×2 sanity drain) — all **test-verified**. Hidden from the main menu per your v1.0.6 instruction, behind `GameRoot.RunModesUnlocked` |
| 10 | **Map UI** | ⚠️ partial | `ProgressionScreens.cs:34-38`: Security, Main Hallway, Classroom 1 (Room 101), Classroom 2 (Room 202), and a sealed `SERVER / IDENTIFICATION ROOM`. Spec also lists office, records room, staircase, restroom, emergency exit as locked/lore rooms → **D17** |
| 11 | **Menus / settings / pause / save** | ✅ | MainMenu (left FNAF column), Settings/Options, Controls, Credits, Pause, Confirmation popups, Loading, Ending/Results. Save: `SaveSystem` + `SaveFormat` with checksum — full round-trip and tamper rejection **test-verified**. Settings implemented: Master/Music/Effects volume, Text size, Captions, Reduced motion, Photosensitivity-safe. Spec's `sanityMultiplier`, `steadyHands`, `crtToggle`, `dyslexiaFont` are absent → **D12**. Logo/tagline removed per your v1.0.6 instruction |
| 12 | **Audio** | ✅ (shape differs) | `AudioDirector.PlaySfx(key, volume)` + `PlayMusic(context)` with crossfade, caption table for accessibility, settings-driven volumes. 11 music contexts incl. `music_menu`, `music_darkroom`, `music_classroom`, `music_anomaly`, `music_end_win/lose` (`ProceduralAudio.cs:160-169`). `CueSO` → caption dictionary; `AmbientAudio` + ducking not implemented → **D18** |

---

## 3. Data schemas (spec §DATA SCHEMAS vs. actual)

| Spec type | Actual | Notes |
|---|---|---|
| `VisitorSO` | `VisitorData` (plain class, `ContentData.cs:81`) | All spec fields present as data: portrait, name, yearLevel, course, idNumber, expiration, status, answers[4], isAnomaly, tells → `Defects`, `BehaviorGlitchVisible`. Not an SO; generated deterministically by `RosterFactory` |
| `AnomalySO` | `DefectType` + `AnomalyBehaviorKind` enums + `WanderingAnomaly` | `threatTier`, `pay`, `silhouetteSprites[]`, `tokenSprite`, `darkOnly`, `faceCheckRequired` have no equivalent (token/tracking layer absent → **D10**) |
| `NightConfig` | `ShiftDef` (`ContentData.cs:154`) | `shiftNumber, studentCount, anomalyCount, tellSubtlety→MaxDefectsPerAnomaly/SubtleDefectsAllowed, ruleChanges→RuleNoteAddendum, cluesAvailable→ExplorationFocus, isAdministratorShift→HasAdministrator`. Missing: `sanityDrainMultiplier` (global in `SanityModel` instead) |
| `CueSO` | `AudioDirector.Captions` dictionary | `clip` is procedural by key; `caption` implemented; `positional`/`gainInLookDown` not |
| `SaveData` | `SaveGame { Session, Settings, Records }` | Present: shift, mode, falseRejects, breaches, archiveFlags → clue keys **in order**, endingsSeen, settings. Missing: `rank`, `museum[]`, `todayBest{date,amount}` → **D13** |

---

## 4. Art / asset strategy

| Spec | Actual | Verdict |
|---|---|---|
| 5-tone ramp: ink `#0A0A0A`, dark `#33353A`, mid `#777A80`, light `#C6C8CC`, paper `#ECEAE2` | `UIFactory.cs:15-25`: Bg `#09090A`, PanelBase `#17171A`, PanelAlt `#242426`, Border `#4D4D52`, InkDim `#9E9E99`, Ink `#EBEBE6`, Paper `#D1CFC4` | Same *structure* (monochrome ramp), **darker overall** — consistent with the "deeper negative space" horror pass you asked for in v1.0.3. → **D16** |
| One red accent `#C0392B`, interactives only | `Accent #B3212B` + `AccentBright #E63842` (L22-23), used for interactives/danger | ✅ spirit, slightly darker red |
| World/characters at **640×360**, point filtering, integer ×3 to 1080p | Backgrounds 1536×640 / 1152×640 / 1280×720; props 112–384 px; `filterMode = Bilinear` (`ProceduralArt.cs:137`) | ❌ different pipeline → **D15**. Bilinear is right for *documents* (spec agrees) but world art loses the crunchy pixel look |
| UI/documents at 1920×1080 native | 1280×720 reference space | → **D1** |
| `PlateProvider` + `IPlateProvider`/`IPortraitProvider`, file convention `ctx_subject_variant_state.png` | `Resources.Load<Texture2D>("Art/" + key)` → procedural fallback (`ProceduralArt.cs:25`); audio likewise (`ProceduralAudio.cs:21`) | ✅ same *outcome* (drop files in, zero code changes), simpler mechanism → **D14** |
| Shaders add grain/haze/distortion (`FilmParams`) | No shaders at all; FX via uGUI overlays/colors | → **D4** |

---

## 5. Deviation register (with a decision for each)

| ID | Deviation | Severity | Recommendation |
|---|---|---|---|
| **D1** | Canvas reference 1280×720 instead of 1920×1080 (aspect identical; `matchWidthOrHeight` driven by `AspectMatchGuard` instead of fixed 0.5) | Low | **Keep 720.** Every layout constant in the project is in 720-space and is overlap-audited there; moving to 1080 means re-laying every screen for zero visual gain. Update the master prompt to say 1280×720 |
| **D2** | 3 scenes + `SceneManager` instead of one scene with swappable roots | Medium | **Keep** — you explicitly asked for Scene A (desk) and Scene B (school/security room). Update the master prompt so a future agent doesn't "fix" it back and break your build |
| **D3** | `GameEvent` is a plain payload + `EventBus.Publish`, not an SO channel; event names renamed (`SaveWritten`→`GameSaved`, `ClueFound`→`ClueDiscovered`, `ClockTick`→`RunTimerTick`, `ViewEntered/Exited`→`RoomEntered/Exited`, `DeskAllowed/Rejected/WrongReject`→`VerdictStamped/VisitorResolved`); frame/hotspot events absent because that subsystem doesn't exist | Low | **Accept.** Internal contract only; nothing external depends on the names |
| **D4** | No `RC/Frame` + `RC/World` shaders, no `RCShaderLib`, no `FilmParams`; grade/grain/haze/invert-pulse are uGUI-overlay based | **Medium-high** | **Decide.** This is the spec's analog-horror look. Cheapest path: keep code FX for the deadline, then add one fullscreen grade shader (grain + vignette + `_Sanity` warp + invert-pulse) driven by the existing `SanityStage` — it slots in without touching gameplay |
| **D5** | Only `GameConfigSO` is a ScriptableObject; visitors/nights/cues are code-generated | Medium | **Keep for now** (determinism + zero-asset shipping is why the zip is 200 KB). Add SO authoring only if a designer needs to edit content without a programmer |
| **D6** | Folder taxonomy differs (8 folders vs the spec's 15); `Data/`, `Exploration/`, `Progression/` exist but are **empty** (leftover `.meta` only) | Cosmetic | Delete the three empty folders in the editor, or update the spec's tree to the real one |
| **D7** | No `Editor/AutoBootstrap.cs` (`[InitializeOnLoad]`); replaced by `[RuntimeInitializeOnLoadMethod]` routing any bootstrap-less scene to the menu (`CoreBootstrap.cs:97`) | Low | **Accept** — arguably better (works in builds too). Keep the SampleScene entry out of Build Settings |
| **D8** | ID format is `HB-YYYY-NNNN` (years 2023–2027, serial 0001–9999); the spec's example was `HBC-YYYY-####` | Trivial | **Confirm which the concept PDF/branding uses.** If `HBC`, it is a 2-line change (`IdRules.Prefix` + length check + canon IDs in `ContentData`) and the logic suite will re-prove fairness |
| **D9** | No `CrossCheckEngine` "1 discrepancy = doubt, 2+ = reject" scoring; the verdict is ground truth (`IsAnomaly`) with a fairness invariant (legit = 0 defects, anomaly ≥1 detectable), plus a checklist that scores how many discrepancies the player actually ticked | Low-medium | **Accept** — it is provably fair (27,075 assertions) and the checklist already gives the "doubt" nuance. If you want the literal rule, it is a small change to `InspectionRules.ExpectedVerdict` |
| **D10** | **No tracking layer**: `TrackingSystem`, `TrackParamsSO`, silhouette haze→ink solidification, `TokenManager`/`TokenView` are absent. Reveal is binary (Focus beam reveals CoverHug; lights = observed) | **High** | **Biggest gameplay gap vs spec.** If the promo video needs "anomaly reveal → collect token", this is the feature to build. It is additive: a tracking meter on the existing Focus beam + a token pickup, no changes to the checkpoint |
| **D11** | Sanity stages Stable/Uneasy/Shaken/Breaking at 70/40/15 vs spec Calm/Neutral/Danger/Hopeless at 76/41/16; no "composure shield absorbs the first hit" | Low | Thresholds are a 3-number change in `SanityModel.StageFor` (my suite asserts the current boundaries, so it will flag the change). The composure shield is a small feature in `GameSession.ApplySanity` — decide if you want that feel |
| **D12** | Settings missing `sanityMultiplier`, `steadyHands`, `crtToggle`, `dyslexiaFont` | Low-medium | `dyslexiaFont` + `crtToggle` are the two worth adding for the accessibility story in your submission; both are cheap (font swap, one overlay toggle) |
| **D13** | Save lacks `rank`, `museum[]`, `todayBest{date,amount}`; has `EndlessBest`/`ChallengeBest`/`EndingsSeen` | Low | Only needed if you want the "museum/collection" meta-progression. Adding fields is backwards-compatible if you bump `SaveFormat.Version` and tolerate old saves |
| **D14** | No `IPlateProvider`/`IPortraitProvider`; direct `Resources.Load("Art/"+key)` with procedural fallback | Low | **Accept** — same zero-code art swap, fewer moving parts |
| **D15** | Procedural textures are bilinear and larger than 640×360 | Low-medium | One-line experiment: set `FilterMode.Point` for world/prop keys (keep Bilinear for documents/UI). Compare in Play and keep whichever reads better on camera |
| **D16** | Palette is a darker ramp than the spec hexes | Cosmetic | **Accept** (matches the horror direction you approved). If the submission needs spec-exact brand colours, they are 8 constants in `UIFactory.cs:15-25` |
| **D17** | Map shows 5 rooms; spec lists more locked/lore rooms (office, records, staircase, restroom, emergency exit) | Low | Cheap lore win for the map screen — add non-clickable labelled boxes |
| **D18** | No `AmbientAudio` room tone + ducking | Low | Music-context loops cover most of it. Ducking (lower music while a caption/scare plays) is a nice polish item |
| **D19** | Checkpoint flow is coroutine-driven, not a named `Idle→Knock→Interview→Inspect→Decide→Confirm→Result` FSM | Low | Same beats, same order, harder to inspect in a debugger. Only worth changing if you need to pause/resume mid-inspection reliably |
| **D20** | The spec's phase-gated output contract ("PHASE N COMPLETE — stop") wasn't followed; the game was built in one pass | Process | Not a code issue. Note for future agent runs |

---

## 6. Spec items that are met exactly (no action)

- Loop: Main Menu → (Mode Select) → Shift (Checkpoint ⇄ Exploration) → Results → Ending, 5 shifts.
- 4 standing questions, verbatim topics: course · year · regular/transferee · first classroom.
- 10 clues, exact list and intended order, gated on investigating Hallway + Classroom 1 + Classroom 2.
- 3 endings, exact titles and conditions, recorded to `endingsSeen`.
- Shift counts 4/6/7/8/8 + Administrator; escalation of defect count, subtlety and duplicate/behavior unlocks.
- Anomalies wear real students' faces and carry the same glitch; four evade behaviors; three light modes.
- Sanity is perception, not health: it never ends a run directly; it drives vignette/distortion/heartbeat/hallucinations/scares.
- Monochrome + a single restrained red for interactives; procedural-first art with drop-in replacement.
- Deterministic seeded RNG; saves regenerate rosters identically.
- Accessibility: reduced motion, photosensitivity-safe, captions, text scale — and the horror FX actually respect them.

---

## 7. Your `Tools/gen_meta.py` — verified here

Ran it against the replicated tree (with placeholder stand-ins for the three `.unity` files, which were not
in the source dump):

```
### RUN 1 ###   meta files created: 59      pinned GUIDs verified
### RUN 2 ###   meta files created: 0       pinned GUIDs verified   <- idempotent
```

| Check | Result |
|---|---|
| Every `.meta` has a valid 32-hex GUID | ✅ 59/59 |
| No duplicate GUIDs | ✅ |
| All 42 scripts get a `MonoImporter` block | ✅ |
| `AspectMatchGuard.cs.meta` created with `sha256(path)[:32]` | ✅ `c99c92a56a6a416ec866f31ea4606ed9` (this closes the "only script without a .meta" note) |
| Never overwrites an existing `.meta` | ✅ — the important safety property: it cannot churn Unity-assigned GUIDs |
| Pinned scene/bootstrap GUIDs asserted | ✅ |

Two notes:
1. It writes an `Assets.meta` (the `if rel != "Assets" or True:` branch always runs). Unity neither needs nor
   uses a meta for the `Assets` folder itself — harmless, but it is a stray file in version control.
2. **GUID dependency worth knowing:** your three scenes reference the bootstrap scripts by the *pinned* GUIDs.
   If Unity ever regenerates one of those `.cs.meta` files (e.g. you delete it, or extract a zip that overwrites
   it), the scene's bootstrap component becomes "Missing (Mono Script)" and Play does nothing except the
   runtime safety net that routes to MainMenu. 10-second check:

   ```powershell
   Select-String -Path Assets\_Project\Scripts\Bootstrap\*SceneBootstrap.cs.meta -Pattern "guid:"
   # expect 0b1b2c3d…, 0c1b2c3d…, 0d1b2c3d… respectively
   ```
   For the same reason, **never extract a zip that contains `.meta` files over your project** — mine don't.
