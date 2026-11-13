using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// One visible arrow in the network view.
    /// It may represent multiple communication events.
    /// </summary>
    public sealed class CommunicationDisplayArc
    {
        public string Id { get; }
        public string SourceNodeId { get; }
        public string TargetNodeId { get; }

        internal CommunicationDisplayArc(
            string id,
            string sourceNodeId,
            string targetNodeId)
        {
            Id = id;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
        }
    }

    public sealed class CommunicationArcProjectionResult
    {
        public IReadOnlyList<CommunicationDisplayArc> Edges
        {
            get;
        }

        internal CommunicationArcProjectionResult(
            IReadOnlyList<CommunicationDisplayArc> edges)
        {
            Edges = edges;
        }
    }

    /// <summary>
    /// Visual projection of initial connections and
    /// actual communication history.
    ///
    /// Existing initial connections remain visible.
    /// Runtime first-contact arrows follow dispatch
    /// events, not later route establishment.
    ///
    /// Multiple messages in the same direction are
    /// represented by one visible arrow.
    /// </summary>
    public static class CommunicationArcProjection
    {
        public static CommunicationArcProjectionResult Build(
            Network network,
            IEnumerable<string> nodeIds,
            CommunicationArcHistory history)
        {
            if (network == null)
                throw new ArgumentNullException(
                    nameof(network));

            if (nodeIds == null)
                throw new ArgumentNullException(
                    nameof(nodeIds));

            if (history == null)
                throw new ArgumentNullException(
                    nameof(history));

            var result = new List<CommunicationDisplayArc>();

            var visibleDirections =
                new HashSet<(string Source, string Target)>();

            foreach (string nodeId in nodeIds)
            {
                AddExisting(
                    network.GetOutgoingConnections(
                        nodeId,
                        CommunicationLayer.Interpersonal),
                    result,
                    visibleDirections);

                AddExisting(
                    network.GetOutgoingConnections(
                        nodeId,
                        CommunicationLayer.Insert),
                    result,
                    visibleDirections);
            }

            // The actual temporal correspondence history.
            // Each event is retained individually, but
            // only new directions create new line objects.
            foreach (var communication in history.Events)
            {
                var direction = (
                    communication.SourceNodeId,
                    communication.TargetNodeId);

                if (!visibleDirections.Add(direction))
                    continue;

                result.Add(new CommunicationDisplayArc(
                    "COMMUNICATION:" +
                    communication.SourceNodeId + ":" +
                    communication.TargetNodeId,
                    communication.SourceNodeId,
                    communication.TargetNodeId));
            }

            return new CommunicationArcProjectionResult(
                result.AsReadOnly());
        }

        private static void AddExisting(
            IReadOnlyList<NetworkConnection> connections,
            List<CommunicationDisplayArc> result,
            HashSet<(string Source, string Target)>
                visibleDirections)
        {
            foreach (var connection in connections)
            {
                // These routes are established on reply
                // delivery by the legacy postal service.
                //
                // Do not let them create delayed visual
                // arrows or duplicates. The real dispatch
                // events already represent both directions.
                if (connection.Id.StartsWith(
                        "ESTABLISHED:",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new CommunicationDisplayArc(
                    connection.Id,
                    connection.SourceNodeId,
                    connection.TargetNodeId));

                visibleDirections.Add((
                    connection.SourceNodeId,
                    connection.TargetNodeId));
            }
        }
    }
}