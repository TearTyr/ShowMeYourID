namespace Hollowbrook.Core
{
    // The one place the build's presentation cuts are owned. Each flag is a product
    // decision, not a workaround: false means the feature's UI is never BUILT at all
    // (its logic may still run — the arrival queue keeps queuing behind the scenes).
    // Flip a flag here and every build/refresh path for it lights up again; no other
    // file knows about the cut, and nothing is ever built-then-hidden.
    public static class ShowcaseBuild
    {
        /// <summary>Waiting-line silhouettes and the banner's QUEUE/CURRENT readout.
        /// The queue itself always runs; only its desk display rides on this flag.</summary>
        public const bool LineDisplay = false;
    }
}
