namespace SEMM91.Domain
{
    public sealed class CulturalProduct
    {
        public string Id { get; }
        public string ArtistId { get; }

        internal CulturalProduct(string id, string artistId)
        {
            Id = id;
            ArtistId = artistId;
        }
    }
}