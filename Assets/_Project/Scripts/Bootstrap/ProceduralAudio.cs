using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollowbrook.Bootstrap
{
    public static class ProceduralAudio
    {
        public const int Rate = 44100;
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static AudioClip Get(string key)
        {
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            try
            {
                var loaded = Resources.Load<AudioClip>("Audio/" + key);
                if (loaded != null) { Cache[key] = loaded; return loaded; }
            }
            catch (Exception) { /* fall through to procedural */ }

            var built = Build(key);
            if (built != null) Cache[key] = built;
            else Debug.LogWarning("[ProceduralAudio] unknown clip key '" + key + "'");
            return built;
        }

        private static AudioClip Make(string key, float seconds, Func<float, float> fn, bool loopSeam = true)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(fn(i / (float)Rate), -1f, 1f);
            if (loopSeam)
            {
                int fade = Math.Min(n / 4, (int)(0.05f * Rate));
                for (int i = 0; i < fade; i++)
                {
                    float t = i / (float)fade;
                    data[i] = data[i] * t + data[n - fade + i] * (1 - t);
                }
            }
            var clip = AudioClip.Create(key, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float freq, float t, float phase = 0f) => Mathf.Sin(2f * Mathf.PI * freq * t + phase);
        private static float Env(float t, float dur, float attack = 0.01f)
        {
            if (t >= dur) return 0f;
            float a = t < attack ? t / attack : 1f;
            float rel = 1f - (t / dur);
            return a * rel * rel;
        }
        private static readonly System.Random NoiseRand = new System.Random(9);
        private static float White() => (float)(NoiseRand.NextDouble() * 2.0 - 1.0);
        private static float LowPass(float[] state, float input, float cutoff01)
        {
            state[0] += cutoff01 * (input - state[0]);
            return state[0];
        }

        private const float Eb1 = 38.89f, Bb1 = 58.27f, Db2 = 69.30f, Gb2 = 46.25f * 2f;
        private const float Eb2 = 77.78f, Gb3 = 185.00f, Bb2 = 116.54f, Ab2 = 103.83f;
        private const float F2 = 87.31f, Cb2 = 61.74f, G2 = 98.00f, Db3 = 138.59f;

        private static AudioClip Build(string key)
        {
            switch (key)
            {
                case "sfx_click":
                    return Make(key, 0.07f, t => Sine(880, t) * Env(t, 0.07f, 0.002f) * 0.5f, false);
                case "sfx_ui_open":
                    return Make(key, 0.16f, t => Sine(440 + t * 900, t) * Env(t, 0.16f, 0.01f) * 0.4f, false);
                case "sfx_ui_close":
                    return Make(key, 0.14f, t => Sine(660 - t * 1400, t) * Env(t, 0.14f, 0.01f) * 0.35f, false);
                case "sfx_paper":
                    return NoiseClip(key, 0.30f, 0.35f, 0.5f);
                case "sfx_stamp_allow":
                    return Make(key, 0.22f, t =>
                        (Sine(120, t) * 0.8f + NoiseAt(t) * 0.5f) * Env(t, 0.22f, 0.002f), false);
                case "sfx_stamp_reject":
                    return Make(key, 0.26f, t =>
                        (Sine(84, t) * 0.9f + Sine(111, t) * 0.4f + NoiseAt(t) * 0.6f) * Env(t, 0.26f, 0.002f), false);
                case "sfx_scan_ok":
                    return Make(key, 0.22f, t =>
                        (t < 0.09f ? Sine(988, t) : Sine(1319, t)) * Env(t % 0.11f, 0.11f, 0.002f) * 0.4f, false);
                case "sfx_scan_error":
                    return Make(key, 0.34f, t =>
                        Sine(220 - t * 240, t) * (1f + 0.5f * Mathf.Sign(Sine(28, t))) * Env(t, 0.34f, 0.005f) * 0.45f, false);
                case "sfx_intercom":
                    return Make(key, 1.1f, t =>
                        NoiseAt(t) * 0.18f * (0.4f + 0.6f * Env(t, 1.1f, 0.2f)) +
                        Sine(60, t) * 0.05f, false);
                case "sfx_knock":
                    return Make(key, 0.9f, t =>
                    {
                        float v = 0;
                        for (int k = 0; k < 3; k++)
                        {
                            float tk = t - k * 0.28f;
                            if (tk >= 0 && tk < 0.12f) v += (Sine(96, tk) * 0.9f + NoiseAt(tk) * 0.35f) * Env(tk, 0.12f, 0.001f);
                        }
                        return v * 0.8f;
                    }, false);
                case "sfx_footstep":
                    return NoiseClip(key, 0.07f, 0.5f, 0.12f);
                case "sfx_door":
                    return Make(key, 0.8f, t =>
                        Sine(70 + 40 * Mathf.Sin(t * 9f), t) * Env(t, 0.8f, 0.1f) * 0.4f +
                        NoiseAt(t) * 0.10f * Env(t, 0.8f, 0.1f), false);
                case "sfx_flicker":
                    return Make(key, 0.5f, t =>
                        (Sine(120, t) * 0.5f + Sine(240, t) * 0.25f) *
                        (NoiseAt(t) > 0.2f ? 1f : 0.15f) * Env(t, 0.5f, 0.01f) * 0.5f, false);
                case "sfx_static":
                    return NoiseClip(key, 0.6f, 0.8f, 0.4f);
                case "sfx_heartbeat":
                    return Make(key, 1.0f, t =>
                    {
                        float v = 0;
                        float t0 = t, t1 = t - 0.22f;
                        if (t0 < 0.14f) v += Sine(52, t0) * Env(t0, 0.14f, 0.004f);
                        if (t1 > 0 && t1 < 0.12f) v += Sine(46, t1) * Env(t1, 0.12f, 0.004f) * 0.8f;
                        return v * 0.9f;
                    }, false);
                case "sfx_stinger":
                    return Make(key, 1.4f, t =>
                        (Sine(233, t) + Sine(247, t) + Sine(466, t) * 0.6f) / 2.6f *
                        Env(t, 1.4f, 0.02f) * 0.7f, false);
                case "sfx_scare":
                    return Make(key, 1.1f, t =>
                        (Sine(740 - t * 420, t) * 0.5f + NoiseAt(t) * 0.5f + Sine(155, t) * 0.4f) *
                        Env(t, 1.1f, 0.006f) * 0.85f, false);
                case "sfx_glitch":
                    return Make(key, 0.35f, t =>
                        (NoiseAt(t) * (Sine(37, t) > 0 ? 0.8f : 0.1f) + Sine(1244, t) * 0.15f) * Env(t, 0.35f, 0.005f), false);
                case "sfx_pickup":
                    return Make(key, 0.28f, t =>
                        Sine(660, t) * Env(t, 0.12f, 0.004f) * 0.4f +
                        (t > 0.1f ? Sine(990, t) * Env(t - 0.1f, 0.16f, 0.004f) * 0.4f : 0f), false);
                case "sfx_bell":
                    return Make(key, 1.6f, t =>
                        (Sine(392, t) + Sine(784, t) * 0.5f + Sine(1176, t) * 0.22f) / 1.7f * Env(t, 1.6f, 0.01f) * 0.5f, false);
                case "sfx_alarm":
                    return Make(key, 1.8f, t =>
                        Sine(330 + 110 * Mathf.Sign(Sine(2.4f, t)), t) * Env(t, 1.8f, 0.05f) * 0.5f, false);

                case "music_menu":       return MenuTheme();
                case "music_checkpoint": return RadioNoiseLoop();
                case "music_hallway":    return HallwayDrone();
                case "music_classroom":  return ClassroomBed();
                case "music_darkroom":   return DarkRoomDrone();
                case "music_investigation": return InvestigationBed();
                case "music_anomaly":    return AnomalyPulse();
                case "music_final":      return FinalDrone();
                case "music_end_win":    return EndWin();
                case "music_end_lose":   return EndLose();
                case "music_end_secret": return EndSecret();

                default: return null;
            }
        }

        private static float NoiseAt(float t)
        {
            int seed = (int)(t * Rate);
            return ((seed * 1103515245 + 12345) % 2147483647) / 1073741823.5f - 1f;
        }

        private static AudioClip NoiseClip(string key, float dur, float cutoff, float vol)
        {
            return Make(key, dur, t =>
            {
                float lp = 0; // one-pole via accumulated state trick
                lp = LowPassShared(key, NoiseAt(t), cutoff);
                return lp * vol * Env(t, dur, 0.01f);
            }, false);
        }

        private static readonly Dictionary<string, float[]> _lpState = new Dictionary<string, float[]>();
        private static float LowPassShared(string key, float input, float cutoff01)
        {
            if (!_lpState.TryGetValue(key, out var st)) { st = new float[1]; _lpState[key] = st; }
            return LowPass(st, input, cutoff01);
        }

        private static AudioClip MenuTheme()
        {
            float[][] bars =
            {
                new[] { Bb1, Eb2, Gb3, Bb2 },            // Ebm over Bb
                new[] { Ab2, Db3, F2 * 2f, Db2 },        // Db over Ab
                new[] { G2, Cb2 * 2f, Eb2, Gb3 * 0.5f }, // Cb over G
                new[] { Ab2, Db3, F2 * 2f, Db2 },        // Db over Ab
                new[] { Bb1, Eb2, Gb3, Bb2 },            // Ebm over Bb
                new[] { Eb2, Ab2 * 0.5f, Cb2 * 2f, Eb2 },// Abm over Eb
                new[] { F2 * 0.5f, Bb2, Db3, F2 },       // Bbm over F
                new[] { Bb1, Eb2, Gb3, Bb2 },            // resolve
            };
            float barLen = 2.4f;
            return Make("music_menu", bars.Length * barLen, t =>
            {
                int bar = Mathf.Clamp((int)(t / barLen), 0, bars.Length - 1);
                float bt = t - bar * barLen;
                float v = 0;
                foreach (var f in bars[bar])
                {
                    v += Sine(f * 0.25f, t) * 0.20f;                  // deep bass
                    v += Sine(f, t) * 0.075f + Sine(f * 1.003f, t) * 0.06f;
                }
                float swell = Mathf.Clamp01(bt / 0.8f) * Mathf.Clamp01((barLen - bt) / 1.1f);
                v *= 0.35f + 0.65f * swell;
                v += NoiseAt(t) * 0.012f;                             // tape hiss
                return v;
            });
        }

        private static AudioClip RadioNoiseLoop()
        {
            return Make("music_checkpoint", 3f, t =>
            {
                float lp = LowPassShared("rn", NoiseAt(t), 0.22f);
                float hum = Sine(60, t) * 0.05f + Sine(120, t) * 0.02f;
                float warble = 0.7f + 0.3f * Sine(0.23f, t);
                return (lp * 0.30f + hum) * warble + Sine(1650, t) * 0.004f * (Sine(0.11f, t) > 0.8f ? 1f : 0f);
            });
        }

        private static AudioClip HallwayDrone()
        {
            return Make("music_hallway", 5f, t =>
                Sine(55, t) * 0.16f + Sine(82.4f, t) * 0.05f +
                LowPassShared("hw", NoiseAt(t), 0.08f) * 0.14f +
                (Sine(0.07f, t) > 0.985f ? Sine(1400, t) * 0.02f : 0f)); // rare distant tick
        }

        private static AudioClip ClassroomBed()
        {
            return Make("music_classroom", 5f, t =>
                Sine(65.4f, t) * 0.10f + Sine(98, t) * 0.045f +
                LowPassShared("cr", NoiseAt(t), 0.06f) * 0.10f);
        }

        private static AudioClip DarkRoomDrone()
        {
            return Make("music_darkroom", 6f, t =>
                Sine(41.2f, t) * 0.20f + Sine(43.1f, t) * 0.14f +           // beating pair
                Sine(110, t) * 0.02f * (0.5f + 0.5f * Sine(0.13f, t)) +
                LowPassShared("dr", NoiseAt(t), 0.04f) * 0.16f);
        }

        private static AudioClip InvestigationBed()
        {
            float[] notes = { Eb2, Gb3 * 0.5f, Ab2 * 0.5f * 2f, Bb2, Db3 * 0.5f * 2f };
            return Make("music_investigation", 6f, t =>
            {
                float v = LowPassShared("inv", NoiseAt(t), 0.05f) * 0.10f + Sine(38.89f, t) * 0.06f;
                float beat = t % 1.5f;
                int idx = (int)(t / 1.5f) % notes.Length;
                if (beat < 0.9f)
                    v += Sine(notes[idx], t) * Env(beat, 0.9f, 0.005f) * 0.10f;
                return v;
            });
        }

        private static AudioClip AnomalyPulse()
        {
            return Make("music_anomaly", 4f, t =>
            {
                float pulse = Env(t % 1f, 0.35f, 0.005f);
                return Sine(40, t) * 0.30f * pulse +
                       (Sine(220, t) * 0.05f + Sine(223.5f, t) * 0.05f) * (0.4f + 0.6f * pulse) +
                       LowPassShared("an", NoiseAt(t), 0.10f) * 0.10f;
            });
        }

        private static AudioClip FinalDrone()
        {
            return Make("music_final", 8f, t =>
            {
                float rise = t / 8f;
                float f = 46 * (1f + rise * 1.5f);
                return Sine(f, t) * 0.18f + Sine(f * 1.5f, t) * 0.08f +
                       Sine(f * 2.02f, t) * 0.05f * rise +
                       (t % 1.1f < 0.3f ? Sine(50, t) * Env(t % 1.1f, 0.3f, 0.004f) * 0.5f * rise : 0f) +
                       LowPassShared("fin", NoiseAt(t), 0.06f) * 0.10f;
            });
        }

        private static AudioClip EndWin()
        {
            return Make("music_end_win", 9f, t =>
            {
                bool resolved = t > 4.5f;
                float root = resolved ? Gb2 * 0.5f : Eb1;
                float third = resolved ? Bb2 : Gb3 * 0.5f;
                float fifth = resolved ? Db3 : Bb2;
                float v = Sine(root, t) * 0.18f + Sine(third, t) * 0.10f + Sine(fifth, t) * 0.09f +
                          Sine(root * 2f, t) * 0.03f;
                return v * (0.5f + 0.5f * Mathf.Clamp01(resolved ? (t - 4.5f) / 2f : t / 2f));
            });
        }

        private static AudioClip EndLose()
        {
            return Make("music_end_lose", 9f, t =>
            {
                float fall = 1f - Mathf.Clamp01(t / 8f) * 0.35f;
                return (Sine(92.5f * fall, t) * 0.16f + Sine(98 * fall, t) * 0.14f + // minor 2nd rub
                        Sine(46.25f * fall, t) * 0.20f +
                        LowPassShared("lose", NoiseAt(t), 0.05f) * 0.22f * (1f - t / 9f));
            });
        }

        private static AudioClip EndSecret()
        {
            float[] bells = { Gb3, Db3 * 2f, Bb2 * 2f, Eb2 * 4f, Ab2 * 2f };
            return Make("music_end_secret", 10f, t =>
            {
                float v = Sine(46.25f, t) * 0.10f + LowPassShared("sec", NoiseAt(t), 0.03f) * 0.06f;
                float beat = t % 1.25f;
                int idx = (int)(t / 1.25f) % bells.Length;
                if (beat < 1.1f) v += Sine(bells[idx], t) * Env(beat, 1.1f, 0.004f) * 0.12f;
                return v;
            });
        }

        public static void ClearCache()
        {
            foreach (var kv in Cache)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            Cache.Clear();
            _lpState.Clear();
        }
    }
}
