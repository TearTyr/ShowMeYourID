#!/usr/bin/env python3
"""Regenerates docs/asset-manifest.csv - the single naming contract for the art/audio team.

Why this script exists: every name in the manifest is a LIVE key in the game. Art dropped at
Assets/_Project/Resources/Art/<key>.png (or audio at Resources/Audio/<key>.wav) replaces the
procedural placeholder with zero code changes, so the CSV must never drift from the code. The
lists below mirror ProceduralArt.AllKeys / ProceduralAudio.AllKeys / FaceAssets, and FlowCheck
proves the mirror in both directions (`== asset manifest sync ==`). Edit the priorities/specs
here, re-run, and commit the CSV.
"""
import csv
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "asset-manifest.csv")

# --------------------------------------------------------------------------- art
# (key, category, priority, status, spec, used_by, notes)
ART = [
    # ---- checkpoint desk documents (the Papers-Please layer) ----
    ("id_card_frame",   "desk-doc",  "P0", "procedural", "PNG RGBA 320x220 px @100 ppu (prints 1:1 in the desk grid); photo slot + 6 text rows are drawn ON TOP by code - deliver the blank card art only", "CheckpointView (ID card, offered card)", "hollow frame with black spine area left dark"),
    ("record_sheet",    "desk-doc",  "P0", "procedural", "PNG RGBA 320x290 px @100 ppu; FORM 11-C layout, header band + ruled rows left blank for code-drawn text", "CheckpointView (student record / admin memo)", ""),
    ("folder_tab",      "desk-doc",  "P0", "procedural", "PNG RGBA 304x26 px @100 ppu, manila tab with poking-up strip on top", "CheckpointView drawer (1ST/2ND floor tabs)", ""),
    ("paper_flat",      "desk-doc",  "P0", "procedural", "PNG RGBA 760x480 px @100 ppu, plain aged sheet for the RULES NOTE / TONIGHT'S LOG overlays", "CheckpointView overlays", ""),
    ("note_paper",      "desk-doc",  "P1", "procedural", "PNG RGBA 390x300 px @100 ppu, sticky/notice paper", "reserve (overlay variant)", ""),
    ("clipboard",       "desk-doc",  "P1", "procedural", "PNG RGBA 370x490 px @100 ppu", "reserve (overlay variant)", ""),
    ("stamp_allow",     "desk-doc",  "P0", "temp-shipped", "PNG RGBA square ~150 px @100 ppu, APPROVE rubber stamp mark, transparent outside the ink", "CheckpointView stamp tray + slip mark", "ships as Art/UI/SecurityDesk/stamp_allow.png; final ink replaces the file"),
    ("stamp_reject",    "desk-doc",  "P0", "procedural", "PNG RGBA square ~150 px @100 ppu, DENY rubber stamp mark", "CheckpointView stamp tray + slip mark", "pair of stamp_allow: same weight, same tilt language"),
    ("bubble_tail",     "desk-doc",  "P1", "procedural", "PNG RGBA 48x40 px @100 ppu, comic-bubble tail pointing left", "CheckpointView speech bubble", ""),

    # ---- student / administrator faces (the eye-check IS the gameplay) ----
    # face rows live in the FACE section below; face_placeholder stays here as functional art
    ("face_placeholder","face",      "P3", "code-drawn", "PNG RGBA 168x208 px @100 ppu (only ever shown if a face asset is missing)", "CheckpointView window/card fallback", "functional fallback - do not retire"),

    # ---- patrol world: rooms ----
    ("hallway_bg",      "world-bg",  "P0", "procedural", "PNG 1536x640 px @64 ppu (=24x10 world units), side-view corridor, doors/lockers baked OR as separate props", "ViewManager hallway", "final art may split baked props into layers; keep the same canvas size"),
    ("hall_depth",      "world-bg",  "P0", "procedural", "PNG RGBA, ALPHA-ONLY black radial falloff (see HallwayDarkness.DepthAt), native px = shroud world size x10", "hallway limited visibility", "must stay alpha-graded or the pool breaks"),
    ("classroom1_bg",   "world-bg",  "P1", "procedural", "PNG 1152x640 px @64 ppu, Room 101 side view", "ViewManager classroom 1 / peek", ""),
    ("classroom2_bg",   "world-bg",  "P1", "procedural", "PNG 1152x640 px @64 ppu, Room 202 side view (night window glow)", "ViewManager classroom 2 / peek", ""),

    # ---- patrol world: props & the GDD interaction objects ----
    ("prop_door",            "world-prop", "P0", "procedural", "PNG RGBA 160x384 px @64 ppu (2.5x6 units), classroom door, stands on the floor line", "RoomBuilder doors", "drawn standing: centre = floor + h/2"),
    ("prop_door_security",   "world-prop", "P0", "procedural", "PNG RGBA 160x384 px @64 ppu, booth/security door with plate", "RoomBuilder security door + office door", ""),
    ("prop_server_door",     "world-prop", "P0", "procedural", "PNG RGBA 208x384 px @64 ppu, SEALED server/ID room door: chains + padlock baked", "hallway east end", "the scratch marks are story - keep them"),
    ("prop_lockers",         "world-prop", "P0", "procedural", "PNG RGBA 288x336 px @64 ppu, locker bank", "hallway", ""),
    ("prop_lightfixture",    "world-prop", "P1", "procedural", "PNG RGBA 208x40 px @64 ppu, ceiling light strip (flicker clue #1)", "hallway clue 'light'", ""),
    ("prop_noticeboard",     "world-prop", "P2", "procedural", "PNG RGBA 208x160 px @64 ppu, notice board with torn corner (clue #6)", "hallway clue 'board'", "GDD: Notice Board"),
    ("prop_photoframe",      "world-prop", "P2", "procedural", "PNG RGBA 144x108 px @64 ppu, framed old school photo (clue #8)", "hallway clue 'photo'", "GDD: Photo"),
    ("prop_clock",           "world-prop", "P2", "procedural", "PNG RGBA 112x112 px @64 ppu, wall clock stopped at 1:47 (clue #10)", "hallway clue 'clock'", "GDD: Clock"),
    ("prop_chalkboard",      "world-prop", "P1", "procedural", "PNG RGBA 384x224 px @64 ppu, half-erased board (clue #2)", "classroom 1 clue 'chalkboard'", "GDD interaction list"),
    ("prop_desk",            "world-prop", "P1", "procedural", "PNG RGBA 208x128 px @64 ppu, teacher's desk (clue #3)", "classroom 1", ""),
    ("prop_lostid",          "world-prop", "P2", "procedural", "PNG RGBA 56x36 px @64 ppu, lost student ID on the shelf (clue #7)", "classroom 1 clue 'lostid'", "GDD: Student ID"),
    ("prop_window",          "world-prop", "P1", "procedural", "PNG RGBA 224x300 px @64 ppu, classroom window with night glass (clue #5)", "classroom 2 clue 'window'", "GDD: Window"),
    ("prop_cabinet",         "world-prop", "P2", "procedural", "PNG RGBA 160x272 px @64 ppu, filing cabinet (clue #9)", "classroom 2 clue 'cabinet'", "GDD: Cabinet"),
    ("token",                "world-prop", "P2", "procedural", "PNG RGBA 28x28 px @64 ppu, pocket-watch token left by a repelled anomaly", "anomaly repel reward", ""),

    # ---- characters ----
    ("custodian_0",     "character", "P0", "procedural", "PNG RGBA 96x224 px @64 ppu, the guard: walk frame A", "PlayerController", "2-frame walk is enough; add _2/_3 later without renaming"),
    ("custodian_1",     "character", "P0", "procedural", "PNG RGBA 96x224 px @64 ppu, walk frame B", "PlayerController", ""),
    ("anomaly_blink",   "character", "P1", "procedural", "PNG RGBA 128x336 px @64 ppu, silhouette: only moves unobserved", "WanderingAnomaly", "reads as a silhouette under the darkness overlay (order 40)"),
    ("anomaly_slide",   "character", "P1", "procedural", "PNG RGBA 128x336 px @64 ppu, lateral glider", "WanderingAnomaly", ""),
    ("anomaly_sprint",  "character", "P1", "procedural", "PNG RGBA 128x336 px @64 ppu, telegraph-then-burst runner", "WanderingAnomaly", ""),
    ("anomaly_coverhug","character", "P1", "procedural", "PNG RGBA 128x336 px @64 ppu, ceiling hugger", "WanderingAnomaly", ""),
    ("hood_figure",     "character", "P1", "procedural", "PNG RGBA 24x48 px @100 ppu, waiting-line silhouette for the queue stands", "CheckpointView stands", ""),

    # ---- map sheet (this pass) ----
    ("map_paper",       "map",       "P2", "procedural", "PNG RGBA 940x440 px @100 ppu, aged/stained/folded sheet with red district stamp; ROOM OUTLINES ARE DRAWN BY CODE ON TOP - deliver blank paper only", "MapView sheet", "owner's hand-drawn reference can replace this file 1:1"),
    ("map_room",        "map",       "P2", "procedural", "PNG RGBA 160x120 px @100 ppu, wobbly double-stroked room outline, transparent centre (stretched per room)", "MapView rooms", ""),
    ("map_sealed",      "map",       "P2", "procedural", "PNG RGBA 160x120 px @100 ppu, red hatch + chain + padlock outline", "MapView sealed room", ""),
    ("map_door",        "map",       "P3", "procedural", "PNG RGBA 24x20 px @100 ppu, doorway gap with an open leaf", "MapView doorways", ""),

    # ---- light & fx ----
    ("light_cone",      "fx",        "P0", "procedural", "PNG RGBA 512x288 px @64 ppu, the beam wedge: hot at the lamp, gone at the reach, feathered shoulders (BeamProfile bakes it)", "FlashlightRig cone + stencil mask", "mask alpha-cutoff 0.05 depends on the feather - keep the falloff"),
    ("lamp_glow",       "fx",        "P0", "procedural", "PNG RGBA 128x128 px, radial warm glow (peek lamp + mask)", "PeekLight", ""),
    ("vignette",        "fx",        "P3", "procedural", "PNG RGBA 512x384, edge darkening", "desk + menu wash", ""),
    ("noise_overlay",   "fx",        "P3", "procedural", "PNG RGBA 256x256 tileable grain", "desk grain / glitch flash", ""),
    ("flash",           "fx",        "P3", "procedural", "PNG RGBA 256x256 white radial", "scare flash", ""),

    # ---- menus / misc backdrops ----
    ("menu_bg",         "ui-bg",     "P3", "procedural", "PNG 1280x720 px @100 ppu, main-menu corridor with the pale eye-glint", "MainMenuView", ""),
    ("desk_bg",         "ui-bg",     "P0", "procedural", "PNG 640x360 px @100 ppu (stretched), radial lamp wash behind the booth", "CheckpointView", ""),
    ("school_facade",   "ui-bg",     "P3", "procedural", "PNG RGBA 440x296 px @100 ppu, the college seen through the window glass", "CheckpointView window", ""),

    # ---- functional / code-drawn (never replace) ----
    ("solid_white",     "functional","P3", "code-drawn", "4x4 flat quad, 1x1 world unit - every placeholder/meter is built from it", "world quads, HUD, map dots", "do NOT ship a file for this key"),
    ("rough_circle",    "functional","P3", "code-drawn", "110 px rough ink circle (map YOU marker, seals)", "MapView, ID back seal", ""),
    ("rough_panel",     "functional","P3", "code-drawn", "256x192 rough paper panel", "reserve", ""),
    ("rough_panel_dark","functional","P0", "code-drawn", "256x192 dark rough panel - the drawer body/front skin", "CheckpointView drawer", "replaceable if you want a wood-grain drawer: same key"),
    ("circle_dark",     "functional","P3", "code-drawn", "110 px flat dark circle", "reserve", ""),
    ("circle_paper",    "functional","P1", "code-drawn", "110 px flat paper circle (ID back seal)", "CheckpointView ID back", ""),
    ("cursor_hand",     "functional","P3", "code-drawn", "24 px hand cursor", "HoverCursor", ""),
    ("icon_check",      "functional","P3", "code-drawn", "square check icon", "reserve", ""),
    ("icon_x",          "functional","P3", "code-drawn", "square cross icon", "reserve", ""),
    ("camera_frame",    "reserve",   "P3", "procedural", "PNG RGBA 252x150, viewfinder corners", "reserve (retired scanner)", "keep unshipped unless the scanner returns"),
    ("window_frame",    "reserve",   "P3", "procedural", "PNG RGBA 470x316 flat frame", "reserve", ""),
]

