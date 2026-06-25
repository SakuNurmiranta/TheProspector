using System.Collections.Generic;
using Unity.Netcode;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
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

        private bool _hasObservedLocalRole;
        private bool _wasKeeper;
        
        private void Start()
        {
            InitializeViews();
            SetState(activeState);
        }

        private void Update()
        {
            //Debug.Log("UIStateDirector Update");
            RefreshAll();
        }

        public void SetState(GameUIState newState)
        {
            activeState = newState;

            var context = BuildContext();

            RefreshPersistentViews(context);
            SetActiveContextualView(newState, context);
            
            stageCameraController?.Focus(newState);
        }

        private void RefreshAll()
        {
            UIContext context = BuildContext();

            if (TryHandleKeeperRoleTransition(context))
                return;

            RefreshPersistentViews(context);

            if (_activeContextualView != null)
            {
                _activeContextualView.Refresh(context);
            }
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
            bool hasKeeperInterventionSnapshot =
                false;

            DomainSnapshotReplicator
                .KeeperInterventionDebugSnapshot
                keeperInterventionSnapshot =
                    default;
            
            GameCoordinator coordinator =
                GameCoordinator.Instance;

            int turn =
                coordinator != null
                    ? coordinator.globalTurn.Value
                    : 0;

            int round =
                coordinator != null
                    ? coordinator.roundIndex.Value
                    : 0;

            GameCoordinator.Season season =
                coordinator != null
                    ? coordinator.CurrentSeason
                    : GameCoordinator.Season.Spring;
            
            ulong keeper =
                coordinator != null
                    ? coordinator.keeperClientId.Value
                    : ulong.MaxValue;

            NetworkManager networkManager =
                NetworkManager.Singleton;

            ulong localClientId =
                networkManager != null
                    ? networkManager.LocalClientId
                    : ulong.MaxValue;

            bool hasLocalLeaderSnapshot = false;
            bool localLeaderIsExhausted = false;

            bool hasLocalInventorySnapshot = false;

            DomainSnapshotReplicator.PlayerInventoryDebugRow
                localInventorySnapshot = default;
            
            DomainSnapshotReplicator snapshotReplicator =
                DomainSnapshotReplicator.Instance;

            if (snapshotReplicator != null &&
                snapshotReplicator.IsSnapshotNetworkReady &&
                localClientId != ulong.MaxValue)
            {
                hasKeeperInterventionSnapshot =
                    true;

                keeperInterventionSnapshot =
                    snapshotReplicator
                        .KeeperInterventionState.Value;
                
                for (int i = 0;
                     i < snapshotReplicator.PlayerInventoryRows.Count;
                     i++)
                {
                    DomainSnapshotReplicator.PlayerInventoryDebugRow row =
                        snapshotReplicator.PlayerInventoryRows[i];

                    if (row.ClientId != localClientId)
                        continue;

                    hasLocalInventorySnapshot = true;
                    localInventorySnapshot = row;

                    hasLocalLeaderSnapshot = true;
                    localLeaderIsExhausted =
                        row.LeaderIsExhausted;

                    break;
                }
            }
            
            NetPlayerState localPlayerState = null;

            if (networkManager != null &&
                networkManager.IsListening &&
                networkManager.LocalClient != null)
            {
                NetworkObject localPlayerObject =
                    networkManager.LocalClient.PlayerObject;

                if (localPlayerObject != null)
                {
                    localPlayerObject.TryGetComponent(
                        out localPlayerState
                    );
                }
            }

            bool isKeeper =
                localClientId != ulong.MaxValue &&
                keeper != ulong.MaxValue &&
                localClientId == keeper;

            return new UIContext(
                currentTurn: turn,
                currentRound: round,
                currentSeason: season,
                keeperClientId: keeper,
                localClientId: localClientId,
                isKeeper: isKeeper,
                activeState: activeState,
                hasLocalLeaderSnapshot:
                hasLocalLeaderSnapshot,
                localLeaderIsExhausted:
                localLeaderIsExhausted,
                localPlayerState: localPlayerState,
                hasLocalInventorySnapshot:
                hasLocalInventorySnapshot,
                hasKeeperInterventionSnapshot:
                hasKeeperInterventionSnapshot,

                keeperInterventionSnapshot:
                keeperInterventionSnapshot,

                snapshotReplicator:
                hasKeeperInterventionSnapshot
                    ? snapshotReplicator
                    : null,
                localInventorySnapshot:
                localInventorySnapshot
                
            );
        } 
        private bool TryHandleKeeperRoleTransition(
            UIContext context)
        {
            /*
             * Do not establish the local role before the
             * player-owned network state exists.
             */
            if (context.LocalPlayerState == null)
                return false;

            if (!_hasObservedLocalRole)
            {
                _hasObservedLocalRole = true;
                _wasKeeper = context.IsKeeper;

                if (context.IsKeeper &&
                    activeState != GameUIState.Keeper)
                {
                    SetState(GameUIState.Keeper);
                    return true;
                }

                return false;
            }

            if (_wasKeeper == context.IsKeeper)
                return false;

            _wasKeeper = context.IsKeeper;

            SetState(
                context.IsKeeper
                    ? GameUIState.Keeper
                    : GameUIState.MainMap
            );

            return true;
        }
    }
}