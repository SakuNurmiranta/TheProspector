
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ZineEditorialEntryKind
    {
        ProductOpinion,
        Skepticism,
        Address,
        DemoAdvertisement,
        ContactSixAxisDescription
    }

    /// <summary>
    /// Yksi julkaistavaksi kerätty tieto.
    ///
    /// SubjectNodeId:
    /// ProductOpinion = kannan esittänyt solmu
    /// Skepticism = skeptinen solmu
    /// Address = osoitteen omistava solmu
    /// DemoAdvertisement = demon mainostava solmu
    /// ContactSixAxisDescription = kuvattu solmu
    ///
    /// Merkintä kuvaa väitettä, ei sen
    /// objektiivista paikkansapitävyyttä.
    /// </summary>
    public sealed class ZineEditorialEntry
    {
        public ZineEditorialEntryKind Kind { get; }

        public int? PublishedAxisMask { get; }

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
            bool? selfDeclaredPublicProfile = null,
            int? publishedAxisMask = null)
        {
            Kind = kind;
            SubjectNodeId = subjectNodeId;
            ProductId = productId;
            Opinion = opinion;
            TargetNodeId = targetNodeId;
            AddressId = addressId;
            SelfDeclaredPublicProfile = selfDeclaredPublicProfile;
            PublishedAxisMask = publishedAxisMask;
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

        /// <summary>
        /// Julkaistu demomainos.
        ///
        /// PublishedAxisMask on toimituksen julkaisema
        /// kuvaus, ei demon todellinen arvoprofiili.
        ///
        /// Vanha kaksiparametrinen kutsu säilyy
        /// yhteensopivana.
        /// </summary>
        internal static ZineEditorialEntry ForDemoAdvertisement(
            string nodeId,
            string demoId,
            SixAxisProfile publishedDescription = null)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException(
                    "Node ID is required.",
                    nameof(nodeId));
            }

            if (string.IsNullOrWhiteSpace(demoId))
            {
                throw new ArgumentException(
                    "Demo ID is required.",
                    nameof(demoId));
            }

            return new ZineEditorialEntry(
                ZineEditorialEntryKind.DemoAdvertisement,
                nodeId,
                productId: demoId,
                publishedAxisMask: publishedDescription?.Mask);
        }

        /// <summary>
        /// Julkaistu henkilön esteettinen kuvaus.
        ///
        /// Kuvaus on toimituksellinen väite,
        /// eikä sitä tarvitse johtaa solmun
        /// todellisesta henkilökohtaisesta profiilista.
        /// </summary>
        internal static ZineEditorialEntry
            ForContactSixAxisDescription(
                string nodeId,
                SixAxisProfile publishedDescription)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException(
                    "Node ID is required.",
                    nameof(nodeId));
            }

            if (publishedDescription == null)
            {
                throw new ArgumentNullException(
                    nameof(publishedDescription));
            }

            return new ZineEditorialEntry(
                ZineEditorialEntryKind.ContactSixAxisDescription,
                nodeId,
                publishedAxisMask: publishedDescription.Mask);
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

        public IReadOnlyList<ZineEditorialEntry> Entries
        {
            get;
        }

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
    /// Kerää tiedot julkaisuhetkeen saakka ja
    /// julkaisee ne määrätyin väliajoin.
    ///
    /// Oletusväli on 30 päivää.
    /// Toistuva zine-skenaario voi käyttää
    /// esimerkiksi 90 päivän julkaisuväliä.
    ///
    /// Ei lähetä numeroita, luo verkon kaaria
    /// eikä muuta lukijoiden tietämystä.
    /// </summary>
    public sealed class ZineEditorialCycle
    {
        public const int PublicationIntervalDays = 30;

        private readonly int _publicationIntervalDays;
        private readonly SimulationClock _clock;

        private readonly List<ZineEditorialEntry>
            _pending = new List<ZineEditorialEntry>();

        private readonly List<ZineEditorialIssue>
            _issues = new List<ZineEditorialIssue>();

        private readonly IReadOnlyList<ZineEditorialIssue>
            _issueView;

        private int _nextIssueNumber;
        private bool _stopped;

        public string ZineId { get; }
        public string EditorNodeId { get; }
        public bool Profile { get; }

        public event Action<ZineEditorialIssue> IssuePublished;

        public int PendingCount => _pending.Count;

        public IReadOnlyList<ZineEditorialIssue> Issues =>
            _issueView;

        public ZineEditorialCycle(
            string zineId,
            string editorNodeId,
            bool profile,
            SimulationClock clock,
            int publicationIntervalDays = PublicationIntervalDays,
            int firstIssueNumber = 1)
        {
            ValidateId(zineId, nameof(zineId));
            ValidateId(editorNodeId, nameof(editorNodeId));

            if (publicationIntervalDays < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(publicationIntervalDays));
            }

            if (firstIssueNumber < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstIssueNumber));
            }

            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            ZineId = zineId;
            EditorNodeId = editorNodeId;
            Profile = profile;

            _publicationIntervalDays =
                publicationIntervalDays;

            _nextIssueNumber = firstIssueNumber;

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

        /// <summary>
        /// Tallentaa toimituksen julkaistavaksi
        /// hyväksymän henkilön esteettisen kuvauksen.
        /// </summary>
        public void RecordContactDescription(
            string nodeId,
            SixAxisProfile publishedDescription)
        {
            _pending.Add(
                ZineEditorialEntry
                    .ForContactSixAxisDescription(
                        nodeId,
                        publishedDescription));
        }

        /// <summary>
        /// Tallentaa toimituksen julkaistavaksi
        /// hyväksymän demomainoksen ja sen kuvauksen.
        /// </summary>
        public void RecordDemoAdvertisement(
            string nodeId,
            string demoId,
            SixAxisProfile publishedDescription)
        {
            if (publishedDescription == null)
            {
                throw new ArgumentNullException(
                    nameof(publishedDescription));
            }

            _pending.Add(
                ZineEditorialEntry.ForDemoAdvertisement(
                    nodeId,
                    demoId,
                    publishedDescription));
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

        private void ScheduleNextPublication()
        {
            int issueNumber = _nextIssueNumber;

            _clock.ScheduleAfter(
                "ZINE-PUBLISH:" +
                ZineId + ":" + issueNumber,
                _publicationIntervalDays,
                Publish);
        }

        public void Stop()
        {
            _stopped = true;
        }

        private void Publish()
        {
            if (_stopped)
                return;

            var issue = new ZineEditorialIssue(
                ZineId,
                EditorNodeId,
                _nextIssueNumber,
                _clock.CurrentDay,
                Profile,
                _pending);

            _issues.Add(issue);

            // Julkaistu sisältö poistuu toimituksen
            // keskeneräisestä keräyspuskurista.
            _pending.Clear();

            _nextIssueNumber++;

            ScheduleNextPublication();

            // Ilmoitetaan valmiista numerosta
            // jakelujärjestelmälle.
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
    }
}
