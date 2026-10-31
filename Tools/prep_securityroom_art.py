#!/usr/bin/env python3
"""Prep the owner-supplied SecurityRoom art kit into game-ready sprites.

Source of truth: <repo>/Art/UI and Design Assets/Security (the owner's layered kit, kept verbatim in the repo).
Destination:      Assets/_Project/Resources/Art/UI/SecurityDesk/<key>.png  (+ .meta via generate_meta.py)
                  Assets/_Project/Resources/Fonts/kit_*.ttf

The kit is a PSD-style export: several FULL-FRAME 2880x1620 layers whose content already sits at
the correct 16:9 desk position (measured: the sanity bar lands on the audited SanityMeter rect,
the pause block on the audited Pause rect, the stamp circles on the re-baselined tray, ...), plus
cropped elements and single-purpose pills that need slicing. This script copies the full-frame
layers as-is and slices the pills/icons/folders at their measured alpha bounding boxes, so the
runtime only ever stretches a sprite into the rect its art was drawn for.

Re-run whenever the kit changes; output names are stable keys (sec_*).
"""
import os
import shutil
import zipfile
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets", "Security")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "UI", "SecurityDesk")
FONT_DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts")


def load(rel):
    return Image.open(os.path.join(SRC, rel)).convert("RGBA")


def abox(im, thresh=8):
    return im.split()[3].point(lambda v: 255 if v > thresh else 0).getbbox()


def save(im, key):
    os.makedirs(DST, exist_ok=True)
    im.save(os.path.join(DST, key + ".png"))
    return key


def column_runs(alpha, box, min_w=6):
    """x-runs of non-transparent columns inside box -> [(x0,x1),...]"""
    runs, inrun = [], False
    for x in range(box[0], box[2]):
        hot = alpha.crop((x, box[1], x + 1, box[3])).getextrema()[1] > 8
        if hot and not inrun:
            runs.append([x, x]); inrun = True
        elif hot and inrun:
            runs[-1][1] = x
        elif not hot:
            inrun = False
    return [(a, b) for a, b in runs if b - a >= min_w]


