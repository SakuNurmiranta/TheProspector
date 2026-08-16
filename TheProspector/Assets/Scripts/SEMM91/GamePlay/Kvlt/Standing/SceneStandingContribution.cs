using System;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// One provenance-bearing release contribution
    /// to a player's/band's Scene Standing.
    ///
    /// Position is the historical anchor used by
    /// the standing calculation.
    ///
    /// Active Field releases always carry unit weight.
    /// Canon Legacy and Rejection Scar weights are
    /// supplied by later calibrated policy.
    /// </summary>
    public sealed class SceneStandingContribution
    {
        private const float WeightTolerance =
            0.0001f;

        public string SourceSceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public SceneStandingContributionKind
            Kind { get; }

        public float Position { get; }

        public float Weight { get; }

        public int BasisTurn { get; }

        public SceneStandingContribution(
            string sourceSceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            SceneStandingContributionKind kind,
            float position,
            float weight,
            int basisTurn)
        {
            SourceSceneReleaseId =
                RequireText(
                    sourceSceneReleaseId,
                    nameof(sourceSceneReleaseId)
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

            if (!Enum.IsDefined(
                    typeof(
                        SceneStandingContributionKind
                    ),
                    kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind)
                );
            }

            if (!IsFinite(position))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position)
                );
            }

            if (!IsFinite(weight) ||
                weight <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weight),
                    weight,
                    "Scene Standing contribution " +
                    "weight must be finite and positive."
                );
            }

            /*
             * The Bible specifies calibration weights
             * for Canon Legacy and Rejection Scars.
             *
             * Current active Field releases simply
             * contribute their current positions.
             * Therefore their baseline weight is 1.
             */
            if (kind ==
                    SceneStandingContributionKind
                        .ActiveField &&
                Math.Abs(weight - 1f) >
                    WeightTolerance)
            {
                throw new ArgumentException(
                    "Active Field Scene Standing " +
                    "contributions must use unit weight.",
                    nameof(weight)
                );
            }

            if (basisTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(basisTurn)
                );
            }

            Kind =
                kind;

            Position =
                position;

            Weight =
                weight;

            BasisTurn =
                basisTurn;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene Standing provenance " +
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