using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkullPresentationController :
        MonoBehaviour
    {
        [Header("Persistent Skull Articulation")]
        [SerializeField]
        private Transform jawPivot;

        [SerializeField]
        private Transform calvariumPivot;

        [SerializeField]
        private LocalHingeRotation jawHinge;

        [SerializeField]
        private LocalHingeRotation calvariumHinge;
        
        private Quaternion _jawNeutralLocalRotation;
        private Quaternion _calvariumNeutralLocalRotation;

        private void Awake()
        {
            if (jawPivot != null)
            {
                _jawNeutralLocalRotation =
                    jawPivot.localRotation;
            }

            if (calvariumPivot != null)
            {
                _calvariumNeutralLocalRotation =
                    calvariumPivot.localRotation;
            }
        }

        public void ApplyPose(
            Transform poseAnchor,
            float jawAngle,
            float calvariumAngle)
        {
            if (poseAnchor == null)
            {
                Debug.LogError(
                    "SkullPresentationController received a null pose anchor.",
                    this
                );

                return;
            }

            if (jawHinge == null || calvariumHinge == null)
            {
                Debug.LogError(
                    "SkullPresentationController is missing hinge references.",
                    this
                );

                return;
            }

            transform.SetPositionAndRotation(
                poseAnchor.position,
                poseAnchor.rotation
            );

            transform.localScale =
                poseAnchor.localScale;

            jawHinge.SetAngle(jawAngle);
            calvariumHinge.SetAngle(calvariumAngle);

            Debug.Log(
                "[SKULL PRESENTATION] Pose applied | " +
                $"anchor={poseAnchor.name} | " +
                $"jaw={jawAngle:F1} | " +
                $"calvarium={calvariumAngle:F1}",
                this
            );
        }    
    }
}