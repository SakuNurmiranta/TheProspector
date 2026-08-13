using System;
using System.Collections.Generic;
using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay.Agency;
using UnityEditor;
using UnityEngine;

namespace SEMM91.GamePlay.Kvlt.Scenario.Tests.Editor
{
    public sealed class
        Peak2StartingScenarioBootstrapperTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        private readonly List<GameObject>
            createdObjects =
                new();

        private readonly
            Peak2StartingScenarioBootstrapper
            bootstrapper =
                new();

        [TearDown]
        public void TearDown()
        {
            foreach (
                GameObject created
                in createdObjects)
            {
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        created
                    );
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void
            FivePlayerBootstrapMakesHumanMayhem()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    FiveParticipants();

            KvltStartingScenarioBootstrapResult
                result =
                    Bootstrap(
                        participants
                    );

            Assert.That(
                result.ParticipantCount,
                Is.EqualTo(5)
            );

            Assert.That(
                result.FoundingClientId,
                Is.EqualTo(17)
            );

            Assert.That(
                participants[17].DisplayName,
                Is.EqualTo("Mayhem")
            );
        }

        [Test]
        public void
            FourBotsBecomeDeterministicTabulaRasaBands()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    FiveParticipants();

            Bootstrap(
                participants,
                bots:
                    new ulong[]
                    {
                        31,
                        8,
                        2,
                        12
                    }
            );

            Assert.That(
                participants[2].DisplayName,
                Is.EqualTo("Band 1")
            );

            Assert.That(
                participants[8].DisplayName,
                Is.EqualTo("Band 2")
            );

            Assert.That(
                participants[12].DisplayName,
                Is.EqualTo("Band 3")
            );

