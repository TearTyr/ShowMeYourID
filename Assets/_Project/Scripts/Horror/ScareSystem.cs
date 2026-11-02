using System.Collections;
using Hollowbrook.Scenes;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    // Stingers and jumpscares with accessibility guards. Lives in each scene file, connected by the scene script.
    // A scare shows the REAL picture of what caught the player and zooms it at the camera.
    public class ScareSystem : MonoBehaviour
    {
        private const float CaptureZoomTo = 2.4f;     // how big a catch zooms the picture
        private const float FailureHoldScale = 0.8f;  // the failure scene's picture waits small and still
        private const float FailureZoomTo = 3.2f;     // ...then lunges at the camera this big

        private GameRoot _gameRoot;
        private Canvas _canvas;
        private Image _black;
        private Image _face;
        private Image _flash;
        private float _cooldownUntil = -99f;
        private Coroutine _shakeRoutine;

        public void Init(GameRoot root, Canvas scareCanvas)
        {
            _gameRoot = root;
            _canvas = scareCanvas;

            var blackRt = UIFactory.NewUI(_canvas.transform, "Black");
            UIFactory.AnchorsFull(blackRt);
            _black = blackRt.gameObject.GetOrAdd<Image>();
            _black.color = new Color(0, 0, 0, 0f);
            _black.raycastTarget = false;

            // the picture fills the screen but keeps its proportions; a scare zooms it up from there
            var faceRt = UIFactory.NewUI(_canvas.transform, "Face");
            UIFactory.AnchorsFull(faceRt);
            _face = faceRt.gameObject.GetOrAdd<Image>();
            _face.preserveAspect = true;
            _face.color = new Color(1, 1, 1, 0f);
            _face.raycastTarget = false;

            var flashRt = UIFactory.NewUI(_canvas.transform, "Flash");
            UIFactory.AnchorsFull(flashRt);
            _flash = flashRt.gameObject.GetOrAdd<Image>();
            _flash.sprite = ProceduralArt.Get("flash");
            _flash.color = new Color(1, 1, 1, 0f);
            _flash.raycastTarget = false;
        }

        /// <summary>An anomaly caught the player: its real picture zooms up over a dark screen.</summary>
        public void Capture(AnomalyBehaviorKind kind) => CaptureWith(BehaviorProfile.ArtKey(kind));

        /// <summary>The Principal caught the player: his glitched face zooms up the same way.</summary>
        public void CaptureAdmin() => CaptureWith(FaceAssets.AdminWindowKey);

        /// <summary>Anything else that jumps the player (a figure waiting behind a door): its picture, by art key.</summary>
        public void CaptureArt(string artKey) => CaptureWith(artKey);

        private void CaptureWith(string artKey)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 3f;
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;

            _face.sprite = ProceduralArt.Get(artKey);
            _gameRoot.Audio.PlaySfx("sfx_scare", photoSafe ? 0.45f : 0.9f);
            if (!reduce) Shake(0.6f);
            StartCoroutine(CaptureRoutine(photoSafe, reduce));
        }

        private IEnumerator CaptureRoutine(bool photoSafe, bool reduce)
        {
            float fadeIn = photoSafe ? 0.40f : 0.05f;
            float hold = photoSafe ? 0.55f : 0.45f;
            float fadeOut = 0.5f;

            if (!photoSafe)
            {
                var fc = _flash.color; fc.a = 0.85f; _flash.color = fc;
                yield return new WaitForSecondsRealtime(0.05f);
                fc.a = 0f; _flash.color = fc;
            }

            var bc0 = _black.color; bc0.a = 0.85f; _black.color = bc0;
            var shown = _face.color; shown.a = 1f; _face.color = shown;
            // the zoom: the picture starts at normal size and grows past the screen while it holds
            yield return Zoom(reduce ? 1.2f : 1f, reduce ? 1.2f : CaptureZoomTo, fadeIn + hold);
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                var bc = _black.color; bc.a = 0.85f * (1f - t / fadeOut); _black.color = bc;
                var c = _face.color; c.a = 1f - t / fadeOut; _face.color = c;
                yield return null;
            }
            var b2 = _black.color; b2.a = 0; _black.color = b2;
            var f2 = _face.color; f2.a = 0; _face.color = f2;
            _face.rectTransform.localScale = Vector3.one;
        }

        // Grow the face's picture from one scale to another over some seconds (it eases out: fast, then slowing).
        private IEnumerator Zoom(float from, float to, float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                float eased = 1f - (1f - k) * (1f - k);
                float s = Mathf.Lerp(from, to, eased);
                _face.rectTransform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _face.rectTransform.localScale = new Vector3(to, to, 1f);
        }

        /// <summary>The scene that ends a failed night: the screen goes black, the real picture of what got you
        /// holds still in the middle long enough to take in what happened, then it zooms at the camera and the
        /// scare lands. <paramref name="onDone"/> fires once the scare has landed, with the screen still black.
        /// Stepped waits (not a frame loop), so any frame rate paces it. Photosensitive and Reduce-Motion
        /// settings soften it.</summary>
        public void FailureSequence(string artKey, System.Action onDone)
        {
            StartCoroutine(FailureRoutine(artKey, onDone));
        }

        private IEnumerator FailureRoutine(string artKey, System.Action onDone)
        {
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            _face.sprite = ProceduralArt.Get(artKey);
            const int steps = 20;
            // 1) black screen, slowly
            for (int i = 1; i <= steps; i++)
            {
                var bc = _black.color; bc.a = i / (float)steps; _black.color = bc;
                yield return new WaitForSecondsRealtime(0.8f / steps);
            }
            // 2) the picture fades up in the middle and holds, small and still
            _face.rectTransform.localScale = Vector3.one * FailureHoldScale;
            for (int i = 1; i <= steps; i++)
            {
                var c = _face.color; c.a = 0.9f * i / steps; _face.color = c;
                yield return new WaitForSecondsRealtime(1.2f / steps);
            }
            yield return new WaitForSecondsRealtime(0.9f);
            // 3) the jumpscare: the same picture zooms at the camera with a flash and a sting
            _gameRoot.Audio.PlaySfx("sfx_scare", photoSafe ? 0.45f : 0.9f);
            if (!photoSafe)
            {
                var fc = _flash.color; fc.a = 0.85f; _flash.color = fc;
                yield return new WaitForSecondsRealtime(0.05f);
                fc.a = 0f; _flash.color = fc;
            }
            var full = _face.color; full.a = 1f; _face.color = full;
            if (!reduce) Shake(0.8f);
            float landSeconds = photoSafe ? 0.9f : 0.7f;
            yield return Zoom(FailureHoldScale, reduce ? 1.2f : FailureZoomTo, landSeconds);
            // 4) hand over with the screen still black; the next screen fades in under it
            _face.rectTransform.localScale = Vector3.one;
            var f0 = _face.color; f0.a = 0f; _face.color = f0;
            if (onDone != null) onDone();
            yield return new WaitForSecondsRealtime(0.4f);
            var b0 = _black.color; b0.a = 0f; _black.color = b0;
        }

        public void Stinger(string kind, float sanityHit)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 1.5f;
            if (sanityHit > 0f)
            {
                _gameRoot.Session.ApplySanity(-sanityHit);
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, _gameRoot.Session.Sanity));
            }
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            _gameRoot.Audio.PlaySfx("sfx_stinger", photoSafe ? 0.4f : 0.7f);
            StartCoroutine(StaticBurst(photoSafe ? 0.10f : 0.22f));
        }

        private IEnumerator StaticBurst(float peakAlpha)
        {
            var fc = _flash.color;
            _flash.sprite = ProceduralArt.Get("noise_overlay");
            fc.a = peakAlpha; _flash.color = fc;
            yield return new WaitForSecondsRealtime(0.09f);
            fc.a = 0f; _flash.color = fc;
            _flash.sprite = ProceduralArt.Get("flash");
        }

        public void Shake(float strength)
        {
            if (_gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion) return;
            if (_gameRoot.MainCamera == null) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(strength));
        }

        private IEnumerator ShakeRoutine(float strength)
        {
            if (_gameRoot.Views == null) yield break;
            var target = _gameRoot.Views.transform;
            float t = 0f;
            float dur = 0.45f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = strength * (1f - t / dur);
                target.localPosition = new Vector3(Random.Range(-k, k) * 0.09f, Random.Range(-k, k) * 0.09f, 0f);
                yield return null;
            }
            target.localPosition = Vector3.zero;
            _shakeRoutine = null;
        }
    }
}
