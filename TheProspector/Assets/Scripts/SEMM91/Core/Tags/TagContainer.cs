using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Tags
{
    [System.Serializable]
    public class TagContainer
    {
        [SerializeField] private TagContainerType containerType;
        
        //placeholder?
        [SerializeField] private List<string> tags = new List<string>();
        
        public TagContainerType ContainerType => containerType;
        public List<string> Tags => tags;
        
        public TagContainer(TagContainerType type)
        {
            containerType = type;
        }
    }
}