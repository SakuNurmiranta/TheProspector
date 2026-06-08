using System;

namespace SEMM91.Core.SceneSpace
{
    [Serializable]
    public class SceneSpaceNode
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public SceneSpaceNodeType NodeType { get; }
        public SceneConstructMode ConstructMode { get; private set; }
        public SceneSpaceNodeStatus Status { get; private set; }
        
        public string ParentNodeId { get; private set; }
        
        public string BoundCollectiveId { get; private set; }
        public string BoundEntityId { get; private set; }

        public SceneSpaceNode(
            string nodeId,
            string displayName,
            SceneSpaceNodeType nodeType,
            SceneConstructMode constructMode = SceneConstructMode.None,
            string parentNodeId = null,
            string boundCollectiveId = null,
            string boundEntityId = null)
        {
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("SceneSpaceNode requires a stable node ID.");
            NodeId = nodeId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? nodeId : displayName;
            NodeType = nodeType;
            ConstructMode = constructMode;
            ParentNodeId = parentNodeId;
            BoundCollectiveId = boundCollectiveId;
            BoundEntityId = boundEntityId;
            Status = SceneSpaceNodeStatus.Active;
        }

        public void SetParent(string parentNodeId)
        {
            ParentNodeId = parentNodeId;
        }
        
        public void BindCollective (string collectiveId)
        {
            BoundCollectiveId = collectiveId;
        }
        
        public void BindEntity (string entityId)
        {
            BoundEntityId = entityId;
        }

        public void Suspend()
        {
            Status = SceneSpaceNodeStatus.Suspended;
        }
        
        public void ConvertToAnd()
        {
            ConstructMode = SceneConstructMode.And;
            Status = SceneSpaceNodeStatus.Converted;
        }

        public bool IsActiveOrConstruct()
        {
            return ConstructMode == SceneConstructMode.Or && 
                   Status == SceneSpaceNodeStatus.Active;
        }
        
    }
}