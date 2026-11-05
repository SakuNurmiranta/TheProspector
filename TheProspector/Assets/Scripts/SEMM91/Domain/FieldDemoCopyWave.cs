
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Yhdistää todelliset kasettikopioiden
    /// jakelutapahtumat kentän SIR-elinkaareen.
    ///
    /// Vastaanottaminen ei aloita kopiointia.
    /// Kentän sulkeminen on eksplisiittinen
    /// ja peruuttamaton päätös.
    /// </summary>
    public sealed class FieldDemoCopyWave
    {
        private readonly Field _field;
        private readonly Network _network;
        private readonly CulturalProduct _product;
        private readonly SIRStateRegistry _sir;

        private readonly HashSet<string> _observedShipments =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<string> _activeCopiers =
            new HashSet<string>(StringComparer.Ordinal);

        private bool _started;
        private bool _closed;

        public int ActiveCopierCount => _activeCopiers.Count;

        public bool IsPassivelyDiscoverable =>
            _closed &&
            _sir.GetFieldState(_field, _product) == SIRState.R;
        
        
        private readonly List<PostalShipment>
            _observedCopyShipments =
                new List<PostalShipment>();

        private readonly HashSet<string>
            _passiveSampledNodes =
                new HashSet<string>(StringComparer.Ordinal);

        private readonly HashSet<string>
            _passivelyExposedNodes =
                new HashSet<string>(StringComparer.Ordinal);


        public double ObservedDeliveredReach
        {
            get
            {
                if (_field.LocalNodeCount == 0)
                    return 0.0;

                var recipients = new HashSet<string>(
                    StringComparer.Ordinal);

                foreach (var shipment in _observedCopyShipments)
                {
                    if (shipment.Status !=
                        PostalShipmentStatus.Delivered)
                        continue;

                    string recipient =
                        shipment.Package.TargetNodeId;

                    if (_field.ContainsLocalNode(recipient))
                        recipients.Add(recipient);
                }

                return (double)recipients.Count /
                       _field.LocalNodeCount;
            }
        }

        public FieldDemoCopyWave(
            Field field,
            Network network,
            CulturalProduct product,
            SIRStateRegistry sir)
        {
            _field = field ??
                throw new ArgumentNullException(nameof(field));

            _network = network ??
                throw new ArgumentNullException(nameof(network));

            _product = product ??
                throw new ArgumentNullException(nameof(product));

            _sir = sir ??
                throw new ArgumentNullException(nameof(sir));
        }

        /// <summary>
        /// Rekisteröi PositiveCopyDistributionService-
        /// palvelun tuottaman oikean kopiointilähetyksen.
        ///
        /// Palauttaa true vain uudesta, tähän
        /// kenttään ja demoon kuuluvasta tapahtumasta.
        /// </summary>
        public bool ObserveCopyDispatch(
            CopyDistributionShipment distribution)
        {
            if (distribution == null)
                throw new ArgumentNullException(
                    nameof(distribution));

            var shipment = distribution.Shipment;
            var package = shipment.Package;
            var copy = distribution.Copy;

            string senderId = package.SourceNodeId;

            if (_observedShipments.Contains(shipment.Id))
                return false;

            // Vain oikean kulttuurituotteen
            // fyysinen kopiointilähetys.
            if (copy.ProductId != _product.Id ||
                package.PayloadProductId != _product.Id)
            {
                return false;
            }

            // Lähetyksen ja kopion pitää vastata
            // samaa tosiasiallista tapahtumaa.
            if (package.CassetteCopyId != copy.Id ||
                package.TargetNodeId !=
                    distribution.RecipientNodeId)
            {
                throw new InvalidOperationException(
                    "Copy distribution and shipment do not match.");
            }

            if (!_field.ContainsLocalNode(senderId))
                return false;

            if (!_network.TryGetNode(senderId, out var node))
            {
                throw new InvalidOperationException(
                    "Unknown copying node.");
            }

            if (_closed)
            {
                throw new InvalidOperationException(
                    "Closed field wave cannot restart.");
            }

            var state = _sir.GetNodeState(node, _product);

            if (state == SIRState.R)
            {
                throw new InvalidOperationException(
                    "Completed copier cannot restart copying.");
            }

            if (!_started)
            {
                _sir.BeginFieldWave(_field, _product);
                _started = true;
            }

            // Useita saman kopioijan lähetyksiä voi
            // tapahtua yhden I-vaiheen aikana.
            if (_activeCopiers.Add(senderId))
            {
                _sir.BeginNodeCopying(node, _product);
            }

          
            _observedShipments.Add(shipment.Id);

// Säilytetään viite todelliseen lähetykseen.
// Sen toimitustila voi muuttua myöhemmin
// simulaatiokellon edetessä.
            _observedCopyShipments.Add(shipment);

            return true;

        }

        
        public bool HasPassiveExposure(string nodeId)
        {
            ValidateId(nodeId, nameof(nodeId));

            return _passivelyExposedNodes.Contains(nodeId);
        }

        /// <summary>
        /// Yksi passiivinen löytöyritys sulkeutuneen
        /// kenttäaallon jälkeen.
        ///
        /// Onnistuminen tarkoittaa tietoisuutta
        /// kulttuurituotteesta, ei kasetin omistamista.
        /// </summary>
        public bool TrySamplePassiveExposure(
            string readerNodeId,
            IReadOnlyDictionary<string, int> capitalByNode,
            Random random)
        {
            ValidateId(readerNodeId, nameof(readerNodeId));

            if (capitalByNode == null)
                throw new ArgumentNullException(
                    nameof(capitalByNode));

            if (random == null)
                throw new ArgumentNullException(nameof(random));

            if (!_field.ContainsLocalNode(readerNodeId))
                return false;

            // Vain sulkeutunut SIR-aalto sallii
            // passiivisen löydettävyyden.
            if (!IsPassivelyDiscoverable)
                return false;

            // Sama lukija ei voi saada samaa
            // löytöyritystä uudelleen.
            if (_passiveSampledNodes.Contains(readerNodeId))
                return false;

            double reach = ObservedDeliveredReach;

            if (reach <= 0.0)
                return false;

            var shares = _field.CalculateShares(capitalByNode);

            double probability = reach * shares[readerNodeId];

            if (probability <= 0.0)
                return false;

            double sample = random.NextDouble();

            if (double.IsNaN(sample) ||
                double.IsInfinity(sample) ||
                sample < 0.0 ||
                sample >= 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(random),
                    "Random sample must be in [0,1).");
            }

            _passiveSampledNodes.Add(readerNodeId);

            if (sample >= probability)
                return false;

            _passivelyExposedNodes.Add(readerNodeId);

            return true;
        }

        
        /// <summary>
        /// Solmun kopiointivaihe päättyy.
        ///
        /// Tämä ei automaattisesti sulje
        /// koko kentän leviämisaaltoa.
        /// </summary>
        public bool FinishCopying(string nodeId)
        {
            ValidateId(nodeId, nameof(nodeId));

            if (!_activeCopiers.Contains(nodeId))
                return false;

            if (!_network.TryGetNode(nodeId, out var node))
                throw new InvalidOperationException(
                    "Unknown copying node.");

            _sir.EndNodeCopying(node, _product);

            _activeCopiers.Remove(nodeId);

            return true;
        }

        /// <summary>
        /// Sulkee kentän leviämisaallon vasta,
        /// kun yhtään aktiivista kopioijaa
        /// ei ole jäljellä.
        ///
        /// Päätös sulkea aalto tulee ylemmästä
        /// simulaatiopolitiikasta.
        /// </summary>
        public bool TryCloseWave()
        {
            if (!_started || _closed)
                return false;

            if (_activeCopiers.Count > 0)
                return false;

            _sir.CloseFieldWave(_field, _product);

            _closed = true;

            return true;
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
