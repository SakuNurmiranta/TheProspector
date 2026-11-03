namespace SEMM91.Domain
{
    public sealed class ReceivedFeedback
    {
        public string ShipmentId { get; }
        public string MessageId { get; }

        public string SourceNodeId { get; }
        public string RecipientNodeId { get; }

        public string ProductId { get; }
        public int Thumb { get; }

        internal ReceivedFeedback(
            string shipmentId,
            string messageId,
            string sourceNodeId,
            string recipientNodeId,
            string productId,
            int thumb)
        {
            ShipmentId = shipmentId;
            MessageId = messageId;
            SourceNodeId = sourceNodeId;
            RecipientNodeId = recipientNodeId;
            ProductId = productId;
            Thumb = thumb;
        }
    }
}