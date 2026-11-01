using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    // The Night-5 administrator's questions and answers (pure rules + script, no UnityEngine).
    //
    // How it plays: the administrator is at the window. Press T (or the bar under the stamps)
    // to talk. He gets no student questions: the hub lists what you can confront him with, built
    // from his papers and the clues you found. Each topic gets one answer and one follow-up that
    // catches him out, then you are back at the hub. From any screen you can:
    //   - step back: close the panel and go to the stamps (T or the bar brings it back).
    // Nothing here refuses him: only the DENY stamp does (the hunt begins). He can be asked
    // the same questions over and over; a lie counts once however often it is pressed.
    // Every follow-up is a lie EXPOSED, and each one shortens the night that follows
    // (GameRoot.BuildHuntTuning). Talking never stamps and never gives clues.
    //
    // To change what he says: edit BuildScript below. Every line is plain text; the words in
    // {braces} are filled in from the papers on the desk (see Resolve).

    /// <summary>What must be true before an option is shown.</summary>
    public enum AdminTalkCond
    {
        None = 0,
        CluePhoto,        // found the old school photo
        ClueCabinet,      // found the work order in the cabinet
    }

    /// <summary>What picking an option does besides moving to the next node.</summary>
    public enum AdminTalkAction
    {
        None = 0,
        StepBack,   // close the panel, the stamps are yours (the conversation can be reopened)
    }

    public class AdminTalkOption
    {
        public string Id = "";
        public string Label = "";
        public string To = "";                          // node id to go to
        public AdminTalkCond Cond = AdminTalkCond.None;
        public AdminTalkAction Action = AdminTalkAction.None;
        public bool Repeatable;                         // false = counted once in the save (lies), still offered again
    }

    public class AdminTalkNode
    {
        public string Id = "";
        public string Text = "";                        // what he says
        public string RepeatText;                       // what he says the next times (hub only)
        public List<AdminTalkOption> Options = new List<AdminTalkOption>();
    }

    /// <summary>The facts the script may use: the real papers and answers on the desk.</summary>
    public struct AdminTalkContext
    {
        public string BadgeName, MemoName, BadgeNumber, MemoNumber, AnswerClassroom;
        public bool CluePhoto, ClueCabinet;
        public bool Night6;            // the Challenge: he is back, with a new set of answers
    }

    /// <summary>Where the conversation stands. Saved with the run as one short string.</summary>
    public class AdminTalkState
    {
        public string Node = "";     // "" = not opened yet
        public int UsedMask;         // one bit per one-shot option
        public int Visits;           // how many times the hub was shown
        public bool Completed;       // a verdict landed on him: the conversation is over

        public string Encode() => Node + "," + UsedMask + "," + Visits + "," + (Completed ? 1 : 0);

        public static AdminTalkState Decode(string s)
        {
            var st = new AdminTalkState();
            if (string.IsNullOrEmpty(s)) return st;
            var p = s.Split(',');
            if (p.Length < 4) return st;   // torn or old value: a fresh conversation
            st.Node = p[0];
            int.TryParse(p[1], out st.UsedMask);
            int.TryParse(p[2], out st.Visits);
            st.Completed = p[3] == "1";
            if (AdminTalk.NodeById(st.Node) == null) st.Node = "";
            return st;
        }
    }

    /// <summary>The result of picking an option.</summary>
    public class AdminTalkStep
    {
        public bool Completed;                          // the panel closes (stepped back, or already over)
        public AdminTalkAction Action;                  // how it ended (StepBack)
        public string NodeId = "";
        public string Speaker = AdminTalk.Speaker;
        public string Text = "";                        // what he says next
    }

    public static class AdminTalk
    {
        public const string Speaker = "V. MARLOWE";
        public const string HubId = "hub";
        public const string Night6HubId = "n6_hub";

        /// <summary>Where the conversation starts: night 6 has its own questions.</summary>
        public static string HubFor(AdminTalkContext ctx) => ctx.Night6 ? Night6HubId : HubId;

        private static readonly List<AdminTalkNode> Script = BuildScript();

        public static IReadOnlyList<AdminTalkNode> Nodes => Script;

        public static AdminTalkNode NodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var n in Script) if (n.Id == id) return n;
            return null;
        }

        // ------------------------------------------------------------ the script
        private static AdminTalkOption O(string id, string label, string to,
            AdminTalkCond cond = AdminTalkCond.None, bool repeatable = false,
            AdminTalkAction action = AdminTalkAction.None) =>
            new AdminTalkOption { Id = id, Label = label, To = to, Cond = cond, Repeatable = repeatable, Action = action };

        private static AdminTalkOption Back(string hub = HubId) =>
            O("back", "[Go back to the other questions.]", hub, AdminTalkCond.None, true);
        private static AdminTalkOption StepBack() =>
            O("end", "[Step back from the glass. The stamp is yours.]", "", AdminTalkCond.None, true, AdminTalkAction.StepBack);

        private static AdminTalkNode Node(string id, string text, params AdminTalkOption[] options)
        {
            var n = new AdminTalkNode { Id = id, Text = text };
            n.Options.AddRange(options);
            return n;
        }

        // A topic: his first answer (with a follow-up question), then his second answer.
        private static void Topic(List<AdminTalkNode> s, string id, string first, string pressLabel, string second,
            string hub = HubId)
        {
            s.Add(Node(id, first, O("press_" + id, pressLabel, id + "2"), Back(hub), StepBack()));
            s.Add(Node(id + "2", second, Back(hub), StepBack()));
        }

        private static List<AdminTalkNode> BuildScript()
        {
            var s = new List<AdminTalkNode>();
            var hub = Node(HubId,
                "There have been… complaints about your performance. Let's not add to them. Open the checkpoint. That is an order from the district.",
                O("ask_name", "The badge says {badge_name}. The memo says {memo_name}. Explain that.", "name"),
                O("ask_num", "Badge {badge_num}. The memo lists {memo_num}. Which one is yours?", "num"),
                O("ask_face", "The face at my glass is not the face on this badge.", "face"),
                O("ask_server", "You have no class in this building. Where do you go first?", "server"),
                O("ask_photo", "The old school photo: a staff member who never aged. That's you.", "photo",
                  AdminTalkCond.CluePhoto),
                O("ask_cabinet", "The work order names the server room. You came to wake it.", "cabinet",
                  AdminTalkCond.ClueCabinet),
                StepBack());
            hub.RepeatText = "Still here. Still watching me. Choose your next sentence carefully, custodian.";
            s.Add(hub);

            Topic(s, "name",
                "Marlowe, Vance. District paperwork is a river, custodian; names bend in it. The memo confirms an administrator is expected tonight. It confirms me.",
                "Then read the memo with me. Out loud.",
                "…'R. Vance - DA-1147.' Fine. Vance is my supervisor's line-item. The district sends a face, not a surname. You stamp for the face in front of you.");
            Topic(s, "num",
                "DA-0233 is my field number. DA-1147 is my office number. The district issues both. I did not expect a night custodian to have opinions about its numbering scheme.",
                "The rule says badge and memo must match. They don't.",
                "Then phone the district and confirm me. Go on. I will wait here, in the cold, while you ask my own office whether I exist. …No? You memorised the note. How diligent.");
            Topic(s, "face",
                "Glass distorts. Fluorescents distort. Tired eyes distort most of all. I have driven four hours; my badge photograph is six years old. We are both something the camera misremembers.",
                "Step into the light. Slowly.",
                "…I will not perform for a doorbell camera. Note that I said doorbell. Note that I know what your booth is.");
            Topic(s, "server",
                "\"{answer_server}\" The server room holds the identification records I am here to audit. Audits are conducted from inside the room, custodian.",
                "Then audit it from outside the glass.",
                "…Everyone else. Yes. The students with their little cards, and you, with your little window. All of you so certain the glass is a wall. It is a door, custodian. It has always been a door.");
            Topic(s, "photo",
                "…A photograph. From a year the district does not discuss, in a hallway the district does not own. You carry it around like a lantern. Put it down, custodian. It lights nothing but your own face.",
                "You haven't aged a day. Neither has the fan behind your sealed door.",
                "…You listen to walls. The building talks to whoever is still awake at two in the morning. I am not the building, custodian. I am the person it is afraid of. Open the gate and let me quiet it.");
            Topic(s, "cabinet",
                "The server room is district property. I am the district. The work order you are waving is my own handwriting, filed before your predecessor learned to lock cabinets.",
                "The seal is broken from the inside. Not by your handwriting.",
                "…Seals fail. Wood splits. Night custodians imagine. I have no interest in what the room wants; I have an interest in what it remembers, and so, custodian, do you.");

            // Night 6: the same man, a new set of questions. His lies are pressed the same way.
            var hub6 = Node(Night6HubId,
                "You again. I told you I do not give up, custodian. Last night you were lucky, and you know it. I am asking once, politely: make the right decision. Open the gate.",
                O("ask_n6_return", "You left. You said you would be back. How are you at my window again?", "n6_return"),
                O("ask_n6_choice", "What is the 'right decision'?", "n6_choice"),
                O("ask_n6_system", "What does the identification system want?", "n6_system"),
                O("ask_n6_hunt", "I am denying you again.", "n6_hunt"),
                StepBack());
            hub6.RepeatText = "Still talking. Still afraid of the answer. Ask what you came to ask.";
            s.Add(hub6);
            Topic(s, "n6_return",
                "Doors are a courtesy, custodian. I was never gone. I was in the walls, in the files, in every face you stamped last week.",
                "Then why knock at all?",
                "...Because a gate opened by choice stays open. A gate forced only breaks, and I would rather keep what is behind it intact. Choose.",
                Night6HubId);
            Topic(s, "n6_choice",
                "To stop pretending the glass is a wall. Five nights you checked cards for a system that prints them. Who do you think wrote yours?",
                "My name is not in any drawer.",
                "...Look again. Every name in this building is on file, including the one on your pay slip. You were enrolled before you ever walked in.",
                Night6HubId);
            Topic(s, "n6_system",
                "To be complete. Every face on file, every desk filled, nobody left outside the list.",
                "And the people who are not on your list?",
                "...They are the exceptions, and exceptions are what I was sent to correct. You are the last one, custodian. The last one is always the quietest.",
                Night6HubId);
            Topic(s, "n6_hunt",
                "Then I will be more patient, and less polite. Last night I stalked. Tonight I hunt.",
                "I have a flashlight.",
                "...Light is the only thing I fear. It is also the only thing you have. Keep it charged, custodian. I will be quicker, and I will not stop.",
                Night6HubId);
            return s;
        }

        // ------------------------------------------------------------ playing it
        /// <summary>Which options are shown now: the condition is met. Used ones stay, so he can be
        /// asked again and again.</summary>
        public static List<AdminTalkOption> OptionsFor(AdminTalkState st, AdminTalkContext ctx)
        {
            var node = NodeById(string.IsNullOrEmpty(st.Node) ? HubFor(ctx) : st.Node) ?? NodeById(HubFor(ctx));
            var list = new List<AdminTalkOption>();
            foreach (var o in node.Options)
            {
                if (CondMet(o.Cond, ctx)) list.Add(o);
            }
            if (list.Count == 0) list.Add(StepBack());   // never a dead end
            return list;
        }

        public static bool CondMet(AdminTalkCond c, AdminTalkContext ctx)
        {
            switch (c)
            {
                case AdminTalkCond.CluePhoto: return ctx.CluePhoto;
                case AdminTalkCond.ClueCabinet: return ctx.ClueCabinet;
                default: return true;
            }
        }

        /// <summary>How many of his lies the player has caught: one per follow-up taken.</summary>
        public static int LiesExposed(AdminTalkState st)
        {
            if (st == null) return 0;
            int count = 0;
            foreach (var n in Script)
                foreach (var o in n.Options)
                    if (o.Id.StartsWith("press_") && (st.UsedMask & (1 << OneShotIndex(o.Id))) != 0) count++;
            return count;
        }

        /// <summary>Open (or re-open) the conversation at the hub.</summary>
        public static AdminTalkStep Open(AdminTalkState st, AdminTalkContext ctx)
        {
            if (st.Completed) return new AdminTalkStep { Completed = true };
            if (string.IsNullOrEmpty(st.Node)) st.Visits++;
            st.Node = HubFor(ctx);
            return Say(st, ctx, st.Node);
        }

        /// <summary>Pick an option. A wrong or hidden option does nothing.</summary>
        public static AdminTalkStep Choose(AdminTalkState st, AdminTalkContext ctx, string optionId)
        {
            if (st.Completed) return new AdminTalkStep { Completed = true };
            AdminTalkOption chosen = null;
            foreach (var o in OptionsFor(st, ctx)) if (o.Id == optionId) { chosen = o; break; }
            string nodeId = string.IsNullOrEmpty(st.Node) ? HubFor(ctx) : st.Node;
            if (chosen == null) return Say(st, ctx, nodeId);

            if (!chosen.Repeatable) st.UsedMask |= 1 << OneShotIndex(chosen.Id);
            if (chosen.Action != AdminTalkAction.None)
            {
                st.Node = HubFor(ctx);   // reopened later, he is back at the start of the topics
                return new AdminTalkStep { Completed = true, Action = chosen.Action };
            }
            if (chosen.To == HubId || chosen.To == Night6HubId) st.Visits++;
            st.Node = chosen.To;
            return Say(st, ctx, chosen.To);
        }

        private static AdminTalkStep Say(AdminTalkState st, AdminTalkContext ctx, string nodeId)
        {
            var node = NodeById(nodeId) ?? NodeById(HubId);
            string text = node.RepeatText != null && st.Visits > 1 ? node.RepeatText : node.Text;
            return new AdminTalkStep { NodeId = node.Id, Text = Resolve(text, ctx) };
        }

        /// <summary>Fill the {tokens} from the papers on the desk.</summary>
        public static string Resolve(string text, AdminTalkContext ctx)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("{badge_name}", ctx.BadgeName ?? "?")
                .Replace("{memo_name}", ctx.MemoName ?? "?")
                .Replace("{badge_num}", ctx.BadgeNumber ?? "?")
                .Replace("{memo_num}", ctx.MemoNumber ?? "?")
                .Replace("{answer_server}", ctx.AnswerClassroom ?? "…");
        }

        // The save keeps one bit per counted option (the lies); this is that option's bit number.
        private static int OneShotIndex(string optionId)
        {
            int i = 0;
            foreach (var n in Script)
                foreach (var o in n.Options)
                {
                    if (o.Repeatable) continue;
                    if (o.Id == optionId) return i;
                    i++;
                }
            return 0;
        }

        // ------------------------------------------------------------ safety check (used by tests)
        /// <summary>Problems in the script: missing nodes, unknown {tokens}, unreachable nodes,
        /// or a node you cannot leave. Empty = fine.</summary>
        public static List<string> ValidateScript()
        {
            var problems = new List<string>();
            var tokens = new HashSet<string> { "{badge_name}", "{memo_name}", "{badge_num}", "{memo_num}", "{answer_server}" };
            var ids = new HashSet<string>();
            foreach (var n in Script)
            {
                if (!ids.Add(n.Id)) problems.Add("duplicate node: " + n.Id);
                if (string.IsNullOrEmpty(n.Text)) problems.Add("node without text: " + n.Id);
                CheckTokens(n.Text, tokens, n.Id, problems);
                bool canLeave = false;
                foreach (var o in n.Options)
                {
                    if (string.IsNullOrEmpty(o.Label)) problems.Add(n.Id + ": option " + o.Id + " has no label");
                    CheckTokens(o.Label, tokens, n.Id, problems);
                    if (o.Action != AdminTalkAction.None) canLeave = true;
                    else if (NodeById(o.To) == null) problems.Add(n.Id + ": option " + o.Id + " points at missing node '" + o.To + "'");
                }
                if (!canLeave) problems.Add("no way out of node: " + n.Id);
            }
            var seen = new HashSet<string> { HubId, Night6HubId };
            var queue = new Queue<string>();
            queue.Enqueue(HubId);
            queue.Enqueue(Night6HubId);
            while (queue.Count > 0)
            {
                var cur = NodeById(queue.Dequeue());
                foreach (var o in cur.Options)
                    if (o.Action == AdminTalkAction.None && seen.Add(o.To)) queue.Enqueue(o.To);
            }
            foreach (var n in Script) if (!seen.Contains(n.Id)) problems.Add("unreachable node: " + n.Id);
            return problems;
        }

        private static void CheckTokens(string text, HashSet<string> tokens, string where, List<string> problems)
        {
            if (string.IsNullOrEmpty(text)) return;
            int i = 0;
            while ((i = text.IndexOf('{', i)) >= 0)
            {
                int j = text.IndexOf('}', i);
                if (j < 0) { problems.Add(where + ": unclosed {token}"); return; }
                string tok = text.Substring(i, j - i + 1);
                if (!tokens.Contains(tok)) problems.Add(where + ": unknown token " + tok);
                i = j + 1;
            }
        }
    }
}
