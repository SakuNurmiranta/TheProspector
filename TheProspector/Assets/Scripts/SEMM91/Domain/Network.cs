
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class Network
    {
        private readonly Dictionary<string, NetworkConnection>
            _connections = new Dictionary<string, NetworkConnection>();

        private readonly Dictionary<string, Node> _nodes =
            new Dictionary<string, Node>();

        private readonly Dictionary<string, AddressSlot> _addresses =
            new Dictionary<string, AddressSlot>();

        
        private readonly Dictionary<string, Dictionary<string, string>>
            _knownAddresses =
                new Dictionary<string, Dictionary<string, string>>();

        
        private readonly Dictionary<string, string> _nodeLocations =
            new Dictionary<string, string>();

        public int NodeCount => _nodes.Count;

        public int AddressCount => _addresses.Count;

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
            AddAddress(id, null);
        }

        public void AddAddress(string id, string countryId)
        {
            if (_addresses.ContainsKey(id))
                throw new ArgumentException(
                    $"Address '{id}' already exists.");

            _addresses.Add(id, new AddressSlot(id, countryId));
        }


        
        public void AddConnection(
            string id,
            string sourceNodeId,
            string targetNodeId,
            CommunicationLayer layer,
            string role)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Connection ID cannot be empty.", nameof(id));

            if (!_nodes.ContainsKey(sourceNodeId))
                throw new ArgumentException(
                    $"Source node '{sourceNodeId}' does not exist.");

            if (!_nodes.ContainsKey(targetNodeId))
                throw new ArgumentException(
                    $"Target node '{targetNodeId}' does not exist.");

            if (_connections.ContainsKey(id))
                throw new ArgumentException(
                    $"Connection '{id}' already exists.");

            if (layer != CommunicationLayer.Interpersonal &&
                layer != CommunicationLayer.Insert)
                throw new ArgumentException(
                    "Invalid communication layer.", nameof(layer));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException(
                    "Connection role cannot be empty.", nameof(role));

            var connection = new NetworkConnection(
                id, sourceNodeId, targetNodeId, layer, role);

            _connections.Add(id, connection);
        }

        public IReadOnlyList<NetworkConnection> GetOutgoingConnections(
            string nodeId,
            CommunicationLayer layer)
        {
            if (!_nodes.ContainsKey(nodeId))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            var result = new List<NetworkConnection>();

            foreach (var connection in _connections.Values)
            {
                if (connection.SourceNodeId == nodeId &&
                    connection.Layer == layer)
                {
                    result.Add(connection);
                }
            }

            return result.AsReadOnly();
        }

        
        public bool HasAddress(string id)
        {
            return _addresses.ContainsKey(id);
        }
        
        
       
        public void LearnAddress(
            string knowerNodeId,
            string targetNodeId,
            string addressId)
        {
            if (!_nodes.ContainsKey(knowerNodeId))
                throw new ArgumentException(
                    $"Node '{knowerNodeId}' does not exist.");

            if (!_nodes.ContainsKey(targetNodeId))
                throw new ArgumentException(
                    $"Node '{targetNodeId}' does not exist.");

            if (!_addresses.ContainsKey(addressId))
                throw new ArgumentException(
                    $"Address '{addressId}' does not exist.");

            if (!_knownAddresses.TryGetValue(
                    knowerNodeId, out var contacts))
            {
                contacts = new Dictionary<string, string>();
                _knownAddresses.Add(knowerNodeId, contacts);
            }

            contacts[targetNodeId] = addressId;
        }


        public void MoveNode(string nodeId, string destinationAddressId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            if (!_nodeLocations.TryGetValue(nodeId, out var sourceAddressId))
                throw new InvalidOperationException(
                    $"Node '{nodeId}' has no current address.");

            if (!_addresses.TryGetValue(destinationAddressId, out var destination))
                throw new ArgumentException(
                    $"Address '{destinationAddressId}' does not exist.");

            if (destination.Occupant != null)
                throw new InvalidOperationException(
                    $"Address '{destinationAddressId}' is occupied.");

            var source = _addresses[sourceAddressId];

            destination.Place(node);
            source.Vacate(node);

            _nodeLocations[nodeId] = destinationAddressId;
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
        
        
        public PostalDistance GetPostalDistance(
            string sourceAddressId,
            string destinationAddressId)
        {
            if (!_addresses.TryGetValue(sourceAddressId, out var source))
                throw new ArgumentException(
                    $"Address '{sourceAddressId}' does not exist.");

            if (!_addresses.TryGetValue(destinationAddressId, out var destination))
                throw new ArgumentException(
                    $"Address '{destinationAddressId}' does not exist.");

            if (string.IsNullOrWhiteSpace(source.CountryId) ||
                string.IsNullOrWhiteSpace(destination.CountryId))
                throw new InvalidOperationException(
                    "Both addresses must have a country ID.");

            return string.Equals(
                source.CountryId,
                destination.CountryId,
                StringComparison.Ordinal)
                ? PostalDistance.Short
                : PostalDistance.Long;
        }
        
        public bool TryGetAddress(string id, out AddressSlot address)
        {
            return _addresses.TryGetValue(id, out address);
        }

        
        public bool TryGetKnownAddress(
            string knowerNodeId,
            string targetNodeId,
            out string addressId)
        {
            addressId = null;

            return _knownAddresses.TryGetValue(
                       knowerNodeId, out var contacts)
                   && contacts.TryGetValue(
                       targetNodeId, out addressId);
        }
        
        public bool TryGetNodeAddress(
            string nodeId,
            out string addressId)
        {
            return _nodeLocations.TryGetValue(nodeId, out addressId);
        }


        public bool TryGetOccupant(string addressId, out Node occupant)
        {
            occupant = null;

            if (!_addresses.TryGetValue(addressId, out var address))
                return false;

            occupant = address.Occupant;
            return occupant != null;
        }

        

    }
}
