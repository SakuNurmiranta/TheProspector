namespace SEMM91.Domain
{
    public sealed class NetworkConnection
    {
        public string Id { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }
        public CommunicationLayer Layer { get; }
        public string Role { get; }

        internal NetworkConnection(
            string id,
            string sourceNodeId,
            string targetNodeId,
            CommunicationLayer layer,
            string role)
        {
            Id = id;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            Layer = layer;
            Role = role;
        }
    }
}