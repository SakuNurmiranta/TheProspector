namespace SEMM91.GamePlay.Kvlt.Movement
{
    /// <summary>
    /// Identifies which authoritative point in the
    /// turn chronology a Canon breakthrough
    /// evaluation belongs to.
    ///
    /// PreMovement:
    ///     frozen state used by ordinary/breakthrough
    ///     movement.
    ///
    /// PostHappening:
    ///     fresh semantic state after existing-Field
    ///     movement and Happening settlement, used for
    ///     year-end canonization screening.
    /// </summary>
    public enum
        SceneReleaseCanonBreakthroughEvaluationPhase
    {
        PreMovement = 100,
        PostHappening = 200
    }
}