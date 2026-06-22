using SEMM91.Networking;
using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class GestationView : ContextualUIView
    {
        [Header("Gestation State")]
        [SerializeField]
        private TextMeshProUGUI selectedIdeaSourceText;

        [SerializeField]
        private TextMeshProUGUI dreamAvailabilityText;

        public override bool Supports(
            GameUIState state)
        {
            return state == GameUIState.Gestation;
        }

        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            if (state == null)
            {
                SetText(
                    selectedIdeaSourceText,
                    "Selected idea source: Connecting..."
                );

                SetText(
                    dreamAvailabilityText,
                    "Dream this turn: Connecting..."
                );

                return;
            }

            SetText(
                selectedIdeaSourceText,
                $"Selected idea source: " +
                $"{state.SelectedIdeaSourceValue}"
            );

            string dreamState;

            if (!state.ActiveValue)
            {
                dreamState = "Unavailable";
            }
            else
            {
                dreamState =
                    state.CanDreamValue
                        ? "Available"
                        : "Used";
            }

            SetText(
                dreamAvailabilityText,
                $"Dream this turn: {dreamState}"
            );
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
    }
}