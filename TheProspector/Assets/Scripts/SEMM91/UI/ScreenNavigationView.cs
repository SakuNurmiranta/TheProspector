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
        }

        public override void Refresh(
            UIContext context)
        {
            RefreshButton(
                mainMapButton,
                context.ActiveState ==
                GameUIState.MainMap
            );

            RefreshButton(
                gestationButton,
                context.ActiveState ==
                GameUIState.Gestation
            );

            RefreshButton(
                rehearsalButton,
                context.ActiveState ==
                GameUIState.Rehearsal
            );

            RefreshButton(
                promotionButton,
                context.ActiveState ==
                GameUIState.Promotion
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
            NavigateTo(
                GameUIState.Gestation
            );
        }

        private void NavigateToRehearsal()
        {
            NavigateTo(
                GameUIState.Rehearsal
            );
        }

        private void NavigateToPromotion()
        {
            NavigateTo(
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
    }
}