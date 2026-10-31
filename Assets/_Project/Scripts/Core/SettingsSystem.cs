using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Owns player preferences and the screen mode. Created by CoreBootstrap.
    public class SettingsSystem
    {
        private const string KeyPrefix = "smyid.settings.";
        private readonly EventBus _bus;

        public SettingsData Current { get; private set; } = new SettingsData();

        public SettingsSystem(EventBus bus)
        {
            _bus = bus;
            LoadFromPrefs();
        }

        public void LoadFromPrefs()
        {
            try
            {
                Current = new SettingsData
                {
                    MasterVolume = PlayerPrefs.GetFloat(KeyPrefix + "master", 0.8f),
                    MusicVolume = PlayerPrefs.GetFloat(KeyPrefix + "music", 0.6f),
                    SfxVolume = PlayerPrefs.GetFloat(KeyPrefix + "sfx", 0.8f),
                    TextScale = PlayerPrefs.GetInt(KeyPrefix + "textScale", 1),
                    ReduceMotion = PlayerPrefs.GetInt(KeyPrefix + "reduceMotion", 0) == 1,
                    PhotosensitivitySafe = PlayerPrefs.GetInt(KeyPrefix + "photosafe", 0) == 1,
                    Captions = PlayerPrefs.GetInt(KeyPrefix + "captions", 1) == 1,
                    Fullscreen = PlayerPrefs.GetInt(KeyPrefix + "fullscreen", 1) == 1,
                    DisplayWidth = PlayerPrefs.GetInt(KeyPrefix + "displayW", 0),
                    DisplayHeight = PlayerPrefs.GetInt(KeyPrefix + "displayH", 0),
                };
                if (Current.SfxVolume <= 0f && !PlayerPrefs.HasKey(KeyPrefix + "sfxExplicit"))
                {
                    Debug.LogWarning("[Settings] stored SFX volume was 0 with no explicit user change - restoring audible default 0.8.");
                    Current.SfxVolume = 0.8f;
                }
                Debug.Log("[Settings] loaded master=" + Current.MasterVolume + " music=" + Current.MusicVolume +
                          " sfx=" + Current.SfxVolume + " text=" + Current.TextScale +
                          " fullscreen=" + Current.Fullscreen + " res=" + Current.DisplayWidth + "x" + Current.DisplayHeight);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] prefs unreadable, using defaults: " + e.Message);
                Current = new SettingsData();
            }
        }

        public void SaveToPrefs(bool persist = true)
        {
            try
            {
                PlayerPrefs.SetFloat(KeyPrefix + "master", Current.MasterVolume);
                PlayerPrefs.SetFloat(KeyPrefix + "music", Current.MusicVolume);
                PlayerPrefs.SetFloat(KeyPrefix + "sfx", Current.SfxVolume);
                PlayerPrefs.SetInt(KeyPrefix + "textScale", Current.TextScale);
                PlayerPrefs.SetInt(KeyPrefix + "reduceMotion", Current.ReduceMotion ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "photosafe", Current.PhotosensitivitySafe ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "captions", Current.Captions ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "fullscreen", Current.Fullscreen ? 1 : 0);
                PlayerPrefs.SetInt(KeyPrefix + "displayW", Current.DisplayWidth);
                PlayerPrefs.SetInt(KeyPrefix + "displayH", Current.DisplayHeight);
                PlayerPrefs.SetInt(KeyPrefix + "sfxExplicit", 1);   // any Apply() is an explicit user write
                if (persist) PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[Settings] prefs write failed: " + e.Message); }
        }

        public void AdoptFromSaveIfFresh(SettingsData saved)
        {
            if (saved == null) return;
            if (PlayerPrefs.HasKey(KeyPrefix + "master")) return;
            Current = saved.Clone();
            SaveToPrefs();
            Notify();
        }

        /// <summary>Flush to disk: called when the settings sheet closes and on quit, not
        /// on every slider tick (a volume drag must not write the prefs file every frame).</summary>
        public void Persist() => PlayerPrefs.Save();

        public void Apply(Action<SettingsData> mutate)
        {
            mutate?.Invoke(Current);
            Current.MasterVolume = Mathf.Clamp01(Current.MasterVolume);
            Current.MusicVolume = Mathf.Clamp01(Current.MusicVolume);
            Current.SfxVolume = Mathf.Clamp01(Current.SfxVolume);
            Current.TextScale = Mathf.Clamp(Current.TextScale, 0, 2);
            SaveToPrefs(false);
            Notify();
        }

        public void Notify() => _bus?.Publish(GameEvent.Of(EventIds.SettingsChanged, Current.Clone()));

        public List<ModeSize> SupportedModes()
        {
            var all = new List<ModeSize>();
            try
            {
                foreach (var r in Screen.resolutions) all.Add(new ModeSize(r.width, r.height));
            }
            catch (Exception e) { Debug.LogWarning("[Settings] Screen.resolutions unavailable: " + e.Message); }
            return DisplayPolicy.Supported(all);
        }

        public ModeSize CurrentMode() => new ModeSize(Screen.width, Screen.height);

        public List<ModeSize> MenuChoices() => DisplayPolicy.MenuChoices(SupportedModes(), CurrentMode());

        public ModeSize SetDisplay(int w, int h, bool fullscreen, bool persist)
        {
            Current.Fullscreen = fullscreen;
            var want = new ModeSize(w, h);
            if (!DisplayPolicy.IsSupported(SupportedModes(), w, h))
                want = DisplayPolicy.Fallback(SupportedModes(), want);
            Current.DisplayWidth = want.W;
            Current.DisplayHeight = want.H;
            ApplyCurrentDisplay(persist);
            return want;
        }

        public void ApplyCurrentDisplay(bool persist)
        {
            try
            {
                Screen.fullScreenMode = Current.Fullscreen
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.Windowed;
                if (Current.DisplayWidth > 0 && Current.DisplayHeight > 0)
                    Screen.SetResolution(Current.DisplayWidth, Current.DisplayHeight, Screen.fullScreenMode);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] display change failed - previous mode kept: " + e.Message);
            }
            if (persist) SaveToPrefs();
            Notify();
        }

        public void ApplyStoredDisplay()
        {
            if (Current.DisplayWidth <= 0 || Current.DisplayHeight <= 0) return; // no preference yet
            if (!DisplayPolicy.IsSupported(SupportedModes(), Current.DisplayWidth, Current.DisplayHeight))
            {
                Debug.LogWarning("[Settings] stored resolution " + Current.DisplayWidth + "x" +
                                 Current.DisplayHeight + " unsupported here - preference cleared.");
                Current.DisplayWidth = 0;
                Current.DisplayHeight = 0;
                SaveToPrefs();
                return;
            }
            ApplyCurrentDisplay(false);
        }

        public void RestoreDisplaySnapshot(SettingsData snapshot)
        {
            if (snapshot == null) return;
            Current.Fullscreen = snapshot.Fullscreen;
            Current.DisplayWidth = snapshot.DisplayWidth;
            Current.DisplayHeight = snapshot.DisplayHeight;
            ApplyCurrentDisplay(false);
        }

        public float MasterVolume => Current.MasterVolume;
        public float MusicVolume => Current.MasterVolume * Current.MusicVolume;
        public float SfxVolume => Current.MasterVolume * Current.SfxVolume;
        public float TextScaleMultiplier => SettingsData.TextScaleMultiplier(Current.TextScale);
        public bool ReduceMotion => Current.ReduceMotion;
        public bool PhotosensitivitySafe => Current.PhotosensitivitySafe;
        public bool Captions => Current.Captions;
    }
}
