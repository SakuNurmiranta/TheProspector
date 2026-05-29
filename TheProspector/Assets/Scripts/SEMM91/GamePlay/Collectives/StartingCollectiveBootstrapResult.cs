using SEMM91.Core.Collectives;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapResult
    {
        public CollectiveRegistry Registry { get; }
        public Collective PlayerBand { get; }
        public Collective Kvlt { get; }
        public Collective Society { get; }

        public StartingCollectiveBootstrapResult(
            CollectiveRegistry registry,
            Collective playerBand,
            Collective kvlt,
            Collective society
        )
        {
            Registry = registry;
            PlayerBand = playerBand;
            Kvlt = kvlt;
            Society = society;
        }
    }
}