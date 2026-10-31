#!/usr/bin/env python3
"""Prep the owner-supplied Main Menu art kit into game-ready sprites.

Source of truth: <repo>/Art/MainMenu
    reference/main-menu-reference.png   the approved 2880x1620 menu composite (design spec)
    masters/main-menu-ui-layer.png      RGBA layer: logo, button frames, icons, label art
    backgrounds/main-menu-background.png  RGBA-free corridor backdrop (2880x1620)
    buttons/menu-frame-wide.png         977x197 white outline used by NEW GAME / CONTINUE
    buttons/menu-frame-square.png       160x162 white outline used by the icon row

Destination: Assets/_Project/Resources/Art/MainMenu/<key>.png  (+ .meta via generate_meta.py)
The same crops are mirrored back into Art/MainMenu/{logos,icons}/ so the source tree shows
exactly what the runtime kit was cut from.

Geometry: the UI master composites into the reference 1:1 at offset (109, 96) - measured by
matching the master's frame/box alpha bounding boxes against the reference composite. The
crop rects below are the master-local alpha bboxes of each element (measured, not guessed).

Re-run whenever the kit changes; output names are stable keys (menu_*).
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "MainMenu")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "MainMenu")

# element -> alpha bbox inside masters/main-menu-ui-layer.png (2669x1397)
CROPS = {
    "menu_logo":          (1435, 12, 2656, 799),   # "SHOW ME YOUR ID!" lettering + soft halo
    "menu_icon_newgame":  (69, 96, 169, 212),      # skull
    "menu_icon_continue": (67, 355, 168, 456),     # four-arrow refresh mark
    "menu_icon_settings": (25, 1260, 135, 1370),   # gear
    "menu_icon_exit":     (222, 1260, 332, 1370),  # door + arrow (two touching components)
    "menu_icon_credits":  (407, 1273, 517, 1348),  # theatre masks
}

# whole-file copies: runtime key -> path inside Art/MainMenu
COPIES = {
    "menu_background":  os.path.join("backgrounds", "main-menu-background.png"),
    "menu_frame_wide":  os.path.join("buttons", "menu-frame-wide.png"),
    "menu_frame_square": os.path.join("buttons", "menu-frame-square.png"),
}

# where the mirrored source crops land inside Art/MainMenu
MIRROR = {
    "menu_logo": os.path.join("logos", "main-menu-logo.png"),
    "menu_icon_newgame": os.path.join("icons", "menu-icon-new-game.png"),
    "menu_icon_continue": os.path.join("icons", "menu-icon-continue.png"),
    "menu_icon_settings": os.path.join("icons", "menu-icon-settings.png"),
    "menu_icon_exit": os.path.join("icons", "menu-icon-exit.png"),
    "menu_icon_credits": os.path.join("icons", "menu-icon-credits.png"),
}


def main():
    os.makedirs(DST, exist_ok=True)
    master = Image.open(os.path.join(SRC, "masters", "main-menu-ui-layer.png")).convert("RGBA")
    n = 0
    for key, box in CROPS.items():
        x0, y0, x1, y1 = box
        crop = master.crop((x0, y0, x1 + 1, y1 + 1))
        crop.save(os.path.join(DST, key + ".png"))
        mirror = MIRROR.get(key)
        if mirror:
            dst = os.path.join(SRC, mirror)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            crop.save(dst)
        n += 1
        print(f"  cropped {key} {crop.size[0]}x{crop.size[1]}")
    for key, rel in COPIES.items():
        src = os.path.join(SRC, rel)
        Image.open(src).convert("RGBA").save(os.path.join(DST, key + ".png"))
        n += 1
        print(f"  copied  {key} from Art/MainMenu/{rel}")
    print(f"prep_mainmenu_art: wrote {n} sprites to {os.path.relpath(DST, ROOT)}")
    print("remember: python3 Tools/generate_meta.py  (new .meta files)")


if __name__ == "__main__":
    main()
