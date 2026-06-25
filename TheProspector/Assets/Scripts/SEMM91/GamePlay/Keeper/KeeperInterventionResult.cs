namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Immutable outcome of one Keeper intervention attempt.
    ///
    /// AppliedVisibilityDelta remains zero until the release
    /// effect policy is implemented in Step 9.7E.2.
    /// </summary>
    public readonly struct KeeperInterventionResult
    {
        public KeeperInterventionRequest
            Request { get; }

        public bool Succeeded { get; }

        public KeeperInterventionFailureReason
            FailureReason { get; }

        public float PullSpent { get; }
        public float PullRemaining { get; }

        public float AppliedVisibilityDelta
        {
            get;
        }

        private KeeperInterventionResult(
            KeeperInterventionRequest request,
            bool succeeded,
            KeeperInterventionFailureReason
                failureReason,
            float pullSpent,
            float pullRemaining,
            float appliedVisibilityDelta)
        {
            Request = request;
            Succeeded = succeeded;

            FailureReason =
                failureReason;

            PullSpent =
                KeeperPullRules.NormalizeAmount(
                    pullSpent
                );

            PullRemaining =
                KeeperPullRules.NormalizeAmount(
                    pullRemaining
                );

            AppliedVisibilityDelta =
                appliedVisibilityDelta;
        }

        public static KeeperInterventionResult
            Success(
                KeeperInterventionRequest request,
                float pullSpent,
                float pullRemaining,
                float appliedVisibilityDelta)
        {
            return new KeeperInterventionResult(
                request,
                succeeded: true,
                KeeperInterventionFailureReason.None,
                pullSpent,
                pullRemaining,
                appliedVisibilityDelta
            );
        }

        public static KeeperInterventionResult
            Failure(
                KeeperInterventionRequest request,
                KeeperInterventionFailureReason
                    failureReason,
                float pullRemaining)
        {
            return new KeeperInterventionResult(
                request,
                succeeded: false,
                failureReason,
                pullSpent: 0.0f,
                pullRemaining,
                appliedVisibilityDelta: 0.0f
            );
        }
    }
}