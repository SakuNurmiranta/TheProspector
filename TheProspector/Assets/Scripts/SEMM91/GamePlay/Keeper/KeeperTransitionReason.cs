namespace SEMM91.GamePlay.Keeper
{
    public enum KeeperTransitionReason : byte
    {
        None = 0,
        InitialAssignment = 1, // needs randomization logic and a virtual demoTape from each player
        YearEndRetained = 2, // should be kinda hard
        YearEndReplaced = 3, // most likely outcome
        DisconnectionFallback = 4, // these should probably look for past Keepers first - this is a problematic case
        SceneCollapseLocked = 5 // the last years play out without a Keeper change in the end
    }
}