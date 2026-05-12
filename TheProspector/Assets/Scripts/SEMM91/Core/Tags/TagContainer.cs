using System.Collections.Generic;
using SEMM91.Core.Tags;
using UnityEngine;

namespace SEMM91.Core.Tags
{
    [System.Serializable]
    public class TagContainer
    {
        [SerializeField] private TagContainerType containerType;
        
        //placeholder?
        [SerializeField] private List<string> tags = new List<string>();
        
        [SerializeField] private HeldTag heldTag;
        [SerializeField] private bool hasHeldTag;

        public TagContainerType ContainerType => containerType;
        public List<string> Tags => tags;
        
        public bool HasHeldTag => hasHeldTag;
        public HeldTag HeldTag => heldTag; 
        
        public TagContainer(TagContainerType type)
        {
            containerType = type;
        }
        
        public void SetTag(TagInstance newTag)
        {
            heldTag = new HeldTag(newTag);
            hasHeldTag = true;

            Debug.Log($"Set {containerType} held tag to {heldTag}");
        }

        public void SetHeldTag(HeldTag newHeldTag)
        {
            heldTag = newHeldTag;
            hasHeldTag = true;

            Debug.Log($"Set {containerType} held tag to {heldTag}");
        }

        public void ClearHeldTag()
        {
            heldTag = null;
            hasHeldTag = false;

            Debug.Log($"Cleared {containerType} held tag");
        }
        
        public void ResolveTurnBoundaryLifecycle()
        {
            if (!hasHeldTag || heldTag == null)
            {
                return;
            }

            if (heldTag.ShouldEvaporateAtTurnBoundary())
            {
                Debug.Log($"Held tag evaporated from {containerType}: {heldTag}");
                ClearHeldTag();
            }
        }
        
        public bool TryExpendHeldTag(out HeldTag expendedTag)
        {
            expendedTag = null;

            if (!hasHeldTag || heldTag == null)
            {
                Debug.LogWarning($"Cannot expend held tag from {containerType}: no held tag present.");
                return false;
            }

            expendedTag = heldTag;

            Debug.Log($"Expended held tag from {containerType}: {heldTag}");

            ClearHeldTag();
            return true;
        }

        public bool ShouldConsumeOnIdeaUse()
        {
            return containerType == TagContainerType.Transient;
        }
    }
}