#!/usr/bin/env python3
"""Turn a FlowCheck --dump-screen JSON into pixel screenshots at any resolution.

The desk is laid out in 1280x720 reference units (CheckpointLayout), exactly like a
ScaleWithScreenSize canvas: AspectMatchGuard picks the matched axis, so the visible
reference area is (W*scale, H*scale) with scale = refY/H (match height, wide screens)
or refX/W (match width, tall screens), and the 1280x720 frame sits centred in it.
One dump therefore renders every breakpoint: only the pixels-per-unit changes.

Sprites resolve against the REAL Resources tree (the SecurityRoom kit first), 9-slice
borders are honoured in reference units (converted from texture pixels through the
sprite's pixelsPerUnit, exactly like uGUI), and text is set with the SAME font files the
game loads (kit Bahnschrift/Chiller extracted from Art/UI/SecurityDesk/1. Fonts/*.zip),
so what you see here is what the editor resolves - not a stand-in typeface.
"""
import json, os, re, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, 'Assets', '_Project', 'Resources')
REFW, REFH = 1280.0, 720.0

PROC = os.path.join(ROOT, 'preview', 'procsprites')   # headless-exported procedural art

# Runtime-cropped kit sprites: the game crops these out of the owner's exports with
# Sprite.Create (CheckpointView.Overlays.CropCrest / KitSkin.DrawerSheetSprite). The
# headless stubs carry no pixel buffer, so the preview mirrors the SAME crop rects
# (top-left pixels off the 789x879 / 712x722 exports) to show what the editor resolves.
RUNTIME_CROPS = {
    'sec_crest_cropped': ('sec_rules_paper', (295, 70, 125, 132)),
    'sec_cabinet2_sheet': ('sec_cabinet2', (0, 0, 712, 617)),
}

_sprite_idx = None
def sprite_path(name):
    global _sprite_idx
    if _sprite_idx is None:
        _sprite_idx = {}
        for base, _dirs, files in os.walk(RES):
            for f in files:
                if f.endswith('.png'):
                    _sprite_idx.setdefault(f[:-4], os.path.join(base, f))
        # procedural sprites (vignette, desk wash, faces...) are painted in code, so the
        # preview reads the pixels FlowCheck exported from the real generator
        if os.path.isdir(PROC):
            for f in os.listdir(PROC):
                if f.endswith('.png'):
                    _sprite_idx.setdefault(f[:-4], os.path.join(PROC, f))
        # materialise the mirrored runtime crops beside the exported sprites
        for crop_name, (src_name, box) in RUNTIME_CROPS.items():
            src = _sprite_idx.get(src_name)
            if not src:
                continue
            dst = os.path.join(PROC, crop_name + '.png')
            if not os.path.exists(dst):
                x, y, w, h = box
                Image.open(src).convert('RGBA').crop((x, y, x + w, y + h)).save(dst)
            _sprite_idx.setdefault(crop_name, dst)
    return _sprite_idx.get(name)

_font_cache = {}
def font_obj(name, px):
    key = (name, px)
    if key in _font_cache:
        return _font_cache[key]
    cand = {
        'kit_bahnschrift_regular': 'Fonts/kit_bahnschrift_regular.otf',
        'kit_bahnschrift_bold': 'Fonts/kit_bahnschrift_bold.otf',
        'kit_bahnschrift_semibold_condensed': 'Fonts/kit_bahnschrift_semibold_condensed.otf',
        'kit_chiller': 'Fonts/kit_chiller.ttf',
        'BarlowCondensed-Regular': 'Fonts/BarlowCondensed-Regular.ttf',
        'BarlowCondensed-SemiBold': 'Fonts/BarlowCondensed-SemiBold.ttf',
        'SpecialElite-Regular': 'Fonts/SpecialElite-Regular.ttf',
        'CaveatBrush-Regular': 'Fonts/CaveatBrush-Regular.ttf',
    }.get(name)
    path = os.path.join(RES, cand) if cand else None
    f = None
    if path and os.path.exists(path):
        try:
            f = ImageFont.truetype(path, px)
        except Exception:
            f = None
    if f is None:
        for p in ('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',
                  '/usr/share/fonts/dejavu/DejaVuSans.ttf'):
            if os.path.exists(p):
                f = ImageFont.truetype(p, px)
                break
    if f is None:
        f = ImageFont.load_default()
    _font_cache[key] = f
    return f

TAG_RE = re.compile(r'<[^>]+>')

