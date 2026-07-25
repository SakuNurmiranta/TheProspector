using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ContextualScenePresentationLayout :
        MonoBehaviour
    {
        [Header("Persistent Skull")]
        [SerializeField]
        private Transform skullPoseAnchor;

        [Header("Skull Articulation")]
        [SerializeField]
        private float jawAngle;

        [SerializeField]
        private float calvariumAngle;

        public Transform SkullPoseAnchor =>
            skullPoseAnchor;

        public float JawAngle =>
            jawAngle;

        public float CalvariumAngle =>
            calvariumAngle;

        public bool HasValidSkullPose =>
            skullPoseAnchor != null;
    }
}