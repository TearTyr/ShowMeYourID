# Options menu — finish & polish report

**Scope:** make the Options sheet look finished, behave correctly, and own every setting it shows —
without redesigning the game. Visual reference: the existing left-sheet main menu.
**SFX-only rule respected:** no music slider was added and no music system was introduced.

---

## 1. What was wrong (found by inspecting the code, confirmed against your screenshots)

1. **Slider labels were rendered off-sheet.** `UIFactory.SliderRow` positioned the label with a
   *negative* centre offset against a left anchor (`width*0.21 - width/2 + 8` = −114 px at width 560),
   so "Master/Music/Effects volume" drew outside the sheet and the track slid left into the label
   column. That is exactly your screenshot: three bare tracks touching the sheet edge, no labels.
   (`ToggleRow` had already been fixed in v1.0.2; `SliderRow` never was.)
2. **No display settings at all.** No fullscreen toggle, no resolution list, nothing persisted.
3. **Reset lived only as the F2×3 shortcut** in `MainMenuView.Update`, with its confirmation inline —
   so Options had no reset and the two paths could drift apart.
4. **Options never re-read saved values on open** (`Show()` only refreshed the text-size buttons),
   so a reopened sheet could show stale slider positions.
5. Settings themselves were already well-behaved (PlayerPrefs, applied immediately, survive a data
   reset) — that part was kept, not rewritten.

## 2. What changed, file by file

| File | Change |
|---|---|
| `Assets/_Project/Scripts/UI/UIFactory.cs` | `SliderRow` rebuilt: label column 0–36 %, track 38–88 %, live **% read-out** 88–100 %, all offsets positive; invisible full-row hover catcher; new `AddHover` helper (uGUI `EventTrigger`, no new component type); `ToggleRow` gains a subtle hover tint |
| `Assets/_Project/Scripts/UI/MenuScreens.cs` | `SettingsView` rebuilt as four labelled sections (AUDIO / DISPLAY / ACCESSIBILITY / DATA) with live refresh on `Show()`; display-change KEEP/REVERT row; RESET DATA button; `ControlsView` text rewritten from the *verified* bindings; F2×3 now calls the shared `GameRoot.RequestDataReset` |
| `Assets/_Project/Scripts/Core/SettingsSystem.cs` | Owns display state exclusively: `SupportedModes`, `MenuChoices`, `SetDisplay`, `ApplyCurrentDisplay`, `ApplyStoredDisplay` (boot-time, self-clearing), `RestoreDisplaySnapshot`; persists `fullscreen/displayW/displayH` in the same `smyid.settings.` namespace |
| `Assets/_Project/Scripts/GameLogic/DisplayPolicy.cs` | **New, engine-free:** dedupe/min-size/sort of supported modes, menu choice selection (preferred heights + always the current mode, capped), support test, aspect-then-area fallback, 15 s revert constant. Fully unit-tested |
| `Assets/_Project/Scripts/GameLogic/GameSession.cs` | `SettingsData` gains `Fullscreen`, `DisplayWidth`, `DisplayHeight`; `Encode`/`Decode` extended **tolerantly** (old 7-field strings still decode; truncated input rejected instead of throwing) |
| `Assets/_Project/Scripts/Core/GameRoot.cs` | Applies the stored display once at service init; new `RequestDataReset(afterReset)` — the single confirmation + reset path shared by F2×3 and Options |
| `README.md` | New, written from verified facts only (see §6) |
| `Tools/LogicTests/Program.cs` | +31 assertions: display policy, fallback ordering, options row budget, settings round-trip incl. legacy strings |

Nothing else was touched. Challenge Mode / Extras remain absent from every menu (the audit asserts
`CHALLENGE MODE` / `EXTRAS` buttons do not exist).

## 3. Behaviour notes (decisions, with reasons)

- **Audio:** two sliders — *Master volume* and *SFX volume* — both live, both persisted, both
  clamped 0..1 so mute and max are exact. The SFX slider feeds `SettingsSystem.SfxVolume`
  (= master × sfx) which `AudioDirector` already consumes on `SettingsChanged`, so it cannot touch
  anything else. No music slider, per the brief. *Honest note:* the build does contain ambient
  context beds and a menu theme (`MusicContextRequested`); they are now only reachable through
  Master. If you want them gone entirely, one line in `GameRoot`/`AudioDirector.PlayMusic` kills them.
- **Display:** fullscreen = borderless `FullScreenWindow`; resolution buttons are built from
  `Screen.resolutions` via `DisplayPolicy` (deduped, ≥1024×600, preferred heights, current mode always
  present, max 4 so the sheet never overflows). Every change applies immediately **and** starts a
  15-second KEEP/REVERT window rendered *outside* the stacking column (above CONTROLS/BACK), so the
  countdown can never re-flow the rows or fall off-screen; timeout or REVERT restores the snapshot,
  KEEP persists. Launch applies the stored mode and silently clears preferences the current display
  no longer supports.
- **Reset:** Options → `RESET DATA…` and F2×3 both call `GameRoot.RequestDataReset`, which shows one
  confirmation naming what is erased (progress, records, endings) and what is kept (all settings —
  they live in PlayerPrefs, the save file does not hold them). Cancel = `ConfirmationPopup` No, which
  touches nothing. F2 still needs three presses ≤1.5 s apart and disarms on gaps.
