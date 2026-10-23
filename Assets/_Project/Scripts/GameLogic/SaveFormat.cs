using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Hollowbrook.Logic
{
    public class SaveGame
    {
        public GameSession Session = new GameSession();
        public SettingsData Settings = new SettingsData();
        public RecordsData Records = new RecordsData();
        public string SavedAtLabel = ""; // human-readable, informational only
    }

    public static class SaveFormat
    {
        public const string Header = "SMYIDSAVE";
        public const int Version = 1;

        public static string Serialize(SaveGame save)
        {
            var s = save.Session;
            var sb = new StringBuilder();
            sb.Append(Header).Append(' ').Append(Version).Append('\n');
            KV(sb, "savedAt", save.SavedAtLabel);
            KV(sb, "mode", s.Mode.ToString());
            sb.Append("seed=").Append(s.Seed).Append('\n');
            sb.Append("shift=").Append(s.CurrentShift).Append('\n');
            sb.Append("completed=").Append(s.CompletedShifts).Append('\n');
            sb.Append("processed=").Append(s.Processed).Append('\n');
            sb.Append("correct=").Append(s.CorrectDecisions).Append('\n');
            sb.Append("falseRej=").Append(s.FalseRejections).Append('\n');
            sb.Append("breaches=").Append(s.Breaches).Append('\n');
            sb.Append("anomRej=").Append(s.AnomaliesRejected).Append('\n');
            sb.Append("tokens=").Append(s.TokensCollected).Append('\n');
            sb.Append("visitorAtSave=").Append(s.VisitorIndexAtSave).Append('\n');
            var q = s.QueueState;
            if (q != null)
            {
                sb.Append("queueStarted=").Append(q.Started ? 1 : 0).Append('\n');
                sb.Append("queueCursor=").Append(q.RosterCursor).Append('\n');
                sb.Append("queueArrived=").Append(q.Arrived).Append('\n');
                sb.Append("queueProcessed=").Append(q.Processed).Append('\n');
                KV(sb, "queueTimer", Inv(q.TimerSeconds));
                KV(sb, "queueWaiting", JoinIdx(q.WaitingIdx));
                sb.Append("queueActive=").Append(q.ActiveIdx).Append('\n');
                KV(sb, "queueVanished", JoinIdx(q.VanishedIdx));
                sb.Append("queueMissing=").Append(q.MissingIncidents).Append('\n');
                sb.Append("queueBudget=").Append(q.BudgetMask).Append('\n');
                sb.Append("queueCollapsed=").Append(q.Overwhelmed ? 1 : 0).Append('\n');
                sb.Append("queuePending=").Append(q.ResolutionPending ? 1 : 0).Append('\n');
                sb.Append("queueWarned=").Append(q.Warned).Append('\n');
            }
            KV(sb, "pool", s.Pool.Encode());
            KV(sb, "admitted", string.Join(",", s.AdmittedStudents.ConvertAll(st => st.VisitorIndex + ":" + (st.IsAnomaly ? 1 : 0))));
            sb.Append("adminAllowed=").Append(s.AdminAllowed ? 1 : 0).Append('\n');
            sb.Append("adminRejected=").Append(s.AdminRejected ? 1 : 0).Append('\n');
            sb.Append("runFinished=").Append(s.RunFinished ? 1 : 0).Append('\n');
            KV(sb, "sanity", Inv(s.Sanity));
            sb.Append("score=").Append(s.Score).Append('\n');
            sb.Append("streak=").Append(s.Streak).Append('\n');
            sb.Append("bestStreak=").Append(s.BestStreak).Append('\n');
            KV(sb, "runTimer", Inv(s.RunTimer));
            sb.Append("runProcessed=").Append(s.RunProcessed).Append('\n');
            sb.Append("warnings=").Append(s.Warnings).Append('\n');
            sb.Append("fines=").Append(s.Fines).Append('\n');
            KV(sb, "mutation", s.ActiveMutationKey);
            KV(sb, "seen", s.TonightLog.Encode());
            KV(sb, "clues", string.Join(",", s.Clues.SaveFound()));
            sb.Append("orderIntact=").Append(s.Clues.OrderIntact ? 1 : 0).Append('\n');
            sb.Append("nextOrder=").Append(s.Clues.NextExpectedOrder).Append('\n');
            KV(sb, "rooms", string.Join(",", s.Clues.SaveRooms()));
            var hist = new List<string>();
            foreach (var h in s.History) hist.Add(h.Encode());
            KV(sb, "history", string.Join("|", hist));
            KV(sb, "settings", save.Settings.Encode());
            sb.Append("endlessBest=").Append(save.Records.EndlessBest).Append('\n');
            sb.Append("challengeBest=").Append(save.Records.ChallengeBest).Append('\n');
            KV(sb, "endingsSeen", string.Join(",", save.Records.EndingsSeen));

            string body = sb.ToString();
            sb.Append("checksum=").Append(Checksum(body)).Append('\n');
            return sb.ToString();
        }

        public static SaveGame Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length < 3) return null;
            var head = lines[0].Split(' ');
            if (head.Length != 2 || head[0] != Header) return null;
            if (!int.TryParse(head[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ver) || ver != Version) return null;

            string checksumLine = null;
            var body = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("checksum=")) { checksumLine = lines[i]; continue; }
                if (lines[i].Length == 0) continue;
                body.Append(lines[i]).Append('\n');
            }
            if (checksumLine == null) return null;
            if (!long.TryParse(checksumLine.Substring("checksum=".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out long stored)) return null;
            if (Checksum(body.ToString()) != stored) return null;

            var map = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                int eq = lines[i].IndexOf('=');
                if (eq <= 0) continue;
                map[lines[i].Substring(0, eq)] = lines[i].Substring(eq + 1);
            }

            var save = new SaveGame();
            var s = save.Session;
            try
            {
                if (map.TryGetValue("mode", out var mode) && System.Enum.TryParse(mode, out GameMode gm)) s.Mode = gm;
                s.Seed = Int(map, "seed", s.Seed);
                s.CurrentShift = Int(map, "shift", 1);
                s.CompletedShifts = Int(map, "completed", 0);
                s.Processed = Int(map, "processed", 0);
                s.CorrectDecisions = Int(map, "correct", 0);
                s.FalseRejections = Int(map, "falseRej", 0);
                s.Breaches = Int(map, "breaches", 0);
                s.AnomaliesRejected = Int(map, "anomRej", 0);
                s.TokensCollected = Int(map, "tokens", 0);
                s.VisitorIndexAtSave = Int(map, "visitorAtSave", 0);
                if (map.ContainsKey("queueStarted"))
                {
                    var q = new QueueSnapshot
                    {
                        Started = Int(map, "queueStarted", 0) == 1,
                        RosterCursor = Int(map, "queueCursor", 0),
                        Arrived = Int(map, "queueArrived", 0),
                        Processed = Int(map, "queueProcessed", 0),
                        TimerSeconds = Flt(map, "queueTimer", 0f),
                        WaitingIdx = IntList(Str(map, "queueWaiting", "")),
                        ActiveIdx = Int(map, "queueActive", -1),
                        VanishedIdx = IntList(Str(map, "queueVanished", "")),
                        MissingIncidents = Int(map, "queueMissing", 0),
                        BudgetMask = Int(map, "queueBudget", 0),
                        Overwhelmed = Int(map, "queueCollapsed", 0) == 1,
                        ResolutionPending = Int(map, "queuePending", 0) == 1,
                        Warned = map.ContainsKey("queueWarned") ? Int(map, "queueWarned", -1) : -1,
                    };
                    s.QueueState = q;
                    s.QueueOverwhelmed = q.Overwhelmed;
                }
                else s.QueueState = null;   // a pre-queue save: ContinueGame falls back to visitorAtSave
                s.Pool.Decode(Str(map, "pool", ""));
                foreach (var pair in Str(map, "admitted", "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var halved = pair.Split(':');
                    if (halved.Length == 2 && int.TryParse(halved[0], System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out int ai))
                        s.AdmittedStudents.Add(new StudentData { VisitorIndex = ai, IsAnomaly = halved[1] == "1", Id = "IDX:" + ai });
                }
                s.AdminAllowed = Int(map, "adminAllowed", 0) == 1;
                s.AdminRejected = Int(map, "adminRejected", 0) == 1;
                s.RunFinished = Int(map, "runFinished", 0) == 1;
                s.Sanity = Flt(map, "sanity", 100f);
                s.Score = Int(map, "score", 0);
                s.Streak = Int(map, "streak", 0);
                s.BestStreak = Int(map, "bestStreak", 0);
                s.RunTimer = Flt(map, "runTimer", 0f);
                s.RunProcessed = Int(map, "runProcessed", 0);
                s.Warnings = Int(map, "warnings", 0);
                s.Fines = Int(map, "fines", 0);
                s.ActiveMutationKey = Str(map, "mutation", "none");
                save.SavedAtLabel = Str(map, "savedAt", "");

                s.TonightLog.Decode(Str(map, "seen", ""));

                s.Clues.LoadFound(Split(Str(map, "clues", "")));
                s.Clues.LoadRooms(SplitList(Str(map, "rooms", "")));
                s.Clues.LoadOrderIntact(Int(map, "orderIntact", 1) == 1, Int(map, "nextOrder", 1));

                s.History.Clear();
                foreach (var h in Str(map, "history", "").Split('|'))
                {
                    if (h.Length == 0) continue;
                    var sum = ShiftSummary.Decode(h);
                    if (sum != null) s.History.Add(sum);
                }

                var settings = SettingsData.Decode(Str(map, "settings", ""));
                if (settings != null) save.Settings = settings;

                save.Records.EndlessBest = Int(map, "endlessBest", 0);
                save.Records.ChallengeBest = Int(map, "challengeBest", 0);
                save.Records.EndingsSeen.Clear();
                foreach (var e in Split(Str(map, "endingsSeen", ""))) save.Records.EndingsSeen.Add(e);
            }
            catch (System.Exception)
            {
                return null; // any malformed value => treat as no save (graceful fallback)
            }
            return save;
        }

        private static void KV(StringBuilder sb, string key, string value) =>
            sb.Append(key).Append('=').Append(value ?? "").Append('\n');

        private static string Inv(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        private static string JoinIdx(List<int> idx)
        {
            var parts = new List<string>();
            foreach (int i in idx) parts.Add(i.ToString(CultureInfo.InvariantCulture));
            return string.Join(",", parts.ToArray());
        }

        private static List<int> IntList(string csv)
        {
            var l = new List<int>();
            foreach (var p in Split(csv))
                if (int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) l.Add(v);
            return l;
        }

        public static long Checksum(string body)
        {
            unchecked
            {
                long h = 17;
                foreach (char c in body) h = h * 31 + c;
                return (h & 0x7FFFFFFFFFFFFFFFL) % 1000000007L;
            }
        }

        private static IEnumerable<string> Split(string csv)
        {
            if (string.IsNullOrEmpty(csv)) yield break;
            foreach (var p in csv.Split(',')) if (p.Length > 0) yield return p;
        }

        private static List<string> SplitList(string csv)
        {
            var l = new List<string>();
            foreach (var p in Split(csv)) l.Add(p);
            return l;
        }

        private static int Int(Dictionary<string, string> m, string k, int d) =>
            m.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : d;

        private static float Flt(Dictionary<string, string> m, string k, float d) =>
            m.TryGetValue(k, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float r) ? r : d;

        private static string Str(Dictionary<string, string> m, string k, string d) =>
            m.TryGetValue(k, out var v) ? v : d;
    }
}
