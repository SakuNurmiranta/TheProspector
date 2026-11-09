
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
            int nodesPerCopy = 4)
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

            foreach (string listedId in initial.ZineListedNodeIds)
            {
                if (!network.TryGetNodeAddress(
                        listedId, out string addressId))
                {
                    throw new InvalidOperationException(
                        "Listed person has no physical address.");
                }

                entries.Add(
                    ZineEditorialEntry.ForAddress(
                        listedId, addressId));

                var ownedSeeds = initial.Definition.Seeds
                    .Where(s => s.SourceNodeId == listedId)
                    .ToList();

                if (ownedSeeds.Count != 1)
                {
                    throw new InvalidOperationException(
                        "Each listed person must have one initial demo.");
                }

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
        }

        private static List<string> ChooseDistributors(
            CirculationInitialWorld initial,
            Network network,
            int count)
        {
            string editorId = initial.EditorNodeId;

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
            {
                throw new InvalidOperationException(
                    "Not enough persons for initial distributors.");
            }

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

            foreach (string id in existingPersonalContacts)
            {
                if (chosen.Count == count)
                    break;

                if (chosenSet.Add(id))
                    chosen.Add(id);
            }

            // Fill remaining slots editorially/manual-like.
            // These do not create interpersonal connections.
            foreach (string id in countryB)
            {
                if (chosen.Count == count)
                    break;

                if (chosenSet.Add(id))
                    chosen.Add(id);
            }

            if (chosen.Count != count)
            {
                throw new InvalidOperationException(
                    "Could not select enough distributors.");
            }

            return chosen;
        }

        private void DispatchInitialCopies()
        {
            var network = _scenario.Network;

            var routes = new List<NetworkConnection>();

            foreach (string distributorId in _distributors)
            {
                string routeId =
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
