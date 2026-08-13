namespace SEMM91.GamePlay.Kvlt.TurnFlow
{
    /// <summary>
    /// Ordered authoritative phases belonging to one
    /// completed Peak-2 turn.
    ///
    /// Numeric ordering is intentional and may be
    /// used for inspection/logging, but runtime
    /// orchestration should consume an explicit
    /// KvltTurnChronologyPlan.
    /// </summary>
    public enum KvltTurnPhase
    {
        OrdinaryActions = 100,

        /*
         * Existing Field objects resolve their
         * turn-t movement/physics before the shared
         * Happening window.
         */
        ExistingFieldSettlement = 200,

        /*
         * Already-committed authoritative Happenings
         * become commonly available while preserving
         * any earlier interactions on those same
         * HappeningIds.
         */
        SharedHappeningWindow = 300,

        /*
         * Behaviors, Hails, Crisis, Beef/Poser,
         * activation, Pending/redemption and Fringe
         * fettering/failure settle here.
         */
        HappeningSettlement = 400,

        /*
         * YEAR END ONLY.
         *
         * Uses post-Happening state but cannot feed
         * back into already-completed Field physics.
         */
        YearEndCanonSettlement = 500,

        /*
         * Turn-t Gravity-bearing Score settles here.
         *
         * Newly canonized material can receive its
         * first retained payout here.
         *
         * Newly fettered Fringe material has not yet
         * ingressed and therefore receives no Field
         * Gravity for this completed turn.
         */
        TurnGravityScoreSettlement = 600,

        /*
         * YEAR END ONLY.
         *
         * Completed-year influence-bearing Gravity is
         * calculated after final turn-t Gravity.
         */
        YearInfluenceKeeperSuccession = 700,

        /*
         * YEAR END ONLY.
         *
         * If succession produced a new continuous
         * tenure, ending-tenure CanonRetained releases
         * become HistoricalCanon.
         *
         * Same incumbent / same tenure is a no-op.
         */
        KeeperTenureTransitionResolution = 800,

        /*
         * Start-of-t+1 admission for releases that
         * successfully fettered during the completed
         * turn's Happening settlement.
         */
        NextTurnIngress = 900,

        /*
         * Publish the fully settled next state and
         * causal outcome/read models.
         */
        NextStatePublication = 1000
    }
}