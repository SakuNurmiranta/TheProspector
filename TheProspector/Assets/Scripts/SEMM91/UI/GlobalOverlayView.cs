using TMPro;
using UnityEngine;
using SEMM91.Networking;

namespace SEMM91.UI
{
    public class GlobalOverlayView : PersistentUIView
    {
        [Header("Status Text")]
        [SerializeField] private TextMeshProUGUI yearText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI keeperText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI stateText;

        public override void Refresh(
            UIContext context)
        {
            if (yearText != null)
            {
                yearText.text =
                    $"Year: {context.CurrentRound}";
            }

            if (turnText != null)
            {
                turnText.text =
                    $"Turn: {context.CurrentTurn}";
            }

            if (keeperText != null)
            {
                keeperText.text =
                    context.KeeperClientId == ulong.MaxValue
                        ? "Keeper: Unassigned"
                        : $"Keeper: Player {context.KeeperClientId}";
            }

            NetPlayerState localPlayerState =
                context.LocalPlayerState;

            if (localPlayerState == null)
            {
                if (roleText != null)
                {
                    roleText.text =
                        "Player: Connecting...";
                }

                if (stateText != null)
                {
                    stateText.text =
                        $"View: {context.ActiveState}";
                }

                return;
            }

            if (roleText != null)
            {
                string role =
                    context.IsKeeper
                        ? "Keeper"
                        : "Regular";

                roleText.text =
                    $"Player: {localPlayerState.DisplayNameStr} | " +
                    $"Role: {role}";
            }

            if (stateText != null)
            {
                string turnState;

                if (!localPlayerState.ActiveValue)
                {
                    turnState = "Inactive";
                }
                else if (
                    localPlayerState.HasCommittedTurnValue)
                {
                    turnState =
                        "Committed — waiting";
                }
                else
                {
                    turnState = "Open";
                }

                stateText.text =
                    $"Stance: " +
                    $"{localPlayerState.CurrentStanceValue} | " +
                    $"Turn state: {turnState}";
            }
        }    }
}