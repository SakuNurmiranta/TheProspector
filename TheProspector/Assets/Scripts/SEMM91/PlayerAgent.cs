using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SEMM91.GamePlay;
using SEMM91.Networking;
using SEMM91.InputSystems;


namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
    {
        private Coroutine _botRoutine;

        private bool _botMode;
        private bool _botStress;
        private int _botSeed;
        
        private PlayerActionController _actionController;
       
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner || !IsClient) return;
            
            _actionController = GetComponent<PlayerActionController>();
            if (_actionController == null)
            {
                Debug.LogError("[PlayerAgent] Missing PlayerActionController on player object.", this);
                return;
            }
            
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
            
            if (Input.GetKeyDown(KeyCode.Space))
                _actionController.RequestDraftAction();

            if (Input.GetKeyDown(KeyCode.Return))
                _actionController.RequestCommitTurn();

            if (Input.GetKeyDown(KeyCode.Backspace))
                _actionController.RequestUndoDraftAction();

            if (Input.GetKeyDown(KeyCode.Alpha1))
                _actionController.RequestSelectStance(BandStance.Gestate);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                _actionController.RequestSelectStance(BandStance.Rehearse);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                _actionController.RequestSelectStance(BandStance.Promote);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                _actionController.RequestCycleActiveVhsSet();
            
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
                if (act) _actionController.RequestDraftAction();
                else _actionController.RequestUndoDraftAction();
            }
        }
    }
}
