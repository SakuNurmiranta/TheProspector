using System.Text;
using SEMM91.InputSystems;
using SEMM91.Core.Entities;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking;
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
                    $"releaseRows={snapshot.KeeperReleaseRows.Count} | " +
                    $"turnRecords=" +
                        $"{snapshot.KvltTurnResolutionRows.Count} | " +
                    $"canonRows=" +
                        $"{snapshot.KvltCanonPrecedentRows.Count} | " +
                    $"semanticRows=" +
                        $"{snapshot.KvltSemanticEnvironmentRows.Count} | " +
                    $"happenings={snapshot.KvltHappeningRows.Count} | " +
                    $"hails={snapshot.KvltHailRows.Count} | " +
                    $"grips={snapshot.KvltParadigmGripRows.Count} | " +
                    $"beefs={snapshot.KvltParadigmBeefRows.Count} | " +
                    $"posers={snapshot.KvltPoserRows.Count}"
                );
            }
            else
            {
                sb.AppendLine("Snapshot: none");
            }

            AppendKvltOverview(sb, snapshot);
            AppendControlLegend(sb);

            sb.AppendLine($"Screen size = {Screen.width}x{Screen.height}");
            sb.AppendLine();

            sb.AppendLine("Players:");

            var playerStates = FindObjectsByType<NetPlayerState>(FindObjectsSortMode.None);

            foreach (var state in playerStates)
            {
                sb.AppendLine(FormatPlayerLine(state, context.KeeperClientId, snapshot));
            }

            diagnosticsText.text = sb.ToString();
        }

        private string FormatPlayerLine(
            NetPlayerState state,
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

            AppendContextTargetSummary(
                sb,
                state
            );

            sb.AppendLine(
                $"  Draft plan: " +
                $"{FormatActionLoad(state.DraftedActionsValue)}"
            );

            sb.AppendLine(
                $"  Committed plan: " +
                $"{FormatActionLoad(state.CommittedActionsValue)}"
            );

            AppendDraftActionSummaries(
                sb,
                state
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

            PlayerActionController actionController =
                state.GetComponent<PlayerActionController>();

            AppendCurrentActionPresentations(
                sb,
                state,
                actionController
            );

            AppendLatestCommandFeedback(
                sb,
                state
            );

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

        private static ulong GetClientId(NetPlayerState state)
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
            sb.AppendLine("  R = Finish/commit current plan");
            sb.AppendLine("  Z = Create empty rehearsal set");
            sb.AppendLine("  X = Cycle contextual target");
            sb.AppendLine("  C = Contextual create (Promote: Happening)");
            sb.AppendLine("  H/O = Hail Satan / Hail Odin");
            sb.AppendLine("  J/K = Vote Society / Vote KVLT");
            sb.AppendLine("  B/N = Keeper boost / suppress first eligible release");
            sb.AppendLine("  ENTER = Commit turn");
            sb.AppendLine("  ESC = Quit");
        }


        private void AppendKvltOverview(StringBuilder sb, DomainSnapshotReplicator snapshot)
        {
            sb.AppendLine("KVLT authoritative state:");

            if (snapshot == null)
            {
                sb.AppendLine("  awaiting snapshot replicator");
                sb.AppendLine();
                return;
            }

            var scene =
                snapshot.KvltSceneState.Value;

            if (!scene.HasState)
            {
                sb.AppendLine("  none");
                sb.AppendLine();
                return;
            }

            sb.AppendLine(
                $"  turn={scene.CurrentTurn} | " +
                $"lastSettled={scene.LastSettledTurn} | " +
                $"published={scene.PublishedTurn} | " +
                $"phase={scene.RuntimePhaseValue}"
            );

            sb.AppendLine(
                $"  Canon={scene.CanonPrecedentCount} | " +
                $"Pressure={scene.PressureEntryCount} | " +
                $"Normative={scene.NormativeAffinityCount} | " +
                $"ScoreEvents={scene.ScoreEventCount} | " +
                $"StandingOwners={scene.StandingOwnerCount}"
            );

            sb.AppendLine(
                $"  Releases={scene.SceneReleaseCount} | " +
                $"Field={scene.FieldReleaseCount} | " +
                $"CanonRetained={scene.CanonRetainedCount} | " +
                $"HistoricalCanon={scene.HistoricalCanonCount}"
            );

            sb.AppendLine(
                $"  Keeper={scene.KeeperClientId} | " +
                $"Tenure={scene.KeeperTenureId} | " +
                $"Pull={scene.KeeperPull:0.###}"
            );

            foreach (var row
                     in snapshot.PlayerInventoryRows)
            {
                string standing =
                    row.HasKvltStanding
                        ? row.KvltStanding.ToString("0.###")
                        : "none";

                string yearInfluence =
                    row.HasYearInfluence
                        ? row.YearInfluence.ToString("0.###")
                        : "not settled";

                sb.AppendLine(
                    $"  * {row.DisplayName} | " +
                    $"score={row.KvltTotalScore:0.###} | " +
                    $"yearInfluence={yearInfluence} | " +
                    $"standing={standing} | " +
                    $"releases={row.KvltReleaseCount}" +
                    (row.IsKvltKeeper
                        ? " | KEEPER"
                        : string.Empty)
                );
            }

            if (snapshot.KvltTurnResolutionRows.Count > 0)
            {
                var record =
                    snapshot.KvltTurnResolutionRows[
                        snapshot.KvltTurnResolutionRows.Count - 1
                    ];

                sb.AppendLine(
                    $"  Outcome t{record.SettledTurn}->" +
                    $"t{record.PublishedTurn}: " +
                    $"move={record.MovementCount}, " +
                    $"reject={record.RejectedCount}, " +
                    $"happenings={record.HappeningCount}, " +
                    $"crises={record.CrisisCount}, " +
                    $"precedents={record.AcceptedPrecedentsRaised}, " +
                    $"contests={record.ParadigmContestCount}, " +
                    $"beef={record.ParadigmBeefCount}, " +
                    $"newPosers={record.NewPoserDeclarationCount}, " +
                    $"screened={record.PostHappeningLegitimacyCount}, " +
                    $"canonized={record.CanonizedReleaseCount}, " +
                    $"scoreEvents={record.ScoreEventCount}, " +
                    $"ingress={record.IngressedReleaseCount}"
                );
            }

            foreach (var hail in snapshot.KvltHailRows)
            {
                sb.AppendLine(
                    $"  HAIL {hail.DeclarerEntityId} -> " +
                    $"{hail.HailedAspectId} | " +
                    $"behavior={hail.BehaviorOccurrenceId} | " +
                    $"happening={hail.HappeningId}");
            }

            foreach (var grip in snapshot.KvltParadigmGripRows)
            {
                sb.AppendLine(
                    $"  GRIP {grip.EntityId} -> " +
                    $"{grip.HailAspectId} | " +
                    $"reinforced={grip.ReinforcementCount} | " +
                    $"last=t{grip.LastReinforcedTurn}");
            }

            foreach (var beef in snapshot.KvltParadigmBeefRows)
            {
                sb.AppendLine(
                    $"  BEEF {beef.FirstHailAspectId}/" +
                    $"{beef.SecondHailAspectId} | " +
                    $"praxis={beef.BehaviorTypeId} | " +
                    $"reinforced={beef.ReinforcementCount} | " +
                    $"last=t{beef.LastSettledTurn} | " +
                    $"behavior={beef.LatestBehaviorOccurrenceId}");
            }

            foreach (var poser in snapshot.KvltPoserRows)
            {
                sb.AppendLine(
                    $"  POSER {poser.EntityId} | " +
                    $"{poser.LosingHailAspectId}->" +
                    $"{poser.WinningHailAspectId} | " +
                    $"active={poser.IsActive} | " +
                    $"remaining={poser.RemainingTurns} | " +
                    $"window=[{poser.ActiveFromTurn}," +
                    $"{poser.ActiveUntilTurnExclusive}) | " +
                    $"behavior={poser.SourceBehaviorOccurrenceId}");
            }

            sb.AppendLine();
        }

        private static void AppendTagSlots(
            StringBuilder sb,
            NetPlayerState state)
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

        private static string FormatActionLoad(
            int productiveActionCount)
        {
            if (TurnActionRules.IsOverreach(
                    productiveActionCount))
            {
                return
                    $"{TurnActionRules.StandardProductiveActionCapacity}/" +
                    $"{TurnActionRules.StandardProductiveActionCapacity} " +
                    "standard + OVERREACH";
            }

            return
                $"{productiveActionCount}/" +
                $"{TurnActionRules.StandardProductiveActionCapacity} " +
                "standard; recovery retained";
        }

        private static void AppendDraftActionSummaries(
            StringBuilder sb,
            NetPlayerState state)
        {
            if (state == null)
                return;

            sb.AppendLine("  Draft slots:");

            if (!state.IsServer &&
                !state.IsOwner)
            {
                sb.AppendLine(
                    "    private to owning player"
                );

                return;
            }

            AppendDraftActionSlot(
                sb,
                "Standard 1",
                state.DraftedStandardSlot1Value,
                false
            );

            AppendDraftActionSlot(
                sb,
                "Standard 2",
                state.DraftedStandardSlot2Value,
                false
            );

            AppendDraftActionSlot(
                sb,
                "Overreach",
                state.DraftedOverreachSlotValue,
                true
            );
        }

        private static void AppendDraftActionSlot(
            StringBuilder sb,
            string slotLabel,
            DraftedActionSummary summary,
            bool isOverreachSlot)
        {
            if (!summary.IsOccupied)
            {
                sb.AppendLine(
                    $"    {slotLabel}: empty"
                );

                return;
            }

            string actionDescription =
                summary.ActionType.ToString();

            if (summary.HasIdeaSource)
            {
                actionDescription +=
                    $" — {summary.IdeaSourceContainerType}";
            }

            if (isOverreachSlot)
            {
                actionDescription +=
                    " [EXHAUSTION ON COMMIT]";
            }

            sb.AppendLine(
                $"    {slotLabel}: " +
                $"{actionDescription}"
            );
        }

        private static void AppendCurrentActionPresentations(
            StringBuilder sb,
            NetPlayerState state,
            PlayerActionController actionController)
        {
            sb.AppendLine("  Current action controls:");

            if (actionController == null)
            {
                sb.AppendLine(
                    "    PlayerActionController unavailable"
                );

                return;
            }

            if (!state.IsServer &&
                !state.IsOwner)
            {
                sb.AppendLine(
                    "    private to owning player"
                );

                return;
            }

            AppendActionPresentation(
                sb,
                "Q",
                actionController.GetPresentation(
                    PlayerCommand.DraftPrimaryAction
                )
            );

            AppendActionPresentation(
                sb,
                "W",
                actionController.GetPresentation(
                    PlayerCommand.DraftSecondaryAction
                )
            );

            AppendActionPresentation(
                sb,
                "E",
                actionController.GetPresentation(
                    PlayerCommand.DraftTertiaryAction
                )
            );

            AppendActionPresentation(
                sb,
                "X",
                actionController.GetPresentation(
                    PlayerCommand.CycleTarget
                )
            );

            AppendActionPresentation(
                sb,
                "D",
                actionController.GetPresentation(
                    PlayerCommand.Dream
                )
            );

            AppendActionPresentation(
                sb,
                "ENTER",
                actionController.GetPresentation(
                    PlayerCommand.CommitTurn
                )
            );

            AppendActionPresentation(
                sb,
                "BACKSPACE",
                actionController.GetPresentation(
                    PlayerCommand.UndoDraftAction
                )
            );

            AppendActionPresentation(
                sb,
                "C",
                actionController.GetPresentation(
                    PlayerCommand.ContextualCreate));

            AppendActionPresentation(
                sb,
                "H",
                actionController.GetPresentation(
                    PlayerCommand.HailSatan));

            AppendActionPresentation(
                sb,
                "O",
                actionController.GetPresentation(
                    PlayerCommand.HailOdin));

            AppendActionPresentation(
                sb,
                "J",
                actionController.GetPresentation(
                    PlayerCommand.VoteSociety));

            AppendActionPresentation(
                sb,
                "K",
                actionController.GetPresentation(
                    PlayerCommand.VoteKvlt));

            AppendActionPresentation(
                sb,
                "B",
                actionController.GetPresentation(
                    PlayerCommand.KeeperBoostVisibility));

            AppendActionPresentation(
                sb,
                "N",
                actionController.GetPresentation(
                    PlayerCommand.KeeperSuppressVisibility));
        }

        private static void AppendActionPresentation(
            StringBuilder sb,
            string controlLabel,
            PlayerActionPresentation presentation)
        {
            string availability =
                presentation.IsAvailable
                    ? "AVAILABLE"
                    : $"BLOCKED — " +
                      $"{presentation.UnavailableReasonText}";

            string destination = string.Empty;

            if (presentation.Destination !=
                ActionPlanDestination.None)
            {
                string destinationLabel =
                    ActionPresentationText.GetDestinationLabel(
                        presentation.Destination
                    );

                destination =
                    $" | destination={destinationLabel}";
            }

            string overreachWarning =
                presentation.ActivatesOverreach
                    ? " | EXHAUSTION ON COMMIT"
                    : string.Empty;

            string immediateMarker =
                presentation.IsImmediate
                    ? " | immediate"
                    : string.Empty;

            sb.AppendLine(
                $"    {controlLabel}: " +
                $"{presentation.Label} | " +
                $"{availability}" +
                $"{destination}" +
                $"{overreachWarning}" +
                $"{immediateMarker}"
            );
        }

        private static void AppendContextTargetSummary(
            StringBuilder sb,
            NetPlayerState state)
        {
            if (state == null)
                return;

            if (!state.IsServer &&
                !state.IsOwner)
            {
                sb.AppendLine(
                    "  Context target: owner-only"
                );

                return;
            }

            PlayerContextTargetSummary summary =
                state.ContextTargetSummaryValue;

            string targetName =
                summary.HasTarget
                    ? summary.DisplayName.ToString()
                    : "none";

            sb.AppendLine(
                $"  Context target: {targetName} | " +
                $"kind={summary.Kind} | " +
                $"canCycle={summary.CanCycle}"
            );
        }

        private static void AppendLatestCommandFeedback(
            StringBuilder sb,
            NetPlayerState state)
        {
            if (state == null)
                return;

            if (!state.IsServer &&
                !state.IsOwner)
            {
                sb.AppendLine(
                    "  Latest command: owner-only"
                );

                return;
            }

            PlayerCommandFeedback feedback =
                state.LatestCommandFeedbackValue;

            if (!feedback.HasValue)
            {
                sb.AppendLine(
                    "  Latest command: none"
                );

                return;
            }

            string message =
                feedback.Message.ToString();

            sb.AppendLine(
                $"  Latest command: #{feedback.Sequence} | " +
                $"{feedback.Command} | " +
                $"{feedback.Status}"
            );

            sb.AppendLine(
                $"    {message}"
            );
        }

    }


}
