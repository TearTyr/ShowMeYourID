#!/usr/bin/env python3
"""Renders the ground-floor map sheet to SVG from the REAL game data.

Why this exists: the Unity editor cannot run headlessly here, so "is the map centred,
are the rooms where the reference drawing puts them, do the labels fit?" would otherwise
be answered by reading code. This tool parses the actual numbers out of
Assets/_Project/Scripts/GameLogic/FloorPlan.cs and the actual layout maths out of
Assets/_Project/Scripts/UI/ProgressionScreens.cs (constants only), applies the same
fit/centre formula the view applies, and draws the result — including a screen outline
at several resolutions, so centring and clipping are visible rather than assumed.

Usage:
    python3 Tools/Preview/render_map_preview.py [outdir]        # default: /tmp/map_preview
"""
import math
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PLAN_CS = os.path.join(ROOT, "Assets/_Project/Scripts/GameLogic/FloorPlan.cs")
SHEET_CS = os.path.join(ROOT, "Assets/_Project/Scripts/GameLogic/MapSheetLayout.cs")
MAP_CS = os.path.join(ROOT, "Assets/_Project/Scripts/UI/ProgressionScreens.cs")

# ---------------------------------------------------------------- parse FloorPlan
def parse_floor_plan(path):
    src = open(path, encoding="utf-8").read()
    dw = float(re.search(r"DesignW\s*=\s*([0-9.]+)f", src).group(1))
    dh = float(re.search(r"DesignH\s*=\s*([0-9.]+)f", src).group(1))
    title = re.search(r'Title\s*=\s*"([^"]+)"', src).group(1)

    rooms = []
    body = src[src.index("Rooms ="):]
    body = body[:body.index("        };")]
    for block in re.findall(r"new FloorRoom\s*\{(.*?)\n            \},", body, re.S):
        def grab(key, cast=str):
            m = re.search(key + r"\s*=\s*([-0-9.]+)f", block)
            return cast(m.group(1)) if m else None
        key = re.search(r'Key\s*=\s*"([^"]+)"', block).group(1)
        label = re.search(r'Label\s*=\s*"((?:[^"\\]|\\.)*)"', block).group(1)
        label = label.replace("\\n", "\n")
        rooms.append({
            "key": key,
            "label": label,
            "x": grab("X", float), "y": grab("Y", float),
            "w": grab("W", float), "h": grab("H", float),
            "room": (re.search(r"Room\s*=\s*RoomId\.(\w+)", block).group(1)
                     if re.search(r"Room\s*=\s*RoomId\.(\w+)", block) else None),
            "sealed": "Sealed = true" in block,
            "comfort": "Comfort = true" in block,
            "door": (grab("DoorX", float), grab("DoorY", float)) if "DoorX" in block else None,
        })

    corridors = []
    cbody = src[src.index("Corridors ="):]
    cbody = cbody[:cbody.index("        };")]
    for nums in re.findall(r"new\[\]\s*\{([^}]+)\}", cbody):
        vals = [float(v.strip().rstrip("f")) for v in nums.split(",")]
        corridors.append(vals)

    # the reference's stair hatch + scratch note (decoration, same chalk as the runs)
    stairs = []
    if "StairsRuns" in src:
        sbody = src[src.index("StairsRuns ="):]
        sbody = sbody[:sbody.index("        };")]
        for nums in re.findall(r"new\[\]\s*\{([^}]+)\}", sbody):
            vals = [float(v.strip().rstrip("f")) for v in nums.split(",")]
            stairs.append(vals)
    note = ""
    m = re.search(r'StairsNote\s*=\s*"([^"]+)"', src)
    if m: note = m.group(1)
    nx = float(re.search(r"StairsNoteX\s*=\s*([0-9.]+)f", src).group(1)) if "StairsNoteX" in src else 0.0
    ny = float(re.search(r"StairsNoteY\s*=\s*([0-9.]+)f", src).group(1)) if "StairsNoteY" in src else 0.0
    return dw, dh, title, rooms, corridors, stairs, note, nx, ny


