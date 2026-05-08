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
        [SerializeField] private TagInstance tagInstance;
        [SerializeField] private float conveyance;
        
        public string IdeaId => ideaId;
        public string AspectId => aspectId;
        public TagInstance TagInstance => tagInstance;
        public float Conveyance => conveyance;

        public Idea(
            string ideaId,
            string aspectId,
            TagInstance tagInstance,
            float conveyance)
        {
            this.ideaId = ideaId;
            this.aspectId = aspectId;
            this.tagInstance = tagInstance;
            this.conveyance = Mathf.Clamp01(conveyance);
        }

        public override string ToString()
        {
            return $"{ideaId} | {aspectId} | {tagInstance} | {conveyance}";
        }
    }
}