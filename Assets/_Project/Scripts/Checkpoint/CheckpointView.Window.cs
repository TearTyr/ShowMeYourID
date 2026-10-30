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
    // The window slot: visitor portrait, the offered ID hand-over, glitch hints.
    // One area of the inspection desk; see CheckpointView.cs for the state machine,
    // the shared fields and the whole-class comment. Same class, split by desk area.
    public partial class CheckpointView
    {
        private void BuildWindowArea()
        {
            // booth dressing from the mockup: two hood bands over the frame, then the frame
            var hood1 = UIFactory.Panel(_desk, "hood1", new Color(0.03f, 0.03f, 0.03f, 0.95f));
            Reg("hood1", hood1.rectTransform, L.Hood1);
            hood1.raycastTarget = false;
            var hood2 = UIFactory.Panel(_desk, "hood2", new Color(0.05f, 0.05f, 0.05f, 0.95f));
            Reg("hood2", hood2.rectTransform, L.Hood2);
            hood2.raycastTarget = false;
            var win = UIFactory.BorderedPanel(_desk, "windowframe", UIFactory.NsInk, UIFactory.NsChalk, 3);
            Reg("window", win, L.Window);
            win.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

            var glass = UIFactory.Panel(_desk, "glass", new Color(0.227f, 0.227f, 0.251f));
            Reg("glass", glass.rectTransform, L.Glass);
            glass.raycastTarget = false;
            var facade = UIFactory.Img(glass.rectTransform, "facade", ProceduralArt.Get("school_facade"));
            UIFactory.AnchorsFull(facade.rectTransform);
            facade.raycastTarget = false;
            var tint = UIFactory.Panel(glass.rectTransform, "tint", UIFactory.Glass);
            UIFactory.AnchorsFull(tint.rectTransform);
            tint.raycastTarget = false;

            // stable registered container; the face Image is letterboxed INSIDE it per sprite,
            // so the artwork is centred whatever its source dimensions (FrameToSlot)
            _portraitRoot = UIFactory.NewUI(_desk, "portraitroot");
            Reg("portrait", _portraitRoot, L.Portrait);
            _visitorPortrait = UIFactory.Img(_portraitRoot, "visitor", ProceduralArt.Get("face_placeholder"));
            _visitorPortrait.preserveAspect = true;
            _visitorPortrait.raycastTarget = false;   // the face is scenery: it takes no clicks

            _glitchOverlay = UIFactory.Img(_desk, "glitch", ProceduralArt.Get("noise_overlay"), new Color(1, 1, 1, 0f));
            Reg("glitch", _glitchOverlay.rectTransform, L.Glitch);
            _glitchOverlay.raycastTarget = false;

            var sill = UIFactory.Panel(_desk, "sill", new Color(0.85f, 0.83f, 0.78f, 0.35f));
            Reg("sill", sill.rectTransform, L.Sill);
            sill.raycastTarget = false;
            var base1 = UIFactory.Panel(_desk, "base1", UIFactory.NsDesk);
            Reg("base1", base1.rectTransform, L.Base1);
            base1.raycastTarget = false;
            var base2 = UIFactory.Panel(_desk, "base2", new Color(0.05f, 0.05f, 0.05f));
            Reg("base2", base2.rectTransform, L.Base2);
            base2.raycastTarget = false;
            var base3 = UIFactory.Panel(_desk, "base3", new Color(0.04f, 0.04f, 0.04f));
            Reg("base3", base3.rectTransform, L.Base3);
            base3.raycastTarget = false;

            // the student slides their ID to the counter edge on arrival (OfferAtCounter);
            // nothing here needs a click target any more
        }

        // The hand-over step: a dimmer plus the centred offered ID. Clicking the card takes
        // it (docks into the desk slot and reveals the papers). Built once, hidden always.
        private void BuildOfferedId()
        {
            var rootRt = UIFactory.NewUI(_desk, "offeredroot");
            L.Place(rootRt, L.FullDesk);
            // no full-desk dimmer: the counter card must never block stamps or tools

            var rt = UIFactory.NewUI(rootRt, "offeredid");
            L.Place(rt, L.OfferedId);
            // raycast ON: clicking anywhere on the offered card takes the ID
            var frame = UIFactory.Img(rt, "frame", ProceduralArt.Get("id_card_frame"), null, true);
            UIFactory.AnchorsFull(frame.rectTransform);
            _offeredPhoto = UIFactory.Img(rt, "photo", ProceduralArt.Get("face_placeholder"));
            L.Place(_offeredPhoto.rectTransform, L.Inner.IdPhoto);
            _offeredPhoto.preserveAspect = true;
            _offeredPhoto.raycastTarget = false;   // the card frame is the drag target, not the photo
            _offeredName = OfferedRow(rt, 0);
            _offeredNumber = OfferedRow(rt, 1);
            _offeredExpires = OfferedRow(rt, 5);
            var hint = UIFactory.Label(rt, "hint", "DRAG ME TO YOUR DESK", 12,
                UIFactory.InkDim, TextAnchor.MiddleCenter);
            L.Place(hint.rectTransform, new L.Region(0, 202, 320, 16));

            // the card on the counter is physical: drag it onto the desk to take it,
            // tap it to take it to the default slot, let go off-desk and it rides back
            _offeredRoot = rootRt.gameObject;
            // the placed card rect (not the stretched container) is the physical object;
            // scaled down so it rests small on the counter edge
            rt.localScale = new Vector3(0.6f, 0.6f, 1f);
            var offerDrag = rt.gameObject.AddComponent<DragPaper>();
            offerDrag.Init(_desk, 0f);
            offerDrag.ReturnsHome = true;
            offerDrag.OnDropped = center => { if (center.y >= 420f) TakeIdAt(center); };
            offerDrag.OnTapped = center => DockIdToSlot();
            _offeredRoot.SetActive(false);


        }

        // Built from BuildPapersLayer so the slip stacks with the other papers.
        private void BuildSlip()
        {
            // The paper slip (ticket): arrives with the ID, carries name/ID/reason, takes
            // the stamp, and goes back to the student with the ID to close the inspection.
            // Same container + scaled-art pattern as the ID card: the slip's written lines
            // keep their 200x120 design space inside one uniformly scaled art layer, while
            // the desk footprint is the concept-measured paper rect.
            var slipRt = UIFactory.NewUI(_desk, "slip");
            L.Place(slipRt, L.CounterSlip);
            slipRt.localScale = Vector3.one;   // readable at the counter, clear of the frame
            slipRt.localEulerAngles = new Vector3(0, 0, L.TiltSlip);
            var slipSurface = slipRt.gameObject.AddComponent<UnityEngine.UI.Image>();
            slipSurface.color = new Color(0f, 0f, 0f, 0f);
            slipSurface.raycastTarget = true;   // the container owns the pointer
            var slipArtRt = UIFactory.NewUI(slipRt, "slipart");
            UIFactory.PlaceTL(slipArtRt, 0, 0, 200, 120);
            slipArtRt.localScale = new Vector3(L.SlipScale, L.SlipScale, 1f);
            var slipBg = UIFactory.Panel(slipArtRt, "bg", new Color(0.88f, 0.86f, 0.80f));
            UIFactory.AnchorsFull(slipBg.rectTransform);
            _slipName = UIFactory.Label(slipArtRt, "sname", "", 11, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(_slipName.rectTransform, new L.Region(8, 8, 184, 16));
            _slipId = UIFactory.Label(slipArtRt, "sid", "", 11, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(_slipId.rectTransform, new L.Region(8, 28, 184, 16));
            _slipReason = UIFactory.Label(slipArtRt, "sreason", "", 10, UIFactory.PaperInk, TextAnchor.UpperLeft);
            L.Place(_slipReason.rectTransform, L.Inner.SlipReason);
            // no printed stamp box: the verdict mark lands WHERE the player drops the
            // stamp (Papers, Please style), clamped fully inside the paper — see
            // TryStampOnDrop / StampPlacement
            var slipBack = UIFactory.Panel(slipArtRt, "slipback", new Color(0.84f, 0.82f, 0.76f));
            UIFactory.AnchorsFull(slipBack.rectTransform);
            var backNote = UIFactory.Label(slipBack.rectTransform, "note", "RADIO NOTE: verify against the registry", 10,
                UIFactory.PaperInk, TextAnchor.MiddleCenter);
            UIFactory.AnchorsFull(backNote.rectTransform);
            slipBack.gameObject.SetActive(false);
            _slipGo = slipRt.gameObject;
            if (_papersLayer != null) _slipGo.transform.SetParent(_papersLayer.transform, false);
            var slipDrag = _slipGo.AddComponent<DragPaper>();
            slipDrag.Init(_desk, 1.2f);
            slipDrag.OnLift = () => _lastPaper = _slipGo;
            // a tap on the counter slip takes it; a tap on the TAKEN slip turns it over —
            // its own flip (FlipSlip), never the ID's: the papers are decoupled
            slipDrag.OnTapped = c => { if (!_slipTaken) TakeSlipAt(new Vector2(740f, 560f)); else FlipSlip(); };
            slipDrag.OnDropped = c => SlipDropped(c);
            _slipGo.SetActive(false);
        }

        private void OfferSlipAtCounter()
        {
            if (_current == null || _slipTaken) return;
            FillSlip(_current);
            var rt = _slipGo.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            L.Place(rt, L.CounterSlip);
            _slipGo.GetComponent<DragPaper>().SetHome(new Vector2(L.CounterSlip.X, L.CounterSlip.Y));
            _slipGo.SetActive(true);
        }

        private void FillSlip(VisitorData v)
        {
            if (_slipName != null) _slipName.text = "NAME: " + v.SlipName;
            if (_slipId != null) _slipId.text = "ID: " + v.SlipIdNumber;
            if (_slipReason != null) _slipReason.text = "REASON: " + v.SlipReason;
        }

        private void TakeSlipAt(Vector2 deskCenter)
        {
            if (_current == null || _slipTaken) return;
            _slipTaken = true;
            _slipGo.transform.SetAsLastSibling();      // taken paper rises to the top
            var rt = _slipGo.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            var drag = _slipGo.GetComponent<DragPaper>();
            drag.PlaceCenter(deskCenter);
            drag.SetHome(new Vector2(740f, 560f));   // desk slot if it ever returns home
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.5f);
            RefreshHandoverPrompt();
        }

        private void SlipDropped(Vector2 center)
        {
            if (!_slipTaken)
            {
                if (center.y >= 420f) TakeSlipAt(center);
                else
                {
                    var rt = _slipGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(L.CounterSlip.X, -L.CounterSlip.Y);
                }
                return;
            }
            if (InWindowZone(center)) HandBackSlip();
        }

        private bool InWindowZone(Vector2 c) =>
            c.x >= L.Window.X - 10f && c.x <= L.Window.Right + 10f &&
            c.y >= L.Window.Y - 10f && c.y <= L.Sill.Bottom + 10f;

        private void HandBackSlip()
        {
            if (!_slipStamped) { _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.25f); return; }
            if (_backSlip) return;
            _backSlip = true;
            SetPaperHeld(_slipGo, false);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
            FinalizeIfReady();
        }

        internal void HandBackId()
        {
            if (_backId) return;
            if (!_slipStamped) { _gameRoot.Audio.PlaySfx("sfx_scan_error", 0.25f); return; }  // decide first
            _backId = true;
            SetPaperHeld(_idCardGo, false);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
            FinalizeIfReady();
        }

        // dimmed = "with the student"; full = on your desk.
        private void SetPaperHeld(GameObject go, bool held)
        {
            if (go == null) return;
            foreach (var img in go.GetComponentsInChildren<Image>(true))
            {
                var c = img.color; c.a = held ? 1f : 0.55f; img.color = c;
            }
            foreach (var t in go.GetComponentsInChildren<Text>(true))
            { var c = t.color; c.a = held ? 1f : 0.55f; t.color = c; }
        }

        // The decision counts HERE — when the student has both papers back, never earlier.
        private void FinalizeIfReady()
        {
            if (!_backId || !_backSlip || !_slipStamped || _pendingVerdict == Verdict.None) return;
            var v = _pendingVerdict;
            _pendingVerdict = Verdict.None;
            _slipGo.SetActive(false);
            if (_idCardGo != null) _idCardGo.SetActive(false);
            Stamp(v);
        }

        // Fresh hand-over state per visitor, incl. the per-inspection stamp-rotation seed.
        private void ResetHandover(VisitorData v)
        {
            _slipTaken = false; _slipStamped = false; _pendingVerdict = Verdict.None;
            _backId = false; _backSlip = false;
            // a fresh desk per student: zoom resets, the old file closes, the query clears
            var idp = _idCardGo != null ? _idCardGo.GetComponent<DragPaper>() : null;
            if (idp != null) idp.Zoom = 1f;
            var slp = _slipGo != null ? _slipGo.GetComponent<DragPaper>() : null;
            if (slp != null) slp.Zoom = 1f;
            if (_recordGo != null) _recordGo.SetActive(false);
            FaceUp(_idCardGo);          // flip state never carries across visitors: both
            FaceUp(_slipGo);            // papers are born face-up on a fresh desk
            CloseFileDrawer();          // drawer shut, no file highlighted on a fresh desk
            _selectedCanon = -1;
            _stampRng = new GameRng((_gameRoot.Session != null ? _gameRoot.Session.Seed : 1) ^ (v.VisitorIndex * 131 + 7));
            SetPaperHeld(_idCardGo, true);
            SetPaperHeld(_slipGo, true);
            if (_stampOverlay != null) { var c = _stampOverlay.color; c.a = 0f; _stampOverlay.color = c; }
            _lastPaper = _idCardGo;
            OfferAtCounter();          // fills + activates the counter card...
            OfferSlipAtCounter();      // ...and the slip (FillSlip lives here)
            // §6 automation: the papers are ALREADY on the desk when the student steps up —
            // dragging still arranges them, but never gates the inspection
            TakeId();
            DockIdToSlot();
            TakeSlipAt(new Vector2(740f, 560f));
        }

        // Offered card rows reuse the ID-card grid offsets (same card size).
        private Text OfferedRow(RectTransform parent, int rowIndex)
        {
            var t = UIFactory.Label(parent, "row" + rowIndex, "", 12, UIFactory.PaperInk);
            L.Place(t.rectTransform, new L.Region(L.Inner.IdRowX,
                L.Inner.IdRules[rowIndex] - L.Inner.IdRowH, L.Inner.IdRowW, L.Inner.IdRowH));
            return t;
        }

        // Headless hook: present the Night-5 administrator so the memo path is provable.
        internal void DebugPresentAdmin()
        {
            var admin = new RosterFactory(_gameRoot.Session.Seed).BuildAdministrator(ShiftPlan.Build()[4]);
            PresentFromQueue(admin, false);   // same state machine the queue path uses
        }

        // Headless hook: the window goes empty mid-beat (the visitor walked off while a
        // typed line was still in flight) so the dock's error path is provable.
        internal void DebugClearVisitor()
        {
            _current = null;
            SetVisitorVisible(false);
            RefreshHud();
        }

        // Arrival beat: the student slides the ID to the counter edge, unasked.
        private void OfferAtCounter()
        {
            if (_current == null || _verdictLocked || _documentsTaken) return;
            if (_offeredRoot.activeSelf) return;
            FillOfferedCard(_current);
            _offeredRoot.SetActive(true);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.45f);   // the slide across the counter
            RefreshHandoverPrompt();
        }

        // The drag-to-take path: the held card appears exactly where the player dropped it.
        private void TakeIdAt(Vector2 deskCenter)
        {
            if (_current == null || _documentsTaken) return;
            TakeId();
            var drag = _idCardGo.GetComponent<DragPaper>();
            if (drag != null) drag.PlaceCenter(deskCenter);
        }

        // The centred card shows just enough to read at arm's length: face, name, number, expiry.
        private void FillOfferedCard(VisitorData v)
        {
            bool admin = v.IsAdministrator;
            _offeredPhoto.sprite = FaceArt.IdSprite(v);   // the counter card shows the SAME photo as the desk card
            FrameToSlot(_offeredPhoto, L.Inner.IdPhoto);
            _offeredName.text = "NAME: " + v.ClaimedName;
            _offeredNumber.text = (admin ? "BADGE NO: " : "ID NO: ") + v.IdNumber;
            _offeredExpires.text = (admin ? "VALID THRU: " : "EXPIRES: ") + v.ExpirationDisplay;
        }

        // Click on the offered card: take it. The papers dock on the desk and unlock.
        private void TakeId()
        {
            if (_current == null || _documentsTaken) return;
            _offeredRoot.SetActive(false);
            _documentsTaken = true;
            if (_idCardGo != null) _idCardGo.transform.SetAsLastSibling();
            SetDocumentsVisible(true);
            if (_current.IsAdministrator && _recordGo != null)
            {
                // the District Roster Memo rides on the record sheet: hand it over WITH the
                // badge (issue #4) — it is the administrator's paper clue
                _recordGo.SetActive(true);
                _recordGo.transform.SetAsLastSibling();
            }
            bool admin = _current.IsAdministrator;
            FillIdCard(_current, admin);
            FillRecordSheet(_current, admin);
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.6f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.DocumentsShown, _current));
            RefreshHandoverPrompt();
            RefreshHud();
        }

        // internal so the headless dump harness can land the post-verdict beat the stubbed
        // coroutine pump skips (the editor runs AfterStamp for real)
        internal void SetVisitorVisible(bool on)
        {
            if (_portraitRoot != null) _portraitRoot.gameObject.SetActive(on);
            _visitorPortrait.gameObject.SetActive(on);
            _visitorShown = on;
            RefreshWindowLayer(on);       // the booth shutter follows the visitor
            if (!on) _bubble.HideNow();   // an empty window carries no line
            SyncBubble();
            _glitchOverlay.gameObject.SetActive(on && _glitchOverlay.color.a > 0f);
        }

        // Swaps in the new faces and clears the old stamp; the fade-in coroutine is
        // instant under Reduced Motion (the coroutine itself checks the setting).
        private void ShowVisitorPortrait(VisitorData v)
        {
            SetVisitorVisible(true);
            PaintFaces(v);
            if (_stampOverlay != null)
            {
                var sc = _stampOverlay.color; sc.a = 0f; _stampOverlay.color = sc;
                _stampOverlay.sprite = null;
            }
            var pc = _visitorPortrait.color; pc.a = 0f; _visitorPortrait.color = pc;
            StartCoroutine(PortraitFadeIn());
        }

        // Both faces from one place so the window, the card and the face-asset override can
        // never disagree about who is standing there. Neither face is a click target: the
        // portrait is scenery behind the glass and the ID photo belongs to the card you drag,
        // so both stay click-through (verification is the player's own eyes — there is no
        // photo-matching widget on this desk).
        private void PaintFaces(VisitorData v)
        {
            _visitorPortrait.sprite = FaceArt.WindowSprite(v);
            _idPhoto.sprite = FaceArt.IdSprite(v);
            // artwork centred in the window (root-local slot)... and in the card's photo slot
            FrameToSlot(_visitorPortrait, new L.Region(0f, 0f, L.Portrait.W, L.Portrait.H));
            FrameToSlot(_idPhoto, L.Inner.IdPhoto);
        }

        // Centre the ARTWORK, not the container: the face Image's own rect is set to the
        // letterbox of its sprite inside the slot, so preserveAspect has nothing left to guess
        // and every portrait — any source dimensions — sits centred in the window/slot.
        internal static void FrameToSlot(Image img, L.Region slot)
        {
            if (img == null || img.sprite == null) return;
            var b = UIFactory.Letterbox(slot.W, slot.H, img.sprite.rect.width, img.sprite.rect.height);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(slot.X + b.X, -(slot.Y + b.Y));
            rt.sizeDelta = new Vector2(b.W, b.H);
        }

        private IEnumerator PortraitFadeIn()
        {
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (reduce)
            {
                var cr = _visitorPortrait.color; cr.a = 1f; _visitorPortrait.color = cr;
                yield break;
            }
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                var c = _visitorPortrait.color; c.a = Mathf.Clamp01(t / 0.3f); _visitorPortrait.color = c;
                yield return null;
            }
            var c2 = _visitorPortrait.color; c2.a = 1f; _visitorPortrait.color = c2;
        }

        private IEnumerator GlitchHint()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            if (_current == null || !gameObject.activeInHierarchy) yield break;
            _glitchOverlay.gameObject.SetActive(true);
            var c = _glitchOverlay.color; c.a = 0.35f; _glitchOverlay.color = c;
            _gameRoot.Audio.PlaySfx("sfx_glitch", 0.35f);
            yield return new WaitForSecondsRealtime(0.12f);
            c = _glitchOverlay.color; c.a = 0f; _glitchOverlay.color = c;
            _glitchOverlay.gameObject.SetActive(false);
        }

        // Knock first, then either the administrator's scripted dialogue or the visitor's
        // greeting (plus a one-off glitch flash for visibly glitchy anomalies at low sanity).
        // Paper sounds and DocumentsShown now fire on the hand-over step, not on arrival.
        private void PlayArrivalVoices(VisitorData v, bool admin)
        {
            _gameRoot.Audio.PlaySfx("sfx_knock", 0.7f);
            if (admin)
            {
                var lines = new List<DialogueLine>
                {
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[0] },
                    new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[1] },
                };
                if (_gameRoot.Session.Clues.FoundCount >= 6)
                    lines.Add(new DialogueLine { Speaker = "YOUR THOUGHTS", Text = "That face. The old school photo — the staff member who never aged. It's them." });
                lines.Add(new DialogueLine { Speaker = "DISTRICT ADMINISTRATOR", Text = Content.AdminLines[3] });
                SayLine("AT THE WINDOW", "A formal coat. A briefcase. A smile that arrives a half-second late.");
                // the scripted introduction hands over to the branching conversation: same
                // visitor, same state, one owner (CheckpointView.AdminTalk)
                if (_gameRoot.Dialogue != null) _gameRoot.Dialogue.Play(lines, () => OpenAdminTalk());
                else OpenAdminTalk();
                _gameRoot.Audio.PlayMusic("final");
            }
            else
            {
                Say(v.Greeting);
                if (v.IsAnomaly && v.BehaviorGlitchVisible && _gameRoot.Session.Sanity < 70f)
                    StartCoroutine(GlitchHint());
            }
        }
    }
}
