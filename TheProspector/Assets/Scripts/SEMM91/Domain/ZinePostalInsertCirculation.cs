using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Moves an existing physical zine copy as an attachment
    /// to an already dispatched Layer 2 postal shipment.
    ///
    /// Never creates a postal message or a network connection.
    /// The recipient learns the issue when opening the letter.
    /// </summary>
    public sealed class ZinePostalInsertCirculation : IDisposable
    {
        private readonly ZineEditorialIssue _issue;
        private readonly ZinePhysicalBulkDistribution _bulk;
        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly TimedPostalExchange _exchange;
        private readonly SimulationWorld _world;

        private readonly bool _enableLetterAttachments;
        private readonly bool _enablePreviouslyReadRecipients;

        private readonly Dictionary<string, PostalShipment>
            _attachments =
                new Dictionary<string, PostalShipment>(
                    StringComparer.Ordinal);
        
        private readonly Dictionary<string, int>
            _attachedCountBySender =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);

        private readonly HashSet<string> _pendingReaders =
            new HashSet<string>(StringComparer.Ordinal);

        // An unordered pair is used to prevent repeated
        // handoffs and immediate A->B->A loops.
        // Each physical issue circulation has its own instance.
        private readonly HashSet<(string, string)> _usedPairs =
            new HashSet<(string, string)>();

        private readonly Dictionary<string, (string, string)>
            _shipmentPairs =
                new Dictionary<string, (string, string)>(
                    StringComparer.Ordinal);

        private bool _enabled;
        private bool _disposed;

        public int AttachedCount { get; private set; }
        public int DeliveredCount { get; private set; }
        public int NewReaderOpenings { get; private set; }
        public int RepeatedReaderOpenings { get; private set; }
        public int ReturnedCount { get; private set; }

        public int PendingCount => _attachments.Count;

        public ZinePostalInsertCirculation(
            ZineEditorialIssue issue,
            ZinePhysicalBulkDistribution bulk,
            ZineIssueKnowledgeRegistry knowledge,
            TimedPostalExchange exchange,
            SimulationWorld world,
            bool enableLetterAttachments = false,
            bool enablePreviouslyReadRecipients = false)
        {
            _issue = issue ??
                throw new ArgumentNullException(nameof(issue));

            _bulk = bulk ??
                throw new ArgumentNullException(nameof(bulk));

            _knowledge = knowledge ??
                throw new ArgumentNullException(nameof(knowledge));

            _exchange = exchange ??
                throw new ArgumentNullException(nameof(exchange));

            _world = world ??
                throw new ArgumentNullException(nameof(world));

            _enableLetterAttachments =
                enableLetterAttachments;

            _enablePreviouslyReadRecipients =
                enablePreviouslyReadRecipients;

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

        /// <summary>
        /// Number of existing postal shipments to which
        /// this node has attached a physical zine copy.
        ///
        /// Counts dispatch events, including shipments that
        /// may subsequently be returned.
        /// </summary>
        public int GetAttachedCountFrom(string senderNodeId)
        {
            if (string.IsNullOrWhiteSpace(senderNodeId))
            {
                throw new ArgumentException(
                    "Sender node ID is required.",
                    nameof(senderNodeId));
            }

            return _attachedCountBySender.TryGetValue(
                senderNodeId,
                out int count)
                ? count
                : 0;
        }
        
        private static (string, string) PairKey(
            string first,
            string second)
        {
            return string.CompareOrdinal(first, second) <= 0
                ? (first, second)
                : (second, first);
        }

        private void OnShipmentSent(PostalShipment shipment)
        {
            if (!_enabled || _disposed || shipment == null)
                return;

            if (shipment.Status !=
                PostalShipmentStatus.InTransit)
            {
                return;
            }

            var package = shipment.Package;

            // Legacy behaviour: only cassette parcels.
            // Extended behaviour: any existing Layer 2
            // letter can carry the physical issue.
            if (!_enableLetterAttachments &&
                package.CassetteCopyId == null)
            {
                return;
            }

            string sender = package.SourceNodeId;
            string recipient = package.TargetNodeId;

            if (sender == recipient)
                return;

            if (_bulk.GetCopies(_issue, sender) < 1)
                return;

            // Do not dispatch several copies simultaneously
            // to a reader whose earlier parcel is unopened.
            if (_pendingReaders.Contains(recipient))
                return;

            // Legacy circulation only targets new readers.
            if (!_enablePreviouslyReadRecipients &&
                _knowledge.HasReceived(
                    recipient, _issue.ProductId))
            {
                return;
            }

            var pair = PairKey(sender, recipient);

            // Extended circulation permits already informed
            // recipients, but does not bounce an issue
            // repeatedly along the same pair of nodes.
            if (_enablePreviouslyReadRecipients &&
                _usedPairs.Contains(pair))
            {
                return;
            }

            // This reserves one existing printed copy.
            // It does not create another postal shipment.
            _bulk.BeginPostalInsert(_issue, shipment);

            _attachments.Add(shipment.Id, shipment);
            _pendingReaders.Add(recipient);

            if (_enablePreviouslyReadRecipients)
            {
                _usedPairs.Add(pair);
                _shipmentPairs.Add(shipment.Id, pair);
            }

            AttachedCount++;
            
            if (!_attachedCountBySender.TryGetValue(
                    sender, out int previous))
            {
                previous = 0;
            }

            _attachedCountBySender[sender] = previous + 1;
        }

        private void OnShipmentDelivered(
            PostalShipment shipment)
        {
            if (_disposed || shipment == null)
                return;

            if (!_attachments.ContainsKey(shipment.Id))
                return;

            _bulk.DeliverPostalInsert(_issue, shipment);

            DeliveredCount++;
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

            // Only the first reading introduces new claims.
            // Previously informed readers can still receive
            // and physically circulate the printed issue.
            bool firstReading = _knowledge.ReceiveIssue(
                item.RecipientNodeId,
                _issue);

            if (firstReading)
                NewReaderOpenings++;
            else
                RepeatedReaderOpenings++;

            _pendingReaders.Remove(item.RecipientNodeId);

            _attachments.Remove(item.ShipmentId);
            _shipmentPairs.Remove(item.ShipmentId);
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed)
                return;

            // Initial Layer 3 bulk allocation happens on
            // day one. Postal inserts begin afterwards.
            if (!_enabled && day >= 1)
                _enabled = true;

            if (_attachments.Count == 0)
                return;

            var returned = new List<string>();

            foreach (var pair in _attachments)
            {
                var shipment = pair.Value;

                if (shipment.Status !=
                    PostalShipmentStatus.Returned)
                {
                    continue;
                }

                _bulk.ReturnPostalInsert(
                    _issue, shipment);

                _pendingReaders.Remove(
                    shipment.Package.TargetNodeId);

                // A genuinely returned parcel must be
                // eligible for retry after address repair.
                if (_shipmentPairs.TryGetValue(
                        shipment.Id, out var route))
                {
                    _usedPairs.Remove(route);
                    _shipmentPairs.Remove(shipment.Id);
                }

                ReturnedCount++;
                returned.Add(pair.Key);
            }

            foreach (string id in returned)
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