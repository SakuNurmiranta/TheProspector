using SEMM91.Core.Collectives;
using SEMM91.GamePlay.Entities;

namespace SEMM91.GamePlay.Collectives
{
    public class StartingCollectiveBootstrapResult
    {
        public CollectiveRegistry Registry { get; }
        public Collective PlayerBand { get; }
        public Collective Kvlt { get; }
        public Collective Society { get; }

        public GameEntity TheHolePremises { get; }
        public GameEntity TheHole { get; }
        
        public StartingCollectiveBootstrapResult(
            CollectiveRegistry registry,
            Collective playerBand,
            Collective kvlt,
            Collective society,
            GameEntity theHolePremises,
            GameEntity theHole
        )
        {
            Registry = registry;
            PlayerBand = playerBand;
            Kvlt = kvlt;
            Society = society;
            TheHolePremises = theHolePremises;
            TheHole = theHole;
        }
    }
}