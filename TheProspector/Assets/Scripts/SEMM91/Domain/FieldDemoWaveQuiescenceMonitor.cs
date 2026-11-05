
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Sulkee käynnistyneen kenttäaallon,
    /// kun havaittu fyysinen levitysketju
    /// on kokonaan käsitelty.
    ///
    /// Seuraa myös kopiointipalvelun
    /// ulkopuolisia kasettisiirtoja.
    /// </summary>
    public sealed class FieldDemoWaveQuiescenceMonitor
        : IDisposable
    {
        private readonly string _productId;
        private readonly FieldDemoCopyWave _wave;
        private readonly SimulationWorld _world;
        private readonly TimedPostalExchange _exchange;

        private readonly List<string> _nodeIds;
        private readonly Func<string, ListeningProgressSnapshot>
            _listeningStatus;

        private readonly Dictionary<string, PostalShipment>
            _physicalShipments =
                new Dictionary<string, PostalShipment>(
                    StringComparer.Ordinal);

        private bool _disposed;

        public int TrackedShipmentCount =>
            _physicalShipments.Count;

        public FieldDemoWaveQuiescenceMonitor(
            string productId,
            FieldDemoCopyWave wave,
            SimulationWorld world,
            TimedPostalExchange exchange,
            IEnumerable<string> nodeIds,
            Func<string, ListeningProgressSnapshot>
                listeningStatus)
        {
            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID cannot be empty.",
                    nameof(productId));

            _productId = productId;

            _wave = wave ??
                throw new ArgumentNullException(nameof(wave));

            _world = world ??
                throw new ArgumentNullException(nameof(world));

            _exchange = exchange ??
                throw new ArgumentNullException(nameof(exchange));

            if (nodeIds == null)
                throw new ArgumentNullException(nameof(nodeIds));

            _nodeIds = new List<string>(nodeIds);

            _listeningStatus = listeningStatus ??
                throw new ArgumentNullException(
                    nameof(listeningStatus));

            _exchange.ShipmentSent += OnShipmentSent;
            _world.DayCompleted += OnDayCompleted;
        }

        private void OnShipmentSent(PostalShipment shipment)
        {
            if (_disposed || _wave.IsPassivelyDiscoverable)
                return;

            var package = shipment.Package;

            // Ei palautekirjeitä eikä muita
            // kulttuurituotteita.
            if (package.CassetteCopyId == null ||
                package.PayloadProductId != _productId)
            {
                return;
            }

            _physicalShipments.Add(
                shipment.Id, shipment);
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed || _wave.IsPassivelyDiscoverable)
                return;

            if (_physicalShipments.Count == 0)
                return;

            foreach (var shipment in
                     _physicalShipments.Values)
            {
                if (shipment.Status ==
                    PostalShipmentStatus.Returned)
                {
                    continue;
                }

                // Matkalla, palautumassa tai
                // muuten keskeneräinen lähetys.
                if (shipment.Status !=
                    PostalShipmentStatus.Delivered)
                {
                    return;
                }

                // Toimitettu ei tarkoita avattua.
                // Avattu ei tarkoita kuunneltua.
                var listening = _listeningStatus(
                    shipment.Package.TargetNodeId);

                if (listening == null ||
                    listening.Phase !=
                        ListeningPhase.Completed)
                {
                    return;
                }
            }

            // Jokainen havaittu fyysinen lähetys
            // on joko palautunut tai sen
            // vastaanottajan kuuntelu on valmis.
            //
            // Kaikkien solmujen päivän päätökset
            // on jo suoritettu, joten uusia
            // jatkolähetyksiä ei voi syntyä
            // enää tällä toimintakierroksella.
            foreach (string nodeId in _nodeIds)
            {
                _wave.FinishCopying(nodeId);
            }

            _wave.TryCloseWave();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.ShipmentSent -= OnShipmentSent;
            _world.DayCompleted -= OnDayCompleted;

            _disposed = true;
        }
    }
}
