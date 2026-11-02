namespace SEMM91.Domain
{
    public sealed class CulturalProduct
    {
        public string Id { get; }
        public string ArtistId { get; }

        
        public bool? Preference { get; }

        

        internal CulturalProduct(
            string id,
            string artistId,
            bool? preference = null)
        {
            Id = id;
            ArtistId = artistId;
            Preference = preference;
        }

    }
}