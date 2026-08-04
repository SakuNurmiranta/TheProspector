using System.Collections;
using SEMM91.InputSystems;
using SEMM91.Networking;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using SEMM91.UI;

namespace SEMM91.Presentation.Development
{
    /// <summary>
    /// Editor-only convenience entry point.
    ///
    /// When Rehearsal is opened directly and Play is pressed:
    /// - loads ClientBootstrap;
    /// - starts normal local single-player hosting;
    /// - waits for the authoritative local player;
    /// - requests Rehearse through the production command path.
    ///
    /// During normal gameplay loading this component immediately
    /// disables itself and performs no work.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class RehearsalDevelopmentBootstrap : MonoBehaviour
    {
        private const string ClientBootstrapSceneName =
            "ClientBootstrap";
        
        private const string RehearsalSceneName =
            "Rehearsal";

        private static bool _launchInProgress;
        
        private ContextualSceneBinder[] _suspendedBinders =
            System.Array.Empty<ContextualSceneBinder>();

#if UNITY_EDITOR
        private Button _developmentUndoOrReturnButton;
        private NetPlayerState _developmentPlayerState;
        private PlayerActionController _developmentActionController;
        private bool _developmentNavigationGuardActive;
#endif

        private void Awake()
        {       
#if UNITY_EDITOR
            // During normal additive loading, the production shell
            // already exists and the binder should operate normally.
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                return;
            }

            /*
             * During direct Rehearsal-scene launch, prevent the
             * ContextualSceneBinder from reaching Start() before
             * GameplayShellReferences exists.
             */
            Scene sourceScene = gameObject.scene;

            List<ContextualSceneBinder> binders = new();

            foreach (GameObject rootObject
                     in sourceScene.GetRootGameObjects())
            {
                binders.AddRange(
                    rootObject.GetComponentsInChildren<
                        ContextualSceneBinder>(true)
                );
            }

            _suspendedBinders = binders.ToArray();

            foreach (ContextualSceneBinder binder
                     in _suspendedBinders)
            {
                if (binder != null)
                {
                    binder.enabled = false;
                }
            }
#endif
        }
        
        private IEnumerator Start()
        {
#if !UNITY_EDITOR
            Destroy(gameObject);
            yield break;
#else
            // Normal gameplay has already established the network session.
            // In that case Rehearsal was loaded through SceneFlowController,
            // so this helper must remain inert.
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                Destroy(gameObject);
                yield break;
            }

            if (_launchInProgress)
            {
                Destroy(gameObject);
                yield break;
            }

            _launchInProgress = true;

            // Rehearsal may be unloaded temporarily when the normal scene
            // flow initially observes BandStance.None.
            DontDestroyOnLoad(gameObject);

            yield return EnsureClientBootstrapLoaded();
            yield return StartSoloMode();

/*
 * Request the stance as early as possible, before
 * SceneFlowController settles on StanceSelection.
 */
            yield return RequestRehearseStance();

            yield return WaitForGameplayShellReady();

            ResumeContextualBinders();

/*
 * Re-enabling a suspended binder schedules its Start().
 * Wait until the Rehearsal binder has registered with
 * UIStateDirector before selecting the presentation state.
 */
            yield return WaitForRehearsalBinder();

            ActivateRehearsalPresentation();
            
            InitializeUnsupportedNavigationGuard();

            yield return SeedDevelopmentIdeas();
            yield return CreateDevelopmentTrack();

            Debug.Log(
                "[REHEARSAL DEV BOOTSTRAP] " +
                "SOLOMODE established and Rehearse requested."
            );

            _launchInProgress = false;
#endif
        }

