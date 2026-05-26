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

            var playerStates = FindObjectsByType<SEMM91.Networking.NetPlayerState>(FindObjectsSortMode.None);

            foreach (var state in playerStates)
            {
                sb.AppendLine(FormatPlayerLine(state, context.KeeperClientId));
                AppendVhsSetOverview(sb, state.PlayerEntity);
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

            int ideaCount = state.PlayerEntity != null ? state.PlayerEntity.Ideas.Count : 0;
            int setCount = state.PlayerEntity != null ? state.PlayerEntity.VhsSets.Count : 0;
            int vhsTrackCount = state.PlayerEntity != null ? state.PlayerEntity.GetTotalVhsTrackCountFromSets() : 0;
            int demoTapeCount = state.PlayerEntity != null ? state.PlayerEntity.DemoTapes.Count : 0;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Client {clientId} | {state.DisplayNameStr}");
            sb.AppendLine($"  Role: {role}");
            sb.AppendLine($"  Stance: {state.CurrentStanceValue}  | Previous: {state.PreviousStanceValue}  | Same: {state.IsContinuingSameStance()}");
            sb.AppendLine($"  Actions: drafted {state.DraftedActionsValue}/3  | committed {state.CommittedActionsValue}/3");
            sb.AppendLine($"  Inventory: ideas {ideaCount}  | sets {setCount}  | VHS tracks {vhsTrackCount}  | demo tapes {demoTapeCount}");
            sb.AppendLine($"  State: score {state.ScoreValue}  | active {state.ActiveValue}  | exhausted {state.ExhaustedValue}");

            return sb.ToString();
        }
        
        
        private void AppendVhsSetOverview(StringBuilder sb, SEMM91.GamePlay.Entities.GameEntity controller)
        {
            if (controller == null)
            {
                sb.AppendLine("  VHS Sets: none");
                return;
            }

            if (controller.VhsSets.Count == 0)
            {
                sb.AppendLine("  VHS Sets: none");
                return;
            }

            var activeSet = controller.GetActiveVhsSet();

            sb.AppendLine("  VHS Sets:");

            foreach (var vhsSet in controller.VhsSets)
            {
                if (vhsSet == null)
                    continue;

                string activeMarker = vhsSet == activeSet ? "ACTIVE" : "inactive";

                sb.AppendLine(
                    $"    * {vhsSet.DisplayName} {activeMarker} " +
                    $"tracks={vhsSet.VhsTracks.Count} last={vhsSet.LastRehearsedTurn}"
                );

                foreach (var vhsTrack in vhsSet.VhsTracks)
                {
                    if (vhsTrack == null)
                        continue;

                    sb.AppendLine(
                        $"      - {vhsTrack.DisplayName} " +
                        $"c={vhsTrack.Conveyance:0.00} " +
                        $"max={vhsTrack.ConveyanceMax:0.00} " +
                        $"r={vhsTrack.RehearsalCount} " +
                        $"raw={vhsTrack.IsRaw} " +
                        $"honed={vhsTrack.IsHoned}"
                    );
                }
            }
        }
    }
}