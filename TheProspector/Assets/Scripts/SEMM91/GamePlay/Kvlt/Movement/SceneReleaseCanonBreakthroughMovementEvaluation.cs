using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable release-level reduction of all
    /// independently qualifying realized Canon
    /// Breakthrough claims into one positional
    /// Breakthrough Drift value.
    ///
    /// Every claim tied for the highest realized
    /// differential contributes its full value.
    /// Every lower qualifying claim contributes +1.
    /// </summary>
    public sealed class
        SceneReleaseCanonBreakthroughMovementEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float StartFieldPosition { get; }

        public int QualifyingClaimCount { get; }

        public int HighestDifferential { get; }

        public int HighestDifferentialClaimCount { get; }

        public int LowerQualifyingClaimCount { get; }

        public int ReleaseBreakthroughValue { get; }

        public float BreakthroughDriftMultiplier { get; }

        public float BreakthroughDrift { get; }

        public bool HasBreakthrough =>
            ReleaseBreakthroughValue > 0;

        public SceneReleaseCanonBreakthroughMovementEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float startFieldPosition,
            int qualifyingClaimCount,
            int highestDifferential,
            int highestDifferentialClaimCount,
            int lowerQualifyingClaimCount,
            int releaseBreakthroughValue,
            float breakthroughDriftMultiplier,
            float breakthroughDrift)
        {
            SceneReleaseId =
                RequireText(
                    sceneReleaseId,
                    nameof(sceneReleaseId)
                );

            SourceDemoTapeId =
                RequireText(
                    sourceDemoTapeId,
                    nameof(sourceDemoTapeId)
                );

            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            if (!IsFinite(startFieldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startFieldPosition)
                );
            }

            if (qualifyingClaimCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(qualifyingClaimCount)
                );
            }

            if (highestDifferential < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(highestDifferential)
                );
            }

            if (highestDifferentialClaimCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(highestDifferentialClaimCount)
                );
            }

            if (lowerQualifyingClaimCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lowerQualifyingClaimCount)
                );
            }

            if (releaseBreakthroughValue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseBreakthroughValue)
                );
            }

            if (!IsFinite(
                    breakthroughDriftMultiplier) ||
                breakthroughDriftMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(breakthroughDriftMultiplier)
                );
            }

            if (!IsFinite(breakthroughDrift))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(breakthroughDrift)
                );
            }

            if (highestDifferentialClaimCount +
                lowerQualifyingClaimCount !=
                qualifyingClaimCount)
            {
                throw new ArgumentException(
                    "Breakthrough claim counts do not " +
                    "sum to the qualifying population."
                );
            }

            if (qualifyingClaimCount == 0)
            {
                if (highestDifferential != 0 ||
                    highestDifferentialClaimCount != 0 ||
                    lowerQualifyingClaimCount != 0 ||
                    releaseBreakthroughValue != 0 ||
                    Math.Abs(breakthroughDrift) >
                        0.0001f)
                {
                    throw new ArgumentException(
                        "Empty breakthrough population " +
                        "must produce zero movement."
                    );
                }
            }
            else
            {
                if (highestDifferential <= 0)
                {
                    throw new ArgumentException(
                        "Qualifying breakthrough claims " +
                        "require a positive maximum."
                    );
                }

                if (highestDifferentialClaimCount <= 0)
                {
                    throw new ArgumentException(
                        "Qualifying breakthrough population " +
                        "must contain at least one highest " +
                        "claim."
                    );
                }

                int expectedReleaseValue =
                    highestDifferential *
                    highestDifferentialClaimCount +
                    lowerQualifyingClaimCount;

                if (releaseBreakthroughValue !=
                    expectedReleaseValue)
                {
                    throw new ArgumentException(
                        "Release breakthrough value does " +
                        "not match memorable-breakthrough " +
                        "aggregation."
                    );
                }
            }

            float expectedDrift =
                releaseBreakthroughValue *
                breakthroughDriftMultiplier;

            if (Math.Abs(
                    expectedDrift -
                    breakthroughDrift) >
                0.0001f)
            {
                throw new ArgumentException(
                    "Breakthrough Drift does not match " +
                    "release breakthrough value and " +
                    "configured multiplier."
                );
            }

            SettledTurn =
                settledTurn;

            StartFieldPosition =
                startFieldPosition;

            QualifyingClaimCount =
                qualifyingClaimCount;

            HighestDifferential =
                highestDifferential;

            HighestDifferentialClaimCount =
                highestDifferentialClaimCount;

            LowerQualifyingClaimCount =
                lowerQualifyingClaimCount;

            ReleaseBreakthroughValue =
                releaseBreakthroughValue;

            BreakthroughDriftMultiplier =
                breakthroughDriftMultiplier;

            BreakthroughDrift =
                breakthroughDrift;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Breakthrough movement provenance " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
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