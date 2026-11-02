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
                Shift = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                Visitors = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                Correct = int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                FalseRejections = int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                Breaches = int.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                SanityEnd = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture),
            };
        }
    }

    public class GameSession
    {
        public GameMode Mode = GameMode.Normal;
        public int Seed = GameRules.Current.FixedSeed;
        public int CurrentShift = 1;        // Normal: shift being played (1..5)
        public int CompletedShifts = 0;     // Normal: shifts fully completed
        public int Processed = 0;           // visitors processed this run
        public int CorrectDecisions = 0;
        public int FalseRejections = 0;
        public int Breaches = 0;            // anomalies allowed in (includes administrator if allowed)
        public int AnomaliesRejected = 0;
        public int ClearedRooms = 0;   // tonight's patrol, see PatrolCycle
        public int PeekAnomaliesGone;  // peek rooms whose standing anomaly was driven out tonight (bits, see PatrolCycle)
        public int VisitorIndexAtSave = 0;   // where a mid-shift continue resumes
        public bool AdminAllowed = false;
        public bool AdminRejected = false;
        // After the DENY stamp the administrator hunts through the school (see AdminHunt).
        // Won = he was driven off; Lost = he reached the server door.
        public HuntState Hunt = HuntState.None;
        // Where a hunt that was left half way stands, so quitting and Continuing is no free escape.
        public int HuntCatches;
        public float HuntSecondsLeft = -1f;     // -1 = the hunt has not been left yet
        // The Shift-5 conversation: where it stands, which one-shots were spent, the tactic
        // counters. Encoded into the save as one invariant string (see SaveFormat "adminTalk").
        // It carries no mechanical power - no clues, no budget, no verdict - so resuming it
        // can never be exploited; it only keeps the administrator's memory consistent.
        public AdminTalkState AdminTalk = new AdminTalkState();
        public float Sanity = SanityModel.Max;
        public int Score = 0;
        public int Streak = 0;
        public int BestStreak = 0;
        public float ShiftClockMinutes = 0f;  // shift clock at last save (22:00 + this)
        public ClueTracker Clues = new ClueTracker();
        public PatrolPool Pool = new PatrolPool();   // outstanding anomalies tonight
        public List<ShiftSummary> History = new List<ShiftSummary>();
        public bool RunFinished = false;
        public PresentationLog TonightLog = new PresentationLog();
        /// <summary>Everyone stamped ALLOW this shift, in stamp order (anomalies included):
        /// the classroom spawner seats exactly these ids. Cleared at shift start.</summary>
        public readonly List<StudentData> AdmittedStudents = new List<StudentData>();
        /// <summary>Mistakes book warnings; every third warning draws a fine (score penalty).
        /// Penalties, never instant game over.</summary>
        public int Warnings;
        public int Fines;
        public bool QueueOverwhelmed = false;      // the waiting line overran the checkpoint (lose)
        public QueueSnapshot QueueState;           // null until a shift queue has been persisted

        public SanityStage SanityStage => SanityModel.StageFor(Sanity);

        public void RecordVerdict(InspectionRules.Outcome o)
        {
            if (o == null || o.Verdict == Verdict.None) return;
            Processed++;
            if (!o.WasAdministrator)
                NightRecords.Add(new NightRecord
                {
                    Name = o.Name, WasAnomaly = o.WasAnomaly, Allowed = o.Verdict == Verdict.Allow,
                    Why = o.RevealedDefects != null && o.RevealedDefects.Count > 0 ? o.RevealedDefects[0] : "",
                });
            if (o.Verdict == Verdict.Allow)
                AdmittedStudents.Add(new StudentData { Id = o.Id, Name = o.Name, IsAnomaly = o.WasAnomaly, VisitorIndex = o.VisitorIndex });
            if (o.Correct)
            {
                CorrectDecisions++;
                RefillPending += SanityModel.RefillOnCorrect;   // paid out gradually, see TakeRefill
                Streak++;
                if (Streak > BestStreak) BestStreak = Streak;
                if (o.WasAnomaly) AnomaliesRejected++;
            }
            else
            {
                Streak = 0;
                Stacks.Add();   // a wrong call costs no sanity itself - it speeds up the drain for a while
                Warnings++;
                if (Warnings % 3 == 0) { Fines++; Score = System.Math.Max(0, Score - 100); }
            }
            if (o.FalseRejection)
            {
                FalseRejections++;
            }
            if (o.AnomalyBreach)
            {
                Breaches++;
                ShiftBreaches++;
                if (o.WasAdministrator) AdminAllowed = true;
            }
            if (o.WasAdministrator && o.Verdict == Verdict.Reject)
            {
                AdminRejected = true;   // a correct call, so it already queued its refill
            }
        }

        /// <summary>The drain stacks wrong decisions put up (see SanityStacks). Transient: a
        /// stack lasts at most half a minute, so it is not saved.</summary>
        public SanityStacks Stacks = new SanityStacks();
        /// <summary>Sanity a correct decision has earned that has not been handed over yet.</summary>
        public float RefillPending;

        /// <summary>Hand over up to <see cref="SanityModel.RefillPerSecond"/> x dt of the pending
        /// refill and return how much to add to sanity this tick.</summary>
        public float TakeRefill(float dt)
        {
            if (RefillPending <= 0f || dt <= 0f) return 0f;
            float give = System.Math.Min(RefillPending, SanityModel.RefillPerSecond * dt);
            RefillPending -= give;
            return give;
        }

        /// <summary>Change sanity. Every LOSS is scaled by the current drain stacks;
        /// gains are not.</summary>
        public void ApplySanity(float delta)
        {
            if (delta < 0) delta *= Stacks.Multiplier;
            Sanity = SanityModel.Clamp(Sanity + delta);
        }

        /// <summary>Anomalies let in during the shift being played (resets each night). Clocking out with
        /// any of them in is a failed night.</summary>
        public int ShiftBreaches;
        /// <summary>The night security log: three counts the player keeps by hand (student encounters,
        /// anomalies detected, security incidents). A recording toy only: nothing reads or scores them.
        /// Cleared when a night begins.</summary>
        public const int LogEntries = 3, LogMax = 99;
        public int[] LogCounts = new int[LogEntries];

        /// <summary>Count one entry up or down (kept between 0 and <see cref="LogMax"/>). Returns the new count.</summary>
        public int ChangeLog(int entry, int delta)
        {
            if (entry < 0 || entry >= LogEntries) return 0;
            int before = LogCounts[entry];
            LogCounts[entry] = System.Math.Max(0, System.Math.Min(LogMax, before + delta));
            if (LogCounts[entry] != before) LogSigned = false;   // a changed box needs signing again
            return LogCounts[entry];
        }

        /// <summary>Everyone who reached the window tonight, in order (saved). The Security Log is checked against it.</summary>
        public readonly List<NightRecord> NightRecords = new List<NightRecord>();
        /// <summary>The log has been signed: on nights 1 to 4 END SHIFT waits for it.</summary>
        public bool LogSigned;
        /// <summary>The Night 1 tutorial. Done (the default) means none: old saves and the other modes.</summary>
        public TutorialStep TutorialStep = TutorialStep.Done;
        /// <summary>What last night's log got wrong (or "matched"): printed at the top of tonight's Rules sheet.</summary>
        public string LogNote = "";
        /// <summary>Sanity the wrong boxes cost, paid when the next night begins.</summary>
        public int PendingLogLoss;

        public void SignLog() => LogSigned = true;

        /// <summary>The night is over: compare the three boxes with what really happened, write the note for
        /// tomorrow's Rules sheet and book the penalty.</summary>
        public NightLogRules.Grade GradeNight(int incidents)
        {
            var grade = NightLogRules.Check(LogCounts, NightRecords, incidents);
            LogNote = grade.Note;
            PendingLogLoss = grade.SanityLoss;
            return grade;
        }
        /// <summary>How the night just played failed (None when it did not) - read by the results screen.</summary>
        public ShiftFailure LastShiftFailure = ShiftFailure.None;
        /// <summary>Severity of the failed night the NEXT night pays for (0 = nothing to pay), and which
        /// night it was (the penalty grows with the night). Saved: a quit at the results screen keeps it.</summary>
        public int PendingPenaltySeverity;
        public int PendingPenaltyNight;
        /// <summary>An anomaly should already be roaming at the next patrol's start (consumed once).</summary>
        public bool RoamingAtStart;

        /// <summary>The night is over and it failed: record how, and what the next night will cost.</summary>
        public void RecordShiftFailure(ShiftFailure kind)
        {
            LastShiftFailure = kind;
            PendingPenaltySeverity = ShiftFailureRules.Severity(ShiftBreaches, kind);
            PendingPenaltyNight = CurrentShift;
        }

        /// <summary>A new night begins: pay the previous night's penalty (less sanity, drain stacks
        /// already up, an anomaly already roaming), then clear it. No-op after a clean night.</summary>
        public void BeginNight()
        {
            ShiftBreaches = 0;
            LogCounts = new int[LogEntries];
            NightRecords.Clear();
            LogSigned = false;
            Stacks.Clear();
            RefillPending = 0f;
            int severity = PendingPenaltySeverity;
            if (severity > 0)
            {
                Sanity = SanityModel.Clamp(SanityModel.Max - ShiftFailureRules.StartSanityLoss(severity, PendingPenaltyNight));
                int stacks = ShiftFailureRules.StartStacks(severity);
                for (int i = 0; i < stacks; i++) Stacks.Add();
                RoamingAtStart = ShiftFailureRules.RoamingAtStart(severity);
            }
            if (PendingLogLoss > 0)
            {
                Sanity = SanityModel.Clamp(Sanity - PendingLogLoss);   // the wrong boxes of last night's log
                PendingLogLoss = 0;
            }
            PendingPenaltySeverity = 0;
            PendingPenaltyNight = 0;
            LastShiftFailure = ShiftFailure.None;
        }

        /// <summary>Only used if the night start file is gone: forget tonight's progress on this
        /// session, so Continue can still start the night from the top.</summary>
        public void RewindNight()
        {
            QueueState = null;
            NightRecords.Clear();
            LogSigned = false;
            if (TutorialStep != TutorialStep.Done) TutorialStep = TutorialSteps.First;   // a lesson starts over with its night
            VisitorIndexAtSave = 0;
            ShiftClockMinutes = 0f;
            AdminAllowed = false;
            AdminRejected = false;
            AdminTalk = new AdminTalkState();
            Hunt = HuntState.None;
            HuntCatches = 0;
            HuntSecondsLeft = -1f;
            RunFinished = false;
        }

        public ClueDiscoveryResult DiscoverClue(string key)
        {
            var res = Clues.Discover(key);
            if (res.FirstTime && res.Def != null) ApplySanity(res.Def.SanityDelta);
            return res;
        }

        public void MarkRoomInvestigated(RoomId room) => Clues.MarkRoomInvestigated(room);

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

        public EndingRules.Evaluation EvaluateEnding()
        {
            // the night's books close: anomalies still outstanding count as breaches, so
            // patrol feeds the ending. Folded EXACTLY ONCE per run - the
            // pool is closed with the books, so a repeated evaluation (ending screen, summary,
            // a resumed save) can never count the same outstanding anomalies twice.
            if (CompletedShifts >= ShiftPlan.TotalShifts && !PoolFolded)
            {
                Breaches += Pool.Pending;
                Pool.Seed(null);
                PoolFolded = true;
            }
            return EndingRules.Evaluate(this);
        }

        /// <summary>True once the outstanding patrol pool has been folded into the breach count.</summary>
        public bool PoolFolded = false;


        public bool LoseThresholdReached =>
            AdminAllowed || Hunt == HuntState.Lost || Breaches >= EndingRules.LoseThreshold ||
            FalseRejections >= EndingRules.LoseThreshold;
    }

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

        public static SettingsData Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.Split(',');
            if (p.Length < 7) return null;
            var d = new SettingsData
            {
                MasterVolume = F(p[0]), MusicVolume = F(p[1]), SfxVolume = F(p[2]),
                TextScale = int.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture),
                ReduceMotion = p[4] == "1", PhotosensitivitySafe = p[5] == "1", Captions = p[6] == "1",
            };
            if (p.Length >= 10)
            {
                d.Fullscreen = p[7] != "0";
                int w, h;
                if (int.TryParse(p[8], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out w) &&
                    int.TryParse(p[9], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out h))
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

    [Serializable]
    public class RecordsData
    {
        public List<string> EndingsSeen = new List<string>(); // "win", "lose", "secret"
    }
}
