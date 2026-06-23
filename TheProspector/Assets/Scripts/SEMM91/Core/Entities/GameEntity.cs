using System.Collections.Generic;
using SEMM91.Core.Ideas;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEMM91.Core.Entities
{
    public class GameEntity : MonoBehaviour
    {
        [FormerlySerializedAs("controllerEntityId")] [Header("Ownership")] [SerializeField]
        private string leaderEntityId;

        [SerializeField] private string ownerEntityId;
        [SerializeField] private string creatorEntityId;

        public string LeaderEntityId => leaderEntityId;
        public string OwnerEntityId => ownerEntityId;
        public string CreatorEntityId => creatorEntityId;

        [Header("Identity")] [SerializeField] private string entityId;
        [SerializeField] private string displayName;
        [SerializeField] private GameEntityType entityType;

        public string EntityId => entityId;
        public string DisplayName => displayName;
        public GameEntityType EntityType => entityType;

        [Header("Collectives")] [SerializeField]
        private List<CollectiveMembership> collectiveMemberships = new();

        public IReadOnlyList<CollectiveMembership> CollectiveMemberships => collectiveMemberships;

        [Header("Tags")] [SerializeField] private List<TagContainer> tagContainers = new();
        public List<TagContainer> TagContainers => tagContainers;

        [Header("Aspects")] [SerializeField] private List<string> aspectIds = new();
        public IReadOnlyList<string> AspectIds => aspectIds;

        [Header("Ideas")] [SerializeField] private List<Idea> ideas = new();
        public IReadOnlyList<Idea> Ideas => ideas;

        [Header("VHS Tracks")] [SerializeField]
        private List<Track> vhsTracks = new();

        public IReadOnlyList<Track> VhsTracks => vhsTracks;

        [Header("VHS Sets")] [SerializeField] private List<RehearsalSet> vhsSets = new();

        [SerializeField] private string activeVhsSetId;
        public IReadOnlyList<RehearsalSet> VhsSets => vhsSets;
        public string ActiveVhsSetId => activeVhsSetId;

        private readonly List<DemoTape> demoTapes = new();
        public IReadOnlyList<DemoTape> DemoTapes => demoTapes;

        [Header("Scope")] [SerializeField] private string nodeId;
        public string NodeId => nodeId;

        [Header("State")] [SerializeField] private GameEntityState state;

        public GameEntityState State => state;

        public bool IsExhausted =>
            HasState(GameEntityState.Exhausted);

        [Header("Information")] [SerializeField]
        private InformationScope informationScope;

        public InformationScope InformationScope => informationScope;

        [Header("Debug")] [SerializeField] private bool logEntityDebug;
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

                if (convictionContainer != null && convictionContainer.HasHeldTag &&
                    convictionContainer.HeldTag.TagInstance.IsOpposedTo(newTag))
                {
                    heldtag.MarkUnstable(1,
                        "Opposes conviction; must be expended into an Idea before end of next turn.");

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

        public void SetExhausted(bool exhausted)
        {
            if (IsExhausted == exhausted) return;

            if (exhausted)
            {
                AddState(GameEntityState.Exhausted);
            }
            else 
            {
                RemoveState(GameEntityState.Exhausted);
            }
        }
        
        public void SetInformationScope(InformationScope newScope)
        {
            informationScope = newScope;
            ELog($"Set information scope for entity {entityId} to {informationScope}");
        }


        public void SetController(string newControllerEntityId)
        {
            leaderEntityId = newControllerEntityId;
            ELog($"Set controller of entity {entityId} to {leaderEntityId}");
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

        public bool AddCollectiveMembership(string collectiveEntityId, bool isActiveMembership)
        {
            if (string.IsNullOrWhiteSpace(collectiveEntityId))
            {
                EWarn($"Cannot add empty collective membership to entity {entityId}");
                return false;
            }

            if (HasCollectiveMembership(collectiveEntityId))
            {
                EWarn($"Entity {entityId} already has collective membership: {collectiveEntityId}");
                return false;
            }

            if (isActiveMembership)
            {
                ClearActiveCollectiveMemberships();
            }

            collectiveMemberships.Add(new CollectiveMembership(collectiveEntityId, isActiveMembership));
            ELog(
                $"Added collective membership to entity {entityId}: {collectiveEntityId}, active={isActiveMembership}");
            return true;
        }

        public bool HasCollectiveMembership(string collectiveEntityId)
        {
            foreach (CollectiveMembership membership in collectiveMemberships)
            {
                if (membership != null && membership.Matches(collectiveEntityId))
                    return true;
            }

            return false;
        }

        public bool HasActiveCollectiveMembership(string collectiveEntityId)
        {
            foreach (CollectiveMembership membership in collectiveMemberships)
            {
                if (membership != null &&
                    membership.Matches(collectiveEntityId) &&
                    membership.IsActiveMembership)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetActiveCollectiveEntityId()
        {
            foreach (CollectiveMembership membership in collectiveMemberships)
            {
                if (membership != null && membership.IsActiveMembership)
                    return membership.CollectiveEntityId;
            }

            return null;
        }

        public bool SetActiveCollectiveMembership(string collectiveEntityId)
        {
            if (!HasCollectiveMembership(collectiveEntityId))
            {
                EWarn($"Cannot activate missing collective membership {collectiveEntityId} on entity {entityId}");
                return false;
            }

            ClearActiveCollectiveMemberships();

            foreach (CollectiveMembership membership in collectiveMemberships)
            {
                if (membership != null && membership.Matches(collectiveEntityId))
                {
                    membership.SetActiveMembership(true);
                    ELog($"Set active collective membership for entity {entityId}: {collectiveEntityId}");
                    return true;
                }
            }

            return false;
        }

        public bool RemoveCollectiveMembership(string collectiveEntityId)
        {
            for (int i = collectiveMemberships.Count - 1; i >= 0; i--)
            {
                CollectiveMembership membership = collectiveMemberships[i];

                if (membership != null && membership.Matches(collectiveEntityId))
                {
                    collectiveMemberships.RemoveAt(i);
                    ELog($"Removed collective membership from entity {entityId}: {collectiveEntityId}");
                    return true;
                }
            }

            EWarn($"Cannot remove missing collective membership {collectiveEntityId} from entity {entityId}");
            return false;
        }

        private void ClearActiveCollectiveMemberships()
        {
            foreach (CollectiveMembership membership in collectiveMemberships)
            {
                membership?.SetActiveMembership(false);
            }
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

        public void InitializeIdentity(
            string explicitEntityId,
            string displayName,
            GameEntityType entityType
        )
        {
            if (string.IsNullOrWhiteSpace(explicitEntityId))
            {
                EWarn("Cannot initialize GameEntity with empty explicit entity ID");
                return;
            }

            entityId = explicitEntityId;
            this.displayName = displayName;
            this.entityType = entityType;

            ELog($"Initialized entity with explicit ID: {entityId}, name={displayName}, type={entityType}");
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

        public void AddVhsTrack(Track track)
        {
            if (track == null)
            {
                ELog($"Cannot add null track to entity {entityId}");
                return;
            }

            vhsTracks.Add(track);
            ELog($"Added VHS track {track.DisplayName} to entity {entityId}");
        }

        public Track GetLatestVhsTrackFromLatestSet()
        {
            RehearsalSet latestSet = GetLatestVhsSet();

            if (latestSet == null) return null;

            return latestSet.GetLatestVhsTrack();
        }

        public void AddVhsSet(RehearsalSet rehearsalSet)
        {
            if (rehearsalSet == null)
            {
                EWarn($"Cannot add null set to entity {entityId}");
                return;
            }

            vhsSets.Add(rehearsalSet);

            if (string.IsNullOrWhiteSpace(activeVhsSetId)) activeVhsSetId = rehearsalSet.VhsSetId;

            ELog($"Added VHS set {rehearsalSet.DisplayName} to entity {entityId}");
        }

        public RehearsalSet GetLatestVhsSet()
        {
            if (vhsSets.Count == 0)
                return null;

            return vhsSets[vhsSets.Count - 1];
        }

        public RehearsalSet GetActiveVhsSet()
        {
            if (string.IsNullOrWhiteSpace(activeVhsSetId)) return null;

            foreach (RehearsalSet vhsSet in vhsSets)
            {
                if (vhsSet != null && vhsSet.VhsSetId == activeVhsSetId)
                    return vhsSet;
            }

            return null;
        }

        public void SetActiveVhsSet(RehearsalSet rehearsalSet)
        {
            if (rehearsalSet == null)
            {
                EWarn($"Cannot set active VHS set to null");
                return;
            }

            if (!vhsSets.Contains(rehearsalSet))
            {
                EWarn(
                    $"Cannot set active VHS set to {rehearsalSet.VhsSetId} because it is not in the entity's VHS sets");
                return;
            }

            activeVhsSetId = rehearsalSet.VhsSetId;
            ELog($"Set active VHS set to {rehearsalSet.DisplayName} for entity {entityId}");
        }

        public int GetTotalVhsTrackCountFromSets()
        {
            int count = 0;

            foreach (RehearsalSet vhsSet in vhsSets)
            {
                if (vhsSet == null) continue;

                count += vhsSet.VhsTracks.Count;
            }

            return count;
        }

        public bool CanCycleActiveVhsSet()
        {
            foreach (RehearsalSet rehearsalSet
                     in vhsSets)
            {
                if (rehearsalSet == null)
                    continue;

                bool isDifferentTarget =
                    string.IsNullOrWhiteSpace(
                        activeVhsSetId
                    ) ||
                    rehearsalSet.VhsSetId !=
                    activeVhsSetId;

                if (isDifferentTarget)
                    return true;
            }

            return false;
        }
        public bool CycleActiveVhsSet()
        {
            if (!CanCycleActiveVhsSet())
            {
                EWarn(
                    $"Cannot cycle active VHS set because " +
                    $"entity {entityId} has no alternative " +
                    "rehearsal set"
                );

                return false;
            }

            int currentIndex = -1;

            for (int i = 0;
                 i < vhsSets.Count;
                 i++)
            {
                RehearsalSet rehearsalSet =
                    vhsSets[i];

                if (rehearsalSet != null &&
                    rehearsalSet.VhsSetId ==
                    activeVhsSetId)
                {
                    currentIndex = i;
                    break;
                }
            }

            for (int offset = 1;
                 offset <= vhsSets.Count;
                 offset++)
            {
                int candidateIndex =
                    currentIndex < 0
                        ? offset - 1
                        : (
                            currentIndex +
                            offset
                        ) % vhsSets.Count;

                RehearsalSet candidate =
                    vhsSets[candidateIndex];

                if (candidate == null)
                    continue;

                if (candidate.VhsSetId ==
                    activeVhsSetId)
                {
                    continue;
                }

                activeVhsSetId =
                    candidate.VhsSetId;

                ELog(
                    $"Set active VHS set to " +
                    $"{candidate.DisplayName} " +
                    $"for entity {entityId}"
                );

                return true;
            }

            EWarn(
                $"Could not find an alternative VHS set " +
                $"for entity {entityId}"
            );

            return false;
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

        public void AddDemoTape(DemoTape demoTape)
        {
            if (demoTape == null)
                return;

            demoTapes.Add(demoTape);
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
                $"state={state}, info={informationScope}, tagsContainers={tagContainers.Count}, " +
                $"aspects={aspectIds.Count}, collectives={collectiveMemberships.Count}, " +
                $"owner={ownerEntityId}, creator={creatorEntityId}, leader={leaderEntityId}"
            );
        }
        
        [ContextMenu("Debug/Print DemoTapes")]
        private void DebugPrintDemoTapes()
        {
            Debug.Log(
                $"[DEMO CHECK] Entity={displayName} id={entityId} demoTapes={demoTapes.Count}",
                this
            );

            for (int i = 0; i < demoTapes.Count; i++)
            {
                DemoTape tape = demoTapes[i];

                if (tape == null)
                {
                    Debug.Log($"[DEMO CHECK] [{i}] NULL", this);
                    continue;
                }

                Debug.Log(
                    $"[DEMO CHECK] [{i}] {tape.DisplayName} sceneState={tape.SceneState}",
                    this
                );
            }
        }
    }
}