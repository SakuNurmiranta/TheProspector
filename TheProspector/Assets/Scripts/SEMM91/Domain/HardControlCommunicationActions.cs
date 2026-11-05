
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class HardControlNotice
    {
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }
        public string ProductId { get; }
        public int Day { get; }
        public PostalShipment Shipment { get; }

        internal HardControlNotice(
            string senderNodeId,
            string recipientNodeId,
            string productId,
            int day,
            PostalShipment shipment)
        {
            SenderNodeId = senderNodeId;
            RecipientNodeId = recipientNodeId;
            ProductId = productId;
            Day = day;
            Shipment = shipment;
        }
    }

    /// <summary>
    /// Täydentää fyysistä kontrollia viestinnällä.
    ///
    /// Pehmeä kontrolli:
    ///   delegoi kasetin siirron ennallaan.
    ///
    /// Kova kontrolli:
    ///   hyllyttää kasetin ja lähettää
    ///   kielteisen palautteen jokaiselle
    ///   erilliselle lähtevälle kontaktille.
    ///
    /// Viestit eivät sisällä fyysistä kasettia.
    /// </summary>
    public sealed class HardControlCommunicationActions
        : IPhysicalControlActions
    {
        private readonly PhysicalControlService _physical;
        private readonly TimedPostalExchange _exchange;

        private readonly Func<
            string, IEnumerable<CopyDistributionRoute>>
            _routesForNode;

        private readonly Func<string, CulturalProduct>
            _resolveProduct;

        private readonly Func<string, string>
            _countryForNode;

        private readonly int _replyAllowanceDays;

        private readonly List<HardControlNotice> _notices =
            new List<HardControlNotice>();

        private readonly IReadOnlyList<HardControlNotice>
            _noticeView;

        public IReadOnlyList<HardControlNotice>
            Notices => _noticeView;

        public HardControlCommunicationActions(
            PhysicalControlService physical,
            TimedPostalExchange exchange,
            Func<string, IEnumerable<CopyDistributionRoute>>
                routesForNode,
            Func<string, CulturalProduct> resolveProduct,
            Func<string, string> countryForNode,
            int replyAllowanceDays = 5)
        {
            if (replyAllowanceDays < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(replyAllowanceDays));

            _physical = physical ??
                throw new ArgumentNullException(
                    nameof(physical));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _routesForNode = routesForNode ??
                throw new ArgumentNullException(
                    nameof(routesForNode));

            _resolveProduct = resolveProduct ??
                throw new ArgumentNullException(
                    nameof(resolveProduct));

            _countryForNode = countryForNode ??
                throw new ArgumentNullException(
                    nameof(countryForNode));

            _replyAllowanceDays = replyAllowanceDays;
            _noticeView = _notices.AsReadOnly();
        }

        public void TransferOne(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            // Siirron mukana kulkeva -1-palaute
            // kuuluu jo PhysicalControlServiceen.
            // Ei lähetetä ylimääräisiä tiedotteita.
            _physical.TransferOne(nodeId, copy, day);
        }

        public void Shelve(
            string nodeId,
            CassetteCopy copy,
            int day)
        {
            if (copy == null)
                throw new ArgumentNullException(
                    nameof(copy));

            var product = _resolveProduct(copy.ProductId);

            if (product == null)
                throw new InvalidOperationException(
                    "Unknown cultural product: " +
                    copy.ProductId);

            var routes = _routesForNode(nodeId);

            if (routes == null)
                throw new InvalidOperationException(
                    "Route provider returned null.");

            var recipients =
                new List<CopyDistributionRoute>();

            var visited =
                new HashSet<string>(
                    StringComparer.Ordinal);

            // Rinnakkaiset kaaret sallitaan
            // verkostossa, mutta sama vastaanottaja
            // saa tästä päätöksestä vain yhden
            // kielteisen tiedotteen.
            foreach (var route in routes)
            {
                if (route == null)
                    continue;

                string recipientId =
                    route.RecipientNodeId;

                if (string.IsNullOrWhiteSpace(recipientId))
                    continue;

                if (recipientId == nodeId)
                    continue;

                if (!visited.Add(recipientId))
                    continue;

                recipients.Add(route);
            }

            // Fyysinen hyllytys ensin.
            // Jos se on estetty, mitään viestejä
            // ei lähetetä.
            _physical.Shelve(nodeId, copy, day);

            foreach (var route in recipients)
            {
                string recipientId =
                    route.RecipientNodeId;

                string suffix =
                    nodeId + ":" +
                    copy.Id + ":" +
                    recipientId;

                // Huomaa: EI cassetteCopy-parametria.
                // Tiedote sisältää vain palautteen.
                var package = new MessagePackage(
                    "HARD-MSG:" + suffix,
                    route.Connection,
                    feedbackProduct: product,
                    thumb: -1);

                var shipment = _exchange.Send(
                    "HARD-SHIP:" + suffix,
                    package,
                    _countryForNode(nodeId),
                    _countryForNode(recipientId),
                    _replyAllowanceDays);

                _notices.Add(
                    new HardControlNotice(
                        nodeId,
                        recipientId,
                        copy.ProductId,
                        day,
                        shipment));
            }
        }
    }
}
