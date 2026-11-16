
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public enum MultiIssueContactIntent
    {
        OfferDemo,
        RequestDemo
    }

    public sealed class MultiIssueContactOpportunity
    {
        public string TargetNodeId { get; }
        public string PublishedAddressId { get; }
        public string IssueProductId { get; }
        public string DemoId { get; }

        public MultiIssueContactIntent Intent { get; }
        public int MatchingAxes { get; }

        internal MultiIssueContactOpportunity(
            string targetNodeId,
            string publishedAddressId,
            string issueProductId,
            string demoId,
            MultiIssueContactIntent intent,
            int matchingAxes)
        {
            TargetNodeId = targetNodeId;
            PublishedAddressId = publishedAddressId;
            IssueProductId = issueProductId;
            DemoId = demoId;
            Intent = intent;
            MatchingAxes = matchingAxes;
        }
    }

    /// <summary>
    /// Produces first-contact opportunities from
    /// received zine issues after issue 1.
    ///
    /// Uses published six-axis claims rather than
    /// private information about the recipient.
    ///
    /// Does not send mail or create network arcs.
    /// </summary>
    public sealed class MultiIssueZineContactSelection
    {
        private const int MinimumMatchingAxes = 4;

        private readonly ConfigurableCirculationScenario
            _scenario;

        private readonly MultiIssueZineContactDirectory
            _directory;

        private readonly ZineIssueKnowledgeRegistry
            _knowledge;

        private readonly SixAxisProfileCatalog
            _profiles;

        public MultiIssueZineContactSelection(
            ConfigurableCirculationScenario scenario,
            MultiIssueZineContactDirectory directory,
            ZineIssueKnowledgeRegistry knowledge,
            SixAxisProfileCatalog profiles)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(
                    nameof(scenario));

            _directory = directory ??
                throw new ArgumentNullException(
                    nameof(directory));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));

            _profiles = profiles ??
                throw new ArgumentNullException(
                    nameof(profiles));
        }

        public IReadOnlyList<MultiIssueContactOpportunity>
            GetOpportunities(string readerNodeId)
        {
            if (string.IsNullOrWhiteSpace(readerNodeId))
                throw new ArgumentException(
                    "Reader ID is required.",
                    nameof(readerNodeId));

            var personal = _profiles.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                readerNodeId);

            var supported = new HashSet<string>(
                _scenario.GetReactionPolicy(readerNodeId)
                    .Decisions
                    .Where(d => d.Reaction ==
                        ListeningReaction.SupportAndCopy)
                    .Select(d => d.ProductId),
                StringComparer.Ordinal);

            var held = _scenario.Inventory
                .GetHeldCopies(readerNodeId)
                .Where(c => supported.Contains(c.ProductId))
                .GroupBy(c => c.ProductId)
                .Select(g => g.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var existing = new HashSet<string>(
                _scenario.Network
                    .GetOutgoingConnections(
                        readerNodeId,
                        CommunicationLayer.Interpersonal)
                    .Select(c => c.TargetNodeId),
                StringComparer.Ordinal);

            var claims = _knowledge.GetClaims(readerNodeId);

            var result =
                new List<MultiIssueContactOpportunity>();

            // Newer received publications are considered
            // before older ones.
            var contacts = _directory.GetContacts(readerNodeId)
                .Where(c => c.PublishedDay > 0)
                .OrderByDescending(c => c.PublishedDay)
                .ThenBy(c => c.SubjectNodeId,
                    StringComparer.Ordinal);

            foreach (var contact in contacts)
            {
                if (existing.Contains(contact.SubjectNodeId))
                    continue;

                var published = claims
                    .Where(c =>
                        c.IssueProductId ==
                            contact.IssueProductId &&
                        c.Entry.SubjectNodeId ==
                            contact.SubjectNodeId)
                    .Select(c => c.Entry)
                    .ToArray();

                MultiIssueContactOpportunity choice = null;

                // An offer uses the target's PUBLISHED
                // aesthetic description, if present.
                var description = published
                    .LastOrDefault(e =>
                        e.Kind ==
                            ZineEditorialEntryKind
                                .ContactSixAxisDescription &&
                        e.PublishedAxisMask.HasValue);

                if (description != null)
                {
                    var targetProfile = new SixAxisProfile(
                        description.PublishedAxisMask.Value);

                    foreach (string demoId in held)
                    {
                        if (!_profiles.TryGetProfile(
                                SixAxisProfileRole.DemoAtmosphere,
                                demoId,
                                out var demoProfile))
                        {
                            continue;
                        }

                        int matching = demoProfile.MatchingAxes(
                            targetProfile);

                        if (matching < MinimumMatchingAxes)
                            continue;

                        choice = new MultiIssueContactOpportunity(
                            contact.SubjectNodeId,
                            contact.AddressId,
                            contact.IssueProductId,
                            demoId,
                            MultiIssueContactIntent.OfferDemo,
                            matching);

                        break;
                    }
                }

                // A demo request needs the advertised
                // product AND its published description.
                // It does not require the reader to own
                // a cassette.
                if (choice == null)
                {
                    foreach (var entry in published)
                    {
                        if (entry.Kind !=
                                ZineEditorialEntryKind
                                    .DemoAdvertisement ||
                            !entry.PublishedAxisMask.HasValue)
                        {
                            continue;
                        }

                        var advertised = new SixAxisProfile(
                            entry.PublishedAxisMask.Value);

                        int matching = personal.MatchingAxes(
                            advertised);

                        if (matching < MinimumMatchingAxes)
                            continue;

                        choice = new MultiIssueContactOpportunity(
                            contact.SubjectNodeId,
                            contact.AddressId,
                            contact.IssueProductId,
                            entry.ProductId,
                            MultiIssueContactIntent.RequestDemo,
                            matching);

                        break;
                    }
                }

                if (choice != null)
                    result.Add(choice);
            }

            return result.AsReadOnly();
        }
    }
}
