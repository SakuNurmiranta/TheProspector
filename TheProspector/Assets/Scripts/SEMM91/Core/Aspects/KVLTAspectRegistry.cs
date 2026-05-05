using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Core.Aspects
{
    
// ReSharper disable once InconsistentNaming
    public class KVLTAspectRegistry
    {
        private readonly GlobalAspectRegistry _globalAspectRegistry;
        private readonly HashSet<string> acceptedAspectIds = new();

        public IReadOnlyCollection<string> AcceptedAspectIds => acceptedAspectIds;

        public KVLTAspectRegistry(GlobalAspectRegistry globalAspectRegistry)
        {
            this._globalAspectRegistry = globalAspectRegistry;
        }

        public bool IsAccepted(string aspectId)
        {
            return acceptedAspectIds.Contains(aspectId);
        }

        public bool IsNovel(string aspectId)
        {
            return _globalAspectRegistry.ContainsAspect(aspectId) && !acceptedAspectIds.Contains(aspectId);
        }

        public bool AcceptAspect(string aspectId)
        {
            if (!_globalAspectRegistry.ContainsAspect(aspectId))
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

        public void DebugPrintAcceptedAspects()
        {
            Debug.Log("KVLT accepted aspects:");

            foreach (string aspectId in acceptedAspectIds)
            {
                Debug.Log("- {aspectId}");
            }
        }
    }
}