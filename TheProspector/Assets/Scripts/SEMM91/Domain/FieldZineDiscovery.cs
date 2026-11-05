
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Zinen numeroiden passiivinen löytyminen
    /// paikallisesta kentästä.
    ///
    /// Numero on ensin saatava todelliselle
    /// kenttään kuuluvalle solmulle.
    ///
    /// Löytyminen ei muodosta verkkoyhteyksiä
    /// eikä muuta solmun yksityisiä mielipiteitä.
    /// </summary>
    public sealed class FieldZineDiscovery
    {
        private sealed class AvailableIssue
        {
            public ZineEditorialIssue Issue { get; }
            public double Reach { get; }

            public AvailableIssue(
                ZineEditorialIssue issue,
                double reach)
            {
                Issue = issue;
                Reach = reach;
            }
        }

        private readonly Field _field;
        private readonly ZineIssueKnowledgeRegistry _knowledge;

        private readonly Dictionary<string, AvailableIssue>
            _available = new Dictionary<string, AvailableIssue>(
                StringComparer.Ordinal);

        public int AvailableIssueCount => _available.Count;

        public FieldZineDiscovery(
            Field field,
            ZineIssueKnowledgeRegistry knowledge)
        {
            _field = field ??
                throw new ArgumentNullException(nameof(field));

            _knowledge = knowledge ??
                throw new ArgumentNullException(
                    nameof(knowledge));
        }

        /// <summary>
        /// Tekee numeron kentässä löydettäväksi.
        ///
        /// Lähteen täytyy kuulua kenttään ja
        /// olla todella vastaanottanut numero.
        ///
        /// Reach on tämän ensimmäisen version
        /// eksplisiittinen levikkiparametri [0,1].
        /// </summary>
        public bool Introduce(
            string sourceNodeId,
            ZineEditorialIssue issue,
            double reach)
        {
            ValidateId(sourceNodeId, nameof(sourceNodeId));

            if (issue == null)
                throw new ArgumentNullException(nameof(issue));

            if (double.IsNaN(reach) ||
                double.IsInfinity(reach) ||
                reach < 0.0 || reach > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reach));
            }

            if (!_field.ContainsLocalNode(sourceNodeId))
                return false;

            // Pelkkä tieto siitä, että julkaisu
            // on olemassa, ei oikeuta levittämään
            // sitä kaikkitietävästi kenttään.
            if (!_knowledge.HasReceived(
                    sourceNodeId, issue.ProductId))
            {
                return false;
            }

            if (_available.TryGetValue(
                    issue.ProductId, out var existing))
            {
                if (!ReferenceEquals(
                        existing.Issue, issue))
                {
                    throw new InvalidOperationException(
                        "Conflicting issue identity.");
                }

                return false;
            }

            _available.Add(
                issue.ProductId,
                new AvailableIssue(issue, reach));

            return true;
        }

        /// <summary>
        /// Yksi kenttään kuuluvan solmun
        /// löytämisyritys.
        ///
        /// Onnistunut löytö käyttää samaa
        /// ReceiveIssue()-metodia kuin zinen
        /// tavallinen jakelujärjestelmä.
        /// </summary>
        public bool TryDiscover(
            string readerNodeId,
            string issueProductId,
            bool readerPrefersTrue,
            IReadOnlyDictionary<string, int> capitalByNode,
            Random random)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));
            ValidateId(issueProductId, nameof(issueProductId));

            if (capitalByNode == null)
                throw new ArgumentNullException(
                    nameof(capitalByNode));

            if (random == null)
                throw new ArgumentNullException(nameof(random));

            if (!_field.ContainsLocalNode(readerNodeId))
                return false;

            if (!_available.TryGetValue(
                    issueProductId, out var available))
            {
                return false;
            }

            var issue = available.Issue;

            // Nykyinen boolean-arvoprofiilin korvike.
            if (issue.Profile != readerPrefersTrue)
                return false;

            // Sama julkaisu ei tuota samaa
            // tietoa uudestaan.
            if (_knowledge.HasReceived(
                    readerNodeId, issueProductId))
            {
                return false;
            }

            var shares = _field.CalculateShares(
                capitalByNode);

            double probability =
                available.Reach * shares[readerNodeId];

            if (probability <= 0.0)
                return false;

            double sample = random.NextDouble();

            if (double.IsNaN(sample) ||
                sample < 0.0 || sample >= 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(random),
                    "Random sample must be in [0,1).");
            }

            if (sample >= probability)
                return false;

            // Kenttä ei luo omaa vaihtoehtoista
            // zinetietoa. Käytetään nykyistä
            // vastaanottajakohtaista rekisteriä.
            return _knowledge.ReceiveIssue(
                readerNodeId, issue);
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
            }
        }
    }
}
