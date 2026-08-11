using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Validates one complete frozen movement batch
    /// before mutating any authoritative Field
    /// Position.
    ///
    /// Once validation succeeds, evaluations are
    /// applied in deterministic SceneReleaseId order.
    /// </summary>
    public sealed class
        SceneReleaseMovementSettlementService
    {
        private const float PositionTolerance =
            0.0001f;

        public IReadOnlyList<
                SceneReleaseMovementApplication>
            Apply(
                IReadOnlyList<SceneRelease> releases,
                IReadOnlyList<
                    SceneReleaseMovementEvaluation>
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
                    SceneReleaseMovementApplication>();
            }

            Dictionary<string, SceneRelease>
                releasesById =
                    BuildReleaseLookup(
                        releases
                    );

            Dictionary<
                    string,
                    SceneReleaseMovementEvaluation>
                evaluationsByReleaseId =
                    new(
                        StringComparer.Ordinal
                    );

            string batchSceneId =
                null;

            int batchTurn =
                -1;

            /*
             * PASS 1
             *
             * Validate the entire frozen batch.
             *
             * No SceneRelease mutation occurs in this
             * pass.
             */
            foreach (
                SceneReleaseMovementEvaluation evaluation
                in evaluations)
            {
                if (evaluation == null)
                {
                    throw new ArgumentException(
                        "Movement batch cannot contain " +
                        "a null evaluation.",
                        nameof(evaluations)
                    );
                }

                if (!evaluationsByReleaseId.TryAdd(
                        evaluation.SceneReleaseId,
                        evaluation))
                {
                    throw new ArgumentException(
                        "Movement batch contains duplicate " +
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
                            "Movement batch cannot mix " +
                            "different scenes.",
                            nameof(evaluations)
                        );
                    }

                    if (evaluation.SettledTurn !=
                        batchTurn)
                    {
                        throw new ArgumentException(
                            "Movement batch cannot mix " +
                            "different settlement turns.",
                            nameof(evaluations)
                        );
                    }
                }

                if (!releasesById.TryGetValue(
                        evaluation.SceneReleaseId,
                        out SceneRelease release))
                {
                    throw new ArgumentException(
                        "Movement evaluation references " +
                        "a SceneRelease outside the " +
                        "supplied population.",
                        nameof(evaluations)
                    );
                }

                ValidateAgainstRelease(
                    release,
                    evaluation
                );
            }

            /*
             * Deterministic mutation order does not
             * affect movement results because every
             * evaluation was already frozen before
             * this pass began.
             */
            SceneReleaseMovementEvaluation[]
                orderedEvaluations =
                    new SceneReleaseMovementEvaluation[
                        evaluationsByReleaseId.Count
                    ];

            evaluationsByReleaseId.Values.CopyTo(
                orderedEvaluations,
                0
            );

            Array.Sort(
                orderedEvaluations,
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
                SceneReleaseMovementApplication>
                applications =
                    new();

            /*
             * PASS 2
             *
             * Authoritative mutation.
             *
             * All predictable failure conditions were
             * rejected before this point.
             */
            foreach (
                SceneReleaseMovementEvaluation evaluation
                in orderedEvaluations)
            {
                SceneRelease release =
                    releasesById[
                        evaluation.SceneReleaseId
                    ];

                if (!release.TryApplyFieldMovement(
                        evaluation.TotalDelta,
                        evaluation.SettledTurn,
                        out
                            SceneReleaseFieldPositionTransition
                            transition))
                {
                    /*
                     * In the current single-threaded
                     * authoritative settlement path,
                     * failure here indicates an
                     * invariant violation between
                     * validation and application.
                     */
                    throw new InvalidOperationException(
                        "Validated SceneRelease movement " +
                        "could not be applied | " +
                        $"release={release.ReleaseId} | " +
                        $"turn={evaluation.SettledTurn}"
                    );
                }

                applications.Add(
                    new SceneReleaseMovementApplication(
                        evaluation,
                        transition
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
                        "Movement release population " +
                        "cannot contain null.",
                        nameof(releases)
                    );
                }

                if (!result.TryAdd(
                        release.ReleaseId,
                        release))
                {
                    throw new ArgumentException(
                        "Movement release population " +
                        "contains duplicate SceneRelease " +
                        "identity.",
                        nameof(releases)
                    );
                }
            }

            return result;
        }

        private static void ValidateAgainstRelease(
            SceneRelease release,
            SceneReleaseMovementEvaluation evaluation)
        {
            if (release.SourceDemoTapeId !=
                evaluation.SourceDemoTapeId)
            {
                throw new ArgumentException(
                    "Movement evaluation DemoTape " +
                    "provenance does not match release."
                );
            }

            if (release.SourceOwnerEntityId !=
                evaluation.SourceOwnerEntityId)
            {
                throw new ArgumentException(
                    "Movement evaluation owner " +
                    "provenance does not match release."
                );
            }

            if (release.HostedSceneNodeId !=
                evaluation.SceneId)
            {
                throw new ArgumentException(
                    "Movement evaluation scene " +
                    "provenance does not match release."
                );
            }

            if (release.LifecycleState !=
                SceneReleaseLifecycleState.Field)
            {
                throw new InvalidOperationException(
                    "Only current Field releases can " +
                    "receive Field movement."
                );
            }

            if (!release.HasFieldPosition ||
                release.FieldPositionState == null)
            {
                throw new InvalidOperationException(
                    "Field movement requires an " +
                    "established Field Position."
                );
            }

            SceneReleaseFieldPositionState
                position =
                    release.FieldPositionState;

            /*
             * The frozen start position must still be
             * the authoritative current position.
             *
             * If it differs, something mutated the
             * release after movement evaluation.
             */
            if (!NearlyEqual(
                    position.CurrentPosition,
                    evaluation.StartFieldPosition))
            {
                throw new InvalidOperationException(
                    "Movement evaluation is stale: " +
                    "authoritative Field Position has " +
                    "changed since the snapshot."
                );
            }

            if (evaluation.SettledTurn <
                position.EstablishedTurn)
            {
                throw new InvalidOperationException(
                    "Movement settlement predates " +
                    "Field Position establishment."
                );
            }

            if (position.LastMovementTurn >=
                evaluation.SettledTurn)
            {
                throw new InvalidOperationException(
                    "SceneRelease already has movement " +
                    "at or after this settlement turn."
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
                PositionTolerance;
        }
    }
}