# Complete UI Overhaul & Dialogue Input Integration - final report

Scope: the two stacked owner directives - (1) make the unused dialogue text box a **functional**
typed input wired into the real interaction loop, and (2) redesign the desk UI to the concept art
(`uploads/1_image.png`, treated as the source of truth for proportions and placement) - finished
end-to-end, responsive across breakpoints, with fonts from the kit zips, on top of the previously
integrated SecurityRoom art kit. All work is **uncommitted** by instruction. No pushes.

Contracts kept throughout: two-question cap per visitor; verdicts are physical (drag stamp onto
the slip, hand both papers back); ending priority Lose > Secret > Win; sanity drives atmosphere;
no scanner / checklist / face-match widget restored anywhere.

---

## 1. The dialogue text box is real input (P0)

| Requirement | Where it lives | How it is proven |
|---|---|---|
| Connected to real input state | `UIFactory.TextField` + stub `InputField` (caret, selection, focus synced with the EventSystem) | `TalkDockProof` types through the stub exactly as uGUI would |
| Type & edit | MultiLine input; caret moves, backspace/delete work | proof types, edits mid-string, re-submits |
| Enter submits, Shift+Enter newline | stub `MultiLineSubmit` semantics (one implementation, editor + headless) | proof asserts a newline does not submit and Enter does |
| Submitted messages show in the dialogue interface | transcript sheet behind `LOG` (last 24 lines, speaker-tagged) | proof reads the sheet rows back |
| Text available to game logic | `GameLogic/SpokenChat.Route` returns the routed result; matching lines enter the Interview path (question budget, bubble answer, testimony) | `SpokenChatTests` (36) + `TalkDockProof` budget assertions |
| Loading / disabled / success / error states | send-beat lock; disabled + explanatory placeholder with no visitor; clear-on-success; keep-text-on-failure | proof walks all four |
| Block empty + duplicate submits | normalized-empty reject; duplicate-of-last reject with a transcript nudge | proof submits "" and the same line twice |
| Usable across transitions | `ResetTalkDock` per visitor; view state survives scene reloads; focus returned by `V` | proof re-focuses after a visitor change |
| Visually obvious | dock strip under the tray with a focus ring, dark ink on paper, kit typeface | every breakpoint screenshot |

Nothing is faked: a typed question spends the same two-slot budget as the numbered buttons and is
answered through the same bubble; unmatched lines draw in-character refusals from the desk, never
invented student dialogue.

## 2. Concept-art redesign

- `CheckpointLayout` re-based to measurements taken off `uploads/1_image.png` (header board,
  booth glass, questions column, cabinet/drawer, papers, tray, dock). The kit board layer rides
  at (0,−31,1280,720) so its painted cut-outs meet the audited meters; its own white strips are
  painted out in prep (`sec_upper_panel_clean`).
- The kit shutter is shorter than the concept booth, so it renders as three bands
  (`sec_window_{open,close}_{top,glass,base}`): hood and base keep painted proportions at the
  concept heights, only the glass band stretches to `L.Glass`.
- ID card / slip are audited containers with scaled art children (`cardart` @0.625,
  `slipart` @0.75) so the concept footprints coexist with design-space children; flips walk the
  art child, stamp marks parent to `slipart`.
- Fonts come from the kit zips (`Resources/Fonts/kit_bahnschrift_*`, `kit_chiller`) with bundled
  open-licence fallbacks; the headless stub probes the real files so proofs see the editor's
  selection.
- Obsolete UI was removed, not layered: the procedural hood/sill/bases/windowframe/glass/facade/
  banner/topbar retire under the kit; the old registry-terminal search box stays gone
  (FlowCheck asserts `station` has no InputField and no searchbox/placeholder node - which is why
  the new placeholder Text is named `ph`).

## 3. Responsive breakpoints (screenshots)

Rendered from one real-hierarchy dump (`FlowCheck --dump-screen`, desk + HUD, cumulative scales,
focus state) by `Tools/Preview/render_screen.py` with the real fonts and uGUI-correct 9-slice
maths:

- `preview/shots_after/screen_{1280x720,1920x1080,2560x1440,2560x1080,1920x1200,1024x768,3840x2160}.png`
- side-by-side concept / before / after: `preview/compare_1280x720.png`, `preview/compare_1920x1080.png`
- contact sheet of every after-shot: `preview/breakpoints_after_sheet.png`

Wide aspects letterbox the 16:9 desk horizontally, tall aspects vertically (AspectMatchGuard's
matched-axis behaviour); nothing stretches, overlaps or clips at any of the seven.

