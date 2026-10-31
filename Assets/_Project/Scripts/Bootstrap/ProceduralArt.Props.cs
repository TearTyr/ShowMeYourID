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

        // The peek's cover: black, with a soft round hole in the middle that shows what is behind it.
        private static Sprite MakePeekHole(int d)
        {
            var c = new Canvas2D(d, d);
            float half = d / 2f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.Clamp01((dist - 0.65f) / 0.35f);   // clear centre, soft rim, black corners
                    c.Px[y * d + x] = new Color(0f, 0f, 0f, a * a * (3f - 2f * a));
                }
            return c.ToSprite("peek_hole", d);   // one unit wide, so the lamp's scale is its diameter
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

        private static Sprite MakeClock(int size, string name = "prop_clock")
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
            return c.ToSprite(name, WorldPpu);
        }

    }
}
