using System;

namespace Hollowbrook.Logic
{
    public sealed class GameRng
    {
        private readonly Random _rand;
        public int Seed { get; }

        public GameRng(int seed)
        {
            Seed = seed;
            _rand = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _rand.Next(minInclusive, maxExclusive);
        }

        public float NextFloat() => (float)_rand.NextDouble();

        public bool Chance(float probability) => NextFloat() < probability;

        public T Pick<T>(T[] items)
        {
            if (items == null || items.Length == 0)
                throw new ArgumentException("Pick called with empty collection");
            return items[_rand.Next(items.Length)];
        }

        public void Shuffle<T>(System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rand.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
