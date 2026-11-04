
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    // Yksi tavoitettavissa oleva kontakti ja
    // sille käytettävä henkilökohtainen yhteys.
    public sealed class CopyDistributionRoute
    {
        public string RecipientNodeId { get; }
        public NetworkConnection Connection { get; }
        public string RecipientCountryCode { get; }

        public CopyDistributionRoute(
            string recipientNodeId,
            NetworkConnection connection,
            string recipientCountryCode)
        {
            if (string.IsNullOrWhiteSpace(recipientNodeId))
                throw new ArgumentException(
                    "Recipient is required.",
                    nameof(recipientNodeId));

            if (connection == null)
                throw new ArgumentNullException(
                    nameof(connection));

            if (string.IsNullOrWhiteSpace(recipientCountryCode))
                throw new ArgumentException(
                    "Country is required.",
                    nameof(recipientCountryCode));

            RecipientNodeId = recipientNodeId;
            Connection = connection;
            RecipientCountryCode = recipientCountryCode;
        }
    }

    // Yhden valitun vastaanottajan lähetyksen tulos.
    public sealed class CopyDistributionShipment
    {
        public string RecipientNodeId { get; }
        public CassetteCopy Copy { get; }
        public PostalShipment Shipment { get; }

        internal CopyDistributionShipment(
            string recipientNodeId,
            CassetteCopy copy,
            PostalShipment shipment)
        {
            RecipientNodeId = recipientNodeId;
            Copy = copy;
            Shipment = shipment;
        }
    }

    /// <summary>
    /// Toteuttaa myönteisen kopiointipäätöksen
    /// kaikille sillä hetkellä kelvollisille
    /// vastaanottajille.
    ///
    /// Ei tee esteettistä päätöstä eikä tarkista
    /// vastaanottajien todellisia kokoelmia.
    /// </summary>
    public sealed class PositiveCopyDistributionService
    {
        private readonly CassetteInventory _inventory;
        private readonly KnownCollectionRegistry _known;
        private readonly TimedPostalExchange _exchange;

        private readonly CopyRecipientSelector _selector =
            new CopyRecipientSelector();

        private readonly HashSet<string> _usedBatchIds =
            new HashSet<string>(StringComparer.Ordinal);

        // Lähettäjä + vastaanottaja + kulttuurituote
        // -> viimeisin tämän palvelun lähetys.
        //
        // Palautunut lähetys voidaan yrittää uudelleen.
        private readonly Dictionary<
            (string Sender, string Recipient, string Product),
            PostalShipment> _outbound =
                new Dictionary<
                    (string Sender, string Recipient, string Product),
                    PostalShipment>();

        public PositiveCopyDistributionService(
            CassetteInventory inventory,
            KnownCollectionRegistry known,
            TimedPostalExchange exchange)
        {
            _inventory = inventory ??
                throw new ArgumentNullException(
                    nameof(inventory));

            _known = known ??
                throw new ArgumentNullException(
                    nameof(known));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));
        }

        public IReadOnlyList<CopyDistributionShipment> DispatchAll(
            string batchId,
            string senderNodeId,
            CassetteCopy sourceCopy,
            IEnumerable<CopyDistributionRoute> routes,
            string senderCountryCode,
            int replyAllowanceDays,
            Func<string, bool> hasCommunicatedDislike,
            Func<string, bool> canOffer)
        {
            ValidateId(batchId, nameof(batchId));
            ValidateId(senderNodeId, nameof(senderNodeId));

            if (sourceCopy == null)
                throw new ArgumentNullException(
                    nameof(sourceCopy));

            if (routes == null)
                throw new ArgumentNullException(
                    nameof(routes));

            if (hasCommunicatedDislike == null)
                throw new ArgumentNullException(
                    nameof(hasCommunicatedDislike));

            if (canOffer == null)
                throw new ArgumentNullException(
                    nameof(canOffer));

            if (string.IsNullOrWhiteSpace(senderCountryCode))
                throw new ArgumentException(
                    "Sender country is required.",
                    nameof(senderCountryCode));

            if (_usedBatchIds.Contains(batchId))
                throw new ArgumentException(
                    "Batch ID already used.",
                    nameof(batchId));

            if (sourceCopy.HolderNodeId != senderNodeId)
                throw new InvalidOperationException(
                    "Sender must hold the source cassette.");

            // Tarkistetaan lisäajan sallittu vaihteluväli.
            _ = PostalTimingRules.CalculateDeadline(
                0, 2, replyAllowanceDays);

            // Materiaalisoidaan reitit ennen
            // ensimmäisen kasetin kopioimista.
            var routeByRecipient =
                new Dictionary<string, CopyDistributionRoute>(
                    StringComparer.Ordinal);

            var contactIds = new List<string>();

            foreach (var route in routes)
            {
                if (route == null)
                    throw new ArgumentException(
                        "Route cannot be null.",
                        nameof(routes));

                // Tarkistetaan maatiedot.
                _ = PostalTimingRules.GetTransitDays(
                    senderCountryCode,
                    route.RecipientCountryCode);

                contactIds.Add(route.RecipientNodeId);

                // Rinnakkaisista yhteyksistä käytetään
                // tässä kierroksessa ensimmäistä.
                if (!routeByRecipient.ContainsKey(
                        route.RecipientNodeId))
                {
                    routeByRecipient.Add(
                        route.RecipientNodeId,
                        route);
                }
            }

            string productId = sourceCopy.ProductId;

            var selected = _selector.SelectAll(
                senderNodeId,
                productId,
                contactIds,
                _known,
                hasCommunicatedDislike,
                recipient =>
                {
                    var key = (
                        senderNodeId,
                        recipient,
                        productId);

                    if (_outbound.TryGetValue(
                            key, out var previous))
                    {
                        // Lähetys matkalla, palautumassa
                        // tai jo toimitettu.
                        if (previous.Status !=
                            PostalShipmentStatus.Returned)
                        {
                            return false;
                        }
                    }

                    return canOffer(recipient);
                });

            var results =
                new List<CopyDistributionShipment>();

            foreach (string recipient in selected)
            {
                var route = routeByRecipient[recipient];

                string suffix = batchId + ":" + recipient;

                string copyId =
                    "FANOUT-COPY:" + suffix;

                string messageId =
                    "FANOUT-MSG:" + suffix;

                string shipmentId =
                    "FANOUT-SHIP:" + suffix;

                // Jokainen vastaanottaja saa
                // erillisen fyysisen kasettikopion.
                var copy = _inventory.RecordCopy(
                    sourceCopy.Id,
                    copyId,
                    senderNodeId,
                    senderNodeId);

                var package = new MessagePackage(
                    messageId,
                    route.Connection,
                    cassetteCopy: copy);

                // Varmistetaan, ettei reitin
                // ilmoitettu vastaanottaja eroa
                // varsinaisesta viestin kohteesta.
                if (package.SourceNodeId != senderNodeId ||
                    package.TargetNodeId != recipient)
                {
                    throw new InvalidOperationException(
                        "Route does not match message endpoints.");
                }

                // Postitus siirtää kopion kuljetukseen
                // ja ajastaa sen saapumisen.
                var shipment = _exchange.Send(
                    shipmentId,
                    package,
                    senderCountryCode,
                    route.RecipientCountryCode,
                    replyAllowanceDays);

                _outbound[
                    (senderNodeId, recipient, productId)] =
                    shipment;

                results.Add(
                    new CopyDistributionShipment(
                        recipient,
                        copy,
                        shipment));
            }

            _usedBatchIds.Add(batchId);

            return results.AsReadOnly();
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }
}
