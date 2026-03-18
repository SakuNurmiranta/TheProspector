namespace SEMM91.UI
{
    public abstract class ContextualUIView : UIView
    {
        public override void Show()
        {
            base.Show();
            DeActivate();
        }
        
    }
}