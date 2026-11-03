using System;

namespace SEMM91.Domain
{
    public sealed class CassetteCopy
    {
        public string Id { get; }
        
        public string InTransitShipmentId { get; private set; }

        public string ProductId { get; }

        public string HolderNodeId { get; private set; }
        
        
        public string CopiedByNodeId { get; }



        internal CassetteCopy(
            string id,
            string productId,
            string holderNodeId,
            string copiedByNodeId = null)
        {
            Id = id;
            ProductId = productId;
            HolderNodeId = holderNodeId;
            CopiedByNodeId = copiedByNodeId;
        }



        internal void MoveTo(string nodeId)
        {
            if (InTransitShipmentId != null)
                throw new InvalidOperationException(
                    "Cannot transfer a cassette while in transit.");

            HolderNodeId = nodeId;
        }

        internal void BeginTransit(string shipmentId)
        {
            if (InTransitShipmentId != null ||
                HolderNodeId == null)
                throw new InvalidOperationException(
                    "Cassette is already in transit.");

            InTransitShipmentId = shipmentId;
            HolderNodeId = null;
        }

        internal void EndTransit(
            string shipmentId,
            string recipientNodeId)
        {
            if (InTransitShipmentId != shipmentId)
                throw new InvalidOperationException(
                    "Cassette does not belong to this shipment.");

            HolderNodeId = recipientNodeId;
            InTransitShipmentId = null;
        }

    }
}