using SEMM91;
using SEMM91.Core.Entities;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine;

namespace SEMM91.InputSystems
{
    /// <summary>
    /// Networked player-will router.
    ///
    /// Receives player/bot gameplay requests and routes them through the existing authorititative systems. </summary>
    ///
    public class PlayerActionController : NetworkBehaviour
    {
        [Header("Debug")]
        //[SerializeField] private bool logRequests = true;
        [SerializeField]
        private bool logAcceptedCommands;

        [SerializeField] private bool logRejectedCommands = true;

        [SerializeField] private bool enableHostForceStartHotkey = true;
        [SerializeField] private KeyCode hostForceStartKey = KeyCode.F8;


        private NetPlayerState _playerState;

        private NetPlayerState GetPlayerState()
        {
            if (_playerState == null)
            {
                _playerState =
                    GetComponent<NetPlayerState>();
            }

            return _playerState;
        }

        public void RequestDream()
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitDreamServerRpc();
        }

        public void RequestDraftAction()
        {
            if (!IsOwner || !IsClient) return;

            SubmitDraftActionServerRpc();
        }

        public void RequestUndoDraftAction()
        {
            if (!IsOwner || !IsClient) return;

            SubmitUndoDraftActionServerRpc();
        }

        public void RequestSelectStance(BandStance stance)
        {
            if (!IsOwner || !IsClient) return;

            SubmitStanceServerRpc(stance);
        }

        public void RequestCommitTurn()
        {
            if (!IsOwner || !IsClient) return;

            SubmitCommitTurnServerRpc();
        }

        public void RequestCycleTarget()
        {
            if (!IsOwner || !IsClient) return;

            SubmitCycleTargetServerRpc();
        }

        public void RequestForceStartSession()
        {
            if (!CanForceStartSession())
                return;

            GameCoordinator.Instance
                .ForceStartPlayableSessionServer();
        }

        private void RequestQuitSession()
        {
            var coordinator = GameCoordinator.Instance;

            if (coordinator != null)
            {
                coordinator.BeginShutdown();
            }
            else
            {
                ShutdownNetworkAndQuit();
            }
        }

        // CanRequest is used for UI buttons (are they in available state or not?)
        public bool CanRequest(
            PlayerCommand command)
        {
            if (!IsOwner || !IsClient)
                return false;

            return GetPresentation(command)
                .IsAvailable;
        }
        public void Request(PlayerCommand command)
        {
            switch (command)
            {
                case PlayerCommand.Dream:
                    RequestDream();
                    break;

                case PlayerCommand.SelectGestate:
                    RequestSelectStance(BandStance.Gestate);
                    break;

                case PlayerCommand.SelectRehearse:
                    RequestSelectStance(BandStance.Rehearse);
                    break;

                case PlayerCommand.SelectPromote:
                    RequestSelectStance(BandStance.Promote);
                    break;

                case PlayerCommand.DraftAction:
                    RequestDraftAction();
                    break;

                case PlayerCommand.UndoDraftAction:
                    RequestUndoDraftAction();
                    break;

                case PlayerCommand.CommitTurn:
                    RequestCommitTurn();
                    break;

                case PlayerCommand.CycleTarget:
                    RequestCycleTarget();
                    break;

                case PlayerCommand.AdminCreateEmptyRehearsalSet:
                    RequestAdminCreateEmptyRehearsalSet();
                    break;

                case PlayerCommand.ForceStartSession:
                    RequestForceStartSession();
                    break;

                case PlayerCommand.QuitSession:
                    RequestQuitSession();
                    break;

                case PlayerCommand.DraftPrimaryAction:
                    RequestDraftStanceSlotAction(1);
                    break;

                case PlayerCommand.DraftSecondaryAction:
                    RequestDraftStanceSlotAction(2);
                    break;

                case PlayerCommand.DraftTertiaryAction:
                    RequestDraftStanceSlotAction(3);
                    break;

                case PlayerCommand.DraftRestAction:
                    RequestDraftRestAction();
                    break;
            }
        }

        private void RequestAdminCreateEmptyRehearsalSet()
        {
            SubmitAdminCreateEmptyRehearsalSetServerRpc();
        }

        private void RequestDraftRestAction()
        {
            RequestCommitTurn();
        }

        private void RequestDraftStanceSlotAction(int slotIndex)
        {
            SubmitDraftStanceSlotActionServerRpc(slotIndex);
        }

