using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // Fills the classroom placeholder with the students the player admitted this shift.
    // Driven by PatrolStarted / PatrolEnded on the EventBus; the seating itself comes from
    // the pure SeatPlanner, so the same seed and the same admitted list always produce the
    // same room - including after save -> resume. Students step in one by one through the
    // door; pausing the game pauses both entry and walking.
    public class StudentSpawner : MonoBehaviour
    {
        private const float EntryGapSeconds = 0.7f;   // one-by-one through the door

        private GameRoot _root;
        private ClassroomPlaceholder _room;
        private readonly List<Student> _students = new List<Student>();
        private readonly List<Student> _queue = new List<Student>();     // waiting at the door
        private readonly List<Student> _walking = new List<Student>();   // on their way to a seat
        private float _nextEntryIn;
        private EventSubscription _subStart;
        private EventSubscription _subEnd;

        public IReadOnlyList<Student> Students => _students;
        public ClassroomPlaceholder Room => _room;
        public int SeatedCount
        {
            get { int n = 0; foreach (var s in _students) if (s != null && s.Seated) n++; return n; }
        }

        public void Init(GameRoot root, ClassroomPlaceholder room)
        {
            _root = root;
            _room = room;
            _subStart = root.Bus.Subscribe(EventIds.PatrolStarted, e => OnPatrolStarted());
            _subEnd = root.Bus.Subscribe(EventIds.PatrolEnded, e => Clear());
        }

        private void OnDestroy()
        {
            if (_root != null && _root.Bus != null)
            {
                if (_subStart != null) _subStart.Dispose();
                if (_subEnd != null) _subEnd.Dispose();
            }
            Clear();
        }

        // Patrol start: everyone stamped ALLOW this shift (anomalies included) takes a seat.
        private void OnPatrolStarted()
        {
            if (_root == null || _root.Session == null) return;
            int seed = _root.Session.Seed ^ (_root.Session.CurrentShift * 7919);
            Spawn(_root.Session.AdmittedStudents, seed);
        }

        /// <summary>Plan and build the room for one admitted list. Safe for empty lists,
        /// duplicate ids and overflow (warn + skip).</summary>
        public void Spawn(List<StudentData> admitted, int seed)
        {
            Clear();
            if (_room == null || admitted == null || admitted.Count == 0) return;

            var ids = new List<string>(admitted.Count);
            foreach (var s in admitted) ids.Add(s != null ? s.Id : null);
            int[] plan = SeatPlanner.Plan(ids, _room.Seats.Length, new GameRng(seed));

            for (int i = 0; i < admitted.Count; i++)
            {
                if (plan[i] < 0)
                {
                    Debug.LogWarning("[Students] no seat left for " + (ids[i] ?? "?") + " - skipped");
                    continue;
                }
                var go = new GameObject("student_" + i);
                go.transform.SetParent(_room.transform, false);
                var st = go.AddComponent<Student>();
                st.Setup(admitted[i], plan[i], _room.Door, _room.Seats[plan[i]]);
                go.SetActive(false);                 // waits at the door until its turn
                _students.Add(st);
                _queue.Add(st);
            }
            _nextEntryIn = 0f;
        }

        public void Clear()
        {
            foreach (var s in _students) if (s != null) Destroy(s.gameObject);
            _students.Clear();
            _queue.Clear();
            _walking.Clear();
        }

        // One headless-or-live step. Pause freezes entry and walking alike.
        public void Tick(float dt)
        {
            if (_root != null && _root.Paused) return;
            if (_queue.Count > 0)
            {
                _nextEntryIn -= dt;
                if (_nextEntryIn <= 0f)
                {
                    var next = _queue[0];
                    _queue.RemoveAt(0);
                    if (next != null)
                    {
                        next.gameObject.SetActive(true);
                        _walking.Add(next);
                    }
                    _nextEntryIn = EntryGapSeconds;
                }
            }
            for (int i = _walking.Count - 1; i >= 0; i--)
            {
                if (_walking[i] == null) { _walking.RemoveAt(i); continue; }
                _walking[i].Tick(dt);
                if (_walking[i].Seated) _walking.RemoveAt(i);
            }
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
