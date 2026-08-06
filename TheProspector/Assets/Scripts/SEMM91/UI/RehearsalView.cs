using System.Text;
using SEMM91.Networking;
using SEMM91.Networking.DebugSnapshots;
using SEMM91.InputSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public class RehearsalView : ContextualUIView
    {
        [Header("Rehearsal State")]
        [SerializeField]
        private TextMeshProUGUI activeSetText;

        [SerializeField]
        private TextMeshProUGUI emptySetStatusText;

        [SerializeField]
        private TextMeshProUGUI setListText;

        [SerializeField]
        private TextMeshProUGUI activeSetTracksText;

        [Header("Rehearsal Controls")]
        [SerializeField]
        private Button contextualCreateButton;

        private TextMeshProUGUI
            _contextualCreateButtonText;

        private PlayerActionController
            _actionController;
        
        public override bool Supports(
            GameUIState state)
        {
            return
                state == GameUIState.Rehearsal;
        }

        private void Awake()
        {
            if (contextualCreateButton == null)
                return;

            _contextualCreateButtonText =
                contextualCreateButton
                    .GetComponentInChildren<
                        TextMeshProUGUI>(true);

            contextualCreateButton.onClick.AddListener(
                RequestContextualCreate
            );
        }

        private void OnDestroy()
        {
            if (contextualCreateButton != null)
            {
                contextualCreateButton.onClick.RemoveListener(
                    RequestContextualCreate
                );
            }
        }
        
        public override void Refresh(
            UIContext context)
        {
            NetPlayerState state =
                context.LocalPlayerState;

            _actionController =
                state != null
                    ? state.GetComponent<
                        PlayerActionController>()
                    : null;

            RefreshContextualCreateButton();
            
            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null ||
                !snapshot.IsSnapshotNetworkReady ||
                context.LocalClientId ==
                    ulong.MaxValue)
            {
                ShowConnectingState();
                return;
            }

            RefreshRehearsalState(
                snapshot,
                context.LocalClientId
            );
        }

        private void RefreshContextualCreateButton()
        {
            if (contextualCreateButton == null)
                return;

            if (_actionController == null)
            {
                contextualCreateButton.interactable =
                    false;

                SetText(
                    _contextualCreateButtonText,
                    "CREATE\nCONNECTING"
                );

                return;
            }

            PlayerActionPresentation presentation =
                _actionController.GetPresentation(
                    PlayerCommand.ContextualCreate
                );

            contextualCreateButton.interactable =
                presentation.IsAvailable;

            if (presentation.IsAvailable)
            {
                SetText(
                    _contextualCreateButtonText,
                    presentation.Label
                        .ToUpperInvariant()
                );

                return;
            }

            string unavailableLabel =
                presentation.UnavailableReason switch
                {
                    ActionUnavailableReason
                            .MissingActiveRehearsalSet =>
                        "NO ACTIVE SET",

                    ActionUnavailableReason.PlayerInactive =>
                        "INACTIVE",

                    ActionUnavailableReason
                            .TurnAlreadyCommitted =>
                        "COMMITTED",

                    ActionUnavailableReason
                            .MissingPlayerState =>
                        "CONNECTING",

                    ActionUnavailableReason
                            .MissingCoordinator =>
                        "UNAVAILABLE",

                    _ =>
                        "UNAVAILABLE"
                };

            SetText(
                _contextualCreateButtonText,
                $"{presentation.Label.ToUpperInvariant()}\n" +
                unavailableLabel
            );
        }

        private void RequestContextualCreate()
        {
            if (_actionController == null)
                return;

            if (!_actionController.CanRequest(
                    PlayerCommand.ContextualCreate
                ))
            {
                return;
            }

            _actionController.Request(
                PlayerCommand.ContextualCreate
            );
        }
        
        private void RefreshRehearsalState(
            DomainSnapshotReplicator snapshot,
            ulong localClientId)
        {
            StringBuilder setBuilder =
                new StringBuilder(
                    "Available rehearsal sets:"
                );

            int localSetCount = 0;
            int activeSetIndex = -1;
            string activeSetName = null;
            bool hasEmptySet = false;

            for (int i = 0;
                 i < snapshot.RehearsalSetRows.Count;
                 i++)
            {
                DomainSnapshotReplicator
                    .RehearsalSetDebugRow row =
                        snapshot.RehearsalSetRows[i];

                if (row.OwnerClientId !=
                    localClientId)
                {
                    continue;
                }

                localSetCount++;

                string setName =
                    row.DisplayName.ToString();

                string trackDescription =
                    row.TrackCount == 0
                        ? "Empty"
                        : row.TrackCount == 1
                            ? "1 track"
                            : $"{row.TrackCount} tracks";

                setBuilder.Append(
                    "\n• "
                );

                setBuilder.Append(
                    setName
                );

                setBuilder.Append(
                    " — "
                );

                setBuilder.Append(
                    trackDescription
                );

                if (row.IsActive)
                {
                    setBuilder.Append(
                        " [ACTIVE]"
                    );

                    activeSetIndex =
                        row.SetIndex;

                    activeSetName =
                        setName;
                }

                if (row.TrackCount == 0)
                {
                    hasEmptySet = true;
                }
            }

            if (localSetCount == 0)
            {
                setBuilder.Append(
                    "\nNone"
                );
            }

            SetText(
                setListText,
                setBuilder.ToString()
            );

            SetText(
                activeSetText,
                activeSetIndex >= 0
                    ? $"Active rehearsal set: " +
                      $"{activeSetName}"
                    : "Active rehearsal set: None"
            );

            SetText(
                emptySetStatusText,
                hasEmptySet
                    ? "Empty set slot: Occupied"
                    : "Empty set slot: Available"
            );

            RefreshActiveSetTracks(
                snapshot,
                localClientId,
                activeSetIndex
            );
        }

        private void RefreshActiveSetTracks(
            DomainSnapshotReplicator snapshot,
            ulong localClientId,
            int activeSetIndex)
        {
            if (activeSetIndex < 0)
            {
                SetText(
                    activeSetTracksText,
                    "Tracks in active set:\nNone"
                );

                return;
            }

            StringBuilder trackBuilder =
                new StringBuilder(
                    "Tracks in active set:"
                );

            int localTrackCount = 0;

            for (int i = 0;
                 i <
                 snapshot.RehearsalTrackRows.Count;
                 i++)
            {
                DomainSnapshotReplicator
                    .RehearsalTrackDebugRow row =
                        snapshot
                            .RehearsalTrackRows[i];

                if (row.OwnerClientId !=
                        localClientId ||
                    row.SetIndex !=
                        activeSetIndex)
                {
                    continue;
                }

                localTrackCount++;

                string lifecycle =
                    row.IsHoned
                        ? "Honed"
                        : row.IsRaw
                            ? "Raw"
                            : "Developing";

                int conveyancePercent =
                    Mathf.RoundToInt(
                        row.Conveyance * 100.0f
                    );

                trackBuilder.Append(
                    "\n• "
                );

                trackBuilder.Append(
                    row.DisplayName.ToString()
                );

                trackBuilder.Append(
                    $" — {row.IdeaCount} ideas"
                );

                trackBuilder.Append(
                    $" | Conveyance " +
                    $"{conveyancePercent}%"
                );

                trackBuilder.Append(
                    $" | Rehearsals " +
                    $"{row.RehearsalCount}"
                );

                trackBuilder.Append(
                    $" | {lifecycle}"
                );
            }

            if (localTrackCount == 0)
            {
                trackBuilder.Append(
                    "\nEmpty"
                );
            }

            SetText(
                activeSetTracksText,
                trackBuilder.ToString()
            );
        }

        private void ShowConnectingState()
        {
            SetText(
                activeSetText,
                "Active rehearsal set: " +
                "Connecting..."
            );

            SetText(
                emptySetStatusText,
                "Empty set slot: Connecting..."
            );

            SetText(
                setListText,
                "Available rehearsal sets: " +
                "Connecting..."
            );

            SetText(
                activeSetTracksText,
                "Tracks in active set: " +
                "Connecting..."
            );
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