# --------------------------------------------------------------------------- faces
FACES = []
for i in range(1, 8):
    nn = f"{i:02d}"
    shipped = True
    prio = "P0"
    FACES.append((f"char_student_{nn}", "face", prio,
                  "shipped" if shipped else "missing",
                  "PNG RGBA 512x640 (4:5) bust, flat front light, neutral - same crop for every student; code letterboxes it into the card slot, the window and the counter card",
                  f"canon student {i} of 7 (Content.TotalStudents)",
                  "illustrated bust from Tools/prep_characters.py"))
    FACES.append((f"char_student_{nn}_anomaly", "face", prio,
                  "shipped" if shipped else "missing",
                  "PNG RGBA 512x640, THE SAME PERSON as the base key, visibly wrong (the pairing is the tell)",
                  f"anomaly variant of student {i}",
                  "FaceAssets.AnomalyKey - window shows this when the run says anomaly/distorted"))
FACES += [
    ("char_admin",        "face", "P3", "missing", "PNG RGBA 512x640, district administrator: formal coat, briefcase, smile that arrives late - badge photo", "Night-5 administrator ID badge", "FaceAssets.AdminKey (new this pass)"),
    ("char_admin_glitch", "face", "P3", "missing", "PNG RGBA 512x640, same administrator, a half-second wrong (the window tell)", "Night-5 administrator at the window", "FaceAssets.AdminWindowKey (new this pass)"),
]