- **States:** buttons keep their hover/focus/pressed ColorBlock; sliders brighten handle+label on
  hover and show a % value; toggles tint their row on hover; selected resolution / text-size buttons
  use the same raised fill as before; unused resolution buttons are hidden, never dead.
- **The red "X"** in a checked box is the sheet's on-state mark (`AccentBright`) — kept for
  consistency with the rest of the game's single-red language.

## 4. Resolution / aspect-ratio checklist

| Case | Result |
|---|---|
| Compile of every changed script vs real Unity assemblies (minimal + full sets) | **PASS** (0 errors, 0 warnings) |
| Options row budget ≤ 505 px column at the 1280×720 reference (13 stacked rows + spacing) | **PASS** (unit test) |
| ≥20 px clearance between the column bottom and CONTROLS/BACK | **PASS** (unit test) |
| Revert window cannot re-flow or overflow rows (anchored outside the column) | **PASS** (unit test + code) |
| 1920×1080 · 2560×1080 (21:9) · 1024×768 (4:3) · windowed 1366×768 / 1600×900 | **NOT TESTED** — needs the Unity editor; `AspectMatchGuard` math for these was verified in the previous hotfix |
| Text-size Large with long labels ("Photosensitivity safe (no strobe, softened scares)") | **NOT TESTED** in editor; labels wrap (VerticalWrapMode.Wrap) and rows are fixed-height by design |

## 5. Functional checklist

| Item | Result |
|---|---|
| SFX slider updates SFX immediately | **Code-verified** (`Apply → Notify → AudioDirector.ApplyVolumes`); runtime **not tested** here |
| Volume persists across launches | **Code-verified** (PlayerPrefs `smyid.settings.sfx/master`); runtime **not tested** |
| Mute (0) and max (1) exact | **PASS** storage (`Clamp01` + round-trip tests incl. 0 and 1); audio path runtime not tested |
| Music slider absent; no music system added | **PASS** |
| Fullscreen toggle works + persists | **Code-verified**; runtime **not tested** |
| Resolution list from supported modes, deduped, current included | **PASS** policy (unit tests); `Screen.resolutions` read runtime not tested |
| Unsupported/failed display change recovers | **PASS** policy fallback + 15 s revert + boot-time clearing (unit tests for policy; Screen calls runtime not tested) |
| No redundant Apply button (everything is immediate) | **PASS** |
| Controls screen matches real bindings | **PASS** — A/D + ←/→, Shift run, E interact, F flashlight, M map, Esc pause, mouse desk, click/Space/Enter/E dialogue, mouse-only security room, F2×3 / Options reset (`PlayerController.cs:57-72,159`, `DialogueView.cs:156`, `CheckpointView`, `SecurityRoomView`) |
| No remapping UI shown | **PASS** (display-only, as permitted) |
| Reset shows confirmation, explains scope, offers Cancel | **PASS** (code) |
| Reset and F2×3 share one path | **PASS** (`GameRoot.RequestDataReset`) |
| Cancel leaves save untouched | **PASS** (code: No → `ConfirmationPopup.Hide`, no side effects) |
| Reset keeps volumes/display/accessibility | **PASS** (settings live in PlayerPrefs; `ResetAllData` deletes only the save file) |
| Back keeps intended changes | **PASS** (`Apply` persists immediately; `Hide` cancels only an *unconfirmed* display change) |
| Challenge Mode / Extras hidden | **PASS** (static audit asserts their absence) |
| No dead/misleading buttons | **PASS** by construction (every button has an onClick; unused resolution buttons are hidden) |
| Logic suite (rules + display policy + layout budget) | **PASS 27,106 / 27,106** |
| Play-mode feel, audio mix, pixel layout, Windows build | **NOT TESTED** — no Unity editor in this environment |

## 6. README

`README.md` (project root) rewritten from verified facts only: status table separating implemented
from unimplemented, Unity 6.3 / Built-in RP / legacy-input requirements, the three real scene names
and build order, the verified controls table, the two persistence stores and their exact keys/paths,
repository layout, and the verification status. Credits are referenced to the in-game CREDITS screen
rather than duplicated. Anything unverifiable here (exact editor patch, manual-test outcomes) is
explicitly marked.

## 7. Still needs a human in the editor

1. Play → SETTINGS: confirm sliders show labels + % and drag smoothly; toggle fullscreen; pick each
   resolution and watch the 15 s KEEP/REVERT row; let it time out once on purpose.
2. Restart the game: volumes, text size, toggles, fullscreen and resolution should all come back.
3. Unplug/switch monitor (or edit the prefs) to check the launch-time clearing of an unsupported
   stored resolution.
4. RESET DATA → Cancel (save intact), then → confirm (save gone, settings intact), then F2×3 once
   (same dialog).
5. Game view at 21:9 and 4:3 with **Text size = LARGE**: check the longest toggle label wraps inside
   the sheet.
6. Your screenshot 2 shows a navy rounded box at the bottom-right that ignores the Game-view border
   (it overlaps editor chrome), so it is most likely an OS/overlay artifact, not game UI. If it
   persists, pause Play, click it, and read
   `EventSystem.current.currentSelectedGameObject.name` / the hovered raycast target — if it ever
   names one of our objects, tell me the name and I'll trace it.

## 8. Before / after (schematics)

`docs/ui/options_before.svg` and `docs/ui/options_after.svg` are **diagrams generated from the layout
constants**, not editor captures — this sandbox has no Unity, so true screenshots remain yours to take.
