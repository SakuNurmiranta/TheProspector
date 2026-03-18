namespace SEMM91.UI
{
    public class PromotionView : ContextualUIView
    {
        public override bool Supports(GameUIState state) => state == GameUIState.Promotion;

        public override void Refresh(UIContext context)
        {
            // TODO: Populate promotion UI
        }
    }
}