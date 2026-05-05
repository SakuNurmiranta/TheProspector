using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Core.Aspects
{
    [Serializable]
    public class Aspect
    {
        [SerializeField] private string aspectId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private AspectType aspectType;
        [SerializeField] private string parentAspectId;
        [SerializeField] private List<string> requiredAspectIds = new();
        
        public string AspectId => aspectId;
        public string DisplayName => displayName;
        public string Description => description;
        public string ParentAspectId => parentAspectId;
        public bool HasParent => !string.IsNullOrWhiteSpace(parentAspectId);
        public AspectType AspectType => aspectType;
        public IReadOnlyList<string> RequiredAspectIds => requiredAspectIds;

        public Aspect(
            string aspectId, 
            string displayName, 
            string description, 
            AspectType aspectType,
            string parentAspectId = ""
            )
        {
            this.aspectId = aspectId;
            this.displayName = displayName;
            this.description = description;
            this.aspectType = aspectType;
            this.parentAspectId = parentAspectId;
        }

        public Aspect(
            string aspectId,
            string displayName,
            string description,
            AspectType aspectType,
            IEnumerable<string> requiredAspectIds,
            string parentAspectId = ""
        )
        {
            this.aspectId = aspectId;
            this.displayName = displayName;
            this.description = description;
            this.aspectType = aspectType;
            this.requiredAspectIds = new List<string>(requiredAspectIds);
            this.parentAspectId = parentAspectId;
        }

        public static Aspect FromDefinition(AspectDefinition definition)
        {
            return new Aspect(
                definition.aspectId,
                definition.displayName,
                definition.description,
                definition.aspectType,
                definition.requiredAspectIds,
                definition.parentAspectId
            );
        }
        
        public override string ToString()
        {
            string parentText = HasParent ? $" parent={parentAspectId}" : " parent=none";
            return $"{displayName} ({aspectType}) [{aspectId}]{parentText} requires={requiredAspectIds.Count}";
        }
    }
}