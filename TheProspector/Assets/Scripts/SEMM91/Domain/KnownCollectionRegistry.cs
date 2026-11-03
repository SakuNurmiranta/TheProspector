using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SEMM91.Domain
{
    public sealed class KnownCollectionRegistry
    {
        private readonly Network _network;

        // Avain:
        // (tietoa ylläpitävä solmu, kokoelman omistava solmu)
        //
        // Arvo:
        // viimeksi vastaanotettu kokoelmatietous.
        private readonly Dictionary<
                (string, string), HashSet<string>>
            _knownCollections = new();

        public KnownCollectionRegistry(Network network)
        {
            _network = network ??
                       throw new ArgumentNullException(
                           nameof(network));
        }

        public void RecordSnapshot(
            string observerNodeId,
            string subjectNodeId,
            IEnumerable<string> productIds)
        {
            ValidateNode(observerNodeId);
            ValidateNode(subjectNodeId);

            if (productIds == null)
                throw new ArgumentNullException(
                    nameof(productIds));

            // Rakennetaan uusi tilannekuva ensin.
            // Virheellinen tieto ei saa muuttaa
            // aiemmin tallennettua kokoelmaa.
            var snapshot = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var productId in productIds)
            {
                ValidateProductId(productId);

                snapshot.Add(productId);
            }


// Kokoelmalistat kuvaavat pysyvää
// vastaanottohistoriaa, eivät nykyistä
// fyysistä kasettikokoelmaa.
//
// Uusi ilmoitus täydentää aikaisempaa tietoa.
// Vanhentunut ilmoitus ei voi poistaa jo
// opittua vastaanottohistoriaa.
            var key = (observerNodeId, subjectNodeId);

            if (_knownCollections.TryGetValue(
                    key, out var previous))
                snapshot.UnionWith(previous);

            _knownCollections[key] = snapshot;
        }

        public bool TryGetKnownCollection(
            string observerNodeId,
            string subjectNodeId,
            out IReadOnlyCollection<string> products)
        {
            ValidateNode(observerNodeId);
            ValidateNode(subjectNodeId);

            if (!_knownCollections.TryGetValue(
                    (observerNodeId, subjectNodeId),
                    out var snapshot))
            {
                products = null;
                return false;
            }

            // Palautetaan itsenäinen vain luettava kopio.
            var copy = new List<string>(snapshot);

            // Järjestys ei vaikuta tietosisältöön.
            // Deterministinen järjestys helpottaa
            // lokitusta ja myöhempiä testejä.
            copy.Sort(StringComparer.Ordinal);

            products = new ReadOnlyCollection<string>(
                copy);

            return true;
        }

        public bool? IsKnownToHold(
            string observerNodeId,
            string subjectNodeId,
            string productId)
        {
            ValidateNode(observerNodeId);
            ValidateNode(subjectNodeId);
            ValidateProductId(productId);

            if (!_knownCollections.TryGetValue(
                    (observerNodeId, subjectNodeId),
                    out var snapshot))
                // Kokoelmasta ei ole tietoa.
                return null;

            // true: demo ilmoitettiin kokoelmassa.
            // false: ilmoitetussa kokoelmassa
            // ei ollut kyseistä demoa.
            return snapshot.Contains(productId);
        }

        private void ValidateNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.",
                    nameof(nodeId));
        }

        private static void ValidateProductId(
            string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID cannot be empty.",
                    nameof(productId));
        }
    }
}