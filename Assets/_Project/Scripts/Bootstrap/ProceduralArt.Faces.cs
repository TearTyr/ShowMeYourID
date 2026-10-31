using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Faces and papers of the inspection: portraits, custodian, anomalies, ID card, record sheet.
    public static partial class ProceduralArt
    {
        public static Sprite Portrait(string keyPrefix, Portrait p, int w = 168, int h = 208)
        {
            string key = keyPrefix + "_" + p.HairStyle + p.FaceVariant + p.Accessory + (p.Distorted ? "d" : "c");
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var c = new Canvas2D(w, h);
            c.GradientV(G(0.80f), G(0.62f));            // paper-gray photo backdrop
            c.Noise(0.05f, StableHash(key));
            float cx = w / 2f, baseY = h * 0.98f;

            var skin = G(0.30f);
            c.FillEllipse(cx, baseY + h * 0.10f, w * 0.46f, h * 0.22f, skin);      // shoulders
            c.FillRect((int)(cx - w * 0.07f), (int)(h * 0.62f), (int)(w * 0.14f), (int)(h * 0.14f), skin); // neck
            float headCy = h * 0.47f, headRx = w * 0.27f, headRy = h * 0.24f;
            c.FillEllipse(cx, headCy, headRx, headRy, skin);                        // head

            var hair = G(0.12f);
            switch (p.HairStyle)
            {
                case 0: // buzz
                    c.FillEllipse(cx, headCy - headRy * 0.25f, headRx * 0.98f, headRy * 0.8f, hair);
                    break;
                case 1: // short side-part
                    c.FillEllipse(cx - headRx * 0.1f, headCy - headRy * 0.3f, headRx * 1.02f, headRy * 0.78f, hair);
                    c.FillRect((int)(cx + headRx * 0.35f), (int)(headCy - headRy * 0.2f), (int)(headRx * 0.6f), (int)(headRy * 0.5f), hair);
                    break;
                case 2: // long
                    c.FillEllipse(cx, headCy - headRy * 0.2f, headRx * 1.15f, headRy * 0.95f, hair);
                    c.FillRect((int)(cx - headRx * 1.12f), (int)(headCy - headRy * 0.1f), (int)(headRx * 0.42f), (int)(headRy * 1.5f), hair);
                    c.FillRect((int)(cx + headRx * 0.72f), (int)(headCy - headRy * 0.1f), (int)(headRx * 0.42f), (int)(headRy * 1.5f), hair);
                    break;
                case 3: // curly (blobs)
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = Mathf.PI * (0.08f + 0.84f * i / 8f);
                        c.FillEllipse(cx - Mathf.Cos(ang) * headRx * 1.02f, headCy - Mathf.Sin(ang) * headRy * 1.05f,
                            headRx * 0.34f, headRy * 0.30f, hair);
                    }
                    break;
                case 4: // ponytail
                    c.FillEllipse(cx, headCy - headRy * 0.3f, headRx * 1.0f, headRy * 0.75f, hair);
                    c.FillEllipse(cx + headRx * 1.15f, headCy + headRy * 0.35f, headRx * 0.30f, headRy * 0.85f, hair);
                    break;
                default: // bun
                    c.FillEllipse(cx, headCy - headRy * 0.3f, headRx * 1.0f, headRy * 0.75f, hair);
                    c.FillEllipse(cx, headCy - headRy * 1.12f, headRx * 0.34f, headRy * 0.30f, hair);
                    break;
            }

            var eyeC = G(0.88f);
            float eyeY = headCy + headRy * (0.02f + (p.FaceVariant % 3) * 0.06f);
            float eyeSpread = headRx * (0.36f + (p.FaceVariant % 2) * 0.10f);
            float eyeW = headRx * (0.16f + (p.FaceVariant / 3) * 0.05f), eyeH = headRy * 0.09f;
            c.FillEllipse(cx - eyeSpread, eyeY, eyeW, eyeH, eyeC);
            c.FillEllipse(cx + eyeSpread, eyeY, eyeW, eyeH, eyeC);
            c.Line((int)cx, (int)(eyeY + headRy * 0.12f), (int)cx, (int)(eyeY + headRy * 0.34f), G(0.55f), 2); // nose
            float mouthW = headRx * (0.28f + (p.FaceVariant % 4) * 0.06f);
            float mouthY = headCy + headRy * 0.62f;
            if (p.FaceVariant % 3 == 0) c.Line((int)(cx - mouthW), (int)mouthY, (int)(cx + mouthW), (int)mouthY, G(0.62f), 2);
            else if (p.FaceVariant % 3 == 1)
            {
                c.Line((int)(cx - mouthW), (int)mouthY + 2, (int)cx, (int)mouthY - 2, G(0.62f), 2);
                c.Line((int)cx, (int)mouthY - 2, (int)(cx + mouthW), (int)mouthY + 2, G(0.62f), 2);
            }
            else c.FillEllipse(cx, mouthY, mouthW * 0.55f, headRy * 0.06f, G(0.62f));

            switch (p.Accessory)
            {
                case 1: // glasses
                    c.FillEllipse(cx - eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.1f, 0.0f));
                    DrawRing(c, cx - eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.9f));
                    DrawRing(c, cx + eyeSpread, eyeY, eyeW * 1.9f, eyeH * 2.6f, G(0.9f));
                    c.Line((int)(cx - eyeSpread + eyeW * 1.9f), (int)eyeY, (int)(cx + eyeSpread - eyeW * 1.9f), (int)eyeY, G(0.9f), 2);
                    break;
                case 2: // earrings
                    c.FillEllipse(cx - headRx * 0.98f, eyeY + headRy * 0.42f, 3, 3, G(0.95f));
                    c.FillEllipse(cx + headRx * 0.98f, eyeY + headRy * 0.42f, 3, 3, G(0.95f));
                    break;
                case 3: // cap
                    c.FillEllipse(cx, headCy - headRy * 0.55f, headRx * 1.06f, headRy * 0.55f, G(0.18f));
                    c.FillRect((int)(cx - headRx * 1.3f), (int)(headCy - headRy * 0.62f), (int)(headRx * 2.6f), 6, G(0.22f));
                    break;
            }

            if (p.Distorted)
            {
                var r = new System.Random(1349);
                c.ShiftRows(r, 7, 9);
                c.FillEllipse(cx + eyeSpread + 5, eyeY - 4, eyeW * 1.3f, eyeH * 1.5f, G(0.95f));
                c.FillEllipse(cx + eyeSpread + 5, eyeY - 4, eyeW * 0.5f, eyeH * 0.7f, G(0.02f));
                c.FillRect((int)(cx - mouthW * 1.4f), (int)mouthY - 2, (int)(mouthW * 2.8f), 4, G(0.05f));
                for (int s = 0; s < 26; s++)
                    c.FillRect(r.Next(w), r.Next(h), 2 + r.Next(10), 1 + r.Next(2), G(r.Next(2) == 0 ? 0.95f : 0.05f, 0.8f));
            }

            for (int i = 0; i < w; i++) { c.Px[i] = G(0.25f); c.Px[(h - 1) * w + i] = G(0.25f); }
            for (int i = 0; i < h; i++) { c.Px[i * w] = G(0.25f); c.Px[i * w + w - 1] = G(0.25f); }

            var sp = c.ToSprite(key, UiPpu);
            Cache[key] = sp;
            return sp;
        }

        private static Sprite MakeCustodian(int w, int h, int frame)
        {
            var c = new Canvas2D(w, h);
            var body = G(0.82f);   // pale silhouette custodian (reads against dark rooms)
            var dark = G(0.25f);
            float cx = w / 2f;
            c.FillEllipse(cx, h * 0.12f, 13, 15, body);                     // head
            c.FillRect((int)(cx - 16), (int)(h * 0.055f), 32, 6, dark);     // cap brim
            c.FillEllipse(cx, h * 0.055f, 14, 8, dark);                     // cap
            c.FillRect((int)(cx - 15), (int)(h * 0.19f), 30, (int)(h * 0.34f), body); // torso (uniform)
            c.FillRect((int)(cx - 15), (int)(h * 0.24f), 30, 5, dark);      // belt line
            c.FillRect((int)(cx - 6), (int)(h * 0.21f), 10, 12, G(0.55f));  // ID badge on chest
            c.FillRect((int)(cx - 22), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            c.FillRect((int)(cx + 15), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            int legTop = (int)(h * 0.53f), legH = (int)(h * 0.44f);
            if (frame == 0)
            {
                c.FillRect((int)(cx - 12), legTop, 9, legH, body);
                c.FillRect((int)(cx + 3), legTop, 9, legH, body);
            }
            else
            {
                c.FillRect((int)(cx - 16), legTop, 9, legH - 6, body);
                c.FillRect((int)(cx + 7), legTop, 9, legH - 6, body);
                c.FillRect((int)(cx - 16), legTop + legH - 8, 14, 6, body); // forward foot
            }
            c.FillRect((int)(cx - 14), h - 6, 12, 6, dark);
            c.FillRect((int)(cx + 2), h - 6, 12, 6, dark);
            return c.ToSprite("custodian_" + frame, WorldPpu);
        }

        private static Sprite MakeAnomaly(int w, int h, AnomalyBehaviorKind kind)
        {
            var c = new Canvas2D(w, h);
            var body = G(0.02f);          // near-black silhouette
            var eye = G(0.95f);
            float cx = w / 2f;
            switch (kind)
            {
                case AnomalyBehaviorKind.Blink: // unnaturally still, long arms
                    c.FillEllipse(cx, h * 0.10f, 15, 19, body);
                    c.FillRect((int)(cx - 17), (int)(h * 0.16f), 34, (int)(h * 0.42f), body);
                    c.FillRect((int)(cx - 26), (int)(h * 0.19f), 9, (int)(h * 0.52f), body);   // long arms
                    c.FillRect((int)(cx + 17), (int)(h * 0.19f), 9, (int)(h * 0.52f), body);
                    c.FillRect((int)(cx - 13), (int)(h * 0.56f), 10, (int)(h * 0.42f), body);
                    c.FillRect((int)(cx + 3), (int)(h * 0.56f), 10, (int)(h * 0.42f), body);
                    c.FillEllipse(cx - 6, h * 0.09f, 2.6f, 3.4f, eye);
                    c.FillEllipse(cx + 7, h * 0.115f, 2.2f, 3.0f, eye); // asymmetric eyes
                    break;
                case AnomalyBehaviorKind.Slide: // legless gliding shape
                    c.FillEllipse(cx, h * 0.14f, 16, 20, body);
                    c.FillEllipse(cx, h * 0.55f, 26, h * 0.42f, body);
                    c.FillEllipse(cx - 7, h * 0.13f, 2.4f, 2.4f, eye);
                    c.FillEllipse(cx + 7, h * 0.13f, 2.4f, 2.4f, eye);
                    break;
                case AnomalyBehaviorKind.Sprint: // leaning runner, stretched
                    c.FillEllipse(cx + 14, h * 0.13f, 14, 17, body);
                    c.FillRect((int)(cx - 24), (int)(h * 0.18f), 52, 26, body);      // leaning torso
                    c.FillRect((int)(cx - 34), (int)(h * 0.30f), 30, 9, body);       // reaching arm
                    c.FillRect((int)(cx - 6), (int)(h * 0.40f), 10, (int)(h * 0.4f), body);
                    c.FillRect((int)(cx + 12), (int)(h * 0.40f), 10, (int)(h * 0.34f), body);
                    c.FillEllipse(cx + 18, h * 0.125f, 2.2f, 3.0f, eye);
                    c.FillEllipse(cx + 24, h * 0.13f, 2.2f, 3.0f, eye);
                    break;
                default: // CoverHug - crouched, arms spread wide (ceiling lurker)
                    c.FillEllipse(cx, h * 0.30f, 22, 26, body);
                    c.FillRect((int)(cx - 84), (int)(h * 0.24f), 76, 12, body);      // spread arms
                    c.FillRect((int)(cx + 8), (int)(h * 0.24f), 76, 12, body);
                    c.FillEllipse(cx, h * 0.62f, 34, h * 0.30f, body);
                    c.FillEllipse(cx - 8, h * 0.27f, 2.4f, 3.2f, eye);
                    c.FillEllipse(cx + 8, h * 0.27f, 2.4f, 3.2f, eye);
                    break;
            }
            var r = new System.Random((int)kind * 977 + 13);
            for (int i = 0; i < 40; i++)
            {
                int x = r.Next(w), y = r.Next(h);
                if (c.Px[y * w + x].r < 0.1f)
                    c.FillRect(x, y, 1 + r.Next(6), 1, G(r.Next(2) == 0 ? 0.9f : 0.05f, 0.5f));
            }
            return c.ToSprite("anomaly_" + kind.ToString().ToLowerInvariant(), WorldPpu);
        }

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
