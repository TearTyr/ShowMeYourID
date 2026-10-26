using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The speech bubble's ONE clock: a line shows the bubble, the 2 s cooldown starts
    /// when the dialogue ENDS, and every new line advances the generation so a stale hide can
    /// never swallow the line that replaced it. Pure so the timing is unit-testable without an
    /// engine; CheckpointView.Speech renders whatever this reports.</summary>
    public class SpeechBubbleClock
    {
        /// <summary>Seconds between "the line ended" and "the bubble is removed".</summary>
        public const float HideDelaySeconds = 2f;

        /// <summary>Is the bubble on screen right now?</summary>
        public bool Visible { get; private set; }
        /// <summary>Strictly monotonic line counter — the view re-renders only on a change,
        /// and a pending hide from an older generation can be recognised as stale.</summary>
        public int Generation { get; private set; }
        public string Who { get; private set; } = "";
        public string Text { get; private set; } = "";
        /// <summary>Seconds since the last line arrived; frozen once the bubble has hidden.</summary>
        public float SecondsSinceLine { get; private set; }
        /// <summary>The cursor is inside the bubble: the hide countdown is suspended for as
        /// long as the player is reading it (the view feeds this from pointer enter/exit).</summary>
        public bool Hovering { get; private set; }

        /// <summary>Show one line. Cancels any pending hide (the countdown restarts).</summary>
        public void Say(string who, string text)
        {
            Who = who ?? "";
            Text = text ?? "";
            Generation++;
            SecondsSinceLine = 0f;
            Visible = true;
        }

        /// <summary>Pointer entered / left the bubble. Entering suspends the countdown (a line
        /// the player is reading must not vanish); leaving RESTARTS the cooldown from zero when
        /// the dialogue has already ended — and changes nothing while it is still talking,
        /// because the countdown only begins once the dialogue stops.</summary>
        public void SetHover(bool on)
        {
            Hovering = on;
            if (!on) SecondsSinceLine = 0f;   // the 2 s window opens as the cursor leaves
        }

        /// <summary>Advance the cooldown. Ticking a hidden bubble does nothing, a hovered one
        /// waits, and a negative dt is clamped — the clock never rewinds.</summary>
        public void Tick(float dt)
        {
            if (!Visible || Hovering) return;
            if (dt > 0f) SecondsSinceLine += dt;
            if (SecondsSinceLine >= HideDelaySeconds) Visible = false;
        }

        /// <summary>Cleanup path (room change, visitor walks off, new shift): hide instantly
        /// and drop the countdown, so no timer survives into the next beat.</summary>
        public void HideNow()
        {
            Visible = false;
            SecondsSinceLine = 0f;
        }
    }

    /// <summary>Text fitting for the bubble: a character-budget wrap estimate (the desk font is
    /// monospace-ish at a fixed size, so chars-per-line is the wireframe's own budget), the
    /// max-lines truncation, and the panel height for a line count. Pure, allocation-light and
    /// shared by CheckpointView.Speech and the headless layout audits.</summary>
    public static class SpeechBubbleLayout
    {
        private const string Ellipsis = "…";

        /// <summary>Wrap into lines of at most <paramref name="charsPerLine"/> characters.
        /// Whole words first, overlong words hard-split, explicit newlines always break.
        /// Empty/null text is one empty line; a budget below 1 is treated as 1.</summary>
        public static List<string> Wrap(string text, int charsPerLine)
        {
            int budget = charsPerLine < 1 ? 1 : charsPerLine;
            var lines = new List<string>();
            if (text == null) text = "";
            foreach (string paragraph in text.Split('\n'))
                WrapParagraph(paragraph, budget, lines);
            return lines;
        }

        private static void WrapParagraph(string paragraph, int budget, List<string> lines)
        {
            if (paragraph.Length == 0) { lines.Add(""); return; }   // an empty paragraph keeps its line
            string current = "";
            foreach (string word in paragraph.Split(' '))
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (candidate.Length <= budget) { current = candidate; continue; }
                if (current.Length > 0) { lines.Add(current); current = ""; }
                current = SplitOverlong(word, budget, lines);
            }
            lines.Add(current);
        }

        // Emits every full-budget chunk of an overlong word and returns the remainder.
        private static string SplitOverlong(string word, int budget, List<string> lines)
        {
            string rest = word;
            while (rest.Length > budget)
            {
                lines.Add(rest.Substring(0, budget));
                rest = rest.Substring(budget);
            }
            return rest;
        }

        /// <summary>How many lines <paramref name="text"/> needs at this budget.</summary>
        public static int CountLines(string text, int charsPerLine) => Wrap(text, charsPerLine).Count;

        /// <summary>The text as it will be displayed: unchanged when it fits, otherwise cut to
        /// <paramref name="maxLines"/> at a whole-word boundary with a trailing ellipsis, always
        /// inside the line budget. <paramref name="lines"/> reports the rendered line count.</summary>
        public static string Fit(string text, int charsPerLine, int maxLines, out int lines)
        {
            var wrapped = Wrap(text, charsPerLine);
            int cap = maxLines < 1 ? 1 : maxLines;
            if (wrapped.Count <= cap) { lines = wrapped.Count; return text ?? ""; }
            lines = cap;
            return Truncate(wrapped, cap, charsPerLine < 1 ? 1 : charsPerLine);
        }

        private static string Truncate(List<string> wrapped, int cap, int budget)
        {
            string joined = string.Join(" ", wrapped.GetRange(0, cap));
            int room = budget * cap - Ellipsis.Length;      // the ellipsis must fit the box too
            if (joined.Length > room) joined = joined.Substring(0, room);
            int lastSpace = joined.LastIndexOf(' ');
            if (lastSpace > 0) joined = joined.Substring(0, lastSpace);   // never half a word
            return joined.TrimEnd() + Ellipsis;
        }

        /// <summary>Panel height for a line count: paddings + the speaker tag + the text lines,
        /// floored at one text line so an empty bubble still has a body.</summary>
        public static float Height(int lineCount, float lineHeight, float tagHeight,
            float padTop, float padBottom)
        {
            int lines = lineCount < 1 ? 1 : lineCount;
            return padTop + tagHeight + lines * lineHeight + padBottom;
        }
    }
}