## 4. Bugs found and fixed in final hardening

1. **Sanity pill never wore the kit tint.** Bootstrap builds the desk before the HUD, so
   `ApplyKitSanity(true)` hit a null HUD at Init - and `SetSanity` repaints the fill on every
   sanity event, so even a successful one-shot tint was clobbered. Fixed with a sticky
   `_kitSanity` flag (kit-aware inks inside `SetSanity`; alarm reds/oranges kept - they read on
   the white pill) plus a lazy once-call in `CheckpointView.RefreshHud`. FlowCheck now proves:
   track bed retired, caption/value dark, stage caption hidden, fill stage-correct.
2. **Speech bubble drew without its paper.** The kit master shipped padded with transparent
   margins and loaded at the default UI PPU, so the 430 px tail slice was a 4-unit sliver in the
   editor and nothing at all in the preview (the renderer fed raw texture pixels as reference
   units). Fixed on all three layers: prep trims `sec_bubble` to its alpha box (2879×1031),
   `ProceduralArt.FileBorders` measures the real art (480/140/140/140) with a PPU-10 override,
   the dump emits `pixelsPerUnit`, and the renderer converts borders through it (source crops in
   texture pixels, destinations in screen pixels - exactly uGUI). Proof: tail slice ≥ 20
   reference units (measured 48).
3. **Silent no-op proofs.** The shutter proof still referenced the pre-band `sec_window` node
   (skipped silently) and the click-through loop tolerated missing layers. Both now assert
   presence first; the shutter proof checks all three bands swap together.

## 5. Intentional deviations from the concept (with reasons)

| Deviation | Reason |
|---|---|
| Talk dock + transcript sheet at the desk foot | new P0 furniture; the concept is a still with no slot for it; placed in the free strip under the tray, outside every audited rect |
| Patrol-map widget keeps the dark chalk list | the kit ships no map paper; inventing torn-map art would fabricate owner art; the list is the live clickable plan index |
| Interaction prompt in accent red under the sanity pill | it is the live hand-over hint and must out-shout the board; the static concept has no prompt |
| Patrol / End Shift pills dim while disabled | state language: the concept depicts them live; dimming is the disabled state |
| Sanity stage caption hidden under the kit pill | no room on the pill; alarm colours + horror FX carry the stage |

## 6. Verification results (actual runs, this pass)

| Suite | Result |
|---|---|
| `dotnet run --project Tools/FlowCheck` | exit 0 - **1127 OK / 0 BAD** |
| `dotnet run --project Tools/LogicTests` | **27,773 PASS / 0 FAIL** |
| `dotnet run --project Tools/Verification/LogicTests` (frozen) | **663 passed / 0 failed** |
| `bash Tools/Verify/verify.sh` | compile clean + audit clean (107 scripts) |
| Unity-editor Play Mode / Windows build | **NOT RUN** - no Unity editor in this environment; geometry verified via the dump/render pipeline instead |

## 7. Changelog of touched items (this turn, on top of the pass)

- `Assets/_Project/Scripts/UI/HUDView.cs` - `_kitSanity` flag; kit-aware `SetSanity`/`FillInk`;
  `ApplyKitSanity` routes its repaint through `SetSanity`.
- `Assets/_Project/Scripts/Checkpoint/CheckpointView.cs` - lazy kit-sanity once-call in
  `RefreshHud` (+ `_kitSanityApplied`).
- `Assets/_Project/Scripts/Checkpoint/CheckpointView.KitSkin.cs` - comment: immediate tint is
  best-effort; the lazy path owns the normal bootstrap order.
- `Assets/_Project/Scripts/Bootstrap/ProceduralArt.cs` - `FilePpus` (sec_bubble PPU 10);
  `FileBorders` re-measured on the trimmed master (480/140/140/140).
- `Assets/_Project/Resources/Art/SecurityRoom/sec_bubble.png` - trimmed to alpha box in place
  (meta/guid untouched); `Tools/prep_securityroom_art.py` trims on export for reproducibility.
- `Tools/FlowCheck/Program.cs` - sanity-pill proofs, band-synced shutter proof, presence
  assertions on every kit layer, tail-slice-units proof, `pu` field in the screen dump.
- `Tools/Preview/render_screen.py` - uGUI-correct 9-slice (source vs destination borders through
  pixelsPerUnit); docstring updated.
- Docs: `REFACTOR_LOG.md` (new top entry), `README.md` (window-talk controls row, verification
  counts + dump/render commands), `ARCHITECTURE.md` (typed window talk paragraph), this report.
