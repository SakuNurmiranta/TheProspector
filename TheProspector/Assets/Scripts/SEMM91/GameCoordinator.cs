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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using SEMM91.Core.Entities;
using Unity.Netcode;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.Core.Tags;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Actions.History;
using SEMM91.GamePlay.Events;
using SEMM91.GamePlay.Circulation;
using SEMM91.GamePlay.Collectives;
using SEMM91.GamePlay.Gestation;
using SEMM91.GamePlay.Gestation.Questing;
using SEMM91.GamePlay.InfoScope;
using SEMM91.GamePlay.Promotion;
using SEMM91.GamePlay.Rehearsal;
using SEMM91.GamePlay.Keeper;
using SEMM91.GamePlay.Kvlt.Scenario;
using SEMM91.GamePlay.Kvlt.Settlement;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.GamePlay.Kvlt.Canon;
using SEMM91.GamePlay.Score;
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

        [Header("Peak 2 Scenario")] [SerializeField]
        private TextAsset peak2FoundingDemoTapeJson;

        [Header("Peak 2 Happening Window")] [SerializeField, Min(0.1f)]
        private float peak2HappeningWindowSeconds = 60f;

        private KvltScenarioProfile
            _peak2ScenarioProfile;

        private Peak2StartingScenarioBootstrapper
            _peak2StartingScenarioBootstrapper;

        private KvltStartingCanonInstitutionBootstrapper
            _peak2StartingCanonInstitutionBootstrapper;

        private KvltExistingFieldRuntimeSettlementService
            _peak2ExistingFieldRuntimeSettlementService;

        private KvltHappeningRuntimePreparationService
            _peak2HappeningRuntimePreparationService;

        private KvltHappeningConsequenceSettlementService
            _peak2HappeningConsequenceSettlementService;

        private KvltPostHappeningRuntimeSettlementService
            _peak2PostHappeningRuntimeSettlementService;

        private KvltPostHappeningCanonizationScreeningResult
            _peak2LatestPostHappeningScreening;

        private KvltCanonizationSettlementResult
            _peak2LatestCanonizationSettlement;

        private KvltTurnScoreSettlementResult
            _peak2LatestTurnScoreSettlement;

        private KvltTurnChronologyPlan
            _peak2PendingTurnChronology;

        private KvltExistingFieldRuntimeSettlementResult
            _peak2PendingExistingFieldSettlement;

        private KvltHappeningRuntimePreparationResult
            _peak2PendingHappeningPreparation;

        private KvltHappeningConsequenceSettlementResult
            _peak2LatestHappeningSettlement;

        private Coroutine
            _peak2HappeningWindowTimer;

        private KvltTurnTailRuntimeSettlementService
            _peak2TurnTailRuntimeSettlementService;

        private KvltSettledSceneStandingResult
            _peak2LatestSettledStanding;

        private KvltNextTurnIngressSettlementResult
            _peak2LatestNextTurnIngress;

        private KvltNextSceneEnvironmentSettlementResult
            _peak2LatestNextSceneEnvironment;

        // -----------------------------------------------------------------------------
        // Singleton / NetworkBehaviour lifecycle
        // -----------------------------------------------------------------------------

        public static GameCoordinator Instance;

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
                new PromotionActionResolver(
                    () => globalTurn.Value
                );
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

            _peak2ScenarioProfile =
                Peak2KvltScenarioProfileFactory
                    .CreateDefault();

            _peak2StartingScenarioBootstrapper =
                new Peak2StartingScenarioBootstrapper();

            _peak2StartingCanonInstitutionBootstrapper =
                new KvltStartingCanonInstitutionBootstrapper();

            _peak2ExistingFieldRuntimeSettlementService =
                new KvltExistingFieldRuntimeSettlementService();

            _peak2HappeningRuntimePreparationService =
                new KvltHappeningRuntimePreparationService();

            _peak2HappeningConsequenceSettlementService =
                new KvltHappeningConsequenceSettlementService();

            _peak2PostHappeningRuntimeSettlementService =
                new KvltPostHappeningRuntimeSettlementService();

            _peak2YearEndKeeperRuntimeSettlementService =
                new KvltYearEndKeeperRuntimeSettlementService();

            _peak2TurnTailRuntimeSettlementService =
                new KvltTurnTailRuntimeSettlementService();
        }


        // Runs after Netcode has spawned the coordinator.
        // Server-only setup, connection callbacks, shared world bootstrap,
        // and host registration belong here.
        public override void OnNetworkSpawn()
        {
            keeperClientId.OnValueChanged -=
                HandleKeeperClientIdChanged;

            keeperClientId.OnValueChanged +=
                HandleKeeperClientIdChanged;

            globalTurn.OnValueChanged -=
                HandleGlobalTurnChanged;

            globalTurn.OnValueChanged +=
                HandleGlobalTurnChanged;

            if (IsServer)
            {
                _gestationActionResolver.Initialize();
                testStarted.Value = false;

                _readyClients.Clear();

                int plannedClients =
                    BotConfig.GetIntArg(
                        "-clients",
                        DefaultTestClientTarget
                    );

                SLog(
                    "READY gate initialized | " +
                    $"plannedClients={plannedClients} | " +
                    $"dedicatedServer=" +
                    $"{NetBootstrap.DedicatedServerModeActive}"
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
            StopPeak2HappeningWindowTimer();

            if (IsServer &&
                NetworkManager.Singleton != null)
            {
                NetworkManager
                        .OnClientConnectedCallback -=
                    OnClientConnected;

                NetworkManager
                        .OnClientDisconnectCallback -=
                    OnClientDisconnected;
            }

            foreach (NetPlayerState state
                     in _playerStates.Values)
            {
                UnbindActionPlanProjectionSource(
                    state
                );
            }
        }

        public override void OnNetworkDespawn()
        {
            StopPeak2HappeningWindowTimer();

            keeperClientId.OnValueChanged -=
                HandleKeeperClientIdChanged;

            globalTurn.OnValueChanged -=
                HandleGlobalTurnChanged;

            base.OnNetworkDespawn();
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

        public event Action<ulong, ulong>
            KeeperClientIdChanged;

        public event Action<int, int>
            GlobalTurnChanged;

        public NetworkVariable<int> globalTurn = new();
        public NetworkVariable<int> roundIndex = new();
        public NetworkVariable<bool> testStarted = new();

        public NetworkVariable<
                KvltTurnResolutionRuntimePhase>
            peak2TurnResolutionPhase =
                new(
                    KvltTurnResolutionRuntimePhase.Idle,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server
                );

        public NetworkVariable<double>
            peak2HappeningWindowEndsAt =
                new(
                    0d,
                    NetworkVariableReadPermission.Everyone,
                    NetworkVariableWritePermission.Server
                );

        public KvltHappeningRuntimePreparationResult
            PendingPeak2HappeningPreparation =>
            _peak2PendingHappeningPreparation;

        public KvltHappeningConsequenceSettlementResult
            LatestPeak2HappeningSettlement =>
            _peak2LatestHappeningSettlement;

        public KvltPostHappeningCanonizationScreeningResult
            LatestPeak2PostHappeningScreening =>
            _peak2LatestPostHappeningScreening;

        public KvltCanonizationSettlementResult
            LatestPeak2CanonizationSettlement =>
            _peak2LatestCanonizationSettlement;

        public KvltTurnScoreSettlementResult
            LatestPeak2TurnScoreSettlement =>
            _peak2LatestTurnScoreSettlement;

        public IReadOnlyList<YearInfluenceEvaluation>
            LatestPeak2YearInfluence =>
            _peak2LatestYearInfluence;

        public IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
            LatestPeak2CanonTenureTransitions =>
            _peak2LatestCanonTenureTransitions;

        public IReadOnlyList<SceneRelease>
            SceneReleases =>
            _seededWorldState?.SceneReleases;

        public KvltSettledSceneStandingResult
            LatestPeak2SettledStanding =>
            _peak2LatestSettledStanding;

        public KvltNextTurnIngressSettlementResult
            LatestPeak2NextTurnIngress =>
            _peak2LatestNextTurnIngress;

        public KvltNextSceneEnvironmentSettlementResult
            LatestPeak2NextSceneEnvironment =>
            _peak2LatestNextSceneEnvironment;

        public SeededWorldState
            AuthoritativePeak2WorldState =>
            IsServer
                ? _seededWorldState
                : null;

        public KvltTurnResolutionRecord
            LatestPeak2TurnResolutionRecord =>
            _seededWorldState?
                .LatestKvltTurnResolutionRecord;

        public bool IsPlayableSessionStarted => _gameStarted && testStarted.Value;
        [SerializeField, Min(1)] private int playablePlayersToStart = 2;
        private const int DefaultTestClientTarget = 6;
        private const int TurnsPerYear = 4;

        private const float
            ProvisionalQuestingSceneSynchronisation =
                1.0f; //this should be replaced with a comparison between the scene canon/field vs the character

        public Season CurrentSeason => (Season)(globalTurn.Value % 4);

        // -----------------------------------------------------------------------------
        // Server-side runtime state
        // -----------------------------------------------------------------------------
        // Runtime collections owned by the coordinator.
        // These track connected players, turn readiness, acted state,
        // and last resolved round snapshots.

        private readonly HashSet<ulong> _readyClients = new();
        private readonly HashSet<ulong> _actedThisTurn = new();
        private readonly Dictionary<ulong, NetPlayerState> _playerStates = new();

        private readonly HashSet<ulong>
            _deploymentBotClients = new();

        private readonly HashSet<ulong>
            _humanClients = new();

        private readonly PhysicalEventVisibilityPolicy
            _physicalEventVisibilityPolicy =
                new PhysicalEventVisibilityPolicy();

        private KvltYearEndKeeperRuntimeSettlementService
            _peak2YearEndKeeperRuntimeSettlementService;

        private IReadOnlyList<YearInfluenceEvaluation>
            _peak2LatestYearInfluence =
                Array.Empty<YearInfluenceEvaluation>();

        private IReadOnlyList<
                SceneReleaseCanonTenureTransitionApplication>
            _peak2LatestCanonTenureTransitions =
                Array.Empty<
                    SceneReleaseCanonTenureTransitionApplication>();

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
                            _playerEntityBootstrapper
                                .CreatePlayerEntity(
                                    clientId,
                                    $"Player {clientId}"
                                );

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

                        BindActionPlanProjectionSource(
                            state
                        );

                        RebuildObservedPhysicalEventProjectionsServer(
                            $"player registered | client={clientId}"
                        );

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

        private bool
            TryAssignPeak2FoundingKeeperServer(
                ulong foundingClientId)
        {
            KeeperTransitionResult transition =
                new KeeperTransitionResult(
                    resolvedRound:
                    roundIndex.Value,
                    reason:
                    KeeperTransitionReason
                        .InitialAssignment,
                    previousKeeperClientId:
                    ulong.MaxValue,
                    nextKeeperClientId:
                    foundingClientId,
                    previousSubjectReleaseId:
                    string.Empty,
                    canonizedReleaseId:
                    string.Empty,
                    incomingSubjectReleaseId:
                    string.Empty,
                    winningSceneOutput:
                    0f,
                    pullGrant:
                    _peak2ScenarioProfile
                        .StartingKeeperPull
                );

            ApplyKeeperTransitionServer(
                transition
            );

            return
                keeperClientId.Value ==
                foundingClientId &&
                _currentKeeperTenure != null &&
                _currentKeeperTenure.KeeperClientId ==
                foundingClientId;
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
            if (_isShuttingDown)
                return;

            bool disconnectedClientWasBot =
                _deploymentBotClients.Contains(id);

            bool disconnectedClientWasHuman =
                _humanClients.Contains(id);

            bool disconnectedPlayerWasKeeper =
                keeperClientId.Value == id;

            _readyClients.Remove(id);
            _actedThisTurn.Remove(id);

            if (_playerStates.TryGetValue(
                    id,
                    out NetPlayerState disconnectedState
                ))
            {
                UnbindActionPlanProjectionSource(
                    disconnectedState
                );
            }

            _playerStates.Remove(id);

            RebuildObservedPhysicalEventProjectionsServer(
                $"player disconnected | client={id}"
            );

            _deploymentBotClients.Remove(id);
            _humanClients.Remove(id);

            _questingTurnUsageRegistry?
                .ClearClient(id);

            SLog(
                $"NET ClientDisconnected id={id} | " +
                $"role=" +
                $"{(disconnectedClientWasBot ? "bot" : disconnectedClientWasHuman ? "human" : "unknown")} | " +
                $"connectedHumans={CountConnectedHumanClients()} | " +
                $"connectedCount=" +
                $"{NetworkManager.ConnectedClientsIds.Count}"
            );

            /*
             * The deployed game server is disposable.
             * Once a running session loses its last human,
             * terminate the entire authoritative process.
             *
             * A bot disconnect must not trigger this path.
             */
            if (NetBootstrap.DedicatedServerModeActive &&
                (_gameStarted || testStarted.Value) &&
                disconnectedClientWasHuman &&
                CountConnectedHumanClients() == 0)
            {
                BeginDedicatedSessionShutdown(
                    "last human disconnected"
                );

                return;
            }

            if (disconnectedPlayerWasKeeper)
            {
                ResolveKeeperDisconnectionFallbackServer(
                    id
                );
            }

            bool advancedTurn = false;

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
        }

        private int CountConnectedHumanClients()
        {
            if (NetworkManager == null)
                return 0;

            int count = 0;

            foreach (ulong clientId in _humanClients)
            {
                if (NetworkManager
                    .ConnectedClientsIds
                    .Contains(clientId))
                {
                    count++;
                }
            }

            return count;
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

        public bool TryGetEntityPhysicalNode(
            GameEntity entity,
            out PhysicalMapNode node)
        {
            node = null;

            if (_seededWorldState == null ||
                entity == null ||
                !entity.HasPhysicalLocation)
            {
                return false;
            }

            return _seededWorldState
                .PhysicalMapGrid
                .TryGetNode(
                    entity.PhysicalNodeId,
                    out node
                );
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

        private bool StartPlayableSessionServer(
            string reason)
        {
            if (!IsServer)
                return false;

            if (_gameStarted ||
                testStarted.Value)
            {
                return false;
            }

            int eligiblePlayers =
                CountEligibleConnectedPlayers();

            if (eligiblePlayers <= 0)
            {
                SLog(
                    $"GAME Start ignored | reason={reason} | " +
                    "no eligible connected players"
                );

                return false;
            }

            globalTurn.Value =
                0;

            roundIndex.Value =
                0;

            _actedThisTurn.Clear();

            ActivateEligiblePlayersForSessionStart();

            if (!TryBootstrapPeak2StartingScenarioServer(
                    out
                    KvltStartingScenarioBootstrapResult
                        scenarioBootstrap))
            {
                return false;
            }

            if (!TryBootstrapPeak2StartingWorldStateServer(
                    scenarioBootstrap))
            {
                return false;
            }

            /*
             * Keeper assignment deliberately differs between
             * development SOLOMODE and the real Peak-2
             * multiplayer scenario.
             *
             * SOLOMODE already has an established Keeperless
             * contract. Reuse that path instead of manually
             * mutating Keeper state here.
             */
            if (NetBootstrap.LocalSinglePlayerModeActive)
            {
                /*
                 * SOLOMODE deliberately stops after the
                 * Keeper-independent starting-state phase.
                 *
                 * It has Mayhem, the source rehearsal VHS,
                 * Freezing Moon DemoTape and starting semantic
                 * KVLT state, but no Keeper tenure and therefore
                 * no CanonRetained institutional SceneRelease.
                 */
                ResolveInitialKeeperAssignmentServer();
            }
            else
            {
                if (!TryAssignPeak2FoundingKeeperServer(
                        scenarioBootstrap
                            .FoundingClientId))
                {
                    Debug.LogError(
                        "[SESSION START] Could not establish " +
                        "Mayhem as founding Keeper."
                    );

                    return false;
                }

                /*
                 * Keeper assignment has now created the actual
                 * continuous founding tenure identity.
                 *
                 * Only now may Freezing Moon become the starting
                 * CanonRetained institution.
                 */
                if (!TryBootstrapPeak2StartingCanonInstitutionServer(
                        scenarioBootstrap,
                        out _))
                {
                    Debug.LogError(
                        "[SESSION START] Could not materialize " +
                        "the founding Freezing Moon Canon " +
                        "institution."
                    );

                    return false;
                }
            }

            _gameStarted =
                true;

            testStarted.Value =
                true;

            RefreshAllDreamAvailability();

            SLog(
                $"GAME Started | reason={reason} | " +
                $"eligiblePlayers={eligiblePlayers} | " +
                $"founder=" +
                $"{scenarioBootstrap.FoundingClientId} | " +
                $"foundingDemo=" +
                $"{scenarioBootstrap.FoundingDemoTapeId} | " +
                $"keeper=" +
                $"{keeperClientId.Value} | " +
                $"singlePlayer=" +
                $"{NetBootstrap.LocalSinglePlayerModeActive} | " +
                $"connectedCount=" +
                $"{NetworkManager.ConnectedClientsIds.Count}"
            );

            PublishDomainProjectionServer(
                $"playable session started: {reason}"
            );

            BroadcastStateClientRpc();

            return true;
        }

        private void HandleKeeperClientIdChanged(
            ulong previousKeeperClientId,
            ulong newKeeperClientId)
        {
            KeeperClientIdChanged?.Invoke(
                previousKeeperClientId,
                newKeeperClientId
            );
        }

        private void HandleGlobalTurnChanged(
            int previousGlobalTurn,
            int newGlobalTurn)
        {
            GlobalTurnChanged?.Invoke(
                previousGlobalTurn,
                newGlobalTurn
            );
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

        private bool
            TryBootstrapPeak2StartingCanonInstitutionServer(
                KvltStartingScenarioBootstrapResult
                    scenarioBootstrap,
                out
                    KvltStartingCanonInstitutionBootstrapResult
                    result)
        {
            result =
                null;

            if (scenarioBootstrap == null)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Missing " +
                    "Scenario bootstrap result."
                );

                return false;
            }

            if (_peak2StartingCanonInstitutionBootstrapper ==
                null ||
                _seededWorldState == null)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Required " +
                    "bootstrap services/state are unavailable."
                );

                return false;
            }

            /*
             * Phase B is Keeper-dependent.
             *
             * The founding release must be frozen under the
             * actual tenure created by Keeper assignment, not
             * under a fabricated bootstrap identity.
             */
            if (_currentKeeperTenure == null)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "Keeper tenure does not exist."
                );

                return false;
            }

            if (keeperClientId.Value !=
                scenarioBootstrap.FoundingClientId ||
                _currentKeeperTenure.KeeperClientId !=
                scenarioBootstrap.FoundingClientId)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "Keeper identity does not match Mayhem | " +
                    $"founder=" +
                    $"{scenarioBootstrap.FoundingClientId} | " +
                    $"keeper={keeperClientId.Value} | " +
                    $"tenureKeeper=" +
                    $"{_currentKeeperTenure.KeeperClientId}"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    _currentKeeperTenure.KeeperTenureId))
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "Keeper tenure has no identity."
                );

                return false;
            }

            if (!_playerStates.TryGetValue(
                    scenarioBootstrap.FoundingClientId,
                    out NetPlayerState foundingState) ||
                foundingState == null ||
                foundingState.PlayerEntity == null)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "player entity is unavailable."
                );

                return false;
            }

            GameEntity mayhem =
                foundingState.PlayerEntity;

            if (mayhem.EntityId !=
                scenarioBootstrap.FoundingEntityId)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "entity identity changed unexpectedly | " +
                    $"expected=" +
                    $"{scenarioBootstrap.FoundingEntityId} | " +
                    $"actual={mayhem.EntityId}"
                );

                return false;
            }

            if (!mayhem.TryGetDemoTapeById(
                    scenarioBootstrap.FoundingDemoTapeId,
                    out DemoTape foundingDemoTape))
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Founding " +
                    "DemoTape is unavailable."
                );

                return false;
            }

            try
            {
                result =
                    _peak2StartingCanonInstitutionBootstrapper
                        .Apply(
                            _seededWorldState,
                            foundingDemoTape,
                            mayhem.EntityId,
                            StartingCollectiveBootstrapper
                                .NodeKvltScene,
                            _currentKeeperTenure
                                .KeeperTenureId,
                            globalTurn.Value
                        );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PEAK2 CANON INSTITUTION] Failed | " +
                    $"{exception}"
                );

                return false;
            }

            SLog(
                "[PEAK2 CANON INSTITUTION] Ready | " +
                $"release={result.Release.ReleaseId} | " +
                $"demo={result.Release.SourceDemoTapeId} | " +
                $"owner={result.Release.SourceOwnerEntityId} | " +
                $"tenure=" +
                $"{result.Release.CanonizedUnderKeeperTenureId} | " +
                $"gravity=" +
                $"{result.Release.FrozenPostAssimilationGravity:F3} | " +
                $"frontierClaims=" +
                $"{result.FrontierClaims.Count} | " +
                $"frontierGravityClaims=" +
                $"{result.GravityDecomposition.FrontierContributions.Count}"
            );

            return true;
        }

        private bool
            TryBootstrapPeak2StartingWorldStateServer(
                KvltStartingScenarioBootstrapResult
                    scenarioBootstrap)
        {
            if (scenarioBootstrap == null)
            {
                return false;
            }

            if (!_playerStates.TryGetValue(
                    scenarioBootstrap.FoundingClientId,
                    out NetPlayerState foundingState) ||
                foundingState == null ||
                foundingState.PlayerEntity == null)
            {
                Debug.LogError(
                    "[PEAK2 WORLD BOOTSTRAP] Founding " +
                    "player entity is unavailable."
                );

                return false;
            }

            if (!foundingState.PlayerEntity
                    .TryGetDemoTapeById(
                        scenarioBootstrap
                            .FoundingDemoTapeId,
                        out DemoTape foundingDemoTape))
            {
                Debug.LogError(
                    "[PEAK2 WORLD BOOTSTRAP] Founding " +
                    "DemoTape is unavailable."
                );

                return false;
            }

            try
            {
                new KvltStartingWorldStateBootstrapper()
                    .Apply(
                        _peak2ScenarioProfile,
                        _seededWorldState,
                        foundingDemoTape,
                        globalTurn.Value
                    );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PEAK2 WORLD BOOTSTRAP] Failed | " +
                    $"{exception}"
                );

                return false;
            }

            SLog(
                "[PEAK2 WORLD BOOTSTRAP] Ready | " +
                $"canon={_seededWorldState.KvltCanon.Records.Count} | " +
                $"societyNorms=" +
                $"{_seededWorldState.SocietyNorms.Count} | " +
                $"normativeAffinities=" +
                $"{_seededWorldState.KvltNormativeCentre.NonZeroAffinityCount}"
            );

            return true;
        }

        private bool
            TryBootstrapPeak2StartingScenarioServer(
                out KvltStartingScenarioBootstrapResult
                    result)
        {
            result = null;

            if (_peak2ScenarioProfile == null ||
                _peak2StartingScenarioBootstrapper == null)
            {
                Debug.LogError(
                    "[PEAK2 BOOTSTRAP] Scenario services " +
                    "are unavailable."
                );

                return false;
            }

            if (peak2FoundingDemoTapeJson == null)
            {
                Debug.LogError(
                    "[PEAK2 BOOTSTRAP] Missing founding " +
                    "DemoTape TextAsset."
                );

                return false;
            }

            Dictionary<ulong, GameEntity>
                participants =
                    new();

            HashSet<ulong> humans =
                new();

            HashSet<ulong> bots =
                new();

            foreach (
                KeyValuePair<ulong, NetPlayerState>
                    pair
                in _playerStates)
            {
                ulong clientId =
                    pair.Key;

                NetPlayerState state =
                    pair.Value;

                if (state == null ||
                    state.PlayerEntity == null)
                {
                    continue;
                }

                if (!NetworkManager
                        .ConnectedClientsIds
                        .Contains(clientId))
                {
                    continue;
                }

                bool dedicatedServerHost =
                    NetBootstrap
                        .DedicatedServerModeActive &&
                    clientId ==
                    NetworkManager.ServerClientId;

                if (dedicatedServerHost)
                {
                    continue;
                }

                participants.Add(
                    clientId,
                    state.PlayerEntity
                );

                if (_humanClients.Contains(
                        clientId))
                {
                    humans.Add(clientId);
                }

                if (_deploymentBotClients.Contains(
                        clientId))
                {
                    bots.Add(clientId);
                }
            }

            /*
             * Local SOLOMODE starts from the ordinary player-count
             * gate and can begin before ReportClientReadyServerRpc
             * has classified the local host.
             *
             * In that mode there is exactly one playable
             * participant, and that participant is definitionally
             * the local human.
             */
            if (NetBootstrap.LocalSinglePlayerModeActive &&
                participants.Count == 1)
            {
                humans.Clear();
                bots.Clear();

                foreach (
                    ulong clientId
                    in participants.Keys)
                {
                    humans.Add(
                        clientId
                    );

                    break;
                }
            }

            try
            {
                result =
                    _peak2StartingScenarioBootstrapper
                        .Bootstrap(
                            _peak2ScenarioProfile,
                            participants,
                            humans,
                            bots,
                            peak2FoundingDemoTapeJson.text,
                            startingTurn:
                            globalTurn.Value,
                            allowSoloDevelopmentMode:
                            NetBootstrap
                                .LocalSinglePlayerModeActive
                        );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PEAK2 BOOTSTRAP] Failed | " +
                    $"{exception}"
                );

                return false;
            }

            /*
             * GameEntity identity is server-domain state.
             * Mirror its finalized Scenario name into the
             * replicated player read model.
             */
            foreach (
                KeyValuePair<ulong, GameEntity>
                    participant
                in participants)
            {
                if (!_playerStates.TryGetValue(
                        participant.Key,
                        out NetPlayerState state) ||
                    state == null)
                {
                    continue;
                }

                state.SetDisplayNameServer(
                    participant.Value.DisplayName
                );
            }

            SLog(
                "[PEAK2 BOOTSTRAP] Player roster ready | " +
                $"participants={result.ParticipantCount} | " +
                $"founderClient={result.FoundingClientId} | " +
                $"founderEntity={result.FoundingEntityId} | " +
                $"foundingDemo={result.FoundingDemoTapeId}"
            );

            return true;
        }

        private void TryStartPlayableSession()
        {
            if (!IsServer)
                return;

            if (_gameStarted || testStarted.Value)
                return;

            int eligiblePlayers =
                CountEligibleConnectedPlayers();

            int requiredPlayers =
                NetBootstrap.LocalSinglePlayerModeActive
                    ? 1
                    : playablePlayersToStart;

            if (eligiblePlayers <
                requiredPlayers)
            {
                return;
            }

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
        public void ReportClientReadyServerRpc(
            bool isDeploymentBot,
            ServerRpcParams p = default)
        {
            if (!IsServer)
                return;

            ulong clientId =
                p.Receive.SenderClientId;

            /*
             * Registration normally already happened through
             * OnClientConnectedCallback. Calling it again is safe
             * and protects against callback/spawn ordering.
             */
            RegisterPlayerServer(clientId);

            if (isDeploymentBot)
            {
                _deploymentBotClients.Add(clientId);
                _humanClients.Remove(clientId);
            }
            else
            {
                _humanClients.Add(clientId);
                _deploymentBotClients.Remove(clientId);
            }

            _readyClients.Add(clientId);

            int plannedClients =
                BotConfig.GetIntArg(
                    "-clients",
                    DefaultTestClientTarget
                );

            string participantRole =
                isDeploymentBot
                    ? "bot"
                    : "human";

            string participantName =
                _playerStates.TryGetValue(
                    clientId,
                    out NetPlayerState readyState) &&
                readyState != null
                    ? readyState.DisplayNameStr
                    : $"Client {clientId}";

            SLog(
                $"READY client={clientId} | " +
                $"role={participantRole} | " +
                $"name={participantName} | " +
                $"readyCount={_readyClients.Count}/" +
                $"{plannedClients}"
            );

            TryStartReadyGatedTestRun();
        }

        // -----------------------------------------------------------------------------
        // Keeper transitions
        // -----------------------------------------------------------------------------

        private List<KeeperCandidate>
            BuildPeak2OpenSeatFallbackCandidates()
        {
            List<KeeperCandidate> candidates =
                new List<KeeperCandidate>();

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates.OrderBy(pair => pair.Key
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

                float? sceneStanding =
                    null;

                if (!string.IsNullOrWhiteSpace(
                        ownerEntityId) &&
                    _seededWorldState
                        .TryGetKvltSceneStanding(
                            ownerEntityId,
                            out var standingState))
                {
                    sceneStanding =
                        standingState.CurrentStanding;
                }

                candidates.Add(
                    new KeeperCandidate(
                        pair.Key,
                        ownerEntityId,
                        yearInfluence: 0f,
                        sceneStanding: sceneStanding,
                        isEligible: true
                    )
                );
            }

            return candidates;
        }

        private void
            ResolveInitialKeeperAssignmentServer()
        {
            List<KeeperCandidate> candidates =
                BuildPeak2OpenSeatFallbackCandidates();

            if (NetBootstrap.LocalSinglePlayerModeActive &&
                candidates.Count <= 1)
            {
                keeperClientId.Value =
                    ulong.MaxValue;

                _latestKeeperTransitionResult =
                    KeeperTransitionResult.None;

                _currentKeeperTenure =
                    null;

                SLog(
                    "KEEPER initial assignment deferred | " +
                    "mode=SOLOMODE | " +
                    $"candidates={candidates.Count}"
                );

                return;
            }

            KeeperTransitionResult result =
                _keeperTransitionResolver
                    .ResolveInitialAssignment(
                        roundIndex.Value,
                        candidates
                    );

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
                        BuildPeak2OpenSeatFallbackCandidates()
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

        private List<KeeperCandidate>
            BuildPeak2KeeperCandidatesFromYearInfluence(
                IReadOnlyList<YearInfluenceEvaluation>
                    evaluations)
        {
            if (evaluations == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluations)
                );
            }

            Dictionary<string, float>
                influenceByEntityId =
                    new(
                        StringComparer.Ordinal
                    );

            foreach (
                YearInfluenceEvaluation evaluation
                in evaluations)
            {
                if (evaluation == null ||
                    !influenceByEntityId.TryAdd(
                        evaluation
                            .BeneficiaryEntityId,
                        evaluation.YearInfluence
                    ))
                {
                    throw new InvalidOperationException(
                        "Peak-2 YearInfluence contains " +
                        "invalid or duplicate beneficiary " +
                        "identity."
                    );
                }
            }

            List<KeeperCandidate> candidates =
                new();

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates.OrderBy(
                    pair =>
                        pair.Key
                ))
            {
                NetPlayerState state =
                    pair.Value;

                if (state == null ||
                    !state.ActiveValue ||
                    state.PlayerEntity == null)
                {
                    continue;
                }

                string entityId =
                    state.PlayerEntity.EntityId;

                if (!influenceByEntityId.TryGetValue(
                        entityId,
                        out float yearInfluence))
                {
                    throw new InvalidOperationException(
                        "Active Keeper candidate has no " +
                        "completed-year influence result | " +
                        $"client={pair.Key} | " +
                        $"entity={entityId}"
                    );
                }

                float? priorSceneStanding =
                    null;

                if (_seededWorldState
                    .TryGetKvltSceneStanding(
                        entityId,
                        out var standingState))
                {
                    priorSceneStanding =
                        standingState
                            .CurrentStanding;
                }

                candidates.Add(
                    new KeeperCandidate(
                        pair.Key,
                        entityId,
                        yearInfluence,
                        sceneStanding:
                        priorSceneStanding,
                        /*
                         * Do not fabricate a parallel
                         * Poser flag. The eligibility seam
                         * remains explicit until the
                         * authoritative Poser state exists.
                         */
                        isEligible:
                        true
                    )
                );
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Peak-2 year-end succession has no " +
                    "active candidates."
                );
            }

            return candidates;
        }

        private void
            ResolvePeak2YearEndKeeperTransitionServer(
                int settledTurn,
                bool sceneCollapseLocksTransition)
        {
            if (_currentKeeperTenure == null ||
                keeperClientId.Value ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Peak-2 year-end succession requires " +
                    "an authoritative incumbent tenure."
                );
            }

            IReadOnlyList<string> activeEntityIds =
                BuildActiveKvltParticipantEntityIds();

            _peak2LatestYearInfluence =
                _peak2YearEndKeeperRuntimeSettlementService
                    .EvaluateYearInfluence(
                        NodeKvltScene,
                        settledTurn,
                        TurnsPerYear,
                        activeEntityIds,
                        _seededWorldState
                            .KvltScoreLedger
                    );

            List<KeeperCandidate> candidates =
                BuildPeak2KeeperCandidatesFromYearInfluence(
                    _peak2LatestYearInfluence
                );

            KeeperTransitionResult transition =
                _peak2YearEndKeeperRuntimeSettlementService
                    .ResolveSuccession(
                        roundIndex.Value,
                        keeperClientId.Value,
                        candidates,
                        sceneCollapseLocksTransition
                    );

            if (!transition.HasResult)
            {
                throw new InvalidOperationException(
                    "Peak-2 year-end succession produced " +
                    "no transition result."
                );
            }

            KeeperRoleResolution roleResolution =
                _keeperRoleResolver.Resolve(
                    transition,
                    _seededWorldState
                );

            if (!roleResolution.Succeeded)
            {
                throw new InvalidOperationException(
                    "Peak-2 Keeper role resolution " +
                    "failed | " +
                    $"failure={roleResolution.FailureReason}"
                );
            }

            NormalizeActionPlansAfterKeeperRoleResolutionServer(
                transition,
                roleResolution
            );

            KeeperTenureState endingTenure =
                _currentKeeperTenure;

            KeeperTenureState nextTenure =
                _peak2YearEndKeeperRuntimeSettlementService
                    .CreateNextTenure(
                        transition,
                        endingTenure
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .YearEndSuccessionSettled;

            _peak2LatestCanonTenureTransitions =
                _peak2YearEndKeeperRuntimeSettlementService
                    .ApplyCanonTenureTransition(
                        _seededWorldState.SceneReleases,
                        endingTenure,
                        nextTenure,
                        settledTurn
                    );

            _currentKeeperTenure =
                nextTenure;

            _latestKeeperTransitionResult =
                transition;

            keeperClientId.Value =
                transition.NextKeeperClientId;

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .TenureTransitionSettled;

            SLog(
                "[PEAK2 KEEPER] Year end settled | " +
                $"turn={settledTurn} | " +
                $"reason={transition.Reason} | " +
                $"previous=" +
                $"{transition.PreviousKeeperClientId} | " +
                $"next={transition.NextKeeperClientId} | " +
                $"influence={transition.WinningInfluence:F3} | " +
                $"tenureChanged=" +
                $"{endingTenure.KeeperTenureId != nextTenure.KeeperTenureId} | " +
                $"historicalCanon=" +
                $"{_peak2LatestCanonTenureTransitions.Count}"
            );
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

            if (peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase.Idle)
            {
                return false;
            }

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
                        state.CommittedActionPayloads.Select(payload =>
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
                    slots.Select(slot =>
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

                case DraftedActionType.ReleaseDemoTape:
                {
                    if (_promotionActionResolver == null)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            "missing promotion resolver."
                        );

                        return false;
                    }

                    if (sourcePayload == null)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            "ReleaseDemoTape has no source payload."
                        );

                        return false;
                    }

                    if (sourcePayload.ActionType !=
                        DraftedActionType.ReleaseDemoTape)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            "ReleaseDemoTape received payload type " +
                            $"{sourcePayload.ActionType}."
                        );

                        return false;
                    }

                    if (!sourcePayload.HasTargetDemoTape)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            "ReleaseDemoTape payload has no target " +
                            "demo tape."
                        );

                        return false;
                    }

                    if (!sourcePayload.HasPhysicalEventLocation)
                    {
                        ProductionLog(
                            $"[PROMOTION BLOCKED] Client {clientId} " +
                            "ReleaseDemoTape payload has no physical " +
                            "Promotion location."
                        );

                        return false;
                    }

                    bool success =
                        _promotionActionResolver
                            .TryReleaseDemoToKvlt(
                                clientId,
                                playerEntity,
                                _seededWorldState,
                                sourcePayload.TargetDemoTapeId,
                                out string message
                            );

                    ProductionLog(
                        "[PROMOTION SELECTED DEMO] " +
                        $"success={success} | " +
                        $"target={sourcePayload.TargetDemoTapeId} | " +
                        $"eventNode=" +
                        $"{sourcePayload.PhysicalEventNodeId} | " +
                        message
                    );

                    return success;
                }

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

            if (peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase.Idle)
            {
                TurnLog(
                    "[TURN CLOSURE] Re-entry ignored | " +
                    $"phase=" +
                    $"{peak2TurnResolutionPhase.Value}"
                );

                return;
            }

            if (_seededWorldState == null)
            {
                throw new InvalidOperationException(
                    "Peak-2 turn closure requires the " +
                    "authoritative SeededWorldState."
                );
            }

            /*
             * globalTurn still identifies the turn whose
             * player actions have just completed.
             *
             * Do not advance the replicated clock until every
             * turn-t settlement / year-end operation has
             * finished.
             */
            KvltTurnChronologyPlan chronology =
                PlanPeak2TurnClosure(
                    globalTurn.Value
                );

            int settledTurn =
                chronology.CompletedTurn;

            TurnLog(
                "[TURN CLOSURE] " +
                $"settle={settledTurn} | " +
                $"year={chronology.YearNumber} | " +
                $"indexInYear=" +
                $"{chronology.TurnIndexInYear} | " +
                $"yearEnd={chronology.IsYearEnd} | " +
                $"publish={chronology.NextTurn}"
            );

            _peak2PendingTurnChronology =
                chronology;

            try
            {
                _peak2PendingExistingFieldSettlement =
                    _peak2ExistingFieldRuntimeSettlementService
                        .Settle(
                            _seededWorldState,
                            _peak2ScenarioProfile,
                            NodeKvltScene,
                            settledTurn
                        );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PEAK2 EXISTING FIELD] Failed | " +
                    $"turn={settledTurn} | " +
                    $"{exception}"
                );

                throw;
            }

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .ExistingFieldSettled;

            TurnLog(
                "[PEAK2 EXISTING FIELD] " +
                $"turn={settledTurn} | " +
                $"movement=" +
                $"{_peak2PendingExistingFieldSettlement.Movement.MovementApplications.Count} | " +
                $"boundaryChecks=" +
                $"{_peak2PendingExistingFieldSettlement.Boundary.BoundaryEvaluations.Count} | " +
                $"rejected=" +
                $"{_peak2PendingExistingFieldSettlement.Boundary.RejectedCount}"
            );

            OpenPeak2SharedHappeningWindowServer();
        }

        private void
            OpenPeak2SharedHappeningWindowServer()
        {
            if (peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase
                    .ExistingFieldSettled)
            {
                throw new InvalidOperationException(
                    "Shared Happening window requires " +
                    "completed Existing-Field settlement."
                );
            }

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .SharedHappeningWindowOpen;

            int currentTurnHappeningCount =
                CountCurrentTurnHappenings(
                    _peak2PendingTurnChronology
                        .CompletedTurn
                );

            if (currentTurnHappeningCount == 0)
            {
                peak2HappeningWindowEndsAt.Value =
                    0d;

                ClosePeak2SharedHappeningWindowServer();
                return;
            }

            peak2HappeningWindowEndsAt.Value =
                NetworkManager.ServerTime.Time +
                peak2HappeningWindowSeconds;

            TurnLog(
                "[PEAK2 HAPPENING WINDOW] Open | " +
                $"turn=" +
                $"{_peak2PendingTurnChronology.CompletedTurn} | " +
                $"happenings={currentTurnHappeningCount} | " +
                $"seconds={peak2HappeningWindowSeconds:F1}"
            );

            PublishDomainProjectionServer(
                "Peak-2 Happening window opened"
            );

            BroadcastStateClientRpc();

            _peak2HappeningWindowTimer =
                StartCoroutine(
                    ClosePeak2HappeningWindowAfterDelay()
                );
        }

        private IEnumerator
            ClosePeak2HappeningWindowAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                peak2HappeningWindowSeconds
            );

            _peak2HappeningWindowTimer = null;

            ClosePeak2SharedHappeningWindowServer();
        }

        public bool
            ClosePeak2SharedHappeningWindowServer()
        {
            if (!IsServer ||
                peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase
                    .SharedHappeningWindowOpen)
            {
                return false;
            }

            StopPeak2HappeningWindowTimer();

            peak2HappeningWindowEndsAt.Value =
                0d;

            int settledTurn =
                _peak2PendingTurnChronology
                    .CompletedTurn;

            string keeperEntityId =
                GetOwnerEntityIdForClient(
                    keeperClientId.Value
                );

            _peak2PendingHappeningPreparation =
                _peak2HappeningRuntimePreparationService
                    .Prepare(
                        _seededWorldState,
                        NodeKvltScene,
                        settledTurn,
                        _peak2PendingExistingFieldSettlement
                            .EvaluationEnvironment,
                        BuildActiveKvltParticipantEntityIds(),
                        keeperEntityId
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .HappeningPrepared;

            TurnLog(
                "[PEAK2 HAPPENING] Prepared | " +
                $"turn={settledTurn} | " +
                $"happenings=" +
                $"{_peak2PendingHappeningPreparation.Preparation.PreparedHappenings.Count} | " +
                $"attempts=" +
                $"{_peak2PendingHappeningPreparation.Preparation.ActivationAttempts.Count} | " +
                $"crises=" +
                $"{_peak2PendingHappeningPreparation.Preparation.OpenedCrises.Count}"
            );

            if (HasOpenPreparedAllegianceCrises())
            {
                peak2TurnResolutionPhase.Value =
                    KvltTurnResolutionRuntimePhase
                        .AwaitingAllegianceCrisisResolution;

                PublishDomainProjectionServer(
                    "awaiting Peak-2 Allegiance Crisis votes"
                );

                BroadcastStateClientRpc();
                return true;
            }

            SettlePreparedPeak2HappeningsServer();
            return true;
        }

        private void SettlePeak2TurnTail(
            KvltTurnChronologyPlan chronology)
        {
            if (chronology == null)
            {
                throw new ArgumentNullException(
                    nameof(chronology)
                );
            }

            KvltTurnResolutionRuntimePhase
                requiredPhase =
                    chronology.IsYearEnd
                        ? KvltTurnResolutionRuntimePhase
                            .TenureTransitionSettled
                        : KvltTurnResolutionRuntimePhase
                            .TurnScoreSettled;

            if (peak2TurnResolutionPhase.Value !=
                requiredPhase)
            {
                throw new InvalidOperationException(
                    "Peak-2 turn tail began from the " +
                    "wrong chronology checkpoint | " +
                    $"actual=" +
                    $"{peak2TurnResolutionPhase.Value} | " +
                    $"required={requiredPhase}"
                );
            }

            int settledTurn =
                chronology.CompletedTurn;

            _peak2LatestSettledStanding =
                _peak2TurnTailRuntimeSettlementService
                    .SettleStanding(
                        _seededWorldState,
                        _peak2ScenarioProfile,
                        NodeKvltScene,
                        settledTurn
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .StandingSettled;

            _peak2LatestNextTurnIngress =
                _peak2TurnTailRuntimeSettlementService
                    .SettleIngress(
                        _seededWorldState,
                        _peak2ScenarioProfile,
                        NodeKvltScene,
                        settledTurn
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .NextTurnPlacementSettled;

            _peak2LatestNextSceneEnvironment =
                _peak2TurnTailRuntimeSettlementService
                    .SettleNextEnvironment(
                        _seededWorldState,
                        _peak2ScenarioProfile,
                        NodeKvltScene,
                        settledTurn,
                        _peak2LatestNextTurnIngress,
                        _peak2PendingHappeningPreparation
                            .PublicSources
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .NextSceneEnvironmentSettled;

            TurnLog(
                "[PEAK2 TURN TAIL] Settled | " +
                $"turn={settledTurn} | " +
                $"standingOwners=" +
                $"{_peak2LatestSettledStanding.OwnerCount} | " +
                $"ingressed=" +
                $"{_peak2LatestNextTurnIngress.IngressedCount} | " +
                $"publishedTurn=" +
                $"{_peak2LatestNextSceneEnvironment.PublishedTurn}"
            );
        }

        private void RecordPeak2TurnResolution(
            KvltTurnChronologyPlan chronology)
        {
            KvltTurnResolutionRecord record =
                new(
                    chronology,
                    _peak2PendingExistingFieldSettlement,
                    _peak2LatestHappeningSettlement,
                    _peak2LatestPostHappeningScreening,
                    chronology.IsYearEnd
                        ? _peak2LatestCanonizationSettlement
                        : null,
                    _peak2LatestTurnScoreSettlement,
                    chronology.IsYearEnd
                        ? _peak2LatestYearInfluence
                        : Array.Empty<
                            YearInfluenceEvaluation>(),
                    chronology.IsYearEnd
                        ? (KeeperTransitionResult?)
                            _latestKeeperTransitionResult
                        : null,
                    chronology.IsYearEnd
                        ? _peak2LatestCanonTenureTransitions
                        : Array.Empty<
                            SceneReleaseCanonTenureTransitionApplication>(),
                    _peak2LatestSettledStanding,
                    _peak2LatestNextTurnIngress,
                    _peak2LatestNextSceneEnvironment
                );

            if (!_seededWorldState
                    .TryRecordKvltTurnResolution(
                        record
                    ))
            {
                throw new InvalidOperationException(
                    "Peak-2 turn-resolution record " +
                    "could not be appended."
                );
            }
        }

        public bool
            TryCastNextPeak2AllegianceVoteServer(
                ulong clientId,
                AllegianceChoice choice,
                out string questionId,
                out string failureReason)
        {
            questionId = string.Empty;
            failureReason = string.Empty;

            if (!IsServer ||
                peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase
                    .AwaitingAllegianceCrisisResolution)
            {
                failureReason =
                    "No Allegiance Crisis is awaiting votes.";

                return false;
            }

            if (!Enum.IsDefined(
                    typeof(AllegianceChoice),
                    choice))
            {
                failureReason =
                    "Invalid Allegiance choice.";

                return false;
            }

            string voterEntityId =
                GetOwnerEntityIdForClient(
                    clientId
                );

            if (string.IsNullOrWhiteSpace(
                    voterEntityId))
            {
                failureReason =
                    "Voting client has no authoritative entity.";

                return false;
            }

            AllegianceCrisis crisis =
                FindNextOpenCrisisForVoter(
                    voterEntityId
                );

            if (crisis == null)
            {
                failureReason =
                    "Voter has no unanswered eligible Crisis.";

                return false;
            }

            questionId =
                crisis.Question.QuestionId;

            if (!crisis.TryCastVote(
                    new AllegianceCrisisVote(
                        voterEntityId,
                        choice,
                        _peak2PendingTurnChronology
                            .CompletedTurn
                    )))
            {
                failureReason =
                    "Allegiance vote was rejected by " +
                    "authoritative Crisis state.";

                return false;
            }

            if (crisis.AllEligibleVotesCast &&
                !crisis.TryResolve(
                    _peak2PendingTurnChronology
                        .CompletedTurn,
                    out _))
            {
                throw new InvalidOperationException(
                    "Completed Allegiance Crisis could not " +
                    "resolve | " +
                    $"question={questionId}"
                );
            }

            TurnLog(
                "[PEAK2 ALLEGIANCE] Vote | " +
                $"turn=" +
                $"{_peak2PendingTurnChronology.CompletedTurn} | " +
                $"question={questionId} | " +
                $"voter={voterEntityId} | " +
                $"choice={choice}"
            );

            if (!HasOpenPreparedAllegianceCrises())
            {
                SettlePreparedPeak2HappeningsServer();
            }
            else
            {
                PublishDomainProjectionServer(
                    "Peak-2 Allegiance vote recorded"
                );

                BroadcastStateClientRpc();
            }

            return true;
        }

        private void
            SettlePreparedPeak2HappeningsServer()
        {
            if (_peak2PendingHappeningPreparation ==
                null)
            {
                throw new InvalidOperationException(
                    "Happening settlement has no retained " +
                    "runtime preparation package."
                );
            }

            _peak2LatestHappeningSettlement =
                _peak2HappeningConsequenceSettlementService
                    .Settle(
                        _peak2PendingHappeningPreparation
                            .GlobalTurn,
                        _peak2PendingHappeningPreparation
                            .Preparation,
                        _peak2PendingHappeningPreparation
                            .PublicSources,
                        _seededWorldState
                            .KvltAcceptedTransgressions,
                        _seededWorldState
                            .KvltAllegianceCrisisRegistry,
                        _peak2PendingHappeningPreparation
                            .CurrentEnvironment
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .HappeningSettled;

            TurnLog(
                "[PEAK2 HAPPENING] Settled | " +
                $"turn=" +
                $"{_peak2LatestHappeningSettlement.GlobalTurn} | " +
                $"happenings=" +
                $"{_peak2LatestHappeningSettlement.SettledHappenings.Count} | " +
                $"covered=" +
                $"{_peak2LatestHappeningSettlement.CoveredActivationApplications} | " +
                $"legitimized=" +
                $"{_peak2LatestHappeningSettlement.CrisisLegitimizedApplications} | " +
                $"pending=" +
                $"{_peak2LatestHappeningSettlement.PendingStoredCount} | " +
                $"redeemed=" +
                $"{_peak2LatestHappeningSettlement.PendingRedeemedCount}"
            );

            CompletePeak2TurnClosureAfterHappening();
        }

        private void
            CompletePeak2TurnClosureAfterHappening()
        {
            KvltTurnChronologyPlan chronology =
                _peak2PendingTurnChronology ??
                throw new InvalidOperationException(
                    "Turn closure lost its chronology plan."
                );

            int settledTurn =
                chronology.CompletedTurn;

            int nextTurn =
                chronology.NextTurn;

            bool reachedYearEnd =
                chronology.IsYearEnd;

            int previousRound =
                roundIndex.Value;

            SettlePeak2PostHappeningCanonAndScore(
                chronology
            );

            _actedThisTurn.Clear();

            ResetPlayerActionsForNewTurn();

            StorePreviousStancesForTurnBoundary();

            _seededWorldState
                .ResolveTagLifecyclesAtTurnBoundary();


            if (reachedYearEnd)
            {
                bool sceneCollapseLocksTransition =
                    IsKeeperTransitionLockedBySceneCollapseServer();

                roundIndex.Value =
                    previousRound + 1;

                SLog(
                    $"ADV Round {previousRound} -> " +
                    $"{roundIndex.Value} " +
                    $"after settled turn {settledTurn}"
                );

                CaptureLastResolvedRoundSnapshot();

                ResolvePeak2YearEndKeeperTransitionServer(
                    settledTurn,
                    sceneCollapseLocksTransition
                );

                ReactivateInactivePlayersAtYearEnd();
            }

            SettlePeak2TurnTail(
                chronology
            );

            RecordPeak2TurnResolution(
                chronology
            );
            
            globalTurn.Value =
                nextTurn;

            RolloverPlayerStancesForNewTurn();

            RefreshAllDreamAvailability();

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase.Published;

            PublishDomainProjectionServer(
                reachedYearEnd
                    ? "year-end transition resolved"
                    : "turn boundary resolved"
            );

            BroadcastStateClientRpc();

            ClearPendingPeak2TurnResolution();

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase.Idle;
        }

        private void
            SettlePeak2PostHappeningCanonAndScore(
                KvltTurnChronologyPlan chronology)
        {
            if (chronology == null)
            {
                throw new ArgumentNullException(
                    nameof(chronology)
                );
            }

            if (peak2TurnResolutionPhase.Value !=
                KvltTurnResolutionRuntimePhase
                    .HappeningSettled)
            {
                throw new InvalidOperationException(
                    "Post-Happening settlement requires " +
                    "completed Happening consequences."
                );
            }

            if (_peak2PendingHappeningPreparation ==
                null)
            {
                throw new InvalidOperationException(
                    "Post-Happening settlement lost the " +
                    "turn-t evaluation environment."
                );
            }

            int settledTurn =
                chronology.CompletedTurn;

            string currentKeeperTenureId =
                _currentKeeperTenure?
                    .KeeperTenureId ??
                string.Empty;

            _peak2LatestPostHappeningScreening =
                _peak2PostHappeningRuntimeSettlementService
                    .Screen(
                        _seededWorldState,
                        NodeKvltScene,
                        settledTurn,
                        _peak2PendingHappeningPreparation
                            .CurrentEnvironment
                    );

            if (chronology.IsYearEnd)
            {
                if (string.IsNullOrWhiteSpace(
                        currentKeeperTenureId))
                {
                    throw new InvalidOperationException(
                        "Winter Canon settlement requires " +
                        "an authoritative Keeper tenure."
                    );
                }

                _peak2LatestCanonizationSettlement =
                    _peak2PostHappeningRuntimeSettlementService
                        .SettleYearEndCanon(
                            _seededWorldState,
                            _peak2ScenarioProfile,
                            NodeKvltScene,
                            settledTurn,
                            currentKeeperTenureId,
                            _peak2PendingHappeningPreparation
                                .CurrentEnvironment,
                            _peak2LatestPostHappeningScreening
                        );

                peak2TurnResolutionPhase.Value =
                    KvltTurnResolutionRuntimePhase
                        .YearEndCanonSettled;

                TurnLog(
                    "[PEAK2 CANON] Settled | " +
                    $"turn={settledTurn} | " +
                    $"canonized=" +
                    $"{_peak2LatestCanonizationSettlement.FreezeApplications.Count} | " +
                    $"canonRecords=" +
                    $"{_seededWorldState.KvltCanon.Records.Count}"
                );
            }
            else
            {
                _peak2LatestCanonizationSettlement =
                    null;
            }

            _peak2LatestTurnScoreSettlement =
                _peak2PostHappeningRuntimeSettlementService
                    .SettleTurnScore(
                        _seededWorldState,
                        NodeKvltScene,
                        settledTurn,
                        currentKeeperTenureId,
                        _peak2LatestPostHappeningScreening
                    );

            peak2TurnResolutionPhase.Value =
                KvltTurnResolutionRuntimePhase
                    .TurnScoreSettled;

            TurnLog(
                "[PEAK2 SCORE] Settled | " +
                $"turn={settledTurn} | " +
                $"events=" +
                $"{_peak2LatestTurnScoreSettlement.EventCount} | " +
                $"ledger=" +
                $"{_seededWorldState.KvltScoreLedger.Count}"
            );
        }

        private int CountCurrentTurnHappenings(
            int settledTurn)
        {
            int count = 0;

            foreach (
                Happening happening
                in _seededWorldState
                    .KvltHappeningRegistry
                    .GetAll())
            {
                if (happening.CommittedTurn ==
                    settledTurn)
                {
                    count++;
                }
            }

            return count;
        }

        private IReadOnlyList<string>
            BuildActiveKvltParticipantEntityIds()
        {
            List<string> result =
                new();

            foreach (
                KeyValuePair<ulong, NetPlayerState> pair
                in _playerStates)
            {
                NetPlayerState state =
                    pair.Value;

                if (state == null ||
                    !state.ActiveValue ||
                    state.PlayerEntity == null)
                {
                    continue;
                }

                result.Add(
                    state.PlayerEntity.EntityId
                );
            }

            result.Sort(
                StringComparer.Ordinal
            );

            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "Peak-2 Happening settlement has no " +
                    "active KVLT participants."
                );
            }

            return result;
        }

        private bool
            HasOpenPreparedAllegianceCrises()
        {
            if (_peak2PendingHappeningPreparation ==
                null)
            {
                return false;
            }

            foreach (
                AllegianceCrisis crisis
                in _peak2PendingHappeningPreparation
                    .Preparation
                    .OpenedCrises)
            {
                if (crisis.IsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        private AllegianceCrisis
            FindNextOpenCrisisForVoter(
                string voterEntityId)
        {
            List<AllegianceCrisis> crises =
                new(
                    _peak2PendingHappeningPreparation
                        .Preparation
                        .OpenedCrises
                );

            crises.Sort(
                (
                        left,
                        right
                    ) =>
                    string.CompareOrdinal(
                        left.Question.QuestionId,
                        right.Question.QuestionId
                    )
            );

            foreach (
                AllegianceCrisis crisis
                in crises)
            {
                if (!crisis.IsOpen ||
                    !IsEligibleUnansweredVoter(
                        crisis,
                        voterEntityId
                    ))
                {
                    continue;
                }

                return crisis;
            }

            return null;
        }

        private static bool
            IsEligibleUnansweredVoter(
                AllegianceCrisis crisis,
                string voterEntityId)
        {
            bool eligible = false;

            foreach (
                string candidate
                in crisis.EligibleVoterEntityIds)
            {
                if (candidate ==
                    voterEntityId)
                {
                    eligible = true;
                    break;
                }
            }

            if (!eligible)
            {
                return false;
            }

            foreach (
                AllegianceCrisisVote vote
                in crisis.Votes)
            {
                if (vote.VoterEntityId ==
                    voterEntityId)
                {
                    return false;
                }
            }

            return true;
        }

        private void
            StopPeak2HappeningWindowTimer()
        {
            if (_peak2HappeningWindowTimer ==
                null)
            {
                return;
            }

            StopCoroutine(
                _peak2HappeningWindowTimer
            );

            _peak2HappeningWindowTimer = null;
        }

        private void
            ClearPendingPeak2TurnResolution()
        {
            _peak2PendingTurnChronology = null;
            _peak2PendingExistingFieldSettlement = null;
            _peak2PendingHappeningPreparation = null;
            peak2HappeningWindowEndsAt.Value = 0d;
        }

        private static KvltTurnChronologyPlan
            PlanPeak2TurnClosure(
                int completedTurn)
        {
            return new KvltTurnChronologyPlanner()
                .Plan(
                    completedTurn,
                    TurnsPerYear
                );
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

        private void
            RebuildObservedPhysicalEventProjectionsServer(
                string reason)
        {
            if (!IsServer)
                return;

            foreach (
                KeyValuePair<ulong, NetPlayerState>
                    observerPair
                in _playerStates.OrderBy(pair => pair.Key
                ))
            {
                ulong observerClientId =
                    observerPair.Key;

                NetPlayerState observerState =
                    observerPair.Value;

                if (observerState == null)
                    continue;

                List<ObservedPhysicalEventSummary>
                    observedEvents =
                        new List<
                            ObservedPhysicalEventSummary
                        >();

                foreach (
                    KeyValuePair<ulong, NetPlayerState>
                        sourcePair
                    in _playerStates.OrderBy(pair => pair.Key
                    ))
                {
                    ulong sourceClientId =
                        sourcePair.Key;

                    NetPlayerState sourceState =
                        sourcePair.Value;

                    if (sourceState == null)
                        continue;

                    /*
                     * Proof-of-concept visibility:
                     *
                     * Every connected observer sees every current physical
                     * Promotion event, including their own.
                     *
                     * Future physical sight rules belong here.
                     */
                    AddVisiblePhysicalEvents(
                        observedEvents,
                        observerClientId,
                        sourceClientId,
                        sourceState.DraftedActionPayloads,
                        ObservedPhysicalEventPlanState.Drafted
                    );

                    AddVisiblePhysicalEvents(
                        observedEvents,
                        observerClientId,
                        sourceClientId,
                        sourceState.CommittedActionPayloads,
                        ObservedPhysicalEventPlanState.Committed
                    );
                }

                observerState
                    .ReplaceObservedPhysicalEventsServer(
                        observedEvents
                    );

                SLog(
                    "[PHYSICAL EVENT PROJECTION] " +
                    $"observer={observerClientId} | " +
                    $"events={observedEvents.Count} | " +
                    $"reason={reason}"
                );
            }
        }

        private void AddVisiblePhysicalEvents(
            List<ObservedPhysicalEventSummary>
                destination,
            ulong observerClientId,
            ulong eventOwnerClientId,
            IReadOnlyList<DraftedActionPayload>
                payloads,
            ObservedPhysicalEventPlanState planState)
        {
            if (destination == null ||
                payloads == null)
            {
                return;
            }

            for (int i = 0;
                 i < payloads.Count;
                 i++)
            {
                DraftedActionPayload payload =
                    payloads[i];

                PhysicalEventVisibilityContext context =
                    new PhysicalEventVisibilityContext(
                        observerClientId,
                        eventOwnerClientId,
                        payload,
                        planState
                    );

                if (!_physicalEventVisibilityPolicy
                        .CanObserve(context))
                {
                    continue;
                }

                destination.Add(
                    ObservedPhysicalEventSummary.Create(
                        payload,
                        eventOwnerClientId,
                        actionPosition: i + 1,
                        planState
                    )
                );
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

        private void BeginDedicatedSessionShutdown(
            string reason)
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;

            SLog(
                "[SESSION SHUTDOWN] Beginning | " +
                $"reason={reason} | " +
                $"globalTurn={globalTurn.Value} | " +
                $"round={roundIndex.Value}"
            );

            Debug.Log(
                "[SESSION SHUTDOWN] Dedicated server exiting."
            );

            /*
             * Application.Quit is the normal Unity exit path.
             *
             * The delayed Environment.Exit fallback is independent
             * of this networked GameCoordinator. It still executes
             * if NGO teardown destroys this object or Unity fails
             * to terminate the headless Windows process cleanly.
             */
            _ = ForceDedicatedServerProcessExitAsync();

            Application.Quit(0);
        }

        private static async Task
            ForceDedicatedServerProcessExitAsync()
        {
            await Task.Delay(1000)
                .ConfigureAwait(false);

            System.Environment.Exit(0);
        }

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

        private void BindActionPlanProjectionSource(
            NetPlayerState state)
        {
            if (state == null)
                return;

            state.ServerActionPlanChanged -=
                HandleServerActionPlanChanged;

            state.ServerActionPlanChanged +=
                HandleServerActionPlanChanged;
        }

        private void UnbindActionPlanProjectionSource(
            NetPlayerState state)
        {
            if (state == null)
                return;

            state.ServerActionPlanChanged -=
                HandleServerActionPlanChanged;
        }

        private void HandleServerActionPlanChanged(
            NetPlayerState changedState)
        {
            if (!IsServer ||
                _isShuttingDown)
            {
                return;
            }

            ulong clientId =
                changedState != null
                    ? changedState.OwnerClientIdCached
                    : ulong.MaxValue;

            RebuildObservedPhysicalEventProjectionsServer(
                $"action plan changed | client={clientId}"
            );
        }
    }
}
