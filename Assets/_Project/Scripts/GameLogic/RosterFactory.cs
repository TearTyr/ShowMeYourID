using System;
using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    public class RosterFactory
    {
        private readonly List<StudentIdentity> _canon;
        private readonly HashSet<string> _registry = new HashSet<string>();
        private readonly GameRng _rng;
        private readonly PresentationLog _genLog = new PresentationLog();

        public IReadOnlyList<StudentIdentity> Canon => _canon;
        public ISet<string> Registry => _registry;
        public PresentationLog GenLog => _genLog;

        public RosterFactory(int seed)
        {
            _rng = new GameRng(seed);
            _canon = Content.BuildStudentRoster();
            foreach (var s in _canon) _registry.Add(s.IdNumber);
        }

        public List<VisitorData> GenerateShift(ShiftDef shift)
        {
            _genLog.Clear(); // duplicates are a "tonight" rule in story mode
            var visitors = new List<VisitorData>();
            var identities = PickDistinctIdentities(shift.VisitorCount);
            var anomalySlots = PickAnomalySlots(shift.VisitorCount, shift.AnomalyCount);

            for (int i = 0; i < shift.VisitorCount; i++)
            {
                var id = identities[i];
                bool anomaly = anomalySlots.Contains(i);
                var v = anomaly ? BuildAnomaly(id, shift) : BuildLegit(id, shift);
                v.VisitorIndex = i;
                v.ShiftNumber = shift.Number;
                visitors.Add(v);
                _genLog.RecordFrom(v, "earlier tonight");
            }
            if (shift.HasAdministrator)
            {
                var admin = BuildAdministrator(shift);
                admin.VisitorIndex = shift.VisitorCount;
                visitors.Add(admin);
            }
            return visitors;
        }

        private List<StudentIdentity> PickDistinctIdentities(int count)
        {
            var pool = new List<StudentIdentity>(_canon);
            _rng.Shuffle(pool);
            return pool.GetRange(0, Math.Min(count, pool.Count));
        }

        private HashSet<int> PickAnomalySlots(int visitorCount, int anomalyCount)
        {
            var slots = new HashSet<int>();
            if (anomalyCount <= 0) return slots;
            var candidates = new List<int>();
            for (int i = 0; i < visitorCount; i++) candidates.Add(i);
            _rng.Shuffle(candidates);
            for (int i = 0; i < anomalyCount && i < candidates.Count; i++) slots.Add(candidates[i]);
            return slots;
        }

        private VisitorData BuildLegit(StudentIdentity id, ShiftDef shift)
        {
            bool nearExpiry = (shift.Number >= 2 || shift.Number == 0) && _rng.Chance(0.25f);
            int expDay = nearExpiry
                ? Content.DayNumberOfDate(2026, 11, 20 + _rng.NextInt(0, 20))
                : Content.DayNumberOfDate(2027, 3, 31);

            var v = new VisitorData
            {
                IsAnomaly = false,
                ClaimedName = id.FullName,
                RecordName = id.FullName,
                IdNumber = id.IdNumber,
                RecordIdNumber = id.IdNumber,
                IdYearLevel = id.YearLevel,
                RecordYearLevel = id.YearLevel,
                IdCourseIndex = id.CourseIndex,
                RecordCourseIndex = id.CourseIndex,
                IdIsTransferee = id.IsTransferee,
                RecordIsTransferee = id.IsTransferee,
                ExpirationDay = expDay,
                WindowPortrait = id.Portrait,
                IdPhoto = id.Portrait,
                InDatabase = true,
                RecordFirstClassroom = id.ClassroomString,
                Greeting = id.Greeting,
            };
            v.ExpirationDisplay = DisplayOf(expDay);
            BuildAnswers(v, id, truthful: true);
            v.SlipName = v.ClaimedName;             // legit slips always agree with the card
            v.SlipIdNumber = v.IdNumber;
            return v;
        }

        private static string DisplayOf(int expDay)
        {
            int offset = expDay - Content.DayNumberOfDate(2026, 11, 2); // days after Nov 2 2026
            var date = new DateTime(2026, 11, 2).AddDays(offset);
            return Content.DisplayDate(date.Year, date.Month, date.Day);
        }

        private VisitorData BuildAnomaly(StudentIdentity id, ShiftDef shift)
        {
            var v = new VisitorData
            {
                IsAnomaly = true,
                ClaimedName = id.FullName,
                RecordName = id.FullName,
                IdNumber = id.IdNumber,
                RecordIdNumber = id.IdNumber,
                IdYearLevel = id.YearLevel,
                RecordYearLevel = id.YearLevel,
                IdCourseIndex = id.CourseIndex,
                RecordCourseIndex = id.CourseIndex,
                IdIsTransferee = id.IsTransferee,
                RecordIsTransferee = id.IsTransferee,
                ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
                WindowPortrait = id.Portrait,
                IdPhoto = id.Portrait,
                InDatabase = true,
                RecordFirstClassroom = id.ClassroomString,
                Greeting = _rng.Pick(Content.AnomalyGreetings),
            };
            v.ExpirationDisplay = DisplayOf(v.ExpirationDay);
            // slip baseline agrees with the card; defects below may move it (seeded, so
            // resumes replay the identical ticket)
            v.SlipName = v.ClaimedName;
            v.SlipIdNumber = v.IdNumber;

            int defectCount = _rng.NextInt(1, shift.MaxDefectsPerAnomaly + 1);
            var pool = BuildDefectPool(shift);
            _rng.Shuffle(pool);

            bool idDefectTaken = false; // {IdFormatInvalid, NotInDatabase, DuplicateId} are exclusive
            bool photoDefectTaken = false; // {PhotoMismatch, PhotoSubtleMismatch} are exclusive
            int applied = 0;
            foreach (var d in pool)
            {
                if (applied >= defectCount) break;
                bool isIdFamily = d == DefectType.IdFormatInvalid || d == DefectType.NotInDatabase || d == DefectType.DuplicateId;
                bool isPhotoFamily = d == DefectType.PhotoMismatch || d == DefectType.PhotoSubtleMismatch;
                if (isIdFamily && idDefectTaken) continue;
                if (isPhotoFamily && photoDefectTaken) continue;
                if (d == DefectType.DuplicateId && !HasDuplicateCandidate(id)) continue;
                if (!ApplyDefect(v, d, id)) continue;
                applied++;
                if (isIdFamily) idDefectTaken = true;
                if (isPhotoFamily) photoDefectTaken = true;
            }
            if (applied == 0) // guarantee the fairness invariant: anomalies always carry a defect
                ApplyDefect(v, DefectType.ExpiredId, id);

            v.InDatabase = IdRules.IsFormatValid(v.IdNumber) && _registry.Contains(v.IdNumber);
            bool behaviorOnly = v.Defects.Count == 1 && v.Defects[0] == DefectType.BehaviorOddity;
            BuildAnswers(v, id, truthful: false, behaviorOnly);
            if (behaviorOnly && v.BehaviorGlitchVisible)
            {
                var p = v.WindowPortrait; p.Distorted = true; v.WindowPortrait = p;
            }
            // defects that rewrote the card ID (duplicate/format) resync an unbooked slip,
            // so a slip disagrees with the card exactly when SlipMismatch is booked
            if (!v.Defects.Contains(DefectType.SlipMismatch))
            {
                v.SlipName = v.ClaimedName;
                v.SlipIdNumber = v.IdNumber;
            }
            return v;
        }

        private static string MutateSerial(string id)
        {
            // format-broken IDs (a defect of their own) still get a visibly different slip
            if (id == null || id.Length != 12) return id + "X";
            if (!int.TryParse(id.Substring(8, 4), System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out int serial))
                return id.Substring(0, 11) + (id[11] == '7' ? '8' : '7');
            serial = serial > 5000 ? serial - 1 : serial + 1;
            return id.Substring(0, 8) + serial.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool HasDuplicateCandidate(StudentIdentity self)
        {
            foreach (var kvId in _genLog.PresentedIds())
            {
                var first = _genLog.FirstCardPhotoOf(kvId);
                if (first.HasValue && first.Value != self.Portrait) return true;
            }
            return false;
        }

        private List<DefectType> BuildDefectPool(ShiftDef shift)
        {
            var pool = new List<DefectType>
            {
                DefectType.PhotoMismatch, DefectType.ExpiredId,
                DefectType.IdFormatInvalid, DefectType.RecordNameMismatch,
                DefectType.YearMismatch, DefectType.CourseMismatch, DefectType.StatusMismatch,
                DefectType.NotInDatabase,
            };
            if (shift.SubtleDefectsAllowed) pool.Add(DefectType.PhotoSubtleMismatch);
            if (shift.BehaviorOddityAllowed) pool.Add(DefectType.BehaviorOddity);
            if (shift.DuplicateIdsAllowed) pool.Add(DefectType.DuplicateId);
            if (shift.Number >= 2) pool.Add(DefectType.SlipMismatch);   // slip vs card, shift 2+
            return pool;
        }

        private bool ApplyDefect(VisitorData v, DefectType d, StudentIdentity id)
        {
            switch (d)
            {
                case DefectType.PhotoMismatch:
                    v.WindowPortrait = v.IdPhoto.Swapped();
                    break;
                case DefectType.PhotoSubtleMismatch:
                    v.WindowPortrait = v.IdPhoto.SubtleVariant(_rng);
                    break;
                case DefectType.ExpiredId:
                {
                    int month = _rng.NextInt(6, 11);
                    int day = _rng.NextInt(1, 26);
                    if (month == 10 && day > 20) day = 20; // strictly before the first shift night
                    v.ExpirationDay = Content.DayNumberOfDate(2026, month, day);
                    v.ExpirationDisplay = Content.DisplayDate(2026, month, day);
                    break;
                }
                case DefectType.IdFormatInvalid:
                    v.IdNumber = CorruptId(id.IdNumber);
                    break;
                case DefectType.YearMismatch:
                    v.RecordYearLevel = 1 + (id.YearLevel % 4); // guaranteed different, stays 1..4
                    break;
                case DefectType.CourseMismatch:
                    v.RecordCourseIndex = (id.CourseIndex + 1 + _rng.NextInt(0, Content.Courses.Length - 1))
                                          % Content.Courses.Length;
                    break;
                case DefectType.StatusMismatch:
                    v.RecordIsTransferee = !id.IsTransferee;
                    break;
                case DefectType.RecordNameMismatch:
                {
                    StudentIdentity other = _canon[_rng.NextInt(0, _canon.Count)];
                    if (other.FullName == id.FullName) other = _canon[(_canon.IndexOf(other) + 1) % _canon.Count];
                    v.RecordName = other.FullName;
                    break;
                }
                case DefectType.NotInDatabase:
                {
                    string candidate;
                    int guard = 0;
                    do { candidate = "HB-2026-" + _rng.NextInt(9000, 10000).ToString("D4", System.Globalization.CultureInfo.InvariantCulture); guard++; }
                    while (_registry.Contains(candidate) && guard < 32);
                    v.IdNumber = candidate;
                    v.RecordIdNumber = candidate;
                    break;
                }
                case DefectType.DuplicateId:
                {
                    var candidates = new List<string>();
                    foreach (var sid in _genLog.PresentedIds())
                    {
                        var first = _genLog.FirstCardPhotoOf(sid);
                        if (first.HasValue && first.Value != v.IdPhoto) candidates.Add(sid);
                    }
                    if (candidates.Count == 0) return false;
                    _rng.Shuffle(candidates);
                    v.IdNumber = candidates[0];
                    v.RecordIdNumber = candidates[0];
                    v.DuplicateOfShift = "earlier tonight (" + _genLog.LabelFor(candidates[0]) + ")";
                    break;
                }
                case DefectType.BehaviorOddity:
                    v.BehaviorGlitchVisible = _rng.Chance(0.5f);
                    break;
                case DefectType.SlipMismatch:
                    // the ticket disagrees with the card: serial nudge or name typo
                    v.SlipName = v.ClaimedName;
                    v.SlipIdNumber = v.IdNumber;
                    if (_rng.Chance(0.5f)) v.SlipIdNumber = MutateSerial(v.IdNumber);
                    else v.SlipName = v.ClaimedName + "E";
                    break;
                default:
                    return false;
            }
            v.Defects.Add(d);
            return true;
        }

        private string CorruptId(string valid)
        {
            switch (_rng.NextInt(0, 6))
            {
                case 0: return "HC-" + valid.Substring(3);                       // wrong prefix
                case 1: return valid.Replace("-", "");                            // missing dashes
                case 2: return valid.Substring(0, 10) + "A" + valid[11];          // letter in serial
                case 3: return valid.Substring(0, 11);                            // short
                case 4: return "HB-2099-" + valid.Substring(8);                   // implausible year
                default: return "HB-" + valid.Substring(3, 4) + "-0000";          // serial 0000
            }
        }

        private void BuildAnswers(VisitorData v, StudentIdentity id, bool truthful, bool behaviorOnly = false)
        {
            string courseAnswer = truthful ? id.CourseString : Content.Courses[v.IdCourseIndex];
            string yearAnswer = truthful ? id.YearString : Content.YearNames[v.IdYearLevel - 1];
            string statusAnswer = (truthful ? id.IsTransferee : v.IdIsTransferee) ? "Transferee" : "Regular";

            v.Answers[QuestionId.Course] = new Answer { Text = courseAnswer + ".", Tone = ToneFor(v, behaviorOnly) };
            v.Answers[QuestionId.Year] = new Answer { Text = yearAnswer + ".", Tone = ToneFor(v, behaviorOnly) };
            v.Answers[QuestionId.Status] = new Answer { Text = statusAnswer + " student.", Tone = ToneFor(v, behaviorOnly) };

            if (behaviorOnly)
            {
                string[] wrong =
                {
                    "Room… 204? I don't remember exactly.",
                    "My first class? It's… somewhere on the second floor.",
                    "I have study hall. Always.",
                    "Room 101. No — wait. Was it 101?",
                };
                v.Answers[QuestionId.FirstClassroom] = new Answer
                {
                    Text = _rng.Pick(wrong),
                    Tone = _rng.Chance(0.5f) ? ToneTag.Hesitant : ToneTag.Flat,
                    ContradictsDocuments = true,
                };
            }
            else
            {
                v.Answers[QuestionId.FirstClassroom] = new Answer
                {
                    Text = id.ClassroomString + ".",
                    Tone = ToneFor(v, behaviorOnly),
                    ContradictsDocuments = false,
                };
            }
        }

        private ToneTag ToneFor(VisitorData v, bool behaviorOnly)
        {
            if (!v.IsAnomaly) return ToneTag.Normal; // fairness: legit never leaks tone
            if (behaviorOnly && _rng.Chance(0.5f)) return ToneTag.Hesitant;
            if (_rng.Chance(0.4f))
            {
                var tags = new[] { ToneTag.Flat, ToneTag.TooSmooth, ToneTag.Echoing, ToneTag.Hesitant };
                return _rng.Pick(tags);
            }
            return ToneTag.Normal;
        }

        public StudentIdentity PickIdentity(GameRng rng) => _canon[rng.NextInt(0, _canon.Count)];

        public VisitorData BuildLateStudent(StudentIdentity id, ShiftDef shift, bool anomaly)
        {
            var v = anomaly ? BuildAnomaly(id, shift) : BuildLegit(id, shift);
            v.ShiftNumber = shift.Number;
            v.VisitorIndex = -1;   // office visitors are outside the checkpoint roster
            return v;
        }

        public VisitorData BuildAdministrator(ShiftDef shift)
        {
            var badgePhoto = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = false };
            var v = new VisitorData
            {
                IsAnomaly = true,
                IsAdministrator = true,
                ShiftNumber = shift.Number,
                ClaimedName = "V. Marlowe",
                RecordName = "R. Vance",                  // roster memo lists a different administrator
                IdNumber = "DA-0233",                     // staff badge number, not on the roster memo
                RecordIdNumber = "DA-1147",               // memo: "R. Vance — DA-1147"
                IdYearLevel = 1, RecordYearLevel = 1,     // unused for staff (no year level)
                IdCourseIndex = 0, RecordCourseIndex = 0, // unused ("District Administration")
                IdIsTransferee = false, RecordIsTransferee = false,
                ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
                ExpirationDisplay = "2027-03-31",
                WindowPortrait = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = true },
                IdPhoto = badgePhoto,                     // subtle photo defect + distortion at window
                InDatabase = false,                       // badge not in the student registry
                DuplicateOfShift = "",
                RecordFirstClassroom = "— (district personnel)",
                Greeting = Content.AdminLines[0],
            };
            v.Defects.Add(DefectType.RecordNameMismatch);  // badge vs roster memo name
            v.Defects.Add(DefectType.NotInDatabase);       // badge number not on the roster
            v.Defects.Add(DefectType.PhotoSubtleMismatch); // same glitch as the other anomalies
            v.Answers[QuestionId.Course] = new Answer { Text = "I am with the district office.", Tone = ToneTag.TooSmooth };
            v.Answers[QuestionId.Year] = new Answer { Text = "That is not relevant to my visit.", Tone = ToneTag.Flat };
            v.Answers[QuestionId.Status] = new Answer { Text = "I am an administrator.", Tone = ToneTag.TooSmooth };
            v.Answers[QuestionId.FirstClassroom] = new Answer { Text = "My first stop is your server room.", Tone = ToneTag.Echoing, ContradictsDocuments = true };
            v.SlipName = v.ClaimedName;
            v.SlipIdNumber = v.IdNumber;
            v.SlipReason = "DISTRICT BUSINESS — SEE OFFICE";
            return v;
        }

        public VisitorData GenerateRunVisitor(int processedSoFar)
        {
            float anomalyChance = Math.Min(0.15f + processedSoFar * 0.02f, 0.6f);
            bool anomaly = _rng.Chance(anomalyChance);
            var id = _canon[_rng.NextInt(0, _canon.Count)];
            var fakeShift = new ShiftDef
            {
                Number = 0,
                VisitorCount = 1,
                AnomalyCount = anomaly ? 1 : 0,
                MaxDefectsPerAnomaly = processedSoFar < 5 ? 1 : (processedSoFar < 12 ? 2 : 3),
                SubtleDefectsAllowed = processedSoFar >= 5,
                DuplicateIdsAllowed = processedSoFar >= 8,
                BehaviorOddityAllowed = processedSoFar >= 5,
                HasAdministrator = false,
            };
            var v = anomaly ? BuildAnomaly(id, fakeShift) : BuildLegit(id, fakeShift);
            v.ShiftNumber = 0;
            _genLog.RecordFrom(v, "presented earlier this run");
            return v;
        }

        public class ScanResult
        {
            public bool FormatValid;
            public bool FoundInDatabase;
            public string RegisteredTo = "";
            public bool DuplicateTonight;
            public bool ReturningStudent;
            public string Display;
        }

        public ScanResult Scan(VisitorData v, PresentationLog playLog)
        {
            var r = new ScanResult();
            if (v.IsAdministrator)
            {
                r.FormatValid = true;
                r.FoundInDatabase = false;
                r.Display = "NOT A STUDENT ID — VERIFY AGAINST DISTRICT ROSTER MEMO";
                return r;
            }
            r.FormatValid = IdRules.IsFormatValid(v.IdNumber);
            if (!r.FormatValid)
            {
                r.Display = "SYSTEM ERROR — UNREADABLE ID NUMBER";
                return r;
            }
            bool presented = playLog != null && playLog.WasPresented(v.IdNumber);
            r.DuplicateTonight = playLog != null && playLog.WasPresentedWithDifferentCard(v.IdNumber, v.IdPhoto);
            r.ReturningStudent = presented && !r.DuplicateTonight;

            r.FoundInDatabase = _registry.Contains(v.IdNumber);
            r.RegisteredTo = NameForRegistryId(v.IdNumber);
            if (!r.FoundInDatabase)
            {
                r.Display = "NOT FOUND — NO STUDENT RECORD FOR " + v.IdNumber
                    + (r.DuplicateTonight
                        ? " · ⚠ ALREADY PRESENTED " + (playLog.LabelFor(v.IdNumber) ?? "").ToUpperInvariant() + " WITH A DIFFERENT CARD"
                        : "");
                return r;
            }
            if (r.DuplicateTonight)
                r.Display = "FOUND — REGISTERED TO " + r.RegisteredTo +
                            " · ⚠ ALREADY PRESENTED " + (playLog.LabelFor(v.IdNumber) ?? "").ToUpperInvariant() +
                            " WITH A DIFFERENT CARD";
            else if (r.ReturningStudent)
                r.Display = "FOUND — " + r.RegisteredTo + " · RETURNING STUDENT";
            else
                r.Display = "FOUND — " + r.RegisteredTo;
            return r;
        }

        private string NameForRegistryId(string idNumber)
        {
            foreach (var s in _canon) if (s.IdNumber == idNumber) return s.FullName;
            return "UNKNOWN";
        }
    }
}
