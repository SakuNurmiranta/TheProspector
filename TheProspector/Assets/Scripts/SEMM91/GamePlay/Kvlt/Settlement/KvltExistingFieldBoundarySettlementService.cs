using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Finalizes the existing resident Field after
    /// simultaneous turn-t movement.
    ///
    /// Only resident Field releases participate:
    ///
    /// Field lifecycle
    /// + established FieldPositionState.
    ///
    /// A newly fettered pre-ingress release therefore
    /// cannot accidentally enter this phase.
    ///
    /// This service must be called before the shared
    /// Happening phase.
    /// </summary>
    public sealed class
        KvltExistingFieldBoundarySettlementService
    {
        private readonly
            SceneReleaseOuterBoundaryEvaluator
            evaluator =
                new();

        private readonly
            SceneReleaseOuterBoundarySettlementService
            settlement =
                new();

        public
            KvltExistingFieldBoundarySettlementResult
            Settle(
                string sceneId,
                int settledTurn,
                float outerBoundary,
                IReadOnlyList<SceneRelease> releases)
        {
            sceneId =
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

            if (float.IsNaN(outerBoundary) ||
                float.IsInfinity(outerBoundary))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerBoundary)
                );
            }

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            List<SceneRelease>
                residentField =
                    GetOrderedResidentField(
                        releases,
                        sceneId
                    );

            List<
                    SceneReleaseOuterBoundaryEvaluation>
                evaluations =
                    new();

            foreach (
                SceneRelease release
                in residentField)
            {
                evaluations.Add(
                    evaluator.Evaluate(
                        release,
                        outerBoundary,
                        settledTurn
                    )
                );
            }

            IReadOnlyList<
                    SceneReleaseOuterBoundaryApplication>
                applications =
                    settlement.Apply(
                        releases,
                        evaluations
                    );

            return new
                KvltExistingFieldBoundarySettlementResult(
                    sceneId,
                    settledTurn,
                    evaluations,
                    applications
                );
        }

        private static List<SceneRelease>
            GetOrderedResidentField(
                IReadOnlyList<SceneRelease> releases,
                string sceneId)
        {
            List<SceneRelease> result =
                new();

            HashSet<string> identities =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneRelease release
                in releases)
            {
                if (release == null)
                {
                    throw new ArgumentException(
                        "Boundary release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!identities.Add(
                        release.ReleaseId))
                {
                    throw new ArgumentException(
                        "Boundary release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                /*
                 * Entry 24 invariant:
                 *
                 * Field lifecycle without a Field
                 * position means successfully fettered
                 * but still waiting for next-turn
                 * ingress.
                 */
                if (!release.HasFieldPosition ||
                    release.FieldPositionState == null)
                {
                    continue;
                }

                result.Add(
                    release
                );
            }

            result.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.ReleaseId,
                        right.ReleaseId
                    )
            );

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Boundary settlement identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}