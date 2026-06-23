using System.Text;
using SEMM91.Networking.DebugSnapshots;
using TMPro;
using UnityEngine;

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

        public override bool Supports(
            GameUIState state)
        {
            return
                state == GameUIState.Rehearsal;
        }

        public override void Refresh(
            UIContext context)
        {
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