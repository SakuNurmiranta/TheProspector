using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Movement;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Immutable output of one Canonization phase.
    ///
    /// This type does not decide whether the current
    /// turn is allowed to canonize. Turn chronology
    /// owns that decision.
    /// </summary>
    public sealed class
        KvltCanonizationSettlementResult
    {
        private readonly
            SceneReleaseNexusBoundaryEvaluation[]
            nexusEvaluations;

        private readonly
            SceneReleaseCanonFreezeApplication[]
            freezeApplications;

        public string SceneId { get; }

        public int SettledTurn { get; }

        public CanonState SceneStartCanon { get; }

        public CanonState NextCanon { get; }

        public CanonSimultaneousMergeEvaluation
            CanonMerge { get; }

        public IReadOnlyList<
                SceneReleaseNexusBoundaryEvaluation>
            NexusEvaluations =>
            nexusEvaluations;

        public IReadOnlyList<
                SceneReleaseCanonFreezeApplication>
            FreezeApplications =>
            freezeApplications;

        public bool HasCanonization =>
            CanonMerge != null;

        public KvltCanonizationSettlementResult(
            string sceneId,
            int settledTurn,
            CanonState sceneStartCanon,
            CanonState nextCanon,
            CanonSimultaneousMergeEvaluation
                canonMerge,
            IReadOnlyList<
                SceneReleaseNexusBoundaryEvaluation>
                sourceNexusEvaluations,
            IReadOnlyList<
                SceneReleaseCanonFreezeApplication>
                sourceFreezeApplications)
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

            NextCanon =
                nextCanon ??
                throw new ArgumentNullException(
                    nameof(nextCanon)
                );

            SceneId =
                sceneId.Trim();

            SettledTurn =
                settledTurn;

            CanonMerge =
                canonMerge;

            nexusEvaluations =
                Copy(
                    sourceNexusEvaluations,
                    nameof(sourceNexusEvaluations)
                );

            freezeApplications =
                Copy(
                    sourceFreezeApplications,
                    nameof(sourceFreezeApplications)
                );
        }

        private static T[] Copy<T>(
            IReadOnlyList<T> source,
            string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    parameterName
                );
            }

            T[] result =
                new T[source.Count];

            for (int index = 0;
                 index < source.Count;
                 index++)
            {
                result[index] =
                    source[index] ??
                    throw new ArgumentException(
                        "Canonization result collection " +
                        "cannot contain null.",
                        parameterName
                    );
            }

            return result;
        }
    }
}