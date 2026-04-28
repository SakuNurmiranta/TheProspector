using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using SEMM91.GamePlay;

namespace SEMM91.Networking
{
    /// <summary>
    /// A place holding and host-authoritative player state for the networking demo.
    /// - Host: tallies current player traits
    /// - Client: Update on what is read + local behaviours
    /// </summary>
    public class NetPlayerState : NetworkBehaviour
    {
        public Dictionary<ulong, LastResolvedRoundData> lastResolvedRound = new (); 
        
        // --Network
        
        // a logical index for players
        public NetworkVariable<int> PlayerIndex = new NetworkVariable<int>(
            -1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // suggested for UI labels and debugging
        public NetworkVariable<FixedString32Bytes> DisplayName = new NetworkVariable<FixedString32Bytes>(
            new FixedString32Bytes("Player"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // -Core traits, the real deal
        
        // The accumulated score for each player (derived from record influence in the future)
        public NetworkVariable<int> Score = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // Just a flag to have something else to track in the game loop (has a real world equivalent in 3 of the rule of 2:3)
        public NetworkVariable<bool> IsExhausted = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        // The player is flagged down here if there is a network failure etc...
        public NetworkVariable<bool> IsActive = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<BandStance> currentStance = new(BandStance.None);
        private readonly NetworkVariable<BandStance> previousStance = new(BandStance.None);
        public BandStance CurrentStanceValue => currentStance.Value;
        public BandStance PreviousStanceValue => previousStance.Value;
        
        private readonly NetworkVariable<byte> actionsUsed = new(0);
        private readonly NetworkVariable<byte> maxFreeActions = new(2);
        private readonly NetworkVariable<byte> productiveActionsUsed = new(0);
        
        private readonly NetworkVariable<byte> draftedProductiveActions = new(0);
        public byte ProductiveActionsUsedValue => productiveActionsUsed.Value;
        
        public byte ActionsUsedValue => actionsUsed.Value;
        public byte MaxFreeActionsValue => maxFreeActions.Value;
        
        public byte DraftedProductiveActionsValue => draftedProductiveActions.Value;
        
        // Computer suggests having this, don't know what it does yet.
        public ulong OwnerClientIdCached { get; private set; } = ulong.MaxValue;
        
        // --Properties of convenience
        
        public int ScoreValue => Score.Value;
        public bool ExhaustedValue => IsExhausted.Value;
        public bool ActiveValue => IsActive.Value;
        public int IndexValue => PlayerIndex.Value;
        public string DisplayNameStr => DisplayName.Value.ToString();

        
        // --Lifecycle

        public override void OnNetworkSpawn()
        {
            OwnerClientIdCached = OwnerClientId;

            Score.OnValueChanged += HandleScoreChanged;
            IsExhausted.OnValueChanged += HandleIsExhaustedChanged;
            IsActive.OnValueChanged += HandleIsActiveChanged;
        }

        private void OnDestroy()
        {
            Score.OnValueChanged -= HandleScoreChanged;
            IsExhausted.OnValueChanged -= HandleIsExhaustedChanged;
            IsActive.OnValueChanged -= HandleIsActiveChanged;
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
            
            PlayerIndex.Value = playerIndex;
            DisplayName.Value = new FixedString32Bytes(displayName);
            
            // Default values when fresh
            Score.Value = 0;
            IsExhausted.Value = false;
            IsActive.Value = true;
        }
        
        // --Server-side helpers (strengthens encapsulation and streamlines calls)
        
        public void SetScoreServer(int newScore)
        {
            if (!IsServer) return;
            Score.Value = newScore;
        }

        public void AddToScoreServer(int delta)
        {
            if (!IsServer) return;
            Score.Value += delta;
        }

        public void SetExhaustedServer(bool newExhausted)
        {
            if (!IsServer) return;
            IsExhausted.Value = newExhausted;
        }

        public void SetActiveServer(bool newActive)
        {
            if (!IsServer) return;
            IsActive.Value = newActive;
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
            
            currentStance.Value = newStance;
        }
        public void StorePreviousStanceServer()
        {
            if (!IsServer)
                return;

            previousStance.Value = currentStance.Value;
        }
        public bool IsContinuingSameStance()
        {
            return currentStance.Value == previousStance.Value;
        }
        
        public void IncrementActionsUsedServer()
        {
            if (!IsServer) return;

            if (actionsUsed.Value < 3)
                actionsUsed.Value++;
        }
        public void ResetActionsUsedServer()
        {
            if (!IsServer) return;
            
            actionsUsed.Value = 0;
        }

        public void IncrementProductiveActionsUsedServer()
        {
            if (!IsServer) return;

            if (productiveActionsUsed.Value < 3)
            {
                productiveActionsUsed.Value++;
            }
        }

        public void ResetProductiveActionsUsedServer()
        {
            if (!IsServer) return;
            
            productiveActionsUsed.Value = 0;
        }
        
        public void IncrementDraftedProductiveActionServer()
        {
            if (!IsServer) return;

            if (draftedProductiveActions.Value < 3)
                draftedProductiveActions.Value++;
        }
       
        public void ResetDraftedProductiveActionsServer()
        {
            if (!IsServer) return;

            draftedProductiveActions.Value = 0;
        }
    }
}
