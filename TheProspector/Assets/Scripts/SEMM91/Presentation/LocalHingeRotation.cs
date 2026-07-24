using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class LocalHingeRotation : MonoBehaviour
    {
        [SerializeField]
        private Vector3 localAxis = Vector3.right;

        [SerializeField]
        private float minimumAngle;

        [SerializeField]
        private float maximumAngle = 45f;

        private Quaternion _restRotation;

        private void Awake()
        {
            _restRotation = transform.localRotation;
        }

        public void SetAngle(float angle)
        {
            float clampedAngle = Mathf.Clamp(
                angle,
                minimumAngle,
                maximumAngle
            );

            transform.localRotation =
                _restRotation *
                Quaternion.AngleAxis(
                    clampedAngle,
                    localAxis.normalized
                );
        }

        public void SetNormalized(float normalized)
        {
            SetAngle(
                Mathf.Lerp(
                    minimumAngle,
                    maximumAngle,
                    Mathf.Clamp01(normalized)
                )
            );
        }

        public void ResetToRest()
        {
            transform.localRotation = _restRotation;
        }
    }
}