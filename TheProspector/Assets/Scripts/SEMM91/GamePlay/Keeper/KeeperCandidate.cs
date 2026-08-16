namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// One active player's candidacy for Keeper
    /// selection.
    ///
    /// Peak-2 succession uses completed-year
    /// YearInfluence as the primary competitive value.
    ///
    /// Scene Standing is a challenger tie-break.
    ///
    /// IsEligible is the integration seam for states
    /// such as active Poserdom. The Keeper resolver
    /// does not need to know why a candidate is
    /// institutionally ineligible.
    ///
    /// SceneOutput remains as a compatibility alias
    /// for the earlier vertical-slice API.
    /// </summary>
    public readonly struct KeeperCandidate
    {
        public ulong ClientId { get; }

        public string OwnerEntityId { get; }

        public float YearInfluence { get; }

        public float? SceneStanding { get; }

        public bool HasSceneStanding =>
            SceneStanding.HasValue;

        public bool IsEligible { get; }

        /*
         * Compatibility alias.
         *
         * Existing callers/tests built before explicit
         * YearInfluence can continue compiling while
         * runtime wiring migrates to the final term.
         */
        public float SceneOutput =>
            YearInfluence;

        public KeeperCandidate(
            ulong clientId,
            string ownerEntityId,
            float sceneOutput)
            : this(
                clientId,
                ownerEntityId,
                yearInfluence:
                    sceneOutput,
                sceneStanding:
                    null,
                isEligible:
                    true
            )
        {
        }

        public KeeperCandidate(
            ulong clientId,
            string ownerEntityId,
            float yearInfluence,
            float? sceneStanding,
            bool isEligible)
        {
            ClientId =
                clientId;

            OwnerEntityId =
                ownerEntityId ??
                string.Empty;

            YearInfluence =
                NormalizeInfluence(
                    yearInfluence
                );

            SceneStanding =
                NormalizeStanding(
                    sceneStanding
                );

            IsEligible =
                isEligible;
        }

        private static float NormalizeInfluence(
            float value)
        {
            if (float.IsNaN(value) ||
                float.IsInfinity(value) ||
                value <= 0.0f)
            {
                return 0.0f;
            }

            return value;
        }

        private static float? NormalizeStanding(
            float? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            if (float.IsNaN(value.Value) ||
                float.IsInfinity(value.Value))
            {
                return null;
            }

            return value.Value;
        }
    }
}