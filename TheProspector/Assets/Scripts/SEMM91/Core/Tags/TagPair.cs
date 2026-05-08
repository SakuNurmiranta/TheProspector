using System;
using UnityEngine;

namespace SEMM91.Core.Tags
{
    [Serializable]
    public class TagPair
    {
        [SerializeField] private TagInstance dominantTag;
        [SerializeField] private TagInstance submissiveTag;
        
        public TagInstance DominantTag => dominantTag;
        public TagInstance SubmissiveTag => submissiveTag;

        public TagPair(TagInstance dominantTag, TagInstance submissiveTag)
        {
            this.dominantTag = dominantTag;
            this.submissiveTag = submissiveTag;
        }

        public bool IsValidOpposition()
        {
            return dominantTag.IsOpposedTo(submissiveTag);
        }

        public bool HasMinimumTRVEDegrees()
        {
            return dominantTag.HasTRVEMinimumDegree() && submissiveTag.HasTRVEMinimumDegree();
        }

        public override string ToString()
        {
            return $"Dominant={dominantTag} | Submissive={submissiveTag}";
        }
    }
}