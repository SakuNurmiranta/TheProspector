
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class DemoOriginAtBirth
    {
        public string ProductId { get; }
        public string InitialHolderNodeId { get; }
        public int BirthDay { get; }
        public int CapitalAtBirth { get; }
        public int OutgoingArcsAtBirth { get; }
        public bool IsFieldBorn { get; }

        internal DemoOriginAtBirth(
            string productId,
            string initialHolderNodeId,
            int birthDay,
            int capitalAtBirth,
            int outgoingArcsAtBirth,
            bool isFieldBorn)
        {
            ProductId = productId;
            InitialHolderNodeId = initialHolderNodeId;
            BirthDay = birthDay;
            CapitalAtBirth = capitalAtBirth;
            OutgoingArcsAtBirth = outgoingArcsAtBirth;
            IsFieldBorn = isFieldBorn;
        }
    }

    public sealed class ResearchShipmentObservation
    {
        public string ShipmentId { get; }
        public string ProductId { get; }
        public string SenderNodeId { get; }
        public string RecipientNodeId { get; }
        public string ConnectionId { get; }

        public int SentDay { get; }
        public int? DeliveredDay { get; }
        public int? OpenedDay { get; }

        public PostalShipmentStatus Status { get; }

        public bool IsInitialSeed { get; }
        public bool IsCopyDistribution { get; }

        public bool ViaEstablishedFirstContactArc =>
            ConnectionId.StartsWith(
                "ESTABLISHED:",
                StringComparison.Ordinal);

        public bool IsFirstContactLetter =>
            ConnectionId.StartsWith(
                "FIRST-ROUTE:",
                StringComparison.Ordinal);

        public bool IsFirstContactReply =>
            ConnectionId.StartsWith(
                "FIRST-REPLY-ROUTE:",
                StringComparison.Ordinal);

        internal ResearchShipmentObservation(
            PostalShipment shipment,
            int sentDay,
            int? deliveredDay,
            int? openedDay,
            bool isInitialSeed,
            bool isCopyDistribution)
        {
            var package = shipment.Package;

            ShipmentId = shipment.Id;
            ProductId = package.PayloadProductId;
            SenderNodeId = package.SourceNodeId;
            RecipientNodeId = package.TargetNodeId;
            ConnectionId = package.ConnectionId;

            SentDay = sentDay;
            DeliveredDay = deliveredDay;
            OpenedDay = openedDay;
            Status = shipment.Status;

            IsInitialSeed = isInitialSeed;
            IsCopyDistribution = isCopyDistribution;
        }

        public override string ToString()
        {
            return
                "Day=" + SentDay +
                " Product=" + ProductId +
                " Shipment=" + ShipmentId +
                " Route=" + SenderNodeId +
                "->" + RecipientNodeId +
                " Status=" + Status +
                " InitialSeed=" + IsInitialSeed +
                " CopyDistribution=" + IsCopyDistribution +
                " EstablishedContact=" +
                    ViaEstablishedFirstContactArc +
                " DeliveredDay=" + DeliveredDay +
                " OpenedDay=" + OpenedDay;
        }
    }

    /// <summary>
    /// Observes physical cassette traffic without changing it.
    ///
    /// Captures initialized seed shipments retrospectively,
    /// then subscribes to actual postal and distribution events.
    ///
    /// Returned shipments retain their original send record.
    /// Each snapshot copies the current postal status.
    /// </summary>
    public sealed class CirculationResearchJournal : IDisposable
    {
        private sealed class TrackedShipment
        {
            public PostalShipment Shipment;
            public int SentDay;
            public int? DeliveredDay;
            public int? OpenedDay;
            public bool IsInitialSeed;
            public bool IsCopyDistribution;
        }

        private readonly ConfigurableCirculationScenario _scenario;

        private readonly Dictionary<string, DemoOriginAtBirth>
            _initialOrigins =
                new Dictionary<string, DemoOriginAtBirth>(
                    StringComparer.Ordinal);

        private readonly Dictionary<string, TrackedShipment>
            _byShipmentId =
                new Dictionary<string, TrackedShipment>(
                    StringComparer.Ordinal);

        private readonly List<TrackedShipment> _shipments =
            new List<TrackedShipment>();

        private bool _disposed;

        public CirculationResearchJournal(
            ConfigurableCirculationScenario scenario,
            CirculationInitialWorld initialWorld)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(nameof(scenario));

            if (initialWorld == null)
                throw new ArgumentNullException(
                    nameof(initialWorld));

            foreach (var seed in initialWorld.Definition.Seeds)
            {
                int outgoing = scenario.Network
                    .GetOutgoingConnections(
                        seed.SourceNodeId,
                        CommunicationLayer.Interpersonal)
                    .Count;

                _initialOrigins.Add(
                    seed.DemoId,
                    new DemoOriginAtBirth(
                        seed.DemoId,
                        seed.SourceNodeId,
                        0,
                        scenario.Capital.GetCapital(
                            seed.SourceNodeId),
                        outgoing,
                        false));

                foreach (var dispatch in
                         scenario.GetSeedWave(seed.DemoId))
                {
                    Track(
                        dispatch.Shipment,
                        0,
                        isInitialSeed: true,
                        isCopyDistribution: true);
                }
            }

            scenario.Exchange.ShipmentSent += OnShipmentSent;
            scenario.Exchange.ShipmentDelivered += OnShipmentDelivered;
            scenario.Exchange.MessageOpened += OnMessageOpened;
            scenario.Distribution.CopyDispatched += OnCopyDispatched;
        }

        public bool TryGetInitialOrigin(
            string productId,
            out DemoOriginAtBirth origin)
        {
            return _initialOrigins.TryGetValue(
                productId, out origin);
        }

        public IReadOnlyList<ResearchShipmentObservation>
            CaptureShipments()
        {
            var result =
                new List<ResearchShipmentObservation>();

            foreach (var tracked in _shipments)
            {
                result.Add(
                    new ResearchShipmentObservation(
                        tracked.Shipment,
                        tracked.SentDay,
                        tracked.DeliveredDay,
                        tracked.OpenedDay,
                        tracked.IsInitialSeed,
                        tracked.IsCopyDistribution));
            }

            result.Sort((a, b) =>
            {
                int day = a.SentDay.CompareTo(b.SentDay);

                return day != 0
                    ? day
                    : string.CompareOrdinal(
                        a.ShipmentId, b.ShipmentId);
            });

            return result.AsReadOnly();
        }

        private void Track(
            PostalShipment shipment,
            int day,
            bool isInitialSeed,
            bool isCopyDistribution)
        {
            if (shipment == null ||
                shipment.Package.CassetteCopyId == null)
            {
                return;
            }

            if (_byShipmentId.ContainsKey(shipment.Id))
                return;

            var tracked = new TrackedShipment
            {
                Shipment = shipment,
                SentDay = day,
                IsInitialSeed = isInitialSeed,
                IsCopyDistribution = isCopyDistribution
            };

            _byShipmentId.Add(shipment.Id, tracked);
            _shipments.Add(tracked);
        }

        private void OnShipmentSent(PostalShipment shipment)
        {
            if (_disposed)
                return;

            Track(
                shipment,
                _scenario.Clock.CurrentDay,
                false,
                false);
        }

        private void OnCopyDispatched(
            CopyDistributionShipment distribution)
        {
            if (_disposed)
                return;

            if (_byShipmentId.TryGetValue(
                    distribution.Shipment.Id,
                    out var tracked))
            {
                tracked.IsCopyDistribution = true;
            }
        }

        private void OnShipmentDelivered(PostalShipment shipment)
        {
            if (_disposed)
                return;

            if (_byShipmentId.TryGetValue(
                    shipment.Id, out var tracked))
            {
                tracked.DeliveredDay =
                    _scenario.Clock.CurrentDay;
            }
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            if (_byShipmentId.TryGetValue(
                    item.ShipmentId, out var tracked))
            {
                tracked.OpenedDay =
                    _scenario.Clock.CurrentDay;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scenario.Exchange.ShipmentSent -= OnShipmentSent;
            _scenario.Exchange.ShipmentDelivered -= OnShipmentDelivered;
            _scenario.Exchange.MessageOpened -= OnMessageOpened;
            _scenario.Distribution.CopyDispatched -= OnCopyDispatched;

            _disposed = true;
        }
    }
}
