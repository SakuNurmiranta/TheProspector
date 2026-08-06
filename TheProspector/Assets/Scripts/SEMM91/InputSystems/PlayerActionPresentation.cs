using SEMM91.GamePlay.Actions;

namespace SEMM91.InputSystems
{
    public enum ActionPlanDestination
    {
        None = 0,
        Standard1 = 1,
        Standard2 = 2,
        Overreach = 3
    }

    public enum ActionUnavailableReason
    {
        None = 0,

        MissingPlayerState,
        MissingCoordinator,
        PlayerInactive,
        KeeperRoleRestricted,
        TurnAlreadyCommitted,

        NoStanceSelected,
        DraftAlreadyStarted,
        PlanFull,
        DraftEmpty,
        EmptyTrackAlreadyExists,

        NoActionAssigned,
        ActionInvalidForStance,

        DreamUnavailable,

        TargetCyclingUnsupported,
        NoSelectableTarget,
        MissingActiveRehearsalSet,
        ContextualCreationUnavailable,
        MissingActingEntity,
        MissingCurrentTrack,
        NoAvailableIdeas,

        MissingNetworkManager,
        HostOnly,
        SessionAlreadyStarted,

        DebugOnly
    }

    public readonly struct PlayerActionPresentation
    {
        public PlayerCommand Command { get; }

        public string Label { get; }

        public bool HasActionType { get; }

        public DraftedActionType ActionType { get; }

        public bool IsAvailable { get; }

        public ActionUnavailableReason UnavailableReason { get; }

        public ActionPlanDestination Destination { get; }

        public bool IsImmediate { get; }

        public bool ActivatesOverreach =>
            Destination ==
            ActionPlanDestination.Overreach;

        public int DestinationPosition =>
            (int)Destination;

        public string UnavailableReasonText =>
            ActionPresentationText.GetReasonText(
                UnavailableReason
            );

        private PlayerActionPresentation(
            PlayerCommand command,
            string label,
            bool hasActionType,
            DraftedActionType actionType,
            bool isAvailable,
            ActionUnavailableReason unavailableReason,
            ActionPlanDestination destination,
            bool isImmediate)
        {
            Command = command;
            Label = label;
            HasActionType = hasActionType;
            ActionType = actionType;
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason;
            Destination = destination;
            IsImmediate = isImmediate;
        }

        public static PlayerActionPresentation Available(
            PlayerCommand command,
            string label,
            DraftedActionType actionType =
                DraftedActionType.None,
            bool hasActionType = false,
            ActionPlanDestination destination =
                ActionPlanDestination.None,
            bool isImmediate = false)
        {
            return new PlayerActionPresentation(
                command,
                label,
                hasActionType,
                actionType,
                true,
                ActionUnavailableReason.None,
                destination,
                isImmediate
            );
        }

        public static PlayerActionPresentation Blocked(
            PlayerCommand command,
            string label,
            ActionUnavailableReason reason,
            DraftedActionType actionType =
                DraftedActionType.None,
            bool hasActionType = false)
        {
            return new PlayerActionPresentation(
                command,
                label,
                hasActionType,
                actionType,
                false,
                reason,
                ActionPlanDestination.None,
                false
            );
        }
    }

    public static class ActionPresentationText
    {
        public static string GetActionLabel(
            DraftedActionType actionType)
        {
            return actionType switch
            {
                DraftedActionType.CreateIdea =>
                    "Create Idea",

                DraftedActionType
                    .DebugPlaceholderGestationSecondary =>
                    "Gestation Secondary Placeholder",

                DraftedActionType.RehearseActiveSet =>
                    "Rehearse Active Set",

                DraftedActionType.RecordActiveSetToDemo =>
                    "Record Active Set to Demo",

                DraftedActionType.ReleaseLatestDemoToKvlt =>
                    "Release Latest Demo to KVLT",

                DraftedActionType
                    .DebugPlaceholderPromotionPrimary =>
                    "Promotion Primary Placeholder",

                DraftedActionType
                    .DebugPlaceholderPromotionSecondary =>
                    "Promotion Secondary Placeholder",

                DraftedActionType.Rest =>
                    "Recover",

                DraftedActionType.Wait =>
                    "Wait",

                DraftedActionType.None =>
                    "No Action",

                _ => actionType.ToString()
            };
        }

