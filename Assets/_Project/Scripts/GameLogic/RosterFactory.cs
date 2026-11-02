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
        private readonly Dictionary<StudentIdentity, int> _timesAnomaly = new Dictionary<StudentIdentity, int>();

        public IReadOnlyList<StudentIdentity> Canon => _canon;
        public ISet<string> Registry => _registry;

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
// The school only has so many students. When a night is longer than the registry,
            // the extra visitors are impostors wearing a student's name (never a second honest
            // visit by the same student). They come last.
            int honestSlots = Math.Min(shift.VisitorCount, _canon.Count);
            int impostors = shift.VisitorCount - honestSlots;
            var turning = PickLeastUsed(Math.Min(honestSlots, Math.Max(0, shift.AnomalyCount - impostors)));
            var identities = PickNight(turning, honestSlots);
            for (int i = 0; i < impostors; i++)
            {
                var worn = _canon[_rng.NextInt(0, _canon.Count)];
                identities.Add(worn);
                CountTurn(worn);
            }

            var isAnomaly = new bool[shift.VisitorCount];
            for (int i = 0; i < isAnomaly.Length; i++)
                isAnomaly[i] = i >= honestSlots || turning.Contains(identities[i]);
            var scared = PickScared(isAnomaly, shift.ScaredCount);

            for (int i = 0; i < shift.VisitorCount; i++)
            {
                var id = identities[i];
                bool afraid = scared.Contains(i);
                var v = isAnomaly[i] ? BuildAnomaly(id, shift, afraid) : BuildLegit(id, shift, afraid);
                v.VisitorIndex = i;
                v.ShiftNumber = shift.Number;
                visitors.Add(v);
                _genLog.RecordFrom(v, "earlier tonight");
            }
            if (shift.HasAdministrator)
            {
                visitors.Add(BuildAdministrator(shift));
            }
            return visitors;
        }

