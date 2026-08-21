using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Events;

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

        [Header("Presentation Controls")]
        [SerializeField]
        private GameObject playButtonRoot;

        [SerializeField]
        private GameObject returnButtonRoot;

        [Header("Presentation Failsafes")]
        [SerializeField, Min(0.1f)]
        private float readyFallbackSeconds = 4.0f;

        private float _readyFallbackElapsed;
        private bool _waitingForReadyFallback;
        
        [Header("Live Artifact")] [SerializeField]
        private Transform presentationObject;

        [SerializeField] private Transform casingHingePivot;

        [SerializeField] private Transform cassette;

        [SerializeField] private Transform innerSleeve;

        [Header("Root Poses")] [SerializeField]
        private Transform centerClosedPose;

        [SerializeField] private Transform caseOpenPose;

        [SerializeField] private Transform removalOrientationPose;

        [SerializeField] private Transform readyPose;

        [SerializeField] private Transform playbackDeparturePose;

        [SerializeField] private Transform playingReadablePose;

        [Header("Articulation Targets")] [SerializeField]
        private Transform lidOpenTarget;

        [SerializeField] private Transform readyCassetteTarget;

        [SerializeField] private Transform cassetteOffscreenTarget;

        [SerializeField] private Transform sleeveReadableTarget;

        [Header("Segment 1")] [SerializeField] private SplineAnimate centerClosedToCaseOpenSpline;

        [SerializeField] private Animator presentationAnimator;

        [SerializeField] private string centerClosedToCaseOpenState =
            "Cassette_CenterClosed_To_CaseOpen";

        [Header("Segment 2")] [SerializeField] private SplineAnimate caseOpenToRemovalOrientationSpline;

        [SerializeField] private string caseOpenToRemovalOrientationState =
            "Cassette_CaseOpen_To_RemovalOrientation";

        [Header("Segment 3")] [SerializeField] private SplineAnimate removalOrientedToReadySpline;


        [SerializeField] private string removalOrientedToReadyState =
            "Cassette_RemovalOriented_To_Ready";

        [Header("Segment 4")] [SerializeField] private SplineAnimate readyToPlaybackDepartureSpline;

        [SerializeField] private string readyToPlaybackDepartureState =
            "Cassette_Ready_To_PlaybackDeparture";

        [ContextMenu("Play CenterClosed To Ready")]
        private void DebugPlayCenterClosedToReady()
        {
            if (!Application.isPlaying)
                return;

            ApplyCenterClosed();
            PlayToReady();
        }

        [Header("Segment 5")] [SerializeField] private SplineAnimate playbackDepartureToPlayingReadableSpline;

        [SerializeField] private string playbackDepartureToPlayingReadableState =
            "Cassette_PlaybackDeparture_To_PlayingReadable";

        [Header("Debug")] [SerializeField] private DebugPose debugPose;

        [Header("Return To Tower")]
        [SerializeField]
        private SplineAnimate
            readyToRemovalOrientedReturnSpline;

        [SerializeField]
        private SplineAnimate
            caseOpenToCenterClosedReturnSpline;

        [SerializeField]
        private string readyToRemovalOrientationReturnState =
            "Cassette_Ready_To_RemovalOrientation_RETURN";

        [SerializeField]
        private string removalOrientationToCaseOpenReturnState =
            "Cassette_RemovalOrientation_To_CaseOpen_RETURN";

        [SerializeField]
        private string caseOpenToCenterClosedReturnState =
            "Cassette_CaseOpen_To_CenterClosed_RETURN";

        [SerializeField]
        private UnityEvent onReturnedToTower;

        private bool _returning;
        
        private DebugPose _lastDebugPose;

        private Quaternion _closedLidRotation;

        private Vector3 _seatedCassettePosition;
        private Quaternion _seatedCassetteRotation;

        private Vector3 _seatedSleevePosition;
        private Quaternion _seatedSleeveRotation;

        private bool _baselineCaptured;

        private string _presentedDemoTapeId =
            string.Empty;

        public string PresentedDemoTapeId =>
            _presentedDemoTapeId;
        
        private void Awake()
        {
            CaptureBaseline();
        }

        private void Start()
        {
            _lastDebugPose = debugPose;
            ApplyDebugPose();

            RefreshControls();
        }

        private void Update()
        {
            if (debugPose != _lastDebugPose)
            {
                _lastDebugPose = debugPose;
                ApplyDebugPose();
            }

            TickReadyFallback();
        }

        private void TickReadyFallback()
        {
            if (!_waitingForReadyFallback)
                return;

            _readyFallbackElapsed += Time.deltaTime;

            if (_readyFallbackElapsed < readyFallbackSeconds)
                return;

            Debug.LogWarning(
                "[CASSETTE PRESENTATION] " +
                "Ready animation exceeded watchdog. " +
                "Forcing Ready state.",
                this
            );

            CompleteReady(forcePose: true);
        }

        private void CompleteReady(bool forcePose)
        {
            if (!_waitingForReadyFallback &&
                State == PresentationState.Ready)
            {
                return;
            }

            _waitingForReadyFallback = false;
            _readyFallbackElapsed = 0.0f;

            if (forcePose)
            {
                PausePresentationMotion();

                if (presentationAnimator != null)
                {
                    presentationAnimator.enabled = false;
                }

                ApplyReady();
            }

            State = PresentationState.Ready;
            IsTransitioning = false;

            RefreshControls();

            Debug.Log(
                forcePose
                    ? "[CASSETTE PRESENTATION] " +
                      "State = Ready (watchdog correction)"
                    : "[CASSETTE PRESENTATION] State = Ready",
                this
            );
        }

        private void PausePresentationMotion()
        {
            if (centerClosedToCaseOpenSpline != null)
                centerClosedToCaseOpenSpline.Pause();

            if (caseOpenToRemovalOrientationSpline != null)
                caseOpenToRemovalOrientationSpline.Pause();

            if (removalOrientedToReadySpline != null)
                removalOrientedToReadySpline.Pause();

            if (readyToPlaybackDepartureSpline != null)
                readyToPlaybackDepartureSpline.Pause();

            if (playbackDepartureToPlayingReadableSpline != null)
                playbackDepartureToPlayingReadableSpline.Pause();
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

        [ContextMenu("Play Segment 1 - CenterClosed To CaseOpen")]
        private void DebugPlaySegment1()
        {
            if (!Application.isPlaying)
                return;

            ApplyCenterClosed();
            PlayToReady();
        }

        public void PlayToReady()
        {
            presentationAnimator.Play(
                centerClosedToCaseOpenState,
                0,
                0f
            );

            centerClosedToCaseOpenSpline.Play();
        }

        public void PlaySegment2()
        {
            if (_returning)
                return;
            
            presentationAnimator.Play(
                caseOpenToRemovalOrientationState,
                0,
                0f
            );

            // Internal handoff: preserve the live transform at the spline join.
            caseOpenToRemovalOrientationSpline.Play();
        }

        public void PlaySegment3()
        {
            if (_returning)
                return;
            
            presentationAnimator.Play(
                removalOrientedToReadyState,
                0,
                0f
            );

            // Internal handoff: preserve the live transform at the spline join.
            removalOrientedToReadySpline.Play();
        }

        [ContextMenu("Play Segment 4 - Ready To PlaybackDeparture")]
        private void DebugPlaySegment4()
        {
            if (!Application.isPlaying)
                return;

            ApplyReady();
            PlaySegment4();

            Debug.Log(
                "[CASSETTE PRESENTATION] " +
                "Playing Segment 4: Ready -> PlaybackDeparture.",
                this
            );
        }

        [ContextMenu("Play Segment 5 - PlaybackDeparture To PlayingReadable")]
        private void DebugPlaySegment5()
        {
            if (!Application.isPlaying)
                return;

            ApplyPlaybackDeparture();

            presentationAnimator.Play(
                playbackDepartureToPlayingReadableState,
                0,
                0f
            );

            playbackDepartureToPlayingReadableSpline.Restart(true);

            Debug.Log(
                "[CASSETTE PRESENTATION] " +
                "Playing Segment 5: PlaybackDeparture -> PlayingReadable.",
                this
            );
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


        public void PlaySegment4()
        {
            presentationAnimator.Play(
                readyToPlaybackDepartureState,
                0,
                0f
            );

            readyToPlaybackDepartureSpline.Play();
        }

        public void PlaySegment5()
        {
            presentationAnimator.Play(
                playbackDepartureToPlayingReadableState,
                0,
                0f
            );

            // Internal handoff: avoid Restart(), which snaps to spline start.
            playbackDepartureToPlayingReadableSpline.Play();
        }

        private void OnEnable()
        {
            centerClosedToCaseOpenSpline.Completed +=
                HandleSegment1Completed;
        }

        private void OnDisable()
        {
            centerClosedToCaseOpenSpline.Completed -=
                HandleSegment1Completed;
        }

        private void HandleSegment1Completed()
        {
            PlaySegment2();
        }
        
        public void PresentToReady(
            string sourceDemoTapeId)
        {
            IsTransitioning = true;
            RefreshControls();
            
            readyToRemovalOrientedReturnSpline
                .Restart(false);

            caseOpenToCenterClosedReturnSpline
                .Restart(false);
            
            if (string.IsNullOrWhiteSpace(
                    sourceDemoTapeId
                ))
            {
                Debug.LogError(
                    "[CASSETTE PRESENTATION] " +
                    "Cannot present an empty DemoTapeId.",
                    this
                );

                return;
            }

            if (presentationObject == null)
            {
                Debug.LogError(
                    "[CASSETTE PRESENTATION] " +
                    "PresentationObject is missing.",
                    this
                );

                return;
            }

            _presentedDemoTapeId =
                sourceDemoTapeId;

            /*
             * Re-arm every forward spline while the
             * presentation artifact is invisible.
             *
             * Restart(false) is safe here precisely
             * because the player cannot see the reset.
             */
            presentationObject.gameObject
                .SetActive(false);

            centerClosedToCaseOpenSpline
                .Restart(false);

            caseOpenToRemovalOrientationSpline
                .Restart(false);

            removalOrientedToReadySpline
                .Restart(false);

            readyToPlaybackDepartureSpline
                .Restart(false);

            playbackDepartureToPlayingReadableSpline
                .Restart(false);

            /*
             * The last Restart() above moved the shared
             * presentation root around. Restore the real
             * beginning pose before exposing it again.
             */
            ApplyCenterClosed();

            presentationObject.gameObject
                .SetActive(true);

            if (presentationAnimator != null)
            {
                presentationAnimator.enabled = true;
            }

            _readyFallbackElapsed = 0.0f;
            _waitingForReadyFallback = true;

            PlayToReady();

            Debug.Log(
                "[CASSETTE PRESENTATION] " +
                $"Presenting DemoTape " +
                $"{_presentedDemoTapeId} -> Ready.",
                this
            );
        }
        
        public enum PresentationState
        {
            TowerRest,
            Ready,
            Playing
        }

        public PresentationState State { get; private set; }
            = PresentationState.TowerRest;

        public bool IsTransitioning { get; private set; }
        
        private void HandleReadyCompleted()
        {
            State = PresentationState.Ready;
            IsTransitioning = false;

            Debug.Log(
                "[CASSETTE PRESENTATION] State = Ready",
                this
            );
        }

        private void HandlePlayingCompleted()
        {
            State = PresentationState.Playing;
            IsTransitioning = false;

            Debug.Log(
                "[CASSETTE PRESENTATION] State = Playing",
                this
            );
        }
        
        public void PlayFromReady()
        {
            if (State != PresentationState.Ready ||
                IsTransitioning)
            {
                Debug.LogWarning(
                    "[CASSETTE PRESENTATION] " +
                    $"Cannot play from state {State}.",
                    this
                );

                return;
            }

            IsTransitioning = true;
            RefreshControls();

            if (presentationAnimator != null)
            {
                presentationAnimator.enabled = true;
            }

            PlaySegment4();
        }
        
        public void NotifyReadyReached()
        {
            CompleteReady(forcePose: false);
        }

        public void NotifyPlayingReached()
        {
            State = PresentationState.Playing;
            IsTransitioning = false;

            RefreshControls();

            Debug.Log(
                "[CASSETTE PRESENTATION] State = Playing",
                this
            );
        }
        
        [ContextMenu("Play Ready To Playing")]
        private void DebugPlayReadyToPlaying()
        {
            if (!Application.isPlaying)
                return;

            ApplyReady();

            State = PresentationState.Ready;
            IsTransitioning = false;

            PlayFromReady();
        }
        
        public void ReturnToTower()
        {
            if (State != PresentationState.Ready ||
                IsTransitioning)
            {
                return;
            }

            IsTransitioning = true;
            RefreshControls();

            _waitingForReadyFallback = false;
            _readyFallbackElapsed = 0.0f;
            PausePresentationMotion();

            _presentedDemoTapeId = string.Empty;

            presentationObject.gameObject.SetActive(false);

            State = PresentationState.TowerRest;
            IsTransitioning = false;

            RefreshControls();

            onReturnedToTower?.Invoke();
        }
        
        public void PlayReturnSegment3()
        {
            if (!_returning)
                return;

            presentationAnimator.Play(
                readyToRemovalOrientationReturnState,
                0,
                1f
            );

            readyToRemovalOrientedReturnSpline.Play();
        }

        public void PlayReturnSegment2()
        {
            if (!_returning)
                return;

            presentationAnimator.Play(
                removalOrientationToCaseOpenReturnState,
                0,
                1f
            );
        }

        public void PlayReturnSegment1()
        {
            if (!_returning)
                return;

            presentationAnimator.Play(
                caseOpenToCenterClosedReturnState,
                0,
                1f
            );

            caseOpenToCenterClosedReturnSpline.Play();
        }
        
        public void NotifyTowerRestReached()
        {
            if (!_returning)
                return;

            _returning = false;
            IsTransitioning = false;
            State = PresentationState.TowerRest;

            _presentedDemoTapeId =
                string.Empty;

            presentationObject.gameObject
                .SetActive(false);

            onReturnedToTower?.Invoke();

            Debug.Log(
                "[CASSETTE PRESENTATION] State = TowerRest",
                this
            );
        }
        
        private void RefreshControls()
        {
            bool ready =
                State == PresentationState.Ready &&
                !IsTransitioning;

            if (playButtonRoot != null)
            {
                playButtonRoot.SetActive(ready);
            }

            if (returnButtonRoot != null)
            {
                returnButtonRoot.SetActive(ready);
            }
        }
        
        [ContextMenu("Debug Spline References")]
        private void DebugSplineReferences()
        {
            Debug.Log(
                "[SPLINE REFERENCES]\n" +
                $"S1 instance={centerClosedToCaseOpenSpline.GetInstanceID()} " +
                $"container={centerClosedToCaseOpenSpline.Container?.name}\n" +

                $"S2 instance={caseOpenToRemovalOrientationSpline.GetInstanceID()} " +
                $"container={caseOpenToRemovalOrientationSpline.Container?.name}\n" +

                $"S3 instance={removalOrientedToReadySpline.GetInstanceID()} " +
                $"container={removalOrientedToReadySpline.Container?.name}\n" +

                $"S4 instance={readyToPlaybackDepartureSpline.GetInstanceID()} " +
                $"container={readyToPlaybackDepartureSpline.Container?.name}\n" +

                $"S5 instance={playbackDepartureToPlayingReadableSpline.GetInstanceID()} " +
                $"container={playbackDepartureToPlayingReadableSpline.Container?.name}",
                this
            );
        }
        
    }
}