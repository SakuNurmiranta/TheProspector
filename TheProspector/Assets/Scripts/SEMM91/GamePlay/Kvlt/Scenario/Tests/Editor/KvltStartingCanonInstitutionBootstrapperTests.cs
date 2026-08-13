using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
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

            /*
             * Keeper-independent semantic phase.
             *
             * Runtime will perform this before assigning
             * the founding Keeper.
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

            return new Context(
                world,
                tape
            );
        }

        private sealed class Context
        {
            public SeededWorldState World { get; }

            public DemoTape DemoTape { get; }

            public Context(
                SeededWorldState world,
                DemoTape demoTape)
            {
                World =
                    world;

                DemoTape =
                    demoTape;
            }
        }
    }
}