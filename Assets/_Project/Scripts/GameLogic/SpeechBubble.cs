using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>The speech bubble's ONE record of what is on screen: a line shows the bubble and
    /// it STAYS until the next line replaces it or the desk clears it (visitor walks off, room
    /// change, new shift) — nothing times out, so the player can never lose a clue to a timer.
    /// Every new line advances the generation so the view re-renders only on a change. Pure so the
    /// rules are unit-testable without an engine; CheckpointView.Speech renders whatever this
    /// reports.</summary>
    public class SpeechBubbleClock
    {
        /// <summary>Is the bubble on screen right now?</summary>
        public bool Visible { get; private set; }
        /// <summary>Strictly monotonic line counter — the view re-renders only on a change.</summary>
        public int Generation { get; private set; }
        public string Who { get; private set; } = "";
        public string Text { get; private set; } = "";

        /// <summary>Show one line. It replaces whatever the bubble was showing.</summary>
        public void Say(string who, string text)
        {
            Who = who ?? "";
            Text = text ?? "";
            Generation++;
            Visible = true;
        }

        /// <summary>Cleanup path (room change, visitor walks off, new shift): hide instantly.</summary>
        public void HideNow()
        {
            Visible = false;
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
