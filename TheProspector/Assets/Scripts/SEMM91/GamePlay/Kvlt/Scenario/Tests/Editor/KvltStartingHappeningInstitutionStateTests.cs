using System;
using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingHappeningInstitutionStateTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private readonly
            KvltStartingWorldStateBootstrapper
            bootstrapper =
                new();

        [Test]
        public void
            NewWorldOwnsEmptyHappeningInstitutionState()
        {
            SeededWorldState world =
                World();

            Assert.That(
                world.KvltHappeningRegistry,
                Is.Not.Null
            );

            Assert.That(
                world.KvltHappeningRegistry.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                world.KvltAcceptedTransgressions,
                Is.Not.Null
            );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .KvltEntityId,
                Is.EqualTo(
                    StartingCollectiveBootstrapper
                        .KvltEntityId
                )
            );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .History.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                world.KvltAllegianceCrisisRegistry,
                Is.Not.Null
            );

            Assert.That(
                world.KvltAllegianceCrisisRegistry.Count,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            ApplyInstallsScenarioAcceptedTransgressionAtStartingTurn()
        {
            SeededWorldState world =
                World();

            /*
             * -1 survives only in the legacy Scenario
             * declaration. Runtime provenance must be
             * normalized to turn 0.
             */
            KvltAcceptedTransgressionSeed seed =
                new(
                    "AT_STARTING_LOW_SPECTACLE",
                    "LOW_SPECTACLE",
                    TagAxis.Physical,
                    TagPole.Negative,
                    TagDegree.Weak,
                    establishedTurn: -1,
                    sourceId:
                        "SCENARIO_HISTORY"
                );

            bootstrapper.Apply(
                ProfileWithAcceptedTransgression(
                    seed
                ),
                world,
                FreezingMoon(),
                startingTurn:
                    0
            );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .History.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .TryGetById(
                        seed.SeedId,
                        out AcceptedTransgressionRecord
                            record
                    ),
                Is.True
            );

            Assert.That(
                record.KvltEntityId,
                Is.EqualTo(
                    StartingCollectiveBootstrapper
                        .KvltEntityId
                )
            );

            Assert.That(
                record.AcceptedTurn,
                Is.EqualTo(0)
            );

            Assert.That(
                record.SourceKind,
                Is.EqualTo(
                    AcceptedTransgressionSourceKind
                        .ScenarioSeed
                )
            );

            Assert.That(
                record.SourceId,
                Is.EqualTo(
                    "SCENARIO_HISTORY"
                )
            );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .IsCovered(
                        "LOW_SPECTACLE",
                        TagAxis.Physical,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.True
            );

            /*
             * Scenario institutional truth does not
             * invent a turn-0 Happening or Crisis.
             */
            Assert.That(
                world.KvltHappeningRegistry.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                world.KvltAllegianceCrisisRegistry.Count,
                Is.EqualTo(0)
            );
        }

        [Test]
        public void
            ApplyRejectsExistingHappeningInstitutionStateBeforeMutation()
        {
            SeededWorldState world =
                World();

            AcceptedTransgressionRecord existing =
                new(
                    "AT_EXISTING",
                    StartingCollectiveBootstrapper
                        .KvltEntityId,
                    "EXISTING_PRAXIS",
                    TagAxis.Symbolic,
                    TagPole.Negative,
                    TagDegree.Weak,
                    acceptedTurn:
                        0,
                    sourceKind:
                        AcceptedTransgressionSourceKind
                            .ScenarioSeed,
                    sourceId:
                        "TEST"
                );

            Assert.That(
                world.KvltAcceptedTransgressions
                    .TryAccept(existing),
                Is.True
            );

            Assert.Throws<InvalidOperationException>(
                () =>
                    bootstrapper.Apply(
                        Peak2KvltScenarioProfileFactory
                            .CreateDefault(),
                        world,
                        FreezingMoon(),
                        startingTurn:
                            0
                    )
            );

            /*
             * The failure happened before ordinary
             * starting-state mutation.
             */
            Assert.That(
                world.KvltCanon.Records.Count,
                Is.EqualTo(0)
            );

            Assert.That(
                world.SocietyNorms.Count,
                Is.EqualTo(0)
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }

        private static KvltScenarioProfile
            ProfileWithAcceptedTransgression(
                KvltAcceptedTransgressionSeed seed)
        {
            KvltScenarioProfile baseline =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            return new KvltScenarioProfile(
                profileId:
                    "PEAK2_TEST_WITH_AT",
                version:
                    baseline.Version,
                requiredKvltPlayerCount:
                    baseline.RequiredKvltPlayerCount,
                turnsPerYear:
                    baseline.TurnsPerYear,
                sharedHappeningWindowSeconds:
                    baseline.SharedHappeningWindowSeconds,
                freshReleasePosition:
                    baseline.FreshReleasePosition,
                innerFieldEntryCeiling:
                    baseline.InnerFieldEntryCeiling,
                outerBoundary:
                    baseline.OuterBoundary,
                nexusBoundary:
                    baseline.NexusBoundary,
                fieldDriftScale:
                    baseline.FieldDriftScale,
                breakthroughDriftMultiplier:
                    baseline.BreakthroughDriftMultiplier,
                initialSceneDamage:
                    baseline.InitialSceneDamage,
                foundingKeeperSplatId:
                    baseline.FoundingKeeperSplatId,
                startingKeeperPull:
                    baseline.StartingKeeperPull,
                standingProjectionPolicy:
                    baseline.StandingProjectionPolicy,
                pressureRebuildPolicy:
                    baseline.PressureRebuildPolicy,
                normativePressureBlendPolicy:
                    baseline.NormativePressureBlendPolicy,
                pullEconomy:
                    baseline.PullEconomy,
                sourceStartingCanon:
                    baseline.StartingCanon,
                sourceStartingSocietyNorms:
                    baseline.StartingSocietyNorms,
                sourceStartingAcceptedTransgressions:
                    new[]
                    {
                        seed
                    },
                sourceStartingHailPraxisHistory:
                    baseline.StartingHailPraxisHistory
            );
        }

        private static DemoTape FreezingMoon()
        {
            TextAsset asset =
                AssetDatabase
                    .LoadAssetAtPath<TextAsset>(
                        FreezingMoonPath
                    );

            Assert.That(
                asset,
                Is.Not.Null
            );

            return AuthoredDemoTapeJsonLoader.Load(
                asset,
                "ENTITY_MAYHEM_TEST",
                recordedTurn:
                    0
            );
        }
    }
}