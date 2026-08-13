namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Immutable description of one resolved Keeper
    /// transition.
    ///
    /// WinningInfluence is the Peak-2 term for the
    /// completed-year influence value which won or
    /// retained the office.
    ///
    /// WinningSceneOutput remains as a compatibility
    /// property for the earlier vertical-slice API.
    /// </summary>
    public readonly struct KeeperTransitionResult
    {
        public bool HasResult { get; }

        public int ResolvedRound { get; }

        public KeeperTransitionReason Reason { get; }

        public ulong PreviousKeeperClientId { get; }

        public ulong NextKeeperClientId { get; }

        public string PreviousSubjectReleaseId
        {
            get;
        }

        public string CanonizedReleaseId { get; }

        public string IncomingSubjectReleaseId
        {
            get;
        }

        /*
         * Legacy storage/property name.
         *
         * Peak-2 runtime now places YearInfluence in
         * this value.
         */
        public float WinningSceneOutput { get; }

        public float WinningInfluence =>
            WinningSceneOutput;

        public float PullGrant { get; }

        public bool KeeperChanged =>
            HasResult &&
            PreviousKeeperClientId !=
            NextKeeperClientId;

        public bool HasAssignedKeeper =>
            HasResult &&
            NextKeeperClientId !=
            ulong.MaxValue;

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
            float pullGrant)
        {
            HasResult =
                true;

            ResolvedRound =
                resolvedRound;

            Reason =
                reason;

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

            PullGrant =
                KeeperPullRules.NormalizeAmount(
                    pullGrant
                );
        }

        public KeeperTransitionResult WithLegacy(
            string previousSubjectReleaseId,
            string canonizedReleaseId,
            string incomingSubjectReleaseId)
        {
            if (!HasResult)
            {
                return this;
            }

            return new KeeperTransitionResult(
                ResolvedRound,
                Reason,
                PreviousKeeperClientId,
                NextKeeperClientId,
                previousSubjectReleaseId,
                canonizedReleaseId,
                incomingSubjectReleaseId,
                WinningSceneOutput,
                PullGrant
            );
        }

        public KeeperTransitionResult WithPullGrant(
            float pullGrant)
        {
            if (!HasResult)
            {
                return this;
            }

            return new KeeperTransitionResult(
                ResolvedRound,
                Reason,
                PreviousKeeperClientId,
                NextKeeperClientId,
                PreviousSubjectReleaseId,
                CanonizedReleaseId,
                IncomingSubjectReleaseId,
                WinningSceneOutput,
                pullGrant
            );
        }
    }
}