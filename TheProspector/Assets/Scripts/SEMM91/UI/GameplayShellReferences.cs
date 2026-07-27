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

        [SerializeField]
        private DemoTowerPresentationController
            demoTowerPresentationController;
        
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
            mediaShelfPresentationController != null &&
            demoTowerPresentationController != null;

        public SkullPresentationController
            SkullPresentationController =>
            skullPresentationController;

        public MediaShelfPresentationController
            MediaShelfPresentationController =>
                mediaShelfPresentationController;

        public DemoTowerPresentationController
            DemoTowerPresentationController =>
                demoTowerPresentationController;
        
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
                    "SkullPresentationController, " +
                    "MediaShelfPresentationController, or " +
                    "DemoTowerPresentationController.",
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

