namespace SEMM91.UI
{
    public struct UIContext
    {
        public int CurrentTurn;
        public int CurrentRound;
        public ulong KeeperClientId;
        public ulong LocalClientId;
        public bool IsKeeper;
        public GameUIState ActiveState;

        public UIContext(
            int currentTurn,
            int currentRound,
            ulong keeperClientId,
            ulong localClientId,
            bool isKeeper,
            GameUIState activeState)
        {
            CurrentTurn = currentTurn;
            CurrentRound = currentRound;
            KeeperClientId = keeperClientId;
            LocalClientId = localClientId;
            IsKeeper = isKeeper;
            ActiveState = activeState;
        }
    }
}