# Asset manifest - naming contract for the art & audio pass

**The rule this whole document rests on:** every name below is already a *live key* in the game.
The code paints a procedural placeholder for each key today; a real file dropped at

```
Assets/_Project/Resources/Art/<key>.png      (sprites, faces, map sheet)
Assets/_Project/Resources/Audio/<key>.wav    (sfx AND music: music contexts are "music_<context>")
```

replaces the placeholder with **zero code changes** (`ProceduralArt.Get` / `ProceduralAudio.Get`
load the file first and only fall back to the painter). Deleting the file restores the placeholder
instantly, so nothing can ever break while the team iterates.

Machine-readable twin: **`docs/asset-manifest.csv`** (regenerate with
`python3 Tools/gen_asset_manifest.py`). `Tools/FlowCheck` proves the CSV and the code agree in
both directions (`== asset manifest sync ==`), so the list cannot silently drift.

Counts: **90 art/face rows · 32 audio rows · 4 fonts = 126 assets.**
Priority tiers map onto the five shifts in the design doc:

| Tier | Ships with | Contents |
|---|---|---|
| **P0** | Night 1 vertical slice (4 students) | desk documents, stamps, booth + hallway, doors/switch/lockers, beam & lamp, guard walk, **faces 01–08 + their anomaly variants**, core sfx, menu/checkpoint/hallway beds |
| **P1** | Nights 2–3 (6–7 students, first anomalies) | **faces 09–14 + variants**, the four anomaly silhouettes, classroom beds & props, glitch/static/flicker/intercom/heartbeat sfx, classroom/darkroom/anomaly beds |
| **P2** | Night 4 investigation + the map | the clue props (board, photo, clock, lost ID, cabinet, light fixture), token, the hand-drawn map sheet set, investigation bed, pickup/stinger/scare/alarm |
| **P3** | Night 5, endings, polish | administrator face pair, menu bg, facade, fx grain/vignette/flash, final + ending beds |

---

## 1. Faces - the eye-check IS the gameplay (`category: face`)

The desk has no face-matching widget: the player compares the window face with the ID photo
with their own eyes, so these portraits carry more gameplay weight than any other asset.

* **`student_face_01` … `student_face_14`** - one per canon student (`Content.TotalStudents = 14`,
  the approved "8 core + 6 variations" plan). `01–06` ship today as temporary PNGs; `07–14` are
  **missing** and are the single highest-value art item in the project.
  Spec: PNG RGBA, **512×640 (4:5)**, bust, flat frontal light, neutral expression, same crop and
  eye-line for every student - the code letterboxes one asset into three places (ID photo slot,
  window portrait, counter card), so inconsistent crops read as different people.
* **`student_face_NN_anomaly`** - the **same person**, visibly wrong. The pairing is the tell:
  the window shows the anomaly variant only when the run's data says this visitor is anomalous
  (`FaceAssets.AnomalyKey`), never because a portrait "looks odd".
* **`admin_face` / `admin_face_glitch`** - the Night-5 district administrator: badge photo and the
  window variant that carries her tell (keys added this pass; `FaceAssets.AdminKey` /
  `AdminWindowKey`).
* `face_placeholder` is a functional fallback (shown only if an asset is missing) - never retire it.

> **Code note for the day `07–14` ship:** bump `FaceAssets.StudentFaceCount` from 6 to 14 (one
> constant). Until then indices wrap modulo 6, i.e. two canon students share a face set.

## 2. Desk documents (`category: desk-doc`) - P0

`id_card_frame` (320×220 px @100 ppu - the card art only; photo and text rows are drawn on top by
code), `record_sheet` (320×290), `folder_tab` (304×26), `paper_flat` (760×480 overlays),
`note_paper`, `clipboard`, `bubble_tail`, `stamp_allow` (temp ink ships as
`stamp_approval_temp.png` via alias), `stamp_reject`. All @ **UiPpu = 100**: pixel size ÷ 100 =
desk-grid units, and the desk grid is fixed at 1280×720 reference, so **deliver at the listed px**.

## 3. Patrol world (`world-bg`, `world-prop`, `character`)

