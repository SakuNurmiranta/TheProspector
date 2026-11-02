using System;

namespace SEMM91.Domain
{
    public sealed class AddressSlot
    {
        public string Id { get; }
        public Node Occupant { get; private set; }

        public AddressSlot(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Address ID cannot be empty.", nameof(id));

            Id = id;
        }

        internal void Place(Node node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (Occupant != null)
                throw new InvalidOperationException(
                    $"Address '{Id}' is already occupied.");

            Occupant = node;
        }
    }
}