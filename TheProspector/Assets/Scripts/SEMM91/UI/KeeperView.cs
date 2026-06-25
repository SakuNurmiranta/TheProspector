using SEMM91.GamePlay.Keeper;
using SEMM91.InputSystems;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public class KeeperView : ContextualUIView
    {
        private const float MaximumUsefulPullSpend =
            1.0f;

        private const float MinimumSuggestedPullSpend =
            0.05f;

        [Header("Keeper State")]
        [SerializeField]
        private TextMeshProUGUI keeperStatusText;

        [Header("Release Target")]
        [SerializeField]
        private TextMeshProUGUI targetText;

        [SerializeField]
        private Button previousTargetButton;

        [SerializeField]
        private Button nextTargetButton;

        [Header("Pull")]
        [SerializeField]
        private Slider pullSpendSlider;

        [SerializeField]
        private TextMeshProUGUI pullSpendText;

        [SerializeField]
        [Min(0.01f)]
        private float defaultPullSpend =
            0.25f;

        [Header("Interventions")]
        [SerializeField]
        private Button boostButton;

        [SerializeField]
        private Button suppressButton;

        [Header("Feedback")]
        [SerializeField]
        private TextMeshProUGUI feedbackText;

        private TextMeshProUGUI _boostButtonText;
        private TextMeshProUGUI _suppressButtonText;

        private PlayerActionController
            _actionController;

        private int _selectedReleaseIndex;
        private int _releaseCount;

        private string _selectedReleaseId =
            string.Empty;

        private float _requestedPullSpend;
        private float _boostPullSpend;
        private float _suppressPullSpend;

        private bool _pullSpendInitialized;

        private void Awake()
        {
            if (previousTargetButton != null)
            {
                previousTargetButton.onClick.AddListener(
                    SelectPreviousTarget
                );
            }

            if (nextTargetButton != null)
            {
                nextTargetButton.onClick.AddListener(
                    SelectNextTarget
                );
            }

            if (boostButton != null)
            {
                _boostButtonText =
                    boostButton
                        .GetComponentInChildren<
                            TextMeshProUGUI
                        >(true);

                boostButton.onClick.AddListener(
                    RequestBoost
                );
            }

            if (suppressButton != null)
            {
                _suppressButtonText =
                    suppressButton
                        .GetComponentInChildren<
                            TextMeshProUGUI
                        >(true);

                suppressButton.onClick.AddListener(
                    RequestSuppress
                );
            }

            if (pullSpendSlider != null)
            {
                pullSpendSlider.wholeNumbers =
                    false;

                pullSpendSlider.onValueChanged
                    .AddListener(
                        OnPullSpendChanged
                    );
            }
        }

        private void OnDestroy()
        {
            if (previousTargetButton != null)
            {
                previousTargetButton.onClick
                    .RemoveListener(
                        SelectPreviousTarget
                    );
            }

            if (nextTargetButton != null)
            {
                nextTargetButton.onClick
                    .RemoveListener(
                        SelectNextTarget
                    );
            }

            if (boostButton != null)
            {
                boostButton.onClick.RemoveListener(
                    RequestBoost
                );
            }

            if (suppressButton != null)
            {
                suppressButton.onClick.RemoveListener(
                    RequestSuppress
                );
            }

            if (pullSpendSlider != null)
            {
                pullSpendSlider.onValueChanged
                    .RemoveListener(
                        OnPullSpendChanged
                    );
            }
        }

        public override bool Supports(
            GameUIState state)
        {
            return state ==
                   GameUIState.Keeper;
        }

        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            _actionController =
                state != null
                    ? state.GetComponent<
                        PlayerActionController
                    >()
                    : null;

            if (state == null ||
                !context
                    .HasKeeperInterventionSnapshot)
            {
                ShowConnectingState();
                return;
            }

            DomainSnapshotReplicator
                    .KeeperInterventionDebugSnapshot
                keeperSnapshot =
                    context
                        .KeeperInterventionSnapshot;

            if (!context.IsKeeper ||
                !keeperSnapshot.HasTenure ||
                keeperSnapshot.KeeperClientId !=
                context.LocalClientId)
            {
                ShowUnavailableState(
                    state,
                    "The local player does not hold " +
                    "the current Keeper tenure."
                );

                return;
            }

            bool turnOpen =
                state.ActiveValue &&
                !state.HasCommittedTurnValue;

            SetText(
                keeperStatusText,
                "Keeper Pull: " +
                $"{keeperSnapshot.Pull:F2}\n" +
                "Boost: " +
                FormatAvailability(
                    keeperSnapshot.BoostAvailable
                ) +
                "\nSuppress: " +
                FormatAvailability(
                    keeperSnapshot
                        .SuppressAvailable
                )
            );

            ConfigurePullControl(
                keeperSnapshot.Pull,
                turnOpen
            );

            RefreshFeedback(state);

            _releaseCount =
                context.KeeperReleaseCount;

            if (_releaseCount <= 0)
            {
                _selectedReleaseIndex = 0;
                _selectedReleaseId =
                    string.Empty;

                SetText(
                    targetText,
                    "No released demos are currently " +
                    "available."
                );

                SetTargetNavigationInteractable(
                    false
                );

                SetInterventionButtons(
                    boostAvailable: false,
                    suppressAvailable: false,
                    boostLabel:
                        "Boost\nNo target",
                    suppressLabel:
                        "Suppress\nNo target"
                );

                return;
            }

            _selectedReleaseIndex =
                Mathf.Clamp(
                    _selectedReleaseIndex,
                    0,
                    _releaseCount - 1
                );

            if (!context.TryGetKeeperReleaseRow(
                    _selectedReleaseIndex,
                    out DomainSnapshotReplicator
                        .KeeperReleaseDebugRow row
                ))
            {
                _selectedReleaseId =
                    string.Empty;

                SetText(
                    targetText,
                    "Selected release is synchronizing."
                );

                SetTargetNavigationInteractable(
                    false
                );

                SetInterventionButtons(
                    boostAvailable: false,
                    suppressAvailable: false,
                    boostLabel:
                        "Boost\nSynchronizing",
                    suppressLabel:
                        "Suppress\nSynchronizing"
                );

                return;
            }

            _selectedReleaseId =
                row.ReleaseId.ToString();

            SetText(
                targetText,
                $"Target " +
                $"{_selectedReleaseIndex + 1}/" +
                $"{_releaseCount}\n" +
                $"{row.DisplayName}\n" +
                $"Owner: {row.OwnerName}\n" +
                $"Organic visibility: " +
                $"{row.OrganicVisibility:F2}\n" +
                $"Effective visibility: " +
                $"{row.EffectiveVisibility:F2}\n" +
                $"Pending adjustment: " +
                $"{row.PendingVisibilityAdjustment:F2}"
            );

            SetTargetNavigationInteractable(
                _releaseCount > 1
            );

            float boostRoom =
                Mathf.Max(
                    0.0f,
                    1.0f -
                    row.EffectiveVisibility
                );

            float suppressRoom =
                Mathf.Max(
                    0.0f,
                    row.EffectiveVisibility
                );

            _boostPullSpend =
                CalculateUsefulPullSpend(
                    _requestedPullSpend,
                    keeperSnapshot.Pull,
                    boostRoom
                );

            _suppressPullSpend =
                CalculateUsefulPullSpend(
                    _requestedPullSpend,
                    keeperSnapshot.Pull,
                    suppressRoom
                );

            bool canSubmit =
                turnOpen &&
                _actionController != null;

            bool boostAvailable =
                canSubmit &&
                keeperSnapshot.BoostAvailable &&
                row.CanReceiveBoost &&
                _boostPullSpend >
                KeeperPullRules
                    .ComparisonTolerance;

            bool suppressAvailable =
                canSubmit &&
                keeperSnapshot.SuppressAvailable &&
                row.CanReceiveSuppress &&
                _suppressPullSpend >
                KeeperPullRules
                    .ComparisonTolerance;

            string boostLabel;

            if (!keeperSnapshot.BoostAvailable)
            {
                boostLabel =
                    "Boost\nUsed this turn";
            }
            else if (!row.CanReceiveBoost ||
                     _boostPullSpend <=
                     KeeperPullRules
                         .ComparisonTolerance)
            {
                boostLabel =
                    "Boost\nNo effect";
            }
            else
            {
                boostLabel =
                    $"Boost\n" +
                    $"{_boostPullSpend:F2} Pull";
            }

            string suppressLabel;

            if (!keeperSnapshot
                    .SuppressAvailable)
            {
                suppressLabel =
                    "Suppress\nUsed this turn";
            }
            else if (!row.CanReceiveSuppress ||
                     _suppressPullSpend <=
                     KeeperPullRules
                         .ComparisonTolerance)
            {
                suppressLabel =
                    "Suppress\nNo effect";
            }
            else
            {
                suppressLabel =
                    $"Suppress\n" +
                    $"{_suppressPullSpend:F2} Pull";
            }

            SetInterventionButtons(
                boostAvailable,
                suppressAvailable,
                boostLabel,
                suppressLabel
            );
        }

        private void ConfigurePullControl(
            float availablePull,
            bool turnOpen)
        {
            float maximum =
                Mathf.Min(
                    MaximumUsefulPullSpend,
                    Mathf.Max(
                        0.0f,
                        availablePull
                    )
                );

            if (maximum <=
                KeeperPullRules
                    .ComparisonTolerance)
            {
                _requestedPullSpend =
                    0.0f;

                if (pullSpendSlider != null)
                {
                    pullSpendSlider.minValue =
                        0.0f;

                    pullSpendSlider.maxValue =
                        1.0f;

                    pullSpendSlider
                        .SetValueWithoutNotify(
                            0.0f
                        );

                    pullSpendSlider.interactable =
                        false;
                }

                SetText(
                    pullSpendText,
                    "Requested Pull: 0.00"
                );

                return;
            }

            float minimum =
                Mathf.Min(
                    MinimumSuggestedPullSpend,
                    maximum
                );

            float sourceValue =
                _pullSpendInitialized
                    ? _requestedPullSpend
                    : defaultPullSpend;

            _requestedPullSpend =
                RoundToHundredth(
                    Mathf.Clamp(
                        sourceValue,
                        minimum,
                        maximum
                    )
                );

            _pullSpendInitialized =
                true;

            if (pullSpendSlider != null)
            {
                pullSpendSlider.minValue =
                    minimum;

                pullSpendSlider.maxValue =
                    maximum;

                pullSpendSlider
                    .SetValueWithoutNotify(
                        _requestedPullSpend
                    );

                pullSpendSlider.interactable =
                    turnOpen;
            }

            SetText(
                pullSpendText,
                "Requested Pull: " +
                $"{_requestedPullSpend:F2}"
            );
        }

        private static float
            CalculateUsefulPullSpend(
                float requestedPull,
                float availablePull,
                float availableEffectRoom)
        {
            float usefulAmount =
                Mathf.Min(
                    requestedPull,
                    Mathf.Min(
                        availablePull,
                        Mathf.Clamp01(
                            availableEffectRoom
                        )
                    )
                );

            if (usefulAmount <=
                KeeperPullRules
                    .ComparisonTolerance)
            {
                return 0.0f;
            }

            return RoundToHundredth(
                usefulAmount
            );
        }

        private void OnPullSpendChanged(
            float value)
        {
            _requestedPullSpend =
                RoundToHundredth(value);

            if (pullSpendSlider != null &&
                !Mathf.Approximately(
                    pullSpendSlider.value,
                    _requestedPullSpend
                ))
            {
                pullSpendSlider
                    .SetValueWithoutNotify(
                        _requestedPullSpend
                    );
            }

            SetText(
                pullSpendText,
                "Requested Pull: " +
                $"{_requestedPullSpend:F2}"
            );
        }

        private void SelectPreviousTarget()
        {
            CycleTarget(-1);
        }

        private void SelectNextTarget()
        {
            CycleTarget(1);
        }

        private void CycleTarget(
            int direction)
        {
            if (_releaseCount <= 0)
                return;

            _selectedReleaseIndex =
                (
                    _selectedReleaseIndex +
                    direction +
                    _releaseCount
                ) %
                _releaseCount;
        }

        private void RequestBoost()
        {
            if (_actionController == null ||
                string.IsNullOrWhiteSpace(
                    _selectedReleaseId
                ) ||
                _boostPullSpend <=
                KeeperPullRules
                    .ComparisonTolerance)
            {
                return;
            }

            _actionController
                .RequestKeeperBoostVisibility(
                    _selectedReleaseId,
                    _boostPullSpend
                );
        }

        private void RequestSuppress()
        {
            if (_actionController == null ||
                string.IsNullOrWhiteSpace(
                    _selectedReleaseId
                ) ||
                _suppressPullSpend <=
                KeeperPullRules
                    .ComparisonTolerance)
            {
                return;
            }

            _actionController
                .RequestKeeperSuppressVisibility(
                    _selectedReleaseId,
                    _suppressPullSpend
                );
        }

        private void RefreshFeedback(
            NetPlayerState state)
        {
            PlayerCommandFeedback feedback =
                state.LatestCommandFeedbackValue;

            if (!feedback.HasValue)
            {
                SetText(
                    feedbackText,
                    "Latest result: None"
                );

                return;
            }

            string status =
                feedback.Status ==
                PlayerCommandFeedbackStatus.Accepted
                    ? "Accepted"
                    : "Rejected";

            SetText(
                feedbackText,
                $"Latest result: {status} — " +
                $"{feedback.Message}"
            );
        }

        private void ShowConnectingState()
        {
            _actionController = null;
            _releaseCount = 0;
            _selectedReleaseId =
                string.Empty;

            SetText(
                keeperStatusText,
                "Keeper state: Connecting..."
            );

            SetText(
                targetText,
                "Release targets: Connecting..."
            );

            SetText(
                pullSpendText,
                "Requested Pull: Connecting..."
            );

            SetText(
                feedbackText,
                "Latest result: None"
            );

            DisableAllControls();
        }

        private void ShowUnavailableState(
            NetPlayerState state,
            string reason)
        {
            _releaseCount = 0;
            _selectedReleaseId =
                string.Empty;

            SetText(
                keeperStatusText,
                "Keeper state unavailable."
            );

            SetText(
                targetText,
                reason
            );

            SetText(
                pullSpendText,
                "Requested Pull: Unavailable"
            );

            RefreshFeedback(state);
            DisableAllControls();
        }

        private void DisableAllControls()
        {
            SetTargetNavigationInteractable(
                false
            );

            SetInterventionButtons(
                boostAvailable: false,
                suppressAvailable: false,
                boostLabel:
                    "Boost\nUnavailable",
                suppressLabel:
                    "Suppress\nUnavailable"
            );

            if (pullSpendSlider != null)
            {
                pullSpendSlider.interactable =
                    false;
            }
        }

        private void SetTargetNavigationInteractable(
            bool interactable)
        {
            if (previousTargetButton != null)
            {
                previousTargetButton.interactable =
                    interactable;
            }

            if (nextTargetButton != null)
            {
                nextTargetButton.interactable =
                    interactable;
            }
        }

        private void SetInterventionButtons(
            bool boostAvailable,
            bool suppressAvailable,
            string boostLabel,
            string suppressLabel)
        {
            if (boostButton != null)
            {
                boostButton.interactable =
                    boostAvailable;
            }

            if (suppressButton != null)
            {
                suppressButton.interactable =
                    suppressAvailable;
            }

            SetText(
                _boostButtonText,
                boostLabel
            );

            SetText(
                _suppressButtonText,
                suppressLabel
            );
        }

        private static string FormatAvailability(
            bool available)
        {
            return available
                ? "Available"
                : "Used or unavailable";
        }

        private static float RoundToHundredth(
            float value)
        {
            return
                Mathf.Round(value * 100.0f) /
                100.0f;
        }

        private static void SetText(
            TextMeshProUGUI target,
            string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}