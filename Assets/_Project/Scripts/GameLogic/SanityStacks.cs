namespace Hollowbrook.Logic
{
    /// <summary>The drain "stacks" a wrong decision puts on the player's sanity. Every wrong call
    /// adds a stack (up to <see cref="MaxStacks"/>) and every stack level has its own timer; while
    /// any stacks are up, every sanity LOSS is scaled by the level's multiplier. The timer is
    /// refreshed by each new stack, and when it runs out the stacks clear together - so a
    /// mistake is never permanent, but piling them up keeps the drain high for longer.
    /// Pure: unit-tested, the session owns one instance.</summary>
    public sealed class SanityStacks
    {
        public const int MaxStacks = 3;

        // index = stacks up. 0 stacks is the slow baseline; 3 is the cap. Seconds are how long
        // that level lasts after the latest stack: the bare minimum (one stack) is short, the
        // maximum is half a minute.
        private static readonly float[] LevelMultiplier = { 0.85f, 1.25f, 1.75f, 2.25f };
        private static readonly float[] LevelSeconds = { 0f, 10f, 20f, 30f };

        /// <summary>Stacks currently up (0..MaxStacks).</summary>
        public int Count { get; private set; }
        /// <summary>Seconds until the stacks clear (0 when there are none).</summary>
        public float SecondsLeft { get; private set; }

        /// <summary>The factor every sanity loss is multiplied by right now.</summary>
        public float Multiplier => LevelMultiplier[Count];

        /// <summary>Seconds a given stack count lasts after its latest stack.</summary>
        public static float SecondsFor(int stacks) =>
            LevelSeconds[stacks < 0 ? 0 : (stacks > MaxStacks ? MaxStacks : stacks)];

        /// <summary>The multiplier for a given stack count.</summary>
        public static float MultiplierFor(int stacks) =>
            LevelMultiplier[stacks < 0 ? 0 : (stacks > MaxStacks ? MaxStacks : stacks)];

        /// <summary>A wrong decision: one more stack (capped), timer restarted for the new level.</summary>
        public void Add()
        {
            if (Count < MaxStacks) Count++;
            SecondsLeft = LevelSeconds[Count];
        }

        /// <summary>Advance the timer. A non-positive dt does nothing; at zero the stacks clear.</summary>
        public void Tick(float dt)
        {
            if (Count == 0 || dt <= 0f) return;
            SecondsLeft -= dt;
            if (SecondsLeft <= 0f) Clear();
        }

        /// <summary>Drop every stack (a new shift starts clean).</summary>
        public void Clear()
        {
            Count = 0;
            SecondsLeft = 0f;
        }
    }
}
