using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Freezes the complete current qualifying Field
    /// Surface population from authoritative
    /// SceneRelease lifecycle/position state and
    /// already-resolved release legitimacy evaluation.
    /// </summary>
    public sealed class
        SceneFieldSurfaceSnapshotEvaluator
    {
        public SceneFieldSurfaceSnapshot Evaluate(
            string sceneId,
            int settledTurn,
            IReadOnlyList<SceneRelease> releases,
            IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
                releaseEvaluations)
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

            if (releases == null)
            {
                throw new ArgumentNullException(
                    nameof(releases)
                );
            }

            if (releaseEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(releaseEvaluations)
                );
            }

            Dictionary<string, SceneRelease>
                releasesById =
                    BuildReleaseLookup(
                        releases
                    );

            Dictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                evaluationsByReleaseId =
                    BuildEvaluationLookup(
                        releasesById,
                        releaseEvaluations,
                        settledTurn
                    );

            List<SceneFieldSurfaceEntry>
                entries =
                    new();

            foreach (
                SceneRelease release
                in releases)
            {
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
                 * By movement settlement, every Field
                 * release must already have completed
                 * ingress.
                 *
                 * Silently dropping it would corrupt
                 * the competitive mean.
                 */
                if (!release.HasFieldPosition ||
                    release.FieldPositionState == null)
                {
                    throw new InvalidOperationException(
                        "Field release has no Field " +
                        "Position at movement snapshot | " +
                        $"release={release.ReleaseId}"
                    );
                }

                if (!evaluationsByReleaseId
                        .TryGetValue(
                            release.ReleaseId,
                            out
                                SceneReleaseLegitimacyEvaluation
                                evaluation))
                {
                    throw new InvalidOperationException(
                        "Field release has no legitimacy " +
                        "evaluation for movement settlement | " +
                        $"release={release.ReleaseId} | " +
                        $"turn={settledTurn}"
                    );
                }

                /*
                 * Field objects without current active
                 * TRVE remain public Field history but
                 * do not participate in ordinary
                 * Natural Drift comparison.
                 */
                if (!evaluation.HasTrve)
                {
                    continue;
                }

                if (!IsFinite(
                        evaluation.Surface))
                {
                    throw new InvalidOperationException(
                        "Release Surface must be finite " +
                        "for Natural Drift."
                    );
                }

                entries.Add(
                    new SceneFieldSurfaceEntry(
                        release.ReleaseId,
                        release.SourceDemoTapeId,
                        release.SourceOwnerEntityId,
                        release.HostedSceneNodeId,
                        settledTurn,
                        evaluation.Surface,
                        release.FieldPositionState
                            .CurrentPosition
                    )
                );
            }

            return new SceneFieldSurfaceSnapshot(
                sceneId,
                settledTurn,
                entries
            );
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
                        "Release population cannot " +
                        "contain null.",
                        nameof(releases)
                    );
                }

                if (!result.TryAdd(
                        release.ReleaseId,
                        release))
                {
                    throw new ArgumentException(
                        "Release population contains " +
                        "duplicate SceneRelease identity.",
                        nameof(releases)
                    );
                }
            }

            return result;
        }

        private static Dictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            BuildEvaluationLookup(
                IReadOnlyDictionary<
                    string,
                    SceneRelease>
                    releasesById,
                IReadOnlyList<
                    SceneReleaseLegitimacyEvaluation>
                    evaluations,
                int settledTurn)
        {
            Dictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                result =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                SceneReleaseLegitimacyEvaluation evaluation
                in evaluations)
            {
                if (evaluation == null)
                {
                    throw new ArgumentException(
                        "Release evaluation population " +
                        "cannot contain null.",
                        nameof(evaluations)
                    );
                }

                if (evaluation.SettledTurn !=
                    settledTurn)
                {
                    throw new ArgumentException(
                        "Natural Drift snapshot cannot " +
                        "mix release evaluations from " +
                        "different settled turns.",
                        nameof(evaluations)
                    );
                }

                if (!releasesById.TryGetValue(
                        evaluation.SourceReleaseId,
                        out SceneRelease release))
                {
                    throw new ArgumentException(
                        "Release evaluation references " +
                        "a SceneRelease not present in " +
                        "the supplied population.",
                        nameof(evaluations)
                    );
                }

                if (evaluation.SourceDemoTapeId !=
                    release.SourceDemoTapeId)
                {
                    throw new ArgumentException(
                        "Release evaluation DemoTape " +
                        "provenance does not match.",
                        nameof(evaluations)
                    );
                }

                if (evaluation.SourceOwnerEntityId !=
                    release.SourceOwnerEntityId)
                {
                    throw new ArgumentException(
                        "Release evaluation owner " +
                        "provenance does not match.",
                        nameof(evaluations)
                    );
                }

                if (!result.TryAdd(
                        evaluation.SourceReleaseId,
                        evaluation))
                {
                    throw new ArgumentException(
                        "Release evaluation population " +
                        "contains duplicate SceneRelease " +
                        "identity.",
                        nameof(evaluations)
                    );
                }
            }

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene identity cannot be empty.",
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