namespace SEMM91.Domain
{
    public sealed class BacklogItem
    {
        public string ShipmentId { get; }

        public string RecipientNodeId { get; }

        public MessagePackage Package { get; }

        internal BacklogItem(
            string shipmentId,
            string recipientNodeId,
            MessagePackage package)
        {
            ShipmentId = shipmentId;
            RecipientNodeId = recipientNodeId;
            Package = package;
        }
    }
}