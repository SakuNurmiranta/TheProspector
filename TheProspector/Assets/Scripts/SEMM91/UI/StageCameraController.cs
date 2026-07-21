using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.UI
{
    [DisallowMultipleComponent]
    public class StageCameraController :
        MonoBehaviour
    {
        [System.Serializable]
        public class FocusBinding
        {
            public GameUIState state;
            public UIFocusAnchor anchor;
        }

        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private float moveSpeed = 6f;

        [SerializeField]
        private float zoomSpeed = 6f;

        /*
         * Legacy single-scene bindings.
         *
         * These remain available while the old scene is still
         * retained as a regression baseline.
         */
        [SerializeField]
        private List<FocusBinding> bindings = new();

        private UIFocusAnchor _currentTarget;

        /// <summary>
        /// Legacy single-scene focus path.
        /// Resolves a serialized anchor by UI state.
        /// </summary>
        public void Focus(
            GameUIState state)
        {
            foreach (FocusBinding binding
                     in bindings)
            {
                if (binding.state != state ||
                    binding.anchor == null)
                {
                    continue;
                }

                Focus(binding.anchor);
                return;
            }

            Debug.LogWarning(
                "[StageCameraController] No focus " +
                $"anchor for state {state}.",
                this
            );
        }

        /// <summary>
        /// Additive-scene focus path.
        ///
        /// The loaded contextual scene supplies its own
        /// scene-local anchor directly.
        /// </summary>
        public void Focus(
            UIFocusAnchor anchor)
        {
            if (anchor == null)
            {
                Debug.LogWarning(
                    "[StageCameraController] Cannot focus " +
                    "a null UIFocusAnchor.",
                    this
                );

                return;
            }

            _currentTarget = anchor;
        }

        /// <summary>
        /// Releases the target only when it still belongs to
        /// the scene requesting release.
        ///
        /// This prevents an outgoing scene from clearing a
        /// newer scene's focus target during additive unload.
        /// </summary>
        public void ReleaseFocus(
            UIFocusAnchor anchor)
        {
            if (_currentTarget != anchor)
                return;

            _currentTarget = null;
        }

        public GameUIState?
            GetCurrentTargetState()
        {
            return
                _currentTarget == null
                    ? null
                    : _currentTarget.State;
        }

        private void Update()
        {
            if (targetCamera == null ||
                _currentTarget == null)
            {
                return;
            }

            Vector3 targetPosition =
                _currentTarget.Position;

            /*
             * Contextual anchors control the camera's stage
             * coordinates. Camera depth remains persistent.
             */
            targetPosition.z =
                targetCamera.transform.position.z;

            targetCamera.transform.position =
                Vector3.Lerp(
                    targetCamera.transform.position,
                    targetPosition,
                    Time.deltaTime * moveSpeed
                );

            if (!targetCamera.orthographic)
                return;

            targetCamera.orthographicSize =
                Mathf.Lerp(
                    targetCamera.orthographicSize,
                    _currentTarget.TargetZoom,
                    Time.deltaTime * zoomSpeed
                );
        }
    }
}