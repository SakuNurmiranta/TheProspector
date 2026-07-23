using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.GamePlay.World
{
    /// <summary>
    /// Authoritative discrete geographical map.
    ///
    /// Coordinates range from (0, 0) through (9, 9), producing
    /// exactly one hundred physical map nodes.
    /// </summary>
    public sealed class PhysicalMapGrid
    {
        public const int Width = 10;
        public const int Height = 10;

        private readonly List<PhysicalMapNode>
            nodes = new List<PhysicalMapNode>(
                Width * Height
            );

        private readonly Dictionary<
            Vector2Int,
            PhysicalMapNode
        > nodesByCoordinate =
            new Dictionary<
                Vector2Int,
                PhysicalMapNode
            >();

        private readonly Dictionary<
            string,
            PhysicalMapNode
        > nodesById =
            new Dictionary<
                string,
                PhysicalMapNode
            >(StringComparer.Ordinal);

        public IReadOnlyList<PhysicalMapNode>
            Nodes => nodes;

        public PhysicalMapGrid()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int coordinate =
                        new Vector2Int(x, y);

                    string nodeId =
                        CreateNodeId(coordinate);

                    PhysicalMapNode node =
                        new PhysicalMapNode(
                            nodeId,
                            coordinate
                        );

                    nodes.Add(node);

                    nodesByCoordinate.Add(
                        coordinate,
                        node
                    );

                    nodesById.Add(
                        nodeId,
                        node
                    );
                }
            }
        }

        public bool TryGetNode(
            Vector2Int coordinate,
            out PhysicalMapNode node)
        {
            return nodesByCoordinate.TryGetValue(
                coordinate,
                out node
            );
        }

        public bool TryGetNode(
            string nodeId,
            out PhysicalMapNode node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            return nodesById.TryGetValue(
                nodeId,
                out node
            );
        }

        public PhysicalMapNode GetNode(
            Vector2Int coordinate)
        {
            if (!TryGetNode(
                    coordinate,
                    out PhysicalMapNode node
                ))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coordinate),
                    coordinate,
                    "The coordinate is outside the physical map."
                );
            }

            return node;
        }

        public static bool IsCoordinateInBounds(
            Vector2Int coordinate)
        {
            return
                coordinate.x >= 0 &&
                coordinate.x < Width &&
                coordinate.y >= 0 &&
                coordinate.y < Height;
        }

        public static string CreateNodeId(
            Vector2Int coordinate)
        {
            if (!IsCoordinateInBounds(coordinate))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coordinate),
                    coordinate,
                    "Cannot create a node ID for a coordinate " +
                    "outside the physical map."
                );
            }

            return
                $"PHYSICAL_NODE_" +
                $"{coordinate.x:D2}_" +
                $"{coordinate.y:D2}";
        }
    }
}