namespace SEMM91.GamePlay.Kvlt.TurnFlow
{
    /// <summary>
    /// Runtime orchestration state for one authoritative
    /// Peak-2 turn closure.
    ///
    /// Domain truth remains in SeededWorldState.
    /// This enum only tells GameCoordinator which
    /// chronology checkpoint may execute next.
    /// </summary>
    public enum KvltTurnResolutionRuntimePhase
    {
        Idle = 0,

        ExistingFieldSettled = 100,

        SharedHappeningWindowOpen = 150,

        HappeningPrepared = 200,

        AwaitingAllegianceCrisisResolution = 300,

        HappeningSettled = 400,

        YearEndCanonSettled = 500,

        TurnScoreSettled = 600,

        YearEndSuccessionSettled = 700,

        TenureTransitionSettled = 800,

        StandingSettled = 900,

        NextTurnPlacementSettled = 1000,

        NextSceneEnvironmentSettled = 1100,

        Published = 1200
    }
}