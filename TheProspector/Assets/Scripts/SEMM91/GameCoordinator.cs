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

        private IdeaFactory _ideaFactory;

        
        //cached mapping for client states
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();
        private bool _gameStarted = false;
        private bool _gameEnded;
        private readonly ulong _finalWinner = ulong.MaxValue;

        public bool GameEnded => _gameEnded;
        public ulong FinalWinner => _finalWinner;
        
        private const float ForgetfulnessConveyanceModHard = 0.95f; //These don't really belong here
        private const float ForgetfulnessConveyanceModSoft = 0.98f;
        private const float MinimumVhsConveyance = 0.1f;

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
                InitializeIdeaFactory();
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
                        
                        Debug.Log(
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
        public void CompleteCommittedTurn(ulong senderClientId, NetPlayerState state)
        {
            if (!IsServer) return;
            if (state == null) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(senderClientId)) return;

            ResolveCommittedStanceOutcome(senderClientId, state);
            ApplyTurnCommitMaintenanceEffects(senderClientId, state);

            SLog($"[TURN COMMIT] Client {senderClientId} locked stance {state.CurrentStanceValue}");

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
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
            {
                SLog($"[STANCE] Client {playerState.OwnerClientId} continues as {playerState.CurrentStanceValue}");
            }
        }
        
        private void StorePreviousStancesForTurnBoundary()
        {
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
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
        
        private void ResolveCommittedStanceOutcome(ulong clientId, NetPlayerState state)
        {
            GameEntity controller = state.PlayerEntity;

            if (controller == null)
            {
                SLog($"[BLOCKED] Client {clientId} has no controller entity.");
                return;
            }
            
            byte actions = state.CommittedActionsValue;

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    SLog(
                        $"[GESTATE] Client {clientId} controller={controller.DisplayName} " +
                        $"actions={actions} aspects={controller.AspectIds.Count} " +
                        $"tagContainers={controller.TagContainers.Count} ideas={controller.Ideas.Count}"
                    );

                    CreateTestIdeaFromPlayerEntity(clientId, controller); //placeholder logic
                    break;

                case BandStance.Rehearse:
                    SLog(
                        $"[REHEARSE] Client {clientId} controller={controller.DisplayName} " +
                        $"actions={actions} availableIdeas={controller.Ideas.Count}"
                    );
                    
                    CreateTestVhsTrackFromPlayerEntityIdeas(clientId, controller, actions); //placeholder logic
                    break;

                case BandStance.Promote:
                    SLog($"[PROMOTE] Client {clientId} increased visibility by {actions} effort.");
                    break;

                default:
                    SLog($"[OUTCOME BLOCKED] Client {clientId} has no valid stance.");
                    break;
            }
        }

        private void CreateTestIdeaFromPlayerEntity(ulong clientId, GameEntity controller)
        {
            
            if (controller == null) return;
            if (controller.AspectIds.Count == 0)
            {
                SLog($"[BLOCKED] Client {clientId} has no aspects.");
                return;
            }

            if (!controller.TryGetTagContainer(TagContainerType.Conviction, out var tagContainer))
            {
                SLog($"[BLOCKED] Client {clientId} has no conviction tag container.");
                return;
            }

            if (!tagContainer.HasHeldTag)
            {
                SLog($"[BLOCKED] Client {clientId} has no conviction tag.");
                return;
            }
            
            if (_ideaFactory == null)
            {
                SLog($"[BLOCKED] Client {clientId} has no idea factory initialized.");
                return;
            }

            string aspectId = controller.AspectIds.First();


            
            bool created = _ideaFactory.TryCreateIdeaFromHeldTag(
                controller,
                aspectId,
                tagContainer,
                0.5f,
                out var idea
                );

            if (!created)
            {
                SLog($"[BLOCKED] Client {clientId} could not create idea from held tag.");
                return;
            }
            
            controller.AddIdea(idea);

            SLog(
                $"[GESTATE CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"idea={idea} totalIdeas={controller.Ideas.Count}"
            );
        }
        
        private void InitializeIdeaFactory()
        {
            TextAsset globalJson = Resources.Load<TextAsset>("AspectData/GlobalAspects");

            if (globalJson == null)
            {
                Debug.LogError("Could not load global aspect data.");
                return;
            }
            
            var globalRegistry = new GlobalAspectRegistry();
            globalRegistry.LoadFromJson(globalJson);
            
            var usabilityEvaluator = new AspectUsabilityEvaluator(globalRegistry);
            _ideaFactory = new IdeaFactory(usabilityEvaluator);

            Debug.Log("Idea factory initialized.");
        }

        private float GetRehearsalConveyanceGain(byte committedActions)
        {
            return committedActions switch
            {
                1 => 0.10f,
                2 => 0.20f,
                >= 3 => 0.30f,
                _ => 0.0f
            };
        }
        private void CreateTestVhsTrackFromPlayerEntityIdeas(
            ulong clientId, 
            GameEntity controller, 
            byte committedActions)
        {
            if (controller == null)
            {
                SLog($"[BLOCKED] Client {clientId} has no controller entity.");
                return;
            }

            if (controller.Ideas.Count == 0) 
            {
                if (committedActions >= 3) // this doesn't feel arbitrary at all; just a placeholder for creating a new vhs set
                {
                    CreateAndActivateNewVhsSet(clientId, controller);
                    return;
                }
                
                RehearseActiveVhsSet(clientId, controller, committedActions);
                return;
            }

           VhsSet activeSet = GetOrCreateActiveVhsSet(clientId, controller);

            string vhsTrackId = System.Guid.NewGuid().ToString();
            string vhsTrackName = $"Track_{activeSet.VhsTracks.Count + 1}";

            float conveyance = committedActions switch
            {
                1 => 0.35f,
                2 => 0.60f,
                >= 3 => 0.85f,
                _ => 0.0f
            };
    
            VhsTrack vhsTrack = new VhsTrack(
                vhsTrackId, 
                vhsTrackName, 
                conveyance,
                globalTurn.Value
            );

            Idea idea = controller.Ideas[0];
    
            vhsTrack.AddIdea(idea);

            if (!controller.RemoveIdea(idea))
            {
                SLog($"[BLOCKED] Client {clientId} could not remove idea from controller.");
                return;
            }
    
            activeSet.AddTrack(vhsTrack);
    
            SLog(
                $"[REHEARSE CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeSet.DisplayName} vhsTrack={vhsTrack.DisplayName} " +
                $"ideasInVhs={vhsTrack.Ideas.Count} conveyance={vhsTrack.Conveyance:0.00} " +
                $"rehearsals={vhsTrack.RehearsalCount} raw={vhsTrack.IsRaw} " +
                $"honed={vhsTrack.IsHoned} totalVhsTracks={controller.GetTotalVhsTrackCountFromSets()}"
            );
        }


        private void RehearseActiveVhsSet(
            ulong clientId,
            GameEntity controller,
            byte committedActions)
        {

            VhsSet activeVhsSet = controller.GetActiveVhsSet();

            if (activeVhsSet == null)
            {
                SLog($"[BLOCKED] Client {clientId} has no vhs set.");
                return;
            }
            
            if (activeVhsSet.VhsTracks.Count == 0)
            {
                SLog($"[BLOCKED] Client {clientId} has no vhs tracks.");
                return;
            }
            
            float gain = GetRehearsalConveyanceGain(committedActions);
            
            activeVhsSet.RehearseAll(gain, globalTurn.Value);
            
            SLog(
                $"[REHEARSE SET UPDATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeVhsSet.DisplayName} tracks={activeVhsSet.VhsTracks.Count} " +
                $"gain={gain:0.00} lastRehearsedTurn={activeVhsSet.LastRehearsedTurn}"
            );
            
            
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
                SLog($"Forgetfulness: Client {clientId} has no controller entity.");
                return;
            }

            // NOTE TO SELF: IDEA LEVEL DECAY COULD ACTUALLY BE A THING,
            // but for now Forgetfulness works at VHS set level.

            VhsSet activeSet = controller.GetActiveVhsSet();

            if (activeSet == null)
            {
                SLog($"[FORGETFULNESS BLOCKED] Client {clientId} has no VHS set.");
                return;
            }

            if (activeSet.VhsTracks.Count == 0)
            {
                SLog($"[FORGETFULNESS NOTE] Client {clientId} active VHS set has no tracks.");
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
                    SLog($"No forgetfulness for active set {activeSet.DisplayName}");
                    continue;
                }

                foreach (VhsTrack vhsTrack in vhsSet.VhsTracks)
                {
                    if (vhsTrack == null) continue;

                    if (activeSet.VhsTracks.Contains(vhsTrack))
                    {
                        SLog(
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

                    SLog(
                        $"[FORGETFULNESS] Client {clientId} set={vhsSet.DisplayName} {vhsTrack.DisplayName} " +
                        $"mod={decayMod:0.00} c={before:0.00}->{vhsTrack.Conveyance:0.00} floorHit={hitFloor}"
                    );
                    
                }


            }
        }

        private VhsSet GetOrCreateActiveVhsSet(ulong clientId, GameEntity controller)
        {
            VhsSet activeSet = controller.GetActiveVhsSet();
            
            if (activeSet != null) return activeSet;

            string setId = System.Guid.NewGuid().ToString();
            string setName = $"Set_{controller.VhsSets.Count + 1}";

            activeSet = new VhsSet(setId, setName, globalTurn.Value);
            
            controller.AddVhsSet(activeSet);
            controller.SetActiveVhsSet(activeSet);
            
            SLog(
                $"[VHS SET CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"set={activeSet.DisplayName} totalSets={controller.VhsSets.Count}"
                );
            
            return activeSet;
        }

        private VhsSet CreateAndActivateNewVhsSet(ulong clientId, GameEntity controller)
        {
            if (controller == null)
            {
                SLog($"[BLOCKED] Client {clientId} has no controller entity.");
                return null;
            }

            string setId = System.Guid.NewGuid().ToString();
            string setName = $"Set_{controller.VhsSets.Count + 1}";

            VhsSet newSet = new VhsSet(setId, setName, globalTurn.Value);
            
            controller.AddVhsSet(newSet);
            controller.SetActiveVhsSet(newSet);
            
            SLog(
                $"[VHS SET CREATED] Client {clientId} controller={controller.DisplayName} " +
                $"activeSet={newSet.DisplayName} totalSets={controller.VhsSets.Count}"
            );

            return newSet;
        }
        
        
        
        
    }
}