Backgrounds @ **WorldPpu = 64** (px ÷ 64 = world units): `hallway_bg` 1536×640, `classroom1_bg` /
`classroom2_bg` 1152×640 (@100), plus the alpha-only `hall_depth`
(the darkness pool - keep its graded alpha or limited visibility breaks).

Props stand on the floor line and size themselves from their own pixels-per-unit, so any
resolution of art just works: `prop_door`, `prop_door_security`, `prop_server_door` (chains +
padlock baked - the scratch marks are story), `prop_lockers`, and the design-doc interaction objects, each now actually drawn
in its room: `prop_lightfixture` (flicker clue), `prop_noticeboard`, `prop_photoframe`,
`prop_clock` (stopped at 1:47), `prop_chalkboard`, `prop_desk`, `prop_lostid`, `prop_window`,
`prop_cabinet`, plus the repel `token`.

Characters: `custodian_0/1` (96×224 walk frames), `anomaly_blink/slide/sprint/coverhug`
(128×336 silhouettes - they render *under* the darkness overlay by design), `hood_figure`
(24×48 waiting-line silhouette).

## 4. The ground-floor map sheet (`category: map`) - P2

`map_paper` **940×440 px @100 ppu**: aged, stained, folded, dog-eared, red district stamp -
**blank paper only**; room outlines, labels, doors, the YOU marker and the finding dots are drawn
on top by `MapView` from `FloorPlan`, so they stay interactive and centred at every resolution.
`map_room` / `map_sealed` (red hatch + chain) / `map_door` are stretched per room, so keep their
wobble proportional. The owner's hand-drawn reference can replace `map_paper.png` 1:1.

## 5. Light & fx (`fx`) - beam first

`light_cone` 512×288 @64: the wedge the flashlight paints **and** the stencil that cuts the
darkness - its feathered shoulders are load-bearing (`SpriteMask.alphaCutoff = 0.05`), so keep the
falloff curve, not just the shape. `lamp_glow` 128 (peek lamp). `vignette`, `noise_overlay`,
`flash` are polish-tier.

## 6. Audio (`type: audio`) - 21 sfx + 11 music contexts

SFX: WAV 44.1 kHz 16-bit **mono**, 0.07–0.5 s, named `sfx_*` exactly as fired
(`sfx_knock`, `sfx_paper`, `sfx_stamp_allow/reject`, `sfx_ui_open/close`, `sfx_door`,
`sfx_footstep`, `sfx_click`, `sfx_bell`, `sfx_scan_error`, then P1/P2: `sfx_glitch`, `sfx_static`,
`sfx_flicker`, `sfx_intercom`, `sfx_heartbeat`, `sfx_pickup`, `sfx_stinger`, `sfx_scare`,
`sfx_alarm`, `sfx_scan_ok`). Captions for the accessibility setting are generated from the key, so
**renaming a key renames its caption** - don't.

Music: loop-seamless stereo beds, key = `music_<context>`: `music_checkpoint` (P0 - the desk bed,
longest-running audio in the game), `music_hallway`, `music_menu`, then `music_classroom`,
`music_darkroom`, `music_anomaly`, and P2/P3 `music_investigation` (context exists in code but is
never played yet - hook it or retire it), `music_final`, `music_end_win/lose/secret`.

## 7. Fonts (`type: font`) - shipped, open licence

BarlowCondensed Regular/SemiBold (UI), SpecialElite (typewriter documents), CaveatBrush
(handwritten map + notes). No work needed unless licensing changes; `UIFactory` falls back to the
built-in runtime font if a file is absent, so text can never break.

## 8. Functional / do-not-ship keys

`solid_white` (the 1×1-unit quad everything placeholder is built from), `rough_circle`,
`rough_panel`, `circle_dark`, `cursor_hand`, `icon_check`, `icon_x`, `camera_frame` and
`window_frame` (retired-scanner reserve). Shipping files for these buys nothing and risks the
meter/overlay maths - except `rough_panel_dark`, which IS the drawer skin and may be replaced by a
wood-grain texture of the same key.

## 9. The owner's SecurityRoom kit (`category: kit`) - shipped & integrated

