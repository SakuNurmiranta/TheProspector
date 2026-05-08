using SEMM91.Core.Aspects;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Entities;
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

            if (!aspectUsabilityEvaluator.CanUseAspect(entity, aspectId))
            {
                Debug.LogError("Entity cannot use the aspect!");
                return false;
            }

            if (!sourceContainer.TryExpendHeldTag(out HeldTag expendedTag))
            {
                Debug.LogError("Entity cannot expend the held tag!");
                return false;
            }
            
            string ideaId = System.Guid.NewGuid().ToString();
            
            idea = new Idea(
                ideaId,
                aspectId,
                expendedTag.TagInstance,
                conveyance
            );
            
            Debug.Log($"Created idea: {idea}");
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
            
            dominantSourceContainer.TryExpendHeldTag(out _);
            submissiveSourceContainer.TryExpendHeldTag(out _);
            
            string ideaId = System.Guid.NewGuid().ToString();
            
            idea = new Idea(
                ideaId,
                aspectId,
                tagPair,
                conveyance
            );
            
            Debug.Log($"Created tag-pair idea: {idea}");
            return true;
        }
    }
}