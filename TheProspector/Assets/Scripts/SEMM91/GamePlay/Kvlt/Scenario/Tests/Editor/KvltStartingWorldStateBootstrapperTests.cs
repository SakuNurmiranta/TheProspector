using System;
using NUnit.Framework;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.World;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        KvltStartingWorldStateBootstrapperTests
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
            ApplyInstallsThreeStartingCanonClaims()
        {
            SeededWorldState world =
                World();

            bootstrapper.Apply(
                Profile(),
                world,
                FreezingMoon(),
                startingTurn:
                    0
            );

            Assert.That(
                world.KvltCanon.Records.Count,
                Is.EqualTo(3)
            );

            AssertCanon(
                world,
                TagAxis.Symbolic
            );

            AssertCanon(
                world,
                TagAxis.Existential
            );

            AssertCanon(
                world,
                TagAxis.Physical
            );
        }

        [Test]
        public void
            CanonProvenancePointsToExactFormalPairIdeas()
        {
            SeededWorldState world =
                World();

            DemoTape tape =
                FreezingMoon();

            bootstrapper.Apply(
                Profile(),
                world,
                tape,
                0
            );

            CanonPrecedentRecord profane =
                FindCanon(
                    world,
                    TagAxis.Symbolic
                );

            CanonPrecedentRecord morbid =
                FindCanon(
                    world,
                    TagAxis.Existential
                );

            CanonPrecedentRecord malevolent =
                FindCanon(
                    world,
                    TagAxis.Physical
                );

            Assert.That(
                profane.SourceTrackId,
                Is.EqualTo(
                    "FREEZING_MOON_TRACK_1"
                )
            );

            Assert.That(
                profane.SourceIdeaId,
                Is.EqualTo(
                    "FM_T1_PROFANE_SACRED"
                )
            );

            Assert.That(
                morbid.SourceTrackId,
                Is.EqualTo(
                    "FREEZING_MOON_TRACK_2"
                )
            );

            Assert.That(
                morbid.SourceIdeaId,
                Is.EqualTo(
                    "FM_T2_MORBID_VITAL"
                )
            );

            Assert.That(
                malevolent.SourceTrackId,
                Is.EqualTo(
                    "FREEZING_MOON_TRACK_3"
                )
            );

            Assert.That(
                malevolent.SourceIdeaId,
                Is.EqualTo(
                    "FM_T3_MALEVOLENT_BENEVOLENT"
                )
            );

            foreach (
                CanonPrecedentRecord record
                in world.KvltCanon.Records)
            {
                Assert.That(
                    record.SourceArtifactId,
                    Is.EqualTo(
                        tape.DemoTapeId
                    )
                );
            }
        }

        [Test]
        public void
            MorbidCanonUsesFormalPairNotMatchingSolitaryIdea()
        {
            SeededWorldState world =
                World();

            bootstrapper.Apply(
                Profile(),
                world,
                FreezingMoon(),
                0
            );

            CanonPrecedentRecord morbid =
                FindCanon(
                    world,
                    TagAxis.Existential
                );

            /*
             * Track 1 also contains solitary Morbid1.
             *
             * Canon provenance must resolve to the formal
             * Morbid1 -> Vital1 pair on Track 2 instead.
             */
            Assert.That(
                morbid.SourceIdeaId,
                Is.Not.EqualTo(
                    "FM_T1_MORBID"
                )
            );

            Assert.That(
                morbid.SourceIdeaId,
                Is.EqualTo(
                    "FM_T2_MORBID_VITAL"
                )
            );
        }

        [Test]
        public void
            ApplyInstallsSocietyAndDerivesCanonicalNormativeCentre()
        {
            SeededWorldState world =
                World();

            KvltScenarioProfile profile =
                Profile();

            bootstrapper.Apply(
                profile,
                world,
                FreezingMoon(),
                0
            );

            Assert.That(
                world.SocietyNorms.Count,
                Is.EqualTo(
                    profile
                        .StartingSocietyNorms
                        .Count
                )
            );

            Assert.That(
                world.KvltNormativeCentre
                    .NonZeroAffinityCount,
                Is.GreaterThan(0)
            );

            Assert.That(
                world.KvltNormativeCentre
                    .GetAffinity(
                        TagAxis.Symbolic,
                        TagPole.Negative,
                        TagDegree.Weak
                    ),
                Is.GreaterThan(0f)
            );
        }

        [Test]
        public void
            StartingStateUsesTurnZeroWithoutPretendingSettlementOccurred()
        {
            SeededWorldState world =
                World();

            bootstrapper.Apply(
                Profile(),
                world,
                FreezingMoon(),
                startingTurn:
                    0
            );

            foreach (
                CanonPrecedentRecord record
                in world.KvltCanon.Records)
            {
                Assert.That(
                    record.EstablishedTurn,
                    Is.EqualTo(0)
                );
            }

            /*
             * Scenario initialization is not a turn
             * settlement.
             */
            Assert.That(
                world.LastAppliedKvltSettlementTurn,
                Is.EqualTo(-1)
            );

            Assert.That(
                world.KvltScenePressure.EntryCount,
                Is.EqualTo(0)
            );
        }

        private static SeededWorldState World()
        {
            return new SeededWorldState(
                new CollectiveRegistry()
            );
        }

        private static KvltScenarioProfile Profile()
        {
            return
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();
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

        private static void AssertCanon(
            SeededWorldState world,
            TagAxis axis)
        {
            CanonPrecedentRecord record =
                FindCanon(
                    world,
                    axis
                );

            Assert.That(
                record,
                Is.Not.Null
            );

            Assert.That(
                record.Pole,
                Is.EqualTo(
                    TagPole.Negative
                )
            );

            Assert.That(
                record.Degree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                record.ProvenanceKind,
                Is.EqualTo(
                    CanonProvenanceKind
                        .ScenarioSeed
                )
            );
        }

        private static CanonPrecedentRecord
            FindCanon(
                SeededWorldState world,
                TagAxis axis)
        {
            foreach (
                CanonPrecedentRecord record
                in world.KvltCanon.Records)
            {
                if (record.Axis == axis &&
                    record.Pole ==
                        TagPole.Negative)
                {
                    return record;
                }
            }

            Assert.Fail(
                $"Missing starting Canon | " +
                $"axis={axis}"
            );

            return null;
        }
    }
}