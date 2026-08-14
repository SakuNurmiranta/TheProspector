using System;
using System.Collections.Generic;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Runtime adapter for the every-turn Happening
    /// preparation phase.
    ///
    /// It does not own Happening, Crisis or Accepted
    /// Transgression state. Those remain authoritative
    /// on SeededWorldState.
    ///
    /// When the shared participant window closes it:
    ///
    /// - advances current-turn Committed Happenings to
    ///   Resolving;
    /// - settles any still-unresolved ordinary Intents;
    /// - projects the authoritative world into the
    ///   already-tested institutional preparation
    ///   service.
    /// </summary>
    public sealed class
        KvltHappeningRuntimePreparationService
    {
        private readonly
            HappeningIntentSettlementService
            intentSettlementService =
                new();

        private readonly
            KvltHappeningPreparationService
            preparationService =
                new();

        public
            KvltHappeningRuntimePreparationResult
            Prepare(
                SeededWorldState world,
                string sceneId,
                int globalTurn,
                TrackEvaluationEnvironment
                    currentEnvironment,
                IReadOnlyList<string>
                    kvltParticipantEntityIds,
                string keeperEntityId)
        {
            if (world == null)
            {
                throw new ArgumentNullException(
                    nameof(world)
                );
            }

            sceneId =
                RequireText(
                    sceneId,
                    nameof(sceneId)
                );

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn)
                );
            }

            if (currentEnvironment == null)
            {
                throw new ArgumentNullException(
                    nameof(currentEnvironment)
                );
            }

            if (currentEnvironment.SettledTurn !=
                globalTurn)
            {
                throw new ArgumentException(
                    "Happening runtime preparation " +
                    "requires the current turn-t " +
                    "evaluation environment.",
                    nameof(currentEnvironment)
                );
            }

            if (kvltParticipantEntityIds == null)
            {
                throw new ArgumentNullException(
                    nameof(kvltParticipantEntityIds)
                );
            }

            keeperEntityId =
                RequireText(
                    keeperEntityId,
                    nameof(keeperEntityId)
                );

            CloseParticipantWindow(
                world,
                globalTurn
            );

            IReadOnlyList<
                    SceneReleaseActivationSource>
                publicSources =
                    BuildPublicSources(
                        world,
                        sceneId,
                        globalTurn
                    );

            IReadOnlyList<Happening>
                happenings =
                    world
                        .KvltHappeningRegistry
                        .GetAll();

            KvltHappeningPreparationResult
                preparation =
                    preparationService.Prepare(
                        globalTurn,
                        happenings,
                        publicSources,
                        world
                            .KvltAcceptedTransgressions,
                        world
                            .KvltAllegianceCrisisRegistry,
                        kvltParticipantEntityIds,
                        keeperEntityId
                    );

            return new
                KvltHappeningRuntimePreparationResult(
                    sceneId,
                    globalTurn,
                    currentEnvironment,
                    publicSources,
                    preparation
                );
        }

        private void CloseParticipantWindow(
            SeededWorldState world,
            int globalTurn)
        {
            foreach (
                Happening happening
                in world
                    .KvltHappeningRegistry
                    .GetAll())
            {
                if (happening.CommittedTurn !=
                    globalTurn)
                {
                    continue;
                }

                if (happening.LifecycleState ==
                    HappeningLifecycleState.Committed)
                {
                    if (!happening.TryBeginResolving(
                            globalTurn))
                    {
                        throw new
                            InvalidOperationException(
                                "Current-turn Happening " +
                                "could not close its " +
                                "participant window | " +
                                $"happening=" +
                                $"{happening.HappeningId}"
                            );
                    }
                }

                if (happening.LifecycleState !=
                    HappeningLifecycleState.Resolving)
                {
                    throw new
                        InvalidOperationException(
                            "Current-turn Happening is not " +
                            "available for institutional " +
                            "preparation | " +
                            $"happening=" +
                            $"{happening.HappeningId} | " +
                            $"state=" +
                            $"{happening.LifecycleState}"
                        );
                }

                intentSettlementService.Settle(
                    happening,
                    globalTurn
                );
            }
        }

        private static IReadOnlyList<
                SceneReleaseActivationSource>
            BuildPublicSources(
                SeededWorldState world,
                string sceneId,
                int globalTurn)
        {
            List<SceneReleaseActivationSource>
                result =
                    new();

            foreach (
                SceneRelease release
                in world.SceneReleases)
            {
                if (release == null)
                {
                    throw new
                        InvalidOperationException(
                            "Authoritative SceneRelease " +
                            "population contains null."
                        );
                }

                if (release.HostedSceneNodeId !=
                    sceneId)
                {
                    continue;
                }

                /*
                 * A release created after the turn being
                 * settled is not yet public to this
                 * Happening environment.
                 */
                if (release.ReleasedTurn >
                    globalTurn)
                {
                    continue;
                }

                /*
                 * Happening activation applies only to
                 * current mutable public material.
                 *
                 * CanonRetained is frozen.
                 * HistoricalCanon, rejection and failed
                 * fettering are terminal for active
                 * release semantics.
                 */
                if (release.LifecycleState !=
                        SceneReleaseLifecycleState
                            .Fringe &&
                    release.LifecycleState !=
                        SceneReleaseLifecycleState
                            .Field)
                {
                    continue;
                }

                GameEntity owner =
                    world.FindEntity(
                        release.SourceOwnerEntityId
                    );

                if (owner == null)
                {
                    throw new
                        InvalidOperationException(
                            "Activation-capable public " +
                            "SceneRelease has no " +
                            "authoritative source owner | " +
                            $"release={release.ReleaseId} | " +
                            $"owner=" +
                            $"{release.SourceOwnerEntityId}"
                        );
                }

                if (!owner.TryGetDemoTapeById(
                        release.SourceDemoTapeId,
                        out DemoTape demoTape))
                {
                    throw new
                        InvalidOperationException(
                            "Activation-capable public " +
                            "SceneRelease has no " +
                            "authoritative source DemoTape | " +
                            $"release={release.ReleaseId} | " +
                            $"demo=" +
                            $"{release.SourceDemoTapeId}"
                        );
                }

                result.Add(
                    new SceneReleaseActivationSource(
                        release,
                        demoTape
                    )
                );
            }

            result.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.Release.ReleaseId,
                        right.Release.ReleaseId
                    )
            );

            return result;
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Happening runtime identity cannot " +
                    "be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}