The owner's layered UI kit lives verbatim in `Art/SecurityRoom/` (numbered export folders).
`Tools/prep_securityroom_art.py` slices/copies it into `Resources/Art/SecurityRoom/<key>.png`
and unpacks the two font families into `Resources/Fonts/kit_*`. Re-run the script whenever the
kit changes; keys are stable, `.meta` files come from `Tools/generate_meta.py`.

Contract of the integration pass (`CheckpointView.KitSkin.cs`):

- **The kit is a skin, never a layout.** Every interactive rect stays where
  `CheckpointLayout` puts it; all kit images are `raycastTarget=false`, so not one pointer
  interaction moved. The header grid WAS re-baselined once, to the kit's own board: the
  sanity bar and pause block now sit in the cut-outs the Upper Panel paints for them
  (measured off the export), and FlowCheck's wireframe table pins the new numbers.
- **Full-frame layers** (`sec_background*`, `sec_light`, `sec_window_*`, `sec_upper_panel*`)
  are exported at the desk's 16:9 space: stretching them across the frame lands their painted
  furniture on the audited rects. `sec_light` breathes on a slow sine unless the player enabled
  photosensitivity/reduce-motion.
- **Sliced elements** (`sec_btn_pill`, `sec_wide_pill`, `sec_caption_bar`, tray cards, icons,
  folder tabs, `sec_sanity_bar`, `sec_pause_block`) are cut at measured alpha boxes and
  stretched only into the rect they were drawn for.
- **`sec_bubble`** is 9-sliced in `ProceduralArt` (border 430/90/90/90 texture px): the left
  tail and rough edge never stretch. It skins both the desk speech bubble and the intro
  dialogue panel; the old procedural tail retires underneath it.
- **Fallbacks everywhere:** every skin call fails soft - a missing kit file leaves the
  procedural look. Fonts try the kit families first (Bahnschrift UI, Chiller display) and fall
  back to the bundled open-licence faces, so text can never vanish.
- Backdrops: `sec_background_close` behind the NIGHT loading card, `sec_background_open`
  behind the shift summary.

## 10. The owner's Main Menu kit (`category: kit`) - shipped & integrated

The approved menu composite lives at `Art/MainMenu/reference/main-menu-reference.png`;
`Art/MainMenu/README.md` documents every source file, crop rect and naming rule.
`Tools/prep_mainmenu_art.py` cuts the kit into `Resources/Art/MainMenu/<key>.png`
(`menu_background`, `menu_logo`, `menu_frame_wide`, `menu_frame_square`, `menu_icon_*`);
keys resolve through the same `ProceduralArt.Get()` drop-in path as the SecurityRoom kit
(`ProceduralArt.MainMenuKitKeys`), and `MainMenuView` rebuilds the composite from them at
1:1 (reference px ÷ 2.25 = canvas units). Verification: `Tools/FlowCheck --dump-menu=…`
walks the live menu hierarchy and `Tools/Preview/render_screen.py` pixels it against the
reference at any resolution.

Contract of the integration pass (`MainMenuView`):

- **The kit is the layout spec, not a skin over the old one.** The previous text-stack menu
  (black backdrop, five filled rows, footer) was removed; every rect is measured off the
  reference composite. Button *addresses* kept their old names (`buttons/NEW GAME`,
  `buttons/SETTINGS`, …) so headless proofs and the game-start sequence never moved.
- **Live text, live states:** labels are Chiller (`UIFactory.TypeFont`) at the measured
  size/tracking, not raster crops, so hover wash, keyboard focus, disabled CONTINUE
  (no save) and localisation keep working. Frames own the raycast; every decorative
  layer (logo, icons, labels, wash) is click-through.
- **Cover-crop, never stretch:** the backdrop sizes itself from
  `UIFactory.VisibleReferenceSize` (16:9 envelope), so odd aspects crop instead of distort.

## 11. Adding a new asset name

1. Add the painter case **and** the key to `ProceduralArt.AllKeys` (same edit, same place) -
   or the audio case + `ProceduralAudio.AllKeys`. Kit-only sprites (no painter) instead join
   `ProceduralArt.MainMenuKitKeys` / FlowCheck's `SecurityKitKeys` and ship their PNG.
2. Add the row to `Tools/gen_asset_manifest.py`, re-run it, commit the CSV.
3. FlowCheck's manifest-sync proof fails loudly if any of the three disagree.
