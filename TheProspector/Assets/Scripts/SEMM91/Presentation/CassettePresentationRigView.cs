using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CassettePresentationRigView : MonoBehaviour
    {
        [Header("Identity Presentation")]
        [SerializeField]
        private TMP_Text physicalTitleText;

        [Header("Tower Endpoint Projection")]
        [SerializeField]
        private Camera towerCamera;

        [SerializeField]
        private Camera presentationCamera;

        [Tooltip(
            "Stable presentation-stage anchor. " +
            "Its camera-space depth is preserved while its viewport X/Y " +
            "are updated from the selected Tower cassette."
        )]
        [SerializeField]
        private Transform towerEndpointAnchor;

        [Tooltip(
            "Forward spline whose FIRST knot must coincide with the selected Tower cassette."
        )]
        [SerializeField]
        private SplineContainer towerToReadySpline;

        [Tooltip(
            "Return spline whose LAST knot must coincide with the selected Tower cassette."
        )]
        [SerializeField]
        private SplineContainer readyToTowerSpline;

        private CassettePresentationData _currentData;

        public CassettePresentationData CurrentData =>
            _currentData;

        public Transform TowerEndpointAnchor =>
            towerEndpointAnchor;

        public void Apply(
            CassettePresentationData data)
        {
            _currentData = data;
            ApplyIdentity(data);
        }

        public bool Apply(
            CassettePresentationData data,
            Transform sourceTowerTransform)
        {
            _currentData = data;
            ApplyIdentity(data);

            return UpdateTowerEndpoint(
                sourceTowerTransform
            );
        }

        private void ApplyIdentity(
            CassettePresentationData data)
        {
            if (physicalTitleText != null)
            {
                physicalTitleText.text =
                    data.DisplayName;
            }
        }

        private bool UpdateTowerEndpoint(
            Transform sourceTowerTransform)
        {
            if (sourceTowerTransform == null)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Source Tower transform is missing.",
                    this
                );
                return false;
            }

            if (towerCamera == null)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Tower Camera is not assigned.",
                    this
                );
                return false;
            }

            if (presentationCamera == null)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Presentation Camera is not assigned.",
                    this
                );
                return false;
            }

            if (towerEndpointAnchor == null)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Tower Endpoint Anchor is not assigned.",
                    this
                );
                return false;
            }

            Vector3 sourceViewport =
                towerCamera.WorldToViewportPoint(
                    sourceTowerTransform.position
                );

            if (sourceViewport.z <= 0.0f)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Selected Tower cassette is behind the Tower Camera.",
                    this
                );
                return false;
            }

            Vector3 anchorViewport =
                presentationCamera.WorldToViewportPoint(
                    towerEndpointAnchor.position
                );

            if (anchorViewport.z <= 0.0f)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    "Tower Endpoint Anchor is behind the Presentation Camera.",
                    this
                );
                return false;
            }

            Vector3 projectedViewport =
                new Vector3(
                    sourceViewport.x,
                    sourceViewport.y,
                    anchorViewport.z
                );

            towerEndpointAnchor.position =
                presentationCamera.ViewportToWorldPoint(
                    projectedViewport
                );

            bool forwardUpdated =
                SetSplineKnotWorldPosition(
                    towerToReadySpline,
                    0,
                    towerEndpointAnchor.position,
                    "TowerToReady first knot"
                );

            bool returnUpdated =
                SetSplineKnotWorldPosition(
                    readyToTowerSpline,
                    -1,
                    towerEndpointAnchor.position,
                    "ReadyToTower last knot"
                );

            return forwardUpdated &&
                   returnUpdated;
        }

        private bool SetSplineKnotWorldPosition(
            SplineContainer container,
            int knotIndex,
            Vector3 worldPosition,
            string label)
        {
            if (container == null)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    $"{label}: SplineContainer is not assigned.",
                    this
                );
                return false;
            }

            Spline spline =
                container.Spline;

            if (spline == null ||
                spline.Count == 0)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    $"{label}: spline has no knots.",
                    this
                );
                return false;
            }

            int resolvedIndex =
                knotIndex < 0
                    ? spline.Count - 1
                    : knotIndex;

            if (resolvedIndex < 0 ||
                resolvedIndex >= spline.Count)
            {
                Debug.LogError(
                    "[CASSETTE RIG] " +
                    $"{label}: knot index {resolvedIndex} is invalid.",
                    this
                );
                return false;
            }

            Vector3 localPosition =
                container.transform.InverseTransformPoint(
                    worldPosition
                );

            BezierKnot knot =
                spline[resolvedIndex];

            knot.Position =
                new float3(
                    localPosition.x,
                    localPosition.y,
                    localPosition.z
                );

            spline[resolvedIndex] =
                knot;

            return true;
        }
    }
}