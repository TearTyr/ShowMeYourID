namespace Hollowbrook.Logic
{
    /// <summary>One admitted visitor as the classroom spawner needs them: pure data, no
    /// engine types, so seating stays headless-testable. IsAnomaly must never change how a
    /// student looks (fairness invariant) - it only books who may appear.</summary>
    public class StudentData
    {
        public string Id;
        public string Name;
        public bool IsAnomaly;
        /// <summary>Roster index of the admitted visitor (-1 when unknown): the save file
        /// stores these so the classroom seats the same students after a resume.</summary>
        public int VisitorIndex = -1;
    }
}
