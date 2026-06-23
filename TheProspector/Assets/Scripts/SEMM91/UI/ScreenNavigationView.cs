using SEMM91.GamePlay.Actions;
using SEMM91.InputSystems;
using SEMM91.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public class ScreenNavigationView : PersistentUIView
    {
        [Header("Navigation")]
        [SerializeField]
        private UIStateDirector uiStateDirector;

        [SerializeField]
        private Button mainMapButton;

        [SerializeField]
        private Button gestationButton;

        [SerializeField]
        private Button rehearsalButton;

        [SerializeField]
        private Button promotionButton;
        
        [SerializeField]
        private Button sceneMapButton;

        private NetPlayerState _localPlayerState;
        private PlayerActionController _actionController;
        
        private void Awake()
        {
            if (mainMapButton != null)
            {
                mainMapButton.onClick.AddListener(
                    NavigateToMainMap
                );
            }

            if (gestationButton != null)
            {
                gestationButton.onClick.AddListener(
                    NavigateToGestation
                );
            }

            if (rehearsalButton != null)
            {
                rehearsalButton.onClick.AddListener(
                    NavigateToRehearsal
                );
            }

            if (promotionButton != null)
            {
                promotionButton.onClick.AddListener(
                    NavigateToPromotion
                );
            }
            
            if (sceneMapButton != null)
            {
                sceneMapButton.onClick.AddListener(
                    NavigateToSceneMap
                );
            }
            
        }

        private void OnDestroy()
        {
            if (mainMapButton != null)
            {
                mainMapButton.onClick.RemoveListener(
                    NavigateToMainMap
                );
            }

            if (gestationButton != null)
            {
                gestationButton.onClick.RemoveListener(
                    NavigateToGestation
                );
            }

            if (rehearsalButton != null)
            {
                rehearsalButton.onClick.RemoveListener(
                    NavigateToRehearsal
                );
            }

            if (promotionButton != null)
            {
                promotionButton.onClick.RemoveListener(
                    NavigateToPromotion
                );
            }
            
            if (sceneMapButton != null)
            {
                sceneMapButton.onClick.RemoveListener(
                    NavigateToSceneMap
                );
            }
        }

        public override void Refresh(
            UIContext context)
        {
            _localPlayerState =
                context.LocalPlayerState;

            _actionController =
                _localPlayerState != null
                    ? _localPlayerState.GetComponent<
                        PlayerActionController
                    >()
                    : null;

            RefreshButton(
                mainMapButton,
                context.ActiveState ==
                GameUIState.MainMap
            );

            RefreshStanceNavigationButton(
                gestationButton,
                PlayerCommand.SelectGestate,
                BandStance.Gestate,
                context.ActiveState ==
                GameUIState.Gestation
            );

            RefreshStanceNavigationButton(
                rehearsalButton,
                PlayerCommand.SelectRehearse,
                BandStance.Rehearse,
                context.ActiveState ==
                GameUIState.Rehearsal
            );

            RefreshStanceNavigationButton(
                promotionButton,
                PlayerCommand.SelectPromote,
                BandStance.Promote,
                context.ActiveState ==
                GameUIState.Promotion
            );
            
            RefreshButton(
                sceneMapButton,
                context.ActiveState ==
                GameUIState.SceneMap
            );
        }
        private static void RefreshButton(
            Button button,
            bool isSelected)
        {
            if (button == null)
                return;

            button.interactable =
                !isSelected;
        }

        private void NavigateToMainMap()
        {
            NavigateTo(
                GameUIState.MainMap
            );
        }

        private void NavigateToGestation()
        {
            RequestStanceAndNavigate(
                PlayerCommand.SelectGestate,
                BandStance.Gestate,
                GameUIState.Gestation
            );
        }

        private void NavigateToRehearsal()
        {
            RequestStanceAndNavigate(
                PlayerCommand.SelectRehearse,
                BandStance.Rehearse,
                GameUIState.Rehearsal
            );
        }

        private void NavigateToPromotion()
        {
            RequestStanceAndNavigate(
                PlayerCommand.SelectPromote,
                BandStance.Promote,
                GameUIState.Promotion
            );
        }

        private void NavigateTo(
            GameUIState destination)
        {
            if (uiStateDirector == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ScreenNavigationView)}] " +
                    $"{nameof(UIStateDirector)} is missing.",
                    this
                );

                return;
            }

            uiStateDirector.SetState(destination);
        }
        
        private void RefreshStanceNavigationButton(
            Button button,
            PlayerCommand stanceCommand,
            BandStance requiredStance,
            bool isCurrentScreen)
        {
            if (button == null)
                return;

            if (_localPlayerState == null ||
                _actionController == null)
            {
                button.interactable = false;
                return;
            }

            bool alreadyInRequiredStance =
                _localPlayerState.CurrentStanceValue ==
                requiredStance;

            if (isCurrentScreen &&
                alreadyInRequiredStance)
            {
                button.interactable = false;
                return;
            }

            if (alreadyInRequiredStance)
            {
                // The stance is already correct. The button only
                // needs to perform local navigation.
                button.interactable = true;
                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(
                    stanceCommand
                );

            button.interactable =
                presentation.IsAvailable;
        }
        
        private void RequestStanceAndNavigate(
            PlayerCommand stanceCommand,
            BandStance requiredStance,
            GameUIState destination)
        {
            if (_localPlayerState == null ||
                _actionController == null)
            {
                return;
            }

            bool stanceAlreadySelected =
                _localPlayerState.CurrentStanceValue ==
                requiredStance;

            if (!stanceAlreadySelected)
            {
                if (!_actionController.CanRequest(
                        stanceCommand
                    ))
                {
                    return;
                }

                _actionController.Request(
                    stanceCommand
                );
            }

            NavigateTo(destination);
        }
        
        private void NavigateToSceneMap()
        {
            NavigateTo(
                GameUIState.SceneMap
            );
        }
    }
}