
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SEMM91.Domain
{
    public sealed class CulturalCapitalLedger
    {
        private readonly Network _network;

        private readonly Dictionary<string, int>
            _capitalByNode =
                new Dictionary<string, int>();

        public CulturalCapitalLedger(Network network)
        {
            _network = network ??
                throw new ArgumentNullException(
                    nameof(network));
        }

        public void InitializeNode(
            string nodeId,
            int startingCapital)
        {
            ValidateNetworkNode(nodeId);

            if (startingCapital < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(startingCapital),
                    "Capital cannot be negative.");

            if (_capitalByNode.ContainsKey(nodeId))
                throw new InvalidOperationException(
                    "Node capital is already initialized.");

            _capitalByNode.Add(
                nodeId, startingCapital);
        }

        public int GetCapital(string nodeId)
        {
            ValidateNetworkNode(nodeId);

            if (!_capitalByNode.TryGetValue(
                    nodeId, out var capital))
                throw new InvalidOperationException(
                    "Node capital has not been initialized.");

            return capital;
        }

        public void Credit(
            string nodeId,
            int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Credit must be positive.");

            int current = GetCapital(nodeId);

            // Lasketaan uusi arvo ennen tilan muutosta.
            // Ylivuoto ei saa muuttaa rekisteriä.
            int updated = checked(current + amount);

            _capitalByNode[nodeId] = updated;
        }

        public IReadOnlyDictionary<string, int> Snapshot()
        {
            // Palautetaan itsenäinen, vain luettava kopio.
            // Kentän laskenta ei saa muuttaa pääomia.
            var copy = new Dictionary<string, int>(
                _capitalByNode);

            return new ReadOnlyDictionary<string, int>(
                copy);
        }

        private void ValidateNetworkNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                throw new ArgumentException(
                    "Node ID cannot be empty.",
                    nameof(nodeId));

            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' is not in the network.",
                    nameof(nodeId));
        }
    }
}