def nine_slice(img, w, h, border_src, border_dst=None):
    # border_src: slice rects in TEXTURE pixels (UnityEngine's Sprite.border).
    # border_dst: where those slices land on screen, in screen pixels (border_src
    # converted through pixelsPerUnit, exactly like uGUI). Defaults to border_src
    # for dumps that predate the ppu field.
    if border_dst is None:
        border_dst = border_src
    sl, sb, sr, st = [int(round(v)) for v in border_src]
    dl, db, dr, dt = [int(round(v)) for v in border_dst]
    sw, sh = img.size
    sl = min(sl, sw // 2); sr = min(sr, sw // 2)
    st = min(st, sh // 2); sb = min(sb, sh // 2)
    dl = min(dl, w // 2); dr = min(dr, w // 2)
    dt = min(dt, h // 2); db = min(db, h // 2)
    out = Image.new('RGBA', (max(1, w), max(1, h)))
    def paste(box_src, box_dst):
        if box_dst[2] <= box_dst[0] or box_dst[3] <= box_dst[1]:
            return
        if box_src[2] <= box_src[0] or box_src[3] <= box_src[1]:
            return
        piece = img.crop(box_src).resize((box_dst[2] - box_dst[0], box_dst[3] - box_dst[1]))
        out.paste(piece, (box_dst[0], box_dst[1]))
    x1s, y1s = sl, st
    x2s, y2s = sw - sr, sh - sb
    x1d, y1d = dl, dt
    x2d, y2d = w - dr, h - db
    paste((0, 0, x1s, y1s), (0, 0, x1d, y1d))
    paste((x1s, 0, x2s, y1s), (x1d, 0, x2d, y1d))
    paste((x2s, 0, sw, y1s), (x2d, 0, w, y1d))
    paste((0, y1s, x1s, y2s), (0, y1d, x1d, y2d))
    paste((x1s, y1s, x2s, y2s), (x1d, y1d, x2d, y2d))
    paste((x2s, y1s, sw, y2s), (x2d, y1d, w, y2d))
    paste((0, y2s, x1s, sh), (0, y2d, x1d, h))
    paste((x1s, y2s, x2s, sh), (x1d, y2d, x2d, h))
    paste((x2s, y2s, sw, sh), (x2d, y2d, w, h))
    return out

def tint(im, color, alpha):
    r, g, b, a = color
    if (r, g, b) == (255, 255, 255) and a >= 0.999 and alpha >= 0.999:
        return im
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            cr, cg, cb, ca = px[x, y]
            px[x, y] = (cr * r // 255, cg * g // 255, cb * b // 255,
                        min(255, int(ca * a * alpha)))
    return im

def draw_text_node(node, w, h, ppu):
    t = node['txt']
    sc = float(t.get('sc', 1.0) or 1.0)
    # a condensed label (localScale.x < 1) keeps its glyph HEIGHT and loses width: lay the
    # lines out in the unscaled box at full pixel size, then squeeze the surface horizontally
    orig_w = w
    if abs(sc - 1.0) > 1e-3 and sc > 0.01:
        w = w / sc
    px = max(6, int(round(t['s'] * ppu)))
    raw = TAG_RE.sub('', t['t'])
    if t.get('bf'):
        # uGUI best-fit: the LARGEST size in [min, min(base, max)] whose wrapped lines fit
        # the box; if even the minimum cannot fit, the minimum wins (vertical overflow
        # then saves the glyphs instead of clipping them)
        lo = max(6, int(t.get('bn', 11)))
        hi = min(int(round(t['s'])), int(t.get('bx', t['s'])))

        def wrapped_lines(f2):
            n = 0
            for para in raw.split('\n'):
                if t['w'] == 0:
                    cur = ''
                    cnt = 1
                    for word in para.split(' '):
                        trial = (cur + ' ' + word).strip()
                        if cur and f2.getlength(trial) > w - 2:
                            cnt += 1
                            cur = word
                        else:
                            cur = trial
                    n += cnt
                else:
                    n += 1
            return n

        f = font_obj(t['f'], px)
        for size in range(hi, lo - 1, -1):
            spx = max(6, int(round(size * ppu)))
            f2 = font_obj(t['f'], spx)
            asc2, desc2 = f2.getmetrics()
            if (asc2 + desc2) * t.get('ls', 1.0) * wrapped_lines(f2) <= h:
                px, f = spx, f2
                break
        else:
            spx = max(6, int(round(lo * ppu)))
            px, f = spx, font_obj(t['f'], spx)
    else:
        f = font_obj(t['f'], px)
    bold = t['st'] in (1, 3)
    color = tuple(t['c'][:3]) + (int(255 * t['c'][3] * node.get('al', 1)),)
    pad = 4
    surf = Image.new('RGBA', (max(1, int(w) + pad * 2), max(1, int(h) + pad * 2)))
    d = ImageDraw.Draw(surf)
    lines = []
    for para in raw.split('\n'):
        if t['w'] == 0:  # Wrap
            cur = ''
            for word in para.split(' '):
                trial = (cur + ' ' + word).strip()
                if d.textlength(trial, font=f) <= w - 2 or not cur:
                    cur = trial
                else:
                    lines.append(cur)
                    cur = word
            lines.append(cur)
        else:
            lines.append(para)
    asc, desc = f.getmetrics()
    lh = (asc + desc) * t.get('ls', 1.0)
    total = lh * len(lines)
    al = t['al']
    if al in (0, 1, 2):
        y0 = pad
    elif al in (3, 4, 5):
        y0 = pad + (h - total) / 2.0
    else:
        y0 = pad + (h - total)
    for i, ln in enumerate(lines):
        lw = d.textlength(ln, font=f)
        if al in (0, 3, 6):
            x = pad
        elif al in (1, 4, 7):
            x = pad + (w - lw) / 2.0
        else:
            x = pad + (w - lw)
        y = y0 + i * lh
        d.text((x, y), ln, font=f, fill=color)
        if bold:
            off = max(1, px // 24)
            d.text((x + off, y), ln, font=f, fill=color)
    if abs(sc - 1.0) > 1e-3:
        surf = surf.resize((max(1, int(orig_w) + pad * 2), max(1, int(h) + pad * 2)),
                           Image.LANCZOS)
    return surf

def render(dump, W, H, out_path):
    ppu_x = W / REFW
    aspect = W / float(H)
    match_h = aspect > REFW / REFH
    scale = (REFH / H) if match_h else (REFW / W)   # ref units per screen pixel
    vis_w, vis_h = W * scale, H * scale
    ox, oy = (vis_w - REFW) / 2.0, (vis_h - REFH) / 2.0
    ppu = 1.0 / scale                                # screen pixels per ref unit
    if dump.get('space') == 'visible':
        # menu dumps are walked in VISIBLE reference space (origin = window top-left),
        # so there is no centred 1280x720 frame to offset into
        vis_w, vis_h = float(dump['refw']), float(dump['refh'])
        ox = oy = 0.0
        ppu = W / vis_w
    canvas = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    for node in dump['nodes']:
        w, h = node['w'] * ppu, node['h'] * ppu
        cx = (ox + node['x'] + node['w'] / 2.0) * ppu
        cy = (oy + node['y'] + node['h'] / 2.0) * ppu
        iw, ih = max(1, int(round(w))), max(1, int(round(h)))
        surf = None
        if 'img' in node:
            im = node['img']
            color = (im['c'][0], im['c'][1], im['c'][2], im['c'][3])
            alpha = node.get('al', 1.0)
            path = sprite_path(im['s']) if im['s'] else None
            if path:
                src = Image.open(path).convert('RGBA')
                if im.get('t') == 1 and 'b' in im:
                    # uGUI converts slice borders from texture pixels through the sprite's
                    # pixelsPerUnit into UI units; honour them in reference units the same way
                    pu = float(im.get('pu', 100.0)) or 100.0
                    border = [v / pu * ppu for v in im['b']]
                    surf = nine_slice(src, iw, ih, im['b'], border)
                elif im.get('pa'):
                    sw, sh = src.size
                    s = min(w / sw, h / sh)
                    nw, nh = max(1, int(sw * s)), max(1, int(sh * s))
                    big = src.resize((nw, nh))
                    surf = Image.new('RGBA', (iw, ih))
                    surf.paste(big, ((iw - nw) // 2, (ih - nh) // 2))
                else:
                    surf = src.resize((iw, ih))
                surf = tint(surf, (color[0], color[1], color[2], 1.0), color[3] * alpha)
            else:
                surf = Image.new('RGBA', (iw, ih),
                                 (color[0], color[1], color[2], int(255 * color[3] * alpha)))
        elif 'txt' in node:
            surf = draw_text_node(node, w, h, ppu)
        if surf is None:
            continue
        if node.get('inp', {}).get('f'):
            # visible focus state: the accent ring uGUI's selection shows on the text box
            ring = Image.new('RGBA', (iw + 2, ih + 2), (0, 0, 0, 0))
            rd = ImageDraw.Draw(ring)
            rd.rectangle((0, 0, iw + 1, ih + 1), outline=(178, 34, 40, 235))
            if surf is None:
                surf = ring
            else:
                # text surfaces carry padding around the box; centre the ring on it
                base = Image.new('RGBA', surf.size, (0, 0, 0, 0))
                base.alpha_composite(ring, ((surf.size[0] - iw - 2) // 2,
                                            (surf.size[1] - ih - 2) // 2))
                base.alpha_composite(surf)
                surf = base
            sw0, sh0 = surf.size
        ang = node.get('a', 0.0)
        if abs(ang) > 0.01:
            surf = surf.rotate(ang, expand=True, resample=Image.BICUBIC)
        sw, sh = surf.size
        canvas.alpha_composite(surf, (int(round(cx - sw / 2.0)), int(round(cy - sh / 2.0))))
    canvas.convert('RGB').save(out_path)
    return out_path

if __name__ == '__main__':
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument('--dump', required=True)
    ap.add_argument('--out', required=True)
    ap.add_argument('--res', nargs='+', default=['1920x1080'])
    a = ap.parse_args()
    dump = json.load(open(a.dump))
    os.makedirs(a.out, exist_ok=True)
    for r in a.res:
        w, h = (int(v) for v in r.lower().split('x'))
        p = os.path.join(a.out, f'screen_{r}.png')
        render(dump, w, h, p)
        print('wrote', p)
