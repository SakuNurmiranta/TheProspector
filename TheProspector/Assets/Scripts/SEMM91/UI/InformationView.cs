namespace SEMM91.UI
{
    public class InformationView : ContextualUIView
    {
        public override bool Supports(GameUIState state) => state == GameUIState.Information;

        public override void Refresh(UIContext context)
        {
            // TODO: Populate information UI
        }
    }
}