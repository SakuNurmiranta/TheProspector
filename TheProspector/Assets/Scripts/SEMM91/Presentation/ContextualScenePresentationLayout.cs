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

        public Transform SkullPoseAnchor =>
            skullPoseAnchor;

        public bool HasValidSkullPose =>
            skullPoseAnchor != null;
    }
}