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
        
        [SerializeField] private TagInstance tagInstance;
        [SerializeField] private bool hasTagInstance;

        
        public TagContainerType ContainerType => containerType;
        public List<string> Tags => tags;
        
        public bool HasTagInstance => hasTagInstance;
        public TagInstance TagInstance => tagInstance; 
        
        public TagContainer(TagContainerType type)
        {
            containerType = type;
        }
        
        public void SetTag(TagInstance newTag)
        {
            tagInstance = newTag;
            hasTagInstance = true;

            Debug.Log($"Set {containerType} tag to {tagInstance}");
        }

        public void ClearTag()
        {
            tagInstance = default;
            hasTagInstance = false;

            Debug.Log($"Cleared {containerType} tag");
        }
    }
}