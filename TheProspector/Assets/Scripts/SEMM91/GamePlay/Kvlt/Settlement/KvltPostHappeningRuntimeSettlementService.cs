using System;
using System.Collections.Generic;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Kvlt.Evaluation;
using SEMM91.GamePlay.Kvlt.Movement;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Kvlt.Settlement
{
    /// <summary>
    /// Runtime adapter for the post-Happening phases:
    ///
    /// - fresh semantic screening;
    /// - Winter-only Canon settlement/publication;
    /// - every-turn Gravity Score settlement.
    ///
    /// Domain mathematics remains in the established
    /// screening, Canonization and Score services.
    /// </summary>
    public sealed class
        KvltPostHappeningRuntimeSettlementService
    {
        private readonly
            KvltPostHappeningCanonizationScreeningService
            screeningService =
                new();

        private readonly
            KvltCanonizationSettlementService
            canonizationService =
                new();

        private readonly
            KvltTurnScoreSettlementService
            turnScoreService =
                new();

        public
            KvltPostHappeningCanonizationScreeningResult
            Screen(
                SeededWorldState world,
                string sceneId,
                int settledTurn,
                TrackEvaluationEnvironment
                    currentEnvironment)
        {
            sceneId =
                ValidateCommon(
                    world,
                    sceneId,
                    settledTurn,
                    currentEnvironment
                );

            IReadOnlyList<DemoTape> demoTapes =
                ResolveRequiredFieldDemoTapes(
                    world,
                    sceneId
                );

            return screeningService.Screen(
                sceneId,
                settledTurn,
                world.KvltCanon,
                currentEnvironment,
                world.SceneReleases,
                demoTapes
            );
        }

        public KvltCanonizationSettlementResult
            SettleYearEndCanon(
                SeededWorldState world,
                KvltScenarioProfile scenario,
                string sceneId,
                int settledTurn,
                string currentKeeperTenureId,
                TrackEvaluationEnvironment
                    currentEnvironment,
                KvltPostHappeningCanonizationScreeningResult
                    screening)
        {
            sceneId =
                ValidateCommon(
                    world,
                    sceneId,
                    settledTurn,
                    currentEnvironment
                );

            if (scenario == null)
            {
                throw new ArgumentNullException(
                    nameof(scenario)
                );
            }

            currentKeeperTenureId =
                RequireText(
                    currentKeeperTenureId,
                    nameof(currentKeeperTenureId)
                );

            ValidateScreening(
                screening,
                sceneId,
                settledTurn
            );

            if (!world.KvltCanon.HasSameHistoryAs(
                    screening.SceneStartCanon))
            {
                throw new InvalidOperationException(
                    "Year-end Canon settlement received " +
                    "stale post-Happening screening."
                );
            }

            IReadOnlyList<DemoTape> demoTapes =
                ResolveRequiredFieldDemoTapes(
                    world,
                    sceneId
                );

            KvltCanonizationSettlementResult result =
                canonizationService.Settle(
                    sceneId,
                    settledTurn,
                    currentKeeperTenureId,
                    screening.SceneStartCanon,
                    currentEnvironment,
                    world.SceneReleases,
                    demoTapes,
                    screening.BreakthroughsByRelease,
                    scenario.CreateSceneSettlementPolicy(),
                    SceneReleaseCanonBreakthroughEvaluationPhase
                        .PostHappening,
                    release =>
                        !world.KvltParadigmState
                            .HasActivePoserdom(
                                release.SourceOwnerEntityId,
                                settledTurn + 1)
                );

            world.KvltCanon.ReplaceWith(
                result.NextCanon
            );

            return result;
        }

        public KvltTurnScoreSettlementResult
            SettleTurnScore(
                SeededWorldState world,
                string sceneId,
                int settledTurn,
                string currentKeeperTenureId,
                KvltPostHappeningCanonizationScreeningResult
                    screening)
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

            if (settledTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn)
                );
            }

            ValidateScreening(
                screening,
                sceneId,
                settledTurn
            );

            return turnScoreService.Settle(
                sceneId,
                settledTurn,
                currentKeeperTenureId ??
                    string.Empty,
                screening.SceneStartCanon,
                world.SceneReleases,
                screening.LegitimacyByRelease,
                world.KvltScoreLedger
            );
        }

        private static IReadOnlyList<DemoTape>
            ResolveRequiredFieldDemoTapes(
                SeededWorldState world,
                string sceneId)
        {
            Dictionary<string, DemoTape> tapesById =
                new(
                    StringComparer.Ordinal
                );

            foreach (
                SceneRelease release
                in world.SceneReleases)
            {
                if (release == null)
                {
                    throw new InvalidOperationException(
                        "Authoritative SceneRelease " +
                        "population contains null."
                    );
                }

                if (release.HostedSceneNodeId !=
                        sceneId ||
                    release.LifecycleState !=
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
                    throw new InvalidOperationException(
                        "Post-Happening Field release " +
                        "source owner is absent | " +
                        $"release={release.ReleaseId} | " +
                        $"owner={release.SourceOwnerEntityId}"
                    );
                }

                if (!sourceOwner.TryGetDemoTapeById(
                        release.SourceDemoTapeId,
                        out DemoTape tape))
                {
                    throw new InvalidOperationException(
                        "Post-Happening Field release " +
                        "source DemoTape is absent | " +
                        $"release={release.ReleaseId} | " +
                        $"demo={release.SourceDemoTapeId}"
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
                        throw new InvalidOperationException(
                            "Different DemoTape instances " +
                            "share one authoritative identity | " +
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

        private static string ValidateCommon(
            SeededWorldState world,
            string sceneId,
            int settledTurn,
            TrackEvaluationEnvironment
                currentEnvironment)
        {
            if (world == null)
            {
                throw new ArgumentNullException(
                    nameof(world)
                );
            }

            string normalizedSceneId =
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

            if (currentEnvironment == null)
            {
                throw new ArgumentNullException(
                    nameof(currentEnvironment)
                );
            }

            if (currentEnvironment.SettledTurn !=
                settledTurn)
            {
                throw new ArgumentException(
                    "Post-Happening environment belongs " +
                    "to another turn.",
                    nameof(currentEnvironment)
                );
            }

            return normalizedSceneId;
        }

        private static void ValidateScreening(
            KvltPostHappeningCanonizationScreeningResult
                screening,
            string sceneId,
            int settledTurn)
        {
            if (screening == null)
            {
                throw new ArgumentNullException(
                    nameof(screening)
                );
            }

            if (screening.SceneId !=
                    sceneId ||
                screening.SettledTurn !=
                    settledTurn)
            {
                throw new ArgumentException(
                    "Post-Happening screening belongs " +
                    "to another scene or turn.",
                    nameof(screening)
                );
            }
        }

        private static string RequireText(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new ArgumentException(
                    "Post-Happening runtime identity " +
                    "cannot be empty.",
                    parameterName
                );
            }

            return value.Trim();
        }
    }
}
