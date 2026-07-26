using UnityEngine;
using SEMM91.Presentation;

namespace SEMM91.UI
{
    public sealed class GameplayShellReferences :
        MonoBehaviour
    {
        public static GameplayShellReferences
            Instance { get; private set; }

        [Header("Shared Gameplay Presentation")]
        [SerializeField]
        private UIStateDirector uiStateDirector;

        [SerializeField]
        private Camera stageCamera;
        
        [SerializeField]
        private StageCameraController stageCameraController;

        [SerializeField]
        private SkullPresentationController
            skullPresentationController;

        [SerializeField]
        private MediaShelfPresentationController
            mediaShelfPresentationController;
        
        public UIStateDirector UIStateDirector =>
            uiStateDirector;

        public Camera StageCamera =>
            stageCamera;

        public StageCameraController
            StageCameraController =>
            stageCameraController;

        public bool IsReady =>
            uiStateDirector != null &&
            stageCamera != null &&
            stageCameraController != null &&
            skullPresentationController != null &&
            mediaShelfPresentationController != null;

        public SkullPresentationController
            SkullPresentationController =>
            skullPresentationController;

        public MediaShelfPresentationController
            MediaShelfPresentationController =>
                mediaShelfPresentationController;
        
        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Debug.LogError(
                    "Multiple GameplayShellReferences " +
                    "instances are loaded.",
                    this
                );

                enabled = false;
                return;
            }

            Instance = this;

            if (!IsReady)
            {
                Debug.LogError(
                    "GameplayShellReferences is missing its " +
                    "UIStateDirector, StageCamera, " +
                    "StageCameraController, " +
                    "SkullPresentationController, or " +
                    "MediaShelfPresentationController.",
                    this
                );
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static bool TryGet(
            out GameplayShellReferences references)
        {
            references = Instance;

            return
                references != null &&
                references.IsReady;
        }
    }
}

