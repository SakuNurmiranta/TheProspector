using System;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable interpretation of one Field release's
    /// current position against one calibrated outer
    /// boundary.
    /// </summary>
    public sealed class
        SceneReleaseOuterBoundaryEvaluation
    {
        public string SceneReleaseId { get; }

        public string SourceDemoTapeId { get; }

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public float OuterBoundary { get; }

        public float FieldPosition { get; }

        public float PeakInwardPosition { get; }

        public float SignedDistanceFromBoundary { get; }

        public float OutwardOvershoot { get; }

        public SceneReleaseOuterBoundaryDisposition
            Disposition { get; }

        public bool CrossedOuterBoundary =>
            Disposition ==
            SceneReleaseOuterBoundaryDisposition
                .Rejected;

        public SceneReleaseOuterBoundaryEvaluation(
            string sceneReleaseId,
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            float outerBoundary,
            float fieldPosition,
            float peakInwardPosition,
            SceneReleaseOuterBoundaryDisposition
                disposition)
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

            if (!IsFinite(outerBoundary) ||
                !IsFinite(fieldPosition) ||
                !IsFinite(peakInwardPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fieldPosition),
                    "Boundary evaluation values " +
                    "must be finite."
                );
            }

            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseOuterBoundaryDisposition
                    ),
                    disposition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(disposition)
                );
            }

            bool actuallyCrossed =
                fieldPosition <
                outerBoundary;

            if (actuallyCrossed !=
                (
                    disposition ==
                    SceneReleaseOuterBoundaryDisposition
                        .Rejected
                ))
            {
                throw new ArgumentException(
                    "Outer-boundary disposition does " +
                    "not match the supplied position."
                );
            }

            SettledTurn =
                settledTurn;

            OuterBoundary =
                outerBoundary;

            FieldPosition =
                fieldPosition;

            PeakInwardPosition =
                peakInwardPosition;

            SignedDistanceFromBoundary =
                fieldPosition -
                outerBoundary;

            OutwardOvershoot =
                actuallyCrossed
                    ? outerBoundary -
                      fieldPosition
                    : 0f;

            Disposition =
                disposition;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Outer-boundary provenance " +
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