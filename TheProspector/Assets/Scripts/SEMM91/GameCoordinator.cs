/*
Architecture pin:

GameCoordinator is the host-authoritative runtime coordinator for the current
vertical slice. It owns network/session registration, game-start readiness,
turn progression, acted-state tracking, year/season state, Keeper transition orchestration,
committed payload routing, and shutdown routing.

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

7. GamePlay.Keeper
   7. GamePlay.Keeper
   KeeperTransitionResolver classifies succession without mutating runtime
   state. KeeperRoleResolver applies institutional collective membership,
   band dormancy, and KVLT leadership. KeeperLegacyResolver applies
   SceneRelease legacy consequences and returns the resulting immutable
   Keeper tenure.

Current architectural rule:
- GameCoordinator may route committed payloads.
- GameCoordinator may own network/session/turn authority.
- GameCoordinator should not directly create ideas, tracks, rehearsal sets,
  demo tapes, scene releases, circulation events, pressure effects,
  or starting player entity content.
- New gameplay rule implementations should be added to domain resolvers or
  new domain services, not directly to GameCoordinator.

Remaining temporary scaffolding:
1. Keeper command restrictions, Pull spending, interventions, and the full
   cluster/canon system remain future Keeper-domain work.
2. Turn/year flow is still coordinator-owned.
3. Payload routing is still local to GameCoordinator until more action domains
   make a separate CommittedPayloadResolver worthwhile.
4. Debug logging is still locally gated here for prototype visibility.
5. Circulation is currently represented by a dummy scene-hosting path rather
   than a proper circulation layer.
*/

