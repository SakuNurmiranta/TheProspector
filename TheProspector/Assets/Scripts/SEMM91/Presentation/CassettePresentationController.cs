using UnityEngine;
using UnityEngine.Events;

namespace SEMM91.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CassettePresentationController : MonoBehaviour
    {
        private enum UiPhase
        {
            TowerRest,
            PresentingToReady,
            Ready,
            PresentingToPlaying,
            Playing,
            PresentingPlayingToReady,
            PresentingReadyToTower
        }

        [Header("Director Presentation IDs")]
        [SerializeField]
        private string toReadyPresentationId =
            "Cassette_To_Ready";

        [SerializeField]
        private string toPlayingPresentationId =
            "Cassette_To_Playing";

        [SerializeField]
        private string playingToReadyPresentationId =
            "Cassette_Playing_To_Ready";

        [SerializeField]
        private string readyToTowerPresentationId =
            "Cassette_Ready_To_Tower";

        [Header("Presentation Rig")]
        [SerializeField]
        private GameObject presentationRoot;

        [SerializeField]
        private CassettePresentationRigView presentationRigView;

        [Header("Presentation Controls")]
        [SerializeField]
        private GameObject playButtonRoot;

        [SerializeField]
        private GameObject returnButtonRoot;

        [Header("Return")]
        [SerializeField]
        private UnityEvent onReturnedToTower;

        private UiPhase _phase =
            UiPhase.TowerRest;

        private float _phaseCompletesAt;
        private bool _waitingForDirectorDuration;

        private string _presentedDemoTapeId =
            string.Empty;

        private MediaShelfItemView _sourceTowerView;

        public string PresentedDemoTapeId =>
            _presentedDemoTapeId;

        public bool IsReady =>
            _phase == UiPhase.Ready;

        public bool IsPlaying =>
            _phase == UiPhase.Playing;

        private void Awake()
        {
            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }
        }

        private void Start()
        {
            RefreshControls();
        }

        private void Update()
        {
            if (!_waitingForDirectorDuration)
                return;

            if (Time.unscaledTime <
                _phaseCompletesAt)
            {
                return;
            }

            _waitingForDirectorDuration = false;

            switch (_phase)
            {
                case UiPhase.PresentingToReady:
                    _phase = UiPhase.Ready;
                    break;

                case UiPhase.PresentingToPlaying:
                    _phase = UiPhase.Playing;
                    break;

                case UiPhase.PresentingPlayingToReady:
                    _phase = UiPhase.Ready;
                    break;

                case UiPhase.PresentingReadyToTower:
                    CompleteReturnToTower();
                    return;
            }

            RefreshControls();
        }

        public void PresentToReady(
            MediaShelfItemView sourceView,
            string sourceDemoTapeId)
        {
            if (sourceView == null)
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "Source cassette view is missing.",
                    this
                );
                return;
            }

            if (presentationRigView == null)
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "PresentationRigView is not assigned.",
                    this
                );
                return;
            }

            CassettePresentationData data =
                new CassettePresentationData(
                    sourceDemoTapeId,
                    sourceView.DisplayTitle
                );

            if (!presentationRigView.Apply(
                    data,
                    sourceView.transform))
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "Could not project the selected cassette " +
                    "into the presentation rig.",
                    this
                );
                return;
            }

            _sourceTowerView = sourceView;

            PresentToReady(
                sourceDemoTapeId
            );
        }

        public void PresentToReady(
            string sourceDemoTapeId)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceDemoTapeId))
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "Cannot present an empty DemoTapeId.",
                    this
                );
                RestoreSourceTowerView();
                return;
            }

            PresentationAnimationDirector director =
                PresentationAnimationDirector.Instance;

            if (director == null)
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "PresentationAnimationDirector is missing.",
                    this
                );
                RestoreSourceTowerView();
                return;
            }

            if (!director.TryGetDuration(
                    toReadyPresentationId,
                    out float durationSeconds))
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    $"Director has no valid duration for " +
                    $"'{toReadyPresentationId}'.",
                    this
                );
                RestoreSourceTowerView();
                return;
            }

            _presentedDemoTapeId =
                sourceDemoTapeId;

            _phase =
                UiPhase.PresentingToReady;

            if (_sourceTowerView != null)
            {
                _sourceTowerView.SetTowerRepresentationVisible(false);
            }

            RefreshControls();

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(true);
            }

            if (!director.Play(
                    toReadyPresentationId))
            {
                _phase = UiPhase.TowerRest;
                _presentedDemoTapeId =
                    string.Empty;

                if (presentationRoot != null)
                {
                    presentationRoot.SetActive(false);
                }

                RestoreSourceTowerView();
                RefreshControls();

                Debug.LogError(
                    "[CASSETTE UI] " +
                    $"Director could not play " +
                    $"'{toReadyPresentationId}'.",
                    this
                );
                return;
            }

            WaitForDirectorDuration(
                durationSeconds
            );
        }

        public void PlayFromReady()
        {
            if (_phase != UiPhase.Ready)
                return;

            PresentationAnimationDirector director =
                PresentationAnimationDirector.Instance;

            if (director == null)
                return;

            if (!director.TryGetDuration(
                    toPlayingPresentationId,
                    out float durationSeconds))
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    $"Director has no valid duration for " +
                    $"'{toPlayingPresentationId}'.",
                    this
                );
                return;
            }

            _phase =
                UiPhase.PresentingToPlaying;

            RefreshControls();

            if (!director.Play(
                    toPlayingPresentationId))
            {
                _phase = UiPhase.Ready;
                RefreshControls();
                return;
            }

            WaitForDirectorDuration(
                durationSeconds
            );
        }

        public void ReturnToTower()
        {
            if (_phase == UiPhase.Playing)
            {
                PlayReturnTransition(
                    playingToReadyPresentationId,
                    UiPhase.PresentingPlayingToReady,
                    UiPhase.Playing
                );
                return;
            }

            if (_phase == UiPhase.Ready)
            {
                PlayReturnTransition(
                    readyToTowerPresentationId,
                    UiPhase.PresentingReadyToTower,
                    UiPhase.Ready
                );
            }
        }

        private void PlayReturnTransition(
            string presentationId,
            UiPhase transitionPhase,
            UiPhase fallbackPhase)
        {
            PresentationAnimationDirector director =
                PresentationAnimationDirector.Instance;

            if (director == null)
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    "PresentationAnimationDirector is missing.",
                    this
                );
                return;
            }

            if (!director.TryGetDuration(
                    presentationId,
                    out float durationSeconds))
            {
                Debug.LogError(
                    "[CASSETTE UI] " +
                    $"Director has no valid duration for " +
                    $"'{presentationId}'.",
                    this
                );
                return;
            }

            _phase = transitionPhase;
            RefreshControls();

            if (!director.Play(
                    presentationId))
            {
                _phase = fallbackPhase;
                RefreshControls();

                Debug.LogError(
                    "[CASSETTE UI] " +
                    $"Director could not play " +
                    $"'{presentationId}'.",
                    this
                );
                return;
            }

            WaitForDirectorDuration(
                durationSeconds
            );
        }

        private void CompleteReturnToTower()
        {
            _waitingForDirectorDuration = false;
            _phaseCompletesAt = 0.0f;

            _phase = UiPhase.TowerRest;
            _presentedDemoTapeId =
                string.Empty;

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }

            RestoreSourceTowerView();
            RefreshControls();
            onReturnedToTower?.Invoke();
        }

        private void RestoreSourceTowerView()
        {
            if (_sourceTowerView == null)
                return;

            _sourceTowerView.SetTowerRepresentationVisible(true);
            _sourceTowerView = null;
        }

        private void WaitForDirectorDuration(
            float durationSeconds)
        {
            _phaseCompletesAt =
                Time.unscaledTime +
                Mathf.Max(
                    0.0f,
                    durationSeconds
                );

            _waitingForDirectorDuration = true;
        }

        private void RefreshControls()
        {
            bool showPlay =
                _phase == UiPhase.Ready;

            bool showReturn =
                _phase == UiPhase.Ready ||
                _phase == UiPhase.Playing;

            if (playButtonRoot != null)
            {
                playButtonRoot.SetActive(
                    showPlay
                );
            }

            if (returnButtonRoot != null)
            {
                returnButtonRoot.SetActive(
                    showReturn
                );
            }
        }
    }
}