def main():
    made = []

    # ---- full-frame layers: content already sits at its desk position ----
    FULL = {
        "sec_background":       "2. Background & Window/Background.png",
        "sec_background_open":  "2. Background & Window/Background Open.png",
        "sec_background_close": "2. Background & Window/Background Close.png",
        "sec_light":            "2. Background & Window/Light.png",
        "sec_window_open":      "2. Background & Window/Window Open.png",
        "sec_window_close":     "2. Background & Window/Window Close.png",
        "sec_upper_panel":      "3. Upper Panel/Upper Panel.png",
        "sec_upper_panel_bg":   "3. Upper Panel/Upper Panel BG.png",
    }
    for key, rel in FULL.items():
        made.append(save(load(rel), key))

    # ---- the header board, re-cut for the concept-measured header grid ----------------
    # The kit's Upper Panel paints its own white sanity/pause strips at ref y53..85, but the
    # concept (and the kit's own Sanity & Pause pills) put them at ref y22..60. The board is
    # shifted -31 at runtime so its furniture meets the pills; its painted strips would ghost
    # behind them, so they are painted out here with the board's own black.
    up = load("3. Upper Panel/Upper Panel.png")
    for (x0, y0, x1, y1) in ((1078, 112, 1822, 198), (2466, 112, 2770, 198)):   # px = ref*2.25
        sample = up.getpixel((max(0, x0 - 8), (y0 + y1) // 2))
        for y in range(y0, y1):
            for x in range(x0, x1):
                up.putpixel((x, y), sample)
    made.append(save(up, "sec_upper_panel_clean"))

    # ---- window band splits -----------------------------------------------------------
    # The kit's glass is shorter than the concept's booth (kit glass ref y190..435, concept
    # y187..537). Splitting the shutter layer into top/glass/base bands lets the runtime
    # stretch ONLY the glass band; hood and counter bases keep their painted proportions.
    for state in ("open", "close"):
        im = load("2. Background & Window/Window %s.png" % state.capitalize())
        made.append(save(im.crop((0, 0, im.width, 428)), "sec_window_%s_top" % state))
        made.append(save(im.crop((0, 428, im.width, 979)), "sec_window_%s_glass" % state))
        made.append(save(im.crop((0, 979, im.width, im.height)), "sec_window_%s_base" % state))

    # ---- sliced pills / icons / folders (measured alpha bboxes) ----
    def sliced(rel, key, box=None, index=None):
        im = load(rel)
        bb = box or abox(im)
        if index is not None:
            runs = column_runs(im.split()[3], bb)
            x0, x1 = runs[index]
            bb = (x0, bb[1], x1 + 1, bb[3])
        made.append(save(im.crop(bb), key))
        return bb

    sliced("4. Questions/Questions Button.png", "sec_btn_pill")            # one question-row pill
    sliced("5. Patrol & End Shift/Patrol and End Shift Button.png", "sec_wide_pill")
    sliced("7. Accept & Decline/Stamp Button.png", "sec_caption_bar")
    sliced("7. Accept & Decline/Stamp BG.png", "sec_stamp_circle_allow", index=0)
    sliced("7. Accept & Decline/Stamp BG.png", "sec_stamp_circle_deny", index=1)
    sliced("7. Accept & Decline/Accept and Decline.png", "sec_icon_accept", index=0)
    sliced("7. Accept & Decline/Accept and Decline.png", "sec_icon_decline", index=1)
    # the sanity bar and the pause block: sliced from the kit layer at their measured rects,
    # then placed at the re-baselined header grid (they land inside the board's cut-outs)
    sliced("3. Upper Panel/Sanity & Pause.png", "sec_sanity_bar", box=(1059, 48, 1841, 133))
    sliced("3. Upper Panel/Sanity & Pause.png", "sec_pause_block", box=(2521, 48, 2833, 133))
    sliced("6. Registry Terminal/Cabinet1 Files.png", "sec_folder_tab1")
    sliced("6. Registry Terminal/Cabinet2 Files.png", "sec_folder_tab2")
    sliced("5. Patrol & End Shift/Patrol.png", "sec_icon_patrol")
    sliced("5. Patrol & End Shift/End Shift.png", "sec_icon_endshift")

    # ---- cropped elements: copied as-is ----
    CROP = {
        "sec_qpanel":        "4. Questions/Questions Panel.png",
        "sec_arrow_right":   "4. Questions/Right-arrow.png",
        "sec_cabinet1":      "6. Registry Terminal/Cabinet1.png",
        "sec_cabinet2":      "6. Registry Terminal/Cabinet2.png",
        "sec_stamp":         "7. Accept & Decline/Stamp.png",
        "sec_rules_paper":   "7. Tonight Log & Rules/Rules Paper.png",
        "sec_rules_dark":    "7. Tonight Log & Rules/Rules.png",
        "sec_log_paper":     "7. Tonight Log & Rules/Tonights Log Paper.png",
        "sec_log_dark":      "7. Tonight Log & Rules/Tonights Log.png",
        "sec_log_rules":     "7. Tonight Log & Rules/TL and R.png",
        "sec_bubble":        "8. Dialouge Box/Dialouge.png",
    }
    for key, rel in CROP.items():
        im = load(rel)
        if key == "sec_bubble":
            # the kit master ships padded with transparent margins; trim to the art so the
            # 9-slice borders in ProceduralArt.FileBorders measure the real tail and edges
            bbox = im.getchannel("A").getbbox()
            if bbox:
                im = im.crop(bbox)
        made.append(save(im, key))

    # ---- fonts: unpack the two supplied families ----
    os.makedirs(FONT_DST, exist_ok=True)
    # Unity imports .otf/.ttf alike; the kit names carry spaces, so map to stable key names
    PICKS = {
        "1. Fonts/Bahnschrift-Font-Family.zip": {
            "Bahnschrift Regular.otf": "kit_bahnschrift_regular.otf",
            "Bahnschrift Bold.otf": "kit_bahnschrift_bold.otf",
            "Bahnschrift SemiBold Condensed.otf": "kit_bahnschrift_semibold_condensed.otf",
        },
        "1. Fonts/chiller.zip": {
            "Chiller.ttf": "kit_chiller.ttf",
        },
    }
    picks = []
    for zrel, want in PICKS.items():
        zpath = os.path.join(SRC, zrel)
        with zipfile.ZipFile(zpath) as z:
            have = {os.path.basename(n): n for n in z.namelist()
                    if n.lower().endswith((".ttf", ".otf")) and not n.startswith("__MACOSX")}
            for base, dst_name in want.items():
                if base not in have:
                    # family zips vary: fall back to the first member whose name contains the
                    # weight word (or the zip's only font), so the kit lands on fresh exports
                    if len(have) == 1:
                        base = next(iter(have))
                    else:
                        word = base.split()[1].lower() if " " in base else "regular"
                        cand = [n for b, n in have.items() if word in b.lower() and "__MACOSX" not in n]
                        if not cand:
                            print("  font missing:", base); continue
                        base = os.path.basename(cand[0])
                with z.open(have[base]) as src, open(os.path.join(FONT_DST, dst_name), "wb") as out:
                    shutil.copyfileobj(src, out)
                picks.append(dst_name)
    print("fonts unpacked:", picks)

    print(f"wrote {len(made)} sprites to {os.path.relpath(DST, ROOT)}")
    for k in made:
        print("  ", k)


if __name__ == "__main__":
    main()
