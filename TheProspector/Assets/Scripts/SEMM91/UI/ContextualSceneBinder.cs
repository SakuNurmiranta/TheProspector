using UnityEngine;

namespace SEMM91.UI
{
    [DisallowMultipleComponent]
    public sealed class ContextualSceneBinder :
        MonoBehaviour
    {
        [Header("Contextual Scene")]
        [SerializeField]
        private ContextualUIView contextualView;

        [SerializeField]
        private Canvas worldSpaceCanvas;

        private UIStateDirector _registeredDirector;

        public bool IsBound { get; private set; }

        private void Awake()
        {
            /*
             * Keep the incoming scene invisible until the
             * transition system explicitly activates it.
             *
             * This binder lives on a separate service root,
             * so hiding the contextual view does not disable
             * the binder itself.
             */
            if (contextualView == null)
                return;

            contextualView.ExitFocus();
            contextualView.DeActivate();
            contextualView.Hide();
        }

        private void Start()
        {
            if (contextualView == null)
            {
                Fail(
                    "ContextualSceneBinder is missing its " +
                    "ContextualUIView."
                );

                return;
            }

            if (worldSpaceCanvas == null)
            {
                Fail(
                    "ContextualSceneBinder is missing its " +
                    "world-space Canvas."
                );

                return;
            }

            if (worldSpaceCanvas.renderMode !=
                RenderMode.WorldSpace)
            {
                Fail(
                    "ContextualSceneBinder requires a " +
                    "world-space Canvas."
                );

                return;
            }

            /*
             * Binding occurs in Start rather than Awake.
             * Every loaded GameplayShellReferences.Awake()
             * has therefore completed before this lookup.
             */
            if (!GameplayShellReferences.TryGet(
                    out GameplayShellReferences shell
                ))
            {
                Fail(
                    "ContextualSceneBinder could not locate " +
                    "a ready GameplayShellReferences."
                );

                return;
            }

            worldSpaceCanvas.worldCamera =
                shell.StageCamera;

            _registeredDirector =
                shell.UIStateDirector;

            _registeredDirector
                .RegisterContextualView(
                    contextualView
                );

            IsBound = true;
        }

        private void OnDestroy()
        {
            if (_registeredDirector != null &&
                contextualView != null)
            {
                _registeredDirector
                    .UnregisterContextualView(
                        contextualView
                    );
            }

            IsBound = false;
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
    }
}