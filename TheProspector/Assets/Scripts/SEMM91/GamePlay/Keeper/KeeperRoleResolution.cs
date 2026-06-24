namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Result of applying the institutional role consequences
    /// of a classified Keeper transition.
    ///
    /// This does not duplicate Keeper identity. The transition
    /// result remains the authoritative description of who entered
    /// or left office.
    /// </summary>
    public readonly struct KeeperRoleResolution
    {
        public KeeperTransitionResult
            TransitionResult { get; }

        public bool Succeeded { get; }

        public bool OutgoingRoleReleased
        {
            get;
        }

        public bool IncomingRoleApplied
        {
            get;
        }

        public string FailureReason { get; }

        private KeeperRoleResolution(
            KeeperTransitionResult transitionResult,
            bool succeeded,
            bool outgoingRoleReleased,
            bool incomingRoleApplied,
            string failureReason)
        {
            TransitionResult =
                transitionResult;

            Succeeded =
                succeeded;

            OutgoingRoleReleased =
                outgoingRoleReleased;

            IncomingRoleApplied =
                incomingRoleApplied;

            FailureReason =
                failureReason ?? string.Empty;
        }

        public static KeeperRoleResolution Success(
            KeeperTransitionResult transitionResult,
            bool outgoingRoleReleased,
            bool incomingRoleApplied)
        {
            return new KeeperRoleResolution(
                transitionResult,
                succeeded: true,
                outgoingRoleReleased,
                incomingRoleApplied,
                failureReason: string.Empty
            );
        }

        public static KeeperRoleResolution Failure(
            KeeperTransitionResult transitionResult,
            string failureReason)
        {
            return new KeeperRoleResolution(
                transitionResult,
                succeeded: false,
                outgoingRoleReleased: false,
                incomingRoleApplied: false,
                failureReason
            );
        }
    }
}