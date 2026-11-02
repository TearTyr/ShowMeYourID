#!/usr/bin/env python3
"""Turn the owner's illustrated busts into the game's face art.

Source: Art/UI and Design Assets/Characters   Destination: Assets/_Project/Resources/Art/Characters
Each bust sits small inside a 956x554 canvas, so it is cropped to its drawing. Every figure
is placed on ONE shared canvas (bottom-centre), so the characters keep their real size
relative to each other. Names the game expects:
  char_student_NN.png          normal art  (window + ID photo)
  char_student_NN_anomaly.png  the same person, wrong (every anomaly wears it)
  char_student_NN_scared.png   the same person, frightened (a scared visitor, student or anomaly)
  char_admin.png / char_admin_glitch.png   the Night-5 administrator (the Director)
  char_guard_idle.png, char_guard_walk_a.png, char_guard_walk_b.png   the player's three poses
  char_anomaly.png   the crouched anomaly (Slide_Anomaly/Anomaly_Slide.png, cropped to its drawing), Blink and Slide
  char_anomaly_sprint_a/b/c.png   the running anomaly's three frames (Runner_Anomaly/Anomaly_Sprint1-3.png)
  char_anomaly_coverhug.png   the one that hangs upside down from the ceiling (Anomaly_Coverhug)
  char_hood.png      the hooded figure standing in the peek rooms (Hood_Figure_/Hood_Figure.png)
Re-run whenever the art changes. New .png files still need a .meta (Tools/generate_meta.py).
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets", "Characters")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "Characters")
STUDENT_DST = os.path.join(DST, "Students")    # char_student_NN and char_student_NN_anomaly
ANOMALY_DST = os.path.join(DST, "Anomalies")   # char_anomaly, char_hood
STAFF_DST = os.path.join(DST, "Staff")         # char_admin, char_admin_glitch, the guard

# (normal, anomaly, scared) per student, in face-index order
STUDENTS = [
    ("Mark/Higher Res Image/Mark_Normal.png", "Mark/Higher Res Image/AnomalyMark.png", "Mark/Higher Res Image/Mark_Scared.png"),
    ("Nathaniel/Nathaniel_Normal.png", "Nathaniel/Nathaniel_Anomaly.png", "Nathaniel/Nathaniel_Scared.png"),
    ("Maria/Maria_Draft3.png", "Maria/Maria_Draft3_Anomaly.png", "Maria/Maria_Draft3_Angry.png"),   # Maria has no scared file yet: the angry one stands in
    ("Liza/Liza_Normal.png", "Liza/Liza_Anomaly.png", "Liza/Liza_Scared.png"),
    ("Raffy/Raf_Normal.png", "Raffy/Raf_Anomaly.png", "Raffy/Raf_Scared.png"),
    ("Renniel/Reniel_Normal.png", "Renniel/Reniel_Anomaly.png", "Renniel/Reniel_Scared.png"),
    ("Rhevsters/Rhevster_Normal.png", "Rhevsters/Rhevster_anomaly.png", "Rhevsters/Rhevster_Scared.png"),
    ("Jose/Jose_Normal.png", "Jose/Jose_Anomaly.png", "Jose/Jose_Scared.png"),
    ("Angelica/Angelica_Normal.png", "Angelica/Angelica_Anomaly.png", "Angelica/Angelica_Scared.png"),
    ("Anthony/Anthony_Normal.png", "Anthony/Anthony_Anomaly.png", "Anthony/Anthony_Scared.png"),
    ("Bea/Bea_Normal.png", "Bea/Bea_Anomaly.png", "Bea/Bea_Scared.png"),
    ("Giones Acirfa/Giones_Normal.png", "Giones Acirfa/Giones_Anomaly.png", "Giones Acirfa/Giones_Scared.png"),
    ("Kristine/Kristine_Normal.png", "Kristine/Kristine_Anomaly.png", "Kristine/Kristine_Scared.png"),
    # Izzy_Neckerman only holds copies of Anthony's pictures: add her here when the real art arrives
]
ADMIN = ("Director/Director_Normal.png", "Director/Director_Anomaly.png")
# the player: standing pose first, then the two walking poses (walk cycle = a, idle, b, idle)
GUARD = [("Sec_Guard_Walk/MCnightguardDraft4.png", "char_guard_idle"),
         ("Sec_Guard_Walk/Frame3.png", "char_guard_walk_a"),
         ("Sec_Guard_Walk/Frame2.png", "char_guard_walk_b")]


def load(rel):
    return Image.open(os.path.join(SRC, rel)).convert("RGBA")


def bbox(im):
    return im.split()[3].point(lambda v: 255 if v > 8 else 0).getbbox()


def prep_guard():
    """The three guard frames share one crop box, so the feet and the body stay where the
    artist drew them from pose to pose."""
    frames = [(load(rel), name) for rel, name in GUARD]
    boxes = [bbox(im) for im, _ in frames]
    box = (min(b[0] for b in boxes), min(b[1] for b in boxes),
           max(b[2] for b in boxes), max(b[3] for b in boxes))
    print("guard crop", box)
    for im, name in frames:
        im.crop(box).save(os.path.join(STAFF_DST, name + ".png"))


def prep_anomalies():
    """The patrol anomalies, all in Characters/Anomalies. The three Sprint frames share one crop box so the
    legs and the body stay where the artist drew them from frame to frame. The Coverhug art is cut off at
    its top edge on purpose: it hangs from the top of the screen, so that edge is the ceiling."""
    slide = load("Slide_Anomaly/Anomaly_Slide.png")
    slide.crop(bbox(slide)).save(os.path.join(ANOMALY_DST, "char_anomaly.png"))
    frames = [load("Runner_Anomaly/Anomaly_Sprint%d.png" % n) for n in (1, 2, 3)]
    boxes = [bbox(f) for f in frames]
    box = (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))
    for f, name in zip(frames, ("char_anomaly_sprint_a", "char_anomaly_sprint_b", "char_anomaly_sprint_c")):
        f.crop(box).save(os.path.join(ANOMALY_DST, name + ".png"))
    hug = load("Anomaly_Coverhug/Anomaly_Coverhug.png")
    hug.crop(bbox(hug)).save(os.path.join(ANOMALY_DST, "char_anomaly_coverhug.png"))
    print("anomalies: sprint crop", box, "coverhug", bbox(hug))


def prep_hood():
    """The hooded figure is a small silhouette (90x152 px), so it is cropped to its drawing and
    enlarged 4x with a smooth filter: the game then draws it about 3.2 units tall without
    stair-stepped edges. If the artist sends a larger export, drop it in and re-run."""
    im = load("Hood_Figure_/Hood_Figure.png")
    im = im.crop(bbox(im))
    im = im.resize((im.width * 4, im.height * 4), Image.LANCZOS)
    im.save(os.path.join(ANOMALY_DST, "char_hood.png"))
    print("hood", im.size)


def main():
    prep_hood()
    prep_anomalies()
    pairs = list(STUDENTS) + [ADMIN]
    crops = {}
    for pair in pairs:
        for rel in pair:
            im = load(rel)
            crops[rel] = im.crop(bbox(im))
    pad = 12
    # the canvas fits the NORMAL busts; a taller anomaly (spiky hair) is shrunk to fit,
    # so the normal faces fill the window instead of leaving room for the tallest one
    normals = [crops[s[0]] for s in STUDENTS] + [crops[ADMIN[0]]]
    cw = max(c.width for c in normals) + 2 * pad
    ch = max(c.height for c in normals) + 2 * pad
    print("shared canvas", cw, "x", ch)

    def put(rel, name, folder):
        c = crops[rel]
        fit = min(1.0, (cw - 2 * pad) / c.width, (ch - 2 * pad) / c.height)
        if fit < 1.0:
            c = c.resize((max(1, round(c.width * fit)), max(1, round(c.height * fit))), Image.LANCZOS)
        canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
        canvas.alpha_composite(c, ((cw - c.width) // 2, ch - pad - c.height))
        canvas.save(os.path.join(folder, name + ".png"))

    for i, (normal, anomaly, scared) in enumerate(STUDENTS, start=1):
        put(normal, "char_student_%02d" % i, STUDENT_DST)
        put(anomaly, "char_student_%02d_anomaly" % i, STUDENT_DST)
        put(scared, "char_student_%02d_scared" % i, STUDENT_DST)
    put(ADMIN[0], "char_admin", STAFF_DST)
    put(ADMIN[1], "char_admin_glitch", STAFF_DST)
    prep_guard()


if __name__ == "__main__":
    main()
