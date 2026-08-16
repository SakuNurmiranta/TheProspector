using System;

namespace SEMM91.GamePlay.Kvlt.Scenario
{
    /// <summary>
    /// Scenario-owned calibration of the Peak-2 Pull
    /// economy.
    ///
    /// Pull remains a persistent clout/scheming
    /// resource and is separate from YearInfluence.
    /// </summary>
    public sealed class KvltPullEconomyProfile
    {
        public float GravityDripRate { get; }

        public float WinningHailGrant { get; }

        public float SuccessfulHappeningGrant { get; }

        public float PullRegenRate { get; }

        public float ReleaseStabilizationCost { get; }

        public float SceneDamageHealCost { get; }

        public float PoserClearCost { get; }

        public float PoserAccusationCost { get; }

        public float
            FailedAccusationCompensationCost { get; }

        public KvltPullEconomyProfile(
            float gravityDripRate,
            float winningHailGrant,
            float successfulHappeningGrant,
            float pullRegenRate,
            float releaseStabilizationCost,
            float sceneDamageHealCost,
            float poserClearCost,
            float poserAccusationCost,
            float failedAccusationCompensationCost)
        {
            RequireNonNegative(
                gravityDripRate,
                nameof(gravityDripRate)
            );

            RequireNonNegative(
                winningHailGrant,
                nameof(winningHailGrant)
            );

            RequireNonNegative(
                successfulHappeningGrant,
                nameof(successfulHappeningGrant)
            );

            if (!IsFinite(pullRegenRate) ||
                pullRegenRate < 0f ||
                pullRegenRate > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pullRegenRate)
                );
            }

            RequirePositive(
                releaseStabilizationCost,
                nameof(releaseStabilizationCost)
            );

            RequirePositive(
                sceneDamageHealCost,
                nameof(sceneDamageHealCost)
            );

            RequirePositive(
                poserClearCost,
                nameof(poserClearCost)
            );

            RequirePositive(
                poserAccusationCost,
                nameof(poserAccusationCost)
            );

            RequireNonNegative(
                failedAccusationCompensationCost,
                nameof(
                    failedAccusationCompensationCost
                )
            );

            GravityDripRate =
                gravityDripRate;

            WinningHailGrant =
                winningHailGrant;

            SuccessfulHappeningGrant =
                successfulHappeningGrant;

            PullRegenRate =
                pullRegenRate;

            ReleaseStabilizationCost =
                releaseStabilizationCost;

            SceneDamageHealCost =
                sceneDamageHealCost;

            PoserClearCost =
                poserClearCost;

            PoserAccusationCost =
                poserAccusationCost;

            FailedAccusationCompensationCost =
                failedAccusationCompensationCost;
        }

        public float CalculatePassiveRegen(
            float remainingPull)
        {
            RequireNonNegative(
                remainingPull,
                nameof(remainingPull)
            );

            return
                remainingPull *
                PullRegenRate;
        }

        private static void RequirePositive(
            float value,
            string parameterName)
        {
            if (!IsFinite(value) ||
                value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static void RequireNonNegative(
            float value,
            string parameterName)
        {
            if (!IsFinite(value) ||
                value < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName
                );
            }
        }

        private static bool IsFinite(
            float value)
        {
            return
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}