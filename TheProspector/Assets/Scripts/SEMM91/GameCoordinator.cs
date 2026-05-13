using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Entities;
using UnityEngine.Serialization; // access NEtPlayerState

namespace SEMM91
{
    public class GameCoordinator : NetworkBehaviour
    {
        public static GameCoordinator Instance;
        public Texture2D gameplayBackground;

        public enum Season
        {
            Spring,
            Summer,
            Fall,
            Winter
        }
        
        // Just a role for now
        [FormerlySerializedAs("KeeperClientId")] public NetworkVariable<ulong> keeperClientId = new();

        // Simple replicated year/season indexes
        [FormerlySerializedAs("GlobalTurn")] public NetworkVariable<int> globalTurn = new();
        [FormerlySerializedAs("RoundIndex")] public NetworkVariable<int> roundIndex = new();
        
        // NetworkVariables for testing sync with bots
        public NetworkVariable<bool> testStarted = new(false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly HashSet<ulong> _readyClients = new();
        
        private const int MinPlayersToStart = 6; 
        private const int TurnsPerYear = 4;

        // Server-only state
        // which players have acted already
        private readonly HashSet<ulong> _actedThisTurn = new();

        //cached mapping for client states
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();
        private bool _gameStarted = false;
        private bool _gameEnded;
        private readonly ulong _finalWinner = ulong.MaxValue;

        public bool GameEnded => _gameEnded;
        public ulong FinalWinner => _finalWinner;
        
        
        private void Awake() => Instance = this;

        private void Update()
        {
            //if (!_gameEnded) return;

            // Press Escape in any window to quit
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (NetworkManager.Singleton != null && NetworkManager.IsListening)

                {
                    NetworkManager.Singleton.Shutdown();
                }
                
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                testStarted.Value = false;
                _readyClients.Clear();
                
                _readyClients.Add(NetworkManager.ServerClientId);
                SLog($"READY server={NetworkManager.ServerClientId} readyCount={_readyClients.Count}/{MinPlayersToStart}");
                
                RunLog.Header(
                    role: "server",
                    testCase: BotConfig.GetStringArg("-tc", "TC-UNKNOWN"),
                    preset: BotConfig.GetStringArg("-netPreset", "P?-UNKNOWN"),
                    clientsPlanned: BotConfig.GetIntArg("-clients", 6),
                    botSeed: BotConfig.GetIntArg("-botSeed", 12345)
                );
                
                // seed for already-connected clients (incl. host)
                foreach (var id in NetworkManager.ConnectedClientsIds)
                {
                    RegisterPlayerServer(id);
                }

                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

                keeperClientId.Value = OwnerClientId; // default to host
                TryStartWhenEnough();
            }
        }

        private new void OnDestroy()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // discover/register player upon connection
        private void RegisterPlayerServer(ulong clientId)
        {
            if (!IsServer) return;

            if (!_playerStates.ContainsKey(clientId))
            {
                if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    var playerObj = client.PlayerObject;
                    if (playerObj != null && playerObj.TryGetComponent(out NetPlayerState state))
                    {
                        _playerStates[clientId] = state;
                        // ensure defaults in host-authoritative mode
                        var index = _playerStates.Count - 1;
                        
                        // after state.InitializeServer(...)
                        state.InitializeServer(index, $"Player {clientId}");
                        
                        GameObject controllerObj = new GameObject($"Controller_{clientId}");
                        GameEntity controllerEntity = controllerObj.AddComponent<GameEntity>();
                        
                        controllerEntity.InitializeIdentity($"Controller {clientId}", GameEntityType.Character);
                        
                        state.SetControllerEntity(controllerEntity);
                        
                        Debug.Log(
                            $"[ENTITY TEST] client={clientId} " +
                            $"hasController={state.ControllerEntity != null} " +
                            $"controllerName={state.ControllerEntity?.DisplayName} " +
                            $"controllerType={state.ControllerEntity?.EntityType}"
                        );
                        // NEW: if we're in dedicated server mode, 
                        // treat the host's own player as inactive so it doesn't block lockstep.
                        if (NetBootstrap.DedicatedServerModeActive &&
                            clientId == NetworkManager.ServerClientId)
                        {
                            Debug.Log("[GameCoordinator] Host player detected in dedicatedServerMode; marking inactive.");
                            state.SetExhaustedServer(false);  // just to be safe
                            state.isActive.Value = false;     // or wrap this in a helper if you prefer
                        }

                        //forces a mid-game joiner to wait until change year/round
                        if (_gameStarted)
                        {
                            state.SetActiveServer(false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"GameCoordinator: No NetPlayerState on player object for client {clientId}");
                    }
                }
            }

            //force per-turn logic
            _actedThisTurn.Remove(clientId);
        }

