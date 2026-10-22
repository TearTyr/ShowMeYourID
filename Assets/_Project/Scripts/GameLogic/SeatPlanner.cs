using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Assigns distinct seats to student ids: a GameRng-shuffled seat order handed
    /// out in list order. First occurrence of a duplicate id keeps its seat (same student,
    /// one body); ids beyond the seat count get -1 and the spawner warns and skips them.
    /// Pure and deterministic: same ids + same seat count + same rng state = same plan.</summary>
    public static class SeatPlanner
    {
        public static int[] Plan(IList<string> ids, int seatCount, GameRng rng)
        {
            int n = ids != null ? ids.Count : 0;
            var plan = new int[n];
            for (int i = 0; i < n; i++) plan[i] = -1;
            if (n == 0 || seatCount <= 0 || rng == null) return plan;

            // Fisher-Yates over the seat indices with the seeded rng
            var order = new List<int>(seatCount);
            for (int s = 0; s < seatCount; s++) order.Add(s);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }

            var seatOf = new Dictionary<string, int>();
            int next = 0;
            for (int i = 0; i < n; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (seatOf.TryGetValue(id, out int seat)) { plan[i] = seat; continue; }
                if (next >= order.Count) continue;          // overflow: skipped (-1)
                plan[i] = order[next++];
                seatOf[id] = plan[i];
            }
            return plan;
        }
    }
}
