using SEMM91.Core.Collectives;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.World;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapResult
    {
        public SeededWorldState WorldState { get; }

        public CollectiveRegistry Registry { get; }
        public Collective Kvlt { get; }
        public Collective Society { get; }

        public GameEntity KvltEntity { get; }
        public GameEntity TheHolePremises { get; }
        public GameEntity TheHole { get; }
        public EntityHostingRecord TheHoleHosting { get; }

        public StartingCollectiveBootstrapResult(
            SeededWorldState worldState,
            CollectiveRegistry registry,
            Collective kvlt,
            Collective society,
            GameEntity kvltEntity,
            GameEntity theHolePremises,
            GameEntity theHole,
            EntityHostingRecord theHoleHosting
        )
        {
            WorldState = worldState;
            Registry = registry;
            Kvlt = kvlt;
            Society = society;
            KvltEntity = kvltEntity;
            TheHolePremises = theHolePremises;
            TheHole = theHole;
            TheHoleHosting = theHoleHosting;
        }
    }
}