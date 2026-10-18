using System;
using System.Collections.Generic;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    /// <summary>Owns the live <see cref="SettingsData"/>, persists it (PlayerPrefs, applied
    /// immediately — survives even when the run save is deleted) and broadcasts
    /// SettingsChanged so views/audio can react. Every setting here is wired to real
    /// behavior (volume gains, text registry rescale, motion/strobe guards, captions).</summary>
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
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Settings] prefs unreadable, using defaults: " + e.Message);
                Current = new SettingsData();
            }
        }

        public void SaveToPrefs()
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
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[Settings] prefs write failed: " + e.Message); }
        }

        /// <summary>Adopt settings loaded from a save file (only if the player has no local
        /// prefs yet — local prefs win to respect per-machine accessibility choices).</summary>
        public void AdoptFromSaveIfFresh(SettingsData saved)
        {
            if (saved == null) return;
            if (PlayerPrefs.HasKey(KeyPrefix + "master")) return;
            Current = saved.Clone();
            SaveToPrefs();
            Notify();
        }

        public void Apply(Action<SettingsData> mutate)
        {
            mutate?.Invoke(Current);
            Current.MasterVolume = Mathf.Clamp01(Current.MasterVolume);
            Current.MusicVolume = Mathf.Clamp01(Current.MusicVolume);
            Current.SfxVolume = Mathf.Clamp01(Current.SfxVolume);
            Current.TextScale = Mathf.Clamp(Current.TextScale, 0, 2);
            SaveToPrefs();
            Notify();
        }

        public void Notify() => _bus?.Publish(GameEvent.Of(EventIds.SettingsChanged, Current.Clone()));

        // ------------------------------------------------------------------ display
        // Screen state has exactly ONE owner (this class). The Options sheet only asks for
        // changes; AspectMatchGuard keeps every canvas readable when the mode changes.

        /// <summary>Every distinct usable mode the current display reports (deduped, sized).</summary>
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

        /// <summary>The resolutions the Options sheet offers: supported preferred heights plus
        /// whatever mode the player is on right now.</summary>
        public List<ModeSize> MenuChoices() => DisplayPolicy.MenuChoices(SupportedModes(), CurrentMode());

        /// <summary>Request a display change. Unsupported requests fall back to the nearest
        /// usable mode instead of failing; failures keep the previous mode.</summary>
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

        /// <summary>Writes Current.* to the Screen. The single call site for SetResolution.</summary>
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
                Debug.LogWarning("[Settings] display change failed — previous mode kept: " + e.Message);
            }
            if (persist) SaveToPrefs();
            Notify();
        }

        /// <summary>Restore the saved display at launch. A preference this machine can no longer
        /// honour (monitor swapped, projector…) is cleared rather than stranding the player.</summary>
        public void ApplyStoredDisplay()
        {
            if (Current.DisplayWidth <= 0 || Current.DisplayHeight <= 0) return; // no preference yet
            if (!DisplayPolicy.IsSupported(SupportedModes(), Current.DisplayWidth, Current.DisplayHeight))
            {
                Debug.LogWarning("[Settings] stored resolution " + Current.DisplayWidth + "x" +
                                 Current.DisplayHeight + " unsupported here — preference cleared.");
                Current.DisplayWidth = 0;
                Current.DisplayHeight = 0;
                SaveToPrefs();
                return;
            }
            ApplyCurrentDisplay(false);
        }

        /// <summary>Undo an unconfirmed change (the 15-second revert path).</summary>
        public void RestoreDisplaySnapshot(SettingsData snapshot)
        {
            if (snapshot == null) return;
            Current.Fullscreen = snapshot.Fullscreen;
            Current.DisplayWidth = snapshot.DisplayWidth;
            Current.DisplayHeight = snapshot.DisplayHeight;
            ApplyCurrentDisplay(false);
        }

        // convenience accessors used across systems
        public float MasterVolume => Current.MasterVolume;
        public float MusicVolume => Current.MasterVolume * Current.MusicVolume;
        public float SfxVolume => Current.MasterVolume * Current.SfxVolume;
        public float TextScaleMultiplier => SettingsData.TextScaleMultiplier(Current.TextScale);
        public bool ReduceMotion => Current.ReduceMotion;
        public bool PhotosensitivitySafe => Current.PhotosensitivitySafe;
        public bool Captions => Current.Captions;
    }
}
