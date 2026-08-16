using SEMM91.GamePlay.Kvlt.TurnFlow;
using SEMM91.InputSystems;

namespace SEMM91.UI
{
    /// <summary>
    /// Maps the authoritative Peak-2 turn-closure phase onto
    /// the action buttons that already exist in every gameplay
    /// screen. This keeps runtime interaction routing independent
    /// from scene-specific Inspector wiring.
    /// </summary>
    public readonly struct Peak2TurnInteractionActionSet
    {
        public bool OverridesTurnPlan { get; }
        public PlayerCommand PrimaryCommand { get; }
        public PlayerCommand SecondaryCommand { get; }
        public bool HasTertiaryCommand { get; }
        public PlayerCommand TertiaryCommand { get; }
        public string WaitingLabel { get; }

        private Peak2TurnInteractionActionSet(
            bool overridesTurnPlan,
            PlayerCommand primaryCommand,
            PlayerCommand secondaryCommand,
            bool hasTertiaryCommand,
            PlayerCommand tertiaryCommand,
            string waitingLabel)
        {
            OverridesTurnPlan = overridesTurnPlan;
            PrimaryCommand = primaryCommand;
            SecondaryCommand = secondaryCommand;
            HasTertiaryCommand = hasTertiaryCommand;
            TertiaryCommand = tertiaryCommand;
            WaitingLabel = waitingLabel ?? string.Empty;
        }

        public static Peak2TurnInteractionActionSet Resolve(
            KvltTurnResolutionRuntimePhase phase)
        {
            switch (phase)
            {
                case KvltTurnResolutionRuntimePhase
                    .SharedHappeningWindowOpen:
                    return new Peak2TurnInteractionActionSet(
                        true,
                        PlayerCommand.HailSatan,
                        PlayerCommand.HailOdin,
                        false,
                        PlayerCommand.DraftTertiaryAction,
                        "HAPPENING WINDOW OPEN — HAIL NOW"
                    );

                case KvltTurnResolutionRuntimePhase
                    .AwaitingAllegianceCrisisResolution:
                    return new Peak2TurnInteractionActionSet(
                        true,
                        PlayerCommand.VoteSociety,
                        PlayerCommand.VoteKvlt,
                        false,
                        PlayerCommand.DraftTertiaryAction,
                        "ALLEGIANCE CRISIS — VOTE UNTIL RESOLVED"
                    );

                default:
                    return new Peak2TurnInteractionActionSet(
                        false,
                        PlayerCommand.DraftPrimaryAction,
                        PlayerCommand.DraftSecondaryAction,
                        true,
                        PlayerCommand.DraftTertiaryAction,
                        string.Empty
                    );
            }
        }
    }
}
