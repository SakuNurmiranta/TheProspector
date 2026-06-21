/*
Architecture pin:

GameCoordinator is the host-authoritative runtime coordinator for the current
vertical slice. It owns network/session registration, game-start readiness,
turn progression, acted-state tracking, year/season state, lightweight Keeper
selection scaffolding, committed payload routing, and shutdown routing.

Current architectural rule:
GameCoordinator should not directly create ideas, tracks, rehearsal sets,
demo tapes, scene releases, circulation events, pressure effects,
or starting player entity content.

Delegated gameplay domains:
1. GamePlay.Agency.PlayerEntityBootstrapper
   Creates the player's initial in-world GameEntity / leader entity.
   Current data is vertical-slice default data, but the responsibility is durable.

2. GamePlay.Gestation.ActionResolver
   Resolves committed gestation payloads, currently idea creation from the
   player entity's available aspects and held tags.

3. GamePlay.Rehearsal.ActionResolver
   Resolves committed rehearsal payloads, currently VHS set / VHS track creation,
   active set rehearsal, and demo recording from rehearsal material.

4. GamePlay.Promotion.ActionResolver
   Resolves committed promotion payloads, currently releasing the latest demo
   into the KVLT / scene path.

5. GamePlay.World.SeededWorldState / related world-state services
   Own persistent scene-space objects, seeded collectives, hosted releases,
   scene nodes, and lookup state used by gameplay resolvers.

6. GamePlay.Pressure.SeasonPressureResolver
   Applies passive seasonal pressure after turn commitment, currently
   Forgetfulness / VHS conveyance erosion.

Current architectural rule:
- GameCoordinator may route committed payloads.
- GameCoordinator may own network/session/turn authority.
- GameCoordinator should not directly create ideas, tracks, rehearsal sets,
  demo tapes, scene releases, circulation events, pressure effects,
  or starting player entity content.
- New gameplay rule implementations should be added to domain resolvers or
  new domain services, not directly to GameCoordinator.

Remaining temporary scaffolding:
1. Keeper logic is still lightweight and coordinator-owned.
2. Turn/year flow is still coordinator-owned.
3. Payload routing is still local to GameCoordinator until more action domains
   make a separate CommittedPayloadResolver worthwhile.
4. Debug logging is still locally gated here for prototype visibility.
5. Circulation is currently represented by a dummy scene-hosting path rather
   than a proper circulation layer.
*/

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Gestation;
using SEMM91.GamePlay.Gestation.Questing;
using SEMM91.GamePlay.Promotion;
using SEMM91.GamePlay.Rehearsal;
using SEMM91.GamePlay.World;
using SeasonPressureResolver = SEMM91.GamePlay.Pressure.SeasonPressureResolver;
using PlayerEntityBootstrapper = SEMM91.GamePlay.Agency.PlayerEntityBootstrapper;

namespace SEMM91
{
    public class GameCoordinator : NetworkBehaviour
    {
        [Header("Debug Logging")] [SerializeField]
        private bool logTurnDebug;

        [SerializeField] private bool logPayloadDebug = true;
        [SerializeField] private bool logProductionDebug = true;
        [SerializeField] private bool logMaintenanceDebug;
        [SerializeField] private bool logEntityDebug;

        // -----------------------------------------------------------------------------
        // Singleton / NetworkBehaviour lifecycle
        // -----------------------------------------------------------------------------

        public static GameCoordinator Instance;
        public Texture2D gameplayBackground;

        // Establishes the local singleton and constructs non-networked helper services.
        // Does not assume that Netcode has spawned this object yet.
        private void Awake()
        {
            Instance = this;

            _gestationActionResolver =
                new GestationActionResolver(
                    ProductionLog,
                    Debug.LogError
                );

            _rehearsalActionResolver =
                new RehearsalActionResolver(
                    () => globalTurn.Value,
                    ProductionLog
                );

            _promotionActionResolver =
                new PromotionActionResolver();
            _startingCollectiveBootstrapper =
                new StartingCollectiveBootstrapper(ProductionLog);
            _seasonPressureResolver =
                new SeasonPressureResolver(MaintenanceLog);
            _playerEntityBootstrapper =
                new PlayerEntityBootstrapper(EntityLog);
            _committedActionSequenceBuilder =
                new CommittedActionSequenceBuilder();
            _characterActionHistoryRegistry =
                new CharacterActionHistoryRegistry();
            _worldEventRegistry =
                new WorldEventRegistry();

            IQuestingCompositeContributionProvider[]
                questingCompositeContributionProviders =
                {
                    new WorldEventQuestingContributionProvider(
                        _worldEventRegistry
                    )
                };

            QuestingCompositeAssembler compositeAssembler =
                new QuestingCompositeAssembler(
                    new QuestingCompositeBuilder(),
                    questingCompositeContributionProviders
                );

            _whiteSlotMachine =
                new TheWhiteSlotMachine(
                    new QuestingMoodInputReader(),
                    compositeAssembler,
                    new QuestingProjectionResolver()
                );

            _questingOutcomeApplicator =
                new QuestingOutcomeApplicator();

            _questingTurnUsageRegistry =
                new QuestingTurnUsageRegistry();
        }

        // Runs after Netcode has spawned the coordinator.
        // Server-only setup, connection callbacks, shared world bootstrap,
        // and host registration belong here.
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _gestationActionResolver.Initialize();
                testStarted.Value = false;

                _readyClients.Clear();

                _readyClients.Add(NetworkManager.ServerClientId);
                int plannedClients = BotConfig.GetIntArg("-clients", DefaultTestClientTarget);

                SLog(
                    $"READY server={NetworkManager.ServerClientId} " +
                    $"readyCount={_readyClients.Count}/{plannedClients}"
                );

                RunLog.Header(
                    role: "server",
                    testCase: BotConfig.GetStringArg("-tc", "TC-UNKNOWN"),
                    preset: BotConfig.GetStringArg("-netPreset", "P?-UNKNOWN"),
                    clientsPlanned: plannedClients,
                    botSeed: BotConfig.GetIntArg("-botSeed", 12345)
                );

                BootstrapSharedWorldIfNeeded();

