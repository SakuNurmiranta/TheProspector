using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class MultiIssueZinePostalInsertCirculation
        : IDisposable
    {
        private sealed class Attachment
        {
            public ZineEditorialIssue Issue;
            public PostalShipment Shipment;
        }

        private readonly RecurringZineCirculation _recurring;
        private readonly ZinePhysicalBulkDistribution _bulk;
        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly ZinePostalInsertCirculation _initial;
        private readonly TimedPostalExchange _exchange;
        private readonly SimulationWorld _world;

        private readonly Dictionary<string, Attachment> _active =
            new Dictionary<string, Attachment>(
                StringComparer.Ordinal);

        // Retained for diagnostics after opening or return.
        private readonly Dictionary<string, string> _history =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        private readonly HashSet<
            (string Issue, string Sender, string Recipient)>
            _sentPairs =
                new HashSet<
                    (string Issue, string Sender, string Recipient)>();

        private readonly HashSet<
            (string Issue, string Recipient)>
            _pendingReaders =
                new HashSet<(string Issue, string Recipient)>();

        private bool _disposed;

        public int AttachedCount { get; private set; }
        public int OpenedCount { get; private set; }
        public int ReturnedCount { get; private set; }

        public MultiIssueZinePostalInsertCirculation(
            RecurringZineCirculation recurring,
            InitialZineCirculation initial,
            ZinePostalInsertCirculation initialRouter,
            TimedPostalExchange exchange,
            SimulationWorld world)
        {
            _recurring = recurring ??
                throw new ArgumentNullException(
                    nameof(recurring));

            if (initial == null)
                throw new ArgumentNullException(
                    nameof(initial));

            _initial = initialRouter ??
                throw new ArgumentNullException(
                    nameof(initialRouter));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _world = world ??
                throw new ArgumentNullException(
                    nameof(world));

            _bulk = initial.Bulk;
            _knowledge = initial.Knowledge;

            // Initial issue router subscribes first.
            // It yields when a newer eligible issue exists.
            _initial.ShouldYieldToNewerIssue =
                CanAttachNewerIssue;

            _exchange.ShipmentSent += OnShipmentSent;
            _exchange.ShipmentDelivered +=
                OnShipmentDelivered;
            _exchange.MessageOpened += OnMessageOpened;
            _world.DayCompleted += OnDayCompleted;
        }

        public bool TryGetAttachedIssueId(
            string shipmentId,
            out string issueProductId)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException(
                    "Shipment ID is required.",
                    nameof(shipmentId));

            return _history.TryGetValue(
                shipmentId, out issueProductId);
        }

        public string GetAttachedIssueId(string shipmentId)
        {
            return TryGetAttachedIssueId(
                shipmentId, out string issueId)
                ? issueId
                : null;
        }

        public bool CanAttachNewerIssue(PostalShipment shipment)
        {
            return !_disposed &&
                   SelectIssue(shipment) != null;
        }

        private ZineEditorialIssue SelectIssue(
            PostalShipment shipment)
        {
            if (shipment == null ||
                shipment.Status !=
                    PostalShipmentStatus.InTransit)
            {
                return null;
            }

            if (_initial.HasAttachment(shipment.Id) ||
                _active.ContainsKey(shipment.Id))
            {
                return null;
            }

            string sender =
                shipment.Package.SourceNodeId;

            string recipient =
                shipment.Package.TargetNodeId;

            if (sender == recipient)
                return null;

            var issues = _recurring.Issues;

            // Pass 1: prefer recipients who have
            // not yet read the issue.
            //
            // Pass 2: an informed reader may also
            // receive a physical copy to pass on.
            for (int pass = 0; pass < 2; pass++)
            {
                bool requireUninformed = pass == 0;

                // Later issues are preferred.
                for (int i = issues.Count - 1; i >= 0; i--)
                {
                    var issue = issues[i];

                    if (_bulk.GetCopies(issue, sender) < 1)
                        continue;

                    // The sender must have had access
                    // to this issue's actual contents.
                    if (!_knowledge.HasReceived(
                            sender, issue.ProductId))
                    {
                        continue;
                    }

                    bool alreadyRead =
                        _knowledge.HasReceived(
                            recipient, issue.ProductId);

                    if (requireUninformed == alreadyRead)
                        continue;

                    if (_pendingReaders.Contains(
                            (issue.ProductId, recipient)))
                    {
                        continue;
                    }

                    if (_sentPairs.Contains((
                            issue.ProductId,
                            sender,
                            recipient)))
                    {
                        continue;
                    }

                    return issue;
                }
            }

            return null;
        }

        private void OnShipmentSent(
            PostalShipment shipment)
        {
            if (_disposed)
                return;

            var issue = SelectIssue(shipment);

            if (issue == null)
                return;

            _bulk.BeginPostalInsert(issue, shipment);

            var attachment = new Attachment
            {
                Issue = issue,
                Shipment = shipment
            };

            _active.Add(shipment.Id, attachment);
            _history.Add(shipment.Id, issue.ProductId);

            _sentPairs.Add((
                issue.ProductId,
                shipment.Package.SourceNodeId,
                shipment.Package.TargetNodeId));

            _pendingReaders.Add((
                issue.ProductId,
                shipment.Package.TargetNodeId));

            AttachedCount++;
        }

        private void OnShipmentDelivered(
            PostalShipment shipment)
        {
            if (_disposed || shipment == null)
                return;

            if (!_active.TryGetValue(
                    shipment.Id, out var attachment))
            {
                return;
            }

            // Physical ownership changes at delivery.
            // Published knowledge does not.
            _bulk.DeliverPostalInsert(
                attachment.Issue, shipment);
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            if (!_active.TryGetValue(
                    item.ShipmentId, out var attachment))
            {
                return;
            }

            if (attachment.Shipment.Status !=
                PostalShipmentStatus.Delivered)
            {
                return;
            }

            _knowledge.ReceiveIssue(
                item.RecipientNodeId,
                attachment.Issue);

            _pendingReaders.Remove((
                attachment.Issue.ProductId,
                item.RecipientNodeId));

            _active.Remove(item.ShipmentId);

            OpenedCount++;
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed || _active.Count == 0)
                return;

            var finished = new List<string>();

            foreach (var pair in _active)
            {
                var attachment = pair.Value;

                if (attachment.Shipment.Status !=
                    PostalShipmentStatus.Returned)
                {
                    continue;
                }

                var shipment = attachment.Shipment;
                var issue = attachment.Issue;

                _bulk.ReturnPostalInsert(issue, shipment);

                _pendingReaders.Remove((
                    issue.ProductId,
                    shipment.Package.TargetNodeId));

                // A returned letter may be retried
                // after its address has been corrected.
                _sentPairs.Remove((
                    issue.ProductId,
                    shipment.Package.SourceNodeId,
                    shipment.Package.TargetNodeId));

                finished.Add(pair.Key);
                ReturnedCount++;
            }

            foreach (string id in finished)
                _active.Remove(id);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.ShipmentSent -= OnShipmentSent;
            _exchange.ShipmentDelivered -=
                OnShipmentDelivered;
            _exchange.MessageOpened -= OnMessageOpened;
            _world.DayCompleted -= OnDayCompleted;

            _initial.ShouldYieldToNewerIssue = null;

            _disposed = true;
        }
    }
}