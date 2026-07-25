using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkullPresentationController :
        MonoBehaviour
    {
        public void ApplyPose(
            Transform poseAnchor)
        {
            if (poseAnchor == null)
            {
                Debug.LogError(
                    "SkullPresentationController received " +
                    "a null pose anchor.",
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

            Debug.Log(
                "[SKULL PRESENTATION] Pose applied | " +
                $"anchor={poseAnchor.name} | " +
                $"position={transform.position} | " +
                $"rotation={transform.rotation.eulerAngles} | " +
                $"scale={transform.localScale}",
                this
            );
        }
    }
}