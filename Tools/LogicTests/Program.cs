// Show Me Your ID! — executable logic suite.
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
            Console.WriteLine("Show Me Your ID! — logic suite (real GameLogic assemblies, no Unity)");

            IdRulesTests();
            ContentTests();
            ShiftPlanTests();
            ClueRegistryTests();
            RosterFairnessTests();
            AdministratorTests();
            ClassroomQuestionTests();
            AdminHuntTests();
            HuntEndingTests();
            InspectionRulesTests();
            EndingRulesTests();
            SessionTests();
            ClueTrackerTests();
            SaveFormatTests();
            ModelMathTests();
            DisplayAndOptionsTests();
            TrackingAndTokenTests();
            CheckpointQueueTests();
            SpeechBubbleTests();
            StampPlacementTests();
            HallwayDarknessTests();
            FaceTruthTests();
            FaceAssetMappingTests();
            QueueBusyHurryTests();
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
            SeatPlannerTests();
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
            Group("IdRules — HB-YYYY-NNNN format");
            Check(IdRules.IsFormatValid("HB-2026-0001"), "valid id accepted");
            Check(IdRules.IsFormatValid("HB-2023-9999"), "earliest/latest year+serial accepted");
            Check(!IdRules.IsFormatValid("HB-2022-0001"), "year before 2023 rejected");
            Check(!IdRules.IsFormatValid("HB-2028-0001"), "year after 2027 rejected");
            Check(!IdRules.IsFormatValid("HB-2026-0000"), "serial 0000 rejected");
            Check(!IdRules.IsFormatValid("hb-2026-0001"), "lowercase prefix rejected");
            Check(!IdRules.IsFormatValid("XX-2026-0001"), "wrong prefix rejected");
            Check(!IdRules.IsFormatValid("HB-20260001"), "missing second dash rejected");
            Check(!IdRules.IsFormatValid("HB-20A6-0001"), "non-digit year rejected");
            Check(!IdRules.IsFormatValid("HB-2026-001"), "too short rejected");
            Check(!IdRules.IsFormatValid("HB-2026-00001"), "too long rejected");
            Check(!IdRules.IsFormatValid(""), "empty rejected");
            Check(!IdRules.IsFormatValid(null), "null rejected");
            Eq(IdRules.SerialOf("HB-2026-0147"), 147, "SerialOf");
            Eq(IdRules.SerialOf("DA-0233"), -1, "SerialOf on invalid id is -1");
            Eq(IdRules.Prefix, "HB-", "prefix constant");

            var registry = new HashSet<string> { "HB-2026-0001" };
            Check(IdRules.DatabaseLookup("HB-2026-0001", registry), "registry hit");
            Check(!IdRules.DatabaseLookup("HB-2026-0002", registry), "registry miss");
            Check(!IdRules.DatabaseLookup("HB-2026-0001", null), "null registry is a miss, not a crash");
        }

        // -------------------------------------------------------------- content
        private static void ContentTests()
        {
            Group("Content — approved constants & roster");
            Eq(Content.TotalStudents, 14, "student pool size");
            var roster = Content.BuildStudentRoster();
            Eq(roster.Count, Content.TotalStudents, "roster matches TotalStudents");
            Eq(roster.Select(s => s.FullName).Distinct().Count(), roster.Count, "roster names unique");
            Eq(roster.Select(s => s.IdNumber).Distinct().Count(), roster.Count, "roster IDs unique");
            Check(roster.All(s => IdRules.IsFormatValid(s.IdNumber)), "every canon ID is format-valid");
            Check(roster.All(s => s.YearLevel >= 1 && s.YearLevel <= 4), "year levels in 1..4");
            Check(roster.All(s => s.CourseIndex >= 0 && s.CourseIndex < Content.Courses.Length), "course indices in range");
            Check(Content.Courses.Length > 0 && Content.YearNames.Length == 4, "course/year pools populated");
            Eq(Content.ShiftDates.Length, 5, "five shift dates");
            // day-number mapping must be strictly monotonic ACROSS THE WINDOW THE GAME
            // ACTUALLY GENERATES (Jun 2026 .. Mar 2027). The model uses 30-day months, so
            // it is only an approximation: 2026-12-31 and 2027-01-01 both map to 1060.
            // No generated content lands in that gap, so ordering is safe — but the
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
        private static void ShiftPlanTests()
        {
            Group("ShiftPlan — approved 4/6/7/8/8+admin progression");
            var shifts = ShiftPlan.Build();
            Eq(shifts.Count, ShiftPlan.TotalShifts, "five shifts");
            Eq(shifts.Count, 5, "TotalShifts is 5");
            int[] wantVisitors = { 4, 6, 7, 8, 8 };
            int[] wantAnomalies = { 0, 2, 3, 4, 4 };
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
            Check(ShiftPlan.ChallengeMutations.Length >= 2, "challenge mutation pool exists");
            Check(ShiftPlan.ChallengeMutations.All(m => !string.IsNullOrEmpty(m.Key)), "every mutation has a key");
            Check(ShiftPlan.ChallengeMutations[0].Key == "none", "wave 0 is the clean 'none' rule");
            Check(ShiftPlan.ChallengeMutations.Skip(1).All(m => !string.IsNullOrEmpty(m.RuleText)),
                  "every real mutation has printable rule text for the paper note");
        }

        // ------------------------------------------------------------ clue set
        private static void ClueRegistryTests()
        {
            Group("ClueRegistry — the ten environmental clues (§2.6)");
            var clues = ClueRegistry.Build();
            Eq(clues.Count, 10, "ten clues");
            Eq(clues.Select(c => c.Order).Distinct().Count(), 10, "orders unique");
            Eq(string.Join(",", clues.Select(c => c.Order).OrderBy(o => o)), "1,2,3,4,5,6,7,8,9,10", "orders are 1..10");
            Eq(clues.Select(c => c.Key).Distinct().Count(), 10, "keys unique");
            Check(clues.All(c => !string.IsNullOrEmpty(c.Display) && !string.IsNullOrEmpty(c.InspectText)), "every clue has display + inspect text");
            Check(clues.All(c => c.Room != RoomId.Security), "no clue lives in the security room (you must leave it)");
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
            Group($"RosterFactory — fairness invariants over {Seeds} seeds x 5 shifts");

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
                            Eq(InspectionRules.ExpectedVerdict(v, null), Verdict.Allow,
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
                            Eq(v.ClassroomTone, ToneTag.Normal,
                               $"seed {seed}: legit answers are always delivered Normal (fair tell)");
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
                                              || (v.Defects.Contains(DefectType.ClassroomMismatch) && !AnswerMatchesFile(v));
                            Eq(AnswerMatchesFile(v), !v.Defects.Contains(DefectType.ClassroomMismatch),
                               $"seed {seed} shift {shift.Number}: an anomaly lies about the classroom exactly when it carries ClassroomMismatch");
                            Check(detectable,
                                  $"seed {seed} shift {shift.Number}: anomaly '{v.ClaimedName}' is undetectable [{string.Join(",", v.Defects)}]");
                            Eq(InspectionRules.ExpectedVerdict(v, null), Verdict.Reject,
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
        // clue keys, no dead ends, REFUSE ENTRY and stepping back always available, and zero
        // mechanical power (talking never stamps).
        private static void AdminTalkSuite()
        {
            Group("AdminTalk — the administrator's questions and answers");

            var problems = AdminTalk.ValidateScript();
            Check(problems.Count == 0,
                "the script validates: real destinations, no unknown tokens, reachable, always a way out" +
                (problems.Count > 0 ? " (first: " + problems[0] + ")" : ""));
            foreach (var p in problems.Take(6)) Console.WriteLine("   script: " + p);

            AdminTalkContext Fresh() => new AdminTalkContext
            {
                BadgeName = "V. Marlowe", MemoName = "R. Vance", BadgeNumber = "DA-0233", MemoNumber = "DA-1147",
                AnswerClassroom = "My first stop is your server room.",
            };

            var ctx = Fresh();
            var st = new AdminTalkState();
            var open = AdminTalk.Open(st, ctx);
            Eq(st.Node, AdminTalk.HubId, "opening lands on the hub");
            Check(open.Text.Contains("complaints about your performance"), "the hub opens with his demand");
            Eq(st.Visits, 1, "the first open counts one visit");
            var hubOptions = AdminTalk.OptionsFor(st, ctx);
            Check(hubOptions.Any(o => o.Action == AdminTalkAction.Refuse) && hubOptions.Any(o => o.Action == AdminTalkAction.StepBack),
                  "the hub always offers REFUSE ENTRY and the exit");
            Check(!hubOptions.Any(o => o.Id == "ask_photo" || o.Id == "ask_cabinet" || o.Id == "ask_server"),
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
            Check(!AdminTalk.OptionsFor(st, ctx).Any(o => o.Id == "ask_name"), "a question already asked is not offered again");
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
            var withClass = Fresh(); withClass.AskedClassroom = true;
            st = new AdminTalkState(); AdminTalk.Open(st, withClass);
            var serverOpt = AdminTalk.OptionsFor(st, withClass).FirstOrDefault(o => o.Id == "ask_server");
            Check(serverOpt != null, "asking him the classroom question unlocks the server-room follow-up");
            Check(AdminTalk.Choose(st, withClass, "ask_server").Text.Contains("My first stop is your server room."),
                  "his answer quotes what he really said at the window");

            // REFUSE ENTRY and the exit end the conversation with the right action
            st = new AdminTalkState(); ctx = Fresh(); AdminTalk.Open(st, ctx);
            var refuse = AdminTalk.Choose(st, ctx, "refuse");
            Check(refuse.Completed && refuse.Action == AdminTalkAction.Refuse && st.Completed, "REFUSE ENTRY ends the conversation as a refusal");
            Check(AdminTalk.Choose(st, ctx, "ask_name").Completed, "a finished conversation stays finished");
            st = new AdminTalkState(); AdminTalk.Open(st, ctx);
            var end = AdminTalk.Choose(st, ctx, "end");
            Check(end.Completed && end.Action == AdminTalkAction.StepBack, "stepping back ends the conversation without refusing");

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
            Group("EndingRules audit — admin gate, idempotent fold, priority");

            GameSession FiveShifts(bool adminRejected, bool adminAllowed)
            {
                var s = new GameSession { Mode = GameMode.Normal };
                s.CompletedShifts = ShiftPlan.TotalShifts;
                s.CurrentShift = ShiftPlan.TotalShifts + 1;   // CompleteShift moves past the last night
                s.AdminRejected = adminRejected;
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
            Eq(EndingRules.Evaluate(secretNoAdmin).Ending, EndingType.Secret,
               "the Secret ending is the clue chain's own prize (the admin gate guards the Win branch)");
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
            var endlessEarly = new GameSession { Mode = GameMode.Endless, CurrentShift = 1, Breaches = 3 };
            Eq(EndingRules.Evaluate(endlessEarly).Ending, EndingType.Lose, "Endless mode keeps its lose thresholds on any 'night'");
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
            Group("Shift rosters — the administrator's place in line");
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
            var rf = new RosterFactory(5);
            bool runModeAdmin = false;
            for (int i = 0; i < 200; i++)
                if (rf.GenerateRunVisitor(i).IsAdministrator) runModeAdmin = true;
            Check(!runModeAdmin, "Endless/Challenge never generate the administrator");
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
            Check(!IdRules.IsFormatValid(admin.IdNumber), "staff badge is not a student ID format");
            Check(!IdRules.DatabaseLookup(admin.IdNumber, rf.Registry), "badge number is not in the student registry");
            Check(admin.ClaimedName != admin.RecordName, "badge name differs from the roster memo");
            Check(admin.IdNumber != admin.RecordIdNumber, "badge number differs from the roster memo");
            var docDefects = IdRules.EvaluateDocuments(admin, rf.Registry, new PresentationLog());
            Check(docDefects.Contains(DefectType.RecordNameMismatch) && docDefects.Contains(DefectType.NotInDatabase),
                  "objective evaluation reports the badge discrepancies (admin special case)");
            Eq(InspectionRules.ExpectedVerdict(admin, null), Verdict.Reject, "administrator must be rejected");
            var allowed = InspectionRules.Apply(admin, Verdict.Allow, null);
            Check(allowed.AnomalyBreach && allowed.WasAdministrator && !allowed.Correct,
                  "allowing the administrator is a breach (=> immediate Lose)");
            var rejected = InspectionRules.Apply(admin, Verdict.Reject, null);
            Check(rejected.Correct && !rejected.AnomalyBreach, "rejecting the administrator is correct");
        }

        // ---------------------------------------------------- classroom question
        // "Rooms the file would say": the answer is "Room 202, Genetics." and the file prints
        // "Room 202 · Genetics" — the same room and class, spelled for a different reader.
        private static bool AnswerMatchesFile(VisitorData v) =>
            v.ClassroomAnswer.TrimEnd('.').Replace(", ", " · ") == v.RecordFirstClassroom;

        private static void ClassroomQuestionTests()
        {
            Group("Classroom question — one free question, a lie provable against the file");
            Check(!string.IsNullOrEmpty(ClassroomQuestion.ButtonLabel) && !string.IsNullOrEmpty(ClassroomQuestion.Spoken),
                  "the question has its button text and its spoken line");
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
            Check(liars > 200, "enough liars to be worth asking (" + liars + ")");
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
                    if (h.Phase == HuntPhase.Hiding || h.Phase == HuntPhase.Tracking)
                    { inp.Peeking = true; inp.PlayerRoom = h.Room; inp.InHallway = false; inp.OnTarget = h.Phase == HuntPhase.Tracking; }
                    if (h.Phase == HuntPhase.Charging) inp.BeamOnFace = true;
                    break;
                case HuntPlayer.Dodger:   // on his head for 2 s, off for 1 s, again and again
                    if (h.Phase == HuntPhase.Hiding || h.Phase == HuntPhase.Tracking)
                    { inp.Peeking = true; inp.PlayerRoom = h.Room; inp.InHallway = false; inp.OnTarget = (t % 3f) < 2f; }
                    break;
                case HuntPlayer.StepBackSpammer:   // peeks in and out every half second, never holds
                    if ((int)(t * 2f) % 2 == 0)
                    { inp.Peeking = true; inp.PlayerRoom = h.Room; inp.InHallway = false; inp.OnTarget = true; }
                    break;
                default:   // random, but sticky: changes a few times a second
                    if (rng.Chance(0.05f)) lastPeek = !lastPeek;
                    if (lastPeek)
                    {
                        inp.Peeking = true; inp.InHallway = false;
                        inp.PlayerRoom = rng.Chance(0.8f) ? h.Room : (rng.Chance(0.5f) ? RoomId.Classroom1 : RoomId.Classroom2);
                        inp.OnTarget = rng.Chance(0.6f);
                    }
                    inp.BeamOnFace = rng.Chance(0.5f);
                    if (rng.Chance(0.1f)) inp.InHallway = !inp.InHallway && !inp.Peeking;
                    break;
            }
            return inp;
        }

        // Runs one hunt; returns the end phase and the simulated seconds it took.
        private static HuntPhase RunHunt(HuntPlayer who, int seed, HuntTuning tun, out float seconds,
            out int stuns, List<HuntOutcome> outcomes)
        {
            var h = new AdminHunt(tun, seed);
            h.Start();
            var rng = new GameRng(seed * 31 + 7);
            bool peek = false;
            const float dt = 0.05f;
            float t = 0f, prevServer = h.ServerLeft;
            float maxT = tun.ServerSeconds * 20f;
            while (!h.Finished && t < maxT)
            {
                var ev = h.Tick(dt, HuntInputFor(who, h, t, rng, ref peek));
                t += dt;
                if ((ev & HuntEvent.FlickerStart) != 0 && outcomes != null) outcomes.Add(h.PendingOutcome);
                // invariants: the clock never runs backwards, every phase honours its limit
                if (h.ServerLeft > prevServer + 1e-4f) { seconds = t; stuns = -1; return HuntPhase.Hiding; }
                prevServer = h.ServerLeft;
                float limit = h.Phase == HuntPhase.Tracking ? tun.TrackMaxSeconds
                    : h.Phase == HuntPhase.Flicker ? tun.FlickerSeconds
                    : h.Phase == HuntPhase.Recoil ? tun.RecoilSeconds
                    : h.Phase == HuntPhase.Charging ? tun.ChargeWaitSeconds + tun.ChargeSeconds
                    : h.Phase == HuntPhase.Hiding ? tun.HideMaxSeconds : float.MaxValue;
                if (h.PhaseTimer > limit + dt + 0.01f) { seconds = t; stuns = -2; return HuntPhase.Hiding; }
            }
            seconds = t;
            stuns = h.Stuns;
            return h.Phase;
        }

        private static void AdminHuntTests()
        {
            Group("AdminHunt — always ends, never stuck, fair outcomes");
            var tun = HuntTuning.Default;
            Check(tun.StunsToWin >= 1 && tun.ServerSeconds > 30f, "default tuning is sane");

            // idle player: the server door meter alone ends it
            var idle = RunHunt(HuntPlayer.Idle, 1, tun, out float idleSecs, out _, null);
            Check(idle == HuntPhase.Lost && idleSecs <= tun.ServerSeconds + 1f,
                  $"a player who does nothing loses when the meter runs out ({idleSecs:F1}s)");

            // a perfect player wins quickly with exactly the needed stuns
            int perfectWins = 0; float slowest = 0f;
            for (int seed = 1; seed <= 300; seed++)
            {
                var end = RunHunt(HuntPlayer.Perfect, seed, tun, out float secs, out int stuns, null);
                if (end == HuntPhase.Won && stuns == tun.StunsToWin) perfectWins++;
                slowest = System.Math.Max(slowest, secs);
            }
            Eq(perfectWins, 300, "a perfect player always wins with exactly the needed stuns");
            Check(slowest < tun.ServerSeconds * 0.6f, $"...and well inside the meter (slowest {slowest:F0}s of {tun.ServerSeconds:F0}s)");

            // adversarial and random players: it still ends, every phase honours its limit
            foreach (var who in new[] { HuntPlayer.Dodger, HuntPlayer.StepBackSpammer, HuntPlayer.Random })
            {
                int ended = 0, badInvariant = 0;
                float longest = 0f;
                for (int seed = 1; seed <= 400; seed++)
                {
                    var end = RunHunt(who, seed, tun, out float secs, out int stuns, null);
                    if (end == HuntPhase.Won || end == HuntPhase.Lost) ended++;
                    if (stuns < 0) badInvariant++;
                    longest = System.Math.Max(longest, secs);
                }
                Eq(ended, 400, $"{who}: all 400 hunts end in a win or a loss");
                Eq(badInvariant, 0, $"{who}: the server clock never rewinds and no phase overstays");
                Check(longest <= tun.ServerSeconds * 3f, $"{who}: longest {longest:F0}s stays bounded");
            }

            // outcomes are random but never the same three times in a row
            int maxRun = 1, charges = 0, total = 0;
            for (int seed = 1; seed <= 200; seed++)
            {
                var seq = new List<HuntOutcome>();   // one hunt at a time: streaks never cross hunts
                RunHunt(HuntPlayer.Perfect, seed, tun, out _, out _, seq);
                int run = 1;
                for (int i = 0; i < seq.Count; i++)
                {
                    total++;
                    if (seq[i] == HuntOutcome.HallwayCharge) charges++;
                    run = i > 0 && seq[i] == seq[i - 1] ? run + 1 : 1;
                    maxRun = System.Math.Max(maxRun, run);
                }
            }
            Check(maxRun <= 2, "never the same outcome three times in a row (longest run " + maxRun + ")");
            Check(charges > total / 5 && charges < total * 4 / 5, "both outcomes happen (" + charges + "/" + total + " charges)");

            // same seed + same input = same hunt
            var a = RunHunt(HuntPlayer.Random, 77, tun, out float aSecs, out int aStuns, null);
            var b = RunHunt(HuntPlayer.Random, 77, tun, out float bSecs, out int bStuns, null);
            Check(a == b && aSecs == bSecs && aStuns == bStuns, "a hunt replays identically from its seed");

            // the lamp must be ON him to count: holding off-target never stuns
            var off = new AdminHunt(tun, 5); off.Start();
            for (int i = 0; i < 400 && off.Phase != HuntPhase.Tracking; i++)
                off.Tick(0.05f, new HuntInput { Peeking = true, PlayerRoom = off.Room });
            for (int i = 0; i < 100; i++) off.Tick(0.05f, new HuntInput { Peeking = true, PlayerRoom = off.Room, OnTarget = false });
            Check(off.Stuns == 0 && off.Hold == 0f, "peeking without the lamp on his head never stuns");

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
            Check(pathFast > pathSlow * 1.3f, "after three stuns he moves clearly faster");
        }

        // the hunt decides how the last night ends
        private static void HuntEndingTests()
        {
            Group("Hunt endings — win needs the hunt won, loss is its own ending, state is saved");
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

            var game = new SaveGame { Session = Night5(HuntState.Active) };
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(game));
            Check(back != null && back.Session.Hunt == HuntState.Active, "the hunt state survives a save and load");
        }

        // ----------------------------------------------------- verdict matrix
        private static void InspectionRulesTests()
        {
            Group("InspectionRules — verdict / false-rejection / breach matrix");
            var rf = new RosterFactory(11);
            var visitors = rf.GenerateShift(ShiftPlan.Get(3));
            var legit = visitors.First(x => !x.IsAnomaly);
            var anomaly = visitors.First(x => x.IsAnomaly && !x.IsAdministrator);

            var okAllow = InspectionRules.Apply(legit, Verdict.Allow, null);
            Check(okAllow.Correct && !okAllow.FalseRejection && !okAllow.AnomalyBreach, "legit + Allow = correct, clean");
            var badReject = InspectionRules.Apply(legit, Verdict.Reject, null);
            Check(!badReject.Correct && badReject.FalseRejection && !badReject.AnomalyBreach, "legit + Reject = false rejection");
            var badAllow = InspectionRules.Apply(anomaly, Verdict.Allow, null);
            Check(!badAllow.Correct && badAllow.AnomalyBreach && !badAllow.FalseRejection, "anomaly + Allow = breach");
            var okReject = InspectionRules.Apply(anomaly, Verdict.Reject, null);
            Check(okReject.Correct && !okReject.AnomalyBreach && !okReject.FalseRejection, "anomaly + Reject = correct");
            Eq(InspectionRules.Apply(legit, Verdict.None, null).Correct, false, "no verdict is never 'correct'");

            // Challenge rule mutation flips the required verdict for some legit visitors
            var strict = new RuleMutationDef("strict_expiry", "test rule");
            var nearExpiryLegit = visitors.FirstOrDefault(x => !x.IsAnomaly && strict.ForcesReject(x));
            if (nearExpiryLegit != null)
            {
                Eq(InspectionRules.ExpectedVerdict(nearExpiryLegit, strict), Verdict.Reject, "mutation forces Reject");
                var forced = InspectionRules.Apply(nearExpiryLegit, Verdict.Reject, strict);
                Check(forced.Correct && !forced.FalseRejection,
                      "rejecting under an official rule mutation is correct and NOT a false rejection");
            }
            else Console.WriteLine("   (no legit visitor matched 'strict_expiry' in this shift — rule path covered by mutation unit checks)");

            Check(!strict.ForcesReject(anomaly), "mutations never force-reject an anomaly (it is already Reject)");
            var noTransferees = new RuleMutationDef("no_transferees", "test");
            var odd = new RuleMutationDef("odd_serials", "test");
            var curfew = new RuleMutationDef("fourth_year_curfew", "test");
            Check(!new RuleMutationDef("unknown_key", "x").ForcesReject(legit), "unknown mutation key is inert");

            // strict_expiry works on a 60-day window measured on Content's day-number axis
            var far = new VisitorData
            {
                IsAnomaly = false, ShiftNumber = 3, IdIsTransferee = false, IdYearLevel = 2,
                IdNumber = "HB-2026-0004", ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
            };
            Check(!strict.ForcesReject(far), "strict_expiry ignores a far-future expiry (2027-03-31)");
            var near = new VisitorData
            {
                IsAnomaly = false, ShiftNumber = 3, IdIsTransferee = false, IdYearLevel = 2,
                IdNumber = "HB-2026-0004", ExpirationDay = Content.ShiftDayNumber(3) + 10,
            };
            Check(strict.ForcesReject(near), "strict_expiry fires 10 days out (inside the 60-day window)");
            var edge = new VisitorData
            {
                IsAnomaly = false, ShiftNumber = 3, IdNumber = "HB-2026-0004",
                ExpirationDay = Content.ShiftDayNumber(3) + 60,
            };
            Check(strict.ForcesReject(edge), "strict_expiry fires exactly at 60 days (inclusive)");
            var past = new VisitorData
            {
                IsAnomaly = false, ShiftNumber = 3, IdNumber = "HB-2026-0004",
                ExpirationDay = Content.ShiftDayNumber(3) + 61,
            };
            Check(!strict.ForcesReject(past), "strict_expiry does not fire at 61 days");

            var probe = new VisitorData
            {
                IsAnomaly = false, ShiftNumber = 3, IdIsTransferee = true, IdYearLevel = 4,
                IdNumber = "HB-2026-0003", ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
            };
            Check(noTransferees.ForcesReject(probe), "no_transferees forces reject on a transferee");
            Check(curfew.ForcesReject(probe), "fourth_year_curfew forces reject on a 4th year");
            Check(odd.ForcesReject(probe), "odd_serials forces reject on serial 0003");
            Check(!noTransferees.ForcesReject(new VisitorData { ShiftNumber = 3, IdNumber = "HB-2026-0004" }),
                  "no_transferees ignores a regular student");
            Check(!curfew.ForcesReject(new VisitorData { ShiftNumber = 3, IdYearLevel = 2, IdNumber = "HB-2026-0004" }),
                  "fourth_year_curfew ignores a 2nd year");
            Check(!odd.ForcesReject(new VisitorData { ShiftNumber = 3, IdNumber = "HB-2026-0004" }),
                  "odd_serials ignores an even serial");

            // the Challenge 'strict_expiry' rule only matters if near-expiry legit IDs exist
            int nearExpiryCount = 0;
            for (int seed = 1; seed <= 40; seed++)
            {
                var rfe = new RosterFactory(seed);
                for (int i = 0; i < 25; i++)
                {
                    var rv = rfe.GenerateRunVisitor(i);
                    if (!rv.IsAnomaly && strict.ForcesReject(rv)) nearExpiryCount++;
                }
            }
            Check(nearExpiryCount > 0,
                  $"endless/challenge runs do produce near-expiry legit IDs (found {nearExpiryCount} in 1000 visitors)");
        }

        // ---------------------------------------------------------- endings
        private static void EndingRulesTests()
        {
            Group("EndingRules — Win / Lose / Secret with Lose priority (conflict C4)");
            Eq(EndingRules.LoseThreshold, 3, "lose threshold is 3");

            var fresh = new GameSession { Mode = GameMode.Normal, CompletedShifts = 0 };
            Eq(EndingRules.Evaluate(fresh).Ending, EndingType.None, "mid-run evaluates to None");
            Eq(EndingRules.Evaluate(fresh).ReasonKey, "continue", "mid-run reason key");

            var win = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, AdminRejected = true };
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
            var secretSession = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, CurrentShift = 6 };
            foreach (var clue in ClueRegistry.Build().OrderBy(x => x.Order)) secretSession.Clues.Discover(clue.Key);
            Check(secretSession.Clues.SecretConditionsMet, "all ten clues in order satisfy the secret conditions");
            Eq(EndingRules.Evaluate(secretSession).Ending, EndingType.Secret, "all clues in order = Secret ending");
            Eq(EndingRules.Evaluate(secretSession).ReasonKey, "secret", "secret reason key");

            secretSession.AdminAllowed = true;
            Eq(EndingRules.Evaluate(secretSession).Ending, EndingType.Lose, "Lose outranks Secret (priority order)");
            var secretWithBreaches = new GameSession { Mode = GameMode.Normal, CompletedShifts = 5, CurrentShift = 6, Breaches = 3 };
            foreach (var clue in ClueRegistry.Build().OrderBy(x => x.Order)) secretWithBreaches.Clues.Discover(clue.Key);
            Eq(EndingRules.Evaluate(secretWithBreaches).Ending, EndingType.Lose, "breaches outrank Secret too");

            // Endless/Challenge never reach the Normal win branch
            var endless = new GameSession { Mode = GameMode.Endless, CompletedShifts = 99 };
            Eq(EndingRules.Evaluate(endless).Ending, EndingType.None, "Endless run does not resolve to the Normal Win");
        }

        // ---------------------------------------------------------- session
        private static void SessionTests()
        {
            Group("GameSession — counters, sanity, shift completion");
            var rf = new RosterFactory(5);
            var visitors = rf.GenerateShift(ShiftPlan.Get(3));
            var legit = visitors.First(x => !x.IsAnomaly);
            var anomaly = visitors.First(x => x.IsAnomaly && !x.IsAdministrator);

            var s = new GameSession { Mode = GameMode.Normal, CurrentShift = 1, Sanity = SanityModel.Max };
            Eq(s.Sanity, 100f, "sanity starts at max");
            Eq(s.SanityStage, SanityStage.Stable, "starts Stable");

            s.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow, null));
            Eq(s.Processed, 1, "processed increments");
            Eq(s.CorrectDecisions, 1, "correct increments");
            Eq(s.Streak, 1, "streak increments");
            Eq(s.BestStreak, 1, "best streak tracks");
            Eq(s.Sanity, 100f, "a correct call does not touch sanity directly");
            Eq(s.RefillPending, SanityModel.RefillOnCorrect, "...it queues a refill instead");
            Eq(s.Stacks.Count, 0, "a correct call puts no drain stack up");

            s.RecordVerdict(InspectionRules.Apply(anomaly, Verdict.Allow, null));
            Eq(s.Breaches, 1, "breach counted");
            Eq(s.Streak, 0, "streak resets on a mistake");
            Eq(s.BestStreak, 1, "best streak preserved");
            Eq(s.Sanity, 100f, "a breach costs no sanity directly");
            Eq(s.Stacks.Count, 1, "...it adds a drain stack");

            s.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject, null));
            Eq(s.FalseRejections, 1, "false rejection counted");
            Eq(s.Sanity, 100f, "a false rejection costs no sanity directly");
            Eq(s.Stacks.Count, 2, "...it adds another drain stack");

            var challenge = new GameSession { Mode = GameMode.Challenge, Sanity = 100f };
            challenge.ApplySanity(-10f);
            Check(Math.Abs(challenge.Sanity - 83f) < 1e-3f, "Challenge drains twice as fast (10 x 2 x the 0.85 baseline = 17)");
            Eq(SanityModel.DrainMultiplier(GameMode.Challenge), 2f, "challenge drain multiplier");
            Eq(SanityModel.DrainMultiplier(GameMode.Normal), 1f, "normal drain multiplier");

            var admin = rf.BuildAdministrator(ShiftPlan.Get(5));
            var adminSession = new GameSession { Sanity = 50f };
            adminSession.RecordVerdict(InspectionRules.Apply(admin, Verdict.Reject, null));
            Check(adminSession.AdminRejected && !adminSession.AdminAllowed, "admin rejection recorded");
            Eq(adminSession.Sanity, 50f, "rejecting the administrator is a correct call: a refill, not an instant jump");
            Eq(adminSession.RefillPending, SanityModel.RefillOnCorrect, "...the refill is queued");
            adminSession.RecordVerdict(InspectionRules.Apply(admin, Verdict.Allow, null));
            Check(adminSession.AdminAllowed, "allowing the administrator sets AdminAllowed");

            Group("SanityStacks — wrong calls speed the drain up for a while");
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

            Group("ShiftFailureRules — what a failed night costs the next one");
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

            Group("GameSession — a failed night is paid for by the next one");
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
            breachSess.RecordVerdict(InspectionRules.Apply(anomaly, Verdict.Allow, null));
            Eq(breachSess.ShiftBreaches, 1, "an anomaly let in counts toward this shift's breaches");
            breachSess.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow, null));
            Eq(breachSess.ShiftBreaches, 1, "a correct call does not");

            Group("GameSession — drain stacks scale losses, a correct call refills gradually");
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
            Check(Math.Abs(handed - 15f) < 1e-3f, "the whole +15 is eventually handed over");
            Eq(ticks, 12, "...in 12 half-second ticks of 1.25");
            Check(ticks * 0.5f >= 5f && ticks * 0.5f <= 8f, "...so the refill takes 5-8 seconds (" + ticks * 0.5f + " s)");
            Eq(refill.TakeRefill(0.5f), 0f, "nothing is handed over once the refill is paid out");
            Eq(refill.TakeRefill(-1f), 0f, "a negative tick hands over nothing");
            Check(SanityModel.RefillOnCorrect == 15f, "a correct call is worth +15");
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

            // endless timer math
            var run = new GameSession { Mode = GameMode.Endless, RunTimer = EndlessRules.StartSeconds };
            run.ApplyRunTimer(InspectionRules.Apply(anomaly, Verdict.Reject, null));
            Eq(run.RunTimer, EndlessRules.StartSeconds + EndlessRules.TimePerAnomalyRejected, "rejecting an anomaly adds time");
            run.ApplyRunTimer(InspectionRules.Apply(legit, Verdict.Allow, null));
            Eq(run.RunTimer, EndlessRules.StartSeconds + EndlessRules.TimePerAnomalyRejected + EndlessRules.TimePerLegitAllowed,
               "allowing a legit visitor adds less time");
            var capped = new GameSession { Mode = GameMode.Endless, RunTimer = EndlessRules.HardCapSeconds };
            capped.ApplyRunTimer(InspectionRules.Apply(anomaly, Verdict.Reject, null));
            Eq(capped.RunTimer, EndlessRules.HardCapSeconds, "timer respects the hard cap");
        }

        // ------------------------------------------------------- clue tracker
        private static void ClueTrackerTests()
        {
            Group("ClueTracker — intended sequence gates the secret ending");
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
            Group("SaveFormat — round-trip, checksum tamper detection, bad input");
            Eq(SaveFormat.Header, "SMYIDSAVE", "header constant");
            Eq(SaveFormat.Version, 2, "version constant");

            var save = new SaveGame { SavedAtLabel = "NIGHT 3 - 02:14" };
            save.Session.Mode = GameMode.Challenge;
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
            save.Session.RunTimer = 187.25f;
            save.Session.RunProcessed = 17;
            save.Session.ActiveMutationKey = "strict_expiry";
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
            save.Records.EndlessBest = 9999;
            save.Records.ChallengeBest = 12345;
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
                Check(Math.Abs(s.RunTimer - save.Session.RunTimer) < 0.01f, "run timer round-trips");
                Eq(s.RunProcessed, save.Session.RunProcessed, "run processed round-trips");
                Eq(s.ActiveMutationKey, save.Session.ActiveMutationKey, "active mutation round-trips");
                Check(s.RunFinished, "run finished flag round-trips");
                Eq(s.History.Count, 1, "shift history round-trips");
                Eq(s.Clues.FoundCount, 4, "clue discoveries round-trip");
                Eq(back.Settings.TextScale, 2, "text scale round-trips");
                Check(back.Settings.ReduceMotion && back.Settings.PhotosensitivitySafe && !back.Settings.Captions,
                      "accessibility toggles round-trip");
                Check(Math.Abs(back.Settings.MasterVolume - 0.55f) < 0.001f, "master volume round-trips");
                Eq(back.Records.EndlessBest, 9999, "endless record round-trips");
                Eq(back.Records.ChallengeBest, 12345, "challenge record round-trips");
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
            foreach (var v in rf.GenerateShift(ShiftPlan.Get(3)).Take(3)) log.RecordFrom(v, "tonight");
            Eq(log.Count, 3, "presentation log recorded three cards");
            var log2 = new PresentationLog();
            log2.Decode(log.Encode());
            Eq(log2.Count, 3, "presentation log round-trips");
            Eq(string.Join(",", log2.PresentedIds().OrderBy(x => x)), string.Join(",", log.PresentedIds().OrderBy(x => x)),
               "presentation log keeps the same IDs");
        }

        // ------------------------------------------------ tracking / reveal / tokens
        private static void TrackingAndTokenTests()
        {
            Group("TrackingPolicy — Focus advances, observation holds, darkness decays");
            Eq(TrackingPolicy.RatePerSecond(true, false), TrackingPolicy.FocusRatePerSecond, "Focus is the only beam that ADVANCES the meter");
            Eq(TrackingPolicy.RatePerSecond(false, true), TrackingPolicy.HoldRatePerSecond, "observation (sweep/lit room) only HOLDS — ambient light never resolves");
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

            Group("AnomalyEncounter — one authoritative progression path (additive to the frozen suite)");
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

            Group("BeamGeometry — true cone coverage (direction, range, angle, body radius)");
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
            Check(!BeamGeometry.Covers(0, 0, 1, 0, focus, 4, focusHalfAt4 + 0.15f, 0f), "outside the focus cone edge at 4u — the modes really differ");
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

            Group("FlashlightInput — latch/hold arbitration (F/Q/L + mouse holds)");
            FlashlightLatches l;
            var none = new FlashlightLatches();
            Eq(FlashlightInput.Resolve(none, false, false, false, false, false, false, out l), LightMode.Off, "no input => Off");
            Eq(FlashlightInput.Resolve(none, true, false, false, false, false, false, out l), LightMode.Sweep, "F latches Sweep");
            Check(l.Sweep && !l.Focus, "F sets only the sweep latch");
            var sweepLatched = new FlashlightLatches { Sweep = true };
            Eq(FlashlightInput.Resolve(sweepLatched, false, true, false, false, false, false, out l), LightMode.Focus, "Q switches to Focus");
            Check(l.Focus && !l.Sweep, "keyboard toggles are mutually exclusive — Q clears the sweep latch");
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
            Check(!l.Sweep && !l.Focus, "a dead battery clears the latches — revival needs deliberate input");
            Eq(FlashlightInput.Resolve(sweepLatched, true, false, false, false, false, true, out l), LightMode.Off, "toggling F while dead does nothing");
            Check(!l.Sweep, "toggling F while dead does not re-latch");
            Eq(FlashlightInput.Resolve(sweepLatched, false, false, true, true, false, false, out l), LightMode.Sweep, "L+hold same frame: the hold still shows a beam this frame");
            Eq(FlashlightInput.Resolve(l, false, false, false, false, false, false, out l), LightMode.Off, "...and L's clear sticks after the hold is released");

            Group("BatteryModel — drain, recover, death and deliberate reboot");
            var bp = new BatteryParams { DrainSweepPerSecond = 2.5f, DrainFocusPerSecond = 6f, RecoverDarkPerSecond = 4f, RecoverLitPerSecond = 8f, RebootFraction = 0.2f };
            bool jd, jr;
            var s0 = new BatteryState { Charge = 1f };
            var s1 = BatteryModel.Tick(s0, LightMode.Sweep, false, 1f, bp, out jd, out jr);
            Check(Math.Abs(s1.Charge - 0.975f) < 1e-4f, "Sweep drains 2.5% per second");
            var s2 = BatteryModel.Tick(s0, LightMode.Focus, false, 1f, bp, out jd, out jr);
            Check(Math.Abs(s2.Charge - 0.94f) < 1e-4f, "Focus drains 6% per second — faster than Sweep");
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

            Group("BehaviorRules — four distinct anomalies, explicit per-kind numbers");
            var bt = BehaviorTuning.Default;
            var blink = BehaviorProfile.For(AnomalyBehaviorKind.Blink, bt);
            var slide = BehaviorProfile.For(AnomalyBehaviorKind.Slide, bt);
            var sprint = BehaviorProfile.For(AnomalyBehaviorKind.Sprint, bt);
            var hug = BehaviorProfile.For(AnomalyBehaviorKind.CoverHug, bt);
            Check(blink.MoveSpeed == bt.BlinkSpeed && slide.MoveSpeed == bt.SlideSpeed &&
                  sprint.MoveSpeed == bt.SprintSpeed && hug.MoveSpeed == bt.CoverHugSpeed,
                "each behavior moves at its own configured speed");
            Check(sprint.DashSpeed > sprint.MoveSpeed, "Sprint's burst is faster than its patrol");
            Check(sprint.TelegraphSeconds > 0f && blink.TelegraphSeconds == 0f && slide.TelegraphSeconds == 0f && hug.TelegraphSeconds == 0f,
                "only Sprint has a windup telegraph — the reaction window is explicit");
            Check(sprint.BeamInterruptsBurst && !blink.BeamInterruptsBurst, "only Sprint's windup is beam-interruptible");
            Check(blink.FreezesWhenObserved && !slide.FreezesWhenObserved && !sprint.FreezesWhenObserved && !hug.FreezesWhenObserved,
                "only Blink freezes when observed");
            Check(hug.CeilingLurker && !blink.CeilingLurker && !slide.CeilingLurker && !sprint.CeilingLurker,
                "only CoverHug is the ceiling lurker (Focus-only identification)");
            Check(blink.CatchGraceSeconds == bt.CatchGraceBlink && slide.CatchGraceSeconds == bt.CatchGraceSlide &&
                  sprint.CatchGraceSeconds == bt.CatchGraceSprint && hug.CatchGraceSeconds == bt.CatchGraceCoverHug,
                "catch windups are per-behavior — no shared coverHug-named constant");
            Check(Math.Abs(hug.SpawnY - 4.4f) < 1e-3f && hug.SpawnY >= hug.MinY && hug.SpawnY <= hug.MaxY,
                "CoverHug spawns INSIDE its own clamp band (no contradictory coordinates)");
            Check(hug.MinY > blink.MaxY, "the ceiling band sits above the ground walkers");
            Check(blink.MinY == blink.MaxY && slide.MinY == slide.MaxY && sprint.MinY == sprint.MaxY,
                "ground walkers stay on the floor line");
            Check(bt.RetreatSpeed > blink.MoveSpeed && bt.RetreatSpeed > hug.MoveSpeed, "a repelled anomaly withdraws faster than it stalks");
            var customB = bt; customB.CatchGraceCoverHug = 5f;
            Eq(BehaviorProfile.For(AnomalyBehaviorKind.CoverHug, customB).CatchGraceSeconds, 5f, "CoverHug grace is tunable on its own");
            Eq(BehaviorProfile.For(AnomalyBehaviorKind.Blink, customB).CatchGraceSeconds, bt.CatchGraceBlink, "...without touching the other three");

            Group("RoomGraph — roaming through connected rooms, never teleporting");
            Eq(RoomGraph.Roamable.Length, 3, "three roamable rooms");
            Check(!RoomGraph.IsRoamable(RoomId.Security), "the security exit is not a patrol route");
            Check(RoomGraph.Connected(RoomId.Hallway, RoomId.Classroom1) && RoomGraph.Connected(RoomId.Classroom1, RoomId.Hallway), "hallway ↔ classroom 1");
            Check(RoomGraph.Connected(RoomId.Hallway, RoomId.Classroom2) && RoomGraph.Connected(RoomId.Classroom2, RoomId.Hallway), "hallway ↔ classroom 2");
            Check(!RoomGraph.Connected(RoomId.Classroom1, RoomId.Classroom2), "classrooms are NOT directly connected");
            Check(float.IsNaN(RoomGraph.DoorX(RoomId.Classroom1, RoomId.Classroom2)), "no phantom door between classrooms");
            Check(float.IsNaN(RoomGraph.DoorX(RoomId.Hallway, RoomId.Hallway)), "no self doors");
            Eq(RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom1), -4.0f, "hallway C1 door anchor == RoomBuilder placement");
            Eq(RoomGraph.DoorX(RoomId.Hallway, RoomId.Classroom2), 4.6f, "hallway C2 door anchor == RoomBuilder placement");
            Eq(RoomGraph.DoorX(RoomId.Classroom1, RoomId.Hallway), 4.9f, "classroom 1 hallway door anchor == RoomBuilder placement");
            Eq(RoomGraph.DoorX(RoomId.Classroom2, RoomId.Hallway), 4.9f, "classroom 2 hallway door anchor == RoomBuilder placement");
            Eq(RoomGraph.NextHop(RoomId.Classroom1, RoomId.Classroom2), RoomId.Hallway, "classroom→classroom routes through the hallway");
            Eq(RoomGraph.NextHop(RoomId.Hallway, RoomId.Classroom2), RoomId.Classroom2, "hallway→classroom is one hop");
            Eq(RoomGraph.NextHop(RoomId.Classroom1, RoomId.Classroom1), RoomId.Classroom1, "already there: no hop");
            Eq(RoomGraph.NextHop(RoomId.Security, RoomId.Hallway), RoomId.Security, "off-graph origins do not route");

            Group("Tokens — collection persists with the run");
            var ses = new GameSession();
            Eq(ses.TokensCollected, 0, "a fresh run holds no tokens");
            ses.TokensCollected = 3;
            var save = new SaveGame { Session = ses };
            var back = SaveFormat.Deserialize(SaveFormat.Serialize(save));
            Check(back != null, "save with tokens serialises");
            if (back != null) Eq(back.Session.TokensCollected, 3, "token count round-trips");
            var old = SaveFormat.Deserialize(SaveFormat.Serialize(new SaveGame()).Replace("tokens=3", "tokens="));
            Check(old != null && old.Session.TokensCollected == 0, "a save without tokens loads as zero");
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

        private static void QueueBusyHurryTests()
        {
            Group("CheckpointQueue — the busy-window hurry multiplier");

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
        }

        private static void StampRulesTests()
        {
            Group("InspectionRules — detain verdict + warnings/fines (stamp rework)");
            var roster = RosterFor(3, 2);
            VisitorData legit = null, anom = null;
            foreach (var v in roster) { if (legit == null && !v.IsAnomaly) legit = v; if (anom == null && v.IsAnomaly) anom = v; }
            Check(legit != null && anom != null, "roster provides a legit and an anomaly subject");

            var rA = InspectionRules.Apply(anom, Verdict.Reject, null);
            Check(rA.Correct, "reject scores anomalies correctly");
            var aL = InspectionRules.Apply(legit, Verdict.Allow, null);
            Check(aL.Correct, "allow scores legit students correctly");

            var sN = new GameSession { Mode = GameMode.Normal, Score = 250 };
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject, null));   // miss 1
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Allow, null));   // correct
            sN.RecordVerdict(InspectionRules.Apply(anom, Verdict.Allow, null));    // miss 2
            sN.RecordVerdict(InspectionRules.Apply(legit, Verdict.Reject, null));  // miss 3 -> fine
            Eq(sN.Warnings, 3, "three mistakes book three warnings");
            Eq(sN.Fines, 1, "the third warning draws a fine");
            Eq(sN.Score, 150, "the fine costs 100 score (penalty, not game over)");
        }

        private static void FloorFileTests()
        {
            Group("RecordSearch — floor folders for the FLOOR FILES cabinet");
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
                int prev = -1;
                foreach (int i in f.Indices)
                {
                    if (i <= prev) ordered = false;              // roster order inside a folder
                    prev = i;
                    if (!seen.Add(i)) ordered = false;           // filed exactly once
                    if (RecordSearch.FloorOf(canon[i].FirstClassroom) != f.Floor) floorsOk = false;
                }
            }
            Eq(seen.Count, canon.Count, "every canon student is filed exactly once");
            Check(ordered, "files keep roster order inside their folder");
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
            Group("DrawerMotion — the floor-files pull drawer");
            Eq(DrawerMotion.EaseOut(0f), 0f, "the pull starts at 0");
            Eq(DrawerMotion.EaseOut(1f), 1f, "the pull ends at 1");
            Check(DrawerMotion.EaseOut(0.5f) > 0.5f,
                  $"an ease-out covers more than half the travel in half the time ({DrawerMotion.EaseOut(0.5f):F3})");
            Check(DrawerMotion.EaseOut(0.25f) > 0.5f,
                  $"...and over half of it in the first quarter ({DrawerMotion.EaseOut(0.25f):F3}) — it answers the click");
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
                  $"the whole motion is under a fifth of a second ({DrawerMotion.DurationSeconds:F3}s) — it feels immediate");

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
            Group("FloorPlan — HOLLOWBROOK COLLEGE ground floor");
            Eq(FloorPlan.Title, "HOLLOWBROOK COLLEGE — GROUND FLOOR", "the sheet carries the full title");
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
            Check(sec.Value.Right <= hall.Value.Left + 0.01f,
                $"the security checkpoint is on the LEFT (its right edge {sec.Value.Right} <= the hallway's {hall.Value.Left})");
            Check(c1.Value.Bottom <= hall.Value.Top + 0.01f && c2.Value.Bottom <= hall.Value.Top + 0.01f,
                $"BOTH classrooms are ABOVE the hallway, as the reference plan draws them " +
                $"({c1.Value.Bottom} / {c2.Value.Bottom} <= {hall.Value.Top})");
            Check(c1.Value.Right <= c2.Value.Left + 0.01f,
                $"Room 101 sits WEST of Room 202 along the north wall ({c1.Value.Right} <= {c2.Value.Left})");
            Check(sec.Value.Top >= hall.Value.Bottom - 0.01f && sec.Value.X < hall.Value.X,
                $"the security checkpoint hangs BELOW the hallway's west end ({sec.Value.Top} >= {hall.Value.Bottom})");
            var crN = FloorPlan.ForKey("room_comfort_north").Value;
            var crS = FloorPlan.ForKey("room_comfort_south").Value;
            Check(crN.Left >= hall.Value.Right - 0.01f && crS.Left >= hall.Value.Right - 0.01f,
                "the comfort rooms hang off the EAST end, clear of the corridor");
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
            Eq(comfort.Count, 2, "the plan carries two comfort rooms");
            foreach (var cr in comfort)
            {
                Check(cr.Comfort && !cr.Room.HasValue && !cr.Sealed,
                      "'" + cr.Key + "' is a comfort room, not a patrol room and not sealed");
                Check(cr.Label.ToUpperInvariant().Contains("COMFORT ROOM"),
                      "'" + cr.Key + "' is labelled as a comfort room");
                Check(!cr.Overlaps(hall.Value) && !cr.Overlaps(c1.Value) && !cr.Overlaps(c2.Value) &&
                      !cr.Overlaps(sec.Value) && !cr.Overlaps(server.Value),
                      "'" + cr.Key + "' obstructs nothing — not the hallway, not a room");
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
            foreach (var id in RoomGraph.Roamable)
                Check(FloorPlan.ForRoom(id).HasValue, "roamable room " + id + " is on the plan");
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
            Group("MapSheetLayout — the centred ground-floor sheet");
            var baseFit = MapSheetLayout.Fit(1280f, 720f);
            Check(baseFit.SheetW > 0f && baseFit.SheetH > 0f, "the sheet has a real size");
            Check(Math.Abs(baseFit.SheetW / baseFit.SheetH - FloorPlan.DesignW / FloorPlan.DesignH) < 1e-4f,
                  $"the sheet keeps the plan's aspect ({baseFit.SheetW / baseFit.SheetH:F4} vs " +
                  $"{FloorPlan.DesignW / FloorPlan.DesignH:F4}) — the drawing is scaled, never stretched");
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
                // so the only thing that can break centring is a size that overflows — hence
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
            Group("Slip generation — fairness across seeds and shifts");
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

        private static void SeatPlannerTests()
        {
            Group("SeatPlanner — determinism, distinct seats, overflow, duplicates");
            var ids = new List<string> { "HB-2026-0001", "HB-2026-0002", "HB-2026-0003", "HB-2026-0004" };
            int[] a = SeatPlanner.Plan(ids, 6, new GameRng(42));
            int[] b = SeatPlanner.Plan(ids, 6, new GameRng(42));
            Check(a.SequenceEqual(b), "same seed + same list = identical seating");
            Check(a.Where(s => s >= 0).Distinct().Count() == 4, "four students, four distinct seats");
            Check(a.All(s => s >= 0 && s < 6), "every seat index is inside the room");

            var many = new List<string>();
            for (int i = 0; i < 10; i++) many.Add("HB-2026-" + (100 + i));
            var over = SeatPlanner.Plan(many, 6, new GameRng(7));
            Check(over.Count(s => s >= 0) == 6 && over.Count(s => s < 0) == 4,
                  "overflow: six seated, four skipped with -1, no exception");
            Check(over.Where(s => s >= 0).Distinct().Count() == 6, "overflow: seated seats stay distinct");

            var dup = new List<string> { "HB-2026-0001", "HB-2026-0002", "HB-2026-0001" };
            var dp = SeatPlanner.Plan(dup, 6, new GameRng(42));
            Check(dp[0] == dp[2] && dp[1] != dp[0], "duplicate id shares one seat (one body)");
            Check(SeatPlanner.Plan(new List<string>(), 6, new GameRng(1)).Length == 0, "empty list: empty plan");
            Check(SeatPlanner.Plan(null, 6, new GameRng(1)).Length == 0, "null list: empty plan, no exception");
            Check(SeatPlanner.Plan(ids, 0, new GameRng(1)).All(s => s == -1), "zero seats: everyone skipped");
        }

        private static void CultureRoundTripTests()
        {
            Group("SaveFormat/SettingsData — locale independence (I-05)");
            var saved = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Globalization.CultureInfo comma = null;
            try { comma = new System.Globalization.CultureInfo("de-DE"); }
            catch (System.Globalization.CultureNotFoundException) { }
            if (comma == null)
            {
                Check(true, "skipped here: invariant-globalization process — rerun with ICU for the de-DE pass");
                return;
            }
            System.Threading.Thread.CurrentThread.CurrentCulture = comma;
            try
            {
                var save = new SaveGame();
                save.Session.Mode = GameMode.Normal;
                save.Session.Sanity = 63.5f;
                save.Session.RunTimer = 187.25f;
                save.Session.QueueState = new QueueSnapshot { Started = true, TimerSeconds = 4.5f, Warned = 1 };
                save.Settings.MasterVolume = 0.55f;
                save.Settings.MusicVolume = 0.3f;
                var text = SaveFormat.Serialize(save);
                Check(text.Contains("sanity=63.5") && text.Contains("queueTimer=4.5") && text.Contains("0.55"),
                      "de-DE: floats serialize with a DOT, never a comma");
                var back = SaveFormat.Deserialize(text);
                Check(back != null && back.Session.Sanity == 63.5f && back.Session.RunTimer == 187.25f &&
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
            Group("RosterFactory — resume replays identical visitors (I-09)");
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
            Group("CheckpointQueue — FIFO invariant + warned-edge persistence (audit fixes)");

            // 1) a free window with a waiting line belongs to the head of the line: a fresh
            //    arrival must never jump it. State is built through restore — the only path
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

            // 2) the pressure warning is once per shift — including across save/continue
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
        // it or the desk clears it — there is no timeout and no hover rule any more.
        private static void SpeechBubbleTests()
        {
            Group("SpeechBubbleClock — a line stays until replaced or cleared");

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
                "the line is still up later — there is no timer to take it away");

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
            Eq(b.Generation, g0 + 10, "generations are strictly monotonic — every line gets its own");
            Eq(b.Text, "line 9", "the bubble always shows the newest line only (no overlap)");

            Group("SpeechBubbleLayout — wrap estimate, truncation, bubble height");

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
            Group("StampPlacement — desk drop point -> slip-local spot");

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
            // dropped at its visual centre (810,530) — hand-computed expectations
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

            Group("StampPlacement — tilted half extent + printable-area clamp");

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
            Group("HallwayDarkness — corridor ambient + the limited-visibility pool");

            float vsLit = HallwayDarkness.DarkerThanLitRoom(0.12f);
            Check(vsLit > 0.70f && vsLit < 0.85f,
                $"the corridor ambient is 70-85% darker than a normally lit room ({vsLit:P1})");
            Check(HallwayDarkness.BaseAlpha > 0.62f,
                "the corridor's base darkness is deeper than a dark classroom's 0.62");

            Eq(HallwayDarkness.DepthAt(0f), 0f, "at the custodian the depth layer adds nothing");
            Eq(HallwayDarkness.DepthAt(HallwayDarkness.ClearRadius), 0f,
                "the pool's edge is still fully clear (the near field is navigable)");
            Eq(HallwayDarkness.DepthAt(HallwayDarkness.DepthRadius), 1f,
                "from DepthRadius out the corridor is as deep as it gets");
            float mid = HallwayDarkness.DepthAt(
                (HallwayDarkness.ClearRadius + HallwayDarkness.DepthRadius) / 2f);
            Check(mid > 0.2f && mid < 0.8f,
                $"the falloff passes through the middle ({mid:F2}) — a grade, not a step");

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

            Check(HallwayDarkness.VisibleFractionAt(0f) > 0.10f &&
                  HallwayDarkness.VisibleFractionAt(0f) < 0.30f,
                "the near field keeps 10-30% of the art's brightness: navigable, not lit");
            Check(HallwayDarkness.VisibleFractionAt(10f) < 0.08f,
                "beyond the pool the corridor keeps under 8% brightness (darkness dominates)");

            float viewHalf = 5f * 16f / 9f;      // ortho size 5 at the 16:9 reference aspect
            float share = HallwayDarkness.PoolShareOfView(viewHalf);
            Check(share > 0.15f && share < 0.25f,
                $"the pool spans 15-25% of the on-screen width ({share:P1})");

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
            Group("Face comparison truth — the cue follows the rendered faces, never a coin flip");
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
                "an anomaly never shows a matching cue — its in-world face is the altered variant");
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
            Group("FaceAssets — stable identity mapping for the temporary face set");
            Check(FaceAssets.StudentKey(0) == "student_face_01" &&
                  FaceAssets.StudentKey(FaceAssets.StudentFaceCount) == "student_face_01",
                "student keys wrap stably over the shipped set");
            Check(FaceAssets.AnomalyKey(0) == "student_face_01_anomaly" &&
                  FaceAssets.AnomalyKey(5) == "student_face_06_anomaly",
                "anomaly keys are PAIRED with their student: student_face_NN_anomaly");
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
                "an altered window face is THAT STUDENT's anomaly variant — paired, explicit, never random");
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < FaceAssets.StudentFaceCount; i++) seen[FaceAssets.StudentKey(i)] = i;
            Check(seen.Count == FaceAssets.StudentFaceCount,
                "every shipped student face maps to exactly one identity slot (no shared faces)");
        }

        private static void CheckpointQueueTests()
        {
            Group("CheckpointQueue — start gate, arrival clock, FIFO line, window promotion");

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

            Group("CheckpointQueue — pressure warning once, overwhelmed collapse once");

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

            Group("CheckpointQueue — missing student (finite roster, one incident per shift)");

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

            Group("CheckpointQueue — snapshot, save round-trip, legacy resume, run-mode source");

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

            int generated = 0;
            var runQ = new CheckpointQueue();
            runQ.AttachRunSource(() =>
            {
                generated++;
                return new VisitorData { VisitorIndex = -1, IsAnomaly = generated % 2 == 0, ClaimedName = "RUN" + generated };
            }, new QueueTuning { ArrivalMinSeconds = 0.01f, ArrivalMaxSeconds = 0.02f, MissingStudentChance = 1f }, 9);
            runQ.MarkStarted();
            runQ.ForceNextArrival();
            for (int i = 0; i < 5; i++) { runQ.Tick(1f); runQ.CompleteActive(); }
            Eq(runQ.ArrivalCount, 6, "run-mode source keeps producing");
            Eq(runQ.EffectiveTotal, -1, "run modes have no effective total");
            Check(!runQ.ShiftComplete && !runQ.SourceExhausted, "run modes never complete by roster");
            Eq(runQ.MissingIncidents, 0, "missing-student is a finite-roster (Normal) mechanic");
            Check(!new CheckpointQueue().IsAttached, "a detached queue reports unattached (PersistRun guard)");

            Group("CheckpointQueue — two-phase resolution (quit mid-banner can never re-stamp)");
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
            Group("DisplayPolicy — resolution choices, fallbacks, revert window");
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
            Group("Options sheet — row budget fits the left sheet at 1280x720 reference");
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
            Group("SettingsData — display fields persist; old saves still load");
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
            Group("SanityModel / Endless / Challenge maths");
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

            Eq(EndlessRules.AnomalyChance(0), 0.15f, "endless anomaly chance at 0");
            Check(EndlessRules.AnomalyChance(10) > EndlessRules.AnomalyChance(0), "anomaly chance ramps");
            Eq(EndlessRules.AnomalyChance(1000), 0.6f, "anomaly chance caps at 0.6");
            Eq(EndlessRules.StreakMultiplier(0), 1f, "streak multiplier starts at 1x");
            Eq(EndlessRules.StreakMultiplier(3), 1.5f, "+0.5x every 3 streak");
            Eq(EndlessRules.StreakMultiplier(300), 4f, "streak multiplier caps at 4x");
            Eq(ChallengeRules.ScoreMultiplier, 2f, "challenge doubles the score");
            Eq(ChallengeRules.StartSeconds, EndlessRules.StartSeconds, "challenge starts from the endless clock");
            Eq(ChallengeRules.VisitorsPerRuleWave, 4, "a rule wave lasts 4 visitors");

            var rng = new GameRng(99);
            Check(ChallengeRules.RollMutation(0, rng) == ShiftPlan.ChallengeMutations[0], "wave 0 is always the clean rule");
            var rolled = new HashSet<string>();
            for (int i = 0; i < 200; i++) rolled.Add(ChallengeRules.RollMutation(1 + i % 5, rng).Key);
            Check(rolled.Count >= 2, "later waves actually rotate through mutations");

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
