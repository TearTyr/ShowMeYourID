# Main Menu art kit

Source of truth for the menu's look: **`reference/main-menu-reference.png`** (2880×1620, the
approved composite). Everything in this folder exists to reproduce that image inside the game
at 1:1 (the composite maps onto the 1280×720 canvas at exactly 2.25 px per reference unit).

The runtime kit lives at `Assets/_Project/Resources/Art/MainMenu/<key>.png` and is regenerated
from this folder by `Tools/prep_mainmenu_art.py` (run `Tools/generate_meta.py` afterwards).
Keys are live: `ProceduralArt.Get(key)` resolves them through the same drop-in path as the
SecurityRoom kit, and `docs/asset-manifest.csv` carries one `shipped-kit` row per key.

## Layout

| Path | Contents |
|---|---|
| `reference/main-menu-reference.png` | the approved 2880×1620 menu composite — design spec, never shipped in-game, kept for pixel comparison (`Tools/FlowCheck --dump-menu` + `Tools/Preview/render_screen.py`) |
| `masters/main-menu-ui-layer.png` | RGBA master layer (2669×1397): logo plate, button frames, icons and label art. Composites into the reference 1:1 at offset (109, 96). All crops are cut from here |
| `backgrounds/main-menu-background.png` | corridor backdrop, 2880×1620, no UI. Drawn full-bleed and cover-cropped (`MainMenuView.FitBackground`) so it never stretches |
| `buttons/menu-frame-wide.png` | 977×197 white outline (6 px stroke) — NEW GAME / CONTINUE frame |
| `buttons/menu-frame-square.png` | 160×162 white outline (6 px stroke) — SETTINGS / EXIT / CREDITS frame |
| `logos/main-menu-logo.png` | cropped "SHOW ME YOUR ID!" plate incl. its soft halo (1222×788) |
| `icons/menu-icon-*.png` | cropped button icons (skull, four-arrow, gear, door+arrow, theatre masks) |
| `fonts/chiller.ttf` | the horror display face used for the button labels (`UIFactory.TypeFont` ships it as `Resources/Fonts/kit_chiller.ttf`) |

The mirrored copies under `logos/` and `icons/` are byte-identical to the runtime kit sprites —
they exist so the source tree shows exactly what each kit sprite was cut from.

## Runtime keys (`Resources/Art/MainMenu/`)

| Key | Cut from | Used by |
|---|---|---|
| `menu_background` | `backgrounds/main-menu-background.png` (whole file) | `MainMenuView` bg layer |
| `menu_logo` | master bbox (1435,12)-(2656,799) | `MainMenuView` logo, top-right |
| `menu_frame_wide` | `buttons/menu-frame-wide.png` (whole file) | NEW GAME / CONTINUE frames |
| `menu_frame_square` | `buttons/menu-frame-square.png` (whole file) | SETTINGS / EXIT / CREDITS frames |
| `menu_icon_newgame` | master bbox (69,96)-(169,212) | skull in NEW GAME |
| `menu_icon_continue` | master bbox (67,355)-(168,456) | four-arrow in CONTINUE |
| `menu_icon_settings` | master bbox (25,1260)-(135,1370) | gear square |
| `menu_icon_exit` | master bbox (222,1260)-(332,1370) | door+arrow square (two touching components — crop the union) |
| `menu_icon_credits` | master bbox (407,1273)-(517,1348) | theatre-masks square |

## Naming conventions

- Runtime sprites: `menu_<role>` — the manifest key, the file name and the sprite name at
  runtime are the same string (the drop-in contract).
- Source files: kebab-case under a purpose folder (`backgrounds/`, `buttons/`, `icons/`, …).
- Never edit runtime kit PNGs by hand — adjust the crop rects in `Tools/prep_mainmenu_art.py`
  and re-run it, so source and kit stay reproducible.

## Geometry notes (measured, not guessed)

- Master → reference offset: (109, 96), scale 1:1.
- Wide buttons: 977×197 at reference (111,154) and (111,404) → canvas units ÷2.25.
- Square row: 160×162 at reference x = 109 / 303 / 492, y = 1331.
- Icons sit optically, not mathematically, centred: skull −2 px, masks −5 px vertically,
  door +3 px horizontally (master positions; `MainMenuView` carries the same offsets).
- Labels are live text (Chiller, size 46, ×1.042 horizontal) so hover/focus/disabled states
  and localisation keep working; the master's raster labels were only used to measure them.
