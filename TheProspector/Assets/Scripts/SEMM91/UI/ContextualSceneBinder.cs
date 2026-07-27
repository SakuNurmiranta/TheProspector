using UnityEngine;
using SEMM91.Presentation;

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
        
        [SerializeField]
        private UIFocusAnchor focusAnchor;

        [SerializeField]
        private ContextualScenePresentationLayout presentationLayout;
        
        private UIStateDirector _registeredDirector;

        private StageCameraController
            _stageCameraController;

        private MediaShelfPresentationController
            _mediaShelfPresentationController;

        private DemoTowerPresentationController
            _demoTowerPresentationController;
        
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
            
            if (focusAnchor == null)
            {
                Fail(
                    "ContextualSceneBinder is missing its " +
                    "UIFocusAnchor."
                );

                return;
            }
            
            if (presentationLayout == null)
            {
                Fail(
                    "ContextualSceneBinder is missing its " +
                    "ContextualScenePresentationLayout."
                );

                return;
            }

            if (!presentationLayout.HasValidSkullPose)
            {
                Fail(
                    "ContextualSceneBinder presentation layout " +
                    "is missing its SkullPoseAnchor."
                );

                return;
            }

            if (!presentationLayout
                    .HasValidMediaShelfPose)
            {
                Fail(
                    "ContextualSceneBinder presentation layout " +
                    "requires a MediaShelfPoseAnchor when " +
                    "the Shelf mode is not Hidden."
                );

                return;
            }

            if (!presentationLayout
                    .HasValidDemoTowerPose)
            {
                Fail(
                    "ContextualSceneBinder presentation layout " +
                    "requires a DemoTowerPoseAnchor when " +
                    "the Tower mode is not Hidden."
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

            shell.SkullPresentationController.ApplyPose(
                presentationLayout.SkullPoseAnchor,
                presentationLayout.JawAngle,
                presentationLayout.CalvariumAngle
            );

            _mediaShelfPresentationController =
                shell.MediaShelfPresentationController;

            _mediaShelfPresentationController
                .ApplyContext(
                    this,
                    presentationLayout
                        .MediaShelfPoseAnchor,
                    presentationLayout
                        .MediaShelfInteractionMode
                );

            _demoTowerPresentationController =
                shell.DemoTowerPresentationController;

            _demoTowerPresentationController
                .ApplyContext(
                    this,
                    presentationLayout
                        .DemoTowerPoseAnchor,
                    presentationLayout
                        .DemoTowerInteractionMode
                );
            
            _registeredDirector =
                shell.UIStateDirector;

            _stageCameraController =
                shell.StageCameraController;

            _registeredDirector
                .RegisterContextualView(
                    contextualView
                );

            _stageCameraController.Focus(
                focusAnchor
            );

            IsBound = true;
        }

        private void OnDestroy()
        {
            if (_demoTowerPresentationController !=
                null)
            {
                _demoTowerPresentationController
                    .ReleaseContext(
                        this
                    );
            }

            if (_mediaShelfPresentationController !=
                null)
            {
                _mediaShelfPresentationController
                    .ReleaseContext(
                        this
                    );
            }

            if (_stageCameraController != null &&
                focusAnchor != null)
            {
                _stageCameraController
                    .ReleaseFocus(
                        focusAnchor
                    );
            }

            if (_registeredDirector != null &&
                contextualView != null)
            {
                _registeredDirector
                    .UnregisterContextualView(
                        contextualView
                    );
            }

            _demoTowerPresentationController =
                null;

            _mediaShelfPresentationController =
                null;

            _stageCameraController = null;
            _registeredDirector = null;
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

