using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Room and menu backdrops: menu bg, hallway, classrooms, security office, facade, frames.
    public static partial class ProceduralArt
    {
        private static Sprite MakeSchoolFacade(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(5);
            c.FillRect(0, 0, w, h, G(0.06f));
            c.FillRect(w / 2 - 60, h / 2 - 60, 120, 110, G(0.85f));          // central block
            c.FillRect(w / 2 - 130, h / 2 - 30, 70, 80, G(0.80f));          // wings
            c.FillRect(w / 2 + 60, h / 2 - 30, 70, 80, G(0.80f));
            for (int i = 0; i < 4; i++)                                      // windows
                for (int j = 0; j < 3; j++)
                    c.FillRect(w / 2 - 48 + i * 26, h / 2 - 48 + j * 30, 12, 18, G(0.10f));
            for (int i = 0; i < 3; i++)
            {
                c.FillRect(w / 2 - 120 + i * 22, h / 2 - 18 + (i % 2) * 22, 10, 16, G(0.12f));
                c.FillRect(w / 2 + 70 + i * 22, h / 2 - 18 + (i % 2) * 22, 10, 16, G(0.12f));
            }
            for (int a = 0; a < 40; a++)                                     // clock
            {
                float t = a / 40f * Mathf.PI * 2f;
                int x = (int)(w / 2 + Mathf.Cos(t) * 14), y = (int)(h / 2 - 74 + Mathf.Sin(t) * 14);
                if (x >= 0 && y >= 0 && x < w && y < h) c.Px[y * w + x] = G(0.10f);
            }
            c.FillRect(w / 2 - 1, h / 2 - 84, 2, 10, G(0.10f));
            RoughLine(c, r, 4, h - 6, w - 4, h - 6, 0.85f, 2);               // ground chalk line
            return c.ToSprite("school_facade", UiPpu);
        }

        private static Sprite MakeCameraFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(31);
            int L = 26;
            RoughLine(c, r, 4, 4, 4 + L, 4, 0.9f, 3); RoughLine(c, r, 4, 4, 4, 4 + L, 0.9f, 3);
            RoughLine(c, r, w - 5, 4, w - 5 - L, 4, 0.9f, 3); RoughLine(c, r, w - 5, 4, w - 5, 4 + L, 0.9f, 3);
            RoughLine(c, r, 4, h - 5, 4 + L, h - 5, 0.9f, 3); RoughLine(c, r, 4, h - 5, 4, h - 5 - L, 0.9f, 3);
            RoughLine(c, r, w - 5, h - 5, w - 5 - L, h - 5, 0.9f, 3); RoughLine(c, r, w - 5, h - 5, w - 5, h - 5 - L, 0.9f, 3);
            return c.ToSprite("camera_frame", UiPpu);
        }

        private static Sprite MakeWindowFrame(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(new Color(0.149f, 0.149f, 0.169f));   // #26262b flat frame
            Border2(c, WireInk);
            return c.ToSprite("window_frame", UiPpu);
        }

        private static Sprite MakeMenuBg(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.05f), G(0.13f));
            float vx = w * 0.5f, vy = h * 0.52f;
            c.FillRect(0, (int)(h * 0.72f), w, (int)(h * 0.28f), G(0.07f));           // floor
            for (int i = 0; i < 9; i++)                                                // wall panels
            {
                float t = i / 9f;
                int x = (int)(vx - (vx) * (1 - t * 0.85f));
                c.FillRect(x, (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
                c.FillRect(w - x - 8 - (int)(30 * t), (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
            }
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 5f;
                int lw = (int)(180 * (1 - t)) + 12, lh = Math.Max(3, (int)(10 * (1 - t)));
                c.FillRect((int)(vx - lw / 2f), (int)(vy - h * 0.34f * (1 - t * 0.9f)), lw, lh, G(0.55f - t * 0.3f));
            }
            c.FillEllipse(vx, vy + h * 0.10f, 16, 52, G(0.015f));
            c.FillEllipse(vx, vy + h * 0.035f, 8, 9, G(0.015f));
            c.FillRect((int)vx - 2, (int)(vy + h * 0.028f), 4, 3, G(0.85f, 0.7f)); // single pale eye-glint
            c.Noise(0.05f, 99);
            c.Vignette(0.95f);
            return c.ToSprite("menu_bg", UiPpu);
        }

        // The corridor's depth shroud: pure BLACK whose ALPHA is the radial falloff baked in
        // WORLD units (HallwayDarkness.DepthAt), so the pool around the custodian stays circular
        // even though the quad is stretched to cover the whole hallway from either end of it.
        // RoomBuilder draws it under the room darkness layer and inside the beam mask's sorting
        // range, so the flashlight punches through both; LightController scales the alpha (and
        // drops it to nothing when the room light is on). 10 px per world unit is plenty for a
        // smoothstep gradient - bilinear filtering does the rest.
        // The desk backdrop from the night-shift mockup: one radial grayscale wash (bright pool
        // behind the booth, falling to near-black at the edges). Replaces the flat black room.
        private static Sprite MakeDeskBg(int w, int h)
        {
            var c = new Canvas2D(w, h);
            float cx = w * 0.5f, cy = h * 0.38f;
            float rx = w * 0.55f, ry = h * 0.60f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx) / rx, dy = (y - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float v = d <= 0.45f ? Mathf.Lerp(0.29f, 0.14f, d / 0.45f)
                          : d <= 1.0f ? Mathf.Lerp(0.14f, 0.043f, (d - 0.45f) / 0.55f)
                          : 0.043f;
                    c.Px[y * w + x] = G(v);
                }
            c.Noise(0.03f, 17);
            return c.ToSprite("desk_bg", UiPpu);
        }

        // Waiting-line silhouette for the queue stands: a hooded figure, dark against the desk.
        private static Sprite MakeHoodFigure(int w, int h)
        {
            var c = new Canvas2D(w, h);
            float cx = w / 2f;
            c.FillEllipse(cx, h * 0.22f, w * 0.30f, h * 0.20f, G(0.10f));          // hood
            c.FillEllipse(cx, h * 0.86f, w * 0.42f, h * 0.42f, G(0.08f));          // shoulders
            c.FillRect((int)(cx - w * 0.16f), (int)(h * 0.34f), (int)(w * 0.32f), (int)(h * 0.30f), G(0.09f));
            c.FillEllipse(cx, h * 0.24f, w * 0.16f, h * 0.12f, G(0.03f));          // face shadow
            return c.ToSprite("hood_figure", UiPpu);
        }

        private static Sprite MakeHallDepth()
        {
            const int pxPerWorldUnit = 10;
            float worldW = HallwayDarkness.ShroudWorldW, worldH = HallwayDarkness.ShroudWorldH;
            int w = Mathf.Max(4, Mathf.RoundToInt(worldW * pxPerWorldUnit));
            int h = Mathf.Max(4, Mathf.RoundToInt(worldH * pxPerWorldUnit));
            var c = new Canvas2D(w, h);
            for (int y = 0; y < h; y++)
            {
                float dy = ((y + 0.5f) / h - 0.5f) * worldH;
                for (int x = 0; x < w; x++)
                {
                    float dx = ((x + 0.5f) / w - 0.5f) * worldW;
                    float d = HallwayDarkness.DepthAt(Mathf.Sqrt(dx * dx + dy * dy));
                    c.Px[y * w + x] = new Color(0f, 0f, 0f, d);   // black always; only alpha grades
                }
            }
            // native size == the shroud's world size, so the quad needs no localScale at all
            return c.ToSprite("hall_depth", w / worldW);
        }

    }
}
