using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The FLOOR FILES cabinet: a station front with two folder tabs, and a real pull drawer
    // under it that comes OUT of the cabinet when the player asks for the files.
    //
    // Why this is its own area of the desk: the drawer has to be built BEFORE the papers so
    // the pulled record sheet lands on top of it (the papers layer is a later sibling), and
    // its motion is driven from CheckpointView.TickDrawer (unscaled time) so a click is
    // answered on the next frames — never after a wind-up, never by a coroutine that a
    // scene change could orphan.
    //
    // The physical read: the cabinet mouth is a RectMask2D slot at its approved grid rect;
    // the drawer body starts pushed up INSIDE the cabinet (fully clipped away) and slides
    // down/out on a fast ease-out, growing a touch in scale as it comes toward the player,
    // while a contact shadow deepens under the front lip and the files emerge from the body
    // a beat behind it (DrawerMotion owns every number, so the curve is unit-testable).
    // The drawer front — the worn panel with the handle — rides out a few pixels with it and
    // covers the mouth when shut, so a closed cabinet shows front and handle only.
    public partial class CheckpointView
    {
        // Floor-folder tints: idle manila under the lamp, the open folder lit, file rows
        // dark until pulled. All multiply over the drawn sprites/panels (uGUI tint).
        private static readonly Color FolderIdle = new Color(0.88f, 0.88f, 0.90f);
        private static readonly Color FolderOpen = new Color(1.18f, 1.15f, 1.05f);
        private static readonly Color FileRowIdle = new Color(0.20f, 0.19f, 0.16f);
        private static readonly Color FileRowSel = new Color(0.42f, 0.38f, 0.26f);
        // cabinet carcass / drawer front / recessed mouth
        private static readonly Color StationInk = new Color(0.10f, 0.11f, 0.12f);
        private static readonly Color DrawerFrontInk = new Color(0.145f, 0.140f, 0.128f);
        private static readonly Color DrawerBodyInk = new Color(0.085f, 0.082f, 0.075f);
        private static readonly Color CavityInk = new Color(0.020f, 0.020f, 0.022f);
        private static readonly Color HandleIdle = new Color(0.42f, 0.41f, 0.38f);
        private static readonly Color HandleLit = new Color(0.72f, 0.68f, 0.58f);
        // shut cabinet face (station-local): the front spans the carcass between the
        // folder-tab shelf and the bottom rail, leaving the caption and tabs their room
        private const float CabFrontX = 20f, CabFrontY = 44f, CabFrontW = 265f, CabFrontH = 102f;
        private static readonly Color CabFaceInk = new Color(0.80f, 0.78f, 0.72f);
        private static readonly Color CabFaceLit = new Color(0.90f, 0.88f, 0.82f);
        private static readonly Color CabGripInk = new Color(0.16f, 0.16f, 0.17f);
        private static readonly Color CabGripLit = new Color(0.32f, 0.32f, 0.34f);
        private static readonly Color CabTagInk = new Color(0.28f, 0.27f, 0.25f);

        private readonly Button[] _folderTabs = new Button[2];
        private readonly Image[] _folderTabInks = new Image[2];
        private Image _stationInk;          // the cabinet front (also the pull's click target)
        private Image _drawerHandle;        // brightens on hover so the pull reads as a handle
        // the shut cabinet's own drawer front: face, grip, tag, recess shadow — one group
        // that fades out as the real drawer pulls (the pulled body carries its own bracket)
        private CanvasGroup _cabFront;
        private Image _cabFace, _cabGrip;
        private Text _cabTag;
        private GameObject _drawerBox;      // the mouth slot: registered on the grid, never moves
        private RectTransform _drawerBody;  // the sliding drawer
        private RectTransform _drawerFront; // the worn front panel + handle (rides out a little)
        private Image _cavityShade;       // dark interior at the top of the body
        private Text _drawerHead;
        private RectTransform _drawerRows;
        private readonly List<Image> _fileRowInks = new List<Image>();
        private readonly List<int> _fileRowCanon = new List<int>();
        private readonly List<RectTransform> _fileRowRts = new List<RectTransform>();
        private readonly List<float> _fileRowBaseY = new List<float>();
        private readonly List<CanvasGroup> _fileRowGroups = new List<CanvasGroup>();

        private int _openFloor = -1;          // -1 = no folder selected
        private int _selectedCanon = -1;      // canon index of the pulled file
        private bool _recordFromFile;         // the sheet on the desk came out of the drawer
        private bool _drawerOpen;             // the requested state (one drawer, one target)
        private float _drawerPull;            // 0 = shut inside the cabinet, 1 = fully out
        private bool _drawerSteppedThisFrame; // the click already moved it: TickDrawer must not
        private bool _drawerVisible;          // the box is on screen (open, or still moving)

        /// <summary>True while the drawer is out (or on its way out) — the headless proofs
        /// read this instead of sampling a mid-animation rect.</summary>
        internal bool DrawerIsOpen => _drawerOpen;
        /// <summary>0..1 pull of the drawer body: 0 shut, 1 fully out.</summary>
        internal float DrawerPull => _drawerPull;
        /// <summary>The floor whose folder is currently selected (-1 = none).</summary>
        internal int DrawerFloor => _openFloor;

        // ---------------------------------------------------------------- build
        private void BuildFileCabinet()
        {
            // The cabinet front: caption, two floor-folder tabs, the pull handle and the
            // status readout. The whole front is the pull — clicking anywhere on the cabinet
            // (that is not a folder tab) opens or closes the drawer.
            var station = UIFactory.Panel(_desk, "station", StationInk);
            Reg("station", station.rectTransform, L.Station);
            _stationInk = station;
            var cap = UIFactory.Label(station.rectTransform, "cap", "FLOOR FILES — STUDENT REGISTRY", 12,
                UIFactory.NsChalk, TextAnchor.MiddleCenter);
            cap.font = UIFactory.UiFontBold;
            cap.raycastTarget = false;
            L.Place(cap.rectTransform, L.Inner.StationCap);
            for (int floor = 1; floor <= _folderTabs.Length; floor++)
            {
                int f = floor;   // capture per tab
                var tab = FolderTab(station.rectTransform, "folder" + f, RoomShort(f),
                    () => ToggleFolder(f));
                L.Place(tab.GetComponent<RectTransform>(), new L.Region(L.Inner.FolderTabX,
                    L.Inner.FolderTabY0 + (f - 1) * L.Inner.FolderTabStep,
                    L.Inner.FolderTabW, L.Inner.FolderTabH));
                _folderTabs[f - 1] = tab;
                _folderTabInks[f - 1] = tab.targetGraphic as Image;
            }
            // The shut cabinet face: a worn drawer front seated in a shadowed recess, with
            // a grip slot and a small OPEN tag — the closed station reads as a physical
            // drawer you pull, never as an empty frame. One CanvasGroup fades the whole
            // front out as the drawer body takes over (ApplyDrawerPull), so the shut face
            // and the pulled drawer can never paint at once.
            var frontRt = UIFactory.NewUI(station.rectTransform, "cabfront");
            UIFactory.PlaceTL(frontRt, CabFrontX, CabFrontY, CabFrontW, CabFrontH);
            _cabFront = frontRt.gameObject.AddComponent<CanvasGroup>();
            _cabFront.blocksRaycasts = false;   // the station's pull button owns every click
            // recess shadow above the front: the face sits IN the carcass, not on it
            var recess = UIFactory.Img(frontRt, "cabrecess", ProceduralArt.Get("solid_white"),
                new Color(0f, 0f, 0f, 0.55f));
            UIFactory.PlaceTL(recess.rectTransform, 2f, -5f, CabFrontW - 4f, 7f);
            recess.raycastTarget = false;
            // the worn front panel (kit paper language: light face, dark grip)
            _cabFace = UIFactory.Img(frontRt, "cabface", ProceduralArt.Get("rough_panel_dark"),
                CabFaceInk);
            UIFactory.AnchorsFull(_cabFace.rectTransform);
            _cabFace.raycastTarget = false;
            // grip slot, centred — the hand closes here
            _cabGrip = UIFactory.Panel(frontRt, "cabgrip", CabGripInk);
            UIFactory.PlaceTL(_cabGrip.rectTransform, (CabFrontW - 76f) * 0.5f, 45f, 76f, 11f);
            _cabGrip.raycastTarget = false;
            // small interaction tag beside the grip: the only instruction on the cabinet
            _cabTag = UIFactory.Label(frontRt, "cabtag", "OPEN ▾", 9, CabTagInk,
                TextAnchor.MiddleRight);
            _cabTag.font = UIFactory.UiFontBold;
            UIFactory.PlaceTL(_cabTag.rectTransform, CabFrontW - 92f, 44f, 82f, 13f);
            _cabTag.raycastTarget = false;
            // contact shadow under the front: depth against the carcass
            var shade = UIFactory.Img(frontRt, "cabshade", ProceduralArt.Get("solid_white"),
                new Color(0f, 0f, 0f, 0.40f));
            UIFactory.PlaceTL(shade.rectTransform, 4f, CabFrontH, CabFrontW - 8f, 5f);
            shade.raycastTarget = false;

            // the pull: one Button on the cabinet front (no colour tint of its own — the
            // handle brightens instead, so the front never flashes), plus the hand cursor and
            // a hover sense so the handle reads as "grip me" before the drawer ever moves.
            var pull = station.gameObject.AddComponent<Button>();
            pull.targetGraphic = _stationInk;
            pull.transition = Selectable.Transition.None;
            pull.onClick.AddListener(ToggleDrawer);
            station.gameObject.AddComponent<HoverCursor>();
            var stationHover = station.gameObject.AddComponent<HoverSense>();
            stationHover.OnEnter = () => SetHandleLit(true);
            stationHover.OnExit = () => SetHandleLit(false);

            // The mouth: the drawer's approved grid rect. It never moves (the layout audit
            // pins it), it clips whatever is pushed up inside the cabinet, and it stacks
            // cavity -> body -> front so the front always covers a shut drawer.
            var slot = UIFactory.BorderedPanel(_desk, "filedrawer", CavityInk, UIFactory.Border, 2);
            Reg("filedrawer", slot, L.FileDrawer);
            slot.gameObject.AddComponent<RectMask2D>();
            _drawerBox = slot.gameObject;

            // the pull depth follows the mouth: shut = fully behind the front lip
            DrawerMotion.TravelPx = L.FileDrawer.H - L.Inner.DrawerLipH;
            _drawerBody = UIFactory.NewUI(slot, "drawerbody");
            UIFactory.PlaceTL(_drawerBody, 0f, 0f, L.FileDrawer.W, L.FileDrawer.H);
            var bodyInk = _drawerBody.gameObject.AddComponent<Image>();
            bodyInk.sprite = ProceduralArt.Get("rough_panel_dark");
            bodyInk.color = DrawerBodyInk;
            bodyInk.raycastTarget = false;

            // the shaded cavity at the top of the body: deepens as the drawer comes out, so the
            // files read as sliding out of a dark interior rather than being pasted on a panel
            _cavityShade = UIFactory.Img(_drawerBody, "drawercavity", ProceduralArt.Get("solid_white"),
                new Color(0f, 0f, 0f, 0f));
            // top-anchored, centred, EXPLICIT size: with a stretched anchor sizeDelta becomes a
            // delta against the parent rather than a width, which would leave the shadow 0px wide
            UIFactory.PlaceTL(_cavityShade.rectTransform, 0f, 0f, L.FileDrawer.W, CavityBandH);
            _cavityShade.raycastTarget = false;

            _drawerHead = UIFactory.Label(_drawerBody, "drawerhead", "", 11,
                new Color(0.72f, 0.65f, 0.45f), TextAnchor.MiddleLeft);
            _drawerHead.font = UIFactory.UiFontBold;
            _drawerHead.raycastTarget = false;
            L.Place(_drawerHead.rectTransform, L.Inner.DrawerHead);

            // the worn front panel: built after the body so it covers the mouth when shut,
            // and it rides out a few pixels with the body (the handle you actually pull)
            _drawerFront = UIFactory.NewUI(slot, "drawerfront");
            var frontInk = _drawerFront.gameObject.AddComponent<Image>();
            frontInk.sprite = ProceduralArt.Get("rough_panel_dark");
            frontInk.color = DrawerFrontInk;
            frontInk.raycastTarget = false;   // clicks fall through to the cabinet's pull
            UIFactory.PlaceTL(_drawerFront, (L.FileDrawer.W - 140f) * 0.5f, 0f, 140f, L.Inner.DrawerFrontH);
            _drawerHandle = UIFactory.Panel(_drawerFront, "handle", HandleIdle);
            _drawerHandle.raycastTarget = false;
            L.Place(_drawerHandle.rectTransform, L.Inner.DrawerHandle);
            var grip = UIFactory.Label(_drawerFront, "drawerlabel", "FLOOR FILES", 10,
                UIFactory.NsMid, TextAnchor.MiddleCenter);
            grip.font = UIFactory.UiFont;
            grip.raycastTarget = false;
            L.Place(grip.rectTransform, L.Inner.DrawerFrontLabel);
            grip.gameObject.SetActive(false);   // the reference bracket is handle-only

            // No status line under the cabinet: pulling a file IS its own feedback (the
            // record sheet rises over the drawer with a paper sound), and the one refusal
            // the registry can give rides the HUD toast like every other desk refusal.

            _drawerOpen = false;
            _drawerPull = 0f;
            _drawerVisible = false;
            ApplyDrawerPull();      // born shut: body fully inside the cabinet, clipped away
            _drawerBox.SetActive(false);
            SetDrawerTabs(false);
            SetHandleLit(false);
        }

        // One manila folder tab: drawn sprite front, dark ink label on the body, a subtle
        // brighten on hover/press like every other desk control.
        private static Button FolderTab(Transform parent, string name, string label, System.Action onClick)
        {
            var rt = UIFactory.NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralArt.Get("folder_tab");
            img.color = FolderIdle;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.28f, 1.20f);   // subtle brighten on hover
            cb.pressedColor = new Color(1.50f, 1.46f, 1.34f);
            cb.fadeDuration = 0.12f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txt = UIFactory.Label(rt, "label", label, 11, UIFactory.PaperInk, TextAnchor.MiddleLeft);
            UIFactory.AnchorsFull(txt.rectTransform);
            txt.font = UIFactory.UiFont;
            txt.raycastTarget = false;
            txt.rectTransform.offsetMin = new Vector2(10, 0);   // the word sits on the tab
            txt.rectTransform.offsetMax = new Vector2(-140, -6); // knob; the body stays paper
            return btn;
        }

        // The handle brightens while the pointer is over the cabinet, and stays bright while
        // the drawer is out — the front and the handle are one object, so they read as one state.
        private bool _stationHovered;
        private void SetHandleLit(bool hovered)
        {
            _stationHovered = hovered;
            RefreshHandle();
        }

        private void RefreshHandle()
        {
            if (_drawerHandle == null) return;
            if (_kitOn)
            {   // the reference bracket reads a dark grip slot on the light front
                _drawerHandle.color = (_stationHovered || _drawerOpen)
                    ? new Color(0.32f, 0.32f, 0.34f) : new Color(0.16f, 0.16f, 0.17f);
                return;
            }
            _drawerHandle.color = (_stationHovered || _drawerOpen) ? HandleLit : HandleIdle;
        }

        // The floor tabs are only reachable with the drawer out: a shut cabinet shows its
        // caption, handle and readout, exactly like a real one.
        private void SetDrawerTabs(bool on)
        {
            for (int i = 0; i < _folderTabs.Length; i++)
                if (_folderTabs[i] != null) _folderTabs[i].gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- motion
        // One frame of drawer motion, on unscaled time from CheckpointView.Update (so pause
        // never leaves it half out) and from the headless harness. A single float drives the
        // whole thing: repeated clicks reverse the SAME animation from wherever it is, which
        // is what makes duplicate drawers / overlapping animations impossible by construction.
        internal void TickDrawer(float dt)
        {
            LastFrameDt = Mathf.Clamp(dt, 0f, 0.1f);
            if (_drawerBody == null) return;   // nothing built (or already torn down)
            if (!_drawerSteppedThisFrame) _drawerPull = DrawerMotion.Advance(_drawerPull, dt, _drawerOpen);
            _drawerSteppedThisFrame = false;
            ApplyDrawerPull();                 // idempotent: it also hides the box when shut
        }

        // Push the current pull value into the transforms. Called every animated frame AND
        // once at build, so the born state and the animated state can never disagree.
        private void ApplyDrawerPull()
        {
            float pull = _drawerPull;

            float outPx = DrawerMotion.BodyOffset(pull);
            float scale = DrawerMotion.BodyScale(pull);
            if (_drawerBody != null)
            {
                // Placed top-left inside the mouth, y DOWN. Shut, the whole body sits ABOVE the
                // mouth (negative y) — pushed up inside the cabinet, so nothing of it shows and
                // only the front panel is on screen. As it is pulled, outPx brings it down until
                // it is flush with the mouth: that downward travel IS the pull. The scale pivot is
                // the top-left corner, so the 3.5% "coming toward you" growth never slides it.
                // desk y of the body's top edge: -TravelPx while shut (the whole body is above
                // the mouth, inside the cabinet) rising to 0 when it is flush with the mouth
                UIFactory.PlaceTL(_drawerBody, 0f, outPx - DrawerMotion.TravelPx,
                    L.FileDrawer.W, L.Inner.DrawerBodyH);
                _drawerBody.localScale = new Vector3(scale, scale, 1f);
            }
            if (_drawerFront != null)
            {
                // the reference pulls the worn front BELOW the row sheet: the handle bracket
                // the player grips hangs under the pulled drawer, not over its mouth. It rides
                // out with the body (a fraction of the travel deeper each frame).
                float frontOut = outPx * L.Inner.DrawerFrontTravelRatio;
                UIFactory.PlaceTL(_drawerFront, (L.FileDrawer.W - 140f) * 0.5f,
                    outPx - DrawerMotion.TravelPx + L.Inner.DrawerBodyH + 10f -
                    frontOut * (1f - pull),
                    140f, L.Inner.DrawerFrontH);
                float fs = 1f + (scale - 1f) * 0.5f;
                _drawerFront.localScale = new Vector3(fs, fs, 1f);
            }
            if (_cavityShade != null)
            {
                var c = _cavityShade.color;
                c.a = DrawerMotion.CavityShadeAt(pull);
                _cavityShade.color = c;
            }

            // the shut cabinet face leaves as the drawer arrives: one float, two states,
            // never both painted at once
            if (_cabFront != null) _cabFront.alpha = 1f - pull;

            // visibility LAST: the transforms above are the parked state when shut, so a
            // hidden drawer is never left mid-travel and a shown one is never a frame behind
            bool shown = pull > 0f || _drawerOpen;
            if (_drawerBox != null && _drawerBox.activeSelf != shown) _drawerBox.SetActive(shown);
            _drawerVisible = shown;
            RefreshHandle();
            if (!shown) return;

            // the files emerge a beat behind the drawer, staggered row by row
            float reveal = DrawerMotion.RowReveal(pull);
            float rowOff = DrawerMotion.RowOffset(pull);
            for (int i = 0; i < _fileRowGroups.Count; i++)
            {
                // later rows trail by a growing fraction of the reveal, so the files fan out
                // of the drawer instead of blinking in as one block
                float lag = _fileRowGroups.Count <= 1 ? 0f
                    : StaggerShare * (i / (float)(_fileRowGroups.Count - 1));
                float r = Mathf.Clamp01(reveal - lag * (1f - reveal));
                if (_fileRowGroups[i] != null)
                {
                    _fileRowGroups[i].alpha = r;
                    // no clicking a file that has not arrived yet
                    _fileRowGroups[i].blocksRaycasts = r > 0.85f;
                }
                if (i < _fileRowRts.Count && _fileRowRts[i] != null && i < _fileRowBaseY.Count)
                {
                    var p = _fileRowRts[i].anchoredPosition;
                    p.y = _fileRowBaseY[i] - (1f - r) * rowOff;
                    _fileRowRts[i].anchoredPosition = p;
                }
            }
        }

        // How much of the row stagger the last row gets (0 = all rows together, 1 = the last
        // row only starts when the first has finished). Small: files fan out, never queue up.
        private const float StaggerShare = 0.35f;

        // Height of the shaded band at the top of the drawer body (its dark interior).
        private const float CavityBandH = 26f;

        /// <summary>The pull act: one drawer, one target state. Clicking the cabinet front,
        /// pressing TAB, or clicking a folder tab all come through here, so a held or repeated
        /// input can never build a second drawer or stack a second animation.</summary>
        internal void ToggleDrawer()
        {
            if (_drawerOpen) CloseDrawer();
            else OpenDrawer(_openFloor >= 1 ? _openFloor : 1);
        }

        // TAB at the desk: the same act as clicking the cabinet front.
        internal void ToggleDrawerByKey() => ToggleDrawer();

        /// <summary>Pull the drawer out on one floor's files. Folders are computed from the
        /// live canon roster, so the drawer always lists exactly what the registry holds.
        /// Asking for the floor that is already out pushes the drawer back in.</summary>
        internal void OpenDrawer(int floor)
        {
            if (floor < 1) floor = 1;
            if (_drawerOpen && _openFloor == floor) { CloseDrawer(); return; }
            // switching classrooms retires the sheet the previous classroom pulled: a record
            // on the desk always belongs to the folder it came out of, so the list and the
            // sheet can never disagree about which room you are browsing
            bool floorChanged = _openFloor != floor;
            _openFloor = floor;
            // only a sheet the drawer itself pulled retires with its folder — the admin's
            // District Roster Memo rides the same window and is never "stale"
            if (floorChanged && _recordFromFile && _recordGo != null && _recordGo.activeSelf)
            {
                _recordGo.SetActive(false);
                _selectedCanon = -1;
                _recordFromFile = false;
            }
            PopulateFloor(floor);
            _drawerOpen = true;
            SetDrawerTabs(true);
            RefreshFolderTabs();
            StepDrawerNow(true);
            if (_gameRoot != null) _gameRoot.Audio.PlaySfx("sfx_paper", 0.3f);
        }

        /// <summary>Push the drawer back in. The readout and the selected file survive (the
        /// pulled record sheet is still on the desk); a fresh visitor clears them.</summary>
        internal void CloseDrawer()
        {
            if (!_drawerOpen && _drawerPull <= 0f) { _drawerOpen = false; return; }
            _drawerOpen = false;
            SetDrawerTabs(false);
            StepDrawerNow(false);
            if (_gameRoot != null) _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.25f);
        }

        // The frame time the desk last ticked with (unscaled). Used only to give an input an
        // immediate first step; the animation itself is driven by the real per-frame dt.
        private float LastFrameDt { get; set; } = 1f / 60f;

        /// <summary>Move the drawer on the SAME frame the player asked for it, so the click and
        /// the first visible travel are one moment. It advances exactly one frame's worth and
        /// marks the frame as stepped, so TickDrawer continues from there instead of stepping
        /// twice — one drawer, one animation, one advance per frame, however fast the clicking.
        /// </summary>
        private void StepDrawerNow(bool opening)
        {
            _drawerPull = DrawerMotion.Advance(_drawerPull, LastFrameDt, opening);
            _drawerSteppedThisFrame = true;
            ApplyDrawerPull();
        }

        // Folder tabs toggle their drawer; a second click on the open folder pushes it shut.
        private void ToggleFolder(int floor) => OpenDrawer(floor);

        // Kept for the callers that speak in "folder" terms (and for the headless proofs):
        // open this floor's folder in the drawer.
        internal void OpenFolder(int floor) => OpenDrawer(floor);

        private void PopulateFloor(int floor)
        {
            var canon = _gameRoot != null && _gameRoot.Roster != null ? _gameRoot.Roster.Canon : null;
            RecordSearch.FloorFolder folder = null;
            if (canon != null)
                foreach (var f in RecordSearch.BuildFloorFolders(canon))
                    if (f.Floor == floor) { folder = f; break; }
            PopulateDrawer(RoomShort(floor), canon,
                folder != null ? folder.Indices : new List<int>());
        }

        /// <summary>Shut the drawer and clear the station for a fresh desk (new visitor, or
        /// leaving the desk): no folder selected, no readout, nothing highlighted.</summary>
        internal void CloseFileDrawer() => CloseFileDrawer(false);

        /// <param name="immediate">Skip the slide (used when the desk itself is going away, so
        /// nothing is left mid-travel to be found on the way back).</param>
        internal void CloseFileDrawer(bool immediate)
        {
            _openFloor = -1;
            _selectedCanon = -1;
            _recordFromFile = false;
            _drawerOpen = false;
            SetDrawerTabs(false);
            RefreshFolderTabs();
            if (immediate)
            {
                _drawerPull = 0f;
                ApplyDrawerPull();
            }
        }

        // Rebuilds the drawer's contents: header + one file row per student (name + registry
        // number). An empty floor still opens — with an explicit NO FILES line. Internal so
        // the headless FlowCheck can drive the empty-folder edge with a synthetic list.
        internal void PopulateDrawer(string label, IReadOnlyList<StudentIdentity> canon,
            IReadOnlyList<int> indices)
        {
            _fileRowInks.Clear();
            _fileRowCanon.Clear();
            _fileRowRts.Clear();
            _fileRowBaseY.Clear();
            _fileRowGroups.Clear();
            if (_drawerRows != null) Destroy(_drawerRows.gameObject);
            if (_drawerBody == null) return;
            _drawerRows = UIFactory.NewUI(_drawerBody, "drawerrows");
            UIFactory.AnchorsFull(_drawerRows);
            int count = indices != null ? indices.Count : 0;
            if (_drawerHead != null)
                _drawerHead.text = label + " — " + count + (count == 1 ? " FILE" : " FILES");
            if (count == 0)
            {
                var none = UIFactory.Label(_drawerRows, "nofiles", "NO FILES — NOTHING FLOOR-FILED HERE", 11,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                none.raycastTarget = false;
                L.Place(none.rectTransform, new L.Region(L.Inner.FileRowX, L.Inner.FileRowY0,
                    L.Inner.FileRowW, L.Inner.FileRowH));
                return;
            }
            // rows fill the drawer below its front lip and header; a taller roster than the
            // drawer holds shows what fits plus an explicit remainder line instead of
            // spilling out of the mouth
            int capacity = DrawerRowCapacity();
            int shown = Mathf.Min(count, capacity);
            for (int i = 0; i < shown; i++)
            {
                int canonIdx = indices[i];
                var s = canon != null && canonIdx >= 0 && canonIdx < canon.Count ? canon[canonIdx] : null;
                var rowRt = UIFactory.NewUI(_drawerRows, "filerow" + i);
                float rowY = L.Inner.FileRowY0 + i * L.Inner.FileRowStep;
                L.Place(rowRt, new L.Region(L.Inner.FileRowX, rowY, L.Inner.FileRowW, L.Inner.FileRowH));
                var pillRt = UIFactory.NewUI(rowRt, "pill");
                UIFactory.PlaceTL(pillRt, 0f, 0f, L.Inner.FileRowPillW, L.Inner.FileRowH);
                var bg = pillRt.gameObject.AddComponent<Image>();
                bg.color = canonIdx == _selectedCanon ? FileRowSel : FileRowIdle;
                // the pill IS the row's click and hover surface (the Button's targetGraphic):
                // with the raycast off here no pointer event ever reached the row in play —
                // the drawer listed files nobody could pull
                bg.raycastTarget = true;
                var edge = UIFactory.Panel(pillRt, "edge", new Color(0.55f, 0.49f, 0.33f));
                edge.rectTransform.anchorMin = new Vector2(0, 0);   // manila left edge: a file
                edge.rectTransform.anchorMax = new Vector2(0, 1);   // folder peeking from the row
                edge.rectTransform.offsetMin = Vector2.zero;
                edge.rectTransform.offsetMax = new Vector2(5, 0);
                edge.raycastTarget = false;
                var nm = UIFactory.Label(pillRt, "fname", s != null ? s.FullName.ToUpperInvariant() : "—",
                    10, UIFactory.Ink, TextAnchor.MiddleLeft);
                UIFactory.PlaceTL(nm.rectTransform, L.Inner.FileRowNameX, 0,
                    L.Inner.FileRowNameW, L.Inner.FileRowH);
                nm.font = UIFactory.UiFont;
                nm.raycastTarget = false;
                var idl = UIFactory.Label(rowRt, "fid", s != null ? s.IdNumber : "",
                    9, UIFactory.InkDim, TextAnchor.MiddleRight);
                UIFactory.PlaceTL(idl.rectTransform, L.Inner.FileRowIdX, 0,
                    L.Inner.FileRowIdW, L.Inner.FileRowH);
                idl.font = UIFactory.UiFont;
                // the registry number is part of the visible row, so it is part of the row's
                // hit area: clicking the id pulls the same file as clicking the name pill
                idl.raycastTarget = true;
                var btn = rowRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;   // the pill is the row's click and hover surface
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.35f, 1.33f, 1.24f);   // subtle brighten on hover
                cb.pressedColor = new Color(1.60f, 1.56f, 1.44f);
                cb.fadeDuration = 0.10f;
                btn.colors = cb;
                int ci = canonIdx;
                btn.onClick.AddListener(() => OpenCanonFile(ci));
                // one CanvasGroup per file: the whole row (ink, manila edge, both labels)
                // emerges together, and it cannot be clicked before it has arrived
                var cg = rowRt.gameObject.AddComponent<CanvasGroup>();
                cg.interactable = true;
                _fileRowInks.Add(bg);
                _fileRowCanon.Add(canonIdx);
                _fileRowRts.Add(rowRt);
                _fileRowBaseY.Add(-rowY);       // PlaceTL stores y as a negative offset
                _fileRowGroups.Add(cg);
            }
            if (count > shown)
            {
                var more = UIFactory.Label(_drawerRows, "morefiles",
                    "+ " + (count - shown) + " MORE FILES — NOT SHOWN", 10,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                more.raycastTarget = false;
                L.Place(more.rectTransform, new L.Region(L.Inner.FileRowX,
                    L.Inner.FileRowY0 + shown * L.Inner.FileRowStep, L.Inner.FileRowW, 16));
            }
            ApplyDrawerPull();      // a rebuilt folder adopts the drawer's current pull at once
        }

        /// <summary>How many file rows fit in the pulled drawer below its lip and header.</summary>
        internal static int DrawerRowCapacity() => Mathf.Max(1, Mathf.FloorToInt(
            (L.Inner.DrawerBodyH - L.Inner.FileRowY0 - L.Inner.FileRowH) / L.Inner.FileRowStep) + 1);

        // The reference tab and sheet head carry the ROOM number alone ("ROOM 202"); the
        // floor wording lives in the cabinet caption and the pulled sheet's registry ids.
        private static string RoomShort(int floor) => "ROOM " + (floor * 100 + floor);

        // The open folder's tab stays lit so the drawer is never orphaned-looking.
        private void RefreshFolderTabs()
        {
            for (int i = 0; i < _folderTabInks.Length; i++)
                if (_folderTabInks[i] != null)
                    _folderTabInks[i].color = _openFloor == i + 1 ? FolderOpen : FolderIdle;
        }

        // Pulls one canon file into the record window — the folder-browsing replacement for
        // the old typed search. The drawer STAYS OUT: browsing several files in a row is the
        // normal use, and a physical drawer does not slam shut because you took a sheet.
        // Same rule as before: during the administrator's visit the registry holds no badge,
        // so no file replaces the memo already on the desk.
        internal void OpenCanonFile(int canonIdx)
        {
            if (_gameRoot == null || _gameRoot.Roster == null) return;
            var canon = _gameRoot.Roster.Canon;
            if (canonIdx < 0 || canonIdx >= canon.Count || canon[canonIdx] == null) return;
            if (_current != null && _current.IsAdministrator)
            {
                // the registry holds no badge during the administrator's visit: the memo
                // already on the desk is the record, and the desk says so the way every
                // other refusal is said — a HUD toast, no permanent status text
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "NO RECORD — SEE ATTACHED MEMO"));
                return;
            }
            var v = canon[canonIdx];
            _selectedCanon = canonIdx;
            _recordFromFile = true;
            FillRecordSheet(v);
            if (_recordGo != null)
            {
                _recordGo.SetActive(true);
                _recordGo.transform.SetAsLastSibling();   // the pulled file rises above the drawer
            }
            RefreshFileRows();
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
        }

        // The selected file row keeps its lit manila tint while its folder is open.
        private void RefreshFileRows()
        {
            for (int i = 0; i < _fileRowInks.Count; i++)
                if (_fileRowInks[i] != null)
                    _fileRowInks[i].color = i < _fileRowCanon.Count && _fileRowCanon[i] == _selectedCanon
                        ? FileRowSel
                        : FileRowIdle;
        }
    }
}
