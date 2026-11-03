using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class BacklogItem
    {

        public IReadOnlyCollection<string>
            SenderCollectionProductIds { get; }

        public string ShipmentId { get; }

        public string RecipientNodeId { get; }

        public MessagePackage Package { get; }


        internal BacklogItem(
            string shipmentId,
            string recipientNodeId,
            MessagePackage package,
            IReadOnlyCollection<string> senderCollectionProductIds)
        {
            ShipmentId = shipmentId;
            RecipientNodeId = recipientNodeId;
            Package = package;
            SenderCollectionProductIds = senderCollectionProductIds;
        }

    }
}