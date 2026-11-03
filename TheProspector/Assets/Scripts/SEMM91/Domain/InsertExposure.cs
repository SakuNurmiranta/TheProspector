namespace SEMM91.Domain
{
    public sealed class InsertExposure
    {
        public string Id { get; }

        public string ConnectionId { get; }

        public string SourceNodeId { get; }

        public string TargetNodeId { get; }

        public string ProductId { get; }

        internal InsertExposure(
            string id,
            string connectionId,
            string sourceNodeId,
            string targetNodeId,
            string productId)
        {
            Id = id;
            ConnectionId = connectionId;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
            ProductId = productId;
        }
    }
}