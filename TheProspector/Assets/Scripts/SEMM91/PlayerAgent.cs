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
                SubmitEndTurnServerRpc();

            if (Input.GetKeyDown(KeyCode.Backspace))
                SubmitSkipTurnServerRpc();
            
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
                if (act) SubmitEndTurnServerRpc();
                else SubmitSkipTurnServerRpc();
            }
        }

        [ServerRpc]
        private void SubmitEndTurnServerRpc(ServerRpcParams p = default)
        {
            var g = GameCoordinator.Instance;
            if (g == null) return;
            g.RegisterEndTurn(p.Receive.SenderClientId);
        }

        [ServerRpc]
        private void SubmitSkipTurnServerRpc(ServerRpcParams p = default)
        {
            var g = GameCoordinator.Instance;
            if (g == null) return;
            g.RegisterSkipTurn(p.Receive.SenderClientId);
        }
        
        [ServerRpc]
        private void SubmitStanceServerRpc(BandStance stance, ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            if (!CanClientChangeStance(clientId))
                return;

            var state = GetComponent<NetPlayerState>();
            if (state == null)
                return;

            state.SetCurrentStanceServer(stance);

            Debug.Log($"[STANCE] Client {clientId} selected {stance}");
        }
        
        private bool CanClientChangeStance(ulong clientId)
        {
            var coordinator = GameCoordinator.Instance;

            if (coordinator == null)
                return false;

            if (coordinator.HasPlayerActed(clientId))
            {
                Debug.Log($"[STANCE BLOCKED] Client {clientId} already ended turn.");
                return false;
            }

            // NEW: enforce same rule as turn participation
            if (!coordinator.CanClientAct(clientId))
            {
                Debug.Log($"[STANCE BLOCKED] Client {clientId} is not allowed to act this turn.");
                return false;
            }

            return true;
        }
    }
}
