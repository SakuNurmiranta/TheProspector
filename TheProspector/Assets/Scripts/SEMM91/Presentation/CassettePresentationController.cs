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
            Playing
        }

        [Header("Director Presentation IDs")]
        [SerializeField]
        private string toReadyPresentationId =
            "Cassette_To_Ready";

        [SerializeField]
        private string toPlayingPresentationId =
            "Cassette_To_Playing";

        [Header("Presentation Visibility")]
        [SerializeField]
        private GameObject presentationRoot;

        [Header("Presentation Controls")]
        [SerializeField]
        private GameObject playButtonRoot;

        [SerializeField]
        private GameObject returnButtonRoot;

        [Header("Return To Tower")]
        [SerializeField]
        private UnityEvent onReturnedToTower;

        private UiPhase _phase =
            UiPhase.TowerRest;

        private float _phaseCompletesAt;
        private bool _waitingForDirectorDuration;

        private string _presentedDemoTapeId =
            string.Empty;

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

            if (Time.unscaledTime < _phaseCompletesAt)
                return;

            _waitingForDirectorDuration = false;

            switch (_phase)
            {
                case UiPhase.PresentingToReady:
                    _phase = UiPhase.Ready;
                    break;

                case UiPhase.PresentingToPlaying:
                    _phase = UiPhase.Playing;
                    break;
            }

            RefreshControls();
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
                return;
            }

            _presentedDemoTapeId =
                sourceDemoTapeId;

            _phase =
                UiPhase.PresentingToReady;

            RefreshControls();

            
            if (presentationRoot != null)
            {
                presentationRoot.SetActive(true);
            }

            if (!director.Play(
                    toReadyPresentationId))
            {
                _phase = UiPhase.TowerRest;
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
            if (_phase != UiPhase.Ready &&
                _phase != UiPhase.Playing)
            {
                return;
            }

            _waitingForDirectorDuration = false;
            _phaseCompletesAt = 0.0f;

            _phase = UiPhase.TowerRest;
            _presentedDemoTapeId =
                string.Empty;

            RefreshControls();

            if (presentationRoot != null)
            {
                presentationRoot.SetActive(false);
            }

            onReturnedToTower?.Invoke();
        }

        private void WaitForDirectorDuration(
            float durationSeconds)
        {
            _phaseCompletesAt =
                Time.unscaledTime +
                Mathf.Max(0.0f, durationSeconds);

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