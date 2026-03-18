namespace SEMM91.UI
{
    public abstract class PersistentUIView : UIView
    {
        public override void Activate()
        {
            base.Activate();
        }

        public override bool Supports(GameUIState state)
        {
            return true;
        }
    }
}