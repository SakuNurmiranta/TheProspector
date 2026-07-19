using System;
using System.Collections;
using SEMM91.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ClientPresentationLoader :
        MonoBehaviour
    {
        private const string
            DefaultGameplayShellSceneName =
                "GameplayShell";

        [Header("Client Lifecycle")]
        [SerializeField]
        private NetBootstrap netBootstrap;

        [Header("Presentation Scenes")]
        [SerializeField]
        private string gameplayShellSceneName =
            DefaultGameplayShellSceneName;

        private bool _shouldBeLoaded;
        private Coroutine _reconcileRoutine;

        public bool IsGameplayShellLoaded =>
            IsSceneLoaded();

        private void Awake()
        {
            if (netBootstrap == null)
            {
                Debug.LogError(
                    "ClientPresentationLoader is missing " +
                    "its NetBootstrap reference.",
                    this
                );

                enabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(
                    gameplayShellSceneName
                ))
            {
                Debug.LogError(
                    "ClientPresentationLoader requires a " +
                    "GameplayShell scene name.",
                    this
                );

                enabled = false;
                return;
            }

            netBootstrap.LocalClientConnected +=
                HandleLocalClientConnected;

            netBootstrap.LocalClientDisconnected +=
                HandleLocalClientDisconnected;

            _shouldBeLoaded =
                netBootstrap.IsLocalClientConnected;
        }

        private void Start()
        {
            BeginReconcile();
        }

        private void OnDestroy()
        {
            if (netBootstrap == null)
                return;

            netBootstrap.LocalClientConnected -=
                HandleLocalClientConnected;

            netBootstrap.LocalClientDisconnected -=
                HandleLocalClientDisconnected;
        }

        private void HandleLocalClientConnected()
        {
            _shouldBeLoaded = true;
            BeginReconcile();
        }

        private void HandleLocalClientDisconnected()
        {
            _shouldBeLoaded = false;
            BeginReconcile();
        }

        private void BeginReconcile()
        {
            if (!isActiveAndEnabled ||
                _reconcileRoutine != null)
            {
                return;
            }

            _reconcileRoutine =
                StartCoroutine(
                    ReconcilePresentationState()
                );
        }

        private IEnumerator
            ReconcilePresentationState()
        {
            while (true)
            {
                bool isLoaded =
                    IsSceneLoaded();

                if (isLoaded ==
                    _shouldBeLoaded)
                {
                    break;
                }

                AsyncOperation operation;

                try
                {
                    operation =
                        _shouldBeLoaded
                            ? SceneManager
                                .LoadSceneAsync(
                                    gameplayShellSceneName,
                                    LoadSceneMode.Additive
                                )
                            : SceneManager
                                .UnloadSceneAsync(
                                    SceneManager
                                        .GetSceneByName(
                                            gameplayShellSceneName
                                        )
                                );
                }
                catch (Exception exception)
                {
                    Debug.LogException(
                        exception,
                        this
                    );

                    break;
                }

                if (operation == null)
                {
                    Debug.LogError(
                        "ClientPresentationLoader could " +
                        "not start the requested scene " +
                        "operation.",
                        this
                    );

                    break;
                }

                yield return operation;
            }

            _reconcileRoutine = null;
        }

        private bool IsSceneLoaded()
        {
            Scene scene =
                SceneManager.GetSceneByName(
                    gameplayShellSceneName
                );

            return
                scene.IsValid() &&
                scene.isLoaded;
        }
    }
}