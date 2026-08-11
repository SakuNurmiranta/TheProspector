using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Pure outer-boundary interpretation.
    ///
    /// This evaluator never mutates SceneRelease
    /// lifecycle or position.
    /// </summary>
    public sealed class
        SceneReleaseOuterBoundaryEvaluator
    {
        public SceneReleaseOuterBoundaryEvaluation
            Evaluate(
                SceneRelease release,
                float outerBoundary,
                int settledTurn)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Only current Field releases can " +
                    "be evaluated against the outer " +
                    "boundary.",
                    nameof(release)
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new ArgumentException(
                    "Outer-boundary evaluation requires " +
                    "an established Field Position.",
                    nameof(release)
                );
            }

            if (float.IsNaN(outerBoundary) ||
                float.IsInfinity(outerBoundary))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerBoundary)
                );
            }

            if (settledTurn <
                release.FieldPositionState
                    .EstablishedTurn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            /*
             * Do not interpret a historical turn using
             * position that already contains movement
             * from a later turn.
             */
            if (release.FieldPositionState
                    .LastMovementTurn >
                settledTurn)
            {
                throw new ArgumentException(
                    "Outer-boundary evaluation turn " +
                    "predates authoritative movement."
                );
            }

            float position =
                release.FieldPositionState
                    .CurrentPosition;

            SceneReleaseOuterBoundaryDisposition
                disposition =
                    position <
                    outerBoundary
                        ? SceneReleaseOuterBoundaryDisposition
                            .Rejected
                        : SceneReleaseOuterBoundaryDisposition
                            .RemainsField;

            return new
                SceneReleaseOuterBoundaryEvaluation(
                    release.ReleaseId,
                    release.SourceDemoTapeId,
                    release.SourceOwnerEntityId,
                    release.HostedSceneNodeId,
                    settledTurn,
                    outerBoundary,
                    position,
                    release.FieldPositionState
                        .PeakInwardPosition,
                    disposition
                );
        }
    }
}