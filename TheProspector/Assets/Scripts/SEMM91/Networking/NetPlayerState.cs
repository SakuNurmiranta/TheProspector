using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;
using SEMM91.InputSystems;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Events;

namespace SEMM91.Networking
{
    /// <summary>
    /// Network-visible state container for one connected player.
    /// 
    /// NetPlayerState stores player identity, score, active/session state,
    /// stance state, and the current draft/commit action buffers used by the
    /// vertical-slice turn system.
    /// 
    /// Server authority:
    /// - Only the server should mutate NetworkVariables and action buffers.
    /// - Clients observe replicated values and request changes through controllers/RPCs.
    /// 
    /// Runtime link:
    /// - PlayerEntity is a server-side runtime reference to the in-world leader entity.
    /// - It is not network-serialized here.
    /// 
    /// This class should store and expose player state. It should not resolve
    /// gameplay rules directly.
    /// </summary>
    public class NetPlayerState : NetworkBehaviour
    {
        // -----------------------------------------------------------------------------
        // Runtime-only player references and yearly snapshot state
        // -----------------------------------------------------------------------------
        public Dictionary<ulong, LastResolvedRoundData> LastResolvedRound = new();
        public GameEntity PlayerEntity { get; private set; }


        // -----------------------------------------------------------------------------
        // Network-visible player identity and condition
        // -----------------------------------------------------------------------------
        public NetworkVariable<int> playerIndex = new(-1);

        public NetworkVariable<FixedString32Bytes> displayName = new(new FixedString32Bytes("Player"));


        public NetworkVariable<int> score = new();

        public NetworkVariable<bool> isActive = new(false);

        private readonly NetworkVariable<bool> _canDream =
            new(
                false,
                NetworkVariableReadPermission.Owner,
                NetworkVariableWritePermission.Server
            );

        private readonly NetworkVariable<TagContainerType> _selectedIdeaSource =
            new(
                TagContainerType.Conviction,
                NetworkVariableReadPermission.Owner,
                NetworkVariableWritePermission.Server
            );
        
        private readonly NetworkVariable<bool> _hasCommittedTurn =
            new(
                false,
                NetworkVariableReadPermission.Owner,
                NetworkVariableWritePermission.Server
            );
        
