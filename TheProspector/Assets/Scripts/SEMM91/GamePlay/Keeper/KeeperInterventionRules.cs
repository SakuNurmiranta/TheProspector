namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Prototype effect policy for light Keeper interventions.
    ///
    /// One unit of committed Pull currently requests one
    /// normalized visibility unit. The conversion is isolated
    /// here so final balancing does not alter the resolver.
    /// </summary>
    public static class KeeperInterventionRules
    {
        public static float
            GetRequestedVisibilityDelta(
                KeeperInterventionType type,
                float pullSpend)
        {
            float magnitude =
                KeeperPullRules.NormalizeAmount(
                    pullSpend
                );

            return type switch
            {
                KeeperInterventionType
                        .BoostVisibility =>
                    magnitude,

                KeeperInterventionType
                        .SuppressVisibility =>
                    -magnitude,

                _ => 0.0f
            };
        }
    }
}