        public static string GetCommandLabel(
            PlayerCommand command)
        {
            return command switch
            {
                PlayerCommand.SelectGestate =>
                    "Select Gestate",

                PlayerCommand.SelectRehearse =>
                    "Select Rehearse",

                PlayerCommand.SelectPromote =>
                    "Select Promote",
                
                PlayerCommand.ReturnToStanceSelection =>
                    "Change Stance",

                PlayerCommand.Dream =>
                    "Dream",

                PlayerCommand.KeeperBoostVisibility =>
                    "Boost Visibility",

                PlayerCommand.KeeperSuppressVisibility =>
                    "Suppress Visibility",
                
                PlayerCommand.DraftAction =>
                    "Draft Current Action",

                PlayerCommand.DraftPrimaryAction =>
                    "Primary Action",

                PlayerCommand.DraftSecondaryAction =>
                    "Secondary Action",

                PlayerCommand.DraftTertiaryAction =>
                    "Tertiary Action",

                PlayerCommand.DraftRestAction =>
                    "Finish and Recover",

                PlayerCommand.UndoDraftAction =>
                    "Undo Last Action",

                PlayerCommand.CommitTurn =>
                    "Commit Turn",

                PlayerCommand.CycleTarget =>
                    "Cycle Target",

                PlayerCommand
                    .CreateEmptyRehearsalSet =>
                    "Create Empty Rehearsal Set (Debug)",
                
                PlayerCommand.CreateNewTrack =>
                    "Create New Track", 
                
                PlayerCommand.ContextualCreate =>
                    "Create",

                PlayerCommand.ForceStartSession =>
                    "Force Start Session",

                PlayerCommand.QuitSession =>
                    "Quit Session",
                
                PlayerCommand.AddIdeaToCurrentTrack =>
                    "Add Idea to Current Track",

                _ => command.ToString()
            };
        }

        public static string GetReasonText(
            ActionUnavailableReason reason)
        {
            return reason switch
            {
                ActionUnavailableReason.None =>
                    string.Empty,

                ActionUnavailableReason.MissingPlayerState =>
                    "Player state is unavailable.",

                ActionUnavailableReason.MissingCoordinator =>
                    "The gameplay coordinator is unavailable.",

                ActionUnavailableReason.PlayerInactive =>
                    "The player is not active in the current session.",

                ActionUnavailableReason.TurnAlreadyCommitted =>
                    "The turn has already been committed.",

                ActionUnavailableReason.NoStanceSelected =>
                    "Select a stance first.",

                ActionUnavailableReason.DraftAlreadyStarted =>
                    "The stance cannot be changed after drafting.",

                ActionUnavailableReason.PlanFull =>
                    "The plan already contains two standard actions and Overreach.",

                ActionUnavailableReason.DraftEmpty =>
                    "There is no drafted action to undo.",

                ActionUnavailableReason.NoActionAssigned =>
                    "No action is assigned to this control.",

                ActionUnavailableReason.ActionInvalidForStance =>
                    "This action does not belong to the selected stance.",

                ActionUnavailableReason.DreamUnavailable =>
                    "Dream is unavailable this turn.",

                ActionUnavailableReason
                    .TargetCyclingUnsupported =>
                    "The selected stance has no contextual target.",

                ActionUnavailableReason.NoSelectableTarget =>
                    "No valid target is currently available.",
                
                ActionUnavailableReason
                        .MissingActiveRehearsalSet =>
                    "No active rehearsal set is selected.",

                ActionUnavailableReason
                        .ContextualCreationUnavailable =>
                    "Contextual creation is not implemented for this stance.",

                ActionUnavailableReason
                    .MissingNetworkManager =>
                    "The network session is unavailable.",

                ActionUnavailableReason.HostOnly =>
                    "Only the listen host may use this command.",

                ActionUnavailableReason
                    .SessionAlreadyStarted =>
                    "The playable session has already started.",

                ActionUnavailableReason.DebugOnly =>
                    "This is a debug-only command.",
                
                ActionUnavailableReason.KeeperRoleRestricted =>
                    "The Keeper cannot perform regular band production actions.",
                
                ActionUnavailableReason.MissingActingEntity =>
                    "The player has no acting entity.",

                ActionUnavailableReason.MissingCurrentTrack =>
                    "Create a new Track before adding Ideas.",

                ActionUnavailableReason.NoAvailableIdeas =>
                    "The player has no available Ideas.",
                
                ActionUnavailableReason.EmptyTrackAlreadyExists =>
                    "Finish the empty Track before creating another.",

                _ =>
                    "The command is unavailable."
            };
        }

        public static string GetDestinationLabel(
            ActionPlanDestination destination)
        {
            return destination switch
            {
                ActionPlanDestination.Standard1 =>
                    "Standard 1",

                ActionPlanDestination.Standard2 =>
                    "Standard 2",

                ActionPlanDestination.Overreach =>
                    "Overreach",

                _ =>
                    "None"
            };
        }
    }
}