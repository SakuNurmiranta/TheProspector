using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Runtime package retained across the shared
    /// Happening / Allegiance Crisis window.
    ///
    /// It freezes:
    ///
    /// - the turn-t semantic environment;
    /// - the exact activation-capable public source set;
    /// - the institutional preparation result.
    ///
    /// Domain truth itself remains on SeededWorldState
    /// and the referenced authoritative domain objects.
    /// </summary>
    public sealed class
        KvltHappeningRuntimePreparationResult
    {
        private readonly
            SceneReleaseActivationSource[]
            publicSources;

        public string SceneId { get; }

        public int GlobalTurn { get; }

        public TrackEvaluationEnvironment
            CurrentEnvironment { get; }

        public KvltHappeningPreparationResult
            Preparation { get; }

        public IReadOnlyList<
                SceneReleaseActivationSource>
            PublicSources =>
            publicSources;

        public bool RequiresCrisisVoting =>
            Preparation.RequiresCrisisVoting;

        public
            KvltHappeningRuntimePreparationResult(
                string sceneId,
                int globalTurn,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    sourcePublicSources,
                KvltHappeningPreparationResult
                    preparation)
        {
            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Happening runtime scene identity " +
                    "cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            CurrentEnvironment =
                currentEnvironment ??
                throw new ArgumentNullException(
                    nameof(currentEnvironment)
                );

            Preparation =
                preparation ??
                throw new ArgumentNullException(
                    nameof(preparation)
                );

            if (sourcePublicSources == null)
            {
                throw new ArgumentNullException(
                    nameof(sourcePublicSources)
                );
            }

            SceneId =
                sceneId.Trim();

            GlobalTurn =
                globalTurn;

            if (CurrentEnvironment.SettledTurn !=
                GlobalTurn)
            {
                throw new ArgumentException(
                    "Happening runtime environment " +
                    "belongs to another turn.",
                    nameof(currentEnvironment)
                );
            }

            if (Preparation.GlobalTurn !=
                GlobalTurn)
            {
                throw new ArgumentException(
                    "Happening preparation belongs to " +
                    "another turn.",
                    nameof(preparation)
                );
            }

            publicSources =
                new SceneReleaseActivationSource[
                    sourcePublicSources.Count
                ];

            for (int index = 0;
                 index < sourcePublicSources.Count;
                 index++)
            {
                SceneReleaseActivationSource source =
                    sourcePublicSources[index] ??
                    throw new ArgumentException(
                        "Happening runtime public source " +
                        "population cannot contain null.",
                        nameof(sourcePublicSources)
                    );

                publicSources[index] =
                    source;
            }
        }
    }
}