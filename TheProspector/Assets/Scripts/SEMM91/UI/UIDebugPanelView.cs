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

            
            
            var coordinator = GameCoordinator.Instance;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Year: {context.CurrentRound}   Turn: {context.CurrentTurn} (global)   Season: {coordinator?.CurrentSeason}");
            sb.AppendLine($"Keeper (role, not owner): {context.KeeperClientId}");
            AppendControlLegend(sb);
 

            // Screen size
            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();

            // Players
            sb.AppendLine("Players:");

            var playerStates = FindObjectsByType<Networking.NetPlayerState>(FindObjectsSortMode.None);

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
        
        private string FormatPlayerLine(Networking.NetPlayerState state, ulong keeperClientId)
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
            AppendCurrentStanceActionLegend(sb, state.CurrentStanceValue);
            sb.AppendLine($"  Inventory: ideas {ideaCount}  | sets {setCount}  | VHS tracks {vhsTrackCount}  | demo tapes {demoTapeCount}");
            sb.AppendLine($"  State: score {state.ScoreValue}  | active {state.ActiveValue}  | exhausted {state.ExhaustedValue}");

            return sb.ToString();
        }
        
        
        private void AppendVhsSetOverview(StringBuilder sb, GamePlay.Entities.GameEntity controller)
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
        
        private void AppendControlLegend(StringBuilder sb)
        {
            sb.AppendLine("Controls:");
            sb.AppendLine("  1/2/3 = Select Gestate / Rehearse / Promote");
            sb.AppendLine("  Q/W/E = Draft stance slot action");
            sb.AppendLine("  R = Draft rest");
            sb.AppendLine("  Z = Undo drafted action");
            sb.AppendLine("  ENTER = Commit turn");
            sb.AppendLine("  ESC = Quit");
        }
        
        private void AppendCurrentStanceActionLegend(StringBuilder sb, GamePlay.BandStance stance)
        {
            sb.AppendLine("  Current stance actions:");

            switch (stance)
            {
                case GamePlay.BandStance.Gestate:
                    sb.AppendLine("    Q: Create idea");
                    sb.AppendLine("    W: Gestate secondary placeholder");
                    sb.AppendLine("    E: Cycle active VHS set");
                    break;

                case GamePlay.BandStance.Rehearse:
                    sb.AppendLine("    Q: Rehearse active set");
                    sb.AppendLine("    W: Record active set to demo");
                    sb.AppendLine("    E: none");
                    break;

                case GamePlay.BandStance.Promote:
                    sb.AppendLine("    Q: Release latest demo to KVLT");
                    sb.AppendLine("    W: Promote secondary placeholder");
                    sb.AppendLine("    E: Promote tertiary placeholder");
                    break;

                case GamePlay.BandStance.None:
                default:
                    sb.AppendLine("    Q: none");
                    sb.AppendLine("    W: none");
                    sb.AppendLine("    E: none");
                    break;
            }
        }
    }
}