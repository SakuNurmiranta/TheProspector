using System;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Historical HailAspect-to-praxis association
    /// already known by the Scene before or during
    /// playable history.
    ///
    /// Peak-2 runtime Happening integration will later
    /// consume these declarations.
    /// </summary>
    public sealed class KvltHistoricalHailPraxisSeed
    {
        public string SeedId { get; }

        public string HailAspectId { get; }

        public string BehaviorTypeId { get; }

        public int EstablishedTurn { get; }

        public KvltHistoricalHailPraxisSeed(
            string seedId,
            string hailAspectId,
            string behaviorTypeId,
            int establishedTurn)
        {
            SeedId =
                RequireText(
                    seedId,
                    nameof(seedId)
                );

            HailAspectId =
                RequireText(
                    hailAspectId,
                    nameof(hailAspectId)
                );

            BehaviorTypeId =
                RequireText(
                    behaviorTypeId,
                    nameof(behaviorTypeId)
                );

            if (establishedTurn < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(establishedTurn)
                );
            }

            EstablishedTurn =
                establishedTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Historical praxis seed identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}