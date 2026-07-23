using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SEMM91.GamePlay.World.Tests.Editor
{
    public class PhysicalMapGridTests
    {
        [Test]
        public void ConstructorCreatesExactlyOneHundredUniqueNodes()
        {
            PhysicalMapGrid grid =
                new PhysicalMapGrid();

            Assert.That(
                grid.Nodes.Count,
                Is.EqualTo(100)
            );

            HashSet<string> nodeIds =
                new HashSet<string>();

            HashSet<Vector2Int> coordinates =
                new HashSet<Vector2Int>();

            foreach (PhysicalMapNode node
                     in grid.Nodes)
            {
                Assert.That(
                    node,
                    Is.Not.Null
                );

                Assert.That(
                    nodeIds.Add(node.NodeId),
                    Is.True,
                    $"Duplicate node ID: {node.NodeId}"
                );

                Assert.That(
                    coordinates.Add(node.Coordinate),
                    Is.True,
                    $"Duplicate coordinate: {node.Coordinate}"
                );
            }
        }

        [Test]
        public void EveryCoordinateFromZeroToNineHasOneNode()
        {
            PhysicalMapGrid grid =
                new PhysicalMapGrid();

            for (int y = 0;
                 y < PhysicalMapGrid.Height;
                 y++)
            {
                for (int x = 0;
                     x < PhysicalMapGrid.Width;
                     x++)
                {
                    Vector2Int coordinate =
                        new Vector2Int(x, y);

                    bool found =
                        grid.TryGetNode(
                            coordinate,
                            out PhysicalMapNode node
                        );

                    Assert.That(
                        found,
                        Is.True,
                        $"Missing coordinate {coordinate}"
                    );

                    Assert.That(
                        node.Coordinate,
                        Is.EqualTo(coordinate)
                    );

                    Assert.That(
                        node.NodeId,
                        Is.EqualTo(
                            PhysicalMapGrid
                                .CreateNodeId(
                                    coordinate
                                )
                        )
                    );
                }
            }
        }

        [Test]
        public void CoordinatesOutsideGridAreRejected()
        {
            PhysicalMapGrid grid =
                new PhysicalMapGrid();

            Vector2Int[] invalidCoordinates =
            {
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(10, 0),
                new Vector2Int(0, 10),
                new Vector2Int(10, 10)
            };

            foreach (Vector2Int coordinate
                     in invalidCoordinates)
            {
                Assert.That(
                    PhysicalMapGrid
                        .IsCoordinateInBounds(
                            coordinate
                        ),
                    Is.False
                );

                Assert.That(
                    grid.TryGetNode(
                        coordinate,
                        out _
                    ),
                    Is.False
                );
            }
        }

        [Test]
        public void SeededWorldStateOwnsPhysicalMapGrid()
        {
            SeededWorldState world =
                new SeededWorldState(
                    collectiveRegistry: null
                );

            Assert.That(
                world.PhysicalMapGrid,
                Is.Not.Null
            );

            Assert.That(
                world.PhysicalMapGrid.Nodes.Count,
                Is.EqualTo(100)
            );
        }
    }
}