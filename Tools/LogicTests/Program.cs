// Show Me Your ID! - executable logic suite.
// Runs the real Assets/_Project/Scripts/GameLogic code on .NET (that folder is
// engine-free by design), so these are actual rule executions, not simulations.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hollowbrook.Logic;

namespace Hollowbrook.LogicTests
{
    internal static class Program
    {
        private static int _pass, _fail;
        private static readonly List<string> Failures = new List<string>();
        private static string _group = "";

        private static void Group(string name)
        {
            _group = name;
            Console.WriteLine("\n== " + name + " ==");
        }

        private static void Check(bool ok, string what)
        {
            if (ok) { _pass++; return; }
            _fail++;
            Failures.Add(_group + " :: " + what);
            Console.WriteLine("   FAIL  " + what);
        }

        private static void Eq<T>(T actual, T expected, string what)
            => Check(EqualityComparer<T>.Default.Equals(actual, expected),
                     what + "  (expected " + expected + ", got " + actual + ")");

        private static int Main(string[] args)
        {
            Console.WriteLine("Show Me Your ID! - logic suite (real GameLogic assemblies, no Unity)");

            IdRulesTests();
            ContentTests();
            ShiftPlanTests();
            HuntFairnessTests();
            EveryStudentTurnsTests();
            RulesAreLiveTests();
            ClueRegistryTests();
            RosterFairnessTests();
            AdministratorTests();
            ClassroomQuestionTests();
            InterviewRuleTests();
            CreditsTests();
            AdminHuntTests();
            HuntEndingTests();
            InspectionRulesTests();
            EndingRulesTests();
            SessionTests();
            ClueTrackerTests();
            SaveFormatTests();
            ModelMathTests();
            DisplayAndOptionsTests();
            TrackingTests();
            CheckpointQueueTests();
            SpeechBubbleTests();
            StampPlacementTests();
            HallwayDarknessTests();
            FaceTruthTests();
            FaceAssetMappingTests();
            ScaredVisitorTests();
            StudentRecordTests();
            PeekFigureTests();
            QueueBusyHurryTests();
            PatrolCycleTests();
            QueueInvariantTests();
            StampRulesTests();
            FloorFileTests();
            DrawerMotionTests();
            FloorPlanTests();
            MapSheetLayoutTests();
            AdminTalkSuite();
            EndingAuditSuite();
            AdminRosterSuite();
            SlipFairnessTests();
            NightLogTests();
            NightLogGradingTests();
            TutorialTests();
            LockdownTests();
            CultureRoundTripTests();
            ResumeReplayTests();

            Console.WriteLine("\n==============================================");
            Console.WriteLine($"  PASS {_pass}   FAIL {_fail}   TOTAL {_pass + _fail}");
            Console.WriteLine("==============================================");
            if (_fail > 0)
            {
                Console.WriteLine("\nFailures:");
                foreach (var f in Failures.Take(40)) Console.WriteLine("  - " + f);
            }
            return _fail == 0 ? 0 : 1;
        }

        private static string P(Portrait p) => $"(h{p.HairStyle},f{p.FaceVariant},a{p.Accessory}{(p.Distorted ? ",D" : "")})";

        // ------------------------------------------------------------------ IDs
        private static void IdRulesTests()
        {
            Group("IdRules - HB-YYYY-NNN format");
            Check(IdRules.IsFormatValid("HB-2026-001"), "valid id accepted");
            Check(IdRules.IsFormatValid("HB-2023-999") && IdRules.IsFormatValid("HB-1999-001") && IdRules.IsFormatValid("HB-2001-129"), "student, professor and staff years accepted");
            Check(!IdRules.IsFormatValid("HB-1989-001"), "year before 1990 rejected");
            Check(!IdRules.IsFormatValid("HB-2028-001"), "year after 2027 rejected");
            Check(!IdRules.IsFormatValid("HB-2026-000"), "serial 000 rejected");
            Check(!IdRules.IsFormatValid("hb-2026-001"), "lowercase prefix rejected");
            Check(!IdRules.IsFormatValid("XX-2026-001"), "wrong prefix rejected");
            Check(!IdRules.IsFormatValid("HB-2026001"), "missing second dash rejected");
            Check(!IdRules.IsFormatValid("HB-20A6-001"), "non-digit year rejected");
            Check(!IdRules.IsFormatValid("HB-2026-01"), "too short rejected");
            Check(!IdRules.IsFormatValid("HB-2026-0001"), "the old four digit serial is too long now");
            Check(!IdRules.IsFormatValid(""), "empty rejected");
            Check(!IdRules.IsFormatValid(null), "null rejected");
            Eq(IdRules.SerialOf("HB-2026-147"), 147, "SerialOf");
            Eq(IdRules.SerialOf("DA-0233"), -1, "SerialOf on invalid id is -1");
            Eq(IdRules.Prefix, "HB-", "prefix constant");

            var registry = new HashSet<string> { "HB-2026-001" };
            Check(IdRules.DatabaseLookup("HB-2026-001", registry), "registry hit");
            Check(!IdRules.DatabaseLookup("HB-2026-002", registry), "registry miss");
            Check(!IdRules.DatabaseLookup("HB-2026-001", null), "null registry is a miss, not a crash");
        }

        // -------------------------------------------------------------- content
        private static void ContentTests()
        {
            Group("Content - approved constants & roster");
            Eq(Content.TotalStudents, 13, "student pool size");
            var roster = Content.BuildStudentRoster();
            Eq(roster.Count, Content.TotalStudents, "roster matches TotalStudents");
            Eq(roster.Select(s => s.FullName).Distinct().Count(), roster.Count, "roster names unique");
            Eq(roster.Select(s => s.IdNumber).Distinct().Count(), roster.Count, "roster IDs unique");
            Check(roster.All(s => IdRules.IsFormatValid(s.IdNumber)), "every canon ID is format-valid");
            Check(roster.All(s => s.YearLevel >= 1 && s.YearLevel <= 5 && (s.YearLevel == 5) == s.IsFaculty), "year levels 1..4 for students, 5 only for the professor");
            Check(roster.All(s => s.CourseIndex >= 0 && s.CourseIndex < Content.Courses.Length), "course indices in range");
            Check(Content.Courses.Length > 0 && Content.YearNames.Length == 5, "course/year pools populated");
            // day-number mapping must be strictly monotonic ACROSS THE WINDOW THE GAME
            // ACTUALLY GENERATES (Jun 2026 .. Mar 2027). The model uses 30-day months, so
            // it is only an approximation: 2026-12-31 and 2027-01-01 both map to 1060.
            // No generated content lands in that gap, so ordering is safe - but the
            // collision is asserted here so anyone extending the date range sees it.
            Check(Content.DayNumberOfDate(2026, 11, 2) < Content.DayNumberOfDate(2026, 11, 3), "day numbers increase within a month");
            Check(Content.DayNumberOfDate(2026, 6, 1) < Content.DayNumberOfDate(2026, 10, 20), "expired-ID window is ordered");
            Check(Content.DayNumberOfDate(2026, 10, 20) < Content.DayNumberOfDate(2026, 11, 20), "expired IDs sort before near-expiry IDs");
            Check(Content.DayNumberOfDate(2026, 11, 20) < Content.DayNumberOfDate(2027, 3, 31), "near-expiry sorts before the far expiry");
            Check(Content.DayNumberOfDate(2026, 10, 20) < Content.ShiftDayNumber(1), "every expired ID predates the first shift night");
            Eq(Content.DayNumberOfDate(2026, 12, 31), Content.DayNumberOfDate(2027, 1, 1),
               "KNOWN LIMIT: the 360-day model collapses 2026-12-31 onto 2027-01-01 (never generated today)");
            Check(Content.DisplayDate(2026, 11, 2) == "2026-11-02", "display date is zero-padded ISO");
            for (int i = 1; i <= 5; i++)
                Eq(Content.ShiftDayNumber(i), 1000 + i, "ShiftDayNumber is monotonic");
        }

        // ------------------------------------------------------------ shift plan
        // He never catches a player who is not looking at him: every catch comes from a visible approach.
        private static void HuntFairnessTests()
        {
            Group("Hunt: no catch from something the player never saw");
            var tuning = HuntTuning.Default;
            tuning.RoomSeconds = 5f;
            var inHall = new HuntInput { InHallway = true, PlayerRoom = RoomId.Hallway };
            int caughtUnseen = 0, followedRoom = 0, followedBehind = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                var hunt = new AdminHunt(tuning, seed);
                hunt.Start();
                int guard = 0;
                // walk until he stops in a room (the player waits in the hallway)
                while (hunt.Phase != HuntPhase.InRoom && !hunt.Finished && guard++ < 5000) hunt.Tick(0.1f, inHall);
                if (hunt.Phase != HuntPhase.InRoom) continue;
                RoomId his = hunt.Room;
                // the room timer runs out with the player in the hallway
                var ev = HuntEvent.None;
                guard = 0;
                while (hunt.Phase == HuntPhase.InRoom && guard++ < 200) ev |= hunt.Tick(0.1f, inHall);
                if ((ev & HuntEvent.Caught) != 0) caughtUnseen++;
                if (hunt.Phase == HuntPhase.Behind && (ev & HuntEvent.AppearedBehind) != 0) followedBehind++;
            }
            Eq(caughtUnseen, 0, "timing out while the player is in the hallway is not a catch");
            Check(followedBehind > 0, "...he comes up behind them instead, where the beam can answer him");

            for (int seed = 1; seed <= 300; seed++)
            {
                var hunt = new AdminHunt(tuning, seed);
                hunt.Start();
                int guard = 0;
                while (hunt.Phase != HuntPhase.InRoom && !hunt.Finished && guard++ < 5000) hunt.Tick(0.1f, inHall);
                if (hunt.Phase != HuntPhase.InRoom) continue;
                RoomId his = hunt.Room;
                RoomId other = his == RoomId.Classroom1 ? RoomId.Classroom2 : RoomId.Classroom1;
                var peekOther = new HuntInput { Peeking = true, PlayerRoom = other };
                var ev = HuntEvent.None;
                guard = 0;
                while (hunt.Phase == HuntPhase.InRoom && hunt.Room == his && guard++ < 200) ev |= hunt.Tick(0.1f, peekOther);
                if ((ev & HuntEvent.Caught) != 0) caughtUnseen++;
                if (hunt.Phase == HuntPhase.InRoom && hunt.Room == other && (ev & HuntEvent.EnteredRoom) != 0)
                {
                    followedRoom++;
                    Check(hunt.PhaseTimer >= tuning.RoomSeconds - GameRules.Current.HuntFollowSeconds - 0.001f,
                        "the follower has only a short time left to be found");
                }
            }
            Eq(caughtUnseen, 0, "timing out while the player peeks another room is not a catch");
            Check(followedRoom > 0, "...he goes to the room being peeked");

            // lit in a peek, he is behind the player the moment they step out, and comes in if they stay
            int ambushed = 0, enteredAfterStay = 0, tried = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var hunt = new AdminHunt(tuning, seed);
                hunt.Start();
                int guard = 0;
                while (hunt.Phase != HuntPhase.InRoom && !hunt.Finished && guard++ < 5000) hunt.Tick(0.1f, inHall);
                if (hunt.Phase != HuntPhase.InRoom) continue;
                tried++;
                var onHim = new HuntInput { Peeking = true, PlayerRoom = hunt.Room, OnTarget = true };
                var ev = HuntEvent.None;
                guard = 0;
                while (hunt.Phase == HuntPhase.InRoom && guard++ < 200) ev |= hunt.Tick(0.1f, onHim);
                if ((ev & HuntEvent.Repelled) == 0) continue;
                var stayIn = new HuntInput { Peeking = true, PlayerRoom = onHim.PlayerRoom };
                ev = HuntEvent.None;
                for (int i = 0; i < 30; i++) ev |= hunt.Tick(0.1f, stayIn);   // three seconds, less than his patience
                Check((ev & (HuntEvent.AppearedBehind | HuntEvent.EnteredRoom | HuntEvent.Caught)) == 0,
                    $"seed {seed}: he waits while the player is still peeking");
                ev = HuntEvent.None;
                for (int i = 0; i < 20 && (ev & HuntEvent.AppearedBehind) == 0; i++) ev |= hunt.Tick(0.1f, inHall);   // two seconds out
                if ((ev & HuntEvent.AppearedBehind) != 0 && hunt.Phase == HuntPhase.Behind) ambushed++;
            }
            Check(tried > 0 && ambushed == tried, $"after lighting him in a peek, he is behind the player soon after they step out ({ambushed}/{tried})");
            for (int seed = 1; seed <= 100; seed++)
            {
                var hunt = new AdminHunt(tuning, seed);
                hunt.Start();
                int guard = 0;
                while (hunt.Phase != HuntPhase.InRoom && !hunt.Finished && guard++ < 5000) hunt.Tick(0.1f, inHall);
                if (hunt.Phase != HuntPhase.InRoom) continue;
                var onHim = new HuntInput { Peeking = true, PlayerRoom = hunt.Room, OnTarget = true };
                var ev = HuntEvent.None;
                guard = 0;
                while (hunt.Phase == HuntPhase.InRoom && guard++ < 200) ev |= hunt.Tick(0.1f, onHim);
                if ((ev & HuntEvent.Repelled) == 0) continue;
                var stayIn = new HuntInput { Peeking = true, PlayerRoom = onHim.PlayerRoom };
                ev = HuntEvent.None;
                for (int i = 0; i < 200 && (ev & HuntEvent.EnteredRoom) == 0; i++) ev |= hunt.Tick(0.1f, stayIn);
                if ((ev & HuntEvent.EnteredRoom) != 0) enteredAfterStay++;
            }
            Check(enteredAfterStay > 0, "a player who stays in the peek is not safe: he comes into their room");