                // seed for already-connected clients (incl. host)
                foreach (var id in NetworkManager.ConnectedClientsIds)
                {
                    RegisterPlayerServer(id);
                }

                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

                keeperClientId.Value = OwnerClientId; // Default to host.

                if (NetBootstrap.DedicatedServerModeActive)
                {
                    TryStartReadyGatedTestRun();
                }
                else
                {
                    TryStartPlayableSession();
                }
            }
        }

        // Cleans up callbacks owned by this coordinator instance.
        // Does not own gameplay persistence; this is runtime-session cleanup only.
        private new void OnDestroy()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }


        // -----------------------------------------------------------------------------
        // Networked session state
        // -----------------------------------------------------------------------------

        public enum Season
        {
            Spring, //vital
            Summer, //warm
            Fall, //morbid
            Winter //cold
        }

        public NetworkVariable<ulong> keeperClientId = new();
        public NetworkVariable<int> globalTurn = new();
        public NetworkVariable<int> roundIndex = new();
        public NetworkVariable<bool> testStarted = new();

        public IReadOnlyList<SeededWorldState.SceneOutputStanding> LatestSceneOutputStandings =>
            _seededWorldState?.LatestSceneOutputStandings;

        public string DominantOutputOwnerEntityId =>
            _seededWorldState?.DominantOutputOwnerEntityId;

        public float DominantOutputScore =>
            _seededWorldState?.DominantOutputScore ?? 0f;

        public bool IsPlayableSessionStarted => _gameStarted && testStarted.Value;
        [SerializeField, Min(1)] private int playablePlayersToStart = 2;
        private const int DefaultTestClientTarget = 6;
        private const int TurnsPerYear = 4;

        private const float
            ProvisionalQuestingSceneSynchronisation =
                1.0f; //this should be replaced with a comparison between the scene canon/field vs the character

        public Season CurrentSeason => (Season)(globalTurn.Value % 4);

        private bool IsEndOfYearTurn()
        {
            return globalTurn.Value > 0 && globalTurn.Value % TurnsPerYear == 0;
        }

        // -----------------------------------------------------------------------------
        // Server-side runtime state
        // -----------------------------------------------------------------------------
        // Runtime collections owned by the coordinator.
        // These track connected players, turn readiness, acted state,
        // and last resolved round snapshots.

        private readonly HashSet<ulong> _readyClients = new();
        private readonly HashSet<ulong> _actedThisTurn = new();
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();

        // Gameplay-domain services owned by the coordinator for this vertical slice.
        // GameCoordinator calls these services during turn/session flow, but should not
        // duplicate their internal domain rules.

        private PlayerEntityBootstrapper _playerEntityBootstrapper;
        private GestationActionResolver _gestationActionResolver;
        private RehearsalActionResolver _rehearsalActionResolver;
        private PromotionActionResolver _promotionActionResolver;
        private SeasonPressureResolver _seasonPressureResolver;
        private CommittedActionSequenceBuilder _committedActionSequenceBuilder;
        private CharacterActionHistoryRegistry _characterActionHistoryRegistry;
        private WorldEventRegistry _worldEventRegistry;
        private TheWhiteSlotMachine _whiteSlotMachine;
        private QuestingOutcomeApplicator _questingOutcomeApplicator;
        private QuestingTurnUsageRegistry _questingTurnUsageRegistry;

        public GestationActionResolver GestationResolver => _gestationActionResolver;
        public RehearsalActionResolver RehearsalResolver => _rehearsalActionResolver;

        private StartingCollectiveBootstrapper _startingCollectiveBootstrapper;
        private SeededWorldState _seededWorldState;


        private bool _gameStarted;
        private bool _isShuttingDown;

        public const string NodeWilderness = "SCENE_NODE_WILDERNESS";
        public const string NodeSociety = "SCENE_NODE_SOCIETY";
        public const string NodeBlackMetalBreach = "SCENE_NODE_BLACK_METAL_BREACH";
        public const string NodeKvltScene = "SCENE_NODE_KVLT";
        public const string NodeDeathMetalScene = "SCENE_NODE_DEATH_METAL";
        public const string NodeBadOrInsideSociety = "SCENE_NODE_BAD_OR_INSIDE_SOCIETY";


        // -----------------------------------------------------------------------------
        // Player registration and bootstrap
        // -----------------------------------------------------------------------------
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

                        GameEntity playerEntity =
                            _playerEntityBootstrapper.CreateStartingPlayerEntity(clientId);

                        state.SetPlayerEntity(playerEntity);

                        EntityLog(
                            $"[ENTITY TEST] client={clientId} " +
                            $"hasPlayerEntity={state.PlayerEntity != null} " +
                            $"playerEntityName={state.PlayerEntity?.DisplayName} " +
                            $"playerEntityType={state.PlayerEntity?.EntityType}"
                        );

                        if (_seededWorldState != null)
                        {
                            _startingCollectiveBootstrapper.AddPlayerLeaderToWorld(
                                _seededWorldState,
                                playerEntity,
                                clientId
                            );
                        }
                        else
                        {
                            Debug.LogWarning(
                                $"[GameCoordinator] Player {clientId} not inserted into shared world: missing SeededWorldState.");
                        }

                        // NEW: if we're in dedicated server mode, 
                        // treat the host's own player as inactive so it doesn't block lockstep.
                        if (NetBootstrap.DedicatedServerModeActive &&
                            clientId == NetworkManager.ServerClientId)
                        {
                            Debug.Log(
                                "[GameCoordinator] Host player detected in dedicatedServerMode; marking inactive.");
                            state.SetExhaustedServer(false); // just to be safe
                            state.isActive.Value = false; // or wrap this in a helper if you prefer
                        }

                        //forces a mid-game joiner to wait until change year/round
                        if (_gameStarted)
                        {
                            state.SetActiveServer(false);
                        }

                        RebuildDomainDebugSnapshot("player registered");
                    }
                    else
                    {
                        Debug.LogWarning($"GameCoordinator: No NetPlayerState on player object for client {clientId}");
                    }
                }
            }

            //force per-turn logic
            _actedThisTurn.Remove(clientId);
            
            if (_playerStates.TryGetValue(
                    clientId,
                    out NetPlayerState registeredState))
            {
                RefreshDreamAvailabilityForPlayer(
                    clientId,
                    registeredState
                );
            }
        }

        // Server callback for late or runtime client joins.
        // Registers network state and attempts game start when enough clients exist.
        private void OnClientConnected(ulong id)
        {
            if (_isShuttingDown)
                return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;

            SLog($"NET ClientConnected id={id} connectedCount={NetworkManager.ConnectedClientsIds.Count}");
            RegisterPlayerServer(id);

            if (NetBootstrap.DedicatedServerModeActive)
            {
                TryStartReadyGatedTestRun();
            }
            else
            {
                TryStartPlayableSession();
            }
        }

        // Server callback for client loss.
        // Removes turn/readiness bookkeeping and repairs Keeper ownership if needed.
        private void OnClientDisconnected(ulong id)
        {
            _readyClients.Remove(id);
            _actedThisTurn.Remove(id);
            _playerStates.Remove(id);
            _questingTurnUsageRegistry?.ClearClient(id);

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
            
            RefreshAllDreamAvailability();

            SLog($"NET ClientDisconnected id={id} connectedCount={NetworkManager.ConnectedClientsIds.Count}");
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

        private int CountEligibleConnectedPlayers()
        {
            if (NetworkManager == null)
                return 0;

            int count = 0;

            foreach (KeyValuePair<ulong, NetPlayerState> player in _playerStates)
            {
                ulong clientId = player.Key;
                NetPlayerState state = player.Value;

                if (state == null)
                    continue;

                if (!NetworkManager.ConnectedClientsIds.Contains(clientId))
                    continue;

                bool isDedicatedServerHost =
                    NetBootstrap.DedicatedServerModeActive &&
                    clientId == NetworkManager.ServerClientId;

                if (isDedicatedServerHost)
                    continue;

                count++;
            }

            return count;
        }

        private void ActivateEligiblePlayersForSessionStart()
        {
            if (NetworkManager == null)
                return;

            foreach (KeyValuePair<ulong, NetPlayerState> player in _playerStates)
            {
                ulong clientId = player.Key;
                NetPlayerState state = player.Value;

                if (state == null)
                    continue;

                if (!NetworkManager.ConnectedClientsIds.Contains(clientId))
                    continue;

                bool isDedicatedServerHost =
                    NetBootstrap.DedicatedServerModeActive &&
                    clientId == NetworkManager.ServerClientId;

                state.SetActiveServer(!isDedicatedServerHost);
                state.SetExhaustedServer(false);
            }
        }
        
        private bool StartPlayableSessionServer(string reason)
        {
            if (!IsServer)
                return false;

            if (_gameStarted || testStarted.Value)
                return false;

            int eligiblePlayers = CountEligibleConnectedPlayers();

            if (eligiblePlayers <= 0)
            {
                SLog(
                    $"GAME Start ignored | reason={reason} | " +
                    "no eligible connected players"
                );

                return false;
            }

            globalTurn.Value = 0;
            roundIndex.Value = 0;
            _actedThisTurn.Clear();

            ActivateEligiblePlayersForSessionStart();
            EnsureKeeperSelected();

            _gameStarted = true;
            testStarted.Value = true;

            RefreshAllDreamAvailability();

            SLog(
                $"GAME Started | reason={reason} | " +
                $"eligiblePlayers={eligiblePlayers} | " +
                $"connectedCount={NetworkManager.ConnectedClientsIds.Count}"
            );

            RebuildDomainDebugSnapshot(
                $"playable session started: {reason}"
            );

            BroadcastStateClientRpc();

            return true;
        }
        
        // -----------------------------------------------------------------------------
        // Shared world bootstrap
        // -----------------------------------------------------------------------------

        private void BootstrapSharedWorldIfNeeded()
        {
            if (_seededWorldState != null)
                return;

            if (_startingCollectiveBootstrapper == null)
            {
                Debug.LogError(
                    "[GameCoordinator] Cannot bootstrap shared world: missing StartingCollectiveBootstrapper.");
                return;
            }

            StartingCollectiveBootstrapResult result =
                _startingCollectiveBootstrapper.BootstrapSharedWorld();

            if (result == null || result.WorldState == null)
            {
                Debug.LogError("[GameCoordinator] Shared world bootstrap failed.");
                return;
            }

            _seededWorldState = result.WorldState;

            if (SeededWorldStateHolder.Instance != null)
            {
                SeededWorldStateHolder.Instance.SetWorldState(_seededWorldState);
            }
            else
            {
                Debug.LogWarning(
                    "[GameCoordinator] SeededWorldStateHolder.Instance is null. " +
                    "Shared world exists in GameCoordinator but was not published to holder."
                );
            }

            ProductionLog(
                "[GameCoordinator] Shared world bootstrapped | " +
                $"entities={_seededWorldState.Entities.Count}, " +
                $"hostingRecords={_seededWorldState.HostingRecords.Count}, " +
                $"sceneNodes={_seededWorldState.SceneSpaceGraph.Nodes.Count}"
            );
        }


        // -----------------------------------------------------------------------------
        // Game start readiness
        // -----------------------------------------------------------------------------
        /*private void TryStartPlayableSession()
        {
            if (!IsServer) return;
            if (_gameStarted) return;
            if (NetworkManager.ConnectedClientsIds.Count < playablePlayersToStart) return;

            globalTurn.Value = 0;
            roundIndex.Value = 0;
            _actedThisTurn.Clear();

            EnsureKeeperSelected();
            _gameStarted = true;
            testStarted.Value = true;
            RefreshAllDreamAvailability();
            SLog($"GAME Started connectedCount={NetworkManager.ConnectedClientsIds.Count}");
            RebuildDomainDebugSnapshot("playable session started");
            BroadcastStateClientRpc();
        }
*/
        
        private void TryStartPlayableSession()
        {
            if (!IsServer)
                return;

            if (_gameStarted || testStarted.Value)
                return;

            int eligiblePlayers = CountEligibleConnectedPlayers();

            if (eligiblePlayers < playablePlayersToStart)
                return;

            StartPlayableSessionServer("player-count gate");
        }
        
        /*private void TryStartReadyGatedTestRun()
        {
            if (!IsServer) return;
            if (_gameStarted) return;
            if (testStarted.Value) return;

            int connected = NetworkManager.ConnectedClientsIds.Count;
            int plannedClients = BotConfig.GetIntArg("-clients", DefaultTestClientTarget);

            if (connected < plannedClients) return;
            if (_readyClients.Count < connected) return;

            _actedThisTurn.Clear();
            globalTurn.Value = 0;
            roundIndex.Value = 0;

            EnsureKeeperSelected();
            _gameStarted = true;
            testStarted.Value = true;
            RefreshAllDreamAvailability();

            SLog($"GAME Started connectedCount={connected} readyCount={_readyClients.Count}/{plannedClients}");
            RebuildDomainDebugSnapshot("ready gated test run started");
        }*/

        private void TryStartReadyGatedTestRun()
        {
            if (!IsServer)
                return;

            if (_gameStarted || testStarted.Value)
                return;

            int connected =
                NetworkManager.ConnectedClientsIds.Count;

            int plannedClients =
                BotConfig.GetIntArg(
                    "-clients",
                    DefaultTestClientTarget
                );

            if (connected < plannedClients)
                return;

            if (_readyClients.Count < connected)
                return;

            StartPlayableSessionServer("ready-gated test run");
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ReportClientReadyServerRpc(ServerRpcParams p = default)
        {
            if (!IsServer) return;

            ulong id = p.Receive.SenderClientId;
            _readyClients.Add(id);

            int plannedClients = BotConfig.GetIntArg("-clients", DefaultTestClientTarget);
            SLog($"READY client={id} readyCount={_readyClients.Count}/{plannedClients}");

            TryStartReadyGatedTestRun();
        }


        // -----------------------------------------------------------------------------
        // Keeper scaffolding
        // -----------------------------------------------------------------------------
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

        private void ElectKeeperFromLastResolvedRound()
        {
            foreach (var kvp in _playerStates)
            {
                var state = kvp.Value;
                if (state == null) continue;

                if (state.LastResolvedRound.Count > 0)
                {
                    var best = state.LastResolvedRound
                        .OrderByDescending(pair => pair.Value.score)
                        .First();

                    SetKeeper(best.Key);
                    return;
                }
            }

            EnsureKeeperSelected();
        }

        private void YearEndKeeperValidityCheck()
        {
            // Build a snapshot of currently active player scores.
            var snapshot = new Dictionary<ulong, int>();

            foreach (var kvp in _playerStates)
            {
                ulong id = kvp.Key;
                NetPlayerState ps = kvp.Value;

                if (!ps.ActiveValue)
                    continue;

                snapshot[id] = ps.ScoreValue;
            }

            ulong currentKeeper = keeperClientId.Value;

            if (!snapshot.TryGetValue(currentKeeper, out int keeperScore))
            {
                // Current keeper is no longer active.
                EnsureKeeperSelected();
                return;
            }

            ulong bestPlayer = currentKeeper;
            int bestScore = keeperScore;

            foreach (var kvp in snapshot)
            {
                if (kvp.Value <= bestScore)
                    continue;

                bestPlayer = kvp.Key;
                bestScore = kvp.Value;
            }

            if (bestPlayer != currentKeeper)
            {
                SetKeeper(bestPlayer);
            }
        }

        // -----------------------------------------------------------------------------
// Questing / Pajazzo
// -----------------------------------------------------------------------------

        /// <summary>
        /// Resolves and applies one optional Dream for the specified
        /// authoritative player.
        ///
        /// This is a server-domain method, not a ServerRpc. A later network
        /// entry point should derive clientId from ServerRpcParams and call
        /// this method.
        /// </summary>
        public bool TryResolveDreamServer(
            ulong clientId,
            out PajazzoResolution resolution,
            out TagInstance? appliedTag,
            out string failureReason)
        {
            resolution = null;
            appliedTag = null;
            failureReason = string.Empty;

            if (!IsServer)
            {
                failureReason =
                    "Dream resolution is server-authoritative.";

                return false;
            }

            if (!_gameStarted)
            {
                failureReason =
                    "Dream resolution is unavailable before the game starts.";

                return false;
            }

            if (NetworkManager == null ||
                !NetworkManager.ConnectedClientsIds.Contains(clientId))
            {
                failureReason =
                    $"Client {clientId} is not connected.";

                return false;
            }

            if (!TryGetPlayerState(
                    clientId,
                    out NetPlayerState state))
            {
                failureReason =
                    $"Client {clientId} has no registered player state.";

                return false;
            }

            if (!state.ActiveValue)
            {
                failureReason =
                    $"Client {clientId} is not currently active.";

                return false;
            }

            if (_actedThisTurn.Contains(clientId))
            {
                failureReason =
                    $"Client {clientId} has already completed the " +
                    "current turn.";

                return false;
            }

            GameEntity character = state.PlayerEntity;

            if (character == null)
            {
                failureReason =
                    $"Client {clientId} has no authoritative player entity.";

                return false;
            }

            int currentTurn = globalTurn.Value;

            if (!_questingTurnUsageRegistry.HasUnusedDreamForTurn(
                    clientId,
                    currentTurn))
            {
                failureReason =
                    $"Client {clientId} has already used Dream on " +
                    $"global turn {currentTurn}.";

                return false;
            }

            PajazzoResolution calculatedResolution;

            try
            {
                bool resolved =
                    _whiteSlotMachine.TryResolve(
                        character,
                        currentTurn,
                        ProvisionalQuestingSceneSynchronisation,
                        out calculatedResolution,
                        out failureReason
                    );

                if (!resolved)
                    return false;
            }
            catch (System.ArgumentException exception)
            {
                failureReason =
                    "Pajazzo rejected invalid domain input: " +
                    exception.Message;

                Debug.LogError(
                    $"[PAJAZZO ERROR] client={clientId} " +
                    $"turn={currentTurn} | {failureReason}"
                );

                return false;
            }
            catch (System.InvalidOperationException exception)
            {
                failureReason =
                    "Pajazzo encountered invalid runtime state: " +
                    exception.Message;

                Debug.LogError(
                    $"[PAJAZZO ERROR] client={clientId} " +
                    $"turn={currentTurn} | {failureReason}"
                );

                return false;
            }

            bool applied =
                _questingOutcomeApplicator.TryApply(
                    character,
                    calculatedResolution.Projection,
                    out TagInstance? calculatedTag,
                    out failureReason
                );

            if (!applied)
                return false;

            bool consumed =
                _questingTurnUsageRegistry.TryConsumeDreamForTurn(
                    clientId,
                    currentTurn
                );

            if (!consumed)
            {
                failureReason =
                    $"Dream usage for client {clientId} changed during " +
                    $"resolution of global turn {currentTurn}.";

                Debug.LogError(
                    $"[PAJAZZO USAGE ERROR] {failureReason}"
                );

                return false;
            }
            
            RefreshDreamAvailabilityForPlayer(
                clientId,
                state
            );

            resolution = calculatedResolution;
            appliedTag = calculatedTag;

            string outcome;

            if (appliedTag.HasValue)
            {
                outcome = "transient-tag";
            }
            else if (resolution.Projection.IsInterrupted)
            {
                outcome = "interrupted";
            }
            else
            {
                outcome = "neutral";
            }

            ProductionLog(
                $"[PAJAZZO] " +
                $"client={clientId} " +
                $"character={character.EntityId} " +
                $"turn={currentTurn} " +
                $"axis={resolution.Projection.ActiveAxis} " +
                $"mood={resolution.Projection.InputMoodValue:F2} " +
                $"composite={resolution.Composite.CompositeValue:F2} " +
                $"sync={resolution.Projection.SynchronisationValue:F2} " +
                $"projection={resolution.Projection.ProjectedValue:F2} " +
                $"outcome={outcome}"
            );

            RebuildDomainDebugSnapshot(
                "Pajazzo Dream resolved"
            );

            return true;
        }
        
        private bool ComputeDreamAvailability(
            ulong clientId,
            NetPlayerState state)
        {
            if (!_gameStarted)
                return false;

            if (state == null || !state.ActiveValue)
                return false;

            if (NetworkManager == null ||
                !NetworkManager.ConnectedClientsIds.Contains(clientId))
            {
                return false;
            }

            if (_actedThisTurn.Contains(clientId))
                return false;

            return _questingTurnUsageRegistry
                .HasUnusedDreamForTurn(
                    clientId,
                    globalTurn.Value
                );
        }

        private void RefreshDreamAvailabilityForPlayer(
            ulong clientId,
            NetPlayerState state)
        {
            if (!IsServer || state == null)
                return;

            state.SetCanDreamServer(
                ComputeDreamAvailability(clientId, state)
            );
        }

        private void RefreshAllDreamAvailability()
        {
            if (!IsServer)
                return;

            foreach (KeyValuePair<ulong, NetPlayerState> player in
                     _playerStates)
            {
                RefreshDreamAvailabilityForPlayer(
                    player.Key,
                    player.Value
                );
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("Debug/Resolve Host Pajazzo Dream")]
        private void DebugResolveHostPajazzoDream()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[PAJAZZO DEBUG] Enter Play Mode first."
                );

                return;
            }

            if (!IsServer || NetworkManager == null)
            {
                Debug.LogWarning(
                    "[PAJAZZO DEBUG] This coordinator is not the server."
                );

                return;
            }

            ulong clientId =
                NetworkManager.ServerClientId;

            bool success =
                TryResolveDreamServer(
                    clientId,
                    out PajazzoResolution resolution,
                    out TagInstance? appliedTag,
                    out string failureReason
                );

            if (!success)
            {
                Debug.LogWarning(
                    $"[PAJAZZO DEBUG] Dream blocked | " +
                    $"client={clientId} | {failureReason}"
                );

                return;
            }

            Debug.Log(
                $"[PAJAZZO DEBUG] Dream resolved | " +
                $"client={clientId} " +
                $"q={resolution.Projection.ProjectedValue:F2} " +
                $"interrupted={resolution.Projection.IsInterrupted} " +
                $"tagApplied={appliedTag.HasValue}"
            );
        }
        
