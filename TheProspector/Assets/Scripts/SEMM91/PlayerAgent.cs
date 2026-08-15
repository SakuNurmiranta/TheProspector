using System.Collections;
using Unity.Netcode;
using UnityEngine;
using SEMM91.InputSystems;
using SEMM91.Networking;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Kvlt.TurnFlow;


namespace SEMM91
{
    public class PlayerAgent : NetworkBehaviour
    {
        [SerializeField] private bool logAgentDebug = false;
        private Coroutine _botRoutine;
        private Coroutine _readyReportRoutine;
        private NetPlayerState _playerState;

        //bot-stuff
        private bool _botPassive;
        private bool _botMode;
        private bool _botStress;
        private int _botSeed;
        private int _lastProcessedBotTurn = int.MinValue;
        private int _lastProcessedHappeningTurn = int.MinValue;
        private int _lastProcessedVoteTurn = int.MinValue;
        private enum MasherProductionPhase
        {
            Gestate,
            Rehearse,
            Promote
        }

        private MasherProductionPhase _masherProductionPhase =
            MasherProductionPhase.Gestate;

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

            _playerState = GetComponent<NetPlayerState>();
            if (_playerState == null)
            {
                Debug.LogError(
                    "[PlayerAgent] Missing NetPlayerState " +
                    "on player object.",
                    this
                );

                return;
            }

            _botPassive =
                BotConfig.HasArg("-botPassive") ||
                BotConfig.GetIntArg(
                    "-botPassive",
                    0
                ) != 0;
            _botMode = BotConfig.HasArg("-bot") || BotConfig.GetIntArg("-bot", 0) != 0;
            _botStress = BotConfig.HasArg("-botStress") || BotConfig.GetIntArg("-botStress", 0) != 0;
            _botSeed = BotConfig.GetIntArg("-botSeed", 12345) + (int)NetworkManager.Singleton.LocalClientId;

            _readyReportRoutine =
                StartCoroutine(
                    ReportReadyUntilAcknowledged()
                );

            if (_botMode && !_botPassive)
            {
                _botRoutine =
                    StartCoroutine(
                        BotLoop(
                            _botSeed,
                            _botStress
                        )
                    );

                Debug.Log(
                    "[BOT] Legacy bot loop started | " +
                    $"stress={_botStress} | " +
                    $"seed={_botSeed} | " +
                    $"clientId=" +
                    $"{NetworkManager.Singleton.LocalClientId}"
                );
            }
            else if (_botMode)
            {
                Debug.Log(
                    "[BOT] Masher-Bot 2000 registered " +
                    "in passive mode | " +
                    $"seed={_botSeed} | " +
                    $"clientId=" +
                    $"{NetworkManager.Singleton.LocalClientId}"
                );
            }
            else
            {
                if (logAgentDebug)
                {
                    Debug.Log(
                        $"[HUMAN] Controls enabled for " +
                        $"clientId={OwnerClientId}."
                    );
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAgentRoutines();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            StopAgentRoutines();
            base.OnDestroy();
        }

        private void StopAgentRoutines()
        {
            if (_readyReportRoutine != null)
            {
                StopCoroutine(_readyReportRoutine);
                _readyReportRoutine = null;
            }

            if (_botRoutine != null)
            {
                StopCoroutine(_botRoutine);
                _botRoutine = null;
            }
        }

        private IEnumerator
            ReportReadyUntilAcknowledged()
        {
            while (IsSpawned &&
                   IsOwner &&
                   IsClient &&
                   _playerState != null &&
                   !_playerState
                       .SessionReadyAcknowledgedValue)
            {
                GameCoordinator coordinator =
                    GameCoordinator.Instance;

                if (coordinator != null &&
                    coordinator.IsSpawned)
                {
                    coordinator
                        .ReportClientReadyServerRpc(
                            _botMode
                        );
                }

                yield return
                    new WaitForSecondsRealtime(0.5f);
            }

            _readyReportRoutine = null;
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


            if (Input.GetKeyDown(KeyCode.I))
            {
                RequestIfAvailable(
                    PlayerCommand.AddIdeaToCurrentTrack
                );
            }

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
                _actionController.Request(PlayerCommand.CreateEmptyRehearsalSet);
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                RequestIfAvailable(
                    PlayerCommand.CycleTarget
                );
            }

            if (Input.GetKeyDown(KeyCode.C))
                RequestIfAvailable(PlayerCommand.ContextualCreate);

            if (Input.GetKeyDown(KeyCode.H))
                RequestIfAvailable(PlayerCommand.HailSatan);

            if (Input.GetKeyDown(KeyCode.O))
                RequestIfAvailable(PlayerCommand.HailOdin);

            if (Input.GetKeyDown(KeyCode.J))
                RequestIfAvailable(PlayerCommand.VoteSociety);

            if (Input.GetKeyDown(KeyCode.K))
                RequestIfAvailable(PlayerCommand.VoteKvlt);

            if (Input.GetKeyDown(KeyCode.B))
                RequestIfAvailable(
                    PlayerCommand.KeeperBoostVisibility);

            if (Input.GetKeyDown(KeyCode.N))
                RequestIfAvailable(
                    PlayerCommand.KeeperSuppressVisibility);
        }