# --------------------------------------------------------------------------- audio
SFX = [
    ("sfx_knock",        "P0", "window knock, 3 beats, wood + glass", ""),
    ("sfx_paper",        "P0", "paper slide / flip / drop - the desk's voice", "fired ~10x per inspection"),
    ("sfx_stamp_allow",  "P0", "APPROVE thud: heavy stamp, soft rebound", ""),
    ("sfx_stamp_reject", "P0", "DENY thud: lower, drier", ""),
    ("sfx_ui_open",      "P0", "sheet/drawer open: short paper+wood", ""),
    ("sfx_ui_close",     "P0", "sheet/drawer close", "most-used sfx in the game"),
    ("sfx_door",         "P0", "room door open/close, hinge + latch", ""),
    ("sfx_footstep",     "P0", "single guard footstep on lino (queue line uses it too)", ""),
    ("sfx_click",        "P0", "dry UI tick", ""),
    ("sfx_bell",         "P0", "distant school bell, one strike, dead room", ""),
    ("sfx_scan_error",   "P0", "refusal buzz (hand-back without a verdict)", ""),
    ("sfx_glitch",       "P1", "digital stutter, 0.12 s", "anomaly tell"),
    ("sfx_static",       "P1", "radio static burst", ""),
    ("sfx_flicker",      "P1", "fluorescent flicker buzz", ""),
    ("sfx_intercom",     "P1", "intercom chirp + room tone", "queue pressure warnings"),
    ("sfx_heartbeat",    "P1", "single low heartbeat", "sanity stages"),
    ("sfx_pickup",       "P2", "token pickup: watch tick + chime", ""),
    ("sfx_stinger",      "P2", "short horror sting", ""),
    ("sfx_scare",        "P2", "full scare hit", "once per run, ideally"),
    ("sfx_alarm",        "P2", "muffled alarm behind walls", ""),
    ("sfx_scan_ok",      "P1", "positive chime (reserve path)", ""),
]
MUSIC = [
    ("music_menu",          "P3", "60-90 s loop, main menu dread", ""),
    ("music_checkpoint",    "P0", "2-4 min loop, the desk: quiet room tone, pencil, clock", "the longest-running bed in the game"),
    ("music_hallway",       "P0", "2-3 min loop, patrol corridor", ""),
    ("music_classroom",     "P1", "2-3 min loop, Room 101", ""),
    ("music_darkroom",      "P1", "2-3 min loop, Room 202 / security office", ""),
    ("music_investigation", "P2", "loop, clue-chain tension", "context exists in code, not yet played - hook it or retire it"),
    ("music_anomaly",       "P1", "loop while an encounter is live, cuts back to the room bed on resolve", ""),
    ("music_final",         "P3", "administrator encounter bed", ""),
    ("music_end_win",       "P3", "ending stinger, 20-40 s, non-loop", ""),
    ("music_end_lose",      "P3", "ending stinger, 20-40 s, non-loop", ""),
    ("music_end_secret",    "P3", "secret ending stinger, 20-40 s, non-loop", ""),
]

