using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    // The window talk router: what a TYPED line spoken through the glass means.
    //
    // The desk's dialogue text box feeds this pure classifier; the view then performs the
    // routed action (ask a verification question through the existing QuestionSystem budget,
    // or let the visitor answer in character). Nothing here touches widgets, holds state of
    // the run, or rolls dice: variation is an explicit counter the caller owns, so every
    // reply is a deterministic function of (context, text, variant) and LogicTests can pin
    // the whole table.
    //
    // Contracts honoured:
    //  * a typed line that maps onto a verification question does NOT bypass the budget —
    //    the view routes it through QuestionSystem.Budget exactly like the panel buttons,
    //    so the two-question cap and the no-repeats rule hold for typed speech too;
    //  * small talk, greetings and refusals never consume budget, never clue, never stamp;
    //  * empty input is rejected here (Rejected) and the view keeps the typed text.
    public enum SpokenTopic
    {
        Rejected = 0,     // nothing sayable (blank / whitespace only)
        Question,         // maps onto a verification question (Question field set)
        NameAsk,          // "who are you / your name"
        IdAsk,            // "your id / number"
        Greeting,
        Thanks,
        Hostile,
        SmallTalk,
        Fallback,         // sayable, but the visitor only stares
    }

    public struct SpokenReply
    {
        public SpokenTopic Topic;
        public QuestionId Question;   // valid when Topic == Question
        public string Text;           // the visitor's spoken reply ("" when the view answers)
        public string RejectReason;   // set when Topic == Rejected
    }

    public struct SpokenContext
    {
        public bool HasVisitor;
        public bool IsAdmin;
        public string ClaimedName;    // what the badge says
        public string ClaimedId;      // badge number
        public string ClaimedRoom;    // e.g. "ROOM 202"
        public int Variant;           // caller-owned counter for reply variation
    }

    public static class SpokenChat
    {
        public const int MaxChars = 140;

        // lower-case, punctuation-flattened, whitespace-collapsed; "" for blank input.
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c)) { if (sb.Length > 0 && sb[sb.Length - 1] != ' ') sb.Append(' '); }
                else if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                // punctuation dropped on purpose: "course?" == "course"
            }
            string s = sb.ToString().Trim(' ');
            return s;
        }

        /// <summary>True when the typed line is a verification question; the matching
        /// question id rides out so the view can spend budget through QuestionSystem.</summary>
        public static bool TryMatchQuestion(string normalized, out QuestionId q)
        {
            q = QuestionId.Course;
            if (string.IsNullOrEmpty(normalized)) return false;
            if (ContainsAny(normalized, "first classroom", "classroom", "class room", "homeroom",
                    "first class", "which room", "what room", "room number"))
            { q = QuestionId.FirstClassroom; return true; }
            if (ContainsAny(normalized, "regular", "transferee", "transfer student", "status",
                    "standing", "enrolled", "enrolment", "enrollment", "late list"))
            { q = QuestionId.Status; return true; }
            if (ContainsAny(normalized, "what year", "which year", "year are you", "year level",
                    "freshman", "sophomore", "junior", "senior", "first year", "second year"))
            { q = QuestionId.Year; return true; }
            if (ContainsAny(normalized, "course", "major", "study", "studies", "program",
                    "programme", "department", "subject"))
            { q = QuestionId.Course; return true; }
            // a bare "year?" / "room?" still reads as the question
            if (normalized == "year" || normalized == "year?") { q = QuestionId.Year; return true; }
            if (normalized == "room" || normalized == "room?") { q = QuestionId.FirstClassroom; return true; }
            return false;
        }

        public static SpokenTopic Classify(string normalized)
        {
            if (string.IsNullOrEmpty(normalized)) return SpokenTopic.Rejected;
            if (TryMatchQuestion(normalized, out _)) return SpokenTopic.Question;
            if (ContainsAny(normalized, "your name", "who are you", "name please", "your name?"))
                return SpokenTopic.NameAsk;
            if (ContainsAny(normalized, "your id", "id number", "student number", "badge number",
                    "registry number", "your number"))
                return SpokenTopic.IdAsk;
            if (ContainsAny(normalized, "thank", "thanks", "appreciate"))
                return SpokenTopic.Thanks;
            if (ContainsAny(normalized, "liar", "lying", "shut up", "get out", "threat", "kill",
                    "hurt you", "fraud", "fake"))
                return SpokenTopic.Hostile;
            if (ContainsAny(normalized, "hello", "good evening", "good night", "evening", "hey",
                    "greetings", "hi"))
                return SpokenTopic.Greeting;
            if (ContainsAny(normalized, "weather", "rain", "cold", "tired", "sleep", "night shift",
                    "long night", "coffee", "curfew", "quiet"))
                return SpokenTopic.SmallTalk;
            return SpokenTopic.Fallback;
        }

        /// <summary>The single entry point the view calls on submit.</summary>
        public static SpokenReply Route(SpokenContext ctx, string raw)
        {
            string n = Normalize(raw);
            if (n.Length > MaxChars) n = n.Substring(0, MaxChars);
            var reply = new SpokenReply { Topic = Classify(n) };
            if (!ctx.HasVisitor)
            {
                reply.Topic = SpokenTopic.Rejected;
                reply.RejectReason = "NO ONE IS AT THE WINDOW.";
                return reply;
            }
            switch (reply.Topic)
            {
                case SpokenTopic.Question:
                    TryMatchQuestion(n, out reply.Question);
                    reply.Text = "";   // the view answers through QuestionSystem (budget!)
                    return reply;
                case SpokenTopic.NameAsk:
                    reply.Text = ctx.IsAdmin
                        ? Pick(ctx.Variant,
                            "The administration does not introduce itself at a window.",
                            "My name is on the memorandum in your hand. Read it.")
                        : Pick(ctx.Variant,
                            Name(ctx) + ". It is on the card, too.",
                            Name(ctx) + ". Ask the card, it prints it better.");
                    return reply;
                case SpokenTopic.IdAsk:
                    reply.Text = ctx.IsAdmin
                        ? "Administrative clearance is not a number you write down."
                        : (string.IsNullOrEmpty(ctx.ClaimedId)
                            ? "…The card says what it says."
                            : ctx.ClaimedId + ". It is printed right there.");
                    return reply;
                case SpokenTopic.Greeting:
                    reply.Text = ctx.IsAdmin
                        ? Pick(ctx.Variant,
                            "Evening. We need to talk about the memorandum.",
                            "Save the pleasantries, officer.")
                        : Pick(ctx.Variant,
                            "Evening, officer.",
                            "…Hi. Night class, sorry about the hour.",
                            "Evening. Cold one tonight.");
                    return reply;
                case SpokenTopic.Thanks:
                    reply.Text = ctx.IsAdmin ? "Do your job and we will get along."
                        : Pick(ctx.Variant, "…Sure. Thanks for letting me through.", "Uh. You too, officer.");
                    return reply;
                case SpokenTopic.Hostile:
                    reply.Text = ctx.IsAdmin
                        ? "Careful, officer. I sign your evaluations."
                        : Pick(ctx.Variant,
                            "I am not lying. Check the papers again.",
                            "…Please. I just want to get to class.");
                    return reply;
                case SpokenTopic.SmallTalk:
                    reply.Text = ctx.IsAdmin
                        ? "The night is long and you are wasting it."
                        : Pick(ctx.Variant,
                            "Rain since six. The hallway smells of wet wool.",
                            "Everyone is tired. Curfew does that.",
                            "Quiet night. Too quiet, if you ask me.");
                    return reply;
                default:
                    reply.Text = ctx.IsAdmin
                        ? "If that is all, I will return to the office."
                        : Pick(ctx.Variant,
                            "…Sorry? The intercom crackles.",
                            "I do not follow, officer.",
                            "…(they glance at the hallway) Can I go in?");
                    return reply;
            }
        }

        private static string Name(SpokenContext ctx) =>
            string.IsNullOrEmpty(ctx.ClaimedName) ? "…It is on the card" : ctx.ClaimedName;

        private static string Pick(int variant, params string[] options)
        {
            if (options == null || options.Length == 0) return "";
            int v = variant % options.Length;
            if (v < 0) v += options.Length;
            return options[v];
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }
    }

    // One entry of the window conversation, as shown in the talk log.
    public struct TranscriptEntry
    {
        public string Who;        // YOU / VISITOR / ADMIN / DESK
        public string Text;
        public bool FromPlayer;
        public TranscriptEntry(string who, string text, bool fromPlayer)
        { Who = who; Text = text; FromPlayer = fromPlayer; }
    }

    // The bounded conversation memory of the current inspection: what you said through the
    // glass and what came back. Pure list, capped, cleared per visitor by the view.
    public class WindowTranscript
    {
        public const int Capacity = 24;
        private readonly List<TranscriptEntry> _entries = new List<TranscriptEntry>();

        public int Count => _entries.Count;
        public IReadOnlyList<TranscriptEntry> Entries => _entries;

        public void Add(string who, string text, bool fromPlayer)
        {
            _entries.Add(new TranscriptEntry(who, text, fromPlayer));
            while (_entries.Count > Capacity) { _entries.RemoveAt(0); _dropped++; }
        }

        /// <summary>The newest <paramref name="n"/> entries, oldest first.</summary>
        public IList<TranscriptEntry> Last(int n)
        {
            int take = n < _entries.Count ? n : _entries.Count;
            return _entries.GetRange(_entries.Count - take, take);
        }

        public int Dropped => _dropped;
        private int _dropped;

        public void Clear() { _entries.Clear(); _dropped = 0; }
    }
}
