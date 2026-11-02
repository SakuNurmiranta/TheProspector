
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class Network
    {
        private readonly Dictionary<string, Node> _nodes =
            new Dictionary<string, Node>();

        private readonly Dictionary<string, AddressSlot> _addresses =
            new Dictionary<string, AddressSlot>();

        private readonly Dictionary<string, string> _nodeLocations =
            new Dictionary<string, string>();

        public int NodeCount => _nodes.Count;

        public void AddNode(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (_nodes.ContainsKey(node.Id))
                throw new ArgumentException(
                    $"Node '{node.Id}' already exists.");

            _nodes.Add(node.Id, node);
        }

        public bool TryGetNode(string id, out Node node)
        {
            return _nodes.TryGetValue(id, out node);
        }

        public void AddAddress(string id)
        {
            if (_addresses.ContainsKey(id))
                throw new ArgumentException(
                    $"Address '{id}' already exists.");

            _addresses.Add(id, new AddressSlot(id));
        }

        public bool HasAddress(string id)
        {
            return _addresses.ContainsKey(id);
        }

        public bool TryGetOccupant(string addressId, out Node occupant)
        {
            occupant = null;

            if (!_addresses.TryGetValue(addressId, out var address))
                return false;

            occupant = address.Occupant;
            return occupant != null;
        }

        public void PlaceNode(string nodeId, string addressId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            if (!_addresses.TryGetValue(addressId, out var address))
                throw new ArgumentException(
                    $"Address '{addressId}' does not exist.");

            if (_nodeLocations.ContainsKey(nodeId))
                throw new InvalidOperationException(
                    $"Node '{nodeId}' already occupies an address.");

            address.Place(node);
            _nodeLocations.Add(nodeId, addressId);
        }
    }
}
