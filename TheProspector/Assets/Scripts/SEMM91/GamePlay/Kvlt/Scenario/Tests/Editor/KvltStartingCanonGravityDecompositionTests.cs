using System.Collections.Generic;
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
        KvltStartingCanonGravityDecompositionTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private const string MayhemEntityId =
            "ENTITY_MAYHEM_TEST";

        private const string KeeperTenureId =
            "TENURE_MAYHEM_INITIAL";

        [Test]
        public void
            StartingReleaseDecomposesIntoThreeActiveAndThreeFrontierClaims()
        {
            Context context =
                CreateContext();

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    Apply(context);

            Assert.That(
                decomposition
                    .ActivePairContributions
                    .Count,
                Is.EqualTo(3)
            );

            Assert.That(
                decomposition
                    .FrontierContributions
                    .Count,
                Is.EqualTo(3)
            );
        }

        [Test]
        public void
            StartingActivePairContributionsReconstructFrozenGravity()
        {
            Context context =
                CreateContext();

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    Apply(context);

            Assert.That(
                decomposition
                    .ActivePairContributionTotal,
                Is.EqualTo(
                    context.Release
                        .FrozenPostAssimilationGravity
                ).Within(0.0001f)
            );

            Assert.That(
                decomposition
                    .FrozenPostAssimilationGravity,
                Is.EqualTo(
                    context.ReleaseBootstrap
                        .FinalLegitimacy
                        .Gravity
                ).Within(0.0001f)
            );
        }

        [Test]
        public void
            StartingFrontierGravityPreservesScenarioProvenance()
        {
            Context context =
                CreateContext();

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    Apply(context);

            foreach (
                SceneReleaseCanonFrontierGravityContribution
                    contribution
                in decomposition.FrontierContributions)
            {
                SceneReleaseCanonFrontierClaim claim =
                    contribution.FrontierClaim;

                Assert.That(
                    claim.ProvenanceKind,
                    Is.EqualTo(
                        SceneReleaseCanonFrontierProvenanceKind
                            .ScenarioSeed
                    )
                );

                Assert.That(
                    claim.CanonicalActivatorPlayerId,
                    Is.EqualTo(
                        MayhemEntityId
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

                Assert.That(
                    contribution
                        .ClaimReleaseGravityContribution,
                    Is.GreaterThanOrEqualTo(0f)
                );
            }
        }

        [Test]
        public void
            StartingDecompositionAttachesOnceWithoutInventingHistory()
        {
            Context context =
                CreateContext();

            SceneReleaseCanonGravityDecompositionState
                decomposition =
                    Apply(context);

            Assert.That(
                context.Release
                    .HasCanonGravityDecomposition,
                Is.True
            );

            Assert.That(
                context.Release
                    .CanonGravityDecompositionState,
                Is.SameAs(
                    decomposition
                )
            );

            Assert.That(
                context.Release.ActivationHistory,
                Is.Empty
            );

            Assert.That(
                context.Release
                    .CanonAssimilationHistory,
                Is.Empty
            );

            Assert.Throws<
                System.InvalidOperationException>(
                () =>
                    new
                        SceneReleaseCanonGravityDecompositionService()
                        .Apply(
                            context.Release,
                            context.ReleaseBootstrap
                                .FinalLegitimacy,
                            context.FrontierClaims
                        )
            );
        }

        private static
            SceneReleaseCanonGravityDecompositionState
            Apply(
                Context context)
        {
            return new
                SceneReleaseCanonGravityDecompositionService()
                .Apply(
                    context.Release,
                    context.ReleaseBootstrap
                        .FinalLegitimacy,
                    context.FrontierClaims
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

            new KvltStartingWorldStateBootstrapper()
                .Apply(
                    Peak2KvltScenarioProfileFactory
                        .CreateDefault(),
                    world,
                    tape,
                    startingTurn:
                        0
                );

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
                            KeeperTenureId,
                            startingTurn:
                                0
                        );

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

            return new Context(
                world,
                tape,
                releaseBootstrap,
                frontierClaims
            );
        }

        private sealed class Context
        {
            public SeededWorldState World { get; }

            public DemoTape DemoTape { get; }

            public
                KvltStartingCanonReleaseBootstrapResult
                ReleaseBootstrap { get; }

            public SceneRelease Release =>
                ReleaseBootstrap.Release;

            public IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                FrontierClaims { get; }

            public Context(
                SeededWorldState world,
                DemoTape demoTape,
                KvltStartingCanonReleaseBootstrapResult
                    releaseBootstrap,
                IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                    frontierClaims)
            {
                World =
                    world;

                DemoTape =
                    demoTape;

                ReleaseBootstrap =
                    releaseBootstrap;

                FrontierClaims =
                    frontierClaims;
            }
        }
    }
}