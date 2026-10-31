using System;
using System.IO;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // Reads/writes the save file on disk. Created by CoreBootstrap.
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
                // crash-safe: Replace keeps the old file as .bak until the new one is in place;
                // a failure between the two operations never leaves the player save-less
                if (File.Exists(p)) File.Replace(tmp, p, p + ".bak");
                else
                {
                    if (File.Exists(p + ".bak")) File.Delete(p + ".bak");
                    File.Move(tmp, p);
                }
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

        private static SaveGame TryReadSave(string path)
        {
            try { return SaveFormat.Deserialize(File.ReadAllText(path)); }
            catch { return null; }
        }

        public SaveGame Read()
        {
            try
            {
                string p = SavePath;
                if (p == null) return null;
                var save = TryReadSave(p);
                if (save == null && File.Exists(p + ".bak")) save = TryReadSave(p + ".bak");
                if (save == null && File.Exists(p + ".tmp")) save = TryReadSave(p + ".tmp");
                if (save == null && File.Exists(p)) Debug.LogWarning("[SaveSystem] save file corrupt/unsupported - treated as no save.");
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
