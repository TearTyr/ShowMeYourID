// Headless test runner for the pure-C# gameplay logic of Show Me Your ID!
// Executed with:  dotnet run --project Tools/Verification/LogicTests
// Exit code 0 = all tests pass. This suite EXECUTES the real game rules —
// it is not a mock of them.
using System;
using System.Collections.Generic;
using System.Linq;
using Hollowbrook.Logic;

namespace SMYID.Tests
{
    public static class TestRunner
    {
        static int _passed, _failed;
        static string _section = "";

        static void Section(string s) { _section = s; Console.WriteLine("\n== " + s + " =="); }
        static void Check(bool cond, string name)
        {
            if (cond) { _passed++; Console.WriteLine("  PASS  " + name); }
            else { _failed++; Console.WriteLine("  FAIL  " + name); }
        }

        public static int Main()
        {
            Console.WriteLine("Show Me Your ID! — headless logic tests");
            try { RunAll(); }
            catch (Exception e)
            {
                _failed++;
                Console.WriteLine("  FAIL  UNHANDLED EXCEPTION: " + e);
            }
            Console.WriteLine($"\nRESULT: {_passed} passed, {_failed} failed");
            return _failed == 0 ? 0 : 1;
        }

        static void RunAll()
        {
            TestIdFormatRules();
            TestRosterCanon();
            TestShiftPlanShape();
            TestGenerationFairnessMultiSeed();
            TestGenerationDeterminism();
            TestAdministrator();
            TestQuestionBudgetAndAnswers();
            TestInspectionRulesAndMutations();
            TestClueTracker();
            TestSanityModel();
            TestEndings();
            TestEndlessChallengeRules();
            TestSaveFormat();
            TestPresentationLog();
            TestFullPlaythroughs();
            TestOfficeRulesAndPool();
            TestAnomalyEncounterStates();
            TestExhibitionCut();
        }

        static void TestOfficeRulesAndPool()
        {
            Section("Security office rules + shared patrol pool (Scene B)");

            var onList = new VisitorData { IsAnomaly = false, OnLateList = true, ShiftNumber = 3,
                ExpirationDay = Content.ShiftDayNumber(3) + 50, IdNumber = "HB-2026-0000".Replace("0000", "0002") };
            var offList = new VisitorData { IsAnomaly = false, OnLateList = false, ShiftNumber = 3,
                ExpirationDay = Content.ShiftDayNumber(3) + 50, IdNumber = "HB-2026-0004" };
            var expired = new VisitorData { IsAnomaly = false, OnLateList = true, ShiftNumber = 3,
                ExpirationDay = Content.ShiftDayNumber(3) - 5, IdNumber = "HB-2026-0006" };
            Check(PatrolRules.ExpectedOfficeVerdict(onList) == Verdict.Allow, "on-list, unexpired late student => Allow");
            Check(PatrolRules.ExpectedOfficeVerdict(offList) == Verdict.Reject, "off-list => Reject by the office rule");
            Check(PatrolRules.ExpectedOfficeVerdict(expired) == Verdict.Reject, "expired ID => Reject by the office rule");

            var offOutcome = InspectionRules.Apply(offList, Verdict.Reject, PatrolRules.OfficeRule);
            Check(offOutcome.Correct && !offOutcome.FalseRejection,
                "rejecting an off-list student is correct and NOT a false rejection");
            var badAllow = InspectionRules.Apply(offList, Verdict.Allow, PatrolRules.OfficeRule);
            Check(!badAllow.Correct && !badAllow.AnomalyBreach && !badAllow.FalseRejection,
                "admitting an off-list student is a mistake but not a breach/complaint");
            var anomaly = new VisitorData { IsAnomaly = true, OnLateList = true, ShiftNumber = 3, IdNumber = "HB-2026-0008" };
            Check(PatrolRules.ExpectedOfficeVerdict(anomaly) == Verdict.Reject, "anomaly at the door => Reject");
            Check(InspectionRules.Apply(anomaly, Verdict.Allow, PatrolRules.OfficeRule).AnomalyBreach,
                "admitting an anomaly at the door counts as a breach");

            // fairness of the generated late queue over many seeds
            int queues = 0, anomalies = 0;
            for (int seed = 1; seed <= 40; seed++)
            {
                var rf = new RosterFactory(seed);
                var pool = new PatrolPool();
                pool.Seed(new[] { AnomalyBehaviorKind.Blink, AnomalyBehaviorKind.Sprint });
                var rng = new GameRng(seed);
                var q = PatrolRules.BuildLateQueue(rf, ShiftPlan.Get(3), pool, rng);
                queues++;
                Check(q.Count == PatrolRules.LateArrivals, "late queue always has exactly 3 arrivals");
                foreach (var v in q)
                {
                    var docs = IdRules.EvaluateDocuments(v, rf.Registry, new PresentationLog());
                    if (v.IsAnomaly)
                    {
                        anomalies++;
                        Check(docs.Count > 0 || v.Defects.Contains(DefectType.BehaviorOddity)
                              || v.Answers.Values.Any(a => a.ContradictsDocuments || a.Tone != ToneTag.Normal),
                              "office anomaly is always detectable");
                        Check(PatrolRules.ExpectedOfficeVerdict(v) == Verdict.Reject, "office anomaly => Reject");
                    }
                    else
                    {
                        Check(docs.Count == 0, "office late student has clean documents");
                        Check(PatrolRules.ExpectedOfficeVerdict(v) == (v.OnLateList ? Verdict.Allow : Verdict.Reject),
                            "office legit verdict follows the late list exactly");
                    }
                }
                Check(pool.Pending >= 0 && pool.Pending <= 2, "pool never goes negative");
            }
            Check(queues == 40 && anomalies > 0, "pool-fed anomalies actually reached the door in the sample");

            var p = new PatrolPool();
            p.Seed(new[] { AnomalyBehaviorKind.Slide, AnomalyBehaviorKind.CoverHug, AnomalyBehaviorKind.Blink });
            Check(p.Pending == 3, "pool seeded with the shift's anomaly count");
            p.Resolve(AnomalyBehaviorKind.Slide);
            Check(p.Pending == 2, "dispelling removes the kind from the pool");
            AnomalyBehaviorKind taken;
            Check(p.TryTake(new GameRng(5), out taken), "office can take an outstanding anomaly");
            Check(p.Pending == 1, "taking consumes it");
            var p2 = new PatrolPool();
            p2.Decode(p.Encode());
            Check(p2.Pending == p.Pending && p2.PendingList[0] == p.PendingList[0], "pool survives encode/decode (save)");
            Check(!new PatrolPool().TryTake(new GameRng(1), out taken), "empty pool takes nothing");
        }

