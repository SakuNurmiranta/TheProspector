
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Resolves currently usable interpersonal circulation routes.
    ///
    /// Preserves initialized routes in their original order and
    /// appends newly established personal connections.
    ///
    /// Layer 3 insert connections are never cassette routes.
    ///
    /// A new connection requires a known postal address before
    /// becoming usable. Collection knowledge is never inferred
    /// merely from the existence of a network connection.
    /// </summary>
    public static class LiveInterpersonalRoutes
    {
        public static IReadOnlyList<CopyDistributionRoute> Resolve(
            Network network,
            string senderNodeId,
            IEnumerable<CopyDistributionRoute> initializedRoutes)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            if (string.IsNullOrWhiteSpace(senderNodeId))
                throw new ArgumentException(
                    "Sender ID is required.",
                    nameof(senderNodeId));

            if (initializedRoutes == null)
                throw new ArgumentNullException(
                    nameof(initializedRoutes));

            if (!network.TryGetNode(senderNodeId, out _))
                throw new ArgumentException(
                    "Sender node does not exist.",
                    nameof(senderNodeId));

            var outgoing = network.GetOutgoingConnections(
                senderNodeId,
                CommunicationLayer.Interpersonal);

            var currentConnections =
                new Dictionary<string, NetworkConnection>(
                    StringComparer.Ordinal);

            foreach (var connection in outgoing)
                currentConnections.Add(
                    connection.Id, connection);

            var seenIds = new HashSet<string>(
                StringComparer.Ordinal);

            var routes = new List<CopyDistributionRoute>();

            // Retain original routes first. Existing scenario
            // behavior and its recipient ordering stay intact.
            foreach (var route in initializedRoutes)
            {
                if (route == null)
                    throw new ArgumentException(
                        "Initial routes cannot contain null.",
                        nameof(initializedRoutes));

                if (!currentConnections.TryGetValue(
                        route.Connection.Id,
                        out var existing))
                {
                    continue;
                }

                if (existing.SourceNodeId != senderNodeId ||
                    existing.TargetNodeId != route.RecipientNodeId)
                {
                    throw new InvalidOperationException(
                        "Initial route endpoints do not match.");
                }

                if (seenIds.Add(existing.Id))
                    routes.Add(route);
            }

            // Only additional live Layer 2 connections
            // are appended, ordered by stable connection ID.
            foreach (var connection in outgoing
                         .OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                if (seenIds.Contains(connection.Id))
                    continue;

                string recipient = connection.TargetNodeId;

                // Knowing somebody socially does not imply
                // knowing their current postal address.
                if (!network.TryGetKnownAddress(
                        senderNodeId,
                        recipient,
                        out string knownAddressId))
                {
                    continue;
                }

                if (!network.TryGetAddress(
                        knownAddressId, out var address))
                {
                    throw new InvalidOperationException(
                        "Known postal address does not exist.");
                }

                if (string.IsNullOrWhiteSpace(address.CountryId))
                {
                    throw new InvalidOperationException(
                        "Known postal address has no country.");
                }

                routes.Add(new CopyDistributionRoute(
                    recipient,
                    connection,
                    address.CountryId));

                seenIds.Add(connection.Id);
            }

            return routes.AsReadOnly();
        }
    }
}
