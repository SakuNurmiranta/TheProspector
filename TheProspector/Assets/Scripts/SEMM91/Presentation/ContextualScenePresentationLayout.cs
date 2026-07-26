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

        [Header("Persistent Media Shelf")]
        [SerializeField]
        private Transform mediaShelfPoseAnchor;

        [SerializeField]
        private MediaShelfInteractionMode
            mediaShelfInteractionMode =
                MediaShelfInteractionMode.Hidden;

        [Header("Skull Articulation")]
        [SerializeField]
        private float jawAngle;

        [SerializeField]
        private float calvariumAngle;

        public Transform SkullPoseAnchor =>
            skullPoseAnchor;

        public Transform MediaShelfPoseAnchor =>
            mediaShelfPoseAnchor;

        public MediaShelfInteractionMode
            MediaShelfInteractionMode =>
            mediaShelfInteractionMode;

        public bool HasValidMediaShelfPose =>
            mediaShelfInteractionMode ==
            MediaShelfInteractionMode.Hidden ||
            mediaShelfPoseAnchor != null;

        public float JawAngle =>
            jawAngle;

        public float CalvariumAngle =>
            calvariumAngle;

        public bool HasValidSkullPose =>
            skullPoseAnchor != null;
    }
}