#endif
        // -----------------------------------------------------------------------------
        // Draft / commit entry points
        // -----------------------------------------------------------------------------

        // Authoritative server-side entry point for a player's committed turn.
        // The player's local draft has already been locked into NetPlayerState.
        // This method resolves the committed payloads, applies passive seasonal effects,
        // marks the player as acted, and advances the global turn if all active players
        // have acted.
        public void CompleteCommittedTurn(ulong clientId, NetPlayerState state)
        {
            if (!IsServer) return;
            if (state == null) return;
            if (!NetworkManager.ConnectedClientsIds.Contains(clientId)) return;

            if (!CanActThisTurn(clientId, state))
            {
                Debug.LogWarning(
                    $"[TURN COMMIT REJECTED] Client {clientId} " +
                    "is not eligible to act."
                );

                return;
            }
            
            LogCommittedPayloads(clientId, state);
            ResolveCommittedPayloadBatch(clientId, state);
            _seasonPressureResolver.ApplySeasonPressure(clientId, state);

            RebuildDomainDebugSnapshot("committed payloads resolved");

            TurnLog($"[TURN COMMIT] Client {clientId} locked stance {state.CurrentStanceValue}");

            MarkActedAndAdvanceIfReady(clientId);
        }

        // Returns whether the client is currently allowed to submit turn actions.
        public bool CanClientAct(ulong clientId)
        {
            if (!TryGetPlayerState(clientId, out var state))
                return false;

            return CanActThisTurn(clientId, state);
        }


        public bool HasPlayerActed(ulong clientId)
        {
            return _actedThisTurn.Contains(clientId);
        }

        private bool CanActThisTurn(
            ulong clientId,
            NetPlayerState state)
        {
            if (!_gameStarted || !testStarted.Value)
                return false;

            if (state == null || !state.ActiveValue)
                return false;

            if (_actedThisTurn.Contains(clientId))
                return false;

            return true;
        }

        // Records that this client has completed the current turn and checks whether
        // the whole session can advance
        private void MarkActedAndAdvanceIfReady(ulong senderClientId)
        {
            _actedThisTurn.Add(senderClientId);


            if (AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
            }
            else
            {
                if (_playerStates.TryGetValue(
                        senderClientId,
                        out NetPlayerState state))
                {
                    RefreshDreamAvailabilityForPlayer(
                        senderClientId,
                        state
                    );
                }
                
                BroadcastStateClientRpc();
            }
        }

        private bool AllActivePlayersActed()
        {
            if (!_gameStarted || !testStarted.Value)
                return false;

            int activePlayerCount = 0;

            foreach (KeyValuePair<ulong, NetPlayerState> player in _playerStates)
            {
                ulong clientId = player.Key;
                NetPlayerState state = player.Value;

                if (state == null || !state.ActiveValue)
                    continue;

                activePlayerCount++;

                if (!_actedThisTurn.Contains(clientId))
                    return false;
            }

            return activePlayerCount > 0;
        }

        // -----------------------------------------------------------------------------
        // Committed payload routing
        // -----------------------------------------------------------------------------
        // This section is the current seam between turn authority and gameplay-domain
        // resolution. GameCoordinator owns the fact that committed payloads are resolved
        // during turn commit, but the meaning of each payload belongs to gameplay-domain
        // resolvers.
        //
        // Extraction candidate:
        // If this switch grows beyond simple dispatch, move it into a
        // CommittedPayloadResolver service.
        private void ResolveCommittedPayloadBatch(
            ulong clientId,
            NetPlayerState state)
        {
            if (state == null)
                return;

            if (state.CommittedActionPayloads.Count == 0)
            {
                ProductionLog(
                    $"[PAYLOADS] Client {clientId} committed no payloads."
                );
            }
            else
            {
                ProductionLog(
                    $"[PAYLOADS] Client {clientId} committed payloads: " +
                    string.Join(
                        ", ",
                        state.CommittedActionPayloads.Select(
                            payload =>
                                payload != null
                                    ? payload.ActionType.ToString()
                                    : "null"
                        )
                    )
                );
            }

            IReadOnlyList<CommittedActionSlot> slots;

            try
            {
                slots = _committedActionSequenceBuilder.Build(
                    state.CommittedActionPayloads
                );
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogError(
                    $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                    exception.Message
                );

                return;
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError(
                    $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                    exception.Message
                );

                return;
            }
            
            Dictionary<int, DraftedActionPayload>
                payloadsByActionPosition = new();

            int committedPayloadIndex = 0;

            foreach (CommittedActionSlot slot in slots)
            {
                if (slot == null)
                {
                    Debug.LogError(
                        $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                        "sequence contains a null slot."
                    );

                    return;
                }

                if (slot.IsImplicit)
                    continue;

                if (committedPayloadIndex >=
                    state.CommittedActionPayloads.Count)
                {
                    Debug.LogError(
                        $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                        "sequence contains more explicit slots than " +
                        "committed payloads."
                    );

                    return;
                }

                DraftedActionPayload payload =
                    state.CommittedActionPayloads[
                        committedPayloadIndex
                    ];

                committedPayloadIndex++;

                if (payload == null)
                {
                    Debug.LogError(
                        $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                        $"explicit slot {slot.ActionPosition} " +
                        "maps to a null payload."
                    );

                    return;
                }

                if (payload.ActionType != slot.ActionType)
                {
                    Debug.LogError(
                        $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                        $"slot {slot.ActionPosition} is " +
                        $"{slot.ActionType}, but its payload is " +
                        $"{payload.ActionType}."
                    );

                    return;
                }

                payloadsByActionPosition.Add(
                    slot.ActionPosition,
                    payload
                );
            }

            if (committedPayloadIndex !=
                state.CommittedActionPayloads.Count)
            {
                Debug.LogError(
                    $"[ACTION SEQUENCE ERROR] Client {clientId} | " +
                    "not every committed payload was mapped to an " +
                    "explicit action slot."
                );

                return;
            }
            
            ProductionLog(
                $"[ACTION SEQUENCE] Client {clientId}: " +
                string.Join(
                    ", ",
                    slots.Select(
                        slot =>
                            $"{slot.ActionPosition}:{slot.ActionType}" +
                            $"{(slot.IsImplicit ? "(implicit)" : "")}"
                    )
                )
            );

            Dictionary<int, bool?> resultsByPosition = new();
            List<CommittedActionSlot> recordingSlots = new();

            foreach (CommittedActionSlot slot in slots)
            {
                payloadsByActionPosition.TryGetValue(
                    slot.ActionPosition,
                    out DraftedActionPayload sourcePayload
                );
                
                if (slot.ActionType ==
                    DraftedActionType.RecordActiveSetToDemo)
                {
                    recordingSlots.Add(slot);
                    continue;
                }

                bool? wasSuccessful =
                    ResolveSingleCommittedActionSlot(
                        clientId,
                        state,
                        slot,
                        sourcePayload
                    );

                resultsByPosition[slot.ActionPosition] =
                    wasSuccessful;
            }

            if (recordingSlots.Count > 0)
            {
                bool recordingSucceeded =
                    ResolveRecordingPayloadStack(
                        clientId,
                        state,
                        recordingSlots.Count
                    );

                foreach (CommittedActionSlot recordingSlot
                         in recordingSlots)
                {
                    resultsByPosition[
                        recordingSlot.ActionPosition
                    ] = recordingSucceeded;
                }

                Debug.Log(
                    $"[RECORD STACK TEST] " +
                    $"client={clientId} takes={recordingSlots.Count}"
                );
            }

            foreach (CommittedActionSlot slot in slots)
            {
                resultsByPosition.TryGetValue(
                    slot.ActionPosition,
                    out bool? wasSuccessful
                );

                RecordResolvedAction(
                    clientId,
                    state,
                    slot,
                    wasSuccessful
                );
            }
        }

        private bool? ResolveSingleCommittedActionSlot(
            ulong clientId,
            NetPlayerState state,
            CommittedActionSlot slot,
            DraftedActionPayload sourcePayload)
        {
            if (state == null || slot == null)
                return false;

            GameEntity playerEntity = state.PlayerEntity;

            if (playerEntity == null)
            {
                ProductionLog(
                    $"[PAYLOAD BLOCKED] Client {clientId} " +
                    $"has no player entity."
                );

                return false;
            }

            switch (slot.ActionType)
            {
                case DraftedActionType.CreateIdea:
                {
                    if (sourcePayload == null)
                    {
                        ProductionLog(
                            $"[GESTATE BLOCKED] Client {clientId} " +
                            "CreateIdea has no source payload."
                        );

                        return false;
                    }

                    if (sourcePayload.ActionType !=
                        DraftedActionType.CreateIdea)
                    {
                        ProductionLog(
                            $"[GESTATE BLOCKED] Client {clientId} " +
                            $"CreateIdea received payload type " +
                            $"{sourcePayload.ActionType}."
                        );

                        return false;
                    }

                    if (!sourcePayload
                            .IdeaSourceContainerType
                            .HasValue)
                    {
                        ProductionLog(
                            $"[GESTATE BLOCKED] Client {clientId} " +
                            "CreateIdea payload has no Idea source."
                        );

                        return false;
                    }

                    _gestationActionResolver.ResolveCreateIdea(
                        clientId,
                        playerEntity,
                        sourcePayload
                            .IdeaSourceContainerType
                            .Value
                    );

                    // Resolver still returns void at this stage.
                    return null;
                }

                case DraftedActionType.RehearseActiveSet:
                    _rehearsalActionResolver
                        .ResolveRehearseActiveSet(
                            clientId,
                            playerEntity,
                            state.CommittedActionsValue
                        );

                    // The resolver currently returns void.
                    return null;

                case DraftedActionType
                    .DebugPlaceholderGestationSecondary:

                    ProductionLog(
                        $"[PLACEHOLDER] Client {clientId} resolved " +
                        $"gestation secondary placeholder."
                    );

                    return null;

                case DraftedActionType.ReleaseLatestDemoToKvlt:
                {
                    if (_promotionActionResolver == null)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            $"missing promotion resolver."
                        );

                        return false;
                    }

                    bool success =
                        _promotionActionResolver
                            .TryReleaseLatestDemoToKvlt(
                                clientId,
                                playerEntity,
                                _seededWorldState,
                                out string message
                            );

                    ProductionLog(
                        $"[PROMOTION] success={success} | {message}"
                    );

                    return success;
                }

                case DraftedActionType
                    .DebugPlaceholderPromotionPrimary:

                    ProductionLog(
                        $"[PLACEHOLDER] Client {clientId} resolved " +
                        $"promotion primary placeholder."
                    );

                    return null;

                case DraftedActionType
                    .DebugPlaceholderPromotionSecondary:

                    ProductionLog(
                        $"[PLACEHOLDER] Client {clientId} resolved " +
                        $"promotion secondary placeholder."
                    );

                    return null;

                case DraftedActionType.Rest:
                {
                    state.SetExhaustedServer(false);

                    string restOrigin =
                        slot.IsImplicit
                            ? "implicit"
                            : "explicit";

                    ProductionLog(
                        $"[REST] Client {clientId} rested. " +
                        $"origin={restOrigin} " +
                        $"position={slot.ActionPosition}"
                    );

                    return true;
                }

                case DraftedActionType.RecordActiveSetToDemo:
                    ProductionLog(
                        $"[RECORD BLOCKED] Client {clientId} " +
                        $"RecordActiveSetToDemo should be resolved " +
                        $"as a stack."
                    );

                    return false;

                default:
                    ProductionLog(
                        $"[PAYLOAD BLOCKED] Client {clientId} " +
                        $"has no valid action type."
                    );

                    return false;
            }

           
        }

        private bool ResolveRecordingPayloadStack(
            ulong clientId,
            NetPlayerState state,
            int takeCount)
        {
            if (state == null)
                return false;

            GameEntity playerEntity = state.PlayerEntity;

            if (playerEntity == null)
            {
                ProductionLog(
                    $"[RECORD BLOCKED] Client {clientId} " +
                    $"has no player entity."
                );

                return false;
            }

            if (_rehearsalActionResolver == null)
            {
                ProductionLog(
                    $"[RECORD BLOCKED] Client {clientId} " +
                    $"missing recording resolver."
                );

                return false;
            }

            bool success =
                _rehearsalActionResolver
                    .TryRecordActiveSetToDemo(
                        clientId,
                        playerEntity,
                        takeCount,
                        out string message
                    );

            ProductionLog(message);

            return success;
        }

        private void RecordResolvedAction(
            ulong clientId,
            NetPlayerState state,
            CommittedActionSlot slot,
            bool? wasSuccessful)
        {
            if (state == null || slot == null)
                return;

            GameEntity playerEntity = state.PlayerEntity;

            if (playerEntity == null)
            {
                Debug.LogError(
                    $"[ACTION HISTORY ERROR] Client {clientId} " +
                    $"has no player entity."
                );

                return;
            }

            CharacterActionKey actionKey =
                new CharacterActionKey(
                    playerEntity.EntityId,
                    globalTurn.Value,
                    slot.ActionPosition
                );

            CharacterActionRecord record =
                new CharacterActionRecord(
                    actionKey: actionKey,
                    clientId: clientId,
                    roundIndex: roundIndex.Value,
                    stance: state.CurrentStanceValue,
                    actionType: slot.ActionType,
                    isImplicit: slot.IsImplicit,
                    wasSuccessful: wasSuccessful
                );

            try
            {
                _characterActionHistoryRegistry.Record(record);
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogError(
                    $"[ACTION HISTORY ERROR] " +
                    $"character={playerEntity.EntityId} " +
                    $"turn={globalTurn.Value} " +
                    $"position={slot.ActionPosition} | " +
                    exception.Message
                );

                return;
            }
            catch (System.InvalidOperationException exception)
            {
                Debug.LogError(
                    $"[ACTION HISTORY ERROR] " +
                    $"character={playerEntity.EntityId} " +
                    $"turn={globalTurn.Value} " +
                    $"position={slot.ActionPosition} | " +
                    exception.Message
                );

                return;
            }

            string successText =
                wasSuccessful.HasValue
                    ? wasSuccessful.Value.ToString()
                    : "unknown";

            PayloadLog(
                $"[ACTION HISTORY] " +
                $"character={playerEntity.EntityId} " +
                $"turn={globalTurn.Value} " +
                $"round={roundIndex.Value} " +
                $"position={slot.ActionPosition} " +
                $"stance={state.CurrentStanceValue} " +
                $"action={slot.ActionType} " +
                $"implicit={slot.IsImplicit} " +
                $"success={successText}"
            );
        }

        // -----------------------------------------------------------------------------
        // Turn and year progression
        // -----------------------------------------------------------------------------

        // Advances the authoritative season counter.
        // Year-end maintenance happens here because the year boundary is derived from
        // globalTurn and must remain server-authoritative.
        private void AdvanceGlobalTurn()
        {
            if (_isShuttingDown)
                return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;

            // clear actions for next turn
            _actedThisTurn.Clear();
            ResetPlayerActionsForNewTurn();

            int prevRound = roundIndex.Value;
            StorePreviousStancesForTurnBoundary();
            
            if (_seededWorldState != null)
            {
                _seededWorldState.ResolveTagLifecyclesAtTurnBoundary();
            }
            
            globalTurn.Value++;

            if (_seededWorldState != null)
            {
                _seededWorldState.TickSceneReleaseCirculation(globalTurn.Value);
                _seededWorldState.EvaluateSceneOutputStandings(globalTurn.Value);
            }

            RebuildDomainDebugSnapshot("scene output evaluated");

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
            RefreshAllDreamAvailability();
            BroadcastStateClientRpc();
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

        private void StorePreviousStancesForTurnBoundary()
        {
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
            {
                playerState.StorePreviousStanceServer();
                TurnLog(
                    $"[STANCE] Client {playerState.OwnerClientId} stored previous stance: {playerState.PreviousStanceValue}");
            }
        }

        private void RolloverPlayerStancesForNewTurn()
        {
            foreach (var playerState in FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None))
            {
                TurnLog($"[STANCE] Client {playerState.OwnerClientId} continues as {playerState.CurrentStanceValue}");
            }
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
                    score = ps.ScoreValue,
                    isActive = ps.ActiveValue
                };
            }

            keeperState.LastResolvedRound = newSnapshot;

            foreach (var kvp in _playerStates)
            {
                if (kvp.Key == keeper) continue; //skip keeper (already updated) 
                kvp.Value.LastResolvedRound = new Dictionary<ulong, NetPlayerState.LastResolvedRoundData>(newSnapshot);
            }
        }

        private void ReactivateInactivePlayersAtYearEnd()
        {
            foreach (KeyValuePair<ulong, NetPlayerState> player in _playerStates)
            {
                ulong clientId = player.Key;
                NetPlayerState state = player.Value;

                if (state == null)
                    continue;

                bool isDedicatedServerHost =
                    NetBootstrap.DedicatedServerModeActive &&
                    NetworkManager != null &&
                    clientId == NetworkManager.ServerClientId;

                if (isDedicatedServerHost)
                    continue;

                if (!state.ActiveValue)
                {
                    state.SetActiveServer(true);
                }
            }
        }

        [ClientRpc]
        private void BroadcastStateClientRpc()
        {
            // For MVP, just UI text is enough; no per-client data push needed beyond NetworkVariables
        }


        // -----------------------------------------------------------------------------
        // Debug / diagnostics
        // -----------------------------------------------------------------------------

        private void SLog(string msg)
        {
            if (!IsServer) return;
            Debug.Log(
                $"[S] t={Time.realtimeSinceStartup:F2} round={roundIndex.Value} turn={globalTurn.Value} keeper={keeperClientId.Value} :: {msg}");
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

        private void EntityLog(string message)
        {
            if (!logEntityDebug) return;
            SLog(message);
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

        private void RebuildDomainDebugSnapshot(string reason)
        {
            if (!IsServer)
                return;

            var snapshotReplicator = DomainSnapshotReplicator.Instance;

            if (snapshotReplicator == null)
            {
                Debug.LogWarning(
                    $"[GameCoordinator] Snapshot rebuild skipped | reason={reason} | no DomainSnapshotReplicator instance");
                return;
            }

            if (!snapshotReplicator.IsSnapshotNetworkReady)
            {
                Debug.Log(
                    $"[GameCoordinator] Snapshot rebuild deferred/skipped | reason={reason} | replicator not network-ready yet");
                return;
            }

            snapshotReplicator.RebuildSnapshotsFromServerDomain();

            Debug.Log($"[GameCoordinator] Snapshot rebuild requested | reason={reason}");
        }


        // -----------------------------------------------------------------------------
        // Shutdown / application control
        // -----------------------------------------------------------------------------

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

        /*public void ForceStartPlayableSessionServer()
        {
            if (!IsServer)
                return;

            if (_gameStarted)
            {
                SLog("GAME Force start ignored: game already started.");
                return;
            }

            int connectedCount = NetworkManager.ConnectedClientsIds.Count;

            if (connectedCount <= 0)
            {
                SLog("GAME Force start ignored: no connected clients.");
                return;
            }

            _gameStarted = true;
            testStarted.Value = true;

            SLog($"GAME Force started connectedCount={connectedCount}");
        }*/
        
        public bool ForceStartPlayableSessionServer()
        {
            if (!IsServer)
                return false;

            if (NetworkManager == null ||
                !NetworkManager.IsHost ||
                NetBootstrap.DedicatedServerModeActive)
            {
                SLog(
                    "GAME Host override rejected: " +
                    "caller is not a local listen-server host."
                );

                return false;
            }

            if (_gameStarted || testStarted.Value)
            {
                SLog(
                    "GAME Host override ignored: " +
                    "session already started."
                );

                return false;
            }

            return StartPlayableSessionServer("host override");
        }
    }
}