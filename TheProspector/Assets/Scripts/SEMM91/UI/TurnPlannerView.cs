using SEMM91.GamePlay.Actions;
using SEMM91.InputSystems;
using SEMM91.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public class TurnPlannerView : PersistentUIView
    {
        [Header("Stance Selection")]
        [SerializeField]
        private Button gestateButton;

        [SerializeField]
        private Button rehearseButton;

        [SerializeField]
        private Button promoteButton;

        private PlayerActionController _actionController;
        
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

        private void Awake()
        {
            if (gestateButton != null)
            {
                gestateButton.onClick.AddListener(
                    RequestGestate
                );
            }

            if (rehearseButton != null)
            {
                rehearseButton.onClick.AddListener(
                    RequestRehearse
                );
            }

            if (promoteButton != null)
            {
                promoteButton.onClick.AddListener(
                    RequestPromote
                );
            }
        }

        private void OnDestroy()
        {
            if (gestateButton != null)
            {
                gestateButton.onClick.RemoveListener(
                    RequestGestate
                );
            }

            if (rehearseButton != null)
            {
                rehearseButton.onClick.RemoveListener(
                    RequestRehearse
                );
            }

            if (promoteButton != null)
            {
                promoteButton.onClick.RemoveListener(
                    RequestPromote
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
                SetStanceButtonsInteractable(false);

                ShowConnectingState();
                return;
            }
            _actionController =
                state.GetComponent<PlayerActionController>();

            RefreshStanceButtons(state);

            
            
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

            if (!state.ActiveValue)
            {
                turnState = "INACTIVE";
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

            if (!summary.HasIdeaSource)
                return actionLabel;

            return
                $"{actionLabel} — " +
                $"{summary.IdeaSourceContainerType}";
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
        
        private void RefreshStanceButtons(
            NetPlayerState state)
        {
            RefreshStanceButton(
                gestateButton,
                PlayerCommand.SelectGestate,
                state.CurrentStanceValue ==
                BandStance.Gestate
            );

            RefreshStanceButton(
                rehearseButton,
                PlayerCommand.SelectRehearse,
                state.CurrentStanceValue ==
                BandStance.Rehearse
            );

            RefreshStanceButton(
                promoteButton,
                PlayerCommand.SelectPromote,
                state.CurrentStanceValue ==
                BandStance.Promote
            );
        }

        private void RefreshStanceButton(
            Button button,
            PlayerCommand command,
            bool isSelected)
        {
            if (button == null)
                return;

            if (_actionController == null)
            {
                button.interactable = false;
                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(command);

            button.interactable =
                presentation.IsAvailable &&
                !isSelected;
        }

        private void SetStanceButtonsInteractable(
            bool interactable)
        {
            if (gestateButton != null)
            {
                gestateButton.interactable =
                    interactable;
            }

            if (rehearseButton != null)
            {
                rehearseButton.interactable =
                    interactable;
            }

            if (promoteButton != null)
            {
                promoteButton.interactable =
                    interactable;
            }
        }
        
        private void RequestGestate()
        {
            RequestCommand(
                PlayerCommand.SelectGestate
            );
        }

        private void RequestRehearse()
        {
            RequestCommand(
                PlayerCommand.SelectRehearse
            );
        }

        private void RequestPromote()
        {
            RequestCommand(
                PlayerCommand.SelectPromote
            );
        }

        private void RequestCommand(
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