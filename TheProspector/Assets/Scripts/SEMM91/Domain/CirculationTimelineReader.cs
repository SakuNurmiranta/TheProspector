
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public enum CirculationHistoryKind
    {
        SeedDispatched,
        MessageOpened,
        ListeningDecision,
        CopyDispatched,
        TransferDispatched,
        Shelved,
        NegativeNoticeDispatched
    }

    public sealed class CirculationHistoryEvent
    {
        public int Day { get; }
        public CirculationHistoryKind Kind { get; }

        // Toiminnon tekijä. MessageOpened-
        // tapahtumassa viestin vastaanottaja.
        public string NodeId { get; }

        public string TargetNodeId { get; }
        public string ProductId { get; }
        public string ShipmentId { get; }

        public ListeningReaction? Reaction { get; }

        internal CirculationHistoryEvent(
            int day,
            CirculationHistoryKind kind,
            string nodeId,
            string productId = null,
            string targetNodeId = null,
            string shipmentId = null,
            ListeningReaction? reaction = null)
        {
            Day = day;
            Kind = kind;
            NodeId = nodeId;
            ProductId = productId;
            TargetNodeId = targetNodeId;
            ShipmentId = shipmentId;
            Reaction = reaction;
        }
    }

    /// <summary>
    /// Muuttumaton havainto simulaation
    /// tapahtumahistoriasta.
    /// </summary>
    public sealed class CirculationTimelineSnapshot
    {
        private readonly HashSet<string> _productIds;

        public int Day { get; }

        public IReadOnlyList<CirculationHistoryEvent>
            Events { get; }

        internal CirculationTimelineSnapshot(
            int day,
            IEnumerable<string> productIds,
            List<CirculationHistoryEvent> events)
        {
            Day = day;

            _productIds = new HashSet<string>(
                productIds,
                StringComparer.Ordinal);

            Events = new List<CirculationHistoryEvent>(
                events).AsReadOnly();
        }

        public IReadOnlyList<CirculationHistoryEvent>
            AtDay(int day)
        {
            if (day < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(day));

            return Events.Where(e => e.Day == day)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyList<CirculationHistoryEvent>
            ForProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId) ||
                !_productIds.Contains(productId))
            {
                throw new ArgumentException(
                    "Unknown cultural product.",
                    nameof(productId));
            }

            return Events
                .Where(e => e.ProductId == productId)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Koostaa tapahtumahistorian jo olemassa
    /// olevista simulaatiorekistereistä.
    ///
    /// Capture() ei lähetä viestejä,
    /// käsittele postia eikä muuta solmutiloja.
    /// </summary>
    public sealed class CirculationTimelineReader
    {
        private readonly ConfigurableCirculationScenario
            _scenario;

        public CirculationTimelineReader(
            ConfigurableCirculationScenario scenario)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(
                    nameof(scenario));
        }

        public CirculationTimelineSnapshot Capture()
        {
            var events =
                new List<CirculationHistoryEvent>();

            // Alustuslähetykset.
            foreach (string productId in
                     _scenario.ProductIds)
            {
                string sourceId =
                    _scenario.GetOriginal(productId)
                        .HolderNodeId;

                foreach (var shipment in
                         _scenario.GetSeedWave(productId))
                {
                    events.Add(new CirculationHistoryEvent(
                        0,
                        CirculationHistoryKind.SeedDispatched,
                        sourceId,
                        productId,
                        targetNodeId: shipment.RecipientNodeId,
                        shipmentId: shipment.Shipment.Id));
                }
            }

            // Kielteisten tiedotteiden
            // lähetyksistä muodostetaan hakemisto.
            // Niissä ei ole fyysistä kasettia.
            var noticeProducts =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            foreach (var notice in
                     _scenario.HardControl.Notices)
            {
                noticeProducts.Add(
                    notice.Shipment.Id,
                    notice.ProductId);
            }

            // Avatut viestit.
            foreach (var opened in
                     _scenario.World.OpenedMessages)
            {
                var copy =
                    _scenario.CopyIndex.ResolveShipment(
                        opened.ShipmentId);

                string productId = copy?.ProductId;

                if (productId == null)
                {
                    noticeProducts.TryGetValue(
                        opened.ShipmentId,
                        out productId);
                }

                events.Add(new CirculationHistoryEvent(
                    opened.Day,
                    CirculationHistoryKind.MessageOpened,
                    opened.NodeId,
                    productId,
                    shipmentId: opened.ShipmentId));
            }

            // Kuuntelupäätökset sekä niiden
            // aiheuttamat myönteiset kopioinnit.
            foreach (string nodeId in
                     _scenario.NodeIds)
            {
                var decisionDays =
                    new Dictionary<string, int>(
                        StringComparer.Ordinal);

                foreach (var decision in
                         _scenario.GetReactionPolicy(nodeId)
                             .Decisions)
                {
                    events.Add(
                        new CirculationHistoryEvent(
                            decision.Day,
                            CirculationHistoryKind
                                .ListeningDecision,
                            nodeId,
                            decision.ProductId,
                            reaction: decision.Reaction));

                    if (decision.Reaction ==
                        ListeningReaction.SupportAndCopy)
                    {
                        decisionDays[decision.ProductId] =
                            decision.Day;
                    }
                }

                foreach (var shipment in
                         _scenario.GetPolicy(nodeId)
                             .Dispatched)
                {
                    // Tässä maailmassa myönteinen
                    // kopiointi tapahtuu päätöspäivänä.
                    if (!decisionDays.TryGetValue(
                            shipment.Copy.ProductId,
                            out int day))
                    {
                        continue;
                    }

                    events.Add(
                        new CirculationHistoryEvent(
                            day,
                            CirculationHistoryKind
                                .CopyDispatched,
                            nodeId,
                            shipment.Copy.ProductId,
                            targetNodeId:
                                shipment.RecipientNodeId,
                            shipmentId:
                                shipment.Shipment.Id));
                }
            }

            // Pehmeä kontrolli: sama kasetti
            // lähtee eteenpäin ilman kopiointia.
            foreach (var transfer in
                     _scenario.Control.Transfers)
            {
                events.Add(new CirculationHistoryEvent(
                    transfer.Day,
                    CirculationHistoryKind
                        .TransferDispatched,
                    transfer.SenderNodeId,
                    transfer.Copy.ProductId,
                    targetNodeId:
                        transfer.RecipientNodeId,
                    shipmentId: transfer.Shipment.Id));
            }

            // Kova kontrolli: fyysinen kasetti
            // jää solmun haltuun.
            foreach (var shelf in
                     _scenario.Control.Shelves)
            {
                events.Add(new CirculationHistoryEvent(
                    shelf.Day,
                    CirculationHistoryKind.Shelved,
                    shelf.NodeId,
                    shelf.ProductId));
            }

            // Kovan kontrollin kielteinen
            // tiedote on lähetys, ei vielä
            // vastaanottajan oppima kanta.
            foreach (var notice in
                     _scenario.HardControl.Notices)
            {
                events.Add(new CirculationHistoryEvent(
                    notice.Day,
                    CirculationHistoryKind
                        .NegativeNoticeDispatched,
                    notice.SenderNodeId,
                    notice.ProductId,
                    targetNodeId:
                        notice.RecipientNodeId,
                    shipmentId:
                        notice.Shipment.Id));
            }

            // Päivien sisällä järjestys on
            // deterministinen näyttöjärjestys,
            // ei väite kaikkien tapahtumien
            // tarkasta suoritusjärjestyksestä.
            events.Sort(CompareEvents);

            return new CirculationTimelineSnapshot(
                _scenario.World.CurrentDay,
                _scenario.ProductIds,
                events);
        }

        private static int CompareEvents(
            CirculationHistoryEvent a,
            CirculationHistoryEvent b)
        {
            int result = a.Day.CompareTo(b.Day);

            if (result != 0)
                return result;

            result = a.Kind.CompareTo(b.Kind);

            if (result != 0)
                return result;

            result = string.CompareOrdinal(
                a.NodeId, b.NodeId);

            if (result != 0)
                return result;

            result = string.CompareOrdinal(
                a.ProductId, b.ProductId);

            if (result != 0)
                return result;

            result = string.CompareOrdinal(
                a.TargetNodeId, b.TargetNodeId);

            if (result != 0)
                return result;

            return string.CompareOrdinal(
                a.ShipmentId, b.ShipmentId);
        }
    }
}
