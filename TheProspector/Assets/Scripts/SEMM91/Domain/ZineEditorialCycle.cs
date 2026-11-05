
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ZineEditorialEntryKind
    {
        ProductOpinion,
        Skepticism,
        Address
    }

    /// <summary>
    /// Yksi julkaistavaksi kerätty tieto.
    ///
    /// SubjectNodeId:
    /// ProductOpinion = kannan esittänyt solmu
    /// Skepticism     = skeptinen solmu
    /// Address        = osoitteen omistava solmu
    ///
    /// Merkintä kuvaa väitettä, ei sen
    /// objektiivista paikkansapitävyyttä.
    /// </summary>
    public sealed class ZineEditorialEntry
    {
        public ZineEditorialEntryKind Kind { get; }

        public string SubjectNodeId { get; }
        public string ProductId { get; }
        public bool? Opinion { get; }

        public bool? SelfDeclaredPublicProfile { get; }
        
        public string TargetNodeId { get; }
        public string AddressId { get; }

        private ZineEditorialEntry(
            ZineEditorialEntryKind kind,
            string subjectNodeId,
            string productId = null,
            bool? opinion = null,
            string targetNodeId = null,
            string addressId = null,
            bool? selfDeclaredPublicProfile = null)
        {
            Kind = kind;
            SubjectNodeId = subjectNodeId;
            ProductId = productId;
            Opinion = opinion;
            TargetNodeId = targetNodeId;
            AddressId = addressId;
            SelfDeclaredPublicProfile = selfDeclaredPublicProfile;
        }

        internal static ZineEditorialEntry ForProductOpinion(
            string nodeId,
            string productId,
            bool opinion)
        {
            return new ZineEditorialEntry(
                ZineEditorialEntryKind.ProductOpinion,
                nodeId,
                productId: productId,
                opinion: opinion);
        }

        internal static ZineEditorialEntry ForSkepticism(
            string observerId,
            string targetId)
        {
            return new ZineEditorialEntry(
                ZineEditorialEntryKind.Skepticism,
                observerId,
                targetNodeId: targetId);
        }

        
        internal static ZineEditorialEntry ForAddress(
            string nodeId,
            string addressId,
            bool? selfDeclaredPublicProfile = null)
        {
            return new ZineEditorialEntry(
                ZineEditorialEntryKind.Address,
                nodeId,
                addressId: addressId,
                selfDeclaredPublicProfile: selfDeclaredPublicProfile);
        }

    }

    /// <summary>
    /// Julkaistu, muuttumaton zinen numero.
    /// Numerolla on oma kulttuurituotetunniste.
    /// </summary>
    public sealed class ZineEditorialIssue
    {
        public string ProductId { get; }
        public string ZineId { get; }
        public string EditorNodeId { get; }

        public int Number { get; }
        public int PublishedDay { get; }

        public bool Profile { get; }

        public IReadOnlyList<ZineEditorialEntry>
            Entries { get; }

        internal ZineEditorialIssue(
            string zineId,
            string editorNodeId,
            int number,
            int publishedDay,
            bool profile,
            IEnumerable<ZineEditorialEntry> entries)
        {
            ZineId = zineId;
            EditorNodeId = editorNodeId;

            Number = number;
            PublishedDay = publishedDay;
            Profile = profile;

            ProductId =
                "ZINE-ISSUE:" + zineId + ":" + number;

            // Erillinen lista estää toimituksen
            // uuden keräyspuskurin muuttamasta
            // jo julkaistua numeroa.
            Entries = new List<ZineEditorialEntry>(
                entries).AsReadOnly();
        }
    }

    /// <summary>
    /// Zinen toimituksellinen julkaisusykli.
    ///
    /// Kerää tiedot julkaisuhetkeen saakka
    /// ja julkaisee ne 30 päivän välein.
    ///
    /// Ei lähetä numeroita, luo verkon kaaria
    /// eikä muuta lukijoiden tietämystä.
    /// </summary>
    public sealed class ZineEditorialCycle
    {
        public const int PublicationIntervalDays = 30;

        private readonly SimulationClock _clock;

        private readonly List<ZineEditorialEntry>
            _pending = new List<ZineEditorialEntry>();

        private readonly List<ZineEditorialIssue>
            _issues = new List<ZineEditorialIssue>();

        private readonly IReadOnlyList<ZineEditorialIssue>
            _issueView;

        private int _nextIssueNumber = 1;

        public string ZineId { get; }
        public string EditorNodeId { get; }
        public bool Profile { get; }

        public event Action<ZineEditorialIssue> IssuePublished;
        
        public int PendingCount => _pending.Count;

        public IReadOnlyList<ZineEditorialIssue>
            Issues => _issueView;

        public ZineEditorialCycle(
            string zineId,
            string editorNodeId,
            bool profile,
            SimulationClock clock)
        {
            ValidateId(zineId, nameof(zineId));
            ValidateId(editorNodeId, nameof(editorNodeId));

            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            ZineId = zineId;
            EditorNodeId = editorNodeId;
            Profile = profile;

            _issueView = _issues.AsReadOnly();

            ScheduleNextPublication();
        }

        public void RecordProductOpinion(
            string opinionHolderNodeId,
            string productId,
            bool opinion)
        {
            ValidateId(
                opinionHolderNodeId,
                nameof(opinionHolderNodeId));

            ValidateId(
                productId,
                nameof(productId));

            _pending.Add(
                ZineEditorialEntry.ForProductOpinion(
                    opinionHolderNodeId,
                    productId,
                    opinion));
        }

        public void RecordSkepticism(
            string observerNodeId,
            string targetNodeId)
        {
            ValidateId(
                observerNodeId,
                nameof(observerNodeId));

            ValidateId(
                targetNodeId,
                nameof(targetNodeId));

            _pending.Add(
                ZineEditorialEntry.ForSkepticism(
                    observerNodeId,
                    targetNodeId));
        }

        public void RecordAddress(
            string nodeId,
            string addressId)
        {
            ValidateId(nodeId, nameof(nodeId));
            ValidateId(addressId, nameof(addressId));

            _pending.Add(
                ZineEditorialEntry.ForAddress(
                    nodeId,
                    addressId));
        }

        private void ScheduleNextPublication()
        {
            int issueNumber = _nextIssueNumber;

            _clock.ScheduleAfter(
                "ZINE-PUBLISH:" +
                ZineId + ":" + issueNumber,
                PublicationIntervalDays,
                Publish);
        }

        private void Publish()
        {
            var issue = new ZineEditorialIssue(
                ZineId,
                EditorNodeId,
                _nextIssueNumber,
                _clock.CurrentDay,
                Profile,
                _pending);

            _issues.Add(issue);

            // Kaikki juuri julkaistu aineisto
            // poistuu toimituksen puskurista.
            
            _pending.Clear();

            _nextIssueNumber++;

            ScheduleNextPublication();

// Ilmoita valmiista, muuttumattomasta
// numerosta jakelujärjestelmälle.
// Seuraavan numeron keräys on jo alkanut.
            IssuePublished?.Invoke(issue);

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
        
        
        public void RecordSelfDeclaredContact(
            string nodeId,
            string addressId,
            bool publicProfile)
        {
            ValidateId(nodeId, nameof(nodeId));
            ValidateId(addressId, nameof(addressId));

            _pending.Add(
                ZineEditorialEntry.ForAddress(
                    nodeId,
                    addressId,
                    publicProfile));
        }

    }
}
