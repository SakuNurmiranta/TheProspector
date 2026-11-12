using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class ZineFirstContactReadinessNode
    {
        public string NodeId { get; }
        public string CountryId { get; }

        public bool HasIssue { get; }
        public bool PendingOutreach { get; }

        public int HeldCassetteCount { get; }
        public int SupportedCassetteCount { get; }

        public int ValidPublishedAddresses { get; }
        public int MatchingPublishedContacts { get; }

        public int CandidateCount { get; }
        public int InternationalCandidates { get; }

        public string Blocker { get; }

        public bool Ready => Blocker == "Ready";

        internal ZineFirstContactReadinessNode(
            string nodeId,
            string countryId,
            bool hasIssue,
            bool pendingOutreach,
            int heldCassetteCount,
            int supportedCassetteCount,
            int validPublishedAddresses,
            int matchingPublishedContacts,
            int candidateCount,
            int internationalCandidates)
        {
            NodeId = nodeId;
            CountryId = countryId;

            HasIssue = hasIssue;
            PendingOutreach = pendingOutreach;

            HeldCassetteCount = heldCassetteCount;
            SupportedCassetteCount = supportedCassetteCount;

            ValidPublishedAddresses = validPublishedAddresses;
            MatchingPublishedContacts = matchingPublishedContacts;

            CandidateCount = candidateCount;
            InternationalCandidates = internationalCandidates;

            if (!hasIssue)
                Blocker = "IssueNotReceived";
            else if (heldCassetteCount == 0)
                Blocker = "NoPhysicalCassette";
            else if (supportedCassetteCount == 0)
                Blocker = "NoSupportedCassette";
            else if (validPublishedAddresses == 0)
                Blocker = "NoValidPublishedAddress";
            else if (matchingPublishedContacts == 0)
                Blocker = "BelowContactMatchingThreshold";
            else if (candidateCount == 0)
                Blocker = "NoEligibleContact";
            else if (pendingOutreach)
                Blocker = "PendingOutreach";
            else
                Blocker = "Ready";
        }

        public override string ToString()
        {
            return
                "Node=" + NodeId +
                " Country=" + CountryId +
                " Issue=" + HasIssue +
                " Held=" + HeldCassetteCount +
                " Supported=" + SupportedCassetteCount +
                " ValidAddresses=" + ValidPublishedAddresses +
                " MatchingContacts=" + MatchingPublishedContacts +
                " Candidates=" + CandidateCount +
                " InternationalCandidates=" +
                    InternationalCandidates +
                " Pending=" + PendingOutreach +
                " Blocker=" + Blocker;
        }
    }

    /// <summary>
    /// Read-only diagnostic snapshot of first-contact readiness.
    ///
    /// Uses existing issue knowledge, inventory, six-axis
    /// eligibility, listening decisions and postal outreach.
    /// </summary>
    public sealed class ZineFirstContactReadinessReport
    {
        public int Day { get; }

        public IReadOnlyList<ZineFirstContactReadinessNode>
            Nodes { get; }

        public int CountryAReaders =>
            Nodes.Count(n =>
                n.CountryId == "A" && n.HasIssue);

        public int CountryBReaders =>
            Nodes.Count(n =>
                n.CountryId == "B" && n.HasIssue);

        public int ReadyNodes =>
            Nodes.Count(n => n.Ready);

        public int ReadyCountryA =>
            Nodes.Count(n =>
                n.CountryId == "A" && n.Ready);

        public int CrossBorderReadyCountryA =>
            Nodes.Count(n =>
                n.CountryId == "A" &&
                n.Ready &&
                n.InternationalCandidates > 0);

        private ZineFirstContactReadinessReport(
            int day,
            IReadOnlyList<ZineFirstContactReadinessNode>
                nodes)
        {
            Day = day;
            Nodes = nodes;
        }

        private static string CountryOf(
            Network network,
            string nodeId)
        {
            if (!network.TryGetNodeAddress(
                    nodeId, out string addressId) ||
                !network.TryGetAddress(
                    addressId, out var address))
            {
                throw new InvalidOperationException(
                    "Node has no valid address: " + nodeId);
            }

            return address.CountryId;
        }

        public static ZineFirstContactReadinessReport Capture(
            DemoSession session)
        {
            if (session == null)
                throw new ArgumentNullException(
                    nameof(session));

            if (session.InitialZine == null ||
                session.SixAxisContacts == null ||
                session.FirstContactPostal == null)
            {
                throw new InvalidOperationException(
                    "A configured six-axis session is required.");
            }

            var scenario = session.Scenario;
            var network = scenario.Network;
            var zine = session.InitialZine;
            var contacts = session.SixAxisContacts;

            var rows =
                new List<ZineFirstContactReadinessNode>();

            foreach (string nodeId in scenario.NodeIds)
            {
                string country = CountryOf(network, nodeId);

                bool hasIssue = zine.Knowledge.HasReceived(
                    nodeId,
                    zine.Issue.ProductId);

                var held = scenario.Inventory
                    .GetHeldCopies(nodeId);

                var supported = new HashSet<string>(
                    scenario.GetReactionPolicy(nodeId)
                        .Decisions
                        .Where(d =>
                            d.Reaction ==
                            ListeningReaction.SupportAndCopy)
                        .Select(d => d.ProductId),
                    StringComparer.Ordinal);

                int supportedCount = held.Count(copy =>
                    supported.Contains(copy.ProductId));

                int validAddresses = 0;
                int matchingContacts = 0;

                if (hasIssue)
                {
                    var personal =
                        contacts.GetPersonalProfile(nodeId);

                    var existing = new HashSet<string>(
                        network.GetOutgoingConnections(
                                nodeId,
                                CommunicationLayer.Interpersonal)
                            .Select(c => c.TargetNodeId),
                        StringComparer.Ordinal);

                    foreach (var entry in zine.Issue.Entries)
                    {
                        if (entry.Kind !=
                            ZineEditorialEntryKind.Address)
                        {
                            continue;
                        }

                        string target = entry.SubjectNodeId;

                        if (target == nodeId ||
                            existing.Contains(target))
                        {
                            continue;
                        }

                        if (!network.TryGetKnownAddress(
                                nodeId,
                                target,
                                out string known) ||
                            known != entry.AddressId)
                        {
                            continue;
                        }

                        validAddresses++;

                        var published =
                            contacts.GetEditorialDescription(
                                target);

                        if (personal.MatchingAxes(published) >=
                            contacts.MinimumMatchingAxes)
                        {
                            matchingContacts++;
                        }
                    }
                }

                var candidates =
                    contacts.GetCandidates(nodeId);

                int international = candidates.Count(c =>
                    CountryOf(
                        network,
                        c.TargetNodeId) != country);

                rows.Add(
                    new ZineFirstContactReadinessNode(
                        nodeId,
                        country,
                        hasIssue,
                        session.FirstContactPostal
                            .HasPendingOutreach(nodeId),
                        held.Count,
                        supportedCount,
                        validAddresses,
                        matchingContacts,
                        candidates.Count,
                        international));
            }

            rows.Sort((a, b) =>
                string.CompareOrdinal(
                    a.NodeId, b.NodeId));

            return new ZineFirstContactReadinessReport(
                session.Day,
                rows.AsReadOnly());
        }

        public override string ToString()
        {
            return
                "Day=" + Day +
                " AReaders=" + CountryAReaders +
                " BReaders=" + CountryBReaders +
                " Ready=" + ReadyNodes +
                " ReadyA=" + ReadyCountryA +
                " CrossBorderReadyA=" +
                    CrossBorderReadyCountryA;
        }
    }
}