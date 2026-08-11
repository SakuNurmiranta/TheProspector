namespace SEMM91.GamePlay.Circulation
{
    public enum SceneReleaseLifecycleState
    {
        Fringe = 0,

        Field = 100,

        /*
         * Canonized Peak-2 artifact retained in
         * Scene Space for the remainder of the
         * continuous Keeper tenure under which it
         * canonized.
         *
         * It is no longer an ordinary Field
         * competitor.
         */
        CanonRetained = 150,

        FailedToFetter = 200,

        Rejected = 300
    }
}