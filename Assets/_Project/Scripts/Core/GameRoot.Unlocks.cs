using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: which hidden modes are open. Night 6 opens when the story has been finished (see
    // Night6Unlocked). Lockdown opens when Night 6 has been beaten. The secret code on the main menu
    // opens them one at a time: the first time it is typed it opens Night 6, the next time Lockdown.
    // These unlocks live in PlayerPrefs (the story save is never touched by Night 6 or Lockdown) and
    // are cleared by the data reset.
    public partial class GameRoot
    {
        private const string CodeStageKey = "hollowbrook.unlock.codestage";
        private const string Night6BeatenKey = "hollowbrook.unlock.night6beaten";

        /// <summary>0 = the code has not been used, 1 = it opened Night 6, 2 = it opened Lockdown too.</summary>
        public int UnlockCodeStage => PlayerPrefs.GetInt(CodeStageKey, 0);

        public bool Night6Beaten => PlayerPrefs.GetInt(Night6BeatenKey, 0) == 1;

        /// <summary>Lockdown opens after Night 6 has been beaten, or when the secret code has opened it.</summary>
        public bool LockdownUnlocked => Night6Beaten || UnlockCodeStage >= 2;

        private void MarkNight6Beaten()
        {
            PlayerPrefs.SetInt(Night6BeatenKey, 1);
            PlayerPrefs.Save();
        }

        private void ClearUnlocks()
        {
            PlayerPrefs.DeleteKey(CodeStageKey);
            PlayerPrefs.DeleteKey(Night6BeatenKey);
            PlayerPrefs.Save();
        }

        /// <summary>The secret code was typed. Opens the next locked mode and says which one.</summary>
        public string ApplyUnlockCode()
        {
            if (!Night6Unlocked)
            {
                PlayerPrefs.SetInt(CodeStageKey, 1);
                PlayerPrefs.Save();
                return "NIGHT 6 UNLOCKED.";
            }
            if (!LockdownUnlocked)
            {
                PlayerPrefs.SetInt(CodeStageKey, 2);
                PlayerPrefs.Save();
                return "LOCKDOWN UNLOCKED.";
            }
            return "NOTHING LEFT TO UNLOCK.";
        }
    }
}
