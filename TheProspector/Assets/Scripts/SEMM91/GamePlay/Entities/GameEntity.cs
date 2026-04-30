using System.Collections.Generic;
using SEMM91.Tags;
using UnityEngine;

namespace SEMM91.GamePlay.Entities
{
    public class GameEntity : MonoBehaviour
    {

        [Header("Ownership")] 
        [SerializeField] private string controllerEntityId;
        [SerializeField] private string ownerEntityId;
        [SerializeField] private string creatorEntityId;
        
        public string ControllerEntityId => controllerEntityId;
        public string OwnerEntityId => ownerEntityId;
        public string CreatorEntityId => creatorEntityId;
        
        [Header("Identity")] [SerializeField] private string entityId;
        [SerializeField] private string displayName;
        [SerializeField] private GameEntityType entityType;

        public string EntityId => entityId;
        public string DisplayName => displayName;
        public GameEntityType EntityType => entityType;

        [Header("Collectives")]
        [SerializeField] private List<CollectiveMembership> collectiveMemberships = new();
        public IReadOnlyList<CollectiveMembership> CollectiveMemberships => collectiveMemberships;
        
        [Header("Tags")]
        [SerializeField] private List<TagContainer> tagContainers = new();
        public List<TagContainer> TagContainers => tagContainers;
        
        [Header("Aspects")] 
        [SerializeField] private List<string> aspectIds = new();
        public IReadOnlyList<string> AspectIds => aspectIds;
        
        [Header("Scope")] [SerializeField] private string nodeId;

        public string NodeId => nodeId;

        [Header("State")] [SerializeField] private GameEntityState state;

        public GameEntityState State => state;

        [Header("Information")] [SerializeField]
        private InformationScope informationScope;

        public InformationScope InformationScope => informationScope;

        [Header("Debug")] [SerializeField] private string debugNodeInput;


        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                entityId = System.Guid.NewGuid().ToString();
                Debug.Log($"Generated entityId={entityId} for {gameObject.name}");
            }
        }

        public void SetNode(string newNodeId)
        {
            nodeId = newNodeId;
            Debug.Log($"Set entity {entityId} to node {nodeId}");
        }

        public bool HasState(GameEntityState targetState)
        {
            return (state & targetState) != 0;
        }

        public void AddState(GameEntityState newState)
        {
            state |= newState;
            Debug.Log($"Added state {newState} to entity {entityId}");
        }

        public void RemoveState(GameEntityState removedState)
        {
            state &= ~removedState;
            Debug.Log($"Removed state {removedState} from entity {entityId}");
        }

        public void SetInformationScope(InformationScope newScope)
        {
            informationScope = newScope;
            Debug.Log($"Set information scope for entity {entityId} to {informationScope}");
        }
        
        
        public void SetController(string newControllerEntityId)
        {
            controllerEntityId = newControllerEntityId;
            Debug.Log($"Set controller of entity {entityId} to {controllerEntityId}");
        }

        public void SetOwner(string newOwnerEntityId)
        {
            ownerEntityId = newOwnerEntityId;
            Debug.Log($"Set owner of entity {entityId} to {ownerEntityId}");
        }

        public void SetCreator(string newCreatorEntityId)
        {
            creatorEntityId = newCreatorEntityId;
            Debug.Log($"Set creator of entity {entityId} to {creatorEntityId}");
        }
        
        public void AddCollectiveMembership(string collectiveEntityId, bool isActiveMembership)
        {
            collectiveMemberships.Add(new CollectiveMembership(collectiveEntityId, isActiveMembership));
            Debug.Log($"Added collective membership to entity {entityId}: {collectiveEntityId}, active={isActiveMembership}");
        }
        
        [ContextMenu("Set Node (Debug)")]
        private void DebugSetNode()
        {
            SetNode(debugNodeInput);
        }

        [ContextMenu("Debug/Add Damaged")]
        private void DebugAddDamaged()
        {
            AddState(GameEntityState.Damaged);
        }

        [ContextMenu("Debug/Remove Damaged")]
        private void DebugRemoveDamaged()
        {
            RemoveState(GameEntityState.Damaged);
        }

        [ContextMenu("Debug/Set Information Scope: Experienced")]
        private void DebugSetExperienced()
        {
            SetInformationScope(InformationScope.Experienced);
        }

        [ContextMenu("Debug/Add Resonance Container")]
        private void DebugAddResonanceContainer()
        {
            tagContainers.Add(new TagContainer(TagContainerType.Resonance));
            Debug.Log($"{displayName} gained Resonance container");
        }
        
        [ContextMenu("Debug/Add Test Aspect")]
        private void DebugAddTestAspect()
        {
            aspectIds.Add("ASPECT_GUITAR");
            Debug.Log($"Added test aspect to entity {entityId}");
        }
        
        [ContextMenu("Debug/Set Self As Owner")]
        private void DebugSetSelfAsOwner()
        {
            SetOwner(entityId);
        }
        
        [ContextMenu("Debug/Add Test Band Membership")]
        private void DebugAddTestBandMembership()
        {
            AddCollectiveMembership("COLLECTIVE_NOT_SAVED_THE_BAND", true);
        }
        
        [ContextMenu("Debug/Print Entity Summary")]
        private void DebugPrintEntitySummary()
        {
            Debug.Log(
                $"ENTITY SUMMARY | id={entityId}, name={displayName}, type={entityType}, node={nodeId}, " +
                $"state={state}, info={informationScope}, tags={tagContainers.Count}, " +
                $"aspects={aspectIds.Count}, collectives={collectiveMemberships.Count}, " +
                $"owner={ownerEntityId}, creator={creatorEntityId}, controller={controllerEntityId}"
            );
        }
    }
}