using UnityEngine;

namespace Hollowbrook.World
{
    // Placeholder furnishing for the Classroom 1 student row: a door anchor plus distinct
    // seat spots on the floor band, one flat desk quad each (same Quad convention as every
    // other placeholder). Built by the school bootstrap; the spawner only reads anchors.
    public class ClassroomPlaceholder : MonoBehaviour
    {
        public const int SeatCount = 6;

        public Vector3 Door;
        public Vector3[] Seats;

        public static ClassroomPlaceholder Build(Transform roomRoot, float floorY, float doorX)
        {
            var go = new GameObject("ClassroomPlaceholder");
            go.transform.SetParent(roomRoot, false);
            var ph = go.AddComponent<ClassroomPlaceholder>();
            ph.Door = new Vector3(doorX, floorY, 0f);
            ph.Seats = new Vector3[SeatCount];
            for (int i = 0; i < SeatCount; i++)
            {
                float x = -6.5f + i * 1.6f;   // six distinct spots along the floor band
                ph.Seats[i] = new Vector3(x, floorY, 0f);
                RoomBuilder.Quad(go.transform, "desk_" + i, new Vector3(x, floorY + 0.35f, 0), 9,
                    new Vector2(0.9f, 0.7f), new Color(0.16f, 0.16f, 0.18f));
            }
            return ph;
        }
    }
}
