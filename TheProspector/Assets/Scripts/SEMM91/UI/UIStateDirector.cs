using System.Collections.Generic;
using System.Net;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Build;
using UnityEngine;

namespace SEMM91.UI
{
    public class UIStateDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StageCameraController stageCameraController;
        [SerializeField] private List<PersistentUIView> persistentViews = new();
        [SerializeField] private List<ContextualUIView> contextualViews = new();
        
        [Header("State")]
        [SerializeField] private GameUIState activeState = GameUIState.MainMap;

        private ContextualUIView _activeContextualView;
        
        public GameUIState ActiveState => activeState;

        private void Start()
        {
            InitializeViews();
            SetState(activeState);
        }

        public void SetState(GameUIState newState)
        {
            activeState = newState;

            var context = BuildContext();

            RefreshPersistentViews(context);
            SetActiveContextualView(newState, context);
            
            stageCameraController?.Focus(newState);
        }

        public void RefreshAll()
        {
            var context = BuildContext();
            RefreshPersistentViews(context);

            if (_activeContextualView != null) _activeContextualView.Refresh(context);
        }

        private void InitializeViews()
        {
            foreach (var view in persistentViews)
            {
                if (view == null) continue;
                view.Show();
                view.Activate();
            }

            foreach (var view in contextualViews)
            {
                if (view == null) continue;
                view.Hide();
            }
            
        }

        private void RefreshPersistentViews(UIContext context)
        {
            foreach (var view in persistentViews)
            {
                if (view == null) continue;

                if (!view.SupportsRole(context.IsKeeper))
                {
                    view.Hide();
                    continue;
                }

                view.Show();
                view.Activate();
                view.Refresh(context);
            }
        }

        private void SetActiveContextualView(GameUIState state, UIContext context)
        {
            if (_activeContextualView != null)
            {
                _activeContextualView.ExitFocus();
                _activeContextualView.DeActivate();
                _activeContextualView.Hide();
                _activeContextualView = null;
            }

            foreach (var view in contextualViews)
            {
                if (view == null) continue;
                if (!view.Supports(state)) continue;
                if (!view.SupportsRole(context.IsKeeper)) continue;
                
                _activeContextualView = view;
                _activeContextualView.Show();
                _activeContextualView.Activate();
                _activeContextualView.EnterFocus();
                _activeContextualView.Refresh(context);
                return;

            }
        }

        private UIContext BuildContext()
        {
            var coordinator = SEMM91.GameCoordinator.Instance;
            
            int turn = coordinator != null ? coordinator.globalTurn.Value : 0;
            int round = coordinator != null ? coordinator.roundIndex.Value : 0;
            ulong keeper = coordinator != null ? coordinator.keeperClientId.Value : ulong.MaxValue;
            
            ulong localClientId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;

            bool isKeeper = localClientId == keeper;

            return new UIContext(
                currentTurn: turn,
                currentRound: round,
                keeperClientId: keeper,
                localClientId: localClientId,
                isKeeper: isKeeper,
                activeState: activeState);
        }
    }
}