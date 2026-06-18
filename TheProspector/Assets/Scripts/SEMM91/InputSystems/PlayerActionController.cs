using SEMM91;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.InputSystems
{
    /// <summary>
    /// Networked player-will router.
    ///
    /// Receives player/bot gameplay requests and routes them through the existing authorititative systems. </summary>
    ///

    public class PlayerActionController : NetworkBehaviour
    {
        [Header("Debug")] 
        //[SerializeField] private bool logRequests = true;
        [SerializeField] private bool logAcceptedCommands = false;
        [SerializeField] private bool logRejectedCommands = true;
        
        public void RequestDream()
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitDreamServerRpc();
        }
        public void RequestDraftAction()
        {
            if (!IsOwner || !IsClient) return;

            SubmitDraftActionServerRpc();
        }
        
        public void RequestUndoDraftAction()
        {
            if (!IsOwner || !IsClient) return;

            SubmitUndoDraftActionServerRpc();
        }

        public void RequestSelectStance(BandStance stance)
        {
            if (!IsOwner || !IsClient) return;

            SubmitStanceServerRpc(stance);
        }

        public void RequestCommitTurn()
        {
            if (!IsOwner || !IsClient) return;

            SubmitCommitTurnServerRpc();
        }

        public void RequestCycleActiveVhsSet()
        {
            if (!IsOwner || !IsClient) return;

            SubmitCycleActiveVhsSetServerRpc();
        }

        private void RequestQuitSession()
        {
            var coordinator = GameCoordinator.Instance;

            if (coordinator != null)
            {
                coordinator.BeginShutdown();
            }
            else
            {
                ShutdownNetworkAndQuit();
            }
        }

        // CanRequest is used for UI buttons (are they in available state or not?)
        public bool CanRequest(PlayerCommand command)
        {
            if (!IsOwner || !IsClient) return false;
            
            var state = GetComponent<NetPlayerState>();
            ulong clientId = OwnerClientId;

            switch (command)
            {
                case PlayerCommand.Dream:
                    return state != null &&
                           state.CanDreamValue;
                
                case PlayerCommand.SelectGestate:
                case PlayerCommand.SelectRehearse:
                case PlayerCommand.SelectPromote:
                    return CanChangeStance(clientId, state);

                case PlayerCommand.DraftAction:
                case PlayerCommand.DraftPrimaryAction:
                case PlayerCommand.DraftSecondaryAction:
                case PlayerCommand.DraftTertiaryAction:
                case PlayerCommand.DraftRestAction:
                    return CanDraftAction(clientId, state);

                case PlayerCommand.UndoDraftAction:
                    return CanUndoDraftAction(clientId, state);

                case PlayerCommand.CommitTurn:
                    return CanCommitTurn(clientId, state);

                case PlayerCommand.AdminCycleActiveRehearsalSet:
                    return CanCycleActiveVhsSet(clientId, state);

                default:
                    return false;
            }
        }
        
        public void Request(PlayerCommand command)
        {
            switch (command)
            {
                case PlayerCommand.Dream:
                    RequestDream();
                    break;
                
                case PlayerCommand.SelectGestate:
                    RequestSelectStance(BandStance.Gestate);
                    break;

                case PlayerCommand.SelectRehearse:
                    RequestSelectStance(BandStance.Rehearse);
                    break;

                case PlayerCommand.SelectPromote:
                    RequestSelectStance(BandStance.Promote);
                    break;

                case PlayerCommand.DraftAction:
                    RequestDraftAction();
                    break;

                case PlayerCommand.UndoDraftAction:
                    RequestUndoDraftAction();
                    break;

                case PlayerCommand.CommitTurn:
                    RequestCommitTurn();
                    break;

                case PlayerCommand.AdminCycleActiveRehearsalSet:
                    RequestCycleActiveVhsSet();
                    break;
                
                case PlayerCommand.AdminCreateEmptyRehearsalSet:
                    RequestAdminCreateEmptyRehearsalSet();
                    break;
                
                case PlayerCommand.QuitSession:
                    RequestQuitSession();
                    break;
                
                case PlayerCommand.DraftPrimaryAction:
                    RequestDraftStanceSlotAction(1);
                    break;
                
                case PlayerCommand.DraftSecondaryAction:
                    RequestDraftStanceSlotAction(2);
                    break;
                
                case PlayerCommand.DraftTertiaryAction:
                    RequestDraftStanceSlotAction(3);
                    break;
                
                case PlayerCommand.DraftRestAction:
                    RequestDraftRestAction();
                    break;
            }
        }
        private void RequestAdminCreateEmptyRehearsalSet()
        {
            SubmitAdminCreateEmptyRehearsalSetServerRpc();
        }
        private void RequestDraftRestAction()
        {
           RequestDraftStanceSlotAction(0);
        }
        private void RequestDraftStanceSlotAction(int slotIndex)
        {
            SubmitDraftStanceSlotActionServerRpc(slotIndex);
        }
        
        private bool CanCycleActiveVhsSet(ulong clientId, NetPlayerState state)
        {
            if (GameCoordinator.Instance == null)
            {
                return false;
            }

            if (state == null)
            {
                return false;
            }

            if (GameCoordinator.Instance.HasPlayerActed(clientId))
            {
                return false;
            }

            if (!state.ActiveValue)
            {
                return false;
            }

            if (state.PlayerEntity == null)
            {
                return false;
            }

            return state.PlayerEntity.VhsSets.Count > 0;
        }
        
        [ServerRpc]
        private void SubmitDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            var state = GetComponent<NetPlayerState>();
            // var coordinator = GameCoordinator.Instance;
            if (!CanDraftAction(clientId,state))
            {
                LogRejected($"Draft action request from client {clientId} rejected.");
                return;
            }
            
            if (state == null)
            {
                LogRejected($"Draft action blocked for client {clientId}: missing NetPlayerState.");
                return;
            }
            
            DraftedActionPayload payload = CreatePayloadForCurrentStance(state);

            if (payload == null)
            {
                LogRejected($"Draft action blocked for client {clientId}: no current stance.");
                return;
            }
            
            state.AddDraftedActionPayloadServer(payload);
            state.IncrementDraftedActionsServer();

            LogAccepted($"Client {clientId} added productive action ({state.DraftedActionsValue}/3)");

        }
        
        [ServerRpc]
        private void SubmitUndoDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            
            var state = GetComponent<NetPlayerState>();
            if (!CanUndoDraftAction(clientId, state))
            {
                LogRejected($"Undo draft request from client {clientId} rejected.");
                return;
            }


            state.RemoveLastDraftedActionPayloadServer();
            state.DecrementDraftedActionsServer();
            LogAccepted($"Client {clientId} removed action ({state.DraftedActionsValue}/3)");
        }
        
        [ServerRpc]
        private void SubmitStanceServerRpc(BandStance stance, ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (!CanChangeStance(clientId, state))
            {
                LogRejected($"Stance selection blocked for client {clientId}: {stance}.");
                return;
            }

            state.SetCurrentStanceServer(stance);

            LogAccepted($"Client {clientId} selected {stance}.");
        }

        [ServerRpc]
        private void SubmitCommitTurnServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (!CanCommitTurn(clientId, state))
            {
                LogRejected($"Commit turn request from client {clientId} rejected.");
                return;
            }
            
            Debug.Log(
                $"[COMMIT REQUEST] client={clientId} stance={state.CurrentStanceValue} " +
                $"drafted={state.DraftedActionsValue}"
            );
            
            CommitDraftToState(clientId, state);
            
            Debug.Log(
                $"[COMMIT AFTER DRAFT TRANSFER] client={clientId} " +
                $"committed={state.CommittedActionsValue} payloads={state.CommittedActionPayloads.Count}"
            );
            
            var coordinator = GameCoordinator.Instance;
            if (coordinator == null)
            {
                LogRejected($"Commit turn blocked for client {clientId}: missing GameCoordinator.");
                return;
            }

            Debug.Log($"[COMMIT BEFORE COMPLETE TURN] client={clientId}");
            coordinator.CompleteCommittedTurn(clientId,state);

            LogAccepted($"Client {clientId} requested turn commit.");
        }

  
        [ServerRpc]
        private void SubmitDreamServerRpc(
            ServerRpcParams p = default)
        {
            ulong clientId =
                p.Receive.SenderClientId;

            Debug.Log(
                $"[DREAM RPC] client={clientId}",
                this
            );

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                LogRejected(
                    $"Dream request from client {clientId} rejected: " +
                    "missing GameCoordinator."
                );

                return;
            }

            bool success =
                coordinator.TryResolveDreamServer(
                    clientId,
                    out _,
                    out _,
                    out string failureReason
                );

            if (!success)
            {
                LogRejected(
                    $"Dream request from client {clientId} rejected: " +
                    failureReason
                );

                return;
            }

            LogAccepted(
                $"Client {clientId} resolved Dream."
            );
        }
        
        [ServerRpc]
        private void SubmitCycleActiveVhsSetServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (state == null)
            {
                LogRejected($"Cycle VHS set request from client {clientId} rejected: missing NetPlayerState.");
                return;
            }

            if (GameCoordinator.Instance == null || GameCoordinator.Instance.HasPlayerActed(clientId))
            {
                LogRejected($"Cycle VHS set request from client {clientId} rejected: player has already committed or coordinator is missing.");
                return;
            }

            if (!state.ActiveValue)
            {
                LogRejected($"Cycle VHS set request from client {clientId} rejected: player is inactive.");
                return;
            }

            GameEntity playerEntity = state.PlayerEntity;

            if (playerEntity == null)
            {
                LogRejected($"Cycle VHS set request from client {clientId} rejected: missing player entity.");
                return;
            }

            if (!playerEntity.CycleActiveVhsSet())
            {
                LogRejected($"Cycle VHS set request from client {clientId} rejected: no alternate VHS set available.");
                return;
            }

            RehearsalSet activeSet = playerEntity.GetActiveVhsSet();

            LogAccepted(
                $"Client {clientId} switched active VHS set to {(activeSet != null ? activeSet.DisplayName : "none")}."
            );
        }

        private bool CanCommitTurn(ulong clientId, NetPlayerState state)
        {
            var coordinator = GameCoordinator.Instance;
            
            return coordinator != null 
                && state != null 
                && !coordinator.HasPlayerActed(clientId) 
                && state.ActiveValue
                && state.CurrentStanceValue != BandStance.None;
        }

        private void CommitDraftToState(ulong clientId, NetPlayerState state)
        {
            byte drafted = state.DraftedActionsValue;

            state.ResetCommittedActionsServer();

            for (int i = 0; i < drafted; i++)
            {
                state.IncrementCommittedActionServer();
            }

            state.CommitDraftedActionPayloadsServer();
            
            if (state.CommittedActionsValue >= 3)
            {
                state.SetExhaustedServer(true);
                LogAccepted($"Client {clientId} overexerted by committing a third productive action.");
            }

            state.ResetDraftedActionsServer();
        }
        
        private bool CanChangeStance(ulong clientId, NetPlayerState state)
        {
            var coordinator = GameCoordinator.Instance;

            return coordinator != null
                   && state != null
                   && !coordinator.HasPlayerActed(clientId)
                   && state.ActiveValue
                   && state.DraftedActionsValue == 0;
        }

        private bool CanDraftAction(ulong clientId, NetPlayerState state)
        {
            var coordinator = GameCoordinator.Instance;

            return coordinator != null
                   && state != null
                   && !coordinator.HasPlayerActed(clientId)
                   && state.ActiveValue
                   && state.CurrentStanceValue != BandStance.None
                   && state.DraftedActionsValue < 3;
        }
        
        private bool CanUndoDraftAction(ulong clientId, NetPlayerState state)
        {
            var coordinator = GameCoordinator.Instance;

            return coordinator != null
                   && state != null
                   && !coordinator.HasPlayerActed(clientId)
                   && state.ActiveValue
                   && state.DraftedActionsValue > 0;
        }
        
        private void LogAccepted(string message)
        {
            if (!logAcceptedCommands)
                return;

            Debug.Log($"[PlayerActionController] ACCEPTED: {message}", this);
        }
        
        private void LogRejected(string message)
        {
            if (!logRejectedCommands)
                return;

            Debug.LogWarning($"[PlayerActionController] REJECTED: {message}", this);
        }
        
        private DraftedActionPayload CreatePayloadForCurrentStance(NetPlayerState state)
        {
            if (state == null)
                return null;

            int currentTurn = GameCoordinator.Instance != null
                ? GameCoordinator.Instance.globalTurn.Value
                : 0;

            return state.CurrentStanceValue switch
            {
                BandStance.Gestate => new DraftedActionPayload(
                    DraftedActionType.CreateIdea,
                    currentTurn
                ),

                BandStance.Rehearse => new DraftedActionPayload(
                    DraftedActionType.RehearseActiveSet,
                    currentTurn
                ),

                BandStance.Promote => null,

                _ => null
            };
        }
        
        private void ShutdownNetworkAndQuit()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        [ServerRpc]
        private void SubmitDraftStanceSlotActionServerRpc(
            int slotIndex,
            ServerRpcParams p = default)
        { 
            ulong clientId = p.Receive.SenderClientId;
            
            Debug.Log($"[SLOT REQUEST] client={clientId} slot={slotIndex}");
            
            var state = GetComponent<NetPlayerState>();
            
            
            if (state == null)
            {
                LogRejected($"Draft slot {slotIndex} rejected for client {clientId}: missing NetPlayerState.");
                return;
            }

            if (!TryGetDraftedActionTypeForSlot(
                    state.CurrentStanceValue,
                    slotIndex,
                    out DraftedActionType actionType,
                    out bool isImmediate))
            {
                LogRejected(
                    $"Draft slot {slotIndex} rejected for client {clientId}: " +
                    $"no action for stance {state.CurrentStanceValue}."
                );
                return;
            }

            Debug.Log(
                $"[SLOT RESOLVED] client={clientId} stance={state.CurrentStanceValue} " +
                $"slot={slotIndex} actionType={actionType} immediate={isImmediate}"
            );

            if (isImmediate)
            {
                ResolveImmediateSlotAction(clientId, state, actionType);
                return;
            }

            if (!CanDraftSpecificAction(state, actionType))
            {
                LogRejected($"Draft slot {slotIndex} rejected for client {clientId}: cannot draft {actionType}.");
                return;
            }
            
            DraftSpecificAction(state, actionType);
            
            LogAccepted(
                $"Client {clientId} drafted slot {slotIndex}: {actionType} " +
                $"({state.DraftedActionsValue}/3)."
            );
        }
        
        private bool TryGetDraftedActionTypeForSlot(
            BandStance stance,
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            if (slotIndex == 0)
            {
                actionType = DraftedActionType.Rest;
                return stance != BandStance.None;
            }
            
            switch (stance)
            {
                case BandStance.Gestate:
                    return TryGetGestateSlotAction(slotIndex, out actionType, out isImmediate);

                case BandStance.Rehearse:
                    return TryGetRehearseSlotAction(slotIndex, out actionType, out isImmediate);

                case BandStance.Promote:
                    return TryGetPromoteSlotAction(slotIndex, out actionType, out isImmediate);
                    
                default:
                    return false;
            }
        }
        
        private bool TryGetGestateSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.CreateIdea;
                    return true;

                case 2:
                    actionType = DraftedActionType.DebugPlaceholderGestationSecondary;
                    return true;

                case 3:
                    actionType = DraftedActionType.DebugCycleActiveRehearsalSet;
                    isImmediate = true;
                    return true;

                default:
                    return false;
            }
        }
        
        private bool TryGetRehearseSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.RehearseActiveSet;
                    return true;

                case 2:
                    actionType = DraftedActionType.RecordActiveSetToDemo;
                    return true;

                case 3:
                    actionType = DraftedActionType.None;
                    return false;

                default:
                    return false;
            }
        }
        
        private bool TryGetPromoteSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.ReleaseLatestDemoToKvlt;
                    return true;

                case 2:
                    actionType = DraftedActionType.DebugPlaceholderPromotionSecondary;
                    return true;

                case 3:
                    actionType = DraftedActionType.None;
                    isImmediate = true;
                    return true;

                default:
                    return false;
            }
        }
        
        private void DraftSpecificAction(
            NetPlayerState state,
            DraftedActionType actionType)
        {
            int currentTurn = GameCoordinator.Instance != null
                ? GameCoordinator.Instance.globalTurn.Value
                : 0;

            var payload = new DraftedActionPayload(actionType, currentTurn);

            state.AddDraftedActionPayloadServer(payload);
            state.IncrementDraftedActionsServer();
        }
        
        private bool CanDraftSpecificAction(
            NetPlayerState state,
            DraftedActionType actionType)
        {
            if (state == null)
                return false;

            if (state.CurrentStanceValue == BandStance.None)
                return false;

            if (state.DraftedActionsValue >= 3)
                return false;

            if (actionType == DraftedActionType.None)
                return false;

            return actionType switch
            {
                DraftedActionType.CreateIdea =>
                    state.CurrentStanceValue == BandStance.Gestate,

                DraftedActionType.DebugPlaceholderGestationSecondary =>
                    state.CurrentStanceValue == BandStance.Gestate,

                DraftedActionType.RehearseActiveSet =>
                    state.CurrentStanceValue == BandStance.Rehearse,

                DraftedActionType.CreateNewRehearsalSet =>
                    state.CurrentStanceValue == BandStance.Rehearse,
                
                DraftedActionType.RecordActiveSetToDemo =>
                    state.CurrentStanceValue == BandStance.Rehearse,

                DraftedActionType.DebugPlaceholderPromotionPrimary =>
                    state.CurrentStanceValue == BandStance.Promote,

                DraftedActionType.DebugPlaceholderPromotionSecondary =>
                    state.CurrentStanceValue == BandStance.Promote,
                
                DraftedActionType.Rest => 
                    state.CurrentStanceValue != BandStance.None,
                
                DraftedActionType.ReleaseLatestDemoToKvlt => 
                    state.CurrentStanceValue == BandStance.Promote,

                _ => false
            };
        }
        
        private void ResolveImmediateSlotAction(
            ulong clientId,
            NetPlayerState state,
            DraftedActionType actionType)
        {
            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    LogAccepted($"Client {clientId} used Gestate tertiary placeholder action.");
                    break;

                case BandStance.Rehearse:
                    //RequestDebugCycleActiveRehearsalSet();
                    break;

                case BandStance.Promote:
                    LogAccepted($"Client {clientId} used Promote tertiary placeholder switch action.");
                    break;

                default:
                    LogRejected($"Immediate slot action rejected for client {clientId}: no valid stance.");
                    break;
            }
        }
        
        [ServerRpc]
        private void SubmitAdminCreateEmptyRehearsalSetServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            var state = GetComponent<NetPlayerState>();

            if (state == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing state.");
                return;
            }
            
            if (state.PlayerEntity == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing controller entity.");
                return;
            }

            var coordinator = GameCoordinator.Instance;

            if (coordinator == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing coordinator.");
                return;
            }
            
            if (coordinator.RehearsalRehearsalResolver == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing rehearsal resolver.");
                return;
            }
            
            bool success = coordinator.RehearsalRehearsalResolver.TryCreateNewActiveEmptyVhsSet(
                clientId,
                state.PlayerEntity,
                out string message
            );


            if (success)
                LogAccepted(message);
            else
                LogRejected(message);
        }
        
#if UNITY_EDITOR
        [ContextMenu("Debug/Request Pajazzo Dream")]
        private void DebugRequestPajazzoDream()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] Enter Play Mode first.",
                    this
                );

                return;
            }

            if (!IsClient)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] This controller is not running " +
                    "as a client.",
                    this
                );

                return;
            }

            if (!IsOwner)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] This controller is not locally owned.",
                    this
                );

                return;
            }

            RequestDream();
        }
#endif
        
        private void DebugCycleActiveRehearsalSetServer(
            ulong clientId,
            NetPlayerState state)
        {
            // old Alpha4 server-side behavior here
        }
    }
}