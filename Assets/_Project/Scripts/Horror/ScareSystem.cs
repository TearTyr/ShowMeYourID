using System.Collections;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.UI;
using Hollowbrook.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowbrook.Horror
{
    /// <summary>Scare director: jumpscares for anomaly captures, stingers for disturbing
    /// discoveries, and camera shake. Accessibility is built in, not bolted on:
    ///  • Photosensitivity-Safe  => no white flash, faces fade in slowly, softer audio;
    ///  • Reduced Motion         => no camera shake, no scale punch.
    /// A cooldown prevents stacked scares (readability + comfort).</summary>
    public class ScareSystem : MonoBehaviour
    {
        private GameRoot R;
        private Canvas _canvas;
        private Image _black;
        private Image _face;
        private Image _flash;
        private float _cooldownUntil = -99f;
        private Coroutine _shakeRoutine;

        public void Init(GameRoot root, Canvas scareCanvas)
        {
            R = root;
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

        /// <summary>Full jumpscare (anomaly caught the player).</summary>
        public void Capture(AnomalyBehaviorKind kind)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 3f;
            bool photoSafe = R.Settings != null && R.Settings.PhotosensitivitySafe;
            bool reduce = R.Settings != null && R.Settings.ReduceMotion;

            var distorted = new Portrait
            {
                HairStyle = (int)kind % 6,
                FaceVariant = ((int)kind + 3) % 6,
                Accessory = 0,
                Distorted = true,
            };
            _face.sprite = ProceduralArt.Portrait("scare", distorted, 280, 320);
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_scare", photoSafe ? 0.45f : 0.9f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, "capture_" + kind));
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

        /// <summary>Light scare: static burst + stinger (clue discoveries, sightings).</summary>
        public void Stinger(string kind, float sanityHit)
        {
            if (Time.unscaledTime < _cooldownUntil) return;
            _cooldownUntil = Time.unscaledTime + 1.5f;
            if (sanityHit > 0f)
            {
                R.Session.ApplySanity(-sanityHit);
                R.Bus.Publish(GameEvent.Of(EventIds.SanityChanged, R.Session.Sanity));
            }
            bool photoSafe = R.Settings != null && R.Settings.PhotosensitivitySafe;
            R.Bus.Publish(GameEvent.Of(EventIds.SfxRequested, "sfx_stinger", photoSafe ? 0.4f : 0.7f, null));
            R.Bus.Publish(GameEvent.Of(EventIds.ScareTriggered, kind));
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

        /// <summary>Camera shake (ignored when Reduced Motion is on).</summary>
        public void Shake(float strength)
        {
            if (R.Settings != null && R.Settings.ReduceMotion) return;
            if (R.MainCamera == null) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(strength));
        }

        private IEnumerator ShakeRoutine(float strength)
        {
            // Shake the WORLD root, not the camera: the camera-follow LateUpdate owns camera
            // position, so shaking here avoids fighting it (and shakes props+player together).
            if (R.Views == null) yield break;
            var target = R.Views.transform;
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
