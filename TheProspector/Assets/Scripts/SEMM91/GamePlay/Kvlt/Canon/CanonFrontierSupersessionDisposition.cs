namespace SEMM91.GamePlay.Kvlt.Canon
{
    public enum
        CanonFrontierSupersessionDisposition
    {
        /*
         * Frontier degree equals both the turn-start
         * and post-settlement Canon degree.
         */
        RemainsCurrent = 100,

        /*
         * Frontier was current at turn start, but a
         * strictly greater same-polarity degree was
         * established during this settlement.
         *
         * Current-turn payout remains valid.
         */
        SupersededAfterSettlement = 200,

        /*
         * Frontier had already been exceeded before
         * this settlement began.
         */
        AlreadySuperseded = 300
    }
}