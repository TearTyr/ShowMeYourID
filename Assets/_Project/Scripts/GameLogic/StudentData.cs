namespace Hollowbrook.Logic
{
    /// <summary>One admitted visitor as the classroom spawner needs them: pure data, no
    /// engine types, so seating stays headless-testable. IsAnomaly must never change how a
    /// student looks (fairness invariant) — it only books who may appear.</summary>
    public class StudentData
    {
        public string Id;
        public string Name;
        public bool IsAnomaly;
    }
}
