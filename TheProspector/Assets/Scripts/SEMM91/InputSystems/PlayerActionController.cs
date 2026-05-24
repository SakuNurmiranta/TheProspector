using SEMM91.GamePlay;
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

        [ServerRpc]
        private void SubmitDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            
            var coordinator = GameCoordinator.Instance;
            if (coordinator == null || !coordinator.CanClientDraftAction(clientId))
            {
                LogRejected($"Draft action request from client {clientId} rejected.");
                return;
            }
            var state = GetComponent<NetPlayerState>();
            if (state == null)
            {
                LogRejected($"Draft action blocked for client {clientId}: missing NetPlayerState.");
                return;
            }
            
            state.IncrementDraftedActionsServer();

            LogAccepted($"Client {clientId} added productive action ({state.DraftedActionsValue}/3)");

        }
        
        [ServerRpc]
        private void SubmitUndoDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            var state = GetComponent<NetPlayerState>();
            if (state == null)
            {
                LogRejected($"Undo draft blocked for client {clientId}: missing NetPlayerState.");
                return;
            }



            state.DecrementDraftedActionsServer();
            LogAccepted($"Client {clientId} removed action ({state.DraftedActionsValue}/3)");
        }
        
        [ServerRpc]
        private void SubmitStanceServerRpc(BandStance stance, ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var coordinator = GameCoordinator.Instance;
            if (coordinator == null || !coordinator.CanClientChangeStance(clientId))
            {
                LogRejected($"Stance selection blocked for client {clientId}: {stance}.");
                return;
            }

            var state = GetComponent<NetPlayerState>();
            if (state == null)
            {
                LogRejected($"Stance selection blocked for client {clientId}: missing NetPlayerState.");
                return;
            }

            state.SetCurrentStanceServer(stance);

            LogAccepted($"Client {clientId} selected {stance}.");
        }

        [ServerRpc]
        private void SubmitCommitTurnServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var coordinator = GameCoordinator.Instance;
            if (coordinator == null)
            {
                LogRejected($"Commit turn blocked for client {clientId}: missing GameCoordinator.");
                return;
            }

            coordinator.RegisterEndTurn(clientId);

            LogAccepted($"Client {clientId} requested turn commit.");
        }

        [ServerRpc]
        private void SubmitCycleActiveVhsSetServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var coordinator = GameCoordinator.Instance;
            if (coordinator == null)
            {
                LogRejected($"Cycle VHS set blocked for client {clientId}: missing GameCoordinator.");
                return;
            }

            coordinator.CycleActiveVhsSetForClient(clientId);

            LogAccepted($"Client {clientId} requested active VHS set cycle.");
        }

        private void LogAccepted(string message)
        {
            if (!logRequests) return;
            
            Debug.Log($"[PlayerActionController] ACCEPTED: {message}", this);
        }
        
        private void LogRejected(string message)
        {
            if (!logRequests) return;

            Debug.Log($"[PlayerActionController] REJECTED: {message}", this);
        }
    }
}