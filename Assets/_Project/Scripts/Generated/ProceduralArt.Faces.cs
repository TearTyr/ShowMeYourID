using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // Blank papers of the inspection: the ID card frame, the record sheet and the empty-photo placeholder.
    // Every face on screen is a real art file; no face is ever painted in code.
    public static partial class ProceduralArt
    {
        // Printed 1:1 at the desk panel size (320x220): flat card stock, header band,
        // photo slot, text rules and barcode exactly where CheckpointLayout.Inner expects.
        private static Sprite MakeIdCard()
        {
            const int w = 320, h = 220;
            var c = new Canvas2D(w, h);
            c.Fill(WirePaper);
            c.FillRect(0, 0, w, 28, WireBand);                                   // header band
            c.FillRect(10, 46, 90, 118, new Color(0.937f, 0.929f, 0.902f));      // photo slot
            c.FillRect(10, 46, 90, 2, WireRule);
            c.FillRect(10, 162, 90, 2, WireRule);
            c.FillRect(10, 46, 2, 118, WireRule);
            c.FillRect(98, 46, 2, 118, WireRule);
            float[] rules = { 52f, 75f, 99f, 123f, 146f, 179f };
            foreach (float yf in rules) c.FillRect(112, (int)yf, 190, 2, WireRule);   // text rules
            for (int i = 0; i < 16; i++) c.FillRect(10 + i * 13, 188, 6, 14, WireBar); // barcode
            Border2(c, WireInk);
            return c.ToSprite("id_card_frame", UiPpu);
        }

        // Printed 1:1 at the desk panel size (320x290): flat sheet, header band, seven
        // The "no photo yet / asset failed" portrait: a designed placeholder in the photo's own
        // language (paper-gray backdrop, muted silhouette bust, thin frame) so a missing or
        // not-yet-assigned sprite can NEVER render as uGUI's default white rectangle. Created
        // once; CheckpointView/DialogueView build their portrait Images with it.
        private static Sprite MakeFacePlaceholder(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.34f), G(0.24f));                 // darker than a real photo: reads as empty
            float cx = w / 2f;
            c.FillEllipse(cx, h * 1.06f, w * 0.44f, h * 0.24f, G(0.16f));          // shoulders
            c.FillEllipse(cx, h * 0.44f, w * 0.20f, h * 0.24f, G(0.16f));          // head
            c.Noise(0.06f, 41);
            for (int x = 0; x < w; x++) { c.Px[x] = G(0.10f); c.Px[(h - 1) * w + x] = G(0.10f); }
            for (int y = 0; y < h; y++) { c.Px[y * w] = G(0.10f); c.Px[y * w + w - 1] = G(0.10f); }
            return c.ToSprite("face_placeholder", UiPpu);
        }

        // form rules, stamp box and barcode strip per the approved wireframe.
        private static Sprite MakeRecordSheet()
        {
            const int w = 320, h = 290;
            var c = new Canvas2D(w, h);
            c.Fill(WirePaper);
            c.FillRect(0, 0, w, 28, WireBand);                                   // header band
            float[] rules = { 42f, 63f, 84f, 105f, 126f, 147f, 168f };
            foreach (float yf in rules) c.FillRect(30, (int)yf, 250, 2, WireRule);    // form rules
            c.FillRect(200, 248, 100, 30, new Color(0.867f, 0.855f, 0.824f));    // stamp box
            for (int i = 0; i < 12; i++) c.FillRect(30 + i * 14, 250, 6, 26, WireBar); // barcode
            Border2(c, WireInk);
            return c.ToSprite("record_sheet", UiPpu);
        }
    }
}
