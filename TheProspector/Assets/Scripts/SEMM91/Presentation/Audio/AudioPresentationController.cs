using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.Presentation.Audio
{
    public sealed class AudioPresentationController : MonoBehaviour
    {
        private const int ExpectedRandomButtonClipCount = 5;

        [Header("Music")]
        [SerializeField] private AudioSource musicSource;

        [Header("Randomized Button Audio")]
        [SerializeField] private AudioSource randomizedButtonSource;
        [SerializeField] private AudioClip[] randomizedButtonClips;

        [Header("Fixed Button Audio")]
        [SerializeField] private AudioSource fixedButtonSource;
        [SerializeField] private AudioClip fixedButtonClip;

        [Tooltip("Buttons in this list use the fixed sound. All other buttons use randomized sounds.")]
        [SerializeField] private List<Button> fixedSoundButtons = new();

        private readonly List<Button> randomizedBoundButtons = new();
        private readonly List<Button> fixedBoundButtons = new();

        private void Awake()
        {
            if (Application.isBatchMode)
            {
                enabled = false;
                return;
            }

            ValidateAssignments();
        }

        private void Start()
        {
            StartMusic();
            BindExistingButtons();
        }

        private void OnDestroy()
        {
            foreach (Button button in randomizedBoundButtons)
            {
                if (button != null)
                    button.onClick.RemoveListener(PlayRandomizedButtonClick);
            }

            foreach (Button button in fixedBoundButtons)
            {
                if (button != null)
                    button.onClick.RemoveListener(PlayFixedButtonClick);
            }

            randomizedBoundButtons.Clear();
            fixedBoundButtons.Clear();
        }

        private void ValidateAssignments()
        {
            if (musicSource == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioPresentationController)}] Music AudioSource is not assigned.",
                    this);
            }

            if (randomizedButtonSource == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioPresentationController)}] Randomized button AudioSource is not assigned.",
                    this);
            }

            if (fixedButtonSource == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioPresentationController)}] Fixed button AudioSource is not assigned.",
                    this);
            }

            if (randomizedButtonClips == null ||
                randomizedButtonClips.Length != ExpectedRandomButtonClipCount)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioPresentationController)}] Assign exactly " +
                    $"{ExpectedRandomButtonClipCount} randomized button clips.",
                    this);
            }

            if (fixedButtonClip == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioPresentationController)}] Fixed button clip is not assigned.",
                    this);
            }
        }

        private void StartMusic()
        {
            if (musicSource == null || musicSource.clip == null)
                return;

            musicSource.loop = true;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        private void BindExistingButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Button button in buttons)
            {
                if (button == null)
                    continue;

                if (fixedSoundButtons.Contains(button))
                {
                    button.onClick.AddListener(PlayFixedButtonClick);
                    fixedBoundButtons.Add(button);
                }
                else
                {
                    button.onClick.AddListener(PlayRandomizedButtonClick);
                    randomizedBoundButtons.Add(button);
                }
            }

            Debug.Log(
                $"[{nameof(AudioPresentationController)}] Bound " +
                $"{randomizedBoundButtons.Count} randomized buttons and " +
                $"{fixedBoundButtons.Count} fixed-sound buttons.",
                this);
        }

        private void PlayRandomizedButtonClick()
        {
            if (randomizedButtonSource == null ||
                randomizedButtonClips == null ||
                randomizedButtonClips.Length == 0)
            {
                return;
            }

            AudioClip selectedClip = GetRandomAssignedClip();

            if (selectedClip != null)
                randomizedButtonSource.PlayOneShot(selectedClip);
        }

        private AudioClip GetRandomAssignedClip()
        {
            int startIndex = Random.Range(0, randomizedButtonClips.Length);

            for (int offset = 0; offset < randomizedButtonClips.Length; offset++)
            {
                int index = (startIndex + offset) % randomizedButtonClips.Length;
                AudioClip candidate = randomizedButtonClips[index];

                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private void PlayFixedButtonClick()
        {
            if (fixedButtonSource == null || fixedButtonClip == null)
                return;

            fixedButtonSource.PlayOneShot(fixedButtonClip);
        }
    }
}