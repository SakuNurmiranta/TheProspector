using System;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Pure phase-aware Nexus-boundary
    /// interpretation.
    ///
    /// A release becomes a Canon candidate only when:
    ///
    /// 1. it remains an active Field release;
    /// 2. its Field Position is valid for the supplied
    ///    breakthrough evaluation phase;
    /// 3. it has reached the Nexus boundary; and
    /// 4. its breakthrough evaluation contains
    ///    qualifying realized novelty.
    ///
    /// PreMovement preserves the strict Camp-3
    /// same-turn movement provenance check.
    ///
    /// PostHappening allows year-end screening at the
    /// already-settled current position, even when the
    /// last movement occurred on an earlier turn.
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
            return Evaluate(
                release,
                breakthrough,
                nexusBoundary,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    .PreMovement
            );
        }

        public SceneReleaseNexusBoundaryEvaluation
            Evaluate(
                SceneRelease release,
                SceneReleaseCanonBreakthroughEvaluation
                    breakthrough,
                float nexusBoundary,
                SceneReleaseCanonBreakthroughEvaluationPhase
                    breakthroughEvaluationPhase)
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

            ValidateTiming(
                position,
                breakthrough,
                breakthroughEvaluationPhase
            );

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
                    disposition,
                    breakthroughEvaluationPhase
                );
        }

        private static void ValidateTiming(
            SceneReleaseFieldPositionState position,
            SceneReleaseCanonBreakthroughEvaluation
                breakthrough,
            SceneReleaseCanonBreakthroughEvaluationPhase
                phase)
        {
            if (!Enum.IsDefined(
                    typeof(
                        SceneReleaseCanonBreakthroughEvaluationPhase
                    ),
                    phase))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(phase)
                );
            }

            switch (phase)
            {
                case
                    SceneReleaseCanonBreakthroughEvaluationPhase
                        .PreMovement:
                {
                    /*
                     * Preserve the complete Camp-3 invariant:
                     * the breakthrough was frozen before this
                     * same turn's settlement movement.
                     */
                    if (position.LastMovementTurn !=
                        breakthrough.SettledTurn)
                    {
                        throw new InvalidOperationException(
                            "Movement-backed Nexus evaluation " +
                            "requires same-turn settled movement."
                        );
                    }

                    SceneReleaseFieldPositionTransition
                        movement =
                            FindSettlementMovement(
                                position,
                                breakthrough.SettledTurn
                            );

                    if (!movement.PreviousPosition.HasValue ||
                        !NearlyEqual(
                            movement.PreviousPosition.Value,
                            breakthrough.StartFieldPosition))
                    {
                        throw new InvalidOperationException(
                            "Movement-backed Canon Breakthrough " +
                            "does not match the frozen position " +
                            "used by settlement movement."
                        );
                    }

                    if (!NearlyEqual(
                            movement.NewPosition,
                            position.CurrentPosition))
                    {
                        throw new InvalidOperationException(
                            "Field Position changed after " +
                            "settlement movement."
                        );
                    }

                    return;
                }

                case
                    SceneReleaseCanonBreakthroughEvaluationPhase
                        .PostHappening:
                {
                    /*
                     * Movement is already immutable.
                     *
                     * It may have occurred this turn or on an
                     * earlier turn. What matters now is that
                     * the fresh post-Happening screening was
                     * taken at the current authoritative
                     * position.
                     */
                    if (position.LastMovementTurn >
                        breakthrough.SettledTurn)
                    {
                        throw new InvalidOperationException(
                            "Post-Happening Nexus screening " +
                            "cannot use future movement state."
                        );
                    }

                    if (!NearlyEqual(
                            breakthrough.StartFieldPosition,
                            position.CurrentPosition))
                    {
                        throw new InvalidOperationException(
                            "Post-Happening Canon Breakthrough " +
                            "was not screened at the current " +
                            "settled Field Position."
                        );
                    }

                    return;
                }

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(phase)
                    );
            }
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