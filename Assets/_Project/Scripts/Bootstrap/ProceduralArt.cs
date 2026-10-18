using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    /// <summary>Procedural-first art pipeline (per master-prompt architecture): every sprite
    /// the game needs is generated at boot into a cache. High-contrast monochrome per the
    /// approved art direction; red only on interactive/danger elements (drawn by UI layer).
    /// If the team later ships real assets, drop them in Resources/Art/&lt;key&gt;.png and
    /// they take priority (graceful upgrade path, same keys).</summary>
    public static class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        public const float WorldPpu = 64f;   // world sprites: 64 px per unit
        public const float UiPpu = 100f;     // UI/document sprites

        public static Sprite Get(string key)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            // upgrade path: real asset in Resources/Art wins over procedural
            try
            {
                var tex = Resources.Load<Texture2D>("Art/" + key);
                if (tex != null)
                {
                    var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), key.EndsWith("_bg") ? WorldPpu : UiPpu);
                    Cache[key] = spr;
                    return spr;
                }
            }
            catch (Exception) { /* no resources — fall through to procedural */ }

            var built = Build(key);
            if (built != null) Cache[built.name] = built;
            return built;
        }

        // ---------------------------------------------------------------- canvas helpers

        private class Canvas2D
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public Canvas2D(int w, int h) { W = w; H = h; Px = new Color[w * h]; }
            public void Fill(Color c) { for (int i = 0; i < Px.Length; i++) Px[i] = c; }
            public void FillRect(int x, int y, int w, int h, Color c)
            {
                int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
                int x1 = Math.Min(W - 1, x + w - 1), y1 = Math.Min(H - 1, y + h - 1);
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++) Px[yy * W + xx] = c;
            }
            public void BlendRect(int x, int y, int w, int h, Color c)
            {
                int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
                int x1 = Math.Min(W - 1, x + w - 1), y1 = Math.Min(H - 1, y + h - 1);
                for (int yy = y0; yy <= y1; yy++)
                    for (int xx = x0; xx <= x1; xx++) Px[yy * W + xx] = Blend(Px[yy * W + xx], c);
            }
            public void FillEllipse(float cx, float cy, float rx, float ry, Color c)
            {
                int x0 = Mathf.Max(0, (int)(cx - rx)), x1 = Mathf.Min(W - 1, (int)(cx + rx));
                int y0 = Mathf.Max(0, (int)(cy - ry)), y1 = Mathf.Min(H - 1, (int)(cy + ry));
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Px[y * W + x] = c;
                    }
            }
            public void Line(int x0, int y0, int x1, int y1, Color c, int thick = 1)
            {
                int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
                int err = dx - dy;
                while (true)
                {
                    FillRect(x0 - thick / 2, y0 - thick / 2, thick, thick, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x0 += sx; }
                    if (e2 < dx) { err += dx; y0 += sy; }
                }
            }
            public void GradientV(Color top, Color bottom)
            {
                for (int y = 0; y < H; y++)
                {
                    Color c = Color.Lerp(top, bottom, H > 1 ? y / (float)(H - 1) : 0f);
                    for (int x = 0; x < W; x++) Px[y * W + x] = c;
                }
            }
            public void Noise(float amount, int seed)
            {
                var r = new System.Random(seed);
                for (int i = 0; i < Px.Length; i++)
                {
                    float n = ((float)r.NextDouble() - 0.5f) * amount;
                    var p = Px[i];
                    Px[i] = new Color(Clamp01(p.r + n), Clamp01(p.g + n), Clamp01(p.b + n), p.a);
                }
            }
            public void ShiftRows(System.Random r, int slices, int maxShift)
            {
                for (int s = 0; s < slices; s++)
                {
                    int y = r.Next(H), hgt = 1 + r.Next(4), shift = r.Next(-maxShift, maxShift + 1);
                    for (int yy = y; yy < Math.Min(H, y + hgt); yy++)
                    {
                        var row = new Color[W];
                        Array.Copy(Px, yy * W, row, 0, W);
                        for (int x = 0; x < W; x++)
                        {
                            int src = (x - shift) % W; if (src < 0) src += W;
                            Px[yy * W + x] = row[src];
                        }
                    }
                }
            }
            public void Vignette(float strength)
            {
                float cx = W / 2f, cy = H / 2f, maxD = Mathf.Sqrt(cx * cx + cy * cy);
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / maxD;
                        Px[y * W + x] = Color.Lerp(Px[y * W + x], Color.black, Mathf.Clamp01((d - 0.45f) / 0.55f) * strength);
                    }
                }
            private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
            public Sprite ToSprite(string name, float ppu)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels(Px);
                tex.Apply(false, false);
                tex.name = name;
                var sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu);
                sp.name = name;
                return sp;
            }
            public static Color Blend(Color dst, Color src)
            {
                float a = src.a + dst.a * (1 - src.a);
                if (a <= 0.0001f) return new Color(0, 0, 0, 0);
                return new Color((src.r * src.a + dst.r * dst.a * (1 - src.a)) / a,
                                 (src.g * src.a + dst.g * dst.a * (1 - src.a)) / a,
                                 (src.b * src.a + dst.b * dst.a * (1 - src.a)) / a, a);
            }
        }

        private static Color G(float v, float a = 1f) => new Color(v, v, v, a);

        // ---------------------------------------------------------------- dispatcher

        private static Sprite Build(string key)
        {
            switch (key)
            {
                case "solid_white": return MakeSolid(G(1f));
                case "vignette": return MakeVignette(512, 384, 0.9f);
                case "noise_overlay": return MakeNoise(256, 256, 0.5f, 7);
                case "flash": return MakeFlash(256);
                case "light_cone": return MakeLightCone(512, 288);
                case "menu_bg": return MakeMenuBg(1280, 720);
                case "hallway_bg": return MakeHallway(1536, 640);
                case "classroom1_bg": return MakeClassroom(1152, 640, false);
                case "classroom2_bg": return MakeClassroom(1152, 640, true);
                case "security_bg": return MakeSecurityRoom(1280, 720);
                case "prop_door": return MakeDoor(160, 384, false);
                case "prop_door_security": return MakeDoor(160, 384, true);
                case "prop_server_door": return MakeDoor(208, 384, false, chained: true);
                case "prop_lockers": return MakeLockers(288, 336);
                case "prop_desk": return MakeDeskProp(208, 128);
                case "prop_chalkboard": return MakeChalkboardProp(384, 224);
                case "prop_window": return MakeWindowProp(224, 300);
                case "prop_noticeboard": return MakeNoticeBoard(208, 160);
                case "prop_cabinet": return MakeCabinet(160, 272);
                case "prop_clock": return MakeClock(112);
                case "prop_photoframe": return MakePhotoFrame(144, 108);
                case "prop_lightfixture": return MakeLightFixture(208, 40);
                case "prop_lostid": return MakeLostId(56, 36);
                case "custodian_0": return MakeCustodian(96, 224, 0);
                case "custodian_1": return MakeCustodian(96, 224, 1);
                case "anomaly_blink": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Blink);
                case "anomaly_slide": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Slide);
                case "anomaly_sprint": return MakeAnomaly(160, 336, AnomalyBehaviorKind.Sprint);
                case "anomaly_coverhug": return MakeAnomaly(192, 224, AnomalyBehaviorKind.CoverHug);
                case "id_card_frame": return MakeIdCard(430, 280);
                case "record_sheet": return MakeRecordSheet(450, 580);
                case "clipboard": return MakeClipboard(370, 490);
                case "note_paper": return MakeNotePaper(390, 300);
                case "stamp_allow": return MakeStamp(false);
                case "stamp_reject": return MakeStamp(true);
                case "icon_check": return MakeIcon(true);
                case "icon_x": return MakeIcon(false);
                default:
                    Debug.LogWarning("[ProceduralArt] unknown sprite key '" + key + "' — returning solid.");
                    return MakeSolid(G(1f, 0f));
            }
        }

        // ---------------------------------------------------------------- portraits

        /// <summary>ID-photo / window-face portrait. Deterministic per Portrait descriptor.</summary>
        public static Sprite Portrait(string keyPrefix, Portrait p, int w = 168, int h = 208)
        {
            string key = keyPrefix + "_" + p.HairStyle + p.FaceVariant + p.Accessory + (p.Distorted ? "d" : "c");
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var c = new Canvas2D(w, h);
            c.GradientV(G(0.80f), G(0.62f));            // paper-gray photo backdrop
            c.Noise(0.05f, key.GetHashCode());
            float cx = w / 2f, baseY = h * 0.98f;

            // shoulders + neck + head (silhouette style)
            var skin = G(0.30f);
            c.FillEllipse(cx, baseY + h * 0.10f, w * 0.46f, h * 0.22f, skin);      // shoulders
            c.FillRect((int)(cx - w * 0.07f), (int)(h * 0.62f), (int)(w * 0.14f), (int)(h * 0.14f), skin); // neck
            float headCy = h * 0.47f, headRx = w * 0.27f, headRy = h * 0.24f;
            c.FillEllipse(cx, headCy, headRx, headRy, skin);                        // head

            // hair variations
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

            // face variations: eyes / nose / mouth (light cuts in the silhouette)
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

            // accessories
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

            // anomaly distortion: glitch slices, offset eye, static patches, stretched mouth
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

            // photo frame edge (worn)
            for (int i = 0; i < w; i++) { c.Px[i] = G(0.25f); c.Px[(h - 1) * w + i] = G(0.25f); }
            for (int i = 0; i < h; i++) { c.Px[i * w] = G(0.25f); c.Px[i * w + w - 1] = G(0.25f); }

            var sp = c.ToSprite(key, UiPpu);
            Cache[key] = sp;
            return sp;
        }

        private static void DrawRing(Canvas2D c, float cx, float cy, float rx, float ry, Color col)
        {
            for (float a = 0; a < Mathf.PI * 2; a += 0.06f)
                c.FillRect((int)(cx + Mathf.Cos(a) * rx), (int)(cy + Mathf.Sin(a) * ry), 2, 2, col);
        }

        // ---------------------------------------------------------------- simple pieces

        private static Sprite MakeSolid(Color c)
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c;
            t.SetPixels(px); t.Apply(false, false); t.name = "solid";
            var s = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), UiPpu);
            s.name = "solid_white";
            return s;
        }

        private static Sprite MakeVignette(int w, int h, float strength)
        {
            var c = new Canvas2D(w, h);
            float cx = w / 2f, cy = h / 2f, maxD = Mathf.Sqrt(cx * cx + cy * cy);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + ((y - cy) * 1.15f) * ((y - cy) * 1.15f)) / maxD;
                    float a = Mathf.Clamp01((d - 0.35f) / 0.65f) * strength;
                    c.Px[y * w + x] = new Color(0, 0, 0, a * a);
                }
            return c.ToSprite("vignette", UiPpu);
        }

        private static Sprite MakeNoise(int w, int h, float alpha, int seed)
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(seed);
            for (int i = 0; i < w * h; i++)
            {
                float v = (float)r.NextDouble();
                c.Px[i] = new Color(v, v, v, alpha * v);
            }
            return c.ToSprite("noise_overlay", UiPpu);
        }

        private static Sprite MakeFlash(int size)
        {
            var c = new Canvas2D(size, size);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    c.Px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(1f - d) * 0.9f);
                }
            return c.ToSprite("flash", UiPpu);
        }

        private static Sprite MakeLightCone(int w, int h)
        {
            var c = new Canvas2D(w, h);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);                       // 0 at player, 1 at far end
                float halfH = h * 0.06f + t * h * 0.44f;             // widening cone
                float alpha = (1f - t * 0.75f) * 0.5f;
                for (int y = 0; y < h; y++)
                {
                    float dy = Mathf.Abs(y - h / 2f);
                    float edge = Mathf.Clamp01((halfH - dy) / (h * 0.08f));
                    c.Px[y * w + x] = new Color(0.9f, 0.9f, 0.85f, alpha * edge);
                }
            }
            return c.ToSprite("light_cone", WorldPpu);
        }

        // ---------------------------------------------------------------- backgrounds

        private static Sprite MakeMenuBg(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.05f), G(0.13f));
            // distant hallway perspective
            float vx = w * 0.5f, vy = h * 0.52f;
            c.FillRect(0, (int)(h * 0.72f), w, (int)(h * 0.28f), G(0.07f));           // floor
            for (int i = 0; i < 9; i++)                                                // wall panels
            {
                float t = i / 9f;
                int x = (int)(vx - (vx) * (1 - t * 0.85f));
                c.FillRect(x, (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
                c.FillRect(w - x - 8 - (int)(30 * t), (int)(vy - h * 0.30f * (1 - t)), 8 + (int)(30 * t), (int)(h * 0.55f * (1 - t) + h * 0.16f), G(0.10f + t * 0.04f));
            }
            // ceiling lights
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 5f;
                int lw = (int)(180 * (1 - t)) + 12, lh = Math.Max(3, (int)(10 * (1 - t)));
                c.FillRect((int)(vx - lw / 2f), (int)(vy - h * 0.34f * (1 - t * 0.9f)), lw, lh, G(0.55f - t * 0.3f));
            }
            // lone silhouette in the distance
            c.FillEllipse(vx, vy + h * 0.10f, 16, 52, G(0.015f));
            c.FillEllipse(vx, vy + h * 0.035f, 8, 9, G(0.015f));
            c.FillRect((int)vx - 2, (int)(vy + h * 0.028f), 4, 3, G(0.85f, 0.7f)); // single pale eye-glint
            c.Noise(0.05f, 99);
            c.Vignette(0.95f);
            return c.ToSprite("menu_bg", UiPpu);
        }

        private static Sprite MakeHallway(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.10f), G(0.16f));                       // upper wall
            int floorY = (int)(h * 0.74f);
            c.FillRect(0, floorY, w, h - floorY, G(0.07f));        // floor
            c.FillRect(0, floorY, w, 3, G(0.22f));                 // baseboard line
            // lockers + doors along the wall
            var r = new System.Random(21);
            for (int x = 24; x < w - 100; x += 180)
            {
                bool isDoor = r.Next(3) == 0;
                if (isDoor)
                {
                    c.FillRect(x, floorY - 300, 120, 300, G(0.13f));
                    c.FillRect(x + 6, floorY - 294, 108, 288, G(0.09f));
                    c.FillEllipse(x + 100, floorY - 150, 5, 5, G(0.5f));     // handle
                    c.FillRect(x + 30, floorY - 268, 60, 18, G(0.35f));      // room plate
                }
                else
                {
                    for (int lx = 0; lx < 3; lx++)
                    {
                        c.FillRect(x + lx * 44, floorY - 260, 40, 260, G(0.115f));
                        c.FillRect(x + lx * 44 + 4, floorY - 256, 32, 252, G(0.085f));
                        c.FillRect(x + lx * 44 + 14, floorY - 220, 12, 3, G(0.3f));   // vent slits
                        c.FillRect(x + lx * 44 + 14, floorY - 214, 12, 3, G(0.3f));
                        c.FillEllipse(x + lx * 44 + 34, floorY - 130, 2.5f, 2.5f, G(0.4f));
                    }
                }
            }
            // ceiling lights (some dark => flicker targets)
            for (int x = 60; x < w; x += 240)
            {
                bool lit = r.Next(4) != 0;
                c.FillRect(x, 18, 150, 12, G(lit ? 0.72f : 0.10f));
                if (lit) c.BlendRect(x - 60, 30, 270, 200, G(0.5f, 0.05f));  // light pool
            }
            // floor tiles
            for (int x = 0; x < w; x += 96) c.FillRect(x, floorY, 2, h - floorY, G(0.09f));
            c.Noise(0.045f, 5);
            c.Vignette(0.55f);
            return c.ToSprite("hallway_bg", WorldPpu);
        }

        private static Sprite MakeClassroom(int w, int h, bool room2)
        {
            var c = new Canvas2D(w, h);
            c.GradientV(G(0.11f), G(0.15f));
            int floorY = (int)(h * 0.72f);
            c.FillRect(0, floorY, w, h - floorY, G(0.075f));
            // back wall chalkboard
            int bx = room2 ? (int)(w * 0.52f) : (int)(w * 0.12f);
            c.FillRect(bx, (int)(h * 0.16f), (int)(w * 0.36f), (int)(h * 0.34f), G(0.16f));     // frame
            c.FillRect(bx + 8, (int)(h * 0.16f) + 8, (int)(w * 0.36f) - 16, (int)(h * 0.34f) - 16, G(0.055f)); // board
            var r = new System.Random(room2 ? 77 : 33);
            for (int i = 0; i < 7; i++)                                                          // chalk scribbles
            {
                int sx = bx + 24 + r.Next((int)(w * 0.36f) - 80);
                int sy = (int)(h * 0.16f) + 30 + r.Next((int)(h * 0.34f) - 70);
                c.Line(sx, sy, sx + 30 + r.Next(90), sy + r.Next(-6, 7), G(0.5f, 0.5f), 2);
            }
            c.FillRect(bx + 8, (int)(h * 0.16f) + (int)(h * 0.34f) - 6, (int)(w * 0.36f) - 16, 6, G(0.25f)); // tray
            // windows on the opposite wall
            int wx = room2 ? (int)(w * 0.06f) : (int)(w * 0.62f);
            for (int i = 0; i < 2; i++)
            {
                int x = wx + i * 260;
                c.FillRect(x, (int)(h * 0.18f), 200, (int)(h * 0.36f), G(0.09f));
                c.FillRect(x + 8, (int)(h * 0.18f) + 8, 184, (int)(h * 0.36f) - 16, G(0.20f));  // night glass
                c.FillRect(x + 96, (int)(h * 0.18f) + 8, 8, (int)(h * 0.36f) - 16, G(0.09f));   // mullion
                c.FillRect(x + 8, (int)(h * 0.18f) + (int)(h * 0.18f), 184, 8, G(0.09f));
                // faint yard + streetlight
                c.FillEllipse(x + 150, (int)(h * 0.18f) + 40, 26, 26, G(0.30f, 0.5f));
            }
            // desk rows (silhouettes)
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 5; col++)
                {
                    int x = 90 + col * (int)(w * 0.185f);
                    int y = floorY + 18 + row * 46;
                    int dw = 130 - row * 8, dh = 26 - row * 3;
                    c.FillRect(x, y, dw, dh, G(0.13f + row * 0.02f));
                    c.FillRect(x + 12, y + dh, 6, 30 - row * 4, G(0.10f));
                    c.FillRect(x + dw - 18, y + dh, 6, 30 - row * 4, G(0.10f));
                }
            // teacher desk + cabinet + clock
            c.FillRect(30, floorY - 96, 190, 96, G(0.12f));
            c.FillRect(38, floorY - 88, 174, 84, G(0.09f));
            c.FillRect(w - 150, floorY - 240, 120, 240, G(0.115f));
            for (int i = 0; i < 3; i++) c.FillRect(w - 142, floorY - 230 + i * 78, 104, 70, G(0.085f));
            c.FillEllipse(w / 2, (int)(h * 0.10f), 34, 34, G(0.14f));
            c.FillEllipse(w / 2, (int)(h * 0.10f), 28, 28, G(0.75f));
            c.Line(w / 2, (int)(h * 0.10f), w / 2, (int)(h * 0.10f) - 18, G(0.1f), 3);   // hour hand
            c.Line(w / 2, (int)(h * 0.10f), w / 2 + 12, (int)(h * 0.10f) + 4, G(0.1f), 2); // minute at :47-ish
            // door
            c.FillRect(w - 320, floorY - 300, 120, 300, G(0.13f));
            c.FillRect(w - 314, floorY - 294, 108, 288, G(0.095f));
            c.FillEllipse(w - 224, floorY - 150, 5, 5, G(0.5f));
            // ceiling light
            c.FillRect(w / 2 - 160, 14, 320, 10, G(0.6f));
            c.BlendRect(w / 2 - 260, 24, 520, 240, G(0.5f, 0.05f));
            c.Noise(0.04f, room2 ? 12 : 8);
            c.Vignette(0.6f);
            return c.ToSprite(room2 ? "classroom2_bg" : "classroom1_bg", WorldPpu);
        }

        private static Sprite MakeSecurityRoom(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.06f));
            // back wall + big observation window (where visitors appear)
            c.FillRect(0, 0, w, (int)(h * 0.66f), G(0.09f));
            int wx = (int)(w * 0.24f), wy = (int)(h * 0.08f), ww = (int)(w * 0.52f), wh = (int)(h * 0.44f);
            c.FillRect(wx - 12, wy - 12, ww + 24, wh + 24, G(0.16f));          // frame
            c.FillRect(wx, wy, ww, wh, G(0.22f));                              // glass (visitor drawn under UI glass tint)
            for (int i = 1; i <= 2; i++) c.FillRect(wx + ww * i / 3 - 3, wy, 6, wh, G(0.14f)); // bars
            // hallway silhouette behind glass
            c.FillRect(wx, wy + (int)(wh * 0.72f), ww, wh - (int)(wh * 0.72f), G(0.17f));
            // desk spanning the bottom
            c.FillRect(0, (int)(h * 0.66f), w, h - (int)(h * 0.66f), G(0.11f));
            c.FillRect(0, (int)(h * 0.66f), w, 6, G(0.24f));
            // monitor + scanner + lamp on desk edges
            c.FillRect((int)(w * 0.03f), (int)(h * 0.52f), 150, 100, G(0.14f));
            c.FillRect((int)(w * 0.03f) + 8, (int)(h * 0.52f) + 8, 134, 78, G(0.30f));
            c.FillRect((int)(w * 0.03f) + 60, (int)(h * 0.52f) + 100, 30, 22, G(0.12f));
            c.FillRect((int)(w * 0.88f), (int)(h * 0.56f), 120, 60, G(0.15f));   // ID scanner
            c.FillRect((int)(w * 0.88f) + 10, (int)(h * 0.56f) + 8, 100, 8, G(0.4f));
            c.Noise(0.04f, 42);
            c.Vignette(0.5f);
            return c.ToSprite("security_bg", UiPpu);
        }

        // ---------------------------------------------------------------- props

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
            // stopped at 1:47 (the minute the closure order was signed)
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

        // ---------------------------------------------------------------- characters

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
            // arms
            c.FillRect((int)(cx - 22), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            c.FillRect((int)(cx + 15), (int)(h * 0.21f), 7, (int)(h * 0.26f), body);
            // legs — 2-frame walk cycle
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
                default: // CoverHug — crouched, arms spread wide (ceiling lurker)
                    c.FillEllipse(cx, h * 0.30f, 22, 26, body);
                    c.FillRect((int)(cx - 84), (int)(h * 0.24f), 76, 12, body);      // spread arms
                    c.FillRect((int)(cx + 8), (int)(h * 0.24f), 76, 12, body);
                    c.FillEllipse(cx, h * 0.62f, 34, h * 0.30f, body);
                    c.FillEllipse(cx - 8, h * 0.27f, 2.4f, 3.2f, eye);
                    c.FillEllipse(cx + 8, h * 0.27f, 2.4f, 3.2f, eye);
                    break;
            }
            // static fringe (visual distortion for anomalies, per approved style)
            var r = new System.Random((int)kind * 977 + 13);
            for (int i = 0; i < 40; i++)
            {
                int x = r.Next(w), y = r.Next(h);
                if (c.Px[y * w + x].r < 0.1f)
                    c.FillRect(x, y, 1 + r.Next(6), 1, G(r.Next(2) == 0 ? 0.9f : 0.05f, 0.5f));
            }
            return c.ToSprite("anomaly_" + kind.ToString().ToLowerInvariant(), WorldPpu);
        }

        // ---------------------------------------------------------------- documents (UI)

        private static Sprite MakeIdCard(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.80f));                                        // worn card stock
            c.FillRect(0, 0, w, 44, G(0.22f));                       // header band
            c.FillRect(14, 58, 120, 150, G(0.55f));                  // photo slot frame
            c.FillRect(18, 62, 112, 142, G(0.86f));                  // photo slot (portrait drawn on top by UI)
            for (int i = 0; i < 5; i++) c.FillRect(150, 66 + i * 30, w - 170, 3, G(0.45f)); // text rules
            c.FillRect(14, h - 52, w - 28, 3, G(0.35f));
            c.FillRect(14, h - 40, w - 120, 16, G(0.7f));            // barcode strip
            var r = new System.Random(101);
            for (int i = 0; i < 26; i++)
                c.FillRect(20 + i * ((w - 40) / 26), h - 40, 6 + r.Next(10), 16, G(0.15f));
            c.Noise(0.05f, 103);
            for (int i = 0; i < w; i++) { c.Px[i] = G(0.35f); c.Px[(h - 1) * w + i] = G(0.35f); }
            for (int i = 0; i < h; i++) { c.Px[i * w] = G(0.35f); c.Px[i * w + w - 1] = G(0.35f); }
            return c.ToSprite("id_card_frame", UiPpu);
        }

        private static Sprite MakeRecordSheet(int w, int h)
        {
            var c = new Canvas2D(w, h);
            c.Fill(G(0.78f));
            c.FillRect(0, 0, w, 56, G(0.24f));
            for (int y = 84; y < h - 90; y += 42)                    // form lines
            {
                c.FillRect(20, y, w * 2 / 5, 3, G(0.5f));
                c.FillRect(w / 2 + 10, y, w * 2 / 5 - 30, 3, G(0.5f));
            }
            c.FillRect(w - 170, h - 84, 140, 60, G(0.85f));          // stamp box
            for (int i = 0; i < w - 170; i += 6) c.FillRect(20 + i, h - 84, 3, 60, G(0.62f));
            c.Noise(0.06f, 107);
            return c.ToSprite("record_sheet", UiPpu);
        }

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
            // coffee ring + folded corner (worn institutional look)
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

        /// <summary>Clears the cache (used on teardown / domain reload).</summary>
        public static void ClearCache()
        {
            foreach (var kv in Cache)
                if (kv.Value != null && kv.Value.texture != null) UnityEngine.Object.Destroy(kv.Value.texture);
            Cache.Clear();
        }
    }
}
