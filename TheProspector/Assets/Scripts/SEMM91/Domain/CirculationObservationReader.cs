using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public enum ObservableDemoPhase
    {
        NotReceived,
        ReceivedUnopened,
        Queued,
        Listening,
        Completed
    }

    public sealed class ObservedNode
    {
        public string Id { get; }
        public string CountryCode { get; }
        public int Capital { get; }

        internal ObservedNode(
            string id,
            string countryCode,
            int capital)
        {
            Id = id;
            CountryCode = countryCode;
            Capital = capital;
        }
    }

    public sealed class ObservedConnection
    {
        public string Id { get; }
        public string SenderId { get; }
        public string RecipientId { get; }

        internal ObservedConnection(
            string id,
            string senderId,
            string recipientId)
        {
            Id = id;
            SenderId = senderId;
            RecipientId = recipientId;
        }
    }

    public sealed class ObservedNodeProduct
    {
        public string NodeId { get; }
        public string ProductId { get; }

        // Historiallinen vastaanotto, ei
        // väite nykyisestä hallussapidosta.
        public bool EverReceived { get; }

        public ObservableDemoPhase Phase { get; }
        public float Progress { get; }

        // Solmun tekemä päätös.
        // Ei automaattisesti julkinen kanta.
        public ListeningReaction? Reaction { get; }
        public int? ReactionDay { get; }

        internal ObservedNodeProduct(
            string nodeId,
            string productId,
            bool everReceived,
            ObservableDemoPhase phase,
            float progress,
            ListeningReaction? reaction,
            int? reactionDay)
        {
            NodeId = nodeId;
            ProductId = productId;
            EverReceived = everReceived;
            Phase = phase;
            Progress = progress;
            Reaction = reaction;
            ReactionDay = reactionDay;
        }
    }


    public sealed class ObservedCommunicatedStance
    {
        // Kuka vastaanotti ja avasi viestin.
        public string ObserverNodeId { get; }

        // Kenen kannan vastaanottaja sai tietää.
        public string SpeakerNodeId { get; }

        public string ProductId { get; }
        public int Thumb { get; }

        internal ObservedCommunicatedStance(
            string observerNodeId,
            string speakerNodeId,
            string productId,
            int thumb)
        {
            ObserverNodeId = observerNodeId;
            SpeakerNodeId = speakerNodeId;
            ProductId = productId;
            Thumb = thumb;
        }
    }

    /// <summary>
    /// Muuttumaton, yhden päivän havaintokuva.
    ///
    /// Ei sisällä viittauksia eläviin
    /// kulttuuripääoma-, kasetti- tai
    /// toimintapolitiikkaolioihin.
    /// </summary>
    public sealed class CirculationObservation
    {
        private readonly Dictionary<string, ObservedNode>
            _nodesById =
                new(
                    StringComparer.Ordinal);

        private readonly Dictionary<
            (string NodeId, string ProductId),
            ObservedNodeProduct> _statesByKey = new();

        
        private readonly Dictionary<
            (string Observer, string Speaker, string Product),
            int> _knownThumbs =
            new Dictionary<
                (string Observer, string Speaker, string Product),
                int>();

        private readonly HashSet<string> _productIdSet =
            new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<ObservedCommunicatedStance>
            CommunicatedStances { get; }

        public int Day { get; }

        public IReadOnlyList<ObservedNode> Nodes { get; }

        public IReadOnlyList<ObservedConnection>
            Connections { get; }

        public IReadOnlyList<string> ProductIds { get; }

        public IReadOnlyList<ObservedNodeProduct>
            ProductStates { get; }

       
        internal CirculationObservation(
            int day,
            List<ObservedNode> nodes,
            List<ObservedConnection> connections,
            List<string> productIds,
            List<ObservedNodeProduct> states,
            List<ObservedCommunicatedStance> communicatedStances)
        {
            Day = day;

            Nodes = nodes.AsReadOnly();
            Connections = connections.AsReadOnly();
            ProductIds = productIds.AsReadOnly();
            ProductStates = states.AsReadOnly();
            CommunicatedStances = communicatedStances.AsReadOnly();

            foreach (var node in nodes)
                _nodesById.Add(node.Id, node);

            foreach (string productId in productIds)
                _productIdSet.Add(productId);

            foreach (var state in states)
            {
                _statesByKey.Add(
                    (state.NodeId, state.ProductId), state);
            }

            foreach (var stance in communicatedStances)
            {
                _knownThumbs.Add(
                    (
                        stance.ObserverNodeId,
                        stance.SpeakerNodeId,
                        stance.ProductId
                    ),
                    stance.Thumb);
            }
        }
    
        
        public int? GetKnownThumb(
            string observerNodeId,
            string speakerNodeId,
            string productId)
        {
            if (string.IsNullOrWhiteSpace(observerNodeId) ||
                !_nodesById.ContainsKey(observerNodeId))
            {
                throw new ArgumentException(
                    "Unknown observer node.",
                    nameof(observerNodeId));
            }

            if (string.IsNullOrWhiteSpace(speakerNodeId) ||
                !_nodesById.ContainsKey(speakerNodeId))
            {
                throw new ArgumentException(
                    "Unknown speaker node.",
                    nameof(speakerNodeId));
            }

            if (string.IsNullOrWhiteSpace(productId) ||
                !_productIdSet.Contains(productId))
            {
                throw new ArgumentException(
                    "Unknown cultural product.",
                    nameof(productId));
            }

            return _knownThumbs.TryGetValue(
                (observerNodeId, speakerNodeId, productId),
                out int thumb)
                ? thumb
                : (int?)null;
        }


        public ObservedNode GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) ||
                !_nodesById.TryGetValue(
                    nodeId, out var node))
                throw new ArgumentException(
                    "Unknown node: " + nodeId,
                    nameof(nodeId));

            return node;
        }

        public ObservedNodeProduct GetState(
            string nodeId,
            string productId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) ||
                string.IsNullOrWhiteSpace(productId) ||
                !_statesByKey.TryGetValue(
                    (nodeId, productId), out var state))
                throw new ArgumentException(
                    "Unknown node/product pair.",
                    nameof(productId));

            return state;
        }
    }

    /// <summary>
    /// Vain lukeva rajapinta simulaatiomaailmaan.
    ///
    /// Topologia kopioidaan määrittelystä
    /// lukijan luomishetkellä.
    ///
    /// Dynaamiset tilat luetaan jokaisen
    /// Capture()-kutsun yhteydessä.
    /// </summary>
    public sealed class CirculationObservationReader
    {
        private sealed class NodeMetadata
        {
            public string Id;
            public string CountryCode;
        }

        private readonly ConfigurableCirculationScenario
            _scenario;

        private readonly List<NodeMetadata>
            _nodes = new();

        private readonly List<ObservedConnection>
            _connections = new();

        public CirculationObservationReader(
            ConfigurableCirculationScenario scenario,
            CirculationWorldDefinition definition)
        {
            _scenario = scenario ??
                        throw new ArgumentNullException(
                            nameof(scenario));

            if (definition == null)
                throw new ArgumentNullException(
                    nameof(definition));

            foreach (var node in definition.Nodes)
                _nodes.Add(new NodeMetadata
                {
                    Id = node.Id,
                    CountryCode = node.CountryCode
                });

            foreach (var connection in
                     definition.Connections)
                _connections.Add(
                    new ObservedConnection(
                        connection.Id,
                        connection.SenderId,
                        connection.RecipientId));
        }

        public CirculationObservation Capture()
        {
            var nodes = new List<ObservedNode>();

            var states =
                new List<ObservedNodeProduct>();

            var productIds =
                new List<string>(_scenario.ProductIds);

            foreach (var node in _nodes)
            {
                nodes.Add(
                    new ObservedNode(
                        node.Id,
                        node.CountryCode,
                        _scenario.Capital.GetCapital(node.Id)));

                foreach (var productId in productIds)
                    states.Add(
                        CaptureProductState(
                            node.Id, productId));
            }


            var communicatedStances =
                new List<ObservedCommunicatedStance>();

            foreach (var observer in _nodes)
            {
                foreach (var speaker in _nodes)
                {
                    if (observer.Id == speaker.Id)
                        continue;

                    foreach (string productId in productIds)
                    {
                        int? thumb =
                            _scenario.Stances.GetLastThumb(
                                observer.Id,
                                speaker.Id,
                                productId);

                        if (!thumb.HasValue)
                            continue;

                        communicatedStances.Add(
                            new ObservedCommunicatedStance(
                                observer.Id,
                                speaker.Id,
                                productId,
                                thumb.Value));
                    }
                }
            }

            return new CirculationObservation(
                _scenario.World.CurrentDay,
                nodes,
                new List<ObservedConnection>(_connections),
                productIds,
                states,
                communicatedStances);

        }

        private ObservedNodeProduct CaptureProductState(
            string nodeId,
            string productId)
        {
            var everReceived =
                _scenario.Inventory.HasEverHeldProduct(
                    nodeId, productId);

            var listening =
                _scenario.GetListeningStatus(
                    nodeId, productId);

            ObservableDemoPhase phase;
            var progress = 0f;

            if (listening == null)
            {
                phase = everReceived
                    ? ObservableDemoPhase.ReceivedUnopened
                    : ObservableDemoPhase.NotReceived;
            }
            else
            {
                progress = listening.Progress;

                switch (listening.Phase)
                {
                    case ListeningPhase.Queued:
                        phase = ObservableDemoPhase.Queued;
                        break;

                    case ListeningPhase.Listening:
                        phase = ObservableDemoPhase.Listening;
                        break;

                    case ListeningPhase.Completed:
                        phase = ObservableDemoPhase.Completed;
                        break;

                    default:
                        throw new InvalidOperationException(
                            "Unknown listening phase.");
                }
            }

            ListeningReaction? reaction = null;
            int? reactionDay = null;

            var policy =
                _scenario.GetReactionPolicy(nodeId);

            foreach (var decision in policy.Decisions)
            {
                if (!string.Equals(
                        decision.ProductId,
                        productId,
                        StringComparison.Ordinal))
                    continue;

                reaction = decision.Reaction;
                reactionDay = decision.Day;
                break;
            }

            return new ObservedNodeProduct(
                nodeId,
                productId,
                everReceived,
                phase,
                progress,
                reaction,
                reactionDay);
        }
    }
}