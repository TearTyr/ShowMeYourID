#!/usr/bin/env python3
"""Prepare the hallway, room and prop art for the game.

Source: Art/UI and Design Assets   Destination: Assets/_Project/Resources/Art/Environment
  Hallway/  bg_hallway_lit, bg_hallway_unlit   the three screen wide corridor (downscaled to 3840 x 1280)
  Rooms/    bg_classroom_1, bg_classroom_2, bg_comfort_room   1920 x 1080 backdrops
  Props/    doors, the clock and the token, cut to the visible art (names are the game's keys)
  Inspect/  inspect_*   close ups shown beside the text when a clue is inspected
Everything is a PNG named by its key (docs/ART-NAMING.md). Re-run whenever the source art changes,
then run python Tools/generate_meta.py for any new files.
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "Environment")

# source file (relative to SRC) -> key, for the pieces cut to their visible art
PROPS = [
    ("Hallway/CLASSROOM DOOR_1.png", "prop_door_classroom_1"),
    ("Hallway/CLASSROOM DOOR_2.png", "prop_door_classroom_2"),
    ("Hallway/MAP DOOR_MAPVIEW.png", "prop_door_comfort"),
    ("Hallway/PROP_SECURITY DOOR.png", "prop_door_security"),
    ("Hallway/PROP_SERVER DOOR.png", "prop_server_door"),
    ("Hallway/PROP CLOCK.png", "prop_clock"),
    ("Hallway/TOKEN.png", "token"),
]
INSPECT = [
    ("Hallway/PROP NOTICE BOARD.png", "inspect_noticeboard"),
    ("Hallway/PROP_PHOTOFRAME.png", "inspect_photoframe"),
    ("Hallway/PROP CLOCK.png", "inspect_clock"),
    ("Hallway/PROP_LIGHT FIXTURE_BULB.png", "inspect_light_bulb"),
    ("Hallway/PROP_LIGHT SWITCH.png", "inspect_light_switch"),
    ("Hallway/CLIPBOARD_RESERVED.png", "inspect_clipboard"),
    ("Hallway/PROP_SERVER DOOR.png", "inspect_server_door"),
    ("Hallway/HALLWAY LOCKER_1.png", "inspect_lockers"),
]
INSPECT_MAX = 512   # close ups are small pictures beside the text


def out(folder, key):
    path = os.path.join(DST, folder)
    os.makedirs(path, exist_ok=True)
    return os.path.join(path, key + ".png")


def cut(rel):
    im = Image.open(os.path.join(SRC, rel)).convert("RGBA")
    return im.crop(im.split()[3].point(lambda v: 255 if v > 8 else 0).getbbox())


def main():
    for rel, key in (("Hallway/HALLWAY W_ LIGHTS.png", "bg_hallway_lit"),
                     ("Hallway/HALLWAY W_O LIGHTS.png", "bg_hallway_unlit")):
        im = Image.open(os.path.join(SRC, rel)).convert("RGB").resize((3840, 1280), Image.LANCZOS)
        im.save(out("Hallway", key), optimize=True)
    for rel, key in (("Rooms/Classroom_BG1.jpg", "bg_classroom_1"), ("Rooms/Classroom_BG2.jpg", "bg_classroom_2"),
                     ("Comfort Room/Bathroom_dimmed.jpg", "bg_comfort_room")):
        Image.open(os.path.join(SRC, rel)).convert("RGB").save(out("Rooms", key), optimize=True)
    for rel, key in PROPS:
        cut(rel).save(out("Props", key), optimize=True)
    for rel, key in INSPECT:
        im = cut(rel)
        im.thumbnail((INSPECT_MAX, INSPECT_MAX), Image.LANCZOS)
        im.save(out("Inspect", key), optimize=True)


if __name__ == "__main__":
    main()
