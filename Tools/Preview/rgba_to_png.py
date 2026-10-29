#!/usr/bin/env python3
"""Convert FlowCheck --dump-sprites output (raw .rgba + manifest.json) to .png in place.

FlowCheck exports every procedurally painted sprite as bottom-up RGBA8 bytes plus a
{key: {w, h}} manifest. Tools/Preview/render_screen.py resolves sprites by .png name,
so the headless pipeline needs this one-step conversion. Unity's GetPixels() starts at
the BOTTOM row while PNG starts at the top, hence the vertical flip.

Usage:  python3 Tools/Preview/rgba_to_png.py [dir]     # default: preview/procsprites
"""
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def main():
    d = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'preview', 'procsprites')
    manifest = json.load(open(os.path.join(d, 'manifest.json'), encoding='utf-8'))
    n = 0
    for key, meta in manifest.items():
        src = os.path.join(d, key + '.rgba')
        if not os.path.exists(src):
            continue
        w, h = int(meta['w']), int(meta['h'])
        raw = open(src, 'rb').read()
        if len(raw) < w * h * 4:
            continue
        img = Image.frombytes('RGBA', (w, h), raw[:w * h * 4])
        img = img.transpose(Image.FLIP_TOP_BOTTOM)
        img.save(os.path.join(d, key + '.png'))
        n += 1
    print(f'converted {n} procedural sprites to png in {d}')


if __name__ == '__main__':
    main()