        static void TestAnomalyEncounterStates()
        {
            Section("Anomaly encounter state machine (roam/stalk/detect/expose/repel/recover)");
            var t = EncounterTuning.Default;
            EncounterStep Step(AnomalyState st, float meter, float timer, bool lit, bool foc, float dist, float dt = 0.1f)
                => AnomalyEncounter.Step(st, meter, timer, lit, foc, true, dist, dt, t);

            Check(Step(AnomalyState.Roaming, 0f, 0f, false, false, 9f).State == AnomalyState.Roaming, "dark + unlit => roaming");
            Check(Step(AnomalyState.Roaming, 0f, 0f, true, false, 9f).State == AnomalyState.Detected, "illuminated => detected");
            Check(Step(AnomalyState.Roaming, 0.3f, 0f, false, false, 9f).State == AnomalyState.Stalking, "half-tracked in the dark => stalking");
            var ex = Step(AnomalyState.Detected, 0.8f, 0f, true, true, 9f);
            Check(ex.State == AnomalyState.Exposed && ex.JustExposed, "sustained focus past 0.55 => exposed (face reveal)");
            Check(Step(AnomalyState.Exposed, 0.8f, 0f, true, false, 9f).State == AnomalyState.Detected,
                "losing the beam stalls exposure back to detected (no punishment wipe)");
            var rep = Step(AnomalyState.Exposed, 1f, 0.2f, true, true, 9f);
            Check(rep.State == AnomalyState.Repelled && rep.JustRepelled, "full meter under focus => repelled");
            Check(Step(AnomalyState.Repelled, 1f, t.ExposedBeatSeconds - 0.1f, true, true, 9f).State == AnomalyState.Repelled,
                "repel beat holds briefly");
            Check(Step(AnomalyState.Repelled, 1f, t.ExposedBeatSeconds, false, false, 9f).State == AnomalyState.Recovering,
                "after the beat it leaves the stage (recovering)");
            var rec = Step(AnomalyState.Recovering, 0f, t.RecoverSeconds, false, false, 9f);
            Check(rec.State == AnomalyState.Roaming && rec.JustRecovered, "after recovery it may roam again");
            Check(Step(AnomalyState.Stalking, 0.2f, 0f, false, false, 1.0f).Threatening, "close + active => threatening (telegraph)");
            Check(!Step(AnomalyState.Stalking, 0.2f, 0f, false, false, 5f).Threatening, "far => not threatening");
            Check(!Step(AnomalyState.Repelled, 1f, 0f, false, false, 1.0f).Threatening, "repelled => not threatening");
            Check(!Step(AnomalyState.Detected, 0.6f, 0f, true, true, 1.0f, 0.1f).JustRepelled, "no instant-kill repel from one frame");
        }

        static void TestExhibitionCut()
        {
            Section("Exhibition cut (5-10 minute booth night) + smart questioning invariants");
            var ex = ShiftPlan.ExhibitionDef();
            Check(ex.VisitorCount == 4 && ex.AnomalyCount == 1, "exhibition night = 4 visitors, 1 anomaly");
            Check(!ex.SubtleDefectsAllowed && ex.BehaviorOddityAllowed,
                "exhibition tell is honest (no subtle photo) but behaviour/voice matters");
            Check(ex.MaxDefectsPerAnomaly >= 1, "the anomaly stays catchable on paper or voice");

            // fairness of the exhibition roster through the real factory
            for (int seed = 1; seed <= 25; seed++)
            {
                var rf = new RosterFactory(seed);
                var q = rf.GenerateShift(ex);
                Check(q.Count(v => v.IsAnomaly) == 1, "seed " + seed + ": exactly one anomaly at the booth");
                foreach (var v in q)
                {
                    if (v.IsAnomaly)
                    {
                        bool paper = IdRules.EvaluateDocuments(v, rf.Registry, new PresentationLog()).Count > 0;
                        bool voice = v.Answers.Values.Any(a => a.ContradictsDocuments || a.Tone != ToneTag.Normal);
                        Check(paper || voice, "seed " + seed + ": booth anomaly detectable by papers OR voice");
                    }
                    else
                    {
                        Check(v.Answers.Values.All(a => !a.ContradictsDocuments && a.Tone == ToneTag.Normal),
                            "seed " + seed + ": legit answers never contradict and never sound odd");
                    }
                }
            }

            var s = new GameSession { Mode = GameMode.Normal, CompletedShifts = 1 };
            Check(EndingRules.EvaluateExhibition(s).ReasonKey == "exhibition", "clean booth night closes as demo complete");
            Check(EndingRules.EvaluateExhibition(new GameSession { Breaches = 3, CompletedShifts = 1 }).Ending == EndingType.Lose,
                "Lose still outranks the demo card");
            var secretS = new GameSession { CompletedShifts = 1 };
            foreach (var c in ClueRegistry.Build().OrderBy(c => c.Order)) secretS.Clues.Discover(c.Key);
            Check(EndingRules.EvaluateExhibition(secretS).Ending == EndingType.Secret, "secret still reachable in the cut");
        }

        // ------------------------------------------------------------ helpers

        static string Describe(VisitorData v) => string.Join("|",
            v.IsAnomaly ? "A" : "L", v.IsAdministrator ? "ADM" : "-", v.ClaimedName, v.RecordName,
            v.IdNumber, v.RecordIdNumber, v.IdYearLevel, v.RecordYearLevel, v.IdCourseIndex,
            v.RecordCourseIndex, v.IdIsTransferee ? "T" : "R", v.RecordIsTransferee ? "T" : "R",
            v.ExpirationDay, v.ExpirationDisplay,
            $"P({v.WindowPortrait.HairStyle},{v.WindowPortrait.FaceVariant},{v.WindowPortrait.Accessory},{(v.WindowPortrait.Distorted ? 1 : 0)})",
            $"C({v.IdPhoto.HairStyle},{v.IdPhoto.FaceVariant},{v.IdPhoto.Accessory},{(v.IdPhoto.Distorted ? 1 : 0)})",
            v.InDatabase ? "DB" : "--", string.Join("+", v.Defects), v.BehaviorGlitchVisible ? "GL" : "--",
            string.Join("/", QuestionSystem.All.Select(q =>
            {
                var a = QuestionSystem.Resolve(v, q);
                return a.Tone + (a.ContradictsDocuments ? "!" : "");
            })));

