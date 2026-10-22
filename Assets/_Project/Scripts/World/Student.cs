using Hollowbrook.Bootstrap;
using Hollowbrook.Logic;
using UnityEngine;

namespace Hollowbrook.World
{
    // One admitted visitor inside the classroom placeholder: steps in through the door and
    // walks to the seat SeatPlanner assigned, then sits. Presentation only — the sprite and
    // tint are identical for every student, anomalies included (fairness invariant).
    public class Student : MonoBehaviour
    {
        private const float WalkSpeed = 2.2f;

        public StudentData Data { get; private set; }
        public int Seat { get; private set; }
        public bool Seated { get; private set; }

        private float _seatX;

        public void Setup(StudentData data, int seat, Vector3 door, Vector3 seatPos)
        {
            Data = data;
            Seat = seat;
            Seated = false;
            _seatX = seatPos.x;
            transform.localPosition = new Vector3(door.x, seatPos.y, 0f);
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralArt.Get("custodian_0");   // existing sprite: no new art
            sr.sortingOrder = 9;                            // prop layer: room light applies
        }

        public void Tick(float dt)
        {
            if (Seated) return;
            var p = transform.localPosition;
            p.x = Mathf.MoveTowards(p.x, _seatX, WalkSpeed * dt);
            transform.localPosition = p;
            if (Mathf.Abs(p.x - _seatX) < 0.05f) Seated = true;
        }
    }
}
