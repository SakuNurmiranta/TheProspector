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

            sb.AppendLine($"Year: {context.CurrentRound}   Turn: {context.CurrentTurn} (global)   Season: {coordinator?.CurrentSeason}");
            sb.AppendLine($"Keeper (role, not owner): {context.KeeperClientId}");
            sb.AppendLine("SPACE = End Turn (score++, exhausted = true)");
            sb.AppendLine("BACKSPACE = Skip Turn (score stays, exhausted = false)");
            sb.AppendLine("ESC = Quit");
 

            // Screen size
            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();

            // Players
            sb.AppendLine("Players:");

            var playerStates = FindObjectsOfType<SEMM91.Networking.NetPlayerState>();

            foreach (var state in playerStates)
            {
                sb.AppendLine(FormatPlayerLine(state, context.KeeperClientId));
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
        
        private string FormatPlayerLine(SEMM91.Networking.NetPlayerState state, ulong keeperClientId)
        {
            ulong clientId = state.OwnerClientIdCached != ulong.MaxValue
                ? state.OwnerClientIdCached
                : state.OwnerClientId;

            bool isKeeper = clientId == keeperClientId;
            string role = isKeeper ? "Keeper" : "Regular";

            return
                $"Client {clientId} | {state.DisplayNameStr} | " +
                $"Role: {role} | " +
                $"Stance: {state.CurrentStanceValue} | " +
                $"PStance: {state.PreviousStanceValue} | " +
                $"Same: {state.IsContinuingSameStance()} | " +
                $"Actions: {state.ActionsUsedValue}/3 | " +
                $"Productive: {state.ProductiveActionsUsedValue}/3 | " +
                $"Score: {state.ScoreValue} | " +
                $"Exhausted: {state.ExhaustedValue} | " +
                $"Active: {state.ActiveValue}";
            
        }
    }
}