        /// <summary>Replays a shift the way play-time sees it: evaluate against the log of
        /// earlier presentations, then record the current visitor.</summary>
        static List<List<DefectType>> DerivedDefectsInOrder(RosterFactory f, List<VisitorData> visitors)
        {
            var log = new PresentationLog();
            var result = new List<List<DefectType>>();
            foreach (var v in visitors)
            {
                result.Add(IdRules.EvaluateDocuments(v, f.Registry, log));
                log.RecordFrom(v, "earlier tonight");
            }
            return result;
        }

        // ------------------------------------------------------------ tests

        static void TestIdFormatRules()
        {
            Section("ID format rules");
            Check(IdRules.IsFormatValid("HB-2024-0101"), "valid canonical ID");
            Check(!IdRules.IsFormatValid("HC-2024-0101"), "wrong prefix rejected");
            Check(!IdRules.IsFormatValid("HB20240101"), "missing dashes rejected");
            Check(!IdRules.IsFormatValid("HB-2024-010A"), "letter in serial rejected");
            Check(!IdRules.IsFormatValid("HB-2099-0101"), "implausible year rejected");
            Check(!IdRules.IsFormatValid("HB-2024-0000"), "serial 0000 rejected");
            Check(!IdRules.IsFormatValid("HB-2024-010"), "short ID rejected");
            Check(!IdRules.IsFormatValid(null) && !IdRules.IsFormatValid(""), "null/empty rejected");
            Check(IdRules.SerialOf("HB-2024-0138") == 138, "serial extraction");
            Check(IdRules.SerialOf("garbage") == -1, "serial of invalid = -1");

            var registry = new HashSet<string> { "HB-2024-0101" };
            Check(IdRules.DatabaseLookup("HB-2024-0101", registry), "DB lookup hit");
            Check(!IdRules.DatabaseLookup("HB-2024-9999", registry), "DB lookup miss");
            Check(!IdRules.DatabaseLookup("HB-2024-0101", null), "DB lookup null-safe");

            // expiration boundary: shift day == expiration day is still VALID (< comparison)
            var v = new VisitorData { ShiftNumber = 1, ExpirationDay = Content.ShiftDayNumber(1) };
            Check(!IdRules.IsExpired(v, 1), "expires today = valid");
            v.ExpirationDay = Content.ShiftDayNumber(1) - 1;
            Check(IdRules.IsExpired(v, 1), "expired yesterday = expired");
        }

        static void TestRosterCanon()
        {
            Section("Student canon (14 unique students)");
            var canon = Content.BuildStudentRoster();
            Check(canon.Count == 14, "14 identities (8 core + 6 variations)");
            Check(canon.Select(c => c.FullName).Distinct().Count() == 14, "names unique");
            Check(canon.Select(c => c.IdNumber).Distinct().Count() == 14, "ID numbers unique");
            Check(canon.All(c => IdRules.IsFormatValid(c.IdNumber)), "all registry IDs valid format");
            var portraits = canon.Select(c => (c.Portrait.HairStyle, c.Portrait.FaceVariant)).Distinct().Count();
            Check(portraits == 14, "all 14 portraits visually distinct (hair+face)");
            Check(canon.All(c => c.YearLevel >= 1 && c.YearLevel <= 4), "year levels in 1..4");
            Check(canon.All(c => c.CourseIndex >= 0 && c.CourseIndex < Content.Courses.Length), "course indices valid");
            Check(canon.Any(c => c.IsTransferee) && canon.Any(c => !c.IsTransferee), "both statuses present");
            Check(canon.Any(c => c.YearLevel == 4) && canon.Any(c => c.YearLevel == 1), "year-level variety for mutations");
            // enrollment-year flavor: 1st year => 2026 ... 4th year => 2023
            Check(canon.All(c => c.IdNumber.Substring(3, 4) == (2027 - c.YearLevel).ToString()),
                "ID enrollment year consistent with year level");
        }

        static void TestShiftPlanShape()
        {
            Section("Five-shift plan (approved progression)");
            var plan = ShiftPlan.Build();
            Check(plan.Count == 5, "5 shifts");
            Check(plan.Select(p => p.VisitorCount).SequenceEqual(new[] { 4, 6, 7, 8, 8 }), "visitors 4/6/7/8/8");
            Check(plan.Select(p => p.AnomalyCount).SequenceEqual(new[] { 0, 2, 3, 4, 4 }), "anomalies 0/2/3/4/4");
            Check(!plan[0].HasAdministrator && plan[4].HasAdministrator, "administrator only on shift 5");
            Check(plan[4].AnomalyCount == 4, "shift 5 = 8 students + administrator");
            Check(plan.All(p => !string.IsNullOrEmpty(p.RuleNoteAddendum)) &&
                  plan.Select(p => p.RuleNoteAddendum).Distinct().Count() == 5, "distinct rule notes per shift");
            Check(plan[2].DuplicateIdsAllowed && !plan[1].DuplicateIdsAllowed, "duplicates introduced shift 3");
            Check(plan[2].SubtleDefectsAllowed && !plan[0].SubtleDefectsAllowed, "subtle defects introduced shift 3");
            Check(plan[3].ExplorationFocus, "shift 4 is the investigation shift");
            Check(ShiftPlan.Get(3).Number == 3 && ShiftPlan.Get(99) == null, "ShiftPlan.Get bounds");
        }

