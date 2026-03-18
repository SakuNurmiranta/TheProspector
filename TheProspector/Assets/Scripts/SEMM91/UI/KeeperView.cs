namespace SEMM91.UI
{
    public class KeeperView : ContextualUIView
    {
        public override bool Supports(GameUIState state) => state == GameUIState.Keeper;

        public override void Refresh(UIContext context)
        {
            // TODO: Populate keeper-only UI
        }
    }
}