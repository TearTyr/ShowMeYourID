namespace Hollowbrook.Logic
{
    /// <summary>Reads the secret unlock code from the keys typed on the main menu (pure, no Unity). Keys must come
    /// one after another without a long pause; a wrong key starts the code again. Typed in full, the code unlocks
    /// the next hidden mode (Night 6 first, then Lockdown), one step each time.</summary>
    public class UnlockCodeReader
    {
        public const string Code = "wwssaaddggez";
        public const float GapSeconds = 3f;          // the next key must come within this long

        /// <summary>The keys the code uses, so the menu only has to watch these.</summary>
        public static readonly char[] Keys = { 'w', 's', 'a', 'd', 'g', 'e', 'z' };

        private int _progress;
        private float _lastKeyAt = -999f;

        /// <summary>How many letters of the code are right so far.</summary>
        public int Progress => _progress;

        /// <summary>One typed key. True the moment the whole code has been typed.</summary>
        public bool Feed(char key, float now)
        {
            if (now - _lastKeyAt > GapSeconds) _progress = 0;
            _lastKeyAt = now;
            _progress = ProgressAfter(_progress, key);
            if (_progress < Code.Length) return false;
            _progress = 0;
            return true;
        }

        // How much of the code the typed keys now match: the right key goes one further, a wrong key keeps
        // whatever end of the typed keys still matches the start of the code (so an extra w does no harm).
        private static int ProgressAfter(int progress, char key)
        {
            string typed = Code.Substring(0, progress) + key;
            for (int length = System.Math.Min(typed.Length, Code.Length); length > 0; length--)
                if (typed.EndsWith(Code.Substring(0, length))) return length;
            return 0;
        }
    }
}
