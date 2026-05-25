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
        [Header("Debug")] [SerializeField] private bool logRequests = true;

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

        public bool CanRequest(PlayerCommand command)
        {
            if (!IsOwner || !IsClient) return false;
            
            var state = GetComponent<NetPlayerState>();
            ulong clientId = OwnerClientId;

            switch (command)
            {
                case PlayerCommand.SelectGestate:
                case PlayerCommand.SelectRehearse:
                case PlayerCommand.SelectPromote:
                    return CanChangeStance(clientId, state);

                case PlayerCommand.DraftAction:
                    return CanDraftAction(clientId, state);

                case PlayerCommand.UndoDraftAction:
                    return CanUndoDraftAction(clientId, state);

                case PlayerCommand.CommitTurn:
                    return CanCommitTurn(clientId, state);

                case PlayerCommand.CycleActiveVhsSet:
                    return CanCycleActiveVhsSet(clientId, state);

                default:
                    return false;
            }
        }
        
        public void Request(PlayerCommand command)
        {
            switch (command)
            {
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

                case PlayerCommand.CycleActiveVhsSet:
                    RequestCycleActiveVhsSet();
                    break;
            }
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
            
            CommitDraftToState(clientId, state);
            
            var coordinator = GameCoordinator.Instance;
            if (coordinator == null)
            {
                LogRejected($"Commit turn blocked for client {clientId}: missing GameCoordinator.");
                return;
            }

            coordinator.CompleteCommittedTurn(clientId,state);

            LogAccepted($"Client {clientId} requested turn commit.");
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

            VhsSet activeSet = playerEntity.GetActiveVhsSet();

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
            if (!logRequests) return;
            
            //Debug.Log($"[PlayerActionController] ACCEPTED: {message}", this);
        }
        
        private void LogRejected(string message)
        {
            if (!logRequests) return;

            Debug.Log($"[PlayerActionController] REJECTED: {message}", this);
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
    }
}