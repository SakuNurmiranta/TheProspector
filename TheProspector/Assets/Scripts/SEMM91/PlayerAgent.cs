using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SEMM91.GamePlay;
using SEMM91.Networking;
using SEMM91;


namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
    {
        private Coroutine _botRoutine;

        private bool _botMode;
        private bool _botStress;
        private int _botSeed;
       
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner || !IsClient) return;

            _botMode = BotConfig.HasArg("-bot") || BotConfig.GetIntArg("-bot", 0) != 0;
            _botStress = BotConfig.HasArg("-botStress") || BotConfig.GetIntArg("-botStress", 0) != 0;
            _botSeed = BotConfig.GetIntArg("-botSeed", 12345) + (int)NetworkManager.Singleton.LocalClientId;

            var gc = GameCoordinator.Instance;
            if (gc != null)
                gc.ReportClientReadyServerRpc();

            if (_botMode)
            {
                _botRoutine = StartCoroutine(BotLoop(_botSeed, _botStress));
                Debug.Log($"[BOT] Started bot loop. stress={_botStress} seed={_botSeed} clientId={NetworkManager.Singleton.LocalClientId}");
            }
            else
            {
                Debug.Log($"[HUMAN] Controls enabled for clientId={NetworkManager.Singleton.LocalClientId} (SPACE/BACKSPACE).");
            }
        }

        public override void OnNetworkDespawn()
        {
            StopBot();
            base.OnNetworkDespawn();
        }

        private void OnDestroy() => StopBot();

        private void StopBot()
        {
            if (_botRoutine != null)
            {
                StopCoroutine(_botRoutine);
                _botRoutine = null;
            }
        }

        private void Update()
        {
            if (!IsOwner || !IsClient) return;
            if (_botMode) return; // bots handled by coroutine

            var coordinator = GameCoordinator.Instance;

            if (coordinator != null && coordinator.HasPlayerActed(OwnerClientId))
            {
                return;
            }
            
            // Human input restored:
            if (Input.GetKeyDown(KeyCode.Space))
                SubmitDraftActionServerRpc();

            if (Input.GetKeyDown(KeyCode.Return))
                SubmitCommitTurnServerRpc();
            
            if (Input.GetKeyDown(KeyCode.Backspace))
                SubmitUndoDraftActionServerRpc();
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SubmitStanceServerRpc(BandStance.Gestate);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SubmitStanceServerRpc(BandStance.Rehearse);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SubmitStanceServerRpc(BandStance.Promote);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                SubmitCycleActiveVhsSetServerRpc();
            }
            
        }

        private IEnumerator BotLoop(int seed, bool stress)
        {
            var rnd = new System.Random(seed);

            // wait for server signal, but DON'T freeze forever without logs
            while (true)
            {
                var gc = GameCoordinator.Instance;
                if (gc != null && gc.testStarted.Value)
                    break;

                yield return null;
            }

            int minMs = BotConfig.GetIntArg("-botMinMs", stress ? 80 : 250);
            int maxMs = BotConfig.GetIntArg("-botMaxMs", stress ? 200 : 800);

            while (true)
            {
                int waitMs = rnd.Next(minMs, maxMs + 1);
                yield return new WaitForSeconds(waitMs / 1000f);

                bool act = rnd.NextDouble() < 0.7;
                if (act) SubmitDraftActionServerRpc();
                else SubmitUndoDraftActionServerRpc();
            }
        }

        [ServerRpc]
        private void SubmitDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            
            var coordinator = GameCoordinator.Instance;
            if (coordinator == null || !coordinator.CanClientDraftAction(clientId)) return;
            
            var state = GetComponent<NetPlayerState>();
            if (state == null) return;

            state.IncrementDraftedActionsServer();

            Debug.Log($"[DRAFT] Client {clientId} added productive action ({state.DraftedActionsValue}/3)");
        }

        [ServerRpc]
        private void SubmitUndoDraftActionServerRpc(ServerRpcParams p = default)
        {
            var state = GetComponent<NetPlayerState>();
            if (state == null) return;

            ulong clientId = p.Receive.SenderClientId;

            state.DecrementDraftedActionsServer();
            Debug.Log($"[DRAFT] Client {clientId} removed action ({state.DraftedActionsValue}/3)");
        }
        
        [ServerRpc]
        private void SubmitStanceServerRpc(BandStance stance, ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var coordinator = GameCoordinator.Instance;
            
            if (coordinator == null || !coordinator.CanClientChangeStance(clientId))
                return;

            var state = GetComponent<NetPlayerState>();
            if (state == null)
                return;

            state.SetCurrentStanceServer(stance);

            Debug.Log($"[STANCE] Client {clientId} selected {stance}");
        }

        [ServerRpc]
        private void SubmitCommitTurnServerRpc(ServerRpcParams p = default)
        {
            var g = GameCoordinator.Instance;
            if (g == null) return;

            g.RegisterEndTurn(p.Receive.SenderClientId);
        }

        [ServerRpc]
        private void SubmitCycleActiveVhsSetServerRpc(ServerRpcParams p = default)
        {
            var coordinator = GameCoordinator.Instance;
            
            if (coordinator == null) return;
            
            coordinator.CycleActiveVhsSetForClient(p.Receive.SenderClientId);
        }
    }
}
