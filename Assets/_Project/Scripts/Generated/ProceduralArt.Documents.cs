using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Scenes
{
    // Desk papers and stamps: the record sheet, ID frame, ALLOW/REJECT stamps.
    public static partial class ProceduralArt
    {
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

        // Speech-bubble tail: a paper triangle pointing LEFT (toward the visitor's face),
        // bordered on its two slanted sides. The base (right edge) is borderless and is
        // buried a few pixels under the bubble panel, so bubble + tail read as one
        // continuous outline - the classic comics seam.
        private static Sprite MakeBubbleTail()
        {
            int w = 20, h = 26;
            var c = new Canvas2D(w, h);
            var fill = new Color(0.859f, 0.851f, 0.824f);   // the desk's paper stock
            var edge = new Color(0.30f, 0.30f, 0.32f);       // UIFactory.Border
            for (int y = 0; y < h; y++)
            {
                float dy = Mathf.Abs(y - (h - 1) * 0.5f);
                float xEdge = dy * (w - 4f) / ((h - 1) * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    if (x < xEdge - 0.5f) continue;
                    c.Px[y * w + x] = x < xEdge + 1.8f ? edge : fill;
                }
            }
            return c.ToSprite("bubble_tail", UiPpu);
        }

        // Floor-files folder front: a manila tab poking up on the left of a worn folder body.
        // Drawn dark - the cabinet lives under the desk lamp, not in an office. Stretched
        // across the station's tab rows; the label text sits on the body below the tab.
        private static Sprite MakeFolderTab(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var manila = new Color(0.44f, 0.39f, 0.27f);
            var manilaLit = new Color(0.54f, 0.48f, 0.33f);
            var edge = new Color(0.16f, 0.14f, 0.10f);
            int tabH = Mathf.Max(5, h / 4);          // the poking-up tab strip
            int tabW = w * 2 / 5;
            c.FillRect(14, 0, tabW, tabH + 2, manilaLit);                       // tab
            c.FillRect(14, 0, tabW, 2, new Color(0.62f, 0.56f, 0.40f));         // lit tab edge
            c.FillRect(0, tabH, w, h - tabH, manila);                           // folder body
            c.FillRect(0, tabH, w, 2, new Color(0.60f, 0.54f, 0.38f, 0.7f));    // fold highlight
            c.FillRect(0, h - 2, w, 2, edge);                                   // shadowed foot
            c.FillRect(w - 2, tabH, 2, h - tabH, edge);                         // shadowed edge
            c.Noise(0.05f, 131);                                                // worn paper grain
            var r = new System.Random(17);
            for (int i = 0; i < 14; i++)                                        // scuffs and creases
                c.FillRect(r.Next(w), tabH + r.Next(Mathf.Max(1, h - tabH)), 2 + r.Next(5), 1,
                    new Color(0.30f, 0.26f, 0.18f, 0.5f));
            return c.ToSprite("folder_tab", UiPpu);
        }

        // The desk's click cursor: a blocky pointing hand in the game's lo-fi procedural
        // style. HoverCursor swaps it in over "clickable slivers" (the ID's black spine).
        // Canvas row 0 is the TOP: the fingertip sits at ~(8,2), which is the hotspot.
        private static Sprite MakeCursorHand(int s)
        {
            var c = new Canvas2D(s, s);
            var ink = new Color(0.07f, 0.07f, 0.08f);
            var skin = new Color(0.92f, 0.92f, 0.88f);
            c.FillRect(6, 0, 8, 14, ink);     // dark silhouette pass (reads on light paper)
            c.FillRect(3, 8, 18, 15, ink);
            c.FillRect(7, 1, 4, 12, skin);    // pointing finger
            c.FillRect(11, 7, 3, 6, skin);    // folded knuckles
            c.FillRect(14, 8, 3, 5, skin);
            c.FillRect(4, 9, 15, 12, skin);   // palm
            c.FillRect(1, 11, 4, 6, skin);    // thumb
            return c.ToSprite("cursor_hand", UiPpu);
        }
    }
}
