using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable result of the NextTurnIngress phase.
    ///
    /// Every contained evaluation was successfully
    /// applied to its authoritative SceneRelease.
    /// </summary>
    public sealed class
        KvltNextTurnIngressSettlementResult
    {
        private readonly
            SceneReleaseIngressEvaluation[]
            ingressEvaluations;

        private readonly
            Dictionary<
                string,
                SceneReleaseIngressEvaluation>
            evaluationsByRelease;

        public string SceneId { get; }

        public int CompletedTurn { get; }

        public int PlacementTurn { get; }

        public IReadOnlyList<
                SceneReleaseIngressEvaluation>
            IngressEvaluations =>
            ingressEvaluations;

        public int IngressedCount =>
            ingressEvaluations.Length;

        public KvltNextTurnIngressSettlementResult(
            string sceneId,
            int completedTurn,
            int placementTurn,
            IReadOnlyList<
                SceneReleaseIngressEvaluation>
                sourceIngressEvaluations)
        {
            SceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (completedTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedTurn)
                );
            }

            if (placementTurn !=
                completedTurn + 1)
            {
                throw new ArgumentException(
                    "Next-turn ingress placement must " +
                    "occur exactly one turn after the " +
                    "completed settlement.",
                    nameof(placementTurn)
                );
            }

            if (sourceIngressEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceIngressEvaluations)
                );
            }

            CompletedTurn =
                completedTurn;

            PlacementTurn =
                placementTurn;

            ingressEvaluations =
                new SceneReleaseIngressEvaluation[
                    sourceIngressEvaluations.Count
                ];

            evaluationsByRelease =
                new Dictionary<
                    string,
                    SceneReleaseIngressEvaluation>(
                        StringComparer.Ordinal
                    );

            for (int index = 0;
                 index <
                 sourceIngressEvaluations.Count;
                 index++)
            {
                SceneReleaseIngressEvaluation
                    evaluation =
                        sourceIngressEvaluations[index] ??
                        throw new ArgumentException(
                            "Next-turn ingress cannot " +
                            "contain null evaluations.",
                            nameof(
                                sourceIngressEvaluations
                            )
                        );

                if (evaluation.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Ingress evaluation belongs " +
                        "to another scene.",
                        nameof(
                            sourceIngressEvaluations
                        )
                    );
                }

                if (evaluation.PlacementTurn !=
                    PlacementTurn)
                {
                    throw new ArgumentException(
                        "Ingress evaluation belongs " +
                        "to another placement turn.",
                        nameof(
                            sourceIngressEvaluations
                        )
                    );
                }

                if (!evaluationsByRelease.TryAdd(
                        evaluation.SceneReleaseId,
                        evaluation))
                {
                    throw new ArgumentException(
                        "Next-turn ingress contains " +
                        "duplicate SceneRelease identity.",
                        nameof(
                            sourceIngressEvaluations
                        )
                    );
                }

                ingressEvaluations[index] =
                    evaluation;
            }
        }

        public bool TryGet(
            string sceneReleaseId,
            out SceneReleaseIngressEvaluation
                evaluation)
        {
            evaluation =
                null;

            if (string.IsNullOrWhiteSpace(
                    sceneReleaseId))
            {
                return false;
            }

            return evaluationsByRelease.TryGetValue(
                sceneReleaseId.Trim(),
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
                    "Next-turn ingress identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}