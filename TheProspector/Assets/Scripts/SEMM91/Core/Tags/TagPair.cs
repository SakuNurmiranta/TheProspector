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
        
        public static bool TryCreate(
            TagInstance dominantTag,
            TagInstance submissiveTag,
            out TagPair tagPair
        )
        {
            tagPair = null;

            if (!dominantTag.IsOpposedTo(submissiveTag))
            {
                Debug.LogWarning(
                    $"Cannot create TagPair: tags are not valid opposites. Dominant={dominantTag}, Submissive={submissiveTag}"
                );
                return false;
            }

            tagPair = new TagPair(dominantTag, submissiveTag);
            return true;
        }
    }
}