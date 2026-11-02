using System;

namespace SEMM91.Domain
{
    public sealed class AddressSlot
    {
        public string Id { get; }
        
        public string CountryId { get; }
        public Node Occupant { get; private set; }


        public AddressSlot(string id) : this(id, null)
        {
        }

        public AddressSlot(string id, string countryId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Address ID cannot be empty.", nameof(id));

            if (countryId != null && string.IsNullOrWhiteSpace(countryId))
                throw new ArgumentException(
                    "Country ID cannot be empty.", nameof(countryId));

            Id = id;
            CountryId = countryId;
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
        
        internal void Vacate(Node node)
        {
            if (Occupant != node)
                throw new InvalidOperationException(
                    $"Node does not occupy address '{Id}'.");

            Occupant = null;
        }
    }
}