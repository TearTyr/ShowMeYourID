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
        private void BuildLeftColumn()
        {
            // Records station: tap to toggle the file window, type a query and FIND (or Enter)
            // to search the registry. The file never opens by itself.
            var station = UIFactory.Panel(_desk, "station", new Color(0.10f, 0.11f, 0.12f));
            Reg("station", station.rectTransform, L.Station);
            var cap = UIFactory.Label(station.rectTransform, "cap", "REGISTRY TERMINAL", 12,
                new Color(0.55f, 0.75f, 0.55f), TextAnchor.MiddleCenter);
            L.Place(cap.rectTransform, new L.Region(0, 8, 320, 20));
            _stationOut = UIFactory.Label(station.rectTransform, "stationout", "SEARCH: ID NUMBER OR NAME", 11,
                new Color(0.55f, 0.75f, 0.55f), TextAnchor.UpperLeft);
            L.Place(_stationOut.rectTransform, new L.Region(8, 40, 304, 60));
            var field = station.gameObject.AddComponent<UnityEngine.UI.InputField>();
            field.onEndEdit.AddListener(SearchRecords);
            _searchField = field;
            GridButton(station.rectTransform, "find", "FIND", () => SearchRecords(_searchField.text),
                new L.Region(110, 250, 100, 30), 12);
            var stBtn = station.gameObject.AddComponent<Button>();   // tap the station body
            stBtn.targetGraphic = station;
            stBtn.onClick.AddListener(ToggleFileWindow);

            // The student file: a draggable window, opened only by a registry search.
            var rec = UIFactory.Img(_desk, "record", ProceduralArt.Get("record_sheet"));
            L.Place(rec.rectTransform, new L.Region(350, 80, 320, 290));
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
            GridButton(rec.rectTransform, "closefile", "X", () => rec.gameObject.SetActive(false),
                new L.Region(288, 2, 28, 20), 10);
            // draggable inside the left/centre zone only: the file can never cover the stamps
            var fileBounds = UIFactory.NewUI(_desk, "filebounds");
            UIFactory.PlaceTL(fileBounds, 0, 0, 960, 720);
            var fileDrag = rec.gameObject.AddComponent<DragPaper>();
            fileDrag.Init(fileBounds, -0.6f);
            rec.gameObject.SetActive(false);   // opens only via the station search

            var idCard = UIFactory.Img(_desk, "idcard", ProceduralArt.Get("id_card_frame"));
            Reg("idcard", idCard.rectTransform, L.IdCard);
            idCard.gameObject.AddComponent<RectMask2D>();

            // the verdict mark lands ON the subject's ID: respects Reduced Motion
            // (no punch, instant) and never blocks clicks
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
            _idHeader = UIFactory.Label(idCard.rectTransform, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_idHeader.rectTransform, L.Inner.IdHeader);
            _idPhoto = UIFactory.Img(idCard.rectTransform, "photo", null);
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
            // back with their tilt on drop. Dropping the ID on the scanner scans it —
            // same rules path as the SCAN button, zero new rules.
            var recordDrag = _recordGo.AddComponent<DragPaper>();
            recordDrag.Init(_desk, -0.6f);
            var idDrag = _idCardGo.AddComponent<DragPaper>();
            idDrag.Init(_desk, 0.8f);
            idDrag.OnTapped = c => FlipId();          // tap the held card to flip it
            idDrag.OnLift = () => _lastPaper = _idCardGo;
            idDrag.OnDropped = center =>
            {
                if (center.x >= L.UtilityScan.X - 20 && center.x <= L.UtilityScan.Right + 20 &&
                    center.y >= L.UtilityScan.Y - 20 && center.y <= L.UtilityScan.Bottom + 20)
                    DoScan();
                else if (InWindowZone(center)) HandBackId();   // hand the ID back to the student
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

        // The registry answer: a matching canon student opens as the file window; a wrong or
        // missing match says NO RECORD — which is itself a clue (ghost IDs are not on file).
        internal void SearchRecords(string query)
        {
            if (_gameRoot == null || _gameRoot.Roster == null) return;
            int idx = RecordSearch.FindInCanon(_gameRoot.Roster.Canon, query);
            if (idx < 0)
            {
                if (_stationOut != null) _stationOut.text = "NO RECORD — " + (query ?? "").ToUpperInvariant();
                if (_recordGo != null) _recordGo.SetActive(false);
                _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.3f);
                return;
            }
            var v = _gameRoot.Roster.Canon[idx];
            FillRecordSheet(v);
            if (_recordGo != null) _recordGo.SetActive(true);
            if (_stationOut != null) _stationOut.text = "FOUND: " + v.FullName.ToUpperInvariant();
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
        }

        internal void ToggleFileWindow()
        {
            if (_recordGo != null) _recordGo.SetActive(!_recordGo.activeSelf);
        }
    }
}
