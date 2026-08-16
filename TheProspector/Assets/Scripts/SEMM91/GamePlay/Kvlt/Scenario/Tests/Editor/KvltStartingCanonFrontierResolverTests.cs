using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingCanonFrontierResolverTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private const string MayhemEntityId =
            "ENTITY_MAYHEM_TEST";

        private const string KeeperTenureId =
            "TENURE_MAYHEM_INITIAL";

        private readonly
            KvltStartingCanonFrontierResolver
            resolver =
                new();

        [Test]
        public void
            ResolvesExactlyThreeScenarioFrontierClaims()
        {
            Context context =
                CreateContext();

            var claims =
                Resolve(context);

            Assert.That(
                claims.Count,
                Is.EqualTo(3)
            );

            foreach (
                SceneReleaseCanonFrontierClaim claim
                in claims)
            {
                Assert.That(
                    claim.ProvenanceKind,
                    Is.EqualTo(
                        SceneReleaseCanonFrontierProvenanceKind
                            .ScenarioSeed
                    )
                );

                Assert.That(
                    claim.IsScenarioSeed,
                    Is.True
                );

                Assert.That(
                    claim.HasLiveBreakthroughProvenance,
                    Is.False
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
            ScenarioClaimsUseExactFreezingMoonPairProvenance()
        {
            Context context =
                CreateContext();

            var claims =
                Resolve(context);

            AssertClaim(
                claims,
                TagAxis.Symbolic,
                "FREEZING_MOON_TRACK_1",
                "FM_T1_PROFANE_SACRED"
            );

            AssertClaim(
                claims,
                TagAxis.Existential,
                "FREEZING_MOON_TRACK_2",
                "FM_T2_MORBID_VITAL"
            );

            AssertClaim(
                claims,
                TagAxis.Physical,
                "FREEZING_MOON_TRACK_3",
                "FM_T3_MALEVOLENT_BENEVOLENT"
            );
        }

        [Test]
        public void
            MorbidSolitaryIdeaDoesNotBecomeFrontierClaim()
        {
            Context context =
                CreateContext();

            var claims =
                Resolve(context);

            foreach (
                SceneReleaseCanonFrontierClaim claim
                in claims)
            {
                Assert.That(
                    claim.SourceIdeaId,
                    Is.Not.EqualTo(
                        "FM_T1_MORBID"
                    )
                );
            }

            AssertClaim(
                claims,
                TagAxis.Existential,
                "FREEZING_MOON_TRACK_2",
                "FM_T2_MORBID_VITAL"
            );
        }

        [Test]
        public void
            ScenarioClaimsCarryMayhemTurnAndKeeperTenure()
        {
            Context context =
                CreateContext();

            var claims =
                Resolve(context);

            foreach (
                SceneReleaseCanonFrontierClaim claim
                in claims)
            {
                Assert.That(
                    claim.CanonicalActivatorPlayerId,
                    Is.EqualTo(
                        MayhemEntityId
                    )
                );

                Assert.That(
                    claim.CanonicalDegreeEstablished,
                    Is.EqualTo(
                        TagDegree.Weak
                    )
                );

                Assert.That(
                    claim.CanonizedTurn,
                    Is.EqualTo(0)
                );

                Assert.That(
                    claim.KeeperTenureId,
                    Is.EqualTo(
                        KeeperTenureId
                    )
                );

                Assert.That(
                    claim.SceneReleaseId,
                    Is.EqualTo(
                        context.Release.ReleaseId
                    )
                );

                Assert.That(
                    claim.SourceDemoTapeId,
                    Is.EqualTo(
                        context.DemoTape.DemoTapeId
                    )
                );
            }
        }

        private System.Collections.Generic
            .IReadOnlyList<
                SceneReleaseCanonFrontierClaim>
            Resolve(
                Context context)
        {
            return resolver.Resolve(
                context.World,
                context.DemoTape,
                context.Release,
                MayhemEntityId
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
                            startingCanonScenePosition:
                                1f,
                            startingTurn:
                                0
                        );

            return new Context(
                world,
                tape,
                releaseBootstrap.Release
            );
        }

        private static void AssertClaim(
            System.Collections.Generic
                .IReadOnlyList<
                    SceneReleaseCanonFrontierClaim>
                claims,
            TagAxis axis,
            string trackId,
            string ideaId)
        {
            SceneReleaseCanonFrontierClaim
                found =
                    null;

            foreach (
                SceneReleaseCanonFrontierClaim claim
                in claims)
            {
                if (claim.Axis != axis)
                {
                    continue;
                }

                found =
                    claim;

                break;
            }

            Assert.That(
                found,
                Is.Not.Null,
                $"Missing frontier axis={axis}"
            );

            Assert.That(
                found.SourceTrackId,
                Is.EqualTo(trackId)
            );

            Assert.That(
                found.SourceIdeaId,
                Is.EqualTo(ideaId)
            );

            Assert.That(
                found.IdeaIndex,
                Is.EqualTo(0)
            );

            Assert.That(
                found.Pole,
                Is.EqualTo(
                    TagPole.Negative
                )
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
