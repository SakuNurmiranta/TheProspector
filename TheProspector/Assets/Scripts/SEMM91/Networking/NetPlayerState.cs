using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Reflection;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.Actions;
using UnityEngine.Serialization;

namespace SEMM91.Networking
{
    /// <summary>
    /// A place holding and host-authoritative player state for the networking demo.
    /// - Host: tallies current player traits
    /// - Client: Update on what is read + local behaviours
    /// </summary>
    public class NetPlayerState : NetworkBehaviour
    {
        public Dictionary<ulong, LastResolvedRoundData> LastResolvedRound = new (); 
        public GameEntity PlayerEntity { get; set;}
        // --Network
        
        // a logical index for players
        [FormerlySerializedAs("PlayerIndex")] public NetworkVariable<int> playerIndex = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // suggested for UI labels and debugging
        [FormerlySerializedAs("DisplayName")] public NetworkVariable<FixedString32Bytes> displayName = new NetworkVariable<FixedString32Bytes>(
            new FixedString32Bytes("Player"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // -Core traits, the real deal
        
        // The accumulated score for each player (derived from record influence in the future)
        [FormerlySerializedAs("Score")] public NetworkVariable<int> score = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // Just a flag to have something else to track in the game loop (has a real world equivalent in 3 of the rule of 2:3)
        [FormerlySerializedAs("IsExhausted")] public NetworkVariable<bool> isExhausted = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // The player is flagged down here if there is a network failure etc...
        [FormerlySerializedAs("IsActive")] public NetworkVariable<bool> isActive = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<BandStance> _currentStance = new(BandStance.None);
        private readonly NetworkVariable<BandStance> _previousStance = new(BandStance.None);
        public BandStance CurrentStanceValue => _currentStance.Value;
        public BandStance PreviousStanceValue => _previousStance.Value;
        

        
        private readonly NetworkVariable<byte> _committedActions = new(0);
        private readonly NetworkVariable<byte> _draftedActions = new(0);
        
        private readonly List<DraftedActionPayload> draftedActionPayloads = new();
        public IReadOnlyList<DraftedActionPayload> DraftedActionPayloads => draftedActionPayloads;
        
        private readonly List<DraftedActionPayload> committedActionPayloads = new();
        public IReadOnlyList<DraftedActionPayload> CommittedActionPayloads => committedActionPayloads;
        
        public byte CommittedActionsValue => _committedActions.Value;

        
        public byte DraftedActionsValue => _draftedActions.Value;
        
        public ulong OwnerClientIdCached { get; private set; } = ulong.MaxValue;
        
        // --Properties of convenience
        
        public int ScoreValue => score.Value;
        public bool ExhaustedValue => isExhausted.Value;
        public bool ActiveValue => isActive.Value;
        public int IndexValue => playerIndex.Value;
        public string DisplayNameStr => displayName.Value.ToString();

        
        // --Lifecycle

        public override void OnNetworkSpawn()
        {
            OwnerClientIdCached = OwnerClientId;

            score.OnValueChanged += HandleScoreChanged;
            isExhausted.OnValueChanged += HandleIsExhaustedChanged;
            isActive.OnValueChanged += HandleIsActiveChanged;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            score.OnValueChanged -= HandleScoreChanged;
            isExhausted.OnValueChanged -= HandleIsExhaustedChanged;
            isActive.OnValueChanged -= HandleIsActiveChanged;
        }
        
        // --Server-side initialization
        
        /// <summary>
        /// Called by the host after player spawn.
        /// </summary>
        
        public void InitializeServer(int playerIndex, string displayName)
        {
            if (!IsServer)
            {
                Debug.LogError($"{nameof(NetPlayerState)}.InitializeServer() called on client.");
                return;
            }
            
            this.playerIndex.Value = playerIndex;
            this.displayName.Value = new FixedString32Bytes(displayName);
            
            // Default values when fresh
            score.Value = 0;
            isExhausted.Value = false;
            isActive.Value = true;
        }
        
        // --Server-side helpers (strengthens encapsulation and streamlines calls)
        
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

        public void SetExhaustedServer(bool newExhausted)
        {
            if (!IsServer) return;
            isExhausted.Value = newExhausted;
        }

        public void SetActiveServer(bool newActive)
        {
            if (!IsServer) return;
            isActive.Value = newActive;
        }
        
        // --callback switches
        private void HandleScoreChanged(int oldScore, int newScore)
        {
            // hook ui
        }

        private void HandleIsExhaustedChanged(bool oldExhausted, bool newExhausted)
        {
            // suggestion to grey out end turn when true
        }
        
        private void HandleIsActiveChanged(bool oldActive, bool newActive)
        {
            // suggestion to dim player on network failure etc...
        }

        // A yearly snapshot of per-player data
        [System.Serializable]
        public struct LastResolvedRoundData
        {
            public int Score;
            public bool IsActive;
        }
        
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
        
        public void SetPlayerEntity(GameEntity entity)
        {
            if (!IsServer) return;

            PlayerEntity = entity;
        }

        public void AddDraftedActionPayloadServer(DraftedActionPayload payload)
        {
            if (payload == null) return;
            
            draftedActionPayloads.Add(payload);
        }

        public bool RemoveLastDraftedActionPayloadServer()
        {
            if (draftedActionPayloads.Count == 0) return false;
            
            draftedActionPayloads.RemoveAt(draftedActionPayloads.Count - 1);
            return true;
        }
        
        public void ClearDraftedActionPayloadsServer()
        {
            draftedActionPayloads.Clear();
        }

        public void CommitDraftedActionPayloadsServer()
        {
            committedActionPayloads.Clear();

            foreach (DraftedActionPayload payload in draftedActionPayloads)
            {
                committedActionPayloads.Add(payload);
            }

            draftedActionPayloads.Clear();
        }

        public void ClearCommittedActionPayloadsServer()
        {
            committedActionPayloads.Clear();
        }
        
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

                object value = null;

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
        
    }
}