# --------------------------------------- parse the sheet constants (pure layout module)
def parse_sheet_constants(path):
    """Reads MapSheetLayout's constants — the SAME source the view and LogicTests use."""
    src = open(path, encoding="utf-8").read()
    C = {}
    for m in re.finditer(r"public const float (\w+) = ([0-9.]+)f", src):
        C[m.group(1)] = float(m.group(2))
    for m in re.finditer(r"public const float (\w+) = ([0-9.]+)f, (\w+) = ([0-9.]+)f", src):
        C[m.group(1)] = float(m.group(2))
        C[m.group(3)] = float(m.group(4))
    return C


def parse_type_sizes(path):
    """The design-space type sizes still live on the view (they are a styling choice)."""
    src = open(path, encoding="utf-8").read()
    m = re.search(r"private const int ([^;]+);", src)
    sizes = {}
    for name, val in re.findall(r"(\w+) = (\d+)", m.group(1)):
        sizes[name] = int(val)
    return sizes


# ------------------------------------------------------------------- the same maths
def layout(screen_w, screen_h, dw, dh, C):
    """Mirrors MapView.Relayout: the visible reference space (AspectMatchGuard), the sheet
    fit, the plan fit inside the sheet, and the CLOSE row under it."""
    # AspectMatchGuard: wider than 16:9 -> match HEIGHT (matchWidthOrHeight = 1), otherwise
    # match WIDTH. UIFactory.VisibleReferenceSize then divides the screen px by
    # reference / screen, so the visible reference area is exactly what the guard leaves.
    aspect = screen_w / screen_h
    if aspect > 1280.0 / 720.0:
        visible = (screen_w * 720.0 / screen_h, 720.0)
    else:
        visible = (1280.0, screen_h * 1280.0 / screen_w)

    # ---- MapSheetLayout.Fit, transcribed 1:1 ----
    avail_w = max(240.0, visible[0] - 24.0)
    avail_h = max(200.0, visible[1] - C["ChromeH"])
    paper_scale = min(avail_w / dw, avail_h / dh)
    paper_w, paper_h = dw * paper_scale, dh * paper_scale

    content_w = dw - C["Margin"] * 2
    content_h = dh - C["Margin"] * 2 - C["TitleBandH"] - C["ControlBandH"]
    plan_scale = min((content_w - C["PlanPad"] * 2) / dw,
                     (content_h - C["PlanPad"] * 2) / dh) * paper_scale
    plan_text = min(max(plan_scale, C["PlanTextMin"]), C["PlanTextMax"])
    chrome_text = min(max(paper_scale, C["ChromeTextMin"]), C["ChromeTextMax"])

    close_w, close_h = C["CloseRowW"], C["CloseRowH"]
    return {
        "visible": visible,
        "paper": (paper_w, paper_h), "paper_scale": paper_scale,
        "plan_scale": plan_scale,
        "plan_text": plan_text, "chrome_text": chrome_text,
        # the title band is taller than the control band, so the drawing sits this far UP
        "plan_centre_y": (C["TitleBandH"] - C["ControlBandH"]) / 2.0,
        "close": (close_w, close_h,
                  visible[1] / 2 - paper_h / 2 - C["CloseGap"] - close_h / 2),
        "content_w": content_w, "margin": C["Margin"],
    }


# ----------------------------------------------------------------------- the render
INK = "#c7bda9"
INK_FAINT = "#8d8577"
RED = "#a82a2e"
RED_BRIGHT = "#e6383d"
PAPER = "#1d1b18"


