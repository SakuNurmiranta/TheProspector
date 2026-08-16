namespace SEMM91.GamePlay.Circulation
{
    public enum SceneReleaseLifecycleState
    {
        Fringe = 0,

        Field = 100,

        /*
         * Canonized artifact still resident during
         * the uninterrupted Keeper tenure under which
         * it canonized.
         */
        CanonRetained = 150,

        /*
         * Canonized artifact whose canonizing Keeper
         * tenure has ended.
         *
         * It is no longer resident in active Scene
         * Space, but all semantic, activation,
         * movement, Canon and Gravity provenance is
         * preserved.
         */
        HistoricalCanon = 175,

        FailedToFetter = 200,

        Rejected = 300
    }
}