        private bool CanCycleTarget(
            ulong clientId,
            NetPlayerState state)
        {
            return GetCycleTargetUnavailableReason(
                       state
                   ) ==
                   ActionUnavailableReason.None;
        }
        private static bool CanCycleIdeaSourceTarget(
            NetPlayerState state)
        {
            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
                return false;

            foreach (TagContainerType sourceType
                     in IdeaSourceCycleOrder)
            {
                if (playerEntity.TryGetTagContainer(
                        sourceType,
                        out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanCycleRehearsalTarget(
            NetPlayerState state)
        {
            GameEntity playerEntity =
                state.PlayerEntity;

            return playerEntity != null &&
                   playerEntity.VhsSets.Count > 0;
        }

        [ServerRpc]
        private void SubmitDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            var state = GetComponent<NetPlayerState>();
            // var coordinator = GameCoordinator.Instance;
            if (!CanDraftAction(clientId, state))
            {
                LogRejected($"Draft action request from client {clientId} rejected.");
                return;
            }

            if (state == null)
            {
                LogRejected($"Draft action blocked for client {clientId}: missing NetPlayerState.");
                return;
            }

            DraftedActionPayload payload = CreatePayloadForCurrentStance(state);

            if (payload == null)
            {
                LogRejected($"Draft action blocked for client {clientId}: no current stance.");
                return;
            }

            if (!state.TryAddDraftedActionServer(
                    payload))
            {
                LogRejected(
                    $"Draft action blocked for client {clientId}: " +
                    "draft state rejected insertion."
                );

                return;
            }

            //state.AddDraftedActionPayloadServer(payload);
            Debug.Log(
                $"[DRAFT PAYLOAD] " +
                $"action={payload.ActionType} " +
                $"ideaSource=" +
                $"{payload.IdeaSourceContainerType?.ToString() ?? "none"} " +
                $"turn={payload.CreatedTurn}",
                this
            );


            //state.IncrementDraftedActionsServer();

            LogAccepted(
                $"Client {clientId} drafted " +
                $"{payload.ActionType} into action position " +
                $"{state.DraftedActionsValue}."
            );
        }

        [ServerRpc]
        private void SubmitUndoDraftActionServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (!CanUndoDraftAction(clientId, state))
            {
                LogRejected($"Undo draft request from client {clientId} rejected.");
                return;
            }


            if (!state.TryRemoveLastDraftedActionServer(
                    out DraftedActionPayload removedPayload))
            {
                LogRejected(
                    $"Undo failed for client {clientId}: " +
                    "draft buffer was empty or inconsistent."
                );

                return;
            }

            LogAccepted(
                $"Client {clientId} removed " +
                $"{removedPayload.ActionType}; " +
                $"remaining drafted actions=" +
                $"{state.DraftedActionsValue}."
            );
        }

        [ServerRpc]
        private void SubmitStanceServerRpc(BandStance stance, ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (!CanChangeStance(clientId, state))
            {
                LogRejected($"Stance selection blocked for client {clientId}: {stance}.");
                return;
            }

            state.SetCurrentStanceServer(stance);

            LogAccepted($"Client {clientId} selected {stance}.");
        }

        [ServerRpc]
        private void SubmitCommitTurnServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            var state = GetComponent<NetPlayerState>();
            if (!CanCommitTurn(clientId, state))
            {
                LogRejected($"Commit turn request from client {clientId} rejected.");
                return;
            }

            Debug.Log(
                $"[COMMIT REQUEST] client={clientId} stance={state.CurrentStanceValue} " +
                $"drafted={state.DraftedActionsValue}"
            );

            CommitDraftToState(clientId, state);

            Debug.Log(
                $"[COMMIT AFTER DRAFT TRANSFER] client={clientId} " +
                $"committed={state.CommittedActionsValue} payloads={state.CommittedActionPayloads.Count}"
            );

            var coordinator = GameCoordinator.Instance;
            if (coordinator == null)
            {
                LogRejected($"Commit turn blocked for client {clientId}: missing GameCoordinator.");
                return;
            }

            Debug.Log($"[COMMIT BEFORE COMPLETE TURN] client={clientId}");
            coordinator.CompleteCommittedTurn(clientId, state);

            LogAccepted($"Client {clientId} requested turn commit.");
        }


        [ServerRpc]
        private void SubmitDreamServerRpc(
            ServerRpcParams p = default)
        {
            ulong clientId =
                p.Receive.SenderClientId;

            Debug.Log(
                $"[DREAM RPC] client={clientId}",
                this
            );

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                LogRejected(
                    $"Dream request from client {clientId} rejected: " +
                    "missing GameCoordinator."
                );

                return;
            }

            bool success =
                coordinator.TryResolveDreamServer(
                    clientId,
                    out _,
                    out _,
                    out string failureReason
                );

            if (!success)
            {
                LogRejected(
                    $"Dream request from client {clientId} rejected: " +
                    failureReason
                );

                return;
            }

            LogAccepted(
                $"Client {clientId} resolved Dream."
            );
        }

