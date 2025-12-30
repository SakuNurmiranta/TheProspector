using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
    {
        private Coroutine _botRoutine;

        // optional: keep your existing bot flags
        private bool _botMode;
        private bool _botStress;
        private int _botSeed;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner && IsClient)
            {
                _botMode = BotConfig.HasArg("-bot");
                _botStress = BotConfig.HasArg("-botStress");

                // deterministic per-client seed (repeatable)
                _botSeed = BotConfig.GetIntArg("-botSeed", 12345) + (int)NetworkManager.Singleton.LocalClientId;

                if (_botMode)
                {
                    _botRoutine = StartCoroutine(BotLoop(_botSeed, _botStress));
                    Debug.Log($"[BOT] Started bot loop. stress={_botStress} seed={_botSeed} clientId={NetworkManager.Singleton.LocalClientId}");
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_botRoutine != null)
            {
                StopCoroutine(_botRoutine);
                _botRoutine = null;
            }

            base.OnNetworkDespawn();
        }

        private void OnDestroy()
        {
            if (_botRoutine != null)
            {
                StopCoroutine(_botRoutine);
                _botRoutine = null;
            }
        }

        private IEnumerator BotLoop(int seed, bool stress)
        {
            var rnd = new System.Random(seed);

            // Optional: wait until server signals test start
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

                // 70/30 bias towards act vs skip
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
    }
}
