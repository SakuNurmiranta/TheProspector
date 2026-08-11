using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Applies the Peak-2 memorable-breakthrough
    /// aggregation rule to independently qualifying
    /// realized Canon Breakthrough claims.
    /// </summary>
    public sealed class
        SceneReleaseCanonBreakthroughMovementEvaluator
    {
        public
            SceneReleaseCanonBreakthroughMovementEvaluation
            Evaluate(
                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough,
                float breakthroughDriftMultiplier)
        {
            if (breakthrough == null)
            {
                throw new ArgumentNullException(
                    nameof(breakthrough)
                );
            }

            if (float.IsNaN(
                    breakthroughDriftMultiplier) ||
                float.IsInfinity(
                    breakthroughDriftMultiplier) ||
                breakthroughDriftMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(breakthroughDriftMultiplier)
                );
            }

            int qualifyingCount =
                breakthrough.QualifyingClaims.Count;

            if (qualifyingCount == 0)
            {
                return new
                    SceneReleaseCanonBreakthroughMovementEvaluation(
                        breakthrough.SceneReleaseId,
                        breakthrough.SourceDemoTapeId,
                        breakthrough.SourceOwnerEntityId,
                        breakthrough.SceneId,
                        breakthrough.SettledTurn,
                        breakthrough.StartFieldPosition,
                        0,
                        0,
                        0,
                        0,
                        0,
                        breakthroughDriftMultiplier,
                        0f
                    );
            }

            int highest =
                0;

            foreach (
                SceneReleaseCanonBreakthroughClaim claim
                in breakthrough.QualifyingClaims)
            {
                if (!claim.IsQualifyingBreakthrough)
                {
                    throw new InvalidOperationException(
                        "Qualifying breakthrough collection " +
                        "contains a non-qualifying claim."
                    );
                }

                if (claim
                        .RealizedBreakthroughDifferential >
                    highest)
                {
                    highest =
                        claim
                            .RealizedBreakthroughDifferential;
                }
            }

            int highestCount =
                0;

            int lowerCount =
                0;

            foreach (
                SceneReleaseCanonBreakthroughClaim claim
                in breakthrough.QualifyingClaims)
            {
                if (claim
                        .RealizedBreakthroughDifferential ==
                    highest)
                {
                    highestCount++;
                }
                else
                {
                    lowerCount++;
                }
            }

            /*
             * Peak-2 memorable-breakthrough rule:
             *
             * - every co-highest claim contributes its
             *   full differential;
             *
             * - every lower qualifying breakthrough
             *   contributes +1.
             */
            int releaseBreakthroughValue =
                highest *
                highestCount +
                lowerCount;

            float breakthroughDrift =
                releaseBreakthroughValue *
                breakthroughDriftMultiplier;

            if (float.IsNaN(breakthroughDrift) ||
                float.IsInfinity(breakthroughDrift))
            {
                throw new InvalidOperationException(
                    "Breakthrough Drift overflowed " +
                    "finite movement range."
                );
            }

            return new
                SceneReleaseCanonBreakthroughMovementEvaluation(
                    breakthrough.SceneReleaseId,
                    breakthrough.SourceDemoTapeId,
                    breakthrough.SourceOwnerEntityId,
                    breakthrough.SceneId,
                    breakthrough.SettledTurn,
                    breakthrough.StartFieldPosition,
                    qualifyingCount,
                    highest,
                    highestCount,
                    lowerCount,
                    releaseBreakthroughValue,
                    breakthroughDriftMultiplier,
                    breakthroughDrift
                );
        }
    }
}