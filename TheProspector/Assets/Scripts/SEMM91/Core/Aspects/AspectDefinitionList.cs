using System;
using System.Collections.Generic;

namespace SEMM91.Core.Aspects
{
    [Serializable]
    public class AspectDefinitionList
    {
        public List<AspectDefinition> aspects = new();
    }
    
    [Serializable]
    public class AspectDefinition
    {
        public string aspectId;
        public string displayName;
        public string description;
        public AspectType aspectType;
        public string parentAspectId;
        public List<string> requiredAspectIds = new();
    }
}