# ---- the owner's SecurityRoom kit: shipped, sliced where measured, all live keys ----
SEC = [
    ("sec_background",       "kit", "P0", "full-frame 2880x1620 layer: booth base, lamp cone baked, no window"),
    ("sec_background_open",  "kit", "P0", "full-frame layer: booth with the shutter OPEN (loading/summary backdrops)"),
    ("sec_background_close", "kit", "P0", "full-frame layer: booth with the shutter CLOSED (night card backdrop)"),
    ("sec_light",            "kit", "P0", "full-frame layer: hanging lamp cone only (slow breath layer)"),
    ("sec_window_open",      "kit", "P0", "full-frame layer: window frame + open glass; opening matches the Portrait rect"),
    ("sec_window_close",     "kit", "P0", "full-frame layer: window frame + closed blinds"),
    ("sec_upper_panel",      "kit", "P0", "full-frame layer: header board with cut-outs for sanity bar + pause"),
    ("sec_upper_panel_bg",   "kit", "P0", "full-frame layer: header board without the widget cut-outs"),
    ("sec_sanity_bar",       "kit", "P0", "sliced widget: sanity bar furniture, placed at L.SanityMeter"),
    ("sec_pause_block",      "kit", "P0", "sliced widget: pause block furniture, placed at L.Pause"),
    ("sec_btn_pill",         "kit", "P0", "sliced pill: question / overlay-close button skin"),
    ("sec_wide_pill",        "kit", "P0", "sliced pill: patrol / end-shift button skin"),
    ("sec_caption_bar",      "kit", "P0", "sliced bar: stamp caption furniture"),
    ("sec_stamp_circle_allow","kit","P0", "sliced tray card + check disc under the APPROVE stamp"),
    ("sec_stamp_circle_deny", "kit", "P0", "sliced tray card + cross disc under the DECLINE stamp"),
    ("sec_icon_accept",      "kit", "P0", "sliced check disc badge for the APPROVE stamp handle"),
    ("sec_icon_decline",     "kit", "P0", "sliced cross disc badge for the DECLINE stamp handle"),
    ("sec_folder_tab1",      "kit", "P0", "sliced folder: 1st-floor tab art"),
    ("sec_folder_tab2",      "kit", "P0", "sliced folder: 2nd-floor tab art"),
    ("sec_icon_patrol",      "kit", "P0", "sliced footprint icon for the patrol button"),
    ("sec_icon_endshift",    "kit", "P0", "sliced moon icon for the end-shift button"),
    ("sec_qpanel",           "kit", "P0", "cropped panel: question column skin"),
    ("sec_arrow_right",      "kit", "P1", "cropped chevron: the asked-mark on question rows"),
    ("sec_cabinet1",         "kit", "P0", "cropped cabinet face: FLOOR FILES station skin"),
    ("sec_cabinet2",         "kit", "P0", "cropped drawer unit: file-drawer body skin"),
    ("sec_stamp",            "kit", "P0", "cropped hand stamp: the draggable verdict stamp body"),
    ("sec_rules_paper",      "kit", "P2", "cropped tilted paper: RULES NOTE overlay skin"),
    ("sec_rules_dark",       "kit", "P2", "cropped dark paper: RN tool-button skin"),
    ("sec_log_paper",        "kit", "P2", "cropped tilted paper: TONIGHT'S LOG overlay skin"),
    ("sec_log_dark",         "kit", "P2", "cropped dark paper: TL tool-button skin"),
    ("sec_log_rules",        "kit", "P3", "cropped pair of papers: reserve composite"),
    ("sec_bubble",           "kit", "P0", "speech box, 9-sliced (left tail + rough edge preserved): bubble + dialogue panel skin"),
]

