using System;

namespace SEMM91.Domain
{
    public sealed class Node
    {
        public string Id { get; }

        public Node(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node ID cannot be empty.", nameof(id));
            
            Id = id;
        }
        
        
    }
}