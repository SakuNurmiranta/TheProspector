using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SEMM91.Core.SceneSpace
{
    public class SceneSpaceGraph
    {
        private readonly Dictionary<string, SceneSpaceNode> nodesById = new();
        public IReadOnlyCollection<SceneSpaceNode> Nodes => nodesById.Values;

        public bool AddNode(SceneSpaceNode node)
        {
            if (node == null) return false;
            
            if (nodesById.ContainsKey(node.NodeId)) return false;
            
            nodesById.Add(node.NodeId, node);
            return true;
        }

        public bool TryGetNode(string nodeId, out SceneSpaceNode node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }
            
            return nodesById.TryGetValue(nodeId, out node);
        }

        public bool ContainsNode(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId) && 
                   nodesById.ContainsKey(nodeId);
        }

        public void ApplyConstructHostingRules()
        {
            foreach (SceneSpaceNode node in nodesById.Values)
            {
                if (node == null)
                    continue;

                if (node.Status != SceneSpaceNodeStatus.Active)
                    continue;

                if (node.ConstructMode != SceneConstructMode.Or)
                    continue;

                // A Breach is allowed to exist directly inside Society.
                // It is the valid host condition for OR constructs.
                if (node.NodeType == SceneSpaceNodeType.Breach)
                    continue;

                if (string.IsNullOrWhiteSpace(node.ParentNodeId))
                    continue;

                if (!nodesById.TryGetValue(node.ParentNodeId, out SceneSpaceNode parent))
                    continue;

                if (parent.NodeType != SceneSpaceNodeType.Society)
                    continue;

                node.Suspend();

                Debug.Log(
                    "[SceneSpaceValidation] Suspended invalid OR construct " +
                    $"{node.DisplayName} inside Society. Society will attempt AND conversion later."
                );
            }
        }    
    }
}