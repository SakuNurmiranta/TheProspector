using System.Text;
using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class UIDebugPanelView : PersistentUIView
    {
        [SerializeField] private TextMeshProUGUI diagnosticsText;

        public override void Refresh(UIContext context)
        {
            Debug.Log("UIDebugPanelView.Refresh called");
            
            if (diagnosticsText == null)
                return;

            
            
            var coordinator = SEMM91.GameCoordinator.Instance;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Year: {context.CurrentRound}   Turn: {context.CurrentTurn} (global)");
            sb.AppendLine($"Keeper (role, not owner): {context.KeeperClientId}");
            sb.AppendLine("SPACE = End Turn (score++, exhausted = true)");
            sb.AppendLine("BACKSPACE = Skip Turn (score stays, exhausted = false)");
            sb.AppendLine("ESC = Quit");
            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();
            sb.AppendLine("Players:");

            // Screen size
            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();

            // Players
            sb.AppendLine("Players:");

            var playerStates = FindObjectsOfType<SEMM91.Networking.NetPlayerState>();

            foreach (var state in playerStates)
            {
                ulong clientId = state.OwnerClientIdCached != ulong.MaxValue
                    ? state.OwnerClientIdCached
                    : state.OwnerClientId;

                bool isKeeper = clientId == context.KeeperClientId;
                string role = isKeeper ? "Keeper" : "Regular";

                string line =
                    $"Client {clientId} | {state.DisplayNameStr} | " +
                    $"Role: {role} | " +
                    $"Score: {state.ScoreValue} | " +
                    $"Exhausted: {state.ExhaustedValue} | " +
                    $"Active: {state.ActiveValue}";

                sb.AppendLine(line);
            }

            // Game over
            if (coordinator != null && coordinator.GameEnded)
            {
                sb.AppendLine();
                sb.AppendLine($"GAME OVER — Winner: Client {coordinator.FinalWinner}");
                sb.AppendLine("Press ESC to quit");
            }

            diagnosticsText.text = sb.ToString();
            
        }
    }
}