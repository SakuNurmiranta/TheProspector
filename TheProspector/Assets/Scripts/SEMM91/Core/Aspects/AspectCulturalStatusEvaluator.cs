namespace SEMM91.Core.Aspects
{
    public class AspectCulturalStatusEvaluator
    {
        private readonly AspectAcceptanceRegistry kvltRegistry;
        private readonly AspectAcceptanceRegistry societyRegistry;

        public AspectCulturalStatusEvaluator(
            AspectAcceptanceRegistry kvltRegistry,
            AspectAcceptanceRegistry societyRegistry)
        {
            this.kvltRegistry = kvltRegistry;
            this.societyRegistry = societyRegistry;
        }

        public AspectCulturalStatus GetStatus(string aspectId)
        {
            bool acceptedKvlt = kvltRegistry.IsAccepted(aspectId);
            bool acceptedSociety = societyRegistry.IsAccepted(aspectId);
            
            if (acceptedKvlt && acceptedSociety) return AspectCulturalStatus.Shared;
            if (acceptedKvlt) return AspectCulturalStatus.KvltOnly;
            if (acceptedSociety) return AspectCulturalStatus.SocietyOnly;
            
            return AspectCulturalStatus.Unclaimed;
        }

        public string GetStatusDescription(string aspectId)
        {
            AspectCulturalStatus status = GetStatus(aspectId);

            return status switch
            {
                AspectCulturalStatus.Unclaimed => $"{aspectId}: Unclaimed/Novel",
                AspectCulturalStatus.KvltOnly => $"{aspectId}: Kvlt Only",
                AspectCulturalStatus.SocietyOnly => $"{aspectId}: Society Only",
                AspectCulturalStatus.Shared => $"{aspectId}: Shared",
                _ => $"{aspectId}: Unknown"
            };
        }
            
    }
}