        private void OnClientConnected(ulong id)
        {
            SLog($"NET ClientConnected id={id} connectedCount={NetworkManager.ConnectedClientsIds.Count}");
            RegisterPlayerServer(id);
            TryStartWhenEnough();
        }

        private void OnClientDisconnected(ulong id)
        {
            _readyClients.Remove(id);
            _actedThisTurn.Remove(id);
            _playerStates.Remove(id);

            if (keeperClientId.Value == id) //if disconnected keeper, pick a new one (if possible)
            {
                ElectKeeperFromLastResolvedRound();
            }
            
            EnsureKeeperSelected();
            
            //if there are no clients left with actions, advance global turn (so we don't get stuck)
            if (IsServer && AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
            }
            SLog($"NET ClientDisconnected id={id} connectedCount={NetworkManager.ConnectedClientsIds.Count}");
        }

        private void ElectKeeperFromLastResolvedRound()
        {
            foreach (var kvp in _playerStates)
            {
                var state = kvp.Value;
                if (state == null) continue;

                if (state.LastResolvedRound.Count > 0)
                {
                    var best = state.LastResolvedRound
                        .OrderByDescending(pair => pair.Value.Score)
                        .First();
                    
                    SetKeeper(best.Key);
                    return;
                }
            }
            
            EnsureKeeperSelected();
        }

        private void TryStartWhenEnough()
        {
            if (!IsServer) return;
            if (_gameStarted) return;
            if (NetworkManager.ConnectedClientsIds.Count < MinPlayersToStart) return;

            globalTurn.Value = 0;
            roundIndex.Value = 0;
            _actedThisTurn.Clear();

            EnsureKeeperSelected();
            _gameStarted = true;
            testStarted.Value = true;
            SLog($"GAME Started connectedCount={NetworkManager.ConnectedClientsIds.Count}");
            BroadcastStateClientRpc();
        }
        
        private void TryStartTestRun()
        {
            if (!IsServer) return;
            if (testStarted.Value) return;
            
            // current connections
            int connected = NetworkManager.ConnectedClientsIds.Count;
            
            // start when ready
            if (connected < MinPlayersToStart) return;
            if (_readyClients.Count < connected) return;
            
            // Reset state for a clean run start
            _actedThisTurn.Clear();
            globalTurn.Value = 0;
            roundIndex.Value = 0;
            
            EnsureKeeperSelected();
            _gameStarted = true;
            
            testStarted.Value = true;
            
            SLog($"GAME Started connectedCount={connected} readyCount={_readyClients.Count}");
        }

        private void SetKeeper(ulong newKeeper)
        {
            //at this stage, this is simply a ritual
            keeperClientId.Value = newKeeper;
            _actedThisTurn.Clear();


        }

        private void EnsureKeeperSelected()
        {
            // Build candidate set: all active players
            var candidates = _playerStates
                .Where(kvp => kvp.Value != null && kvp.Value.ActiveValue)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id) // deterministic order: lowest clientId first
                .ToList();

            if (candidates.Count == 0)
            {
                // No active players -> no meaningful keeper. 
                // You can keep the old value or use a sentinel.
                // For now, we just leave keeperClientId as-is.
                return;
            }

            // If current keeper is still valid & active, keep them.
            if (candidates.Contains(keeperClientId.Value))
                return;

            // Otherwise, elect a new keeper deterministically.
            ulong newKeeper = candidates[0]; // lowest active clientId
            SetKeeper(newKeeper);
        }
        
        // -- Public API for turn actions
        public void RegisterEndTurn(ulong senderClientId)
        {
            if (!IsServer) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(senderClientId)) return;

            if (!TryGetPlayerState(senderClientId, out var state)) return;

            if (state.CurrentStanceValue == BandStance.None)
            {
                SLog($"[TURN BLOCKED] Client {senderClientId} has no stance selected.");
                return;
            }

            if (!CanActThisTurn(senderClientId, state))
                return;
            
            byte drafted = state.DraftedActionsValue;

            //state.ResetActionsUsedServer();
            state.ResetCommittedActionsServer();

