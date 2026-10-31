using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    // Paints every sprite in code; real files in Resources/Art win if present.
    // This file holds the cache, the key switch and the shared pixel helpers; the painters
    // live beside it in ProceduralArt.Rooms / .Props / .Faces / .Documents (one partial class).
    public static partial class ProceduralArt
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        // Cache keys whose sprite wraps a texture IMPORTED from Resources (drop-in art /
        // the SecurityRoom kit). That texture is an asset on disk: Unity refuses Destroy on
        // it in the editor AND in builds ("Destroying assets is not permitted to avoid data
        // loss"), so ClearCache must free only the runtime Sprite wrapper for these keys and
        // never the borrowed texture. Every writer of Cache's file branch registers here.
        private static readonly HashSet<string> FileBacked = new HashSet<string>();
        public const float WorldPpu = 64f;   // world sprites: 64 px per unit
        public const float UiPpu = 100f;     // UI/document sprites
        // flat quad sprite (solid_white): 4 px texture at 4 px per unit => native 1x1 world
        // unit, so world placeholder quads can scale in plain units (see MakeSolid).
        public const float QuadPpu = 4f;

        // 9-slice borders in TEXTURE PIXELS for kit sprites that must stretch without dragging
        // their detail: the dialogue bubble keeps its left tail and its rough edge intact.
        // Measured on the TRIMMED master (2879x1031): the tail+left edge run to x=480, the
        // torn top/bottom/right edges to ~140. (x=left, y=bottom, z=right, w=top -
        // UnityEngine's Sprite.border order.)
        private static readonly Dictionary<string, Vector4> FileBorders =
            new Dictionary<string, Vector4> { { "sec_bubble", new Vector4(480f, 140f, 140f, 140f) } };

        // pixelsPerUnit overrides for kit masters authored at print resolution: sec_bubble's
        // 2879x1031 texture maps to ~288x103 reference units at PPU 10, so its 480px tail slice
        // lands as a real 48-unit border. At the default UI PPU the same slice would draw as
        // a 5-unit sliver and the bubble would lose its tail.
        private static readonly Dictionary<string, float> FilePpus =
            new Dictionary<string, float>
            {
                { "sec_bubble", 10f },
                // the guard's frames are 452 px tall and the old drawn guard stood 3.5 world units
                // high, so 129 px per unit keeps him the same size
                { "char_guard_idle", 129f }, { "char_guard_walk_a", 129f }, { "char_guard_walk_b", 129f },
                // backdrops: the three screen wide hallway covers 30 units, a room 17.8 (the camera's
                // 16:9 view), the bathroom 17.1
                { "bg_hallway_unlit", 108f },
                { "bg_classroom_1", 108f }, { "bg_classroom_2", 108f }, { "bg_comfort_room", 112f },
                // props drawn about 3.4 units tall (doors), a 1.5 unit wall clock, a half unit token
                { "prop_door_classroom_1", 308f }, { "prop_door_classroom_2", 299f },
                { "prop_door_comfort", 196f }, { "prop_door_security", 188f }, { "prop_server_door", 191f },
                { "prop_light_switch", 950f }, { "prop_clipboard", 918f },
                { "prop_clock", 524f }, { "token", 1154f },
            };

        /// <summary>True when a real file (or a cached sprite) exists for the key - callers that
        /// have their own painter use this to fall back instead of showing a broken image.</summary>
        // The art folders under Resources/Art, in the order they are searched. A key is the
        // file name without the extension and is unique across all folders (see
        // docs/ART-NAMING.md), so adding art never means renaming a key. A key with no file
        // falls back to the procedural painter.
        private static readonly string[] ArtFolders =
        {
            "Art/",
            "Art/Characters/",
            "Art/UI/SecurityDesk/",
            "Art/UI/MainMenu/",
            "Art/Environment/Hallway/",
            "Art/Environment/Rooms/",
            "Art/Environment/Props/",
            "Art/Environment/Inspect/",
        };

        private static Texture2D LoadFileTexture(string key)
        {
            foreach (var folder in ArtFolders)
            {
                var tex = Resources.Load<Texture2D>(folder + key);
                if (tex != null) return tex;
            }
            return null;
        }

        /// <summary>Every cached sprite key - the headless sprite export walks this.</summary>
        public static System.Collections.Generic.IEnumerable<string> CachedKeys => Cache.Keys;

        public static bool TryGet(string key, out Sprite sprite)
        {
            sprite = null;
            if (Cache.TryGetValue(key, out var cached) && cached != null) { sprite = cached; return true; }
            try
            {
                if (LoadFileTexture(key) != null) { sprite = Get(key); return true; }
            }
            catch (System.Exception) { /* no resources: caller falls back */ }
            return false;
        }

        public static Sprite Get(string key)
        {
            if (Cache.TryGetValue(key, out var s) && s != null) return s;
            try
            {
                var tex = LoadFileTexture(key);
                if (tex != null)
                {
                    var ppu = FilePpus.TryGetValue(key, out var fppu) ? fppu : UiPpu;
                    var spr = FileBorders.TryGetValue(key, out var border)
                        ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border, false)
                        : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f), key.EndsWith("_bg") ? WorldPpu : ppu);
                    // the procedural path names its sprites after the key; a dropped-in file must
                    // behave identically (name-based lookups and the headless proofs rely on it)
                    spr.name = key;
                    Cache[key] = spr;
                    FileBacked.Add(key);
                    return spr;
                }
            }
            catch (Exception) { /* no resources - fall through to procedural */ }

            var built = Build(key);
            if (built != null) Cache[built.name] = built;
            return built;
        }

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
                // SetPixels reads row 0 as the texture's BOTTOM; the canvas draws row 0 as top.
                var rows = new Color[Px.Length];
                for (int y = 0; y < H; y++)
                    System.Array.Copy(Px, y * W, rows, (H - 1 - y) * W, W);
                tex.SetPixels(rows);
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

        /// <summary>Every sprite key the procedural painter knows - the exact list an asset
        /// drop-in can replace (Resources/Art/&lt;key&gt;.png wins over the painter) and the list
        /// the asset manifest and the headless proofs cross-check against. Kept next to Build so
        /// a new case and a new name land in the same edit.</summary>
        public static readonly string[] AllKeys =
        {
            "solid_white", "vignette", "noise_overlay", "flash", "light_cone",
            "menu_bg", "desk_bg", "hood_figure",
            "hallway_bg", "hall_depth", "classroom1_bg", "classroom2_bg",
            "prop_door", "prop_door_security", "prop_server_door", "prop_lockers", "prop_desk",
            "prop_chalkboard", "prop_window", "prop_noticeboard", "prop_cabinet", "prop_clock",
            "token",
            "rough_panel", "rough_panel_dark", "rough_circle", "circle_dark", "circle_paper",
            "paper_flat", "school_facade", "camera_frame", "window_frame", "lamp_glow",
            "prop_photoframe", "prop_lightfixture", "prop_lostid",
            "custodian_0", "custodian_1",
            "anomaly_blink", "anomaly_slide", "anomaly_sprint", "anomaly_coverhug",
            "face_placeholder",
            "record_sheet", "clipboard", "note_paper", "stamp_allow", "stamp_reject",
            "folder_tab", "cursor_hand", "icon_check", "icon_x", "bubble_tail",
            "id_card_frame",
            "map_paper", "map_room", "map_sealed", "map_door",
        };

        /// <summary>The Main Menu art kit (Assets/_Project/Resources/Art/UI/MainMenu/&lt;key&gt;.png,
        /// cut from Art/MainMenu by Tools/prep_mainmenu_art.py). Kit keys live OUTSIDE AllKeys -
        /// they have no procedural painter, exactly like the SecurityRoom kit - but they resolve
        /// through the same Get()/LoadFileTexture() path and the same manifest rows. FlowCheck
        /// proves the folder ships whole (`== Main Menu art kit ==`).</summary>
        public static readonly string[] MainMenuKitKeys =
        {
            "menu_background", "menu_logo", "menu_frame_wide", "menu_frame_square",
            "menu_icon_newgame", "menu_icon_continue", "menu_icon_settings",
            "menu_icon_exit", "menu_icon_credits",
        };

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
                case "desk_bg": return MakeDeskBg(640, 360);
                case "hood_figure": return MakeHoodFigure(24, 48);
                case "hallway_bg": return MakeHallway(1536, 640);
                case "hall_depth": return MakeHallDepth();
                case "classroom1_bg": return MakeClassroom(1152, 640, false);
                case "classroom2_bg": return MakeClassroom(1152, 640, true);
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
                case "token": return MakeClock(28, "token");   // pocket-watch token left by a revealed anomaly
                case "rough_panel": return MakeRoughPanel(256, 192, 0.88f, "rough_panel");
                case "rough_panel_dark": return MakeRoughPanel(256, 192, 0.16f, "rough_panel_dark");
                case "rough_circle": return MakeRoughCircle(128);
                case "circle_dark": return MakeFlatCircle("circle_dark", new Color(0.098f, 0.098f, 0.110f));
                case "circle_paper": return MakeFlatCircle("circle_paper", new Color(0.839f, 0.831f, 0.800f));
                case "paper_flat": return MakePaperFlat();
                case "school_facade": return MakeSchoolFacade(440, 296);
                case "camera_frame": return MakeCameraFrame(252, 150);
                case "window_frame": return MakeWindowFrame(470, 316);
                case "lamp_glow": return MakeLampGlow(128);
                case "prop_photoframe": return MakePhotoFrame(144, 108);
                case "prop_lightfixture": return MakeLightFixture(208, 40);
                case "prop_lostid": return MakeLostId(56, 36);
                case "custodian_0": return MakeCustodian(96, 224, 0);
                case "custodian_1": return MakeCustodian(96, 224, 1);
                case "anomaly_blink": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Blink);
                case "anomaly_slide": return MakeAnomaly(128, 336, AnomalyBehaviorKind.Slide);
                case "anomaly_sprint": return MakeAnomaly(160, 336, AnomalyBehaviorKind.Sprint);
                case "anomaly_coverhug": return MakeAnomaly(192, 224, AnomalyBehaviorKind.CoverHug);
                case "face_placeholder": return MakeFacePlaceholder(168, 208);
                case "id_card_frame": return MakeIdCard();
                case "record_sheet": return MakeRecordSheet();
                case "clipboard": return MakeClipboard(370, 490);
                case "note_paper": return MakeNotePaper(390, 300);
                case "stamp_allow": return MakeStamp(false);
                case "stamp_reject": return MakeStamp(true);
                case "folder_tab": return MakeFolderTab(304, 26);
                case "cursor_hand": return MakeCursorHand(24);
                case "icon_check": return MakeIcon(true);
                case "icon_x": return MakeIcon(false);
                case "bubble_tail": return MakeBubbleTail();
                // the hand-drawn ground-floor plan (sheet, rough room outlines, seal, doorways)
                case "map_paper": return MakeMapPaper(940, 440);
                case "map_room": return MakeMapRoom(160, 120);
                case "map_sealed": return MakeMapSealed(160, 120);
                case "map_door": return MakeMapDoor(24, 20);
                default:
                    Debug.LogWarning("[ProceduralArt] unknown sprite key '" + key + "' - returning solid.");
                    return MakeSolid(G(1f, 0f));
            }
        }

        private static void DrawRing(Canvas2D c, float cx, float cy, float rx, float ry, Color col)
        {
            for (float a = 0; a < Mathf.PI * 2; a += 0.06f)
                c.FillRect((int)(cx + Mathf.Cos(a) * rx), (int)(cy + Mathf.Sin(a) * ry), 2, 2, col);
        }

        private static Sprite MakeSolid(Color c)
        {
            var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = c;
            t.SetPixels(px); t.Apply(false, false); t.name = "solid";
            // The one flat quad sprite every world placeholder is built from: its native size
            // is exactly 1x1 WORLD UNIT (4 px texture at 4 px per unit), so a placeholder's
            // localScale IS its size in units (a 1.7x3.4 door quad scales 1.7x3.4). Drawing it
            // at UiPpu shrank every world quad to 1/25th: the darkness overlay became a small
            // floating box and doors/switches/wall bands vanished into dots.
            // uGUI Images ignore pixelsPerUnit (they stretch to their RectTransform), so the
            // UI uses of this sprite are unaffected.
            var s = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), QuadPpu);
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

        private static Sprite MakeRoughPanel(int w, int h, float paper, string name = "rough_panel")
        {
            var c = new Canvas2D(w, h);
            var r = new System.Random(77);
            c.FillRect(0, 0, w, h, G(paper));
            RoughLine(c, r, 2, 2, w - 3, 3, 0.07f, 3);
            RoughLine(c, r, w - 3, 2, w - 3, h - 3, 0.07f, 3);
            RoughLine(c, r, w - 3, h - 3, 2, h - 3, 0.07f, 3);
            RoughLine(c, r, 2, h - 3, 2, 2, 0.07f, 3);
            RoughLine(c, r, 6, 7, w - 7, 7, 0.18f, 1);
            RoughLine(c, r, 7, 6, 7, h - 7, 0.18f, 1);
            return c.ToSprite(name, UiPpu);
        }

        private static Sprite MakeRoughCircle(int d)
        {
            var c = new Canvas2D(d, d);
            var r = new System.Random(91);
            float rad = d / 2f - 4f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - d / 2f) * (x - d / 2f) + (y - d / 2f) * (y - d / 2f));
                    if (dist <= rad) c.Px[y * d + x] = G(0.88f);
                    if (dist <= rad && dist > rad - 3f + r.Next(-1, 2)) c.Px[y * d + x] = G(0.08f);
                }
            return c.ToSprite("rough_circle", UiPpu);
        }

        private static Sprite MakeFlatCircle(string name, Color fill)
        {
            const int d = 110;
            var c = new Canvas2D(d, d);
            float half = d / 2f, rad = half - 1f;
            for (int y = 0; y < d; y++)
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half));
                    if (dist <= rad) c.Px[y * d + x] = dist > rad - 2f ? WireInk : fill;
                }
            return c.ToSprite(name, UiPpu);
        }

        // Plain flat paper for the rules-note and tonight's-log overlays.
        private static Sprite MakePaperFlat()
        {
            var c = new Canvas2D(760, 480);
            c.Fill(WirePaper);
            Border2(c, WireInk);
            return c.ToSprite("paper_flat", UiPpu);
        }

        /// <summary>Stable string hash for art seeds: string.GetHashCode is randomized per
        /// process, which would repaint noise differently every launch.</summary>
        public static int StableHash(string s)
        {
            int h = 17;
            if (s != null) for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
            return h;
        }

        public static void ClearCache()
        {
            foreach (var kv in Cache)
            {
                var spr = kv.Value;
                if (spr == null) continue;
                if (FileBacked.Contains(kv.Key))
                {
                    // kit/drop-in sprite: the texture belongs to the imported asset - releasing
                    // it is forbidden (and wrong: the next session reloads the same asset).
                    // Only the Sprite.Create wrapper is ours, so only that gets destroyed.
                    UnityEngine.Object.Destroy(spr);
                    continue;
                }
                // procedural sprite: texture and wrapper were both allocated here - free both.
                if (spr.texture != null) UnityEngine.Object.Destroy(spr.texture);
                UnityEngine.Object.Destroy(spr);
            }
            Cache.Clear();
            FileBacked.Clear();
        }
    }
}