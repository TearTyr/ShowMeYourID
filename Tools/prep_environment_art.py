#!/usr/bin/env python3
"""Prepare the hallway, room and prop art for the game.

Source: Art/UI and Design Assets   Destination: Assets/_Project/Resources/Art/Environment
The comfort room backdrop is converted from JPG to PNG so every game sprite is a PNG named by
its key (docs/ART-NAMING.md). Re-run whenever the source art changes, then run
python Tools/generate_meta.py for any new files.
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "Environment")


def out(folder, key):
    path = os.path.join(DST, folder)
    os.makedirs(path, exist_ok=True)
    return os.path.join(path, key + ".png")


def main():
    Image.open(os.path.join(SRC, "Comfort Room", "Bathroom_dimmed.jpg")).convert("RGBA").save(
        out("Rooms", "bg_comfort_room"))


if __name__ == "__main__":
    main()
