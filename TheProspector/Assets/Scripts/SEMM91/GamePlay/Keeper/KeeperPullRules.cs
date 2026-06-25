namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Numeric policy for the Keeper's tenure-owned Pull resource.
    ///
    /// Pull currently maps directly from the influence proxy.
    /// Keeping the conversion here allows the final influence
    /// system to replace that policy without rewriting tenure,
    /// interventions, UI, or networking.
    /// </summary>
    public static class KeeperPullRules
    {
        public const float ComparisonTolerance =
            0.0001f;

        public static float FromInfluence(
            float influence)
        {
            return NormalizeAmount(
                influence
            );
        }

        public static float NormalizeAmount(
            float amount)
        {
            if (float.IsNaN(amount) ||
                float.IsInfinity(amount) ||
                amount <= 0.0f)
            {
                return 0.0f;
            }

            return amount;
        }

        public static bool IsValidCost(
            float cost)
        {
            return
                !float.IsNaN(cost) &&
                !float.IsInfinity(cost) &&
                cost >
                ComparisonTolerance;
        }

        public static bool CanAfford(
            float availablePull,
            float cost)
        {
            if (!IsValidCost(cost))
                return false;

            float normalizedAvailable =
                NormalizeAmount(
                    availablePull
                );

            return
                normalizedAvailable +
                ComparisonTolerance >=
                cost;
        }
        
        public static float GetTransitionGrant(
            KeeperTransitionReason reason,
            float influence)
        {
            switch (reason)
            {
                case KeeperTransitionReason
                    .InitialAssignment:

                case KeeperTransitionReason
                    .YearEndRetained:

                case KeeperTransitionReason
                    .YearEndReplaced:

                    return FromInfluence(
                        influence
                    );

                case KeeperTransitionReason
                    .DisconnectionFallback:

                case KeeperTransitionReason
                    .SceneCollapseLocked:

                case KeeperTransitionReason.None:

                default:

                    return 0.0f;
            }
        }
        
        public static KeeperInterventionFailureReason
            ValidateInterventionRequest(
                KeeperInterventionRequest request)
        {
            if (request.KeeperClientId ==
                ulong.MaxValue)
            {
                return KeeperInterventionFailureReason
                    .InvalidKeeper;
            }

            if (string.IsNullOrWhiteSpace(
                    request.ReleaseId
                ))
            {
                return KeeperInterventionFailureReason
                    .InvalidRelease;
            }

            if (request.RequestedTurn < 0)
            {
                return KeeperInterventionFailureReason
                    .InvalidTurn;
            }

            if (!IsValidCost(
                    request.PullSpend
                ))
            {
                return KeeperInterventionFailureReason
                    .InvalidPullSpend;
            }

            if (request.InterventionType !=
                KeeperInterventionType
                    .BoostVisibility &&
                request.InterventionType !=
                KeeperInterventionType
                    .SuppressVisibility)
            {
                return KeeperInterventionFailureReason
                    .UnsupportedIntervention;
            }

            return KeeperInterventionFailureReason.None;
        }
    }
}