using SEMM91;
using SEMM91.Core.Entities;
using SEMM91.Core.Recordings;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using SEMM91.GamePlay;
using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Keeper;
using SEMM91.GamePlay.Kvlt.Transgression;
using SEMM91.GamePlay.Kvlt.Paradigm;
using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.GamePlay.World;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

#if UNITY_EDITOR
using SEMM91.GamePlay.Rehearsal.Development;
#endif

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

        [SerializeField] private string debugRehearsalSetId;
        [SerializeField] private string debugDemoTapeId;


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

        public void RequestKeeperBoostVisibility(
            string releaseId,
            float pullSpend)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitKeeperBoostVisibilityServerRpc(
                new FixedString64Bytes(
                    releaseId ?? string.Empty
                ),
                pullSpend
            );
        }

        public void RequestKeeperSuppressVisibility(
            string releaseId,
            float pullSpend)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitKeeperSuppressVisibilityServerRpc(
                new FixedString64Bytes(
                    releaseId ?? string.Empty
                ),
                pullSpend
            );
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

        public void RequestAllegianceVote(
            AllegianceChoice choice)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitAllegianceVoteServerRpc(
                choice
            );
        }

        public void RequestHail(string hailAspectId)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitHailServerRpc(
                new FixedString64Bytes(
                    hailAspectId ?? string.Empty));
        }

        public void RequestCycleTarget()
        {
            if (!IsOwner || !IsClient) return;

            SubmitCycleTargetServerRpc();
        }

        public void RequestSelectRehearsalSet(
            string vhsSetId)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitSelectRehearsalSetServerRpc(
                new FixedString64Bytes(
                    vhsSetId ?? string.Empty
                )
            );
        }

        public void RequestDraftDemoRelease(
            string demoTapeId)
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitDraftDemoReleaseServerRpc(
                new FixedString64Bytes(
                    demoTapeId ?? string.Empty
                )
            );
        }

        public void RequestForceStartSession()
        {
            if (!IsOwner || !IsClient)
                return;

            NetPlayerState state =
                GetPlayerState();

            const PlayerCommand command =
                PlayerCommand.ForceStartSession;

            ActionUnavailableReason unavailableReason =
                GetForceStartUnavailableReason();

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                if (IsServer)
                {
                    string reasonText =
                        ActionPresentationText.GetReasonText(
                            unavailableReason
                        );

                    RejectCommand(
                        OwnerClientId,
                        state,
                        command,
                        reasonText
                    );
                }

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    OwnerClientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
                );

                return;
            }

            bool success =
                coordinator.ForceStartPlayableSessionServer();

            if (!success)
            {
                RejectCommand(
                    OwnerClientId,
                    state,
                    command,
                    "Host override was rejected."
                );

                return;
            }

            AcceptCommand(
                OwnerClientId,
                state,
                command,
                "Playable session started by host override."
            );
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
                case PlayerCommand.AddIdeaToCurrentTrack:
                    RequestAddIdeaToCurrentTrack();
                    break;

                case PlayerCommand.Dream:
                    RequestDream();
                    break;

                case PlayerCommand.KeeperBoostVisibility:
                    RequestDefaultKeeperVisibilityIntervention(
                        boost: true);
                    break;

                case PlayerCommand.KeeperSuppressVisibility:
                    RequestDefaultKeeperVisibilityIntervention(
                        boost: false);
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

                case PlayerCommand.ReturnToStanceSelection:
                    RequestSelectStance(BandStance.None);
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

                case PlayerCommand.CreateEmptyRehearsalSet:
                    RequestAdminCreateEmptyRehearsalSet();
                    break;

                case PlayerCommand.CreateNewTrack:
                    RequestCreateNewTrack();
                    break;

                case PlayerCommand.ContextualCreate:
                    RequestContextualCreate();
                    break;

                case PlayerCommand.HailSatan:
                    RequestHail(
                        Peak2KvltParadigmCatalog.SatanAspectId);
                    break;

                case PlayerCommand.HailOdin:
                    RequestHail(
                        Peak2KvltParadigmCatalog.OdinAspectId);
                    break;

                case PlayerCommand.VoteSociety:
                    RequestAllegianceVote(
                        AllegianceChoice.Society);
                    break;

                case PlayerCommand.VoteKvlt:
                    RequestAllegianceVote(
                        AllegianceChoice.Kvlt);
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

        private void RequestAddIdeaToCurrentTrack()
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitAddIdeaToCurrentTrackServerRpc();
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

        private void RequestContextualCreate()
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitContextualCreateServerRpc();
        }

        private void RequestCreateNewTrack()
        {
            if (!IsOwner || !IsClient)
                return;

            SubmitCreateNewTrackServerRpc();
        }

        private void RequestDefaultKeeperVisibilityIntervention(
            bool boost)
        {
            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null)
                return;

            foreach (var row in snapshot.KeeperReleaseRows)
            {
                bool available = boost
                    ? row.CanReceiveBoost
                    : row.CanReceiveSuppress;

                if (!available)
                    continue;

                if (boost)
                {
                    RequestKeeperBoostVisibility(
                        row.ReleaseId.ToString(),
                        1f);
                }
                else
                {
                    RequestKeeperSuppressVisibility(
                        row.ReleaseId.ToString(),
                        1f);
                }

                return;
            }
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
            if (state == null)
                return false;

            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
                return false;

            foreach (TagContainerType sourceType
                     in IdeaSourceCycleOrder)
            {
                if (sourceType ==
                    state.SelectedIdeaSourceValue)
                {
                    continue;
                }

                if (playerEntity.TryGetTagContainer(
                        sourceType,
                        out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetContextualCreateLabel(
            NetPlayerState state)
        {
            return state?.CurrentStanceValue switch
            {
                BandStance.Gestate =>
                    "Create New Idea",

                BandStance.Rehearse =>
                    "Create New Track",

                BandStance.Promote =>
                    "Create New Happening",

                _ =>
                    "Create"
            };
        }

        private static bool CanCycleRehearsalTarget(
            NetPlayerState state)
        {
            if (state == null)
                return false;

            GameEntity playerEntity =
                state.PlayerEntity;

            return
                playerEntity != null &&
                playerEntity.CanCycleActiveVhsSet();
        }

        [ServerRpc]
        private void SubmitDraftActionServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.DraftAction;

            ActionUnavailableReason unavailableReason =
                GetDraftUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            DraftedActionPayload payload =
                CreatePayloadForCurrentStance(state);

            if (payload == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "No default action is assigned to the selected stance."
                );

                return;
            }

            if (!state.TryAddDraftedActionServer(payload))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The action could not be added to the draft."
                );

                return;
            }

            Debug.Log(
                $"[DRAFT PAYLOAD] " +
                $"action={payload.ActionType} " +
                $"ideaSource={payload.IdeaSourceContainerType?.ToString() ?? "none"} " +
                $"physicalEventNode={(payload.HasPhysicalEventLocation ? payload.PhysicalEventNodeId : "none")} " +
                $"turn={payload.CreatedTurn}",
                this
            );
            ActionPlanDestination destination =
                (ActionPlanDestination)
                state.DraftedActionsValue;

            string destinationLabel =
                ActionPresentationText.GetDestinationLabel(
                    destination
                );

            string actionLabel =
                ActionPresentationText.GetActionLabel(
                    payload.ActionType
                );

            AcceptCommand(
                clientId,
                state,
                command,
                $"{actionLabel} drafted into {destinationLabel}."
            );
        }

        [ServerRpc]
        private void SubmitUndoDraftActionServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.UndoDraftAction;

            ActionUnavailableReason unavailableReason =
                GetUndoUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                string reasonText =
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    );

                RejectCommand(
                    clientId,
                    state,
                    command,
                    reasonText
                );

                return;
            }

            if (!state.TryRemoveLastDraftedActionServer(
                    out DraftedActionPayload removedPayload))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The draft could not be updated."
                );

                return;
            }

            string actionLabel =
                ActionPresentationText.GetActionLabel(
                    removedPayload.ActionType
                );

            AcceptCommand(
                clientId,
                state,
                command,
                $"{actionLabel} removed from the plan."
            );
        }

        [ServerRpc]
        private void SubmitStanceServerRpc(
            BandStance stance,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            PlayerCommand command =
                GetStanceCommand(stance);

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            ActionUnavailableReason unavailableReason =
                GetStanceChangeUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                string reasonText =
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    );

                RejectCommand(
                    clientId,
                    state,
                    command,
                    reasonText
                );

                return;
            }

            state.SetCurrentStanceServer(stance);

            RefreshContextTargetSummaryServer();

            string feedbackMessage =
                stance == BandStance.None
                    ? "Returned to stance selection."
                    : $"Stance changed to {stance}.";

            AcceptCommand(
                clientId,
                state,
                command,
                feedbackMessage
            );
        }

        [ServerRpc]
        private void SubmitCommitTurnServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.CommitTurn;

            ActionUnavailableReason unavailableReason =
                GetCommitUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                string reasonText =
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    );

                RejectCommand(
                    clientId,
                    state,
                    command,
                    reasonText
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
                );

                return;
            }

            if (!coordinator.CanClientAct(clientId))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The server no longer considers this player eligible to act."
                );

                return;
            }

            bool isKeeperTurn =
                coordinator.IsClientCurrentKeeper(
                    clientId
                );

            bool isOverreach =
                TurnActionRules.IsOverreach(
                    state.DraftedActionsValue
                );

            Debug.Log(
                $"[COMMIT REQUEST] client={clientId} " +
                $"stance={state.CurrentStanceValue} " +
                $"drafted={state.DraftedActionsValue}"
            );

            CommitDraftToState(
                clientId,
                state
            );

            Debug.Log(
                $"[COMMIT AFTER DRAFT TRANSFER] " +
                $"client={clientId} " +
                $"committed={state.CommittedActionsValue} " +
                $"payloads={state.CommittedActionPayloads.Count}"
            );

            Debug.Log(
                $"[COMMIT BEFORE COMPLETE TURN] client={clientId}"
            );

            coordinator.CompleteCommittedTurn(
                clientId,
                state
            );

            RefreshContextTargetSummaryServer();

            string feedbackMessage;

            if (isKeeperTurn)
            {
                feedbackMessage =
                    "Keeper turn committed.";
            }
            else
            {
                feedbackMessage =
                    isOverreach
                        ? "Overreach committed. The acting entity is Exhausted."
                        : "Turn committed with recovery retained.";
            }

            AcceptCommand(
                clientId,
                state,
                command,
                feedbackMessage
            );
        }

        [ServerRpc]
        private void SubmitAllegianceVoteServerRpc(
            AllegianceChoice choice,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                Debug.LogWarning(
                    "[ALLEGIANCE VOTE REJECTED] " +
                    $"client={clientId} | " +
                    "coordinator unavailable"
                );

                return;
            }

            if (!coordinator
                    .TryCastNextPeak2AllegianceVoteServer(
                        clientId,
                        choice,
                        out string questionId,
                        out string failureReason
                    ))
            {
                Debug.LogWarning(
                    "[ALLEGIANCE VOTE REJECTED] " +
                    $"client={clientId} | " +
                    $"{failureReason}"
                );

                return;
            }

            Debug.Log(
                "[ALLEGIANCE VOTE ACCEPTED] " +
                $"client={clientId} | " +
                $"question={questionId} | " +
                $"choice={choice}"
            );
        }

        [ServerRpc]
        private void SubmitHailServerRpc(
            FixedString64Bytes hailAspectId,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;
            NetPlayerState state = GetPlayerState();
            string aspectId = hailAspectId.ToString();
            PlayerCommand command =
                aspectId ==
                    Peak2KvltParadigmCatalog.SatanAspectId
                    ? PlayerCommand.HailSatan
                    : PlayerCommand.HailOdin;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            string happeningId = string.Empty;
            string failureReason = string.Empty;

            if (coordinator == null ||
                !coordinator.TryHailNextPeak2HappeningServer(
                    clientId,
                    aspectId,
                    out happeningId,
                    out failureReason))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    coordinator == null
                        ? "The gameplay coordinator is unavailable."
                        : failureReason);
                return;
            }

            AcceptCommand(
                clientId,
                state,
                command,
                $"{aspectId} Hail recorded in {happeningId}.");
        }

        [ServerRpc]
        private void SubmitDreamServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.Dream;

            Debug.Log(
                $"[DREAM RPC] client={clientId}",
                this
            );

            ActionUnavailableReason unavailableReason =
                GetDreamUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                string reasonText =
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    );

                RejectCommand(
                    clientId,
                    state,
                    command,
                    reasonText
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
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
                RejectCommand(
                    clientId,
                    state,
                    command,
                    failureReason
                );

                return;
            }

            AcceptCommand(
                clientId,
                state,
                command,
                "Dream resolved."
            );
        }

        [ServerRpc]
        private void
            SubmitKeeperBoostVisibilityServerRpc(
                FixedString64Bytes releaseId,
                float pullSpend,
                ServerRpcParams rpcParams = default)
        {
            ResolveKeeperInterventionRequestServer(
                releaseId,
                pullSpend,
                KeeperInterventionType
                    .BoostVisibility,
                PlayerCommand
                    .KeeperBoostVisibility,
                rpcParams
            );
        }

        [ServerRpc]
        private void
            SubmitKeeperSuppressVisibilityServerRpc(
                FixedString64Bytes releaseId,
                float pullSpend,
                ServerRpcParams rpcParams = default)
        {
            ResolveKeeperInterventionRequestServer(
                releaseId,
                pullSpend,
                KeeperInterventionType
                    .SuppressVisibility,
                PlayerCommand
                    .KeeperSuppressVisibility,
                rpcParams
            );
        }

        private void ResolveKeeperInterventionRequestServer(
            FixedString64Bytes releaseId,
            float pullSpend,
            KeeperInterventionType interventionType,
            PlayerCommand command,
            ServerRpcParams rpcParams)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            if (!state.ActiveValue)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The player is not active in the current session."
                );

                return;
            }

            if (state.HasCommittedTurnValue)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The turn has already been committed."
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
                );

                return;
            }

            /*
             * Keeper identity and turn are derived here on the
             * authoritative server. Neither is trusted from the client.
             */
            KeeperInterventionRequest request =
                new KeeperInterventionRequest(
                    keeperClientId:
                    clientId,
                    releaseId:
                    releaseId.ToString(),
                    interventionType:
                    interventionType,
                    requestedTurn:
                    coordinator.globalTurn.Value,
                    pullSpend:
                    pullSpend
                );

            bool success =
                coordinator
                    .TryResolveKeeperInterventionServer(
                        request,
                        out KeeperInterventionResult result,
                        out string failureReason
                    );

            if (!success)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    failureReason
                );

                return;
            }

            AcceptCommand(
                clientId,
                state,
                command,
                $"{ActionPresentationText.GetCommandLabel(command)} " +
                $"resolved. Pull remaining: " +
                $"{result.PullRemaining:F2}."
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

            const PlayerCommand command =
                PlayerCommand.CycleTarget;

            ActionUnavailableReason reason =
                GetCycleTargetUnavailableReason(state);

            if (reason != ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(reason)
                );

                return;
            }

            if (!TryCycleTargetServer(
                    state,
                    out string selectedTarget,
                    out string failureReason))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    failureReason
                );

                return;
            }

            RefreshContextTargetSummaryServer();

            AcceptCommand(
                clientId,
                state,
                command,
                $"Selected {selectedTarget}."
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

        [ServerRpc]
        private void SubmitSelectRehearsalSetServerRpc(
            FixedString64Bytes vhsSetId,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            /*
             * Reuse the existing target-selection feedback channel
             * during this quick implementation pass.
             */
            const PlayerCommand command =
                PlayerCommand.CycleTarget;

            ActionUnavailableReason unavailableReason =
                GetRegularBandActionUnavailableReason(
                    state
                );

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            if (state.CurrentStanceValue !=
                BandStance.Rehearse)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "VHS sets can only be selected while rehearsing."
                );

                return;
            }

            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The player has no authoritative GameEntity."
                );

                return;
            }

            string requestedSetId =
                vhsSetId.ToString();

            if (!playerEntity.TrySetActiveVhsSetById(
                    requestedSetId,
                    out RehearsalSet selectedSet
                ))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    $"VHS set {requestedSetId} was not found."
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
                );

                return;
            }

            coordinator.PublishDomainProjectionServer(
                $"active rehearsal set selected | " +
                $"client={clientId} | " +
                $"set={selectedSet.VhsSetId}"
            );

            RefreshContextTargetSummaryServer();

            Debug.Log(
                "[MEDIA SHELF VHS SELECTED] " +
                $"client={clientId} | " +
                $"setId={selectedSet.VhsSetId} | " +
                $"setName={selectedSet.DisplayName}",
                this
            );

            AcceptCommand(
                clientId,
                state,
                command,
                $"Selected rehearsal set {selectedSet.DisplayName}."
            );
        }

        [ServerRpc]
        private void SubmitDraftDemoReleaseServerRpc(
            FixedString64Bytes demoTapeId,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.DraftPrimaryAction;

            ActionUnavailableReason reason =
                GetSpecificDraftUnavailableReason(
                    state,
                    DraftedActionType.ReleaseDemoTape
                );

            if (reason != ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(reason)
                );

                return;
            }

            GameEntity playerEntity =
                state.PlayerEntity;

            if (playerEntity == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The player has no authoritative GameEntity."
                );

                return;
            }

            string requestedDemoTapeId =
                demoTapeId.ToString();

            if (!playerEntity.TryGetDemoTapeById(
                    requestedDemoTapeId,
                    out DemoTape demoTape
                ))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The selected demo tape does not belong " +
                    "to this player."
                );

                return;
            }

            if (demoTape.SceneState !=
                DemoTapeSceneState.Unreleased)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    $"Demo '{demoTape.DisplayName}' is already released."
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null ||
                !coordinator.TryGetEntityPhysicalNode(
                    playerEntity,
                    out PhysicalMapNode eventNode
                ))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The acting player has no valid " +
                    "Promotion event location."
                );

                return;
            }

            DraftedActionPayload payload =
                DraftedActionPayload
                    .CreateSelectedDemoRelease(
                        coordinator.globalTurn.Value,
                        eventNode.NodeId,
                        demoTape.DemoTapeId
                    );

            if (!state.TryAddDraftedActionServer(payload))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The cassette release could not be " +
                    "added to the action plan."
                );

                return;
            }

            ActionPlanDestination destination =
                (ActionPlanDestination)
                state.DraftedActionsValue;

            Debug.Log(
                "[MEDIA SHELF DEMO RELEASE DRAFTED] " +
                $"client={clientId} | " +
                $"demoId={demoTape.DemoTapeId} | " +
                $"demo={demoTape.DisplayName} | " +
                $"node={eventNode.NodeId} | " +
                $"position={state.DraftedActionsValue}",
                this
            );

            AcceptCommand(
                clientId,
                state,
                command,
                $"Release of {demoTape.DisplayName} drafted into " +
                $"{ActionPresentationText.GetDestinationLabel(destination)}."
            );
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

        private static DraftedActionPayload
            CreatePayloadForAction(
                NetPlayerState state,
                DraftedActionType actionType,
                int currentTurn)
        {
            if (actionType ==
                DraftedActionType.CreateIdea)
            {
                return new DraftedActionPayload(
                    actionType,
                    currentTurn,
                    state.SelectedIdeaSourceValue
                );
            }

            if (actionType ==
                DraftedActionType
                    .ReleaseLatestDemoToKvlt)
            {
                GameEntity playerEntity =
                    state.PlayerEntity;

                GameCoordinator coordinator =
                    GameCoordinator.Instance;

                if (coordinator == null ||
                    !coordinator.TryGetEntityPhysicalNode(
                        playerEntity,
                        out PhysicalMapNode eventNode
                    ))
                {
                    Debug.LogError(
                        "[PROMOTION DRAFT] Cannot capture " +
                        "the acting entity's physical location | " +
                        $"entity=" +
                        $"{playerEntity?.EntityId ?? "null"}"
                    );

                    return null;
                }

                return DraftedActionPayload
                    .CreatePhysicalPromotionEvent(
                        currentTurn,
                        eventNode.NodeId
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

#if UNITY_EDITOR

        [ContextMenu("Debug/Seed Peak 1 Ideas")]
        public void RequestDevelopmentSeedPeak1Ideas()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[TRACK BUILD] Enter Play Mode first.",
                    this
                );

                return;
            }

            if (!IsClient || !IsOwner)
            {
                Debug.LogWarning(
                    "[TRACK BUILD] Idea seed request rejected: " +
                    "controller is not locally owned.",
                    this
                );

                return;
            }

            SubmitDevelopmentSeedPeak1IdeasServerRpc();
        }

        [ServerRpc]
        private void SubmitDevelopmentSeedPeak1IdeasServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            if (state == null ||
                state.PlayerEntity == null)
            {
                Debug.LogWarning(
                    "[TRACK BUILD] Idea seed rejected: " +
                    "authoritative player entity is unavailable.",
                    this
                );

                return;
            }

            if (state.CurrentStanceValue !=
                BandStance.Rehearse)
            {
                Debug.LogWarning(
                    "[TRACK BUILD] Idea seed rejected: " +
                    "player is not in Rehearse stance.",
                    this
                );

                return;
            }

            bool success =
                Peak1DevelopmentIdeaSeeder.Seed(
                    state.PlayerEntity,
                    out string message
                );

            if (!success)
            {
                Debug.LogWarning(
                    $"[TRACK BUILD] {message}",
                    this
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            coordinator?.PublishDomainProjectionServer(
                $"Peak 1 Ideas seeded | client={clientId}"
            );

            Debug.Log(
                $"[TRACK BUILD] client={clientId} | " +
                message,
                this
            );
        }

#endif

        [ServerRpc]
        private void SubmitDraftStanceSlotActionServerRpc(
            int slotIndex,
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            Debug.Log(
                $"[SLOT REQUEST] client={clientId} slot={slotIndex}"
            );

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            if (!TryGetSlotCommand(
                    slotIndex,
                    out PlayerCommand command))
            {
                LogRejected(
                    $"Draft slot rejected for client {clientId}: " +
                    $"invalid slot index {slotIndex}."
                );

                return;
            }

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            if (!TryGetDraftedActionTypeForSlot(
                    state.CurrentStanceValue,
                    slotIndex,
                    out DraftedActionType actionType,
                    out bool isImmediate))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    $"No action is assigned to slot {slotIndex} " +
                    $"for stance {state.CurrentStanceValue}."
                );

                return;
            }

            Debug.Log(
                $"[SLOT RESOLVED] client={clientId} " +
                $"stance={state.CurrentStanceValue} " +
                $"slot={slotIndex} " +
                $"actionType={actionType} " +
                $"immediate={isImmediate}"
            );

            if (isImmediate)
            {
                ActionUnavailableReason unavailableReason =
                    GetRegularBandActionUnavailableReason(
                        state
                    );

                if (unavailableReason !=
                    ActionUnavailableReason.None)
                {
                    string reasonText =
                        ActionPresentationText.GetReasonText(
                            unavailableReason
                        );

                    RejectCommand(
                        clientId,
                        state,
                        command,
                        reasonText
                    );

                    return;
                }

                if (!TryResolveImmediateSlotAction(
                        clientId,
                        state,
                        actionType,
                        out string immediateMessage))
                {
                    RejectCommand(
                        clientId,
                        state,
                        command,
                        immediateMessage
                    );

                    return;
                }

                AcceptCommand(
                    clientId,
                    state,
                    command,
                    immediateMessage
                );

                return;
            }

            ActionUnavailableReason draftReason =
                GetSpecificDraftUnavailableReason(
                    state,
                    actionType
                );

            if (draftReason !=
                ActionUnavailableReason.None)
            {
                string reasonText =
                    ActionPresentationText.GetReasonText(
                        draftReason
                    );

                RejectCommand(
                    clientId,
                    state,
                    command,
                    reasonText
                );

                return;
            }

            if (!DraftSpecificAction(
                    state,
                    actionType))
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The action could not be added to the draft."
                );

                return;
            }

            ActionPlanDestination destination =
                (ActionPlanDestination)
                state.DraftedActionsValue;

            string destinationLabel =
                ActionPresentationText.GetDestinationLabel(
                    destination
                );

            string actionLabel =
                ActionPresentationText.GetActionLabel(
                    actionType
                );

            AcceptCommand(
                clientId,
                state,
                command,
                $"{actionLabel} drafted into {destinationLabel}."
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
                $"ideaSource={payload.IdeaSourceContainerType?.ToString() ?? "none"} " +
                $"physicalEventNode={(payload.HasPhysicalEventLocation ? payload.PhysicalEventNodeId : "none")} " +
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

        private bool TryResolveImmediateSlotAction(
            ulong clientId,
            NetPlayerState state,
            DraftedActionType actionType,
            out string message)
        {
            message = string.Empty;

            if (state == null)
            {
                message = "Player state is unavailable.";
                return false;
            }

            if (state.CurrentStanceValue !=
                BandStance.Promote)
            {
                message =
                    $"Stance {state.CurrentStanceValue} " +
                    "has no implemented immediate action.";

                return false;
            }

            if (actionType != DraftedActionType.None)
            {
                message =
                    "The Promote immediate slot received an " +
                    "unexpected drafted action type.";

                return false;
            }

            message =
                "Promotion tertiary placeholder resolved.";

            return true;
        }

        [ServerRpc]
        private void SubmitAdminCreateEmptyRehearsalSetServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetComponent<NetPlayerState>();

            const PlayerCommand command =
                PlayerCommand.CreateEmptyRehearsalSet;

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            ActionUnavailableReason unavailableReason =
                GetRegularBandActionUnavailableReason(
                    state
                );

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            if (state.PlayerEntity == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The player has no acting entity."
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The gameplay coordinator is unavailable."
                );

                return;
            }

            if (coordinator.RehearsalResolver == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The rehearsal resolver is unavailable."
                );

                return;
            }

            bool success =
                coordinator.RehearsalResolver
                    .TryCreateNewActiveEmptyVhsSet(
                        clientId,
                        state.PlayerEntity,
                        out string message
                    );

            if (!success)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    message
                );

                return;
            }

            coordinator.PublishDomainProjectionServer(
                $"empty rehearsal set created | " +
                $"client={clientId}"
            );

            RefreshContextTargetSummaryServer();

            AcceptCommand(
                clientId,
                state,
                command,
                message
            );
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