        [ServerRpc]
        private void SubmitCycleTargetServerRpc(
            ServerRpcParams p = default)
        {
            ulong clientId =
                p.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            if (!CanCycleTarget(clientId, state))
            {
                LogRejected(
                    $"Cycle target request from client {clientId} " +
                    "was rejected."
                );

                return;
            }

            if (!TryCycleTargetServer(
                    state,
                    out string selectedTarget,
                    out string failureReason))
            {
                LogRejected(
                    $"Cycle target request from client {clientId} " +
                    $"was rejected: {failureReason}"
                );

                return;
            }

            LogAccepted(
                $"Client {clientId} selected target " +
                $"{selectedTarget}."
            );
        }

        public bool CanForceStartSession()
        {
            if (!IsOwner || !IsClient)
                return false;

            NetworkManager manager =
                NetworkManager.Singleton;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            return manager != null &&
                   manager.IsHost &&
                   coordinator != null &&
                   !coordinator.IsPlayableSessionStarted;
        }

        private bool CanCommitTurn(
            ulong clientId,
            NetPlayerState state)
        {
            return GetCommitUnavailableReason(
                       state
                   ) ==
                   ActionUnavailableReason.None;
        }
        private void CommitDraftToState(
            ulong clientId,
            NetPlayerState state)
        {
            int productiveActionCount = 0;

            foreach (DraftedActionPayload payload
                     in state.DraftedActionPayloads)
            {
                if (payload == null)
                    continue;

                if (TurnActionRules.IsProductive(
                        payload.ActionType))
                {
                    productiveActionCount++;
                }
            }

            if (!TurnActionRules.IsValidProductiveActionCount(
                    productiveActionCount))
            {
                Debug.LogError(
                    $"[COMMIT ERROR] Client {clientId} attempted " +
                    $"{productiveActionCount} productive actions.",
                    this
                );

                return;
            }

            state.ResetCommittedActionsServer();

            for (int i = 0;
                 i < productiveActionCount;
                 i++)
            {
                state.IncrementCommittedActionServer();
            }

            state.CommitDraftedActionPayloadsServer();

            string loadDescription =
                TurnActionRules.IsOverreach(
                    productiveActionCount)
                    ? "OVERREACH"
                    : "recovery retained";

            LogAccepted(
                $"Client {clientId} committed " +
                $"{productiveActionCount} productive actions; " +
                $"{loadDescription}."
            );
        }

        private bool CanChangeStance(
            ulong clientId,
            NetPlayerState state)
        {
            return GetStanceChangeUnavailableReason(
                       state
                   ) ==
                   ActionUnavailableReason.None;
        }
        private bool CanDraftAction(
            ulong clientId,
            NetPlayerState state)
        {
            return GetDraftUnavailableReason(
                       state
                   ) ==
                   ActionUnavailableReason.None;
        }
        private bool CanUndoDraftAction(
            ulong clientId,
            NetPlayerState state)
        {
            return GetUndoUnavailableReason(
                       state
                   ) ==
                   ActionUnavailableReason.None;
        }
        private void LogAccepted(string message)
        {
            if (!logAcceptedCommands)
                return;

            Debug.Log($"[PlayerActionController] ACCEPTED: {message}", this);
        }

        private void LogRejected(string message)
        {
            if (!logRejectedCommands)
                return;

            Debug.LogWarning($"[PlayerActionController] REJECTED: {message}", this);
        }

        private static DraftedActionPayload CreatePayloadForAction(
            NetPlayerState state,
            DraftedActionType actionType,
            int currentTurn)
        {
            if (actionType == DraftedActionType.CreateIdea)
            {
                return new DraftedActionPayload(
                    actionType,
                    currentTurn,
                    state.SelectedIdeaSourceValue
                );
            }

            return new DraftedActionPayload(
                actionType,
                currentTurn
            );
        }

