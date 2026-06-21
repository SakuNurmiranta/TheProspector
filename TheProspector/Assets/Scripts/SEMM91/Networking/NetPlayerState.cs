using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Reflection;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions;

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
            

        // -----------------------------------------------------------------------------
        // Network-visible stance state
        // -----------------------------------------------------------------------------

        private readonly NetworkVariable<BandStance> _currentStance = new();
        private readonly NetworkVariable<BandStance> _previousStance = new();
        public BandStance CurrentStanceValue => _currentStance.Value;
        public BandStance PreviousStanceValue => _previousStance.Value;


        // -----------------------------------------------------------------------------
        // Network-visible action counters
        // -----------------------------------------------------------------------------

        private readonly NetworkVariable<byte> _committedActions = new();
        private readonly NetworkVariable<byte> _draftedActions = new();
        public byte CommittedActionsValue => _committedActions.Value;
        public byte DraftedActionsValue => _draftedActions.Value;


        // -----------------------------------------------------------------------------
        // Server-side action payload buffers
        // -----------------------------------------------------------------------------
        // These are not replicated NetworkVariables; they are filled by server-side
        // RPC handling and consumed by GameCoordinator during turn commit.
        private readonly List<DraftedActionPayload> _draftedActionPayloads = new();
        private readonly List<DraftedActionPayload> _committedActionPayloads = new();
        public IReadOnlyList<DraftedActionPayload> DraftedActionPayloads => _draftedActionPayloads;
        public IReadOnlyList<DraftedActionPayload> CommittedActionPayloads => _committedActionPayloads;


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
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            score.OnValueChanged -= HandleScoreChanged;
            isActive.OnValueChanged -= HandleIsActiveChanged;
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

            playerIndex.Value = initPlayerIndex;
            displayName.Value = new FixedString32Bytes(initDisplayName);

            // Default values when fresh
            score.Value = 0;
            isActive.Value = false;
            _canDream.Value = false;
            _selectedIdeaSource.Value = TagContainerType.Conviction;
            _hasCommittedTurn.Value = false;
        }

        // -----------------------------------------------------------------------------
        // Server-authoritative player-state mutators
        // -----------------------------------------------------------------------------

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

        // -----------------------------------------------------------------------------
        // Server-authoritative action counter mutators
        // -----------------------------------------------------------------------------
        public void IncrementCommittedActionServer()
        {
            if (!IsServer) return;

            if (_committedActions.Value < 3)
            {
                _committedActions.Value++;
            }
        }

        public void ResetCommittedActionsServer()
        {
            if (!IsServer) return;

            _committedActions.Value = 0;
        }

        public void IncrementDraftedActionsServer()
        {
            if (!IsServer) return;

            if (_draftedActions.Value < 3)
                _draftedActions.Value++;
        }

        public void DecrementDraftedActionsServer()
        {
            if (!IsServer) return;

            if (_draftedActions.Value > 0)
                _draftedActions.Value--;
        }

        public void ResetDraftedActionsServer()
        {
            if (!IsServer) return;

            _draftedActions.Value = 0;
        }


        // -----------------------------------------------------------------------------
        // Server-side action payload mutators
        // -----------------------------------------------------------------------------
        public void AddDraftedActionPayloadServer(DraftedActionPayload payload)
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

        public void CommitDraftedActionPayloadsServer()
        {
            ClearCommittedActionPayloadsServer();

            foreach (DraftedActionPayload payload in _draftedActionPayloads)
            {
                _committedActionPayloads.Add(payload);
            }

            ClearDraftedActionPayloadsServer();
        }

        private void ClearCommittedActionPayloadsServer()
        {
            _committedActionPayloads.Clear();
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