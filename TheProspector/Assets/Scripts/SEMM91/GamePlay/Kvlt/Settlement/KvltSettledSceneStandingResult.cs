using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Standing;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable Scene Standing snapshot produced
    /// after turn-t lifecycle resolution and before
    /// next-turn ingress.
    ///
    /// One evaluation exists for every owner that has
    /// public release provenance in the requested
    /// scene, even when that owner currently has no
    /// standing-bearing corpus.
    /// </summary>
    public sealed class
        KvltSettledSceneStandingResult
    {
        private readonly
            SceneStandingEvaluation[]
            evaluations;

        private readonly
            Dictionary<
                string,
                SceneStandingEvaluation>
            evaluationsByOwner;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<
                SceneStandingEvaluation>
            Evaluations =>
            evaluations;

        public int OwnerCount =>
            evaluations.Length;

        public KvltSettledSceneStandingResult(
            string sceneId,
            int settledTurn,
            IReadOnlyList<
                SceneStandingEvaluation>
                sourceEvaluations)
        {
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

            if (sourceEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceEvaluations)
                );
            }

            SettledTurn =
                settledTurn;

            evaluations =
                new SceneStandingEvaluation[
                    sourceEvaluations.Count
                ];

            evaluationsByOwner =
                new Dictionary<
                    string,
                    SceneStandingEvaluation>(
                        StringComparer.Ordinal
                    );

            for (int index = 0;
                 index < sourceEvaluations.Count;
                 index++)
            {
                SceneStandingEvaluation evaluation =
                    sourceEvaluations[index] ??
                    throw new ArgumentException(
                        "Settled Scene Standing cannot " +
                        "contain null evaluations.",
                        nameof(sourceEvaluations)
                    );

                if (evaluation.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Scene Standing evaluation " +
                        "belongs to another scene.",
                        nameof(sourceEvaluations)
                    );
                }

                if (evaluation.SettledTurn !=
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Scene Standing evaluation " +
                        "belongs to another turn.",
                        nameof(sourceEvaluations)
                    );
                }

                if (!evaluationsByOwner.TryAdd(
                        evaluation
                            .SourceOwnerEntityId,
                        evaluation))
                {
                    throw new ArgumentException(
                        "Settled Scene Standing contains " +
                        "duplicate owner identity.",
                        nameof(sourceEvaluations)
                    );
                }

                evaluations[index] =
                    evaluation;
            }
        }

        public bool TryGet(
            string sourceOwnerEntityId,
            out SceneStandingEvaluation evaluation)
        {
            evaluation =
                null;

            if (string.IsNullOrWhiteSpace(
                    sourceOwnerEntityId))
            {
                return false;
            }

            return evaluationsByOwner.TryGetValue(
                sourceOwnerEntityId.Trim(),
                out evaluation
            );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Settled Scene Standing identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}