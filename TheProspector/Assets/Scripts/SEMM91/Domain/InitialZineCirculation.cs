using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// The first, already published zine issue in a configured world.
    ///
    /// Day 0: the editor knows the issue and owns its print run.
    /// Day 1: finite Layer 3 distribution to selected distributors.
    ///
    /// Does not create interpersonal contacts, editorial decisions,
    /// follow-up issues, or automatic downstream forwarding.
    /// </summary>
    public sealed class InitialZineCirculation
    {
        private readonly ConfigurableCirculationScenario _scenario;

        private readonly List<string> _distributors;

        private readonly IReadOnlyList<string> _distributorView;

        private readonly List<ZineBulkShipment>
            _forwardedBulkShipments = new();

        public IReadOnlyList<ZineBulkShipment>
            ForwardedBulkShipments =>
            _forwardedBulkShipments.AsReadOnly();

        private readonly Dictionary<string, string>
            _advertisedDemoIds;

        public ZineEditorialIssue Issue { get; }

        public ZineIssueKnowledgeRegistry Knowledge { get; }

        public InsertDistributionRegistry Inserts { get; }

        public ZineIssueInsertDelivery Delivery { get; }

        public ZinePhysicalBulkDistribution Bulk { get; }

        public IReadOnlyList<string> DistributorNodeIds =>
            _distributorView;

        /// <summary>
        /// Initial directory metadata associating each listed person
        /// with the distinct demo they possess at initialization.
        ///
        /// The older ZineEditorialEntry schema does not yet serialize
        /// this association into a readable editorial claim.
        /// </summary>
        public IReadOnlyDictionary<string, string>
            AdvertisedDemoIds => _advertisedDemoIds;

        public int PrintedCopies =>
            Bulk.GetPrintedCopies(Issue);

        public InitialZineCirculation(
            CirculationInitialWorld initial,
            ConfigurableCirculationScenario scenario,
            int distributorCount = 5,
            int nodesPerCopy = 4,
            bool enableLocalForwarding = false)
        {
            if (initial == null)
                throw new ArgumentNullException(nameof(initial));

            _scenario = scenario ??
                        throw new ArgumentNullException(nameof(scenario));

            if (distributorCount < 2)
                throw new ArgumentOutOfRangeException(
                    nameof(distributorCount));

            if (nodesPerCopy < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(nodesPerCopy));

            var network = scenario.Network;

            var entries = new List<ZineEditorialEntry>();

            _advertisedDemoIds =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            foreach (var listedId in initial.ZineListedNodeIds)
            {
                if (!network.TryGetNodeAddress(
                        listedId, out var addressId))
                    throw new InvalidOperationException(
                        "Listed person has no physical address.");

                entries.Add(
                    ZineEditorialEntry.ForAddress(
                        listedId, addressId));

                var ownedSeeds = initial.Definition.Seeds
                    .Where(s => s.SourceNodeId == listedId)
                    .ToList();

                if (ownedSeeds.Count != 1)
                    throw new InvalidOperationException(
                        "Each listed person must have one initial demo.");

                _advertisedDemoIds.Add(
                    listedId,
                    ownedSeeds[0].DemoId);
            }

            Issue = new ZineEditorialIssue(
                "INITIAL-ZINE",
                initial.EditorNodeId,
                1,
                0,
                false,
                entries);

            Knowledge =
                new ZineIssueKnowledgeRegistry(network);

            Inserts = new InsertDistributionRegistry();

            Delivery = new ZineIssueInsertDelivery(
                Inserts, Knowledge);

            Bulk = new ZinePhysicalBulkDistribution(
                Delivery);

            Bulk.PrintIssue(
                Issue,
                network.NodeCount,
                nodesPerCopy);

            // The editor already possesses their own issue.
            // No other explicit person is informed on day zero.
            Knowledge.ReceiveIssue(
                initial.EditorNodeId, Issue);

            _distributors = ChooseDistributors(
                initial,
                network,
                distributorCount);

            _distributorView =
                _distributors.AsReadOnly();

            // Schedule physical Layer 3 distribution.
            // No new personal correspondence is implied.
            scenario.Clock.ScheduleAfter(
                "INITIAL-ZINE-BULK:" + Issue.ProductId,
                1,
                DispatchInitialCopies);

            if (enableLocalForwarding)
                scenario.Clock.ScheduleAfter(
                    "INITIAL-ZINE-FORWARD:" + Issue.ProductId,
                    2,
                    ForwardInitialDistributorCopies);
        }

        private static List<string> ChooseDistributors(
            CirculationInitialWorld initial,
            Network network,
            int count)
        {
            var editorId = initial.EditorNodeId;

            var countryA = initial.Definition.Nodes
                .Where(n => n.CountryCode == "A")
                .Select(n => n.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            var countryB = initial.Definition.Nodes
                .Where(n =>
                    n.CountryCode == "B" &&
                    n.Id != editorId)
                .Select(n => n.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            if (countryA.Count == 0 ||
                countryB.Count < count - 1)
                throw new InvalidOperationException(
                    "Not enough persons for initial distributors.");

            var chosen = new List<string>();
            var chosenSet = new HashSet<string>(
                StringComparer.Ordinal);

            // One initial distributor in country A.
            // This can be a manually selected Layer 3 distributor
            // without an existing interpersonal relationship.
            chosen.Add(countryA[0]);
            chosenSet.Add(countryA[0]);

            // Prefer direct Layer 2 contacts of the editor in B.
            var existingPersonalContacts = network
                .GetOutgoingConnections(
                    editorId,
                    CommunicationLayer.Interpersonal)
                .Select(c => c.TargetNodeId)
                .Where(id => countryB.Contains(id))
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal);

            foreach (var id in existingPersonalContacts)
            {
                if (chosen.Count == count)
                    break;

                if (chosenSet.Add(id))
                    chosen.Add(id);
            }

            // Fill remaining slots editorially/manual-like.
            // These do not create interpersonal connections.
            foreach (var id in countryB)
            {
                if (chosen.Count == count)
                    break;

                if (chosenSet.Add(id))
                    chosen.Add(id);
            }

            if (chosen.Count != count)
                throw new InvalidOperationException(
                    "Could not select enough distributors.");

            return chosen;
        }

        private void DispatchInitialCopies()
        {
            var network = _scenario.Network;

            var routes = new List<NetworkConnection>();

            foreach (var distributorId in _distributors)
            {
                var routeId =
                    "INITIAL-ZINE-L3:" +
                    Issue.EditorNodeId + ":" +
                    distributorId;

                network.AddConnection(
                    routeId,
                    Issue.EditorNodeId,
                    distributorId,
                    CommunicationLayer.Insert,
                    "zine-distributor");

                var route = network
                    .GetOutgoingConnections(
                        Issue.EditorNodeId,
                        CommunicationLayer.Insert)
                    .Single(c => c.Id == routeId);

                routes.Add(route);
            }

            Bulk.DistributeInitial(Issue, routes);
        }

        private string GetNodeCountry(string nodeId)
        {
            var network = _scenario.Network;

            if (!network.TryGetNodeAddress(
                    nodeId, out var addressId) ||
                !network.TryGetAddress(
                    addressId, out var address))
                throw new InvalidOperationException(
                    "Node has no valid current address: " + nodeId);

            return address.CountryId;
        }

        /// <summary>
        /// One finite local forwarding wave after initial allocation.
        ///
        /// Uses existing Layer 3 stock transfer. Does not create
        /// interpersonal relationships, letters, cassette copies
        /// or additional printed zine copies.
        /// </summary>
        private void ForwardInitialDistributorCopies()
        {
            var network = _scenario.Network;

            foreach (var distributor in _distributors)
            {
                var owned = Bulk.GetCopies(Issue, distributor);

                // Keep one issue copy with the original distributor.
                var budget = Math.Min(4, Math.Max(0, owned - 1));

                if (budget == 0)
                    continue;

                var country = GetNodeCountry(distributor);

                var personalContacts = new HashSet<string>(
                    network.GetOutgoingConnections(
                            distributor,
                            CommunicationLayer.Interpersonal)
                        .Select(c => c.TargetNodeId),
                    StringComparer.Ordinal);

                // Prefer known personal contacts. If insufficient,
                // Layer 3 may also use manually selected local
                // distribution recipients without personal arcs.
                var recipients = _scenario.NodeIds
                    .Where(id => id != distributor)
                    .Where(id => GetNodeCountry(id) == country)
                    .OrderByDescending(id =>
                        personalContacts.Contains(id))
                    .ThenBy(id => id, StringComparer.Ordinal)
                    .ToList();

                foreach (var recipient in recipients)
                {
                    if (budget == 0)
                        break;

                    // A recipient already informed through another
                    // distributor does not need another issue copy.
                    if (Knowledge.HasReceived(
                            recipient, Issue.ProductId))
                        continue;

                    var routeId =
                        "INITIAL-ZINE-FORWARD:" +
                        Issue.ProductId + ":" +
                        distributor + ":" + recipient;

                    network.AddConnection(
                        routeId,
                        distributor,
                        recipient,
                        CommunicationLayer.Insert,
                        "zine-local-forward");

                    var route = network.GetOutgoingConnections(
                            distributor,
                            CommunicationLayer.Insert)
                        .Single(c => c.Id == routeId);

                    // Transfer one EXISTING physical issue copy.
                    var shipment = Bulk.Transfer(
                        Issue,
                        route,
                        1);

                    _forwardedBulkShipments.Add(shipment);

                    budget--;
                }
            }
        }
    }
}