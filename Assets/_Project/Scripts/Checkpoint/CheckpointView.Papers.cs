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
    // (The FLOOR FILES cabinet and its pull drawer live in CheckpointView.FileDrawer.cs —
    // built first so the pulled record sheet always lands on top of the open drawer.)
    public partial class CheckpointView
    {
        private void BuildLeftColumn()
        {
            // The student file: a draggable window, opened only by pulling it from the
            // floor-files drawer.
            var recGo = UIFactory.NewUI(_desk, "record");
            var rec = recGo.gameObject.AddComponent<Hollowbrook.UI.HotRectImage>();
            rec.sprite = ProceduralArt.Get("record_sheet");
            rec.raycastTarget = true;   // the file window grabs the pointer — by its header
            L.Place(rec.rectTransform, new L.Region(16, 168, 320, 290));
            // Dragged by the header band only: an open file lies OVER the pulled drawer, so
            // the sheet's body stays click-through and the rows, tabs and cabinet under the
            // paper keep working while you read (the X button keeps its own raycast).
            rec.HotRectTopLeft = new Rect(0f, 0f, 320f, 30f);
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

            // The card is a CONTAINER at its concept-measured desk rect; the printed card
            // (frame, photo, rows — all laid out in the 320x220 design space) rides inside
            // as one uniformly scaled art layer, so the desk footprint matches the concept
            // art while the card's inner grid keeps its designed proportions. The container
            // owns the pointer (invisible raycast surface); the art is click-through.
            var idCard = UIFactory.Img(_desk, "idcard", null);
            idCard.color = new Color(0f, 0f, 0f, 0f);
            idCard.raycastTarget = true;   // draggable paper: the container takes pointers
            Reg("idcard", idCard.rectTransform, L.IdCard);
            idCard.rectTransform.localEulerAngles = new Vector3(0, 0, L.TiltCard);
            var art = UIFactory.Img(idCard.rectTransform, "cardart", ProceduralArt.Get("id_card_frame"));
            UIFactory.PlaceTL(art.rectTransform, 0, 0, 320, 220);
            art.rectTransform.localScale = new Vector3(IdCardScale, IdCardScale, 1f);
            art.raycastTarget = false;
            _idArt = art;
            var cardRt = art.rectTransform;

            // the verdict mark: born riding the ID (invisible), re-parented onto the slip
            // when the stamp lands — it snaps into the slip's printed stamp box and
            // respects Reduced Motion (no punch, instant); it never blocks clicks
            _stampOverlay = UIFactory.Img(cardRt, "stampoverlay", null, new Color(1, 1, 1, 0f));
            UIFactory.AnchorsFull(_stampOverlay.rectTransform);
            _stampOverlay.rectTransform.localEulerAngles = new Vector3(0, 0, -12f);
            _stampOverlay.raycastTarget = false;

            // the back face: seal, issue line and the UV strip — the subtle discrepancy tier.
            // Flip with F (or a tap on the held card); the front hides while it shows.
            var back = UIFactory.Panel(cardRt, "idback", new Color(0.82f, 0.81f, 0.78f));
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
            // back — and the ONLY click target that flips the card (the
            // whole-card flip fired from anywhere on the papers). Both spines carry the
            // hand-cursor hover so the handle reads as clickable.
            var stripe = UIFactory.Panel(cardRt, "idstripe", new Color(0.06f, 0.06f, 0.07f));
            stripe.rectTransform.anchorMin = new Vector2(0, 0);
            stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.offsetMin = new Vector2(0, 0);
            stripe.rectTransform.offsetMax = new Vector2(14, 0);
            SpineHandle(stripe, FlipId);
            _idStripe = stripe;
            var bstripe = UIFactory.Panel(back.rectTransform, "idstripeback", new Color(0.06f, 0.06f, 0.07f));
            bstripe.rectTransform.anchorMin = new Vector2(1, 0);
            bstripe.rectTransform.anchorMax = new Vector2(1, 1);
            bstripe.rectTransform.offsetMin = new Vector2(-14, 0);
            bstripe.rectTransform.offsetMax = new Vector2(0, 0);
            SpineHandle(bstripe, FlipId);
            _idHeader = UIFactory.Label(cardRt, "header", "HOLLOWBROOK COLLEGE — STUDENT ID", 12,
                new Color(0.9f, 0.9f, 0.88f), TextAnchor.MiddleCenter);
            L.Place(_idHeader.rectTransform, L.Inner.IdHeader);
            _idPhoto = UIFactory.Img(cardRt, "idphoto",
                ProceduralArt.Get("face_placeholder"));   // unique name; placeholder until the face paints
            L.Place(_idPhoto.rectTransform, L.Inner.IdPhoto);
            _idPhoto.preserveAspect = true;
            // click-through: the photograph is printed on the card, it is not a widget. The
            // card body (its frame Image) is the only drag target, and there is no
            // photo-matching interaction on this desk any more.
            _idPhoto.raycastTarget = false;
            _idName = IdRow(cardRt, "name", 0);
            _idNumber = IdRow(cardRt, "num", 1);
            _idYear = IdRow(cardRt, "year", 2);
            _idCourse = IdRow(cardRt, "course", 3);
            _idStatus = IdRow(cardRt, "status", 4);
            _idExpires = IdRow(cardRt, "exp", 5);

            _recordGo = rec.gameObject;
            _idCardGo = idCard.gameObject;
            // clean desk: the papers stay hidden until the ID is actually in your hand
            _recordGo.SetActive(false);
            _idCardGo.SetActive(false);

            // papers are physical: draggable over the desk, lifted while held, settling
            // back with their tilt on drop. Dropping the ID at the window hands it back —
            // there is no scanner, verification is manual (documents, floor files,
            // questions).
            var recordDrag = _recordGo.AddComponent<DragPaper>();
            recordDrag.Init(_desk, -0.6f);
            var idDrag = _idCardGo.AddComponent<DragPaper>();
                        idDrag.Init(_desk, 0.8f);
            // the card body is deliberately NOT a button: it drags, it does not flip.
            // The flip belongs to the black spine alone (see SpineHandle above).
            // a click on the card lifts it into the inspect view (the spine still flips it);
            // clicking anywhere off it puts it back, tilted, where it lay
            idDrag.Inspectable = true;
            idDrag.KeepVisible = _recordGo.transform;   // inspect the ID with the file still in view
            idDrag.OnTapped = c => idDrag.Inspect();
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
            bool template = !admin && ProceduralArt.Get("id_card_template") != null;
            LayoutIdCard(template);
            _idHeader.text = template ? "" : admin ? "DISTRICT STAFF BADGE" : "HOLLOWBROOK COLLEGE — STUDENT ID";
            // the printed template already carries the field names, so it holds the values only
            _idName.text = (template ? "" : "NAME: ") + v.ClaimedName;
            _idNumber.text = template ? v.IdNumber : (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            if (admin)
            {
                _idYear.text = "DIVISION: Facilities Oversight";
                _idCourse.text = "AUTHORITY: District Office";
                _idStatus.text = "CLEARANCE: Full building";
            }
            else
            {
                string year = Content.YearNames[v.IdYearLevel - 1];
                string course = Content.Courses[v.IdCourseIndex];
                string status = v.IdIsTransferee ? "TRANSFEREE" : "REGULAR";
                _idYear.text = (template ? "" : "YEAR: ") + year;
                _idCourse.text = (template ? "" : "COURSE: ") + course;
                _idStatus.text = (template ? "" : "STATUS: ") + status;
            }
            _idExpires.text = admin ? "VALID THRU: " + v.ExpirationDisplay : "EXPIRES: " + v.ExpirationDisplay;
        }

        // The student card is the owner's printed template (header, field names, barcode);
        // the administrator's badge keeps the older plain card. This swaps the art, the flip
        // handle (the header bar on the template, the black spine on the badge) and where the
        // lines of text sit. All numbers are in the 320x220 design space of the card.
        private void LayoutIdCard(bool template)
        {
            if (_idArt == null) return;
            _idArt.sprite = template ? ProceduralArt.Get("id_card_template") : ProceduralArt.Get("id_card_frame");
            var st = _idStripe.rectTransform;
            if (template)
            {
                st.anchorMin = new Vector2(0, 1);
                st.anchorMax = new Vector2(1, 1);
                st.offsetMin = new Vector2(0, -59);
                st.offsetMax = Vector2.zero;
                _idStripe.color = new Color(0f, 0f, 0f, 0f);   // the printed header shows; the bar is just the click area
                _idPhotoSlot = L.Inner.IdPhotoTemplate;
                PlaceIdText(_idName, 76);
                PlaceIdText(_idNumber, 100);
                PlaceIdText(_idCourse, 121);
                PlaceIdText(_idYear, 141.5f);
                PlaceIdText(_idStatus, 162);
                L.Place(_idExpires.rectTransform, new L.Region(16, 190, 250, 18));
                _idExpires.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                st.anchorMin = new Vector2(0, 0);
                st.anchorMax = new Vector2(0, 1);
                st.offsetMin = Vector2.zero;
                st.offsetMax = new Vector2(14, 0);
                _idStripe.color = new Color(0.06f, 0.06f, 0.07f);
                _idPhotoSlot = L.Inner.IdPhoto;
                Text[] rows = { _idName, _idNumber, _idYear, _idCourse, _idStatus, _idExpires };
                for (int i = 0; i < rows.Length; i++)
                {
                    L.Place(rows[i].rectTransform, new L.Region(L.Inner.IdRowX,
                        L.Inner.IdRules[i] - L.Inner.IdRowH, L.Inner.IdRowW, L.Inner.IdRowH));
                    rows[i].alignment = TextAnchor.UpperLeft;
                }
            }
            FrameToSlot(_idPhoto, _idPhotoSlot);
        }

        // one value line on the printed template: just above its field line, left aligned
        private static void PlaceIdText(Text t, float y)
        {
            L.Place(t.rectTransform, new L.Region(112f, y, 163f, 16f));
            t.alignment = TextAnchor.MiddleLeft;
        }

        // The district roster memo that arrives with the administrator's badge.
        private void FillAdminMemo(VisitorData v)
        {
            _recHeader.text = "DISTRICT ROSTER MEMO";
            _recName.text = "ASSIGNED: " + v.RecordName;
            _recNumber.text = "BADGE NO: " + v.RecordIdNumber;
            _recYear.text = "EFFECTIVE: " + Content.ShiftDates[Mathf.Clamp(_gameRoot.Session.CurrentShift - 1, 0, 4)];
            _recCourse.text = "PURPOSE: Assume control of site";
            _recStatus.text = "SIGNED: District Seal (smudged)";
            _recClassroom.text = "ESCORT: None authorized";
            _recEnrolled.text = "FILED: 1987-10-05";
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
            _recClassroom.text = "FIRST CLASS: " + v.ClassroomString;
            _recEnrolled.text = "REGISTRY: ON FILE";
        }
    }
}