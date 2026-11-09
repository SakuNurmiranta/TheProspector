
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class ZineSixAxisContactCandidate
    {
        public string TargetNodeId { get; }
        public string PublishedAddressId { get; }
        public int MatchingAxes { get; }

        internal ZineSixAxisContactCandidate(
            string targetNodeId,
            string publishedAddressId,
            int matchingAxes)
        {
            TargetNodeId = targetNodeId;
            PublishedAddressId = publishedAddressId;
            MatchingAxes = matchingAxes;
        }
    }

    /// <summary>
    /// Evaluates potential first contacts from a published
    /// zine directory.
    ///
    /// Compares the reader's actual personal preference
    /// against the editor's issue-specific description
    /// of the target person.
    ///
    /// Does not compare two cassette demos.
    /// Does not send mail or create network arcs.
    /// </summary>
    public sealed class ZineSixAxisContactEligibility
    {
        private readonly Network _network;
        private readonly ZineIssueKnowledgeRegistry _knowledge;
        private readonly ZineEditorialIssue _issue;
        private readonly SixAxisProfileCatalog _profiles;

        public int MinimumMatchingAxes { get; }

        public ZineSixAxisContactEligibility(
            Network network,
            ZineIssueKnowledgeRegistry knowledge,
            ZineEditorialIssue issue,
            SixAxisProfileCatalog profiles,
            int minimumMatchingAxes = 4)
        {
            _network = network ??
                throw new ArgumentNullException(nameof(network));

            _knowledge = knowledge ??
                throw new ArgumentNullException(nameof(knowledge));

            _issue = issue ??
                throw new ArgumentNullException(nameof(issue));

            _profiles = profiles ??
                throw new ArgumentNullException(nameof(profiles));

            if (minimumMatchingAxes < 0 ||
                minimumMatchingAxes > SixAxisProfile.AxisCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumMatchingAxes));
            }

            MinimumMatchingAxes = minimumMatchingAxes;

            foreach (var entry in _issue.Entries)
            {
                if (entry.Kind != ZineEditorialEntryKind.Address)
                    continue;

                _profiles.GetProfile(
                    SixAxisProfileRole.PublishedContactDescription,
                    entry.SubjectNodeId,
                    _issue.ProductId);
            }
        }

        public SixAxisProfile GetPersonalProfile(string nodeId)
        {
            return _profiles.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                nodeId);
        }

        public SixAxisProfile GetEditorialDescription(
            string nodeId)
        {
            return _profiles.GetProfile(
                SixAxisProfileRole.PublishedContactDescription,
                nodeId,
                _issue.ProductId);
        }

        public IReadOnlyList<ZineSixAxisContactCandidate>
            GetCandidates(string readerNodeId)
        {
            var reader = GetPersonalProfile(readerNodeId);

            if (!_knowledge.HasReceived(
                    readerNodeId,
                    _issue.ProductId))
            {
                return Array.Empty<
                    ZineSixAxisContactCandidate>();
            }

            var existing = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var arc in
                     _network.GetOutgoingConnections(
                         readerNodeId,
                         CommunicationLayer.Interpersonal))
            {
                existing.Add(arc.TargetNodeId);
            }

            var seen = new HashSet<string>(
                StringComparer.Ordinal);

            var candidates =
                new List<ZineSixAxisContactCandidate>();

            foreach (var claim in
                     _knowledge.GetClaims(readerNodeId))
            {
                if (claim.IssueProductId !=
                        _issue.ProductId ||
                    claim.Entry.Kind !=
                        ZineEditorialEntryKind.Address)
                {
                    continue;
                }

                var entry = claim.Entry;
                string target = entry.SubjectNodeId;

                if (!seen.Add(target) ||
                    target == readerNodeId ||
                    existing.Contains(target) ||
                    !_network.TryGetNode(target, out _))
                {
                    continue;
                }

                if (!_network.TryGetKnownAddress(
                        readerNodeId,
                        target,
                        out string knownAddress) ||
                    knownAddress != entry.AddressId)
                {
                    continue;
                }

                int matching = reader.MatchingAxes(
                    GetEditorialDescription(target));

                if (matching < MinimumMatchingAxes)
                    continue;

                candidates.Add(
                    new ZineSixAxisContactCandidate(
                        target,
                        entry.AddressId,
                        matching));
            }

            candidates.Sort((a, b) =>
                string.CompareOrdinal(
                    a.TargetNodeId,
                    b.TargetNodeId));

            return candidates.AsReadOnly();
        }
    }
}