        static void TestGenerationFairnessMultiSeed()
        {
            Section("Generation fairness (50 seeds × 5 shifts)");
            int legitChecked = 0, anomalyChecked = 0;
            bool legitClean = true, legitDb = true, legitPhoto = true, legitTone = true, legitAnswers = true, legitNeverDup = true;
            bool anomalyDetectable = true, anomalyHasDefect = true, defectsConsistent = true;
            bool countsOk = true, namesDistinct = true, subtleTiming = true, dupTiming = true;
            int expShift2 = 0;

            for (int seed = 1; seed <= 50; seed++)
            {
                var f = new RosterFactory(seed);
                var plan = ShiftPlan.Build();
                foreach (var shift in plan)
                {
                    var visitors = f.GenerateShift(shift);
                    int expectedCount = shift.VisitorCount + (shift.HasAdministrator ? 1 : 0);
                    if (visitors.Count != expectedCount) countsOk = false;
                    if (visitors.Count(v => v.IsAnomaly) != shift.AnomalyCount + (shift.HasAdministrator ? 1 : 0)) countsOk = false;
                    var students = visitors.Where(v => !v.IsAdministrator).ToList();
                    if (students.Select(v => v.ClaimedName).Distinct().Count() != students.Count)
                        namesDistinct = false; // students distinct per shift; admin excluded

                    var derived = DerivedDefectsInOrder(f, visitors);
                    for (int i = 0; i < visitors.Count; i++)
                    {
                        var v = visitors[i];
                        var d = derived[i];
                        if (v.IsAdministrator) continue;
                        if (!v.IsAnomaly)
                        {
                            legitChecked++;
                            if (d.Count != 0) legitClean = false;
                            if (!v.InDatabase) legitDb = false;
                            if (!v.PhotoMatches) legitPhoto = false;
                            if (QuestionSystem.All.Any(q => QuestionSystem.Resolve(v, q).Tone != ToneTag.Normal)) legitTone = false;
                            if (QuestionSystem.All.Any(q => QuestionSystem.Resolve(v, q).ContradictsDocuments)) legitAnswers = false;
                            var scan = f.Scan(v, LogBefore(visitors, f, i));
                            if (scan.DuplicateTonight) legitNeverDup = false;
                        }
                        else
                        {
                            anomalyChecked++;
                            if (v.Defects.Count == 0) anomalyHasDefect = false;
                            bool docDetectable = d.Count > 0;
                            bool questionDetectable = v.Defects.Contains(DefectType.BehaviorOddity) &&
                                QuestionSystem.Resolve(v, QuestionId.FirstClassroom).ContradictsDocuments;
                            if (!docDetectable && !questionDetectable) anomalyDetectable = false;
                            // every generated document-defect must be objectively derivable
                            foreach (var gen in v.Defects)
                            {
                                if (gen == DefectType.BehaviorOddity) continue;
                                if (!d.Contains(gen)) defectsConsistent = false;
                            }
                            if (v.Defects.Contains(DefectType.PhotoSubtleMismatch) && shift.Number < 3) subtleTiming = false;
                            if (v.Defects.Contains(DefectType.DuplicateId) && shift.Number < 3) dupTiming = false;
                            if (v.Defects.Contains(DefectType.ExpiredId) && !IdRules.IsExpired(v, shift.Number)) expShift2++;
                        }
                    }
                }
            }
            Check(legitChecked >= 900, $"legit visitors checked ({legitChecked})");
            Check(anomalyChecked >= 600, $"anomaly visitors checked ({anomalyChecked})");
            Check(legitClean, "legit visitors have ZERO objective document defects");
            Check(legitDb, "legit visitors always found in database");
            Check(legitPhoto, "legit photo always matches window");
            Check(legitTone, "legit answers never leak abnormal tone");
            Check(legitAnswers, "legit answers never contradict documents");
            Check(legitNeverDup, "legit visitors never flagged as duplicates");
            Check(anomalyHasDefect, "every anomaly carries >=1 defect");
            Check(anomalyDetectable, "every anomaly detectable by documents/scan/photo or <=2 questions");
            Check(defectsConsistent, "generated defects are objectively derivable from documents");
            Check(countsOk, "visitor/anomaly counts match the approved plan");
            Check(namesDistinct, "student names distinct within a shift");
            Check(subtleTiming && dupTiming, "subtle/duplicate defects only from shift 3");
            Check(expShift2 == 0, "generated ExpiredId defects are actually expired for their shift");
        }

        static PresentationLog LogBefore(List<VisitorData> visitors, RosterFactory f, int upto)
        {
            var log = new PresentationLog();
            for (int i = 0; i < upto; i++) log.RecordFrom(visitors[i], "earlier tonight");
            return log;
        }

        static void TestGenerationDeterminism()
        {
            Section("Determinism (save/resume stability)");
            var a = new RosterFactory(777);
            var b = new RosterFactory(777);
            var c = new RosterFactory(778);
            bool same = true, diff = false;
            foreach (var shift in ShiftPlan.Build())
            {
                var va = a.GenerateShift(shift);
                var vb = b.GenerateShift(shift);
                var vc = c.GenerateShift(shift);
                for (int i = 0; i < va.Count; i++)
                {
                    if (Describe(va[i]) != Describe(vb[i])) same = false;
                    if (Describe(va[i]) != Describe(vc[i])) diff = true;
                }
            }
            Check(same, "same seed => identical rosters across all 5 shifts");
            Check(diff, "different seed => different rosters");
        }

        static void TestAdministrator()
        {
            Section("Shift-5 administrator");
            var f = new RosterFactory(42);
            var admin = f.BuildAdministrator(ShiftPlan.Get(5));
            Check(admin.IsAnomaly && admin.IsAdministrator, "administrator is an anomaly");
            Check(admin.ClaimedName != admin.RecordName, "badge name != roster memo name");
            Check(admin.IdNumber != admin.RecordIdNumber, "badge number != roster memo number");
            Check(!admin.InDatabase, "badge not in student database");
            var derived = IdRules.EvaluateDocuments(admin, f.Registry, new PresentationLog());
            Check(derived.Contains(DefectType.RecordNameMismatch) && derived.Contains(DefectType.NotInDatabase),
                "administrator has >=2 objective, derivable discrepancies");
            var scan = f.Scan(admin, new PresentationLog());
            Check(!scan.FoundInDatabase && scan.Display.Contains("ROSTER MEMO"),
                "scanner directs administrator verification to the roster memo");
            Check(QuestionSystem.Resolve(admin, QuestionId.FirstClassroom).ContradictsDocuments,
                "administrator leaks intent when questioned (server room)");
            Check(admin.WindowPortrait.Distorted, "administrator shows the anomaly glitch");
            Check(InspectionRules.ExpectedVerdict(admin, null) == Verdict.Reject, "administrator must be rejected");
        }

        static void TestQuestionBudgetAndAnswers()
        {
            Section("Questioning (max 2 — conflict C1 resolution)");
            Check(Content.MaxQuestionsPerInspection == 2, "approved limit is 2 questions");
            Check(QuestionSystem.All.Length == 4, "4 question types available");
            var f = new RosterFactory(9);
            var v = f.GenerateShift(ShiftPlan.Get(2))[0];
            var budget = new QuestionSystem.Budget();
            Check(budget.Max == 2 && budget.Remaining == 2, "budget starts at 2");
            Check(budget.TryAsk(v, QuestionId.Course, out var a1) && a1.Text.Length > 0, "Q1 asked");
            Check(!budget.TryAsk(v, QuestionId.Course, out _), "repeat question refused");
            Check(budget.TryAsk(v, QuestionId.Year, out _), "Q2 asked");
            Check(!budget.TryAsk(v, QuestionId.Status, out _), "Q3 refused — budget spent");
            Check(budget.Remaining == 0 && budget.Asked == 2, "budget accounting");
            budget.Reset();
            Check(budget.Remaining == 2, "budget resets for next visitor");

            // legit answers match their documents exactly
            var legit = f.GenerateShift(ShiftPlan.Get(1));
            foreach (var lv in legit)
            {
                bool ok = QuestionSystem.Resolve(lv, QuestionId.Course).Text.StartsWith(Content.Courses[lv.RecordCourseIndex])
                    && QuestionSystem.Resolve(lv, QuestionId.Year).Text.StartsWith(Content.YearNames[lv.RecordYearLevel - 1])
                    && QuestionSystem.Resolve(lv, QuestionId.Status).Text.StartsWith(lv.RecordIsTransferee ? "Transferee" : "Regular");
                Check(ok, "legit answers match documents: " + lv.ClaimedName);
            }
        }

