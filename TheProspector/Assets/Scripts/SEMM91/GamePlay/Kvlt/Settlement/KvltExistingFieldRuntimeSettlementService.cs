using System;
using System.Collections.Generic;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Normative;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Runtime adapter for Peak-2 phase:
    ///
    /// ExistingFieldSettlement.
    ///
    /// It converts authoritative SeededWorldState into
    /// the immutable inputs expected by the already
    /// tested movement/boundary domain services.
    ///
    /// No movement or rejection mathematics belongs
    /// here.
    /// </summary>
    public sealed class
        KvltExistingFieldRuntimeSettlementService
    {
        private readonly
            CanonicalNormativeCentreDeriver
            canonicalNormativeCentreDeriver =
                new();

        private readonly
            KvltExistingFieldMovementSettlementService
            movementSettlement =
                new();

        private readonly
            KvltExistingFieldBoundarySettlementService
            boundarySettlement =
                new();

        public
            KvltExistingFieldRuntimeSettlementResult
            Settle(
                SeededWorldState world,
                KvltScenarioProfile scenario,
                string sceneId,
                int settledTurn)
        {
            if (world == null)
            {
                throw new ArgumentNullException(
                    nameof(world)
                );
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(
                    nameof(scenario)
                );
            }

            if (string.IsNullOrWhiteSpace(
                    sceneId))
            {
                throw new ArgumentException(
                    "Existing-Field runtime scene " +
                    "identity cannot be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            sceneId =
                sceneId.Trim();

            /*
             * Scene_t uses:
             *
             * - the currently published effective
             *   Normative Centre;
             * - Canon_t interpreted independently for
             *   Canon-only evaluation;
             * - the authoritative Society profile.
             *
             * Nothing produced during this settlement
             * can feed back into this environment.
             */
            NormativeCentre
                canonNormativeCentre =
                    canonicalNormativeCentreDeriver
                        .Derive(
                            world.KvltCanon
                        );

            TrackEvaluationEnvironment
                environment =
                    new(
                        settledTurn,
                        world.KvltNormativeCentre,
                        canonNormativeCentre,
                        world.SocietyNorms
                    );

            IReadOnlyList<DemoTape>
                demoTapes =
                    BuildRequiredDemoTapePopulation(
                        world,
                        sceneId
                    );

            KvltSceneSettlementPolicy policy =
                scenario.CreateSceneSettlementPolicy();

            KvltExistingFieldMovementSettlementResult
                movement =
                    movementSettlement.Settle(
                        sceneId,
                        settledTurn,
                        world.KvltCanon,
                        environment,
                        world.SceneReleases,
                        demoTapes,
                        policy
                    );

            /*
             * Boundary interpretation occurs
             * immediately after movement and before
             * Happenings.
             */
            KvltExistingFieldBoundarySettlementResult
                boundary =
                    boundarySettlement.Settle(
                        sceneId,
                        settledTurn,
                        scenario.OuterBoundary,
                        world.SceneReleases
                    );

            return new
                KvltExistingFieldRuntimeSettlementResult(
                    sceneId,
                    settledTurn,
                    environment,
                    movement,
                    boundary
                );
        }

        private static IReadOnlyList<DemoTape>
            BuildRequiredDemoTapePopulation(
                SeededWorldState world,
                string sceneId)
        {
            Dictionary<string, DemoTape>
                tapesById =
                    new(
                        StringComparer.Ordinal
                    );

            /*
             * Only current Field releases participate
             * in this phase.
             *
             * Their immutable source DemoTapes must
             * remain recoverable from the authoritative
             * source owner.
             */
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

                if (release.LifecycleState !=
                    SceneReleaseLifecycleState.Field)
                {
                    continue;
                }

                GameEntity sourceOwner =
                    world.FindEntity(
                        release.SourceOwnerEntityId
                    );

                if (sourceOwner == null)
                {
                    throw new
                        InvalidOperationException(
                            "Field SceneRelease source " +
                            "owner is absent from the " +
                            "authoritative world | " +
                            $"release={release.ReleaseId} | " +
                            $"owner=" +
                            $"{release.SourceOwnerEntityId}"
                        );
                }

                if (!sourceOwner.TryGetDemoTapeById(
                        release.SourceDemoTapeId,
                        out DemoTape tape))
                {
                    throw new
                        InvalidOperationException(
                            "Field SceneRelease source " +
                            "DemoTape is absent from its " +
                            "authoritative owner | " +
                            $"release={release.ReleaseId} | " +
                            $"demo=" +
                            $"{release.SourceDemoTapeId} | " +
                            $"owner=" +
                            $"{release.SourceOwnerEntityId}"
                        );
                }

                if (tapesById.TryGetValue(
                        tape.DemoTapeId,
                        out DemoTape existing))
                {
                    if (!ReferenceEquals(
                            existing,
                            tape))
                    {
                        throw new
                            InvalidOperationException(
                                "Different DemoTape " +
                                "instances share one " +
                                "authoritative identity | " +
                                $"demo={tape.DemoTapeId}"
                            );
                    }

                    continue;
                }

                tapesById.Add(
                    tape.DemoTapeId,
                    tape
                );
            }

            List<DemoTape> result =
                new(
                    tapesById.Values
                );

            result.Sort(
                (
                    left,
                    right
                ) =>
                    string.CompareOrdinal(
                        left.DemoTapeId,
                        right.DemoTapeId
                    )
            );

            return result;
        }
    }
}