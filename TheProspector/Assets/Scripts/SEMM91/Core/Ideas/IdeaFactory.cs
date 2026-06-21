using SEMM91.Core.Aspects;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.Core.Ideas
{
    public class IdeaFactory
    {
        private readonly AspectUsabilityEvaluator aspectUsabilityEvaluator;

        public IdeaFactory(AspectUsabilityEvaluator aspectUsabilityEvaluator)
        {
            this.aspectUsabilityEvaluator = aspectUsabilityEvaluator;
        }

        public bool TryCreateIdeaFromHeldTag(
            GameEntity entity,
            string aspectId,
            TagContainer sourceContainer,
            float conveyance,
            out Idea idea
        )
        {
            idea = null;
            
            if (entity == null)
            {
                Debug.LogError("Entity is null!");
                return false;
            }

            if (sourceContainer == null)
            {
                Debug.LogError("Source container is null!");
                return false;
            }

            if (!EntityOwnsTagContainer(entity, sourceContainer))
            {
                Debug.LogError("Entity does not own the source container!");
                return false;
            }
            
            if (!aspectUsabilityEvaluator.CanUseAspect(entity, aspectId))
            {
                Debug.LogError("Entity cannot use the aspect!");
                return false;
            }

            /*if (!sourceContainer.TryExpendHeldTag(out HeldTag expendedTag))
            {
                Debug.LogError("Entity cannot expend the held tag!");
                return false;
            }*/

            if (!sourceContainer.HasHeldTag)
            {
                Debug.LogError("Entity does not have a held tag!");
                return false;
            }
            
            HeldTag sourceHeldTag = sourceContainer.HeldTag;

            if (sourceContainer.ShouldConsumeOnIdeaUse())
            {
                if (!sourceContainer.TryExpendHeldTag(out sourceHeldTag))
                {
                    Debug.LogError("Entity cannot expend the held tag!");
                    return false;
                }
            }
            
            string ideaId = System.Guid.NewGuid().ToString();
            
            idea = new Idea(
                ideaId,
                aspectId,
                sourceHeldTag.TagInstance,
                conveyance,
                entity.EntityId,
                sourceContainer.ContainerType
            );
            
            //Debug.Log($"Created idea: {idea}");
            return true;
        }

        public bool TryCreateTagPairIdeaFromHeldTags(
            GameEntity entity,
            string aspectId,
            TagContainer dominantSourceContainer,
            TagContainer submissiveSourceContainer,
            float conveyance,
            out Idea idea)
        {
            idea = null;

            if (entity == null)
            {
                Debug.LogWarning("Cannot create tag-pair idea: Entity is null!");
                return false;
            }

            if (dominantSourceContainer == null || submissiveSourceContainer == null)
            {
                Debug.LogWarning("Cannot create tag-pair idea: One or both source-containers are null!");
                return false;
            }

            if (!EntityOwnsTagContainer(entity, dominantSourceContainer))
            {
                Debug.LogWarning("Cannot create tag-pair idea: Entity does not own the dominant source-container!");
                return false;
            }

            if (!EntityOwnsTagContainer(entity, submissiveSourceContainer))
            {
                Debug.LogWarning("Cannot create tag-pair idea: Entity does not own the submissive source-container!");
                return false;
            }

            if (!aspectUsabilityEvaluator.CanUseAspect(entity, aspectId))
            {
                Debug.LogWarning("Cannot create tag-pair idea: Entity cannot use the aspect!");
                return false;
            }

            if (!dominantSourceContainer.HasHeldTag || !submissiveSourceContainer.HasHeldTag)
            {
                Debug.LogWarning("Cannot create tag-pair idea: Entity does not have a held tag!");
                return false;
            }
            
            HeldTag dominantHeldTag = dominantSourceContainer.HeldTag;
            HeldTag submissiveHeldTag = submissiveSourceContainer.HeldTag;

            if (!TagPair.TryCreate(
                    dominantHeldTag.TagInstance,
                    submissiveHeldTag.TagInstance,
                    out TagPair tagPair))
            {
                Debug.LogWarning("Cannot create tag-pair idea: Tags are not valid opposites!");
                return false;
            }
            
            /*dominantSourceContainer.TryExpendHeldTag(out _);
            submissiveSourceContainer.TryExpendHeldTag(out _);*/

            if (dominantSourceContainer.ShouldConsumeOnIdeaUse())
            {
                dominantSourceContainer.TryExpendHeldTag(out _);
            }

            if (submissiveSourceContainer.ShouldConsumeOnIdeaUse())
            {
                submissiveSourceContainer.TryExpendHeldTag(out _); 
            }
            
            string ideaId = System.Guid.NewGuid().ToString();
            
            idea = new Idea(
                ideaId,
                aspectId,
                tagPair,
                conveyance,
                entity.EntityId,
                dominantSourceContainer.ContainerType
            );
            
            Debug.Log($"Created tag-pair idea: {idea}");
            return true;
        }

        private bool EntityOwnsTagContainer(GameEntity entity, TagContainer candidateContainer)
        {
            if (entity == null || candidateContainer == null)
            {
                return false;
            }

            foreach (TagContainer entityContainer in entity.TagContainers)
            {
                if (ReferenceEquals(entityContainer, candidateContainer))
                {
                    return true;
                }
            }

            return false;
        }
    }
}