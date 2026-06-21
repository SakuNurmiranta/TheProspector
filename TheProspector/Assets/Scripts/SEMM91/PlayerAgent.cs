using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SEMM91.InputSystems;
using SEMM91.Networking;
using Unity.Services.Lobbies.Models;


namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
    {
        [SerializeField] private bool logAgentDebug = false;
        private Coroutine _botRoutine;
        private NetPlayerState _playerState;
        
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
            
            _playerState =  GetComponent<NetPlayerState>();
            if (_playerState == null)
            {
                Debug.LogError(
                    "[PlayerAgent] Missing NetPlayerState " +
                    "on player object.",
                    this
                );

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
                if (logAgentDebug)
                    Debug.Log($"[HUMAN] Controls enabled for clientId={OwnerClientId} (SPACE/BACKSPACE).");
            }
        }

        public override void OnNetworkDespawn()
        {
            StopBot();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy() => StopBot();

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

            if (Input.GetKeyDown(KeyCode.Escape))
                _actionController.Request(
                    PlayerCommand.QuitSession
                );

            if (Input.GetKeyDown(KeyCode.F9))
            {
                _actionController.Request(
                    PlayerCommand.ForceStartSession
                );
            }
            
            if (_playerState != null &&
                _playerState.HasCommittedTurnValue)
            {
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
                RequestIfAvailable(PlayerCommand.DraftAction);

            if (Input.GetKeyDown(KeyCode.Return))
                RequestIfAvailable(PlayerCommand.CommitTurn);

            if (Input.GetKeyDown(KeyCode.Backspace))
                RequestIfAvailable(PlayerCommand.UndoDraftAction);
            
           

            if (Input.GetKeyDown(KeyCode.D))
            {
                RequestIfAvailable(PlayerCommand.Dream);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
                RequestIfAvailable(PlayerCommand.SelectGestate);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                RequestIfAvailable(PlayerCommand.SelectRehearse);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                RequestIfAvailable(PlayerCommand.SelectPromote);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                RequestIfAvailable(PlayerCommand.CycleTarget);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                RequestIfAvailable(PlayerCommand.DraftPrimaryAction);
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                RequestIfAvailable(PlayerCommand.DraftSecondaryAction);
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                RequestIfAvailable(PlayerCommand.DraftTertiaryAction);
            }

            //Reserve for rest action at least in debug
            if (Input.GetKeyDown(KeyCode.R))
            {
                RequestIfAvailable(PlayerCommand.DraftRestAction);
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                _actionController.Request(PlayerCommand.AdminCreateEmptyRehearsalSet);
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                RequestIfAvailable(
                    PlayerCommand.CycleTarget
                );
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

                PlayerCommand command = rnd.NextDouble() < 0.7
                    ? PlayerCommand.DraftAction
                    : PlayerCommand.UndoDraftAction;

                _actionController.Request(command);
            }
        }
        
        private void RequestIfAvailable(
            PlayerCommand command)
        {
            if (_actionController == null)
                return;

            if (!_actionController.CanRequest(command))
                return;

            _actionController.Request(command);
        }
    }
}
