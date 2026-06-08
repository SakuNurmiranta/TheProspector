using UnityEngine;

namespace SEMM91.GamePlay.World
{
    public class SeededWorldStateHolder : MonoBehaviour
    {
        public static SeededWorldStateHolder Instance { get; private set; }
        
        public SeededWorldState WorldState { get; private set; }
        
        public bool HasWorldState => WorldState != null;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple SeededWorldStateHolders in scene!");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }

        public void SetWorldState(SeededWorldState worldState)
        {
            if (worldState == null)
            {
                Debug.LogWarning("Cannot set null world state!");
                return;
            }
            
            if (WorldState != null)
            {
                Debug.LogWarning("World state already set!");
                return;
            }

            WorldState = worldState;
            
            Debug.Log(
                "[SeededWorldStateHolder] Shared world state assigned | " +
                $"entities={WorldState.Entities.Count}, " +
                $"hostingRecords={WorldState.HostingRecords.Count}, " +
                $"collectives={WorldState.CollectiveRegistry?.Collectives.Count ?? 0}, " +
                $"sceneNodes={WorldState.SceneSpaceGraph?.Nodes.Count ?? 0}"
            );
        }
        
        public void DebugPrintSummary()
        {
            if (WorldState == null)
            {
                Debug.LogWarning("[SeededWorldStateHolder] No world state assigned");
                return;
            }

            WorldState.DebugPrintSummary();
            WorldState.DebugPrintLookupSummary();
        }
    }
}