using SEMM91.Networking;

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
            NetPlayerState localPlayerState = null)
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
        }
    }
}