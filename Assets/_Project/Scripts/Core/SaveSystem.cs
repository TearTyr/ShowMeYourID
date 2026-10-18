using System;
using System.IO;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    /// <summary>File-backed save/load (Application.persistentDataPath). The format itself is
    /// the pure-logic <see cref="SaveFormat"/> (versioned, checksum-guarded, round-trip
    /// tested). Every read path is exception-safe: a missing/corrupt save reports "no save"
    /// instead of throwing, so the main menu can never get stuck on a bad Continue.</summary>
    public class SaveSystem
    {
        private const string FileName = "hollowbrook_save.txt";
        private readonly EventBus _bus;

        public string SaveDirectory
        {
            get
            {
                try
                {
                    string dir = Application.persistentDataPath;
                    if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
                    Directory.CreateDirectory(dir);
                    return dir;
                }
                catch (Exception e)
                {
                    Debug.LogError("[SaveSystem] save directory unavailable: " + e.Message);
                    return null;
                }
            }
        }

        public string SavePath
        {
            get
            {
                string dir = SaveDirectory;
                return dir == null ? null : Path.Combine(dir, FileName);
            }
        }

        public SaveSystem(EventBus bus) { _bus = bus; }

        public bool HasSave()
        {
            try
            {
                string p = SavePath;
                return p != null && File.Exists(p);
            }
            catch (Exception) { return false; }
        }

        public bool Write(SaveGame save)
        {
            try
            {
                string p = SavePath;
                if (p == null || save == null) return false;
                string tmp = p + ".tmp";
                File.WriteAllText(tmp, SaveFormat.Serialize(save));
                if (File.Exists(p)) File.Delete(p);
                File.Move(tmp, p);
                _bus?.Publish(GameEvent.Of(EventIds.GameSaved));
                Debug.Log("[SaveSystem] saved to " + p);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] write failed: " + e.Message);
                return false;
            }
        }

        /// <summary>Returns null when there is no usable save.</summary>
        public SaveGame Read()
        {
            try
            {
                string p = SavePath;
                if (p == null || !File.Exists(p)) return null;
                var save = SaveFormat.Deserialize(File.ReadAllText(p));
                if (save == null) Debug.LogWarning("[SaveSystem] save file corrupt/unsupported — treated as no save.");
                else _bus?.Publish(GameEvent.Of(EventIds.GameLoaded));
                return save;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] read failed: " + e.Message);
                return null;
            }
        }

        public void Delete()
        {
            try
            {
                string p = SavePath;
                if (p != null && File.Exists(p)) File.Delete(p);
            }
            catch (Exception e) { Debug.LogWarning("[SaveSystem] delete failed: " + e.Message); }
        }
    }
}
