using TMPro;
using UnityEngine;
using SEMM91.Networking;

namespace SEMM91.UI
{
    public class GlobalOverlayView : PersistentUIView
    {
        [Header("Status Text")] [SerializeField]
        private TextMeshProUGUI yearText;

        [SerializeField] private TextMeshProUGUI turnText;

        [SerializeField] private TextMeshProUGUI keeperText;

        [SerializeField] private TextMeshProUGUI roleText;

        [SerializeField] private TextMeshProUGUI stateText;

        [SerializeField] private TextMeshProUGUI seasonText;

        [SerializeField] private TextMeshProUGUI leaderText;

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

            if (seasonText != null)
            {
                seasonText.text =
                    $"Season: {context.CurrentSeason}";
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

                if (leaderText != null)
                {
                    leaderText.text =
                        "Leader condition: Connecting...";
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

            if (leaderText != null)
            {
                if (!context.HasLocalLeaderSnapshot)
                {
                    leaderText.text =
                        "Leader condition: Synchronizing...";
                }
                else
                {
                    string condition =
                        context.LocalLeaderIsExhausted
                            ? "Exhausted"
                            : "Rested";

                    leaderText.text =
                        $"Leader condition: {condition}";
                }
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
        }
    }
}