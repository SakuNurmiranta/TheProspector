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

            RenderKvltState(snapshot);
        }

        private void RenderKvltState(
            DomainSnapshotReplicator snapshot)
        {
            var scene =
                snapshot.KvltSceneState.Value;

            StringBuilder builder =
                new StringBuilder(
                    "KVLT Standing / Score:"
                );

            for (int i = 0;
                 i < snapshot.PlayerInventoryRows.Count;
                 i++)
            {
                DomainSnapshotReplicator
                    .PlayerInventoryDebugRow row =
                        snapshot.PlayerInventoryRows[i];

                string ownerName =
                    row.DisplayName.ToString();

                if (string.IsNullOrWhiteSpace(
                        ownerName))
                {
                    ownerName =
                        row.PlayerIndex >= 0
                            ? $"Player {row.PlayerIndex}"
                            : "Unknown";
                }

                builder.Append(
                    $"\n{i + 1}. {ownerName}"
                );

                builder.Append(
                    $" — Releases: " +
                    $"{row.KvltReleaseCount}"
                );

                builder.Append(
                    $" | Score: " +
                    $"{row.KvltTotalScore:0.###}"
                );

                builder.Append(
                    row.HasKvltStanding
                        ? $" | Standing: " +
                          $"{row.KvltStanding:0.###}"
                        : " | Standing: none"
                );

                if (row.IsKvltKeeper)
                {
                    builder.Append(
                        " [KEEPER]"
                    );
                }
            }

            if (snapshot.PlayerInventoryRows.Count == 0)
            {
                builder.Append(
                    "\nNo KVLT player state has been " +
                    "published yet."
                );
            }

            SetText(
                sceneSummaryText,
                scene.HasState
                    ? $"Releases: {scene.SceneReleaseCount} | " +
                      $"Field: {scene.FieldReleaseCount} | " +
                      $"Canon: {scene.CanonRetainedCount} | " +
                      $"Historical: {scene.HistoricalCanonCount}"
                    : "KVLT state unavailable"
            );

            SetText(
                dominantOutputText,
                scene.HasState
                    ? $"Canon precedents: " +
                      $"{scene.CanonPrecedentCount} | " +
                      $"Pressure: {scene.PressureEntryCount} | " +
                      $"Normative: " +
                      $"{scene.NormativeAffinityCount}"
                    : "KVLT semantics unavailable"
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
                "KVLT state: Synchronizing..."
            );

            SetText(
                dominantOutputText,
                "KVLT semantics: Synchronizing..."
            );

            SetText(
                standingsText,
                "KVLT Standing / Score: " +
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