// Who turns tonight: whoever has been an anomaly the fewest times this run, ties broken at
        // random, so every student gets their turn in every run. The roster replays from the seed,
        // so a Continue picks the same students.
        private List<StudentIdentity> PickLeastUsed(int count)
        {
            var pool = new List<StudentIdentity>(_canon);
            _rng.Shuffle(pool);
            var order = new List<StudentIdentity>();
            foreach (var s in pool)
            {
                int place = 0;
                while (place < order.Count && Turns(order[place]) <= Turns(s)) place++;
                order.Insert(place, s);
            }
            var picked = order.GetRange(0, count);
            foreach (var s in picked) CountTurn(s);
            return picked;
        }

        private int Turns(StudentIdentity s) => _timesAnomaly.TryGetValue(s, out int n) ? n : 0;

        private void CountTurn(StudentIdentity s) => _timesAnomaly[s] = Turns(s) + 1;

        // Who arrives frightened: spread over real students and anomalies (a real student first, then
        // an anomaly, and so on), never the administrator. Decided BEFORE the defects, because a scared
        // face hides the monster art and changes what an anomaly can be caught by.
        private HashSet<int> PickScared(bool[] isAnomaly, int count)
        {
            var honest = new List<int>();
            var wrong = new List<int>();
            for (int i = 0; i < isAnomaly.Length; i++)
            {
                if (isAnomaly[i]) wrong.Add(i);
                else honest.Add(i);
            }
            _rng.Shuffle(honest);
            _rng.Shuffle(wrong);

            var picked = new HashSet<int>();
            for (int n = 0; n < count; n++)
            {
                var from = n % 2 == 0 ? honest : wrong;
                if (from.Count == 0) from = honest.Count > 0 ? honest : wrong;
                if (from.Count == 0) break;
                picked.Add(from[0]);
                from.RemoveAt(0);
            }
            return picked;
        }

        // Tonight's line: everyone turning, the rest honest, in a random order.
        private List<StudentIdentity> PickNight(List<StudentIdentity> turning, int count)
        {
            var rest = new List<StudentIdentity>(_canon);
            rest.RemoveAll(s => turning.Contains(s));
            _rng.Shuffle(rest);
            var night = new List<StudentIdentity>(turning);
            night.AddRange(rest.GetRange(0, count - turning.Count));
            _rng.Shuffle(night);
            return night;
        }

        private VisitorData BuildLegit(StudentIdentity id, ShiftDef shift, bool scared)
        {
            bool nearExpiry = shift.NearExpiryAllowed && _rng.Chance(GameRules.Current.NearExpiryChance);
            int expDay = nearExpiry
                ? Content.DayNumberOfDate(2026, 11, 20 + _rng.NextInt(0, 20))
                : Content.DayNumberOfDate(2027, 3, 31);

            var v = new VisitorData
            {
                IsAnomaly = false,
                IsScared = scared,
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
            v.FaceAssetIndex = _canon.IndexOf(id);   // stable face-asset identity
            v.WindowFaceIndex = v.FaceAssetIndex;    // the real student stands at the window
            BuildAnswers(v, id);
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

        private VisitorData BuildAnomaly(StudentIdentity id, ShiftDef shift, bool scared)
        {
            var v = new VisitorData
            {
                IsAnomaly = true,
                IsScared = scared,
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
            v.FaceAssetIndex = _canon.IndexOf(id);   // the person being impersonated, stably
            v.WindowFaceIndex = v.FaceAssetIndex;    // a photo defect below may change this
            // slip baseline agrees with the card; defects below may move it (seeded, so
            // resumes replay the identical ticket)
            v.SlipName = v.ClaimedName;
            v.SlipIdNumber = v.IdNumber;

            int defectCount = _rng.NextInt(1, shift.MaxDefectsPerAnomaly + 1);
            var pool = BuildDefectPool(shift, scared);
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
            BuildAnswers(v, id);
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

        private List<DefectType> BuildDefectPool(ShiftDef shift, bool scared)
        {
            var pool = new List<DefectType>
            {
                DefectType.PhotoMismatch, DefectType.ExpiredId,
                DefectType.IdFormatInvalid, DefectType.RecordNameMismatch,
                DefectType.YearMismatch, DefectType.CourseMismatch, DefectType.StatusMismatch,
                DefectType.NotInDatabase,
            };
            // a scared face hides the anomaly art, and a subtle photo change has no picture of its own
            if (shift.SubtleDefectsAllowed && !scared) pool.Add(DefectType.PhotoSubtleMismatch);
            if (shift.ClassroomLieAllowed) pool.Add(DefectType.ClassroomMismatch);
            if (shift.ClassroomLieAllowed) pool.Add(DefectType.SpokenMismatch);
            if (shift.DuplicateIdsAllowed) pool.Add(DefectType.DuplicateId);
            if (shift.SlipMismatchAllowed) pool.Add(DefectType.SlipMismatch);
            return pool;
        }

        private bool ApplyDefect(VisitorData v, DefectType d, StudentIdentity id)
        {
            switch (d)
            {
                case DefectType.PhotoMismatch:
                    v.WindowPortrait = v.IdPhoto.Swapped();
                    // somebody else's face stands at the window (1..N-1 steps never lands on the same face art)
                    v.WindowFaceIndex = v.FaceAssetIndex + 1 + _rng.NextInt(0, FaceAssets.StudentFaceCount - 1);
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
                    v.IdYearLevel = 1 + (id.YearLevel % 4); // the card lies; always different, stays 1..4
                    break;
                case DefectType.CourseMismatch:
                    v.IdCourseIndex = (id.CourseIndex + 1 + _rng.NextInt(0, Content.Courses.Length - 1))
                                      % Content.Courses.Length;
                    break;
                case DefectType.StatusMismatch:
                    v.IdIsTransferee = !id.IsTransferee;
                    break;
                case DefectType.RecordNameMismatch:
                {
                    StudentIdentity other = _canon[_rng.NextInt(0, _canon.Count)];
                    if (other.FullName == id.FullName) other = _canon[(_canon.IndexOf(other) + 1) % _canon.Count];
                    v.ClaimedName = other.FullName;   // the card names somebody else
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
                case DefectType.ClassroomMismatch:
                    break;   // nothing on the papers: the lie is told in BuildAnswers
                case DefectType.SpokenMismatch:
                    v.SpokenLieQuestion = _rng.NextInt(0, 3);   // course, year or status
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

        // What the visitor says to each of the four questions. An honest student says what their
        // card says (and the classroom on their file). Two kinds of anomaly lie out loud:
        // ClassroomMismatch about the room, SpokenMismatch about course, year or status. Some
        // anomalies also get the words wrong: rolled once, for the whole conversation. A real
        // student never speaks that way.
        private void BuildAnswers(VisitorData v, StudentIdentity id)
        {
            // a scared visitor, student or not, talks the same shaken way: speech never gives them away
            v.SpeaksAwkwardly = v.IsAnomaly && !v.IsScared && _rng.Chance(GameRules.Current.AwkwardSpeechChance);
            AwkwardSpeech awkward = v.SpeaksAwkwardly ? id.Awkward : null;
            if (v.IsScared)
            {
                awkward = Content.ScaredSpeech;
                v.Greeting = string.IsNullOrEmpty(id.ScaredGreeting) ? Content.ScaredSpeech.Greeting : id.ScaredGreeting;
            }
            else if (awkward != null) v.Greeting = awkward.Greeting;
            v.Answers[(int)QuestionId.Course] = InterviewRules.CourseSpeech(Content.Courses[v.IdCourseIndex], awkward);
            v.Answers[(int)QuestionId.Year] = InterviewRules.YearSpeech(Content.YearNames[v.IdYearLevel - 1], awkward);
            v.Answers[(int)QuestionId.Status] = InterviewRules.StatusSpeech(v.IdIsTransferee, awkward);
            string classroom = v.Defects.Contains(DefectType.ClassroomMismatch) ? LieAboutClassroom(id) : id.ClassroomSpeech;
            v.ClassroomAnswer = InterviewRules.ClassroomSpeech(classroom, awkward);
            v.Answers[(int)QuestionId.Classroom] = v.ClassroomAnswer;
            if (v.Defects.Contains(DefectType.SpokenMismatch)) LieAboutCard(v, awkward);
        }

        // The spoken answer to the chosen question is never what the card says.
        private void LieAboutCard(VisitorData v, AwkwardSpeech awkward)
        {
            var q = (QuestionId)v.SpokenLieQuestion;
            switch (q)
            {
                case QuestionId.Course:
                    int other = (v.IdCourseIndex + 1 + _rng.NextInt(0, Content.Courses.Length - 1)) % Content.Courses.Length;
                    v.Answers[(int)q] = InterviewRules.CourseSpeech(Content.Courses[other], awkward);
                    break;
                case QuestionId.Year:
                    int year = 1 + (v.IdYearLevel + _rng.NextInt(0, 3)) % 4;   // 1..4, never the card's
                    v.Answers[(int)q] = InterviewRules.YearSpeech(Content.YearNames[year - 1], awkward);
                    break;
                default:
                    v.Answers[(int)q] = InterviewRules.StatusSpeech(!v.IdIsTransferee, awkward);
                    break;
            }
        }

        // Three kinds of lie, all provable against the file: the other room, a room the school
        // does not have (the drawer holds only two folders), or the right room and a wrong class.
        private string LieAboutClassroom(StudentIdentity id)
        {
            switch (_rng.NextInt(0, 3))
            {
                case 0:
                    var other = id.FirstClassroom == RoomId.Classroom1 ? RoomId.Classroom2 : RoomId.Classroom1;
                    return Content.Speech(other, id.FirstClassSubject);
                case 1:
                    int[] missing = { 303, 305, 410 };
                    return "Room " + _rng.Pick(missing) + ", " + id.FirstClassSubject + ".";
                default:
                    return Content.Speech(id.FirstClassroom, OtherSubjectInRoom(id));
            }
        }

        // A real class from the same room that is not this student's.
        private string OtherSubjectInRoom(StudentIdentity id)
        {
            var subjects = new List<string>();
            foreach (var s in _canon)
                if (s.FirstClassroom == id.FirstClassroom && s.FirstClassSubject != id.FirstClassSubject)
                    subjects.Add(s.FirstClassSubject);
            return subjects.Count > 0 ? _rng.Pick(subjects.ToArray()) : "Study Hall";
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
                RecordIdNumber = "DA-1147",               // memo: "R. Vance - DA-1147"
                IdYearLevel = 1, RecordYearLevel = 1,     // unused for staff (no year level)
                IdCourseIndex = 0, RecordCourseIndex = 0, // unused ("District Administration")
                IdIsTransferee = false, RecordIsTransferee = false,
                ExpirationDay = Content.DayNumberOfDate(2027, 3, 31),
                ExpirationDisplay = "2027-03-31",
                WindowPortrait = new Portrait { HairStyle = 2, FaceVariant = 3, Accessory = 0, Distorted = true },
                IdPhoto = badgePhoto,                     // subtle photo defect + distortion at window
                InDatabase = false,                       // badge not in the student registry
                DuplicateOfShift = "",
                RecordFirstClassroom = "- (district personnel)",
                Greeting = Content.AdminLines[0],
            };
            v.Defects.Add(DefectType.RecordNameMismatch);  // badge vs roster memo name
            v.Defects.Add(DefectType.NotInDatabase);       // badge number not on the roster
            v.Defects.Add(DefectType.PhotoSubtleMismatch); // same glitch as the other anomalies
            v.ClassroomAnswer = "My first stop is your server room.";
            for (int q = 0; q < InterviewRules.Count; q++) v.Answers[q] = "That is not your concern.";
            v.Answers[(int)QuestionId.Classroom] = v.ClassroomAnswer;
            v.WindowFaceIndex = -1;                        // the administrator has his own face art
            v.SlipName = v.ClaimedName;
            v.SlipIdNumber = v.IdNumber;
            v.SlipReason = "DISTRICT BUSINESS - SEE OFFICE";
            // the administrator is the EXTRA final entry after the standard roster, so its
            // run-wide index is the roster length - set here, where the identity is built, so
            // every caller (roster generation AND the headless harness) gets a unique index
            v.VisitorIndex = shift.VisitorCount;
            return v;
        }
    }
}
