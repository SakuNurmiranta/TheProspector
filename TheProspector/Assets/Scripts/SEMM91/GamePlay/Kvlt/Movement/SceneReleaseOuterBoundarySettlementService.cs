using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Validates a complete outer-boundary settlement
    /// batch before applying any terminal rejection.
    ///
    /// RemainsField evaluations are retained as
    /// settlement facts but require no mutation.
    /// </summary>
    public sealed class
        SceneReleaseOuterBoundarySettlementService
    {
        private const float PositionTolerance =
            0.0001f;

        public IReadOnlyList<
                SceneReleaseOuterBoundaryApplication>
            Apply(
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<
                    SceneReleaseOuterBoundaryEvaluation>
                    evaluations)
        {
            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (evaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluations)
                );
            }

            if (evaluations.Count == 0)
            {
                return Array.Empty<
                    SceneReleaseOuterBoundaryApplication>();
            }

            Dictionary<string, SceneRelease>
                releasesById =
                    BuildReleaseLookup(
                        releases
                    );

            Dictionary<
                    string,
                    SceneReleaseOuterBoundaryEvaluation>
                evaluationsById =
                    new(
                        StringComparer.Ordinal
                    );

            string batchSceneId =
                null;

            int batchTurn =
                -1;

            /*
             * PASS 1:
             * validate complete batch without mutation.
             */
            foreach (
                SceneReleaseOuterBoundaryEvaluation
                    evaluation
                in evaluations)
            {
                if (evaluation == null)
                {
                    throw new ArgumentException(
                        "Boundary batch cannot contain " +
                        "null evaluation.",
                        nameof(evaluations)
                    );
                }

                if (!evaluationsById.TryAdd(
                        evaluation.SceneReleaseId,
                        evaluation))
                {
                    throw new ArgumentException(
                        "Boundary batch contains duplicate " +
                        "SceneRelease identity.",
                        nameof(evaluations)
                    );
                }

                if (batchSceneId == null)
                {
                    batchSceneId =
                        evaluation.SceneId;

                    batchTurn =
                        evaluation.SettledTurn;
                }
                else
                {
                    if (evaluation.SceneId !=
                        batchSceneId)
                    {
                        throw new ArgumentException(
                            "Boundary batch cannot mix " +
                            "scenes.",
                            nameof(evaluations)
                        );
                    }

                    if (evaluation.SettledTurn !=
                        batchTurn)
                    {
                        throw new ArgumentException(
                            "Boundary batch cannot mix " +
                            "settlement turns.",
                            nameof(evaluations)
                        );
                    }
                }

                if (!releasesById.TryGetValue(
                        evaluation.SceneReleaseId,
                        out SceneRelease release))
                {
                    throw new ArgumentException(
                        "Boundary evaluation references " +
                        "a release outside supplied " +
                        "population.",
                        nameof(evaluations)
                    );
                }

                ValidateAgainstRelease(
                    release,
                    evaluation
                );
            }

            SceneReleaseOuterBoundaryEvaluation[]
                ordered =
                    new
                        SceneReleaseOuterBoundaryEvaluation[
                            evaluationsById.Count
                        ];

            evaluationsById.Values.CopyTo(
                ordered,
                0
            );

            Array.Sort(
                ordered,
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.SceneReleaseId,
                        right.SceneReleaseId
                    )
            );

            List<
                SceneReleaseOuterBoundaryApplication>
                applications =
                    new();

            /*
             * PASS 2:
             * apply terminal state only after every
             * evaluation has passed validation.
             */
            foreach (
                SceneReleaseOuterBoundaryEvaluation
                    evaluation
                in ordered)
            {
                if (!evaluation.CrossedOuterBoundary)
                {
                    continue;
                }

                SceneRelease release =
                    releasesById[
                        evaluation.SceneReleaseId
                    ];

                if (!release.TryReject(
                        evaluation.SettledTurn,
                        evaluation.OuterBoundary))
                {
                    throw new InvalidOperationException(
                        "Validated rejection could not " +
                        "be applied | " +
                        $"release={release.ReleaseId} | " +
                        $"turn={evaluation.SettledTurn}"
                    );
                }

                applications.Add(
                    new
                        SceneReleaseOuterBoundaryApplication(
                            evaluation,
                            release.RejectionState
                        )
                );
            }

            return applications.ToArray();
        }

        private static Dictionary<
                string,
                SceneRelease>
            BuildReleaseLookup(
                IReadOnlyList<SceneRelease> releases)
        {
            Dictionary<string, SceneRelease>
                result =
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

                if (!result.TryAdd(
                        release.ReleaseId,
                        release))
                {
                    throw new ArgumentException(
                        "Boundary release population " +
                        "contains duplicate identity.",
                        nameof(releases)
                    );
                }
            }

            return result;
        }

        private static void ValidateAgainstRelease(
            SceneRelease release,
            SceneReleaseOuterBoundaryEvaluation
                evaluation)
        {
            if (release.SourceDemoTapeId !=
                evaluation.SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Boundary evaluation DemoTape " +
                    "provenance does not match."
                );
            }

            if (release.SourceOwnerEntityId !=
                evaluation.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Boundary evaluation owner " +
                    "provenance does not match."
                );
            }

            if (release.HostedSceneNodeId !=
                evaluation.SceneId)
            {
                throw new ArgumentException(
                    "Boundary evaluation scene " +
                    "provenance does not match."
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new InvalidOperationException(
                    "Only current Field releases can " +
                    "be boundary-settled."
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new InvalidOperationException(
                    "Boundary settlement requires " +
                    "Field Position."
                );
            }

            if (!NearlyEqual(
                    release.FieldPositionState
                        .CurrentPosition,
                    evaluation.FieldPosition))
            {
                throw new InvalidOperationException(
                    "Boundary evaluation is stale: " +
                    "Field Position changed after " +
                    "evaluation."
                );
            }

            if (!NearlyEqual(
                    release.FieldPositionState
                        .PeakInwardPosition,
                    evaluation.PeakInwardPosition))
            {
                throw new InvalidOperationException(
                    "Boundary evaluation is stale: " +
                    "historical peak changed after " +
                    "evaluation."
                );
            }

            bool currentlyCrossed =
                release.FieldPositionState
                    .CurrentPosition <
                evaluation.OuterBoundary;

            if (currentlyCrossed !=
                evaluation.CrossedOuterBoundary)
            {
                throw new InvalidOperationException(
                    "Boundary evaluation no longer " +
                    "matches authoritative position."
                );
            }

            if (release.FieldPositionState
                    .LastMovementTurn >
                evaluation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "Boundary evaluation predates " +
                    "authoritative Field movement."
                );
            }
        }

        private static bool NearlyEqual(
            float left,
            float right)
        {
            return
                Math.Abs(left - right) <=
                PositionTolerance;
        }
    }
}