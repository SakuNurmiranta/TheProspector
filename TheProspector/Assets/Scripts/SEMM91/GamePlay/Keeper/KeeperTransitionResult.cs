namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Immutable description of one resolved Keeper transition.
    ///
    /// Legacy and canonization IDs are present now as extension
    /// points. Step 9.7B will begin populating them.
    /// </summary>
    public readonly struct KeeperTransitionResult
    {
        public bool HasResult { get; }

        public int ResolvedRound { get; }
        public KeeperTransitionReason Reason { get; }

        public ulong PreviousKeeperClientId { get; }
        public ulong NextKeeperClientId { get; }

        public string PreviousSubjectReleaseId { get; }
        public string CanonizedReleaseId { get; }
        public string IncomingSubjectReleaseId { get; }

        public float WinningSceneOutput { get; }
        public float InitialPull { get; }

        public bool KeeperChanged =>
            HasResult &&
            PreviousKeeperClientId !=
            NextKeeperClientId;

        public bool HasAssignedKeeper =>
            HasResult &&
            NextKeeperClientId != ulong.MaxValue;

        public static KeeperTransitionResult None =>
            default;

        public KeeperTransitionResult(
            int resolvedRound,
            KeeperTransitionReason reason,
            ulong previousKeeperClientId,
            ulong nextKeeperClientId,
            string previousSubjectReleaseId,
            string canonizedReleaseId,
            string incomingSubjectReleaseId,
            float winningSceneOutput,
            float initialPull)
        {
            HasResult = true;

            ResolvedRound = resolvedRound;
            Reason = reason;

            PreviousKeeperClientId =
                previousKeeperClientId;

            NextKeeperClientId =
                nextKeeperClientId;

            PreviousSubjectReleaseId =
                previousSubjectReleaseId ??
                string.Empty;

            CanonizedReleaseId =
                canonizedReleaseId ??
                string.Empty;

            IncomingSubjectReleaseId =
                incomingSubjectReleaseId ??
                string.Empty;

            WinningSceneOutput =
                winningSceneOutput;

            InitialPull =
                KeeperPullRules.NormalizeAmount(
                    initialPull
                );
        }
        
        public KeeperTransitionResult WithLegacy(
            string previousSubjectReleaseId,
            string canonizedReleaseId,
            string incomingSubjectReleaseId)
        {
            if (!HasResult)
                return this;

            return new KeeperTransitionResult(
                ResolvedRound,
                Reason,
                PreviousKeeperClientId,
                NextKeeperClientId,
                previousSubjectReleaseId,
                canonizedReleaseId,
                incomingSubjectReleaseId,
                WinningSceneOutput,
                InitialPull
            );
        }
        
        public KeeperTransitionResult WithInitialPull(
            float initialPull)
        {
            if (!HasResult)
                return this;

            return new KeeperTransitionResult(
                ResolvedRound,
                Reason,
                PreviousKeeperClientId,
                NextKeeperClientId,
                PreviousSubjectReleaseId,
                CanonizedReleaseId,
                IncomingSubjectReleaseId,
                WinningSceneOutput,
                initialPull
            );
        }
    }
}