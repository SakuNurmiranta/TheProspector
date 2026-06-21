using System.Text;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking.DebugSnapshots;
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
            var snapshot = DomainSnapshotReplicator.Instance;

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                $"Year: {context.CurrentRound}   Turn: {context.CurrentTurn} (global)   Season: {coordinator?.CurrentSeason}");
            sb.AppendLine($"Keeper (role, not owner): {context.KeeperClientId}");

            if (snapshot != null)
            {
                sb.AppendLine(
                    $"Snapshot: v{snapshot.SnapshotVersion.Value} | " +
                    $"playerRows={snapshot.PlayerInventoryRows.Count} | " +
                    $"sceneRows={snapshot.SceneOutputRows.Count}"
                );
            }
            else
            {
                sb.AppendLine("Snapshot: none");
            }

            AppendSceneOutputOverview(sb, snapshot);
            AppendControlLegend(sb);

            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();

            sb.AppendLine("Players:");

            var playerStates = FindObjectsByType<Networking.NetPlayerState>(FindObjectsSortMode.None);

            foreach (var state in playerStates)
            {
                sb.AppendLine(FormatPlayerLine(state, context.KeeperClientId, snapshot));
            }

            diagnosticsText.text = sb.ToString();
        }

        private string FormatPlayerLine(
            Networking.NetPlayerState state,
            ulong keeperClientId,
            DomainSnapshotReplicator snapshot)
        {
            ulong clientId = GetClientId(state);

            bool isKeeper = clientId == keeperClientId;
            string role = isKeeper ? "Keeper" : "Regular";
            string turnSubmissionState;
            StringBuilder sb = new StringBuilder();

            if (state.IsServer || state.IsOwner)
            {
                if (!state.ActiveValue)
                {
                    turnSubmissionState = "Inactive";
                }
                else if (state.HasCommittedTurnValue)
                {
                    turnSubmissionState = "Committed and waiting";
                }
                else
                {
                    turnSubmissionState = "Open";
                }
            }
            else
            {
                turnSubmissionState = "owner-only";
            }



            bool hasInventorySnapshot = false;

            int ideaCount = 0;
            int setCount = 0;
            int vhsTrackCount = 0;
            int demoTapeCount = 0;

            string leaderEntityId = "None";
            bool leaderIsExhausted = false;
            string latestDemoId = "None";
            string latestDemoSceneState = "None";
            

            if (snapshot != null)
            {
                foreach (var row in snapshot.PlayerInventoryRows)
                {
                    if (row.ClientId != clientId)
                        continue;

                    hasInventorySnapshot = true;

                    ideaCount = row.IdeaCount;
                    setCount = row.VhsSetCount;
                    vhsTrackCount = row.TrackCount;
                    demoTapeCount = row.DemoTapeCount;

                    leaderEntityId = row.LeaderEntityId.ToString();
                    leaderIsExhausted = row.LeaderIsExhausted;
                    latestDemoId = row.LatestDemoId.ToString();
                    latestDemoSceneState = row.LatestDemoSceneState.ToString();

                    break;
                }
            }



            sb.AppendLine($"Client {clientId} | {state.DisplayNameStr}");
            sb.AppendLine($"  Role: {role}");
            sb.AppendLine(
                $"  Stance: {state.CurrentStanceValue}  | " +
                $"Previous: {state.PreviousStanceValue}  | " +
                $"Same: {state.IsContinuingSameStance()}"
            );
            
            sb.AppendLine(
                $" Idea source target: {state.SelectedIdeaSourceValue}"
            );
            
            sb.AppendLine(
                $"  Actions: drafted {state.DraftedActionsValue}/3  | " +
                $"committed {state.CommittedActionsValue}/3"
            );
            
            sb.AppendLine(
                $"  Turn state: {turnSubmissionState}"
            );

            AppendTagSlots(sb, state);
            string dreamAvailability;

            if (state.IsServer || state.IsOwner)
            {
                dreamAvailability = 
                    state.CanDreamValue
                        ? "Available"
                        : "Unavailable";
            }
            else
            {
                dreamAvailability = "owner-only";
            }
            
            sb.AppendLine($"  Dream: {dreamAvailability}");
            
            AppendCurrentStanceActionLegend(sb, state.CurrentStanceValue);

            if (hasInventorySnapshot)
            {
                sb.AppendLine($"  Leader Entity: {leaderEntityId}");
                sb.AppendLine(
                    $"  Leader condition: " +
                    $"exhausted {leaderIsExhausted}"
                );
                sb.AppendLine(
                    $"  Inventory snapshot: ideas {ideaCount}  | " +
                    $"sets {setCount}  | " +
                    $"VHS tracks {vhsTrackCount}  | " +
                    $"demo tapes {demoTapeCount}"
                );
                sb.AppendLine(
                    $"  Latest demo: {latestDemoId} | " +
                    $"sceneState {latestDemoSceneState}"
                );
            }
            else
            {
                sb.AppendLine("  Inventory snapshot: awaiting server snapshot row");
            }

            AppendVhsSetOverview(sb, state.PlayerEntity);
            
            sb.AppendLine(
                $"  Player state: score {state.ScoreValue}  | " +
                $"active {state.ActiveValue}"
            );

            return sb.ToString();
        }

        private static ulong GetClientId(Networking.NetPlayerState state)
        {
            if (state == null)
                return ulong.MaxValue;

            return state.OwnerClientIdCached != ulong.MaxValue
                ? state.OwnerClientIdCached
                : state.OwnerClientId;
        }

        private void AppendVhsSetOverview(
            StringBuilder sb,
            GameEntity controller)
        {
            if (controller == null)
            {
                sb.AppendLine(
                    "  Rehearsal Sets: unavailable on this peer " +
                    "(PlayerEntity is server-domain only)"
                );

                return;
            }

            if (controller.VhsSets.Count == 0)
            {
                sb.AppendLine("  Rehearsal Sets: none");
                return;
            }

            var activeSet = controller.GetActiveVhsSet();

            sb.AppendLine("  Rehearsal Sets:");

            foreach (var vhsSet in controller.VhsSets)
            {
                if (vhsSet == null)
                    continue;

                bool isActive = vhsSet == activeSet;
                bool isEmpty = vhsSet.VhsTracks.Count == 0;

                string activeMarker =
                    isActive
                        ? "ACTIVE"
                        : "inactive";

                string emptyMarker =
                    isEmpty
                        ? " EMPTY"
                        : string.Empty;

                sb.AppendLine(
                    $"    * {vhsSet.DisplayName} " +
                    $"[{activeMarker}{emptyMarker}] " +
                    $"tracks={vhsSet.VhsTracks.Count} " +
                    $"created={vhsSet.CreatedTurn} " +
                    $"lastRehearsed={vhsSet.LastRehearsedTurn}"
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
            sb.AppendLine("  Z = Create empty rehearsal set");
            sb.AppendLine("  X = Cycle contextual target");
            sb.AppendLine("  ENTER = Commit turn");
            sb.AppendLine("  ESC = Quit");
        }

        private void AppendCurrentStanceActionLegend(StringBuilder sb, BandStance stance)
        {
            sb.AppendLine("  Current stance actions:");

            switch (stance)
            {
                case BandStance.Gestate:
                    sb.AppendLine("    Q: Create idea");
                    sb.AppendLine("    W: Gestate secondary placeholder");
                    sb.AppendLine("    E: none");
                    sb.AppendLine("    X: Cycle idea-source target");
                    break;

                case BandStance.Rehearse:
                    sb.AppendLine("    Q: Rehearse active set");
                    sb.AppendLine("    W: Record active set to demo");
                    sb.AppendLine("    E: none");
                    sb.AppendLine("    X: Cycle active rehearsal set");
                    break;

                case BandStance.Promote:
                    sb.AppendLine("    Q: Release latest demo to KVLT");
                    sb.AppendLine("    W: Promote secondary placeholder");
                    sb.AppendLine("    E: Promote tertiary placeholder");
                    break;

                case BandStance.None:
                default:
                    sb.AppendLine("    Q: none");
                    sb.AppendLine("    W: none");
                    sb.AppendLine("    E: none");
                    break;
            }
        }

        private void AppendSceneOutputOverview(StringBuilder sb, DomainSnapshotReplicator snapshot)
        {
            sb.AppendLine("Scene Output:");

            if (snapshot == null)
            {
                sb.AppendLine("  awaiting snapshot replicator");
                sb.AppendLine();
                return;
            }

            if (snapshot.SceneOutputRows.Count == 0)
            {
                sb.AppendLine("  none");
                sb.AppendLine();
                return;
            }

            bool hasDominant = false;

            foreach (var row in snapshot.SceneOutputRows)
            {
                if (!row.IsDominantOwner)
                    continue;

                sb.AppendLine(
                    $"  Dominant: {row.OwnerName} | score {row.AccumulatedSceneOutput:0.00}"
                );

                hasDominant = true;
                break;
            }

            if (!hasDominant)
            {
                sb.AppendLine("  Dominant: none");
            }

            foreach (var row in snapshot.SceneOutputRows)
            {
                string marker = row.IsDominantOwner ? " DOMINANT" : "";

                sb.AppendLine(
                    $"  * {row.OwnerName} | " +
                    $"client {row.OwnerClientId} | " +
                    $"releases {row.HostedReleaseCount} | " +
                    $"score {row.AccumulatedSceneOutput:0.00}" +
                    marker
                );
            }

            sb.AppendLine();
        }
        
        private static void AppendTagSlots(
            StringBuilder sb,
            Networking.NetPlayerState state)
        {
            sb.AppendLine("  Tags:");

            var entity = state?.PlayerEntity;

            if (entity == null)
            {
                sb.AppendLine(
                    "    unavailable on this peer " +
                    "(PlayerEntity is server-domain only)"
                );

                return;
            }

            if (entity.TagContainers == null ||
                entity.TagContainers.Count == 0)
            {
                sb.AppendLine("    none");
                return;
            }

            foreach (var container in entity.TagContainers)
            {
                if (container == null)
                    continue;

                if (!container.HasHeldTag ||
                    container.HeldTag == null)
                {
                    sb.AppendLine(
                        $"    {container.ContainerType}: empty"
                    );

                    continue;
                }

                var heldTag = container.HeldTag;
                var tag = heldTag.TagInstance;

                string lifecycle;

                if (heldTag.IsEvaporating)
                {
                    lifecycle =
                        $" | {heldTag.State}" +
                        $" | remaining={heldTag.RemainingTurns}" +
                        $" | reason={heldTag.InstabilityReason}";
                }
                else
                {
                    lifecycle = $" | {heldTag.State}";
                }

                sb.AppendLine(
                    $"    {container.ContainerType}: " +
                    $"{tag.axis} {tag.pole} {tag.degree}" +
                    lifecycle
                );
            }
        }
    }
    
    
}