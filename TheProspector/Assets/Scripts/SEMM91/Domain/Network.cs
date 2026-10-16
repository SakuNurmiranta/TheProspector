using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class Network
    {
        private readonly Dictionary<string, Node> _nodes =
            new Dictionary<string, Node>();

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
    }
}