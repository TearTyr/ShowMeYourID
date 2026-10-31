#!/usr/bin/env python3
"""Renders the ACCEPT stamp placeholder into Assets/_Project/Resources/Art/stamp_allow.png.

Why a generator and not a hand-made/AI raster
---------------------------------------------
* The project paints every sprite in code (ProceduralArt) and prefers a dropped-in file at
  `Resources/Art/<key>.png` when one exists - this writes exactly that override for the ONE
  key the task asks for (`stamp_allow`). The procedural painter stays in place as the
  fallback, so nothing is deleted or replaced.
* A worn rubber stamp needs a real alpha channel and muted flat ink; generating it keeps the
  result deterministic, reproducible and tweakable (re-run to re-roll the wear).

Art direction (matches MakeStamp + the desk's monochrome stock)
---------------------------------------------------------------
* double ring emblem (bold outer, thinner inner) with a heavy check mark inside
* muted, low-saturation green-grey ink - the ACCEPT tray tint (0.42,0.58,0.42) pulled dark,
  so it sits with the warm paper stock (#dbd9d2) and the cool UI ink (#3a3a40)
* worn press: low-frequency alpha mottling, ink-starved specks, two chips on the outer ring
* transparent background; RGB stays the ink colour even where alpha is 0, so bilinear
  filtering can never fringe

Sized for the REDUCED display size
----------------------------------
Authored at 512 and box/LANCZOS-downsampled to 128 px, i.e. ~3.3x the 39 px the mark now
renders at (CheckpointView.StampMarkSize) and still >=2x at the 1.5x desk zoom. Strokes are
deliberately chunky so the ring, the inner ring and the check all survive that downscale.

Usage:  python3 Tools/make_stamp_asset.py [--size 128] [--seed 4] [--out PATH] [--preview PATH]
"""
import argparse
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter

# ---- art constants (512 px working canvas = 4x the shipped 128 px asset) -----------------
WORK = 512
INK_RGB = (62, 71, 62)          # muted dark green-grey: saturation ~13%, reads as approval ink
INK_ALPHA = 0.94                # never fully opaque - pressed ink on paper stock
OUTER_R, OUTER_W = 214, 32      # bold outer ring
INNER_R, INNER_W = 168, 24      # thinner inner ring (a classic rubber-stamp double border)
CHECK_PTS = ((150, 262), (220, 338), (360, 186))
CHECK_W = 56
C = WORK // 2


def disc(draw, cx, cy, r, fill):
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill)


def press(mask, seed):
    """Wear the ink *slightly*: mottling, starvation specks, one chip per ring. The wear must
    never break the emblem's readability at 39 px, so it bites thin bands only."""
    import random

    # low-frequency alpha mottle (0.82..1.0 of the pressed ink) - an uneven rubber pad
    noise = Image.effect_noise((WORK, WORK), 128).filter(ImageFilter.GaussianBlur(7))
    mottle = noise.point(lambda v: int(209 + (v / 255.0) * 46))
    mask = ImageChops.multiply(mask, mottle)

    draw = ImageDraw.Draw(mask)
    rnd = random.Random(seed)

    # ink-starved specks (only bite where ink already is: multiply keeps the background clear)
    for _ in range(140):
        x, y = rnd.randrange(WORK), rnd.randrange(WORK)
        r = rnd.uniform(1.0, 2.8)
        draw.ellipse([x - r, y - r, x + r, y + r], fill=0)

    # one chip per ring: erase an ARC of that ring's own band, never a wedge to the centre
    for radius, width in ((OUTER_R, OUTER_W), (INNER_R, INNER_W)):
        a0 = rnd.uniform(0, 360)
        span = rnd.uniform(7, 13)
        draw.arc([C - radius, C - radius, C + radius, C + radius], a0, a0 + span,
                 fill=0, width=width + 6)
    return mask


def build(seed):
    mask = Image.new("L", (WORK, WORK), 0)
    d = ImageDraw.Draw(mask)

    # emblem: double ring
    d.ellipse([C - OUTER_R, C - OUTER_R, C + OUTER_R, C + OUTER_R], outline=255, width=OUTER_W)
    d.ellipse([C - INNER_R, C - INNER_R, C + INNER_R, C + INNER_R], outline=255, width=INNER_W)

    # the check: round joins + round caps (PIL lines are butt-capped)
    d.line(list(CHECK_PTS), fill=255, width=CHECK_W, joint="curve")
    for x, y in CHECK_PTS:
        disc(d, x, y, CHECK_W / 2.0, 255)

    mask = press(mask, seed)
    # 4x supersample -> shipped size: clean anti-aliased edges that survive a bilinear downscale
    return mask


def to_rgba(mask, size):
    alpha = mask.resize((size, size), Image.LANCZOS).point(lambda v: int(v * INK_ALPHA))
    img = Image.new("RGBA", (size, size), INK_RGB + (255,))   # RGB = ink everywhere (no fringe)
    img.putalpha(alpha)
    return img


def preview(img, path, size):
    """Stamp on the desk's paper stock at the real display sizes, 6x up for inspection."""
    paper = (219, 217, 210)                                    # the slip's stock (#dbd9d2)
    pad, scale = 12, 6
    cells = [("39 px  (new mark)", 39), ("56 px  (old mark)", 56), ("zoom 1.5x (58 px)", 58)]
    w = pad + sum(c[1] * scale + pad for c in cells)
    h = pad + 58 * scale + 34 + pad
    sheet = Image.new("RGBA", (w, h), paper + (255,))
    dr = ImageDraw.Draw(sheet)
    x = pad
    for label, px in cells:
        mark = img.resize((px, px), Image.LANCZOS)
        sheet.alpha_composite(mark.resize((px * scale, px * scale), Image.NEAREST), (x, pad))
        dr.text((x, pad + 58 * scale + 8), label, fill=(58, 58, 64, 255))
        x += px * scale + pad
    sheet.convert("RGB").save(path)
    return path


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    ap = argparse.ArgumentParser()
    ap.add_argument("--size", type=int, default=128)
    ap.add_argument("--seed", type=int, default=4)
    ap.add_argument("--out", default=os.path.join(root, "Assets/_Project/Resources/Art/stamp_allow.png"))
    ap.add_argument("--preview", default=os.path.join(here, "stamp_preview.png"))
    args = ap.parse_args()

    img = to_rgba(build(args.seed), args.size)
    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    img.save(args.out, "PNG", optimize=True)

    # ---- self-check: the acceptance numbers, printed so the report can quote them ----
    check = Image.open(args.out).convert("RGBA")
    a = check.getchannel("A")
    corners = [a.getpixel(p) for p in ((0, 0), (args.size - 1, 0), (0, args.size - 1),
                                       (args.size - 1, args.size - 1))]
    hist = a.histogram()
    opaque = sum(hist[96:]) / float(args.size * args.size)
    small = check.resize((39, 39), Image.BOX)
    sa = small.getchannel("A").histogram()
    ink_px = sum(sa[64:]) / float(39 * 39)
    print("wrote", os.path.relpath(args.out, root))
    print("  size        : %dx%d, mode %s" % (check.width, check.height, check.mode))
    print("  background  : corner alphas %s (0 = fully transparent)" % corners)
    print("  ink coverage: %.1f%% of the asset, %.1f%% of a 39x39 downscale" % (opaque * 100, ink_px * 100))
    print("  ink colour  : rgb%s @ alpha %.2f" % (INK_RGB, INK_ALPHA))
    print("  preview     :", preview(check, args.preview, args.size))


if __name__ == "__main__":
    main()
