using System;

namespace SEMM91.Domain
{
    public sealed class Node
    {
        public string Id { get; }

        
        public bool? Preference { get; private set; }

        public void SetPreference(bool preference)
        {
            Preference = preference;
        }

        public bool Supports(CulturalProduct product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (!Preference.HasValue ||
                !product.Preference.HasValue)
                throw new InvalidOperationException(
                    "Both preferences must be initialized.");

            return Preference.Value == product.Preference.Value;
        }

        public Node(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node ID cannot be empty.", nameof(id));
            
            Id = id;
        }
        
        
    }
}