#if UNITY_EDITOR
        private static IEnumerator
            EnsureClientBootstrapLoaded()
        {
            Scene bootstrapScene =
                SceneManager.GetSceneByName(
                    ClientBootstrapSceneName
                );

            if (bootstrapScene.IsValid() &&
                bootstrapScene.isLoaded)
            {
                yield break;
            }

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    ClientBootstrapSceneName,
                    LoadSceneMode.Additive
                );

            if (loadOperation == null)
            {
                Debug.LogError(
                    "[REHEARSAL DEV BOOTSTRAP] " +
                    "Could not begin loading ClientBootstrap."
                );

                yield break;
            }

            yield return loadOperation;
        }

        private static IEnumerator StartSoloMode()
        {
            NetBootstrap netBootstrap = null;

            while (netBootstrap == null)
            {
                netBootstrap =
                    Object.FindFirstObjectByType<NetBootstrap>(
                        FindObjectsInactive.Include
                    );

                yield return null;
            }

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsListening)
            {
                yield break;
            }

            netBootstrap.StartLocalSinglePlayerHost();

            while (NetworkManager.Singleton == null ||
                   !NetworkManager.Singleton.IsListening ||
                   !NetworkManager.Singleton.IsClient ||
                   !NetworkManager.Singleton.IsServer)
            {
                yield return null;
            }
        }

        private static IEnumerator
            RequestRehearseStance()
        {
            PlayerActionController localController = null;

            while (localController == null ||
                   !localController.CanRequest(
                       PlayerCommand.SelectRehearse
                   ))
            {
                PlayerActionController[] controllers =
                    Object.FindObjectsByType<PlayerActionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                localController = null;

                foreach (PlayerActionController controller
                         in controllers)
                {
                    if (controller != null &&
                        controller.IsOwner &&
                        controller.IsClient)
                    {
                        localController = controller;
                        break;
                    }
                }

                yield return null;
            }

            localController.Request(
                PlayerCommand.SelectRehearse
            );
        }
        
        private static IEnumerator
            WaitForGameplayShellReady()
        {
            while (!GameplayShellReferences.TryGet(out _))
            {
                yield return null;
            }

            /*
             * Give the newly enabled contextual binder a clean
             * following frame in which to run Start().
             */
            yield return null;
        }
        
        private static IEnumerator WaitForRehearsalBinder()
        {
            while (true)
            {
                ContextualSceneBinder[] binders =
                    Object.FindObjectsByType<
                        ContextualSceneBinder>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                foreach (ContextualSceneBinder binder in binders)
                {
                    if (binder == null)
                        continue;

                    if (binder.gameObject.scene.name !=
                        RehearsalSceneName)
                    {
                        continue;
                    }

                    if (binder.IsBound)
                        yield break;
                }

                yield return null;
            }
        }
        
        private static void ActivateRehearsalPresentation()
        {
            if (!GameplayShellReferences.TryGet(
                    out GameplayShellReferences shell
                ))
            {
                Debug.LogError(
                    "[REHEARSAL DEV BOOTSTRAP] " +
                    "GameplayShellReferences disappeared " +
                    "before presentation activation."
                );

                return;
            }

            shell.UIStateDirector.SetState(
                GameUIState.Rehearsal
            );
        }

        private void InitializeUnsupportedNavigationGuard()
        {
            TurnPlannerView turnPlanner =
                Object.FindFirstObjectByType<TurnPlannerView>(
                    FindObjectsInactive.Include
                );

            if (turnPlanner == null)
            {
                Debug.LogWarning(
                    "[REHEARSAL DEV BOOTSTRAP] " +
                    "TurnPlannerView was not found."
                );

                return;
            }

            /*
             * Editor-only reflection keeps this development concern
             * out of TurnPlannerView and the production command path.
             */
            FieldInfo undoButtonField =
                typeof(TurnPlannerView).GetField(
                    "undoButton",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            _developmentUndoOrReturnButton =
                undoButtonField?.GetValue(turnPlanner) as Button;

            if (_developmentUndoOrReturnButton == null)
            {
                Debug.LogWarning(
                    "[REHEARSAL DEV BOOTSTRAP] " +
                    "Could not access TurnPlannerView.undoButton."
                );

                return;
            }

            NetPlayerState[] playerStates =
                Object.FindObjectsByType<NetPlayerState>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (NetPlayerState state in playerStates)
            {
                if (state == null ||
                    !state.IsOwner ||
                    !state.IsClient)
                {
                    continue;
                }

                _developmentPlayerState = state;
                _developmentActionController =
                    state.GetComponent<PlayerActionController>();

                break;
            }

            if (_developmentPlayerState == null ||
                _developmentActionController == null)
            {
                Debug.LogWarning(
                    "[REHEARSAL DEV BOOTSTRAP] " +
                    "Local player controls were not found."
                );

                return;
            }

            _developmentNavigationGuardActive = true;

            Debug.Log(
                "[REHEARSAL DEV BOOTSTRAP] " +
                "Unsupported stance return is guarded; " +
                "Undo remains available while a draft exists."
            );
        }

        private static IEnumerator CreateDevelopmentTrack()
        {
            PlayerActionController localController = null;

            while (localController == null)
            {
                PlayerActionController[] controllers =
                    Object.FindObjectsByType<PlayerActionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                foreach (PlayerActionController controller
                         in controllers)
                {
                    if (controller != null &&
                        controller.IsClient &&
                        controller.IsOwner)
                    {
                        localController = controller;
                        break;
                    }
                }

                if (localController == null)
                {
                    yield return null;
                }
            }

            localController
                .RequestDevelopmentCreateEmptyTrack();

            // Allow the ServerRpc and snapshot publication
            // to complete before logging bootstrap completion.
            yield return null;
        }
        
        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!_developmentNavigationGuardActive ||
                _developmentUndoOrReturnButton == null ||
                _developmentPlayerState == null ||
                _developmentActionController == null)
            {
                return;
            }

            bool hasDraftedActions =
                _developmentPlayerState
                    .DraftedStandardSlot1Value.IsOccupied ||
                _developmentPlayerState
                    .DraftedStandardSlot2Value.IsOccupied ||
                _developmentPlayerState
                    .DraftedOverreachSlotValue.IsOccupied;

            if (!hasDraftedActions)
            {
                /*
                 * With an empty draft, TurnPlannerView uses this button
                 * for ReturnToStanceSelection. That destination is not
                 * supported by the direct-scene development harness.
                 */
                _developmentUndoOrReturnButton.interactable = false;
                return;
            }

            /*
             * With a draft present, the same button represents Undo.
             * Preserve the production command availability result.
             */
            _developmentUndoOrReturnButton.interactable =
                _developmentActionController.CanRequest(
                    PlayerCommand.UndoDraftAction
                );
#endif
        }

        private void ResumeContextualBinders()
        {
            foreach (ContextualSceneBinder binder
                     in _suspendedBinders)
            {
                /*
                 * The original directly opened Rehearsal scene may
                 * already have been unloaded and recreated by the
                 * production SceneFlowController. Destroyed references
                 * are ignored; the newly loaded scene binds normally.
                 */
                if (binder != null)
                {
                    binder.enabled = true;
                }
            }

            _suspendedBinders =
                System.Array.Empty<ContextualSceneBinder>();
        }
        
        private static IEnumerator SeedDevelopmentIdeas()
        {
            PlayerActionController localController = null;

            while (localController == null)
            {
                PlayerActionController[] controllers =
                    Object.FindObjectsByType<PlayerActionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                foreach (PlayerActionController controller
                         in controllers)
                {
                    if (controller != null &&
                        controller.IsClient &&
                        controller.IsOwner)
                    {
                        localController = controller;
                        break;
                    }
                }

                if (localController == null)
                {
                    yield return null;
                }
            }

            localController
                .RequestDevelopmentSeedPeak1Ideas();

            // Let the host-side ServerRpc complete before the
            // empty track request is sent.
            yield return null;
        }
        
#endif
    }
}