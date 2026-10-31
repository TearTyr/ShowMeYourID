#!/usr/bin/env python3
"""Build the blank student ID card from the owner's PSD.

Source: Art/UI and Design Assets/ID/SMYI-ID.psd
Output: Assets/_Project/Resources/Art/id_card_template.png
The sample card has Mark's name, number, major, grade, status, expiry and photo baked in as
separate layers. They are hidden here, so the game prints the live values itself on the
printed labels (NAME, STUDENT NUMBER, MAJOR, GRADE, STATUS). The card is extended with a white
strip at the bottom (the EXPIRES line goes there) so it has the same 320:220 proportions as the
desk card layout. Needs: pip install psd-tools pillow
"""
import os
from PIL import Image
from psd_tools import PSDImage

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Art", "UI and Design Assets", "ID", "SMYI-ID.psd")
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Art", "id_card_template.png")
HIDE = {"Mark mendoza", "HB-2024-0101", "bs information technology", "freshman", "regular",
        "expires 2027 - 04 - 12", "Markfinalizedsas"}
OUT_W = 1170          # 320 design units x ~3.66


def walk(layers):
    for layer in layers:
        yield layer
        if layer.is_group():
            yield from walk(layer)


def main():
    psd = PSDImage.open(SRC)
    for layer in walk(psd):
        if layer.name in HIDE:
            layer.visible = False
    card = psd.composite(force=True).convert("RGBA")
    w, h = card.size                      # 3900 x 2400
    full_h = round(w * 220 / 320)         # 2681: the extra strip carries the expiry line
    sheet = Image.new("RGBA", (w, full_h), (255, 255, 255, 255))
    sheet.alpha_composite(card, (0, 0))
    out_h = round(OUT_W * full_h / w)
    sheet.resize((OUT_W, out_h), Image.LANCZOS).save(DST)
    print("wrote", DST, OUT_W, "x", out_h)


if __name__ == "__main__":
    main()
