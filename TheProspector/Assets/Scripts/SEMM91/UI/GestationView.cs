namespace SEMM91.UI
{
    public class GestationView : ContextualUIView
    {
        public override bool Supports(GameUIState state) => state == GameUIState.Gestation;

        public override void Refresh(UIContext context)
        {
            // TODO: Populate gestation UI
        }
    }
}