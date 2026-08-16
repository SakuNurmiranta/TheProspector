using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Runtime adapter for the final authoritative
    /// phases of one Peak-2 turn closure:
    ///
    /// Standing_t
    ///     -> ingress_(t+1)
    ///     -> Pressure_(t+1)
    ///     -> NormativeCentre_(t+1).
    ///
    /// The established domain services continue to
    /// own all projection and placement mathematics.
    /// </summary>
    public sealed class
        KvltTurnTailRuntimeSettlementService
    {
        private readonly
            KvltSettledSceneStandingService
            standingService =
                new();

        private readonly
            KvltNextTurnIngressSettlementService
            ingressService =
                new();

        private readonly
            KvltNextSceneEnvironmentSettlementService
            environmentService =
                new();

        public KvltSettledSceneStandingResult
            SettleStanding(
                SeededWorldState world,
                KvltScenarioProfile scenario,
                string sceneId,
                int settledTurn)
        {
            ValidateCommon(
                world,
                scenario,
                sceneId,
                settledTurn
            );

            KvltSettledSceneStandingResult result =
                standingService.Settle(
                    sceneId.Trim(),
                    settledTurn,
                    world.SceneReleases,
                    scenario
                        .StandingProjectionPolicy
                );

            if (!world
                    .TryApplyKvltSettledSceneStanding(
                        result
                    ))
            {
                throw new InvalidOperationException(
                    "Settled Scene Standing could not " +
                    "be published to the authoritative " +
                    "world."
                );
            }

            return result;
        }

        public KvltNextTurnIngressSettlementResult
            SettleIngress(
                SeededWorldState world,
                KvltScenarioProfile scenario,
                string sceneId,
                int settledTurn)
        {
            ValidateCommon(
                world,
                scenario,
                sceneId,
                settledTurn
            );

            return ingressService.Settle(
                sceneId.Trim(),
                settledTurn,
                scenario.FreshReleasePosition,
                scenario.InnerFieldEntryCeiling,
                world.SceneReleases
            );
        }

        public
            KvltNextSceneEnvironmentSettlementResult
            SettleNextEnvironment(
                SeededWorldState world,
                KvltScenarioProfile scenario,
                string sceneId,
                int settledTurn,
                KvltNextTurnIngressSettlementResult
                    ingress,
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    turnPublicSources)
        {
            ValidateCommon(
                world,
                scenario,
                sceneId,
                settledTurn
            );

            if (ingress == null)
            {
                throw new ArgumentNullException(
                    nameof(ingress)
                );
            }

            if (ingress.CompletedTurn !=
                    settledTurn ||
                ingress.SceneId !=
                    sceneId.Trim())
            {
                throw new ArgumentException(
                    "Next-environment ingress belongs " +
                    "to another scene turn.",
                    nameof(ingress)
                );
            }

            IReadOnlyList<DemoTape> demoTapes =
                BuildUniqueDemoTapePopulation(
                    turnPublicSources,
                    sceneId.Trim()
                );

            KvltNextSceneEnvironmentSettlementResult
                result =
                    environmentService.Settle(
                        sceneId.Trim(),
                        world.KvltCanon,
                        world.SceneReleases,
                        demoTapes,
                        ingress,
                        scenario
                            .PressureRebuildPolicy,
                        scenario
                            .NormativePressureBlendPolicy
                    );

            world.ApplySettledScenePressure(
                result.Pressure.Pressure
            );

            world.ApplySettledNormativeCentre(
                result.NormativeCentre.Centre
            );

            return result;
        }

        private static IReadOnlyList<DemoTape>
            BuildUniqueDemoTapePopulation(
                IReadOnlyList<
                    SceneReleaseActivationSource>
                    turnPublicSources,
                string sceneId)
        {
            if (turnPublicSources == null)
            {
                throw new ArgumentNullException(
                    nameof(turnPublicSources)
                );
            }

            Dictionary<string, DemoTape> byId =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneReleaseActivationSource source
                in turnPublicSources)
            {
                if (source == null)
                {
                    throw new ArgumentException(
                        "Turn public source population " +
                        "cannot contain null.",
                        nameof(turnPublicSources)
                    );
                }

                if (source.Release
                        .HostedSceneNodeId !=
                    sceneId)
                {
                    throw new ArgumentException(
                        "Turn public source belongs to " +
                        "another scene.",
                        nameof(turnPublicSources)
                    );
                }

                if (byId.TryGetValue(
                        source.DemoTape.DemoTapeId,
                        out DemoTape existing))
                {
                    if (!ReferenceEquals(
                            existing,
                            source.DemoTape))
                    {
                        throw new InvalidOperationException(
                            "Different DemoTape instances " +
                            "share one authoritative " +
                            "identity."
                        );
                    }

                    continue;
                }

                byId.Add(
                    source.DemoTape.DemoTapeId,
                    source.DemoTape
                );
            }

            List<DemoTape> result =
                new(
                    byId.Values
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

        private static void ValidateCommon(
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
                    "Turn-tail scene identity cannot " +
                    "be empty.",
                    nameof(sceneId)
                );
            }

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }
        }
    }
}