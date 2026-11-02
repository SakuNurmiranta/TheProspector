using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class Artist
    {
        public string Id { get; }
        public string OriginFieldId { get; }

        public string PrimaryContactNodeId { get; private set; }

        
        private readonly HashSet<string> _blockedByNodeIds =
            new HashSet<string>();

        internal void BlockBy(string nodeId)
        {
            _blockedByNodeIds.Add(nodeId);
        }

        internal bool IsBlockedBy(string nodeId)
        {
            return _blockedByNodeIds.Contains(nodeId);
        }

        
        internal Artist(
            string id,
            string originFieldId,
            string primaryContactNodeId)
        {
            Id = id;
            OriginFieldId = originFieldId;
            PrimaryContactNodeId = primaryContactNodeId;
        }
    }
}