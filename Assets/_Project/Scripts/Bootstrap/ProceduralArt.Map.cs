using System;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // The hand-drawn ground-floor plan: aged paper, rough room outlines, a barred seal and the
    // corridor chalk. Everything is painted in code like the rest of the game's art, so the map
    // ships with no external files - and a real file dropped at Resources/Art/<key>.png still
    // wins (ProceduralArt.Get), which is how the owner's reference drawing can replace it later.
    //
    // Palette: this is a document read under a night-shift lamp, so the paper stays DARK (the
    // horror UI is never blown out by a bright sheet) and the "ink" is a pale warm chalk - the
    // same relationship the ID cards and record sheets use, inverted. The sealed server /
    // identification room is the only red thing on the plan.
    public static partial class ProceduralArt
    {
        private static readonly Color MapPaperBase = new Color(0.115f, 0.108f, 0.096f);
        private static readonly Color MapRoomFill = new Color(0.185f, 0.176f, 0.158f, 0.55f);
        private static readonly Color MapChalk = new Color(0.78f, 0.745f, 0.655f);
        private static readonly Color MapChalkFaint = new Color(0.78f, 0.745f, 0.655f, 0.35f);
        private static readonly Color MapSealInk = new Color(0.66f, 0.16f, 0.18f);

        /// <summary>The sheet the plan is drawn on: aged, stained, creased, with a rough
        /// pencil border and a red district stamp in the corner. Painted once at the map's
        /// design size and stretched to the sheet rect (its detail is soft enough not to care).
        /// </summary>
        private static Sprite MakeMapPaper(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(StableHash("map_paper"));
            c.Fill(MapPaperBase);

            // long uneven ageing: darker at the edges, a warm bloom where the lamp would hit
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float fx = x / (float)(w - 1), fy = y / (float)(h - 1);
                    float edge = Mathf.Max(Mathf.Abs(fx - 0.5f), Mathf.Abs(fy - 0.5f)) * 2f;
                    float lamp = 1f - Mathf.Clamp01(Mathf.Abs(fx - 0.42f) + Mathf.Abs(fy - 0.30f));
                    var p = c.Px[y * w + x];
                    float k = 1f - 0.30f * Mathf.Clamp01(edge) + 0.10f * lamp;
                    c.Px[y * w + x] = new Color(p.r * k, p.g * k, p.b * k, p.a);
                }

            // coffee rings and foxing
            for (int i = 0; i < 7; i++)
            {
                int cx = r.Next(w), cy = r.Next(h), rad = 18 + r.Next(46);
                Ring(c, cx, cy, rad, new Color(0.30f, 0.22f, 0.12f, 0.10f + (float)r.NextDouble() * 0.08f));
            }
            for (int i = 0; i < 260; i++)
            {
                int x = r.Next(w), y = r.Next(h), s = 1 + r.Next(3);
                c.BlendRect(x, y, s, s, new Color(0.42f, 0.36f, 0.26f, 0.05f + (float)r.NextDouble() * 0.07f));
            }

            // folds: one down the middle, one across - a plan that has been kept in a drawer
            Fold(c, w / 2, true);
            Fold(c, (int)(h * 0.46f), false);

            // torn / dog-eared corners so the sheet is not a perfect rectangle
            Corner(c, 0, 0, 1, 1, r);
            Corner(c, w - 1, 0, -1, 1, r);
            Corner(c, 0, h - 1, 1, -1, r);
            Corner(c, w - 1, h - 1, -1, -1, r);

            // the rough pencil border, drawn twice so it reads as a hand-run rule
            RoughRect(c, r, 9, 9, w - 19, h - 19, MapChalkFaint, 2);
            RoughRect(c, r, 13, 13, w - 27, h - 27, new Color(MapChalk.r, MapChalk.g, MapChalk.b, 0.18f), 1);

            // district stamp, bottom-right, half-legible
            int sx = w - 150, sy = h - 74;
            for (int a = 0; a < 64; a++)
            {
                float t = a / 64f * Mathf.PI * 2f;
                RoughLine(c, r, sx + (int)(58 * Mathf.Cos(t)), sy + (int)(26 * Mathf.Sin(t)),
                    sx + (int)(58 * Mathf.Cos(t + 0.12f)), sy + (int)(26 * Mathf.Sin(t + 0.12f)), 0.42f, 2);
            }
            for (int i = 0; i < 3; i++)
                RoughLine(c, r, sx - 40, sy - 8 + i * 9, sx + 40, sy - 8 + i * 9, 0.40f, 2);

            c.Noise(0.035f, StableHash("map_paper_grain"));
            c.Vignette(0.35f);
            return c.ToSprite("map_paper", UiPpu);
        }

        /// <summary>One hand-drawn room: a wobbly double-stroked outline around a soft fill.
        /// Stretched to any room rect - the wobble is proportional, so a corridor and a
        /// classroom both look ruled by the same unsteady hand.</summary>
        private static Sprite MakeMapRoom(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(StableHash("map_room"));
            c.FillRect(4, 4, w - 8, h - 8, MapRoomFill);
            RoughRect(c, r, 5, 5, w - 11, h - 11, MapChalk, 3);
            RoughRect(c, r, 9, 9, w - 19, h - 19, MapChalkFaint, 1);
            // a couple of shading scratches inside, so the fill is not flat digital colour
            for (int i = 0; i < 9; i++)
            {
                int y = 8 + r.Next(Mathf.Max(1, h - 16));
                RoughLine(c, r, 8 + r.Next(6), y, w - 10 - r.Next(w / 3), y + r.Next(-1, 2), 0.30f, 1);
            }
            return c.ToSprite("map_room", UiPpu);
        }

        /// <summary>The sealed server / identification room: the same rough outline in red,
        /// barred with a hatch and a chain, so "you may not go in here" reads at a glance.</summary>
        private static Sprite MakeMapSealed(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(StableHash("map_sealed"));
            c.FillRect(4, 4, w - 8, h - 8, new Color(0.20f, 0.075f, 0.08f, 0.75f));
            for (int x = -h; x < w; x += 13)
                RoughLine(c, r, x, h - 5, x + h, 5, 0.40f, 2);
            RoughRect(c, r, 5, 5, w - 11, h - 11, MapSealInk, 3);
            RoughRect(c, r, 9, 9, w - 19, h - 19, new Color(MapSealInk.r, MapSealInk.g, MapSealInk.b, 0.45f), 1);
            // chain across the door side + a padlock, echoing prop_server_door
            int cy = h / 2;
            for (int x = 8; x < w - 10; x += 16)
                c.FillEllipse(x, cy + (x / 16 % 2 == 0 ? -2 : 2), 7, 5, new Color(0.55f, 0.52f, 0.48f, 0.5f));
            c.FillEllipse(w / 2, cy, 9, 11, new Color(0.60f, 0.57f, 0.52f, 0.75f));
            c.FillRect(w / 2 - 4, cy - 3, 8, 10, new Color(0.22f, 0.20f, 0.18f, 0.9f));
            return c.ToSprite("map_sealed", UiPpu);
        }

        /// <summary>A doorway gap in a wall: two short chalk jambs with the wall broken between
        /// them, so the plan shows how the rooms hang off the one corridor.</summary>
        private static Sprite MakeMapDoor(int w, int h)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(StableHash("map_door"));
            c.FillRect(0, 0, w, 3, MapChalk);
            c.FillRect(0, h - 3, w, 3, MapChalk);
            RoughLine(c, r, w / 2, 3, w / 2 + 6, h / 2, 0.62f, 2);   // the open leaf
            return c.ToSprite("map_door", UiPpu);
        }

        // ---- shared rough-ink helpers (the plan's whole look) --------------------

        // A rectangle ruled by hand: each side is a RoughLine, and the corners miss each other
        // by a pixel or two because the pen overran. Drawn twice by the callers for weight.
        private static void RoughRect(Canvas2D c, System.Random r, int x, int y, int w, int h, Color col, int thick)
        {
            float v = Mathf.Clamp01(Mathf.Max(Mathf.Max(col.r, col.g), col.b));
            float a = col.a;
            int j = 2;   // corner overrun
            RoughLineA(c, r, x - j, y, x + w + j, y + r.Next(-1, 2), v, a, thick);
            RoughLineA(c, r, x - j, y + h, x + w + j, y + h + r.Next(-1, 2), v, a, thick);
            RoughLineA(c, r, x, y - j, x + r.Next(-1, 2), y + h + j, v, a, thick);
            RoughLineA(c, r, x + w, y - j, x + w + r.Next(-1, 2), y + h + j, v, a, thick);
        }

        // RoughLine takes a grey; the map's ink carries an alpha, so this wraps it.
        private static void RoughLineA(Canvas2D c, System.Random r, int x0, int y0, int x1, int y1,
            float v, float a, int thick)
        {
            int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                int x = (int)(x0 + (x1 - x0) * t) + r.Next(-1, 2);
                int yy = (int)(y0 + (y1 - y0) * t) + r.Next(-1, 2);
                for (int d = 0; d < thick; d++)
                {
                    int py = yy + d;
                    if (x < 0 || py < 0 || x >= c.W || py >= c.H) continue;
                    c.Px[py * c.W + x] = Canvas2D.Blend(c.Px[py * c.W + x], new Color(v, v, v * 0.97f, a));
                }
            }
        }

        // A faint ring stain (coffee, water) - outline only, the way a dried ring looks.
        private static void Ring(Canvas2D c, int cx, int cy, int rad, Color col)
        {
            for (int a = 0; a < 90; a++)
            {
                float t = a / 90f * Mathf.PI * 2f;
                float rr = rad + Mathf.Sin(t * 3f) * 2f;
                int x = (int)(cx + rr * Mathf.Cos(t)), y = (int)(cy + rr * 0.72f * Mathf.Sin(t));
                if (x < 0 || y < 0 || x >= c.W || y >= c.H) continue;
                c.Px[y * c.W + x] = Canvas2D.Blend(c.Px[y * c.W + x], col);
                if (x + 1 < c.W) c.Px[y * c.W + x + 1] = Canvas2D.Blend(c.Px[y * c.W + x + 1], col);
            }
        }

        // A crease: one dark line with a paler highlight beside it, so the fold has a ridge.
        private static void Fold(Canvas2D c, int at, bool vertical)
        {
            int len = vertical ? c.H : c.W;
            for (int i = 0; i < len; i++)
            {
                int wobble = (int)(Mathf.Sin(i * 0.05f) * 2f);
                int a = at + wobble, b = a + 1;
                if (vertical)
                {
                    if (a >= 0 && a < c.W) BlendColumn(c, a, i, new Color(0f, 0f, 0f, 0.20f));
                    if (b >= 0 && b < c.W) BlendColumn(c, b, i, new Color(1f, 0.96f, 0.88f, 0.045f));
                }
                else
                {
                    if (a >= 0 && a < c.H) BlendRow(c, i, a, new Color(0f, 0f, 0f, 0.20f));
                    if (b >= 0 && b < c.H) BlendRow(c, i, b, new Color(1f, 0.96f, 0.88f, 0.045f));
                }
            }
        }

        private static void BlendColumn(Canvas2D c, int x, int y, Color col)
        {
            if (x < 0 || y < 0 || x >= c.W || y >= c.H) return;
            c.Px[y * c.W + x] = Canvas2D.Blend(c.Px[y * c.W + x], col);
        }

        private static void BlendRow(Canvas2D c, int x, int y, Color col)
        {
            if (x < 0 || y < 0 || x >= c.W || y >= c.H) return;
            c.Px[y * c.W + x] = Canvas2D.Blend(c.Px[y * c.W + x], col);
        }

        // A dog-ear: the corner is bitten off (transparent) and a small fold shows behind it.
        private static void Corner(Canvas2D c, int x0, int y0, int dx, int dy, System.Random r)
        {
            int size = 14 + r.Next(12);
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size - i; j++)
                {
                    int x = x0 + dx * i, y = y0 + dy * j;
                    if (x < 0 || y < 0 || x >= c.W || y >= c.H) continue;
                    c.Px[y * c.W + x] = new Color(0f, 0f, 0f, 0f);
                }
            for (int i = 0; i < size; i++)
            {
                int x = x0 + dx * i, y = y0 + dy * (size - i);
                if (x < 0 || y < 0 || x >= c.W || y >= c.H) continue;
                c.Px[y * c.W + x] = new Color(0.30f, 0.28f, 0.24f, 0.85f);
            }
        }
    }
}
