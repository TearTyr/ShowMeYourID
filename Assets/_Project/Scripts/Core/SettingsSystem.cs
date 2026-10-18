using System;
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
