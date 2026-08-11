using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Immutable proof that one frozen movement
    /// evaluation was applied to authoritative
    /// SceneRelease Field Position.
    /// </summary>
    public sealed class SceneReleaseMovementApplication
    {
        private const float Tolerance =
            0.0001f;

        public SceneReleaseMovementEvaluation
            Evaluation { get; }

        public SceneReleaseFieldPositionTransition
            PositionTransition { get; }

        public string SceneReleaseId =>
            Evaluation.SceneReleaseId;

        public int SettledTurn =>
            Evaluation.SettledTurn;

        public SceneReleaseMovementApplication(
            SceneReleaseMovementEvaluation evaluation,
            SceneReleaseFieldPositionTransition
                positionTransition)
        {
            Evaluation =
                evaluation ??
                throw new ArgumentNullException(
                    nameof(evaluation)
                );

            PositionTransition =
                positionTransition ??
                throw new ArgumentNullException(
                    nameof(positionTransition)
                );

            if (positionTransition.Kind !=
                SceneReleaseFieldPositionTransitionKind
                    .SettlementMovement)
            {
                throw new ArgumentException(
                    "Movement application requires a " +
                    "settlement movement transition.",
                    nameof(positionTransition)
                );
            }

            if (positionTransition.GlobalTurn !=
                evaluation.SettledTurn)
            {
                throw new ArgumentException(
                    "Movement application turn mismatch.",
                    nameof(positionTransition)
                );
            }

            if (!positionTransition
                    .PreviousPosition.HasValue)
            {
                throw new ArgumentException(
                    "Movement application requires a " +
                    "previous Field Position.",
                    nameof(positionTransition)
                );
            }

            if (!NearlyEqual(
                    positionTransition
                        .PreviousPosition.Value,
                    evaluation.StartFieldPosition))
            {
                throw new ArgumentException(
                    "Applied movement started from a " +
                    "different Field Position.",
                    nameof(positionTransition)
                );
            }

            if (!NearlyEqual(
                    positionTransition.AppliedDelta,
                    evaluation.TotalDelta))
            {
                throw new ArgumentException(
                    "Applied movement delta does not " +
                    "match frozen evaluation.",
                    nameof(positionTransition)
                );
            }

            if (!NearlyEqual(
                    positionTransition.NewPosition,
                    evaluation
                        .ProjectedFieldPosition))
            {
                throw new ArgumentException(
                    "Applied Field Position does not " +
                    "match frozen projection.",
                    nameof(positionTransition)
                );
            }
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(
                    left - right
                ) <=
                Tolerance;
        }
    }
}