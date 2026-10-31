using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: erasing data and writing the save file.
    public partial class GameRoot
    {
        /// <summary>F2×3 data reset: deletes the save file (run progress, records, endings
        /// seen) and clears in-memory run state. Accessibility/local settings are preserved
        /// on purpose (PlayerPrefs). Callers should confirm with the player first.</summary>
        public void ResetAllData()
        {
            Saves.Delete();
            Records = new RecordsData();
            Session = new GameSession();
            Bus.Publish(GameEvent.Of(EventIds.Notification, "All save data erased."));
            Debug.Log("[GameRoot] data reset complete (settings preserved).");
        }

        /// <summary>The single confirmation + reset path shared by the F2 x3 shortcut and the
        /// Options > RESET DATA button, so both can never drift apart. Settings (volumes,
        /// display, accessibility) live in PlayerPrefs and are deliberately untouched.</summary>
        public void RequestDataReset(System.Action afterReset = null)
        {
            if (Confirm == null) { ResetAllData(); if (afterReset != null) afterReset(); return; }
            Confirm.Ask("ERASE ALL DATA?",
                "Deletes your save: shift progress, records and unlocked endings.\n" +
                "Volumes, display and accessibility settings are kept.",
                () =>
                {
                    ResetAllData();
                    Audio.PlaySfx("sfx_static", 0.3f);
                    if (afterReset != null) afterReset();
                });
        }

        /// <summary>Save at safe boundaries (shift end / run end / quit). Corrupt-safe by design.</summary>
        public void PersistRun()
        {
            if (Queue != null && Queue.IsAttached)
            {
                Session.QueueState = Queue.BuildSnapshot();
                Session.QueueOverwhelmed = Queue.Collapsed;
            }
            Session.ShiftClockMinutes = _shiftMinutes;   // the resume must not rewind the night
            var save = new SaveGame
            {
                Session = Session,
                Settings = Settings.Current.Clone(),
                Records = Records,
                SavedAtLabel = "Night " + Session.CurrentShift + " · " + ClockString,
            };
            save.Session.VisitorIndexAtSave = Queue != null && Queue.IsAttached && !Queue.InfiniteSource
                ? Queue.RosterCursor : 0;
            Saves.Write(save);
        }
    }
}
