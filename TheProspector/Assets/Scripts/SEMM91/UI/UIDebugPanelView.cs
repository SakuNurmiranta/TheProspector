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

            string latestVhsInfo = "none";

            if (state.PlayerEntity != null)
            {
                var latestVhs = state.PlayerEntity.GetLatestVhsTrackFromLatestSet();

                if (latestVhs != null)
                {
                    latestVhsInfo =
                        $"{latestVhs.DisplayName} " +
                        $"c={latestVhs.Conveyance:0.00} " +
                        $"max={latestVhs.ConveyanceMax:0.00} " +
                        $"r={latestVhs.RehearsalCount} " +
                        $"raw={latestVhs.IsRaw} " +
                        $"honed={latestVhs.IsHoned}";
                }
            }
            
            string latestSetInfo = "none";

            if (state.PlayerEntity != null && state.PlayerEntity.VhsSets.Count > 0)
            {
                var latestSet = state.PlayerEntity.VhsSets[state.PlayerEntity.VhsSets.Count - 1];

                latestSetInfo =
                    $"{latestSet.DisplayName} " +
                    $"tracks={latestSet.VhsTracks.Count} " +
                    $"last={latestSet.LastRehearsedTurn}";
            }
            
            string activeSetInfo = "none";

            if (state.PlayerEntity != null)
            {
                var activeSet = state.PlayerEntity.GetActiveVhsSet();

                if (activeSet != null)
                {
                    activeSetInfo =
                        $"{activeSet.DisplayName} " +
                        $"tracks={activeSet.VhsTracks.Count} " +
                        $"last={activeSet.LastRehearsedTurn}";
                }
            }
            
            
            return
                $"Client {clientId} | {state.DisplayNameStr} | " +
                $"Role: {role} | " +
                $"Stance: {state.CurrentStanceValue} | " +
                $"PStance: {state.PreviousStanceValue} | " +
                $"Same: {state.IsContinuingSameStance()} | " +
                $"Drafted: {state.DraftedActionsValue}/3 | " +
                $"Commits: {state.CommittedActionsValue}/3 | " +
                $"Ideas: {(state.PlayerEntity != null ? state.PlayerEntity.Ideas.Count : 0)} | " +
                $"Sets: {(state.PlayerEntity != null ? state.PlayerEntity.VhsSets.Count : 0)} | " +
                $"ActiveSet: {activeSetInfo} | " +
                $"LatestSet: {latestSetInfo} | " +
                $"VHS: {(state.PlayerEntity != null ? state.PlayerEntity.GetTotalVhsTrackCountFromSets() : 0)} | " +
                $"LatestVHS: {latestVhsInfo} | " +
                $"Score: {state.ScoreValue} | " +
                $"Exhausted: {state.ExhaustedValue} | " +
                $"Active: {state.ActiveValue}" +
                $"\n ";
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