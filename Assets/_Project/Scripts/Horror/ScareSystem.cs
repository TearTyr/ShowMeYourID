using System.Collections;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    // Stingers and jumpscares with accessibility guards. Created by each scene bootstrap.
    public class ScareSystem : MonoBehaviour
    {
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
            _black = blackRt.gameObject.AddComponent<Image>();
            _black.color = new Color(0, 0, 0, 0f);
            _black.raycastTarget = false;

            var faceRt = UIFactory.NewUI(_canvas.transform, "Face");
            UIFactory.Anchor(faceRt, new Vector2(0.5f, 0.5f), new Vector2(560, 640), Vector2.zero);
            _face = faceRt.gameObject.AddComponent<Image>();
            _face.color = new Color(1, 1, 1, 0f);
            _face.raycastTarget = false;

            var flashRt = UIFactory.NewUI(_canvas.transform, "Flash");
            UIFactory.AnchorsFull(flashRt);
            _flash = flashRt.gameObject.AddComponent<Image>();
            _flash.sprite = ProceduralArt.Get("flash");
            _flash.color = new Color(1, 1, 1, 0f);
            _flash.raycastTarget = false;
        }

        public void Capture(AnomalyBehaviorKind kind)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 3f;
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;

            var distorted = new Portrait
            {
                HairStyle = (int)kind % 6,
                FaceVariant = ((int)kind + 3) % 6,
                Accessory = 0,
                Distorted = true,
            };
            _face.sprite = ProceduralArt.Portrait("scare", distorted, 280, 320);
            _gameRoot.Audio.PlaySfx("sfx_scare", photoSafe ? 0.45f : 0.9f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, "capture_" + kind));
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

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                var bc = _black.color; bc.a = 0.85f * (t / fadeIn); _black.color = bc;
                var c = _face.color;
                c.a = t / fadeIn;
                _face.color = c;
                if (!reduce)
                {
                    float punch = 1f + 0.35f * (1f - t / fadeIn);
                    _face.rectTransform.localScale = new Vector3(punch, punch, 1f);
                }
                yield return null;
            }
            _face.rectTransform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                var bc = _black.color; bc.a = 0.85f * (1f - t / fadeOut); _black.color = bc;
                var c = _face.color; c.a = 1f - t / fadeOut; _face.color = c;
                yield return null;
            }
            var b2 = _black.color; b2.a = 0; _black.color = b2;
            var f2 = _face.color; f2.a = 0; _face.color = f2;
        }

        /// <summary>The scene that ends a failed night: the screen goes black, a face holds still in the
        /// middle long enough to take in what happened, then the jumpscare lands. <paramref name="onDone"/>
        /// fires once the scare has landed, with the screen still black. Stepped waits (not a frame
        /// loop), so any frame rate paces it. Photosensitive and Reduce-Motion settings soften it.</summary>
        public void FailureSequence(AnomalyBehaviorKind kind, System.Action onDone)
        {
            StartCoroutine(FailureRoutine(kind, onDone));
        }

        private IEnumerator FailureRoutine(AnomalyBehaviorKind kind, System.Action onDone)
        {
            bool photoSafe = _gameRoot.Settings != null && _gameRoot.Settings.PhotosensitivitySafe;
            bool reduce = _gameRoot.Settings != null && _gameRoot.Settings.ReduceMotion;
            _face.sprite = ProceduralArt.Portrait("scare", new Portrait
            {
                HairStyle = (int)kind % 6, FaceVariant = ((int)kind + 3) % 6, Accessory = 0, Distorted = true,
            }, 280, 320);
            const int steps = 20;
            // 1) black screen, slowly
            for (int i = 1; i <= steps; i++)
            {
                var bc = _black.color; bc.a = i / (float)steps; _black.color = bc;
                yield return new WaitForSecondsRealtime(0.8f / steps);
            }
            // 2) a face fades up in the middle and holds, small and still
            _face.rectTransform.localScale = Vector3.one * 0.7f;
            for (int i = 1; i <= steps; i++)
            {
                var c = _face.color; c.a = 0.9f * i / steps; _face.color = c;
                yield return new WaitForSecondsRealtime(1.2f / steps);
            }
            yield return new WaitForSecondsRealtime(0.9f);
            // 3) the jumpscare: it lunges forward with a flash and a sting
            _gameRoot.Audio.PlaySfx("sfx_scare", photoSafe ? 0.45f : 0.9f);
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, "failure_" + kind));
            if (!photoSafe)
            {
                var fc = _flash.color; fc.a = 0.85f; _flash.color = fc;
                yield return new WaitForSecondsRealtime(0.05f);
                fc.a = 0f; _flash.color = fc;
            }
            var full = _face.color; full.a = 1f; _face.color = full;
            _face.rectTransform.localScale = Vector3.one * (reduce ? 1.0f : 1.35f);
            if (!reduce) Shake(0.8f);
            yield return new WaitForSecondsRealtime(photoSafe ? 0.9f : 0.7f);
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
            _gameRoot.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, kind));
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
