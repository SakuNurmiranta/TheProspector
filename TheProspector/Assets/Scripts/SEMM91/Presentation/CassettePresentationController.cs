using UnityEngine;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CassettePresentationController :
        MonoBehaviour
    {
        private enum DebugPose
        {
            CenterClosed,
            CaseOpen,
            RemovalOrientation,
            Ready,
            PlaybackDeparture,
            PlayingReadable
        }

        [Header("Live Artifact")]
        [SerializeField]
        private Transform presentationObject;

        [SerializeField]
        private Transform casingHingePivot;

        [SerializeField]
        private Transform cassette;

        [SerializeField]
        private Transform innerSleeve;

        [Header("Root Poses")]
        [SerializeField]
        private Transform centerClosedPose;

        [SerializeField]
        private Transform caseOpenPose;

        [SerializeField]
        private Transform removalOrientationPose;

        [SerializeField]
        private Transform readyPose;

        [SerializeField]
        private Transform playbackDeparturePose;

        [SerializeField]
        private Transform playingReadablePose;

        [Header("Articulation Targets")]
        [SerializeField]
        private Transform lidOpenTarget;

        [SerializeField]
        private Transform readyCassetteTarget;

        [SerializeField]
        private Transform cassetteOffscreenTarget;

        [SerializeField]
        private Transform sleeveReadableTarget;

        [Header("Debug")]
        [SerializeField]
        private DebugPose debugPose;

        private DebugPose _lastDebugPose;
        
        private Quaternion _closedLidRotation;

        private Vector3 _seatedCassettePosition;
        private Quaternion _seatedCassetteRotation;

        private Vector3 _seatedSleevePosition;
        private Quaternion _seatedSleeveRotation;

        private bool _baselineCaptured;

        private void Awake()
        {
            CaptureBaseline();
        }
        
        private void Start()
        {
            _lastDebugPose = debugPose;
            ApplyDebugPose();
        }

        private void Update()
        {
            if (debugPose == _lastDebugPose)
                return;

            _lastDebugPose = debugPose;
            ApplyDebugPose();
        }

        private void CaptureBaseline()
        {
            if (casingHingePivot == null ||
                cassette == null ||
                innerSleeve == null)
            {
                Debug.LogError(
                    "[CASSETTE PRESENTATION] " +
                    "Cannot capture baseline. " +
                    "Artifact references are missing.",
                    this
                );

                return;
            }

            _closedLidRotation =
                casingHingePivot.localRotation;

            _seatedCassettePosition =
                cassette.localPosition;

            _seatedCassetteRotation =
                cassette.localRotation;

            _seatedSleevePosition =
                innerSleeve.localPosition;

            _seatedSleeveRotation =
                innerSleeve.localRotation;

            _baselineCaptured = true;

            Debug.Log(
                "[CASSETTE PRESENTATION] " +
                "Closed/seated baseline captured.",
                this
            );
        }

        [ContextMenu("Apply Debug Pose")]
        private void ApplyDebugPose()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "[CASSETTE PRESENTATION] " +
                    "Enter Play Mode before applying poses.",
                    this
                );

                return;
            }

            if (!_baselineCaptured)
            {
                Debug.LogError(
                    "[CASSETTE PRESENTATION] " +
                    "Baseline has not been captured.",
                    this
                );

                return;
            }

            switch (debugPose)
            {
                case DebugPose.CenterClosed:
                    ApplyCenterClosed();
                    break;

                case DebugPose.CaseOpen:
                    ApplyCaseOpen();
                    break;

                case DebugPose.RemovalOrientation:
                    ApplyRemovalOrientation();
                    break;

                case DebugPose.Ready:
                    ApplyReady();
                    break;

                case DebugPose.PlaybackDeparture:
                    ApplyPlaybackDeparture();
                    break;

                case DebugPose.PlayingReadable:
                    ApplyPlayingReadable();
                    break;
            }
        }

        private void ApplyCenterClosed()
        {
            ResetArticulatedParts();
            ApplyRootPose(centerClosedPose);

            LogAppliedPose("CenterClosed");
        }

        private void ApplyCaseOpen()
        {
            ResetArticulatedParts();
            ApplyRootPose(caseOpenPose);
            ApplyOpenLid();

            LogAppliedPose("CaseOpen");
        }

        private void ApplyRemovalOrientation()
        {
            ResetArticulatedParts();
            ApplyRootPose(removalOrientationPose);
            ApplyOpenLid();

            LogAppliedPose("RemovalOrientation");
        }

        private void ApplyReady()
        {
            ResetArticulatedParts();
            ApplyRootPose(readyPose);
            ApplyOpenLid();

            ApplyWorldTarget(
                cassette,
                readyCassetteTarget
            );

            LogAppliedPose("Ready");
        }

        private void ApplyPlaybackDeparture()
        {
            ResetArticulatedParts();
            ApplyRootPose(playbackDeparturePose);
            ApplyOpenLid();

            ApplyWorldTarget(
                cassette,
                cassetteOffscreenTarget
            );

            LogAppliedPose("PlaybackDeparture");
        }

        private void ApplyPlayingReadable()
        {
            ResetArticulatedParts();
            ApplyRootPose(playingReadablePose);
            ApplyOpenLid();

            ApplyWorldTarget(
                cassette,
                cassetteOffscreenTarget
            );

            ApplyWorldTarget(
                innerSleeve,
                sleeveReadableTarget
            );

            LogAppliedPose("PlayingReadable");
        }

        private void ResetArticulatedParts()
        {
            casingHingePivot.localRotation =
                _closedLidRotation;

            cassette.localPosition =
                _seatedCassettePosition;

            cassette.localRotation =
                _seatedCassetteRotation;

            innerSleeve.localPosition =
                _seatedSleevePosition;

            innerSleeve.localRotation =
                _seatedSleeveRotation;
        }

        private void ApplyRootPose(
            Transform pose)
        {
            if (presentationObject == null ||
                pose == null)
            {
                return;
            }

            presentationObject.localPosition =
                pose.localPosition;

            presentationObject.localRotation =
                pose.localRotation;
        }

        private void ApplyOpenLid()
        {
            if (lidOpenTarget == null)
            {
                return;
            }

            casingHingePivot.localRotation =
                lidOpenTarget.localRotation;
        }

        private static void ApplyWorldTarget(
            Transform subject,
            Transform target)
        {
            if (subject == null ||
                target == null)
            {
                return;
            }

            subject.SetPositionAndRotation(
                target.position,
                target.rotation
            );
        }

        private void LogAppliedPose(
            string poseName)
        {
            Debug.Log(
                "[CASSETTE PRESENTATION] " +
                $"Applied {poseName} | " +
                $"localPosition=" +
                $"{presentationObject.localPosition} | " +
                $"localRotation=" +
                $"{presentationObject.localEulerAngles}",
                this
            );
        }
    }
}