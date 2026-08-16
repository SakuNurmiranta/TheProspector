using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Fresh semantic screening captured after
    /// Happening settlement and before year-end
    /// Canonization.
    ///
    /// This is deliberately separate from the
    /// movement-phase evaluation result.
    /// </summary>
    public sealed class
        KvltPostHappeningCanonizationScreeningResult
    {
        private readonly
            SceneReleaseLegitimacyEvaluation[]
            legitimacyEvaluations;

        private readonly
            Dictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            legitimacyByRelease;

        private readonly
            Dictionary<
                string,
                SceneReleaseCanonBreakthroughEvaluation>
            breakthroughsByRelease;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public CanonState SceneStartCanon { get; }

        public IReadOnlyList<
                SceneReleaseLegitimacyEvaluation>
            LegitimacyEvaluations =>
            legitimacyEvaluations;

        public IReadOnlyDictionary<
                string,
                SceneReleaseLegitimacyEvaluation>
            LegitimacyByRelease =>
            legitimacyByRelease;

        public IReadOnlyDictionary<
                string,
                SceneReleaseCanonBreakthroughEvaluation>
            BreakthroughsByRelease =>
            breakthroughsByRelease;

        public
            KvltPostHappeningCanonizationScreeningResult(
                string sceneId,
                int settledTurn,
                CanonState sceneStartCanon,
                IReadOnlyList<
                    SceneReleaseLegitimacyEvaluation>
                    sourceLegitimacyEvaluations,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseLegitimacyEvaluation>
                    sourceLegitimacyByRelease,
                IReadOnlyDictionary<
                    string,
                    SceneReleaseCanonBreakthroughEvaluation>
                    sourceBreakthroughsByRelease)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
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

            SceneStartCanon =
                sceneStartCanon ??
                throw new ArgumentNullException(
                    nameof(sceneStartCanon)
                );

            if (sourceLegitimacyEvaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceLegitimacyEvaluations
                    )
                );
            }

            if (sourceLegitimacyByRelease == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceLegitimacyByRelease
                    )
                );
            }

            if (sourceBreakthroughsByRelease == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceBreakthroughsByRelease
                    )
                );
            }

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            legitimacyEvaluations =
                new
                    SceneReleaseLegitimacyEvaluation[
                        sourceLegitimacyEvaluations
                            .Count
                    ];

            for (int index = 0;
                 index <
                 sourceLegitimacyEvaluations.Count;
                 index++)
            {
                legitimacyEvaluations[index] =
                    sourceLegitimacyEvaluations[index] ??
                    throw new ArgumentException(
                        "Post-Happening legitimacy " +
                        "collection cannot contain null.",
                        nameof(
                            sourceLegitimacyEvaluations
                        )
                    );
            }

            legitimacyByRelease =
                CopyDictionary(
                    sourceLegitimacyByRelease,
                    nameof(
                        sourceLegitimacyByRelease
                    )
                );

            breakthroughsByRelease =
                CopyDictionary(
                    sourceBreakthroughsByRelease,
                    nameof(
                        sourceBreakthroughsByRelease
                    )
                );
        }

        private static Dictionary<string, T>
            CopyDictionary<T>(
                IReadOnlyDictionary<string, T> source,
                string parameterName)
            where T : class
        {
            Dictionary<string, T> result =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                KeyValuePair<string, T> pair
                in source)
            {
                if (string.IsNullOrWhiteSpace(
                        pair.Key) ||
                    pair.Value == null)
                {
                    throw new ArgumentException(
                        "Post-Happening screening lookup " +
                        "contains invalid provenance.",
                        parameterName
                    );
                }

                result.Add(
                    pair.Key,
                    pair.Value
                );
            }

            return result;
        }
    }
}