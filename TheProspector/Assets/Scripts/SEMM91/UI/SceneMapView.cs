using System.Text;
using SEMM91.Networking.DebugSnapshots;
using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class SceneMapView : PersistentUIView
    {
        [Header("Scene Output")]
        [SerializeField]
        private TextMeshProUGUI sceneSummaryText;

        [SerializeField]
        private TextMeshProUGUI dominantOutputText;

        [SerializeField]
        private TextMeshProUGUI standingsText;

        private int _lastRenderedSnapshotVersion =
            int.MinValue;

        // public override bool Supports(
        //     GameUIState state)
        // {
        //     return state == GameUIState.SceneMap;
        // }

        private void OnEnable()
        {
            _lastRenderedSnapshotVersion =
                int.MinValue;
        }

        public override void Refresh(
            UIContext context)
        {
            DomainSnapshotReplicator snapshot =
                DomainSnapshotReplicator.Instance;

            if (snapshot == null ||
                !snapshot.IsSnapshotNetworkReady)
            {
                _lastRenderedSnapshotVersion =
                    int.MinValue;

                ShowSynchronizingState();
                return;
            }

            int snapshotVersion =
                snapshot.SnapshotVersion.Value;

            if (snapshotVersion ==
                _lastRenderedSnapshotVersion)
            {
                return;
            }

            _lastRenderedSnapshotVersion =
                snapshotVersion;

            RenderSceneOutput(snapshot);
        }

        private void RenderSceneOutput(
            DomainSnapshotReplicator snapshot)
        {
            int standingCount =
                snapshot.SceneOutputRows.Count;

            int totalHostedReleases = 0;
            string dominantOwnerName = null;

            StringBuilder builder =
                new StringBuilder(
                    "Scene output standings:"
                );

            for (int i = 0;
                 i < standingCount;
                 i++)
            {
                DomainSnapshotReplicator
                    .SceneOutputDebugRow row =
                        snapshot.SceneOutputRows[i];

                string ownerName =
                    row.OwnerName.ToString();

                if (string.IsNullOrWhiteSpace(
                        ownerName))
                {
                    ownerName =
                        row.PlayerIndex >= 0
                            ? $"Player {row.PlayerIndex}"
                            : "Unknown";
                }

                totalHostedReleases +=
                    row.HostedReleaseCount;

                builder.Append(
                    $"\n{i + 1}. {ownerName}"
                );

                builder.Append(
                    $" — Releases: " +
                    $"{row.HostedReleaseCount}"
                );

                builder.Append(
                    $" | Output: " +
                    $"{row.AccumulatedSceneOutput:0.##}"
                );

                if (row.IsDominantOwner)
                {
                    builder.Append(
                        " [DOMINANT]"
                    );

                    dominantOwnerName =
                        ownerName;
                }
            }

            if (standingCount == 0)
            {
                builder.Append(
                    "\nNo scene output has been " +
                    "evaluated yet."
                );
            }

            SetText(
                sceneSummaryText,
                $"Hosted releases: " +
                $"{totalHostedReleases} | " +
                $"Ranked owners: {standingCount}"
            );

            SetText(
                dominantOutputText,
                dominantOwnerName != null
                    ? $"Dominant scene output: " +
                      dominantOwnerName
                    : "Dominant scene output: None"
            );

            SetText(
                standingsText,
                builder.ToString()
            );
        }

        private void ShowSynchronizingState()
        {
            SetText(
                sceneSummaryText,
                "Scene output: Synchronizing..."
            );

            SetText(
                dominantOutputText,
                "Dominant scene output: " +
                "Synchronizing..."
            );

            SetText(
                standingsText,
                "Scene output standings: " +
                "Synchronizing..."
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