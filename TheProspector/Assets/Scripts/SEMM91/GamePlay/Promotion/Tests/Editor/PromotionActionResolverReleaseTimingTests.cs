using NUnit.Framework;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.SceneSpace;
using SEMM91.GamePlay.World;
using UnityEngine;

namespace SEMM91.GamePlay.Promotion
    .Tests.Editor
{
    public class
        PromotionActionResolverReleaseTimingTests
    {
        private GameObject entityObject;
        private GameEntity playerEntity;
        private SeededWorldState world;

        [SetUp]
        public void SetUp()
        {
            entityObject =
                new GameObject(
                    "Promotion Test Player"
                );

            playerEntity =
                entityObject
                    .AddComponent<GameEntity>();

            playerEntity.InitializeIdentity(
                "PLAYER_ENTITY",
                "Player",
                GameEntityType.Character
            );

            world =
                new SeededWorldState(null);

            world.SceneSpaceGraph.AddNode(
                new SceneSpaceNode(
                    StartingCollectiveBootstrapper
                        .NodeKvltScene,
                    "KVLT",
                    SceneSpaceNodeType.Scene,
                    SceneConstructMode.Or
                )
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (entityObject != null)
            {
                Object.DestroyImmediate(
                    entityObject
                );
            }
        }

        [Test]
        public void ReleaseUsesCurrentGlobalTurn_NotDemoRecordedTurn()
        {
            DemoTape demo =
                CreateDemo(
                    "DEMO_A",
                    recordedTurn: 2
                );

            playerEntity.AddDemoTape(
                demo
            );

            int currentTurn = 7;

            PromotionActionResolver resolver =
                new PromotionActionResolver(
                    () => currentTurn
                );

            bool released =
                resolver.TryReleaseDemoToKvlt(
                    1ul,
                    playerEntity,
                    world,
                    demo.DemoTapeId,
                    out string message
                );

            Assert.That(
                released,
                Is.True,
                message
            );

            Assert.That(
                world.SceneReleases.Count,
                Is.EqualTo(1)
            );

            SceneRelease release =
                world.SceneReleases[0];

            Assert.That(
                demo.RecordedTurn,
                Is.EqualTo(2)
            );

            Assert.That(
                release.ReleasedTurn,
                Is.EqualTo(7)
            );

            Assert.That(
                release.LifecycleState,
                Is.EqualTo(
                    SceneReleaseLifecycleState
                        .Fringe
                )
            );
        }

        [Test]
        public void ResolverReadsCurrentTurnAtEachRelease()
        {
            int currentTurn = 4;

            PromotionActionResolver resolver =
                new PromotionActionResolver(
                    () => currentTurn
                );

            DemoTape firstDemo =
                CreateDemo(
                    "DEMO_FIRST",
                    recordedTurn: 1
                );

            playerEntity.AddDemoTape(
                firstDemo
            );

            Assert.That(
                resolver.TryReleaseDemoToKvlt(
                    1ul,
                    playerEntity,
                    world,
                    firstDemo.DemoTapeId,
                    out _
                ),
                Is.True
            );

            currentTurn = 9;

            DemoTape secondDemo =
                CreateDemo(
                    "DEMO_SECOND",
                    recordedTurn: 3
                );

            playerEntity.AddDemoTape(
                secondDemo
            );

            Assert.That(
                resolver.TryReleaseDemoToKvlt(
                    1ul,
                    playerEntity,
                    world,
                    secondDemo.DemoTapeId,
                    out _
                ),
                Is.True
            );

            Assert.That(
                world.SceneReleases.Count,
                Is.EqualTo(2)
            );

            Assert.That(
                world.SceneReleases[0]
                    .ReleasedTurn,
                Is.EqualTo(4)
            );

            Assert.That(
                world.SceneReleases[1]
                    .ReleasedTurn,
                Is.EqualTo(9)
            );
        }

        private static DemoTape CreateDemo(
            string id,
            int recordedTurn)
        {
            return new DemoTape(
                id,
                id,
                $"{id}_SET",
                $"{id} Set",
                recordedTurn,
                1,
                1f,
                System.Array.Empty<
                    DemoTapeTrackSnapshot>()
            );
        }
    }
}