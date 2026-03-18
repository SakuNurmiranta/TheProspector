namespace SEMM91.UI
{
    public class RehearsalView : ContextualUIView
    {
        public override bool Supports(GameUIState state) => state == GameUIState.Rehearsal;

        public override void Refresh(UIContext context)
        {
            // TODO: Populate rehearsal UI
        }
    }
}