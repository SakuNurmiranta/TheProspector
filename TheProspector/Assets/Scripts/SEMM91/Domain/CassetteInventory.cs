using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class CassetteInventory
    {
        private readonly Network _network;

        private readonly Dictionary<string, CassetteCopy> _copies = new();

        public CassetteInventory(Network network)
        {
            _network = network ??
                       throw new ArgumentNullException(nameof(network));
        }
        
        
        internal void BeginShipment(
            string copyId,
            string senderNodeId,
            string shipmentId)
        {
            if (!_copies.TryGetValue(copyId, out var copy))
                throw new ArgumentException(
                    $"Cassette '{copyId}' does not exist.");

            if (copy.HolderNodeId != senderNodeId)
                throw new InvalidOperationException(
                    "Sender does not hold this cassette.");

            copy.BeginTransit(shipmentId);
        }

        internal void CompleteShipment(
            string copyId,
            string shipmentId,
            string recipientNodeId)
        {
            if (!_copies.TryGetValue(copyId, out var copy))
                throw new ArgumentException(
                    $"Cassette '{copyId}' does not exist.");

            ValidateHolder(recipientNodeId);

            copy.EndTransit(shipmentId, recipientNodeId);
        }


        public CassetteCopy RegisterExistingCopy(
            string copyId,
            CulturalProduct product,
            string holderNodeId)
        {
            if (string.IsNullOrWhiteSpace(copyId))
                throw new ArgumentException(
                    "Copy ID cannot be empty.", nameof(copyId));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (_copies.ContainsKey(copyId))
                throw new ArgumentException(
                    $"Cassette '{copyId}' already exists.");

            ValidateHolder(holderNodeId);

            var copy = new CassetteCopy(
                copyId, product.Id, holderNodeId);

            _copies.Add(copyId, copy);

            return copy;
        }

        public bool TryGetCopy(
            string copyId,
            out CassetteCopy copy)
        {
            return _copies.TryGetValue(copyId, out copy);
        }

        public void RecordTransfer(
            string copyId,
            string fromNodeId,
            string toNodeId)
        {
            if (!_copies.TryGetValue(copyId, out var copy))
                throw new ArgumentException(
                    $"Cassette '{copyId}' does not exist.");

            if (copy.HolderNodeId != fromNodeId)
                throw new InvalidOperationException(
                    "Sender does not hold this cassette.");

            if (fromNodeId == toNodeId)
                throw new ArgumentException(
                    "Cannot transfer a cassette to its holder.");

            ValidateHolder(toNodeId);

            copy.MoveTo(toNodeId);
        }

        private void ValidateHolder(string nodeId)
        {
            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            if (!_network.TryGetNodeAddress(nodeId, out _))
                throw new InvalidOperationException(
                    $"Node '{nodeId}' has no address.");
        }


        public CassetteCopy RecordCopy(
            string sourceCopyId,
            string newCopyId,
            string fromNodeId,
            string toNodeId)
        {
            if (!_copies.TryGetValue(sourceCopyId, out var source))
                throw new ArgumentException(
                    $"Source cassette '{sourceCopyId}' does not exist.");

            if (string.IsNullOrWhiteSpace(newCopyId))
                throw new ArgumentException(
                    "New copy ID cannot be empty.", nameof(newCopyId));

            if (_copies.ContainsKey(newCopyId))
                throw new ArgumentException(
                    $"Cassette '{newCopyId}' already exists.");

            if (source.HolderNodeId != fromNodeId)
                throw new InvalidOperationException(
                    "Sender does not hold the source cassette.");

            ValidateHolder(toNodeId);

            var copy = new CassetteCopy(
                newCopyId,
                source.ProductId,
                toNodeId);

            _copies.Add(newCopyId, copy);

            return copy;
        }
    }
}