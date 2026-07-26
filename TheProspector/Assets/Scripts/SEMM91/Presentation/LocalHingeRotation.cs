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

        private float _baseAngle;
        private float _additiveAngle;

        public float BaseAngle =>
            _baseAngle;

        public float CurrentAngle =>
            Mathf.Clamp(
                _baseAngle + _additiveAngle,
                minimumAngle,
                maximumAngle
            );

        private void Awake()
        {
            _restRotation = transform.localRotation;

            ApplyRotation();
        }

        /// <summary>
        /// Sets the primary pose angle, normally supplied by
        /// ContextualScenePresentationLayout.
        /// </summary>
        public void SetAngle(float angle)
        {
            _baseAngle = angle;

            ApplyRotation();
        }

        /// <summary>
        /// Adds a temporary procedural offset without replacing
        /// the contextual pose angle.
        /// </summary>
        public void SetAdditiveAngle(float angle)
        {
            _additiveAngle = angle;

            ApplyRotation();
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
            _baseAngle = 0f;
            _additiveAngle = 0f;

            ApplyRotation();
        }

        private void ApplyRotation()
        {
            Vector3 axis =
                localAxis.sqrMagnitude > 0f
                    ? localAxis.normalized
                    : Vector3.right;

            transform.localRotation =
                _restRotation *
                Quaternion.AngleAxis(
                    CurrentAngle,
                    axis
                );
        }
    }
}