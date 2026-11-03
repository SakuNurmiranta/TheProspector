
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class CassetteDispatchService
    {
        private readonly Network _network;
        private readonly CassetteInventory _inventory;
        private readonly KnownCollectionRegistry _known;

        private readonly Dictionary<string, PostalShipment>
            _outgoingShipments =
                new Dictionary<string, PostalShipment>();

        public CassetteDispatchService(
            Network network,
            CassetteInventory inventory,
            KnownCollectionRegistry known)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));

            _inventory = inventory ??
                throw new ArgumentNullException(
                    nameof(inventory));

            _known = known ??
                throw new ArgumentNullException(
                    nameof(known));
        }

        public bool CanOfferDemo(
            string senderNodeId,
            string targetNodeId,
            string productId)
        {
            if (senderNodeId == targetNodeId)
                return false;

            // Vain lähettäjän oma tietämys vaikuttaa
            // päätökseen. Tuntematon kokoelma ei
            // tarkoita todistettua vastaanottoa.
            bool? knownOwnership = _known.IsKnownToHold(
                senderNodeId,
                targetNodeId,
                productId);

            if (knownOwnership == true)
                return false;

            // Lähettäjä muistaa omat lähteneet
            // lähetyksensä, vaikka vastaanottaja
            // ei olisi ehtinyt vastata niihin.
            foreach (var shipment in
                     _outgoingShipments.Values)
            {
                var package = shipment.Package;

                if (package.SourceNodeId != senderNodeId ||
                    package.TargetNodeId != targetNodeId ||
                    package.PayloadProductId != productId)
                {
                    continue;
                }

                // Palautunut lähetys voidaan uusia.
                // Matkalla, palautumassa tai perillä
                // oleva lähetys estää uuden tarjouksen.
                if (shipment.Status !=
                    PostalShipmentStatus.Returned)
                {
                    return false;
                }
            }

            return true;
        }

        public PostalShipment Dispatch(
            string shipmentId,
            MessagePackage package)
        {
            if (string.IsNullOrWhiteSpace(shipmentId))
                throw new ArgumentException(
                    "Shipment ID cannot be empty.",
                    nameof(shipmentId));

            if (package == null)
                throw new ArgumentNullException(
                    nameof(package));

            if (package.CassetteCopyId == null)
                throw new ArgumentException(
                    "Cassette dispatch requires a cassette.",
                    nameof(package));

            if (_outgoingShipments.ContainsKey(shipmentId))
                throw new ArgumentException(
                    "Shipment ID already registered.",
                    nameof(shipmentId));

            if (!CanOfferDemo(
                    package.SourceNodeId,
                    package.TargetNodeId,
                    package.PayloadProductId))
            {
                throw new InvalidOperationException(
                    "Recipient is already known to have " +
                    "received this product, or another " +
                    "shipment is still active.");
            }

            // Varsinainen postijärjestelmä hoitaa
            // osoitteen, kuljetuksen ja haltijuuden.
            var shipment = PostalShipment.Dispatch(
                shipmentId,
                _network,
                _inventory,
                package);

            // Tallennetaan vasta onnistuneen lähetyksen
            // jälkeen. Lähetysobjektin tilan muutokset
            // näkyvät palvelulle myöhemmin.
            _outgoingShipments.Add(
                shipmentId, shipment);

            return shipment;
        }
    }
}
