using System;
using System.Collections;
using SEMM91.GamePlay.Actions;
using SEMM91.Networking;
using SEMM91.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SceneFlowController :
        MonoBehaviour
    {
        private const string StanceSelectionSceneName =
            "StanceSelection";

        private const string GestationSceneName =
            "Gestation";

        private const string RehearsalSceneName =
            "Rehearsal";

        private const string PromotionSceneName =
            "Promotion";

        private const string KeeperSceneName =
            "Keeper";

        private static readonly string[]
            ContextualSceneNames =
            {
                StanceSelectionSceneName,
                GestationSceneName,
                RehearsalSceneName,
                PromotionSceneName,
                KeeperSceneName
            };

        private UIStateDirector _uiStateDirector;
        private GameCoordinator _coordinator;
        private NetPlayerState _localPlayerState;

        private Coroutine _reconcileRoutine;
        private bool _reconcileRequested;

        public event Action<int, int, GameUIState>
            SameSceneTurnAdvanced;

        private string _currentContextualSceneName;

        private bool _hasPendingTurnBoundary;
        private int _pendingPreviousTurn;
        private int _pendingNewTurn;
        private string _sceneNameAtTurnBoundary;
        
        private void Start()
        {
            if (!GameplayShellReferences.TryGet(
                    out GameplayShellReferences shell))
            {
                Fail(
                    "SceneFlowController could not locate " +
                    "a ready GameplayShellReferences."
                );

                return;
            }

            _uiStateDirector =
                shell.UIStateDirector;

            if (_uiStateDirector == null)
            {
                Fail(
                    "SceneFlowController requires a " +
                    "UIStateDirector."
                );

                return;
            }

            RequestReconcile();
        }

        private void OnDestroy()
        {
            UnbindCoordinator();
            UnbindLocalPlayerState();
        }

        private void HandleKeeperClientIdChanged(
            ulong previousKeeperClientId,
            ulong newKeeperClientId)
        {
            RequestReconcile();
        }

        private void HandleCurrentStanceChanged(
            BandStance previousStance,
            BandStance newStance)
        {
            RequestReconcile();
        }

        private void HandleGlobalTurnChanged(
            int previousGlobalTurn,
            int newGlobalTurn)
        {
            if (!_hasPendingTurnBoundary)
            {
                _sceneNameAtTurnBoundary =
                    _currentContextualSceneName;
            }

            _hasPendingTurnBoundary = true;
            _pendingPreviousTurn =
                previousGlobalTurn;
            _pendingNewTurn =
                newGlobalTurn;

            RequestReconcile();
        }
        
        private void RequestReconcile()
        {
            _reconcileRequested = true;

            if (!isActiveAndEnabled ||
                _reconcileRoutine != null)
            {
                return;
            }

            _reconcileRoutine =
                StartCoroutine(
                    ReconcileAuthoritativeScene()
                );
        }

        private IEnumerator
            ReconcileAuthoritativeScene()
        {
            /*
             * Initial network objects may not exist when
             * GameplayShell first starts. Wait until the
             * authoritative local state can be resolved.
             */
            while (true)
            {
                if (!TryBindAuthoritativeState())
                {
                    yield return null;
                    continue;
                }

                _reconcileRequested = false;

                SceneDestination destination =
                    ResolveAuthoritativeDestination();

                yield return
                    ApplyDestination(destination);

                /*
                 * Allow related Keeper and stance NetworkVariable
                 * changes from the same server operation to settle.
                 */
                yield return null;

                if (_reconcileRequested)
                    continue;

                ResolvePendingTurnBoundary();

                break;
            }

            _reconcileRoutine = null;
        }
        
        private void ResolvePendingTurnBoundary()
        {
            if (!_hasPendingTurnBoundary)
                return;

            bool remainedInSameScene =
                !string.IsNullOrWhiteSpace(
                    _sceneNameAtTurnBoundary
                ) &&
                _sceneNameAtTurnBoundary ==
                _currentContextualSceneName;

            if (remainedInSameScene)
            {
                GameUIState activeState =
                    _uiStateDirector.ActiveState;

                SameSceneTurnAdvanced?.Invoke(
                    _pendingPreviousTurn,
                    _pendingNewTurn,
                    activeState
                );

                Debug.Log(
                    "[SCENE FLOW] Same-scene turn advanced | " +
                    $"scene={_currentContextualSceneName} | " +
                    $"uiState={activeState} | " +
                    $"turn={_pendingPreviousTurn}" +
                    $"->{_pendingNewTurn}",
                    this
                );
            }

            _hasPendingTurnBoundary = false;
            _pendingPreviousTurn = 0;
            _pendingNewTurn = 0;
            _sceneNameAtTurnBoundary = null;
        }

        private bool TryBindAuthoritativeState()
        {
            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null ||
                !coordinator.IsSpawned)
            {
                BindCoordinator(null);
                BindLocalPlayerState(null);

                return false;
            }

            BindCoordinator(coordinator);

            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager == null ||
                !networkManager.IsListening ||
                networkManager.LocalClient == null)
            {
                BindLocalPlayerState(null);
                return false;
            }

            NetworkObject localPlayerObject =
                networkManager
                    .LocalClient
                    .PlayerObject;

            if (localPlayerObject == null ||
                !localPlayerObject.TryGetComponent(
                    out NetPlayerState localPlayerState))
            {
                BindLocalPlayerState(null);
                return false;
            }

            BindLocalPlayerState(
                localPlayerState
            );

            return true;
        }

        private void BindCoordinator(
            GameCoordinator coordinator)
        {
            if (_coordinator == coordinator)
                return;

            UnbindCoordinator();

            _coordinator = coordinator;

            if (_coordinator != null)
            {
                _coordinator
                        .KeeperClientIdChanged +=
                    HandleKeeperClientIdChanged;
                
                _coordinator.GlobalTurnChanged +=
                    HandleGlobalTurnChanged;
            }
        }

        private void UnbindCoordinator()
        {
            if (_coordinator != null)
            {
                _coordinator
                        .KeeperClientIdChanged -=
                    HandleKeeperClientIdChanged;
                
                _coordinator.GlobalTurnChanged -=
                    HandleGlobalTurnChanged;
            }

            _coordinator = null;
        }

        private void BindLocalPlayerState(
            NetPlayerState localPlayerState)
        {
            if (_localPlayerState ==
                localPlayerState)
            {
                return;
            }

            UnbindLocalPlayerState();

            _localPlayerState =
                localPlayerState;

            if (_localPlayerState != null)
            {
                _localPlayerState
                        .CurrentStanceChanged +=
                    HandleCurrentStanceChanged;
            }
        }

        private void UnbindLocalPlayerState()
        {
            if (_localPlayerState != null)
            {
                _localPlayerState
                        .CurrentStanceChanged -=
                    HandleCurrentStanceChanged;
            }

            _localPlayerState = null;
        }

        private SceneDestination
            ResolveAuthoritativeDestination()
        {
            NetworkManager networkManager =
                NetworkManager.Singleton;

            ulong localClientId =
                networkManager != null
                    ? networkManager.LocalClientId
                    : ulong.MaxValue;

            bool isKeeper =
                localClientId != ulong.MaxValue &&
                _coordinator
                    .keeperClientId
                    .Value == localClientId;

            if (isKeeper)
            {
                return new SceneDestination(
                    KeeperSceneName,
                    GameUIState.Keeper
                );
            }

            return _localPlayerState
                .CurrentStanceValue switch
            {
                BandStance.Gestate =>
                    new SceneDestination(
                        GestationSceneName,
                        GameUIState.Gestation
                    ),

                BandStance.Rehearse =>
                    new SceneDestination(
                        RehearsalSceneName,
                        GameUIState.Rehearsal
                    ),

                BandStance.Promote =>
                    new SceneDestination(
                        PromotionSceneName,
                        GameUIState.Promotion
                    ),

                _ =>
                    new SceneDestination(
                        StanceSelectionSceneName,
                        GameUIState.StanceSelection
                    )
            };
        }

        private IEnumerator ApplyDestination(
            SceneDestination destination)
        {
            Scene destinationScene =
                SceneManager.GetSceneByName(
                    destination.SceneName
                );

            if (!destinationScene.IsValid() ||
                !destinationScene.isLoaded)
            {
                AsyncOperation loadOperation =
                    SceneManager.LoadSceneAsync(
                        destination.SceneName,
                        LoadSceneMode.Additive
                    );

                if (loadOperation == null)
                {
                    Debug.LogError(
                        "SceneFlowController could not load " +
                        $"scene {destination.SceneName}.",
                        this
                    );

                    yield break;
                }

                yield return loadOperation;
            }

            /*
             * ContextualSceneBinder performs its registration
             * and camera-focus work in Start().
             */
            yield return null;

            /*
             * The authoritative destination may have changed
             * while the additive load was running.
             */
            SceneDestination latestDestination =
                ResolveAuthoritativeDestination();

            if (latestDestination.SceneName !=
                destination.SceneName)
            {
                _reconcileRequested = true;
                yield break;
            }

            _uiStateDirector.SetState(
                destination.UIState
            );

            /*
             * Activate the incoming view before unloading the
             * previous scene. Its binder has already supplied
             * the new camera focus target.
             */
            for (int i = 0;
                 i < ContextualSceneNames.Length;
                 i++)
            {
                string sceneName =
                    ContextualSceneNames[i];

                if (sceneName ==
                    destination.SceneName)
                {
                    continue;
                }

                _currentContextualSceneName =
                    destination.SceneName;
                
                Scene loadedScene =
                    SceneManager.GetSceneByName(
                        sceneName
                    );

                if (!loadedScene.IsValid() ||
                    !loadedScene.isLoaded)
                {
                    continue;
                }

                AsyncOperation unloadOperation =
                    SceneManager.UnloadSceneAsync(
                        loadedScene
                    );

                if (unloadOperation != null)
                {
                    yield return unloadOperation;
                }
            }

            Debug.Log(
                "[SCENE FLOW] " +
                $"scene={destination.SceneName} | " +
                $"uiState={destination.UIState} | " +
                $"stance=" +
                $"{_localPlayerState.CurrentStanceValue} | " +
                $"keeper=" +
                $"{_coordinator.keeperClientId.Value}",
                this
            );
        }

        private void Fail(
            string message)
        {
            Debug.LogError(
                message,
                this
            );

            enabled = false;
        }

        private readonly struct
            SceneDestination
        {
            public string SceneName { get; }

            public GameUIState UIState { get; }

            public SceneDestination(
                string sceneName,
                GameUIState uiState)
            {
                SceneName = sceneName;
                UIState = uiState;
            }
        }
    }
}