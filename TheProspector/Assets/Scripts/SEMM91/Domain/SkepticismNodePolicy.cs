
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class RecordedReferral
    {
        public string ShipmentId { get; }
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }
        public string ProductId { get; }
        public int? Thumb { get; }

        internal RecordedReferral(
            string shipmentId,
            string senderNodeId,
            string recipientNodeId,
            string productId,
            int? thumb)
        {
            ShipmentId = shipmentId;
            SenderNodeId = senderNodeId;
            RecipientNodeId = recipientNodeId;
            ProductId = productId;
            Thumb = thumb;
        }
    }

    /// <summary>
    /// Lähetystunnistekohtainen tieto siitä,
    /// kuka lähetti kasetin, kenelle ja
    /// millaisen suosituksen kanssa.
    ///
    /// Rekisteröinti tapahtuu lähetyshetkellä.
    /// Vastaanottaja ei saa tietoa ennen kuin
    /// sen oma viesti avataan.
    /// </summary>
    public sealed class SkepticismReferralRegistry
    {
        private readonly Dictionary<
            string, RecordedReferral> _records =
                new Dictionary<
                    string, RecordedReferral>(
                    StringComparer.Ordinal);

        public int Count => _records.Count;

        public void Register(
            string shipmentId,
            string senderNodeId,
            string recipientNodeId,
            string productId,
            int? thumb)
        {
            ValidateId(shipmentId, nameof(shipmentId));
            ValidateId(senderNodeId, nameof(senderNodeId));
            ValidateId(recipientNodeId, nameof(recipientNodeId));
            ValidateId(productId, nameof(productId));

            if (senderNodeId == recipientNodeId)
                throw new ArgumentException(
                    "Sender and recipient must differ.");

            if (thumb.HasValue &&
                (thumb.Value < -1 || thumb.Value > 1))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(thumb));
            }

            if (_records.ContainsKey(shipmentId))
                throw new ArgumentException(
                    "Referral shipment already registered.",
                    nameof(shipmentId));

            _records.Add(
                shipmentId,
                new RecordedReferral(
                    shipmentId,
                    senderNodeId,
                    recipientNodeId,
                    productId,
                    thumb));
        }

        public bool TryGet(
            string shipmentId,
            out RecordedReferral referral)
        {
            return _records.TryGetValue(
                shipmentId,
                out referral);
        }

        private static void ValidateId(
            string id,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }

    /// <summary>
    /// Yhdistää avatun fyysisen lähetyksen
    /// myöhemmin valmistuvaan kuuntelureaktioon.
    ///
    /// Ei muuta kuuntelua, postia,
    /// kopiointia tai kontrollitoimia.
    /// </summary>
    public sealed class SkepticismNodePolicy
        : ISimulationNodePolicy
    {
        private readonly string _nodeId;

        private readonly ISimulationNodePolicy _inner;

        private readonly ListeningReactionNodePolicy
            _reactionPolicy;

        private readonly Func<BacklogItem, CassetteCopy>
            _resolveCopy;

        private readonly SkepticismReferralRegistry
            _referrals;

        private readonly DirectionalSkepticismLedger
            _ledger;

        // Ensimmäinen avattu fyysinen kasetti
        // aloittaa tietyn demon kuuntelun.
        // Myöhemmät kaksoiskappaleet eivät vaihda
        // tämän kuuntelun suosittelijaa.
        private readonly HashSet<string>
            _firstOpenedProducts =
                new HashSet<string>(
                    StringComparer.Ordinal);

        private readonly Dictionary<
            string, RecordedReferral>
            _firstPositiveReferrals =
                new Dictionary<
                    string, RecordedReferral>(
                    StringComparer.Ordinal);

        private int _processedDecisionCount;

        public SkepticismNodePolicy(
            string nodeId,
            ISimulationNodePolicy inner,
            ListeningReactionNodePolicy reactionPolicy,
            Func<BacklogItem, CassetteCopy> resolveCopy,
            SkepticismReferralRegistry referrals,
            DirectionalSkepticismLedger ledger)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            _nodeId = nodeId;

            _inner = inner ??
                throw new ArgumentNullException(nameof(inner));

            _reactionPolicy = reactionPolicy ??
                throw new ArgumentNullException(
                    nameof(reactionPolicy));

            _resolveCopy = resolveCopy ??
                throw new ArgumentNullException(
                    nameof(resolveCopy));

            _referrals = referrals ??
                throw new ArgumentNullException(
                    nameof(referrals));

            _ledger = ledger ??
                throw new ArgumentNullException(
                    nameof(ledger));
        }

        public void OnMessageOpened(
            string nodeId,
            BacklogItem message,
            int capitalAward,
            int day)
        {
            ValidateNode(nodeId);

            if (message == null)
                throw new ArgumentNullException(
                    nameof(message));

            CassetteCopy copy = _resolveCopy(message);

            // Annetaan nykyisen kuuntelukerroksen
            // käsitellä viesti ensin.
            _inner.OnMessageOpened(
                nodeId,
                message,
                capitalAward,
                day);

            if (copy == null)
                return;

            // Vain ensimmäinen avattu fyysinen
            // kappale liittyy tähän kuunteluun.
            if (!_firstOpenedProducts.Add(copy.ProductId))
                return;

            if (!_referrals.TryGet(
                    message.ShipmentId,
                    out var evidence))
            {
                return;
            }

            // Lähetyshistoria on todiste,
            // mutta hyväksymme sen vain, jos
            // tiedot vastaavat todella
            // avattua pakettia.
            if (evidence.RecipientNodeId != _nodeId ||
                evidence.ProductId != copy.ProductId ||
                evidence.SenderNodeId == _nodeId)
            {
                return;
            }

            // Pelkkä kasetin toimittaminen ei
            // riitä myönteiseksi suositukseksi.
            //
            // Lisäksi tarkistamme suoraan
            // avatun viestin peukkuarvon.
            if (evidence.Thumb != +1 ||
                message.Package.Thumb != +1)
            {
                return;
            }

            _firstPositiveReferrals.Add(
                copy.ProductId,
                evidence);
        }

        public void OnDay(string nodeId, int day)
        {
            ValidateNode(nodeId);

            // Tässä voi valmistua kuuntelu ja
            // syntyä uusi ListeningReactionDecision.
            _inner.OnDay(nodeId, day);

            var decisions = _reactionPolicy.Decisions;

            // Tarkastetaan vain uudet päätökset.
            // Maailman päivittäinen kierto ei saa
            // kirjata samaa seurausta uudelleen.
            while (_processedDecisionCount < decisions.Count)
            {
                var decision =
                    decisions[_processedDecisionCount];

                _processedDecisionCount++;

                if (!_firstPositiveReferrals.TryGetValue(
                        decision.ProductId,
                        out var referral))
                {
                    continue;
                }

                _ledger.AssessReferral(
                    _nodeId,
                    referral.SenderNodeId,
                    decision.ProductId,
                    +1,
                    decision.Reaction);
            }
        }

        private void ValidateNode(string nodeId)
        {
            if (!string.Equals(
                    nodeId,
                    _nodeId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Skepticism policy invoked for wrong node.",
                    nameof(nodeId));
            }
        }
    }
}
