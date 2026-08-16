using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable proof that one outer-boundary
    /// evaluation produced a terminal rejection.
    /// </summary>
    public sealed class
        SceneReleaseOuterBoundaryApplication
    {
        private const float Tolerance =
            0.0001f;

        public SceneReleaseOuterBoundaryEvaluation
            Evaluation { get; }

        public SceneReleaseRejectionState
            RejectionState { get; }

        public string SceneReleaseId =>
            Evaluation.SceneReleaseId;

        public int SettledTurn =>
            Evaluation.SettledTurn;

        public SceneReleaseOuterBoundaryApplication(
            SceneReleaseOuterBoundaryEvaluation
                evaluation,
            SceneReleaseRejectionState
                rejectionState)
        {
            Evaluation =
                evaluation ??
                throw new ArgumentNullException(
                    nameof(evaluation)
                );

            RejectionState =
                rejectionState ??
                throw new ArgumentNullException(
                    nameof(rejectionState)
                );

            if (!evaluation.CrossedOuterBoundary)
            {
                throw new ArgumentException(
                    "Boundary application requires a " +
                    "Rejected evaluation.",
                    nameof(evaluation)
                );
            }

            if (rejectionState.RejectedTurn !=
                evaluation.SettledTurn)
            {
                throw new ArgumentException(
                    "Rejection turn does not match " +
                    "boundary evaluation."
                );
            }

            if (!NearlyEqual(
                    rejectionState.OuterBoundary,
                    evaluation.OuterBoundary) ||
                !NearlyEqual(
                    rejectionState.RejectedPosition,
                    evaluation.FieldPosition) ||
                !NearlyEqual(
                    rejectionState.PeakInwardPosition,
                    evaluation.PeakInwardPosition))
            {
                throw new ArgumentException(
                    "Rejection history does not match " +
                    "the frozen boundary evaluation."
                );
            }
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(left - right) <=
                Tolerance;
        }
    }
}