namespace SEMM91.GamePlay.Keeper
{
    public enum KeeperInterventionFailureReason : byte
    {
        None = 0,
        InvalidKeeper = 1,
        InvalidRelease = 2,
        InvalidTurn = 3,
        InvalidPullSpend = 4,
        UnsupportedIntervention = 5,
        NotCurrentKeeper = 6,
        MissingTenure = 7,
        MissingRelease = 8,
        InsufficientPull = 9,
        AlreadyIntervenedThisTurn = 10
    }
}