        private DraftedActionPayload CreatePayloadForCurrentStance(
            NetPlayerState state)
        {
            if (state == null)
                return null;

            int currentTurn =
                GameCoordinator.Instance != null
                    ? GameCoordinator.Instance.globalTurn.Value
                    : 0;

            return state.CurrentStanceValue switch
            {
                BandStance.Gestate =>
                    CreatePayloadForAction(
                        state,
                        DraftedActionType.CreateIdea,
                        currentTurn
                    ),

                BandStance.Rehearse =>
                    CreatePayloadForAction(
                        state,
                        DraftedActionType.RehearseActiveSet,
                        currentTurn
                    ),

                BandStance.Promote => null,

                _ => null
            };
        }

        private void ShutdownNetworkAndQuit()
        {
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

        [ServerRpc]
        private void SubmitDraftStanceSlotActionServerRpc(
            int slotIndex,
            ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;

            Debug.Log($"[SLOT REQUEST] client={clientId} slot={slotIndex}");

            var state = GetComponent<NetPlayerState>();


            if (state == null)
            {
                LogRejected($"Draft slot {slotIndex} rejected for client {clientId}: missing NetPlayerState.");
                return;
            }

            if (!TryGetDraftedActionTypeForSlot(
                    state.CurrentStanceValue,
                    slotIndex,
                    out DraftedActionType actionType,
                    out bool isImmediate))
            {
                LogRejected(
                    $"Draft slot {slotIndex} rejected for client {clientId}: " +
                    $"no action for stance {state.CurrentStanceValue}."
                );
                return;
            }

            Debug.Log(
                $"[SLOT RESOLVED] client={clientId} stance={state.CurrentStanceValue} " +
                $"slot={slotIndex} actionType={actionType} immediate={isImmediate}"
            );

            if (isImmediate)
            {
                ActionUnavailableReason reason =
                    GetOpenTurnUnavailableReason(state);

                if (reason !=
                    ActionUnavailableReason.None)
                {
                    LogRejected(
                        $"Immediate slot {slotIndex} rejected for " +
                        $"client {clientId}: " +
                        $"{ActionPresentationText.GetReasonText(reason)}"
                    );

                    return;
                }

                ResolveImmediateSlotAction(
                    clientId,
                    state,
                    actionType
                );

                return;
            }
            if (!CanDraftSpecificAction(state, actionType))
            {
                LogRejected($"Draft slot {slotIndex} rejected for client {clientId}: cannot draft {actionType}.");
                return;
            }

            if (!DraftSpecificAction(
                    state,
                    actionType))
            {
                LogRejected(
                    $"Draft slot {slotIndex} rejected for " +
                    $"client {clientId}: insertion failed."
                );

                return;
            }

            string planPosition =
                state.DraftedActionsValue ==
                TurnActionRules.MaximumProductiveActions
                    ? "Overreach"
                    : $"Standard {state.DraftedActionsValue}";

            LogAccepted(
                $"Client {clientId} drafted {actionType} " +
                $"into {planPosition}."
            );
        }

        private bool TryGetDraftedActionTypeForSlot(
            BandStance stance,
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (stance)
            {
                case BandStance.Gestate:
                    return TryGetGestateSlotAction(slotIndex, out actionType, out isImmediate);

                case BandStance.Rehearse:
                    return TryGetRehearseSlotAction(slotIndex, out actionType, out isImmediate);

                case BandStance.Promote:
                    return TryGetPromoteSlotAction(slotIndex, out actionType, out isImmediate);

                default:
                    return false;
            }
        }

        private bool TryGetGestateSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.CreateIdea;
                    return true;

                case 2:
                    actionType =
                        DraftedActionType
                            .DebugPlaceholderGestationSecondary;

                    return true;

                case 3:
                    return false;

                default:
                    return false;
            }
        }

        private bool TryGetRehearseSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.RehearseActiveSet;
                    return true;

                case 2:
                    actionType = DraftedActionType.RecordActiveSetToDemo;
                    return true;

                case 3:
                    return false;

                default:
                    return false;
            }
        }

        private bool TryGetPromoteSlotAction(
            int slotIndex,
            out DraftedActionType actionType,
            out bool isImmediate)
        {
            actionType = DraftedActionType.None;
            isImmediate = false;

            switch (slotIndex)
            {
                case 1:
                    actionType = DraftedActionType.ReleaseLatestDemoToKvlt;
                    return true;

                case 2:
                    actionType = DraftedActionType.DebugPlaceholderPromotionSecondary;
                    return true;

                case 3:
                    actionType = DraftedActionType.None;
                    isImmediate = true;
                    return true;

                default:
                    return false;
            }
        }

        private bool DraftSpecificAction(
            NetPlayerState state,
            DraftedActionType actionType)
        {
            int currentTurn =
                GameCoordinator.Instance != null
                    ? GameCoordinator.Instance.globalTurn.Value
                    : 0;

            DraftedActionPayload payload =
                CreatePayloadForAction(
                    state,
                    actionType,
                    currentTurn
                );

            if (!state.TryAddDraftedActionServer(
                    payload))
            {
                return false;
            }

            Debug.Log(
                $"[DRAFT PAYLOAD] " +
                $"action={payload.ActionType} " +
                $"ideaSource=" +
                $"{payload.IdeaSourceContainerType?.ToString() ?? "none"} " +
                $"turn={payload.CreatedTurn}",
                this
            );

            return true;
        }

        private bool CanDraftSpecificAction(
            NetPlayerState state,
            DraftedActionType actionType)
        {
            return GetSpecificDraftUnavailableReason(
                       state,
                       actionType
                   ) ==
                   ActionUnavailableReason.None;
        }
        private void ResolveImmediateSlotAction(
            ulong clientId,
            NetPlayerState state,
            DraftedActionType actionType)
        {
            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    LogAccepted($"Client {clientId} used Gestate tertiary placeholder action.");
                    break;

                case BandStance.Rehearse:
                    //RequestDebugCycleActiveRehearsalSet();
                    break;

                case BandStance.Promote:
                    LogAccepted($"Client {clientId} used Promote tertiary placeholder switch action.");
                    break;

                default:
                    LogRejected($"Immediate slot action rejected for client {clientId}: no valid stance.");
                    break;
            }
        }

        [ServerRpc]
        private void SubmitAdminCreateEmptyRehearsalSetServerRpc(ServerRpcParams p = default)
        {
            ulong clientId = p.Receive.SenderClientId;
            var state = GetComponent<NetPlayerState>();

            if (state == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing state.");
                return;
            }

            if (state.PlayerEntity == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing controller entity.");
                return;
            }

            var coordinator = GameCoordinator.Instance;

            if (coordinator == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing coordinator.");
                return;
            }

            if (coordinator.RehearsalResolver == null)
            {
                LogRejected($"Create empty rehearsal set rejected for client {clientId}: missing rehearsal resolver.");
                return;
            }

            bool success = coordinator.RehearsalResolver.TryCreateNewActiveEmptyVhsSet(
                clientId,
                state.PlayerEntity,
                out string message
            );


            if (success)
                LogAccepted(message);
            else
                LogRejected(message);
        }