#if UNITY_EDITOR
        [ContextMenu(
            "Debug/Request Select Rehearsal Set"
        )]
        private void
            DebugRequestSelectRehearsalSet()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] Enter Play Mode first.",
                    this
                );

                return;
            }

            if (!IsClient || !IsOwner)
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] Controller is not " +
                    "the locally owned client controller.",
                    this
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    debugRehearsalSetId
                ))
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] Enter a VHS set ID.",
                    this
                );

                return;
            }

            RequestSelectRehearsalSet(
                debugRehearsalSetId
            );
        }
#endif

#if UNITY_EDITOR
        [ContextMenu(
            "Debug/Request Draft Selected Demo Release"
        )]
        private void
            DebugRequestDraftSelectedDemoRelease()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] Enter Play Mode first.",
                    this
                );

                return;
            }

            if (!IsClient || !IsOwner)
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] This is not the " +
                    "locally owned controller.",
                    this
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(debugDemoTapeId))
            {
                Debug.LogWarning(
                    "[MEDIA SHELF DEBUG] Enter an " +
                    "unreleased DemoTapeId.",
                    this
                );

                return;
            }

            RequestDraftDemoRelease(debugDemoTapeId);
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

                if (candidate == state.SelectedIdeaSourceValue)
                {
                    continue;
                }

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

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
            {
                failureReason =
                    "The gameplay coordinator is unavailable.";

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

            coordinator.PublishDomainProjectionServer(
                $"active rehearsal set cycled | " +
                $"client={state.OwnerClientId}"
            );

            return true;
        }

        private ActionUnavailableReason
            GetContextualCreateUnavailableReason(
                NetPlayerState state)
        {
            if (state == null)
            {
                return ActionUnavailableReason
                    .MissingPlayerState;
            }

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                    return GetSpecificDraftUnavailableReason(
                        state,
                        DraftedActionType.CreateIdea
                    );

                case BandStance.Rehearse:
                {
                    ActionUnavailableReason baseReason =
                        GetRegularBandActionUnavailableReason(
                            state
                        );

                    if (baseReason !=
                        ActionUnavailableReason.None)
                    {
                        return baseReason;
                    }

                    if (IsServer)
                    {
                        if (state.PlayerEntity == null)
                        {
                            return ActionUnavailableReason
                                .MissingActingEntity;
                        }

                        RehearsalSet activeSet =
                            state.PlayerEntity.GetActiveVhsSet();

                        if (activeSet == null)
                        {
                            return ActionUnavailableReason
                                .MissingActiveRehearsalSet;
                        }

                        return activeSet.HasEmptyTrack
                            ? ActionUnavailableReason
                                .EmptyTrackAlreadyExists
                            : ActionUnavailableReason.None;
                    }

                    PlayerContextTargetSummary summary =
                        state.ContextTargetSummaryValue;

                    bool hasActiveSet =
                        summary.Kind ==
                        PlayerContextTargetKind.RehearsalSet &&
                        summary.HasTarget;

                    if (!hasActiveSet)
                    {
                        return ActionUnavailableReason
                            .MissingActiveRehearsalSet;
                    }

                    return summary.HasEmptyTrack
                        ? ActionUnavailableReason
                            .EmptyTrackAlreadyExists
                        : ActionUnavailableReason.None;
                }

                case BandStance.Promote:
                {
                    ActionUnavailableReason baseReason =
                        GetRegularBandActionUnavailableReason(state);

                    if (baseReason !=
                        ActionUnavailableReason.None)
                        return baseReason;

                    if (IsServer)
                    {
                        GameCoordinator coordinator =
                            GameCoordinator.Instance;

                        if (coordinator == null)
                            return ActionUnavailableReason
                                .MissingCoordinator;

                        return coordinator
                            .CanCreatePeak2HappeningServer(
                                state.OwnerClientId,
                                out _)
                            ? ActionUnavailableReason.None
                            : ActionUnavailableReason
                                .ContextualCreationUnavailable;
                    }

                    return ActionUnavailableReason.None;
                }

                case BandStance.None:
                default:
                    return ActionUnavailableReason
                        .NoStanceSelected;
            }
        }

        private ActionUnavailableReason
            GetAddIdeaToCurrentTrackUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetRegularBandActionUnavailableReason(
                    state
                );

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.CurrentStanceValue !=
                BandStance.Rehearse)
            {
                return ActionUnavailableReason
                    .ActionInvalidForStance;
            }

            /*
             * PlayerEntity is server-only. A remote client can verify
             * the replicated active-set summary, while the ServerRpc
             * performs the complete authoritative check below.
             */
            if (!IsServer)
            {
                PlayerContextTargetSummary summary =
                    state.ContextTargetSummaryValue;

                bool hasActiveSet =
                    summary.Kind ==
                    PlayerContextTargetKind.RehearsalSet &&
                    summary.HasTarget;

                return hasActiveSet
                    ? ActionUnavailableReason.None
                    : ActionUnavailableReason
                        .MissingActiveRehearsalSet;
            }

            if (state.PlayerEntity == null)
            {
                return ActionUnavailableReason
                    .MissingActingEntity;
            }

            var activeSet =
                state.PlayerEntity.GetActiveVhsSet();

            if (activeSet == null)
            {
                return ActionUnavailableReason
                    .MissingActiveRehearsalSet;
            }

            if (activeSet.GetLatestVhsTrack() == null)
            {
                return ActionUnavailableReason
                    .MissingCurrentTrack;
            }

            foreach (var idea in state.PlayerEntity.Ideas)
            {
                if (idea != null)
                {
                    return ActionUnavailableReason.None;
                }
            }

            return ActionUnavailableReason
                .NoAvailableIdeas;
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

        private bool IsCurrentKeeper(
            NetPlayerState state)
        {
            if (state == null)
                return false;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            return coordinator != null &&
                   coordinator.IsClientCurrentKeeper(
                       state.OwnerClientId
                   );
        }

        private ActionUnavailableReason
            GetRegularBandActionUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetOpenTurnUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (IsCurrentKeeper(state))
            {
                return ActionUnavailableReason
                    .KeeperRoleRestricted;
            }

            return ActionUnavailableReason.None;
        }

        private ActionUnavailableReason
            GetDraftUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetRegularBandActionUnavailableReason(
                    state
                );

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

            if (IsCurrentKeeper(state))
            {
                /*
                 * Keeper actions will eventually produce their own
                 * committed payloads. Until that pipeline exists, an
                 * empty commit allows the Keeper to complete the
                 * lockstep turn.
                 *
                 * A regular band draft should never survive Keeper
                 * entry. Treat one as invalid state rather than
                 * resolving dormant-band production.
                 */
                if (state.DraftedActionsValue > 0)
                {
                    return ActionUnavailableReason
                        .KeeperRoleRestricted;
                }

                return ActionUnavailableReason.None;
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
                GetRegularBandActionUnavailableReason(
                    state
                );

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
                GetRegularBandActionUnavailableReason(
                    state
                );

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
            GetReturnToStanceSelectionUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason reason =
                GetStanceChangeUnavailableReason(
                    state
                );

            if (reason !=
                ActionUnavailableReason.None)
            {
                return reason;
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
            GetDreamUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetRegularBandActionUnavailableReason(
                    state
                );

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
                GetRegularBandActionUnavailableReason(
                    state
                );

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                {
                    bool canCycle =
                        IsServer
                            ? CanCycleIdeaSourceTarget(state)
                            : state.ContextTargetSummaryValue.CanCycle;

                    if (!canCycle)
                    {
                        return ActionUnavailableReason
                            .NoSelectableTarget;
                    }

                    return ActionUnavailableReason.None;
                }

                case BandStance.Rehearse:
                {
                    bool canCycle =
                        IsServer
                            ? CanCycleRehearsalTarget(state)
                            : state.ContextTargetSummaryValue.CanCycle;

                    if (!canCycle)
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

        private ActionUnavailableReason
            GetCreateNewTrackUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetRegularBandActionUnavailableReason(state);

            if (baseReason !=
                ActionUnavailableReason.None)
            {
                return baseReason;
            }

            if (state.CurrentStanceValue !=
                BandStance.Rehearse)
            {
                return ActionUnavailableReason
                    .ActionInvalidForStance;
            }

            PlayerContextTargetSummary target =
                state.ContextTargetSummaryValue;

            if (target.Kind !=
                PlayerContextTargetKind.RehearsalSet ||
                !target.HasTarget)
            {
                return ActionUnavailableReason
                    .MissingActiveRehearsalSet;
            }

            return ActionUnavailableReason.None;
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

                DraftedActionType.ReleaseDemoTape =>
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
            BuildContextualCreatePresentation(
                PlayerCommand command,
                NetPlayerState state)
        {
            string label =
                GetContextualCreateLabel(state);

            ActionUnavailableReason reason =
                GetContextualCreateUnavailableReason(
                    state
                );

            if (state?.CurrentStanceValue ==
                BandStance.Gestate)
            {
                if (reason !=
                    ActionUnavailableReason.None)
                {
                    return PlayerActionPresentation.Blocked(
                        command,
                        label,
                        reason,
                        DraftedActionType.CreateIdea,
                        hasActionType: true
                    );
                }

                return PlayerActionPresentation.Available(
                    command,
                    label,
                    DraftedActionType.CreateIdea,
                    hasActionType: true,
                    destination:
                    GetNextPlanDestination(state)
                );
            }

            if (reason !=
                ActionUnavailableReason.None)
            {
                return PlayerActionPresentation.Blocked(
                    command,
                    label,
                    reason
                );
            }

            return PlayerActionPresentation.Available(
                command,
                label,
                isImmediate: true
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
                GetRegularBandActionUnavailableReason(
                    state
                );

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

                case PlayerCommand.AddIdeaToCurrentTrack:
                    return BuildNonDraftPresentation(
                        command,
                        "Add Idea to Current Track",
                        GetAddIdeaToCurrentTrackUnavailableReason(
                            state
                        ),
                        isImmediate: true
                    );

                case PlayerCommand.ContextualCreate:
                    return BuildContextualCreatePresentation(
                        command,
                        state
                    );

                case PlayerCommand.HailSatan:
                    return BuildNonDraftPresentation(
                        command,
                        "Hail Satan",
                        GetHailUnavailableReason(state),
                        isImmediate: true);

                case PlayerCommand.HailOdin:
                    return BuildNonDraftPresentation(
                        command,
                        "Hail Odin",
                        GetHailUnavailableReason(state),
                        isImmediate: true);

                case PlayerCommand.VoteSociety:
                    return BuildNonDraftPresentation(
                        command,
                        "Vote Society",
                        GetAllegianceVoteUnavailableReason(state),
                        isImmediate: true);

                case PlayerCommand.VoteKvlt:
                    return BuildNonDraftPresentation(
                        command,
                        "Vote KVLT",
                        GetAllegianceVoteUnavailableReason(state),
                        isImmediate: true);

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

                case PlayerCommand.ReturnToStanceSelection:
                    return BuildNonDraftPresentation(
                        command,
                        "Change Stance",
                        GetReturnToStanceSelectionUnavailableReason(
                            state
                        )
                    );

                case PlayerCommand.Dream:
                    return BuildNonDraftPresentation(
                        command,
                        "Dream",
                        GetDreamUnavailableReason(state)
                    );

                case PlayerCommand.KeeperBoostVisibility:
                    return BuildNonDraftPresentation(
                        command,
                        "Boost First Eligible Release",
                        GetKeeperVisibilityUnavailableReason(
                            state,
                            boost: true),
                        isImmediate: true);

                case PlayerCommand.KeeperSuppressVisibility:
                    return BuildNonDraftPresentation(
                        command,
                        "Suppress First Eligible Release",
                        GetKeeperVisibilityUnavailableReason(
                            state,
                            boost: false),
                        isImmediate: true);

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
                    string label;

                    if (IsCurrentKeeper(state))
                    {
                        label = "Commit Keeper Turn";
                    }
                    else
                    {
                        label =
                            state != null &&
                            TurnActionRules.IsOverreach(
                                state.DraftedActionsValue
                            )
                                ? "Commit Overreach"
                                : "Commit Turn";
                    }

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

                case PlayerCommand.CreateNewTrack:
                    return BuildNonDraftPresentation(
                        command,
                        "Create New Track",
                        GetCreateNewTrackUnavailableReason(
                            state
                        ),
                        isImmediate: true
                    );

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
                    .CreateEmptyRehearsalSet:
                    return BuildNonDraftPresentation(
                        command,
                        "Create Empty Rehearsal Set (Debug)",
                        GetRegularBandActionUnavailableReason(
                            state
                        ),
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
            GetKeeperVisibilityUnavailableReason(
                NetPlayerState state,
                bool boost)
        {
            if (state == null)
                return ActionUnavailableReason
                    .MissingPlayerState;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
                return ActionUnavailableReason
                    .MissingCoordinator;

            if (!coordinator.IsClientCurrentKeeper(
                    state.OwnerClientId))
                return ActionUnavailableReason
                    .KeeperRoleRestricted;

            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null)
                return ActionUnavailableReason
                    .NoSelectableTarget;

            var keeper = snapshot.KeeperInterventionState.Value;
            bool interventionAvailable = boost
                ? keeper.BoostAvailable
                : keeper.SuppressAvailable;

            if (!interventionAvailable)
                return ActionUnavailableReason
                    .NoActionAssigned;

            foreach (var row in snapshot.KeeperReleaseRows)
            {
                if (boost
                        ? row.CanReceiveBoost
                        : row.CanReceiveSuppress)
                    return ActionUnavailableReason.None;
            }

            return ActionUnavailableReason.NoSelectableTarget;
        }

        private ActionUnavailableReason
            GetHailUnavailableReason(
                NetPlayerState state)
        {
            if (state == null)
                return ActionUnavailableReason
                    .MissingPlayerState;

            if (!state.ActiveValue)
                return ActionUnavailableReason
                    .PlayerInactive;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null)
                return ActionUnavailableReason
                    .MissingCoordinator;

            return coordinator.testStarted.Value
                ? ActionUnavailableReason.None
                : ActionUnavailableReason.NoActionAssigned;
        }

        private ActionUnavailableReason
            GetAllegianceVoteUnavailableReason(
                NetPlayerState state)
        {
            ActionUnavailableReason baseReason =
                GetHailUnavailableReason(state);

            if (baseReason != ActionUnavailableReason.None)
                return baseReason;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            return coordinator.peak2TurnResolutionPhase.Value ==
                KvltTurnResolutionRuntimePhase
                    .AwaitingAllegianceCrisisResolution
                ? ActionUnavailableReason.None
                : ActionUnavailableReason.NoActionAssigned;
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

        private void AcceptCommand(
            ulong clientId,
            NetPlayerState state,
            PlayerCommand command,
            string message)
        {
            if (state != null)
            {
                state.PublishCommandFeedbackServer(
                    command,
                    PlayerCommandFeedbackStatus.Accepted,
                    message
                );
            }

            LogAccepted(
                $"Client {clientId}: {message}"
            );
        }

        private void RejectCommand(
            ulong clientId,
            NetPlayerState state,
            PlayerCommand command,
            string message)
        {
            if (state != null)
            {
                state.PublishCommandFeedbackServer(
                    command,
                    PlayerCommandFeedbackStatus.Rejected,
                    message
                );
            }

            LogRejected(
                $"Client {clientId}: {message}"
            );
        }

        private static PlayerCommand GetStanceCommand(
            BandStance stance)
        {
            return stance switch
            {
                BandStance.None =>
                    PlayerCommand
                        .ReturnToStanceSelection,

                BandStance.Gestate =>
                    PlayerCommand.SelectGestate,

                BandStance.Rehearse =>
                    PlayerCommand.SelectRehearse,

                BandStance.Promote =>
                    PlayerCommand.SelectPromote,

                _ =>
                    PlayerCommand
                        .ReturnToStanceSelection
            };
        }

        private static bool TryGetSlotCommand(
            int slotIndex,
            out PlayerCommand command)
        {
            command = slotIndex switch
            {
                1 => PlayerCommand.DraftPrimaryAction,
                2 => PlayerCommand.DraftSecondaryAction,
                3 => PlayerCommand.DraftTertiaryAction,
                _ => default
            };

            return slotIndex is >= 1 and <= 3;
        }

        public void RefreshContextTargetSummaryServer()
        {
            if (!IsServer)
                return;

            NetPlayerState state =
                GetPlayerState();

            if (state == null)
                return;

            PlayerContextTargetSummary summary =
                BuildContextTargetSummaryServer(state);

            state.SetContextTargetSummaryServer(summary);
        }

        private static PlayerContextTargetSummary
            BuildContextTargetSummaryServer(
                NetPlayerState state)
        {
            if (state == null)
            {
                return PlayerContextTargetSummary.Empty;
            }

            GameEntity playerEntity =
                state.PlayerEntity;

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                {
                    bool canCycle =
                        CanCycleIdeaSourceTarget(state);

                    bool hasSelectedSource =
                        playerEntity != null &&
                        playerEntity.TryGetTagContainer(
                            state.SelectedIdeaSourceValue,
                            out _
                        );

                    string displayName =
                        hasSelectedSource
                            ? state.SelectedIdeaSourceValue.ToString()
                            : null;

                    return PlayerContextTargetSummary.Create(
                        PlayerContextTargetKind.IdeaSource,
                        displayName,
                        canCycle
                    );
                }

                case BandStance.Rehearse:
                {
                    bool canCycle =
                        CanCycleRehearsalTarget(state);

                    RehearsalSet activeSet =
                        playerEntity?.GetActiveVhsSet();

                    return PlayerContextTargetSummary.Create(
                        PlayerContextTargetKind.RehearsalSet,
                        activeSet?.DisplayName,
                        canCycle,
                        hasEmptyTrack:
                        activeSet?.HasEmptyTrack ?? false
                    );
                }

                default:
                    return PlayerContextTargetSummary.Empty;
            }
        }

        [ServerRpc]
        private void SubmitCreateNewTrackServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetPlayerState();

            const PlayerCommand command =
                PlayerCommand.CreateNewTrack;

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            ActionUnavailableReason unavailableReason =
                GetCreateNewTrackUnavailableReason(state);

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            if (state.PlayerEntity == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The player has no acting entity."
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null ||
                coordinator.RehearsalResolver == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The rehearsal resolver is unavailable."
                );

                return;
            }

            bool succeeded =
                coordinator.RehearsalResolver
                    .TryCreateEmptyTrackInActiveSet(
                        clientId,
                        state.PlayerEntity,
                        coordinator.globalTurn.Value,
                        out string message
                    );

            if (!succeeded)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    message
                );

                return;
            }

            coordinator.PublishDomainProjectionServer(
                $"track created | client={clientId}"
            );

            RefreshContextTargetSummaryServer();

            AcceptCommand(
                clientId,
                state,
                command,
                message
            );
        }

        [ServerRpc]
        private void SubmitContextualCreateServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetPlayerState();

            const PlayerCommand command =
                PlayerCommand.ContextualCreate;

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            ActionUnavailableReason unavailableReason =
                GetContextualCreateUnavailableReason(
                    state
                );

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            switch (state.CurrentStanceValue)
            {
                case BandStance.Gestate:
                {
                    if (!DraftSpecificAction(
                            state,
                            DraftedActionType.CreateIdea
                        ))
                    {
                        RejectCommand(
                            clientId,
                            state,
                            command,
                            "The Idea action could not be added " +
                            "to the draft."
                        );

                        return;
                    }

                    ActionPlanDestination destination =
                        (ActionPlanDestination)
                        state.DraftedActionsValue;

                    AcceptCommand(
                        clientId,
                        state,
                        command,
                        "Create Idea drafted into " +
                        ActionPresentationText
                            .GetDestinationLabel(
                                destination
                            ) +
                        "."
                    );

                    return;
                }

                case BandStance.Rehearse:
                {
                    if (state.PlayerEntity == null)
                    {
                        RejectCommand(
                            clientId,
                            state,
                            command,
                            "The player has no acting entity."
                        );

                        return;
                    }

                    GameCoordinator coordinator =
                        GameCoordinator.Instance;

                    if (coordinator == null ||
                        coordinator.RehearsalResolver == null)
                    {
                        RejectCommand(
                            clientId,
                            state,
                            command,
                            "The rehearsal resolver is unavailable."
                        );

                        return;
                    }

                    bool succeeded =
                        coordinator.RehearsalResolver
                            .TryCreateEmptyTrackInActiveSet(
                                clientId,
                                state.PlayerEntity,
                                coordinator.globalTurn.Value,
                                out string message
                            );

                    if (!succeeded)
                    {
                        RejectCommand(
                            clientId,
                            state,
                            command,
                            message
                        );

                        return;
                    }

                    coordinator.PublishDomainProjectionServer(
                        $"contextual track created | " +
                        $"client={clientId}"
                    );

                    RefreshContextTargetSummaryServer();

                    AcceptCommand(
                        clientId,
                        state,
                        command,
                        message
                    );

                    return;
                }

                case BandStance.Promote:
                {
                    GameCoordinator coordinator =
                        GameCoordinator.Instance;

                    string happeningId = string.Empty;
                    string failureReason = string.Empty;

                    if (coordinator == null ||
                        !coordinator.TryCreatePeak2HappeningServer(
                            clientId,
                            out happeningId,
                            out failureReason))
                    {
                        RejectCommand(
                            clientId,
                            state,
                            command,
                            coordinator == null
                                ? "The gameplay coordinator is unavailable."
                                : failureReason);
                        return;
                    }

                    AcceptCommand(
                        clientId,
                        state,
                        command,
                        $"Created Happening {happeningId}.");
                    return;
                }

                default:
                    RejectCommand(
                        clientId,
                        state,
                        command,
                        "Contextual creation is unavailable " +
                        "for the current stance."
                    );

                    return;
            }
        }

        [ServerRpc]
        private void SubmitAddIdeaToCurrentTrackServerRpc(
            ServerRpcParams rpcParams = default)
        {
            ulong clientId =
                rpcParams.Receive.SenderClientId;

            NetPlayerState state =
                GetPlayerState();

            const PlayerCommand command =
                PlayerCommand.AddIdeaToCurrentTrack;

            if (state == null)
            {
                RejectCommand(
                    clientId,
                    null,
                    command,
                    "Player state is unavailable."
                );

                return;
            }

            ActionUnavailableReason unavailableReason =
                GetAddIdeaToCurrentTrackUnavailableReason(
                    state
                );

            if (unavailableReason !=
                ActionUnavailableReason.None)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    ActionPresentationText.GetReasonText(
                        unavailableReason
                    )
                );

                return;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null ||
                coordinator.RehearsalResolver == null)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    "The rehearsal resolver is unavailable."
                );

                return;
            }

            bool succeeded =
                coordinator.RehearsalResolver
                    .TryAppendNextIdeaToLatestTrack(
                        clientId,
                        state.PlayerEntity,
                        out string message
                    );

            if (!succeeded)
            {
                RejectCommand(
                    clientId,
                    state,
                    command,
                    message
                );

                return;
            }

            coordinator.PublishDomainProjectionServer(
                $"track Idea appended | client={clientId}"
            );

            RefreshContextTargetSummaryServer();

            AcceptCommand(
                clientId,
                state,
                command,
                message
            );
        }

    }
}
