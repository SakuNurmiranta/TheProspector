
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class ZineBulkShipment
    {
        public string IssueProductId { get; }
        public string ConnectionId { get; }
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }
        public int CopyCount { get; }

        internal ZineBulkShipment(
            string issueProductId,
            NetworkConnection route,
            int copyCount)
        {
            IssueProductId = issueProductId;
            ConnectionId = route.Id;
            SenderNodeId = route.SourceNodeId;
            RecipientNodeId = route.TargetNodeId;
            CopyCount = copyCount;
        }
    }

    /// <summary>
    /// Finite physical stock for individual zine issues.
    ///
    /// Layer 3: initial bulk distribution and direct forwarding.
    /// Layer 2: one issue copy may travel inside an existing
    /// cassette shipment, without creating another message.
    /// </summary>
    public sealed class ZinePhysicalBulkDistribution
    {
        private sealed class PrintRun
        {
            public ZineEditorialIssue Issue { get; }
            public int PrintedCopies { get; }
            public bool InitialAllocationCompleted { get; set; }

            public Dictionary<string, int> Stock { get; }

            // Shipment ID -> original sender.
            // One physical copy is reserved per shipment.
            public Dictionary<string, string> PostalInTransit { get; }

            public PrintRun(
                ZineEditorialIssue issue,
                int copies)
            {
                Issue = issue;
                PrintedCopies = copies;

                Stock = new Dictionary<string, int>(
                    StringComparer.Ordinal);

                Stock.Add(issue.EditorNodeId, copies);

                PostalInTransit =
                    new Dictionary<string, string>(
                        StringComparer.Ordinal);
            }
        }

        private readonly ZineIssueInsertDelivery _delivery;

        private readonly Dictionary<string, PrintRun> _runs =
            new Dictionary<string, PrintRun>(
                StringComparer.Ordinal);

        public ZinePhysicalBulkDistribution(
            ZineIssueInsertDelivery delivery)
        {
            _delivery = delivery ??
                throw new ArgumentNullException(
                    nameof(delivery));
        }

        public int PrintIssue(
            ZineEditorialIssue issue,
            int population,
            int nodesPerCopy = 4)
        {
            if (issue == null)
                throw new ArgumentNullException(
                    nameof(issue));

            if (population < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(population));

            if (nodesPerCopy < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(nodesPerCopy));

            if (_runs.ContainsKey(issue.ProductId))
                throw new InvalidOperationException(
                    "This issue has already been printed.");

            int copies = population / nodesPerCopy;

            if (population % nodesPerCopy != 0)
                copies = checked(copies + 1);

            _runs.Add(
                issue.ProductId,
                new PrintRun(issue, copies));

            return copies;
        }

        public int GetPrintedCopies(
            ZineEditorialIssue issue)
        {
            return GetRun(issue).PrintedCopies;
        }

        public int GetCopies(
            ZineEditorialIssue issue,
            string holderNodeId)
        {
            if (string.IsNullOrWhiteSpace(holderNodeId))
                throw new ArgumentException(
                    "Holder ID is required.",
                    nameof(holderNodeId));

            var stock = GetRun(issue).Stock;

            return stock.TryGetValue(
                holderNodeId, out int copies)
                    ? copies
                    : 0;
        }

        public int GetTotalCopiesHeld(
            ZineEditorialIssue issue)
        {
            int total = 0;

            foreach (int copies in GetRun(issue).Stock.Values)
                total = checked(total + copies);

            return total;
        }

        /// <summary>
        /// Includes copies travelling to a recipient or returning.
        ///
        /// Conservation invariant:
        /// held + postal in transit == printed,
        /// provided no further print runs are introduced.
        /// </summary>
        public int GetPostalInTransitCopies(
            ZineEditorialIssue issue)
        {
            return GetRun(issue).PostalInTransit.Count;
        }

        public IReadOnlyList<ZineBulkShipment> DistributeInitial(
            ZineEditorialIssue issue,
            IEnumerable<NetworkConnection> routes)
        {
            var run = GetRun(issue);

            if (routes == null)
                throw new ArgumentNullException(
                    nameof(routes));

            if (run.InitialAllocationCompleted)
                throw new InvalidOperationException(
                    "Initial distribution already completed.");

            if (GetCopies(issue, issue.EditorNodeId) !=
                run.PrintedCopies)
            {
                throw new InvalidOperationException(
                    "The print run has already been dispatched.");
            }

            var unique =
                new Dictionary<string, NetworkConnection>(
                    StringComparer.Ordinal);

            foreach (var route in routes)
            {
                ValidateRoute(
                    route,
                    issue.EditorNodeId);

                if (!unique.TryGetValue(
                        route.TargetNodeId,
                        out var previous) ||
                    string.CompareOrdinal(
                        route.Id, previous.Id) < 0)
                {
                    unique[route.TargetNodeId] = route;
                }
            }

            var selected =
                new List<NetworkConnection>(unique.Values);

            selected.Sort((a, b) =>
            {
                int target = string.CompareOrdinal(
                    a.TargetNodeId, b.TargetNodeId);

                return target != 0
                    ? target
                    : string.CompareOrdinal(a.Id, b.Id);
            });

            var shipments = new List<ZineBulkShipment>();

            if (selected.Count == 0)
                return shipments.AsReadOnly();

            int perDistributor =
                run.PrintedCopies / selected.Count;

            int remainder =
                run.PrintedCopies % selected.Count;

            for (int i = 0; i < selected.Count; i++)
            {
                int amount =
                    perDistributor + (i < remainder ? 1 : 0);

                if (amount == 0)
                    continue;

                shipments.Add(
                    Transfer(issue, selected[i], amount));
            }

            run.InitialAllocationCompleted = true;

            return shipments.AsReadOnly();
        }

        /// <summary>
        /// Layer 3: directly transfer a number of copies.
        /// The existing insert delivery informs the recipient.
        /// </summary>
        public ZineBulkShipment Transfer(
            ZineEditorialIssue issue,
            NetworkConnection route,
            int copies)
        {
            var run = GetRun(issue);

            ValidateRoute(
                route,
                route?.SourceNodeId);

            if (copies < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(copies));

            int owned = GetCopies(
                issue, route.SourceNodeId);

            if (owned < copies)
                throw new InvalidOperationException(
                    "Insufficient physical zine stock.");

            // Perform the informational delivery before
            // committing the physical stock transfer.
            _delivery.DistributePhysical(
                issue,
                new[] { route });

            run.Stock[route.SourceNodeId] =
                owned - copies;

            int recipientStock = GetCopies(
                issue, route.TargetNodeId);

            run.Stock[route.TargetNodeId] =
                checked(recipientStock + copies);

            return new ZineBulkShipment(
                issue.ProductId, route, copies);
        }

        /// <summary>
        /// Layer 2: reserve one existing physical zine copy
        /// for an already dispatched postal shipment.
        ///
        /// The postal shipment may contain a cassette, feedback,
        /// an address notice, or another valid letter payload.
        ///
        /// No new postal message or graph connection is created.
        /// </summary>
        public void BeginPostalInsert(
            ZineEditorialIssue issue,
            PostalShipment shipment)
        {
            var run = GetRun(issue);

            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (shipment.Status !=
                PostalShipmentStatus.InTransit)
            {
                throw new InvalidOperationException(
                    "Postal shipment is not in transit.");
            }

            if (run.PostalInTransit.ContainsKey(shipment.Id))
                throw new InvalidOperationException(
                    "Shipment already has a zine insert.");

            string sender = shipment.Package.SourceNodeId;

            int held = GetCopies(issue, sender);

            if (held < 1)
                throw new InvalidOperationException(
                    "Sender has no zine copy available.");

            run.PostalInTransit.Add(
                shipment.Id, sender);

            run.Stock[sender] = held - 1;
        }

        /// <summary>
        /// The postal service has successfully delivered
        /// the containing cassette shipment.
        ///
        /// Physical ownership changes now, but the recipient
        /// does not learn the issue's contents until opening.
        /// </summary>
        public void DeliverPostalInsert(
            ZineEditorialIssue issue,
            PostalShipment shipment)
        {
            var run = GetRun(issue);

            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (shipment.Status !=
                PostalShipmentStatus.Delivered)
            {
                throw new InvalidOperationException(
                    "Shipment has not been delivered.");
            }

            if (!run.PostalInTransit.TryGetValue(
                    shipment.Id, out string originalSender) ||
                originalSender != shipment.Package.SourceNodeId)
            {
                throw new InvalidOperationException(
                    "Unknown postal insert shipment.");
            }

            string recipient = shipment.Package.TargetNodeId;

            int current = GetCopies(issue, recipient);
            int updated = checked(current + 1);

            run.PostalInTransit.Remove(shipment.Id);
            run.Stock[recipient] = updated;
        }

        /// <summary>
        /// Reconcile a completed return to the original sender.
        /// A copy is never awarded to the intended recipient
        /// when its containing package was returned.
        /// </summary>
        public void ReturnPostalInsert(
            ZineEditorialIssue issue,
            PostalShipment shipment)
        {
            var run = GetRun(issue);

            if (shipment == null)
                throw new ArgumentNullException(
                    nameof(shipment));

            if (shipment.Status !=
                PostalShipmentStatus.Returned)
            {
                throw new InvalidOperationException(
                    "Shipment has not returned.");
            }

            if (!run.PostalInTransit.TryGetValue(
                    shipment.Id, out string sender) ||
                sender != shipment.Package.SourceNodeId)
            {
                throw new InvalidOperationException(
                    "Unknown returning postal insert.");
            }

            int held = GetCopies(issue, sender);
            int updated = checked(held + 1);

            run.PostalInTransit.Remove(shipment.Id);
            run.Stock[sender] = updated;
        }

        private PrintRun GetRun(
            ZineEditorialIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException(
                    nameof(issue));

            if (!_runs.TryGetValue(
                    issue.ProductId, out var run) ||
                !ReferenceEquals(run.Issue, issue))
            {
                throw new ArgumentException(
                    "Issue is not registered with this distributor.",
                    nameof(issue));
            }

            return run;
        }

        private static void ValidateRoute(
            NetworkConnection route,
            string expectedSource)
        {
            if (route == null)
                throw new ArgumentException(
                    "Distribution route cannot be null.",
                    nameof(route));

            if (route.Layer != CommunicationLayer.Insert)
                throw new ArgumentException(
                    "Physical bulk distribution requires Layer 3.",
                    nameof(route));

            if (!string.Equals(
                    route.SourceNodeId,
                    expectedSource,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Unexpected distribution sender.",
                    nameof(route));
            }

            if (string.Equals(
                    route.SourceNodeId,
                    route.TargetNodeId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Cannot send a zine to its holder.",
                    nameof(route));
            }
        }
    }
}