        static void TestInspectionRulesAndMutations()
        {
            Section("Inspection rules + challenge mutations");
            var f = new RosterFactory(3);
            var s2 = f.GenerateShift(ShiftPlan.Get(2));
            var legit = s2.First(v => !v.IsAnomaly);
            var anomaly = s2.First(v => v.IsAnomaly);

            Check(InspectionRules.ExpectedVerdict(legit, null) == Verdict.Allow, "legit => Allow");
            Check(InspectionRules.ExpectedVerdict(anomaly, null) == Verdict.Reject, "anomaly => Reject");

            var o = InspectionRules.Apply(legit, Verdict.Allow, null);
            Check(o.Correct && !o.FalseRejection && !o.AnomalyBreach, "allow legit = correct");
            o = InspectionRules.Apply(legit, Verdict.Reject, null);
            Check(!o.Correct && o.FalseRejection && !o.AnomalyBreach, "reject legit = FALSE REJECTION");
            o = InspectionRules.Apply(anomaly, Verdict.Allow, null);
            Check(!o.Correct && o.AnomalyBreach && !o.FalseRejection, "allow anomaly = BREACH");
            o = InspectionRules.Apply(anomaly, Verdict.Reject, null);
            Check(o.Correct && !o.AnomalyBreach, "reject anomaly = correct");
            Check(o.RevealedDefects.Count > 0, "verdict feedback reveals the discrepancies");

            // mutations on hand-built legit visitors
            var transferee = MakeLegit(transfer: true, year: 2, expDay: Content.DayNumberOfDate(2027, 3, 31), id: "HB-2025-0101");
            var fourth = MakeLegit(transfer: false, year: 4, expDay: Content.DayNumberOfDate(2027, 3, 31), id: "HB-2023-0102");
            var nearExp = MakeLegit(transfer: false, year: 1, expDay: Content.ShiftDayNumber(0) + 20, id: "HB-2026-0103");
            var oddSerial = MakeLegit(transfer: false, year: 1, expDay: Content.DayNumberOfDate(2027, 3, 31), id: "HB-2026-0103");
            var evenSerial = MakeLegit(transfer: false, year: 1, expDay: Content.DayNumberOfDate(2027, 3, 31), id: "HB-2026-0104");

            var mNoTrans = ShiftPlan.ChallengeMutations.First(m => m.Key == "no_transferees");
            Check(InspectionRules.ExpectedVerdict(transferee, mNoTrans) == Verdict.Reject, "no-transferees forces reject");
            Check(InspectionRules.ExpectedVerdict(fourth, mNoTrans) == Verdict.Allow, "no-transferees leaves others");
            var o2 = InspectionRules.Apply(transferee, Verdict.Reject, mNoTrans);
            Check(o2.Correct && !o2.FalseRejection, "mutation reject is NOT a false rejection");

            var mFourth = ShiftPlan.ChallengeMutations.First(m => m.Key == "fourth_year_curfew");
            Check(InspectionRules.ExpectedVerdict(fourth, mFourth) == Verdict.Reject, "4th-year curfew forces reject");
            Check(InspectionRules.ExpectedVerdict(transferee, mFourth) == Verdict.Allow, "curfew spares 2nd years");

            var mExp = ShiftPlan.ChallengeMutations.First(m => m.Key == "strict_expiry");
            Check(InspectionRules.ExpectedVerdict(nearExp, mExp) == Verdict.Reject, "strict expiry forces reject (<=60d)");
            Check(InspectionRules.ExpectedVerdict(fourth, mExp) == Verdict.Allow, "strict expiry spares 2027 IDs");

            var mOdd = ShiftPlan.ChallengeMutations.First(m => m.Key == "odd_serials");
            Check(InspectionRules.ExpectedVerdict(oddSerial, mOdd) == Verdict.Reject, "odd serial forced reject");
            Check(InspectionRules.ExpectedVerdict(evenSerial, mOdd) == Verdict.Allow, "even serial allowed");
            Check(InspectionRules.ExpectedVerdict(anomaly, mOdd) == Verdict.Reject, "mutations never rescue anomalies");
        }

        static VisitorData MakeLegit(bool transfer, int year, int expDay, string id) => new VisitorData
        {
            IsAnomaly = false, ShiftNumber = 0, ClaimedName = "T", RecordName = "T",
            IdNumber = id, RecordIdNumber = id, IdYearLevel = year, RecordYearLevel = year,
            IdCourseIndex = 0, RecordCourseIndex = 0, IdIsTransferee = transfer, RecordIsTransferee = transfer,
            ExpirationDay = expDay, WindowPortrait = default, IdPhoto = default, InDatabase = true,
        };

        static void TestClueTracker()
        {
            Section("Clues & secret-ending tracking (conflict C5)");
            var clues = ClueRegistry.Build();
            Check(clues.Count == 10, "ten environmental interactions");
            Check(clues.Select(c => c.Order).SequenceEqual(Enumerable.Range(1, 10)), "orders 1..10");
            Check(clues.Select(c => c.Key).Distinct().Count() == 10, "unique keys");
            Check(clues.Count(c => c.Room == RoomId.Hallway) == 5, "hallway hosts 5 clues");
            Check(clues.Count(c => c.Room == RoomId.Classroom1) == 3, "classroom 1 hosts 3 clues");
            Check(clues.Count(c => c.Room == RoomId.Classroom2) == 2, "classroom 2 hosts 2 clues");
            Check(clues.All(c => !string.IsNullOrEmpty(c.ChainHint)), "every clue hints the next (discoverable order)");

            var t = new ClueTracker();
            var r = t.Discover("light");
            Check(r.FirstTime && r.AdvancedSequence && !r.BrokeSequence && t.OrderIntact, "first clue in sequence");
            t.Discover("chalkboard"); t.Discover("desk");
            Check(t.FoundCount == 3 && t.NextExpectedOrder == 4 && t.OrderIntact, "sequence advances 1-2-3");
            var again = t.Discover("light");
            Check(!again.FirstTime && !again.BrokeSequence && t.FoundCount == 3, "re-inspect is idempotent");
            t.Discover("window"); // out of order (4=door expected)
            Check(!t.OrderIntact && t.FoundCount == 4, "out-of-order find breaks the sequence");
            foreach (var c in clues) t.Discover(c.Key);
            Check(t.AllFound && !t.SecretConditionsMet, "all found but broken order => no secret");
            Check(t.InvestigatedRooms.Contains(RoomId.Hallway) && t.InvestigatedRooms.Contains(RoomId.Classroom1)
                && t.InvestigatedRooms.Contains(RoomId.Classroom2), "all three rooms investigated");

            var t2 = new ClueTracker();
            foreach (var c in clues.OrderBy(c => c.Order)) t2.Discover(c.Key);
            Check(t2.AllFound && t2.OrderIntact && t2.SecretConditionsMet, "intended sequence => secret conditions met");
            Check(t2.Get("clock").Order == 10 && t2.Get("nope") == null, "clue lookup");
        }

