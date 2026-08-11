using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Pure post-movement Nexus-boundary
    /// interpretation.
    ///
    /// A release becomes a Canon candidate only when:
    ///
    /// 1. it remains an active Field release;
    /// 2. movement for this settlement has completed;
    /// 3. it has reached the Nexus boundary; and
    /// 4. its same-turn frozen breakthrough evaluation
    ///    contains qualifying realized novelty.
    ///
    /// No lifecycle or Canon mutation occurs here.
    /// </summary>
    public sealed class
        SceneReleaseNexusBoundaryEvaluator
    {
        private const float PositionTolerance =
            0.0001f;

        public SceneReleaseNexusBoundaryEvaluation
            Evaluate(
                SceneRelease release,
                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough,
                float nexusBoundary)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (breakthrough == null)
            {
                throw new ArgumentNullException(
                    nameof(breakthrough)
                );
            }

            if (float.IsNaN(nexusBoundary) ||
                float.IsInfinity(nexusBoundary))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nexusBoundary)
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new ArgumentException(
                    "Only current Field releases can " +
                    "be interpreted against the Nexus.",
                    nameof(release)
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new ArgumentException(
                    "Nexus-boundary evaluation requires " +
                    "an established Field Position.",
                    nameof(release)
                );
            }

            ValidateProvenance(
                release,
                breakthrough
            );

            SceneReleaseFieldPositionState position =
                release.FieldPositionState;

            /*
             * Boundary resolution belongs after the
             * simultaneous movement pass.
             *
             * Qualifying breakthrough releases are
             * part of the active-TRVE movement
             * population, including lone/zero-drift
             * cases, so a settlement movement
             * transition should exist even when its
             * applied delta was zero.
             */
            if (position.LastMovementTurn !=
                breakthrough.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Nexus-boundary evaluation requires " +
                    "movement to have settled for the " +
                    "same turn as Canon Breakthrough."
                );
            }

            SceneReleaseFieldPositionTransition
                movement =
                    FindSettlementMovement(
                        position,
                        breakthrough.SettledTurn
                    );

            /*
             * This proves that the breakthrough truth
             * and movement calculation originated from
             * the same frozen pre-movement position.
             */
            if (!movement.PreviousPosition.HasValue ||
                !NearlyEqual(
                    movement.PreviousPosition.Value,
                    breakthrough.StartFieldPosition))
            {
                throw new InvalidOperationException(
                    "Canon Breakthrough evaluation does " +
                    "not match the frozen Field Position " +
                    "used by settlement movement."
                );
            }

            if (!NearlyEqual(
                    movement.NewPosition,
                    position.CurrentPosition))
            {
                throw new InvalidOperationException(
                    "Nexus-boundary evaluation detected " +
                    "Field Position mutation after the " +
                    "settlement movement transition."
                );
            }

            float currentPosition =
                position.CurrentPosition;

            bool reachedNexus =
                currentPosition >=
                nexusBoundary;

            SceneReleaseNexusBoundaryDisposition
                disposition =
                    reachedNexus &&
                    breakthrough
                        .HasQualifyingBreakthrough
                        ? SceneReleaseNexusBoundaryDisposition
                            .CanonCandidate
                        : SceneReleaseNexusBoundaryDisposition
                            .RemainsField;

            return new
                SceneReleaseNexusBoundaryEvaluation(
                    breakthrough,
                    nexusBoundary,
                    currentPosition,
                    disposition
                );
        }

        private static void ValidateProvenance(
            SceneRelease release,
            SceneReleaseCanonBreakthroughEvaluation
                breakthrough)
        {
            if (breakthrough.SceneReleaseId !=
                release.ReleaseId)
            {
                throw new ArgumentException(
                    "Canon Breakthrough belongs to a " +
                    "different SceneRelease.",
                    nameof(breakthrough)
                );
            }

            if (breakthrough.SourceDemoTapeId !=
                release.SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Canon Breakthrough DemoTape " +
                    "provenance does not match.",
                    nameof(breakthrough)
                );
            }

            if (breakthrough.SourceOwnerEntityId !=
                release.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Canon Breakthrough owner " +
                    "provenance does not match.",
                    nameof(breakthrough)
                );
            }

            if (breakthrough.SceneId !=
                release.HostedSceneNodeId)
            {
                throw new ArgumentException(
                    "Canon Breakthrough scene " +
                    "provenance does not match.",
                    nameof(breakthrough)
                );
            }

            if (breakthrough.SettledTurn <
                release.FieldPositionState
                    .EstablishedTurn)
            {
                throw new ArgumentException(
                    "Canon Breakthrough predates Field " +
                    "Position establishment.",
                    nameof(breakthrough)
                );
            }
        }

        private static
            SceneReleaseFieldPositionTransition
            FindSettlementMovement(
                SceneReleaseFieldPositionState position,
                int settledTurn)
        {
            SceneReleaseFieldPositionTransition found =
                null;

            foreach (
                SceneReleaseFieldPositionTransition
                    transition
                in position.Transitions)
            {
                if (transition.Kind !=
                        SceneReleaseFieldPositionTransitionKind
                            .SettlementMovement ||
                    transition.GlobalTurn !=
                        settledTurn)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException(
                        "Field Position history contains " +
                        "multiple settlement movements " +
                        "for one turn."
                    );
                }

                found =
                    transition;
            }

            return found ??
                throw new InvalidOperationException(
                    "Field Position history has no " +
                    "settlement movement for Nexus " +
                    "boundary evaluation."
                );
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(
                    left - right
                ) <=
                PositionTolerance;
        }
    }
}