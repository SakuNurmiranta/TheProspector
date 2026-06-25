namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// Immutable request to apply one Keeper intervention
    /// to one release already present in scene space.
    /// </summary>
    public readonly struct KeeperInterventionRequest
    {
        public ulong KeeperClientId { get; }
        public string ReleaseId { get; }

        public KeeperInterventionType
            InterventionType { get; }

        public int RequestedTurn { get; }
        public float PullSpend { get; }

        public KeeperInterventionRequest(
            ulong keeperClientId,
            string releaseId,
            KeeperInterventionType interventionType,
            int requestedTurn,
            float pullSpend)
        {
            KeeperClientId =
                keeperClientId;

            ReleaseId =
                releaseId ?? string.Empty;

            InterventionType =
                interventionType;

            RequestedTurn =
                requestedTurn;

            PullSpend =
                KeeperPullRules.NormalizeAmount(
                    pullSpend
                );
        }
    }
}