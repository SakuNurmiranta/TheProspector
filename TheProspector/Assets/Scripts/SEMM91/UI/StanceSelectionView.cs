using SEMM91.InputSystems;
using SEMM91.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public sealed class StanceSelectionView :
        ContextualUIView
    {
        [Header("Stance Selection")]
        [SerializeField]
        private Button gestationButton;

        [SerializeField]
        private Button rehearsalButton;

        [SerializeField]
        private Button promotionButton;

        private PlayerActionController
            _actionController;

        private void Awake()
        {
            if (gestationButton != null)
            {
                gestationButton.onClick.AddListener(
                    RequestGestation
                );
            }

            if (rehearsalButton != null)
            {
                rehearsalButton.onClick.AddListener(
                    RequestRehearsal
                );
            }

            if (promotionButton != null)
            {
                promotionButton.onClick.AddListener(
                    RequestPromotion
                );
            }
        }

        private void OnDestroy()
        {
            if (gestationButton != null)
            {
                gestationButton.onClick.RemoveListener(
                    RequestGestation
                );
            }

            if (rehearsalButton != null)
            {
                rehearsalButton.onClick.RemoveListener(
                    RequestRehearsal
                );
            }

            if (promotionButton != null)
            {
                promotionButton.onClick.RemoveListener(
                    RequestPromotion
                );
            }
        }

        public override bool Supports(
            GameUIState state)
        {
            return
                state ==
                GameUIState.StanceSelection;
        }

        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            _actionController =
                state != null
                    ? state.GetComponent<
                        PlayerActionController
                    >()
                    : null;

            RefreshButton(
                gestationButton,
                PlayerCommand.SelectGestate
            );

            RefreshButton(
                rehearsalButton,
                PlayerCommand.SelectRehearse
            );

            RefreshButton(
                promotionButton,
                PlayerCommand.SelectPromote
            );
        }

        private void RefreshButton(
            Button button,
            PlayerCommand command)
        {
            if (button == null)
                return;

            if (_actionController == null)
            {
                button.interactable = false;
                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(
                    command
                );

            button.interactable =
                presentation.IsAvailable;
        }

        private void RequestGestation()
        {
            RequestStance(
                PlayerCommand.SelectGestate
            );
        }

        private void RequestRehearsal()
        {
            RequestStance(
                PlayerCommand.SelectRehearse
            );
        }

        private void RequestPromotion()
        {
            RequestStance(
                PlayerCommand.SelectPromote
            );
        }

        private void RequestStance(
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