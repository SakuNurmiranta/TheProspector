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

        /// <summary>
        /// Enables postal forwarding after the initial bulk.
        /// The constructor's older parameter name is retained
        /// for source compatibility with Crucible 9.2.18.
        /// </summary>
        public bool Layer2ForwardingEnabled { get; }

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
                
                entries.Add(
                    ZineEditorialEntry.ForDemoAdvertisement(
                        listedId,
                        ownedSeeds[0].DemoId));
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

            Layer2ForwardingEnabled = enableLocalForwarding;
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

            // Initial physical demo ownership is known before
            // aesthetic profiles or autonomous behaviour begin.
            var initialDemoOwners = new HashSet<string>(
                initial.Definition.Seeds
                    .Select(s => s.SourceNodeId),
                StringComparer.Ordinal);

            // Count distinct reachable people, not parallel
            // edges of the directed multigraph.
            int PersonalContacts(string nodeId)
            {
                return network.GetOutgoingConnections(
                        nodeId,
                        CommunicationLayer.Interpersonal)
                    .Select(c => c.TargetNodeId)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
            }

            // Country A distributor selection:
            //
            // 1. Has an initial demo AND personal contacts.
            // 2. Otherwise prefer an initial demo owner.
            // 3. Otherwise prefer more personal contacts.
            // 4. Break ties by stable node ID.
            //
            // No aesthetic profiles, listening results,
            // future events or random rolls are inspected.
            var selectedA = countryA
                .Select(id => new
                {
                    Id = id,
                    HasDemo = initialDemoOwners.Contains(id),
                    Contacts = PersonalContacts(id)
                })
                .OrderByDescending(candidate =>
                    candidate.HasDemo &&
                    candidate.Contacts > 0)
                .ThenByDescending(candidate =>
                    candidate.HasDemo)
                .ThenByDescending(candidate =>
                    candidate.Contacts)
                .ThenBy(candidate =>
                        candidate.Id,
                    StringComparer.Ordinal)
                .First()
                .Id;

            var chosen = new List<string>();
            var chosenSet = new HashSet<string>(
                StringComparer.Ordinal);

            chosen.Add(selectedA);
            chosenSet.Add(selectedA);

            // Preserve the existing country B policy:
            // prefer the editor's established Layer 2 contacts.
            var countryBSet = new HashSet<string>(
                countryB,
                StringComparer.Ordinal);

            var existingPersonalContacts = network
                .GetOutgoingConnections(
                    editorId,
                    CommunicationLayer.Interpersonal)
                .Select(c => c.TargetNodeId)
                .Where(id => countryBSet.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal);

            foreach (var id in existingPersonalContacts)
            {
                if (chosen.Count == count)
                    break;

                if (chosenSet.Add(id))
                    chosen.Add(id);
            }

            // Fill remaining country B distributor positions
            // without creating personal relationships.
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
    }
}