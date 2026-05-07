using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Core.Aspects
{
    public class AspectAcceptanceRegistry
    {
        private readonly string registryName;
        private readonly GlobalAspectRegistry globalRegistry;
        private readonly HashSet<string> acceptedAspectIds = new();
        
        public string RegistryName => registryName;
        public IReadOnlyCollection<string> AcceptedAspectIds => acceptedAspectIds;

        public AspectAcceptanceRegistry(string registryName, GlobalAspectRegistry globalRegistry)
        {
            this.registryName = registryName;
            this.globalRegistry = globalRegistry;
        }

        public bool IsAccepted(string aspectId)
        {
            return acceptedAspectIds.Contains(aspectId);
        }

        public bool IsNovel(string aspectId)
        {
            return globalRegistry.ContainsAspect(aspectId) && !acceptedAspectIds.Contains(aspectId);
        }

        public bool AcceptAspect(string aspectId)
        {
            if (!globalRegistry.ContainsAspect(aspectId))
            {
                Debug.LogWarning($"Aspect {aspectId} is not registered");
                return false;
            }
            
            if (!acceptedAspectIds.Add(aspectId))
            {
                Debug.LogWarning($"Aspect {aspectId} is already accepted");
                return false;
            }
            
            Debug.Log($"Aspect {aspectId} accepted");
            return true;
        }
        
        public bool RemoveAspect(string aspectId)
        {
            if (!acceptedAspectIds.Remove(aspectId))
            {
                Debug.LogWarning($"Aspect {aspectId} is not accepted");
                return false;
            }
            
            Debug.Log($"Aspect {aspectId} removed");
            return true;
        }
        
        public bool LoadAcceptedIdsFromJson(TextAsset jsonTextAsset)
        {
            if (jsonTextAsset == null)
            {
                Debug.LogWarning("Aspect acceptance JSON is null");
                return false;
            }
            
            AspectIdList idList = JsonUtility.FromJson<AspectIdList>(jsonTextAsset.text);

            if (idList == null || idList.acceptedAspectIds == null)
            {
                Debug.LogWarning("Aspect acceptance JSON is invalid");
                return false;
            }
            
            bool loadedAny = false;
            
            foreach (string aspectId in idList.acceptedAspectIds)
            {
                bool accepted = AcceptAspect(aspectId);
                loadedAny |= accepted;
            }
            
            Debug.Log($"Loaded accepted aspects from JSON. Count = {acceptedAspectIds.Count}");
            return loadedAny;
        }

        public void DebugPrintAcceptedAspects()
        {
            Debug.Log($"Accepted aspects for {registryName} registry:");
            
            foreach (string aspectId in acceptedAspectIds)
            {
                Debug.Log("- {aspectId}");
            }
        }
        
        
        
        
    }
}