
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Välittää julkaistuja zinen numeroita
    /// olemassa olevien Insert-yhteyksien kautta.
    ///
    /// Ei luo kasetteja, postipaketteja
    /// eikä toimituksellista sisältöä.
    /// </summary>
    public sealed class ZineIssueInsertDelivery
    {
        private readonly InsertDistributionRegistry _registry;

        private readonly ZineIssueKnowledgeRegistry _knowledge;

        private readonly Dictionary<
            string, ZineEditorialIssue> _issues =
                new Dictionary<
                    string, ZineEditorialIssue>(
                    StringComparer.Ordinal);

        private readonly HashSet<
            (string Issue, string Sender, string Recipient)>
            _delivered =
                new HashSet<
                    (string Issue, string Sender, string Recipient)>();

        public ZineIssueInsertDelivery(
            InsertDistributionRegistry registry,
            ZineIssueKnowledgeRegistry knowledge = null)
        {
            _registry = registry ??
                        throw new ArgumentNullException(nameof(registry));

            _knowledge = knowledge;
        }

        public ZineEditorialIssue GetIssue(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId) ||
                !_issues.TryGetValue(
                    productId, out var issue))
            {
                throw new ArgumentException(
                    "Unknown zine issue.",
                    nameof(productId));
            }

            return issue;
        }

        /// <summary>
        /// Physical delivery is not gated by the recipient's aesthetic taste.
        /// Keeps the existing Layer 3 exposure and issue knowledge registries.
        /// The finite copy count is managed by ZinePhysicalBulkDistribution.
        /// </summary>
        public IReadOnlyList<InsertExposure> DistributePhysical(
            ZineEditorialIssue issue,
            IEnumerable<NetworkConnection> routes)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));
            return Distribute(issue, routes, _ => issue.Profile);
        }

        public IReadOnlyList<InsertExposure> Distribute(
            ZineEditorialIssue issue,
            IEnumerable<NetworkConnection> routes,
            Func<string, bool> recipientPrefersTrue)
        {
            if (issue == null)
                throw new ArgumentNullException(
                    nameof(issue));

            if (routes == null)
                throw new ArgumentNullException(
                    nameof(routes));

            if (recipientPrefersTrue == null)
                throw new ArgumentNullException(
                    nameof(recipientPrefersTrue));

            if (_issues.TryGetValue(
                    issue.ProductId, out var existing))
            {
                if (!ReferenceEquals(existing, issue))
                    throw new InvalidOperationException(
                        "Conflicting zine issue identity.");
            }
            else
            {
                _issues.Add(issue.ProductId, issue);
            }

            var results = new List<InsertExposure>();

            foreach (var connection in routes)
            {
                if (connection == null)
                    throw new ArgumentException(
                        "Distribution route cannot be null.",
                        nameof(routes));

                if (connection.Layer != CommunicationLayer.Insert)
                    throw new ArgumentException(
                        "Zine distribution requires Insert layer.",
                        nameof(routes));

                string recipientId =
                    connection.TargetNodeId;

                // Boolean-arvoprofiilin korvike.
                // true-lukija vastaanottaa true-zinen
                // ja false-lukija false-zinen.
                if (recipientPrefersTrue(recipientId)
                    != issue.Profile)
                {
                    continue;
                }

                var key = (
                    issue.ProductId,
                    connection.SourceNodeId,
                    recipientId);

                // Sama jakelija ei toimita samaa
                // numeroa kahdesti samalle solmulle.
                if (_delivered.Contains(key))
                    continue;

                string insertId =
                    "ZINE-INSERT:" +
                    issue.ProductId + ":" +
                    connection.SourceNodeId + ":" +
                    recipientId;

                var exposure = _registry.DistributeById(
                    insertId,
                    connection,
                    issue.ProductId);

                _delivered.Add(key);
                results.Add(exposure);

                // Vastaanottaja saa numeron sisältämät
                // julkaistut tiedot käytettäväkseen.
                _knowledge?.ReceiveIssue(
                    recipientId,
                    issue);
            }

            return results.AsReadOnly();
        }
    }
}
