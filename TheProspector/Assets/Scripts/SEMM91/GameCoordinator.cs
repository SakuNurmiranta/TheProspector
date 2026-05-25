/*
Architecture pin:

GameCoordinator currently functions as a vertical-slice convergence point. It handles networking, turn progression, player registration, controller entity seeding, temporary stance outcome resolution, idea creation, and track creation.

    This is acceptable for the current prototype phase, but should be treated as temporary scaffolding.

    Future refactor targets:
1. Extract player/controller creation into a PlayerEntityBootstrapper or StartingControllerFactory.
2. Extract stance outcome logic into a BandStanceResolver or separate Gestation/Rehearsal/Promotion resolvers.
3. Keep GameCoordinator focused on network/session/turn orchestration.
4. Do not refactor yet unless the current implementation step becomes blocked by this concentration.

    Current rule:
Continue vertical-slice implementation, but avoid adding more semantic construction logic directly into GameCoordinator unless it is clearly temporary test scaffolding.*/

using System.Collections.Generic;
using System.Linq;
using SEMM91.Core.Aspects;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Entities;

using UnityEngine.Serialization; // access NEtPlayerState

using GestationActionResolver = SEMM91.GamePlay.Gestation.ActionResolver;
using RehearsalActionResolver = SEMM91.GamePlay.Rehearsal.ActionResolver;

namespace SEMM91
{
    public class GameCoordinator : NetworkBehaviour
    {
        [Header("Debug Logging")]
        [SerializeField] private bool logTurnDebug = false;
        [SerializeField] private bool logPayloadDebug = false;
        [SerializeField] private bool logProductionDebug = false;
        [SerializeField] private bool logMaintenanceDebug = false;
        [SerializeField] private bool logNetworkDebug = false;
        [SerializeField] private bool logBootDebug = false;
        [SerializeField] private bool logEntityDebug = false;
        [SerializeField] private bool logTagDebug = false;
        
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

        private GestationActionResolver _gestationActionResolver;
        private RehearsalActionResolver _rehearsalActionResolver;
        
        //cached mapping for client states
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();
        private bool _gameStarted = false;
        private bool _gameEnded;
        private bool _isShuttingDown;
        private readonly ulong _finalWinner = ulong.MaxValue;

        public bool GameEnded => _gameEnded;
        public ulong FinalWinner => _finalWinner;
        
        private const float ForgetfulnessConveyanceModHard = 0.95f; //These don't really belong here
        private const float ForgetfulnessConveyanceModSoft = 0.98f;
        private const float MinimumVhsConveyance = 0.1f;