        private readonly NetworkVariable<DraftedActionSummary>
            _draftedStandardSlot1 =
                new(
                    DraftedActionSummary.Empty,
                    NetworkVariableReadPermission.Owner,
                    NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<DraftedActionSummary>
            _draftedStandardSlot2 =
                new(
                    DraftedActionSummary.Empty,
                    NetworkVariableReadPermission.Owner,
                    NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<DraftedActionSummary>
            _draftedOverreachSlot =
                new(
                    DraftedActionSummary.Empty,
                    NetworkVariableReadPermission.Owner,
                    NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<PlayerCommandFeedback>
            _latestCommandFeedback =
                new(
                    PlayerCommandFeedback.Empty,
                    NetworkVariableReadPermission.Owner,
                    NetworkVariableWritePermission.Server
                );

        private readonly NetworkVariable<PlayerContextTargetSummary>
            _contextTargetSummary =
                new(
                    PlayerContextTargetSummary.Empty,
                    NetworkVariableReadPermission.Owner,
                    NetworkVariableWritePermission.Server
                );

        private uint _commandFeedbackSequence;
        
        // -----------------------------------------------------------------------------
        // Network-visible stance state
        // -----------------------------------------------------------------------------

        private readonly NetworkVariable<BandStance> _currentStance = new();
        private readonly NetworkVariable<BandStance> _previousStance = new();
        public BandStance CurrentStanceValue => _currentStance.Value;
        public BandStance PreviousStanceValue => _previousStance.Value;
        
        public event Action<BandStance, BandStance> CurrentStanceChanged;

        public event Action<NetPlayerState>
            ServerActionPlanChanged;
        
        // -----------------------------------------------------------------------------
        // Network-visible action counters
        // -----------------------------------------------------------------------------

        private readonly NetworkVariable<byte> _committedActions = new();
        private readonly NetworkVariable<byte> _draftedActions = new();
        public byte CommittedActionsValue => _committedActions.Value;
        public byte DraftedActionsValue => _draftedActions.Value;

        public PlayerCommandFeedback LatestCommandFeedbackValue =>
            _latestCommandFeedback.Value;

        public PlayerContextTargetSummary ContextTargetSummaryValue =>
            _contextTargetSummary.Value;

        private readonly NetworkList<
            ObservedPhysicalEventSummary
        > _observedPhysicalEvents =
            new NetworkList<
                ObservedPhysicalEventSummary
            >(
                null,
                NetworkVariableReadPermission.Owner,
                NetworkVariableWritePermission.Server
            );

        public NetworkList<
            ObservedPhysicalEventSummary
        > ObservedPhysicalEvents =>
            _observedPhysicalEvents;
        
        // -----------------------------------------------------------------------------
        // Server-side action payload buffers
        // -----------------------------------------------------------------------------
        // These are not replicated NetworkVariables; they are filled by server-side
        // RPC handling and consumed by GameCoordinator during turn commit.
        private readonly List<DraftedActionPayload> _draftedActionPayloads = new();
        private readonly List<DraftedActionPayload> _committedActionPayloads = new();
        public IReadOnlyList<DraftedActionPayload> DraftedActionPayloads => _draftedActionPayloads;
        public IReadOnlyList<DraftedActionPayload> CommittedActionPayloads => _committedActionPayloads;

        public DraftedActionSummary DraftedStandardSlot1Value =>
            _draftedStandardSlot1.Value;

        public DraftedActionSummary DraftedStandardSlot2Value =>
            _draftedStandardSlot2.Value;

        public DraftedActionSummary DraftedOverreachSlotValue =>
            _draftedOverreachSlot.Value;
        
        public DraftedActionSummary GetDraftedActionSummary(
            int actionPosition)
        {
            return actionPosition switch
            {
                1 => _draftedStandardSlot1.Value,
                2 => _draftedStandardSlot2.Value,
                3 => _draftedOverreachSlot.Value,
                _ => DraftedActionSummary.Empty
            };
        }
        
        // -----------------------------------------------------------------------------
        // Cached ownership and convenience read properties
        // -----------------------------------------------------------------------------
        public ulong OwnerClientIdCached { get; private set; } = ulong.MaxValue;

        // --Properties of convenience

        public int ScoreValue => score.Value;
        
        public bool ActiveValue => isActive.Value;
        
        public bool HasCommittedTurnValue =>
            _hasCommittedTurn.Value;
        
        public int IndexValue => playerIndex.Value;
        public string DisplayNameStr => displayName.Value.ToString();

        public bool CanDreamValue => _canDream.Value;
        
        public TagContainerType SelectedIdeaSourceValue => _selectedIdeaSource.Value;

        // -----------------------------------------------------------------------------
        // NetworkBehaviour lifecycle
        // -----------------------------------------------------------------------------

        public override void OnNetworkSpawn()
        {
            OwnerClientIdCached = OwnerClientId;

            score.OnValueChanged += HandleScoreChanged;
            isActive.OnValueChanged += HandleIsActiveChanged;
            _currentStance.OnValueChanged += HandleCurrentStanceChanged;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            score.OnValueChanged -=
                HandleScoreChanged;

            isActive.OnValueChanged -=
                HandleIsActiveChanged;

            _currentStance.OnValueChanged -=
                HandleCurrentStanceChanged;

            _observedPhysicalEvents.Dispose();
        }

        // -----------------------------------------------------------------------------
        // Server-side initialization
        // -----------------------------------------------------------------------------

        /// <summary>
        /// Called by the host after player spawn.
        /// </summary>
        public void InitializeServer(int initPlayerIndex, string initDisplayName)
        {
            if (!IsServer)
            {
                Debug.LogError($"{nameof(NetPlayerState)}.InitializeServer() called on client.");
                return;
            }

            ResetDraftedActionsServer();
            ResetCommittedActionsServer();
            
            playerIndex.Value = initPlayerIndex;
            displayName.Value = new FixedString32Bytes(initDisplayName);

            // Default values when fresh
            score.Value = 0;
            isActive.Value = false;
            _canDream.Value = false;
            _selectedIdeaSource.Value = TagContainerType.Conviction;
            _hasCommittedTurn.Value = false;
            _commandFeedbackSequence = 0;

            _latestCommandFeedback.Value =
                PlayerCommandFeedback.Empty;

            _contextTargetSummary.Value =
                PlayerContextTargetSummary.Empty;
        }

        // -----------------------------------------------------------------------------
        // Server-authoritative player-state mutators
        // -----------------------------------------------------------------------------

        private void SetDraftedActionSummaryServer(
            int actionPosition,
            DraftedActionSummary summary)
        {
            if (!IsServer)
                return;

            switch (actionPosition)
            {
                case 1:
                    _draftedStandardSlot1.Value =
                        summary;
                    break;

                case 2:
                    _draftedStandardSlot2.Value =
                        summary;
                    break;

                case 3:
                    _draftedOverreachSlot.Value =
                        summary;
                    break;

                default:
                    Debug.LogError(
                        $"[DRAFT SUMMARY] Invalid action " +
                        $"position {actionPosition}.",
                        this
                    );
                    break;
            }
        }
        
        private void ValidateDraftStateServer(
            string operation)
        {
            if (!IsServer)
                return;

            int payloadCount =
                _draftedActionPayloads.Count;

            bool countMatches =
                _draftedActions.Value ==
                payloadCount;

            bool occupancyMatches =
                _draftedStandardSlot1.Value.IsOccupied ==
                (payloadCount >= 1) &&
                _draftedStandardSlot2.Value.IsOccupied ==
                (payloadCount >= 2) &&
                _draftedOverreachSlot.Value.IsOccupied ==
                (payloadCount >= 3);

            bool contentsMatch = true;

            for (int i = 0;
                 i < payloadCount;
                 i++)
            {
                DraftedActionSummary actual =
                    GetDraftedActionSummary(i + 1);

                DraftedActionSummary expected =
                    DraftedActionSummary.FromPayload(
                        _draftedActionPayloads[i]
                    );

                if (!actual.Equals(expected))
                {
                    contentsMatch = false;
                    break;
                }
            }

            if (countMatches &&
                occupancyMatches &&
                contentsMatch)
            {
                return;
            }

            Debug.LogError(
                $"[DRAFT STATE ERROR] operation={operation} | " +
                $"payloads={payloadCount} | " +
                $"counter={_draftedActions.Value} | " +
                $"slot1={_draftedStandardSlot1.Value.IsOccupied} | " +
                $"slot2={_draftedStandardSlot2.Value.IsOccupied} | " +
                $"overreach={_draftedOverreachSlot.Value.IsOccupied}",
                this
            );
        }
        
        public bool TryAddDraftedActionServer(
            DraftedActionPayload payload)
        {
            if (!IsServer || payload == null)
                return false;

            if (_draftedActionPayloads.Count >=
                TurnActionRules.MaximumProductiveActions)
            {
                return false;
            }

            int actionPosition =
                _draftedActionPayloads.Count + 1;

            _draftedActionPayloads.Add(payload);

            SetDraftedActionSummaryServer(
                actionPosition,
                DraftedActionSummary.FromPayload(payload)
            );

            _draftedActions.Value =
                (byte)_draftedActionPayloads.Count;

            ValidateDraftStateServer(
                "add"
            );

            NotifyServerActionPlanChanged();
            
            return true;
        }
        
        public bool TryRemoveLastDraftedActionServer(
            out DraftedActionPayload removedPayload)
        {
            removedPayload = null;

            if (!IsServer ||
                _draftedActionPayloads.Count == 0)
            {
                return false;
            }

            int removedPosition =
                _draftedActionPayloads.Count;

            removedPayload =
                _draftedActionPayloads[
                    removedPosition - 1
                ];

            _draftedActionPayloads.RemoveAt(
                removedPosition - 1
            );

            SetDraftedActionSummaryServer(
                removedPosition,
                DraftedActionSummary.Empty
            );

            _draftedActions.Value =
                (byte)_draftedActionPayloads.Count;

            ValidateDraftStateServer(
                "undo"
            );

            NotifyServerActionPlanChanged();
            
            return true;
        }
        
        public void SetScoreServer(int newScore)
        {
            if (!IsServer) return;
            score.Value = newScore;
        }

        public void AddToScoreServer(int delta)
        {
            if (!IsServer) return;
            score.Value += delta;
        }
        

        public void SetActiveServer(bool newActive)
        {
            if (!IsServer) return;
            isActive.Value = newActive;
        }

        public void SetPlayerEntity(GameEntity entity)
        {
            if (!IsServer) return;

            PlayerEntity = entity;
        }

        public void SetCanDreamServer(bool canDream)
        {
            if (!IsServer)
                return;

            _canDream.Value = canDream;
        }

        public void SetSelectedIdeaSourceServer(
            TagContainerType sourceContainerType)
        {
            if (!IsServer) return;
            _selectedIdeaSource.Value = sourceContainerType;
        }
        
        public void SetHasCommittedTurnServer(
            bool hasCommittedTurn)
        {
            if (!IsServer)
                return;

            _hasCommittedTurn.Value = hasCommittedTurn;
        }
        
        public void PublishCommandFeedbackServer(
            PlayerCommand command,
            PlayerCommandFeedbackStatus status,
            string message)
        {
            if (!IsServer)
                return;

            _commandFeedbackSequence++;

            _latestCommandFeedback.Value =
                PlayerCommandFeedback.Create(
                    _commandFeedbackSequence,
                    command,
                    status,
                    message
                );
        }

        public void SetContextTargetSummaryServer(
            PlayerContextTargetSummary summary)
        {
            if (!IsServer)
                return;

            _contextTargetSummary.Value = summary;
        }

        public void SetDisplayNameServer(
            string newDisplayName)
        {
            if (!IsServer)
            {
                Debug.LogError(
                    $"{nameof(NetPlayerState)}." +
                    $"{nameof(SetDisplayNameServer)}() " +
                    "called on client.",
                    this
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(newDisplayName))
                return;

            displayName.Value =
                new FixedString32Bytes(newDisplayName);
        }
        
        // -----------------------------------------------------------------------------
        // NetworkVariable change callbacks
        // -----------------------------------------------------------------------------
        private void HandleScoreChanged(int oldScore, int newScore)
        {
            // hook ui
        }

        private void HandleIsActiveChanged(bool oldActive, bool newActive)
        {
            // suggestion to dim player on network failure etc...
        }
        
        private void HandleCurrentStanceChanged(
            BandStance oldStance,
            BandStance newStance)
        {
            CurrentStanceChanged?.Invoke(
                oldStance,
                newStance
            );
        }

        // -----------------------------------------------------------------------------
        // Snapshot data types
        // -----------------------------------------------------------------------------
        [System.Serializable]
        public struct LastResolvedRoundData
        {
            public int score;
            public bool isActive;
        }

        // -----------------------------------------------------------------------------
        // Server-authoritative stance mutators
        // -----------------------------------------------------------------------------
        public void SetCurrentStanceServer(BandStance newStance)
        {
            if (!IsServer) return;

            _currentStance.Value = newStance;
        }

        public void StorePreviousStanceServer()
        {
            if (!IsServer)
                return;

            _previousStance.Value = _currentStance.Value;
        }

        public bool IsContinuingSameStance()
        {
            return _currentStance.Value == _previousStance.Value;
        }
        
        public void ReplaceObservedPhysicalEventsServer(
            IReadOnlyList<
                ObservedPhysicalEventSummary
            > events)
        {
            if (!IsServer ||
                ObservedPhysicalEvents == null)
            {
                return;
            }

            ObservedPhysicalEvents.Clear();

            if (events == null)
                return;

            for (int i = 0;
                 i < events.Count;
                 i++)
            {
                ObservedPhysicalEvents.Add(
                    events[i]
                );
            }
        }
        
        private void NotifyServerActionPlanChanged()
        {
            if (!IsServer)
                return;

            ServerActionPlanChanged?.Invoke(
                this
            );
        }

        // -----------------------------------------------------------------------------
        // Server-authoritative action counter mutators
        // -----------------------------------------------------------------------------
        public void IncrementCommittedActionServer()
        {
            if (!IsServer) return;

            if (_committedActions.Value < TurnActionRules.MaximumProductiveActions)
            {
                _committedActions.Value++;
            }
        }

        public void ResetCommittedActionsServer()
        {
            if (!IsServer)
                return;

            _committedActions.Value = 0;

            _committedActionPayloads.Clear();

            NotifyServerActionPlanChanged();
        }

        /*public void IncrementDraftedActionsServer()
        {
            if (!IsServer) return;

            if (_draftedActions.Value < TurnActionRules.MaximumProductiveActions)
                _draftedActions.Value++;
        }

        public void DecrementDraftedActionsServer()
        {
            if (!IsServer) return;

            if (_draftedActions.Value > 0)
                _draftedActions.Value--;
        }
*/
        public void ResetDraftedActionsServer()
        {
            if (!IsServer)
                return;

            _draftedActionPayloads.Clear();

            _draftedActions.Value = 0;

            _draftedStandardSlot1.Value =
                DraftedActionSummary.Empty;

            _draftedStandardSlot2.Value =
                DraftedActionSummary.Empty;

            _draftedOverreachSlot.Value =
                DraftedActionSummary.Empty;

            ValidateDraftStateServer(
                "reset"
            );
            
            NotifyServerActionPlanChanged();
        }


        // -----------------------------------------------------------------------------
        // Server-side action payload mutators
        // -----------------------------------------------------------------------------
        /*public void AddDraftedActionPayloadServer(DraftedActionPayload payload)
        {
            if (payload == null) return;

            _draftedActionPayloads.Add(payload);
        }

        public bool RemoveLastDraftedActionPayloadServer()
        {
            if (_draftedActionPayloads.Count == 0) return false;

            _draftedActionPayloads.RemoveAt(_draftedActionPayloads.Count - 1);
            return true;
        }

        private void ClearDraftedActionPayloadsServer()
        {
            _draftedActionPayloads.Clear();
        }
*/
        public void CommitDraftedActionPayloadsServer()
        {
            if (!IsServer)
                return;

            _committedActionPayloads.Clear();

            foreach (DraftedActionPayload payload
                     in _draftedActionPayloads)
            {
                _committedActionPayloads.Add(payload);
            }

            ResetDraftedActionsServer();
        }


        // -----------------------------------------------------------------------------
        // Debug / diagnostics
        // -----------------------------------------------------------------------------
        [ContextMenu("Debug Linked PlayerEntity")]
        private void DebugLinkedPlayerEntity()
        {
            if (PlayerEntity == null)
            {
                Debug.Log("[LINK CHECK] NetPlayerState.PlayerEntity is NULL", this);
                return;
            }

            Debug.Log(
                $"[LINK CHECK] NetPlayerState.PlayerEntity = {PlayerEntity.DisplayName} " +
                $"id={PlayerEntity.EntityId} type={PlayerEntity.EntityType}",
                this
            );

            var activeSet = PlayerEntity.GetActiveVhsSet();

            if (activeSet == null)
            {
                Debug.Log("[LINK CHECK] Active VHS set is NULL", this);
                return;
            }

            Debug.Log(
                $"[LINK CHECK] Active VHS set exists: {activeSet.DisplayName}",
                this
            );

            Debug.Log($"[SET CHECK] Runtime type = {activeSet.GetType().FullName}", this);

            foreach (var field in activeSet.GetType().GetFields(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value = field.GetValue(activeSet);

                Debug.Log(
                    $"[SET CHECK] FIELD {field.Name} | type={field.FieldType.Name} | value={value}",
                    this
                );
            }

            foreach (var prop in activeSet.GetType().GetProperties(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                object value;

                try
                {
                    value = prop.GetValue(activeSet);
                }
                catch
                {
                    value = "[unreadable]";
                }

                Debug.Log(
                    $"[SET CHECK] PROPERTY {prop.Name} | type={prop.PropertyType.Name} | value={value}",
                    this
                );
            }
        }

#if UNITY_EDITOR
        [ContextMenu(
            "Debug Observed Physical Events"
        )]
        private void DebugObservedPhysicalEvents()
        {
            int count =
                ObservedPhysicalEvents?.Count ?? 0;

            Debug.Log(
                "[OBSERVED PHYSICAL EVENTS] " +
                $"owner={OwnerClientIdCached} | " +
                $"isOwner={IsOwner} | " +
                $"count={count}",
                this
            );

            if (ObservedPhysicalEvents == null)
                return;

            for (int i = 0;
                 i < ObservedPhysicalEvents.Count;
                 i++)
            {
                ObservedPhysicalEventSummary summary =
                    ObservedPhysicalEvents[i];

                Debug.Log(
                    "[OBSERVED PHYSICAL EVENT] " +
                    $"index={i} | " +
                    $"event={summary.EventId} | " +
                    $"owner={summary.OwnerClientId} | " +
                    $"action={summary.ActionType} | " +
                    $"node={summary.PhysicalNodeId} | " +
                    $"turn={summary.CreatedTurn} | " +
                    $"position={summary.ActionPosition} | " +
                    $"state={summary.PlanState}",
                    this
                );
            }
        }
        
        [ContextMenu("Debug Dream Availability")]
        private void DebugDreamAvailability()
        {
            ulong localClientId =
                NetworkManager != null
                    ? NetworkManager.LocalClientId
                    : ulong.MaxValue;

            Debug.Log(
                $"[DREAM AVAILABILITY] " +
                $"owner={OwnerClientIdCached} " +
                $"local={localClientId} " +
                $"isOwner={IsOwner} " +
                $"canDream={CanDreamValue}",
                this
            );
        }
        
        [ContextMenu("Debug Selected Idea Source")]
        private void DebugSelectedIdeaSource()
        {
            Debug.Log(
                $"[IDEA SOURCE] " +
                $"owner={OwnerClientIdCached} " +
                $"isOwner={IsOwner} " +
                $"source={SelectedIdeaSourceValue}",
                this
            );
        }
#endif
    }
}