
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Carries a physical zine issue as an optional insert
    /// inside an already dispatched cassette shipment.
    ///
    /// ShipmentSent:
    ///   reserve one existing physical issue copy.
    ///
    /// ShipmentDelivered:
    ///   transfer physical ownership to the recipient.
    ///
    /// MessageOpened:
    ///   project the published information to the reader.
    ///
    /// DayCompleted:
    ///   reconcile postal shipments returned to sender.
    ///
    /// Does not generate messages or Layer 3 routes.
    /// </summary>
    public sealed class ZinePostalInsertCirculation : IDisposable
    {
        private readonly ZineEditorialIssue _issue;
        private readonly ZinePhysicalBulkDistribution _bulk;
        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly TimedPostalExchange _exchange;
        private readonly SimulationWorld _world;

        private readonly Dictionary<string, PostalShipment>
            _attachments =
                new Dictionary<string, PostalShipment>(
                    StringComparer.Ordinal);

        // Avoid sending several copies simultaneously
        // to the same still-uninformed recipient.
        private readonly HashSet<string> _pendingReaders =
            new HashSet<string>(StringComparer.Ordinal);

        private bool _enabled;
        private bool _disposed;

        public ZinePostalInsertCirculation(
            ZineEditorialIssue issue,
            ZinePhysicalBulkDistribution bulk,
            ZineIssueKnowledgeRegistry knowledge,
            TimedPostalExchange exchange,
            SimulationWorld world)
        {
            _issue = issue ??
                throw new ArgumentNullException(
                    nameof(issue));

            _bulk = bulk ??
                throw new ArgumentNullException(
                    nameof(bulk));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _world = world ??
                throw new ArgumentNullException(
                    nameof(world));

            _exchange.ShipmentSent += OnShipmentSent;
            _exchange.ShipmentDelivered += OnShipmentDelivered;
            _exchange.MessageOpened += OnMessageOpened;
            _world.DayCompleted += OnDayCompleted;
        }

        public bool HasAttachment(string shipmentId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException(
                    "Shipment ID is required.",
                    nameof(shipmentId));

            return _attachments.ContainsKey(shipmentId);
        }

        private void OnShipmentSent(PostalShipment shipment)
        {
            if (!_enabled || _disposed)
                return;

            if (shipment == null ||
                shipment.Status != PostalShipmentStatus.InTransit)
                return;

            var package = shipment.Package;

            // Only attach to a genuine cassette shipment.
            // Feedback-only messages do not distribute zines.
            if (package.CassetteCopyId == null)
                return;

            string sender = package.SourceNodeId;
            string recipient = package.TargetNodeId;

            if (sender == recipient)
                return;

            if (_bulk.GetCopies(_issue, sender) < 1)
                return;

            // No new copy is needed if the recipient already
            // knows this issue or another copy is on its way.
            if (_knowledge.HasReceived(
                    recipient, _issue.ProductId) ||
                _pendingReaders.Contains(recipient))
            {
                return;
            }

            _bulk.BeginPostalInsert(_issue, shipment);

            _attachments.Add(shipment.Id, shipment);
            _pendingReaders.Add(recipient);
        }

        private void OnShipmentDelivered(PostalShipment shipment)
        {
            if (_disposed || shipment == null)
                return;

            if (!_attachments.ContainsKey(shipment.Id))
                return;

            // No issue knowledge is given at delivery.
            _bulk.DeliverPostalInsert(_issue, shipment);
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            if (!_attachments.TryGetValue(
                    item.ShipmentId,
                    out var shipment))
            {
                return;
            }

            if (shipment.Status !=
                PostalShipmentStatus.Delivered)
            {
                return;
            }

            _knowledge.ReceiveIssue(
                item.RecipientNodeId,
                _issue);

            _pendingReaders.Remove(
                item.RecipientNodeId);

            _attachments.Remove(item.ShipmentId);
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed)
                return;

            // The initial Layer 3 print run is allocated
            // on day 1. Start postal inserts only after
            // that first complete distribution day.
            if (!_enabled && day >= 1)
                _enabled = true;

            if (_attachments.Count == 0)
                return;

            var returnedIds = new List<string>();

            foreach (var pair in _attachments)
            {
                if (pair.Value.Status !=
                    PostalShipmentStatus.Returned)
                {
                    continue;
                }

                _bulk.ReturnPostalInsert(
                    _issue, pair.Value);

                _pendingReaders.Remove(
                    pair.Value.Package.TargetNodeId);

                returnedIds.Add(pair.Key);
            }

            foreach (string id in returnedIds)
                _attachments.Remove(id);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.ShipmentSent -= OnShipmentSent;
            _exchange.ShipmentDelivered -= OnShipmentDelivered;
            _exchange.MessageOpened -= OnMessageOpened;
            _world.DayCompleted -= OnDayCompleted;

            _disposed = true;
        }
    }
}
