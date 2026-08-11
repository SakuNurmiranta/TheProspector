using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Standing
{
    /// <summary>
    /// Immutable settled Scene Standing evaluation
    /// for one owner in one scene.
    ///
    /// Scene Standing is undefined when the owner has
    /// no successfully-fettered remembered corpus.
    /// </summary>
    public sealed class SceneStandingEvaluation
    {
        private readonly
            SceneStandingContribution[]
            contributions;

        public string SourceOwnerEntityId { get; }

        public string SceneId { get; }

        public int SettledTurn { get; }

        public IReadOnlyList<
                SceneStandingContribution>
            Contributions =>
            contributions;

        public bool HasStanding { get; }

        public float? Standing { get; }

        public float TotalWeight { get; }

        public SceneStandingEvaluation(
            string sourceOwnerEntityId,
            string sceneId,
            int settledTurn,
            IReadOnlyList<
                SceneStandingContribution>
                sourceContributions)
        {
            SourceOwnerEntityId =
                RequireText(
                    sourceOwnerEntityId,
                    nameof(sourceOwnerEntityId)
                );

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

            if (sourceContributions == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceContributions)
                );
            }

            SettledTurn =
                settledTurn;

            contributions =
                new SceneStandingContribution[
                    sourceContributions.Count
                ];

            HashSet<string>
                seenReleaseIds =
                    new(
                        StringComparer.Ordinal
                    );

            double totalWeight =
                0d;

            double weightedPosition =
                0d;

            for (int index = 0;
                 index < sourceContributions.Count;
                 index++)
            {
                SceneStandingContribution
                    contribution =
                        sourceContributions[index] ??
                        throw new ArgumentException(
                            "Scene Standing cannot " +
                            "contain a null contribution.",
                            nameof(sourceContributions)
                        );

                if (contribution
                        .SourceOwnerEntityId !=
                    SourceOwnerEntityId)
                {
                    throw new ArgumentException(
                        "Scene Standing contribution " +
                        "belongs to a different owner.",
                        nameof(sourceContributions)
                    );
                }

                if (contribution.SceneId !=
                    SceneId)
                {
                    throw new ArgumentException(
                        "Scene Standing contribution " +
                        "belongs to a different scene.",
                        nameof(sourceContributions)
                    );
                }

                if (contribution.BasisTurn >
                    SettledTurn)
                {
                    throw new ArgumentException(
                        "Scene Standing contribution " +
                        "cannot originate from a future " +
                        "turn.",
                        nameof(sourceContributions)
                    );
                }

                if (!seenReleaseIds.Add(
                        contribution
                            .SourceSceneReleaseId))
                {
                    throw new ArgumentException(
                        "A SceneRelease may contribute " +
                        "only once to one settled Scene " +
                        "Standing evaluation.",
                        nameof(sourceContributions)
                    );
                }

                contributions[index] =
                    contribution;

                totalWeight +=
                    contribution.Weight;

                weightedPosition +=
                    contribution.Position *
                    contribution.Weight;
            }

            if (contributions.Length == 0)
            {
                HasStanding =
                    false;

                Standing =
                    null;

                TotalWeight =
                    0f;

                return;
            }

            HasStanding =
                true;

            TotalWeight =
                (float)totalWeight;

            Standing =
                (float)(
                    weightedPosition /
                    totalWeight
                );
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Scene Standing identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}