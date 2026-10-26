using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // World props: doors, lockers, desks, cabinets, fixtures, the flashlight cone and lamp glow.
    public static partial class ProceduralArt
    {
        private static Sprite MakeLightCone(int w, int h)
        {
            // Soft tapered beam: hot pool at the lamp, feathered shoulders, long decay at the
            // reach, plus a faint wide halo (dust/spill) that stays under the stencil cutoff so
            // it lifts nearby surfaces without cutting the mask. BeamProfile owns the curves.
            var c = new Canvas2D(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                for (int y = 0; y < h; y++)
                {
                    float u = Math.Abs(y - h / 2f) / (h / 2f);      // 0 on axis, 1 at the sprite rim
                    float uN = u / Math.Max(t, 0.08f);              // ...in units of the WEDGE half-width
                    float a = 0.55f * BeamProfile.Light(t, uN);
                    c.Px[y * w + x] = new Color(0.92f, 0.90f, 0.84f, a);
                }
            }
            return c.ToSprite("light_cone", WorldPpu);
        }

        private static void RoughLine(Canvas2D c, System.Random r, int x0, int y0, int x1, int y1, float col, int thick)
        {
            int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = (int)(x0 + (x1 - x0) * t) + r.Next(-1, 2);
                int y = (int)(y0 + (y1 - y0) * t) + r.Next(-1, 2);
                for (int d = 0; d < thick; d++)
                {
                    int yy = y + d;
                    if (x >= 0 && yy >= 0 && x < c.W && yy < c.H)
                        c.Px[yy * c.W + x] = new Color(col, col, col * 0.97f, 1f);
                }
            }
        }

        private static Sprite MakeLampGlow(int d)
        {
            var c = new Canvas2D(d, d);
            float half = d / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.Clamp01(1f - dist);
                    c.Px[y * d + x] = new Color(1f, 0.97f, 0.88f, a * a * 0.85f);
                }
            return c.ToSprite("lamp_glow", UiPpu);
        }

        private static readonly Color WirePaper = new Color(0.820f, 0.812f, 0.780f);   // #d1cfc7
        private static readonly Color WireInk = new Color(0.227f, 0.227f, 0.251f);      // #3a3a40
        private static readonly Color WireBand = new Color(0.231f, 0.231f, 0.243f);     // #3b3b3e
        private static readonly Color WireRule = new Color(0.541f, 0.533f, 0.502f);      // #8a8880
        private static readonly Color WireBar = new Color(0.333f, 0.325f, 0.306f);       // #55534e

        private static void Border2(Canvas2D c, Color col)
        {
            c.FillRect(0, 0, c.W, 2, col);
            c.FillRect(0, c.H - 2, c.W, 2, col);
            c.FillRect(0, 0, 2, c.H, col);
            c.FillRect(c.W - 2, 0, 2, c.H, col);
        }

        // Flat decision circle (ALLOW / CALL PRINCIPAL): solid fill, 2px rim, no speckle.
        private static Sprite MakeDoor(int w, int h, bool security, bool chained = false)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.14f));
            c.FillRect(8, 8, w - 16, h - 12, G(security ? 0.10f : 0.115f));
            c.FillRect(20, 26, w - 40, (int)(h * 0.3f), G(0.085f));
            c.FillRect(20, (int)(h * 0.42f), w - 40, (int)(h * 0.44f), G(0.085f));
            c.FillEllipse(w - 28, h / 2, 6, 6, G(0.55f));
            if (security) c.FillRect(w / 2 - 44, 40, 88, 20, G(0.35f)); // "SECURITY" plate
            if (chained)
            {
                for (int i = 0; i < 5; i++)
                    c.Line(10, 60 + i * 54, w - 10, 84 + i * 54, G(0.45f), 5);   // chains
                c.FillEllipse(w / 2, h / 2, 14, 18, G(0.5f));                    // padlock
                c.FillRect(w / 2 - 10, h / 2 - 4, 20, 26, G(0.42f));
            }
            c.Noise(0.04f, 3);
            return c.ToSprite(chained ? "prop_server_door" : (security ? "prop_door_security" : "prop_door"), WorldPpu);
        }

        private static Sprite MakeLockers(int w, int h)
        {
            var c = new Canvas2D(w, h);
            for (int i = 0; i < 3; i++)
            {
                c.FillRect(i * (w / 3), 0, w / 3 - 4, h, G(0.115f));
                c.FillRect(i * (w / 3) + 5, 5, w / 3 - 14, h - 10, G(0.085f));
                c.FillRect(i * (w / 3) + 18, 30, w / 3 - 44, 6, G(0.3f));
                c.FillRect(i * (w / 3) + 18, 42, w / 3 - 44, 6, G(0.3f));
                c.FillEllipse(i * (w / 3) + w / 6 - 2, h / 2, 3, 3, G(0.42f));
            }
            c.Noise(0.04f, 17);
            return c.ToSprite("prop_lockers", WorldPpu);
        }

        private static Sprite MakeDeskProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, (int)(h * 0.35f), G(0.15f));
            c.FillRect(10, (int)(h * 0.35f), 12, (int)(h * 0.65f), G(0.10f));
            c.FillRect(w - 22, (int)(h * 0.35f), 12, (int)(h * 0.65f), G(0.10f));
            c.FillRect((int)(w * 0.3f), 6, (int)(w * 0.3f), 10, G(0.6f)); // paper on desk
            c.Noise(0.04f, 23);
            return c.ToSprite("prop_desk", WorldPpu);
        }

        private static Sprite MakeChalkboardProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.16f));
            c.FillRect(8, 8, w - 16, h - 20, G(0.055f));
            var r = new System.Random(91);
            for (int i = 0; i < 8; i++)
            {
                int sx = 24 + r.Next(w - 120), sy = 24 + r.Next(h - 70);
                c.Line(sx, sy, sx + 40 + r.Next(80), sy + r.Next(-8, 9), G(0.45f, 0.55f), 2);
            }
            c.FillRect(8, h - 14, w - 16, 6, G(0.25f));
            c.Noise(0.03f, 29);
            return c.ToSprite("prop_chalkboard", WorldPpu);
        }

        private static Sprite MakeWindowProp(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.10f));
            c.FillRect(10, 10, w - 20, h - 20, G(0.19f));     // night glass
            c.FillRect(w / 2 - 4, 10, 8, h - 20, G(0.10f));
            c.FillRect(10, h / 2 - 4, w - 20, 8, G(0.10f));
            c.FillEllipse((int)(w * 0.72f), (int)(h * 0.24f), 22, 22, G(0.34f, 0.6f)); // streetlight glow
            c.FillRect((int)(w * 0.72f) - 2, (int)(h * 0.24f), 4, (int)(h * 0.5f), G(0.08f));
            c.Noise(0.03f, 31);
            return c.ToSprite("prop_window", WorldPpu);
        }

        private static Sprite MakeNoticeBoard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.15f));
            c.FillRect(8, 8, w - 16, h - 16, G(0.10f));
            var r = new System.Random(57);
            for (int i = 0; i < 7; i++) // pinned papers
            {
                int px = 16 + r.Next(w - 80), py = 16 + r.Next(h - 70);
                c.FillRect(px, py, 52 + r.Next(24), 44 + r.Next(28), G(0.68f, 0.9f));
                c.FillRect(px + 22, py - 3, 6, 6, G(0.3f));
            }
            c.Noise(0.03f, 37);
            return c.ToSprite("prop_noticeboard", WorldPpu);
        }

        private static Sprite MakeCabinet(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.125f));
            for (int i = 0; i < 3; i++)
            {
                int dy = 10 + i * (h / 3);
                c.FillRect(8, dy, w - 16, h / 3 - 14, G(0.09f));
                c.FillRect(w / 2 - 18, dy + (h / 3) / 2 - 3, 36, 6, G(0.4f));
            }
            c.Noise(0.04f, 41);
            return c.ToSprite("prop_cabinet", WorldPpu);
        }

        private static Sprite MakeClock(int size)
        {
            var c = new Canvas2D(size, size);
            int half = size / 2;
            c.FillEllipse(half, half, half - 2, half - 2, G(0.14f));
            c.FillEllipse(half, half, half - 8, half - 8, G(0.78f));
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                c.FillRect(half + (int)(Mathf.Sin(a) * (half - 14)) - 1, half - (int)(Mathf.Cos(a) * (half - 14)) - 1, 3, 3, G(0.15f));
            }
            float ha = (1 + 47 / 60f) * Mathf.PI / 6f, ma = 47 * Mathf.PI / 30f;
            c.Line(half, half, half + (int)(Mathf.Sin(ha) * half * 0.45f), half - (int)(Mathf.Cos(ha) * half * 0.45f), G(0.1f), 4);
            c.Line(half, half, half + (int)(Mathf.Sin(ma) * half * 0.72f), half - (int)(Mathf.Cos(ma) * half * 0.72f), G(0.1f), 2);
            c.FillEllipse(half, half, 4, 4, G(0.2f));
            return c.ToSprite("prop_clock", WorldPpu);
        }

        private static Sprite MakePhotoFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.17f));
            c.FillRect(8, 8, w - 16, h - 16, G(0.55f));     // old photo
            var r = new System.Random(71);
            for (int row = 0; row < 3; row++)               // rows of heads
                for (int i = 0; i < 9; i++)
                {
                    int x = 16 + i * ((w - 32) / 9), y = 20 + row * ((h - 36) / 3);
                    c.FillEllipse(x + 4, y + 8, 4, 5, G(0.2f));
                    c.FillRect(x, y + 12, 9, 8, G(0.2f));
                }
            c.FillRect(w - 34, 22, 12, 20, G(0.05f));       // the ageless staff figure
            c.FillEllipse(w - 28, 18, 5, 5, G(0.05f));
            c.FillEllipse(w - 30, 17, 1.5f, 1.5f, G(0.95f)); // glint
            c.Noise(0.08f, 73);
            return c.ToSprite("prop_photoframe", WorldPpu);
        }

        private static Sprite MakeLightFixture(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, h / 2 - 6, w, 12, G(0.13f));
            c.FillRect(w / 2 - 90, 0, 180, h, G(0.16f));
            c.FillRect(w / 2 - 82, 6, 164, h - 14, G(0.7f));
            return c.ToSprite("prop_lightfixture", WorldPpu);
        }

        private static Sprite MakeLostId(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.FillRect(0, 0, w, h, G(0.72f));
            c.FillRect(2, 2, w - 4, 8, G(0.3f));
            c.FillRect(5, 13, 14, 18, G(0.45f));           // photo slot
            for (int i = 0; i < 4; i++) c.FillRect(23, 14 + i * 5, w - 28, 2, G(0.35f));
            c.Noise(0.06f, 79);
            return c.ToSprite("prop_lostid", WorldPpu);
        }
    }
}
