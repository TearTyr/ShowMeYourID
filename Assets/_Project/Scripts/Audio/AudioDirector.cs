using System.Collections;
using System.Collections.Generic;
using Hollowbrook.Core;
using Hollowbrook.Bootstrap;
using UnityEngine;

namespace Hollowbrook.Audio
{
    // Plays every sound and music bed; also emits captions. Created by CoreBootstrap.
    public class AudioDirector : MonoBehaviour
    {
        private EventBus _bus;
        private SettingsSystem _settings;
        private AudioSource _musicA, _musicB;
        private AudioSource[] _sfxPool;
        private int _poolIndex;
        private string _musicKey = "";
        private Coroutine _fadeRoutine;
        private readonly List<EventSubscription> _subs = new List<EventSubscription>();
        private readonly HashSet<string> _missingWarned = new HashSet<string>();

        private static readonly Dictionary<string, string> Captions = new Dictionary<string, string>
        {
            { "sfx_knock", "*KNOCK  KNOCK  KNOCK*" },
            { "sfx_stinger", "*a sharp, dissonant sting*" },
            { "sfx_scare", "*SCREAM — something lunges*" },
            { "sfx_glitch", "*static crackles and snaps*" },
            { "sfx_flicker", "*the fluorescent light buzzes and stutters*" },
            { "sfx_heartbeat", "*your own heartbeat, too loud*" },
            { "sfx_intercom", "*intercom static*" },
            { "sfx_alarm", "*alarm klaxon*" },
            { "sfx_bell", "*shift bell*" },
            { "sfx_stamp_reject", "*REJECTED — the stamp comes down*" },
            { "sfx_stamp_allow", "*ALLOWED — the stamp comes down*" },
            { "sfx_scan_error", "*scanner: error tone*" },
            { "sfx_scan_ok", "*scanner: chirp*" },
            { "sfx_door", "*a door opens somewhere*" },
            { "music_anomaly", "*the air hums with something wrong*" },
        };

        public void Init(EventBus bus, SettingsSystem settings)
        {
            _bus = bus;
            _settings = settings;

            _musicA = gameObject.AddComponent<AudioSource>();
            _musicB = gameObject.AddComponent<AudioSource>();
            _musicA.loop = true; _musicB.loop = true;
            _musicA.playOnAwake = false; _musicB.playOnAwake = false;

            _sfxPool = new AudioSource[8];
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
                _sfxPool[i].loop = false;
            }

            _subs.Add(bus.Subscribe(EventIds.SettingsChanged, OnSettings));
            ApplyVolumes();
        }

        private void OnDestroy()
        {
            foreach (var s in _subs) s?.Dispose();
            _subs.Clear();
        }

        public void PlaySfx(string key, float volume = 1f)
        {
            var clip = ProceduralAudio.Get(key);
            if (clip == null)
            {
                if (_missingWarned.Add(key)) Debug.LogWarning("[AudioDirector] missing sfx '" + key + "' — skipped gracefully.");
                return;
            }
            if (_sfxPool == null || _sfxPool.Length == 0) return;
            var src = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sfxPool.Length;
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * (_settings != null ? _settings.SfxVolume : 0.8f));
            src.pitch = 1f + (clip.length > 0.4f ? 0f : (Random.value - 0.5f) * 0.06f);
            src.Play();

            if (_settings != null && _settings.Captions && Captions.TryGetValue(key, out var caption))
                _bus?.Publish(GameEvent.Of(EventIds.CaptionRequested, caption, 2.6f, null));
        }


        public void PlayMusic(string contextKey)
        {
            string clipKey = string.IsNullOrEmpty(contextKey) ? "" : "music_" + contextKey;
            if (clipKey == _musicKey) return;
            _musicKey = clipKey;
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(Crossfade(clipKey, 1.2f));
        }

        private IEnumerator Crossfade(string clipKey, float seconds)
        {
            var from = _musicA.isPlaying || _musicA.clip != null ? _musicA : _musicB;
            var to = from == _musicA ? _musicB : _musicA;

            AudioClip clip = null;
            if (!string.IsNullOrEmpty(clipKey))
            {
                clip = ProceduralAudio.Get(clipKey);
                if (clip == null)
                {
                    if (_missingWarned.Add(clipKey)) Debug.LogWarning("[AudioDirector] missing music '" + clipKey + "' — staying silent.");
                }
            }
            to.clip = clip;
            to.volume = 0f;
            to.loop = true;
            if (clip != null) to.Play();
            if (clipKey == "music_anomaly" && _settings != null && _settings.Captions)
                _bus?.Publish(GameEvent.Of(EventIds.CaptionRequested, Captions["music_anomaly"], 3.5f, null));

            float musicVol = _settings != null ? _settings.MusicVolume : 0.5f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                to.volume = musicVol * k;
                from.volume = musicVol * (1f - k);
                yield return null;
            }
            to.volume = musicVol;
            from.Stop();
            from.clip = null;
            _fadeRoutine = null;
        }

        private void OnSettings(GameEvent e) => ApplyVolumes();

        private void ApplyVolumes()
        {
            if (_settings == null) return;
            float mv = _settings.MusicVolume;
            if (_musicA != null && _musicA.isPlaying) _musicA.volume = mv;
            if (_musicB != null && _musicB.isPlaying) _musicB.volume = mv;
        }

        public void StopEverything()
        {
            if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
            _musicKey = "";
            if (_musicA != null) { _musicA.Stop(); _musicA.clip = null; }
            if (_musicB != null) { _musicB.Stop(); _musicB.clip = null; }
            if (_sfxPool != null) foreach (var s in _sfxPool) if (s != null) s.Stop();
        }
    }
}