        static void TestSanityModel()
        {
            Section("Sanity model");
            Check(SanityModel.StageFor(100) == SanityStage.Stable && SanityModel.StageFor(70) == SanityStage.Stable, "stage: Stable");
            Check(SanityModel.StageFor(69.9f) == SanityStage.Uneasy && SanityModel.StageFor(40) == SanityStage.Uneasy, "stage: Uneasy");
            Check(SanityModel.StageFor(39.9f) == SanityStage.Shaken && SanityModel.StageFor(15) == SanityStage.Shaken, "stage: Shaken");
            Check(SanityModel.StageFor(14.9f) == SanityStage.Breaking && SanityModel.StageFor(0) == SanityStage.Breaking, "stage: Breaking");
            Check(SanityModel.HallucinationChancePerSecond(SanityStage.Stable) == 0f, "no hallucinations while Stable");
            Check(SanityModel.HallucinationChancePerSecond(SanityStage.Breaking) >
                  SanityModel.HallucinationChancePerSecond(SanityStage.Shaken), "hallucinations scale with stage");

            var s = new GameSession { Mode = GameMode.Normal };
            s.ApplySanity(-1000); Check(s.Sanity == 0, "sanity clamps at 0");
            s.ApplySanity(+1000); Check(s.Sanity == 100, "sanity clamps at 100");
            s.ApplySanity(-10); Check(Math.Abs(s.Sanity - 90) < 0.001f, "normal drain 1:1");
            var c = new GameSession { Mode = GameMode.Challenge };
            c.ApplySanity(-10); Check(Math.Abs(c.Sanity - 80) < 0.001f, "challenge drain x2 (approved)");
            c.Sanity = 50; c.ApplySanity(+10); Check(Math.Abs(c.Sanity - 60) < 0.001f, "recovery not multiplied");

            var sess = new GameSession();
            float before = sess.Sanity;
            var res = sess.DiscoverClue("window"); // -8
            Check(res.FirstTime && Math.Abs(sess.Sanity - (before - 8)) < 0.001f, "clue discovery applies sanity cost");
            float afterFirst = sess.Sanity;
            sess.DiscoverClue("window");
            Check(Math.Abs(sess.Sanity - afterFirst) < 0.001f, "sanity cost applied only once per clue");
        }

        static void TestEndings()
        {
            Section("Endings (Win / Lose / Secret, conflicts C3/C4)");
            // Lose: three breaches
            var s = new GameSession { Mode = GameMode.Normal, CurrentShift = 2 };
            var f = new RosterFactory(11);
            var anomaly = f.GenerateShift(ShiftPlan.Get(2)).First(v => v.IsAnomaly);
            for (int i = 0; i < 3; i++) s.RecordVerdict(InspectionRules.Apply(anomaly, Verdict.Allow, null));
            var ev = s.EvaluateEnding();
            Check(ev.Ending == EndingType.Lose && ev.ReasonKey == "breaches", "3 breaches => Lose mid-run");
            Check(s.LoseThresholdReached, "LoseThresholdReached flag");

            // Lose: three false rejections
            s = new GameSession { Mode = GameMode.Normal };
            var legit = f.GenerateShift(ShiftPlan.Get(1))[0];
            for (int i = 0; i < 3; i++) s.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject, null));
            ev = s.EvaluateEnding();
            Check(ev.Ending == EndingType.Lose && ev.ReasonKey == "false_rejections", "3 false rejections => Lose");

            // Lose: administrator allowed = immediate (C3)
            s = new GameSession { Mode = GameMode.Normal, CurrentShift = 5 };
            var admin = f.BuildAdministrator(ShiftPlan.Get(5));
            s.RecordVerdict(InspectionRules.Apply(admin, Verdict.Allow, null));
            ev = s.EvaluateEnding();
            Check(ev.Ending == EndingType.Lose && ev.ReasonKey == "admin_breach" && s.Breaches == 1,
                "administrator allowed => immediate Lose even under breach threshold");

            // Win: five shifts done, mistakes under thresholds, admin rejected
            s = new GameSession { Mode = GameMode.Normal };
            for (int shift = 1; shift <= 5; shift++) { s.CurrentShift = shift; s.CompleteShift(); }
            s.AdminRejected = true; s.Breaches = 2; s.FalseRejections = 2;
            ev = s.EvaluateEnding();
            Check(ev.Ending == EndingType.Win && ev.ReasonKey == "win", "5 shifts + <3 mistakes => Win");

            // Lose overrides Win at exactly 3
            s.Breaches = 3;
            Check(s.EvaluateEnding().Ending == EndingType.Lose, "3 breaches after shift 5 => Lose (threshold inclusive)");

            // Secret: win path + all clues in order (C4: lose still overrides secret)
            s = new GameSession { Mode = GameMode.Normal, AdminRejected = true };
            foreach (var c in ClueRegistry.Build().OrderBy(c => c.Order)) s.DiscoverClue(c.Key);
            for (int shift = 1; shift <= 5; shift++) { s.CurrentShift = shift; s.CompleteShift(); }
            ev = s.EvaluateEnding();
            Check(ev.Ending == EndingType.Secret && ev.ReasonKey == "secret", "all clues in order + 5 shifts => Secret");
            Check(ev.ReasonText.Length > 40, "secret ending has narrative text");

            s.Breaches = 3;
            Check(s.EvaluateEnding().Ending == EndingType.Lose, "Lose overrides Secret (C4)");

