using System.Collections;
using System.Collections.Generic;
using System.Text;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Left column papers: record sheet + ID card, filling and the clean-desk gate.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        // Floor-folder tints: idle manila under the lamp, the open folder lit, file rows
        // dark until pulled. All multiply over the drawn sprites/panels (uGUI tint).
        private static readonly Color FolderIdle = new Color(0.88f, 0.88f, 0.90f);
        private static readonly Color FolderOpen = new Color(1.18f, 1.15f, 1.05f);
        private static readonly Color FileRowIdle = new Color(0.20f, 0.19f, 0.16f);
        private static readonly Color FileRowSel = new Color(0.42f, 0.38f, 0.26f);

        private void BuildLeftColumn()
        {
            // Records station: the FLOOR FILES cabinet. Click a floor's folder tab to slide
            // the drawer open with that floor's student files; click a file to pull it into
            // the record window. The file never opens by itself and nothing is ever typed —
            // browsing IS the search.
            var station = UIFactory.Panel(_desk, "station", new Color(0.10f, 0.11f, 0.12f));
            Reg("station", station.rectTransform, L.Station);
            var cap = UIFactory.Label(station.rectTransform, "cap", "FLOOR FILES — STUDENT REGISTRY", 12,
                UIFactory.NsChalk, TextAnchor.MiddleCenter);
            cap.font = UIFactory.UiFontBold;
            L.Place(cap.rectTransform, L.Inner.StationCap);
            for (int floor = 1; floor <= _folderTabs.Length; floor++)
            {
                int f = floor;   // capture per tab
                var tab = FolderTab(station.rectTransform, "folder" + f, RecordSearch.FloorLabel(f),
                    () => ToggleFolder(f));
                L.Place(tab.GetComponent<RectTransform>(), new L.Region(L.Inner.FolderTabX,
                    L.Inner.FolderTabY0 + (f - 1) * L.Inner.FolderTabStep,
                    L.Inner.FolderTabW, L.Inner.FolderTabH));
                _folderTabs[f - 1] = tab;
                _folderTabInks[f - 1] = tab.targetGraphic as Image;
            }
            _stationOut = UIFactory.Label(station.rectTransform, "stationout", "", 10,
                UIFactory.NsMid, TextAnchor.UpperLeft);
            _stationOut.font = UIFactory.UiFont;
            L.Place(_stationOut.rectTransform, L.Inner.StationOut);

            // The drawer: a fixed grid rect under the station, hidden until a tab opens it.
            // Rows are rebuilt per folder (PopulateDrawer); the pulled record sheet lands
            // on top of it (papers layer is a later sibling).
            var drawer = UIFactory.BorderedPanel(_desk, "filedrawer", new Color(0.075f, 0.075f, 0.07f),
                UIFactory.Border, 2);
            Reg("filedrawer", drawer, L.FileDrawer);
            _drawerHead = UIFactory.Label(drawer, "drawerhead", "", 11,
                new Color(0.72f, 0.65f, 0.45f), TextAnchor.MiddleLeft);
            L.Place(_drawerHead.rectTransform, L.Inner.DrawerHead);
            _drawerGo = drawer.gameObject;
            _drawerGo.SetActive(false);

            // The student file: a draggable window, opened only by pulling it from the
            // floor-files drawer.
            var rec = UIFactory.Img(_desk, "record", ProceduralArt.Get("record_sheet"));
            rec.raycastTarget = true;   // the file window must grab the pointer to drag
            L.Place(rec.rectTransform, new L.Region(16, 168, 320, 290));
            rec.gameObject.AddComponent<RectMask2D>();   // rows can never spill off the sheet
            _recHeader = UIFactory.Label(rec.rectTransform, "header", "STUDENT RECORD — FORM 11-C", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_recHeader.rectTransform, L.Inner.RecordHeader);
            _recName = Row(rec.rectTransform, "rname", 0);
            _recNumber = Row(rec.rectTransform, "rnum", 1);
            _recYear = Row(rec.rectTransform, "ryear", 2);
            _recCourse = Row(rec.rectTransform, "rcourse", 3);
            _recStatus = Row(rec.rectTransform, "rstatus", 4);
            _recClassroom = Row(rec.rectTransform, "rroom", 5);
            _recEnrolled = Row(rec.rectTransform, "renrolled", 6);
            var closeBtn = GridButton(rec.rectTransform, "closefile", "X",
                () => rec.gameObject.SetActive(false), new L.Region(288, 2, 28, 20), 10);
            var xrt = closeBtn.GetComponent<RectTransform>();  // top-right corner, any size
            xrt.anchorMin = new Vector2(1, 1);
            xrt.anchorMax = new Vector2(1, 1);
            xrt.pivot = new Vector2(1, 1);
            xrt.anchoredPosition = new Vector2(-4, -4);
            xrt.sizeDelta = new Vector2(28, 20);
            // draggable inside the left/centre zone only: the file can never cover the stamps
            var fileBounds = UIFactory.NewUI(_desk, "filebounds");
            UIFactory.PlaceTL(fileBounds, 0, 0, 960, 720);
            var fileDrag = rec.gameObject.AddComponent<DragPaper>();
            fileDrag.Init(fileBounds, -0.6f);
            fileDrag.SetHome(new Vector2(16f, 168f));   // left column under the cabinet
            rec.gameObject.SetActive(false);   // opens only by pulling a file at the station

            var idCard = UIFactory.Img(_desk, "idcard", ProceduralArt.Get("id_card_frame"));
            idCard.raycastTarget = true;   // draggable paper: the frame takes pointer events
            // slightly smaller default card (owner feedback): the whole card — sprite,
            // photo, rows — scales down uniformly through DragPaper.BaseScale, so the
            // proportions and the inner grid stay exactly as designed and the Z zoom
            // still reads 1x..1.8x relative to THIS resting size
            idCard.rectTransform.localScale = new Vector3(IdCardScale, IdCardScale, 1f);
            Reg("idcard", idCard.rectTransform, L.IdCard);
            idCard.gameObject.AddComponent<RectMask2D>();

            // the verdict mark: born riding the ID (invisible), re-parented onto the slip
            // when the stamp lands — it snaps into the slip's printed stamp box and
            // respects Reduced Motion (no punch, instant); it never blocks clicks
            _stampOverlay = UIFactory.Img(idCard.rectTransform, "stampoverlay", null, new Color(1, 1, 1, 0f));
            UIFactory.AnchorsFull(_stampOverlay.rectTransform);
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            // the back face: seal, issue line and the UV strip — the subtle discrepancy tier.
            // Flip with F (or a tap on the held card); the front hides while it shows.
            var back = UIFactory.Panel(idCard.rectTransform, "idback", new Color(0.82f, 0.81f, 0.78f));
            UIFactory.AnchorsFull(back.rectTransform);
            var seal = UIFactory.Img(back.rectTransform, "seal", ProceduralArt.Get("circle_paper"),
                new Color(0.35f, 0.42f, 0.38f, 0.9f));
            L.Place(seal.rectTransform, new L.Region(200, 60, 90, 90));
            var issue = UIFactory.Label(back.rectTransform, "issue",
                "ISSUED: HB REGISTRY · SEAL DISTRICT 3", 10, UIFactory.PaperInk, TextAnchor.MiddleLeft);
            L.Place(issue.rectTransform, new L.Region(20, 160, 280, 16));
            var uv = UIFactory.Panel(back.rectTransform, "uvstrip", new Color(0.55f, 0.55f, 0.75f, 0.35f));
            L.Place(uv.rectTransform, new L.Region(20, 190, 280, 14));
            back.gameObject.SetActive(false);
            // the card's black spine: left edge on the front, mirrored to the right on the
            // back — and the ONLY click target that flips the card (owner feedback: the
            // whole-card flip fired from anywhere on the papers). Both spines carry the
            // hand-cursor hover so the handle reads as clickable.
            var stripe = UIFactory.Panel(idCard.rectTransform, "idstripe", new Color(0.06f, 0.06f, 0.07f));
            stripe.rectTransform.anchorMin = new Vector2(0, 0);
            stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.offsetMin = new Vector2(0, 0);
            stripe.rectTransform.offsetMax = new Vector2(14, 0);
            SpineHandle(stripe, FlipId);
            var bstripe = UIFactory.Panel(back.rectTransform, "idstripeback", new Color(0.06f, 0.06f, 0.07f));
            bstripe.rectTransform.anchorMin = new Vector2(1, 0);
            bstripe.rectTransform.anchorMax = new Vector2(1, 1);
            bstripe.rectTransform.offsetMin = new Vector2(-14, 0);
            bstripe.rectTransform.offsetMax = new Vector2(0, 0);
            SpineHandle(bstripe, FlipId);
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_idHeader.rectTransform, L.Inner.IdHeader);
            _idPhoto = UIFactory.Img(idCard.rectTransform, "idphoto",
                ProceduralArt.Get("face_placeholder"));   // unique name; placeholder until the face paints
            L.Place(_idPhoto.rectTransform, L.Inner.IdPhoto);
            _idPhoto.preserveAspect = true;
            _idName = IdRow(idCard.rectTransform, "name", 0);
            _idNumber = IdRow(idCard.rectTransform, "num", 1);
            _idYear = IdRow(idCard.rectTransform, "year", 2);
            _idCourse = IdRow(idCard.rectTransform, "course", 3);
            _idStatus = IdRow(idCard.rectTransform, "status", 4);
            _idExpires = IdRow(idCard.rectTransform, "exp", 5);

            _recordGo = rec.gameObject;
            _idCardGo = idCard.gameObject;
            // clean desk: the papers stay hidden until the ID is actually in your hand
            _recordGo.SetActive(false);
            _idCardGo.SetActive(false);

            // papers are physical: draggable over the desk, lifted while held, settling
            // back with their tilt on drop. Dropping the ID at the window hands it back —
            // the scanner is retired, verification is manual (documents, floor files,
            // questions).
            var recordDrag = _recordGo.AddComponent<DragPaper>();
            recordDrag.Init(_desk, -0.6f);
            var idDrag = _idCardGo.AddComponent<DragPaper>();
            idDrag.BaseScale = IdCardScale;   // set before Init: the lift/drop maths captures it
            idDrag.Init(_desk, 0.8f);
            // the card body is deliberately NOT a button: it drags, it does not flip.
            // The flip belongs to the black spine alone (see SpineHandle above).
            idDrag.OnLift = () => _lastPaper = _idCardGo;
            idDrag.OnDropped = center =>
            {
                if (InWindowZone(center)) HandBackId();   // hand the ID back to the student
            };
        }

        private Text Row(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 11, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.RecordRowX,
                L.Inner.RecordRules[rowIndex] - L.Inner.RecordRowH, L.Inner.RecordRowW, L.Inner.RecordRowH));
            return t;
        }

        private Text IdRow(RectTransform parent, string name, int rowIndex)
        {
            var t = UIFactory.Label(parent, name, "", 12, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.IdRowX,
                L.Inner.IdRules[rowIndex] - L.Inner.IdRowH, L.Inner.IdRowW, L.Inner.IdRowH));
            return t;
        }

        // The ID's black spine as a flip handle: raycast on (the only sliver of the card
        // that takes clicks), a subtle brighten on hover/press — multiplied UP because the
        // spine is near-black — and the hand cursor while the pointer is over it.
        private static Button SpineHandle(Image spine, System.Action onFlip)
        {
            spine.raycastTarget = true;
            var b = spine.gameObject.AddComponent<Button>();
            b.targetGraphic = spine;
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(2.2f, 2.2f, 2.35f);
            cb.pressedColor = new Color(3.2f, 3.2f, 3.4f);
            cb.fadeDuration = 0.10f;
            b.colors = cb;
            if (onFlip != null) b.onClick.AddListener(() => onFlip());
            spine.gameObject.AddComponent<HoverCursor>();
            return b;
        }

        // The card the visitor holds up. The Night-5 administrator carries a district
        // badge, so every line has an admin wording.
        private void FillIdCard(VisitorData v, bool admin)
        {
            _idHeader.text = admin ? "DISTRICT STAFF BADGE" : "HOLLOWBROOK COLLEGE — STUDENT ID";
            _idName.text = "NAME: " + v.ClaimedName;
            _idNumber.text = (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            if (admin)
            {
                _idYear.text = "DIVISION: Facilities Oversight";
                _idCourse.text = "AUTHORITY: District Office";
                _idStatus.text = "CLEARANCE: Full building";
            }
            else
            {
                _idYear.text = "YEAR: " + Content.YearNames[v.IdYearLevel - 1];
                _idCourse.text = "COURSE: " + Content.Courses[v.IdCourseIndex];
                _idStatus.text = "STATUS: " + (v.IdIsTransferee ? "TRANSFEREE" : "REGULAR");
            }
            _idExpires.text = admin ? "VALID THRU: " + v.ExpirationDisplay : "EXPIRES: " + v.ExpirationDisplay;
        }

        // The school's own record sheet — the paper the card gets compared against.
        private void FillRecordSheet(VisitorData v, bool admin)
        {
            _recHeader.text = admin ? "DISTRICT ROSTER MEMO" : "STUDENT RECORD — FORM 11-C";
            if (admin)
            {
                _recName.text = "ASSIGNED: " + v.RecordName;
                _recNumber.text = "BADGE NO: " + v.RecordIdNumber;
                _recYear.text = "EFFECTIVE: " + Content.ShiftDates[Mathf.Clamp(_gameRoot.Session.CurrentShift - 1, 0, 4)];
                _recCourse.text = "PURPOSE: Assume control of site";
                _recStatus.text = "SIGNED: District Seal (smudged)";
                _recClassroom.text = "ESCORT: None authorized";
                _recEnrolled.text = "FILED: 1987-10-05";
            }
            else
            {
                _recName.text = "NAME: " + v.RecordName;
                _recNumber.text = "ID NO: " + v.RecordIdNumber;
                _recYear.text = "YEAR LEVEL: " + Content.YearNames[v.RecordYearLevel - 1];
                _recCourse.text = "COURSE/PROGRAM: " + Content.Courses[v.RecordCourseIndex];
                _recStatus.text = "STATUS: " + (v.RecordIsTransferee ? "TRANSFEREE" : "REGULAR");
                _recClassroom.text = "FIRST CLASS: " + v.RecordFirstClassroom;
                _recEnrolled.text = "EXPIRES: " + v.ExpirationDisplay;
            }
        }

        // Clean-desk visibility: the three paper groups appear only once the ID is in hand.
        private void SetDocumentsVisible(bool on)
        {
            // only the ID is handed over; the registry file opens when the PLAYER opens it
            if (_idCardGo != null) _idCardGo.SetActive(on);
        }

        // Papers render above the window and below the stamps/overlays: one layer.
        private void BuildPapersLayer()
        {
            var layer = UIFactory.NewUI(_desk, "PapersLayer");
            UIFactory.AnchorsFull(layer);          // layer rect == desk rect, coords preserved
            _papersLayer = layer.gameObject;
            if (_recordGo != null) _recordGo.transform.SetParent(layer, false);
            if (_idCardGo != null) _idCardGo.transform.SetParent(layer, false);
            BuildSlip();      // the slip is paper too: it lives in the same layer
        }

        // The registry file, filled from a canon row (the ground-truth student record).
        private void FillRecordSheet(StudentIdentity v)
        {
            _recHeader.text = "STUDENT RECORD — FORM 11-C";
            _recName.text = "NAME: " + v.FullName;
            _recNumber.text = "ID NO: " + v.IdNumber;
            _recYear.text = "YEAR: " + v.YearString;
            _recCourse.text = "COURSE: " + v.CourseString;
            _recStatus.text = "STATUS: " + v.StatusString;
            _recClassroom.text = "FIRST CLASS: " + v.FirstClassroom.ToString();
            _recEnrolled.text = "REGISTRY: ON FILE";
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
            txt.rectTransform.offsetMin = new Vector2(10, 0);   // on the body, clear of the
            txt.rectTransform.offsetMax = new Vector2(-6, -6);  // poking-up tab strip
            return btn;
        }

        // Folder tabs toggle their drawer; a second click on the open folder slides it shut.
        private void ToggleFolder(int floor)
        {
            if (_openFloor == floor) CloseFileDrawer();
            else OpenFolder(floor);
        }

        // Slides the drawer open on one floor's files. Folders are computed from the live
        // canon roster, so the drawer always lists exactly what the registry holds.
        internal void OpenFolder(int floor)
        {
            _openFloor = floor;
            var canon = _gameRoot != null && _gameRoot.Roster != null ? _gameRoot.Roster.Canon : null;
            RecordSearch.FloorFolder folder = null;
            if (canon != null)
                foreach (var f in RecordSearch.BuildFloorFolders(canon))
                    if (f.Floor == floor) { folder = f; break; }
            PopulateDrawer(RecordSearch.FloorLabel(floor), canon,
                folder != null ? folder.Indices : new List<int>());
            RefreshFolderTabs();
            if (_drawerGo != null) _drawerGo.SetActive(true);
            if (_gameRoot != null) _gameRoot.Audio.PlaySfx("sfx_paper", 0.25f);
        }

        // Rebuilds the drawer's contents: header + one file row per student (name + registry
        // number). An empty floor still opens — with an explicit NO FILES line. Internal so
        // the headless FlowCheck can drive the empty-folder edge with a synthetic list.
        internal void PopulateDrawer(string label, IReadOnlyList<StudentIdentity> canon,
            IReadOnlyList<int> indices)
        {
            _fileRowInks.Clear();
            _fileRowCanon.Clear();
            if (_drawerRows != null) Destroy(_drawerRows.gameObject);
            if (_drawerGo == null) return;
            _drawerRows = UIFactory.NewUI(_drawerGo.transform, "drawerrows");
            UIFactory.AnchorsFull(_drawerRows);
            int count = indices != null ? indices.Count : 0;
            if (_drawerHead != null)
                _drawerHead.text = label + " — " + count + (count == 1 ? " FILE" : " FILES");
            if (count == 0)
            {
                var none = UIFactory.Label(_drawerRows, "nofiles", "NO FILES — NOTHING FLOOR-FILED HERE", 11,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                L.Place(none.rectTransform, new L.Region(L.Inner.FileRowX, L.Inner.FileRowY0,
                    L.Inner.FileRowW, L.Inner.FileRowH));
                return;
            }
            // rows fill the fixed drawer region; a taller roster than the drawer holds shows
            // what fits plus an explicit remainder line instead of spilling off the panel
            int capacity = Mathf.Max(1, Mathf.FloorToInt(
                (L.FileDrawer.H - L.Inner.FileRowY0 - 6f) / L.Inner.FileRowStep));
            int shown = Mathf.Min(count, capacity);
            for (int i = 0; i < shown; i++)
            {
                int canonIdx = indices[i];
                var s = canon != null && canonIdx >= 0 && canonIdx < canon.Count ? canon[canonIdx] : null;
                var rowRt = UIFactory.NewUI(_drawerRows, "filerow" + i);
                L.Place(rowRt, new L.Region(L.Inner.FileRowX,
                    L.Inner.FileRowY0 + i * L.Inner.FileRowStep, L.Inner.FileRowW, L.Inner.FileRowH));
                var bg = rowRt.gameObject.AddComponent<Image>();
                bg.color = canonIdx == _selectedCanon ? FileRowSel : FileRowIdle;
                var edge = UIFactory.Panel(rowRt, "edge", new Color(0.55f, 0.49f, 0.33f));
                edge.rectTransform.anchorMin = new Vector2(0, 0);   // manila left edge: a file
                edge.rectTransform.anchorMax = new Vector2(0, 1);   // folder peeking from the row
                edge.rectTransform.offsetMin = Vector2.zero;
                edge.rectTransform.offsetMax = new Vector2(5, 0);
                edge.raycastTarget = false;
                var nm = UIFactory.Label(rowRt, "fname", s != null ? s.FullName.ToUpperInvariant() : "—",
                    11, UIFactory.Ink, TextAnchor.MiddleLeft);
                UIFactory.PlaceTL(nm.rectTransform, 12, 0, 168, L.Inner.FileRowH);
                var idl = UIFactory.Label(rowRt, "fid", s != null ? s.IdNumber : "",
                    10, UIFactory.InkDim, TextAnchor.MiddleRight);
                UIFactory.PlaceTL(idl.rectTransform, 180, 0, 112, L.Inner.FileRowH);
                var btn = rowRt.gameObject.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.transition = Selectable.Transition.ColorTint;
                var cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.35f, 1.33f, 1.24f);   // subtle brighten on hover
                cb.pressedColor = new Color(1.60f, 1.56f, 1.44f);
                cb.fadeDuration = 0.10f;
                btn.colors = cb;
                int ci = canonIdx;
                btn.onClick.AddListener(() => OpenCanonFile(ci));
                _fileRowInks.Add(bg);
                _fileRowCanon.Add(canonIdx);
            }
            if (count > shown)
            {
                var more = UIFactory.Label(_drawerRows, "morefiles",
                    "+ " + (count - shown) + " MORE FILES — NOT SHOWN", 10,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                L.Place(more.rectTransform, new L.Region(L.Inner.FileRowX,
                    L.Inner.FileRowY0 + shown * L.Inner.FileRowStep, L.Inner.FileRowW, 16));
            }
        }

        // Slides the drawer shut and resets the station readout (fresh desk per visitor).
        private void CloseFileDrawer()
        {
            _openFloor = -1;
            if (_drawerGo != null) _drawerGo.SetActive(false);
            RefreshFolderTabs();
            if (_stationOut != null) _stationOut.text = "";
        }

        // The open folder's tab stays lit so the drawer is never orphaned-looking.
        private void RefreshFolderTabs()
        {
            for (int i = 0; i < _folderTabInks.Length; i++)
                if (_folderTabInks[i] != null)
                    _folderTabInks[i].color = _openFloor == i + 1 ? FolderOpen : FolderIdle;
        }

        // Pulls one canon file into the record window — the folder-browsing replacement for
        // the old typed search. Same rule as before: during the administrator's visit the
        // registry holds no badge, so no file replaces the memo already on the desk.
        internal void OpenCanonFile(int canonIdx)
        {
            if (_gameRoot == null || _gameRoot.Roster == null) return;
            var canon = _gameRoot.Roster.Canon;
            if (canonIdx < 0 || canonIdx >= canon.Count || canon[canonIdx] == null) return;
            if (_current != null && _current.IsAdministrator)
            {
                if (_stationOut != null) _stationOut.text = "NO RECORD — SEE ATTACHED MEMO";
                return;
            }
            var v = canon[canonIdx];
            _selectedCanon = canonIdx;
            FillRecordSheet(v);
            if (_recordGo != null)
            {
                _recordGo.SetActive(true);
                _recordGo.transform.SetAsLastSibling();   // the pulled file rises above the drawer
            }
            RefreshFileRows();
            if (_stationOut != null) _stationOut.text = "FOUND: " + v.FullName.ToUpperInvariant();
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
