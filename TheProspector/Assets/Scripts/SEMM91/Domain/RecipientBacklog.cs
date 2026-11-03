
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class RecipientBacklog
    {
        private readonly Dictionary<string, Queue<BacklogItem>>
            _pending =
                new Dictionary<string, Queue<BacklogItem>>();

        private readonly HashSet<string> _registeredShipments =
            new HashSet<string>();

        public void ReceiveDelivered(PostalShipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            if (shipment.Status != PostalShipmentStatus.Delivered)
                throw new InvalidOperationException(
                    "Only delivered shipments can enter backlog.");

            if (_registeredShipments.Contains(shipment.Id))
                throw new InvalidOperationException(
                    "Shipment already registered in backlog.");

            var recipientId = shipment.Package.TargetNodeId;

            if (!_pending.TryGetValue(
                    recipientId, out var queue))
            {
                queue = new Queue<BacklogItem>();
                _pending.Add(recipientId, queue);
            }


            var item = new BacklogItem(
                shipment.Id,
                recipientId,
                shipment.Package,
                shipment.SenderCollectionProductIds);


            queue.Enqueue(item);
            _registeredShipments.Add(shipment.Id);
        }

        public int GetPendingCount(string recipientNodeId)
        {
            ValidateNodeId(recipientNodeId);

            return _pending.TryGetValue(
                recipientNodeId, out var queue)
                ? queue.Count
                : 0;
        }

        public bool TryTakeNext(
            string recipientNodeId,
            out BacklogItem item)
        {
            ValidateNodeId(recipientNodeId);

            if (!_pending.TryGetValue(
                    recipientNodeId, out var queue) ||
                queue.Count == 0)
            {
                item = null;
                return false;
            }

            item = queue.Dequeue();
            return true;
        }

        private static void ValidateNodeId(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Recipient node ID cannot be empty.",
                    nameof(nodeId));
        }
    }
}
