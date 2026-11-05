
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class CirculationNodeInspector
    {
        public string NodeId { get; }
        public string CountryCode { get; }
        public int Capital { get; }

        public IReadOnlyList<ObservedConnection> Incoming
        {
            get;
        }

        public IReadOnlyList<ObservedConnection> Outgoing
        {
            get;
        }

        public IReadOnlyList<ObservedNodeProduct> Products
        {
            get;
        }

        // Mitä tämä solmu on oppinut
        // muiden solmujen kannoista.
        public IReadOnlyList<ObservedCommunicatedStance>
            OpinionsReceived { get; }

        // Ketkä muut ovat saaneet tietoonsa
        // tämän solmun kannan.
        public IReadOnlyList<ObservedCommunicatedStance>
            OpinionsKnownByOthers { get; }

        internal CirculationNodeInspector(
            ObservedNode node,
            List<ObservedConnection> incoming,
            List<ObservedConnection> outgoing,
            List<ObservedNodeProduct> products,
            List<ObservedCommunicatedStance> received,
            List<ObservedCommunicatedStance> knownByOthers)
        {
            NodeId = node.Id;
            CountryCode = node.CountryCode;
            Capital = node.Capital;

            Incoming = incoming.AsReadOnly();
            Outgoing = outgoing.AsReadOnly();
            Products = products.AsReadOnly();

            OpinionsReceived = received.AsReadOnly();
            OpinionsKnownByOthers =
                knownByOthers.AsReadOnly();
        }
    }

    public sealed class CirculationProductInspector
    {
        public string ProductId { get; }

        public IReadOnlyList<ObservedNodeProduct>
            NodeStates { get; }

        public IReadOnlyList<ObservedCommunicatedStance>
            KnownCommunications { get; }

        public int EverReceivedCount { get; }
        public int NotReceivedCount { get; }
        public int ReceivedUnopenedCount { get; }
        public int QueuedCount { get; }
        public int ListeningCount { get; }
        public int CompletedCount { get; }

        public int SupportCount { get; }
        public int TransferCount { get; }
        public int ShelveCount { get; }

        internal CirculationProductInspector(
            string productId,
            List<ObservedNodeProduct> states,
            List<ObservedCommunicatedStance> communications)
        {
            ProductId = productId;
            NodeStates = states.AsReadOnly();
            KnownCommunications =
                communications.AsReadOnly();

            EverReceivedCount =
                states.Count(s => s.EverReceived);

            NotReceivedCount =
                states.Count(s =>
                    s.Phase ==
                    ObservableDemoPhase.NotReceived);

            ReceivedUnopenedCount =
                states.Count(s =>
                    s.Phase ==
                    ObservableDemoPhase.ReceivedUnopened);

            QueuedCount =
                states.Count(s =>
                    s.Phase ==
                    ObservableDemoPhase.Queued);

            ListeningCount =
                states.Count(s =>
                    s.Phase ==
                    ObservableDemoPhase.Listening);

            CompletedCount =
                states.Count(s =>
                    s.Phase ==
                    ObservableDemoPhase.Completed);

            // Nämä ovat kuuntelun jälkeisiä
            // päätöksiä, eivät julkisten
            // kannanottojen lukumääriä.
            SupportCount =
                states.Count(s =>
                    s.Reaction ==
                    ListeningReaction.SupportAndCopy);

            TransferCount =
                states.Count(s =>
                    s.Reaction ==
                    ListeningReaction.OpposeAndTransfer);

            ShelveCount =
                states.Count(s =>
                    s.Reaction ==
                    ListeningReaction.OpposeAndShelve);
        }
    }

    public static class CirculationInspectorProjection
    {
        public static CirculationNodeInspector ForNode(
            CirculationObservation observation,
            string nodeId)
        {
            if (observation == null)
                throw new ArgumentNullException(
                    nameof(observation));

            // Samalla validoidaan solmun olemassaolo.
            ObservedNode node =
                observation.GetNode(nodeId);

            var incoming = observation.Connections
                .Where(c => c.RecipientId == nodeId)
                .ToList();

            var outgoing = observation.Connections
                .Where(c => c.SenderId == nodeId)
                .ToList();

            var products = observation.ProductStates
                .Where(p => p.NodeId == nodeId)
                .ToList();

            var received =
                observation.CommunicatedStances
                    .Where(s =>
                        s.ObserverNodeId == nodeId)
                    .ToList();

            var knownByOthers =
                observation.CommunicatedStances
                    .Where(s =>
                        s.SpeakerNodeId == nodeId)
                    .ToList();

            return new CirculationNodeInspector(
                node,
                incoming,
                outgoing,
                products,
                received,
                knownByOthers);
        }

        public static CirculationProductInspector ForProduct(
            CirculationObservation observation,
            string productId)
        {
            if (observation == null)
                throw new ArgumentNullException(
                    nameof(observation));

            if (string.IsNullOrWhiteSpace(productId) ||
                !observation.ProductIds.Contains(productId))
            {
                throw new ArgumentException(
                    "Unknown cultural product.",
                    nameof(productId));
            }

            var states = observation.ProductStates
                .Where(s => s.ProductId == productId)
                .ToList();

            var communications =
                observation.CommunicatedStances
                    .Where(s =>
                        s.ProductId == productId)
                    .ToList();

            return new CirculationProductInspector(
                productId,
                states,
                communications);
        }
    }
}
