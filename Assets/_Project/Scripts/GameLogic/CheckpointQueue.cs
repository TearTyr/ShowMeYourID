using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Queue tuning knobs — fed from GameConfigSO in production. Tests may
    /// override the live instance; that is a test-only path, never a shipping value.</summary>
    public class QueueTuning
    {
        public float ArrivalMinSeconds = 3f;
        public float ArrivalMaxSeconds = 5f;
        public int PressureWarnAt = 5;
        public int OverwhelmedAt = 7;
        public float MissingStudentChance = 0.25f;
        public int MissingStudentLimit = 1;
        // While the window is busy (an inspection is live) the arrival clock runs at this
        // multiplier. 1 = never hurries (old behaviour / tests); 2 = dawdling grows the line.
        public float BusyHurryMultiplier = 1f;

        public QueueTuning Clone() => (QueueTuning)MemberwiseClone();

        public float RollInterval(GameRng rng)
        {
            float min = Math.Min(ArrivalMinSeconds, ArrivalMaxSeconds);
            float max = Math.Max(ArrivalMinSeconds, ArrivalMaxSeconds);
            return min + rng.NextFloat() * (max - min);
        }
    }

    /// <summary>Flat, serializable queue state (SaveFormat writes it as the queue* keys).
    /// Line membership is stored as VisitorIndex values — unique within a Normal-mode
    /// shift roster, which is regenerated identically from the run seed on load.</summary>
    [Serializable]
    public class QueueSnapshot
    {
        public bool Started;
        public int RosterCursor;
        public int Arrived;
        public int Processed;
        public float TimerSeconds;
        public List<int> WaitingIdx = new List<int>();
        public int ActiveIdx = -1;
        public List<int> VanishedIdx = new List<int>();
        public int MissingIncidents;
        public int BudgetMask;
        public bool Overwhelmed;
        public bool ResolutionPending;   // quit happened between the stamp and the banner end

        public bool HasProgress => Started || Arrived > 0 || WaitingIdx.Count > 0 || ActiveIdx >= 0;
    }

    /// <summary>What one Tick / ForceNextArrival / ResumePresentation changed. The queue is
    /// pure logic: it reports, the owner (desk view) presents, sounds and toasts.</summary>
    public struct QueueTickResult
    {
        public VisitorData ToWindow;      // arrived with the window free — present now
        public VisitorData ToLine;        // arrived behind an occupied window — enqueued
        public VisitorData Vanished;      // missing-student victim (with MissingStudent)
        public bool MissingStudent;       // a missing-student incident fired this call
        public bool LineChanged;          // waiting line membership changed — rebuild the strip
        public bool PressureWarning;      // crossed PressureWarnAt (once per shift)
        public bool Overwhelmed;          // crossed OverwhelmedAt (once — the queue collapses)
        public bool Any => ToWindow != null || ToLine != null || MissingStudent ||
                           PressureWarning || Overwhelmed || LineChanged;
    }

    /// <summary>The checkpoint arrival queue (Phase 9). Separates the concepts the old
    /// single roster index conflated:
    ///   roster source   — visitors who have not arrived yet (finite list or run generator),
    ///   waiting line    — arrived, not yet at the window (strict FIFO),
    ///   active visitor  — the one inspection the window holds,
    ///   arrival clock   — randomized interval, armed only while a shift has started.
    /// Owned by GameRoot so it survives scene reloads; ticked by the desk view on scaled
    /// time, so pause (timeScale 0) and patrol (view destroyed) freeze it with no leaks.
    /// Missing-student incidents are a Normal-mode (finite roster) story mechanic.</summary>
    public class CheckpointQueue
    {
        private List<VisitorData> _roster;             // Normal mode source (null in run modes)
        private Func<VisitorData> _runSource;          // Endless/Challenge generator
        private GameRng _rng = new GameRng(1);
        private int _baseSeed = 1;
        private readonly Queue<VisitorData> _waiting = new Queue<VisitorData>();
        private readonly HashSet<VisitorData> _vanished = new HashSet<VisitorData>();
        private bool _warned;                          // pressure warning edge, once per shift

        public QueueTuning Tuning { get; private set; } = new QueueTuning();

        public bool Started { get; private set; }
        public VisitorData Active { get; private set; }
        public int WaitingCount => _waiting.Count;
        public IEnumerable<VisitorData> WaitingLine => _waiting;
        public int ArrivalCount { get; private set; }
        public int ProcessedCount { get; private set; }
        public int MissingIncidents { get; private set; }
        public bool Collapsed { get; private set; }
        public int RosterCursor { get; private set; }
        /// <summary>Seconds until the next arrival; negative = clock not armed.</summary>
        public float TimerSeconds { get; private set; } = -1f;
        /// <summary>Questions already asked at the window, synced by the desk view so a
        /// mid-inspection quit/continue cannot refund the budget.</summary>
        public int ActiveBudgetMask { get; set; }

        public bool IsAttached => _roster != null || _runSource != null;
        public bool InfiniteSource => _runSource != null;

        /// <summary>Visitors the source can still produce (vanished ones never arrive).</summary>
        public int RemainingSource
        {
            get
            {
                if (InfiniteSource) return int.MaxValue;
                if (_roster == null) return 0;
                int n = 0;
                for (int i = RosterCursor; i < _roster.Count; i++)
                    if (!_vanished.Contains(_roster[i])) n++;
                return n;
            }
        }

        public bool SourceExhausted => !InfiniteSource && RemainingSource == 0;

        /// <summary>Roster size minus missing students; -1 when the source is infinite.</summary>
        public int EffectiveTotal
        {
            get
            {
                if (InfiniteSource || _roster == null) return -1;
                return _roster.Count - _vanished.Count;
            }
        }

        /// <summary>The shift is only over when the source is dry, the line is empty and
        /// the window is free — never merely because the line ran dry mid-shift.</summary>
        public bool ShiftComplete => Started && !InfiniteSource && _roster != null &&
                                     SourceExhausted && _waiting.Count == 0 && Active == null;

        /// <summary>0 = normal, 1 = at/over the pressure warning, 2 = at/over overwhelmed.</summary>
        public int PressureLevel
        {
            get
            {
                if (_waiting.Count >= Tuning.OverwhelmedAt) return 2;
                if (_waiting.Count >= Tuning.PressureWarnAt) return 1;
                return 0;
            }
        }

        // ---------------------------------------------------------------- attach

        /// <summary>Normal mode: a finite pre-generated roster is the arrival source.</summary>
        public void AttachRoster(List<VisitorData> roster, QueueTuning tuning, int seed)
        {
            _roster = roster ?? new List<VisitorData>();
            _runSource = null;
            AttachCommon(tuning, seed);
        }

        /// <summary>Endless/Challenge: visitors are generated on demand, forever.</summary>
        public void AttachRunSource(Func<VisitorData> generator, QueueTuning tuning, int seed)
        {
            _roster = null;
            _runSource = generator;
            AttachCommon(tuning, seed);
        }

        private void AttachCommon(QueueTuning tuning, int seed)
        {
            Tuning = tuning ?? new QueueTuning();
            _baseSeed = seed;
            _rng = new GameRng(seed);
            _waiting.Clear();
            _vanished.Clear();
            _warned = false;
            Started = false;
            Active = null;
            ArrivalCount = 0;
            ProcessedCount = 0;
            MissingIncidents = 0;
            Collapsed = false;
            ResolutionPending = false;
            RosterCursor = 0;
            TimerSeconds = -1f;
            ActiveBudgetMask = 0;
        }

        // ---------------------------------------------------------------- clock

        /// <summary>The sticky-note click: the shift begins, arrivals may start.</summary>
        public void MarkStarted() => Started = true;

        /// <summary>Deliver the next arrival now (first visitor of a shift, or a resume
        /// with an expired clock) and arm the interval that follows.</summary>
        public QueueTickResult ForceNextArrival()
        {
            var r = new QueueTickResult();
            if (!Started || Collapsed || RemainingSource <= 0) return r;
            TryArrive(ref r);
            ArmTimer();
            return r;
        }

        /// <summary>Advance the arrival clock by scaled dt. At most one arrival per call;
        /// overshoot carries into the next interval so a stalled frame cannot swallow
        /// arrivals or double-fire them.</summary>
        public QueueTickResult Tick(float dt)
        {
            var r = new QueueTickResult();
            if (!Started || Collapsed || dt <= 0f) return r;
            if (TimerSeconds < 0f)
            {
                if (RemainingSource <= 0) return r;   // dry source: the clock stays idle
                ArmTimer();
            }
            // the line hurries only while the window is busy: lingering at a visitor is what
            // makes the yard fill up. A free window lets the base interval breathe.
            float hurry = Active != null ? Tuning.BusyHurryMultiplier : 1f;
            TimerSeconds -= dt * hurry;
            if (TimerSeconds <= 0f && RemainingSource > 0)
            {
                TryArrive(ref r);
                if (RemainingSource > 0) TimerSeconds += Tuning.RollInterval(_rng);
                else TimerSeconds = -1f;
            }
            return r;
        }

        private void ArmTimer()
        {
            TimerSeconds = RemainingSource > 0 ? Tuning.RollInterval(_rng) : -1f;
        }

        // ---------------------------------------------------------------- arrivals

        private void TryArrive(ref QueueTickResult r)
        {
            var v = InfiniteSource ? _runSource() : TakeFromRoster();
            if (v == null) return;
            ArrivalCount++;
            if (Active == null) { Active = v; r.ToWindow = v; }
            else { _waiting.Enqueue(v); r.ToLine = v; }
            r.LineChanged = true;
            EvaluateMissingStudent(v, ref r);
            EvaluatePressure(ref r);
        }

        private VisitorData TakeFromRoster()
        {
            if (_roster == null) return null;
            while (RosterCursor < _roster.Count && _vanished.Contains(_roster[RosterCursor]))
                RosterCursor++;                       // vanished students never arrive
            if (RosterCursor >= _roster.Count) return null;
            return _roster[RosterCursor++];
        }

        // ---------------------------------------------------------------- verdicts

        /// <summary>True between the stamp (resolution committed) and the post-banner
        /// completion. Snapshotted, so a quit mid-banner can never restore the resolved
        /// visitor as stampable again.</summary>
        public bool ResolutionPending { get; private set; }

        /// <summary>The stamp moment: the verdict is permanent, so the processed count and
        /// budget consumption are committed immediately — even if the player quits during
        /// the banner.</summary>
        public void CommitActiveResolution()
        {
            if (Active == null || ResolutionPending) return;
            ResolutionPending = true;
            ProcessedCount++;
            ActiveBudgetMask = 0;
        }

        /// <summary>The window visitor leaves (after the banner): promotes the oldest
        /// waiting visitor immediately — no second wait — and returns it, or null when the
        /// window is now free for the arrival clock. Safe to call without a prior commit
        /// (it then counts the resolution itself, exactly once).</summary>
        public VisitorData CompleteActive()
        {
            if (Active == null) return null;
            if (!ResolutionPending) ProcessedCount++;
            ResolutionPending = false;
            ActiveBudgetMask = 0;
            Active = null;
            if (_waiting.Count > 0) Active = _waiting.Dequeue();
            return Active;
        }

        /// <summary>Story completion (the administrator encounter resolving): the night is
        /// over — everyone still waiting is sent home and the window closes without
        /// counting anyone as processed. Returns how many left the line.</summary>
        public int AbandonLine()
        {
            int n = _waiting.Count;
            _waiting.Clear();
            Active = null;
            ResolutionPending = false;
            ActiveBudgetMask = 0;
            return n;
        }

        /// <summary>Rebuild the presentation after the desk view was recreated (patrol
        /// return, save/continue): promote a waiting head or deliver an expired-clock
        /// arrival so the window is never dead while the source can produce.</summary>
        public QueueTickResult ResumePresentation()
        {
            var r = new QueueTickResult();
            if (!Started || Collapsed) return r;
            if (Active == null && _waiting.Count > 0)
            {
                Active = _waiting.Dequeue();
                r.ToWindow = Active;
                r.LineChanged = true;
            }
            else if (Active == null && RemainingSource > 0 && TimerSeconds <= 0f)
            {
                TryArrive(ref r);
                ArmTimer();                       // the resumed arrival must not chain-fire
            }
            if (TimerSeconds < 0f && RemainingSource > 0) ArmTimer();
            return r;
        }

        // ---------------------------------------------------------------- pressure

        private void EvaluatePressure(ref QueueTickResult r)
        {
            int level = PressureLevel;
            if (level >= 2)
            {
                if (Collapsed) return;
                Collapsed = true;                     // once: no further arrivals, ever
                r.Overwhelmed = true;
            }
            else if (level >= 1 && !_warned)
            {
                _warned = true;                       // crossed-threshold edge, not per-frame
                r.PressureWarning = true;
            }
        }

        // ---------------------------------------------------------------- missing student

        private void EvaluateMissingStudent(VisitorData arrived, ref QueueTickResult r)
        {
            if (InfiniteSource || _roster == null) return;      // story mechanic: finite rosters
            if (!arrived.IsAnomaly || arrived.IsAdministrator) return;
            if (MissingIncidents >= Tuning.MissingStudentLimit) return;
            if (Tuning.MissingStudentChance <= 0f) return;
            if (!_rng.Chance(Tuning.MissingStudentChance)) return;

            bool fromLine;
            var victim = PickVictim(out fromLine);
            if (victim == null) return;               // nobody eligible left — nothing is consumed
            _vanished.Add(victim);
            if (fromLine) ArrivalCount--;             // they slipped out of the night's books:
                                                      // arrival progression updates with the counts (§9.6)
            MissingIncidents++;
            r.MissingStudent = true;
            r.Vanished = victim;
            r.LineChanged = true;
        }

        /// <summary>Prefer a legitimate visitor already waiting; otherwise a legitimate
        /// visitor who has not arrived yet. Never the active visitor, an already processed
        /// visitor, an anomaly or the district administrator; never the same student twice.</summary>
        private VisitorData PickVictim(out bool fromLine)
        {
            fromLine = false;
            var candidates = new List<VisitorData>();
            foreach (var w in _waiting)
                if (IsVictimEligible(w)) candidates.Add(w);
            if (candidates.Count > 0)
            {
                var victim = candidates[_rng.NextInt(0, candidates.Count)];
                RebuildWaitingWithout(victim);
                fromLine = true;
                return victim;
            }
            for (int i = RosterCursor; i < _roster.Count; i++)
                if (IsVictimEligible(_roster[i])) candidates.Add(_roster[i]);
            if (candidates.Count == 0) return null;
            return candidates[_rng.NextInt(0, candidates.Count)];   // stays in the roster; the cursor skips it
        }

        private bool IsVictimEligible(VisitorData v) =>
            v != null && !v.IsAnomaly && !v.IsAdministrator && v != Active && !_vanished.Contains(v);

        private void RebuildWaitingWithout(VisitorData victim)
        {
            var keep = new Queue<VisitorData>();
            foreach (var w in _waiting) if (w != victim) keep.Enqueue(w);
            _waiting.Clear();
            foreach (var w in keep) _waiting.Enqueue(w);
        }

        // ---------------------------------------------------------------- persistence

        public QueueSnapshot BuildSnapshot()
        {
            var s = new QueueSnapshot
            {
                Started = Started,
                RosterCursor = RosterCursor,
                Arrived = ArrivalCount,
                Processed = ProcessedCount,
                TimerSeconds = TimerSeconds,
                ActiveIdx = Active != null ? Active.VisitorIndex : -1,
                MissingIncidents = MissingIncidents,
                BudgetMask = ActiveBudgetMask,
                Overwhelmed = Collapsed,
                ResolutionPending = ResolutionPending,
            };
            if (!InfiniteSource)      // run modes never restore a mid-run line (they restart fresh)
            {
                foreach (var w in _waiting) s.WaitingIdx.Add(w.VisitorIndex);
                foreach (var v in _vanished) s.VanishedIdx.Add(v.VisitorIndex);
                s.WaitingIdx.Sort();  // stable file output; FIFO order is restored by ascending index below
                s.VanishedIdx.Sort();
            }
            return s;
        }

        /// <summary>Silent restore (no arrival hooks): the roster must already be attached
        /// and identically generated — VisitorIndex maps back to the same VisitorData.</summary>
        public void RestoreFromSnapshot(QueueSnapshot s, int reseedKey)
        {
            if (s == null) return;
            _rng = new GameRng(_baseSeed ^ reseedKey);   // interval stream restarts; bounds hold
            Started = s.Started;
            RosterCursor = ClampCursor(s.RosterCursor);
            ArrivalCount = Math.Max(0, s.Arrived);
            ProcessedCount = Math.Max(0, s.Processed);
            TimerSeconds = s.TimerSeconds;
            MissingIncidents = Math.Max(0, s.MissingIncidents);
            ActiveBudgetMask = s.BudgetMask;
            Collapsed = s.Overwhelmed;
            ResolutionPending = s.ResolutionPending;
            _waiting.Clear();
            _vanished.Clear();
            foreach (int idx in s.VanishedIdx)
            {
                var v = ByIndex(idx);
                if (v != null) _vanished.Add(v);
            }
            // FIFO order == ascending arrival order == ascending roster index (the cursor
            // walks the roster forward), so sorted indices restore the exact line order.
            var ordered = new List<int>(s.WaitingIdx);
            ordered.Sort();
            foreach (int idx in ordered)
            {
                var v = ByIndex(idx);
                if (v != null && !_vanished.Contains(v)) _waiting.Enqueue(v);
            }
            Active = ByIndex(s.ActiveIdx);
            if (Active != null && _vanished.Contains(Active)) Active = null;
            // reconstruct the warning edge: a line saved at/over the threshold had already
            // warned; a shorter line can still earn its first warning after the resume.
            _warned = Collapsed || _waiting.Count >= Tuning.PressureWarnAt;
        }

        /// <summary>Pre-queue save fallback: resume as if the first `resumeFromVisitor`
        /// roster entries had already been presented, window empty, clock ready.</summary>
        public void RestoreLegacy(int resumeFromVisitor)
        {
            Started = true;
            RosterCursor = ClampCursor(resumeFromVisitor);
            ArrivalCount = RosterCursor;
            ProcessedCount = RosterCursor;
            _warned = true;
            TimerSeconds = 0f;                           // ResumePresentation delivers immediately
        }

        private int ClampCursor(int v) =>
            _roster == null ? Math.Max(0, v) : Math.Max(0, Math.Min(v, _roster.Count));

        private VisitorData ByIndex(int visitorIndex)
        {
            if (_roster == null || visitorIndex < 0) return null;
            foreach (var v in _roster)
                if (v.VisitorIndex == visitorIndex) return v;
            return null;
        }
    }
}
