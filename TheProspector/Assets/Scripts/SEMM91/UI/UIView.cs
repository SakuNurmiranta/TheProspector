using UnityEngine;

namespace SEMM91.UI
{
    public abstract class UIView : MonoBehaviour
    {
        [Header("UI View")] 
        [SerializeField] private string viewId;
        [SerializeField] private UILayerType layerType = UILayerType.Contextual;
        [SerializeField] private UIRoleScope roleScope = UIRoleScope.Any;
        
        protected bool IsVisible { get; private set; }
        protected bool IsInteractable { get; private set; }
        protected bool IsFocused { get; private set; }
        
        public string ViewId => viewId;
        public UILayerType LayerType => layerType;
        public UIRoleScope RoleScope => roleScope;

        public virtual void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
            IsVisible = false;
            IsInteractable = false;
            IsFocused = false;
        }

        public virtual void Activate()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            
            IsVisible = true;
            IsInteractable = true;
        }

        public virtual void DeActivate()
        {
            IsInteractable = false;
            IsFocused = false;
        }

        public virtual void EnterFocus()
        {
            IsFocused = true;
        }

        public virtual void ExitFocus()
        {
            IsFocused = false;
        }

        public virtual bool Supports(GameUIState state)
        {
            return false;
        }

        public virtual bool SupportsRole(bool isKeeper)
        {
            return roleScope switch
            {
                UIRoleScope.Any => true,
                UIRoleScope.RegularOnly => !isKeeper,
                UIRoleScope.KeeperOnly => isKeeper,
                _ => true
            };
        }

        public abstract void Refresh(UIContext context);

    }
}