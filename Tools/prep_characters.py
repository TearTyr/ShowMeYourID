#!/usr/bin/env python3
"""Turn the owner's illustrated busts into the game's face art.

Source: Art/UI and Design Assets/Characters   Destination: Assets/_Project/Resources/Art
Each bust sits small inside a 956x554 canvas, so it is cropped to its drawing. Every figure
is placed on ONE shared canvas (bottom-centre), so the characters keep their real size
relative to each other. Names the game expects:
  student_face_NN.png          normal art  (window + ID photo)
  student_face_NN_anomaly.png  the same person, wrong (photo-defect anomalies only)
  admin_face.png / admin_face_glitch.png   the Night-5 administrator (the Director)
Re-run whenever the art changes. New .png files still need a .meta (Tools/generate_meta.py).
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets", "Characters")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art")

# (normal, anomaly) per student, in face-index order
STUDENTS = [
    ("Mark/Higher Res Image/Mark_Normal.png", "Mark/Higher Res Image/AnomalyMark.png"),
    ("Jose/Jose_Normal.png", "Jose/Jose_Anomaly.png"),
    ("Maria/Maria_Draft3.png", "Maria/Maria_Draft3_Anomaly.png"),
    ("Liza/Liza_Normal.png", "Liza/Liza_Anomaly.png"),
    ("Raffy/Raf_Normal.png", "Raffy/Raf_Anomaly.png"),
    ("Renniel/Reniel_Normal.png", "Renniel/Reniel_Anomaly.png"),
    ("Rhevsters/Rhevster_Normal.png", "Rhevsters/Rhevster_anomaly.png"),
]
ADMIN = ("Director/Director_Normal.png", "Director/Director_Anomaly.png")


def load(rel):
    return Image.open(os.path.join(SRC, rel)).convert("RGBA")


def bbox(im):
    return im.split()[3].point(lambda v: 255 if v > 8 else 0).getbbox()


def main():
    pairs = [(a, b) for a, b in STUDENTS] + [ADMIN]
    crops = {}
    for pair in pairs:
        for rel in pair:
            im = load(rel)
            crops[rel] = im.crop(bbox(im))
    pad = 12
    # the canvas fits the NORMAL busts; a taller anomaly (spiky hair) is shrunk to fit,
    # so the normal faces fill the window instead of leaving room for the tallest one
    normals = [crops[n] for n, _ in STUDENTS] + [crops[ADMIN[0]]]
    cw = max(c.width for c in normals) + 2 * pad
    ch = max(c.height for c in normals) + 2 * pad
    print("shared canvas", cw, "x", ch)

    def put(rel, name):
        c = crops[rel]
        fit = min(1.0, (cw - 2 * pad) / c.width, (ch - 2 * pad) / c.height)
        if fit < 1.0:
            c = c.resize((max(1, round(c.width * fit)), max(1, round(c.height * fit))), Image.LANCZOS)
        canvas = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
        canvas.alpha_composite(c, ((cw - c.width) // 2, ch - pad - c.height))
        canvas.save(os.path.join(DST, name + ".png"))

    for i, (normal, anomaly) in enumerate(STUDENTS, start=1):
        put(normal, "student_face_%02d" % i)
        put(anomaly, "student_face_%02d_anomaly" % i)
    put(ADMIN[0], "admin_face")
    put(ADMIN[1], "admin_face_glitch")


if __name__ == "__main__":
    main()
