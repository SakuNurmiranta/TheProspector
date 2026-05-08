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
        [SerializeField] private IdeaPayloadType payloadType;
        [SerializeField] private TagInstance tagInstance;
        [SerializeField] private TagPair tagPair;
        [SerializeField] private float conveyance;
        
        public string IdeaId => ideaId;
        public string AspectId => aspectId;
        public IdeaPayloadType PayloadType => payloadType;
        public TagInstance TagInstance => tagInstance;
        public TagPair TagPair => tagPair;
        public float Conveyance => conveyance;

        public Idea(
            string ideaId,
            string aspectId,
            TagInstance tagInstance,
            float conveyance)
        {
            this.ideaId = ideaId;
            this.aspectId = aspectId;
            this.payloadType = IdeaPayloadType.SingleTag;
            this.tagInstance = tagInstance;
            this.tagPair = null;
            this.conveyance = Mathf.Clamp01(conveyance);
        }

        public Idea(
            string ideaId,
            string aspectId,
            TagPair tagPair,
            float conveyance
        )
        {
            this.ideaId = ideaId;
            this.aspectId = aspectId;
            this.payloadType = IdeaPayloadType.TagPair;
            this.tagInstance = default;
            this.tagPair = tagPair;
            this.conveyance = Mathf.Clamp01(conveyance);
        }

        public override string ToString()
        {
            string payloadText = payloadType switch
            {
                IdeaPayloadType.SingleTag => $"tag={tagInstance}",
                IdeaPayloadType.TagPair => $"tagPair={tagPair}",
                _ => "unknown"
            };
            
            return $"{ideaId}: aspect={aspectId}, {payloadText} | conveyance={conveyance:0.00}";
        }
    }
}