        private void Awake()
        {
            Instance = this;
            
            _gestationActionResolver = new GestationActionResolver(
                ProductionLog,
                Debug.LogError
                );
            
            _rehearsalActionResolver = new RehearsalActionResolver(
                () => globalTurn.Value,
                TurnLog
            );
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _gestationActionResolver.Initialize();
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
                        
                        GameObject playerEntityObj = new GameObject($"Leader_{clientId}");
                        GameEntity playerEntity = playerEntityObj.AddComponent<GameEntity>();
                        
                        playerEntity.InitializeIdentity($"Player {clientId}", GameEntityType.Character);

                        playerEntity.AddAspectId("ASPECT_KNOWS_GUITAR");
                        playerEntity.AddAspectId("ASPECT_HAS_GUITAR");
                        
                        playerEntity.AddTagContainer(TagContainerType.Conviction);
                        
                        playerEntity.TrySetTag(
                            TagContainerType.Conviction,
                            new TagInstance(TagAxis.Symbolic, TagPole.Negative, TagDegree.Weak));
                        
                        state.SetPlayerEntity(playerEntity);
                        
                       EntityLog(
                            $"[ENTITY TEST] client={clientId} " +
                            $"hasController={state.PlayerEntity != null} " +
                            $"controllerName={state.PlayerEntity?.DisplayName} " +
                            $"controllerType={state.PlayerEntity?.EntityType} | " +
                            $"[ENTITY SEED] {playerEntity.DisplayName} aspects={playerEntity.AspectIds.Count} tags={playerEntity.TagContainers.Count}"
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
            if (_isShuttingDown)
                return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;
            
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
        public void CompleteCommittedTurn(ulong clientId, NetPlayerState state)
        {
            if (!IsServer) return;
            if (state == null) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(clientId)) return;

            LogCommittedPayloads(clientId, state);
            ResolveCommittedPayloads(clientId, state);
            ApplyTurnCommitMaintenanceEffects(clientId, state);

            TurnLog($"[TURN COMMIT] Client {clientId} locked stance {state.CurrentStanceValue}");

            MarkActedAndAdvanceIfReady(clientId, state);
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
            if (_isShuttingDown)
                return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;
            
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
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
            {
                TurnLog($"[STANCE] Client {playerState.OwnerClientId} continues as {playerState.CurrentStanceValue}");
            }
        }
        
        private void StorePreviousStancesForTurnBoundary()
        {
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
            {
                playerState.StorePreviousStanceServer();
                TurnLog($"[STANCE] Client {playerState.OwnerClientId} stored previous stance: {playerState.PreviousStanceValue}");
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

            TurnLog("[ACTION] Reset actions and productive actions for new turn");
        }
        
        private void ApplyTurnCommitMaintenanceEffects(ulong clientId, NetPlayerState state)
        {
            ApplyForgetfulnessIfNeeded(clientId, state);
            // Later: ApplyColdWind, ApplyRelaxed, ApplyExhaustion... etc
        }

        private void ApplyForgetfulnessIfNeeded(ulong clientId, NetPlayerState state)
        {
            if (state == null) return;

            GameEntity controller = state.PlayerEntity;

            if (controller == null)
            {
                TurnLog($"Forgetfulness: Client {clientId} has no controller entity.");
                return;
            }

            // NOTE TO SELF: IDEA LEVEL DECAY COULD ACTUALLY BE A THING,
            // but for now Forgetfulness works at VHS set level.

            VhsSet activeSet = controller.GetActiveVhsSet();

            if (activeSet == null)
            {
                MaintenanceLog($"[FORGETFULNESS BLOCKED] Client {clientId} has no VHS set.");
                return;
            }

            if (activeSet.VhsTracks.Count == 0)
            {
                MaintenanceLog($"[FORGETFULNESS NOTE] Client {clientId} active VHS set has no tracks.");
            }

            float decayMod = state.CurrentStanceValue == BandStance.Rehearse
                ? ForgetfulnessConveyanceModSoft
                : ForgetfulnessConveyanceModHard;
            
            foreach (VhsSet vhsSet in controller.VhsSets)
            {
                if (vhsSet == null)
                    continue;

                if (vhsSet == activeSet)
                {
                    MaintenanceLog($"No forgetfulness for active set {activeSet.DisplayName}");
                    continue;
                }

                foreach (VhsTrack vhsTrack in vhsSet.VhsTracks)
                {
                    if (vhsTrack == null) continue;

                    if (activeSet.VhsTracks.Contains(vhsTrack))
                    {
                        MaintenanceLog(
                            $"[FORGETFULNESS BYPASSED] Client {clientId} " +
                            $"set={vhsSet.DisplayName} track={vhsTrack.DisplayName} sharedWithActiveSet=True"
                        );
                        continue;
                    }
                    float before = vhsTrack.Conveyance;

                    bool hitFloor = vhsTrack.ApplyConveyanceMultiplier(
                        ForgetfulnessConveyanceModHard,
                        MinimumVhsConveyance
                    );

                    MaintenanceLog(
                        $"[FORGETFULNESS] Client {clientId} set={vhsSet.DisplayName} {vhsTrack.DisplayName} " +
                        $"mod={decayMod:0.00} c={before:0.00}->{vhsTrack.Conveyance:0.00} floorHit={hitFloor}"
                    );
                    
                }


            }
        }
        
        private void LogCommittedPayloads(ulong clientId, NetPlayerState state)
        {
            if (state == null)
                return;

            if (state.CommittedActionPayloads.Count == 0)
            {
                SLog($"[PAYLOADS] Client {clientId} committed no payloads.");
                return;
            }

            foreach (var payload in state.CommittedActionPayloads)
            {
                PayloadLog($"[PAYLOAD] Client {clientId} {payload.ActionType}");
            }
        }
        
        private void TurnLog(string message)
        {
            if (!logTurnDebug) return;
            SLog(message);
        }

        private void PayloadLog(string message)
        {
            if (!logPayloadDebug) return;
            SLog(message);
        }

        private void ProductionLog(string message)
        {
            if (!logProductionDebug) return;
            SLog(message);
        }

        private void MaintenanceLog(string message)
        {
            if (!logMaintenanceDebug) return;
            SLog(message);
        }

        private void NetworkDebugLog(string message)
        {
            if (!logNetworkDebug) return;
            SLog(message);
        }
        
        private void BootLog(string message)
        {
            if (!logBootDebug) return;
            SLog(message);
        }

        private void EntityLog(string message)
        {
            if (!logEntityDebug) return;
            SLog(message);
        }

        private void TagLog(string message)
        {
            if (!logTagDebug) return;
            SLog(message);
        }
        
        public void BeginShutdown()
        {
            _isShuttingDown = true;

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

        private void ResolveCommittedPayloads(ulong clientId, NetPlayerState state)
        {
            if (state == null) return;

            if (state.CommittedActionPayloads.Count == 0)
            {
                ProductionLog($"[PAYLOADS] Client {clientId} committed no payloads.");
                return;
            }

            foreach (var payload in state.CommittedActionPayloads)
            {
                if (payload == null) continue;
                
                ResolveCommittedPayload(clientId, state, payload);
            }
        }

        private void ResolveCommittedPayload(
            ulong clientId,
            NetPlayerState state,
            DraftedActionPayload payload)
        {
            if (state == null || payload == null) return;

            GameEntity playerEntity = state.PlayerEntity;

            if (playerEntity == null)
            {
                ProductionLog($"[PAYLOAD BLOCKED] Client {clientId} has no player entity.");
                return;
            }

            switch (payload.ActionType)
            {
                case DraftedActionType.CreateIdea:
                    _gestationActionResolver.ResolveCreateIdea(clientId, playerEntity);
                    break;
                
                case DraftedActionType.RehearseActiveSet:
                    _rehearsalActionResolver.ResolveRehearseActiveSet(
                        clientId,
                        playerEntity,
                        state.CommittedActionsValue
                    );
                    break;
                
                default:
                    ProductionLog($"[PAYLOAD BLOCKED] Client {clientId} has no valid action type.");
                    break;
            }
        }
        
        
    }
}