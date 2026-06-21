using SEMM91.Core.Entities;

namespace SEMM91.Core.Aspects
{
    public class AspectUsabilityEvaluator
    {
        private readonly GlobalAspectRegistry globalRegistry;

        public AspectUsabilityEvaluator(GlobalAspectRegistry globalRegistry)
        {
            this.globalRegistry = globalRegistry;
        }

        public bool CanUseAspect(GameEntity entity, string aspectId)
        {
            if (entity == null) return false;
            
            if (!globalRegistry.TryGetAspect(aspectId, out Aspect aspect)) return false;

            foreach (string requiredAspectId in aspect.RequiredAspectIds)
            {
                if (!EntityHasAspect(entity, requiredAspectId)) return false;
            }
            
            return true;
        }

        private bool EntityHasAspect(GameEntity entity, string aspectId)
        {
            foreach (string ownedAspectId in entity.AspectIds)
            {
                if (ownedAspectId == aspectId) return true;
            }

            return false;
        }
    }
}