namespace SEMM91.GamePlay.Kvlt.Scenario
{
    public enum Peak2SessionStartGateStatus : byte
    {
        WaitingForParticipants = 0,
        WaitingForClassification = 1,
        WaitingForReadiness = 2,
        InvalidParticipantCount = 3,
        InvalidRoleComposition = 4,
        InvalidRosterState = 5,
        Ready = 6,
        Started = 7,
        BootstrapFailed = 8
    }

    public readonly struct Peak2SessionRosterCounts
    {
        public int ConnectedPlayers { get; }
        public int ReadyPlayers { get; }
        public int HumanPlayers { get; }
        public int BotPlayers { get; }

        public int ClassifiedPlayers =>
            HumanPlayers + BotPlayers;

        public Peak2SessionRosterCounts(
            int connectedPlayers,
            int readyPlayers,
            int humanPlayers,
            int botPlayers)
        {
            ConnectedPlayers = connectedPlayers;
            ReadyPlayers = readyPlayers;
            HumanPlayers = humanPlayers;
            BotPlayers = botPlayers;
        }
    }

    /// <summary>
    /// Pure policy for deciding when the closed Peak-2 scenario may start.
    /// Connection callbacks may update the displayed roster, but only a fully
    /// classified and ready canonical roster is allowed through this gate.
    /// </summary>
    public static class Peak2SessionStartGate
    {
        public static Peak2SessionStartGateStatus Evaluate(
            int requiredPlayers,
            Peak2SessionRosterCounts roster,
            bool allowSoloDevelopmentMode)
        {
            if (requiredPlayers <= 0 ||
                roster.ConnectedPlayers < 0 ||
                roster.ReadyPlayers < 0 ||
                roster.HumanPlayers < 0 ||
                roster.BotPlayers < 0 ||
                roster.ReadyPlayers >
                roster.ConnectedPlayers ||
                roster.ClassifiedPlayers >
                roster.ConnectedPlayers)
            {
                return Peak2SessionStartGateStatus
                    .InvalidRosterState;
            }

            if (allowSoloDevelopmentMode)
            {
                return roster.ConnectedPlayers >= 1
                    ? Peak2SessionStartGateStatus.Ready
                    : Peak2SessionStartGateStatus
                        .WaitingForParticipants;
            }

            if (roster.ConnectedPlayers < requiredPlayers)
            {
                return Peak2SessionStartGateStatus
                    .WaitingForParticipants;
            }

            if (roster.ConnectedPlayers > requiredPlayers)
            {
                return Peak2SessionStartGateStatus
                    .InvalidParticipantCount;
            }

            if (roster.ClassifiedPlayers <
                roster.ConnectedPlayers)
            {
                return Peak2SessionStartGateStatus
                    .WaitingForClassification;
            }

            if (roster.ReadyPlayers <
                roster.ConnectedPlayers)
            {
                return Peak2SessionStartGateStatus
                    .WaitingForReadiness;
            }

            int requiredBotCount =
                requiredPlayers - 1;

            if (roster.HumanPlayers != 1 ||
                roster.BotPlayers != requiredBotCount)
            {
                return Peak2SessionStartGateStatus
                    .InvalidRoleComposition;
            }

            return Peak2SessionStartGateStatus.Ready;
        }
    }
}
