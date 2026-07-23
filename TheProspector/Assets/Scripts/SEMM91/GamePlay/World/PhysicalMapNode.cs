using System;
using UnityEngine;

namespace SEMM91.GamePlay.World
{
    /// <summary>
    /// One discrete geographical position in the physical world map.
    ///
    /// This is separate from SceneSpaceNode, which represents semantic
    /// scene membership such as KVLT, Society, and Wilderness.
    /// </summary>
    public sealed class PhysicalMapNode
    {
        public string NodeId { get; }

        public Vector2Int Coordinate { get; }

        public PhysicalMapNode(
            string nodeId,
            Vector2Int coordinate)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException(
                    "A physical map node requires a non-empty node ID.",
                    nameof(nodeId)
                );
            }

            NodeId = nodeId;
            Coordinate = coordinate;
        }
    }
}