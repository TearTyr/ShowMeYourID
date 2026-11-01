using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Scenes
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
