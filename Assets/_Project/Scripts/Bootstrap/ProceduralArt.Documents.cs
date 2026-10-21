using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Desk papers and stamps: clipboard, note paper, ALLOW/REJECT stamps, check/cross icons.
    public static partial class ProceduralArt
    {
        private static Sprite MakeClipboard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.16f));
            c.FillRect(w / 2 - 34, 0, 68, 26, G(0.45f));             // clip
            c.FillRect(14, 20, w - 28, h - 34, G(0.76f));            // paper
            for (int i = 0; i < 8; i++)
            {
                int y = 44 + i * 52;
                c.FillRect(28, y, 26, 26, G(0.85f));                 // checkbox
                c.FillRect(30, y + 2, 22, 22, G(0.2f, 0.15f));
                c.FillRect(66, y + 10, w - 110, 3, G(0.45f));        // row rule
            }
            c.Noise(0.05f, 109);
            return c.ToSprite("clipboard", UiPpu);
        }

        private static Sprite MakeNotePaper(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.74f));
            for (int y = 24; y < h - 20; y += 26) c.FillRect(18, y, w - 36, 2, G(0.5f));
            c.Noise(0.07f, 113);
            for (float a = 0; a < Mathf.PI * 2; a += 0.1f)
                c.FillRect((int)(w * 0.78f + Mathf.Cos(a) * 26), (int)(h * 0.25f + Mathf.Sin(a) * 22), 3, 3, G(0.45f, 0.6f));
            for (int i = 0; i < 34; i++) c.FillRect(w - 34 + i / 2, i, 34 - i, 2, G(0.62f));
            return c.ToSprite("note_paper", UiPpu);
        }

        private static Sprite MakeStamp(bool reject)
        {
            int size = 168;
            var c = new Canvas2D(size, size);
            Color col = reject ? new Color(0.72f, 0.12f, 0.16f, 0.92f) : G(0.2f, 0.9f);
            DrawRing(c, size / 2f, size / 2f, 74, 74, col);
            DrawRing(c, size / 2f, size / 2f, 64, 64, col);
            DrawRing(c, size / 2f, size / 2f, 62, 62, col);
            if (reject)
            {
                c.Line(48, 48, size - 48, size - 48, col, 12);
                c.Line(size - 48, 48, 48, size - 48, col, 12);
            }
            else
            {
                c.Line(46, size / 2, size / 2 - 12, size - 56, col, 12);
                c.Line(size / 2 - 12, size - 56, size - 42, 54, col, 12);
            }
            var r = new System.Random(reject ? 7 : 9);
            for (int i = 0; i < 90; i++) // worn ink gaps
                c.FillRect(r.Next(size), r.Next(size), 2 + r.Next(4), 2 + r.Next(3), G(0, 0));
            var sp = c.ToSprite(reject ? "stamp_reject" : "stamp_allow", UiPpu);
            return sp;
        }

        private static Sprite MakeIcon(bool check)
        {
            int s = 36;
            var c = new Canvas2D(s, s);
            if (check) { c.Line(6, 18, 14, 28, G(0.85f), 5); c.Line(14, 28, 30, 8, G(0.85f), 5); }
            else { c.Line(8, 8, 28, 28, new Color(0.85f, 0.2f, 0.24f), 5); c.Line(28, 8, 8, 28, new Color(0.85f, 0.2f, 0.24f), 5); }
            return c.ToSprite(check ? "icon_check" : "icon_x", UiPpu);
        }
    }
}