        private IEnumerator BotLoop(
            int seed,
            bool stress)
        {
            /*
             * Wait until the replicated playable-session flag
             * becomes available on this client.
             */
            while (true)
            {
                GameCoordinator coordinator =
                    GameCoordinator.Instance;

                if (coordinator != null &&
                    coordinator.testStarted.Value)
                {
                    break;
                }

                yield return null;
            }

            int turnDelayMs =
                BotConfig.GetIntArg(
                    "-botTurnDelayMs",
                    stress ? 100 : 1000
                );

            int stepDelayMs =
                BotConfig.GetIntArg(
                    "-botStepDelayMs",
                    stress ? 100 : 400
                );

            float turnDelaySeconds =
                turnDelayMs / 1000f;

            float stepDelaySeconds =
                stepDelayMs / 1000f;

            Debug.Log(
                "[MASHER BOT] Turn driver started | " +
                $"seed={seed} | " +
                $"turnDelayMs={turnDelayMs} | " +
                $"stepDelayMs={stepDelayMs} | " +
                $"clientId={OwnerClientId}"
            );

            while (true)
            {
                GameCoordinator coordinator =
                    GameCoordinator.Instance;

                if (coordinator != null &&
                    coordinator.testStarted.Value &&
                    _playerState != null &&
                    _playerState.ActiveValue)
                {
                    int interactionTurn =
                        coordinator.globalTurn.Value;

                    if (coordinator
                            .peak2TurnResolutionPhase.Value ==
                        KvltTurnResolutionRuntimePhase
                            .SharedHappeningWindowOpen &&
                        _lastProcessedHappeningTurn !=
                            interactionTurn)
                    {
                        _lastProcessedHappeningTurn =
                            interactionTurn;

                        int configuredHail =
                            BotConfig.GetIntArg(
                                "-botHailAspect",
                                -1);

                        if (configuredHail == 2)
                        {
                            yield return new WaitForSecondsRealtime(
                                stepDelaySeconds);
                            continue;
                        }

                        PlayerCommand hailCommand =
                            configuredHail == 0
                                ? PlayerCommand.HailSatan
                                : configuredHail == 1
                                    ? PlayerCommand.HailOdin
                                    : (OwnerClientId +
                                       (ulong)interactionTurn) % 2 == 0
                                        ? PlayerCommand.HailSatan
                                        : PlayerCommand.HailOdin;

                        RequestIfAvailable(hailCommand);
                        yield return new WaitForSecondsRealtime(
                            stepDelaySeconds);
                        continue;
                    }

                    if (coordinator
                            .peak2TurnResolutionPhase.Value ==
                        KvltTurnResolutionRuntimePhase
                            .AwaitingAllegianceCrisisResolution &&
                        _lastProcessedVoteTurn !=
                            interactionTurn)
                    {
                        _lastProcessedVoteTurn =
                            interactionTurn;

                        int configuredVote =
                            BotConfig.GetIntArg(
                                "-botAllegianceVote",
                                -1);

                        PlayerCommand voteCommand =
                            configuredVote == 0
                                ? PlayerCommand.VoteSociety
                                : configuredVote == 1
                                    ? PlayerCommand.VoteKvlt
                                    : (OwnerClientId +
                                       (ulong)interactionTurn) % 2 == 0
                                        ? PlayerCommand.VoteKvlt
                                        : PlayerCommand.VoteSociety;

                        RequestIfAvailable(voteCommand);
                        yield return new WaitForSecondsRealtime(
                            stepDelaySeconds);
                        continue;
                    }
                }

                if (coordinator == null ||
                    !coordinator.testStarted.Value ||
                    _playerState == null ||
                    !_playerState.ActiveValue ||
                    _playerState.HasCommittedTurnValue)
                {
                    yield return null;
                    continue;
                }

                int globalTurn =
                    coordinator.globalTurn.Value;

                if (globalTurn ==
                    _lastProcessedBotTurn)
                {
                    yield return null;
                    continue;
                }

                /*
                 * Mark the turn before beginning the sequence.
                 * If a step fails, the bot halts rather than
                 * spamming repeated requests into the same turn.
                 */
                _lastProcessedBotTurn =
                    globalTurn;

                Debug.Log(
                    "[MASHER BOT] Beginning turn | " +
                    $"turn={globalTurn} | " +
                    $"clientId={OwnerClientId}"
                );

                yield return new WaitForSecondsRealtime(
                    turnDelaySeconds
                );

                /*
                 * The prototype Keeper currently completes its
                 * lockstep turn through an empty commit. Regular
                 * band stance and draft commands are correctly
                 * unavailable while the band is dormant.
                 */
                if (coordinator.IsClientCurrentKeeper(
                        OwnerClientId))
                {
                    Debug.Log(
                        "[MASHER BOT] Keeper turn detected | " +
                        $"turn={globalTurn} | " +
                        $"clientId={OwnerClientId}"
                    );

                    if (_actionController.CanRequest(
                            PlayerCommand.KeeperBoostVisibility))
                    {
                        _actionController.Request(
                            PlayerCommand.KeeperBoostVisibility);

                        yield return new WaitForSecondsRealtime(
                            stepDelaySeconds);
                    }

                    if (!TryMasherRequest(
                            PlayerCommand.CommitTurn,
                            globalTurn))
                    {
                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(
                        stepDelaySeconds
                    );

                    /*
                     * When the human has already committed, this
                     * request may advance the turn immediately and
                     * reset HasCommittedTurn before the bot sees it.
                     */
                    bool keeperCommitObserved =
                        _playerState.HasCommittedTurnValue ||
                        coordinator.globalTurn.Value != globalTurn;

                    if (!keeperCommitObserved)
                    {
                        Debug.LogError(
                            "[MASHER BOT] Keeper commit was not observed | " +
                            $"turn={globalTurn} | " +
                            $"currentTurn={coordinator.globalTurn.Value}"
                        );

                        yield break;
                    }

                    Debug.Log(
                        "[MASHER BOT] Keeper turn completed | " +
                        $"turn={globalTurn} | " +
                        "actions=EmptyKeeperCommit"
                    );

                    continue;
                }
                BandStance selectedStance;
                PlayerCommand stanceCommand;

                switch (_masherProductionPhase)
                {
                    case MasherProductionPhase.Gestate:
                        selectedStance = BandStance.Gestate;
                        stanceCommand = PlayerCommand.SelectGestate;
                        break;

                    case MasherProductionPhase.Rehearse:
                        selectedStance = BandStance.Rehearse;
                        stanceCommand = PlayerCommand.SelectRehearse;
                        break;

                    case MasherProductionPhase.Promote:
                        selectedStance = BandStance.Promote;
                        stanceCommand = PlayerCommand.SelectPromote;
                        break;

                    default:
                        Debug.LogError(
                            "[MASHER BOT] Unknown production phase | " +
                            $"phase={_masherProductionPhase}"
                        );

                        yield break;
                }

                Debug.Log(
                    "[MASHER BOT] Production phase selected | " +
                    $"turn={globalTurn} | " +
                    $"phase={_masherProductionPhase} | " +
                    $"stance={selectedStance}"
                );
                // -------------------------------------------------
                // Step 1: select Gestate
                // -------------------------------------------------

                if (!TryMasherRequest(
                        stanceCommand,
                        globalTurn))
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(
                    stepDelaySeconds
                );

                if (_playerState.CurrentStanceValue !=
                    selectedStance)
                {
                    Debug.LogError(
                        "[MASHER BOT] Stance confirmation failed | " +
                        $"turn={globalTurn} | " +
                        $"expected={selectedStance} | " +
                        $"actual={_playerState.CurrentStanceValue}"
                    );

                    yield break;
                }

                if (selectedStance == BandStance.Promote &&
                    _actionController.CanRequest(
                        PlayerCommand.ContextualCreate))
                {
                    _actionController.Request(
                        PlayerCommand.ContextualCreate);

                    yield return new WaitForSecondsRealtime(
                        stepDelaySeconds);
                }

                // -------------------------------------------------
                // Step 2: draft Gestate primary
                // -------------------------------------------------

                if (!TryMasherRequest(
                        PlayerCommand.DraftPrimaryAction,
                        globalTurn))
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(
                    stepDelaySeconds
                );

                if (_playerState.DraftedActionsValue < 1)
                {
                    Debug.LogError(
                        "[MASHER BOT] Primary draft was not observed | " +
                        $"turn={globalTurn} | " +
                        $"drafted={_playerState.DraftedActionsValue}"
                    );

                    yield break;
                }

                // -------------------------------------------------
                // Step 3: draft Gestate secondary
                // -------------------------------------------------

                if (!TryMasherRequest(
                        PlayerCommand.DraftSecondaryAction,
                        globalTurn))
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(
                    stepDelaySeconds
                );

                if (_playerState.DraftedActionsValue < 2)
                {
                    Debug.LogError(
                        "[MASHER BOT] Secondary draft was not observed | " +
                        $"turn={globalTurn} | " +
                        $"drafted={_playerState.DraftedActionsValue}"
                    );

                    yield break;
                }

                // -------------------------------------------------
                // Step 4: commit
                // -------------------------------------------------

                if (!TryMasherRequest(
                        PlayerCommand.CommitTurn,
                        globalTurn))
                {
                    yield break;
                }

                yield return new WaitForSecondsRealtime(
                    stepDelaySeconds
                );

                /*
                 * If the human had already committed, the bot's
                 * commit may immediately advance the global turn.
                 * In that case HasCommittedTurn may already have
                 * reset before this client observes it.
                 */
                bool commitObserved =
                    _playerState.HasCommittedTurnValue ||
                    coordinator.globalTurn.Value != globalTurn;

                if (!commitObserved)
                {
                    Debug.LogError(
                        "[MASHER BOT] Commit was not observed | " +
                        $"turn={globalTurn} | " +
                        $"currentTurn={coordinator.globalTurn.Value}"
                    );

                    yield break;
                }

                MasherProductionPhase completedPhase =
                    _masherProductionPhase;

                _masherProductionPhase =
                    _masherProductionPhase switch
                    {
                        MasherProductionPhase.Gestate =>
                            MasherProductionPhase.Rehearse,

                        MasherProductionPhase.Rehearse =>
                            MasherProductionPhase.Promote,

                        MasherProductionPhase.Promote =>
                            MasherProductionPhase.Gestate,

                        _ =>
                            MasherProductionPhase.Gestate
                    };

                Debug.Log(
                    "[MASHER BOT] Turn completed | " +
                    $"turn={globalTurn} | " +
                    $"phase={completedPhase} | " +
                    $"stance={selectedStance} | " +
                    "actions=Primary,Secondary | " +
                    $"nextPhase={_masherProductionPhase}"
                );
            }
        }

        private bool TryMasherRequest(
            PlayerCommand command,
            int globalTurn)
        {
            if (_actionController == null)
            {
                Debug.LogError(
                    "[MASHER BOT] Missing action controller | " +
                    $"turn={globalTurn} | " +
                    $"command={command}"
                );

                return false;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(command);

            if (!presentation.IsAvailable)
            {
                Debug.LogError(
                    "[MASHER BOT] Command unavailable | " +
                    $"turn={globalTurn} | " +
                    $"command={command} | " +
                    $"reason={presentation.UnavailableReason} | " +
                    $"reasonText={presentation.UnavailableReasonText}"
                );

                return false;
            }

            Debug.Log(
                "[MASHER BOT] Requesting command | " +
                $"turn={globalTurn} | " +
                $"command={command} | " +
                $"label={presentation.Label}"
            );

            _actionController.Request(command);

            return true;
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