def render(screen_w, screen_h, dw, dh, title, rooms, corridors, C, out_path,
           stairs=(), note="", note_x=0.0, note_y=0.0):
    L = layout(screen_w, screen_h, dw, dh, C)
    vx, vy = L["visible"]
    ps = L["plan_scale"]
    pw, ph = L["paper"]

    W, H = vx + 80, vy + 80          # svg canvas: the visible reference area + a margin
    ox, oy = W / 2, H / 2            # screen centre in svg space

    def plan_pt(x, y):
        """design space -> svg space. Design y points DOWN and so does svg y, so the drawing
        is NOT mirrored here — which mirrors the view: ToAnchoredY negates for RectTransform's
        y-up space, and Anchor's y-up then puts it back down again."""
        return (ox + x * ps, oy - L["plan_centre_y"] + y * ps)

    s = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W:.0f}" height="{H:.0f}" '
         f'viewBox="0 0 {W:.0f} {H:.0f}" font-family="sans-serif">']
    s.append(f'<rect width="{W}" height="{H}" fill="#08080a"/>')
    # the visible reference area (what the player actually has)
    s.append(f'<rect x="{ox-vx/2}" y="{oy-vy/2}" width="{vx}" height="{vy}" '
             f'fill="#0c0c0e" stroke="#2a2a30" stroke-dasharray="4 4"/>')
    s.append(f'<text x="{ox-vx/2+6}" y="{oy-vy/2+14}" fill="#4a4a52" font-size="11">'
             f'screen {screen_w}x{screen_h}  ->  visible reference {vx:.0f}x{vy:.0f}</text>')
    # dimmer
    s.append(f'<rect x="{ox-vx/2}" y="{oy-vy/2}" width="{vx}" height="{vy}" fill="#000" opacity="0.55"/>')

    # ---- the sheet (dead centre) ----
    s.append(f'<g transform="rotate(-0.35 {ox} {oy})">')
    s.append(f'<rect x="{ox-pw/2}" y="{oy-ph/2}" width="{pw}" height="{ph}" '
             f'fill="{PAPER}" stroke="#3a352c" stroke-width="2"/>')
    # ageing: a few stains + folds, echoing the procedural map_paper sprite
    for (cx, cy, r) in [(0.22, 0.3, 0.09), (0.78, 0.62, 0.07), (0.5, 0.85, 0.05)]:
        s.append(f'<circle cx="{ox-pw/2+cx*pw}" cy="{oy-ph/2+cy*ph}" r="{r*pw:.1f}" '
                 f'fill="none" stroke="#4a3a22" stroke-width="2" opacity="0.35"/>')
    s.append(f'<line x1="{ox}" y1="{oy-ph/2}" x2="{ox}" y2="{oy+ph/2}" stroke="#000" opacity="0.35"/>')
    s.append(f'<line x1="{ox-pw/2}" y1="{oy-ph/2+0.46*ph}" x2="{ox+pw/2}" y2="{oy-ph/2+0.46*ph}" '
             f'stroke="#000" opacity="0.3"/>')

    # ---- title / rule / subtitle ----
    m = L["margin"]
    tsize = max(9, round(C["TitleSize"] * L["chrome_text"]))
    ssize = max(9, round(C["SubTitleSize"] * L["chrome_text"]))
    lsize = max(9, round(C["LegendSize"] * L["chrome_text"]))
    hsize = max(9, round(C["HintSize"] * L["chrome_text"]))
    s.append(f'<text x="{ox}" y="{oy-ph/2+m+6+tsize*1.05:.1f}" fill="#ececec" font-size="{tsize}" '
             f'text-anchor="middle" font-family="cursive,serif" letter-spacing="1">{title}</text>')
    rule_y = oy - ph/2 + m + 56
    s.append(f'<line x1="{ox-L["content_w"]*0.30:.1f}" y1="{rule_y:.1f}" '
             f'x2="{ox+L["content_w"]*0.30:.1f}" y2="{rule_y:.1f}" '
             f'stroke="{INK_FAINT}" stroke-width="2" opacity="0.5"/>')
    s.append(f'<text x="{ox}" y="{oy-ph/2+m+60+ssize*1.0:.1f}" fill="{INK_FAINT}" font-size="{ssize}" '
             f'text-anchor="middle">NIGHT PATROL REFERENCE  ·  DISTRICT FILE 3-B</text>')

    # ---- corridors, rooms, doors ----
    for (x0, y0, x1, y1) in corridors:
        a, b = plan_pt(x0, y0), plan_pt(x1, y1)
        s.append(f'<line x1="{a[0]:.1f}" y1="{a[1]:.1f}" x2="{b[0]:.1f}" y2="{b[1]:.1f}" '
                 f'stroke="{INK}" stroke-width="{max(2, 3*ps):.1f}" opacity="0.55"/>')

    for r in rooms:
        cx, cy = plan_pt(r["x"], r["y"])
        w, h = r["w"] * ps, r["h"] * ps
        x, y = cx - w/2, cy - h/2
        if r["sealed"]:
            fill, stroke, label_col = "#33100f", RED, RED_BRIGHT
            s.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" '
                     f'fill="{fill}" stroke="{stroke}" stroke-width="2.5"/>')
            step = 11
            s.append(f'<clipPath id="clip{r["key"]}"><rect x="{x:.1f}" y="{y:.1f}" '
                     f'width="{w:.1f}" height="{h:.1f}"/></clipPath>')
            s.append(f'<g clip-path="url(#clip{r["key"]})" stroke="{RED}" stroke-width="2" opacity="0.55">')
            for k in range(int(-h), int(w) + step, step):
                s.append(f'<line x1="{x+k:.1f}" y1="{y+h:.1f}" x2="{x+k+h:.1f}" y2="{y:.1f}"/>')
            s.append('</g>')
        else:
            fill = "#2a2723" if r["comfort"] else "#26241f"
            s.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" '
                     f'fill="{fill}" fill-opacity="0.75" stroke="{INK}" stroke-width="2"/>')
            s.append(f'<rect x="{x+3:.1f}" y="{y+3:.1f}" width="{w-6:.1f}" height="{h-6:.1f}" '
                     f'fill="none" stroke="{INK_FAINT}" stroke-width="1" opacity="0.6"/>')
            label_col = "#ececec"
        # the label, centred in the room, at the size the view computes
        size = 20 if r["label"] == "MAIN HALLWAY" else (13 if r["sealed"] else (13 if r["comfort"] else 15))
        size = max(9, round(size * L["plan_text"]))
        lines = r["label"].split("\n")
        lh = size * 1.05
        y0 = cy - lh * (len(lines) - 1) / 2 + size * 0.35
        for i, ln in enumerate(lines):
            s.append(f'<text x="{cx:.1f}" y="{y0 + i*lh:.1f}" fill="{label_col}" font-size="{size}" '
                     f'text-anchor="middle" font-family="cursive,serif">{ln}</text>')
        # doorway
        if r["door"]:
            dx, dy = plan_pt(r["door"][0], r["door"][1])
            hall = next(q for q in rooms if q["room"] == "Hallway")
            horizontal = hall["y"] - hall["h"]/2 + 1 < r["door"][1] < hall["y"] + hall["h"]/2 - 1
            dw2, dh2 = ((C["DoorLongWallW"], C["DoorLongWallH"]) if horizontal
                        else (C["DoorEndWallW"], C["DoorEndWallH"]))
            dw2, dh2 = dw2 * ps / 2, dh2 * ps / 2
            col = RED if r["sealed"] else "#d9d2c2"
            s.append(f'<rect x="{dx-dw2:.1f}" y="{dy-dh2:.1f}" width="{dw2*2:.1f}" height="{dh2*2:.1f}" '
                     f'fill="{PAPER}" stroke="{col}" stroke-width="1.6"/>')

    # ---- stair hatch + the custodian's scratch (reference decoration) ----
    for (x0, y0, x1, y1) in stairs:
        a, b = plan_pt(x0, y0), plan_pt(x1, y1)
        s.append(f'<line x1="{a[0]:.1f}" y1="{a[1]:.1f}" x2="{b[0]:.1f}" y2="{b[1]:.1f}" '
                 f'stroke="{INK}" stroke-width="{max(1.5, 2*ps):.1f}" opacity="0.5"/>')
    if note:
        nxp, nyp = plan_pt(note_x, note_y)
        s.append(f'<text x="{nxp:.1f}" y="{nyp:.1f}" fill="{INK_FAINT}" '
                 f'font-size="{max(9, round(13*L["plan_text"]))}" font-family="cursive,serif">{note}</text>')

    # ---- YOU marker on the checkpoint ----
    sec = next(q for q in rooms if q["room"] == "Security")
    hx, hy = plan_pt(sec["x"], sec["y"])
    mr = 13 * ps
    s.append(f'<circle cx="{hx:.1f}" cy="{hy:.1f}" r="{mr:.1f}" fill="none" stroke="{RED_BRIGHT}" stroke-width="2.5"/>')
    s.append(f'<text x="{hx:.1f}" y="{hy+mr+max(9, round(13*L["plan_text"]))*1.05:.1f}" fill="{RED_BRIGHT}" '
             f'font-size="{max(9, round(13*L["plan_text"]))}" text-anchor="middle">YOU</text>')

    # ---- legend + hint ----
    s.append(f'<text x="{ox}" y="{oy+ph/2-m-28+lsize*0.8:.1f}" fill="{INK_FAINT}" font-size="{lsize}" '
             f'text-anchor="middle">RED HATCH = SEALED, NO ENTRY        RED DOT = A FINDING YOU HAVE LOGGED</text>')
    hint = ("Click a room to move there. Red dots are findings you have discovered."
            if False else "From the checkpoint the map is reference only — use PATROL HALLWAY to investigate.")
    s.append(f'<text x="{ox}" y="{oy+ph/2-m-6+hsize*0.8:.1f}" fill="#e0dcd2" font-size="{hsize}" '
             f'text-anchor="middle">{hint}</text>')
    s.append('</g>')

    # ---- CLOSE, under the sheet ----
    cw, ch, cy_from_bottom = L["close"]
    # bottom-edge anchor + CENTRE pivot: anchoredPosition.y is measured up from the bottom
    # of the visible area to the row's centre (same rule the view uses)
    ccx = ox
    ccy = (oy + vy/2) - cy_from_bottom      # bottom-edge anchor, centre pivot
    s.append(f'<rect x="{ccx-cw/2:.1f}" y="{ccy-ch/2:.1f}" width="{cw:.1f}" height="{ch:.1f}" '
             f'fill="#0e0e10" stroke="#5a5346" stroke-width="1.5"/>')
    s.append(f'<text x="{ccx:.1f}" y="{ccy+5:.1f}" fill="#ececec" font-size="14" '
             f'text-anchor="middle">CLOSE  [M]</text>')
    s.append('</svg>')

    open(out_path, "w", encoding="utf-8").write("\n".join(s))

    # ---- the numbers a reviewer would otherwise have to take on trust ----
    sheet_bottom = oy + ph / 2
    close_top = ccy - ch / 2
    return {
        "visible": (vx, vy), "sheet": (pw, ph), "sheet_centre": (ox, oy),
        "sheet_edges": (ox - pw/2, oy - ph/2, ox + pw/2, oy + ph/2),
        # svg y grows DOWN, so the gap is the close row's top minus the sheet's bottom
        "close_top": close_top, "gap_under_sheet": close_top - sheet_bottom,
        "close_bottom": ccy + ch / 2, "bottom_margin": oy + vy/2 - (ccy + ch/2),
        "plan_scale": ps, "plan_text": L["plan_text"], "chrome_text": L["chrome_text"],
        "title_px": tsize, "close_centre_from_bottom": cy_from_bottom,
    }


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else "/tmp/map_preview"
    os.makedirs(outdir, exist_ok=True)
    dw, dh, title, rooms, corridors, stairs, note, nx, ny = parse_floor_plan(PLAN_CS)
    C = parse_sheet_constants(SHEET_CS)
    C.update(parse_type_sizes(MAP_CS))
    print(f"parsed FloorPlan: {dw:.0f}x{dh:.0f}, {len(rooms)} rooms, {len(corridors)} corridor runs")
    print(f"parsed MapView constants: { {k: v for k, v in sorted(C.items())} }\n")
    print(f"{'screen':>12} | {'visible ref':>12} | {'sheet':>12} | {'plan':>6} | {'title':>5} | "
          f"{'gap under sheet':>15} | {'bottom margin':>13} | clipped?")
    print("-" * 112)
    for (w, h) in [(1280, 720), (1920, 1080), (1024, 768), (800, 600), (2560, 1080),
                   (1280, 600), (3840, 2160)]:
        info = render(w, h, dw, dh, title, rooms, corridors, C,
                      os.path.join(outdir, f"map_{w}x{h}.svg"),
                      stairs, note, nx, ny)
        x0, y0, x1, y1 = info["sheet_edges"]
        vx, vy = info["visible"]
        ox, oy = (vx + 80) / 2, (vy + 80) / 2      # same svg frame the renderer uses
        left, top, right, bottom = ox - vx/2, oy - vy/2, ox + vx/2, oy + vy/2
        clipped = (x0 < left - 0.5 or x1 > right + 0.5 or y0 < top - 0.5 or y1 > bottom + 0.5 or
                   info["close_bottom"] > bottom + 0.5 or info["gap_under_sheet"] < 0)
        print(f"{w}x{h:>5} | {vx:>5.0f}x{vy:<5.0f} | {info['sheet'][0]:>5.0f}x{info['sheet'][1]:<5.0f} | "
              f"{info['plan_scale']:>6.3f} | {info['title_px']:>5} | {info['gap_under_sheet']:>15.1f} | "
              f"{info['bottom_margin']:>13.1f} | {'YES' if clipped else 'no'}")
    print(f"\nSVG written to {outdir}/")


if __name__ == "__main__":
    main()
