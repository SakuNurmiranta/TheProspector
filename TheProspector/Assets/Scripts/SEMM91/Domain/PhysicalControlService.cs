
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum PhysicalControlDisposition
    {
        Transferred,
        Shelved
    }

    public sealed class PhysicalTransferRecord
    {
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }
        public CassetteCopy Copy { get; }
        public PostalShipment Shipment { get; }
        public int Day { get; }

        internal PhysicalTransferRecord(
            string senderNodeId,
            string recipientNodeId,
            CassetteCopy copy,
            PostalShipment shipment,
            int day)
        {
            SenderNodeId = senderNodeId;
            RecipientNodeId = recipientNodeId;
            Copy = copy;
            Shipment = shipment;
            Day = day;
        }
    }

    public sealed class PhysicalShelfRecord
    {
        public string NodeId { get; }
        public string CopyId { get; }
        public string ProductId { get; }
        public int Day { get; }

        internal PhysicalShelfRecord(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            NodeId = nodeId;
            CopyId = copy.Id;
            ProductId = copy.ProductId;
            Day = day;
        }
    }

    /// <summary>
    /// Toteuttaa pehmeän ja kovan kontrollin
    /// fyysiset seuraukset.
    ///
    /// Siirto käyttää olemassa olevaa
    /// CassetteCopy-oliota ja postijärjestelmää.
    ///
    /// Hyllytys tallennetaan erillisenä
    /// tilana; fyysinen kasetti jää haltijalle.
    /// </summary>
    public sealed class PhysicalControlService
        : IPhysicalControlActions
    {
        private readonly TimedPostalExchange _exchange;

        private readonly Func<
            string,
            IEnumerable<CopyDistributionRoute>>
            _routesForNode;

        private readonly Func<string, CulturalProduct>
            _productResolver;

        private readonly Func<string, string>
            _countryForNode;

        private readonly Func<
            string, string, string, bool>
            _isEligible;

        private readonly Action<
            PostalShipment, CassetteCopy>
            _onTransferred;

        private readonly int _replyAllowanceDays;

        // Sama fyysinen kasetti voi kulkea
        // monen solmun kautta. Siksi tila on
        // solmu- ja kasettikohtainen.
        private readonly Dictionary<
            (string NodeId, string CopyId),
            PhysicalControlDisposition> _dispositions =
                new Dictionary<
                    (string NodeId, string CopyId),
                    PhysicalControlDisposition>();

        private readonly List<PhysicalTransferRecord>
            _transfers =
                new List<PhysicalTransferRecord>();

        private readonly List<PhysicalShelfRecord>
            _shelves =
                new List<PhysicalShelfRecord>();

        private readonly IReadOnlyList<PhysicalTransferRecord>
            _transferView;

        private readonly IReadOnlyList<PhysicalShelfRecord>
            _shelfView;

        public IReadOnlyList<PhysicalTransferRecord>
            Transfers => _transferView;

        public IReadOnlyList<PhysicalShelfRecord>
            Shelves => _shelfView;

        public PhysicalControlService(
            TimedPostalExchange exchange,
            Func<string, IEnumerable<CopyDistributionRoute>>
                routesForNode,
            Func<string, CulturalProduct> productResolver,
            Func<string, string> countryForNode,
            Func<string, string, string, bool> isEligible,
            Action<PostalShipment, CassetteCopy>
                onTransferred = null,
            int replyAllowanceDays = 5)
        {
            if (replyAllowanceDays < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(replyAllowanceDays));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _routesForNode = routesForNode ??
                throw new ArgumentNullException(
                    nameof(routesForNode));

            _productResolver = productResolver ??
                throw new ArgumentNullException(
                    nameof(productResolver));

            _countryForNode = countryForNode ??
                throw new ArgumentNullException(
                    nameof(countryForNode));

            _isEligible = isEligible ??
                throw new ArgumentNullException(
                    nameof(isEligible));

            _onTransferred = onTransferred;
            _replyAllowanceDays = replyAllowanceDays;

            _transferView = _transfers.AsReadOnly();
            _shelfView = _shelves.AsReadOnly();
        }

        public PhysicalControlDisposition? GetDisposition(
            string nodeId,
            string copyId)
        {
            ValidateId(nodeId, nameof(nodeId));
            ValidateId(copyId, nameof(copyId));

            return _dispositions.TryGetValue(
                (nodeId, copyId),
                out var disposition)
                ? disposition
                : (PhysicalControlDisposition?)null;
        }

        public void TransferOne(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            EnsureAvailable(nodeId, copy, day);

            var product = _productResolver(copy.ProductId);

            if (product == null)
                throw new InvalidOperationException(
                    "Cultural product cannot be resolved.");

            var routes = _routesForNode(nodeId);

            if (routes == null)
                throw new InvalidOperationException(
                    "Route provider returned null.");

            CopyDistributionRoute selected = null;

            // Valitaan täsmälleen ensimmäinen
            // ehdot täyttävä reitti.
            foreach (var route in routes)
            {
                if (route == null)
                    continue;

                string recipientId =
                    route.RecipientNodeId;

                if (string.Equals(
                    nodeId,
                    recipientId,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (!_isEligible(
                    nodeId,
                    recipientId,
                    copy.ProductId))
                {
                    continue;
                }

                selected = route;
                break;
            }

            if (selected == null)
                throw new InvalidOperationException(
                    "No eligible transfer recipient.");

            // Tärkeää: EI kutsua RecordCopy().
            // Tämä on täsmälleen sama kasetti.
            //
            // Pakettiin liitetään myös kielteinen
            // suositus, jonka vastaanottaja oppii
            // vasta avatessaan viestin.
            string suffix = nodeId + ":" + copy.Id;

            var package = new MessagePackage(
                "SOFT-MSG:" + suffix,
                selected.Connection,
                cassetteCopy: copy,
                feedbackProduct: product,
                thumb: -1);

            var shipment = _exchange.Send(
                "SOFT-SHIP:" + suffix,
                package,
                _countryForNode(nodeId),
                _countryForNode(selected.RecipientNodeId),
                _replyAllowanceDays);

            var key = (nodeId, copy.Id);

            _dispositions.Add(
                key,
                PhysicalControlDisposition.Transferred);

            _transfers.Add(
                new PhysicalTransferRecord(
                    nodeId,
                    selected.RecipientNodeId,
                    copy,
                    shipment,
                    day));

            // Ilmoitetaan olemassa olevan kasetin
            // uusi lähetys maailmalle, jotta
            // vastaanottaja löytää saman olion.
            _onTransferred?.Invoke(
                shipment, copy);
        }

        public void Shelve(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            EnsureAvailable(nodeId, copy, day);

            // Ei postilähetystä.
            // Ei uutta kopiota.
            // Kasetin haltija säilyy ennallaan.
            _dispositions.Add(
                (nodeId, copy.Id),
                PhysicalControlDisposition.Shelved);

            _shelves.Add(
                new PhysicalShelfRecord(
                    nodeId,
                    copy,
                    day));
        }

        private void EnsureAvailable(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            ValidateId(nodeId, nameof(nodeId));

            if (copy == null)
                throw new ArgumentNullException(
                    nameof(copy));

            if (day < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(day));

            if (GetDisposition(nodeId, copy.Id).HasValue)
                throw new InvalidOperationException(
                    "This node has already disposed " +
                    "of this cassette.");

            if (!string.Equals(
                    copy.HolderNodeId,
                    nodeId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Node is not the cassette holder.");
            }
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
}
