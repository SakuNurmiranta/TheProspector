using NUnit.Framework;
using SEMM91.Core.Entities;
using UnityEngine;

namespace SEMM91.GamePlay.World.Tests.Editor
{
    public class
        SeededWorldStatePhysicalLocationTests
    {
        [Test]
        public void RegisteredEntityCanBePlacedAtValidCoordinate()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            GameObject entityObject =
                new GameObject(
                    "PhysicalLocationTestEntity"
                );

            GameEntity entity =
                entityObject.AddComponent<
                    GameEntity
                >();
            
            entity.InitializeIdentity(
                "PHYSICAL_LOCATION_TEST_ENTITY",
                "Physical Location Test Entity",
                GameEntityType.Character
            );

            try
            {
                Assert.That(
                    entity.HasPhysicalLocation,
                    Is.False
                );

                Assert.That(
                    world.AddEntity(entity),
                    Is.True
                );

                Vector2Int coordinate =
                    new Vector2Int(3, 7);

                Assert.That(
                    world.TrySetEntityPhysicalLocation(
                        entity,
                        coordinate
                    ),
                    Is.True
                );

                Assert.That(
                    entity.HasPhysicalLocation,
                    Is.True
                );

                Assert.That(
                    entity.PhysicalNodeId,
                    Is.EqualTo(
                        PhysicalMapGrid.CreateNodeId(
                            coordinate
                        )
                    )
                );

                Assert.That(
                    world.TryGetEntityPhysicalCoordinate(
                        entity,
                        out Vector2Int resolved
                    ),
                    Is.True
                );

                Assert.That(
                    resolved,
                    Is.EqualTo(coordinate)
                );
            }
            finally
            {
                Object.DestroyImmediate(
                    entityObject
                );
            }
        }

        [Test]
        public void OutOfBoundsPlacementIsRejected()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            GameObject entityObject =
                new GameObject(
                    "InvalidLocationTestEntity"
                );

            GameEntity entity =
                entityObject.AddComponent<
                    GameEntity
                >();
            
            entity.InitializeIdentity(
                "INVALID_LOCATION_TEST_ENTITY",
                "Invalid Location Test Entity",
                GameEntityType.Character
            );

            try
            {
                world.AddEntity(entity);

                Assert.That(
                    world.TrySetEntityPhysicalLocation(
                        entity,
                        new Vector2Int(10, 4)
                    ),
                    Is.False
                );

                Assert.That(
                    entity.HasPhysicalLocation,
                    Is.False
                );
            }
            finally
            {
                Object.DestroyImmediate(
                    entityObject
                );
            }
        }

        [Test]
        public void UnregisteredEntityCannotBePlaced()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            GameObject entityObject =
                new GameObject(
                    "UnregisteredLocationTestEntity"
                );

            GameEntity entity =
                entityObject.AddComponent<
                    GameEntity
                >();
            
            entity.InitializeIdentity(
                "UNREGISTERED_LOCATION_TEST_ENTITY",
                "Unregistered Location Test Entity",
                GameEntityType.Character
            );

            try
            {
                Assert.That(
                    world.TrySetEntityPhysicalLocation(
                        entity,
                        new Vector2Int(2, 2)
                    ),
                    Is.False
                );

                Assert.That(
                    entity.HasPhysicalLocation,
                    Is.False
                );
            }
            finally
            {
                Object.DestroyImmediate(
                    entityObject
                );
            }
        }
    }
}