            // Secret requires ALL THREE rooms; clues found but a room never investigated can't
            // happen via Discover (it marks rooms), so simulate the partial case explicitly:
            s = new GameSession { Mode = GameMode.Normal, AdminRejected = true };
            var ordered = ClueRegistry.Build().OrderBy(c => c.Order).ToList();
            for (int i = 0; i < 9; i++) s.DiscoverClue(ordered[i].Key); // miss the last clue
            for (int shift = 1; shift <= 5; shift++) { s.CurrentShift = shift; s.CompleteShift(); }
            Check(s.EvaluateEnding().Ending == EndingType.Win, "9/10 clues => Win, not Secret");

            // Not finished => no ending yet
            s = new GameSession { Mode = GameMode.Normal, CurrentShift = 3, CompletedShifts = 2 };
            Check(s.EvaluateEnding().Ending == EndingType.None, "mid-run with clean record => None (continue)");
        }

        static void TestEndlessChallengeRules()
        {
            Section("Endless & Challenge rules");
            Check(Math.Abs(EndlessRules.AnomalyChance(0) - 0.15f) < 1e-4f, "endless anomaly rate starts 15%");
            Check(Math.Abs(EndlessRules.AnomalyChance(100) - 0.6f) < 1e-4f, "endless anomaly rate caps at 60%");

            var s = new GameSession { Mode = GameMode.Endless, RunTimer = 100f };
            var f = new RosterFactory(5);
            var shift2 = f.GenerateShift(ShiftPlan.Get(2));
            var anomaly = shift2.First(v => v.IsAnomaly);
            var legit = shift2.First(v => !v.IsAnomaly);

            float d = s.ApplyRunTimer(InspectionRules.Apply(anomaly, Verdict.Reject, null));
            Check(Math.Abs(d - EndlessRules.TimePerAnomalyRejected) < 1e-4f && Math.Abs(s.RunTimer - 120f) < 1e-4f,
                "correct anomaly reject => +20s");
            d = s.ApplyRunTimer(InspectionRules.Apply(legit, Verdict.Allow, null));
            Check(Math.Abs(d - EndlessRules.TimePerLegitAllowed) < 1e-4f, "correct legit allow => +8s");
            d = s.ApplyRunTimer(InspectionRules.Apply(legit, Verdict.Reject, null));
            Check(Math.Abs(d + EndlessRules.TimePenaltyForMistake) < 1e-4f, "mistake => -12s");
            s.RunTimer = EndlessRules.HardCapSeconds - 1;
            s.ApplyRunTimer(InspectionRules.Apply(anomaly, Verdict.Reject, null));
            Check(s.RunTimer <= EndlessRules.HardCapSeconds, "timer respects hard cap");

            Check(Math.Abs(EndlessRules.StreakMultiplier(0) - 1f) < 1e-4f, "streak x1.0 at 0");
            Check(Math.Abs(EndlessRules.StreakMultiplier(3) - 1.5f) < 1e-4f, "streak x1.5 at 3");
            Check(Math.Abs(EndlessRules.StreakMultiplier(99) - 4f) < 1e-4f, "streak multiplier caps x4");
            int score = EndlessRules.ScoreFor(InspectionRules.Apply(anomaly, Verdict.Reject, null), 3);
            Check(score == 150, "anomaly at x1.5 streak = 150");
            score = EndlessRules.ScoreFor(InspectionRules.Apply(anomaly, Verdict.Allow, null), 0);
            Check(score == 0, "mistake scores 0");

            var rng = new GameRng(1);
            Check(ChallengeRules.RollMutation(0, rng).Key == "none", "challenge wave 0 has no mutation");
            bool sawMutation = false;
            for (int i = 1; i < 40; i++) if (ChallengeRules.RollMutation(i, rng).Key != "none") sawMutation = true;
            Check(sawMutation, "later challenge waves roll rule mutations");
            Check(ShiftPlan.ChallengeMutations.All(m => m.Key == "none" || !string.IsNullOrEmpty(m.RuleText)),
                "every mutation ships rule-note text");
            Check(Math.Abs(ChallengeRules.ScoreMultiplier - 2f) < 1e-4f, "challenge score multiplier x2");

            // endless generation ramps and stays fair
            var ef = new RosterFactory(23);
            int anomalies = 0, legitBad = 0, anomalyUndetectable = 0;
            var log = new PresentationLog();
            for (int i = 0; i < 200; i++)
            {
                var v = ef.GenerateRunVisitor(i);
                var d2 = IdRules.EvaluateDocuments(v, ef.Registry, log);
                if (v.IsAnomaly)
                {
                    anomalies++;
                    bool detectable = d2.Count > 0 || (v.Defects.Contains(DefectType.BehaviorOddity) &&
                        QuestionSystem.Resolve(v, QuestionId.FirstClassroom).ContradictsDocuments);
                    if (!detectable) anomalyUndetectable++;
                }
                else if (d2.Count != 0) legitBad++;
                log.RecordFrom(v, "earlier this run");
            }
            Check(anomalies > 30, $"endless run produces anomalies ({anomalies}/200)");
            Check(legitBad == 0, "endless legit visitors stay clean even with run-long duplicate log");
            Check(anomalyUndetectable == 0, "endless anomalies always detectable");
        }

        static void TestSaveFormat()
        {
            Section("Save format (round-trip, corruption guard)");
            var save = new SaveGame { SavedAtLabel = "Night 3 · 01:12" };
            var s = save.Session;
            s.Mode = GameMode.Normal; s.Seed = 20261102; s.CurrentShift = 3; s.CompletedShifts = 2;
            s.Processed = 10; s.CorrectDecisions = 9; s.FalseRejections = 1; s.Breaches = 0;
            s.AnomaliesRejected = 2; s.AdminAllowed = false; s.AdminRejected = false;
            s.Sanity = 73.25f; s.Score = 1450; s.Streak = 4; s.BestStreak = 6;
            s.RunTimer = 88.5f; s.RunProcessed = 12; s.ActiveMutationKey = "strict_expiry";
            s.History.Add(new ShiftSummary { Shift = 1, Visitors = 4, Correct = 4, FalseRejections = 0, Breaches = 0, SanityEnd = 96.5f });
            s.History.Add(new ShiftSummary { Shift = 2, Visitors = 10, Correct = 9, FalseRejections = 1, Breaches = 0, SanityEnd = 73.25f });
            s.TonightLog.Record("HB-2024-0101", new Portrait { HairStyle = 0, FaceVariant = 0, Accessory = 1 }, "earlier tonight");
            foreach (var k in new[] { "light", "chalkboard", "desk" }) s.Clues.Discover(k);
            save.Settings = new SettingsData
            {
                MasterVolume = 0.75f, MusicVolume = 0.4f, SfxVolume = 1f,
                TextScale = 2, ReduceMotion = true, PhotosensitivitySafe = true, Captions = false,
            };
            save.Records.EndlessBest = 4200; save.Records.ChallengeBest = 9100;
            save.Records.EndingsSeen.Add("lose");

            string text = SaveFormat.Serialize(save);
            var back = SaveFormat.Deserialize(text);
            Check(back != null, "deserialize succeeds");
            if (back == null) { Check(false, "save round-trip aborted — deserialize returned null"); return; }
            Check(back.Session.Mode == s.Mode && back.Session.Seed == s.Seed && back.Session.CurrentShift == 3
                && back.Session.CompletedShifts == 2, "core run fields survive round-trip");
            Check(back.Session.Processed == 10 && back.Session.CorrectDecisions == 9 && back.Session.FalseRejections == 1
                && back.Session.Breaches == 0 && back.Session.AnomaliesRejected == 2, "counters survive");
            Check(Math.Abs(back.Session.Sanity - 73.25f) < 0.001f && Math.Abs(back.Session.RunTimer - 88.5f) < 0.001f,
                "floats survive invariant-culture round-trip");
            Check(back.Session.ActiveMutationKey == "strict_expiry", "mutation key survives");
            Check(back.Session.History.Count == 2 && back.Session.History[1].Shift == 2
                && Math.Abs(back.Session.History[0].SanityEnd - 96.5f) < 0.001f, "shift history survives");
            Check(back.Session.TonightLog.WasPresented("HB-2024-0101"), "presentation log survives");
            Check(back.Session.Clues.FoundCount == 3 && back.Session.Clues.OrderIntact
                && back.Session.Clues.NextExpectedOrder == 4, "clue progress + sequence survive");
            Check(back.Session.Clues.InvestigatedRooms.Contains(RoomId.Hallway)
                && back.Session.Clues.InvestigatedRooms.Contains(RoomId.Classroom1), "room investigation survives");
            Check(back.Settings.TextScale == 2 && back.Settings.ReduceMotion && back.Settings.PhotosensitivitySafe
                && !back.Settings.Captions && Math.Abs(back.Settings.MasterVolume - 0.75f) < 0.001f, "settings survive");
            Check(back.Records.EndlessBest == 4200 && back.Records.ChallengeBest == 9100
                && back.Records.EndingsSeen.Contains("lose"), "records survive");

            // corruption guards
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
            int shiftLine = lines.FindIndex(l => l.StartsWith("shift="));
            lines[shiftLine] = "shift=9";
            Check(SaveFormat.Deserialize(string.Join("\n", lines)) == null, "tampered body => rejected (checksum)");
            Check(SaveFormat.Deserialize(text.Substring(0, text.Length / 2)) == null, "truncated => null, no throw");
            Check(SaveFormat.Deserialize("GARBAGE") == null, "garbage => null");
            Check(SaveFormat.Deserialize(text.Replace("SMYIDSAVE 1", "SMYIDSAVE 99")) == null, "future version => null");
            Check(SaveFormat.Deserialize(null) == null && SaveFormat.Deserialize("") == null, "null/empty => null");

            // out-of-order clue save keeps broken sequence
            var broken = new SaveGame();
            broken.Session.Clues.Discover("desk"); // order 3 first => breaks sequence
            var b2 = SaveFormat.Deserialize(SaveFormat.Serialize(broken));
            Check(!b2.Session.Clues.OrderIntact, "broken sequence state persists");
        }

        static void TestPresentationLog()
        {
            Section("Presentation log (duplicate rule fairness)");
            var log = new PresentationLog();
            var ana = Content.BuildStudentRoster()[0];
            var marco = Content.BuildStudentRoster()[1];
            log.Record(ana.IdNumber, ana.Portrait, "earlier tonight");
            Check(log.WasPresented(ana.IdNumber), "presentation recorded");
            Check(!log.WasPresentedWithDifferentCard(ana.IdNumber, ana.Portrait), "same card => returning student, NOT duplicate");
            Check(log.WasPresentedWithDifferentCard(ana.IdNumber, marco.Portrait), "different card => duplicate");
            log.Record(ana.IdNumber, marco.Portrait, "ignored"); // first presentation wins
            Check(!log.WasPresentedWithDifferentCard(ana.IdNumber, ana.Portrait), "log keeps first presenter only");
            Check(!log.WasPresented("HB-0000-0000"), "unknown id not presented");
            Check(!log.WasPresentedWithDifferentCard(null, ana.Portrait), "null-safe");

            string enc = log.Encode();
            var log2 = new PresentationLog();
            log2.Decode(enc);
            Check(log2.WasPresented(ana.IdNumber) && !log2.WasPresentedWithDifferentCard(ana.IdNumber, ana.Portrait),
                "encode/decode round-trip");
            var log3 = new PresentationLog();
            log3.Decode("corrupt:entry||x");
            Check(log3.Count == 0, "corrupt log decodes empty (graceful)");
        }

        static void TestFullPlaythroughs()
        {
            Section("Full playthrough simulations (10 seeds)");
            int wins = 0, secrets = 0;
            for (int seed = 100; seed < 110; seed++)
            {
                var f = new RosterFactory(seed);
                var session = new GameSession { Mode = GameMode.Normal, Seed = seed };
                var clues = ClueRegistry.Build().OrderBy(c => c.Order).ToList();
                int clueIdx = 0;

                foreach (var shift in ShiftPlan.Build())
                {
                    session.CurrentShift = shift.Number;
                    session.TonightLog.Clear();
                    var visitors = f.GenerateShift(shift);
                    foreach (var v in visitors)
                    {
                        var expected = InspectionRules.ExpectedVerdict(v, null);
                        session.RecordVerdict(InspectionRules.Apply(v, expected, null));
                        session.TonightLog.RecordFrom(v, "earlier tonight");
                        if (session.LoseThresholdReached) break;
                    }
                    // explore between visitors: two clues per shift in the intended order
                    for (int k = 0; k < 2 && clueIdx < clues.Count; k++)
                    {
                        session.DiscoverClue(clues[clueIdx].Key);
                        clueIdx++;
                    }
                    session.CompleteShift();
                    if (session.LoseThresholdReached) break;
                }

                var ev = session.EvaluateEnding();
                if (ev.Ending == EndingType.Secret) secrets++;
                else if (ev.Ending == EndingType.Win) wins++;
                Check(ev.Ending == EndingType.Secret,
                    $"seed {seed}: perfect play + all clues in order => Secret ({session.CorrectDecisions} correct, sanity {session.Sanity:0})");
            }
            Check(wins + secrets == 10, "all perfect playthroughs reach an ending");
            Check(secrets == 10, "perfect play always unlocks the Secret ending");
        }
    }
}
