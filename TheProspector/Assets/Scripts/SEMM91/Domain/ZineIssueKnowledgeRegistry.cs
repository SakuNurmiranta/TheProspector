
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Yksi lukijan zinen kautta saama väite.
    ///
    /// Entry sisältää varsinaisen väitteen
    /// ja sen alkuperäisen lähdesolmun.
    /// </summary>
    public sealed class ZineKnowledgeClaim
    {
        public string ReaderNodeId { get; }
        public string IssueProductId { get; }
        public string EditorNodeId { get; }
        public int PublishedDay { get; }

        public ZineEditorialEntry Entry { get; }

        internal ZineKnowledgeClaim(
            string readerNodeId,
            ZineEditorialIssue issue,
            ZineEditorialEntry entry)
        {
            ReaderNodeId = readerNodeId;
            IssueProductId = issue.ProductId;
            EditorNodeId = issue.EditorNodeId;
            PublishedDay = issue.PublishedDay;
            Entry = entry;
        }
    }

    /// <summary>
    /// Solmukohtainen zine-tietorekisteri.
    ///
    /// Tallentaa julkaistut väitteet sellaisina
    /// kuin ne esiintyvät numeroissa.
    ///
    /// Ei muuta solmun yksityistä mielipidettä,
    /// skeptisyyttä tai verkon yhteyksiä.
    /// </summary>
    public sealed class ZineIssueKnowledgeRegistry
    {
        private readonly Dictionary<
            string, HashSet<string>>
            _receivedIssues =
                new Dictionary<string, HashSet<string>>(
                    StringComparer.Ordinal);

        private readonly Dictionary<
            string, List<ZineKnowledgeClaim>>
            _claims =
                new Dictionary<
                    string, List<ZineKnowledgeClaim>>(
                    StringComparer.Ordinal);

        public bool ReceiveIssue(
            string readerNodeId,
            ZineEditorialIssue issue)
        {
            ValidateId(
                readerNodeId,
                nameof(readerNodeId));

            if (issue == null)
                throw new ArgumentNullException(
                    nameof(issue));

            if (!_receivedIssues.TryGetValue(
                    readerNodeId, out var received))
            {
                received = new HashSet<string>(
                    StringComparer.Ordinal);

                _receivedIssues.Add(
                    readerNodeId, received);
            }

            // Sama numero kirjataan vain kerran,
            // vaikka se saapuisi usealta jakelijalta.
            if (!received.Add(issue.ProductId))
                return false;

            if (!_claims.TryGetValue(
                    readerNodeId, out var list))
            {
                list = new List<ZineKnowledgeClaim>();

                _claims.Add(readerNodeId, list);
            }

            foreach (var entry in issue.Entries)
            {
                list.Add(new ZineKnowledgeClaim(
                    readerNodeId,
                    issue,
                    entry));
            }

            return true;
        }

        public bool HasReceived(
            string readerNodeId,
            string issueProductId)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));
            ValidateId(issueProductId, nameof(issueProductId));

            return _receivedIssues.TryGetValue(
                readerNodeId, out var received)
                && received.Contains(issueProductId);
        }

        public int ReceivedIssueCount(string readerNodeId)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));

            return _receivedIssues.TryGetValue(
                readerNodeId, out var received)
                ? received.Count
                : 0;
        }

        public IReadOnlyList<ZineKnowledgeClaim> GetClaims(
            string readerNodeId)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));

            if (!_claims.TryGetValue(
                    readerNodeId, out var list))
            {
                return Array.Empty<ZineKnowledgeClaim>();
            }

            // Muuttumaton tilannekuva.
            return new List<ZineKnowledgeClaim>(
                list).AsReadOnly();
        }

        /// <summary>
        /// Palauttaa uusimmassa lukijan saamassa
        /// julkaisussa ilmoitetun osoitteen.
        ///
        /// Tämä ei takaa, että osoite on oikea.
        /// Vanhempi numero ei saa kumota
        /// myöhemmässä numerossa julkaistua
        /// korjausta.
        /// </summary>
        public string GetLatestPublishedAddress(
            string readerNodeId,
            string subjectNodeId)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));
            ValidateId(subjectNodeId, nameof(subjectNodeId));

            if (!_claims.TryGetValue(
                    readerNodeId, out var list))
            {
                return null;
            }

            string address = null;
            int newestPublicationDay = -1;

            foreach (var claim in list)
            {
                if (claim.Entry.Kind !=
                    ZineEditorialEntryKind.Address)
                {
                    continue;
                }

                if (!string.Equals(
                        claim.Entry.SubjectNodeId,
                        subjectNodeId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (claim.PublishedDay <
                    newestPublicationDay)
                {
                    continue;
                }

                newestPublicationDay =
                    claim.PublishedDay;

                address = claim.Entry.AddressId;
            }

            return address;
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
