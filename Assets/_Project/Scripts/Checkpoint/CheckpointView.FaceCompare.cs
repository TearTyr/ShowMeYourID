using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // Face comparison at the window: click the ID's face photograph, then click the student at
    // the window, and the desk answers with a brief outline cue — green when the printed photo
    // and the face in the glass are the same portrait, muted amber when they are not. The truth
    // comes from the run's own data (VisitorData.PhotoMatches: the window portrait vs the photo
    // printed on the card — the same structs the defect system swaps for PhotoMismatch), never
    // from a guess and never from image recognition. It only INFORMS: no verdict, no deny, no
    // score. One desk area; see CheckpointView.cs for the shared fields and state machine.
    public partial class CheckpointView
    {
        private IdPhotoSelect _photoSelect;
        private ClickSense _portraitClick;
        private Image _idPhotoHit;      // click-catcher over the DISPLAYED photo pixels
        private Image _portraitHit;     // click-catcher over the DISPLAYED window portrait
        private RectTransform _photoSelOutline;    // thin pale frame while the photo is selected
        private Image _photoSelBorder;
        private RectTransform _faceCue;            // brief green/amber frame over the portrait
        private Image _faceCueBorder;
        private Coroutine _cueRoutine;
        private bool _photoSelected;
        // what the last comparison played (stub coroutines finish synchronously headlessly, so
        // the proof reads these instead of sampling a mid-pulse alpha)
        internal bool LastFaceCueMatch { get; private set; }
        internal int FaceCueCount { get; private set; }
        // the cue inks own their alpha: SetPaperHeld's card-wide dim/brighten must skip them
        private readonly HashSet<Image> _cueInks = new HashSet<Image>();

        private static readonly Color SelInk = new Color(0.86f, 0.89f, 0.93f);   // pale, thin
        private static readonly Color MatchInk = new Color(0.42f, 0.62f, 0.42f); // muted green
        private static readonly Color MismatchInk = new Color(0.74f, 0.46f, 0.26f); // muted amber

        // Wired once, after the window and the papers exist: the photo and the portrait become
        // click targets, each with a single handler assigned here (no duplicate subscriptions).
        private void BuildFaceCompare()
        {
            if (_idPhoto != null)
            {
                // the photo Image letterboxes inside its slot; the hitbox is the rect the player
                // actually sees, so clicks outside the face never select (and the Image itself
                // stays click-through, leaving the card body free to drag)
                _idPhoto.raycastTarget = false;
                _idPhotoHit = UIFactory.AspectHitbox(_idPhoto, "idphotohit");
                _cueInks.Add(_idPhotoHit);   // SetPaperHeld must never brighten the click-catcher
                _photoSelect = _idPhotoHit.gameObject.AddComponent<IdPhotoSelect>();
                _photoSelect.Init(_idCardGo != null ? _idCardGo.GetComponent<DragPaper>() : null);
                _photoSelect.OnSelected = () => SetPhotoSelected(!_photoSelected);  // click toggles
                // a hollow frame sprite: outlines the photo without ever covering it
                _photoSelOutline = UIFactory.Img(_idPhoto.rectTransform, "photosel",
                    ProceduralArt.Get("outline_frame"), SelInk).rectTransform;
                UIFactory.AnchorsFull(_photoSelOutline);
                _photoSelBorder = _photoSelOutline.GetComponent<Image>();
                _photoSelBorder.raycastTarget = false;
                _cueInks.Add(_photoSelBorder);
                SetPhotoSelected(false, true);
            }
            if (_visitorPortrait != null)
            {
                _visitorPortrait.raycastTarget = false;
                _portraitHit = UIFactory.AspectHitbox(_visitorPortrait, "visitorhit");
                _cueInks.Add(_portraitHit);
                _portraitClick = _portraitHit.gameObject.AddComponent<ClickSense>();
                _portraitClick.OnClick = OnWindowStudentClicked;
                _faceCue = UIFactory.Img(_visitorPortrait.rectTransform, "facecue",
                    ProceduralArt.Get("outline_frame"), MatchInk).rectTransform;
                UIFactory.AnchorsFull(_faceCue);
                _faceCueBorder = _faceCue.GetComponent<Image>();
                _faceCueBorder.raycastTarget = false;
                _cueInks.Add(_faceCueBorder);
                SetCueAlpha(0f);
            }
        }

        /// <summary>Selection state of the ID photograph (thin outline on the photo only —
        /// never the whole card). Changing the selection before comparing is allowed.</summary>
        internal bool FacePhotoSelected => _photoSelected;

        private void SetPhotoSelected(bool on, bool quiet = false)
        {
            _photoSelected = on;
            if (_photoSelBorder != null)
            {
                var c = _photoSelBorder.color;
                c.a = on ? 0.85f : 0f;
                _photoSelBorder.color = c;
            }
            if (!quiet && _gameRoot != null)
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    on ? "ID photo selected — now click the face at the window."
                       : "ID photo selection cleared."));
        }

        // The student at the window: with a photo selected this is the comparison; without one
        // it is a quiet nudge, never a silent no-op. Dialogue and the rest of the window keep
        // their own behaviour — this handler only adds the click.
        private void OnWindowStudentClicked()
        {
            if (_current == null || _gameRoot == null) return;
            if (!_photoSelected)
            {
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "Select the ID PHOTO first, then the face at the window."));
                return;
            }
            CompareFaceWithIdPhoto();
        }

        // The verdict of the mechanic: the run's own pairing truth. PhotoMismatch /
        // PhotoSubtleMismatch are generated by swapping the ID photo away from the window
        // portrait (or, for the administrator, glitching the window face), so
        // DisplayedFacesMatch is exactly "these two displayed faces are the same person".
        internal bool CompareFaceWithIdPhoto()
        {
            if (_current == null) return true;
            bool match = _current.DisplayedFacesMatch;
            SetPhotoSelected(false, true);          // the comparison consumes the selection
            PlayFaceCue(match);
            return match;
        }

        private void PlayFaceCue(bool match)
        {
            if (_faceCueBorder == null) return;
            LastFaceCueMatch = match;
            FaceCueCount++;
            if (_cueRoutine != null) StopCoroutine(_cueRoutine);   // one cue at a time, no pile-up
            _cueRoutine = StartCoroutine(FaceCuePulse(match));
        }

        // A brief frame around the face: in, hold, out. ReduceMotion gets a flat half-second.
        private IEnumerator FaceCuePulse(bool match)
        {
            var ink = match ? MatchInk : MismatchInk;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            if (reduce)
            {
                SetCueColor(ink, 0.8f);
                yield return WaitUnpaused(0.5f);
                SetCueAlpha(0f);
            }
            else
            {
                float t = 0f;
                while (t < 0.9f)
                {
                    t += Time.unscaledDeltaTime;
                    float a = t < 0.15f ? t / 0.15f : (t < 0.6f ? 1f : Mathf.Clamp01((0.9f - t) / 0.3f));
                    SetCueColor(ink, a * 0.85f);
                    yield return null;
                }
                SetCueAlpha(0f);
            }
            _cueRoutine = null;
        }

        private void SetCueColor(Color ink, float a)
        {
            if (_faceCueBorder == null) return;
            var c = ink; c.a = a;
            _faceCueBorder.color = c;
        }

        private void SetCueAlpha(float a)
        {
            if (_faceCueBorder == null) return;
            var c = _faceCueBorder.color; c.a = a;
            _faceCueBorder.color = c;
        }

        // A new visitor (or leaving the desk) invalidates any half-finished comparison: the
        // outline and the cue clear, and no timer survives into the next inspection.
        internal void ResetFaceCompare()
        {
            if (_cueRoutine != null) { StopCoroutine(_cueRoutine); _cueRoutine = null; }
            SetCueAlpha(0f);
            SetPhotoSelected(false, true);
        }

        // The sprites swap per visitor (and per face-asset mapping), so the hitboxes re-fit to
        // whatever is displayed — clicks keep matching pixels at any card zoom or position.
        internal void SyncFaceHitboxes()
        {
            UIFactory.SyncAspectHitbox(_idPhoto, _idPhotoHit);
            UIFactory.SyncAspectHitbox(_visitorPortrait, _portraitHit);
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

        // Headless hooks: the same two paths the pointer takes.
        internal void DebugSelectIdPhoto() => SetPhotoSelected(true, true);
        internal bool DebugCompareFaces() => CompareFaceWithIdPhoto();
        internal void DebugClickWindowStudent() => OnWindowStudentClicked();
    }
}