#if UNITY_EDITOR
        [ContextMenu("Debug/Request Pajazzo Dream")]
        private void DebugRequestPajazzoDream()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] Enter Play Mode first.",
                    this
                );

                return;
            }

            if (!IsClient)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] This controller is not running " +
                    "as a client.",
                    this
                );

                return;
            }

            if (!IsOwner)
            {
                Debug.LogWarning(
                    "[DREAM DEBUG] This controller is not locally owned.",
                    this
                );

                return;
            }

            RequestDream();
        }
#endif

        private void DebugCycleActiveRehearsalSetServer(
            ulong clientId,
            NetPlayerState state)
        {
            // old Alpha4 server-side behavior here
        }

        private static readonly TagContainerType[] IdeaSourceCycleOrder =
        {
            TagContainerType.Resonance,
            TagContainerType.Mood,
            TagContainerType.Conviction,
            TagContainerType.Transient
        };

        private bool TryCycleTargetServer(
            NetPlayerState state,
            out string selectedTarget,
            out string failureReason)
        {
            selectedTarget = null;
            failureReason = null;

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    return TryCycleIdeaSourceTargetServer(
                        state,
                        out selectedTarget,
                        out failureReason
                    );

                case BandStance.Rehearse:
                    return TryCycleRehearsalTargetServer(
                        state,
                        out selectedTarget,
                        out failureReason
                    );

                default:
                    failureReason =
                        $"Stance {state.CurrentStanceValue} " +
                        "does not currently support target cycling.";

                    return false;
            }
        }

        private bool TryCycleIdeaSourceTargetServer(
            NetPlayerState state,
            out string selectedTarget,
            out string failureReason)
        {
            selectedTarget = null;
            failureReason = null;

            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
            {
                failureReason =
                    "The player has no authoritative GameEntity.";

                return false;
            }

            int currentIndex =
                System.Array.IndexOf(
                    IdeaSourceCycleOrder,
                    state.SelectedIdeaSourceValue
                );

            for (int offset = 1;
                 offset <= IdeaSourceCycleOrder.Length;
                 offset++)
            {
                int candidateIndex =
                    (
                        currentIndex +
                        offset +
                        IdeaSourceCycleOrder.Length
                    ) %
                    IdeaSourceCycleOrder.Length;

                TagContainerType candidate =
                    IdeaSourceCycleOrder[candidateIndex];

                if (!playerEntity.TryGetTagContainer(
                        candidate,
                        out _))
                {
                    continue;
                }

                state.SetSelectedIdeaSourceServer(candidate);

                selectedTarget =
                    $"Idea source {candidate}";

                return true;
            }

            failureReason =
                "The player has no selectable Idea-source container.";

            return false;
        }

        private bool TryCycleRehearsalTargetServer(
            NetPlayerState state,
            out string selectedTarget,
            out string failureReason)
        {
            selectedTarget = null;
            failureReason = null;

            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
            {
                failureReason =
                    "The player has no authoritative GameEntity.";

                return false;
            }

            if (!playerEntity.CycleActiveVhsSet())
            {
                failureReason =
                    "The player has no selectable rehearsal set.";

                return false;
            }

            RehearsalSet activeSet =
                playerEntity.GetActiveVhsSet();

            selectedTarget =
                activeSet != null
                    ? activeSet.DisplayName
                    : "none";

            return true;
        }

        private ActionUnavailableReason
            GetOpenTurnUnavailableReason(
                NetPlayerState state)
        {
            if (state == null)
            {
                return ActionUnavailableReason
                    .MissingPlayerState;
            }

            if (GameCoordinator.Instance == null)
            {
                return ActionUnavailableReason
                    .MissingCoordinator;
            }

            if (!state.ActiveValue)
            {
                return ActionUnavailableReason
                    .PlayerInactive;
            }

            if (state.HasCommittedTurnValue)
            {
                return ActionUnavailableReason
                    .TurnAlreadyCommitted;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetDraftUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.CurrentStanceValue ==
                BandStance.None)
            {
                return ActionUnavailableReason
                    .NoStanceSelected;
            }

            if (state.DraftedActionsValue >=
                TurnActionRules.MaximumProductiveActions)
            {
                return ActionUnavailableReason.PlanFull;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetCommitUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.CurrentStanceValue ==
                BandStance.None)
            {
                return ActionUnavailableReason
                    .NoStanceSelected;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetUndoUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.DraftedActionsValue == 0)
            {
                return ActionUnavailableReason.DraftEmpty;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetStanceChangeUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.DraftedActionsValue > 0)
            {
                return ActionUnavailableReason
                    .DraftAlreadyStarted;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetDreamUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (!state.CanDreamValue)
            {
                return ActionUnavailableReason
                    .DreamUnavailable;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetCycleTargetUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                {
                    if (IsServer &&
                        !CanCycleIdeaSourceTarget(state))
                    {
                        return ActionUnavailableReason
                            .NoSelectableTarget;
                    }

                    return ActionUnavailableReason.None;
                }

                case BandStance.Rehearse:
                {
                    if (IsServer &&
                        !CanCycleRehearsalTarget(state))
                    {
                        return ActionUnavailableReason
                            .NoSelectableTarget;
                    }

                    return ActionUnavailableReason.None;
                }

                default:
                    return ActionUnavailableReason
                        .TargetCyclingUnsupported;
            }
        }

        private static bool IsActionValidForStance(
            BandStance stance,
            DraftedActionType actionType)
        {
            return actionType switch
            {
                DraftedActionType.CreateIdea =>
                    stance == BandStance.Gestate,

                DraftedActionType
                        .DebugPlaceholderGestationSecondary =>
                    stance == BandStance.Gestate,

                DraftedActionType.RehearseActiveSet =>
                    stance == BandStance.Rehearse,

                DraftedActionType.RecordActiveSetToDemo =>
                    stance == BandStance.Rehearse,

                DraftedActionType
                        .DebugPlaceholderPromotionPrimary =>
                    stance == BandStance.Promote,

                DraftedActionType
                        .DebugPlaceholderPromotionSecondary =>
                    stance == BandStance.Promote,

                DraftedActionType.ReleaseLatestDemoToKvlt =>
                    stance == BandStance.Promote,

                _ =>
                    false
            };
        }

        private ActionUnavailableReason
            GetSpecificDraftUnavailableReason(
                NetPlayerState state,
                DraftedActionType actionType)
        {
            ActionUnavailableReason draftReason =
                GetDraftUnavailableReason(state);

            if (draftReason !=
                ActionUnavailableReason.None)
            {
                return draftReason;
            }

            if (!IsActionValidForStance(
                    state.CurrentStanceValue,
                    actionType))
            {
                return ActionUnavailableReason
                    .ActionInvalidForStance;
            }

            return ActionUnavailableReason.None;
        }

        private static ActionPlanDestination
            GetNextPlanDestination(
                NetPlayerState state)
        {
            if (state == null)
            {
                return ActionPlanDestination.None;
            }

            return state.DraftedActionsValue switch
            {
                0 => ActionPlanDestination.Standard1,
                1 => ActionPlanDestination.Standard2,
                2 => ActionPlanDestination.Overreach,
                _ => ActionPlanDestination.None
            };
        }

        private static bool TryGetDefaultDraftActionType(
            BandStance stance,
            out DraftedActionType actionType)
        {
            actionType = stance switch
            {
                BandStance.Gestate =>
                    DraftedActionType.CreateIdea,

                BandStance.Rehearse =>
                    DraftedActionType.RehearseActiveSet,

                _ =>
                    DraftedActionType.None
            };

            return actionType != DraftedActionType.None;
        }

        private static PlayerActionPresentation
            BuildNonDraftPresentation(
                PlayerCommand command,
                string label,
                ActionUnavailableReason reason,
                bool isImmediate = false)
        {
            if (reason ==
                ActionUnavailableReason.None)
            {
                return PlayerActionPresentation.Available(
                    command,
                    label,
                    isImmediate: isImmediate
                );
            }

            return PlayerActionPresentation.Blocked(
                command,
                label,
                reason
            );
        }

        private PlayerActionPresentation
            BuildDraftPresentation(
                PlayerCommand command,
                NetPlayerState state,
                DraftedActionType actionType)
        {
            string label =
                ActionPresentationText.GetActionLabel(
                    actionType
                );

            ActionUnavailableReason reason =
                GetSpecificDraftUnavailableReason(
                    state,
                    actionType
                );

            if (reason !=
                ActionUnavailableReason.None)
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    label,
                    reason,
                    actionType,
                    true
                );
            }

            return PlayerActionPresentation.Available(
                command,
                label,
                actionType,
                true,
                GetNextPlanDestination(state)
            );
        }

        private PlayerActionPresentation
            BuildDefaultDraftPresentation(
                PlayerCommand command,
                NetPlayerState state)
        {
            ActionUnavailableReason draftReason =
                GetDraftUnavailableReason(state);

            if (draftReason !=
                ActionUnavailableReason.None)
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    ActionPresentationText.GetCommandLabel(
                        command
                    ),
                    draftReason
                );
            }

            if (!TryGetDefaultDraftActionType(
                    state.CurrentStanceValue,
                    out DraftedActionType actionType))
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    ActionPresentationText.GetCommandLabel(
                        command
                    ),
                    ActionUnavailableReason
                        .NoActionAssigned
                );
            }

            return BuildDraftPresentation(
                command,
                state,
                actionType
            );
        }

        private PlayerActionPresentation
            BuildStanceSlotPresentation(
                PlayerCommand command,
                int slotIndex,
                NetPlayerState state)
        {
            string fallbackLabel =
                ActionPresentationText.GetCommandLabel(
                    command
                );

            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    fallbackLabel,
                    baseReason
                );
            }

            if (state.CurrentStanceValue ==
                BandStance.None)
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    fallbackLabel,
                    ActionUnavailableReason
                        .NoStanceSelected
                );
            }

            if (!TryGetDraftedActionTypeForSlot(
                    state.CurrentStanceValue,
                    slotIndex,
                    out DraftedActionType actionType,
                    out bool isImmediate))
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    fallbackLabel,
                    ActionUnavailableReason
                        .NoActionAssigned
                );
            }

            if (isImmediate)
            {
                string immediateLabel =
                    GetImmediateSlotLabel(
                        state.CurrentStanceValue,
                        slotIndex
                    );

                return PlayerActionPresentation.Available(
                    command,
                    immediateLabel,
                    isImmediate: true
                );
            }

            return BuildDraftPresentation(
                command,
                state,
                actionType
            );
        }


        private static string GetImmediateSlotLabel(
            BandStance stance,
            int slotIndex)
        {
            if (stance == BandStance.Promote &&
                slotIndex == 3)
            {
                return "Promotion Tertiary Placeholder";
            }

            return "Immediate Action";
        }

        public PlayerActionPresentation GetPresentation(
            PlayerCommand command)
        {
            NetPlayerState state =
                GetPlayerState();

            switch (command)
            {
                case PlayerCommand.SelectGestate:
                    return BuildNonDraftPresentation(
                        command,
                        "Select Gestate",
                        GetStanceChangeUnavailableReason(
                            state
                        )
                    );

                case PlayerCommand.SelectRehearse:
                    return BuildNonDraftPresentation(
                        command,
                        "Select Rehearse",
                        GetStanceChangeUnavailableReason(
                            state
                        )
                    );

                case PlayerCommand.SelectPromote:
                    return BuildNonDraftPresentation(
                        command,
                        "Select Promote",
                        GetStanceChangeUnavailableReason(
                            state
                        )
                    );

                case PlayerCommand.Dream:
                    return BuildNonDraftPresentation(
                        command,
                        "Dream",
                        GetDreamUnavailableReason(state)
                    );

                case PlayerCommand.DraftAction:
                    return BuildDefaultDraftPresentation(
                        command,
                        state
                    );

                case PlayerCommand.DraftPrimaryAction:
                    return BuildStanceSlotPresentation(
                        command,
                        1,
                        state
                    );

                case PlayerCommand.DraftSecondaryAction:
                    return BuildStanceSlotPresentation(
                        command,
                        2,
                        state
                    );

                case PlayerCommand.DraftTertiaryAction:
                    return BuildStanceSlotPresentation(
                        command,
                        3,
                        state
                    );

                case PlayerCommand.DraftRestAction:
                {
                    string label =
                        state != null &&
                        TurnActionRules.IsOverreach(
                            state.DraftedActionsValue
                        )
                            ? "Commit Overreach"
                            : "Finish and Recover";

                    return BuildNonDraftPresentation(
                        command,
                        label,
                        GetCommitUnavailableReason(state)
                    );
                }

                case PlayerCommand.UndoDraftAction:
                    return BuildNonDraftPresentation(
                        command,
                        "Undo Last Action",
                        GetUndoUnavailableReason(state)
                    );

                case PlayerCommand.CommitTurn:
                {
                    string label =
                        state != null &&
                        TurnActionRules.IsOverreach(
                            state.DraftedActionsValue
                        )
                            ? "Commit Overreach"
                            : "Commit Turn";

                    return BuildNonDraftPresentation(
                        command,
                        label,
                        GetCommitUnavailableReason(state)
                    );
                }

                case PlayerCommand.CycleTarget:
                {
                    string label =
                        state?.CurrentStanceValue switch
                        {
                            BandStance.Gestate =>
                                "Cycle Idea Source",

                            BandStance.Rehearse =>
                                "Cycle Rehearsal Set",

                            _ =>
                                "Cycle Target"
                        };

                    return BuildNonDraftPresentation(
                        command,
                        label,
                        GetCycleTargetUnavailableReason(
                            state
                        ),
                        true
                    );
                }

                case PlayerCommand.ForceStartSession:
                {
                    ActionUnavailableReason reason =
                        GetForceStartUnavailableReason();

                    return BuildNonDraftPresentation(
                        command,
                        "Force Start Session",
                        reason,
                        true
                    );
                }

                case PlayerCommand.QuitSession:
                    return PlayerActionPresentation.Available(
                        command,
                        "Quit Session",
                        isImmediate: true
                    );

                case PlayerCommand
                    .AdminCreateEmptyRehearsalSet:
                    return PlayerActionPresentation.Available(
                        command,
                        "Create Empty Rehearsal Set (Debug)",
                        isImmediate: true
                    );

                default:
                    return PlayerActionPresentation.Blocked(
                        command,
                        ActionPresentationText.GetCommandLabel(
                            command
                        ),
                        ActionUnavailableReason
                            .NoActionAssigned
                    );
            }
        }
        
        private ActionUnavailableReason
            GetForceStartUnavailableReason()
        {
            NetworkManager manager =
                NetworkManager.Singleton;

            if (manager == null)
            {
                return ActionUnavailableReason
                    .MissingNetworkManager;
            }

            if (!manager.IsHost)
            {
                return ActionUnavailableReason.HostOnly;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                return ActionUnavailableReason
                    .MissingCoordinator;
            }

            if (coordinator.IsPlayableSessionStarted)
            {
                return ActionUnavailableReason
                    .SessionAlreadyStarted;
            }

            return ActionUnavailableReason.None;
        }
        
        
    }
}