using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    // ============================================================================
    //  The Shift-5 administrator conversation: PURE rules + data + state machine.
    //
    //  Design contract (owner brief §7):
    //   * The conversation is evidence and theatre. It NEVER stamps a verdict, never
    //     touches the question budget, never awards clues, never triggers an ending.
    //     The physical ACCEPT/DENY stamp at hand-back remains the only decision.
    //   * Branches react to what the player actually raises (a specific discrepancy,
    //     a specific discovered clue, a specific verification question already asked),
    //     to the administrator's own accumulated tactics (compliance / challenge /
    //     silence counters), and never to internal anomaly flags.
    //   * Every node offers a way back to the inspection (the "step back" option), so
    //     no branch can softlock the shift; silence is always available and always
    //     returns to the hub.
    //   * Stable string IDs everywhere; no logic ever matches rendered English.
    //
    //  Everything here is engine-free so LogicTests can walk every branch for real.
    // ============================================================================

    /// <summary>What a response option requires before it may be offered. Evaluated against
    /// the live run (clue tracker, question budget, conversation counters) — never against
    /// hidden anomaly flags.</summary>
    public enum AdminTalkCond
    {
        None = 0,
        CluePhoto,          // the old school photo with the staff member who never aged
        ClueCabinet,        // the sealed-project work order in the Classroom 2 cabinet
        AskedFirstClassroom,// the player already heard "my first stop is your server room"
        AskedStatus,        // the player already heard "I am an administrator"
        AskedCourse,        // the player already heard "I am with the district office"
    }

    /// <summary>Line-variant conditions: which of a node's candidate lines speaks, given the
    //  conversation so far. First match wins; Always last.</summary>
    public enum AdminTalkMood
    {
        Always = 0,
        AfterChallenge,     // the player has challenged at least once before this line
        AfterCompliance,    // the player has deferred at least once before this line
        AfterEvidence,      // the player has raised a discovered clue before this line
        AfterSilence,       // the player has stayed silent at least once before this line
        SilenceOnce,        // this is the first silence of the encounter
        SilenceTwice,       // the second
        SilenceThrice,      // the third and beyond: the mask slips
        HubReturn,          // re-opening the hub after at least one branch
    }

    /// <summary>One candidate line of a node: spoken when its mood matches.</summary>
    public struct AdminTalkLine
    {
        public AdminTalkMood Mood;
        public string Text;
    }

    /// <summary>One player response option. <see cref="To"/> is a node id, "hub", or "" for
    /// "the conversation ends, the inspection is mine".</summary>
    public class AdminTalkOption
    {
        public string Id = "";
        public string Label = "";          // may carry {tokens} resolved from the live visitor
        public string To = "";
        public AdminTalkCond Cond = AdminTalkCond.None;
        public AdminTalkEffect Effect = AdminTalkEffect.None;
        /// <summary>One-shot options disappear once taken (challenges, evidence, defer);
        /// silence and the exit are always available.</summary>
        public bool Repeatable = false;
    }

    public enum AdminTalkEffect
    {
        None = 0,
        Compliance,
        Challenge,
        Evidence,
    }

    public class AdminTalkNode
    {
        public string Id = "";
        /// <summary>Who speaks the node's lines: the administrator, or the room itself
        /// (silence beats, stage directions).</summary>
        public string Speaker = "V. MARLOWE";
        public List<AdminTalkLine> Lines = new List<AdminTalkLine>();
        public List<AdminTalkOption> Options = new List<AdminTalkOption>();
        /// <summary>Seconds of deliberate quiet before the lines speak (silence beats).
        /// Presentation-only; the engine never blocks on it.</summary>
        public float PauseSeconds = 0f;
    }

    /// <summary>The live facts the conversation may read: the real visitor data, the real
    /// question budget, the real clue tracker, and the conversation's own counters. Built by
    /// the view from GameRoot/Session — the engine never reaches into the run itself.</summary>
    public struct AdminTalkContext
    {
        public string BadgeName, MemoName, BadgeNumber, MemoNumber;
        public string AnswerCourse, AnswerStatus, AnswerFirstClassroom;
        public bool CluePhoto, ClueCabinet;
        public bool AskedCourse, AskedStatus, AskedFirstClassroom;
        public int Compliance, Challenge, Evidence, Silence, HubVisits;
    }

    /// <summary>The encounter's persisted state: where the conversation stands, which one-shot
    /// options were spent, and the tactic counters. Encoded as one invariant string for the
    /// save file (same convention as the queue's budget mask).</summary>
    public class AdminTalkState
    {
        public string Node = "";            // "" = the conversation has not opened yet
        public int UsedMask;                // bit per option id (stable option order)
        public int Compliance, Challenge, Evidence, Silence, HubVisits;
        public bool Completed;              // the player stepped back for the last time

        public bool HasUsed(int optionIndex) => (UsedMask & (1 << optionIndex)) != 0;
        public void Use(int optionIndex) => UsedMask |= 1 << optionIndex;

        public string Encode() =>
            Node + "," + UsedMask + "," + Compliance + "," + Challenge + "," + Evidence + "," +
            Silence + "," + HubVisits + "," + (Completed ? 1 : 0);

        public static AdminTalkState Decode(string s)
        {
            var st = new AdminTalkState();
            if (string.IsNullOrEmpty(s)) return st;
            var p = s.Split(',');
            if (p.Length < 8) return st;             // torn/old value: a fresh conversation
            st.Node = p[0];
            int.TryParse(p[1], out st.UsedMask);
            int.TryParse(p[2], out st.Compliance);
            int.TryParse(p[3], out st.Challenge);
            int.TryParse(p[4], out st.Evidence);
            int.TryParse(p[5], out st.Silence);
            int.TryParse(p[6], out st.HubVisits);
            st.Completed = p[7] == "1";
            if (AdminTalk.NodeById(st.Node) == null) st.Node = "";   // unknown node: fail safe
            return st;
        }
    }

    /// <summary>The result of taking one option: where the conversation goes and what it did.
    //  Pure data — the view presents it, the engine never touches UI or run state.</summary>
    public class AdminTalkStep
    {
        public bool Completed;              // the conversation is over; the inspection is mine
        public string NodeId = "";          // the node whose lines speak next
        public List<string> Lines = new List<string>();
        public string Speaker = "V. MARLOWE";
        public float PauseSeconds;
        public AdminTalkEffect Applied = AdminTalkEffect.None;
    }

    public static class AdminTalk
    {
        // ---------------------------------------------------------------- the script
        // Tokens {badge_name} {memo_name} {badge_num} {memo_num} {answer_course}
        // {answer_status} {answer_server} are resolved from the LIVE visitor data when the
        // line speaks, so the conversation can never contradict the papers on the desk.
        private static readonly List<AdminTalkNode> Script = BuildScript();

        public static IReadOnlyList<AdminTalkNode> Nodes => Script;

        public static AdminTalkNode NodeById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var n in Script) if (n.Id == id) return n;
            return null;
        }

        /// <summary>Stable option order — the save's used-mask indexes into this.</summary>
        public static int OptionIndex(string optionId)
        {
            int i = 0;
            foreach (var n in Script)
                foreach (var o in n.Options)
                {
                    if (o.Id == optionId) return i;
                    i++;
                }
            return -1;
        }

        public static string HubId => "hub";

        private static AdminTalkNode N(string id, string speaker, float pause,
            AdminTalkLine[] lines, AdminTalkOption[] options)
        {
            var n = new AdminTalkNode { Id = id, Speaker = speaker, PauseSeconds = pause };
            n.Lines.AddRange(lines);
            n.Options.AddRange(options);
            return n;
        }

        private static AdminTalkLine L(AdminTalkMood m, string t) => new AdminTalkLine { Mood = m, Text = t };

        private static AdminTalkOption O(string id, string label, string to,
            AdminTalkCond cond = AdminTalkCond.None, AdminTalkEffect fx = AdminTalkEffect.None,
            bool repeatable = false)
            => new AdminTalkOption { Id = id, Label = label, To = to, Cond = cond, Effect = fx, Repeatable = repeatable };

        // the three options that close or stretch every beat: silence, step back, and the hub
        private static AdminTalkOption SilenceOpt() =>
            O("silence", "[Say nothing. Let the clock answer for you.]", "silence",
              AdminTalkCond.None, AdminTalkEffect.None, true);
        private static AdminTalkOption EndOpt() =>
            O("end", "[Step back from the glass. The inspection is yours.]", "",
              AdminTalkCond.None, AdminTalkEffect.None, true);

        private static List<AdminTalkNode> BuildScript()
        {
            var s = new List<AdminTalkNode>();

            // ---- the hub: the demand, and every way to answer it ----
            s.Add(N("hub", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.HubReturn, "Still here. Still watching me. Choose your next sentence carefully, custodian."),
                    L(AdminTalkMood.AfterChallenge, "…Let us begin again. I am the district's administrator, and you are holding a door that is not yours to hold."),
                    L(AdminTalkMood.AfterCompliance, "Good. You understand how this works. Then the rest is a formality."),
                    L(AdminTalkMood.AfterEvidence, "You have been digging in this building. I can hear it in the way you stands. Ask your little questions, then open the gate."),
                    L(AdminTalkMood.AfterSilence, "…You prefer silence. So do I. It is the district, however, that prefers an answer."),
                    L(AdminTalkMood.Always, "There have been… complaints about your performance. Let's not add to them. Open the checkpoint. That is an order from the district."),
                },
                new[]
                {
                    O("chal_name", "The badge says {badge_name}. The memo says {memo_name}. Explain that.", "chal_name",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("chal_num", "Badge {badge_num}. The roster memo lists {memo_num}. Which one is yours?", "chal_num",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("chal_face", "The face at my glass is not the face on this badge.", "chal_face",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("defer", "…Yes. Of course. Give me a moment to open the gate.", "defer",
                      AdminTalkCond.None, AdminTalkEffect.Compliance),
                    O("ev_photo", "The old school photo in the hall shows a staff member who never aged. It's you, isn't it?", "ev_photo",
                      AdminTalkCond.CluePhoto, AdminTalkEffect.Evidence),
                    O("ev_cabinet", "The work order in the cabinet names the server room. You didn't come to inspect. You came to wake it.", "ev_cabinet",
                      AdminTalkCond.ClueCabinet, AdminTalkEffect.Evidence),
                    O("q_server", "You told me your first stop is the server room. Students aren't your business, are they?", "q_server",
                      AdminTalkCond.AskedFirstClassroom),
                    O("q_status", "You said: \"{answer_status}\". The memo says R. Vance is the administrator. Which one is standing here?", "q_status",
                      AdminTalkCond.AskedStatus),
                    O("q_course", "You said: \"{answer_course}\". District office. Then why does the memo put you in charge of this building tonight?", "q_course",
                      AdminTalkCond.AskedCourse),
                    SilenceOpt(),
                    EndOpt(),
                }));

            // ---- branch A1: the name discrepancy ----
            s.Add(N("chal_name", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "Marlowe, Vance. District paperwork is a river, custodian; names bend in it. The memo confirms an administrator is expected tonight. It confirms me."),
                    L(AdminTalkMood.AfterChallenge, "You keep counting syllables on a badge while the district's order stands unsigned. Read the memo again if you must — it expects an administrator. It does not expect this interrogation."),
                },
                new[]
                {
                    O("press_name", "Then read the memo with me. Line by line. Out loud.", "press_name",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("stand_name", "Names are paperwork. The face is my business.", "stand_name"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("press_name", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "…'R. Vance — DA-1147.' Fine. Vance is my supervisor's line-item. The district sends a face, not a surname. You stamp for the face in front of you."),
                    L(AdminTalkMood.AfterSilence, "…You enjoy watching me read. 'R. Vance — DA-1147.' There. The district sends a face, not a surname, and the face is mine."),
                },
                new[]
                {
                    O("press_name2", "A face the badge photo doesn't have. That's twice now.", "chal_face",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("press_name_back", "…Fine. Paperwork bends. The rule doesn't.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("stand_name", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "Then look at my face, custodian. Look at it as long as you like. When you are finished, you will open the gate, and we will both pretend this pause never happened."),
                },
                new[]
                {
                    O("stand_face", "I looked. That's the problem.", "chal_face",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("stand_back", "…The rule says badge and memo. Not faces.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch A2: the number discrepancy ----
            s.Add(N("chal_num", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "DA-0233 is my field number. DA-1147 is my office number. The district issues both. I did not expect a night custodian to have opinions about its numbering scheme."),
                    L(AdminTalkMood.AfterCompliance, "You were being reasonable a moment ago. DA-0233, DA-1147 — the district issues both. Do not let a digit undo your good sense."),
                },
                new[]
                {
                    O("num_rule", "Night five's rule is written: badge name and number must match the memo. They don't. No exceptions.", "num_rule",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("num_back", "…Two numbers for one person. Noted.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("num_rule", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "Then phone the district and confirm me. Go on. Use the phone in your little booth. I will wait here, in the cold, while you ask my own office whether I exist."),
                },
                new[]
                {
                    O("no_phone", "The note says no overrides by phone. There is no call to make. There is only the memo.", "no_phone",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("num_back2", "…I'll keep the phone in its cradle. And the gate shut.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("no_phone", "V. MARLOWE", 0.9f,
                new[]
                {
                    L(AdminTalkMood.Always, "…You memorised the note. How diligent. The district will remember diligence, custodian, when it decides which custodians remain employed."),
                    L(AdminTalkMood.AfterChallenge, "…Diligent. Obstinate. The district has words for both, and files for either."),
                },
                new[]
                {
                    O("no_phone_back", "It can keep its words. I keep the gate.", "hub"),
                    O("defer_late", "…I don't want trouble. Come in, then.", "defer",
                      AdminTalkCond.None, AdminTalkEffect.Compliance),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch A3: the face discrepancy ----
            s.Add(N("chal_face", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "Glass distorts. Fluorescents distort. Tired eyes distort most of all. I have driven four hours; my badge photograph is six years old. We are both something the camera misremembers."),
                },
                new[]
                {
                    O("face_light", "Step into the light. Slowly. Let me misremember nothing.", "face_light",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("face_back", "…Tired eyes. Perhaps.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("face_light", "V. MARLOWE", 1.1f,
                new[]
                {
                    L(AdminTalkMood.Always, "…I will not perform for a doorbell camera. Note that I said doorbell. Note that I know what your booth is. And note, custodian, that you asked me to step into the light twice before you decided what you saw."),
                },
                new[]
                {
                    O("face_twice", "Twice. Because the first time, something in your face waited for my question.", "hub"),
                    O("face_back2", "…I saw enough.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch B: compliance ----
            s.Add(N("defer", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.AfterChallenge, "There. Sense, at last — after your little accusations. I expected better judgment from you, and here it is."),
                    L(AdminTalkMood.Always, "There. Sense, at last. Then put the papers down, custodian. The memo is a formality; the district is the rule. Open the gate and let me do my work."),
                },
                new[]
                {
                    O("defer_press", "…And if I don't put the papers down?", "defer_press"),
                    O("refuse_defer", "No. The rule stands until the badge matches the memo.", "refuse",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("defer_press", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "Then you will stand at this glass until dawn with your little stamps, and in the morning the district will ask why its administrator was held in a yard by a doorbell camera. Hold me if you like. Be remembered for it."),
                },
                new[]
                {
                    O("defer_press_back", "…Remembered, then.", "hub"),
                    O("refuse_defer2", "Held, not remembered. The rule stands.", "refuse",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("refuse", "V. MARLOWE", 0.9f,
                new[]
                {
                    L(AdminTalkMood.Always, "…The rule. Yes. How comfortable the rule is, when it is the only thing left standing between you and me. Keep it, then. Keep it close. It will not keep you."),
                },
                new[]
                {
                    O("refuse_back", "It has kept the others out.", "hub"),
                    O("defer_late2", "…I didn't mean it. Come in.", "defer",
                      AdminTalkCond.None, AdminTalkEffect.Compliance),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch C: discovered evidence ----
            s.Add(N("ev_photo", "V. MARLOWE", 1.1f,
                new[]
                {
                    L(AdminTalkMood.Always, "…A photograph. From a year the district does not discuss, in a hallway the district does not own. You found it, and you kept it, and now you carry it around like a lantern. Put the lantern down, custodian. It is lighting nothing but your own face."),
                    L(AdminTalkMood.AfterEvidence, "Another relic, then. You collect this building's leftovers and call it an investigation. The photograph is paper. I am the district. Paper loses."),
                },
                new[]
                {
                    O("photo_press", "The photo is from 1987. You haven't aged a day. Neither has the fan behind your sealed door.", "photo_press",
                      AdminTalkCond.None, AdminTalkEffect.Evidence),
                    O("photo_back", "…Paper. Yes. Paper is what I stamp.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("photo_press", "V. MARLOWE", 0.9f,
                new[]
                {
                    L(AdminTalkMood.Always, "…You listen to walls. Fans spin; records hum; the building talks to whoever is still awake at two in the morning. I am not the building, custodian. I am the person it is afraid of. Open the gate and let me quiet it."),
                },
                new[]
                {
                    O("photo_press_back", "Afraid of you, or afraid for you?", "hub"),
                    O("photo_quiet", "[Say nothing. Let the fan answer.]", "silence",
                      AdminTalkCond.None, AdminTalkEffect.None, true),
                    EndOpt(),
                }));
            s.Add(N("ev_cabinet", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "The server room is district property. I am the district. Therefore the server room is mine, and the work order you are waving is my own handwriting, filed before your predecessor learned to lock cabinets."),
                },
                new[]
                {
                    O("cab_press", "The district sealed that room. The seal is broken from the inside. Your handwriting didn't do that.", "cab_press",
                      AdminTalkCond.None, AdminTalkEffect.Evidence),
                    O("cab_back", "…Your handwriting. Of course.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("cab_press", "V. MARLOWE", 0.9f,
                new[]
                {
                    L(AdminTalkMood.Always, "…Seals fail. Wood splits. Night custodians imagine. I have no interest in what the room wants; I have an interest in what it remembers, and so, custodian, do you, or you would not be standing there with my work order in your hand."),
                },
                new[]
                {
                    O("cab_press_back", "Then remember it from outside the glass.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch: the verification answers turned back on it ----
            s.Add(N("q_server", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "\"{answer_server}\" I said, and I say again: an expression. The server room holds the identification records I am here to audit. Audits are conducted from inside the room, custodian. That is what an audit is."),
                },
                new[]
                {
                    O("server_press", "Then audit it from outside the glass, like everyone else who wants something in this school.", "server_press",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("server_back", "…An expression. Noted with the others.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("server_press", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "…Everyone else. Yes. The students with their little cards, the teachers with their little keys, and you, with your little window. All of you outside the glass, all of you so certain the glass is a wall. It is a door, custodian. It has always been a door."),
                },
                new[]
                {
                    O("server_press_back", "Then tonight it stays a window.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("q_status", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "\"{answer_status}\" Because I am one. The memo names the office, not the officer; the district rotates officers the way it rotates locks. You are inspecting a chair, custodian, and I am merely sitting in it tonight."),
                },
                new[]
                {
                    O("status_press", "Chairs don't scratch door frames from the inside.", "status_press",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("status_back", "…A chair. Sitting in it tonight.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("status_press", "V. MARLOWE", 0.9f,
                new[]
                {
                    L(AdminTalkMood.Always, "…You have been reading the sealed door. Everyone reads the sealed door eventually. It is the only document in this school that never lies, because no one is left alive in it to."),
                },
                new[]
                {
                    O("status_press_back", "Then I'll keep the door sealed and the chair outside.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("q_course", "V. MARLOWE", 0f,
                new[]
                {
                    L(AdminTalkMood.Always, "\"{answer_course}\" The district office is where this building's file is kept, and I am the hand that carries files. Why am I here tonight? Because the file grew teeth, custodian, and the district prefers its teeth inspected by someone with a badge."),
                },
                new[]
                {
                    O("course_press", "My badge is a custodian's. Yours is a stranger's. That's the whole file.", "course_press",
                      AdminTalkCond.None, AdminTalkEffect.Challenge),
                    O("course_back", "…A hand that carries files.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));
            s.Add(N("course_press", "V. MARLOWE", 0.6f,
                new[]
                {
                    L(AdminTalkMood.Always, "…Stranger. What a small word for someone who has driven four hours in the dark to stand at your window. Stranger. Keep saying it. See whether it still fits by morning."),
                },
                new[]
                {
                    O("course_press_back", "By morning you'll be gone. The word stays.", "hub"),
                    SilenceOpt(), EndOpt(),
                }));

            // ---- branch D: silence ----
            s.Add(N("silence", "THE BOOTH", 1.1f,
                new[]
                {
                    L(AdminTalkMood.SilenceOnce, "…You say nothing. The clock says something for you: two minutes past the hour, and the district's administrator is standing in a yard like a parent at a locked school gate. Very well. I will repeat myself, slowly, for the custodian who prefers quiet."),
                    L(AdminTalkMood.SilenceTwice, "…Again nothing. You are waiting for me to fill the silence, to add a sentence that slips. I have sat in longer rooms than this booth, custodian, and out-silenced better witnesses than you."),
                    L(AdminTalkMood.SilenceThrice, "…Stop that. Stop standing there being quiet at me. I am not one of your students; I do not confess; I ORDER. Open. The. Gate. — …There. You hear the difference now, don't you. Between the order and the request. You have been hearing it all night."),
                    L(AdminTalkMood.Always, "…Silence. How provincial. The district reads silence as consent, custodian; I would know, I wrote that sentence into three handbooks."),
                },
                new[]
                {
                    O("silence_back", "[Keep the silence. Let it sit on the glass between you.]", "hub",
                      AdminTalkCond.None, AdminTalkEffect.None, true),
                    O("silence_break", "…My name for the log is 'no'. That's all you get.", "hub",
                      AdminTalkCond.None, AdminTalkEffect.Challenge, true),
                    EndOpt(),
                }));

            return s;
        }

        // ---------------------------------------------------------------- evaluation
        public static bool CondMet(AdminTalkCond c, AdminTalkContext ctx)
        {
            switch (c)
            {
                case AdminTalkCond.CluePhoto: return ctx.CluePhoto;
                case AdminTalkCond.ClueCabinet: return ctx.ClueCabinet;
                case AdminTalkCond.AskedFirstClassroom: return ctx.AskedFirstClassroom;
                case AdminTalkCond.AskedStatus: return ctx.AskedStatus;
                case AdminTalkCond.AskedCourse: return ctx.AskedCourse;
                default: return true;
            }
        }

        private static bool MoodMet(AdminTalkMood m, AdminTalkContext ctx, AdminTalkState st)
        {
            switch (m)
            {
                case AdminTalkMood.AfterChallenge: return ctx.Challenge > 0;
                case AdminTalkMood.AfterCompliance: return ctx.Compliance > 0;
                case AdminTalkMood.AfterEvidence: return ctx.Evidence > 0;
                case AdminTalkMood.AfterSilence: return ctx.Silence > 0;
                case AdminTalkMood.SilenceOnce: return ctx.Silence <= 1;
                case AdminTalkMood.SilenceTwice: return ctx.Silence == 2;
                case AdminTalkMood.SilenceThrice: return ctx.Silence >= 3;
                case AdminTalkMood.HubReturn: return ctx.HubVisits > 1;
                case AdminTalkMood.Always: return true;
                default: return true;
            }
        }

        /// <summary>The lines a node speaks right now: every candidate whose mood matches the
        /// conversation so far, in script order, tokens resolved from the live visitor. A node
        /// can therefore stack a state-aware beat on top of its base line.</summary>
        public static List<string> Speak(string nodeId, AdminTalkContext ctx, AdminTalkState st)
        {
            var node = NodeById(nodeId);
            var outLines = new List<string>();
            if (node == null) return outLines;
            var c = Current(ctx, st);
            bool firstHub = nodeId == HubId && st.HubVisits <= 1;
            foreach (var l in node.Lines)
            {
                if (l.Mood == AdminTalkMood.HubReturn)
                {
                    if (firstHub || nodeId != HubId) continue;
                    if (st.HubVisits <= 1) continue;
                    outLines.Add(l.Text);
                    continue;
                }
                if (l.Mood != AdminTalkMood.Always && !MoodMet(l.Mood, c, st)) continue;
                if (l.Mood == AdminTalkMood.Always)
                {
                    // the base line speaks only when no state-aware line claimed the beat
                    if (outLines.Count > 0) continue;
                    if (nodeId == HubId && HasStateMood(c, st)) continue;
                }
                outLines.Add(l.Text);
            }
            if (outLines.Count == 0 && node.Lines.Count > 0) outLines.Add(node.Lines[node.Lines.Count - 1].Text);
            for (int i = 0; i < outLines.Count; i++) outLines[i] = Resolve(outLines[i], c);
            return outLines;
        }

        private static bool HasStateMood(AdminTalkContext ctx, AdminTalkState st) =>
            ctx.Challenge > 0 || ctx.Compliance > 0 || ctx.Evidence > 0 || ctx.Silence > 0 || st.HubVisits > 1;

        /// <summary>The options the player may actually see right now: condition met, and
        /// one-shots not yet spent. The exit and silence are never filtered out.</summary>
        public static List<AdminTalkOption> OptionsFor(AdminTalkState st, AdminTalkContext ctx)
        {
            var node = NodeById(string.IsNullOrEmpty(st.Node) ? HubId : st.Node);
            var list = new List<AdminTalkOption>();
            if (node == null) { list.Add(EndOption()); return list; }   // fail safe: always escapable
            for (int i = 0; i < node.Options.Count; i++)
            {
                var o = node.Options[i];
                if (!CondMet(o.Cond, ctx)) continue;
                if (!o.Repeatable)
                {
                    int idx = OptionIndex(o.Id);
                    if (idx >= 0 && st.HasUsed(idx)) continue;
                }
                list.Add(o);
            }
            if (list.Count == 0) list.Add(EndOption());   // a drained node still ends safely
            return list;
        }

        private static AdminTalkOption EndOption() =>
            new AdminTalkOption { Id = "end_fallback", Label = "[Step back from the glass.]", To = "" };

        /// <summary>Resolve {tokens} from the live visitor + question data. Never invents:
        /// every token is a field the inspection system already owns.</summary>
        public static string Resolve(string text, AdminTalkContext ctx)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("{badge_name}", ctx.BadgeName ?? "?")
                .Replace("{memo_name}", ctx.MemoName ?? "?")
                .Replace("{badge_num}", ctx.BadgeNumber ?? "?")
                .Replace("{memo_num}", ctx.MemoNumber ?? "?")
                .Replace("{answer_course}", ctx.AnswerCourse ?? "…")
                .Replace("{answer_status}", ctx.AnswerStatus ?? "…")
                .Replace("{answer_server}", ctx.AnswerFirstClassroom ?? "…");
        }

        /// <summary>Take one option. Mutates only the conversation state; applies only the
        /// option's own effect; returns what speaks next. A bad id or a filtered option fails
        /// safe: nothing happens, the caller keeps its node.</summary>
        public static AdminTalkStep Choose(AdminTalkState st, AdminTalkContext ctx, string optionId)
        {
            var step = new AdminTalkStep();
            if (st.Completed) { step.Completed = true; return step; }
            var node = NodeById(string.IsNullOrEmpty(st.Node) ? HubId : st.Node);
            if (node == null) node = NodeById(HubId);
            AdminTalkOption chosen = null;
            foreach (var o in OptionsFor(st, ctx)) if (o.Id == optionId) { chosen = o; break; }
            if (chosen == null)
            {
                // invalid or filtered option: nothing happens, the node keeps standing
                step.NodeId = node.Id;
                step.Lines = Speak(node.Id, ctx, st);
                return step;
            }

            int idx = OptionIndex(chosen.Id);
            if (idx >= 0 && !chosen.Repeatable) st.Use(idx);

            if (string.IsNullOrEmpty(chosen.To))
            {
                st.Completed = true;
                step.Completed = true;
                step.Applied = chosen.Effect;
                return step;
            }

            string dest = chosen.To;
            if (dest == HubId) st.HubVisits++;          // arriving at the hub is a hub visit...
            if (dest == "silence") st.Silence++;        // ...and entering the silence IS the silence
            st.Node = dest;
            step.NodeId = dest;
            step.Applied = chosen.Effect;
            var destNode = NodeById(dest);
            step.Speaker = destNode != null ? destNode.Speaker : "V. MARLOWE";
            step.PauseSeconds = destNode != null ? destNode.PauseSeconds : 0f;
            step.Lines = Speak(dest, ctx, st);          // reacts to the history so far...
            ApplyEffect(st, chosen.Effect);             // ...this choice counts from the NEXT beat
            return step;
        }

        private static void ApplyEffect(AdminTalkState st, AdminTalkEffect fx)
        {
            switch (fx)
            {
                case AdminTalkEffect.Compliance: st.Compliance++; break;
                case AdminTalkEffect.Challenge: st.Challenge++; break;
                case AdminTalkEffect.Evidence: st.Evidence++; break;
            }
        }

        /// <summary>Open (or re-open) the conversation: lands on the hub, counting the visit.</summary>
        public static AdminTalkStep Open(AdminTalkState st, AdminTalkContext ctx)
        {
            var step = new AdminTalkStep();
            if (st.Completed) { step.Completed = true; return step; }
            if (string.IsNullOrEmpty(st.Node)) st.HubVisits++;
            st.Node = HubId;
            step.NodeId = HubId;
            var node = NodeById(HubId);
            step.Speaker = node.Speaker;
            step.Lines = Speak(HubId, ctx, st);
            return step;
        }

        // the context with this step's counters already visible to mood selection
        private static AdminTalkContext Current(AdminTalkContext ctx, AdminTalkState st)
        {
            ctx.Compliance = st.Compliance;
            ctx.Challenge = st.Challenge;
            ctx.Evidence = st.Evidence;
            ctx.Silence = st.Silence;
            ctx.HubVisits = st.HubVisits;
            return ctx;
        }

        // ---------------------------------------------------------------- validation
        /// <summary>Static health of the script: duplicate ids, dangling destinations, nodes
        /// with no options, nodes that can never be reached, and — the softlock rule — nodes
        /// from which the exit can never be reached. Returns human-readable problems; empty
        /// means the conversation is safe to ship.</summary>
        public static List<string> ValidateScript()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            foreach (var n in Script)
            {
                if (!ids.Add(n.Id)) problems.Add("duplicate node id: " + n.Id);
                if (n.Lines.Count == 0) problems.Add("node without lines: " + n.Id);
                if (n.Options.Count == 0) problems.Add("node without options: " + n.Id);
                foreach (var o in n.Options)
                {
                    if (!string.IsNullOrEmpty(o.To) && o.To != HubId && NodeById(o.To) == null)
                        problems.Add(n.Id + ": option " + o.Id + " points at missing node " + o.To);
                    if (string.IsNullOrEmpty(o.Label)) problems.Add(n.Id + ": option " + o.Id + " has no label");
                }
            }
            // reachability from the hub, following every option
            var seen = new HashSet<string> { HubId };
            var queue = new Queue<string>();
            queue.Enqueue(HubId);
            while (queue.Count > 0)
            {
                var cur = NodeById(queue.Dequeue());
                if (cur == null) continue;
                foreach (var o in cur.Options)
                {
                    string to = string.IsNullOrEmpty(o.To) ? "" : o.To;
                    if (to == "" || to == HubId) continue;
                    if (seen.Add(to)) queue.Enqueue(to);
                }
            }
            foreach (var n in Script)
                if (!seen.Contains(n.Id)) problems.Add("unreachable node: " + n.Id);

            // every node can still reach the exit (no inescapable cycle)
            foreach (var n in Script)
            {
                var reach = new HashSet<string> { n.Id };
                var q2 = new Queue<string>();
                q2.Enqueue(n.Id);
                bool canEnd = false;
                while (q2.Count > 0)
                {
                    var cur = NodeById(q2.Dequeue());
                    if (cur == null) continue;
                    foreach (var o in cur.Options)
                    {
                        if (string.IsNullOrEmpty(o.To)) { canEnd = true; continue; }
                        if (reach.Add(o.To)) q2.Enqueue(o.To);
                    }
                }
                if (!canEnd) problems.Add("no exit reachable from: " + n.Id);
            }

            // tokens in labels/lines must all be resolvable, or a line would show a raw brace
            var tokens = new HashSet<string>
            {
                "{badge_name}", "{memo_name}", "{badge_num}", "{memo_num}",
                "{answer_course}", "{answer_status}", "{answer_server}",
            };
            foreach (var n in Script)
            {
                foreach (var l in n.Lines) CheckTokens(l.Text, tokens, n.Id, problems);
                foreach (var o in n.Options) CheckTokens(o.Label, tokens, n.Id, problems);
            }

            // the hub must always offer the exit and the silence
            var hub = NodeById(HubId);
            bool hubEnd = false, hubSilence = false;
            foreach (var o in hub.Options)
            {
                if (o.Id == "end") hubEnd = true;
                if (o.Id == "silence") hubSilence = true;
            }
            if (!hubEnd) problems.Add("hub offers no exit");
            if (!hubSilence) problems.Add("hub offers no silence");
            return problems;
        }

        private static void CheckTokens(string text, HashSet<string> tokens, string where, List<string> problems)
        {
            if (string.IsNullOrEmpty(text)) return;
            int i = 0;
            while ((i = text.IndexOf('{', i)) >= 0)
            {
                int j = text.IndexOf('}', i);
                if (j < 0) { problems.Add(where + ": unclosed token in a line"); return; }
                string tok = text.Substring(i, j - i + 1);
                if (!tokens.Contains(tok)) problems.Add(where + ": unknown token " + tok);
                i = j + 1;
            }
        }
    }
}