# ---- the owner's Main Menu kit: shipped, sliced where measured, all live keys ----
MENU = [
    ("menu_background",    "kit", "P0", "full-frame 2880x1620 corridor backdrop (cover-cropped by MainMenuView.FitBackground)"),
    ("menu_logo",          "kit", "P0", "cropped logo plate: SHOW ME YOUR ID! lettering + soft halo, top right"),
    ("menu_frame_wide",    "kit", "P0", "cropped 977x197 white outline: NEW GAME / CONTINUE button frame"),
    ("menu_frame_square",  "kit", "P0", "cropped 160x162 white outline: SETTINGS / EXIT / CREDITS button frame"),
    ("menu_icon_newgame",  "kit", "P0", "cropped skull icon inside the NEW GAME frame"),
    ("menu_icon_continue", "kit", "P0", "cropped four-arrow icon inside the CONTINUE frame"),
    ("menu_icon_settings", "kit", "P0", "cropped gear icon inside the first square"),
    ("menu_icon_exit",     "kit", "P0", "cropped door+arrow icon inside the second square"),
    ("menu_icon_credits",  "kit", "P1", "cropped theatre-masks icon inside the third square"),
]

FONTS = [
    ("kit_bahnschrift_regular", "font", "P0", "shipped-kit", "OTF, owner-supplied - UI body face (UiFont, fallback BarlowCondensed)", "UIFactory.UiFont", ""),
    ("kit_bahnschrift_bold", "font", "P0", "shipped-kit", "OTF, owner-supplied - UI emphasis (UiFontBold, fallback BarlowCondensed-SemiBold)", "UIFactory.UiFontBold", ""),
    ("kit_bahnschrift_semibold_condensed", "font", "P3", "shipped-kit", "OTF, owner-supplied - reserve weight", "reserve", ""),
    ("kit_chiller", "font", "P1", "shipped-kit", "TTF, owner-supplied - horror display face (TypeFont, fallback SpecialElite)", "UIFactory.TypeFont", ""),
    ("BarlowCondensed-Regular",  "font", "P0", "temp-shipped", "TTF, open licence (OFL) - fallback UI body face", "UIFactory.UiFont fallback", ""),
    ("BarlowCondensed-SemiBold", "font", "P0", "temp-shipped", "TTF, OFL - fallback UI emphasis", "UIFactory.UiFontBold fallback", ""),
    ("SpecialElite-Regular",     "font", "P1", "temp-shipped", "TTF, OFL - fallback typewriter documents", "UIFactory.TypeFont fallback", ""),
    ("CaveatBrush-Regular",      "font", "P2", "temp-shipped", "TTF, OFL - handwritten map + notes", "UIFactory.HandFont", "the map title/labels use this"),
]


