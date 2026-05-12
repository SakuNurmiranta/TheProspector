using System;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.Core.Ideas
{
    [Serializable]
    public class Idea
    {
        [SerializeField] private string ideaId;
        [SerializeField] private string aspectId;
        [SerializeField] private string sourceEntityId;
        [SerializeField] private TagContainerType sourceContainerType; //This lets me know the way the original tag was created
        [SerializeField] private IdeaPayloadType payloadType;
        [SerializeField] private TagInstance tagInstance;
        [SerializeField] private TagPair tagPair;
        [SerializeField] private float conveyance;
        
        public string IdeaId => ideaId;
        public string AspectId => aspectId;
        public string SourceEntityId => sourceEntityId;
        public TagContainerType SourceContainerType => sourceContainerType;
        public IdeaPayloadType PayloadType => payloadType;
        public TagInstance TagInstance => tagInstance;
        public TagPair TagPair => tagPair;
        public float Conveyance => conveyance;

        // Single tag idea constructor
        public Idea(
            string ideaId,
            string aspectId,
            TagInstance tagInstance,
            float conveyance,
            string sourceEntityId = "",
            TagContainerType sourceContainerType = TagContainerType.Transient
            )
        {
            this.ideaId = ideaId;
            this.aspectId = aspectId;
            this.payloadType = IdeaPayloadType.SingleTag;
            this.tagInstance = tagInstance;
            this.tagPair = null;
            this.conveyance = Mathf.Clamp01(conveyance);
            this.sourceEntityId = sourceEntityId;
            this.sourceContainerType = sourceContainerType;
        }

        // Tag pair idea constructor
        public Idea(
            string ideaId,
            string aspectId,
            TagPair tagPair,
            float conveyance,
            string sourceEntityId = "",
            TagContainerType sourceContainerType = TagContainerType.Transient
        )
        {
            this.ideaId = ideaId;
            this.aspectId = aspectId;
            this.payloadType = IdeaPayloadType.TagPair;
            this.tagInstance = default;
            this.tagPair = tagPair;
            this.conveyance = Mathf.Clamp01(conveyance);
            this.sourceEntityId = sourceEntityId;
            this.sourceContainerType = sourceContainerType;
        }

        public override string ToString()
        {
            string payloadText = payloadType switch
            {
                IdeaPayloadType.SingleTag => $"tag={tagInstance}",
                IdeaPayloadType.TagPair => $"tagPair={tagPair}",
                _ => "unknown"
            };
            
            return $"{ideaId}: aspect={aspectId}, {payloadText}, conveyance={conveyance:0.00}, " +
                   $"sourceEntity={sourceEntityId}, sourceContainer={sourceContainerType}";
        }

        public bool IsTRVEEligible()
        {
            if (payloadType != IdeaPayloadType.TagPair)
            {
                return false;
            }

            if (tagPair == null)
            {
                return false;
            }

            return tagPair.HasMinimumTRVEDegrees();
        }
    }
}