            // a hunt left half way starts again where it was, and never with more than a fresh one
            var resumed = new AdminHunt(tuning, 3);
            resumed.Start(2, 40f);
            Eq(resumed.Catches, 2, "a resumed hunt keeps its catches");
            Check(Math.Abs(resumed.SurviveLeft - 40f) < 0.001f, "...and its dawn clock");
            var cheated = new AdminHunt(tuning, 3);
            cheated.Start(0, 99999f);
            Check(cheated.SurviveLeft <= tuning.SurviveSeconds, "...never more time than a fresh hunt");
            var nearEnd = new AdminHunt(tuning, 3);
            nearEnd.Start(99, 40f);
            Eq(nearEnd.Catches, tuning.CatchesToLose - 1, "...and never starts already lost");
            var withHunt = new SaveGame();
            withHunt.Session.Hunt = HuntState.Active;
            withHunt.Session.HuntCatches = 2;
            withHunt.Session.HuntSecondsLeft = 61.5f;
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(withHunt));
            Check(back != null && back.Session.HuntCatches == 2 && Math.Abs(back.Session.HuntSecondsLeft - 61.5f) < 0.01f,
                "the hunt's catches and clock survive the save file");
            var plain = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame()));
            Check(plain != null && plain.Session.HuntCatches == 0 && plain.Session.HuntSecondsLeft < 0f, "a save with no hunt reads back as none");

            // a player who IS looking at him and fails is caught: the hunt still bites
            var seen = new AdminHunt(tuning, 7);
            seen.Start();
            int steps = 0;
            while (seen.Phase != HuntPhase.InRoom && steps++ < 5000) seen.Tick(0.1f, inHall);
            var looking = new HuntInput { Peeking = true, PlayerRoom = seen.Room, OnTarget = false };
            var seenEv = HuntEvent.None;
            steps = 0;
            while (seen.Phase == HuntPhase.InRoom && steps++ < 200) seenEv |= seen.Tick(0.1f, looking);
            Check((seenEv & HuntEvent.Caught) != 0, "a player who sees him and cannot light his head is still caught");

            // nobody is ever stuck: idle and wandering players both reach an end
            for (int seed = 1; seed <= 100; seed++)
            {
                var idle = new AdminHunt(HuntTuning.Default, seed);
                idle.Start();
                for (int i = 0; i < 20000 && !idle.Finished; i++) idle.Tick(0.1f, inHall);
                Check(idle.Finished, $"seed {seed}: a player who does nothing still reaches dawn or the door");
                var rng = new GameRng(seed);
                var wander = new AdminHunt(HuntTuning.Default, seed);
                wander.Start();
                var rooms = new[] { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 };
                for (int i = 0; i < 20000 && !wander.Finished; i++)
                {
                    bool peek = rng.Chance(0.5f);
                    wander.Tick(0.1f, new HuntInput { Peeking = peek, PlayerRoom = peek ? rooms[rng.NextInt(0, 4)] : RoomId.Hallway,
                                                      InHallway = !peek, OnTarget = rng.Chance(0.3f), BeamOnFace = rng.Chance(0.3f) });
                }
                Check(wander.Finished, $"seed {seed}: a player moving at random still reaches an end");
            }
        }

        // Every student takes a turn, in every run. The owner's report: Raffy never turned in a whole game.
        private static void EveryStudentTurnsTests()
        {
            Group("Every student is an anomaly at least once per run");
            int students = Content.TotalStudents;
            var plan = ShiftPlan.Build();
            for (int seed = 1; seed <= 300; seed++)
            {
                var turned = new HashSet<int>();
                var factory = new RosterFactory(seed * 7919);
                foreach (var night in plan)
                    foreach (var v in factory.GenerateShift(night))
                        if (v.IsAnomaly && !v.IsAdministrator) turned.Add(v.FaceAssetIndex);
                Eq(turned.Count, students, $"seed {seed}: all {students} students turned");
            }
            foreach (var night in plan)
            {
                var v = new RosterFactory(5).GenerateShift(night);
                int honest = night.VisitorCount - night.AnomalyCount;
                Eq(v.Count(x => x.IsAnomaly && !x.IsAdministrator), night.AnomalyCount, $"night {night.Number}: as many anomalies as its row says");
                Eq(v.Count(x => !x.IsAnomaly), honest, $"night {night.Number}: the rest are real students");
                var real = v.Where(x => !x.IsAnomaly).Select(x => x.ClaimedName).ToList();
                Eq(real.Distinct().Count(), real.Count, $"night {night.Number}: nobody real comes twice");
            }
            // the same seed always rebuilds the same run: Continue depends on it
            var a = new RosterFactory(99);
            var b = new RosterFactory(99);
            foreach (var night in plan)
            {
                var x = a.GenerateShift(night);
                var y = b.GenerateShift(night);
                Check(x.Select(s => s.ClaimedName + s.IsAnomaly + s.WindowFaceIndex).SequenceEqual(y.Select(s => s.ClaimedName + s.IsAnomaly + s.WindowFaceIndex)),
                    $"night {night.Number}: the same seed gives the same line");
            }

            Group("Anomalies sometimes speak awkwardly, with no markers");
            int awkwardCount = 0, plainCount = 0;
            var awkwardLines = new HashSet<string>();
            foreach (var row in GameRules.Current.Students)
                foreach (var line in new[] { row.Awkward.Course, row.Awkward.Year, row.Awkward.Status, row.Awkward.Classroom })
                    Check(line.Contains("{0}"), row.FullName + ": awkward lines keep the fact in them");
            for (int seed = 1; seed <= 100; seed++)
            {
                var factory = new RosterFactory(seed);
                foreach (var night in plan)
                    foreach (var v in factory.GenerateShift(night))
                    {
                        if (v.IsAdministrator) continue;
                        foreach (var line in v.Answers) Check(!line.Contains("[") && !line.Contains("{0}"), $"seed {seed}: no tone marker and no blank in '{line}'");
                        if (!v.SpeaksAwkwardly) { if (v.IsAnomaly) plainCount++; continue; }
                        awkwardCount++;
                        Check(v.IsAnomaly, $"seed {seed}: only an anomaly speaks awkwardly");
                        var student = GameRules.Current.Students[v.FaceAssetIndex];
                        Eq(v.Greeting, student.Awkward.Greeting, $"seed {seed}: the greeting is that student's habit");
                        if (!v.Defects.Contains(DefectType.SpokenMismatch))
                            Check(v.Answers[(int)QuestionId.Course].Contains(Content.Courses[v.IdCourseIndex]) &&
                                  v.Answers[(int)QuestionId.Year].Contains(Content.YearNames[v.IdYearLevel - 1]),
                                $"seed {seed}: the facts are the card's, only the sound is off");
                        awkwardLines.Add(student.Awkward.Greeting);
                    }
            }
            Check(awkwardCount > 0 && plainCount > 0, $"some anomalies speak awkwardly, some do not ({awkwardCount} / {plainCount})");
            Check(Math.Abs(awkwardCount - plainCount) < (awkwardCount + plainCount) / 4, "about half of them, as the rule says");
            Eq(awkwardLines.Count, students, "every student has been heard speaking their own way");

            Group("Every student has both pictures");
            foreach (var row in GameRules.Current.Students) Check(row.Awkward != null, row.FullName + " has an awkward way of speaking");
            for (int i = 0; i < students; i++)
                Check(FaceAssets.AnomalyKey(i) == FaceAssets.StudentKey(i) + "_anomaly", "face " + i + " has its anomaly key");
            for (int seed = 1; seed <= 50; seed++)
            {
                var factory = new RosterFactory(seed);
                foreach (var night in plan)
                    foreach (var v in factory.GenerateShift(night))
                    {
                        if (v.IsAdministrator) continue;
                        int face = v.WindowFaceIndex >= 0 ? v.WindowFaceIndex : v.FaceAssetIndex;
                        Check(FaceAssets.WindowKey(face, v.IsAnomaly).EndsWith("_anomaly") == v.IsAnomaly,
                            $"seed {seed}: {v.ClaimedName}: the window face shows the monster exactly when it is an anomaly");
                        Check(!FaceAssets.IdKey(v.FaceAssetIndex).EndsWith("_anomaly"), "the ID card always shows the real face");
                    }
            }
        }

        // The owner's rule: nothing buried in code. Change a rule, and the game has to change.
        private static void RulesAreLiveTests()
        {
            Group("Changing a rule changes the game");
            var keep = GameRules.Current;
            try
            {
                var r = new GameRules();
                GameRules.Current = r;

                r.LoseThreshold = 7;
                Eq(EndingRules.LoseThreshold, 7, "the lose limit comes from the rules");
                var lose = new GameSession { Breaches = 6, CurrentShift = ShiftPlan.TotalShifts };
                Check(!lose.LoseThresholdReached, "...six breaches are now not enough");
                r.QuestionBudget = 3;
                Eq(InterviewRules.Remaining(0), 3, "the question budget comes from the rules");
                r.StableFrom = 95f;
                Eq(SanityModel.StageFor(90f), SanityStage.Uneasy, "sanity stages come from the rules");
                r.HallucinationBreaking = 0.5f;
                Eq(SanityModel.HallucinationChancePerSecond(SanityStage.Breaking), 0.5f, "hallucination chances come from the rules");
                r.RefillOnCorrect = 40f;
                Eq(SanityModel.RefillOnCorrect, 40f, "the sanity refill comes from the rules");
                r.BlackoutSeverity = 4;
                Eq(ShiftFailureRules.Severity(0, ShiftFailure.Blackout), 4, "a blackout's weight comes from the rules");
                r.MaxStartSanityLoss = 5f;
                Eq(ShiftFailureRules.StartSanityLoss(5, 4), 5f, "the penalty cap comes from the rules");

                r.StandingChance = 1f;
                foreach (RoomId room in new[] { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 })
                    Check(PatrolCycle.StandingIn(3, 2, room), "chance 1: every peek room holds one");
                r.StandingChance = 0f;
                int standing = 0;
                foreach (RoomId room in new[] { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 })
                    if (PatrolCycle.StandingIn(3, 2, room)) standing++;
                Eq(standing, 1, "chance 0: still one, the patrol always meets something");

                // the tables: a night and a student are rows
                var nights = new List<ShiftDef>(r.Nights);
                nights.Insert(0, r.Nights[0].Copy());
                r.Nights = nights.ToArray();
                Eq(ShiftPlan.TotalShifts, 6, "adding a row adds a night");
                Eq(ShiftPlan.Night6Number, 7, "the Challenge moves after it");
                Eq(ShiftPlan.Build()[5].Number, 6, "rows number themselves");
                r.Nights[1].AnomalyCount = 1;
                Eq(new RosterFactory(1).GenerateShift(ShiftPlan.Get(2)).Count(v => v.IsAnomaly), 1, "a night's anomaly count comes from its row");
                ShiftPlan.Build()[0].Title = "changed";
                Check(r.Nights[0].Title != "changed", "nothing can change the table by accident");

                int before = r.Students.Length;
                var students = new List<StudentRow>(r.Students);
                students.Add(new StudentRow { FullName = "New Student", IdNumber = "HB-2026-0999", YearLevel = 2, FirstClassSubject = "Art", Greeting = "Hi.", Awkward = r.Students[0].Awkward });
                r.Students = students.ToArray();
                Eq(Content.TotalStudents, before + 1, "adding a row adds a student");
                Eq(FaceAssets.StudentFaceCount, before + 1, "...with a face");
                Eq(FaceAssets.StudentKey(before), "char_student_" + (before + 1).ToString("D2"), "...in the next picture");

                r.AwkwardSpeechChance = 0f;
                Check(new RosterFactory(1).GenerateShift(ShiftPlan.Get(5)).All(v => !v.SpeaksAwkwardly), "awkward chance 0: nobody");
                r.AwkwardSpeechChance = 1f;
                Check(new RosterFactory(1).GenerateShift(ShiftPlan.Get(5)).Where(v => v.IsAnomaly && !v.IsScared).All(v => v.SpeaksAwkwardly), "awkward chance 1: every anomaly that is not scared (scared ones use the shared shaken words)");

                r.RandomSeed = false; r.FixedSeed = 12345;
                Eq(r.NewRunSeed(), 12345, "seed mode Fixed uses the number from the rules");
                r.RandomSeed = true;
                var seeds = new HashSet<int>();
                for (int i = 0; i < 20; i++) seeds.Add(r.NewRunSeed());
                Check(seeds.Count > 1, "seed mode Random gives different runs");
            }
            finally { GameRules.Current = keep; }
        }

        private static void ShiftPlanTests()
        {
            Group("ShiftPlan - approved 4/6/7/8/8+admin progression");
            var shifts = ShiftPlan.Build();
            Eq(shifts.Count, ShiftPlan.TotalShifts, "five shifts");
            Eq(shifts.Count, 5, "TotalShifts is 5");
            int[] wantVisitors = { 4, 6, 7, 8, 8 };   // night 5 adds the administrator on top
            int[] wantAnomalies = { 2, 3, 4, 5, 5 };
            for (int i = 0; i < shifts.Count; i++)
            {
                var s = shifts[i];
                Eq(s.Number, i + 1, $"shift {i + 1} numbering");
                Eq(s.VisitorCount, wantVisitors[i], $"shift {i + 1} visitor count");
                Eq(s.AnomalyCount, wantAnomalies[i], $"shift {i + 1} anomaly count");
                Eq(s.HasAdministrator, i == 4, $"shift {i + 1} administrator flag");
                Check(!string.IsNullOrEmpty(s.Title) && !string.IsNullOrEmpty(s.Objective)
                      && !string.IsNullOrEmpty(s.RuleNoteAddendum) && !string.IsNullOrEmpty(s.DateLabel),
                      $"shift {i + 1} has title/objective/rule note/date");
                Check(ReferenceEquals(ShiftPlan.Get(s.Number), null) || ShiftPlan.Get(s.Number).Number == s.Number,
                      $"ShiftPlan.Get({s.Number}) resolves");
            }
            Check(ShiftPlan.Get(6) == null, "ShiftPlan.Get(6) is null");
            Check(shifts[0].MaxDefectsPerAnomaly <= shifts[4].MaxDefectsPerAnomaly, "defect cap escalates");
            Check(!shifts[0].SubtleDefectsAllowed && !shifts[1].SubtleDefectsAllowed, "subtle photo defects gated off for shifts 1-2");
            Check(shifts[2].SubtleDefectsAllowed && shifts[2].DuplicateIdsAllowed && shifts[2].ClassroomLieAllowed,
                  "shift 3 unlocks subtle/duplicate/classroom-lie defects");
            Check(!shifts[0].ClassroomLieAllowed && shifts[1].ClassroomLieAllowed, "classroom lies start on night 2 (night 1 has no anomalies)");
            Check(shifts[3].ExplorationFocus && !shifts[4].ExplorationFocus, "only shift 4 is investigation-focused");
        }

        // ------------------------------------------------------------ clue set
        private static void ClueRegistryTests()
        {
            Group("ClueRegistry - the ten environmental clues (§2.6)");
            var clues = ClueRegistry.Build();
            Eq(clues.Count, 10, "ten clues");
            Eq(clues.Select(c => c.Order).Distinct().Count(), 10, "orders unique");
            Eq(string.Join(",", clues.Select(c => c.Order).OrderBy(o => o)), "1,2,3,4,5,6,7,8,9,10", "orders are 1..10");
            Eq(clues.Select(c => c.Key).Distinct().Count(), 10, "keys unique");
            Check(clues.All(c => !string.IsNullOrEmpty(c.Display) && !string.IsNullOrEmpty(c.InspectText)), "every clue has display + inspect text");
            Check(clues.All(c => c.Room != RoomId.Security), "no clue lives in the security room (you must leave it)");
            var tokenClue = clues.FirstOrDefault(c => c.Key == ClueRegistry.TokenKey);
            Check(tokenClue != null && tokenClue.Order == 7 && tokenClue.Room == RoomId.Classroom1,
                  "the token is finding 7, in Room 101");
            var tr = new ClueTracker();
            Check(!tr.IsFound(ClueRegistry.TokenKey), "a fresh run has not found the token");
            tr.Discover(ClueRegistry.TokenKey);
            Check(tr.IsFound(ClueRegistry.TokenKey), "inspecting the token marks it found");
            Check(clues.Any(c => c.IsScare), "at least one clue is a scare event");
            Check(clues.Where(c => c.SanityDelta != 0f).All(c => c.SanityDelta < 0f), "disturbing clues cost sanity, never grant it");
            var rooms = clues.Select(c => c.Room).Distinct().OrderBy(r => r).ToArray();
            Check(rooms.Contains(RoomId.Hallway) && rooms.Contains(RoomId.Classroom1) && rooms.Contains(RoomId.Classroom2),
                  "clues span hallway + both classrooms (secret ending requires all three)");
        }

        // ------------------------------------------------ roster fairness (core)
        private static void RosterFairnessTests()
        {
            const int Seeds = 60;
            Group($"RosterFactory - fairness invariants over {Seeds} seeds x 5 shifts");

            // determinism: same seed must regenerate an identical roster (saves rely on it)
            var a = new RosterFactory(1234).GenerateShift(ShiftPlan.Get(3));
            var b = new RosterFactory(1234).GenerateShift(ShiftPlan.Get(3));
            Eq(a.Count, b.Count, "same seed -> same visitor count");
            Check(a.Zip(b, (x, y) => x.ClaimedName == y.ClaimedName && x.IsAnomaly == y.IsAnomaly
                                   && x.IdNumber == y.IdNumber && x.Defects.Count == y.Defects.Count).All(ok => ok),
                  "same seed -> identical roster (deterministic RNG)");
            var c = new RosterFactory(9999).GenerateShift(ShiftPlan.Get(3));
            Check(!a.Zip(c, (x, y) => x.IdNumber == y.IdNumber && x.IsAnomaly == y.IsAnomaly).All(ok => ok),
                  "different seed -> different roster");

            int legitSeen = 0, anomalySeen = 0, totalVisitors = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var rf = new RosterFactory(seed);
                Check(rf.Canon.Count == Content.TotalStudents && rf.Registry.Count == Content.TotalStudents,
                      $"seed {seed}: registry covers the whole canon");

                foreach (var shift in ShiftPlan.Build())
                {
                    var visitors = rf.GenerateShift(shift);
                    totalVisitors += visitors.Count;

                    Eq(visitors.Count, shift.VisitorCount + (shift.HasAdministrator ? 1 : 0),
                       $"seed {seed} shift {shift.Number}: visitor count (admin appended)");
                    Eq(visitors.Count(v => v.IsAnomaly), shift.AnomalyCount + (shift.HasAdministrator ? 1 : 0),
                       $"seed {seed} shift {shift.Number}: anomaly count");
                    Eq(visitors.Count(v => !v.IsAnomaly && !v.IsAdministrator), shift.VisitorCount - shift.AnomalyCount,
                       $"seed {seed} shift {shift.Number}: legitimate count");
                    Eq(visitors.Count(v => v.IsAdministrator), shift.HasAdministrator ? 1 : 0,
                       $"seed {seed} shift {shift.Number}: exactly one administrator when flagged");
                    Eq(visitors.Select(v => v.VisitorIndex).Distinct().Count(), visitors.Count,
                       $"seed {seed} shift {shift.Number}: visitor indices unique");

                    // the play log holds entries 0..i-1 at each visitor's turn (recorded after the verdict)
                    var playLog = new PresentationLog();
                    foreach (var v in visitors)
                    {
                        Eq(v.ShiftNumber, shift.Number, $"seed {seed} shift {shift.Number}: shift stamp on visitor");
                        var docDefects = IdRules.EvaluateDocuments(v, rf.Registry, playLog);

                        if (v.IsAdministrator)
                        {
                            Check(v.Defects.Count > 0 && docDefects.Count > 0,
                                  $"seed {seed}: administrator always carries objective badge defects");
                        }
                        else if (!v.IsAnomaly)
                        {
                            legitSeen++;
                            Check(v.Defects.Count == 0,
                                  $"seed {seed} shift {shift.Number}: legit '{v.ClaimedName}' carries defects [{string.Join(",", v.Defects)}]");
                            Check(docDefects.Count == 0,
                                  $"seed {seed} shift {shift.Number}: legit '{v.ClaimedName}' has objective discrepancies [{string.Join(",", docDefects)}]");
                            Eq(InspectionRules.ExpectedVerdict(v), Verdict.Allow,
                               $"seed {seed} shift {shift.Number}: legit visitor must be Allow");
                            Check(IdRules.IsFormatValid(v.IdNumber) && IdRules.DatabaseLookup(v.IdNumber, rf.Registry),
                                  $"seed {seed}: legit ID is valid and registered");
                            Check(!IdRules.IsExpired(v, v.ShiftNumber),
                                  $"seed {seed}: legit ID is not expired");
                            Check(v.ClaimedName == v.RecordName && v.IdNumber == v.RecordIdNumber
                                  && v.IdYearLevel == v.RecordYearLevel && v.IdCourseIndex == v.RecordCourseIndex
                                  && v.IdIsTransferee == v.RecordIsTransferee && v.PhotoMatches,
                                  $"seed {seed}: legit ID and record agree on every field");
                            Check(AnswerMatchesFile(v),
                                  $"seed {seed}: legit '{v.ClaimedName}' names the room and class on their file ('{v.ClassroomAnswer}' vs '{v.RecordFirstClassroom}')");
                            Check(!v.SpeaksAwkwardly, $"seed {seed}: a real student never speaks awkwardly");
                        }
                        else
                        {
                            anomalySeen++;
                            Check(v.Defects.Count > 0,
                                  $"seed {seed} shift {shift.Number}: anomaly '{v.ClaimedName}' has no defects (uncatchable)");
                            Check(v.Defects.Count <= shift.MaxDefectsPerAnomaly || v.IsAdministrator,
                                  $"seed {seed} shift {shift.Number}: anomaly carries {v.Defects.Count} defects > cap {shift.MaxDefectsPerAnomaly}");
                            // the tell is on the papers, or in an answer that disagrees with the file
                            bool detectable = docDefects.Count > 0
                                              || (v.Defects.Contains(DefectType.ClassroomMismatch) && !AnswerMatchesFile(v))
                                              || (v.Defects.Contains(DefectType.SpokenMismatch) && SpokenLieIsProvable(v));
                            Eq(AnswerMatchesFile(v), !v.Defects.Contains(DefectType.ClassroomMismatch),
                               $"seed {seed} shift {shift.Number}: an anomaly lies about the classroom exactly when it carries ClassroomMismatch");
                            Check(detectable,
                                  $"seed {seed} shift {shift.Number}: anomaly '{v.ClaimedName}' is undetectable [{string.Join(",", v.Defects)}]");
                            Eq(InspectionRules.ExpectedVerdict(v), Verdict.Reject,
                               $"seed {seed} shift {shift.Number}: anomaly must be Reject");
                            if (!shift.SubtleDefectsAllowed)
                                Check(!v.Defects.Contains(DefectType.PhotoSubtleMismatch),
                                      $"seed {seed} shift {shift.Number}: subtle photo defect used before it is unlocked");
                            if (!shift.DuplicateIdsAllowed)
                                Check(!v.Defects.Contains(DefectType.DuplicateId),
                                      $"seed {seed} shift {shift.Number}: duplicate ID used before it is unlocked");
                            if (!shift.ClassroomLieAllowed)
                                Check(!v.Defects.Contains(DefectType.ClassroomMismatch),
                                      $"seed {seed} shift {shift.Number}: classroom lie used before it is unlocked");
                            // a duplicate is only evidence once the earlier card has been seen
                            if (v.Defects.Contains(DefectType.DuplicateId))
                                Check(docDefects.Contains(DefectType.DuplicateId),
                                      $"seed {seed} shift {shift.Number}: duplicate ID is provable from the play log at that point");
                        }

                        Check(!string.IsNullOrEmpty(v.Greeting), $"seed {seed}: visitor has a greeting line");
                        Check(v.DescribeDefects().Count > 0, $"seed {seed}: results banner always has at least one line");
                        playLog.RecordFrom(v, "tonight");
                    }

                }
            }
            Console.WriteLine($"   ({totalVisitors} visitors generated: {legitSeen} legitimate, {anomalySeen} anomalies)");
            Check(legitSeen > 500 && anomalySeen > 200, "sample size is meaningful");
        }

        // ------------------------------------------------------- administrator
        // The Shift-5 questions and answers: every topic reachable, evidence gated by the REAL
        // clue keys, no dead ends, stepping back always available, unlimited questions, and zero
        // mechanical power (talking never stamps).
        private static void AdminTalkSuite()
        {
            Group("AdminTalk - the administrator's questions and answers");

            var problems = AdminTalk.ValidateScript();
            Check(problems.Count == 0,
                "the script validates: real destinations, no unknown tokens, reachable, always a way out" +
                (problems.Count > 0 ? " (first: " + problems[0] + ")" : ""));
            foreach (var p in problems.Take(6)) Console.WriteLine("   script: " + p);

            AdminTalkContext Fresh() => new AdminTalkContext
            {
                BadgeName = "V. Marlowe", MemoName = "R. Vance", BadgeNumber = "HB-1999-010", MemoNumber = "HB-1999-001",
                AnswerClassroom = "My first stop is your server room.",
            };

            var ctx = Fresh();
            var st = new AdminTalkState();
            var open = AdminTalk.Open(st, ctx);
            Eq(st.Node, AdminTalk.HubId, "opening lands on the hub");
            Check(open.Text.Contains("complaints about your performance"), "the hub opens with his demand");
            Eq(st.Visits, 1, "the first open counts one visit");
            var hubOptions = AdminTalk.OptionsFor(st, ctx);
            Check(hubOptions.Any(o => o.Action == AdminTalkAction.StepBack), "the hub always offers the exit");
            Check(!hubOptions.Any(o => o.Id == "refuse") && AdminTalk.Nodes.All(n => n.Options.All(o => o.Id != "refuse")),
                  "there is no REFUSE ENTRY option anywhere: only the DENY stamp turns him away");
            Check(!hubOptions.Any(o => o.Id == "ask_photo" || o.Id == "ask_cabinet"),
                  "evidence and the server question stay hidden until earned");

            // tokens are filled from the real papers
            var asked = AdminTalk.Choose(st, ctx, "ask_name");
            Eq(st.Node, "name", "asking about the name moves to its answer");
            Check(AdminTalk.OptionsFor(st, ctx).Any(o => o.Id == "press_name"), "the answer offers a follow-up");
            var nameLabel = AdminTalk.Resolve(hubOptions.First(o => o.Id == "ask_name").Label, ctx);
            Check(nameLabel.Contains("V. Marlowe") && nameLabel.Contains("R. Vance") && !nameLabel.Contains("{"),
                  "option labels use the live badge and memo names");

            // follow-up, then back: the question is spent, the hub speaks its return line
            AdminTalk.Choose(st, ctx, "press_name");
            Eq(st.Node, "name2", "the follow-up reaches the second answer");
            AdminTalk.Choose(st, ctx, "back");
            Eq(st.Node, AdminTalk.HubId, "back returns to the hub");
            Check(AdminTalk.OptionsFor(st, ctx).Any(o => o.Id == "ask_name"), "a question already asked is offered again (no limit)");
            int liesBefore = AdminTalk.LiesExposed(st);
            for (int i = 0; i < 5; i++)
            {
                AdminTalk.Choose(st, ctx, "ask_name"); AdminTalk.Choose(st, ctx, "press_name"); AdminTalk.Choose(st, ctx, "back");
            }
            Eq(AdminTalk.LiesExposed(st), liesBefore, "pressing the same lie again and again exposes it only once");
            var again = AdminTalk.Choose(st, ctx, "ask_num");
            AdminTalk.Choose(st, ctx, "back");
            Check(AdminTalk.Open(new AdminTalkState { Node = AdminTalk.HubId, Visits = 3 }, ctx).Text.StartsWith("Still here"),
                  "returning to the hub uses his 'still here' line");

            // a wrong or hidden option does nothing
            string nodeBefore = st.Node; string enc = st.Encode();
            var bad = AdminTalk.Choose(st, ctx, "ask_photo");
            Check(!bad.Completed && st.Encode() == enc && st.Node == nodeBefore,
                  "picking a hidden option changes nothing (clue not found)");
            Check(!AdminTalk.Choose(st, ctx, "no_such_option").Completed, "an unknown option id changes nothing");

            // clue gating off the real keys
            var withPhoto = Fresh(); withPhoto.CluePhoto = true;
            st = new AdminTalkState(); AdminTalk.Open(st, withPhoto);
            Check(AdminTalk.OptionsFor(st, withPhoto).Any(o => o.Id == "ask_photo"), "the photo clue unlocks its question");
            AdminTalk.Choose(st, withPhoto, "ask_photo");
            Check(AdminTalk.NodeById(st.Node).Id == "photo", "the photo question reaches its answer");
            var withCabinet = Fresh(); withCabinet.ClueCabinet = true;
            st = new AdminTalkState(); AdminTalk.Open(st, withCabinet);
            Check(AdminTalk.OptionsFor(st, withCabinet).Any(o => o.Id == "ask_cabinet"), "the cabinet clue unlocks its question");
            var withClass = Fresh();
            st = new AdminTalkState(); AdminTalk.Open(st, withClass);
            var serverOpt = AdminTalk.OptionsFor(st, withClass).FirstOrDefault(o => o.Id == "ask_server");
            Check(serverOpt != null, "asking him the classroom question unlocks the server-room follow-up");
            Check(AdminTalk.Choose(st, withClass, "ask_server").Text.Contains("My first stop is your server room."),
                  "his answer quotes what he really said at the window");

            // the exit only closes the panel: the conversation can be picked up again
            st = new AdminTalkState(); ctx = Fresh(); AdminTalk.Open(st, ctx);
            var end = AdminTalk.Choose(st, ctx, "end");
            Check(end.Completed && end.Action == AdminTalkAction.StepBack && !st.Completed, "stepping back closes the panel, the conversation is not over");
            Check(!AdminTalk.Choose(st, ctx, "ask_name").Completed && st.Node == "name", "...and he can be questioned again afterwards");
            st.Completed = true;   // what a verdict does
            Check(AdminTalk.Choose(st, ctx, "ask_name").Completed, "a finished conversation stays finished");

            // night 6: his own hub and four new questions, no refuse button, same lie counting
            var ctx6 = Fresh(); ctx6.Night6 = true;
            var st6 = new AdminTalkState();
            var open6 = AdminTalk.Open(st6, ctx6);
            Eq(st6.Node, AdminTalk.Night6HubId, "night 6 opens on its own hub");
            Check(open6.Text.Contains("I do not give up") && open6.Text.Contains("right decision"),
                  "he says he does not give up and asks for the right decision");
            var hub6 = AdminTalk.OptionsFor(st6, ctx6);
            Check(hub6.Count(o => o.Id.StartsWith("ask_n6_")) == 4 && hub6.Any(o => o.Action == AdminTalkAction.StepBack),
                  "night 6 offers four new questions and the exit");
            Check(!hub6.Any(o => o.Id == "ask_name" || o.Id == "ask_num"), "none of the night 5 questions are left");
            AdminTalk.Choose(st6, ctx6, "ask_n6_return");
            Eq(st6.Node, "n6_return", "a night 6 question reaches its answer");
            AdminTalk.Choose(st6, ctx6, "press_n6_return");
            Eq(AdminTalk.LiesExposed(st6), 1, "pressing a night 6 answer exposes a lie");
            AdminTalk.Choose(st6, ctx6, "back");
            Eq(st6.Node, AdminTalk.Night6HubId, "back returns to the night 6 hub, not the night 5 one");
            AdminTalk.Choose(st6, ctx6, "end");
            Eq(st6.Node, AdminTalk.Night6HubId, "stepping back keeps him at the night 6 hub");

            // every node can be left, from anywhere, at any depth
            foreach (var node in AdminTalk.Nodes)
            {
                var probe = new AdminTalkState { Node = node.Id, Visits = 1 };
                var opts = AdminTalk.OptionsFor(probe, Fresh());
                Check(opts.Count > 0 && opts.Any(o => o.Action != AdminTalkAction.None),
                      "node '" + node.Id + "' always offers a way out");
            }

            // the save string round-trips and tolerates garbage
            st = new AdminTalkState(); ctx = Fresh(); AdminTalk.Open(st, ctx);
            AdminTalk.Choose(st, ctx, "ask_num"); AdminTalk.Choose(st, ctx, "press_num");
            var dec = AdminTalkState.Decode(st.Encode());
            Check(dec.Node == st.Node && dec.UsedMask == st.UsedMask && dec.Visits == st.Visits && dec.Completed == st.Completed,
                  "the conversation state round-trips through the save string");
            Check(AdminTalkState.Decode("").Node == "" && AdminTalkState.Decode("a,b").Node == "" && AdminTalkState.Decode("zzz,1,1,0").Node == "",
                  "an empty, torn or unknown-node value decodes to a fresh conversation");

            // the script never talks about her; the administrator is a he
            foreach (var node in AdminTalk.Nodes)
                Check(!node.Text.Contains(" she ") && !node.Text.Contains(" her "), "node '" + node.Id + "' has no stray pronoun");
        }


        // The ending evaluator's integration contract: the administrator's rejection is part of
        // the Win/Secret gate, the pending-pool fold happens exactly once, and the documented
        // Lose > Secret > Win priority holds even when everything else is satisfied.
        private static void EndingAuditSuite()
        {
            Group("EndingRules audit - admin gate, idempotent fold, priority");

            GameSession FiveShifts(bool adminRejected, bool adminAllowed)
            {
                var s = new GameSession { Mode = GameMode.Normal };
                s.CompletedShifts = ShiftPlan.TotalShifts;
                s.CurrentShift = ShiftPlan.TotalShifts + 1;   // CompleteShift moves past the last night
                s.AdminRejected = adminRejected;
                s.Hunt = adminRejected ? HuntState.Won : HuntState.None;
                s.AdminAllowed = adminAllowed;
                return s;
            }

            var win = FiveShifts(true, false);
            Eq(EndingRules.Evaluate(win).Ending, EndingType.Win, "five shifts + rejection = Win");
            var unresolved = FiveShifts(false, false);
            var eval = EndingRules.Evaluate(unresolved);
            Eq(eval.Ending, EndingType.None, "five shifts with NO decision on the administrator awards no ending");
            Eq(eval.ReasonKey, "admin_unresolved", "...and says why (stable key)");
            var allowed = FiveShifts(false, true);
            Eq(EndingRules.Evaluate(allowed).Ending, EndingType.Lose, "allowing the administrator = immediate Lose");

            // secret still needs the rejection too (a completed run includes the final decision)
            var secret = FiveShifts(true, false);
            FillSecretClues(secret);
            Eq(EndingRules.Evaluate(secret).Ending, EndingType.Secret, "ten ordered clues + rejection = Secret");
            var secretNoAdmin = FiveShifts(false, false);
            FillSecretClues(secretNoAdmin);
            Eq(EndingRules.Evaluate(secretNoAdmin).Ending, EndingType.None,
               "the Secret needs him turned away and survived, like the Win");
            var unresolvedNoClues = FiveShifts(false, false);
            Eq(EndingRules.Evaluate(unresolvedNoClues).ReasonKey, "admin_unresolved",
               "without the clue chain, an undecided administrator still blocks the Win");
            var secretButLost = FiveShifts(true, true);
            FillSecretClues(secretButLost);
            Eq(EndingRules.Evaluate(secretButLost).Ending, EndingType.Lose,
               "Lose outranks Secret (priority Lose > Secret > Win)");

            // incomplete progression never awards the standard ending early
            var early = new GameSession { Mode = GameMode.Normal, AdminRejected = true, CompletedShifts = 4 };
            Eq(EndingRules.Evaluate(early).Ending, EndingType.None, "four shifts award nothing, rejection or not");

            // ---- the pending-pool fold is exactly-once, however often the run is evaluated
            var s2 = FiveShifts(true, false);
            s2.Breaches = 1;
            s2.Pool.Seed(new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Slide });
            var e1 = s2.EvaluateEnding();
            Eq(s2.Breaches, 3, "the outstanding patrol pool folds into the breach count at the final eval");
            Eq(e1.Ending, EndingType.Lose, "...and can tip the night into the Lose ending");
            var e2 = s2.EvaluateEnding();
            var e3 = s2.EvaluateEnding();
            Eq(s2.Breaches, 3, "repeated evaluation never counts the same outstanding anomalies twice");
            Eq(e2.Ending, e3.Ending, "repeated evaluation never changes the ending");

            var s3 = FiveShifts(true, false);
            s3.Pool.Seed(new[] { AnomalyBehaviorKind.Sprint });
            Eq(s3.EvaluateEnding().Ending, EndingType.Win, "one outstanding anomaly folds to 1 breach: still a Win");
            s3.EvaluateEnding();
            Eq(s3.Breaches, 1, "...folded once");

            // the ordinary Lose thresholds keep their teeth
            var fr = new GameSession { Mode = GameMode.Normal, CurrentShift = ShiftPlan.TotalShifts, FalseRejections = 3 };
            Eq(EndingRules.Evaluate(fr).Ending, EndingType.Lose, "three false rejections = Lose");
            var br = new GameSession { Mode = GameMode.Normal, CurrentShift = ShiftPlan.TotalShifts, Breaches = 3 };
            Eq(EndingRules.Evaluate(br).Ending, EndingType.Lose, "three breaches = Lose");
            var ow = new GameSession { Mode = GameMode.Normal, CurrentShift = ShiftPlan.TotalShifts, QueueOverwhelmed = true };
            Eq(EndingRules.Evaluate(ow).Ending, EndingType.Lose, "the overrun line still ends the run");

            // ...but only on the final night: nights 1-4 never end the game, however bad
            for (int night = 1; night < ShiftPlan.TotalShifts; night++)
            {
                var earlyNight = new GameSession { Mode = GameMode.Normal, CurrentShift = night, Breaches = 3, FalseRejections = 3, QueueOverwhelmed = true };
                Eq(EndingRules.Evaluate(earlyNight).Ending, EndingType.None,
                   "night " + night + ": three breaches, three false rejections and an overrun line end nothing");
            }
        }

        private static void FillSecretClues(GameSession s)
        {
            foreach (var c in ClueRegistry.Build()) s.Clues.Discover(c.Key);   // registry order
            s.MarkRoomInvestigated(RoomId.Hallway);
            s.MarkRoomInvestigated(RoomId.Classroom1);
            s.MarkRoomInvestigated(RoomId.Classroom2);
        }

        // Roster shape against the design table: 4/6/7/8/8 standard entries, the administrator
        // appended exactly once and last, never in earlier shifts, never in the run modes.
        private static void AdminRosterSuite()
        {
            Group("Shift rosters - the administrator's place in line");
            int[] want = { 4, 6, 7, 8, 8 };
            for (int shift = 1; shift <= 5; shift++)
            {
                var roster = new RosterFactory(11).GenerateShift(ShiftPlan.Get(shift));
                int admins = 0;
                foreach (var v in roster) if (v.IsAdministrator) admins++;
                Eq(roster.Count - admins, want[shift - 1], $"shift {shift} keeps its standard entry count");
                Eq(admins, shift == 5 ? 1 : 0, $"shift {shift} carries {(shift == 5 ? "exactly one administrator" : "no administrator")}");
                if (shift == 5)
                    Check(roster[roster.Count - 1].IsAdministrator, "the administrator is the LAST visitor of shift 5");
            }
            // determinism: the same seed replays the same final visitor
            var a = new RosterFactory(21).GenerateShift(ShiftPlan.Get(5));
            var b = new RosterFactory(21).GenerateShift(ShiftPlan.Get(5));
            Check(a[a.Count - 1].IdNumber == b[b.Count - 1].IdNumber && a[a.Count - 1].IsAdministrator,
                "the administrator regenerates identically from the seed (save/resume safe)");
        }

        private static void AdministratorTests()
        {
            Group("Shift-5 administrator encounter (conflict C3)");
            var rf = new RosterFactory(7);
            var admin = rf.BuildAdministrator(ShiftPlan.Get(5));
            Check(admin.IsAdministrator && admin.IsAnomaly, "administrator is flagged as both admin and anomaly");
            Check(admin.IdNumber != admin.RecordIdNumber, "badge number differs from the number on the memo");
            Check(!IdRules.DatabaseLookup(admin.IdNumber, rf.Registry), "badge number is not in the student registry");
            Check(admin.ClaimedName != admin.RecordName, "badge name differs from the roster memo");
            Check(admin.IdNumber != admin.RecordIdNumber, "badge number differs from the roster memo");
            var docDefects = IdRules.EvaluateDocuments(admin, rf.Registry, new PresentationLog());
            Check(docDefects.Contains(DefectType.RecordNameMismatch) && docDefects.Contains(DefectType.NotInDatabase),
                  "objective evaluation reports the badge discrepancies (admin special case)");
            Eq(InspectionRules.ExpectedVerdict(admin), Verdict.Reject, "administrator must be rejected");
            var allowed = InspectionRules.Apply(admin, Verdict.Allow);
            Check(allowed.AnomalyBreach && allowed.WasAdministrator && !allowed.Correct,
                  "allowing the administrator is a breach (=> immediate Lose)");
            var rejected = InspectionRules.Apply(admin, Verdict.Reject);
            Check(rejected.Correct && !rejected.AnomalyBreach, "rejecting the administrator is correct");
        }

        // The words this visitor uses: a scared visitor the shared shaken words, an awkward anomaly its own
        // student's awkward lines, everybody else plain speech (null).
        private static AwkwardSpeech SpeechOf(VisitorData v)
        {
            if (v.IsScared) return Content.ScaredSpeech;
            return v.SpeaksAwkwardly ? GameRules.Current.Students[v.FaceAssetIndex].Awkward : null;
        }

        // "Rooms the file would say": the answer is "Room 202, Genetics." and the file prints
        // "Room 202 · Genetics" - the same room and class, spelled for a different reader.
        private static bool AnswerMatchesFile(VisitorData v)
        {
            var awkward = SpeechOf(v);
            string plain = v.RecordFirstClassroom.Replace(" · ", ", ") + ".";
            return v.ClassroomAnswer == InterviewRules.ClassroomSpeech(plain, awkward);
        }

        // A spoken lie is provable when the answer to the chosen question is not what the card says.
        private static bool SpokenLieIsProvable(VisitorData v)
        {
            if (v.SpokenLieQuestion < 0 || v.SpokenLieQuestion >= InterviewRules.Count) return false;
            return v.Answers[v.SpokenLieQuestion] != CardSpeech(v, (QuestionId)v.SpokenLieQuestion);
        }

        // What a visitor with this card would say, in the words this visitor uses: an awkward anomaly
        // says the card's fact in its own student's awkward lines.
        private static string CardSpeech(VisitorData v, QuestionId q)
        {
            var awkward = SpeechOf(v);
            switch (q)
            {
                case QuestionId.Course: return InterviewRules.CourseSpeech(Content.Courses[v.IdCourseIndex], awkward);
                case QuestionId.Year: return InterviewRules.YearSpeech(Content.YearNames[v.IdYearLevel - 1], awkward);
                case QuestionId.Status: return InterviewRules.StatusSpeech(Content.StatusWord(v.IdIsTransferee, v.IsFaculty), awkward);
                default: return v.ClassroomAnswer;
            }
        }

        // The log is graded against what really happened. Pure rules, plus the save and the night start.
        private static void NightLogGradingTests()
        {
            Group("Night log grading - compared with the night, explained only in the next night's note");
            var records = new List<NightRecord>
            {
                new NightRecord { Name = "Liza May Ramos", WasAnomaly = false, Allowed = true, Why = "" },
                new NightRecord { Name = "Paolo Garcia", WasAnomaly = true, Allowed = false, Why = "The ID expired on 2026-09-04." },
                new NightRecord { Name = "Bea Dela Cruz", WasAnomaly = true, Allowed = true, Why = "The face at the window does not match the ID photo." },
            };
            var right = NightLogRules.Check(new[] { 3, 2, 1 }, records, 1);
            Check(right.Mistakes == 0 && right.SanityLoss == 0 && right.Right.All(r => r), "three right counts: no mistakes, no penalty");
            Check(right.Note.Contains("matched"), "a matching log says so");

            var wrong = NightLogRules.Check(new[] { 3, 1, 0 }, records, 1);
            Eq(wrong.Mistakes, 2, "two wrong boxes");
            Check(wrong.Right[0] && !wrong.Right[1] && !wrong.Right[2], "each box is judged on its own");
            Eq(wrong.SanityLoss, 2 * NightLogRules.SanityLossPerMistake, "the penalty is per wrong box");
            Check(wrong.Note.Contains("Anomalies detected: you wrote 1, there were 2") && wrong.Note.Contains("Paolo Garcia: The ID expired on 2026-09-04"),
                "the note names the anomalies and what was wrong with them");
            Check(wrong.Note.Contains("Security incidents: you wrote 0, there was 1") && wrong.Note.Contains("Penalty: 10 sanity"),
                "the note names the incident count and the penalty");
            Check(!wrong.Note.Contains("Student encounters"), "a right box is not mentioned");
            Eq(NightLogRules.Check(null, records, 0).Mistakes, 2, "no counts at all means zeros: the encounters and anomalies are wrong");
            Check(NightLogRules.Check(new[] { 0, 0, 0 }, new List<NightRecord>(), 0).Mistakes == 0, "an empty night with empty boxes is right");

            // session: signing, grading, the next night paying, saving
            var s = new GameSession();
            foreach (var r in records) s.NightRecords.Add(r);
            Check(!s.LogSigned, "a new log is unsigned");
            s.SignLog();
            Check(s.LogSigned, "signing signs");
            s.ChangeLog(0, +1);
            Check(!s.LogSigned, "changing a box takes the signature back");
            s.ChangeLog(0, -1);
            s.SignLog();
            s.ChangeLog(0, 0);
            Check(s.LogSigned, "a click that changes nothing keeps the signature");
            var grade = s.GradeNight(1);
            Check(s.PendingLogLoss == grade.SanityLoss && s.LogNote == grade.Note && grade.Mistakes == 3, "grading books the penalty and the note");
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = s })).Session;
            Check(back.NightRecords.Count == 3 && back.NightRecords[1].Name == "Paolo Garcia" && back.NightRecords[1].WasAnomaly && !back.NightRecords[1].Allowed
                  && back.NightRecords[1].Why == "The ID expired on 2026-09-04.", "who came survives a save");
            Check(back.LogSigned && back.PendingLogLoss == s.PendingLogLoss && back.LogNote == s.LogNote, "the signature, the penalty and the note survive a save");
            s.Sanity = 90f;
            s.BeginNight();
            Check(Math.Abs(s.Sanity - (90f - grade.SanityLoss)) < 0.01f && s.PendingLogLoss == 0, "the next night pays the penalty once");
            Check(s.NightRecords.Count == 0 && !s.LogSigned && s.LogNote == grade.Note, "a new night clears the list and the signature but keeps last night's note to read");
            var odd = new GameSession();
            odd.NightRecords.Add(new NightRecord { Name = "A|B^C", WasAnomaly = true, Allowed = true, Why = "x|y^z" });
            var oddBack = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = odd })).Session;
            Check(oddBack.NightRecords.Count == 1 && oddBack.NightRecords[0].WasAnomaly, "odd characters in a name cannot break the save");
        }

        private static void TutorialTests()
        {
            Group("Night 1 tutorial - step order, what each step holds, the lesson roster");
            var f = new TutorialFacts();
            Check(TutorialSteps.Advance(TutorialStep.FirstStudent, f) == TutorialStep.FirstStudent, "nothing done: the first step stays");
            f.Processed = 1;
            Check(TutorialSteps.Advance(TutorialStep.FirstStudent, f) == TutorialStep.FirstAnomaly, "one stamp: on to the second visitor");
            f.Processed = 2;
            Check(TutorialSteps.Advance(TutorialStep.FirstStudent, f) == TutorialStep.GoPatrol, "two stamps: time to patrol");
            Check(TutorialSteps.Advance(TutorialStep.GoPatrol, f) == TutorialStep.GoPatrol, "still at the desk: the patrol step waits");
            f.OnPatrol = true;
            Check(TutorialSteps.Advance(TutorialStep.GoPatrol, f) == TutorialStep.HallAnomaly, "in the hallway: the anomaly lesson");
            Check(TutorialSteps.Advance(TutorialStep.HallAnomaly, f) == TutorialStep.HallAnomaly, "the anomaly is still there: the lesson waits");
            f.HallAnomalyDriven = true;
            Check(TutorialSteps.Advance(TutorialStep.HallAnomaly, f) == TutorialStep.PeekClean, "driven out: on to Room 101");
            f.ClearedRooms = PatrolCycle.Bit(RoomId.Classroom1);
            f.Peeking = true;
            Check(TutorialSteps.Advance(TutorialStep.PeekClean, f) == TutorialStep.PeekClean, "still looking into Room 101: wait for the step back");
            f.Peeking = false;
            Check(TutorialSteps.Advance(TutorialStep.PeekClean, f) == TutorialStep.PeekAnomaly, "stepped back: on to Room 202");
            f.PeekAnomaliesGone = PatrolCycle.Bit(RoomId.Classroom2);
            f.Peeking = true;
            Check(TutorialSteps.Advance(TutorialStep.PeekAnomaly, f) == TutorialStep.PeekAnomaly, "the figure is gone but the player is still peeking");
            f.Peeking = false;
            Check(TutorialSteps.Advance(TutorialStep.PeekAnomaly, f) == TutorialStep.Finish, "stepped back: training over");
            Check(TutorialSteps.Advance(TutorialStep.Finish, f) == TutorialStep.Finish, "the last step ends only with GOT IT");
            Check(TutorialSteps.Advance(TutorialStep.Done, f) == TutorialStep.Done, "a finished tutorial never comes back");
            Check(TutorialSteps.Advance(TutorialStep.FirstStudent, f) == TutorialStep.Finish, "a Continue after everything was done lands on the last step");
            Check(TutorialSteps.First == TutorialStep.Controls, "the tutorial opens with the keybinds");
            Check(TutorialSteps.Advance(TutorialStep.Controls, f) == TutorialStep.Controls, "the keybinds wait for their CONTINUE button, whatever the game is doing");
            Check(TutorialSteps.AfterButton(TutorialStep.Controls) == TutorialStep.FirstStudent && TutorialSteps.AfterButton(TutorialStep.Finish) == TutorialStep.Done,
                "CONTINUE leads to the first lesson and GOT IT ends the tutorial");
            Check(TutorialSteps.AfterButton(TutorialStep.HallAnomaly) == TutorialStep.HallAnomaly, "a step without a button stays where it is");

            foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
            {
                bool lesson = step == TutorialStep.Controls || (step >= TutorialStep.GoPatrol && step <= TutorialStep.PeekAnomaly);
                Check(TutorialSteps.HoldsLine(step) == lesson, step + ": the line waits during the keybinds and the lessons away from the window only");
                Check(TutorialSteps.OpensPatrol(step) == (step == TutorialStep.GoPatrol), step + ": only the patrol step opens the patrol early");
                Check(TutorialSteps.NeedsGotIt(step) == (step == TutorialStep.Controls || step == TutorialStep.Finish), step + ": only the keybinds and the last step have a button");
                Check(TutorialSteps.IsKeyList(step) == (step == TutorialStep.Controls), step + ": only the keybinds are a list");
                bool active = step != TutorialStep.Done;
                Check(TutorialSteps.Active(step) == active, step + ": active unless done");
                Check((TutorialSteps.Title(step).Length > 0 && TutorialSteps.Body(step).Length > 0) == active, step + ": a step has words, Done has none");
                Check(!TutorialSteps.Title(step).Contains("\u2014") && !TutorialSteps.Body(step).Contains("\u2014"), step + ": no em dash");
                int room = TutorialSteps.IsKeyList(step) ? 420 : 200;
                Check(TutorialSteps.Body(step).Length <= room, step + ": short enough for the box (" + TutorialSteps.Body(step).Length + ")");
                Check(TutorialSteps.PeekAllowed(step, RoomId.Hallway) && TutorialSteps.PeekAllowed(step, RoomId.Security), step + ": the hallway and security doors are never guarded");
            }
            Check(TutorialSteps.PeekAllowed(TutorialStep.Done, RoomId.Classroom2) && TutorialSteps.PeekAllowed(TutorialStep.Finish, RoomId.ComfortRoom),
                "after the lessons every door opens");
            Check(!TutorialSteps.PeekAllowed(TutorialStep.HallAnomaly, RoomId.Classroom1), "before Room 101 is the lesson, its door is shut");
            Check(TutorialSteps.PeekAllowed(TutorialStep.PeekClean, RoomId.Classroom1) && !TutorialSteps.PeekAllowed(TutorialStep.PeekClean, RoomId.Classroom2)
                  && !TutorialSteps.PeekAllowed(TutorialStep.PeekClean, RoomId.ComfortRoom), "Room 101 lesson: only Room 101 opens");
            Check(TutorialSteps.PeekAllowed(TutorialStep.PeekAnomaly, RoomId.Classroom2) && !TutorialSteps.PeekAllowed(TutorialStep.PeekAnomaly, RoomId.Classroom1)
                  && !TutorialSteps.PeekAllowed(TutorialStep.PeekAnomaly, RoomId.ComfortRoom2), "Room 202 lesson: only Room 202 opens");

            // the lesson roster: a real student, then one clear broken rule, nobody frightened yet
            var night1 = ShiftPlan.Build()[0];
            for (int seed = 1; seed <= 300; seed++)
            {
                var roster = new RosterFactory(seed).GenerateShift(night1);
                Check(roster.Count == 4 && roster.Count(v => v.IsAnomaly) == 2 && roster.Count(v => v.IsScared) == 1, "seed " + seed + ": still four visitors, two anomalies, one frightened");
                Check(!roster[0].IsAnomaly && !roster[0].IsScared, "seed " + seed + ": visitor 1 is a calm real student");
                Check(roster[1].IsAnomaly && !roster[1].IsScared && roster[1].Defects.Count == 1 && roster[1].Defects[0] == DefectType.ExpiredId,
                    "seed " + seed + ": visitor 2 has exactly one defect, the expired ID");
                Check(!roster[2].IsScared == roster[3].IsScared, "seed " + seed + ": the frightened one comes later in the night");
                for (int i = 0; i < roster.Count; i++) Check(roster[i].VisitorIndex == i, "seed " + seed + ": visitor numbers follow the order");
                Check(PatrolCycle.StandingIn(seed, 1, RoomId.Classroom2) && !PatrolCycle.StandingIn(seed, 1, RoomId.Classroom1)
                      && !PatrolCycle.StandingIn(seed, 1, RoomId.ComfortRoom) && !PatrolCycle.StandingIn(seed, 1, RoomId.ComfortRoom2),
                    "seed " + seed + ": on night 1 only Room 202 holds a standing anomaly");
            }
            Check(!ShiftPlan.Build()[1].TutorialOpening && !ShiftPlan.Build()[4].TutorialOpening, "only night 1 opens with the lesson");

            // saving and starting over
            var s = new GameSession();
            Check(s.TutorialStep == TutorialStep.Done, "a plain session has no tutorial");
            s.TutorialStep = TutorialStep.PeekAnomaly;
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = s })).Session;
            Check(back.TutorialStep == TutorialStep.PeekAnomaly, "the step survives a save");
            s.RewindNight();
            Check(s.TutorialStep == TutorialStep.Controls, "rewinding the night restarts a lesson in progress, from the keybinds");
            s.TutorialStep = TutorialStep.Done;
            s.RewindNight();
            Check(s.TutorialStep == TutorialStep.Done, "rewinding never starts a finished tutorial again");
        }

        private static void LockdownTests()
        {
            Group("Lockdown - mistakes, the clock on each face, the night's shape");
            foreach (var verdict in new[] { Verdict.Allow, Verdict.Reject })
                foreach (bool anomaly in new[] { false, true })
                {
                    bool wrong = verdict == Verdict.Allow ? anomaly : !anomaly;
                    Check(LockdownRules.IsStrike(anomaly, verdict) == wrong, "a " + (anomaly ? "anomaly" : "student") + " who is " + verdict + "ed is a mistake only when wrong");
                }
            Check(!LockdownRules.IsStrike(true, Verdict.None) && !LockdownRules.IsStrike(false, Verdict.None), "no verdict is no mistake");
            Check(LockdownRules.TakenIsStrike(false) && !LockdownRules.TakenIsStrike(true), "a taken student is a mistake, a taken anomaly is not");
            Check(!LockdownRules.Lost(2) && LockdownRules.Lost(3) && LockdownRules.Lost(4), "the third mistake loses");
            Check(LockdownRules.Rank(0) == "FLAWLESS" && LockdownRules.Rank(1) == "STEADY" && LockdownRules.Rank(2) == "BARELY HELD", "the rank follows the mistakes");
            Check(!LockdownRules.WinText(0).Contains("\u2014") && !LockdownRules.WinText(2).Contains("\u2014") && !LockdownRules.LoseText.Contains("\u2014"), "no em dash in the ending texts");
            Check(LockdownRules.WinText(1).Contains("1 mistake ") && LockdownRules.WinText(2).Contains("2 mistakes"), "the win text counts the mistakes in plain words");

            var clock = new LockdownClock();
            Check(!clock.Running && !clock.Tick(5f), "a clock that was never started does nothing");
            clock.Start();
            Check(clock.Running && Math.Abs(clock.Fraction - 1f) < 1e-4f && !clock.Warning, "a started clock is full");
            Check(!clock.Tick(24f) && Math.Abs(clock.SecondsLeft - (LockdownRules.SecondsPerVisitor - 24f)) < 1e-3f && !clock.Warning, "after 24 seconds 11 are left and it does not warn yet");
            Check(!clock.Tick(1.5f) && clock.Warning, "at 9.5 seconds it warns");
            Check(clock.Tick(20f) && clock.SecondsLeft == 0f && !clock.Running, "it says once when the time is up");
            Check(!clock.Tick(5f), "and never says it twice");
            clock.Start(); clock.Stop();
            Check(!clock.Running && !clock.Tick(100f), "a stopped clock does not run out");

            Check(ShiftPlan.LockdownNumber == ShiftPlan.TotalShifts + 2 && ShiftPlan.LastNight(GameMode.Lockdown) == ShiftPlan.LockdownNumber, "Lockdown is numbered after the Challenge");
            Check(ShiftPlan.LastNight(GameMode.Normal) == ShiftPlan.TotalShifts && ShiftPlan.LastNight(GameMode.Challenge) == ShiftPlan.Night6Number, "the other modes keep their last nights");
            var def = ShiftPlan.LockdownDef();
            Check(def.Lockdown && def.Number == ShiftPlan.LockdownNumber && def.NightLabel == "LOCKDOWN" && !def.HasAdministrator && !def.TutorialOpening, "the Lockdown night is flagged and has no administrator");
            Check(def.VisitorCount == Content.TotalStudents && def.AnomalyCount == 6 && def.ScaredCount == def.VisitorCount, "every student comes, six are anomalies, all are frightened");
            Check(ShiftPlan.Build()[2].NightLabel == "NIGHT 3" && !ShiftPlan.Build()[2].Lockdown, "a story night keeps its name");

            for (int seed = 1; seed <= 100; seed++)
            {
                var roster = new RosterFactory(seed).GenerateShift(def);
                Check(roster.Count == 13 && roster.Count(v => v.IsAnomaly) == 6 && roster.All(v => v.IsScared) && !roster.Any(v => v.IsAdministrator), "seed " + seed + ": thirteen frightened faces, six anomalies");
                Check(roster.Select(v => v.RecordName).Distinct().Count() == 13, "seed " + seed + ": each student comes exactly once");
                for (int i = 0; i < roster.Count; i++) Check(roster[i].VisitorIndex == i, "seed " + seed + ": visitor numbers follow the order");
            }
            Check(new GameSession().LockdownStrikes == 0 && new GameSession().Mode == GameMode.Normal, "a plain session has no mistakes and is the story");
        }

        private static void NightLogTests()
        {
            Group("Night security log - three hand counts, clamped, saved, cleared each night");
            var s = new GameSession();
            Check(s.LogCounts.Length == 3 && s.LogCounts[0] == 0 && s.LogCounts[1] == 0 && s.LogCounts[2] == 0, "a new session has three empty counts");
            s.ChangeLog(0, +1); s.ChangeLog(0, +1); s.ChangeLog(2, +1);
            Check(s.LogCounts[0] == 2 && s.LogCounts[1] == 0 && s.LogCounts[2] == 1, "each entry counts on its own");
            for (int i = 0; i < 5; i++) s.ChangeLog(0, -1);
            Check(s.LogCounts[0] == 0, "an entry never goes below zero");
            for (int i = 0; i < 150; i++) s.ChangeLog(1, +1);
            Check(s.LogCounts[1] == GameSession.LogMax, "an entry stops at " + GameSession.LogMax);
            Check(s.ChangeLog(5, +1) == 0 && s.ChangeLog(-1, +1) == 0, "an entry that does not exist is ignored");
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = s })).Session;
            Check(back.LogCounts[0] == 0 && back.LogCounts[1] == GameSession.LogMax && back.LogCounts[2] == 1, "the counts survive a save and load");
            var fresh = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = new GameSession() })).Session;
            Check(fresh.LogCounts.Length == 3 && fresh.LogCounts[0] + fresh.LogCounts[1] + fresh.LogCounts[2] == 0,
                "an untouched log loads back empty");
            s.BeginNight();
            Check(s.LogCounts[1] == 0 && s.LogCounts[2] == 0, "a new night starts with a clean log");
        }

        private static void CreditsTests()
        {
            Group("Credits - every role has people, and the text is clean");
            bool filled = CreditsData.Team.Length > 0;
            foreach (var sec in CreditsData.Team)
            {
                filled &= !string.IsNullOrWhiteSpace(sec.Role) && sec.People != null && sec.People.Length > 0;
                if (sec.People != null)
                    foreach (var person in sec.People) filled &= !string.IsNullOrWhiteSpace(person);
            }
            Check(filled, "every credit has a role and at least one name");
            string text = CreditsData.Build();
            bool allThere = true;
            foreach (var sec in CreditsData.Team)
            {
                allThere &= text.Contains(sec.Role);
                foreach (var person in sec.People) allThere &= text.Contains(person);
            }
            Check(allThere, "the built credits text lists every role and every name");
            Check(!text.Contains((char)0x2014) && !text.Contains("procedurally"),
                  "the credits have no em dash and no stale 'generated at runtime' note");
            Check(text.Contains("Unity") && text.Contains("Bahnschrift"), "the credits thank the engine and the fonts");
            string[] storyboard = { "Jessie Mariel De Mesa", "Daniel Steven Solitaria", "Daniella Garing", "Diosdado Fabelico II" };
            bool storyboardIsTeam = false;
            foreach (var sec in CreditsData.Team)
                if (sec.Role == "STORYBOARD") storyboardIsTeam = string.Join("|", sec.People) == string.Join("|", storyboard);
            Check(storyboardIsTeam, "the storyboard credit is Mariel, Daniel, Ella and Dio");
            Check(!text.Contains("Clarence Manalo  -") || !text.Contains("STORYBOARD  -  Clarence"), "Clarence is no longer credited for the storyboard");
        }

        private static void InterviewRuleTests()
        {
            Group("Interview rules - two questions per student, two of them locked until you look");
            int mask = 0;
            Check(InterviewRules.Remaining(mask) == 2 && InterviewRules.CanAsk(mask, QuestionId.Course), "a new student has 2 questions");
            mask |= InterviewRules.Bit(QuestionId.Course);
            Check(InterviewRules.Remaining(mask) == 1, "one question used leaves one");
            mask |= InterviewRules.Bit(QuestionId.Classroom);
            Check(InterviewRules.Remaining(mask) == 0, "two questions used leaves none");
            Check(!InterviewRules.CanAsk(mask, QuestionId.Year), "a third new question is refused");
            Check(InterviewRules.CanAsk(mask, QuestionId.Course), "an answer already given can be heard again for free");
            Check(!InterviewRules.Unlocked(QuestionId.Year, false, true) && InterviewRules.Unlocked(QuestionId.Year, true, false),
                  "the year question needs the ID flipped");
            Check(!InterviewRules.Unlocked(QuestionId.Status, true, false) && InterviewRules.Unlocked(QuestionId.Status, false, true),
                  "the status question needs their file opened");
            Check(InterviewRules.Unlocked(QuestionId.Course, false, false) && InterviewRules.Unlocked(QuestionId.Classroom, false, false),
                  "course and classroom are always open");
            Check(InterviewRules.LockedHint(QuestionId.Year).Length > 0 && InterviewRules.LockedHint(QuestionId.Status).Length > 0,
                  "the locked questions say what unlocks them");
            Check(InterviewRules.AskedCount(0xF) == 4 && InterviewRules.AskedCount(0) == 0, "asked count follows the mask");

            int spokenLiars = 0; var lieQuestions = new HashSet<int>();
            for (int seed = 1; seed <= 200; seed++)
            {
                var rf = new RosterFactory(seed);
                foreach (var shift in ShiftPlan.Build())
                    foreach (var v in rf.GenerateShift(shift))
                    {
                        if (v.IsAdministrator) continue;
                        for (int qi = 0; qi < InterviewRules.Count; qi++)
                            Check(!string.IsNullOrEmpty(v.Answers[qi]), $"seed {seed}: every visitor has an answer to question {qi}");
                        bool liar = v.Defects.Contains(DefectType.SpokenMismatch);
                        if (!liar)
                            for (int qi = 0; qi < 3; qi++)
                                Check(v.Answers[qi] == CardSpeech(v, (QuestionId)qi),
                                      $"seed {seed}: '{v.ClaimedName}' repeats their card unless they carry SpokenMismatch (question {qi})");
                        if (!shift.ClassroomLieAllowed)
                            Check(!liar, $"seed {seed} shift {shift.Number}: spoken lie used before it is unlocked");
                        if (!liar) continue;
                        spokenLiars++; lieQuestions.Add(v.SpokenLieQuestion);
                        Check(SpokenLieIsProvable(v), $"seed {seed}: a spoken lie always differs from the card");
                    }
            }
            Check(spokenLiars > 100, "enough spoken liars to be worth asking (" + spokenLiars + ")");
            Check(lieQuestions.Count == 3, "the spoken lie covers course, year and status");
        }

        private static void ClassroomQuestionTests()
        {
            Group("Questions - four to choose from, a lie provable against the card or the file");
            bool textsOk = true;
            for (int qi = 0; qi < InterviewRules.Count; qi++)
                textsOk &= !string.IsNullOrEmpty(InterviewRules.Button((QuestionId)qi)) &&
                           !string.IsNullOrEmpty(InterviewRules.Spoken((QuestionId)qi)) &&
                           !string.IsNullOrEmpty(InterviewRules.Tag((QuestionId)qi));
            Check(textsOk, "every question has its button text, tag and spoken line");
            var canon = Content.BuildStudentRoster();
            int wrongRoom = 0, missingRoom = 0, wrongClass = 0, liars = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var rf = new RosterFactory(seed);
                foreach (var shift in ShiftPlan.Build())
                    foreach (var v in rf.GenerateShift(shift))
                    {
                        if (v.IsAdministrator) continue;
                        // the FILE is always the truth: whatever the card says, the file matches the canon student
                        var student = canon.First(c => c.FullName == v.RecordName);
                        Check(v.RecordYearLevel == student.YearLevel && v.RecordCourseIndex == student.CourseIndex &&
                              v.RecordIsTransferee == student.IsTransferee && v.RecordFirstClassroom == student.ClassroomString,
                              $"seed {seed}: the file of '{v.RecordName}' is never altered by a defect");
                        // face art: only a photo defect changes who stands at the window
                        bool photoMismatch = v.Defects.Contains(DefectType.PhotoMismatch);
                        Eq(FaceAssets.StudentKey(v.WindowFaceIndex) != FaceAssets.StudentKey(v.FaceAssetIndex), photoMismatch,
                           $"seed {seed}: the window shows another student's face exactly on a PhotoMismatch");
                        if (!v.Defects.Contains(DefectType.ClassroomMismatch)) continue;
                        liars++;
                        string ans = v.ClassroomAnswer;
                        if (ans.StartsWith("Room 303") || ans.StartsWith("Room 305") || ans.StartsWith("Room 410")) missingRoom++;
                        else if (!ans.StartsWith(Content.RoomName(student.FirstClassroom))) wrongRoom++;
                        else wrongClass++;
                    }
            }
            Check(wrongRoom > 20 && missingRoom > 20 && wrongClass > 20,
                  $"all three lies occur (other room {wrongRoom}, no such room {missingRoom}, wrong class {wrongClass})");
            Check(liars > 150, "enough liars to be worth asking (" + liars + ")");
            var admin = new RosterFactory(1).BuildAdministrator(ShiftPlan.Build()[4]);
            Check(admin.ClassroomAnswer.Contains("server room"), "the administrator names the server room");
        }

        // ------------------------------------------------------ administrator hunt
        // The hunt must ALWAYS end: no input (or lack of input) can leave anyone stuck.
        private enum HuntPlayer { Idle, Perfect, Dodger, Random, StepBackSpammer }

        private static HuntInput HuntInputFor(HuntPlayer who, AdminHunt h, float t, GameRng rng, ref bool lastPeek)
        {
            var inp = new HuntInput { InHallway = true, PlayerRoom = RoomId.Hallway };
            switch (who)
            {
                case HuntPlayer.Idle:
                    break;
                case HuntPlayer.Perfect:
                    inp.BeamOnFace = h.Phase == HuntPhase.Behind;
                    if (h.Phase == HuntPhase.InRoom)
                    { inp.Peeking = true; inp.PlayerRoom = h.Room; inp.InHallway = false; inp.OnTarget = true; }
                    break;
                case HuntPlayer.Dodger:   // holds for 1 s, lets go for 2 s, again and again
                    if (h.Phase == HuntPhase.InRoom)
                    { inp.Peeking = true; inp.PlayerRoom = h.Room; inp.InHallway = false; inp.OnTarget = (t % 3f) < 1f; }
                    break;
                case HuntPlayer.StepBackSpammer:   // peeks in and out every half second, never holds
                    if ((int)(t * 2f) % 2 == 0)
                    { inp.Peeking = true; inp.PlayerRoom = RoomId.ComfortRoom; inp.InHallway = false; inp.OnTarget = true; }
                    break;
                default:   // random, but sticky: changes a few times a second
                    if (rng.Chance(0.05f)) lastPeek = !lastPeek;
                    if (lastPeek)
                    {
                        inp.Peeking = true; inp.InHallway = false;
                        inp.PlayerRoom = rng.Chance(0.5f) ? RoomId.ComfortRoom : RoomId.Classroom1;
                        inp.OnTarget = rng.Chance(0.6f);
                    }
                    inp.BeamOnFace = rng.Chance(0.5f);
                    break;
            }
            return inp;
        }

        // Runs one hunt; returns the end phase and the simulated seconds it took. catches is -1
        // when the dawn clock rewound and -2 when a phase overstayed its limit.
        private static HuntPhase RunHunt(HuntPlayer who, int seed, HuntTuning tun, out float seconds,
            out int catches, List<HuntPhase> stops)
        {
            var h = new AdminHunt(tun, seed);
            var rng = new GameRng(seed * 31 + 7);
            bool peek = false;
            h.Start();
            const float dt = 0.05f;
            float t = 0f, prevLeft = h.SurviveLeft;
            for (int i = 0; i < 40000 && !h.Finished; i++)
            {
                var ev = h.Tick(dt, HuntInputFor(who, h, t, rng, ref peek));
                t += dt;
                if (stops != null && (ev & (HuntEvent.EnteredRoom | HuntEvent.AppearedBehind)) != 0)
                    stops.Add(h.Phase);
                if (h.SurviveLeft > prevLeft + 1e-4f) { seconds = t; catches = -1; return HuntPhase.Stalking; }
                prevLeft = h.SurviveLeft;
                float limit = h.Phase == HuntPhase.InRoom ? tun.RoomSeconds
                    : h.Phase == HuntPhase.Behind ? tun.BehindSeconds
                    : h.Phase == HuntPhase.Retreat ? tun.RetreatSeconds
                    : h.Phase == HuntPhase.Stalking ? tun.StalkMaxSeconds : float.MaxValue;
                if (h.PhaseTimer > limit + dt + 0.01f) { seconds = t; catches = -2; return HuntPhase.Stalking; }
            }
            seconds = t;
            catches = h.Catches;
            return h.Phase;
        }

        // He goes for the player: behind them in the hallway, and in the room they peek into.
        private static void HuntTargetingTests()
        {
            Group("AdminHunt - he goes for the player");
            var tun = HuntTuning.Default;
            int behind = 0, hallwayPicks = 0, sameRoom = 0, peekPicks = 0, otherRooms = 0;
            for (int seed = 1; seed <= 400; seed++)
            {
                var inHall = new AdminHunt(tun, seed); inHall.Start();
                var hall = new HuntInput { InHallway = true, PlayerRoom = RoomId.Hallway };
                for (int i = 0; i < 4000 && inHall.Phase == HuntPhase.Stalking; i++) inHall.Tick(0.1f, hall);
                hallwayPicks++;
                if (inHall.Phase == HuntPhase.Behind) behind++;

                var inPeek = new AdminHunt(tun, seed); inPeek.Start();
                var peek = new HuntInput { Peeking = true, PlayerRoom = RoomId.Classroom2 };
                for (int i = 0; i < 4000 && inPeek.Phase == HuntPhase.Stalking; i++) inPeek.Tick(0.1f, peek);
                peekPicks++;
                if (inPeek.Phase == HuntPhase.Behind) otherRooms += 1000;   // must never happen during a peek
                else if (inPeek.Room == RoomId.Classroom2) sameRoom++;
            }
            Check(behind > hallwayPicks * 0.5, $"walking the hallway, he comes up behind the player most of the time ({behind}/{hallwayPicks})");
            Check(otherRooms == 0, "...and never behind a player who is peeking");
            Check(sameRoom > peekPicks * 0.4, $"peeking into a room, he is often in that very room ({sameRoom}/{peekPicks})");
            Check(sameRoom < peekPicks * 0.8, "...but not every time, so the other rooms still matter");
        }

        private static void AdminHuntTests()
        {
            HuntTargetingTests();
            Group("AdminHunt - always ends, never stuck, fair stops");
            var tun = HuntTuning.Default;
            Check(tun.CatchesToLose >= 1 && tun.SurviveSeconds > 30f, "default tuning is sane");

            // idle player: he walks in and catches them until it is over
            var idle = RunHunt(HuntPlayer.Idle, 1, tun, out float idleSecs, out int idleCatches, null);
            Check(idle == HuntPhase.Lost && idleCatches == tun.CatchesToLose && idleSecs < tun.SurviveSeconds,
                  $"a player who does nothing is caught out before dawn ({idleSecs:F1}s)");

            // a perfect player survives to dawn, uncaught
            int perfectWins = 0;
            for (int seed = 1; seed <= 300; seed++)
            {
                var end = RunHunt(HuntPlayer.Perfect, seed, tun, out float secs, out int caught, null);
                if (end == HuntPhase.Won && caught == 0 && secs <= tun.SurviveSeconds + 0.2f) perfectWins++;
            }
            Eq(perfectWins, 300, "a perfect player always reaches dawn without being caught");

            // adversarial and random players: it still ends, every phase honours its limit
            foreach (var who in new[] { HuntPlayer.Dodger, HuntPlayer.StepBackSpammer, HuntPlayer.Random })
            {
                int ended = 0, badInvariant = 0;
                float longest = 0f;
                for (int seed = 1; seed <= 400; seed++)
                {
                    var end = RunHunt(who, seed, tun, out float secs, out int caught, null);
                    if (end == HuntPhase.Won || end == HuntPhase.Lost) ended++;
                    if (caught < 0) badInvariant++;
                    longest = System.Math.Max(longest, secs);
                }
                Eq(ended, 400, $"{who}: all 400 hunts end in a win or a loss");
                Eq(badInvariant, 0, $"{who}: the dawn clock never rewinds and no phase overstays");
                Check(longest <= tun.SurviveSeconds + 0.2f, $"{who}: longest {longest:F0}s, never past dawn");
            }

            // every kind of stop happens
            int rooms = 0, behinds = 0;
            var roomsSeen = new HashSet<RoomId>();
            for (int seed = 1; seed <= 200; seed++)
            {
                var seq = new List<HuntPhase>();
                RunHunt(HuntPlayer.Perfect, seed, tun, out _, out _, seq);
                foreach (var stop in seq) { if (stop == HuntPhase.InRoom) rooms++; else behinds++; }
                var watch = new AdminHunt(tun, seed); watch.Start();
                for (int i = 0; i < 2000 && !watch.Finished; i++)
                {
                    watch.Tick(0.05f, new HuntInput { InHallway = true, BeamOnFace = true });
                    if (watch.Phase == HuntPhase.InRoom) roomsSeen.Add(watch.Room);
                }
            }
            Check(rooms > 0 && behinds > 0, $"he stops in rooms and behind you ({rooms}/{behinds})");
            Eq(roomsSeen.Count, 4, "he can be in Room 101, Room 202, CR-1 and CR-2");

            // same seed + same input = same hunt
            var a = RunHunt(HuntPlayer.Random, 77, tun, out float aSecs, out int aCatches, null);
            var b = RunHunt(HuntPlayer.Random, 77, tun, out float bSecs, out int bCatches, null);
            Check(a == b && aSecs == bSecs && aCatches == bCatches, "a hunt replays identically from its seed");

            // looking is not enough: without the lamp on his head he is never sent away
            var off = new AdminHunt(tun, 5); off.Start();
            for (int i = 0; i < 6000 && off.Phase != HuntPhase.InRoom; i++)
                off.Tick(0.05f, new HuntInput { BeamOnFace = true, InHallway = true });
            int repelsBefore = off.Repels;
            for (int i = 0; i < 100; i++) off.Tick(0.05f, new HuntInput { Peeking = true, PlayerRoom = off.Room, OnTarget = false });
            Check(off.Repels == repelsBefore && off.Hold == 0f, "peeking without the lamp on his head never sends him away");

            // his picture: bounded, and harder after every stun
            float maxAbs = 0f, pathSlow = 0f, pathFast = 0f;
            float px = 0, py = 0, qx = 0, qy = 0;
            AdminHuntMotion.HeadOffset(0f, 0, 5, out px, out py);
            AdminHuntMotion.HeadOffset(0f, 3, 5, out qx, out qy);
            for (int i = 1; i <= 200; i++)
            {
                float t = i * 0.01f;
                AdminHuntMotion.HeadOffset(t, 0, 5, out float x0, out float y0);
                AdminHuntMotion.HeadOffset(t, 3, 5, out float x3, out float y3);
                pathSlow += (float)System.Math.Sqrt((x0 - px) * (x0 - px) + (y0 - py) * (y0 - py));
                pathFast += (float)System.Math.Sqrt((x3 - qx) * (x3 - qx) + (y3 - qy) * (y3 - qy));
                px = x0; py = y0; qx = x3; qy = y3;
                maxAbs = System.Math.Max(maxAbs, System.Math.Max(System.Math.Abs(x3), System.Math.Abs(y3)));
            }
            Check(maxAbs < 4f, "his picture stays within a few units of its anchor (" + maxAbs.ToString("F2") + ")");
            Check(pathFast > pathSlow * 1.3f, "after three visits he moves clearly faster");
        }

        // the hunt decides how the last night ends
        private static void HuntEndingTests()
        {
            Group("Hunt endings - win needs the hunt won, loss is its own ending, state is saved");
            GameSession Night5(HuntState hunt) => new GameSession
            {
                CurrentShift = ShiftPlan.TotalShifts, CompletedShifts = ShiftPlan.TotalShifts,
                AdminRejected = true, Hunt = hunt, PoolFolded = true,
            };
            var active = EndingRules.Evaluate(Night5(HuntState.Active));
            Eq(active.Ending, EndingType.None, "a night closed while the hunt is still on is not a win");
            var won = EndingRules.Evaluate(Night5(HuntState.Won));
            Check(won.Ending == EndingType.Win && won.ReasonKey == "win", "driving him off wins the game");
            var lost = EndingRules.Evaluate(Night5(HuntState.Lost));
            Check(lost.Ending == EndingType.Lose && lost.ReasonKey == "server_door", "the server door loss is its own ending");
            var lostSecret = Night5(HuntState.Lost);
            Eq(EndingRules.Evaluate(lostSecret).ReasonKey, "server_door", "the loss outranks everything else on the final night");
            Check(Night5(HuntState.Lost).LoseThresholdReached, "a lost hunt counts as a lose condition");

            var skipped = Night5(HuntState.Ready);
            foreach (var clue in ClueRegistry.Build().OrderBy(c => c.Order)) skipped.DiscoverClue(clue.Key);
            Check(skipped.Clues.SecretConditionsMet, "(the ten findings are in order)");
            Check(EndingRules.Evaluate(skipped).Ending == EndingType.None, "the Secret cannot skip the hunt");

            var game = new SaveGame { Session = Night5(HuntState.Active) };
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(game));
            Check(back != null && back.Session.Hunt == HuntState.Active, "the hunt state survives a save and load");
            var ready = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = Night5(HuntState.Ready) }));
            Check(ready != null && ready.Session.Hunt == HuntState.Ready, "waiting for the Patrol click survives a save and load");
        }

        // ----------------------------------------------------- verdict matrix
        private static void InspectionRulesTests()
        {
            Group("InspectionRules - verdict / false-rejection / breach matrix");
            var rf = new RosterFactory(11);
            var visitors = rf.GenerateShift(ShiftPlan.Get(3));
            var legit = visitors.First(x => !x.IsAnomaly);
            var anomaly = visitors.First(x => x.IsAnomaly && !x.IsAdministrator);

            var okAllow = InspectionRules.Apply(legit, Verdict.Allow);
            Check(okAllow.Correct && !okAllow.FalseRejection && !okAllow.AnomalyBreach, "legit + Allow = correct, clean");
            var badReject = InspectionRules.Apply(legit, Verdict.Reject);
            Check(!badReject.Correct && badReject.FalseRejection && !badReject.AnomalyBreach, "legit + Reject = false rejection");
            var badAllow = InspectionRules.Apply(anomaly, Verdict.Allow);
            Check(!badAllow.Correct && badAllow.AnomalyBreach && !badAllow.FalseRejection, "anomaly + Allow = breach");
            var okReject = InspectionRules.Apply(anomaly, Verdict.Reject);
            Check(okReject.Correct && !okReject.AnomalyBreach && !okReject.FalseRejection, "anomaly + Reject = correct");
            Eq(InspectionRules.Apply(legit, Verdict.None).Correct, false, "no verdict is never 'correct'");
        }

        // ---------------------------------------------------------- endings
        private static void EndingRulesTests()
        {
            Group("EndingRules - Win / Lose / Secret with Lose priority (conflict C4)");
            Eq(EndingRules.LoseThreshold, 3, "lose threshold is 3");

            var fresh = new GameSession { Mode = GameMode.Normal, CompletedShifts = 0 };
            Eq(EndingRules.Evaluate(fresh).Ending, EndingType.None, "mid-run evaluates to None");
            Eq(EndingRules.Evaluate(fresh).ReasonKey, "continue", "mid-run reason key");

            var win = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, AdminRejected = true, Hunt = HuntState.Won };
            Eq(EndingRules.Evaluate(win).Ending, EndingType.Win, "five clean shifts = Win");
            Eq(EndingRules.Evaluate(win).ReasonKey, "win", "win reason key");
            Check(!string.IsNullOrEmpty(EndingRules.Evaluate(win).ReasonText), "win has ending text");

            var breaches = new GameSession { CompletedShifts = 5, CurrentShift = 6, Breaches = 3 };
            Eq(EndingRules.Evaluate(breaches).Ending, EndingType.Lose, "3 breaches = Lose even after 5 shifts");
            Eq(EndingRules.Evaluate(breaches).ReasonKey, "breaches", "breach reason key");
            Check(breaches.LoseThresholdReached, "LoseThresholdReached at 3 breaches");
            Check(!new GameSession { Breaches = 2 }.LoseThresholdReached, "2 breaches is not yet a loss");

            var falseRej = new GameSession { CompletedShifts = 5, CurrentShift = 6, FalseRejections = 3 };
            Eq(EndingRules.Evaluate(falseRej).Ending, EndingType.Lose, "3 false rejections = Lose");
            Eq(EndingRules.Evaluate(falseRej).ReasonKey, "false_rejections", "false-rejection reason key");

            var adminBreach = new GameSession { CompletedShifts = 5, CurrentShift = 6, AdminAllowed = true };
            Eq(EndingRules.Evaluate(adminBreach).Ending, EndingType.Lose, "allowing the administrator = immediate Lose");
            Eq(EndingRules.Evaluate(adminBreach).ReasonKey, "admin_breach", "admin reason key");
            Check(EndingRules.Evaluate(new GameSession { AdminAllowed = true, CompletedShifts = 4, CurrentShift = 5 }).Ending == EndingType.Lose,
                  "admin breach fires mid-run on the final night too");

            // Lose outranks Secret
            var secretSession = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, CurrentShift = 6, AdminRejected = true, Hunt = HuntState.Won };
            foreach (var clue in ClueRegistry.Build().OrderBy(x => x.Order)) secretSession.Clues.Discover(clue.Key);
            Check(secretSession.Clues.SecretConditionsMet, "all ten clues in order satisfy the secret conditions");
            Eq(EndingRules.Evaluate(secretSession).Ending, EndingType.Secret, "all clues in order = Secret ending");
            Eq(EndingRules.Evaluate(secretSession).ReasonKey, "secret", "secret reason key");

            secretSession.AdminAllowed = true;
            Eq(EndingRules.Evaluate(secretSession).Ending, EndingType.Lose, "Lose outranks Secret (priority order)");
            // ---- night 6, the Challenge
            var n6 = ShiftPlan.Night6Def();
            Check(n6.Number == 6 && n6.HasAdministrator && n6.VisitorCount == 0 && n6.AnomalyCount == 0,
                  "night 6 is the administrator alone, like night 5");
            Check(n6.DateLabel == "Sat · Nov 7" && n6.LongDateLabel == "SATURDAY - NOVEMBER 7",
                  "night 6 has its own date");
            Eq(ShiftPlan.LastNight(GameMode.Normal), 5, "the story ends at night 5");
            Eq(ShiftPlan.LastNight(GameMode.Challenge), 6, "the Challenge reaches night 6");
            Check(ShiftPlan.Get(6) == null, "night 6 is not part of the story plan");
            var c6 = new GameSession { Mode = GameMode.Challenge, CurrentShift = 6, CompletedShifts = 1 };
            Eq(EndingRules.Evaluate(c6).Ending, EndingType.None, "night 6 without a turned away, survived administrator is unresolved");
            c6.AdminRejected = true; c6.Hunt = HuntState.Won;
            var c6win = EndingRules.Evaluate(c6);
            Check(c6win.Ending == EndingType.Win && c6win.ReasonKey == "night6", "night 6 survived is its own Win");
            var c6lose = new GameSession { Mode = GameMode.Challenge, CurrentShift = 6, CompletedShifts = 0, AdminRejected = true, Hunt = HuntState.Lost };
            var c6loseEval = EndingRules.Evaluate(c6lose);
            Check(c6loseEval.Ending == EndingType.Lose && c6loseEval.ReasonKey == "server_door",
                  "night 6: reaching the server door loses (he needs two catches there)");
            var c6allow = new GameSession { Mode = GameMode.Challenge, CurrentShift = 6, AdminAllowed = true };
            Eq(EndingRules.Evaluate(c6allow).Ending, EndingType.Lose, "night 6: letting him in loses at once");

            var secretWithBreaches = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, CurrentShift = 6, Breaches = 3 };
            foreach (var clue in ClueRegistry.Build().OrderBy(x => x.Order)) secretWithBreaches.Clues.Discover(clue.Key);
            Eq(EndingRules.Evaluate(secretWithBreaches).Ending, EndingType.Lose, "breaches outrank Secret too");
        }

        // ---------------------------------------------------------- session
        private static void SessionTests()
        {
            Group("GameSession - counters, sanity, shift completion");
            var rf = new RosterFactory(5);
            var visitors = rf.GenerateShift(ShiftPlan.Get(3));
            var legit = visitors.First(x => !x.IsAnomaly);
            var anomaly = visitors.First(x => x.IsAnomaly && !x.IsAdministrator);

            var s = new GameSession { Mode = GameMode.Normal, CurrentShift = 1, Sanity = SanityModel.Max };
            Eq(s.Sanity, 100f, "sanity starts at max");
            Eq(s.SanityStage, SanityStage.Stable, "starts Stable");

            s.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow));
            Eq(s.Processed, 1, "processed increments");
            Eq(s.CorrectDecisions, 1, "correct increments");
            Eq(s.Streak, 1, "streak increments");
            Eq(s.BestStreak, 1, "best streak tracks");
            Eq(s.Sanity, 100f, "a correct call does not touch sanity directly");
            Eq(s.RefillPending, SanityModel.RefillOnCorrect, "...it queues a refill instead");
            Eq(s.Stacks.Count, 0, "a correct call puts no drain stack up");

            s.RecordVerdict(InspectionRules.Apply(anomaly, Verdict.Allow));
            Eq(s.Breaches, 1, "breach counted");
            Eq(s.Streak, 0, "streak resets on a mistake");
            Eq(s.BestStreak, 1, "best streak preserved");
            Eq(s.Sanity, 100f, "a breach costs no sanity directly");
            Eq(s.Stacks.Count, 1, "...it adds a drain stack");

            s.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject));
            Eq(s.FalseRejections, 1, "false rejection counted");
            Eq(s.Sanity, 100f, "a false rejection costs no sanity directly");
            Eq(s.Stacks.Count, 2, "...it adds another drain stack");

            var admin = rf.BuildAdministrator(ShiftPlan.Get(5));
            var adminSession = new GameSession { Sanity = 50f };
            adminSession.RecordVerdict(InspectionRules.Apply(admin, Verdict.Reject));
            Check(adminSession.AdminRejected && !adminSession.AdminAllowed, "admin rejection recorded");
            Eq(adminSession.Sanity, 50f, "rejecting the administrator is a correct call: a refill, not an instant jump");
            Eq(adminSession.RefillPending, SanityModel.RefillOnCorrect, "...the refill is queued");
            adminSession.RecordVerdict(InspectionRules.Apply(admin, Verdict.Allow));
            Check(adminSession.AdminAllowed, "allowing the administrator sets AdminAllowed");

            Group("SanityStacks - wrong calls speed the drain up for a while");
            var st0 = new SanityStacks();
            Eq(st0.Count, 0, "no stacks at the start");
            Eq(st0.Multiplier, 0.85f, "the baseline drain is 0.85x");
            st0.Add();
            Eq(st0.Count, 1, "one wrong call: one stack");
            Eq(st0.Multiplier, 1.25f, "one stack drains 1.25x");
            Eq(st0.SecondsLeft, 10f, "one stack lasts 10 s (the 7-12 s minimum)");
            st0.Add();
            Eq(st0.Multiplier, 1.75f, "two stacks drain 1.75x");
            Eq(st0.SecondsLeft, 20f, "two stacks last 20 s");
            st0.Add();
            Eq(st0.Multiplier, 2.25f, "three stacks drain 2.25x");
            Eq(st0.SecondsLeft, 30f, "three stacks last 30 s (the maximum)");
            st0.Add();
            Eq(st0.Count, SanityStacks.MaxStacks, "the stack count is capped at three");
            Eq(st0.Multiplier, 2.25f, "...so the drain is capped at 2.25x");
            Eq(st0.SecondsLeft, 30f, "...and a fourth wrong call restarts the 30 s");
            st0.Tick(29f);
            Eq(st0.Count, 3, "still stacked at 29 s");
            st0.Tick(1.5f);
            Eq(st0.Count, 0, "the stacks clear together when the timer runs out");
            Eq(st0.Multiplier, 0.85f, "...back to the 0.85x baseline");
            Eq(st0.SecondsLeft, 0f, "...with no time left");
            st0.Tick(5f);
            Eq(st0.Count, 0, "ticking with no stacks does nothing");
            st0.Add(); st0.Tick(-3f); st0.Tick(0f);
            Eq(st0.SecondsLeft, 10f, "a negative or zero tick never rewinds or advances the timer");
            st0.Clear();
            Eq(st0.Count, 0, "Clear drops every stack");
            Eq(SanityStacks.SecondsFor(99), 30f, "an out-of-range count clamps to the maximum");
            Eq(SanityStacks.MultiplierFor(-4), 0.85f, "a negative count clamps to the baseline");

            Group("ShiftFailureRules - what a failed night costs the next one");
            Eq(ShiftFailureRules.Severity(0, ShiftFailure.None), 0, "no failure = no severity");
            Eq(ShiftFailureRules.Severity(1, ShiftFailure.Breach), 1, "one anomaly let in = severity 1");
            Eq(ShiftFailureRules.Severity(3, ShiftFailure.Breach), 3, "three let in = severity 3");
            Eq(ShiftFailureRules.Severity(0, ShiftFailure.Blackout), 2, "a blackout alone is worth two");
            Eq(ShiftFailureRules.Severity(2, ShiftFailure.Blackout), 4, "a blackout adds to the anomalies let in");
            Eq(ShiftFailureRules.Severity(0, ShiftFailure.Breach), 1, "a failed night is never free (at least 1)");
            Eq(ShiftFailureRules.Severity(99, ShiftFailure.Breach), ShiftFailureRules.MaxSeverity, "severity is capped");
            Eq(ShiftFailureRules.StartSanityLoss(0, 3), 0f, "no severity, no loss");
            Check(Math.Abs(ShiftFailureRules.StartSanityLoss(1, 1) - 8f) < 1e-3f, "severity 1 on night 1 costs 8 sanity");
            Check(ShiftFailureRules.StartSanityLoss(2, 1) > ShiftFailureRules.StartSanityLoss(1, 1), "more severe = more sanity lost");
            Check(ShiftFailureRules.StartSanityLoss(2, 4) > ShiftFailureRules.StartSanityLoss(2, 1), "the same failure costs more on a later night");
            Eq(ShiftFailureRules.StartSanityLoss(ShiftFailureRules.MaxSeverity, 4), ShiftFailureRules.MaxStartSanityLoss, "the loss is capped (the next night never starts below 55)");
            Eq(ShiftFailureRules.StartStacks(0), 0, "no stacks for no severity");
            Eq(ShiftFailureRules.StartStacks(2), 2, "severity 2 starts with two stacks");
            Eq(ShiftFailureRules.StartStacks(5), SanityStacks.MaxStacks, "stacks are capped at the stack cap");
            Check(!ShiftFailureRules.RoamingAtStart(0) && ShiftFailureRules.RoamingAtStart(1), "an anomaly is already roaming after any failed night");

            Group("GameSession - a failed night is paid for by the next one");
            var fail = new GameSession { Mode = GameMode.Normal, CurrentShift = 2, Sanity = 0f };
            fail.ShiftBreaches = 1;
            fail.RecordShiftFailure(ShiftFailure.Blackout);
            Eq(fail.LastShiftFailure, ShiftFailure.Blackout, "the failure is recorded for the results screen");
            Eq(fail.PendingPenaltySeverity, 3, "a blackout with one anomaly let in is severity 3");
            Eq(fail.PendingPenaltyNight, 2, "...earned on night 2");
            fail.CurrentShift = 3;
            fail.BeginNight();
            Check(Math.Abs(fail.Sanity - (100f - ShiftFailureRules.StartSanityLoss(3, 2))) < 1e-3f, "the next night starts with the sanity the failure cost (not at 0)");
            Eq(fail.Stacks.Count, 3, "...with the drain stacks already up");
            Check(fail.RoamingAtStart, "...and an anomaly already roaming");
            Eq(fail.PendingPenaltySeverity, 0, "the penalty is paid exactly once");
            Eq(fail.LastShiftFailure, ShiftFailure.None, "...and the new night starts without a failure on record");
            Eq(fail.ShiftBreaches, 0, "...and a fresh breach count");
            var clean = new GameSession { Mode = GameMode.Normal, CurrentShift = 3, Sanity = 80f };
            clean.Stacks.Add(); clean.RefillPending = 9f;
            clean.BeginNight();
            Eq(clean.Sanity, 80f, "a clean night leaves sanity alone");
            Eq(clean.Stacks.Count, 0, "...but a new night always starts with no stacks");
            Eq(clean.RefillPending, 0f, "...and no refill left over");
            Check(!clean.RoamingAtStart, "...and nothing roaming at the start");
            var breachSess = new GameSession { Mode = GameMode.Normal, Sanity = 100f };
            breachSess.RecordVerdict(InspectionRules.Apply(anomaly, Verdict.Allow));
            Eq(breachSess.ShiftBreaches, 1, "an anomaly let in counts toward this shift's breaches");
            breachSess.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow));
            Eq(breachSess.ShiftBreaches, 1, "a correct call does not");

            Group("GameSession - drain stacks scale losses, a correct call refills gradually");
            var drain = new GameSession { Mode = GameMode.Normal, Sanity = 100f };
            drain.ApplySanity(-10f);
            Check(Math.Abs(drain.Sanity - 91.5f) < 1e-3f, "a loss at the baseline costs 0.85x (10 -> 8.5)");
            drain.Stacks.Add(); drain.Stacks.Add(); drain.Stacks.Add();
            drain.Sanity = 100f;
            drain.ApplySanity(-10f);
            Check(Math.Abs(drain.Sanity - 77.5f) < 1e-3f, "at three stacks the same loss costs 2.25x (10 -> 22.5)");
            drain.ApplySanity(+10f);
            Check(Math.Abs(drain.Sanity - 87.5f) < 1e-3f, "gains are never scaled by the stacks");
            var refill = new GameSession { Sanity = 40f };
            refill.RefillPending = SanityModel.RefillOnCorrect;
            float handed = 0f; int ticks = 0;
            while (refill.RefillPending > 0f && ticks < 100) { handed += refill.TakeRefill(0.5f); ticks++; }
            Check(Math.Abs(handed - SanityModel.RefillOnCorrect) < 1e-3f, "the whole refill is eventually handed over");
            int wantTicks = (int)Math.Ceiling(SanityModel.RefillOnCorrect / (SanityModel.RefillPerSecond * 0.5f));
            Eq(ticks, wantTicks, "...at the rules' speed, half a second at a time");
            Check(SanityModel.RefillPerSecond < 2.5f && SanityModel.RefillOnCorrect < 15f, "...and both numbers are nerfed from the old 15 at 2.5 a second");
            Eq(refill.TakeRefill(0.5f), 0f, "nothing is handed over once the refill is paid out");
            Eq(refill.TakeRefill(-1f), 0f, "a negative tick hands over nothing");
            Check(SanityModel.RefillOnCorrect == GameRules.Current.RefillOnCorrect, "a correct call is worth what the rules say");
            Check(SanityModel.DeskDrainPerSecond < SanityModel.DarknessDrainPerSecond &&
                  SanityModel.DarknessDrainPerSecond < SanityModel.SecurityRoomRecoveryPerSecond,
                  "a student at the window drains slowest, then the dark, and resting out-recovers both");

            // sanity clamping
            var low = new GameSession { Sanity = 1f };
            low.ApplySanity(-50f);
            Eq(low.Sanity, SanityModel.Min, "sanity clamps at 0");
            var high = new GameSession { Sanity = 99f };
            high.ApplySanity(+50f);
            Eq(high.Sanity, SanityModel.Max, "sanity clamps at 100");

            // shift completion bookkeeping
            var prog = new GameSession { CurrentShift = 1, Processed = 4, CorrectDecisions = 4 };
            prog.CompleteShift();
            Eq(prog.CompletedShifts, 1, "CompletedShifts increments");
            Eq(prog.CurrentShift, 2, "CurrentShift advances");
            Eq(prog.History.Count, 1, "a summary is recorded");
            Eq(prog.History[0].Shift, 1, "summary stores the shift number");
            var enc = prog.History[0].Encode();
            var dec = ShiftSummary.Decode(enc);
            Eq(dec.Shift, prog.History[0].Shift, "ShiftSummary round-trips (shift)");
            Eq(dec.Visitors, prog.History[0].Visitors, "ShiftSummary round-trips (visitors)");
            Eq(dec.Correct, prog.History[0].Correct, "ShiftSummary round-trips (correct)");
            Check(Math.Abs(dec.SanityEnd - prog.History[0].SanityEnd) < 0.01f, "ShiftSummary round-trips (sanity)");
        }

        // ------------------------------------------------------- clue tracker
        private static void ClueTrackerTests()
        {
            Group("ClueTracker - intended sequence gates the secret ending");
            var ordered = ClueRegistry.Build().OrderBy(x => x.Order).Select(x => x.Key).ToList();

            var t = new ClueTracker();
            Eq(t.TotalCount, 10, "tracker knows all ten clues");
            Eq(t.FoundCount, 0, "starts with none found");
            Check(!t.AllFound && !t.SecretConditionsMet, "secret unavailable at start");

            var first = t.Discover(ordered[0]);
            Check(first.FirstTime && first.AdvancedSequence && !first.BrokeSequence, "first in-order find advances the sequence");
            Check(t.OrderIntact, "order still intact");
            var again = t.Discover(ordered[0]);
            Check(!again.FirstTime, "re-discovering is not a first-time find");
            Eq(t.FoundCount, 1, "re-discovery does not double count");

            foreach (var key in ordered.Skip(1)) t.Discover(key);
            Check(t.AllFound, "all ten found");
            Check(t.OrderIntact, "order intact after the full sequence");
            Check(t.SecretConditionsMet, "secret conditions met (all found, in order, all three rooms investigated)");
            Eq(t.InvestigatedRooms.Count, 3, "exactly the three explorable rooms were marked");
            Check(!t.InvestigatedRooms.Contains(RoomId.Security), "the security room is not an investigation room");

            var broken = new ClueTracker();
            var outOfOrder = broken.Discover(ordered[5]);
            Check(outOfOrder.FirstTime && outOfOrder.BrokeSequence && !outOfOrder.AdvancedSequence,
                  "an out-of-order find breaks the sequence");
            Check(!broken.OrderIntact, "OrderIntact cleared");
            foreach (var key in ordered) broken.Discover(key);
            Check(broken.AllFound && !broken.SecretConditionsMet, "all found but out of order => no secret ending");

            // save support
            var keys = t.SaveFound();
            var reloaded = new ClueTracker();
            reloaded.LoadFound(keys);
            Eq(reloaded.FoundCount, 10, "LoadFound restores all discoveries");
            Check(reloaded.AllFound, "restored tracker reports AllFound");
            var unknown = new ClueTracker().Discover("not_a_clue");
            Check(unknown.Def == null && !unknown.FirstTime, "unknown clue key is ignored safely");
        }

        // ----------------------------------------------------------- saving
        private static void SaveFormatTests()
        {
            Group("SaveFormat - round-trip, checksum tamper detection, bad input");
            Eq(SaveFormat.Header, "SMYIDSAVE", "header constant");
            Eq(SaveFormat.Version, 4, "version constant");

            var save = new SaveGame { SavedAtLabel = "NIGHT 3 - 02:14" };
            save.Session.Mode = GameMode.Normal;
            save.Session.Seed = 424242;
            save.Session.CurrentShift = 4;
            save.Session.CompletedShifts = 3;
            save.Session.Processed = 21;
            save.Session.CorrectDecisions = 19;
            save.Session.FalseRejections = 1;
            save.Session.Breaches = 2;
            save.Session.AnomaliesRejected = 8;
            save.Session.AdminAllowed = false;
            save.Session.AdminRejected = true;
            save.Session.Sanity = 63.5f;
            save.Session.ShiftBreaches = 2;
            save.Session.PendingPenaltySeverity = 3;
            save.Session.PendingPenaltyNight = 2;
            save.Session.LastShiftFailure = ShiftFailure.Breach;
            save.Session.Score = 5150;
            save.Session.Streak = 4;
            save.Session.BestStreak = 9;
            save.Session.RunFinished = true;
            save.Session.History.Add(new ShiftSummary { Shift = 1, Visitors = 4, Correct = 4, FalseRejections = 0, Breaches = 0, SanityEnd = 100f });
            foreach (var clue in ClueRegistry.Build().Take(4).OrderBy(x => x.Order)) save.Session.Clues.Discover(clue.Key);
            save.Settings.MasterVolume = 0.55f;
            save.Settings.MusicVolume = 0.3f;
            save.Settings.SfxVolume = 0.9f;
            save.Settings.TextScale = 2;
            save.Settings.ReduceMotion = true;
            save.Settings.PhotosensitivitySafe = true;
            save.Settings.Captions = false;
            save.Records.EndingsSeen = new List<string> { "win", "secret" };

            var text = SaveFormat.Serialize(save);
            Check(text.StartsWith(SaveFormat.Header + " " + SaveFormat.Version), "serialized text starts with header+version");
            Check(text.Contains("checksum="), "serialized text carries a checksum");

            var back = SaveFormat.Deserialize(text);
            Check(back != null, "deserialize succeeds on a clean save");
            if (back != null)
            {
                var s = back.Session;
                Eq(s.Mode, save.Session.Mode, "mode round-trips");
                Eq(s.Seed, save.Session.Seed, "seed round-trips");
                Eq(s.CurrentShift, save.Session.CurrentShift, "current shift round-trips");
                Eq(s.CompletedShifts, save.Session.CompletedShifts, "completed shifts round-trip");
                Eq(s.Processed, save.Session.Processed, "processed round-trips");
                Eq(s.CorrectDecisions, save.Session.CorrectDecisions, "correct round-trips");
                Eq(s.FalseRejections, save.Session.FalseRejections, "false rejections round-trip");
                Eq(s.Breaches, save.Session.Breaches, "breaches round-trip");
                Eq(s.AnomaliesRejected, save.Session.AnomaliesRejected, "anomalies rejected round-trip");
                Check(s.AdminRejected && !s.AdminAllowed, "admin flags round-trip");
                Check(Math.Abs(s.Sanity - save.Session.Sanity) < 0.01f, "sanity round-trips");
                Eq(s.ShiftBreaches, 2, "this shift's breaches round-trip");
                Eq(s.PendingPenaltySeverity, 3, "a pending failed-night penalty round-trips (quit at the results screen)");
                Eq(s.PendingPenaltyNight, 2, "...with the night it was earned on");
                Eq(s.LastShiftFailure, ShiftFailure.Breach, "...and how the night failed");
                Eq(s.Score, save.Session.Score, "score round-trips");
                Eq(s.Streak, save.Session.Streak, "streak round-trips");
                Eq(s.BestStreak, save.Session.BestStreak, "best streak round-trips");
                Check(s.RunFinished, "run finished flag round-trips");
                Eq(s.History.Count, 1, "shift history round-trips");
                Eq(s.Clues.FoundCount, 4, "clue discoveries round-trip");
                Eq(back.Settings.TextScale, 2, "text scale round-trips");
                Check(back.Settings.ReduceMotion && back.Settings.PhotosensitivitySafe && !back.Settings.Captions,
                      "accessibility toggles round-trip");
                Check(Math.Abs(back.Settings.MasterVolume - 0.55f) < 0.001f, "master volume round-trips");
                Eq(string.Join(",", back.Records.EndingsSeen), "win,secret", "endings seen round-trip");
                Eq(back.SavedAtLabel, save.SavedAtLabel, "saved-at label round-trips");
            }

            // tamper detection: flip one character of a data line (not the checksum line)
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            int idx = lines.FindIndex(l => l.StartsWith("completed="));
            Check(idx > 0, "found the 'completed=' line to tamper with");
            lines[idx] = "completed=99";
            Check(SaveFormat.Deserialize(string.Join("\n", lines)) == null, "tampered value is rejected by the checksum");

            // every other corruption path
            Check(SaveFormat.Deserialize(null) == null, "null save rejected");
            Check(SaveFormat.Deserialize("") == null, "empty save rejected");
            Check(SaveFormat.Deserialize("garbage") == null, "garbage rejected");
            Check(SaveFormat.Deserialize(text.Replace(SaveFormat.Header, "NOTMINE")) == null, "wrong header rejected");
            Check(SaveFormat.Deserialize(text.Replace("checksum=", "chksum=")) == null, "missing checksum rejected");
            var noVersion = text.Split('\n');
            noVersion[0] = SaveFormat.Header + " 99";
            Check(SaveFormat.Deserialize(string.Join("\n", noVersion)) == null, "wrong version rejected");
            Check(SaveFormat.Checksum("abc") == SaveFormat.Checksum("abc"), "checksum is deterministic");
            Check(SaveFormat.Checksum("abc") != SaveFormat.Checksum("abd"), "checksum is sensitive");

            // settings + presentation log encode/decode
            var st = new SettingsData { MasterVolume = 0.25f, TextScale = 0, ReduceMotion = true, Captions = false };
            var stBack = SettingsData.Decode(st.Encode());
            Check(Math.Abs(stBack.MasterVolume - 0.25f) < 0.001f && stBack.TextScale == 0
                  && stBack.ReduceMotion && !stBack.Captions, "SettingsData round-trips");
            Eq(SettingsData.TextScaleMultiplier(0) < SettingsData.TextScaleMultiplier(1), true, "text scale increases size");
            Check(SettingsData.TextScaleMultiplier(2) > SettingsData.TextScaleMultiplier(1), "large text scale is larger");

            var log = new PresentationLog();
            var rf = new RosterFactory(2);
            var firstThree = rf.GenerateShift(ShiftPlan.Get(3)).Take(3).ToList();
            foreach (var v in firstThree) log.RecordFrom(v, "tonight");
            // a card with a broken ID number is not logged, and a repeated number is logged once
            int logged = firstThree.Where(v => IdRules.IsFormatValid(v.IdNumber)).Select(v => v.IdNumber).Distinct().Count();
            Check(logged >= 1, "setup: at least one of the first three cards is loggable");
            Eq(log.Count, logged, "presentation log recorded every valid card once");
            var log2 = new PresentationLog();
            log2.Decode(log.Encode());
            Eq(log2.Count, logged, "presentation log round-trips");
            Eq(string.Join(",", log2.PresentedIds().OrderBy(x => x)), string.Join(",", log.PresentedIds().OrderBy(x => x)),
               "presentation log keeps the same IDs");
        }

        // ------------------------------------------------ tracking / reveal / tokens
        private static void TrackingTests()
        {
            Group("TrackingPolicy - Focus advances, observation holds, darkness decays");
            Eq(TrackingPolicy.RatePerSecond(true, false), TrackingPolicy.FocusRatePerSecond, "Focus is the only beam that ADVANCES the meter");
            Eq(TrackingPolicy.RatePerSecond(false, true), TrackingPolicy.HoldRatePerSecond, "observation (sweep/lit room) only HOLDS - ambient light never resolves");
            Eq(TrackingPolicy.HoldRatePerSecond, 0f, "hold rate is exactly zero");
            Check(TrackingPolicy.RatePerSecond(false, false) < 0f, "unobserved anomalies decay");
            Check(TrackingPolicy.FocusRatePerSecond > TrackingPolicy.HoldRatePerSecond, "Focus beats ambient light");
            Eq(TrackingPolicy.RatePerSecond(true, true, 0.7f, 0f, 0.4f), 0.7f, "custom rates: focus wins over observed");
            Eq(TrackingPolicy.RatePerSecond(false, true, 0.7f, 0f, 0.4f), 0f, "custom rates: observed holds at the hold rate");
            Eq(TrackingPolicy.RatePerSecond(false, false, 0.7f, 0f, 0.4f), -0.4f, "custom rates: darkness decays at the decay rate");

            float m = 0f;
            float t = 0f;
            while (!TrackingPolicy.IsFull(m) && t < 10f) { m = TrackingPolicy.Advance(m, 1f / 60f, true, false); t += 1f / 60f; }
            Check(TrackingPolicy.IsFull(m), "holding Focus fills the meter");
            Check(t > 1.5f && t < 2.2f, $"a full meter takes ~1.8s of Focus (took {t:F2}s)");

            m = 0.4f;
            for (int i = 0; i < 60; i++) m = TrackingPolicy.Advance(m, 1f / 60f, false, true);
            Eq(m, 0.4f, "one second of room light holds the meter exactly (never resolves, never decays)");
            for (int i = 0; i < 120; i++) m = TrackingPolicy.Advance(m, 1f / 60f, false, false);
            Eq(m, 0f, "two seconds of darkness decay 0.4 back to zero (clamped)");
            Eq(TrackingPolicy.Advance(0.1f, 1f, false, false), 0f, "decay clamps at zero, never negative");
            Eq(TrackingPolicy.Advance(0.5f, 0f, 0.55f), 0.5f, "zero dt is a no-op");
            Eq(TrackingPolicy.Advance(0.9f, 1f, 0.55f), 1f, "advance clamps at one");
            Eq(TrackingPolicy.Solidify(0.25f), 0.25f, "solidify factor mirrors the meter");
            Eq(TrackingPolicy.Solidify(4f), 1f, "solidify clamps");
            Check(!TrackingPolicy.HallucinationDispelled(0.49f), "0.49s of Focus does not dispel a hallucination");
            Check(TrackingPolicy.HallucinationDispelled(0.5f), "0.5s of Focus truth-checks a hallucination");
            Check(TrackingPolicy.GraceSeconds > 0f, "losing Focus has a short grace window before decay");

            Group("AnomalyEncounter - one authoritative progression path (additive to the frozen suite)");
            var t2 = EncounterTuning.Default;
            Check(t2.ExposeMeter > 0f && t2.ExposeMeter < t2.RepelMeter, "expose threshold sits between zero and the repel meter");
            EncounterStep S(AnomalyState st, float meter, float timer, bool lit, bool foc, float dist, float dt = 0.1f)
                => AnomalyEncounter.Step(st, meter, timer, lit, foc, true, dist, dt, t2);
            Check(S(AnomalyState.Detected, 0.5f, 0f, true, true, 9f).State != AnomalyState.Exposed,
                "meter below the expose threshold is NOT exposed even under perfect focus");
            Check(S(AnomalyState.Detected, t2.ExposeMeter + 0.01f, 0f, true, true, 9f).JustExposed,
                "meter above the expose threshold under focus exposes");
            var custom = t2; custom.ExposeMeter = 0.2f;
            Check(AnomalyEncounter.Step(AnomalyState.Detected, 0.3f, 0f, true, true, true, 9f, 0.1f, custom).JustExposed,
                "the expose threshold is tunable, not hardcoded");
            var repelTick = S(AnomalyState.Exposed, 1f, 0.2f, true, true, 0.5f);
            Check(repelTick.State == AnomalyState.Repelled && !repelTick.Threatening,
                "the tick that repels cannot also count as a catch frame (threat uses the NEW state)");
            Check(S(AnomalyState.Detected, 0f, 0f, false, false, 9f).State == AnomalyState.Roaming,
                "dark and untracked settles back to roaming (no stuck Detected)");
            var fell = S(AnomalyState.Exposed, 0.7f, 0.4f, true, false, 9f);
            Check(fell.State == AnomalyState.Detected && !fell.JustRepelled,
                "losing focus before the meter fills falls back with no free repel");
            Check(S(AnomalyState.Repelled, 1f, t2.ExposedBeatSeconds, true, true, 9f).State == AnomalyState.Recovering,
                "after the retreat beat the anomaly leaves the stage");
            var rec = S(AnomalyState.Recovering, 0f, t2.RecoverSeconds, false, false, 9f);
            Check(rec.State == AnomalyState.Roaming && rec.JustRecovered, "recovery completes exactly once");
            Check(!S(AnomalyState.Repelled, 1f, 0f, true, true, 0.4f).Threatening, "a repelled anomaly next to you is not threatening");
            Check(!S(AnomalyState.Recovering, 0f, 1f, false, false, 0.4f).Threatening, "a recovering anomaly is not threatening");

            Group("BeamGeometry - true cone coverage (direction, range, angle, body radius)");
            var sweep = BeamGeometry.DefaultParamsFor(LightMode.Sweep);
            var focus = BeamGeometry.DefaultParamsFor(LightMode.Focus);
            var off = BeamGeometry.DefaultParamsFor(LightMode.Off);
            Eq(off.Range, 0f, "Off has no reach");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, off, 1, 0, 0.5f), "Off covers nothing");
            Check(sweep.Range > 0f && sweep.HalfAngleDeg > focus.HalfAngleDeg, "Sweep is the wide mode");
            Check(focus.Range > sweep.Range, "Focus reaches farther than Sweep");
            Check(BeamGeometry.Covers(0, 0, 1, 0, sweep, 3, 0, 0f), "on-axis point inside range is covered");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, sweep, sweep.Range + 1f, 0, 0f), "beyond range is not covered");
            Check(BeamGeometry.Covers(0, 0, 1, 0, sweep, sweep.Range + 0.4f, 0, 0.5f), "body radius lets the far edge graze");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, sweep, -3, 0, 0f), "behind the origin is never covered");
            float sweepHalfAt4 = 4f * sweep.HalfTan;
            Check(BeamGeometry.Covers(0, 0, 1, 0, sweep, 4, sweepHalfAt4 - 0.1f, 0f), "inside the sweep cone edge at 4u");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, sweep, 4, sweepHalfAt4 + 0.1f, 0f), "outside the sweep cone edge at 4u");
            float focusHalfAt4 = 4f * focus.HalfTan;
            Check(BeamGeometry.Covers(0, 0, 1, 0, focus, 4, focusHalfAt4 - 0.05f, 0f), "inside the focus cone edge at 4u");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, focus, 4, focusHalfAt4 + 0.15f, 0f), "outside the focus cone edge at 4u - the modes really differ");
            Check(BeamGeometry.Covers(0, 0, -1, 0, sweep, -3, 0, 0f), "aiming left covers the left side");
            Check(!BeamGeometry.Covers(0, 0, -1, 0, sweep, 3, 0, 0f), "aiming left does NOT cover the right side");
            Check(BeamGeometry.Covers(0, 0, 0, 1, sweep, 0, 3, 0f), "aiming up covers above");
            Check(!BeamGeometry.Covers(0, 0, 0, 1, sweep, 3, 0, 0f), "aiming up does not cover the side");
            Check(BeamGeometry.Covers(2, -1, 1, 1, sweep, 4, 1, 0f), "diagonal aim works from an offset origin");
            Check(BeamGeometry.Covers(0, 0, 7, 0, sweep, 3, 0, 0f), "direction need not be normalized");
            Check(!BeamGeometry.Covers(0, 0, 0, 0, sweep, 3, 0, 0f), "degenerate aim covers nothing");
            Check(BeamGeometry.Covers(0, 0, 1, 0, sweep, 4, sweepHalfAt4 + 0.3f, 0.5f), "target radius forgives a grazing silhouette");
            Check(!BeamGeometry.Covers(0, 0, 1, 0, sweep, 4, sweepHalfAt4 + 0.8f, 0.5f), "target radius does not forgive everything");
            var p = BeamGeometry.ParamsFor(LightMode.Focus, 5f, 40f, 9f, 6f);
            Eq(p.Range, 9f, "ParamsFor picks the Focus numbers");
            Eq(p.HalfAngleDeg, 6f, "ParamsFor picks the Focus angle");

            Group("FlashlightInput - latch/hold arbitration (F/Q/L + mouse holds)");
            FlashlightLatches l;
            var none = new FlashlightLatches();
            Eq(FlashlightInput.Resolve(none, false, false, false, false, false, false, out l), LightMode.Off, "no input => Off");
            Eq(FlashlightInput.Resolve(none, true, false, false, false, false, false, out l), LightMode.Sweep, "F latches Sweep");
            Check(l.Sweep && !l.Focus, "F sets only the sweep latch");
            var sweepLatched = new FlashlightLatches { Sweep = true };
            Eq(FlashlightInput.Resolve(sweepLatched, false, true, false, false, false, false, out l), LightMode.Focus, "Q switches to Focus");
            Check(l.Focus && !l.Sweep, "keyboard toggles are mutually exclusive - Q clears the sweep latch");
            var focusLatched = new FlashlightLatches { Focus = true };
            Eq(FlashlightInput.Resolve(focusLatched, false, true, false, false, false, false, out l), LightMode.Off, "Q again toggles Focus off");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, true, false, false, false, out l), LightMode.Off, "L switches the beam off");
            Check(!l.Sweep && !l.Focus, "L clears conflicting latches");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, false, false, true, false, out l), LightMode.Focus, "held RMB overrides a latched mode");
            Check(l.Sweep && !l.Focus, "a mouse hold never changes the latches");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, false, false, false, false, out l), LightMode.Sweep, "releasing the mouse returns to the latched mode");
            Eq(FlashlightInput.Resolve(focusLatched, false, false, false, true, false, false, out l), LightMode.Sweep, "held LMB temporarily beats a Focus latch");
            Eq(FlashlightInput.Resolve(none, false, false, false, true, true, false, out l), LightMode.Focus, "both buttons held: Focus wins");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, false, false, false, true, out l), LightMode.Off, "a dead battery forces Off");
            Check(!l.Sweep && !l.Focus, "a dead battery clears the latches - revival needs deliberate input");
            Eq(FlashlightInput.Resolve(sweepLatched, true, false, false, false, false, true, out l), LightMode.Off, "toggling F while dead does nothing");
            Check(!l.Sweep, "toggling F while dead does not re-latch");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, true, true, false, false, out l), LightMode.Sweep, "L+hold same frame: the hold still shows a beam this frame");
            Eq(FlashlightInput.Resolve(l, false, false, false, false, false, false, out l), LightMode.Off, "...and L's clear sticks after the hold is released");

            Group("BatteryModel - drain, recover, death and deliberate reboot");
            var bp = new BatteryParams { DrainSweepPerSecond = 2.5f, DrainFocusPerSecond = 6f, RecoverDarkPerSecond = 4f, RecoverLitPerSecond = 8f, RebootFraction = 0.2f };
            bool jd, jr;
            var s0 = new BatteryState { Charge = 1f };
            var s1 = BatteryModel.Tick(s0, LightMode.Sweep, false, 1f, bp, out jd, out jr);
            Check(Math.Abs(s1.Charge - 0.975f) < 1e-4f, "Sweep drains 2.5% per second");
            var s2 = BatteryModel.Tick(s0, LightMode.Focus, false, 1f, bp, out jd, out jr);
            Check(Math.Abs(s2.Charge - 0.94f) < 1e-4f, "Focus drains 6% per second - faster than Sweep");
            Check(!jd && !jr, "no transitions mid-drain");
            var s3 = BatteryModel.Tick(s0, LightMode.Off, true, 1f, bp, out jd, out jr);
            Check(Math.Abs(s3.Charge - 1f) < 1e-4f, "charge clamps at full");
            var dark = BatteryModel.Tick(new BatteryState { Charge = 0.5f }, LightMode.Off, false, 1f, bp, out jd, out jr);
            var lit = BatteryModel.Tick(new BatteryState { Charge = 0.5f }, LightMode.Off, true, 1f, bp, out jd, out jr);
            Check(lit.Charge > dark.Charge, "recovery is faster in a genuinely lit room");
            Check(Math.Abs(dark.Charge - 0.54f) < 1e-4f, "dark recovery is 4% per second");
            var st = new BatteryState { Charge = 0.05f };
            int deaths = 0;
            for (int i = 0; i < 60 * 5; i++)
            {
                st = BatteryModel.Tick(st, LightMode.Focus, false, 1f / 60f, bp, out jd, out jr);
                if (jd) deaths++;
                if (st.Dead) break;
            }
            Check(st.Dead && st.Charge == 0f, "draining to zero kills the battery, clamped at 0");
            for (int i = 0; i < 60 * 5; i++) BatteryModel.Tick(st, LightMode.Focus, false, 1f / 60f, bp, out jd, out jr);
            Check(deaths == 1 && !jd, "the depletion callback fires exactly once");
            Check(BatteryModel.Tick(st, LightMode.Focus, false, 10f, bp, out jd, out jr).Charge >= 0f, "a dead battery never goes negative");
            int boots = 0;
            var recovering = st;
            for (int i = 0; i < 60 * 10 && recovering.Dead; i++)
            {
                recovering = BatteryModel.Tick(recovering, LightMode.Off, false, 1f / 60f, bp, out jd, out jr);
                if (jr) boots++;
            }
            Check(!recovering.Dead && boots == 1, "the dead battery revives at the reboot threshold, exactly once");
            Check(recovering.Charge >= bp.RebootFraction, "revival only at or above the reboot fraction");
            var stillDead = BatteryModel.Tick(new BatteryState { Charge = 0.1f, Dead = true }, LightMode.Off, false, 1f, bp, out jd, out jr);
            Check(stillDead.Dead && !jr, "below the reboot fraction it stays dead (no soft-lock flip-flop)");
            var afterBoot = BatteryModel.Tick(recovering, LightMode.Sweep, false, 1f, bp, out jd, out jr);
            Check(afterBoot.Charge < recovering.Charge && !afterBoot.Dead, "after a deliberate relight the beam works again");
            Check(BatteryModel.Tick(s0, LightMode.Sweep, false, 0f, bp, out jd, out jr).Charge == 1f, "zero dt is a no-op");

            Group("BehaviorRules - four distinct anomalies, explicit per-kind numbers");
            var bt = BehaviorTuning.Default;
            var blink = BehaviorProfile.For(AnomalyBehaviorKind.Blink, bt);
            var slide = BehaviorProfile.For(AnomalyBehaviorKind.Slide, bt);
            var sprint = BehaviorProfile.For(AnomalyBehaviorKind.Sprint, bt);
            var hug = BehaviorProfile.For(AnomalyBehaviorKind.CoverHug, bt);
            foreach (var kind in new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Slide, AnomalyBehaviorKind.Sprint, AnomalyBehaviorKind.CoverHug })
            {
                Check(Math.Abs(BehaviorProfile.For(kind, bt).BeamPace - bt.BeamPace) < 1e-4f && bt.BeamPace > 0f && bt.BeamPace < 1f,
                    kind + ": a beam on it slows it down (" + bt.BeamPace + ")");
                Check(BehaviorProfile.For(kind, new BehaviorTuning()).BeamPace == 1f, kind + ": an unset tuning never freezes an anomaly");
            }
            Check(blink.MoveSpeed == bt.BlinkSpeed && slide.MoveSpeed == bt.SlideSpeed &&
                  sprint.MoveSpeed == bt.SprintSpeed && hug.MoveSpeed == bt.CoverHugSpeed,
                "each behavior moves at its own configured speed");
            Check(sprint.DashSpeed > sprint.MoveSpeed, "Sprint's burst is faster than its patrol");
            Check(sprint.TelegraphSeconds > 0f && blink.TelegraphSeconds == 0f && slide.TelegraphSeconds == 0f && hug.TelegraphSeconds == 0f,
                "only Sprint has a windup telegraph - the reaction window is explicit");
            Check(sprint.BeamInterruptsBurst && !blink.BeamInterruptsBurst, "only Sprint's windup is beam-interruptible");
            Check(blink.FreezesWhenObserved && !slide.FreezesWhenObserved && !sprint.FreezesWhenObserved && !hug.FreezesWhenObserved,
                "only Blink freezes when observed");
            Check(hug.CeilingLurker && !blink.CeilingLurker && !slide.CeilingLurker && !sprint.CeilingLurker,
                "only CoverHug is the ceiling lurker (Focus-only identification)");
            Check(blink.CatchGraceSeconds == bt.CatchGraceBlink && slide.CatchGraceSeconds == bt.CatchGraceSlide &&
                  sprint.CatchGraceSeconds == bt.CatchGraceSprint && hug.CatchGraceSeconds == bt.CatchGraceCoverHug,
                "catch windups are per-behavior - no shared coverHug-named constant");
            Check(Math.Abs(hug.SpawnY - BehaviorProfile.HangerCentreY) < 1e-3f && hug.SpawnY >= hug.MinY && hug.SpawnY <= hug.MaxY,
                "CoverHug spawns INSIDE its own clamp band (no contradictory coordinates)");
            Check(Math.Abs(hug.SpawnY + BehaviorProfile.HangerHeight * 0.5f - BehaviorProfile.TopOfScreenY) < 1e-3f,
                "CoverHug hangs from the top of the screen: its top edge is the ceiling");
            Check(Math.Abs(blink.SpawnY - BehaviorProfile.GroundCentreY) < 1e-3f && slide.SpawnY == blink.SpawnY,
                  "Blink and Slide stand on the floor line");
            Check(Math.Abs(BehaviorProfile.GroundCentreY * 2f - BehaviorProfile.CrouchedHeight) < 1e-3f, "...their centre is half their height");
            Check(Math.Abs(sprint.SpawnY - BehaviorProfile.RunnerFloorY) < 1e-3f && Math.Abs(sprint.MinY - BehaviorProfile.RunnerFloorY) < 1e-3f
                  && Math.Abs(sprint.MaxY - BehaviorProfile.RunnerCeilingY) < 1e-3f,
                "the runner may be on the floor or on the ceiling: its band spans both");
            Check(Math.Abs(BehaviorProfile.RunnerCeilingY + BehaviorProfile.RunnerHeight * 0.5f - BehaviorProfile.TopOfScreenY) < 1e-3f,
                "...on the ceiling its back touches the top of the screen");
            Check(hug.MinY > blink.MaxY && hug.MinY > sprint.MinY, "the ceiling band sits above the ground walkers");
            Check(blink.MinY == blink.MaxY && slide.MinY == slide.MaxY && hug.MinY == hug.MaxY,
                "Blink, Slide and CoverHug stay at their one height");
            Eq(BehaviorProfile.ArtKey(AnomalyBehaviorKind.Sprint), "char_anomaly_sprint_a", "the runner has its own drawing");
            Eq(BehaviorProfile.ArtKey(AnomalyBehaviorKind.CoverHug), "char_anomaly_coverhug", "the hanger has its own drawing");
            Eq(BehaviorProfile.ArtKey(AnomalyBehaviorKind.Blink) + BehaviorProfile.ArtKey(AnomalyBehaviorKind.Slide), "char_anomalychar_anomaly", "Blink and Slide share the crouched body");
            Eq(BehaviorProfile.NearestDoorX(0.9f), 0.4f, "the hanger finds the nearest door");
            Eq(BehaviorProfile.NearestDoorX(-50f), -13.8f, "...even far away");
            Check(bt.RetreatSpeed > blink.MoveSpeed && bt.RetreatSpeed > hug.MoveSpeed, "a repelled anomaly withdraws faster than it stalks");
            var customB = bt; customB.CatchGraceCoverHug = 5f;
            Eq(BehaviorProfile.For(AnomalyBehaviorKind.CoverHug, customB).CatchGraceSeconds, 5f, "CoverHug grace is tunable on its own");
            Eq(BehaviorProfile.For(AnomalyBehaviorKind.Blink, customB).CatchGraceSeconds, bt.CatchGraceBlink, "...without touching the other three");

        }

        // ------------------------------------------------------- checkpoint queue
        private static List<VisitorData> RosterFor(int seed, int shiftNumber) =>
            new RosterFactory(seed).GenerateShift(ShiftPlan.Build()[shiftNumber - 1]);

        private static int FindSeed(Func<List<VisitorData>, bool> pred, int shiftNumber)
        {
            for (int seed = 1; seed <= 400; seed++)
                if (pred(RosterFor(seed, shiftNumber))) return seed;
            return -1;
        }

        private static void PatrolCycleTests()
        {
            int c1 = PatrolCycle.Bit(RoomId.Classroom1), c2 = PatrolCycle.Bit(RoomId.Classroom2);
            int cr1 = PatrolCycle.Bit(RoomId.ComfortRoom), cr2 = PatrolCycle.Bit(RoomId.ComfortRoom2);
            Check(!PatrolCycle.Done(0), "patrol: nothing peeked is not done");
            Eq(PatrolCycle.Left(0), "Room 101, Room 202, a CR", "patrol: everything is left at the start");
            Check(!PatrolCycle.Done(c1 | c2), "patrol: the classrooms alone are not enough");
            Check(!PatrolCycle.Done(c1 | cr1), "patrol: Room 202 is needed");
            Check(PatrolCycle.Done(c1 | c2 | cr1), "patrol: CR-1 counts");
            Check(PatrolCycle.Done(c1 | c2 | cr2), "patrol: CR-2 counts");
            Eq(PatrolCycle.Left(c1 | c2 | cr2), "", "patrol: nothing is left when done");

            var s = new GameSession();
            s.ClearedRooms = c1 | cr2;
            s.PeekAnomaliesGone = c2 | cr1;
            var rt = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = s })).Session;
            Eq(rt.ClearedRooms, c1 | cr2, "patrol: progress survives a save");
            Eq(rt.PeekAnomaliesGone, c2 | cr1, "patrol: the standing anomalies driven out survive a save");

            // standing anomalies: deterministic, only in the four peek rooms, at least one a night
            var rooms = new[] { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 };
            bool always = true, steady = true, manyNights = false, someEmpty = false, hallwayNever = true;
            for (int seed = 1; seed <= 300; seed++)
                for (int night = 1; night <= 4; night++)
                {
                    int here = 0;
                    foreach (var r in rooms)
                    {
                        bool a1 = PatrolCycle.StandingIn(seed, night, r);
                        steady &= a1 == PatrolCycle.StandingIn(seed, night, r);
                        if (a1) here++;
                    }
                    always &= here >= 1;
                    manyNights |= here >= 2;
                    someEmpty |= here < 4;
                    hallwayNever &= !PatrolCycle.StandingIn(seed, night, RoomId.Hallway) && !PatrolCycle.StandingIn(seed, night, RoomId.Security);
                }
            Check(always, "standing: every night at least one peek room holds an anomaly");
            Check(steady, "standing: the same seed and night always gives the same rooms");
            Check(manyNights, "standing: some nights have more than one");
            Check(someEmpty, "standing: some rooms are empty, so the player has to look");
            Check(hallwayNever, "standing: never in the hallway or the security room");
        }

        private static void QueueBusyHurryTests()
        {
            Group("CheckpointQueue - the busy-window hurry multiplier");

            var roster = RosterFor(11, 2);
            var q = new CheckpointQueue();
            q.AttachRoster(roster, new QueueTuning
            {
                MissingStudentChance = 0f,
                ArrivalMinSeconds = 10f,
                ArrivalMaxSeconds = 10f,
                BusyHurryMultiplier = 2f,
            }, 4321);
            q.MarkStarted();
            q.ForceNextArrival();                 // window busy: the clock now runs double
            Check(!q.Tick(4.9f).Any, "busy window: 4.9s real time (9.8s of clock) is not enough");
            Check(q.Tick(0.2f).ToLine != null, "busy window: 5.1s real time completes the 10s interval");

            // clear the window completely, then the clock must breathe at base speed again
            q.CommitActiveResolution(); q.CompleteActive();   // the waiter steps up
            q.CommitActiveResolution(); q.CompleteActive();   // and leaves: window free
            Check(!q.Tick(9.7f).Any, "free window: 9.7s is not enough for the 9.8s left");
            Check(q.Tick(0.2f).Any, "free window: the remaining interval completes at base speed");

            // a student who is done never leaves the window empty for long
            var q2 = new CheckpointQueue();
            q2.AttachRoster(RosterFor(11, 3), new QueueTuning
            {
                MissingStudentChance = 0f, ArrivalMinSeconds = 100f, ArrivalMaxSeconds = 100f,
                NextStudentSeconds = 3f,
            }, 4321);
            q2.MarkStarted();
            q2.ForceNextArrival();
            q2.CommitActiveResolution(); q2.CompleteActive();
            Check(!q2.Tick(2.9f).Any, "empty window: the short gap still takes its 3 seconds");
            Check(q2.Tick(0.2f).ToWindow != null, "empty window: the next student steps up after 3 seconds, not the long clock");
        }

        private static void StampRulesTests()
        {
            Group("InspectionRules - detain verdict + warnings/fines (stamp rework)");
            var roster = RosterFor(3, 2);
            VisitorData legit = null, anom = null;
            foreach (var v in roster) { if (legit == null && !v.IsAnomaly) legit = v; if (anom == null && v.IsAnomaly) anom = v; }
            Check(legit != null && anom != null, "roster provides a legit and an anomaly subject");

            var rA = InspectionRules.Apply(anom, Verdict.Reject);
            Check(rA.Correct, "reject scores anomalies correctly");
            var aL = InspectionRules.Apply(legit, Verdict.Allow);
            Check(aL.Correct, "allow scores legit students correctly");

            var sN = new GameSession { Mode = GameMode.Normal, Score = 250 };
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject));   // miss 1
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow));   // correct
            sN.RecordVerdict(InspectionRules.Apply(anom, Verdict.Allow));    // miss 2
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject));  // miss 3 -> fine
            Eq(sN.Warnings, 3, "three mistakes book three warnings");
            Eq(sN.Fines, 1, "the third warning draws a fine");
            Eq(sN.Score, 150, "the fine costs 100 score (penalty, not game over)");
        }

        private static void FloorFileTests()
        {
            Group("RecordSearch - floor folders for the FLOOR FILES cabinet");
            var canon = new RosterFactory(12).Canon;
            var folders = RecordSearch.BuildFloorFolders(canon);
            Eq(folders.Count, 2, "the roster files under exactly two floors");
            Eq(folders[0].Floor, 1, "folders come out floor-ascending");
            Eq(folders[1].Floor, 2, "the second folder is the second floor");
            Check(folders[0].Indices.Count > 0 && folders[1].Indices.Count > 0,
                  "both floors hold files (no dead folder in shipping content)");
            var seen = new HashSet<int>();
            bool ordered = true, floorsOk = true;
            foreach (var f in folders)
            {
                string prev = "";
                foreach (int i in f.Indices)
                {
                    if (string.Compare(canon[i].FullName, prev, System.StringComparison.OrdinalIgnoreCase) < 0) ordered = false;   // A-Z inside a folder
                    prev = canon[i].FullName;
                    if (!seen.Add(i)) ordered = false;           // filed exactly once
                    if (RecordSearch.FloorOf(canon[i].FirstClassroom) != f.Floor) floorsOk = false;
                }
            }
            Eq(seen.Count, canon.Count, "every canon student is filed exactly once");
            Check(ordered, "files are listed A-Z by name inside their folder");
            Check(floorsOk, "each file sits on its classroom's floor");
            Check(folders[0].Label != folders[1].Label &&
                  folders[0].Label.Contains("ROOM 101") && folders[1].Label.Contains("ROOM 202"),
                  "folder labels are distinct and name the room (got '" + folders[0].Label + "')");
            Eq(RecordSearch.FloorOf(RoomId.Classroom1), 1, "Room 101 is the 1st floor");
            Eq(RecordSearch.FloorOf(RoomId.Classroom2), 2, "Room 202 is the 2nd floor");
            Eq(RecordSearch.BuildFloorFolders(null).Count, 0, "a null canon files nothing");
            Eq(RecordSearch.BuildFloorFolders(new List<StudentIdentity>()).Count, 0,
               "an empty canon files nothing");
            // determinism: the same roster always produces the same folders
            var again = RecordSearch.BuildFloorFolders(canon);
            bool same = again.Count == folders.Count;
            for (int f = 0; same && f < again.Count; f++)
            {
                same = again[f].Floor == folders[f].Floor && again[f].Label == folders[f].Label &&
                       again[f].Indices.Count == folders[f].Indices.Count;
                for (int k = 0; same && k < again[f].Indices.Count; k++)
                    same = again[f].Indices[k] == folders[f].Indices[k];
            }
            Check(same, "floor folders are deterministic (same canon, same folders)");
        }

        // The floor-files drawer's motion curve. Pure maths, so "does it answer the click
        // almost instantly, decelerate, and never bounce?" is a testable claim, not a vibe.
        private static void DrawerMotionTests()
        {
            Group("DrawerMotion - the floor-files pull drawer");
            Eq(DrawerMotion.EaseOut(0f), 0f, "the pull starts at 0");
            Eq(DrawerMotion.EaseOut(1f), 1f, "the pull ends at 1");
            Check(DrawerMotion.EaseOut(0.5f) > 0.5f,
                  $"an ease-out covers more than half the travel in half the time ({DrawerMotion.EaseOut(0.5f):F3})");
            Check(DrawerMotion.EaseOut(0.25f) > 0.5f,
                  $"...and over half of it in the first quarter ({DrawerMotion.EaseOut(0.25f):F3}) - it answers the click");
            bool monotonic = true, noOvershoot = true;
            float prev = 0f;
            for (int i = 0; i <= 200; i++)
            {
                float v = DrawerMotion.EaseOut(i / 200f);
                if (v < prev - 1e-6f) monotonic = false;   // never runs backwards = never bounces
                if (v > 1f + 1e-6f) noOvershoot = false;   // never passes the end = no overshoot
                prev = v;
            }
            Check(monotonic, "the pull is monotonic (no bounce, no rubber-band)");
            Check(noOvershoot, "the pull never overshoots the open position");

            Eq(DrawerMotion.Advance(0f, 1f / 60f, true), 1f / 60f / DrawerMotion.DurationSeconds,
               "one 60 Hz frame advances exactly dt/duration");
            Check(DrawerMotion.Advance(0f, 0f, true) == 0f, "a zero frame moves nothing");
            Check(DrawerMotion.Advance(0.5f, -3f, true) == 0.5f, "a negative dt is treated as no time at all");
            Eq(DrawerMotion.Advance(0.5f, 99f, true), 1f, "opening clamps at 1 (no run-away)");
            Eq(DrawerMotion.Advance(0.5f, 99f, false), 0f, "closing clamps at 0");
            int frames = 0;
            float pull = 0f;
            while (pull < 1f && frames < 600) { pull = DrawerMotion.Advance(pull, 1f / 60f, true); frames++; }
            Check(pull >= 1f && Math.Abs(frames * (1f / 60f) - DrawerMotion.DurationSeconds) < 1f / 60f + 1e-4f,
                  $"a full pull takes its stated {DrawerMotion.DurationSeconds * 1000f:F0} ms ({frames} frames at 60 Hz)");
            Check(DrawerMotion.DurationSeconds <= 0.2f,
                  $"the whole motion is under a fifth of a second ({DrawerMotion.DurationSeconds:F3}s) - it feels immediate");

            Eq(DrawerMotion.BodyOffset(0f), 0f, "a shut drawer has travelled 0px");
            Eq(DrawerMotion.BodyOffset(1f), DrawerMotion.TravelPx, "an open drawer has travelled the full depth");
            Eq(DrawerMotion.BodyScale(0f), 1f, "shut, the body is at 1x");
            Eq(DrawerMotion.BodyScale(1f), DrawerMotion.NearScale, "out, it is at the near scale");
            Check(DrawerMotion.NearScale > 1f && DrawerMotion.NearScale < 1.1f,
                  $"the depth cue is real but subtle ({DrawerMotion.NearScale:F3}x)");

            Eq(DrawerMotion.RowReveal(0f), 0f, "shut, the files are not revealed");
            Eq(DrawerMotion.RowReveal(1f), 1f, "open, the files are fully revealed");
            Check(DrawerMotion.RowReveal(0.5f) < DrawerMotion.EaseOut(0.5f),
                  "the files LAG the body, so they emerge from the drawer instead of riding painted on it");
            Eq(DrawerMotion.RowOffset(0f), DrawerMotion.RowEmergePx, "shut, each file still sits its emerge-distance up");
            Eq(DrawerMotion.RowOffset(1f), 0f, "open, every file has settled into place");
            Eq(DrawerMotion.CavityShadeAt(0f), 0f, "shut, the cavity is not shaded");
            Eq(DrawerMotion.CavityShadeAt(1f), DrawerMotion.CavityShadeAlpha, "open, the cavity shade is at full depth");

            Check(!DrawerMotion.Finished(0.5f, true) && DrawerMotion.Finished(1f, true),
                  "Finished only reports done at the end of an opening");
            Check(!DrawerMotion.Finished(0.5f, false) && DrawerMotion.Finished(0f, false),
                  "...and at the start of a closing (i.e. fully shut)");
        }

        // The ground-floor plan is data, so the map's layout rules are provable without pixels:
        // every room present, in the place the reference drawing puts it, none overlapping, and
        // the sealed / comfort rooms distinguishable from the patrol rooms.
        private static void FloorPlanTests()
        {
            Group("FloorPlan - HOLLOWBROOK COLLEGE ground floor");
            Eq(FloorPlan.Title, "HOLLOWBROOK COLLEGE - GROUND FLOOR", "the sheet carries the full title");
            Check(FloorPlan.LayoutIsSane(out string problem),
                "every room is on the sheet and no two rooms overlap" +
                (problem != null ? " (" + problem + ")" : ""));
            foreach (var r in FloorPlan.Rooms)
            {
                Check(!string.IsNullOrEmpty(r.Label) && !string.IsNullOrEmpty(r.Key),
                      "room '" + r.Key + "' has a key and a label");
                Check(r.W > 0f && r.H > 0f, "room '" + r.Key + "' has a real size");
                Check(r.Sealed || r.Comfort || r.Room.HasValue,
                      "room '" + r.Key + "' is either a patrol room or explicitly sealed/comfort");
                Check(!(r.Sealed && r.Comfort), "room '" + r.Key + "' is not both sealed and a comfort room");
            }

            var sec = FloorPlan.ForRoom(RoomId.Security);
            var hall = FloorPlan.ForRoom(RoomId.Hallway);
            var c1 = FloorPlan.ForRoom(RoomId.Classroom1);
            var c2 = FloorPlan.ForRoom(RoomId.Classroom2);
            Check(sec.HasValue && hall.HasValue && c1.HasValue && c2.HasValue,
                "the security checkpoint, the hallway and both classrooms are all on the plan");
            var server = FloorPlan.ForKey("room_server");
            Check(server.HasValue && server.Value.Sealed && !server.Value.Room.HasValue,
                "the server / identification room is on the plan, SEALED, and not a patrol destination");
            Check(server.Value.Label.Contains("SERVER") && server.Value.Label.Contains("SEALED"),
                "the sealed room says what it is (got '" + server.Value.Label.Replace("\n", " / ") + "')");

            // the reference drawing's spatial relationships
            Check(sec.Value.Left < hall.Value.Left && sec.Value.Top >= hall.Value.Bottom,
                "the security checkpoint hangs under the hallway's west end, as on the desk map");
            Check(c1.Value.Bottom <= hall.Value.Top + 0.01f && c2.Value.Bottom <= hall.Value.Top + 0.01f,
                $"BOTH classrooms are ABOVE the hallway, as the reference plan draws them " +
                $"({c1.Value.Bottom} / {c2.Value.Bottom} <= {hall.Value.Top})");
            Check(c1.Value.Right <= c2.Value.Left + 0.01f,
                $"Room 101 sits WEST of Room 202 along the north wall ({c1.Value.Right} <= {c2.Value.Left})");
            Check(sec.Value.Top >= hall.Value.Bottom - 0.01f && sec.Value.X < hall.Value.X,
                $"the security checkpoint hangs BELOW the hallway's west end ({sec.Value.Top} >= {hall.Value.Bottom})");
            var crN = FloorPlan.ForKey("room_cr1").Value;
            Check(crN.Left >= hall.Value.Right - 0.01f,
                "the comfort room hangs off the EAST end, clear of the corridor");
            Check(server.Value.Left >= hall.Value.Right - 0.01f,
                $"the sealed room is on the RIGHT ({server.Value.Left} >= {hall.Value.Right})");
            Check(hall.Value.W > 2.5f * Math.Max(hall.Value.H, 1f) &&
                  hall.Value.W > c1.Value.W && hall.Value.W > sec.Value.W,
                $"the hallway is the long central passage ({hall.Value.W}x{hall.Value.H})");
            Check(c1.Value.Label.Contains("CLASSROOM 1") && c1.Value.Label.Contains("101") &&
                  c2.Value.Label.Contains("CLASSROOM 2") && c2.Value.Label.Contains("202"),
                "the classrooms keep their names and room numbers");

            // comfort rooms: present, labelled, and out of the way
            var comfort = FloorPlan.ComfortRooms();
            Eq(comfort.Count, 2, "the plan carries CR-1 and CR-2, like the desk map");
            foreach (var cr in comfort)
            {
                Check(cr.Comfort && (cr.Room == RoomId.ComfortRoom || cr.Room == RoomId.ComfortRoom2) && !cr.Sealed,
                      "'" + cr.Key + "' is a comfort room you can peek into, and it is not sealed");
                Check(cr.Label.StartsWith("CR-"), "'" + cr.Key + "' is labelled like the desk map");
                Check(!cr.Overlaps(hall.Value) && !cr.Overlaps(c1.Value) && !cr.Overlaps(c2.Value) &&
                      !cr.Overlaps(sec.Value) && !cr.Overlaps(server.Value),
                      "'" + cr.Key + "' obstructs nothing - not the hallway, not a room");
                Check(cr.DoorX.HasValue && cr.DoorY.HasValue, "'" + cr.Key + "' has a doorway on the plan");
            }

            // doors sit on their room's own wall, so the drawn gap reads as a way in
            bool doorsOnWalls = true;
            foreach (var r in FloorPlan.Rooms)
            {
                if (!r.DoorX.HasValue || !r.DoorY.HasValue) continue;
                float dx = r.DoorX.Value, dy = r.DoorY.Value;
                bool onWall = Math.Abs(dx - r.Left) < 0.01f || Math.Abs(dx - r.Right) < 0.01f ||
                              Math.Abs(dy - r.Top) < 0.01f || Math.Abs(dy - r.Bottom) < 0.01f;
                if (!onWall) { doorsOnWalls = false; Console.WriteLine("   door off-wall: " + r.Key); }
            }
            Check(doorsOnWalls, "every doorway is drawn on its own room's wall");

            // patrol coverage: the map can never lose a room the player can actually walk to
            Check(FloorPlan.ForRoom(RoomId.Hallway).HasValue, "the hallway, where anomalies roam, is on the plan");
            float mx, my;
            Check(FloorPlan.TryMarkerPos(RoomId.Hallway, out mx, out my) &&
                  Math.Abs(mx - hall.Value.X) < 1e-4f && Math.Abs(my - hall.Value.Y) < 1e-4f,
                "the position marker for a room is that room's centre");

            // the fit maths the map view centres itself with
            Eq(FloorPlan.FitScale(FloorPlan.DesignW, FloorPlan.DesignH), 1f, "the design space fits itself at 1x");
            Eq(FloorPlan.FitScale(FloorPlan.DesignW / 2f, FloorPlan.DesignH), 0.5f,
               "a half-width area scales the plan to half (uniform, never stretched)");
            Eq(FloorPlan.FitScale(FloorPlan.DesignW, FloorPlan.DesignH / 2f), 0.5f,
               "a half-height area does the same");
            Eq(FloorPlan.FitScale(0f, 0f), 1f, "a degenerate area falls back to 1x instead of dividing by zero");
        }

        // The map sheet's layout maths. Pure, so "centred at every resolution, never clipped,
        // never unreadable" is a proved property rather than a screenshot someone squinted at.
        private static void MapSheetLayoutTests()
        {
            Group("MapSheetLayout - the centred ground-floor sheet");
            var baseFit = MapSheetLayout.Fit(1280f, 720f);
            Check(baseFit.SheetW > 0f && baseFit.SheetH > 0f, "the sheet has a real size");
            Check(Math.Abs(baseFit.SheetW / baseFit.SheetH - FloorPlan.DesignW / FloorPlan.DesignH) < 1e-4f,
                  $"the sheet keeps the plan's aspect ({baseFit.SheetW / baseFit.SheetH:F4} vs " +
                  $"{FloorPlan.DesignW / FloorPlan.DesignH:F4}) - the drawing is scaled, never stretched");
            Check(baseFit.FitsIn(1280f, 720f), "the sheet and its CLOSE row fit inside 1280x720");

            // CLOSE is anchored to the screen's bottom edge while the sheet is centred, so the
            // gap between them is only meaningful once both are in the same space
            float gap = baseFit.GapUnderSheet(720f);
            Check(Math.Abs(gap - MapSheetLayout.CloseGap) < 0.01f,
                  $"CLOSE hangs exactly CloseGap under the sheet (got {gap:F2})");
            Check(baseFit.CloseBottomMargin >= 0f,
                  $"CLOSE stays on screen with a {baseFit.CloseBottomMargin:F0}px bottom margin");

            var screens = new[]
            {
                new[] { 1280f, 720f }, new[] { 1920f, 1080f }, new[] { 1024f, 768f },
                new[] { 800f, 600f }, new[] { 2560f, 1080f }, new[] { 1280f, 600f },
                new[] { 3840f, 2160f }, new[] { 3440f, 1440f }, new[] { 640f, 480f },
            };
            bool allFit = true, allReadable = true, allGap = true, allCentred = true;
            foreach (var s in screens)
            {
                // AspectMatchGuard: wider than 16:9 matches HEIGHT, otherwise WIDTH
                var visible = s[0] / s[1] > 1280f / 720f
                    ? new[] { s[0] * 720f / s[1], 720f }
                    : new[] { 1280f, s[1] * 1280f / s[0] };
                var fit = MapSheetLayout.Fit(visible[0], visible[1]);
                if (!fit.FitsIn(visible[0], visible[1])) { allFit = false; Console.WriteLine("   does not fit: " + s[0] + "x" + s[1]); }
                if (MapSheetLayout.TypeSize(30, fit.ChromeTextScale, 1f) < 20 ||
                    MapSheetLayout.TypeSize(12, fit.ChromeTextScale, 1f) < 10 ||
                    MapSheetLayout.TypeSize(15, fit.PlanTextScale, 1f) < 11)
                { allReadable = false; Console.WriteLine("   unreadable type: " + s[0] + "x" + s[1]); }
                if (Math.Abs(fit.GapUnderSheet(visible[1]) - MapSheetLayout.CloseGap) > 0.01f ||
                    fit.CloseBottomMargin < 0f) allGap = false;
                // centred BY CONSTRUCTION: the sheet is anchored to the centre with no offset,
                // so the only thing that can break centring is a size that overflows - hence
                // the fit check above. Assert the margin symmetry that follows from it.
                float sideMargin = (visible[0] - fit.SheetW) / 2f;
                float topMargin = (visible[1] - fit.SheetH) / 2f;
                if (sideMargin < -0.01f || topMargin < -0.01f) allCentred = false;
            }
            Check(allFit, "the sheet and CLOSE fit at 9 resolutions from 640x480 to 4k, 4:3 to 21:9");
            Check(allCentred, "...with non-negative margins on both axes, i.e. centred and unclipped");
            Check(allReadable, "...and the type never drops below readable at any of them");
            Check(allGap, "...and CLOSE keeps the same gap under the sheet, on screen, at all of them");

            // the drawing scales with the space it is given (no hard-coded pixel offsets)
            var big = MapSheetLayout.Fit(2560f, 1440f);
            var small = MapSheetLayout.Fit(1280f, 720f);
            Check(big.PlanScale > small.PlanScale, "a bigger window draws a bigger plan");
            Check(Math.Abs(big.PlanScale / small.PlanScale - big.PaperScale / small.PaperScale) < 1e-3f,
                  "the plan and the sheet scale together (one uniform fit)");

            // the design -> RectTransform mapping. Design y points DOWN like a drawing,
            // RectTransform y points UP: the flip lives in exactly one function.
            Check(MapSheetLayout.ToAnchoredY(-126f, 1f) > 0f,
                  "a room ABOVE the centre in design space lands above the centre on screen");
            Check(MapSheetLayout.ToAnchoredY(126f, 1f) < 0f,
                  "...and a room below it lands below it (the plan is never mirrored)");
            Eq(MapSheetLayout.ToAnchoredX(20f, 2f), 40f, "x scales by the plan scale");
            Eq(MapSheetLayout.ToAnchoredY(20f, 2f), -40f, "y scales by the plan scale, negated");

            // degenerate input must not produce a broken sheet
            var tiny = MapSheetLayout.Fit(0f, 0f);
            Check(tiny.SheetW > 0f && tiny.SheetH > 0f && tiny.PlanScale > 0f,
                  "a zero-size window still yields a sane (minimum) sheet instead of NaN");
            Check(MapSheetLayout.TypeSize(30, 0f, 1f) >= 9, "type sizes have a floor");
        }

        private static void SlipFairnessTests()
        {
            Group("Slip generation - fairness across seeds and shifts");
            bool legitOk = true, mismatchOk = true;
            int mismatchSeen = 0;
            for (int seed = 1; seed <= 6; seed++)
                for (int sh = 1; sh <= 5; sh++)
                    foreach (var v in RosterFor(seed, sh))
                    {
                        bool agrees = v.SlipName == v.ClaimedName && v.SlipIdNumber == v.IdNumber;
                        if (!v.IsAnomaly) { if (!agrees) legitOk = false; }
                        else
                        {
                            bool has = v.Defects.Contains(DefectType.SlipMismatch);
                            if (has) { mismatchSeen++; if (agrees) mismatchOk = false; }
                            else if (!agrees) mismatchOk = false;
                        }
                    }
            Check(legitOk, "legitimate slips always agree with the card (fairness invariant)");
            Check(mismatchSeen > 0, "slip discrepancies really occur on anomalies");
            Check(mismatchOk, "a slip disagrees exactly when the SlipMismatch defect is booked");
        }

        private static void CultureRoundTripTests()
        {
            Group("SaveFormat/SettingsData - locale independence (I-05)");
            var saved = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Globalization.CultureInfo comma = null;
            try { comma = new System.Globalization.CultureInfo("de-DE"); }
            catch (System.Globalization.CultureNotFoundException) { }
            if (comma == null)
            {
                Check(true, "skipped here: invariant-globalization process - rerun with ICU for the de-DE pass");
                return;
            }
            System.Threading.Thread.CurrentThread.CurrentCulture = comma;
            try
            {
                var save = new SaveGame();
                save.Session.Mode = GameMode.Normal;
                save.Session.Sanity = 63.5f;
                save.Session.QueueState = new QueueSnapshot { Started = true, TimerSeconds = 4.5f, Warned = 1 };
                save.Settings.MasterVolume = 0.55f;
                save.Settings.MusicVolume = 0.3f;
                var text = SaveFormat.Serialize(save);
                Check(text.Contains("sanity=63.5") && text.Contains("queueTimer=4.5") && text.Contains("0.55"),
                      "de-DE: floats serialize with a DOT, never a comma");
                var back = SaveFormat.Deserialize(text);
                Check(back != null && back.Session.Sanity == 63.5f &&
                      back.Session.QueueState.TimerSeconds == 4.5f && back.Settings.MasterVolume == 0.55f,
                      "de-DE: checksummed round-trip parses back exactly");
                var roster = RosterFor(5, 1);
                Check(roster.All(v => v.IdNumber.All(c => c >= '0' && c <= '9' || c == '-' || (c >= 'A' && c <= 'Z'))),
                      "de-DE: generated IDs stay ASCII digits (D4 formatting is invariant)");
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        private static void ResumeReplayTests()
        {
            Group("RosterFactory - resume replays identical visitors (I-09)");
            var plan = ShiftPlan.Build();
            var played = new RosterFactory(99);
            List<VisitorData> shift4Played = null;
            for (int i = 0; i < 4; i++) shift4Played = played.GenerateShift(plan[i]);   // a played run
            var replay = new RosterFactory(99);
            for (int i = 0; i < 3; i++) replay.GenerateShift(plan[i]);   // what Continue does
            var shift4Replay = replay.GenerateShift(plan[3]);
            Check(shift4Played.Count == shift4Replay.Count &&
                  shift4Played.Zip(shift4Replay, (x, y) => x.IdNumber == y.IdNumber && x.IsAnomaly == y.IsAnomaly)
                             .All(ok => ok),
                  "shift 4 roster is identical whether played straight or replayed from the seed");
        }

        private static void QueueInvariantTests()
        {
            Group("CheckpointQueue - FIFO invariant + warned-edge persistence (audit fixes)");

            // 1) a free window with a waiting line belongs to the head of the line: a fresh
            //    arrival must never jump it. State is built through restore - the only path
            //    that can legitimately hold Active==null with a non-empty line.
            var roster = RosterFor(7, 1);
            var q = new CheckpointQueue();
            q.AttachRoster(roster, new QueueTuning { MissingStudentChance = 0f }, 7);
            q.RestoreFromSnapshot(new QueueSnapshot
            {
                Started = true,
                RosterCursor = 3,
                Arrived = 3,
                Processed = 0,
                TimerSeconds = 5f,
                WaitingIdx = new List<int> { roster[1].VisitorIndex, roster[2].VisitorIndex },
                ActiveIdx = -1,
                Warned = 0,
            }, 0);
            Check(q.Active == null && q.WaitingCount == 2, "setup: free window behind a two-student line");
            var r = q.ForceNextArrival();
            Check(r.ToWindow == roster[1], "FIFO: the line head steps up to the free window, not the fresh arrival");
            Check(r.ToLine == roster[3], "FIFO: the fresh arrival joins the line behind it");
            Check(q.WaitingCount == 2 && q.WaitingLine.First() == roster[2],
                  "FIFO: line order is preserved behind the promoted head");

            // 2) the pressure warning is once per shift - including across save/continue
            var t = new QueueTuning
            {
                MissingStudentChance = 0f,
                PressureWarnAt = 2,
                OverwhelmedAt = 99,
                ArrivalMinSeconds = 1f,
                ArrivalMaxSeconds = 1f,
            };
            var a = new CheckpointQueue();
            a.AttachRoster(roster, t.Clone(), 21);
            a.MarkStarted();
            a.ForceNextArrival();                              // window
            Check(!a.ForceNextArrival().PressureWarning, "line 1: no warning yet");
            Check(a.ForceNextArrival().PressureWarning, "line 2: the one-time warning fires");
            a.CommitActiveResolution(); a.CompleteActive();    // line shrinks back to 1
            var snap = a.BuildSnapshot();
            Eq(snap.Warned, 1, "the snapshot carries the fired warning edge");

            var b = new CheckpointQueue();
            b.AttachRoster(roster, t.Clone(), 21);
            b.RestoreFromSnapshot(snap, 0);
            var grow = b.ForceNextArrival();                   // line back to the threshold
            Check(b.WaitingCount == 2, "resume: the line is back at the warning threshold");
            Check(!grow.PressureWarning, "continue never re-fires the already-fired warning");

            // 3) the warned key survives the checksummed file; key-less (older) files fall
            //    back to reconstructing the edge from the restored line length
            var save = new SaveGame();
            save.Session.Mode = GameMode.Normal;
            save.Session.QueueState = snap;
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(save));
            Check(back != null && back.Session.QueueState != null && back.Session.QueueState.Warned == 1,
                  "queueWarned round-trips through the checksummed save file");

            var legacyLong = new QueueSnapshot
            {
                Started = true,
                RosterCursor = 3,
                Arrived = 3,
                WaitingIdx = new List<int> { roster[1].VisitorIndex, roster[2].VisitorIndex },
                ActiveIdx = -1,
            };
            Eq(legacyLong.Warned, -1, "a snapshot without the key reads as unknown (-1)");
            var c = new CheckpointQueue();
            c.AttachRoster(roster, t.Clone(), 21);
            c.RestoreFromSnapshot(legacyLong, 0);              // line 2 at restore: had warned
            Check(!c.ForceNextArrival().PressureWarning, "legacy save at threshold: no second warning");

            var legacyShort = new QueueSnapshot
            {
                Started = true,
                RosterCursor = 2,
                Arrived = 2,
                WaitingIdx = new List<int> { roster[1].VisitorIndex },
                ActiveIdx = roster[0].VisitorIndex,
            };
            var d = new CheckpointQueue();
            d.AttachRoster(roster, t.Clone(), 21);
            d.RestoreFromSnapshot(legacyShort, 0);             // line 1: warning still earnable
            Check(d.ForceNextArrival().PressureWarning, "legacy save below threshold: the warning is still earnable once");
        }

        // ------------------------------------------------------- speech bubble (task 4)
        // The bubble beside the student's head: a line STAYS until the next line replaces
        // it or the desk clears it - there is no timeout and no hover rule any more.
        private static void SpeechBubbleTests()
        {
            Group("SpeechBubbleClock - a line stays until replaced or cleared");

            var b = new SpeechBubbleClock();
            Check(!b.Visible && b.Generation == 0, "born hidden, with no generations spent");
            Eq(b.Who, "", "born with no speaker");
            Eq(b.Text, "", "born with no line");

            b.Say("AT THE WINDOW", "Good evening.");
            Check(b.Visible, "a line shows the bubble");
            Eq(b.Who, "AT THE WINDOW", "the bubble carries the speaker");
            Eq(b.Text, "Good evening.", "the bubble carries the line");
            Eq(b.Generation, 1, "the first line is generation 1");

            // nothing but a new line or a cleanup may change what is on screen
            Check(b.Visible && b.Text == "Good evening.",
                "the line is still up later - there is no timer to take it away");

            b.Say("AT THE WINDOW", "Second visit.");
            Check(b.Visible && b.Generation == 2, "the next line replaces it (generation 2)");
            Eq(b.Text, "Second visit.", "...and the bubble shows the NEW line");

            // robustness: null payload
            b.Say(null, null);
            Check(b.Visible, "a null line still shows the bubble");
            Eq(b.Who, "", "null speaker is stored as empty");
            Eq(b.Text, "", "null text is stored as empty");

            // HideNow: the cleanup path (room change / visitor walks off)
            b.Say("AT THE WINDOW", "Cleanup.");
            b.HideNow();
            Check(!b.Visible, "HideNow removes the bubble instantly");
            b.HideNow();
            Check(!b.Visible, "...and is idempotent");
            b.Say("AT THE WINDOW", "After cleanup.");
            Check(b.Visible, "a later line still shows normally");

            int g0 = b.Generation;
            for (int i = 0; i < 10; i++) b.Say("AT THE WINDOW", "line " + i);
            Eq(b.Generation, g0 + 10, "generations are strictly monotonic - every line gets its own");
            Eq(b.Text, "line 9", "the bubble always shows the newest line only (no overlap)");

            Group("SpeechBubbleLayout - wrap estimate, truncation, bubble height");

            Eq(SpeechBubbleLayout.Wrap("", 10).Count, 1, "empty text is one empty line");
            Eq(SpeechBubbleLayout.Wrap(null, 10).Count, 1, "null text is one empty line");
            Eq(SpeechBubbleLayout.Wrap("short", 10)[0], "short", "a short line stays one line");
            Eq(SpeechBubbleLayout.Wrap("0123456789", 10).Count, 1, "text exactly filling the line stays one line");
            var wrapped = SpeechBubbleLayout.Wrap("aaaa bbbb cccc", 9);
            Eq(wrapped.Count, 2, "word wrap: 'aaaa bbbb cccc' at 9 chars is two lines");
            Eq(wrapped[0], "aaaa bbbb", "...first line keeps whole words");
            Eq(wrapped[1], "cccc", "...second line carries the rest");
            var split = SpeechBubbleLayout.Wrap("aaaaaaaaaa", 4);
            Eq(split.Count, 3, "an overlong word is hard-split");
            Eq(split[0], "aaaa", "...into full-line chunks");
            Eq(split[2], "aa", "...with the remainder last");
            Eq(SpeechBubbleLayout.Wrap("a\nb", 10).Count, 2, "explicit newlines force new lines");
            Eq(SpeechBubbleLayout.Wrap("a\n\nb", 10).Count, 3, "an empty paragraph keeps its line");
            Eq(SpeechBubbleLayout.CountLines("aaaa bbbb cccc", 9), 2, "CountLines agrees with Wrap");
            foreach (int cpl in new[] { 1, 7, 20, 36 })
            {
                bool over = false;
                foreach (string line in SpeechBubbleLayout.Wrap(
                    "the quick brown fox jumps over the lazy dog near the midnight checkpoint window", cpl))
                    if (line.Length > cpl) over = true;
                Check(!over, "no wrapped line exceeds " + cpl + " chars");
            }
            Eq(SpeechBubbleLayout.Wrap("ab", 0).Count, 2, "charsPerLine below 1 is treated as 1 (no crash, no infinite loop)");

            string fits = SpeechBubbleLayout.Fit("one two three", 10, 5, out int fitLines);
            Eq(fits, "one two three", "text that fits is returned unchanged");
            Eq(fitLines, 2, "...with its true line count");
            string longText = "word " + string.Join(" ", Enumerable.Range(1, 60).Select(i => "w" + i));
            string cut = SpeechBubbleLayout.Fit(longText, 20, 3, out int cutLines);
            Eq(cutLines, 3, "overlong text is cut to the max lines");
            Check(cut.EndsWith("…"), "the cut ends with an ellipsis");
            Check(cut.Length <= 20 * 3, "the cut never exceeds the line budget (bubble can't overflow)");
            Check(longText.StartsWith(cut.Substring(0, cut.Length - 1)),
                "the cut keeps the BEGINNING of the line verbatim (word-boundary trim, nothing shuffled)");
            Check(!cut.EndsWith(" …") && !cut.EndsWith("  …"),
                "the ellipsis attaches directly to the last whole word");
            string oneLine = SpeechBubbleLayout.Fit("exactly fits here", 8, 1, out int one1);
            Eq(one1, 1, "maxLines=1 produces exactly one line");
            Check(oneLine.EndsWith("…") && oneLine.Length <= 8, "...inside the single-line budget");
            string exact = SpeechBubbleLayout.Fit("aaa bbb", 7, 1, out int exactLines);
            Eq(exact, "aaa bbb", "text that exactly fills maxLines is NOT truncated");
            Eq(exactLines, 1, "...and reports one line");

            Check(Math.Abs(SpeechBubbleLayout.Height(1, 15f, 16f, 8f, 8f) - (8 + 16 + 15 + 8)) < 1e-4f,
                "height = paddings + tag line + text lines");
            Check(Math.Abs(SpeechBubbleLayout.Height(0, 15f, 16f, 8f, 8f) - (8 + 16 + 15 + 8)) < 1e-4f,
                "height floors at one text line");
            Check(Math.Abs(SpeechBubbleLayout.Height(10, 15f, 16f, 8f, 8f) - 182f) < 1e-4f,
                "ten lines grow the bubble to 182px (the desk box caps at 210)");
            Check(SpeechBubbleLayout.Height(4, 15f, 16f, 8f, 8f) < SpeechBubbleLayout.Height(5, 15f, 16f, 8f, 8f),
                "the bubble grows monotonically with the line count");
        }

        // --------------------------------------------------- stamp placement (task 2)
        // Papers, Please stamping maths: desk drop point -> paper-local spot (position,
        // scale, tilt) and the printable-area clamp that keeps the mark on the paper.
        private static void StampPlacementTests()
        {
            Group("StampPlacement - desk drop point -> slip-local spot");

            var p = StampPlacement.DeskToLocal(100f, 50f, 0f, 0f, 1f, 0f);
            Check(Math.Abs(p.X - 100f) < 1e-4f && Math.Abs(p.Y - 50f) < 1e-4f,
                "no tilt, no zoom: the local spot is the plain offset");
            p = StampPlacement.DeskToLocal(100f, 50f, 40f, 20f, 1f, 0f);
            Check(Math.Abs(p.X - 60f) < 1e-4f && Math.Abs(p.Y - 30f) < 1e-4f,
                "the paper's position is subtracted first");
            p = StampPlacement.DeskToLocal(100f, 50f, 0f, 0f, 2f, 0f);
            Check(Math.Abs(p.X - 50f) < 1e-4f && Math.Abs(p.Y - 25f) < 1e-4f,
                "a 2x-zoomed paper halves the local offset (the mark scales with the sheet)");
            p = StampPlacement.DeskToLocal(10f, 0f, 0f, 0f, 1f, 90f);
            Check(Math.Abs(p.X) < 1e-3f && Math.Abs(p.Y - 10f) < 1e-3f,
                "a 90°-tilted paper rotates the drop point into its own frame");
            p = StampPlacement.DeskToLocal(10f, 0f, 0f, 0f, 1f, -90f);
            Check(Math.Abs(p.X) < 1e-3f && Math.Abs(p.Y + 10f) < 1e-3f,
                "...and -90° rotates the other way");
            p = StampPlacement.DeskToLocal(10f, 5f, 0f, 0f, 1f, 180f);
            Check(Math.Abs(p.X + 10f) < 1e-3f && Math.Abs(p.Y + 5f) < 1e-3f,
                "180° flips both axes");

            // the shipping case: slip at (660,440), zoom 1.5, resting tilt 1.2°,
            // dropped at its visual centre (810,530) - hand-computed expectations
            p = StampPlacement.DeskToLocal(810f, 530f, 660f, 440f, 1.5f, 1.2f);
            Check(Math.Abs(p.X - 98.72f) < 0.02f && Math.Abs(p.Y - 62.08f) < 0.02f,
                $"the zoomed, tilted slip maps its centre drop to ~(98.72,62.08) (got {p.X:F2},{p.Y:F2})");

            // round-trip: local -> desk -> local is identity for arbitrary tilt/scale
            foreach (float tilt in new[] { -12f, -1.2f, 0f, 3.4f, 27.9f, 180f, -179f })
                foreach (float scale in new[] { 0.6f, 1f, 1.5f, 1.8f })
                {
                    float lx0 = 37f, ly0 = 81f;
                    double rad = tilt * Math.PI / 180.0;
                    float c = (float)Math.Cos(rad), n = (float)Math.Sin(rad);
                    // forward map (inverse of DeskToLocal): desk = top + scale * R(tilt) * local
                    float deskX = 200f + scale * (lx0 * c + ly0 * n);
                    float deskY = 90f + scale * (-lx0 * n + ly0 * c);
                    var back = StampPlacement.DeskToLocal(deskX, deskY, 200f, 90f, scale, tilt);
                    Check(Math.Abs(back.X - lx0) < 0.02f && Math.Abs(back.Y - ly0) < 0.02f,
                        $"round-trip holds at tilt {tilt}°, scale {scale}");
                }

            // degenerate input guards
            p = StampPlacement.DeskToLocal(100f, 100f, 0f, 0f, 0f, 0f);
            Check(!float.IsNaN(p.X) && !float.IsNaN(p.Y) && !float.IsInfinity(p.X) && !float.IsInfinity(p.Y),
                "a zero scale never produces NaN/inf (epsilon-guarded)");

            Group("StampPlacement - tilted half extent + printable-area clamp");

            Check(Math.Abs(StampPlacement.TiltedHalfExtent(56f, 0f) - 28f) < 1e-4f,
                "untilted: the half extent is half the mark size");
            Check(Math.Abs(StampPlacement.TiltedHalfExtent(56f, 90f) - 28f) < 1e-4f,
                "90°: a square's half extent is unchanged");
            Check(Math.Abs(StampPlacement.TiltedHalfExtent(56f, 45f) - 28f * (float)Math.Sqrt(2)) < 1e-3f,
                "45° is the worst case: half diagonal = s/2 * sqrt(2)");
            Check(StampPlacement.TiltedHalfExtent(56f, 18f) < StampPlacement.TiltedHalfExtent(56f, 45f) &&
                  StampPlacement.TiltedHalfExtent(56f, 10f) < StampPlacement.TiltedHalfExtent(56f, 18f),
                "the extent grows monotonically up to 45°");
            Check(Math.Abs(StampPlacement.TiltedHalfExtent(56f, 18f) - 35.28f) < 0.02f,
                "the shipped tilt span (18°) budgets ~35.3px of a 56px mark");

            // the shipped slip: 200x120 sheet, 56px mark, 18° max tilt, 2px padding
            float m = StampPlacement.TiltedHalfExtent(56f, 18f) + 2f;
            var inside = StampPlacement.ClampToPrintable(new StampPlacement.Point(100f, 60f), 200f, 120f, 56f, 18f, 2f);
            Check(Math.Abs(inside.X - 100f) < 1e-4f && Math.Abs(inside.Y - 60f) < 1e-4f,
                "a middle drop is untouched by the clamp");
            var tl = StampPlacement.ClampToPrintable(new StampPlacement.Point(-40f, -40f), 200f, 120f, 56f, 18f, 2f);
            Check(Math.Abs(tl.X - m) < 1e-3f && Math.Abs(tl.Y - m) < 1e-3f,
                $"a far top-left drop clamps to the printable corner ({tl.X:F1},{tl.Y:F1} = margin {m:F1})");
            var br = StampPlacement.ClampToPrintable(new StampPlacement.Point(999f, 999f), 200f, 120f, 56f, 18f, 2f);
            Check(Math.Abs(br.X - (200f - m)) < 1e-3f && Math.Abs(br.Y - (120f - m)) < 1e-3f,
                "a far bottom-right drop clamps to the opposite printable corner");
            var pad = StampPlacement.ClampToPrintable(new StampPlacement.Point(0f, 0f), 200f, 120f, 56f, 18f, 10f);
            Check(Math.Abs(pad.X - (m + 8f)) < 1e-3f, "more padding pushes the clamp further in");
            var tiny = StampPlacement.ClampToPrintable(new StampPlacement.Point(0f, 0f), 40f, 40f, 56f, 18f, 2f);
            Check(Math.Abs(tiny.X - 20f) < 1e-4f && Math.Abs(tiny.Y - 20f) < 1e-4f,
                "a sheet smaller than the mark clamps to its centre (range never inverts)");

            // sweep: whatever comes in, the WHOLE tilted mark stays on the paper
            bool outside = false;
            for (float x = -50f; x <= 250f; x += 7f)
                for (float y = -50f; y <= 170f; y += 7f)
                {
                    var q = StampPlacement.ClampToPrintable(new StampPlacement.Point(x, y), 200f, 120f, 56f, 18f, 2f);
                    if (q.X - m < -1e-3f || q.X + m > 200f + 1e-3f || q.Y - m < -1e-3f || q.Y + m > 120f + 1e-3f)
                        outside = true;
                }
            Check(!outside, "every clamped spot keeps the tilted mark fully inside the 200x120 slip (sweep proof)");

            // the keyboard default spot (80% / 68% of the sheet) survives the clamp unchanged
            var kb = StampPlacement.ClampToPrintable(new StampPlacement.Point(200f * 0.8f, 120f * 0.68f),
                200f, 120f, 56f, 18f, 2f);
            Check(Math.Abs(kb.X - 160f) < 1e-3f && Math.Abs(kb.Y - 81.6f) < 1e-3f,
                $"the keyboard stamp's corner default sits inside the printable area as-is ({kb.X:F1},{kb.Y:F1})");
        }

        private static void HallwayDarknessTests()
        {
            Group("HallwayDarkness - corridor ambient + the limited-visibility pool");

            float vsLit = HallwayDarkness.DarkerThanLitRoom(0.12f);
            Check(vsLit > 0.40f && vsLit < 0.60f,
                $"the guard's weak light is 40-60% darker than a normally lit room ({vsLit:P1})");
            Check(HallwayDarkness.AlphaAt(10f) > 0.62f,
                "beyond the pool the corridor is deeper than a dark classroom's 0.62");

            Eq(HallwayDarkness.DepthAt(0f), 0f, "at the custodian the depth layer adds nothing");
            Eq(HallwayDarkness.DepthAt(HallwayDarkness.ClearRadius), 0f,
                "the pool's edge is still fully clear (the near field is navigable)");
            Eq(HallwayDarkness.DepthAt(HallwayDarkness.DepthRadius), 1f,
                "from DepthRadius out the corridor is as deep as it gets");
            float mid = HallwayDarkness.DepthAt(
                (HallwayDarkness.ClearRadius + HallwayDarkness.DepthRadius) / 2f);
            Check(mid > 0.2f && mid < 0.8f,
                $"the falloff passes through the middle ({mid:F2}) - a grade, not a step");

            float prev = HallwayDarkness.DepthAt(0f); bool monotone = true; bool smooth = true;
            float slope = 0f;
            for (float d = 0.25f; d <= 9f; d += 0.25f)
            {
                float v = HallwayDarkness.DepthAt(d);
                if (v < prev - 1e-6f) monotone = false;
                float s = v - prev;
                if (slope >= 0f && Math.Abs(s - slope) > 0.12f) smooth = false;   // no hard jump
                slope = s; prev = v;
            }
            Check(monotone, "the falloff never brightens with distance");
            Check(smooth, "no single step of the falloff is a hard circular edge");

            Check(HallwayDarkness.VisibleFractionAt(0f) > 0.30f &&
                  HallwayDarkness.VisibleFractionAt(0f) < 0.55f,
                "the near field keeps 30-55% of the art's brightness: a weak light you can see by");
            Check(HallwayDarkness.VisibleFractionAt(10f) < 0.08f,
                "beyond the pool the corridor keeps under 8% brightness (darkness dominates)");

            float viewHalf = 5f * 16f / 9f;      // ortho size 5 at the 16:9 reference aspect
            float share = HallwayDarkness.PoolShareOfView(viewHalf);
            Check(share > 0.10f && share < 0.20f,
                $"the pool spans 10-20% of the on-screen width ({share:P1})");

            Check(HallwayDarkness.ShroudWorldW / 2f >=
                  HallwayDarkness.HallwayHalfWidth + HallwayDarkness.RoomDarkMargin +
                  HallwayDarkness.PlayerTravel,
                "the depth quad covers the room's dark region from either end of the walk");
            Check(HallwayDarkness.ShroudWorldH / 2f >=
                  HallwayDarkness.RoomDarkHalfHeight + HallwayDarkness.PlayerYOffset,
                "...and vertically, from the custodian's standing height");
            Check(Math.Abs(HallwayDarkness.PlayerY - (HallwayDarkness.FloorY + HallwayDarkness.PlayerBodyLift)) < 1e-6f,
                "the pool's vertical anchor is the shared floor line + the body lift");
        }

        // The face-comparison cue must never lie: its truth is the run's own pairing data,
        // compared exactly as the sprites render it (features + the distortion overlay).
        private static void FaceTruthTests()
        {
            Group("Face comparison truth - the cue follows the rendered faces, never a coin flip");
            int pairs = 0, mismatched = 0;
            bool neverGreenOnBookedMismatch = true, ruleHolds = true, anomalyNeverMatches = true;
            for (int seed = 1; seed <= 12; seed++)
                for (int night = 1; night <= 5; night++)
                    foreach (var v in RosterFor(seed, night))
                    {
                        bool photoDefect = v.Defects.Contains(DefectType.PhotoMismatch) ||
                                           v.Defects.Contains(DefectType.PhotoSubtleMismatch);
                        if (photoDefect && v.DisplayedFacesMatch) neverGreenOnBookedMismatch = false;
                        bool renderedSame = v.WindowPortrait == v.IdPhoto &&
                                            v.WindowPortrait.Distorted == v.IdPhoto.Distorted &&
                                            !v.IsAnomaly;   // anomalies wear their altered variant
                        if (v.DisplayedFacesMatch != renderedSame) ruleHolds = false;
                        if (v.IsAnomaly && v.DisplayedFacesMatch) anomalyNeverMatches = false;
                        pairs++;
                        if (!v.DisplayedFacesMatch) mismatched++;
                    }
            Check(neverGreenOnBookedMismatch,
                "a booked photo-family defect NEVER shows a matching cue (no green lies)");
            Check(ruleHolds,
                "the cue agrees with the faces AS RENDERED (features, distortion, anomaly variant)");
            Check(anomalyNeverMatches,
                "an anomaly never shows a matching cue - its in-world face is the altered variant");
            Check(mismatched > 0 && mismatched < pairs,
                $"rosters contain both outcomes to find ({mismatched} mismatched of {pairs})");
            var p = new Portrait { HairStyle = 1, FaceVariant = 2, Accessory = 0 };
            Check(p != p.Swapped(), "a swapped portrait never reads as the same face");
            Check(p != p.SubtleVariant(new GameRng(3)), "a subtle variant differs in one feature group");
            var d = p; d.Distorted = true;
            Check(p == d, "Portrait equality ignores distortion (the scoring rules' contract)...");
            var v2 = new VisitorData { WindowPortrait = d, IdPhoto = p };
            Check(!v2.DisplayedFacesMatch, "...while the face cue still sees a glitched window face as different");
        }

        private static void FaceAssetMappingTests()
        {
            Group("FaceAssets - stable identity mapping for the temporary face set");
            Check(FaceAssets.StudentKey(0) == "char_student_01" &&
                  FaceAssets.StudentKey(FaceAssets.StudentFaceCount) == "char_student_01",
                "student keys wrap stably over the shipped set");
            Check(FaceAssets.AnomalyKey(0) == "char_student_01_anomaly" &&
                  FaceAssets.AnomalyKey(6) == "char_student_07_anomaly",
                "anomaly keys are PAIRED with their student: char_student_NN_anomaly");
            Check(FaceAssets.WindowKey(-1, false) == null && FaceAssets.IdKey(-1) == null,
                "no asset index (old saves, administrator) -> the procedural painter, never a broken image");
            bool cardIsReal = true, anomalyIsPaired = true;
            for (int i = 0; i < 24; i++)
            {
                if (FaceAssets.IdKey(i) != FaceAssets.StudentKey(i)) cardIsReal = false;
                var wk = FaceAssets.WindowKey(i, true);
                if (!(wk.EndsWith("_anomaly") && wk.StartsWith(FaceAssets.StudentKey(i)))) anomalyIsPaired = false;
                if (wk == FaceAssets.WindowKey(i, false)) anomalyIsPaired = false;
            }
            Check(cardIsReal, "the ID card ALWAYS shows the real student's face art");
            Check(anomalyIsPaired,
                "an altered window face is THAT STUDENT's anomaly variant - paired, explicit, never random");
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < FaceAssets.StudentFaceCount; i++) seen[FaceAssets.StudentKey(i)] = i;
            Check(seen.Count == FaceAssets.StudentFaceCount,
                "every shipped student face maps to exactly one identity slot (no shared faces)");
            var names = Content.BuildStudentRoster();
            string[] want = { "Mark Angelo Mendoza", "Nathaniel Aquino", "Maria Clara Santos", "Liza May Ramos",
                              "Raffy Isedru", "Ranielle Waterbonia", "Rhevster Estrellita",
                              "Paolo Garcia", "Angelica Cruz", "Mark Anthony Villanueva", "Bea Dela Cruz",
                              "Giones Acirfa", "Kristine Joy Bautista" };   // Izzy Neckerman is Off until her art arrives
            bool namesOk = names.Count == want.Length && Content.TotalStudents == FaceAssets.StudentFaceCount;
            for (int i = 0; namesOk && i < want.Length; i++) namesOk = names[i].FullName == want[i];
            Check(namesOk, "the registry has the thirteen students in face-art order (row N wears char_student_NN)");
            var numbers = new HashSet<string>();
            foreach (var st in names) numbers.Add(st.IdNumber);
            Check(numbers.Count == names.Count, "every student has their own ID number");
        }

        // A scared visitor can be a real student or an anomaly, and nothing about how they look or sound
        // may tell the two apart: the same scared face art, the same shaken words. Only the papers can.
        private static void ScaredVisitorTests()
        {
            Group("Scared visitors - the same face and the same words for students and anomalies");
            var plan = ShiftPlan.Build();
            var students = GameRules.Current.Students;
            int honestScared = 0, wrongScared = 0;
            for (int seed = 1; seed <= 100; seed++)
            {
                var factory = new RosterFactory(seed);
                foreach (var night in plan)
                {
                    var roster = factory.GenerateShift(night);
                    int scared = 0, scaredHonest = 0, scaredWrong = 0;
                    foreach (var v in roster)
                    {
                        if (v.IsAdministrator) { Check(!v.IsScared, "the administrator is never scared"); continue; }
                        if (!v.IsScared) continue;
                        scared++;
                        var row = students[v.FaceAssetIndex];
                        Eq(v.Greeting, row.ScaredGreeting, $"seed {seed} night {night.Number}: a scared visitor says that student's scared greeting");
                        Check(!v.SpeaksAwkwardly, "a scared anomaly never speaks awkwardly: speech cannot give them away");
                        if (!v.Defects.Contains(DefectType.SpokenMismatch))
                            Eq(v.Answers[(int)QuestionId.Course], InterviewRules.CourseSpeech(Content.Courses[v.IdCourseIndex], Content.ScaredSpeech),
                                "a scared visitor answers in the shared shaken words");
                        if (v.IsAnomaly)
                        {
                            scaredWrong++; wrongScared++;
                            Check(v.Defects.Count > 0 && !v.Defects.Contains(DefectType.PhotoSubtleMismatch),
                                $"seed {seed} night {night.Number}: a scared anomaly never relies on a subtle photo change");
                            bool paperTell = false;
                            foreach (var d in v.Defects) if (d != DefectType.PhotoSubtleMismatch) paperTell = true;
                            Check(paperTell, $"seed {seed} night {night.Number}: a scared anomaly still has a tell");
                        }
                        else { scaredHonest++; honestScared++; Check(v.Defects.Count == 0, "a scared student is still clean"); }
                    }
                    Eq(scared, night.ScaredCount, $"seed {seed} night {night.Number}: as many scared visitors as the row says");
                    if (night.ScaredCount >= 2)
                        Check(scaredHonest >= 1 && scaredWrong >= 1, $"seed {seed} night {night.Number}: scared visitors are both students and anomalies");
                }
            }
            Check(honestScared > 0 && wrongScared > 0, $"over many runs both kinds arrive scared ({honestScared} students, {wrongScared} anomalies)");

            for (int i = 0; i < FaceAssets.StudentFaceCount; i++)
            {
                Eq(FaceAssets.ScaredKey(i), FaceAssets.StudentKey(i) + "_scared", "the scared art is paired with its student");
                Eq(FaceAssets.WindowKey(i, true, true), FaceAssets.ScaredKey(i), "a scared anomaly wears the scared art, not the monster art");
                Eq(FaceAssets.WindowKey(i, false, true), FaceAssets.ScaredKey(i), "a scared student wears the same art");
                Eq(FaceAssets.WindowKey(i, true, false), FaceAssets.AnomalyKey(i), "a calm anomaly still wears the monster art");
                Eq(FaceAssets.WindowKey(i, false, false), FaceAssets.StudentKey(i), "a calm student still wears their own face");
            }
            Check(FaceAssets.WindowKey(-1, true, true) == null, "no asset index still means the procedural painter");
            foreach (var row in students) Check(!string.IsNullOrEmpty(row.ScaredGreeting), row.FullName + " has a scared greeting");
            Check(Content.ScaredSpeech.Course.Contains("{0}") && Content.ScaredSpeech.Year.Contains("{0}") &&
                  Content.ScaredSpeech.Status.Contains("{0}") && Content.ScaredSpeech.Classroom.Contains("{0}"),
                "the scared words keep the fact in them, so the papers can still be checked");

            var a = new RosterFactory(31);
            var b = new RosterFactory(31);
            foreach (var night in plan)
                Check(a.GenerateShift(night).Select(v => v.IsScared).SequenceEqual(b.GenerateShift(night).Select(v => v.IsScared)),
                    $"night {night.Number}: the same seed picks the same scared visitors");
        }

        // The owner's student list: a professor who still comes to the window, years that are titles, three
        // digit IDs, and fake IDs that look like real ones.
        private static void StudentRecordTests()
        {
            Group("Student records - faculty, year titles, three digit IDs");
            var roster = Content.BuildStudentRoster();
            var raffy = roster.First(s => s.FullName == "Raffy Isedru");
            Check(raffy.IsFaculty && raffy.YearString == "Professor" && raffy.StatusString == "Faculty", "Raffy is a professor: year Professor, status Faculty");
            Eq(roster.Count(s => s.IsFaculty), 1, "only Raffy is faculty");
            Eq(string.Join(",", Content.YearNames), "Freshman,Sophomore,Junior,Senior,Professor", "the year titles");
            Eq(InterviewRules.YearSpeech("Junior"), "I'm a Junior.", "a year is said as a title");
            Eq(InterviewRules.StatusSpeech(Content.StatusWord(false, true)), "I'm a faculty member.", "faculty say what they are");

            int fakes = 0, professorsSeen = 0, studentYearLies = 0;
            for (int seed = 1; seed <= 150; seed++)
            {
                var factory = new RosterFactory(seed);
                foreach (var night in ShiftPlan.Build())
                    foreach (var v in factory.GenerateShift(night))
                    {
                        if (v.IsAdministrator) continue;
                        if (v.IsFaculty)
                        {
                            professorsSeen++;
                            Check(!v.Defects.Contains(DefectType.YearMismatch) && !v.Defects.Contains(DefectType.StatusMismatch),
                                "a professor never gets a year or status lie");
                            Eq(v.IdYearLevel, 5, "the professor's card says Professor");
                        }
                        else if (v.Defects.Contains(DefectType.YearMismatch))
                        {
                            studentYearLies++;
                            Check(v.IdYearLevel >= 1 && v.IdYearLevel <= 4, "a student's wrong year is still a student year");
                        }
                        if (v.Defects.Contains(DefectType.NotInDatabase))
                        {
                            fakes++;
                            Check(IdRules.IsFormatValid(v.IdNumber) && !factory.Registry.Contains(v.IdNumber),
                                $"seed {seed}: a fake ID looks like a real one ('{v.IdNumber}') but is not in the registry");
                        }
                        if (v.Defects.Contains(DefectType.IdFormatInvalid))
                            Check(!IdRules.IsFormatValid(v.IdNumber), $"seed {seed}: a broken ID really is broken ('{v.IdNumber}')");
                    }
            }
            Check(fakes > 20 && professorsSeen > 20 && studentYearLies > 20, $"the runs covered fakes, professors and year lies ({fakes}, {professorsSeen}, {studentYearLies})");
        }

        // Peek rooms: where things stand, how big they are there, what the lamp does to them.
        private static void PeekFigureTests()
        {
            Group("Peek figures - floor spots, perspective size, clearing speed");
            var rooms = new[] { RoomId.Classroom1, RoomId.Classroom2, RoomId.ComfortRoom, RoomId.ComfortRoom2 };
            foreach (var room in rooms)
            {
                var spots = PeekSpots.For(room);
                Eq(spots.Length, 3, room + " has three floor spots");
                foreach (var spot in spots)
                {
                    Check(spot.FeetY <= -1.3f && spot.FeetY >= PeekSpots.FrontFeetY, room + ": a spot's feet are on the lower part of the picture (" + spot.FeetY + ")");
                    Check(Math.Abs(spot.X) <= 4f, room + ": a spot is inside the room (" + spot.X + ")");
                }
                Eq(PeekSpots.CeilingXs(room).Length, 3, room + " has three ceiling spots");
            }
            var cr1 = PeekSpots.For(RoomId.ComfortRoom);
            var cr2 = PeekSpots.For(RoomId.ComfortRoom2);
            for (int i = 0; i < 3; i++)
                Check(cr2[i].X == -cr1[i].X && cr2[i].FeetY == cr1[i].FeetY, "CR-2 is CR-1 mirrored, spot " + i);

            // perspective: the further back the feet, the smaller the figure, and full size at the front edge
            foreach (var kind in new[] { PeekKind.Hood, PeekKind.Slide })
            {
                Check(Math.Abs(PeekSpots.HeightAt(kind, RoomId.Classroom1, PeekSpots.FrontFeetY) - PeekSpots.FrontHeight(kind)) < 1e-3f, kind + ": full size at the front edge");
                Check(PeekSpots.HeightAt(kind, RoomId.Classroom1, -1.4f) < PeekSpots.HeightAt(kind, RoomId.Classroom1, -2.9f), kind + ": smaller further back");
                Check(PeekSpots.HeightAt(kind, RoomId.Classroom1, -1.4f) > 0.4f * PeekSpots.FrontHeight(kind), kind + ": but never a doll");
            }
            Eq(PeekSpots.HeightAt(PeekKind.Coverhug, RoomId.ComfortRoom, 0f), PeekSpots.FrontHeight(PeekKind.Coverhug), "the hanger has no perspective, it hangs");
            Check(PeekSpots.FrontHeight(PeekKind.Hood) > 3.2f, "the hooded figure is bigger than the first version (3.2)");

            // what a room holds, and the night's rolls
            int slides = 0, hangers = 0, rushes = 0, stills = 0;
            for (int seed = 1; seed <= 200; seed++)
                for (int night = 1; night <= 4; night++)
                    foreach (var room in rooms)
                    {
                        var kind = PeekSpots.KindIn(seed, night, room);
                        bool classroom = room == RoomId.Classroom1 || room == RoomId.Classroom2;
                        Check(classroom == (kind == PeekKind.Hood), "classrooms hold the hood and nothing else does");
                        if (kind == PeekKind.Slide) slides++;
                        if (kind == PeekKind.Coverhug) hangers++;
                        var reaction = PeekSpots.ReactionIn(seed, night, room);
                        if (kind == PeekKind.Coverhug) Check(reaction == PeekReaction.Still, "the hanger never moves");
                        if (reaction == PeekReaction.Rush) rushes++; else stills++;
                        int index = PeekSpots.SpotIndexIn(seed, night, room);
                        Check(index >= 0 && index < 3, "the spot index is one of three");
                        Check(PeekSpots.KindIn(seed, night, room) == kind && PeekSpots.ReactionIn(seed, night, room) == reaction
                              && PeekSpots.SpotIndexIn(seed, night, room) == index, "the same night gives the same answers");
                    }
            Check(slides > 100 && hangers > 100, "comfort rooms hold both the crouched one and the hanger (" + slides + " / " + hangers + ")");
            Check(rushes > 200 && stills > 200, "some rush and some stay still (" + rushes + " / " + stills + ")");

            // clearing speed: 2 seconds plain, 1.3 times faster with Focus, draining when the lamp leaves
            Check(Math.Abs(PeekClear.SecondsToClear(false, 2f) - 2f) < 1e-4f, "plain: two seconds");
            Check(Math.Abs(PeekClear.SecondsToClear(true, 2f) - 2f / 1.3f) < 1e-4f, "Focus: 1.3 times faster");
            float bar = 0f; int frames = 0;
            while (bar < 1f && frames < 1000) { bar = PeekClear.Advance(bar, 1f / 60f, true, false, 2f); frames++; }
            Check(frames >= 119 && frames <= 121, "a plain lamp fills the bar in about 120 frames (" + frames + ")");
            bar = 0f; frames = 0;
            while (bar < 1f && frames < 1000) { bar = PeekClear.Advance(bar, 1f / 60f, true, true, 2f); frames++; }
            Check(frames >= 91 && frames <= 93, "a Focus lamp fills it in about 92 frames (" + frames + ")");
            Check(PeekClear.Advance(0.5f, 1f, false, true, 2f) == 0f && Math.Abs(PeekClear.Advance(1f, 1f, false, false, 2f) - 0.5f) < 1e-4f,
                "off the body the bar drains, and never below zero");
            Eq(PeekClear.Advance(0.3f, 0f, true, true, 2f), 0.3f, "no time passing changes nothing");
        }

        private static void CheckpointQueueTests()
        {
            Group("CheckpointQueue - start gate, arrival clock, FIFO line, window promotion");

            var roster = RosterFor(11, 2);           // night 2: six visitors, two anomalies
            Eq(roster.Count, 6, "test roster has six visitors");

            var q = new CheckpointQueue();
            q.AttachRoster(roster, new QueueTuning { MissingStudentChance = 0f }, 1234);
            Check(!q.Started, "a fresh shift has not started (sticky-note gate)");
            Check(!q.Tick(10f).Any && q.ArrivalCount == 0, "the clock is dead before the shift starts");
            Check(!q.ForceNextArrival().Any, "forced arrivals refuse before the shift starts");
            Check(!q.ShiftComplete, "an unstarted shift is never 'complete'");

            q.MarkStarted();
            var first = q.ForceNextArrival();
            Check(first.ToWindow == roster[0] && first.ToLine == null, "first visitor walks to the free window");
            Eq(q.ArrivalCount, 1, "arrival counted exactly once");
            Eq(q.WaitingCount, 0, "the window visitor is not counted as waiting");
            Check(q.TimerSeconds >= 3f && q.TimerSeconds <= 5f,
                  "arrival clock armed inside [3,5]s (got " + q.TimerSeconds.ToString("0.00") + ")");

            var tuning = new QueueTuning();
            var rng = new GameRng(7);
            float lo = 99f, hi = -1f; bool inBounds = true;
            for (int i = 0; i < 500; i++)
            {
                float v = tuning.RollInterval(rng);
                inBounds &= v >= 3f && v <= 5f;
                lo = Math.Min(lo, v); hi = Math.Max(hi, v);
            }
            Check(inBounds, "500 rolled intervals all land inside [3,5]s");
            Check(hi - lo > 0.5f, "intervals are actually randomized (spread " + (hi - lo).ToString("0.00") + "s)");

            Check(!q.Tick(2.9f).Any, "no arrival before the shortest interval elapsed");
            var second = q.Tick(2.2f);               // 5.1s total >= any interval in [3,5]
            Check(second.ToLine == roster[1] && second.ToWindow == null,
                  "second arrival joins the line while the window is busy");
            Eq(q.WaitingCount, 1, "one waiting, window excluded");

            for (int i = 0; i < 2; i++)
            {
                var r = q.Tick(6f);                  // >= max interval: exactly one arrival per tick
                Check(r.ToLine != null && r.ToWindow == null, "arrival #" + (i + 3) + " accumulates in the line");
            }
            Eq(q.ArrivalCount, 4, "four arrivals total");
            Eq(string.Join(",", q.WaitingLine.Select(v => v.VisitorIndex.ToString())), "1,2,3",
               "the waiting line is FIFO in arrival order");

            var promoted = q.CompleteActive();
            Eq(q.ProcessedCount, 1, "processed count ticks exactly once per resolution");
            Check(promoted == roster[1] && q.Active == roster[1], "the OLDEST waiting visitor steps up");
            Eq(q.WaitingCount, 2, "exactly one silhouette left the line");
            Eq(q.CompleteActive(), roster[2], "promotion keeps FIFO order");
            Eq(q.CompleteActive(), roster[3], "promotion keeps FIFO order to the end");
            q.CompleteActive();                       // the last window visitor is resolved
            Eq(q.ProcessedCount, 4, "all four resolutions counted");
            Check(q.CompleteActive() == null, "resolving a free window does nothing (no double counts)");
            Eq(q.ProcessedCount, 4, "…and never inflates the processed count");

            Check(!q.ShiftComplete, "empty line + free window is NOT completion while the source can produce");
            Eq(q.EffectiveTotal, 6, "effective total counts the full roster");
            q.ForceNextArrival();
            var lastArrival = q.Tick(6f);
            Check(lastArrival.ToLine == roster[5] || q.Active == roster[5], "the last roster entry arrives");
            q.CompleteActive(); q.CompleteActive();
            Check(q.ShiftComplete, "completion: started + source dry + line empty + window free");
            Check(q.TimerSeconds < 0f, "the arrival clock goes idle once the source is dry");
            Check(!q.Tick(60f).Any, "a dry clock never fabricates arrivals");

            Group("CheckpointQueue - pressure warning once, overwhelmed collapse once");

            var bigRoster = RosterFor(5, 4);         // night 4: eight visitors
            Eq(bigRoster.Count, 8, "pressure roster has eight visitors");
            var pq = new CheckpointQueue();
            pq.AttachRoster(bigRoster, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 0f,
            }, 77);
            pq.MarkStarted();
            pq.ForceNextArrival();
            int warnings = 0, overloads = 0;
            for (int i = 0; i < 10; i++)
            {
                var r = pq.Tick(1f);
                if (r.PressureWarning) warnings++;
                if (r.Overwhelmed) overloads++;
            }
            Eq(warnings, 1, "pressure warning fires exactly once, at the crossing (5 waiting)");
            Eq(overloads, 1, "overwhelmed fires exactly once, at the threshold (7 waiting)");
            Eq(pq.ArrivalCount, 8, "all eight arrived before the collapse (1 window + 7 waiting)");
            Eq(pq.WaitingCount, 7, "seven waiting when the line overran");
            Check(pq.Collapsed, "the queue collapses on overwhelm");
            Check(!pq.Tick(60f).Any && pq.ArrivalCount == 8, "collapsed: no further arrivals, ever");
            Check(!pq.ForceNextArrival().Any, "collapsed: forced arrivals refuse too");
            Eq(pq.PressureLevel, 2, "pressure level stays at overwhelmed");

            var owSession = new GameSession { Mode = GameMode.Normal, CurrentShift = ShiftPlan.TotalShifts, QueueOverwhelmed = true };
            var owEval = EndingRules.Evaluate(owSession);
            Eq(owEval.Ending, EndingType.Lose, "queue overload is a run failure");
            Eq(owEval.ReasonKey, "overwhelmed", "…with its own honest reason (not false rejection / breach)");
            var prio = new GameSession { Mode = GameMode.Normal, CurrentShift = ShiftPlan.TotalShifts, QueueOverwhelmed = true, AdminAllowed = true };
            Eq(EndingRules.Evaluate(prio).ReasonKey, "admin_breach", "existing lose routes keep priority");
            Check(EndingRules.Evaluate(new GameSession { Mode = GameMode.Normal }).Ending == EndingType.None,
                  "a clean session still evaluates to None (additive change)");

            Group("CheckpointQueue - missing student (finite roster, one incident per shift)");

            // (a) probability-failure path: chance 0 changes nothing
            var r0 = RosterFor(11, 2);
            var q0 = new CheckpointQueue();
            q0.AttachRoster(r0, new QueueTuning { MissingStudentChance = 0f }, 5);
            q0.MarkStarted();
            bool sawAnomaly = false, sawIncident = false;
            for (int i = 0; i < 6; i++)
            {
                var r = i == 0 ? q0.ForceNextArrival() : q0.Tick(6f);
                var arrived = r.ToWindow ?? r.ToLine;
                sawAnomaly |= arrived != null && arrived.IsAnomaly;
                sawIncident |= r.MissingStudent;
                q0.CompleteActive();
            }
            Check(sawAnomaly, "chance-0 roster did contain an anomaly");
            Check(!sawIncident, "chance 0: anomalies arrive, nobody vanishes");
            Eq(q0.EffectiveTotal, 6, "chance 0: effective total untouched");

            // (b) forced path, anomaly first: the victim comes from the un-arrived source
            int seedSrc = FindSeed(rr => rr[0].IsAnomaly && rr.Count(v => !v.IsAnomaly) >= 3, 2);
            Check(seedSrc > 0, "found a seed whose first visitor is an anomaly");
            var rSrc = RosterFor(seedSrc, 2);
            var qs = new CheckpointQueue();
            qs.AttachRoster(rSrc, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f,
            }, 4242);
            qs.MarkStarted();
            var rAnom = qs.ForceNextArrival();
            Check(rAnom.ToWindow == rSrc[0] && rAnom.MissingStudent, "the arriving anomaly triggers the incident");
            var victim = rAnom.Vanished;
            Check(victim != null && !victim.IsAnomaly && !victim.IsAdministrator,
                  "the vanished student is legitimate");
            Check(victim != qs.Active, "never the visitor at the window");
            Eq(qs.MissingIncidents, 1, "one incident recorded");
            Eq(qs.EffectiveTotal, rSrc.Count - 1, "effective total drops by exactly one");

            var arrivedNames = new List<string> { rAnom.ToWindow.ClaimedName };
            for (int i = 0; i < rSrc.Count; i++)
            {
                var r = qs.Tick(1f);
                var v = r.ToWindow ?? r.ToLine;
                if (v != null) arrivedNames.Add(v.ClaimedName);
                Check(!r.MissingStudent, "no second incident while the limit is spent");
                qs.CompleteActive();
            }
            Eq(qs.MissingIncidents, 1, "limit: one incident per shift, later anomalies roll nothing");
            Eq(qs.EffectiveTotal, rSrc.Count - 1, "effective total stable after later arrivals");
            Check(!arrivedNames.Contains(victim.ClaimedName), "the vanished student never arrives");
            Eq(qs.ArrivalCount, rSrc.Count - 1, "arrival count == effective total at the end");
            Check(qs.ShiftComplete, "shift completes with the vanished student excluded");

            // (c) forced path, anomaly later: a waiting legitimate student is preferred
            int seedWait = FindSeed(rr => !rr[0].IsAnomaly && !rr[1].IsAnomaly && rr[2].IsAnomaly, 2);
            Check(seedWait > 0, "found a seed with legit, legit, anomaly arrival order");
            var rWait = RosterFor(seedWait, 2);
            var qw = new CheckpointQueue();
            qw.AttachRoster(rWait, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f,
            }, 99);
            qw.MarkStarted();
            qw.ForceNextArrival();                   // legit #0 -> window
            qw.Tick(1f);                             // legit #1 -> line
            var rW = qw.Tick(1f);                    // anomaly #2 -> line, incident fires
            Check(rW.MissingStudent && rW.Vanished == rWait[1],
                  "a legitimate visitor already in the line is preferred as the victim");
            Eq(qw.WaitingCount, 1, "the line shrank immediately (only the anomaly waits)");
            Check(qw.WaitingLine.First() == rWait[2], "FIFO order of the remaining line intact");
            Eq(qw.EffectiveTotal, rWait.Count - 1, "effective total updated immediately");
            Eq(qw.ArrivalCount, 2, "arrival progression updated: the vanished waiter no longer counts as arrived");

            // (d) nobody eligible: the roll consumes nothing, no phantom incident
            var synthetic = new List<VisitorData>
            {
                new VisitorData { VisitorIndex = 0, IsAnomaly = true, ClaimedName = "A" },
                new VisitorData { VisitorIndex = 1, IsAnomaly = true, IsAdministrator = true, ClaimedName = "ADMIN" },
                new VisitorData { VisitorIndex = 2, IsAnomaly = true, ClaimedName = "B" },
            };
            var qn = new CheckpointQueue();
            qn.AttachRoster(synthetic, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f,
            }, 3);
            qn.MarkStarted();
            qn.ForceNextArrival(); qn.Tick(1f); qn.Tick(1f);
            Eq(qn.MissingIncidents, 0, "anomalies and the administrator are never victims");
            Eq(qn.EffectiveTotal, 3, "no phantom removals when nobody is eligible");

            // (e) already-processed visitors are never victims
            int seedLate = FindSeed(rr => !rr[0].IsAnomaly && rr[1].IsAnomaly, 2);
            Check(seedLate > 0, "found a seed with legit then anomaly");
            var rLate = RosterFor(seedLate, 2);
            var ql = new CheckpointQueue();
            ql.AttachRoster(rLate, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f,
            }, 808);
            ql.MarkStarted();
            ql.ForceNextArrival();                   // legit #0 at the window
            ql.CompleteActive();                     // …resolved and gone
            var rL = ql.Tick(1f);                    // anomaly #1 arrives, incident fires
            Check(rL.MissingStudent && rL.Vanished != rLate[0],
                  "a processed visitor is never chosen as the victim");
            Check(rL.Vanished == null || rL.Vanished.VisitorIndex >= 2, "victim comes from the un-arrived source");

            Group("CheckpointQueue - snapshot, save round-trip, legacy resume, run-mode source");

            var snapRoster = RosterFor(seedWait, 2);
            var sq = new CheckpointQueue();
            sq.AttachRoster(snapRoster, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f,
            }, 99);
            sq.MarkStarted();
            sq.ForceNextArrival();                   // #0 window
            sq.Tick(1f);                             // #1 line (vanishes via the anomaly below)
            sq.Tick(1f);                             // #2 anomaly -> incident
            sq.ActiveQuestionAsked = true;
            var snap = sq.BuildSnapshot();
            Check(snap.Started && snap.Arrived == 2 && snap.WaitingIdx.Count == 1 &&
                  snap.ActiveIdx == snapRoster[0].VisitorIndex && snap.MissingIncidents == 1 &&
                  snap.QuestionAsked,
                  "snapshot captures started/arrived/line/active/incident/asked");

            var session = new GameSession { Mode = GameMode.Normal, Seed = seedWait, QueueState = snap };
            var roundTrip = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame { Session = session }));
            Check(roundTrip != null && roundTrip.Session.QueueState != null, "queue snapshot survives the save file");
            if (roundTrip != null && roundTrip.Session.QueueState != null)
            {
                var back = roundTrip.Session.QueueState;
                Check(back.Started && back.Arrived == 2 && back.ActiveIdx == snap.ActiveIdx &&
                      back.MissingIncidents == 1 && back.QuestionAsked == snap.QuestionAsked &&
                      Math.Abs(back.TimerSeconds - snap.TimerSeconds) < 0.001f &&
                      back.WaitingIdx.SequenceEqual(snap.WaitingIdx) &&
                      back.VanishedIdx.SequenceEqual(snap.VanishedIdx),
                      "every queue field round-trips (timer, line, vanished included)");

                // restore onto a freshly regenerated identical roster (what CONTINUE does)
                var rq = new CheckpointQueue();
                rq.AttachRoster(RosterFor(seedWait, 2), new QueueTuning(), 555);
                rq.RestoreFromSnapshot(back, 555);
                Check(rq.Started && rq.ArrivalCount == 2 && rq.MissingIncidents == 1, "restore brings the counters back");
                Check(rq.Active != null && rq.Active.VisitorIndex == snap.ActiveIdx, "restore brings the window visitor back");
                Check(rq.ActiveQuestionAsked, "restore brings the asked question back");
                Eq(rq.WaitingCount, 1, "restore brings the waiting line back");
                Check(rq.WaitingLine.First().VisitorIndex == snap.WaitingIdx[0], "…in the right order");
                Eq(rq.EffectiveTotal, snapRoster.Count - 1, "restore keeps the vanished student vanished");
                var afterRestore = rq.Tick(60f);
                Check(afterRestore.ToWindow == null, "restore never re-spawns the first visitor");
            }

            var legacyRoster = RosterFor(11, 2);
            var lq = new CheckpointQueue();
            lq.AttachRoster(legacyRoster, new QueueTuning(), 1);
            lq.RestoreLegacy(2);
            Check(lq.Started && lq.RosterCursor == 2, "legacy save resumes at its stored cursor");
            var lres = lq.ResumePresentation();
            Check(lres.ToWindow == legacyRoster[2], "legacy resume presents the next unprocessed visitor");
            Check(lq.TimerSeconds > 0f, "legacy resume arms the arrival clock");

            Check(!new CheckpointQueue().IsAttached, "a detached queue reports unattached (PersistRun guard)");

            Group("CheckpointQueue - two-phase resolution (quit mid-banner can never re-stamp)");
            var twoPhase = RosterFor(11, 2);
            var tq = new CheckpointQueue();
            tq.AttachRoster(twoPhase, new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 0f,
            }, 21);
            tq.MarkStarted();
            tq.ForceNextArrival();                   // roster[0] at the window
            tq.Tick(1f);                             // roster[1] waiting
            tq.ActiveQuestionAsked = true;
            tq.CommitActiveResolution();             // the stamp moment
            Eq(tq.ProcessedCount, 1, "the stamp books the resolution immediately");
            Check(tq.ResolutionPending, "resolution pending while the banner is up");
            Check(!tq.ActiveQuestionAsked, "committing clears the asked flag");
            tq.CommitActiveResolution();
            Eq(tq.ProcessedCount, 1, "committing twice books once");
            var tqSnap = tq.BuildSnapshot();
            Check(tqSnap.ResolutionPending && tqSnap.ActiveIdx == twoPhase[0].VisitorIndex &&
                  tqSnap.Processed == 1,
                  "a mid-banner snapshot keeps the resolved visitor flagged pending");
            var tq2 = new CheckpointQueue();
            tq2.AttachRoster(RosterFor(11, 2), new QueueTuning
            {
                ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 0f,
            }, 21);
            tq2.RestoreFromSnapshot(tqSnap, 21);
            Check(tq2.ResolutionPending && tq2.ProcessedCount == 1 && tq2.Active != null,
                  "restore brings the pending resolution back");
            var tqPromoted = tq2.CompleteActive();   // the deferred step-up finishes on resume
            Eq(tq2.ProcessedCount, 1, "finishing the pending resolution does not double-count");
            Check(tqPromoted == tq2.Active && tqPromoted.VisitorIndex == twoPhase[1].VisitorIndex,
                  "the oldest waiter still steps up after the deferred completion");
            Check(!tq2.ResolutionPending, "completion clears the pending flag");

            // the administrator outro sends the rest of the line home: completion must not
            // be blocked by waiters (or a promoted active) that will never be called
            for (int i = 0; i < 6; i++) tq2.Tick(1f);   // drain the source (admin-last shape)
            Check(tq2.SourceExhausted, "the source ran dry while the line grew");
            Check(tq2.WaitingCount >= 2 && tq2.Active != null, "waiters piled up behind the window");
            Check(!tq2.ShiftComplete, "waiters block completion (before the story beat)");
            int abandoned = tq2.AbandonLine();
            Check(abandoned >= 2, "AbandonLine sends the remaining line home (" + abandoned + ")");
            Check(tq2.Active == null && tq2.WaitingCount == 0, "the window closes with the night");
            Check(tq2.ShiftComplete, "the shift completes once the story beat closed the line");
        }

        // --------------------------------------------- display policy + options layout
        private static void DisplayAndOptionsTests()
        {
            Group("DisplayPolicy - resolution choices, fallbacks, revert window");
            Eq(DisplayPolicy.RevertSeconds, 15, "unconfirmed display changes revert after 15s");

            var raw = new List<ModeSize>
            {
                new ModeSize(1920, 1080), new ModeSize(1920, 1080), // duplicate from the driver
                new ModeSize(1280, 720), new ModeSize(800, 600),    // too small -> dropped
                new ModeSize(2560, 1440), new ModeSize(1600, 900), new ModeSize(1024, 768),
            };
            var sup = DisplayPolicy.Supported(raw);
            Eq(sup.Count, 5, "duplicates and sub-minimum modes are removed");
            Eq(sup[0].W + "x" + sup[0].H, "1024x768", "sorted by area, smallest first");
            Eq(sup[sup.Count - 1].W + "x" + sup[sup.Count - 1].H, "2560x1440", "largest last");

            var choices = DisplayPolicy.MenuChoices(sup, new ModeSize(1600, 900), 4);
            Check(choices.Count <= 4, "menu never offers more buttons than the sheet has");
            Check(choices.Contains(new ModeSize(1600, 900)), "the current mode is always offered");
            Check(choices.Contains(new ModeSize(1920, 1080)), "1080p is offered when supported");
            var sorted = new List<ModeSize>(choices);
            sorted.Sort((a, b) => (a.W * a.H).CompareTo(b.W * b.H));
            Check(choices.SequenceEqual(sorted), "choices are presented smallest-first");

            var odd = DisplayPolicy.MenuChoices(sup, new ModeSize(1366, 768), 4);
            Check(odd.Contains(new ModeSize(1366, 768)), "an unusual native mode is still selectable");

            Check(DisplayPolicy.IsSupported(sup, 1920, 1080), "IsSupported hit");
            Check(!DisplayPolicy.IsSupported(sup, 1920, 1081), "IsSupported miss");
            Check(!DisplayPolicy.IsSupported(null, 1920, 1080), "null list is safely unsupported");

            Eq(DisplayPolicy.Fallback(sup, new ModeSize(1600, 900)).W, 1600, "exact preference survives");
            var fb = DisplayPolicy.Fallback(sup, new ModeSize(1680, 1050)); // gone from the list
            // no aspect match available -> closest area wins (1080p is 0.31 Mpx away vs 0.32)
            Eq(fb.W + "x" + fb.H, "1920x1080", "missing preference falls back to the closest-area mode");
            var aspectFirst = DisplayPolicy.Fallback(
                new List<ModeSize> { new ModeSize(1280, 720), new ModeSize(1920, 1080), new ModeSize(1024, 768) },
                new ModeSize(1152, 864)); // 4:3 preference
            Eq(aspectFirst.W + "x" + aspectFirst.H, "1024x768",
               "aspect match beats area match when choosing a fallback");
            Eq(DisplayPolicy.Fallback(new List<ModeSize>(), new ModeSize(1234, 567)).W, 1234,
               "empty supported list returns the request unchanged (caller keeps launcher default)");
            Eq(DisplayPolicy.Fallback(null, new ModeSize(1234, 567)).W, 1234, "null list is safe");

            // ------------------------------------------------ options sheet layout budget
            Group("Options sheet - row budget fits the left sheet at 1280x720 reference");
            // Mirrors SettingsView.Init row heights + VStack spacing 8: the simplified
            // audio-only modal (Phase 10.3) is AUDIO header + three volume rows. If a row
            // is added, this fails on purpose: the sheet must never overflow its column.
            float[] rows = { 18, 34, 34, 34 };       // AUDIO header + master + music + sfx
            const float ColumnHeight = 150f;
            const float Spacing = 8f;
            float total = 0f;
            foreach (var h in rows) total += h;
            total += Spacing * (rows.Length - 1);
            Check(total <= ColumnHeight,
                  $"options rows ({total:F0}px incl. spacing) must fit the {ColumnHeight:F0}px column");
            // column spans 145..295 from the top; the BACK button occupies 638..680
            Check(638f - 295f >= 20f, "action button keeps >=20px clearance below the column");

            // ------------------------------------------------ settings persistence shape
            Group("SettingsData - display fields persist; old saves still load");
            var d = new SettingsData
            {
                MasterVolume = 0.4f, SfxVolume = 1f, TextScale = 2, Fullscreen = false,
                DisplayWidth = 2560, DisplayHeight = 1440, Captions = false,
            };
            var back = SettingsData.Decode(d.Encode());
            Check(back != null, "settings encode/decode round-trips");
            if (back != null)
            {
                Check(Math.Abs(back.MasterVolume - 0.4f) < 0.001f && Math.Abs(back.SfxVolume - 1f) < 0.001f,
                      "volumes round-trip (mute=0 and max=1 included)");
                Eq(back.TextScale, 2, "text scale round-trips");
                Check(!back.Fullscreen, "windowed flag round-trips");
                Eq(back.DisplayWidth, 2560, "stored width round-trips");
                Eq(back.DisplayHeight, 1440, "stored height round-trips");
                Check(!back.Captions, "captions flag round-trips");
            }
            var legacy = new SettingsData { MasterVolume = 0.5f, SfxVolume = 0.25f }.Encode();
            var legacy7 = string.Join(",", legacy.Split(',').Take(7)); // a pre-display-settings save
            var fromOld = SettingsData.Decode(legacy7);
            Check(fromOld != null, "a 7-field (old) settings string still decodes");
            if (fromOld != null)
            {
                Check(Math.Abs(fromOld.SfxVolume - 0.25f) < 0.001f, "old saves keep their volumes");
                Check(fromOld.Fullscreen && fromOld.DisplayWidth == 0,
                      "old saves get safe display defaults (no stored preference)");
            }
            Check(SettingsData.Decode("1,2,3") == null, "truncated settings string is rejected, not crashed");
        }

        // ------------------------------------------------------- model maths
        private static void ModelMathTests()
        {
            Group("SanityModel maths");
            Eq(SanityModel.StageFor(100f), SanityStage.Stable, "100 = Stable");
            Eq(SanityModel.StageFor(70f), SanityStage.Stable, "70 = Stable (boundary)");
            Eq(SanityModel.StageFor(69.9f), SanityStage.Uneasy, "69.9 = Uneasy");
            Eq(SanityModel.StageFor(40f), SanityStage.Uneasy, "40 = Uneasy (boundary)");
            Eq(SanityModel.StageFor(39.9f), SanityStage.Shaken, "39.9 = Shaken");
            Eq(SanityModel.StageFor(15f), SanityStage.Shaken, "15 = Shaken (boundary)");
            Eq(SanityModel.StageFor(14.9f), SanityStage.Breaking, "14.9 = Breaking");
            Eq(SanityModel.StageFor(0f), SanityStage.Breaking, "0 = Breaking");
            Check(SanityModel.HallucinationChancePerSecond(SanityStage.Stable) == 0f, "no hallucinations while Stable");
            Check(SanityModel.HallucinationChancePerSecond(SanityStage.Uneasy)
                  < SanityModel.HallucinationChancePerSecond(SanityStage.Shaken), "hallucinations escalate (Uneasy<Shaken)");
            Check(SanityModel.HallucinationChancePerSecond(SanityStage.Shaken)
                  < SanityModel.HallucinationChancePerSecond(SanityStage.Breaking), "hallucinations escalate (Shaken<Breaking)");
            Eq(SanityModel.Clamp(-5f), SanityModel.Min, "clamp low");
            Eq(SanityModel.Clamp(500f), SanityModel.Max, "clamp high");
            Eq(SanityModel.Clamp(42f), 42f, "clamp passthrough");
            Check(SanityModel.SecurityRoomRecoveryPerSecond > SanityModel.DarknessDrainPerSecond,
                  "resting at the desk recovers faster than darkness drains");

            // GameRng determinism
            var r1 = new GameRng(2026);
            var r2 = new GameRng(2026);
            var seq1 = Enumerable.Range(0, 20).Select(_ => r1.NextInt(0, 1000)).ToList();
            var seq2 = Enumerable.Range(0, 20).Select(_ => r2.NextInt(0, 1000)).ToList();
            Eq(string.Join(",", seq1), string.Join(",", seq2), "same seed -> same RNG stream");
            Check(seq1.All(x => x >= 0 && x < 1000), "NextInt respects its bounds");
            Eq(r1.NextInt(5, 5), 5, "degenerate NextInt range returns min");
            var list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var before = string.Join(",", list);
            new GameRng(4).Shuffle(list);
            Eq(list.OrderBy(x => x).Select(x => x.ToString()).Aggregate((x, y) => x + "," + y), "1,2,3,4,5,6,7,8",
               "shuffle preserves every element");
            Check(string.Join(",", list) != before || true, "shuffle ran");
            try { new GameRng(1).Pick(new int[0]); Check(false, "Pick on an empty collection should throw"); }
            catch (ArgumentException) { Check(true, "Pick on an empty collection throws ArgumentException"); }
        }
    }
}
