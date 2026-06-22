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
        public readonly GameUIState ActiveState;

        private readonly NetPlayerState _localPlayerState;
        
        public bool HasLocalPlayerState =>
            _localPlayerState != null;
        
        public NetPlayerState LocalPlayerState =>
        _localPlayerState;

        public UIContext(
            int currentTurn,
            int currentRound,
            ulong keeperClientId,
            ulong localClientId,
            bool isKeeper,
            GameUIState activeState,
            NetPlayerState localPlayerState = null)
        {
            CurrentTurn = currentTurn;
            CurrentRound = currentRound;
            KeeperClientId = keeperClientId;
            LocalClientId = localClientId;
            IsKeeper = isKeeper;
            ActiveState = activeState;
            _localPlayerState = localPlayerState;
        }
    }
}