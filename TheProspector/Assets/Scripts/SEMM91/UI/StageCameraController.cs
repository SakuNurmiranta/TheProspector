using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.UI
{
    public class StageCameraController : MonoBehaviour
    {
        [System.Serializable]
        public class FocusBinding
        {
            public GameUIState state;
            public UIFocusAnchor anchor;
        }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private List<FocusBinding> bindings = new();

        private UIFocusAnchor _currentTarget;

        public void Focus(GameUIState state)
        {
            foreach (var binding in bindings)
            {
                if (binding.state == state && binding.anchor != null)
                {
                    _currentTarget = binding.anchor;
                    return;
                }
            }
            
            Debug.LogWarning($"[StageCameraController] No focus anchor for state {state}");
        }

        public GameUIState? GetCurrentTargetState()
        {
            return _currentTarget == null ? null : _currentTarget.State;
        }

        private void Update()
        {
            if (targetCamera == null || _currentTarget == null) return;

            Vector3 targetPosition = _currentTarget.Position;
            targetPosition.z = targetCamera.transform.position.z;

            targetCamera.transform.position = Vector3.Lerp(
                targetCamera.transform.position,
                targetPosition,
                Time.deltaTime * moveSpeed
            );

            if (targetCamera.orthographic)
            {
                targetCamera.orthographicSize = Mathf.Lerp(
                    targetCamera.orthographicSize,
                    _currentTarget.TargetZoom,
                    Time.deltaTime * zoomSpeed
                );
            }
        }
    }
}