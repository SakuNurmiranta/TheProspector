using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Pressure
{
    /// <summary>
    /// Reconstructable result of rebuilding Dynamic
    /// Scene Pressure from one settled surviving
    /// non-canon Field corpus.
    /// </summary>
    public sealed class ScenePressureRebuildEvaluation
    {
        private readonly
            ScenePressureSourceContribution[]
            sourceContributions;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public SettledScenePressure Pressure { get; }

        public IReadOnlyList<
                ScenePressureSourceContribution>
            SourceContributions =>
            sourceContributions;

        public int SourceContributionCount =>
            sourceContributions.Length;

        public ScenePressureRebuildEvaluation(
            string sceneId,
            int settledTurn,
            SettledScenePressure pressure,
            IReadOnlyList<
                ScenePressureSourceContribution>
                contributions)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                throw new ArgumentException(
                    "Scene identity cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            Pressure =
                pressure ??
                throw new ArgumentNullException(
                    nameof(pressure)
                );

            if (contributions == null)
            {
                throw new ArgumentNullException(
                    nameof(contributions)
                );
            }

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            sourceContributions =
                new ScenePressureSourceContribution[
                    contributions.Count
                ];

            for (int index = 0;
                 index < contributions.Count;
                 index++)
            {
                ScenePressureSourceContribution
                    contribution =
                        contributions[index] ??
                        throw new ArgumentException(
                            "Pressure decomposition cannot " +
                            "contain null.",
                            nameof(contributions)
                        );

                if (contribution.SceneId !=
                        SceneId ||
                    contribution.SettledTurn !=
                        SettledTurn)
                {
                    throw new ArgumentException(
                        "Pressure contribution belongs " +
                        "to another scene settlement.",
                        nameof(contributions)
                    );
                }

                sourceContributions[index] =
                    contribution;
            }
        }
    }
}