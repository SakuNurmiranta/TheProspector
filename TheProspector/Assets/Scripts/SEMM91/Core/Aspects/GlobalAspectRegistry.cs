using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Core.Aspects
{
    public class GlobalAspectRegistry
    {
        private readonly Dictionary<string, Aspect> aspectsById = new();

        public IReadOnlyDictionary<string, Aspect> AspectsById => aspectsById;

        public bool RegisterAspect(Aspect aspect)
        {
            if (aspect == null)
            {
                Debug.LogWarning("Aspect is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(aspect.AspectId))
            {
                Debug.LogWarning("Aspect has no ID");
                return false;
            }

            if (aspectsById.ContainsKey(aspect.AspectId))
            {
                Debug.LogWarning($"Aspect with ID {aspect.AspectId} already exists");
                return false;
            }
            
            aspectsById.Add(aspect.AspectId, aspect);
            //Debug.Log($"Aspect {aspect.AspectId} registered");
            return true;
        }
        
        public bool TryGetAspect(string aspectId, out Aspect aspect)
        {
            return aspectsById.TryGetValue(aspectId, out aspect);
        }
        
        public bool ContainsAspect(string aspectId)
        {
            return aspectsById.ContainsKey(aspectId);
        }

        public bool LoadFromJson(TextAsset jsonTextAsset)
        {
            if (jsonTextAsset == null)
            {
                Debug.LogWarning("Aspect definition JSON is null");
                return false;
            }
            
            AspectDefinitionList definitionList = JsonUtility.FromJson<AspectDefinitionList>(jsonTextAsset.text);

            if (definitionList == null || definitionList.aspects == null)
            {
                Debug.LogWarning("Aspect definition JSON is invalid");
                return false;
            }

            bool loadedAny = false;

            foreach (AspectDefinition definition in definitionList.aspects)
            {
                Aspect aspect = Aspect.FromDefinition(definition);
                bool registered = RegisterAspect(aspect);
                loadedAny |= registered;
            }
            
            //Debug.Log($"Loaded aspects from JSON. Count = {aspectsById.Count}");
            return loadedAny;
        }

    }
}