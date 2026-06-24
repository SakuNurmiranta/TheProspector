namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Result of applying the legacy consequences of one
    /// classified Keeper transition.
    ///
    /// TransitionResult contains the enriched release IDs.
    /// NextTenure is the complete tenure state after resolution.
    /// </summary>
    public readonly struct KeeperLegacyResolution
    {
        public KeeperTransitionResult
            TransitionResult { get; }

        public KeeperTenureState
            NextTenure { get; }

        public KeeperLegacyResolution(
            KeeperTransitionResult
                transitionResult,
            KeeperTenureState nextTenure)
        {
            TransitionResult =
                transitionResult;

            NextTenure =
                nextTenure;
        }
    }
}