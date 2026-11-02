namespace SEMM91.Domain
{
    public sealed class CassetteCopy
    {
        public string Id { get; }
        public string ProductId { get; }

        public string HolderNodeId { get; private set; }

        internal CassetteCopy(
            string id,
            string productId,
            string holderNodeId)
        {
            Id = id;
            ProductId = productId;
            HolderNodeId = holderNodeId;
        }

        internal void MoveTo(string nodeId)
        {
            HolderNodeId = nodeId;
        }
    }
}