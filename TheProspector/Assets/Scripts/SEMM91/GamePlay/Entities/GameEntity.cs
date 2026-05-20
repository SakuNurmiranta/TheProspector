using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tracks;
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
        
        [Header("Ideas")] 
        [SerializeField] private List<Idea> ideas = new();
        public IReadOnlyList<Idea> Ideas => ideas;
        
        [Header("VHS Tracks")]
        [SerializeField] private List<VhsTrack> vhsTracks = new();
        public IReadOnlyList<VhsTrack> VhsTracks => vhsTracks;

        [Header("VHS Sets")] 
        [SerializeField] private List<VhsSet> vhsSets = new();
        public IReadOnlyList<VhsSet> VhsSets => vhsSets;
        
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

        private TagContainer GetTagContainer(TagContainerType containerType)
        {
            foreach (var container in tagContainers)
            {
                if (container.ContainerType == containerType)
                    return container;
            }

            return null;
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

        public bool TrySetTag(TagContainerType containerType, TagInstance newTag)
        {
            TagContainer targetContainer = GetTagContainer(containerType);   
            
            if (targetContainer == null)
            {
                Debug.Log($"Could not find tag container of type {containerType} for entity {entityId}");
                return false;
            }
            
            HeldTag heldtag = new HeldTag(newTag);

            if (containerType == TagContainerType.Transient)
            {
                TagContainer convictionContainer = GetTagContainer(TagContainerType.Conviction);
                
                if (convictionContainer != null && convictionContainer.HasHeldTag && convictionContainer.HeldTag.TagInstance.IsOpposedTo(newTag))
                {
                   heldtag.MarkUnstable(1, "Opposes conviction; must be expended into an Idea before end of next turn.");
                   
                   Debug.LogWarning($"Unstable transient on entity {entityId} because of conviction");
                }
            }
            
            targetContainer.SetHeldTag(heldtag);
            return true;
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

        public void ResolveTagLifecycleAtTurnBoundary()
        {
            foreach (TagContainer container in tagContainers)
            {
                container.ResolveTurnBoundaryLifecycle();
            }
            
            Debug.Log($"Resolved tag lifecycle for entity {entityId}");
        }

        public void AddAspectId(string aspectId)
        {
            if (aspectIds.Contains(aspectId))
            {
                Debug.Log($"Entity {entityId} already has aspect {aspectId}");
                return;
            }
            
            aspectIds.Add(aspectId);
            Debug.Log($"Added aspect {aspectId} to entity {entityId}");
        }

        public void AddIdea(Idea idea)
        {
            if (idea == null)
            {
                Debug.LogWarning($"Cannot add null idea to entity {entityId}");
                return;
            }
            
            ideas.Add(idea);
            Debug.Log($"Added idea {idea} to entity {entityId}");
        }

        public bool RemoveIdea(Idea idea)
        {
            if (idea == null)
            {
                Debug.LogWarning($"Cannot remove null idea from entity {entityId}");
                return false;
            }

            bool removed = ideas.Remove(idea);

            if (removed)
            {
                Debug.Log($"Removed idea {idea} from entity {entityId}");
            }
            else
            {
                Debug.LogWarning($"Entity {entityId} does not have idea {idea}");
            }
            return removed;
        }
        
        public bool TryGetTagContainer(TagContainerType containerType, out TagContainer container)
        {
            foreach (TagContainer existingContainer in tagContainers)
            {
                if (existingContainer.ContainerType == containerType)
                {
                    container = existingContainer;
                    return true;
                }
            }

            container = null;
            return false;
        }

        public void InitializeIdentity(string newDisplayName, GameEntityType newEntityType)
        {
            displayName = newDisplayName;
            entityType = newEntityType;
            
            if (string.IsNullOrWhiteSpace(entityId))
            {
                entityId = System.Guid.NewGuid().ToString();
                Debug.Log($"Generated entityId={entityId} for {gameObject.name}");
            }
            
            Debug.Log($"Initialized identity for entity {entityId}: {displayName} ({entityType})");
        }

        public void AddTagContainer(TagContainerType containerType)
        {
            foreach (TagContainer existingContainer in tagContainers)
            {
                if (existingContainer.ContainerType == containerType)
                {
                    Debug.LogWarning($"Entity {entityId} already has tag container of type {containerType}");
                    return;
                }   
            }
            
            tagContainers.Add(new TagContainer(containerType));
            Debug.Log($"Added tag container of type {containerType} to entity {entityId}");
        }

        public void AddVhsTrack(VhsTrack vhsTrack)
        {
            if (vhsTrack == null)
            {
                Debug.LogWarning($"Cannot add null track to entity {entityId}");
                return;
            }
            
            vhsTracks.Add(vhsTrack);
            Debug.Log($"Added VHS track {vhsTrack.DisplayName} to entity {entityId}");
        }

        public VhsTrack GetLatestVhsTrackFromLatestSet()
        {
            VhsSet latestSet = GetLatestVhsSet();

            if (latestSet == null) return null;

            return latestSet.GetLatestVhsTrack();
        }
        
        public void AddVhsSet(VhsSet vhsSet)
        {
            if (vhsSet == null)
            {
                Debug.LogWarning($"Cannot add null set to entity {entityId}");
                return; 
            }
            
            vhsSets.Add(vhsSet);
            Debug.Log($"Added VHS set {vhsSet.DisplayName} to entity {entityId}");
        }
        
        public VhsSet GetLatestVhsSet()
        {
            if (vhsSets.Count == 0)
                return null;
            
            return vhsSets[vhsSets.Count - 1];
        }

        public int GetTotalVhsTrackCountFromSets()
        {
            int count = 0;

            foreach (VhsSet vhsSet in vhsSets)
            {
                if (vhsSet == null) continue;
                
                count += vhsSet.VhsTracks.Count;
            }
            
            return count;
        }
        
        [ContextMenu("Debug/Set Node")]
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