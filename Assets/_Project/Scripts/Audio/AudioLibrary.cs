using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollowbrook.Audio
{
    // Finds a sound by name. Only real audio files count: a file at Resources/Audio/<key>.wav (or .ogg)
    // is the sound, and a name with no file is silent. Nothing is made up in code.
    public static class AudioLibrary
    {
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        /// <summary>Every sound name the game asks for: the sfx it fires plus the music contexts
        /// (AudioDirector prefixes "music_"). This is the shopping list of files to add; the
        /// asset manifest cross-checks it.</summary>
        public static readonly string[] AllKeys =
        {
            "sfx_click", "sfx_ui_open", "sfx_ui_close", "sfx_paper", "sfx_stamp_allow",
            "sfx_stamp_reject", "sfx_scan_ok", "sfx_scan_error", "sfx_knock", "sfx_door",
            "sfx_footstep", "sfx_flicker", "sfx_static", "sfx_glitch", "sfx_intercom",
            "sfx_bell", "sfx_pickup", "sfx_heartbeat", "sfx_stinger", "sfx_scare", "sfx_alarm",
            "music_menu", "music_checkpoint", "music_hallway", "music_classroom",
            "music_darkroom", "music_investigation", "music_anomaly", "music_final",
            "music_end_win", "music_end_lose", "music_end_secret",
        };

        /// <summary>The clip for a name, or null when there is no file for it (the caller stays silent).</summary>
        public static AudioClip Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out var clip) && clip != null) return clip;
            try
            {
                var loaded = Resources.Load<AudioClip>("Audio/" + key);
                if (loaded != null) Cache[key] = loaded;
                return loaded;
            }
            catch (Exception) { return null; }   // no resources: silent
        }

        /// <summary>Drops the references. The clips are imported assets, so nothing is destroyed.</summary>
        public static void ClearCache() => Cache.Clear();
    }
}
