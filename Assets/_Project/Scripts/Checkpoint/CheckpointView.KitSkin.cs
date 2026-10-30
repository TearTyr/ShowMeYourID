using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The owner's SecurityRoom art kit, applied over the audited desk grid.
    //
    // Rule of the pass: the kit is a SKIN, not a layout. Every interactive rect stays exactly
    // where CheckpointLayout puts it (the DeskLayoutAudit and the FlowCheck overlap matrix still
    // own the geometry); the kit's full-frame layers were exported at the same 16:9 desk space,
    // so stretching them across the frame lands their painted furniture on the audited rects
    // (measured: the sanity bar lands on SanityMeter, the pause block on Pause, the stamp
    // circles on the re-baselined tray). Cropped elements are stretched into the rect they were
    // drawn for. Anything the kit does not cover keeps its procedural skin, and every skin call
    // fails soft: a missing file leaves the old look instead of breaking the desk.
    public partial class CheckpointView
    {
        private Image _windowTop, _windowGlass, _windowBase;   // the shutter's three bands
        private Image _windowLayer;     // open/closed shutter over the booth window
        private Image _lightLayer;      // the hanging lamp cone (subtle flicker layer)
        private Image _bubbleTailInk;   // procedural tail, retired under the kit bubble's own tail
        private float _lightPhase;

        // One pass at the end of Init: the desk is fully built by then, so the skin is a flat
        // list of "find node, restyle it" steps — no build-order coupling, no duplicated references.
        private void ApplyKitSkin()
        {
            // ---- the booth: base, lamp, shutter, then the header boards ----
            var bg = FindImage(_root, "bg");
            if (bg != null) Fade(bg);                       // the procedural wash retires
            Image baseLayer = FullFrame(_desk, "sec_base", "sec_background", 0);
            _lightLayer = FullFrame(_desk, "sec_light", "sec_light", 1);
            // the shutter rides as THREE bands: hood and counter bases keep their painted
            // proportions at the concept's heights, only the glass band stretches (the kit's
            // glass is shorter than the concept booth)
            _windowTop = KitRect(_desk, "sec_window_top", "sec_window_close_top",
                new L.Region(0, -10, 1280, 207), 2);
            _windowGlass = KitRect(_desk, "sec_window_glass", "sec_window_close_glass",
                new L.Region(0, 185, 1280, 265), 2);   // full-frame width: the blinds art is the
                                                      // centred 36% of the export — stretching it
                                                      // into L.Glass squeezed it to a third
            _windowBase = KitRect(_desk, "sec_window_base", "sec_window_close_base",
                new L.Region(0, 447, 1280, 272), 2);
            _windowLayer = _windowGlass;
            // the board rides -31 so its painted furniture meets the concept header line;
            // its own white strips are painted out in prep (sec_upper_panel_clean)
            KitRect(_desk, "sec_upper_bg", "sec_upper_panel_bg", new L.Region(0, -31, 1280, 720), 3);
            KitRect(_desk, "sec_upper", "sec_upper_panel_clean", new L.Region(0, -31, 1280, 720), 4);
            // the sanity bar and pause block are sliced from the kit layer and dropped into the
            // board's own cut-outs (the re-baselined header grid), so the HUD's live meter and
            // the pause button land exactly on their painted furniture
            KitRect(_desk, "sec_sanity_bar", "sec_sanity_bar", L.SanityMeter, 5);
            KitRect(_desk, "sec_pause_block", "sec_pause_block", L.Pause, 6);
            if (baseLayer == null || _windowLayer == null) return;   // kit absent: old look stays
            _kitOn = true;   // labels that sit on kit paper flip to dark ink from here on

            // the procedural booth furniture retires under the kit bands: hood, sill, bases,
            // the white window frame and the glass fill would all double-paint over the art
            foreach (var n in new[] { "hood1", "hood2", "sill", "base1", "base2", "base3",
                                      "windowframe", "windowframe_border", "glass", "tint" })
                Fade(FindImage(_desk, n));

            // film grain + vignette ride ON TOP of the kit (they are the night-shift grade)
            var grain = _root.Find("grain");
            var vig = _root.Find("vignette");
            if (grain != null) { grain.SetParent(_desk, false); grain.SetAsLastSibling(); }
            if (vig != null) { vig.SetParent(_desk, false); vig.SetAsLastSibling(); }

            // ---- header: the kit's board replaces the flat bar; widgets keep their rects ----
            var topbar = FindImage(_desk, "topbar");
            if (topbar != null) Fade(topbar);
            Fade(FindImage(_desk, "daybanner_border"));
            Fade(FindImage(_desk, "daybanner"));
            if (_topLeft != null) _topLeft.color = UIFactory.NsChalk;   // white night line on the board
            var facade = FindImage(_desk, "facade");
            if (facade != null) Fade(facade);   // the kit glass paints the booth interior
            // best-effort immediate tint; under the normal bootstrap order the HUD does not
            // exist yet, and CheckpointView.RefreshHud lands it lazily on the first refresh.
            if (_gameRoot != null && _gameRoot.HUD != null) _gameRoot.HUD.ApplyKitSanity(true);
            SkinButton(FindButton(_desk, "pause"), null);   // kit pause block shows through
            var pauseLbl = ChildText(FindButton(_desk, "pause"));
            if (pauseLbl != null) pauseLbl.color = UIFactory.NsInk;

            // ---- questions: kit panel + pill buttons + the arrow as the asked-mark ----
            var qOuter = FindImage(_desk, "qpanel_border");
            if (qOuter != null) Stretch(qOuter, "sec_qpanel");
            var qInner = FindImage(_desk, "qpanel");
            if (qInner != null) Fade(qInner);
            for (int i = 0; i < 4; i++)
            {
                var b = FindButton(_desk, "q" + i);
                if (b == null) continue;
                SkinButton(b, "sec_btn_pill");
                var t = ButtonLabel(b);
                if (t != null)
                {
                    t.color = UIFactory.NsInk;
                    // concept rows read left-to-right: bullet arrow, then the question
                    t.alignment = TextAnchor.MiddleLeft;
                    var lrt = t.rectTransform;
                    // stretch first: the label is born on a point anchor, where offsetMin/Max
                    // would compute a negative sizeDelta and Unity would draw nothing
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = new Vector2(26f, 2f);
                    lrt.offsetMax = new Vector2(-8f, -2f);
                }
                var arrow = UIFactory.Img(b.transform, "askarrow", ProceduralArt.Get("sec_arrow_right"),
                    UIFactory.NsInk);
                arrow.raycastTarget = false;
                UIFactory.Anchor(arrow.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 12f),
                    new Vector2(10f, 0f));
                arrow.gameObject.SetActive(true);   // the concept paints the arrow as the row bullet
            }

            // ---- patrol / end shift: kit pill + the footprint and moon icons ----
            SkinButtonWithIcon(_patrolBtn, "sec_wide_pill", "sec_icon_patrol", UIFactory.NsInk);
            SkinButtonWithIcon(_endShiftBtn, "sec_wide_pill", "sec_icon_endshift", UIFactory.NsInk);

            // ---- records station: kit cabinet face, kit folder tabs, kit panel sheet ----
            if (_stationInk != null)
            {
                var cab = ProceduralArt.Get("sec_cabinet1");
                if (cab != null) { _stationInk.sprite = cab; _stationInk.color = Color.white; }
            }
            // The kit exports two DIFFERENT tab shapes (tab1 = knob + folder body,
            // tab2 = a bare edge strip): stretched into the one shared tab rect they read
            // as two misaligned folders. One folder shape for both rows, with the open/idle
            // tint carrying the state, keeps the cabinet face symmetric and centred.
            for (int i = 0; i < _folderTabInks.Length; i++)
            {
                var tab = ProceduralArt.Get("sec_folder_tab1");
                if (tab != null && _folderTabInks[i] != null) _folderTabInks[i].sprite = tab;
            }
            if (_recordsBody != null)
            {
                var bodyInk = _recordsBody.GetComponent<Image>();
                var cab2 = ProceduralArt.Get("sec_cabinet2");
                // the export carries a pulled-front bracket painted under the sheet; crop to
                // the sheet so the file rows sit on clean paper, not on a handle
                var sheet = DrawerSheetSprite(cab2);
                if (bodyInk != null && sheet != null) { bodyInk.sprite = sheet; bodyInk.color = Color.white; }
            }
            // the reference sheet reads dark ink on light paper: the head flips from the
            // procedural night ink to the kit's paper language
            var headT = FindTransform(_desk, "drawerhead");
            if (headT != null)
            {
                var ht = headT.GetComponent<Text>();
                if (ht != null) { ht.color = UIFactory.NsInk; ht.font = UIFactory.UiFontBold; }
            }

            // ---- verdict tray: kit circles under the stamps, kit caption bars, kit stamp art ----
            var allow = FindTransform(_desk, "stamp_allow");
            var deny = FindTransform(_desk, "stamp_deny");
            if (allow != null && deny != null)
            {
                var circDeny = TrayCircle("sec_tray_deny", "sec_stamp_circle_deny");
                var circAllow = TrayCircle("sec_tray_allow", "sec_stamp_circle_allow");
                if (circDeny != null) circDeny.rectTransform.SetSiblingIndex(deny.GetSiblingIndex());
                if (circAllow != null) circAllow.rectTransform.SetSiblingIndex(allow.GetSiblingIndex());
            }
            SkinStamp("stamp_allow", "sec_icon_accept");
            SkinStamp("stamp_deny", "sec_icon_decline");

            // ---- speech bubble: the kit bubble (tail included) as a 9-slice panel skin ----
            var bubbleOuter = FindImage(_desk, "bubble_border");
            if (bubbleOuter != null)
            {
                var kit = ProceduralArt.Get("sec_bubble");
                if (kit != null && kit.border.sqrMagnitude > 0f)
                {
                    bubbleOuter.sprite = kit;
                    bubbleOuter.type = Image.Type.Sliced;
                    bubbleOuter.color = Color.white;
                    // uGUI draws a sliced border as border / (spritePPU / canvasReferencePPU): with
                    // the master authored at PPU 10 on the default 100 canvas that is 10x too big,
                    // the borders swallow the panel and the white shrinks to a sliver far smaller
                    // than the text. Pin the multiplier so the borders are the 48u/14u the layout
                    // is built on, whatever the canvas reference is.
                    if (bubbleOuter.pixelsPerUnit > 0f)
                        bubbleOuter.pixelsPerUnitMultiplier =
                            L.Inner.BubbleKitArtPxPerUnit / bubbleOuter.pixelsPerUnit;
                }
            }
            var bubbleInner = FindImage(_desk, "bubble");
            if (bubbleInner != null) Fade(bubbleInner);
            _bubbleTailInk = FindImage(_desk, "bubbletail");
            if (_bubbleTailInk != null) Fade(_bubbleTailInk);   // the kit bubble carries its own tail
            if (_bubbleText != null)
            {
                // the kit tail lives inside the sliced left border: give the text room for it
                L.Place(_bubbleText.rectTransform, new L.Region(
                    L.Inner.BubbleKitTailW + L.Inner.BubbleKitInsetX, L.Inner.BubbleKitInsetY,
                    L.Inner.BubblePanelW + L.Inner.BubbleKitEdgeW - L.Inner.BubbleKitWhiteMargin
                        - 2f * L.Inner.BubbleKitInsetX, 60f));
            }
        }

        // The booth shutter follows the visitor: open glass while someone stands at the window,
        // closed blinds while the booth waits. Called from SetVisitorVisible.
        internal void RefreshWindowLayer(bool visitorShown)
        {
            if (_windowLayer == null) return;
            string state = visitorShown ? "open" : "close";
            Swap(_windowTop, "sec_window_" + state + "_top");
            Swap(_windowGlass, "sec_window_" + state + "_glass");
            Swap(_windowBase, "sec_window_" + state + "_base");
        }

        private static void Swap(Image img, string key)
        {
            if (img == null) return;
            var sp = ProceduralArt.Get(key);
            if (sp != null) img.sprite = sp;
        }

        // The hanging lamp breathes unless the player asked for calm (photosensitivity / reduce
        // motion): a slow sine on the cone layer's alpha, never a strobe, never below half.
        private void TickKitLight(float dt)
        {
            if (_lightLayer == null) return;
            bool calm = _gameRoot != null && _gameRoot.Settings != null &&
                        (_gameRoot.Settings.PhotosensitivitySafe || _gameRoot.Settings.ReduceMotion);
            if (calm) { SetLightAlpha(1f); return; }
            _lightPhase += dt * 1.7f;
            SetLightAlpha(0.86f + 0.14f * Mathf.Sin(_lightPhase));
        }

        private void SetLightAlpha(float a)
        {
            var c = _lightLayer.color; c.a = a; _lightLayer.color = c;
        }

        // The arrow is the row's BULLET in the concept art (every row carries one); the
        // asked state lives in the row label. Kept as a hook so the no-kit desk can differ.
        internal void RefreshQuestionArrow(int index, bool asked)
        {
            var b = FindButton(_desk, "q" + index);
            if (b == null) return;
            var arrow = b.transform.Find("askarrow");
            if (arrow != null) arrow.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- skin helpers
        private static Image FullFrame(Transform parent, string name, string key, int siblingIndex)
        {
            var sp = ProceduralArt.Get(key);
            if (sp == null) return null;
            var img = UIFactory.Img(parent, name, sp);
            UIFactory.AnchorsFull(img.rectTransform);
            img.raycastTarget = false;
            img.rectTransform.SetSiblingIndex(siblingIndex);
            return img;
        }

        private static Image KitRect(Transform parent, string name, string key, L.Region region, int siblingIndex)
        {
            var sp = ProceduralArt.Get(key);
            if (sp == null) return null;
            var img = UIFactory.Img(parent, name, sp);
            img.raycastTarget = false;
            L.Place(img.rectTransform, region);
            img.rectTransform.SetSiblingIndex(siblingIndex);
            return img;
        }

        private static void Fade(Image img)
        {
            if (img == null) return;
            var c = img.color; c.a = 0f; img.color = c;
        }

        private static void Stretch(Image img, string key)
        {
            var sp = ProceduralArt.Get(key);
            if (sp == null || img == null) return;
            img.sprite = sp;
            img.type = Image.Type.Simple;
            img.color = Color.white;
        }

        // The panel sheet export (sec_cabinet2) is the paper sheet PLUS a pulled-front
        // bracket painted under it (~the bottom 14% of the export). The stretched sheet must
        // stop at the paper: cropping the export keeps all seven file rows on clean sheet
        // instead of on a handle.
        private static Sprite _drawerSheet;
        private static Sprite DrawerSheetSprite(Sprite src)
        {
            if (_drawerSheet != null) return _drawerSheet;
            var tex = src != null ? src.texture : null;
            if (tex == null || tex.height < 16) return src;
            float sheetH = Mathf.Floor(tex.height * 0.855f);   // measured: bracket starts ~86% down
            _drawerSheet = Sprite.Create(tex,
                new Rect(0f, tex.height - sheetH, tex.width, sheetH),
                new Vector2(0.5f, 0.5f), src.pixelsPerUnit);
            _drawerSheet.name = src.name + "_sheet";
            return _drawerSheet;
        }

        // Button = border Image on the row + "bg" child + label. The kit pill replaces the bg,
        // the border fades to an invisible click surface (alpha 0 still raycasts).
        private static void SkinButton(Button b, string pillKey)
        {
            if (b == null) return;
            Fade(b.GetComponent<Image>());
            var bgT = b.transform.Find("bg");
            var bg = bgT != null ? bgT.GetComponent<Image>() : null;
            if (pillKey != null && bg != null) Stretch(bg, pillKey);
            else if (bg != null) Fade(bg);
        }

        private static void SkinButtonWithIcon(Button b, string pillKey, string iconKey, Color labelInk)
        {
            SkinButton(b, pillKey);
            if (b == null) return;
            var t = ChildText(b);
            if (t != null) t.color = labelInk;
            var sp = ProceduralArt.Get(iconKey);
            if (sp == null) return;
            var icon = UIFactory.Img(b.transform, "kiticon", sp, labelInk);
            icon.raycastTarget = false;
            UIFactory.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 20f),
                new Vector2(16f, 0f));
        }

        private static Text ChildText(Button b) => ButtonLabel(b);

        // The label by name, not by search: skinned buttons are often born inactive (the
        // question row waits for a visitor) and GetComponentInChildren skips hidden nodes —
        // so the fallback searches inactive children too.
        private static Text ButtonLabel(Button b)
        {
            if (b == null) return null;
            var lt = b.transform.Find("label");
            if (lt == null)
            {
                var bg = b.transform.Find("bg");
                lt = bg != null ? bg.Find("label") : null;
            }
            return lt != null ? lt.GetComponent<Text>() : b.GetComponentInChildren<Text>(true);
        }

        // The reference pills stay paper-white when disabled: only the word and the icon
        // dim. The generic CanvasGroup dim would grey out the kit pill art with them.
        internal void ApplyKitPill(Button b, bool on)
        {
            if (b == null) return;
            b.interactable = on;
            var cg = b.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            float k = on ? 1f : 0.45f;
            var t = ButtonLabel(b);
            if (t != null) t.color = new Color(UIFactory.NsInk.r * k, UIFactory.NsInk.g * k,
                                               UIFactory.NsInk.b * k, 1f);
            var icon = b.transform.Find("kiticon");
            if (icon != null)
            {
                var ii = icon.GetComponent<Image>();
                if (ii != null) ii.color = new Color(k, k, k, 1f);
            }
        }

        private Image TrayCircle(string name, string key)
        {
            var sp = ProceduralArt.Get(key);
            if (sp == null) return null;
            var img = UIFactory.Img(_desk, name, sp);
            img.raycastTarget = false;
            L.Place(img.rectTransform, key.EndsWith("allow") ? L.StampAllow : L.StampDeny);
            return img;
        }

        // The draggable stamp becomes the kit's hand stamp, tinted by its verdict, wearing the
        // check/cross disc as a badge; the printed mark on the slip is unchanged.
        private void SkinStamp(string stampName, string iconKey)
        {
            var t = FindTransform(_desk, stampName);
            if (t == null) return;
            var img = t.GetComponent<Image>();
            var stampArt = ProceduralArt.Get("sec_stamp");
            // RefreshStamps keeps driving the Image colour (verdict tint, dim until live), so the
            // kit handle inherits the established wake/dim language for free
            if (img != null && stampArt != null) img.sprite = stampArt;
            var lbl = t.Find("label");
            if (lbl != null)
            {
                var lt = lbl.GetComponent<Text>();
                if (lt != null) lt.color = UIFactory.NsInk;   // dark word on the white handle
            }
            var sp = ProceduralArt.Get(iconKey);
            if (sp == null) return;
            // badge on the HEAD knob (26px), not a 46px disc over the whole silhouette:
            // the check/cross stays crisp and the stamp art stays readable (owner #6)
            var icon = UIFactory.Img(t, "kiticon", sp);
            icon.raycastTarget = false;
            UIFactory.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f),
                new Vector2(0f, 40f));
        }

        private static Image FindImage(Transform root, string name)
        {
            var t = FindTransform(root, name);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Button FindButton(Transform root, string name)
        {
            var t = FindTransform(root, name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private static Transform FindTransform(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
            {
                var hit = FindTransform(t.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }
    }
}
