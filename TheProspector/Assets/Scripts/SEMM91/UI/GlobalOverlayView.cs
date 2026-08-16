using TMPro;
using UnityEngine;
using SEMM91.Networking;
using SEMM91.GamePlay.Kvlt.Scenario;

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
            GameCoordinator coordinator =
                GameCoordinator.Instance;

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
                if (coordinator != null &&
                    !coordinator
                        .IsPlayableSessionStarted)
                {
                    stateText.text =
                        $"Session: " +
                        $"{FormatSessionGateStatus(coordinator.sessionStartGateStatus.Value)}\n" +
                        $"Roster: " +
                        $"{coordinator.sessionConnectedPlayerCount.Value}/" +
                        $"{coordinator.sessionRequiredPlayerCount.Value} connected | " +
                        $"{coordinator.sessionReadyPlayerCount.Value} ready | " +
                        $"H {coordinator.sessionHumanPlayerCount.Value} | " +
                        $"B {coordinator.sessionBotPlayerCount.Value}\n" +
                        $"Protocol: " +
                        $"{Peak2NetworkProtocol.DisplayLabel}";

                    return;
                }

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

        private static string FormatSessionGateStatus(
            Peak2SessionStartGateStatus status)
        {
            return status switch
            {
                Peak2SessionStartGateStatus
                    .WaitingForParticipants =>
                    "Waiting for participants",

                Peak2SessionStartGateStatus
                    .WaitingForClassification =>
                    "Waiting for role classification",

                Peak2SessionStartGateStatus
                    .WaitingForReadiness =>
                    "Waiting for ready acknowledgements",

                Peak2SessionStartGateStatus
                    .InvalidParticipantCount =>
                    "Invalid participant count",

                Peak2SessionStartGateStatus
                    .InvalidRoleComposition =>
                    "Invalid roles — requires H 1 / B 4",

                Peak2SessionStartGateStatus
                    .InvalidRosterState =>
                    "Invalid roster state",

                Peak2SessionStartGateStatus.Ready =>
                    "Roster ready — starting",

                Peak2SessionStartGateStatus.Started =>
                    "Started",

                Peak2SessionStartGateStatus
                    .BootstrapFailed =>
                    "Scenario bootstrap failed",

                _ => status.ToString()
            };
        }
    }
}
