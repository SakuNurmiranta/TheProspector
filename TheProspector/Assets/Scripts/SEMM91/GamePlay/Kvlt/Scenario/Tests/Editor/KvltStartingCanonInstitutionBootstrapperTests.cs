using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Kvlt.Standing;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingCanonInstitutionBootstrapperTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private const string MayhemEntityId =
            "ENTITY_MAYHEM_TEST";

        private const string KeeperTenureId =
            "TENURE_MAYHEM_INITIAL";

        private readonly
            KvltStartingCanonInstitutionBootstrapper
            bootstrapper =
                new();

        [Test]
        public void
            ApplyBuildsCompleteStartingCanonInstitution()
        {
            Context context =
                CreateContext();

            KvltStartingCanonInstitutionBootstrapResult
                result =
                    Apply(context);

            Assert.That(
                result.Release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .CanonRetained
                )
            );

            Assert.That(
                result.Release.CanonizedTurn,
                Is.EqualTo(0)
            );

            Assert.That(
                result.Release
                    .CanonizedUnderKeeperTenureId,
                Is.EqualTo(
                    KeeperTenureId
                )
            );

            Assert.That(
                result.Release
                    .PairActivationStates
                    .Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.FrontierClaims.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.GravityDecomposition
                    .ActivePairContributions
                    .Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.GravityDecomposition
                    .FrontierContributions
                    .Count,
                Is.EqualTo(3)
            );

            Assert.That(
                result.Release
                    .HasCanonGravityDecomposition,
                Is.True
            );
        }

        [Test]
        public void
            ApplyPreservesStartingCanonAndInventsNoGameplayHistory()
        {
            Context context =
                CreateContext();

            int canonBefore =
                context.World
                    .KvltCanon
                    .Records
                    .Count;

            KvltStartingCanonInstitutionBootstrapResult
                result =
                    Apply(context);

            Assert.That(
                context.World
                    .KvltCanon
                    .Records
                    .Count,
                Is.EqualTo(
                    canonBefore
                )
            );

            Assert.That(
                canonBefore,
                Is.EqualTo(3)
            );

            Assert.That(
                result.Release.ActivationHistory,
                Is.Empty
            );

            Assert.That(
                result.Release.ActivationAttempts,
                Is.Empty
            );

            Assert.That(
                result.Release
                    .CanonAssimilationHistory,
                Is.Empty
            );

            foreach (
                SceneReleaseCanonFrontierClaim claim
                in result.FrontierClaims)
            {
                Assert.That(
                    claim.ProvenanceKind,
                    Is.EqualTo(
                        SceneReleaseCanonFrontierProvenanceKind
                            .ScenarioSeed
                    )
                );

                Assert.That(
                    claim.BreakthroughClaim,
                    Is.Null
                );

                Assert.That(
                    claim.ActivationProvenance,
                    Is.Null
                );
            }
        }

        [Test]
        public void
            ApplyIsOneShotForFoundingDemoTape()
        {
            Context context =
                CreateContext();

            Apply(context);

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    Apply(context)
            );

            Assert.That(
                context.World
                    .SceneReleases
                    .Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            StartingCanonSettlesStandingAtNexusWithoutFieldHistory()
        {
            Context context =
                CreateContext();

            KvltStartingCanonInstitutionBootstrapResult
                bootstrap =
                    Apply(context);

            KvltSettledSceneStandingResult settled =
                new
                    KvltTurnTailRuntimeSettlementService()
                    .SettleStanding(
                        context.World,
                        context.Scenario,
                        StartingCollectiveBootstrapper
                            .NodeKvltScene,
                        settledTurn:
                            0
                    );

            Assert.That(
                bootstrap.Release.HasFieldPosition,
                Is.False
            );

            Assert.That(
                settled.TryGet(
                    MayhemEntityId,
                    out SceneStandingEvaluation standing
                ),
                Is.True
            );

            Assert.That(
                standing.HasStanding,
                Is.True
            );

            Assert.That(
                standing.Contributions.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                standing.Contributions[0].Kind,
                Is.EqualTo(
                    SceneStandingContributionKind
                        .CanonLegacy
                )
            );

            Assert.That(
                standing.Contributions[0].Position,
                Is.EqualTo(
                    context.Scenario.NexusBoundary
                )
            );

            /*
             * Three unique degree-1 starting frontiers:
             * base 1 + magnitude 3 x policy weight 1.
             */
            Assert.That(
                standing.Contributions[0].Weight,
                Is.EqualTo(4f)
                    .Within(0.0001f)
            );
        }

        private
            KvltStartingCanonInstitutionBootstrapResult
            Apply(
                Context context)
        {
            return bootstrapper.Apply(
                context.World,
                context.DemoTape,
                MayhemEntityId,
                StartingCollectiveBootstrapper
                    .NodeKvltScene,
                KeeperTenureId,
                startingCanonScenePosition:
                    context.Scenario.NexusBoundary,
                startingTurn:
                    0
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

            KvltScenarioProfile scenario =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            /*
             * Keeper-independent semantic phase.
             *
             * Runtime will perform this before assigning
             * the founding Keeper.
             */
            new KvltStartingWorldStateBootstrapper()
                .Apply(
                    scenario,
                    world,
                    tape,
                    startingTurn:
                        0
                );

            return new Context(
                world,
                tape,
                scenario
            );
        }

        private sealed class Context
        {
            public SeededWorldState World { get; }

            public DemoTape DemoTape { get; }

            public KvltScenarioProfile Scenario { get; }

            public Context(
                SeededWorldState world,
                DemoTape demoTape,
                KvltScenarioProfile scenario)
            {
                World =
                    world;

                DemoTape =
                    demoTape;

                Scenario =
                    scenario;
            }
        }
    }
}