            // Apply draft
            for (int i = 0; i < drafted; i++)
            {
                //state.IncrementActionsUsedServer();
                state.IncrementCommittedActionServer();
            }

            if (state.CommittedActionsValue >= 3)
            {
                state.SetExhaustedServer(true);
                SLog($"[OVEREXERTION] Client {senderClientId} took a third productive action.");
            }
            
            ResolveCommittedStanceOutcome(senderClientId, state);
            
            state.ResetDraftedActionsServer();
            state.AddToScoreServer(1);

            SLog($"[TURN COMMIT] Client {senderClientId} locked stance {state.CurrentStanceValue}");
            SLog($"ACT EndTurn from client={senderClientId} (+score)");

            MarkActedAndAdvanceIfReady(senderClientId, state);
        }
        

        private bool TryGetPlayerState(ulong clientId, out NetPlayerState state)
        {
            if (_playerStates.TryGetValue(clientId, out state) && state != null) return true;

            //fallback
            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObj = client.PlayerObject;
                if (playerObj != null && playerObj.TryGetComponent(out state))
                {
                    _playerStates[clientId] = state;
                    return true;
                }
            }

            state = null;
            return false;
        }

        // a player can act if they haven't acted yet this turn and they are active (IsActive)
        private bool CanActThisTurn(ulong clientID, NetPlayerState state)
        {
            if (_actedThisTurn.Contains(clientID))
                return false;

            if (!state.ActiveValue)
                return false;

            return true;
        }

        private void MarkActedAndAdvanceIfReady(ulong senderClientId, NetPlayerState state)
        {
       
            _actedThisTurn.Add(senderClientId);
            

            if (AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
            }
            else
            {
                BroadcastStateClientRpc();
            }
        }

        private bool AllActivePlayersActed()
        {
            foreach (var kvp in _playerStates)
            {
                var clientId = kvp.Key;
                var state = kvp.Value;
                if (state == null) continue;

                if (!state.ActiveValue) continue;

                if (!_actedThisTurn.Contains(clientId)) return false;

            }

            return true;

        }

        private void AdvanceGlobalTurn()
        {
            // clear actions for next turn
            _actedThisTurn.Clear();
            ResetPlayerActionsForNewTurn();

            int prevTurn = globalTurn.Value;
            int prevRound = roundIndex.Value;
            StorePreviousStancesForTurnBoundary();
            globalTurn.Value++;

            //increment year in four season cycles
            if (IsEndOfYearTurn())
            {
                roundIndex.Value++;
                SLog($"ADV Round {prevRound} -> {roundIndex.Value} (year end)");
                
                //TO DO: Hook up keeper validity check & tally updates
                YearEndKeeperValidityCheck();
                UpdateLastResolvedRound();
                ReactivateInactivePlayersAtYearEnd();
                

            }

            // NEW: pick a Keeper deterministically when entering turn 1
            EnsureKeeperSelected();
            RolloverPlayerStancesForNewTurn();
            BroadcastStateClientRpc();
            
        }

        private void UpdateLastResolvedRound()
        {
            ulong keeper = keeperClientId.Value;
            
            if (!_playerStates.TryGetValue(keeper, out var keeperState))
                return;
            
            var newSnapshot = new Dictionary<ulong, NetPlayerState.LastResolvedRoundData>();
            foreach (var kvp in _playerStates)
            {
                ulong id = kvp.Key;
                var ps = kvp.Value;
                if (ps == null) continue;

                newSnapshot[id] = new NetPlayerState.LastResolvedRoundData()
                {
                    Score = ps.ScoreValue,
                    IsActive = ps.ActiveValue
                };
            }
            
            keeperState.LastResolvedRound = newSnapshot;

            foreach (var kvp in _playerStates)
            {
                if (kvp.Key == keeper) continue; //skip keeper (already updated) 
                kvp.Value.LastResolvedRound = new Dictionary<ulong, NetPlayerState.LastResolvedRoundData>(newSnapshot);
            }
        }
        
        private void YearEndKeeperValidityCheck()
        {
            //build a list of active players
            var snapshot = new Dictionary<ulong, int>();
               
            //read their scores
            foreach (var kvp in _playerStates)
            {
                ulong id = kvp.Key;
                NetPlayerState ps = kvp.Value;
                if (!ps.ActiveValue) continue;
                    
                snapshot[id] = ps.ScoreValue;
            }
                
            ulong currentKeeper = keeperClientId.Value;

            if (!snapshot.ContainsKey(currentKeeper))
            {
                //current keeper is no longer active
                EnsureKeeperSelected();
                return;
            }
                
            //compare -> replace keeper if necessary
            int keeperScore = snapshot[currentKeeper];
                
            ulong bestPlayer = currentKeeper;
            int bestScore = keeperScore;

            foreach (var kvp in snapshot)
            {
                if (kvp.Value > bestScore)
                {
                    bestPlayer = kvp.Key;
                    bestScore = kvp.Value;
                }
            }
                
            // dethroned
            if (bestPlayer != currentKeeper)
            {   
                //update keeperClientId via setKeeper(newKeeper)
                SetKeeper(bestPlayer);
            }
        }
        


        [ClientRpc]
        private void BroadcastStateClientRpc()
        {
            // For MVP, just UI text is enough; no per-client data push needed beyond NetworkVariables
        }

        private void ReactivateInactivePlayersAtYearEnd()
        {
            foreach (var kvp in _playerStates)
            {
                var state = kvp.Value;
                if (state == null) continue;

                if (!state.ActiveValue)
                {
                    state.SetActiveServer(true);
                }
            }
        }
        
        private void SLog(string msg)
        {
            if (!IsServer) return;
            Debug.Log($"[S] t={Time.realtimeSinceStartup:F2} round={roundIndex.Value} turn={globalTurn.Value} keeper={keeperClientId.Value} :: {msg}");
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReportClientReadyServerRpc(ServerRpcParams p = default)
        {
            if (!IsServer) return;

            ulong id = p.Receive.SenderClientId;
            _readyClients.Add(id);
            
            SLog($"READY client= {id} readyCount={_readyClients.Count}/{MinPlayersToStart}");

            TryStartTestRun();
        }

        public Season CurrentSeason => (Season)(globalTurn.Value % 4);
        
        private bool IsEndOfYearTurn()
        {
            return globalTurn.Value > 0 && globalTurn.Value % TurnsPerYear == 0;
        }
        
        private void RolloverPlayerStancesForNewTurn()
        {
            foreach (var playerState in FindObjectsOfType<NetPlayerState>())
            {
                SLog($"[STANCE] Client {playerState.OwnerClientId} continues as {playerState.CurrentStanceValue}");
            }
        }
        
        private void StorePreviousStancesForTurnBoundary()
        {
            foreach (var playerState in FindObjectsOfType<NetPlayerState>())
            {
                playerState.StorePreviousStanceServer();
                SLog($"[STANCE] Client {playerState.OwnerClientId} stored previous stance: {playerState.PreviousStanceValue}");
            }
        }
        
        public bool HasPlayerActed(ulong clientId)
        {
            return _actedThisTurn.Contains(clientId);
        }
        
        public bool CanClientAct(ulong clientId)
        {
            if (!TryGetPlayerState(clientId, out var state))
                return false;

            return CanActThisTurn(clientId, state);
        }
        
        private void ResetPlayerActionsForNewTurn()
        {
            foreach (var kvp in _playerStates)
            {
                var state = kvp.Value;
                if (state == null) continue;

                //state.ResetActionsUsedServer();
                state.ResetCommittedActionsServer();
            }

            SLog("[ACTION] Reset actions and productive actions for new turn");
        }

        public bool CanClientChangeStance(ulong clientId)
        {
            if (!TryGetPlayerState(clientId, out var state))
                return false;

            if (!CanClientAct(clientId))
                return false;

            if (state.DraftedActionsValue > 0)
            {
                SLog($"[STANCE] Client {clientId} cannot change stance while actions have been drafted - undo first!");
                return false;
            }
            
            return true;
        }
        
        private void ResolveCommittedStanceOutcome(ulong clientId, NetPlayerState state)
        {
            byte actions = state.CommittedActionsValue;

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    SLog($"[GESTATE] Client {clientId} generated {actions} idea effort.");
                    break;

                case BandStance.Rehearse:
                    SLog($"[REHEARSE] Client {clientId} improved conveyance by {actions} effort.");
                    break;

                case BandStance.Promote:
                    SLog($"[PROMOTE] Client {clientId} increased visibility by {actions} effort.");
                    break;

                default:
                    SLog($"[OUTCOME BLOCKED] Client {clientId} has no valid stance.");
                    break;
            }
        }
    }
}