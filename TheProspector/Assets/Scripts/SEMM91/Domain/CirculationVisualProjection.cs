
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public enum CirculationNodeTone
    {
        Neutral,
        SelectedNode,
        OutgoingContact,
        NotReceived,
        ReceivedUnopened,
        Queued,
        Listening,
        Completed
    }

    public sealed class CirculationNodeVisual
    {
        public string Id { get; }
        public CirculationNodeTone Tone { get; }
        public float Progress { get; }

        internal CirculationNodeVisual(
            string id,
            CirculationNodeTone tone,
            float progress)
        {
            Id = id;
            Tone = tone;
            Progress = progress;
        }
    }

    public sealed class CirculationConnectionVisual
    {
        public string Id { get; }
        public string SenderId { get; }
        public string RecipientId { get; }
        public bool IsHighlighted { get; }

        internal CirculationConnectionVisual(
            string id,
            string senderId,
            string recipientId,
            bool isHighlighted)
        {
            Id = id;
            SenderId = senderId;
            RecipientId = recipientId;
            IsHighlighted = isHighlighted;
        }
    }

    public sealed class CirculationVisualSnapshot
    {
        private readonly Dictionary<string, CirculationNodeVisual>
            _nodesById =
                new Dictionary<string, CirculationNodeVisual>(
                    StringComparer.Ordinal);

        public int Day { get; }

        public IReadOnlyList<CirculationNodeVisual> Nodes { get; }

        public IReadOnlyList<CirculationConnectionVisual>
            Connections { get; }

        internal CirculationVisualSnapshot(
            int day,
            List<CirculationNodeVisual> nodes,
            List<CirculationConnectionVisual> connections)
        {
            Day = day;
            Nodes = nodes.AsReadOnly();
            Connections = connections.AsReadOnly();

            foreach (var node in nodes)
                _nodesById.Add(node.Id, node);
        }

        public CirculationNodeVisual GetNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) ||
                !_nodesById.TryGetValue(nodeId, out var result))
            {
                throw new ArgumentException(
                    "Unknown visual node: " + nodeId,
                    nameof(nodeId));
            }

            return result;
        }
    }

    /// <summary>
    /// Muuntaa vain luettavan simulaatiohavainnon
    /// käyttöliittymän esitystiloiksi.
    ///
    /// Ei kutsu SimulationWorldin metodeja.
    /// Ei sisällä UnityEngine-riippuvuuksia.
    /// </summary>
    public static class CirculationVisualProjection
    {
        public static CirculationVisualSnapshot Build(
            CirculationObservation observation,
            string selectedNodeId = null,
            string selectedProductId = null)
        {
            if (observation == null)
                throw new ArgumentNullException(
                    nameof(observation));

            if (selectedNodeId != null &&
                selectedProductId != null)
            {
                throw new ArgumentException(
                    "Select either a node or a demo.");
            }

            if (selectedNodeId != null)
                observation.GetNode(selectedNodeId);

            if (selectedProductId != null &&
                !observation.ProductIds.Contains(
                    selectedProductId))
            {
                throw new ArgumentException(
                    "Unknown demo: " + selectedProductId,
                    nameof(selectedProductId));
            }

            var nodes = new List<CirculationNodeVisual>();
            var connections =
                new List<CirculationConnectionVisual>();

            var outgoingTargets =
                new HashSet<string>(StringComparer.Ordinal);

            if (selectedNodeId != null)
            {
                foreach (var edge in observation.Connections)
                {
                    if (edge.SenderId == selectedNodeId)
                        outgoingTargets.Add(edge.RecipientId);
                }
            }

            foreach (var node in observation.Nodes)
            {
                CirculationNodeTone tone =
                    CirculationNodeTone.Neutral;

                float progress = 0f;

                if (selectedNodeId != null)
                {
                    if (node.Id == selectedNodeId)
                    {
                        tone = CirculationNodeTone.SelectedNode;
                    }
                    else if (outgoingTargets.Contains(node.Id))
                    {
                        tone = CirculationNodeTone.OutgoingContact;
                    }
                }
                else if (selectedProductId != null)
                {
                    var state = observation.GetState(
                        node.Id, selectedProductId);

                    tone = MapPhase(state.Phase);
                    progress = state.Progress;
                }

                nodes.Add(
                    new CirculationNodeVisual(
                        node.Id, tone, progress));
            }

            foreach (var edge in observation.Connections)
            {
                connections.Add(
                    new CirculationConnectionVisual(
                        edge.Id,
                        edge.SenderId,
                        edge.RecipientId,
                        selectedNodeId != null &&
                        edge.SenderId == selectedNodeId));
            }

            return new CirculationVisualSnapshot(
                observation.Day,
                nodes,
                connections);
        }

        private static CirculationNodeTone MapPhase(
            ObservableDemoPhase phase)
        {
            switch (phase)
            {
                case ObservableDemoPhase.NotReceived:
                    return CirculationNodeTone.NotReceived;

                case ObservableDemoPhase.ReceivedUnopened:
                    return CirculationNodeTone.ReceivedUnopened;

                case ObservableDemoPhase.Queued:
                    return CirculationNodeTone.Queued;

                case ObservableDemoPhase.Listening:
                    return CirculationNodeTone.Listening;

                case ObservableDemoPhase.Completed:
                    return CirculationNodeTone.Completed;

                default:
                    throw new InvalidOperationException(
                        "Unknown observable demo phase.");
            }
        }
    }
}
