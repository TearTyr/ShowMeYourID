using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    [Serializable]
    public class ShiftSummary
    {
        public int Shift;
        public int Visitors;
        public int Correct;
        public int FalseRejections;
        public int Breaches;
        public float SanityEnd;

        public string Encode() =>
            Shift + ":" + Visitors + ":" + Correct + ":" + FalseRejections + ":" + Breaches + ":" +
            SanityEnd.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        public static ShiftSummary Decode(string s)
        {
            var parts = s.Split(':');
            if (parts.Length != 6) return null;
            return new ShiftSummary
            {
                Shift = int.Parse(parts[0]),
                Visitors = int.Parse(parts[1]),
                Correct = int.Parse(parts[2]),
                FalseRejections = int.Parse(parts[3]),
                Breaches = int.Parse(parts[4]),
                SanityEnd = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
            };
        }
    }

    /// <summary>Aggregate run state for all modes. Pure C# — the Unity layer observes and
    /// mutates this through its methods only, so all rules stay testable headlessly.</summary>
    public class GameSession
    {
        public GameMode Mode = GameMode.Normal;
        public int Seed = 20261102;
        public int CurrentShift = 1;        // Normal: shift being played (1..5)
        public int CompletedShifts = 0;     // Normal: shifts fully completed
        public int Processed = 0;           // visitors processed this run
        public int CorrectDecisions = 0;
        public int FalseRejections = 0;
        public int Breaches = 0;            // anomalies allowed in (includes administrator if allowed)
        public int AnomaliesRejected = 0;
        public int TokensCollected = 0;      // fully-tracked anomalies (master prompt §3)
        public bool AdminAllowed = false;
        public bool AdminRejected = false;
        public float Sanity = SanityModel.Max;
        public int Score = 0;
        public int Streak = 0;
        public int BestStreak = 0;
        public float RunTimer = 0f;         // Endless/Challenge seconds remaining
        public int RunProcessed = 0;        // Endless/Challenge visitors this run
        public string ActiveMutationKey = "none";
        public ClueTracker Clues = new ClueTracker();
        public PatrolPool Pool = new PatrolPool();   // outstanding anomalies tonight
        public List<ShiftSummary> History = new List<ShiftSummary>();
        /// <summary>Set when an ending/run result was reached; such saves must not offer Continue.</summary>
        public bool RunFinished = false;
        /// <summary>Play-time presentation log for the current night/run. The checkpoint UI
        /// records each visitor here AFTER their verdict; the scanner and duplicate rule read it.
        /// Cleared at the start of every Normal shift ("tonight" scope).</summary>
        public PresentationLog TonightLog = new PresentationLog();

        public SanityStage SanityStage => SanityModel.StageFor(Sanity);

        public RuleMutationDef ActiveMutation
        {
            get
            {
                foreach (var m in ShiftPlan.ChallengeMutations)
                    if (m.Key == ActiveMutationKey) return m;
                return ShiftPlan.ChallengeMutations[0];
            }
        }

        /// <summary>Record a permanent verdict outcome (§2.4) and update all counters.</summary>
        public void RecordVerdict(InspectionRules.Outcome o)
        {
            if (o == null || o.Verdict == Verdict.None) return;
            Processed++;
            if (o.Correct)
            {
                CorrectDecisions++;
                Streak++;
                if (Streak > BestStreak) BestStreak = Streak;
                if (o.WasAnomaly) AnomaliesRejected++;
            }
            else
            {
                Streak = 0;
            }
            if (o.FalseRejection)
            {
                FalseRejections++;
                ApplySanity(-6f); // mistakes are disturbing events (§4.3)
            }
            if (o.AnomalyBreach)
            {
                Breaches++;
                ApplySanity(-8f);
                if (o.WasAdministrator) AdminAllowed = true;
            }
            if (o.WasAdministrator && o.Verdict == Verdict.Reject)
            {
                AdminRejected = true;
                // Rejecting the administrator correctly is the climax — small recovery.
                ApplySanity(+10f);
            }
        }

        /// <summary>Negative deltas scale with the mode drain multiplier (Challenge = 2x).</summary>
        public void ApplySanity(float delta)
        {
            if (delta < 0) delta *= SanityModel.DrainMultiplier(Mode);
            Sanity = SanityModel.Clamp(Sanity + delta);
        }

        public ClueDiscoveryResult DiscoverClue(string key)
        {
            var res = Clues.Discover(key);
            if (res.FirstTime && res.Def != null) ApplySanity(res.Def.SanityDelta);
            return res;
        }

        public void MarkRoomInvestigated(RoomId room) => Clues.MarkRoomInvestigated(room);

        /// <summary>Endless/Challenge timer bookkeeping after a verdict.</summary>
        public float ApplyRunTimer(InspectionRules.Outcome o)
        {
            float delta;
            if (o.Correct)
                delta = o.WasAnomaly ? EndlessRules.TimePerAnomalyRejected : EndlessRules.TimePerLegitAllowed;
            else
                delta = -EndlessRules.TimePenaltyForMistake;
            RunTimer = Math.Min(RunTimer + delta, EndlessRules.HardCapSeconds);
            return delta;
        }

        public void CompleteShift()
        {
            History.Add(new ShiftSummary
            {
                Shift = CurrentShift,
                Visitors = Processed, // cumulative; UI shows per-shift deltas
                Correct = CorrectDecisions,
                FalseRejections = FalseRejections,
                Breaches = Breaches,
                SanityEnd = Sanity,
            });
            CompletedShifts++;
            CurrentShift++;
        }

        public EndingRules.Evaluation EvaluateEnding() => EndingRules.Evaluate(this);

        public bool LoseThresholdReached =>
            AdminAllowed || Breaches >= EndingRules.LoseThreshold || FalseRejections >= EndingRules.LoseThreshold;
    }

    /// <summary>Player settings (pure model; the Unity SettingsSystem persists + applies these).</summary>
    [Serializable]
    public class SettingsData
    {
        public float MasterVolume = 0.8f;  // 0..1
        public float MusicVolume = 0.6f;   // 0..1
        public float SfxVolume = 0.8f;     // 0..1
        public int TextScale = 1;          // 0 Small, 1 Normal, 2 Large
        public bool ReduceMotion = false;  // disables camera shake / sprint lunges / pulsing
        public bool PhotosensitivitySafe = false; // no strobe flashes; scares fade instead of popping
        public bool Captions = true;       // text captions for significant audio events
        public bool Fullscreen = true;     // display: exclusive fullscreen vs windowed
        public int DisplayWidth = 0;       // stored resolution preference; 0 = "no preference, keep launcher default"
        public int DisplayHeight = 0;

        public SettingsData Clone() => (SettingsData)MemberwiseClone();

        public static float TextScaleMultiplier(int textScale)
        {
            switch (textScale) { case 0: return 0.85f; case 2: return 1.25f; default: return 1f; }
        }

        public string Encode() =>
            Inv(MasterVolume) + "," + Inv(MusicVolume) + "," + Inv(SfxVolume) + "," +
            TextScale + "," + B(ReduceMotion) + "," + B(PhotosensitivitySafe) + "," + B(Captions) +
            "," + B(Fullscreen) + "," + DisplayWidth + "," + DisplayHeight;

        /// <summary>Tolerant by design: older saves carry 7 fields, newer ones 10. Anything
        /// missing falls back to the defaults above instead of failing the whole load.</summary>
        public static SettingsData Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.Split(',');
            if (p.Length < 7) return null;
            var d = new SettingsData
            {
                MasterVolume = F(p[0]), MusicVolume = F(p[1]), SfxVolume = F(p[2]),
                TextScale = int.Parse(p[3]),
                ReduceMotion = p[4] == "1", PhotosensitivitySafe = p[5] == "1", Captions = p[6] == "1",
            };
            if (p.Length >= 10)
            {
                d.Fullscreen = p[7] != "0";
                int w, h;
                if (int.TryParse(p[8], out w) && int.TryParse(p[9], out h))
                {
                    d.DisplayWidth = w;
                    d.DisplayHeight = h;
                }
            }
            return d;
        }

        private static string Inv(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        private static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        private static string B(bool b) => b ? "1" : "0";
    }

    /// <summary>Meta records persisted alongside saves (best scores, endings seen).</summary>
    [Serializable]
    public class RecordsData
    {
        public int EndlessBest = 0;
        public int ChallengeBest = 0;
        public List<string> EndingsSeen = new List<string>(); // "win", "lose", "secret"
    }
}
