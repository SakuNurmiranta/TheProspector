using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Score;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingCanonTenureLifecycleTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private const string MayhemEntityId =
            "ENTITY_MAYHEM_TEST";

        private const string InitialTenureId =
            "TENURE_MAYHEM_INITIAL";

        private const string NextTenureId =
            "TENURE_NEXT_KEEPER";

        [Test]
        public void
            StartingCanonRetainedReleasePaysFullFrozenGravity()
        {
            Context context =
                CreateContext();

            ScoreLedger ledger =
                new();

            bool awarded =
                new CanonRetainedScoreAwardService()
                    .TryAward(
                        context.Release,
                        InitialTenureId,
                        ledger,
                        globalTurn:
                            2,
                        out ScoreEvent scoreEvent
                    );

            Assert.That(
                awarded,
                Is.True
            );

            Assert.That(
                scoreEvent,
                Is.Not.Null
            );

            Assert.That(
                scoreEvent.Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .CanonRetainedGravity
                )
            );

            Assert.That(
                scoreEvent.BeneficiaryEntityId,
                Is.EqualTo(
                    MayhemEntityId
                )
            );

            Assert.That(
                scoreEvent.Amount,
                Is.EqualTo(
                    context.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    MayhemEntityId
                ),
                Is.EqualTo(
                    context.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            EndingFoundingTenureMovesStartingReleaseToHistoricalCanon()
        {
            Context context =
                CreateContext();

            var applications =
                new
                    SceneReleaseCanonTenureTransitionService()
                    .Apply(
                        new[]
                        {
                            context.Release
                        },
                        InitialTenureId,
                        NextTenureId,
                        globalTurn:
                            3
                    );

            Assert.That(
                applications.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                context.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            Assert.That(
                context.Release
                    .CanonizedUnderKeeperTenureId,
                Is.EqualTo(
                    InitialTenureId
                )
            );

            /*
             * Tenure transition changes lifecycle,
             * not the immutable Canon freeze.
             */
            Assert.That(
                context.Release
                    .HasCanonGravityDecomposition,
                Is.True
            );

            Assert.That(
                context.Release
                    .CanonGravityDecompositionState
                    .FrontierContributions
                    .Count,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void
            HistoricalFreezingMoonPaysThreeScenarioFrontierClaims()
        {
            Context context =
                CreateContext();

            new
                SceneReleaseCanonTenureTransitionService()
                .Apply(
                    new[]
                    {
                        context.Release
                    },
                    InitialTenureId,
                    NextTenureId,
                    globalTurn:
                        3
                );

            ScoreLedger ledger =
                new();

            IReadOnlyList<
                    HistoricalCanonInstitutionalGravityAward>
                awards =
                    new
                        HistoricalCanonInstitutionalGravityScoreAwardService()
                        .AwardEligibleClaims(
                            context.Release,
                            context.World.KvltCanon,
                            ledger,
                            globalTurn:
                                4
                        );

            Assert.That(
                awards.Count,
                Is.EqualTo(3)
            );

            float awardedTotal =
                0f;

            foreach (
                HistoricalCanonInstitutionalGravityAward
                    award
                in awards)
            {
                Assert.That(
                    award.CanonicalActivatorPlayerId,
                    Is.EqualTo(
                        MayhemEntityId
                    )
                );

                Assert.That(
                    award.ScoreEvent.Kind,
                    Is.EqualTo(
                        ScoreEventKind
                            .InstitutionalGravity
                    )
                );

                Assert.That(
                    award.ScoreEvent
                        .BeneficiaryEntityId,
                    Is.EqualTo(
                        MayhemEntityId
                    )
                );

                Assert.That(
                    award.FrontierContribution
                        .FrontierClaim
                        .ProvenanceKind,
                    Is.EqualTo(
                        SceneReleaseCanonFrontierProvenanceKind
                            .ScenarioSeed
                    )
                );

                Assert.That(
                    award.FrontierContribution
                        .FrontierClaim
                        .BreakthroughClaim,
                    Is.Null
                );

                Assert.That(
                    award.FrontierContribution
                        .FrontierClaim
                        .ActivationProvenance,
                    Is.Null
                );

                awardedTotal +=
                    award.Amount;
            }

            Assert.That(
                awardedTotal,
                Is.EqualTo(
                    context.Release
                        .CanonGravityDecompositionState
                        .FrontierContributionTotal
                ).Within(0.0001f)
            );

            Assert.That(
                ledger.GetLifetimeTotal(
                    MayhemEntityId
                ),
                Is.EqualTo(
                    awardedTotal
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            FinalRetainedPayoutBlocksHistoricalPayoutOnTransitionTurn()
        {
            Context context =
                CreateContext();

            ScoreLedger ledger =
                new();

            /*
             * Winter/turn 3:
             *
             * full retained Gravity is paid before
             * Keeper succession and tenure transition.
             */
            Assert.That(
                new CanonRetainedScoreAwardService()
                    .TryAward(
                        context.Release,
                        InitialTenureId,
                        ledger,
                        globalTurn:
                            3,
                        out ScoreEvent retained
                    ),
                Is.True
            );

            Assert.That(
                retained.Kind,
                Is.EqualTo(
                    ScoreEventKind
                        .CanonRetainedGravity
                )
            );

            /*
             * Later in that same turn the founding
             * tenure ends.
             */
            new
                SceneReleaseCanonTenureTransitionService()
                .Apply(
                    new[]
                    {
                        context.Release
                    },
                    InitialTenureId,
                    NextTenureId,
                    globalTurn:
                        3
                );

            Assert.That(
                context.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .HistoricalCanon
                )
            );

            /*
             * Claim-level historical payout must NOT
             * begin on the same turn because full
             * retained Gravity already paid.
             */
            var sameTurnHistorical =
                new
                    HistoricalCanonInstitutionalGravityScoreAwardService()
                    .AwardEligibleClaims(
                        context.Release,
                        context.World.KvltCanon,
                        ledger,
                        globalTurn:
                            3
                    );

            Assert.That(
                sameTurnHistorical,
                Is.Empty
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(1)
            );

            /*
             * Spring/turn 4:
             * historical frontier payout begins.
             */
            var nextTurnHistorical =
                new
                    HistoricalCanonInstitutionalGravityScoreAwardService()
                    .AwardEligibleClaims(
                        context.Release,
                        context.World.KvltCanon,
                        ledger,
                        globalTurn:
                            4
                    );

            Assert.That(
                nextTurnHistorical.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                ledger.Count,
                Is.EqualTo(4)
            );
        }

        private static Context CreateContext()
        {
            SeededWorldState world =
                new(
                    new CollectiveRegistry()
                );

            TextAsset asset =
                AssetDatabase
                    .LoadAssetAtPath<TextAsset>(
                        FreezingMoonPath
                    );

            Assert.That(
                asset,
                Is.Not.Null
            );

            DemoTape tape =
                AuthoredDemoTapeJsonLoader.Load(
                    asset,
                    MayhemEntityId,
                    recordedTurn:
                        0
                );

            /*
             * Entry 6:
             * starting Canon / Society / Normative.
             */
            new KvltStartingWorldStateBootstrapper()
                .Apply(
                    Peak2KvltScenarioProfileFactory
                        .CreateDefault(),
                    world,
                    tape,
                    startingTurn:
                        0
                );

            /*
             * Entry 7:
             * already-canonical starting release with
             * real frozen T/R/G.
             */
            KvltStartingCanonReleaseBootstrapResult
                releaseBootstrap =
                    new
                        KvltStartingCanonReleaseBootstrapper()
                        .Apply(
                            world,
                            tape,
                            MayhemEntityId,
                            StartingCollectiveBootstrapper
                                .NodeKvltScene,
                            InitialTenureId,
                            startingCanonScenePosition:
                                1f,
                            startingTurn:
                                0
                        );

            /*
             * Entry 8:
             * exact ScenarioSeed frontier provenance.
             */
            IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                frontierClaims =
                    new
                        KvltStartingCanonFrontierResolver()
                        .Resolve(
                            world,
                            tape,
                            releaseBootstrap.Release,
                            MayhemEntityId
                        );

            /*
             * Entry 9:
             * frozen claim-level Gravity basis.
             */
            new
                SceneReleaseCanonGravityDecompositionService()
                .Apply(
                    releaseBootstrap.Release,
                    releaseBootstrap
                        .FinalLegitimacy,
                    frontierClaims
                );

            return new Context(
                world,
                tape,
                releaseBootstrap.Release
            );
        }

        private sealed class Context
        {
            public SeededWorldState World { get; }

            public DemoTape DemoTape { get; }

            public SceneRelease Release { get; }

            public Context(
                SeededWorldState world,
                DemoTape demoTape,
                SceneRelease release)
            {
                World =
                    world;

                DemoTape =
                    demoTape;

                Release =
                    release;
            }
        }
    }
}