using System.Collections.Generic;
using System.Linq;
using SEMM91.Core.Entities;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Gestation;
using SEMM91.GamePlay.Gestation.Questing;
using SEMM91.GamePlay.Promotion;
using SEMM91.GamePlay.Rehearsal;
using SEMM91.GamePlay.Keeper;
using SEMM91.GamePlay.World;
using SEMM91.InputSystems;
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
            _keeperTransitionResolver =
                new KeeperTransitionResolver();
            _keeperLegacyResolver =
                new KeeperLegacyResolver();
            _keeperRoleResolver =
                new KeeperRoleResolver();
            _keeperInterventionResolver =
                new KeeperInterventionResolver();

            _latestKeeperTransitionResult =
                KeeperTransitionResult.None;

            _currentKeeperTenure = null;

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

                //keeperClientId.Value = OwnerClientId; // Default to host.

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

        public NetworkVariable<ulong> keeperClientId =
            new(
                ulong.MaxValue,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        public NetworkVariable<int> globalTurn = new();
        public NetworkVariable<int> roundIndex = new();
        public NetworkVariable<bool> testStarted = new();

        public IReadOnlyList<SeededWorldState.SceneOutputStanding>
            LatestSceneOutputStandings =>
            _seededWorldState?.LatestSceneOutputStandings;

        public IReadOnlyList<SceneRelease>
            SceneReleases =>
            _seededWorldState?.SceneReleases;

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
        private KeeperTransitionResolver _keeperTransitionResolver;
        private KeeperLegacyResolver _keeperLegacyResolver;
        private KeeperRoleResolver _keeperRoleResolver;
        private KeeperInterventionResolver _keeperInterventionResolver;
        private KeeperTransitionResult _latestKeeperTransitionResult;
        private KeeperTenureState _currentKeeperTenure;

        private CommittedActionSequenceBuilder _committedActionSequenceBuilder;
        private CharacterActionHistoryRegistry _characterActionHistoryRegistry;
        private WorldEventRegistry _worldEventRegistry;
        private TheWhiteSlotMachine _whiteSlotMachine;
        private QuestingOutcomeApplicator _questingOutcomeApplicator;
        private QuestingTurnUsageRegistry _questingTurnUsageRegistry;

        public GestationActionResolver GestationResolver => _gestationActionResolver;
        public RehearsalActionResolver RehearsalResolver => _rehearsalActionResolver;

        public KeeperTransitionResult
            LatestKeeperTransitionResult =>
            _latestKeeperTransitionResult;

        public KeeperTenureState
            CurrentKeeperTenure =>
            _currentKeeperTenure;

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
                        playerEntity.SetExhausted(false);

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
                            //state.SetExhaustedServer(false); 
                            state.SetActiveServer(false);
                        }

                        //forces a mid-game joiner to wait until change year/round
                        if (_gameStarted)
                        {
                            state.SetActiveServer(false);
                        }

                        PublishDomainProjectionServer("player registered");
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
        private void OnClientDisconnected(
            ulong id)
        {
            bool disconnectedPlayerWasKeeper =
                keeperClientId.Value == id;

            _readyClients.Remove(id);
            _actedThisTurn.Remove(id);
            _playerStates.Remove(id);

            _questingTurnUsageRegistry?
                .ClearClient(id);

            if (disconnectedPlayerWasKeeper)
            {
                ResolveKeeperDisconnectionFallbackServer(
                    id
                );
            }

            bool advancedTurn = false;

            /*
             * If removing the client completes the acted-player set,
             * AdvanceGlobalTurn performs the final publication itself.
             */
            if (IsServer &&
                AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
                advancedTurn = true;
            }

            if (!advancedTurn)
            {
                RefreshAllDreamAvailability();

                PublishDomainProjectionServer(
                    disconnectedPlayerWasKeeper
                        ? "keeper disconnect fallback resolved"
                        : "player disconnected"
                );

                BroadcastStateClientRpc();
            }

            SLog(
                $"NET ClientDisconnected id={id} " +
                $"connectedCount=" +
                $"{NetworkManager.ConnectedClientsIds.Count}"
            );
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
                //state.SetExhaustedServer(false);
                state.SetHasCommittedTurnServer(false);

                GameEntity actingEntity =
                    state.PlayerEntity;

                if (actingEntity != null)
                {
                    actingEntity.SetExhausted(false);
                }
                else
                {
                    Debug.LogWarning(
                        $"[SESSION START] Client {clientId} has no " +
                        "acting entity to initialize."
                    );
                }
            }
        }

        private int PublishInitialPlayerDemosToKvltServer()
        {
            if (!IsServer)
                return 0;

            if (_promotionActionResolver == null)
            {
                Debug.LogError(
                    "[INITIAL SCENE] " +
                    "Promotion resolver is unavailable."
                );

                return 0;
            }

            if (_seededWorldState == null)
            {
                Debug.LogError(
                    "[INITIAL SCENE] " +
                    "Seeded world state is unavailable."
                );

                return 0;
            }

            int publishedReleaseCount = 0;

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates.OrderBy(
                    pair => pair.Key
                ))
            {
                ulong clientId =
                    pair.Key;

                NetPlayerState state =
                    pair.Value;

                if (state == null ||
                    !state.ActiveValue)
                {
                    continue;
                }

                GameEntity playerEntity =
                    state.PlayerEntity;

                if (playerEntity == null)
                {
                    Debug.LogWarning(
                        "[INITIAL SCENE] " +
                        $"Client {clientId} has no player entity."
                    );

                    continue;
                }

                var startingDemo =
                    playerEntity
                        .GetLatestUnreleasedDemoTape();

                if (startingDemo == null)
                {
                    Debug.LogWarning(
                        "[INITIAL SCENE] " +
                        $"Client {clientId} has no " +
                        "unreleased starting demo."
                    );

                    continue;
                }

                if (startingDemo.RecordedTurn >= 0)
                {
                    Debug.LogWarning(
                        "[INITIAL SCENE] " +
                        $"Client {clientId} latest demo is not " +
                        "pre-session material | " +
                        $"demo={startingDemo.DisplayName} | " +
                        $"recordedTurn={startingDemo.RecordedTurn}"
                    );

                    continue;
                }

                bool released =
                    _promotionActionResolver
                        .TryReleaseLatestDemoToKvlt(
                            clientId,
                            playerEntity,
                            _seededWorldState,
                            out string message
                        );

                if (!released)
                {
                    Debug.LogWarning(
                        "[INITIAL SCENE] " +
                        $"Failed to publish starting demo | " +
                        $"client={clientId} | " +
                        $"reason={message}"
                    );

                    continue;
                }

                publishedReleaseCount++;

                ProductionLog(
                    "[INITIAL SCENE RELEASE] " +
                    $"client={clientId} | " +
                    $"entity={playerEntity.DisplayName} | " +
                    $"demo={startingDemo.DisplayName} | " +
                    $"demoId={startingDemo.DemoTapeId} | " +
                    $"result={message}"
                );
            }

            return publishedReleaseCount;
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
            
            int initialReleaseCount =
                PublishInitialPlayerDemosToKvltServer();

            if (initialReleaseCount <= 0)
            {
                Debug.LogError(
                    "[SESSION START] " +
                    "Playable session cannot begin because " +
                    "no initial demos were published."
                );

                return false;
            }

            if (initialReleaseCount !=
                eligiblePlayers)
            {
                Debug.LogWarning(
                    "[SESSION START] " +
                    "Initial scene release count does not match " +
                    "eligible player count | " +
                    $"eligiblePlayers={eligiblePlayers} | " +
                    $"initialReleases={initialReleaseCount}"
                );
            }

            _seededWorldState
                .EvaluateSceneOutputStandings(
                    globalTurn.Value
                );

            
            ResolveInitialKeeperAssignmentServer();

            _gameStarted = true;
            testStarted.Value = true;

            RefreshAllDreamAvailability();

            SLog(
                $"GAME Started | reason={reason} | " +
                $"eligiblePlayers={eligiblePlayers} | " +
                $"initialReleases={initialReleaseCount} | " +
                $"connectedCount=" +
                $"{NetworkManager.ConnectedClientsIds.Count}"
            );

            PublishDomainProjectionServer(
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
        // Keeper transitions
        // -----------------------------------------------------------------------------

        private List<KeeperCandidate>
            BuildKeeperCandidatesFromSceneOutput()
        {
            Dictionary<string, float>
                outputByOwnerEntityId =
                    new Dictionary<string, float>();

            IReadOnlyList<
                SeededWorldState.SceneOutputStanding
            > standings =
                LatestSceneOutputStandings;

            if (standings != null)
            {
                for (int i = 0;
                     i < standings.Count;
                     i++)
                {
                    SeededWorldState.SceneOutputStanding
                        standing =
                            standings[i];

                    if (string.IsNullOrWhiteSpace(
                            standing.OwnerEntityId
                        ))
                    {
                        continue;
                    }

                    outputByOwnerEntityId[
                        standing.OwnerEntityId
                    ] = standing.Score;
                }
            }

            List<KeeperCandidate> candidates =
                new List<KeeperCandidate>();

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates.OrderBy(
                    pair => pair.Key
                ))
            {
                NetPlayerState state =
                    pair.Value;

                if (state == null ||
                    !state.ActiveValue)
                {
                    continue;
                }

                GameEntity playerEntity =
                    state.PlayerEntity;

                string ownerEntityId =
                    playerEntity?.EntityId ??
                    string.Empty;

                float sceneOutput = 0.0f;

                if (!string.IsNullOrWhiteSpace(
                        ownerEntityId
                    ))
                {
                    outputByOwnerEntityId.TryGetValue(
                        ownerEntityId,
                        out sceneOutput
                    );
                }

                candidates.Add(
                    new KeeperCandidate(
                        pair.Key,
                        ownerEntityId,
                        sceneOutput
                    )
                );
            }

            return candidates;
        }

        private void
            ResolveInitialKeeperAssignmentServer()
        {
            KeeperTransitionResult result =
                _keeperTransitionResolver
                    .ResolveInitialAssignment(
                        roundIndex.Value,
                        BuildKeeperCandidatesFromSceneOutput()
                    );

            ApplyKeeperTransitionServer(result);
        }

        private void
            ResolveYearEndKeeperTransitionServer(
                bool sceneCollapseLocksTransition)
        {
            List<KeeperCandidate> candidates =
                BuildKeeperCandidatesFromSceneOutput();

            KeeperTransitionResult result;

            if (sceneCollapseLocksTransition)
            {
                float incumbentSceneOutput = 0.0f;

                for (int i = 0;
                     i < candidates.Count;
                     i++)
                {
                    KeeperCandidate candidate =
                        candidates[i];

                    if (candidate.ClientId !=
                        keeperClientId.Value)
                    {
                        continue;
                    }

                    incumbentSceneOutput =
                        candidate.SceneOutput;

                    break;
                }

                result =
                    _keeperTransitionResolver
                        .ResolveSceneCollapseLock(
                            roundIndex.Value,
                            keeperClientId.Value,
                            incumbentSceneOutput
                        );
            }
            else
            {
                result =
                    _keeperTransitionResolver
                        .ResolveYearEnd(
                            roundIndex.Value,
                            keeperClientId.Value,
                            candidates
                        );
            }

            ApplyKeeperTransitionServer(result);
        }

        private void
            ResolveKeeperDisconnectionFallbackServer(
                ulong disconnectedKeeperClientId)
        {
            KeeperTransitionResult result =
                _keeperTransitionResolver
                    .ResolveDisconnectionFallback(
                        roundIndex.Value,
                        disconnectedKeeperClientId,
                        BuildKeeperCandidatesFromSceneOutput()
                    );

            ApplyKeeperTransitionServer(result);
        }

        private void ApplyKeeperTransitionServer(
            KeeperTransitionResult classifiedResult)
        {
            if (!IsServer)
                return;

            if (!classifiedResult.HasResult)
            {
                SLog(
                    "KEEPER transition produced no result"
                );

                return;
            }

            /*
             * Role transfer occurs before irreversible legacy mutation.
             *
             * If the collective role contract cannot be applied, the
             * replicated Keeper identity, tenure and canonization state
             * remain unchanged.
             */
            KeeperRoleResolution roleResolution =
                _keeperRoleResolver.Resolve(
                    classifiedResult,
                    _seededWorldState
                );

            if (!roleResolution.Succeeded)
            {
                Debug.LogError(
                    "[KEEPER TRANSITION] " +
                    "Institutional role resolution failed | " +
                    $"reason={classifiedResult.Reason} | " +
                    $"previous=" +
                    $"{classifiedResult.PreviousKeeperClientId} | " +
                    $"next=" +
                    $"{classifiedResult.NextKeeperClientId} | " +
                    $"failure={roleResolution.FailureReason}"
                );

                return;
            }

            NormalizeActionPlansAfterKeeperRoleResolutionServer(
                classifiedResult,
                roleResolution
            );

            string nextKeeperOwnerEntityId =
                GetOwnerEntityIdForClient(
                    classifiedResult.NextKeeperClientId
                );

            if (classifiedResult.HasAssignedKeeper &&
                string.IsNullOrWhiteSpace(
                    nextKeeperOwnerEntityId
                ))
            {
                Debug.LogWarning(
                    "[KEEPER TRANSITION] " +
                    "Assigned Keeper has no resolvable " +
                    "owner entity ID | " +
                    $"client=" +
                    $"{classifiedResult.NextKeeperClientId}"
                );
            }

            KeeperLegacyResolution legacyResolution =
                _keeperLegacyResolver.Resolve(
                    classifiedResult,
                    _currentKeeperTenure,
                    _seededWorldState,
                    nextKeeperOwnerEntityId
                );

            KeeperTransitionResult resolvedResult =
                legacyResolution.TransitionResult;

            /*
             * The role and legacy resolvers have now completed.
             * Commit the coordinator-owned state together before the
             * caller publishes the resulting domain projection.
             */
            _currentKeeperTenure =
                legacyResolution.NextTenure;

            _latestKeeperTransitionResult =
                resolvedResult;

            keeperClientId.Value =
                resolvedResult.NextKeeperClientId;

            string tenureDescription =
                _currentKeeperTenure == null
                    ? "none"
                    : $"keeper=" +
                      $"{_currentKeeperTenure.KeeperClientId}, " +
                      $"startedRound=" +
                      $"{_currentKeeperTenure.StartedRound}, " +
                      $"subject=" +
                      $"{_currentKeeperTenure.CanonizationSubjectReleaseId}, " +
                      $"subjectYears=" +
                      $"{_currentKeeperTenure.SubjectTenureYears}, " +
                      $"pull={_currentKeeperTenure.Pull}";

            SLog(
                $"KEEPER transition | " +
                $"reason={resolvedResult.Reason} | " +
                $"round={resolvedResult.ResolvedRound} | " +
                $"previous=" +
                $"{resolvedResult.PreviousKeeperClientId} | " +
                $"next=" +
                $"{resolvedResult.NextKeeperClientId} | " +
                $"output=" +
                $"{resolvedResult.WinningSceneOutput:F2} | " +
                $"roleReleased=" +
                $"{roleResolution.OutgoingRoleReleased} | " +
                $"roleApplied=" +
                $"{roleResolution.IncomingRoleApplied} | " +
                $"previousSubject=" +
                $"{resolvedResult.PreviousSubjectReleaseId} | " +
                $"canonized=" +
                $"{resolvedResult.CanonizedReleaseId} | " +
                $"incomingSubject=" +
                $"{resolvedResult.IncomingSubjectReleaseId} | " +
                $"tenure=[{tenureDescription}]"
            );
        }

        private string GetOwnerEntityIdForClient(
            ulong clientId)
        {
            if (clientId == ulong.MaxValue)
                return string.Empty;

            if (!_playerStates.TryGetValue(
                    clientId,
                    out NetPlayerState state
                ))
            {
                return string.Empty;
            }

            GameEntity playerEntity =
                state?.PlayerEntity;

            return playerEntity?.EntityId ??
                   string.Empty;
        }

        public bool IsClientCurrentKeeper(
            ulong clientId)
        {
            return
                clientId != ulong.MaxValue &&
                keeperClientId.Value != ulong.MaxValue &&
                keeperClientId.Value == clientId;
        }

        /// <summary>
        /// Resolves one Keeper visibility intervention against
        /// authoritative server-domain state.
        ///
        /// This is not a ServerRpc. The player-owned networking
        /// component must derive Keeper identity from the RPC sender,
        /// construct the request, and call this method.
        /// </summary>
        public bool TryResolveKeeperInterventionServer(
            KeeperInterventionRequest request,
            out KeeperInterventionResult result,
            out string failureReason)
        {
            result = default;
            failureReason = string.Empty;

            if (!IsServer)
            {
                failureReason =
                    "Keeper interventions are server-authoritative.";

                return false;
            }

            if (!_gameStarted ||
                !testStarted.Value)
            {
                failureReason =
                    "Keeper interventions are unavailable " +
                    "before the playable session starts.";

                return false;
            }

            if (_keeperInterventionResolver == null)
            {
                failureReason =
                    "Keeper intervention resolver is unavailable.";

                Debug.LogError(
                    "[KEEPER INTERVENTION] " +
                    failureReason
                );

                return false;
            }

            bool resolved =
                _keeperInterventionResolver.TryResolve(
                    request,
                    keeperClientId.Value,
                    globalTurn.Value,
                    _currentKeeperTenure,
                    _seededWorldState,
                    out KeeperTenureState
                        nextTenure,
                    out result
                );

            if (!resolved)
            {
                failureReason =
                    result.FailureReason.ToString();

                Debug.LogWarning(
                    "[KEEPER INTERVENTION REJECTED] " +
                    $"keeper={request.KeeperClientId} | " +
                    $"release={request.ReleaseId} | " +
                    $"type={request.InterventionType} | " +
                    $"turn={request.RequestedTurn} | " +
                    $"pull={request.PullSpend:F2} | " +
                    $"reason={result.FailureReason}"
                );

                return false;
            }

            /*
             * The domain resolver has already staged the release
             * adjustment. Commit the corresponding immutable tenure
             * before publishing either state.
             */
            _currentKeeperTenure =
                nextTenure;

            Debug.Log(
                "[KEEPER INTERVENTION RESOLVED] " +
                $"keeper={request.KeeperClientId} | " +
                $"release={request.ReleaseId} | " +
                $"type={request.InterventionType} | " +
                $"turn={request.RequestedTurn} | " +
                $"pullSpent={result.PullSpent:F2} | " +
                $"pullRemaining={result.PullRemaining:F2} | " +
                $"visibilityDelta=" +
                $"{result.AppliedVisibilityDelta:F2}"
            );

            PublishDomainProjectionServer(
                "Keeper intervention resolved"
            );

            BroadcastStateClientRpc();

            return true;
        }

        private void ResetRegularActionPlanServer(
            ulong clientId)
        {
            if (clientId == ulong.MaxValue)
                return;

            if (!_playerStates.TryGetValue(
                    clientId,
                    out NetPlayerState state
                ) ||
                state == null)
            {
                return;
            }

            state.SetCurrentStanceServer(
                BandStance.None
            );

            state.ResetDraftedActionsServer();

            state.SetContextTargetSummaryServer(
                PlayerContextTargetSummary.Empty
            );
        }

        private void NormalizeActionPlansAfterKeeperRoleResolutionServer(
            KeeperTransitionResult transition,
            KeeperRoleResolution roleResolution)
        {
            if (roleResolution.OutgoingRoleReleased)
            {
                ResetRegularActionPlanServer(
                    transition.PreviousKeeperClientId
                );
            }

            if (roleResolution.IncomingRoleApplied &&
                transition.HasAssignedKeeper)
            {
                ResetRegularActionPlanServer(
                    transition.NextKeeperClientId
                );
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

            if (IsClientCurrentKeeper(clientId))
            {
                failureReason =
                    "The Keeper cannot Dream while their band is dormant.";

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

            PublishDomainProjectionServer(
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

            if (IsClientCurrentKeeper(clientId))
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

            TurnLog($"[TURN COMMIT] Client {clientId} locked stance {state.CurrentStanceValue}");

            MarkActedAndAdvanceIfReady(
                clientId,
                state
            );
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
        private void MarkActedAndAdvanceIfReady(
            ulong senderClientId,
            NetPlayerState state)
        {
            if (state == null)
                return;

            _actedThisTurn.Add(senderClientId);
            state.SetHasCommittedTurnServer(true);

            if (AllActivePlayersActed())
            {
                AdvanceGlobalTurn();
                return;
            }

            PublishDomainProjectionServer(
                "committed payloads resolved"
            );

            RefreshDreamAvailabilityForPlayer(
                senderClientId,
                state
            );

            BroadcastStateClientRpc();
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

            int productiveActionCount =
                CountProductiveCommittedActions(state);

            if (!TurnActionRules.IsValidProductiveActionCount(
                    productiveActionCount))
            {
                Debug.LogError(
                    $"[ACTION LOAD ERROR] Client {clientId} committed " +
                    $"{productiveActionCount} productive actions."
                );

                return;
            }


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

            ApplyTurnLoadOutcome(
                clientId,
                state,
                productiveActionCount
            );
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
                    string restOrigin =
                        slot.IsImplicit
                            ? "implicit"
                            : "explicit";

                    ProductionLog(
                        $"[RECOVERY SLOT] Client {clientId} | " +
                        $"origin={restOrigin} | " +
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

            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsListening)
            {
                return;
            }

            _actedThisTurn.Clear();
            ResetPlayerActionsForNewTurn();

            int previousRound =
                roundIndex.Value;

            StorePreviousStancesForTurnBoundary();

            if (_seededWorldState != null)
            {
                _seededWorldState
                    .ResolveTagLifecyclesAtTurnBoundary();
            }

            globalTurn.Value++;

            bool reachedYearEnd =
                IsEndOfYearTurn();

            if (_seededWorldState != null)
            {
                _seededWorldState
                    .TickSceneReleaseCirculation(
                        globalTurn.Value
                    );

                _seededWorldState
                    .EvaluateSceneOutputStandings(
                        globalTurn.Value
                    );
            }

            if (reachedYearEnd)
            {
                bool sceneCollapseLocksTransition =
                    IsKeeperTransitionLockedBySceneCollapseServer();

                /*
                 * The transition starts the new round, so both the
                 * transition result and a newly created tenure use
                 * the resulting round index.
                 */
                roundIndex.Value =
                    previousRound + 1;

                SLog(
                    $"ADV Round {previousRound} -> " +
                    $"{roundIndex.Value} (year end)"
                );

                /*
                 * Capture completed-year player state before any
                 * reactivation or future Keeper-role mutation.
                 */
                CaptureLastResolvedRoundSnapshot();

                ResolveYearEndKeeperTransitionServer(
                    sceneCollapseLocksTransition
                );

                ReactivateInactivePlayersAtYearEnd();
            }

            RolloverPlayerStancesForNewTurn();
            RefreshAllDreamAvailability();

            PublishDomainProjectionServer(
                reachedYearEnd
                    ? "year-end transition resolved"
                    : "turn boundary resolved"
            );

            BroadcastStateClientRpc();
        }

        private void ResetPlayerActionsForNewTurn()
        {
            foreach (KeyValuePair<ulong, NetPlayerState> player
                     in _playerStates)
            {
                NetPlayerState state = player.Value;

                if (state == null)
                    continue;

                state.ResetCommittedActionsServer();
                state.SetHasCommittedTurnServer(false);
            }

            TurnLog(
                "[ACTION] Reset actions and turn-commit state " +
                "for new turn"
            );
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

        private void CaptureLastResolvedRoundSnapshot()
        {
            Dictionary<
                ulong,
                NetPlayerState.LastResolvedRoundData
            > snapshot =
                new Dictionary<
                    ulong,
                    NetPlayerState.LastResolvedRoundData
                >();

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates)
            {
                NetPlayerState state =
                    pair.Value;

                if (state == null)
                    continue;

                snapshot[pair.Key] =
                    new NetPlayerState
                        .LastResolvedRoundData
                        {
                            score =
                                state.ScoreValue,

                            isActive =
                                state.ActiveValue
                        };
            }

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates)
            {
                NetPlayerState state =
                    pair.Value;

                if (state == null)
                    continue;

                state.LastResolvedRound =
                    new Dictionary<
                        ulong,
                        NetPlayerState
                        .LastResolvedRoundData
                    >(snapshot);
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

        private bool
            IsKeeperTransitionLockedBySceneCollapseServer()
        {
            /*
             * Extension point for the later scene-collapse resolver.
             *
             * The Bible requires collapse resolution to occur before
             * Keeper succession. A collapsed scene cannot be inherited;
             * the incumbent remains Keeper for the apocalypse round.
             *
             * The vertical slice currently has no authoritative collapse
             * state, so the gate remains inactive.
             */
            return false;
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

        public void PublishDomainProjectionServer(
            string reason)
        {
            if (!IsServer)
            {
                Debug.LogWarning(
                    $"[{nameof(GameCoordinator)}] " +
                    $"Domain projection publication rejected | " +
                    $"reason={reason} | caller is not server"
                );

                return;
            }

            DomainSnapshotReplicator snapshotReplicator =
                DomainSnapshotReplicator.Instance;

            if (snapshotReplicator == null)
            {
                Debug.LogWarning(
                    $"[{nameof(GameCoordinator)}] " +
                    $"Domain projection publication skipped | " +
                    $"reason={reason} | " +
                    $"no {nameof(DomainSnapshotReplicator)} instance"
                );

                return;
            }

            if (!snapshotReplicator.IsSnapshotNetworkReady)
            {
                Debug.Log(
                    $"[{nameof(GameCoordinator)}] " +
                    $"Domain projection publication deferred/skipped | " +
                    $"reason={reason} | replicator not network-ready"
                );

                return;
            }

            snapshotReplicator
                .RebuildSnapshotsFromServerDomain();

            Debug.Log(
                $"[{nameof(GameCoordinator)}] " +
                $"Domain projection published | reason={reason}"
            );
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

        private static int CountProductiveCommittedActions(
            NetPlayerState state)
        {
            if (state == null)
                return 0;

            int productiveActionCount = 0;

            foreach (DraftedActionPayload payload
                     in state.CommittedActionPayloads)
            {
                if (payload == null)
                    continue;

                if (TurnActionRules.IsProductive(
                        payload.ActionType))
                {
                    productiveActionCount++;
                }
            }

            return productiveActionCount;
        }

        private void ApplyTurnLoadOutcome(
            ulong clientId,
            NetPlayerState state,
            int productiveActionCount)
        {
            if (state == null)
                return;

            GameEntity actingEntity =
                state.PlayerEntity;

            if (actingEntity == null)
            {
                Debug.LogError(
                    $"[TURN LOAD ERROR] Client {clientId} " +
                    "has no acting entity."
                );

                return;
            }

            if (!TurnActionRules.IsValidProductiveActionCount(
                    productiveActionCount))
            {
                Debug.LogError(
                    $"[TURN LOAD ERROR] Client {clientId} " +
                    $"resolved {productiveActionCount} productive " +
                    "actions, exceeding the legal turn limit."
                );

                return;
            }

            bool overreached =
                TurnActionRules.IsOverreach(
                    productiveActionCount);

            actingEntity.SetExhausted(overreached);

            if (overreached)
            {
                ProductionLog(
                    $"[OVERREACH] Client {clientId} | " +
                    $"entity={actingEntity.EntityId} | " +
                    $"{productiveActionCount} productive actions | " +
                    "recovery replaced | Exhausted=True"
                );

                return;
            }

            ProductionLog(
                $"[RECOVERY] Client {clientId} | " +
                $"entity={actingEntity.EntityId} | " +
                $"{productiveActionCount}/" +
                $"{TurnActionRules.StandardProductiveActionCapacity} " +
                "standard productive actions | " +
                "recovery retained | Exhausted=False"
            );
        }
    }
}