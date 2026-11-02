using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SEMM91.Domain
{
    public sealed class Field
    {
        private readonly Network _network;

        
        private readonly Dictionary<string, Artist> _artists =
            new Dictionary<string, Artist>();

        private readonly Dictionary<string, CulturalProduct> _demos =
            new Dictionary<string, CulturalProduct>();

        
        private readonly List<string> _localNodeIds =
            new List<string>();

        public string Id { get; }

        public int LocalNodeCount => _localNodeIds.Count;

        public Field(string id, Network network)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Field ID cannot be empty.", nameof(id));

            _network = network ??
                       throw new ArgumentNullException(nameof(network));

            Id = id;
        }

        public void RegisterLocalNode(string nodeId)
        {
            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            if (!_network.TryGetNodeAddress(nodeId, out _))
                throw new InvalidOperationException(
                    $"Node '{nodeId}' has no address.");

            if (_localNodeIds.Contains(nodeId))
                throw new ArgumentException(
                    $"Node '{nodeId}' is already registered.");

            _localNodeIds.Add(nodeId);
        }

        
        

        public void BlockArtist(string artistId, string nodeId)
        {
            if (!_artists.TryGetValue(artistId, out var artist))
                throw new ArgumentException(
                    $"Artist '{artistId}' does not exist.");

            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            artist.BlockBy(nodeId);
        }

        
        public bool CanOfferDemoDirectly(
            string artistId,
            string nodeId)
        {
            if (!_artists.TryGetValue(artistId, out var artist))
                throw new ArgumentException(
                    $"Artist '{artistId}' does not exist.");

            if (!_network.TryGetNode(nodeId, out _))
                throw new ArgumentException(
                    $"Node '{nodeId}' does not exist.");

            return !artist.IsBlockedBy(nodeId);
        }

        

        public bool TryGetUnblockedPrimaryContact(
            string artistId,
            out string contactNodeId)
        {
            if (!_artists.TryGetValue(artistId, out var artist))
                throw new ArgumentException(
                    $"Artist '{artistId}' does not exist.");

            string primaryId = artist.PrimaryContactNodeId;

            if (artist.IsBlockedBy(primaryId))
            {
                contactNodeId = null;
                return false;
            }

            contactNodeId = primaryId;
            return true;
        }

        public bool ContainsLocalNode(string nodeId)
        {
            return _localNodeIds.Contains(nodeId);
        }
        
        
        public IReadOnlyDictionary<string, double> CalculateShares(
            IReadOnlyDictionary<string, int> capitalByNode)
        {
            if (capitalByNode == null)
                throw new ArgumentNullException(
                    nameof(capitalByNode));

            if (_localNodeIds.Count == 0)
                throw new InvalidOperationException(
                    "Field contains no local nodes.");

            double total = 0;

            foreach (var nodeId in _localNodeIds)
            {
                if (!capitalByNode.TryGetValue(nodeId, out var capital))
                    throw new ArgumentException(
                        $"Missing capital for node '{nodeId}'.");

                if (capital < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(capitalByNode),
                        "Cultural capital cannot be negative.");

                total += capital;
            }

            if (total <= 0)
                throw new InvalidOperationException(
                    "Field shares are undefined when total capital is zero.");

            var shares = new Dictionary<string, double>();

            foreach (var nodeId in _localNodeIds)
            {
                shares.Add(
                    nodeId,
                    capitalByNode[nodeId] / total);
            }

            return new ReadOnlyDictionary<string, double>(shares);
        }
        
        
        public string SelectInitialRecipient(
            IReadOnlyDictionary<string, int> capitalByNode,
            Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var shares = CalculateShares(capitalByNode);

            double sample = random.NextDouble();

            if (double.IsNaN(sample) ||
                sample < 0.0 || sample >= 1.0)
                throw new ArgumentOutOfRangeException(
                    nameof(random),
                    "Random sample must be in [0, 1).");

            double cumulative = 0.0;
            string lastPositiveNodeId = null;

            foreach (var nodeId in _localNodeIds)
            {
                double share = shares[nodeId];

                if (share <= 0.0)
                    continue;

                lastPositiveNodeId = nodeId;
                cumulative += share;

                if (sample < cumulative)
                    return nodeId;
            }

            // Suoja liukulukulaskennan pyöristysvirheiltä.
            return lastPositiveNodeId;
        }
        
        
        public Artist SpawnArtist(
            string artistId,
            IReadOnlyDictionary<string, int> capitalByNode,
            Random random)
        {
            if (string.IsNullOrWhiteSpace(artistId))
                throw new ArgumentException(
                    "Artist ID cannot be empty.", nameof(artistId));

            if (_artists.ContainsKey(artistId))
                throw new ArgumentException(
                    $"Artist '{artistId}' already exists.");

            string recipientId =
                SelectInitialRecipient(capitalByNode, random);

            var artist = new Artist(
                artistId, Id, recipientId);

            _artists.Add(artistId, artist);

            return artist;
        }


        public CulturalProduct CreateDemo(
            string demoId,
            string artistId,
            bool? preference = null)

        {
            if (string.IsNullOrWhiteSpace(demoId))
                throw new ArgumentException(
                    "Demo ID cannot be empty.", nameof(demoId));

            if (string.IsNullOrWhiteSpace(artistId) ||
                !_artists.ContainsKey(artistId))
                throw new ArgumentException(
                    $"Artist '{artistId}' does not exist.");

            if (_demos.ContainsKey(demoId))
                throw new ArgumentException(
                    $"Demo '{demoId}' already exists.");

           
            var demo = new CulturalProduct(
                demoId, artistId, preference);


            _demos.Add(demoId, demo);

            return demo;
        }



    }
}