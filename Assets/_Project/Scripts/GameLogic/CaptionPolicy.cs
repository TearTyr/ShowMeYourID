namespace Hollowbrook.Logic
{
    /// <summary>When the caption bar may speak (pure). The white text at the bottom is for moments that matter:
    /// an ordinary caption has to wait out a quiet gap after the last one and is dropped if it just repeats it.
    /// An urgent caption (the hunt, a warning) is always shown.</summary>
    public static class CaptionPolicy
    {
        public const float QuietGapSeconds = 9f;      // nothing ordinary is shown this soon after the last caption
        public const float RepeatSeconds = 45f;       // the same words are not shown again this soon

        public static bool Allow(float now, float lastShownAt, string text, string lastText, bool urgent)
        {
            if (urgent) return true;
            if (now - lastShownAt < QuietGapSeconds) return false;
            return !(text == lastText && now - lastShownAt < RepeatSeconds);
        }
    }
}