def main():
    rows = []
    for key, cat, prio, status, spec, used, notes in ART:
        rows.append([key, "art", cat, prio, status, "PNG", spec, used, notes])
    for key, cat, prio, status, spec, used, notes in FACES:
        rows.append([key, "art", cat, prio, status, "PNG", spec, used, notes])
    for key, prio, spec, notes in SFX:
        rows.append([key, "audio", "sfx", prio, "procedural", "WAV 44.1 kHz 16-bit MONO", spec, "ProceduralAudio / AudioDirector.PlaySfx", notes])
    for key, prio, spec, notes in MUSIC:
        rows.append([key, "audio", "music", prio, "procedural", "WAV or OGG 44.1 kHz STEREO, loop-seamless", spec, "AudioDirector.PlayMusic(context) -> music_<context>", notes])
    for key, cat, prio, spec in SEC:
        rows.append([key, "art", cat, prio, "shipped-kit", "PNG", spec,
                     "CheckpointView.KitSkin / DialogueView / LoadingView / ShiftResults", ""])
    for key, cat, prio, spec in MENU:
        rows.append([key, "art", cat, prio, "shipped-kit", "PNG", spec,
                     "MainMenuView (main menu composite per Art/Old/MainMenu/reference)", ""])
    for key, typ, prio, status, spec, used, notes in FONTS:
        rows.append([key, typ, "type", prio, status, "TTF", spec, used, notes])

    rows.sort(key=lambda r: (r[1], r[3], r[0]))
    with open(OUT, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["key", "type", "category", "priority", "status", "format", "spec", "used_by", "notes"])
        w.writerows(rows)
    arts = sum(1 for r in rows if r[1] == "art")
    aud = sum(1 for r in rows if r[1] == "audio")
    print(f"wrote {OUT}: {len(rows)} rows ({arts} art/face/font-art, {aud} audio, {len(FONTS)} fonts)")
    for p in ("P0", "P1", "P2", "P3"):
        print(f"   {p}: {sum(1 for r in rows if r[3] == p)} rows")


if __name__ == "__main__":
    main()
