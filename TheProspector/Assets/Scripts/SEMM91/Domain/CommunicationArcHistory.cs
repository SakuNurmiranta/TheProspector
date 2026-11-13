using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// One actual outgoing Layer 2 communication.
    /// Created at dispatch, not at delivery.
    ///
    /// Every shipment has its own event instance.
    /// A reply creates a new event in the reverse direction.
    /// </summary>
    public sealed class CommunicationArcEvent
    {
        private readonly PostalShipment _shipment;

        public string ShipmentId => _shipment.Id;
        public string PackageId => _shipment.Package.Id;

        public string SourceNodeId =>
            _shipment.Package.SourceNodeId;

        public string TargetNodeId =>
            _shipment.Package.TargetNodeId;

        public string ReplyToPackageId =>
            _shipment.Package.ReplyToPackageId;

        public int SentDay { get; }

        // Status can change later without changing
        // the historical fact of dispatch.
        public PostalShipmentStatus Status =>
            _shipment.Status;

        internal CommunicationArcEvent(
            PostalShipment shipment,
            int sentDay)
        {
            _shipment = shipment ??
                throw new ArgumentNullException(
                    nameof(shipment));

            SentDay = sentDay;
        }
    }

    /// <summary>
    /// Append-only history of sent communications.
    ///
    /// Observes existing postal events. Does not modify
    /// network routes, capital, trust or postal timing.
    /// </summary>
    public sealed class CommunicationArcHistory : IDisposable
    {
        private readonly SimulationClock _clock;
        private readonly TimedPostalExchange _exchange;

        private readonly List<CommunicationArcEvent>
            _events = new List<CommunicationArcEvent>();

        private readonly IReadOnlyList<CommunicationArcEvent>
            _readOnlyEvents;

        private bool _disposed;

        public IReadOnlyList<CommunicationArcEvent> Events =>
            _readOnlyEvents;

        public CommunicationArcHistory(
            SimulationClock clock,
            TimedPostalExchange exchange)
        {
            _clock = clock ??
                throw new ArgumentNullException(
                    nameof(clock));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _readOnlyEvents = _events.AsReadOnly();

            _exchange.ShipmentSent += OnShipmentSent;
        }

        private void OnShipmentSent(PostalShipment shipment)
        {
            _events.Add(
                new CommunicationArcEvent(
                    shipment,
                    _clock.CurrentDay));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.ShipmentSent -= OnShipmentSent;
            _disposed = true;
        }
    }
}