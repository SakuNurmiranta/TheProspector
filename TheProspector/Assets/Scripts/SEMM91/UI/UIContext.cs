using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;

namespace SEMM91.UI
{
    public readonly struct UIContext
    {
        public readonly int CurrentTurn;
        public readonly int CurrentRound;
        public readonly ulong KeeperClientId;
        public readonly ulong LocalClientId;
        public readonly bool IsKeeper;
        public readonly bool HasLocalLeaderSnapshot;
        public readonly bool LocalLeaderIsExhausted;
        public readonly GameUIState ActiveState;
        public readonly bool HasLocalInventorySnapshot;

        public readonly bool
            HasKeeperInterventionSnapshot;

        private readonly
            DomainSnapshotReplicator
            .KeeperInterventionDebugSnapshot
            _keeperInterventionSnapshot;

        public DomainSnapshotReplicator
            .KeeperInterventionDebugSnapshot
            KeeperInterventionSnapshot =>
            _keeperInterventionSnapshot;

        private readonly DomainSnapshotReplicator
            _snapshotReplicator;

        public int KeeperReleaseCount
        {
            get
            {
                if (!HasKeeperInterventionSnapshot ||
                    _snapshotReplicator == null ||
                    _snapshotReplicator
                        .KeeperReleaseRows == null)
                {
                    return 0;
                }

                return _snapshotReplicator
                    .KeeperReleaseRows.Count;
            }
        }

        public bool TryGetKeeperReleaseRow(
            int index,
            out DomainSnapshotReplicator
                .KeeperReleaseDebugRow row)
        {
            row = default;

            if (!HasKeeperInterventionSnapshot ||
                _snapshotReplicator == null ||
                _snapshotReplicator
                    .KeeperReleaseRows == null ||
                index < 0 ||
                index >= _snapshotReplicator
                    .KeeperReleaseRows.Count)
            {
                return false;
            }

            row =
                _snapshotReplicator
                    .KeeperReleaseRows[index];

            return true;
        }
        
        private readonly
            DomainSnapshotReplicator.PlayerInventoryDebugRow
            _localInventorySnapshot;

        public DomainSnapshotReplicator.PlayerInventoryDebugRow
            LocalInventorySnapshot =>
            _localInventorySnapshot;
        
        private readonly NetPlayerState _localPlayerState;
        public readonly GameCoordinator.Season CurrentSeason;
        public bool HasLocalPlayerState =>
            _localPlayerState != null;
        
        public NetPlayerState LocalPlayerState =>
        _localPlayerState;

        public UIContext(
            int currentTurn,
            int currentRound,
            GameCoordinator.Season currentSeason,
            ulong keeperClientId,
            ulong localClientId,
            bool isKeeper,
            GameUIState activeState,
            bool hasLocalLeaderSnapshot,
            bool localLeaderIsExhausted,
            NetPlayerState localPlayerState = null,
            bool hasLocalInventorySnapshot = false,
            DomainSnapshotReplicator.PlayerInventoryDebugRow
                localInventorySnapshot = default,
            bool hasKeeperInterventionSnapshot = false,
            DomainSnapshotReplicator
                .KeeperInterventionDebugSnapshot
                keeperInterventionSnapshot = default,
            DomainSnapshotReplicator
                snapshotReplicator = null)
        {
            CurrentTurn = currentTurn;
            CurrentRound = currentRound;
            CurrentSeason = currentSeason;
            KeeperClientId = keeperClientId;
            LocalClientId = localClientId;
            IsKeeper = isKeeper;
            ActiveState = activeState;
            HasLocalLeaderSnapshot = hasLocalLeaderSnapshot;
            LocalLeaderIsExhausted = localLeaderIsExhausted;
            _localPlayerState = localPlayerState;
            HasLocalInventorySnapshot =
                hasLocalInventorySnapshot;

            _localInventorySnapshot =
                localInventorySnapshot;
            
            HasKeeperInterventionSnapshot =
                hasKeeperInterventionSnapshot;

            _keeperInterventionSnapshot =
                keeperInterventionSnapshot;

            _snapshotReplicator =
                snapshotReplicator;
        }
    }
}