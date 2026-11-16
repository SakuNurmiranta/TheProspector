using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class PublishedZineContact
    {
        public string SubjectNodeId { get; }
        public string AddressId { get; }
        public string IssueProductId { get; }
        public int PublishedDay { get; }

        internal PublishedZineContact(
            string subjectNodeId,
            string addressId,
            string issueProductId,
            int publishedDay)
        {
            SubjectNodeId = subjectNodeId;
            AddressId = addressId;
            IssueProductId = issueProductId;
            PublishedDay = publishedDay;
        }
    }

    /// <summary>
    /// A reader's directory of published contact addresses
    /// gathered from all received zine issues.
    ///
    /// This does not invent aesthetic profiles,
    /// cassette ownership or personal relationships.
    /// </summary>
    public sealed class MultiIssueZineContactDirectory
    {
        private readonly Network _network;
        private readonly ZineIssueKnowledgeRegistry _knowledge;

        public MultiIssueZineContactDirectory(
            Network network,
            ZineIssueKnowledgeRegistry knowledge)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));
        }

        public IReadOnlyList<PublishedZineContact>
            GetContacts(string readerNodeId)
        {
            if (string.IsNullOrWhiteSpace(readerNodeId))
                throw new ArgumentException(
                    "Reader ID is required.",
                    nameof(readerNodeId));

            var newest = new Dictionary<
                string, PublishedZineContact>(
                StringComparer.Ordinal);

            foreach (var claim in
                     _knowledge.GetClaims(readerNodeId))
            {
                var entry = claim.Entry;

                if (entry.Kind !=
                    ZineEditorialEntryKind.Address)
                {
                    continue;
                }

                string target = entry.SubjectNodeId;

                if (target == readerNodeId ||
                    !_network.TryGetNode(target, out _))
                {
                    continue;
                }

                var candidate = new PublishedZineContact(
                    target,
                    entry.AddressId,
                    claim.IssueProductId,
                    claim.PublishedDay);

                if (!newest.TryGetValue(
                        target, out var previous))
                {
                    newest.Add(target, candidate);
                    continue;
                }

                if (candidate.PublishedDay >
                    previous.PublishedDay)
                {
                    newest[target] = candidate;
                }
                else if (candidate.PublishedDay ==
                         previous.PublishedDay &&
                         string.CompareOrdinal(
                             candidate.IssueProductId,
                             previous.IssueProductId) > 0)
                {
                    newest[target] = candidate;
                }
            }

            var result = new List<PublishedZineContact>();

            foreach (var candidate in newest.Values)
            {
                // The reader's known address must agree
                // with the most recent received publication.
                if (!_network.TryGetKnownAddress(
                        readerNodeId,
                        candidate.SubjectNodeId,
                        out string knownAddress))
                {
                    continue;
                }

                if (knownAddress != candidate.AddressId)
                    continue;

                result.Add(candidate);
            }

            result.Sort((a, b) =>
                string.CompareOrdinal(
                    a.SubjectNodeId,
                    b.SubjectNodeId));

            return result.AsReadOnly();
        }

        public bool TryGetAdvertisedDemo(
            string readerNodeId,
            PublishedZineContact contact,
            out string demoId)
        {
            demoId = null;

            if (contact == null)
                return false;

            foreach (var claim in
                     _knowledge.GetClaims(readerNodeId))
            {
                if (claim.IssueProductId !=
                    contact.IssueProductId)
                {
                    continue;
                }

                if (claim.Entry.Kind !=
                    ZineEditorialEntryKind.DemoAdvertisement)
                {
                    continue;
                }

                if (claim.Entry.SubjectNodeId !=
                    contact.SubjectNodeId)
                {
                    continue;
                }

                demoId = claim.Entry.ProductId;
                return true;
            }

            return false;
        }
    }
}