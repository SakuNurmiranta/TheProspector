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
    }
}