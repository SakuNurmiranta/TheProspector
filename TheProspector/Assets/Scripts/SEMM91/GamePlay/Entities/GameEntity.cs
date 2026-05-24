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

        [SerializeField] private string activeVhsSetId;
        public IReadOnlyList<VhsSet> VhsSets => vhsSets;
        public string ActiveVhsSetId => activeVhsSetId;
        
        [Header("Scope")] [SerializeField] private string nodeId;
        public string NodeId => nodeId;

        [Header("State")] [SerializeField] private GameEntityState state;

        public GameEntityState State => state;

        [Header("Information")] [SerializeField]
        private InformationScope informationScope;

        public InformationScope InformationScope => informationScope;

        [Header("Debug")]
        [SerializeField] private bool logEntityDebug;
        [SerializeField] private bool logEntityWarnings = true;
        [SerializeField] private string debugNodeInput;


        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                entityId = System.Guid.NewGuid().ToString();
                ELog($"Generated entityId={entityId} for {gameObject.name}");
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
            ELog($"Set entity {entityId} to node {nodeId}");
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
                EWarn($"Could not find tag container of type {containerType} for entity {entityId}");
                return false;
            }
            
            HeldTag heldtag = new HeldTag(newTag);

            if (containerType == TagContainerType.Transient)
            {
                TagContainer convictionContainer = GetTagContainer(TagContainerType.Conviction);
                
                if (convictionContainer != null && convictionContainer.HasHeldTag && convictionContainer.HeldTag.TagInstance.IsOpposedTo(newTag))
                {
                   heldtag.MarkUnstable(1, "Opposes conviction; must be expended into an Idea before end of next turn.");
                   
                   EWarn($"Unstable transient on entity {entityId} because of conviction");
                }
            }
            
            targetContainer.SetHeldTag(heldtag);
            return true;
        }
        
        public void AddState(GameEntityState newState)
        {
            state |= newState;
            ELog($"Added state {newState} to entity {entityId}");
        }

        public void RemoveState(GameEntityState removedState)
        {
            state &= ~removedState;
            ELog($"Removed state {removedState} from entity {entityId}");
        }

        public void SetInformationScope(InformationScope newScope)
        {
            informationScope = newScope;
            ELog($"Set information scope for entity {entityId} to {informationScope}");
        }
        
        
        public void SetController(string newControllerEntityId)
        {
            controllerEntityId = newControllerEntityId;
            ELog($"Set controller of entity {entityId} to {controllerEntityId}");
        }

        public void SetOwner(string newOwnerEntityId)
        {
            ownerEntityId = newOwnerEntityId;
            ELog($"Set owner of entity {entityId} to {ownerEntityId}");
        }

        public void SetCreator(string newCreatorEntityId)
        {
            creatorEntityId = newCreatorEntityId;
            ELog($"Set creator of entity {entityId} to {creatorEntityId}");
        }
        
        public void AddCollectiveMembership(string collectiveEntityId, bool isActiveMembership)
        {
            collectiveMemberships.Add(new CollectiveMembership(collectiveEntityId, isActiveMembership));
            ELog($"Added collective membership to entity {entityId}: {collectiveEntityId}, active={isActiveMembership}");
        }

        public void ResolveTagLifecycleAtTurnBoundary()
        {
            foreach (TagContainer container in tagContainers)
            {
                container.ResolveTurnBoundaryLifecycle();
            }
            
            ELog($"Resolved tag lifecycle for entity {entityId}");
        }

        public void AddAspectId(string aspectId)
        {
            if (aspectIds.Contains(aspectId))
            {
                ELog($"Entity {entityId} already has aspect {aspectId}");
                return;
            }
            
            aspectIds.Add(aspectId);
            ELog($"Added aspect {aspectId} to entity {entityId}");
        }

        public void AddIdea(Idea idea)
        {
            if (idea == null)
            {
                EWarn($"Cannot add null idea to entity {entityId}");
                return;
            }
            
            ideas.Add(idea);
            ELog($"Added idea {idea} to entity {entityId}");
        }

        public bool RemoveIdea(Idea idea)
        {
            if (idea == null)
            {
                EWarn($"Cannot remove null idea from entity {entityId}");
                return false;
            }

            bool removed = ideas.Remove(idea);

            if (removed)
            {
                ELog($"Removed idea {idea} from entity {entityId}");
            }
            else
            {
                EWarn($"Entity {entityId} does not have idea {idea}");
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
                ELog($"Generated entityId={entityId} for {gameObject.name}");
            }
            
            ELog($"Initialized identity for entity {entityId}: {displayName} ({entityType})");
        }

        public void AddTagContainer(TagContainerType containerType)
        {
            foreach (TagContainer existingContainer in tagContainers)
            {
                if (existingContainer.ContainerType == containerType)
                {
                    EWarn($"Entity {entityId} already has tag container of type {containerType}");
                    return;
                }   
            }
            
            tagContainers.Add(new TagContainer(containerType));
            ELog($"Added tag container of type {containerType} to entity {entityId}");
        }

        public void AddVhsTrack(VhsTrack vhsTrack)
        {
            if (vhsTrack == null)
            {
                ELog($"Cannot add null track to entity {entityId}");
                return;
            }
            
            vhsTracks.Add(vhsTrack);
            ELog($"Added VHS track {vhsTrack.DisplayName} to entity {entityId}");
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
                EWarn($"Cannot add null set to entity {entityId}");
                return; 
            }
            
            vhsSets.Add(vhsSet);
            
            if (string.IsNullOrWhiteSpace(activeVhsSetId)) activeVhsSetId = vhsSet.VhsSetId;
            
            ELog($"Added VHS set {vhsSet.DisplayName} to entity {entityId}");
        }
        
        public VhsSet GetLatestVhsSet()
        {
            if (vhsSets.Count == 0)
                return null;
            
            return vhsSets[vhsSets.Count - 1];
        }

        public VhsSet GetActiveVhsSet()
        {
            if (string.IsNullOrWhiteSpace(activeVhsSetId)) return null;

            foreach (VhsSet vhsSet in vhsSets)
            {
                if (vhsSet != null && vhsSet.VhsSetId == activeVhsSetId)
                    return vhsSet;
            }

            return null;
        }

        public void SetActiveVhsSet(VhsSet vhsSet)
        {
            if (vhsSet == null)
            {
                EWarn($"Cannot set active VHS set to null");
                return;
            }

            if (!vhsSets.Contains(vhsSet))
            {
                EWarn($"Cannot set active VHS set to {vhsSet.VhsSetId} because it is not in the entity's VHS sets");
                return;
            }
            
            activeVhsSetId = vhsSet.VhsSetId;
            ELog($"Set active VHS set to {vhsSet.DisplayName} for entity {entityId}");
            
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

        public bool CycleActiveVhsSet()
        {
            if (vhsSets.Count == 0)
            {
                EWarn($"Cannot cycle active VHS set because entity {entityId} has no VHS sets");
                return false;
            }

            if (string.IsNullOrWhiteSpace(activeVhsSetId))
            {
                activeVhsSetId = vhsSets[0].VhsSetId;
                ELog($"Set active VHS set to {vhsSets[0].DisplayName} for entity {entityId}");
                return true;
            }

            int currentIndex = -1;

            for (int i = 0; i < vhsSets.Count; i++)
            {
                if (vhsSets[i] != null && vhsSets[i].VhsSetId == activeVhsSetId)
                {
                    currentIndex = i;
                    break;
                } 
            }
            
            int nextIndex = currentIndex < 0 
                ? 0
                : (currentIndex + 1) % vhsSets.Count;
            
            activeVhsSetId = vhsSets[nextIndex].VhsSetId;
            
            ELog($"Set active VHS set to {vhsSets[nextIndex].DisplayName} for entity {entityId}");
            return true;
        }
        
        private void ELog(string message)
        {
            if (!logEntityDebug) return;

            Debug.Log($"[GameEntity] {message}", this);
        }

        private void EWarn(string message)
        {
            if (!logEntityWarnings) return;

            Debug.LogWarning($"[GameEntity] {message}", this);
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
            ELog($"{displayName} gained Resonance container");
        }
        
        [ContextMenu("Debug/Add Test Aspect")]
        private void DebugAddTestAspect()
        {
            aspectIds.Add("ASPECT_GUITAR");
            ELog($"Added test aspect to entity {entityId}");
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