            Assert.That(
                participants[31].DisplayName,
                Is.EqualTo("Band 4")
            );
        }

        [Test]
        public void
            OnlyMayhemReceivesHistoricalRehearsalSetAndDemoTape()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    FiveParticipants();

            Bootstrap(
                participants
            );

            GameEntity mayhem =
                participants[17];

            Assert.That(
                mayhem.VhsSets.Count,
                Is.EqualTo(1)
            );

            Assert.That(
                mayhem.DemoTapes.Count,
                Is.EqualTo(1)
            );

            RehearsalSet sourceSet =
                mayhem.VhsSets[0];

            var demo =
                mayhem.DemoTapes[0];

            Assert.That(
                sourceSet.VhsSetId,
                Is.EqualTo(
                    demo.SourceSetId
                )
            );

            Assert.That(
                sourceSet.VhsTracks.Count,
                Is.EqualTo(3)
            );

            Assert.That(
                demo.TrackSnapshots.Count,
                Is.EqualTo(3)
            );

            foreach (
                ulong botClientId
                in new ulong[]
                {
                    2,
                    8,
                    12,
                    31
                })
            {
                Assert.That(
                    participants[botClientId]
                        .VhsSets,
                    Is.Empty
                );

                Assert.That(
                    participants[botClientId]
                        .DemoTapes,
                    Is.Empty
                );
            }
        }

        [Test]
        public void
            FreezingMoonUsesMayhemEntityAsSourceAtTurnZero()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    FiveParticipants();

            KvltStartingScenarioBootstrapResult
                result =
                    Bootstrap(
                        participants
                    );

            var tape =
                participants[17].DemoTapes[0];

            Assert.That(
                tape.RecordedTurn,
                Is.EqualTo(0)
            );

            Assert.That(
                result.FoundingDemoTapeId,
                Is.EqualTo(
                    Peak2KvltScenarioProfileFactory
                        .FoundingDemoTapeId
                )
            );

            foreach (
                var track
                in tape.TrackSnapshots)
            {
                foreach (
                    var idea
                    in track.IdeaSnapshots)
                {
                    Assert.That(
                        idea.SourceEntityId,
                        Is.EqualTo(
                            result.FoundingEntityId
                        )
                    );
                }
            }
        }

        [Test]
        public void
            WrongProductionRolePopulationIsRejected()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    FiveParticipants();

            Assert.Throws<ArgumentException>(
                () =>
                    Bootstrap(
                        participants,
                        humans:
                            new ulong[]
                            {
                                17,
                                31
                            },
                        bots:
                            new ulong[]
                            {
                                2,
                                8,
                                12
                            }
                    )
            );
        }

        [Test]
        public void
            SoloDevelopmentModeBootstrapsMayhemWithoutBots()
        {
            Dictionary<ulong, GameEntity>
                participants =
                    new()
                    {
                        {
                            17,
                            CreatePlayer(
                                17
                            )
                        }
                    };

            KvltStartingScenarioBootstrapResult
                result =
                    bootstrapper.Bootstrap(
                        Peak2KvltScenarioProfileFactory
                            .CreateDefault(),
                        participants,
                        new ulong[]
                        {
                            17
                        },
                        Array.Empty<ulong>(),
                        LoadJson(),
                        startingTurn:
                            0,
                        allowSoloDevelopmentMode:
                            true
                    );

            Assert.That(
                result.FoundingClientId,
                Is.EqualTo(17)
            );

            Assert.That(
                participants[17].DisplayName,
                Is.EqualTo("Mayhem")
            );

            Assert.That(
                participants[17].DemoTapes.Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void
            DefaultProfileStartsAtFivePlayersWithCorrectCanonClaims()
        {
            KvltScenarioProfile profile =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            Assert.That(
                profile.RequiredKvltPlayerCount,
                Is.EqualTo(5)
            );

            Assert.That(
                profile.TurnsPerYear,
                Is.EqualTo(4)
            );

            Assert.That(
                profile.StartingCanon.Count,
                Is.EqualTo(3)
            );

            AssertCanon(
                profile,
                SEMM91.Core.Tags.TagAxis.Symbolic
            );

            AssertCanon(
                profile,
                SEMM91.Core.Tags.TagAxis.Existential
            );

            AssertCanon(
                profile,
                SEMM91.Core.Tags.TagAxis.Physical
            );

            foreach (
                var canon
                in profile.StartingCanon)
            {
                Assert.That(
                    canon.EstablishedTurn,
                    Is.EqualTo(0)
                );

                Assert.That(
                    canon.SourceArtifactId,
                    Is.EqualTo(
                        Peak2KvltScenarioProfileFactory
                            .FoundingDemoTapeId
                    )
                );
            }
        }

        private KvltStartingScenarioBootstrapResult
            Bootstrap(
                Dictionary<ulong, GameEntity>
                    participants,
                ulong[] humans = null,
                ulong[] bots = null)
        {
            humans ??=
                new ulong[]
                {
                    17
                };

            bots ??=
                new ulong[]
                {
                    2,
                    8,
                    12,
                    31
                };

            return bootstrapper.Bootstrap(
                Peak2KvltScenarioProfileFactory
                    .CreateDefault(),
                participants,
                humans,
                bots,
                LoadJson(),
                startingTurn:
                    0
            );
        }

        private Dictionary<ulong, GameEntity>
            FiveParticipants()
        {
            return new Dictionary<
                ulong,
                GameEntity>
            {
                {
                    31,
                    CreatePlayer(31)
                },
                {
                    17,
                    CreatePlayer(17)
                },
                {
                    8,
                    CreatePlayer(8)
                },
                {
                    2,
                    CreatePlayer(2)
                },
                {
                    12,
                    CreatePlayer(12)
                }
            };
        }

        private GameEntity CreatePlayer(
            ulong clientId)
        {
            GameEntity entity =
                new PlayerEntityBootstrapper(
                    _ => { }
                )
                .CreatePlayerEntity(
                    clientId,
                    $"Player {clientId}"
                );

            createdObjects.Add(
                entity.gameObject
            );

            return entity;
        }

        private static string LoadJson()
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

            return asset.text;
        }

        private static void AssertCanon(
            KvltScenarioProfile profile,
            SEMM91.Core.Tags.TagAxis axis)
        {
            bool found =
                false;

            foreach (
                var record
                in profile.StartingCanon)
            {
                if (record.Axis != axis)
                    continue;

                found =
                    true;

                Assert.That(
                    record.Pole,
                    Is.EqualTo(
                        SEMM91.Core.Tags.TagPole
                            .Negative
                    )
                );

                Assert.That(
                    record.Degree,
                    Is.EqualTo(
                        SEMM91.Core.Tags.TagDegree
                            .Weak
                    )
                );
            }

            Assert.That(
                found,
                Is.True
            );
        }
    }
}