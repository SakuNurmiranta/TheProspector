using SEMM91.GamePlay.Actions;
using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.InputSystems;
using SEMM91.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public class TurnPlannerView : PersistentUIView
    {
        private PlayerActionController _actionController;
        
        [Header("Action Selection")]
        [SerializeField]
        private Button primaryActionButton;

        [SerializeField]
        private Button secondaryActionButton;

        [SerializeField]
        private Button tertiaryActionButton;

        private TextMeshProUGUI _primaryActionButtonText;
        private TextMeshProUGUI _secondaryActionButtonText;
        private TextMeshProUGUI _tertiaryActionButtonText;

        private PlayerCommand _primaryActionCommand =
            PlayerCommand.DraftPrimaryAction;

        private PlayerCommand _secondaryActionCommand =
            PlayerCommand.DraftSecondaryAction;

        private PlayerCommand _tertiaryActionCommand =
            PlayerCommand.DraftTertiaryAction;
        
        [Header("Plan Slots")]
        [SerializeField]
        private TextMeshProUGUI standardSlot1Text;

        [SerializeField]
        private TextMeshProUGUI standardSlot2Text;

        [SerializeField]
        private TextMeshProUGUI recoveryText;

        [SerializeField]
        private TextMeshProUGUI overreachSlotText;

        [Header("Turn State")]
        [SerializeField]
        private TextMeshProUGUI feedbackText;

        [SerializeField]
        private TextMeshProUGUI waitingText;

        [Header("Plan Controls")]
        [SerializeField]
        private Button undoButton;

        [SerializeField]
        private Button commitButton;

        private TextMeshProUGUI
            _undoButtonText;

        private TextMeshProUGUI
            _commitButtonText;

        private PlayerCommand _undoOrReturnCommand =
            PlayerCommand.ReturnToStanceSelection;
        
        [Header("Rehearsal Construction")]
        [SerializeField]
        private Button createTrackButton;

        private TextMeshProUGUI
            _createTrackButtonText;
        
        [Header("Context Target")]
        [SerializeField]
        private TextMeshProUGUI contextTargetText;

        [SerializeField]
        private Button cycleTargetButton;

        private TextMeshProUGUI _cycleTargetButtonText;
        private void Awake()
        {
            if (createTrackButton != null)
            {
                _createTrackButtonText =
                    createTrackButton
                        .GetComponentInChildren<
                            TextMeshProUGUI>(true);

                createTrackButton.onClick.AddListener(
                    RequestCreateTrack
                );
            }
            
            if (primaryActionButton != null)
            {
                _primaryActionButtonText =
                    primaryActionButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                primaryActionButton.onClick.AddListener(
                    RequestPrimaryAction
                );
            }

            if (secondaryActionButton != null)
            {
                _secondaryActionButtonText =
                    secondaryActionButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                secondaryActionButton.onClick.AddListener(
                    RequestSecondaryAction
                );
            }

            if (tertiaryActionButton != null)
            {
                _tertiaryActionButtonText =
                    tertiaryActionButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                tertiaryActionButton.onClick.AddListener(
                    RequestTertiaryAction
                );
            }
            
            if (undoButton != null)
            {
                _undoButtonText =
                    undoButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                undoButton.onClick.AddListener(
                    RequestUndoOrReturn
                );
            }

            if (commitButton != null)
            {
                _commitButtonText =
                    commitButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                commitButton.onClick.AddListener(
                    RequestCommit
                );
            }
            
            if (cycleTargetButton != null)
            {
                _cycleTargetButtonText =
                    cycleTargetButton
                        .GetComponentInChildren<TextMeshProUGUI>(
                            true
                        );

                cycleTargetButton.onClick.AddListener(
                    RequestCycleTarget
                );
            }
        }

        private void OnDestroy()
        {
            if (createTrackButton != null)
            {
                createTrackButton.onClick.RemoveListener(
                    RequestCreateTrack
                );
            }
            
            if (primaryActionButton != null)
            {
                primaryActionButton.onClick.RemoveListener(
                    RequestPrimaryAction
                );
            }

            if (secondaryActionButton != null)
            {
                secondaryActionButton.onClick.RemoveListener(
                    RequestSecondaryAction
                );
            }

            if (tertiaryActionButton != null)
            {
                tertiaryActionButton.onClick.RemoveListener(
                    RequestTertiaryAction
                );
            }
            
            if (undoButton != null)
            {
                undoButton.onClick.RemoveListener(
                    RequestUndoOrReturn
                );
            }

            if (commitButton != null)
            {
                commitButton.onClick.RemoveListener(
                    RequestCommit
                );
            }
            
            if (cycleTargetButton != null)
            {
                cycleTargetButton.onClick.RemoveListener(
                    RequestCycleTarget
                );
            }
        }
        
        
        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            if (state == null)
            {
                _actionController = null;
                SetActionButtonsInteractable(false);
                SetPlanControlButtonsInteractable(false);
                SetContextTargetConnectingState();
                
                ShowActionButtonsConnectingState();
                ShowPlanControlButtonsConnectingState();
                ShowConnectingState();
                return;
            }
            _actionController =
                state.GetComponent<PlayerActionController>();

            RefreshActionButtons();
            RefreshPlanControlButtons(
                context.ActiveState,
                state
            );
            RefreshContextTarget(state);
            
            SetText(
                standardSlot1Text,
                "Standard 1: " +
                FormatDraftSummary(
                    state.DraftedStandardSlot1Value
                )
            );

            SetText(
                standardSlot2Text,
                "Standard 2: " +
                FormatDraftSummary(
                    state.DraftedStandardSlot2Value
                )
            );

            DraftedActionSummary overreach =
                state.DraftedOverreachSlotValue;

            if (overreach.IsOccupied)
            {
                SetText(
                    recoveryText,
                    "Recovery: FORFEITED"
                );

                SetText(
                    overreachSlotText,
                    "OVERREACH: " +
                    FormatDraftSummary(overreach) +
                    " — Exhaustion on commit"
                );
            }
            else
            {
                SetText(
                    recoveryText,
                    "Recovery: Retained"
                );

                SetText(
                    overreachSlotText,
                    "Overreach: Empty"
                );
            }
            RefreshCreateTrackButton(
                context.ActiveState
            );
            RefreshFeedback(state);
            RefreshWaitingState(state);
        }

        private void ShowConnectingState()
        {
            SetText(
                standardSlot1Text,
                "Standard 1: Connecting..."
            );

            SetText(
                standardSlot2Text,
                "Standard 2: Connecting..."
            );

            SetText(
                recoveryText,
                "Recovery: Connecting..."
            );

            SetText(
                overreachSlotText,
                "Overreach: Connecting..."
            );

            SetText(
                feedbackText,
                "Latest result: None"
            );

            SetText(
                waitingText,
                "Turn plan unavailable"
            );
        }

        private void RefreshFeedback(
            NetPlayerState state)
        {
            PlayerCommandFeedback feedback =
                state.LatestCommandFeedbackValue;

            if (!feedback.HasValue)
            {
                SetText(
                    feedbackText,
                    "Latest result: None"
                );

                return;
            }

            string status =
                feedback.Status ==
                PlayerCommandFeedbackStatus.Accepted
                    ? "Accepted"
                    : "Rejected";

            string message =
                feedback.Message.ToString();

            SetText(
                feedbackText,
                $"Latest result: {status} — {message}"
            );
        }

        private void RefreshWaitingState(
            NetPlayerState state)
        {
            string turnState;

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            KvltTurnResolutionRuntimePhase phase =
                coordinator != null
                    ? coordinator
                        .peak2TurnResolutionPhase.Value
                    : KvltTurnResolutionRuntimePhase.Idle;

            Peak2TurnInteractionActionSet interaction =
                Peak2TurnInteractionActionSet.Resolve(
                    phase
                );

            if (!state.ActiveValue)
            {
                turnState = "INACTIVE";
            }
            else if (interaction.OverridesTurnPlan)
            {
                turnState = interaction.WaitingLabel;

                if (phase ==
                        KvltTurnResolutionRuntimePhase
                            .SharedHappeningWindowOpen &&
                    coordinator != null &&
                    Unity.Netcode.NetworkManager.Singleton !=
                        null)
                {
                    double remainingSeconds =
                        coordinator
                            .peak2HappeningWindowEndsAt.Value -
                        Unity.Netcode.NetworkManager.Singleton
                            .ServerTime.Time;

                    int displayedSeconds =
                        Mathf.Max(
                            0,
                            Mathf.CeilToInt(
                                (float)remainingSeconds
                            )
                        );

                    turnState +=
                        $" — {displayedSeconds}s";
                }
            }
            else if (state.HasCommittedTurnValue)
            {
                turnState =
                    "COMMITTED — WAITING FOR OTHER PLAYERS";
            }
            else
            {
                turnState = "PLAN OPEN";
            }

            SetText(
                waitingText,
                turnState
            );
        }

        private static string FormatDraftSummary(
            DraftedActionSummary summary)
        {
            if (!summary.IsOccupied)
                return "Empty";

            string actionLabel =
                ActionPresentationText.GetActionLabel(
                    summary.ActionType
                );

            if (summary.HasPhysicalEventLocation)
            {
                return
                    $"{actionLabel} — " +
                    $"Gig at {summary.PhysicalEventNodeId}";
            }

            if (summary.HasIdeaSource)
            {
                return
                    $"{actionLabel} — " +
                    $"{summary.IdeaSourceContainerType}";
            }

            return actionLabel;
        }
        private static void SetText(
            TextMeshProUGUI target,
            string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private void RequestCommand(
            PlayerCommand command)
        {
            TryRequestCommand(command);
        }

        private bool TryRequestCommand(
            PlayerCommand command)
        {
            if (_actionController == null)
                return false;

            if (!_actionController.CanRequest(command))
                return false;

            _actionController.Request(command);
            return true;
        }        
        private void RefreshActionButtons()
        {
            GameCoordinator coordinator =
                GameCoordinator.Instance;

            KvltTurnResolutionRuntimePhase phase =
                coordinator != null
                    ? coordinator
                        .peak2TurnResolutionPhase.Value
                    : KvltTurnResolutionRuntimePhase.Idle;

            Peak2TurnInteractionActionSet interaction =
                Peak2TurnInteractionActionSet.Resolve(
                    phase
                );

            _primaryActionCommand =
                interaction.PrimaryCommand;

            _secondaryActionCommand =
                interaction.SecondaryCommand;

            _tertiaryActionCommand =
                interaction.TertiaryCommand;

            RefreshActionButton(
                primaryActionButton,
                _primaryActionButtonText,
                _primaryActionCommand
            );

            RefreshActionButton(
                secondaryActionButton,
                _secondaryActionButtonText,
                _secondaryActionCommand
            );

            if (interaction.HasTertiaryCommand)
            {
                RefreshActionButton(
                    tertiaryActionButton,
                    _tertiaryActionButtonText,
                    _tertiaryActionCommand
                );
            }
            else if (tertiaryActionButton != null)
            {
                tertiaryActionButton.interactable = false;
                SetText(
                    _tertiaryActionButtonText,
                    "Interaction\nNot used"
                );
            }
        }

        private void RefreshActionButton(
            Button button,
            TextMeshProUGUI buttonText,
            PlayerCommand command)
        {
            if (button == null)
                return;

            if (_actionController == null)
            {
                button.interactable = false;
                SetText(buttonText, "Connecting...");
                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(command);

            button.interactable =
                presentation.IsAvailable;

            SetText(
                buttonText,
                FormatActionButtonText(presentation)
            );
        }
        
        private static string FormatActionButtonText(
            PlayerActionPresentation presentation)
        {
            if (presentation.IsAvailable)
            {
                if (presentation.IsImmediate)
                {
                    return
                        $"{presentation.Label}\n" +
                        "IMMEDIATE";
                }

                if (presentation.ActivatesOverreach)
                {
                    return
                        $"{presentation.Label}\n" +
                        "OVERREACH";
                }

                if (presentation.Destination !=
                    ActionPlanDestination.None)
                {
                    string destination =
                        ActionPresentationText
                            .GetDestinationLabel(
                                presentation.Destination
                            );

                    return
                        $"{presentation.Label}\n" +
                        destination;
                }

                return presentation.Label;
            }

            string unavailableLabel =
                GetCompactUnavailableLabel(
                    presentation.UnavailableReason
                );

            return
                $"{presentation.Label}\n" +
                unavailableLabel;
        }

        private static string GetCompactUnavailableLabel(
            ActionUnavailableReason reason)
        {
            return reason switch
            {
                ActionUnavailableReason.NoSelectableTarget =>
                    "NO ALTERNATIVE",

                ActionUnavailableReason.TargetCyclingUnsupported =>
                    "NO TARGET",
                
                ActionUnavailableReason.NoActionAssigned =>
                    "NO ACTION",

                ActionUnavailableReason.NoStanceSelected =>
                    "SELECT STANCE",

                ActionUnavailableReason.PlayerInactive =>
                    "INACTIVE",

                ActionUnavailableReason.TurnAlreadyCommitted =>
                    "COMMITTED",

                ActionUnavailableReason.DraftEmpty =>
                    "NOTHING TO UNDO",
                
                ActionUnavailableReason.PlanFull =>
                    "PLAN FULL",

                ActionUnavailableReason.ActionInvalidForStance =>
                    "WRONG STANCE",
                
                ActionUnavailableReason.DraftAlreadyStarted =>
                    "UNDO DRAFT FIRST",
                
                ActionUnavailableReason
                        .MissingActiveRehearsalSet =>
                    "NO ACTIVE SET",

                _ =>
                    "UNAVAILABLE"
            };
        }
        
        private void ShowActionButtonsConnectingState()
        {
            SetText(
                _primaryActionButtonText,
                "Primary\nConnecting..."
            );

            SetText(
                _secondaryActionButtonText,
                "Secondary\nConnecting..."
            );

            SetText(
                _tertiaryActionButtonText,
                "Tertiary\nConnecting..."
            );
        }

        private void SetActionButtonsInteractable(
            bool interactable)
        {
            if (primaryActionButton != null)
            {
                primaryActionButton.interactable =
                    interactable;
            }

            if (secondaryActionButton != null)
            {
                secondaryActionButton.interactable =
                    interactable;
            }

            if (tertiaryActionButton != null)
            {
                tertiaryActionButton.interactable =
                    interactable;
            }
        }
        
        private void RequestPrimaryAction()
        {
            RequestCommand(
                _primaryActionCommand
            );
        }

        private void RequestSecondaryAction()
        {
            RequestCommand(
                _secondaryActionCommand
            );
        }

        private void RequestTertiaryAction()
        {
            RequestCommand(
                _tertiaryActionCommand
            );
        }
        
        private void RefreshPlanControlButtons(
            GameUIState activeState,
            NetPlayerState state)
        {
            bool showUndoOrReturnButton =
                activeState == GameUIState.Gestation ||
                activeState == GameUIState.Rehearsal ||
                activeState == GameUIState.Promotion;

            bool hasDraftedActions =
                state.DraftedStandardSlot1Value.IsOccupied ||
                state.DraftedStandardSlot2Value.IsOccupied ||
                state.DraftedOverreachSlotValue.IsOccupied;

            _undoOrReturnCommand =
                hasDraftedActions
                    ? PlayerCommand.UndoDraftAction
                    : PlayerCommand
                        .ReturnToStanceSelection;

            if (undoButton != null)
            {
                undoButton.gameObject.SetActive(
                    showUndoOrReturnButton
                );
            }

            if (showUndoOrReturnButton)
            {
                RefreshPlanControlButton(
                    undoButton,
                    _undoButtonText,
                    _undoOrReturnCommand
                );
            }

            RefreshPlanControlButton(
                commitButton,
                _commitButtonText,
                PlayerCommand.CommitTurn
            );
        }

        private void RefreshPlanControlButton(
            Button button,
            TextMeshProUGUI buttonText,
            PlayerCommand command)
        {
            if (button == null)
                return;

            if (_actionController == null)
            {
                button.interactable = false;
                SetText(
                    buttonText,
                    "Connecting..."
                );

                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(command);

            button.interactable =
                presentation.IsAvailable;

            SetText(
                buttonText,
                FormatPlanControlButtonText(
                    presentation
                )
            );
        }

        private static string FormatPlanControlButtonText(
            PlayerActionPresentation presentation)
        {
            if (presentation.IsAvailable)
            {
                return presentation.Label;
            }

            string unavailableLabel =
                GetCompactUnavailableLabel(
                    presentation.UnavailableReason
                );

            return
                $"{presentation.Label}\n" +
                unavailableLabel;
        }
        
        private void ShowPlanControlButtonsConnectingState()
        {
            SetText(
                _undoButtonText,
                "Change Stance\nConnecting..."
            );

            SetText(
                _commitButtonText,
                "Commit Turn\nConnecting..."
            );
        }

        private void SetPlanControlButtonsInteractable(
            bool interactable)
        {
            if (undoButton != null)
            {
                undoButton.interactable =
                    interactable;
            }

            if (commitButton != null)
            {
                commitButton.interactable =
                    interactable;
            }
        }
        
        private void RequestUndoOrReturn()
        {
            RequestCommand(
                _undoOrReturnCommand
            );
        }

        private void RequestCommit()
        {
            RequestCommand(
                PlayerCommand.CommitTurn
            );
        }
        
        private void RefreshContextTarget(
            NetPlayerState state)
        {
            PlayerContextTargetSummary summary =
                state.ContextTargetSummaryValue;

            SetText(
                contextTargetText,
                FormatContextTarget(summary)
            );

            if (cycleTargetButton == null)
                return;

            if (_actionController == null)
            {
                cycleTargetButton.interactable = false;

                SetText(
                    _cycleTargetButtonText,
                    "Cycle Target\nUNAVAILABLE"
                );

                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(
                    PlayerCommand.CycleTarget
                );

            cycleTargetButton.interactable =
                presentation.IsAvailable;

            SetText(
                _cycleTargetButtonText,
                FormatCycleTargetButtonText(
                    presentation
                )
            );
        }
        
        private static string FormatContextTarget(
            PlayerContextTargetSummary summary)
        {
            switch (summary.Kind)
            {
                case PlayerContextTargetKind.IdeaSource:
                    return summary.HasTarget
                        ? $"Idea source: {summary.DisplayName}"
                        : "Idea source: None";

                case PlayerContextTargetKind.RehearsalSet:
                    return summary.HasTarget
                        ? $"Rehearsal set: {summary.DisplayName}"
                        : "Rehearsal set: None";

                default:
                    return "Context target: Not used";
            }
        }
        
        private static string FormatCycleTargetButtonText(
            PlayerActionPresentation presentation)
        {
            if (presentation.IsAvailable)
            {
                return presentation.Label;
            }

            string unavailableLabel =
                GetCompactUnavailableLabel(
                    presentation.UnavailableReason
                );

            return
                $"{presentation.Label}\n" +
                unavailableLabel;
        }
        
        private void SetContextTargetConnectingState()
        {
            SetText(
                contextTargetText,
                "Context target: Connecting..."
            );

            if (cycleTargetButton != null)
            {
                cycleTargetButton.interactable = false;
            }

            SetText(
                _cycleTargetButtonText,
                "Cycle Target\nConnecting..."
            );
        }
        
        private void RequestCycleTarget()
        {
            RequestCommand(
                PlayerCommand.CycleTarget
            );
        }
        
        private void RefreshCreateTrackButton(
            GameUIState activeState)
        {
            if (createTrackButton == null)
                return;

            bool show =
                activeState == GameUIState.Rehearsal;

            createTrackButton.gameObject.SetActive(show);

            if (!show)
                return;

            RefreshActionButton(
                createTrackButton,
                _createTrackButtonText,
                PlayerCommand.CreateNewTrack
            );
        }

        private void RequestCreateTrack()
        {
            RequestCommand(
                PlayerCommand.CreateNewTrack
            );
        }
    }
}
