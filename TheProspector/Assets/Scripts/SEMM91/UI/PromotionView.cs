using SEMM91.Networking.DebugSnapshots;
using TMPro;
using UnityEngine;

namespace SEMM91.UI
{
    public class PromotionView :
        ContextualUIView
    {
        [Header("Promotion State")]
        [SerializeField]
        private TextMeshProUGUI
            candidateText;

        [SerializeField]
        private TextMeshProUGUI
            sourceSetText;

        [SerializeField]
        private TextMeshProUGUI
            recordingDetailsText;

        [SerializeField]
        private TextMeshProUGUI
            promotionStatusText;

        public override bool Supports(
            GameUIState state)
        {
            return
                state ==
                GameUIState.Promotion;
        }

        public override void Refresh(
            UIContext context)
        {
            if (!context
                    .HasLocalInventorySnapshot)
            {
                ShowSynchronizingState();
                return;
            }

            DomainSnapshotReplicator
                .PlayerInventoryDebugRow row =
                    context
                        .LocalInventorySnapshot;

            if (!row.HasPromotableDemo)
            {
                ShowNoCandidateState(
                    row.DemoTapeCount
                );

                return;
            }

            string demoName =
                row.PromotableDemoName
                    .ToString();

            string sourceSetName =
                row.PromotableDemoSourceSetName
                    .ToString();

            int conveyancePercent =
                Mathf.RoundToInt(
                    row
                        .PromotableDemoAverageConveyance *
                    100.0f
                );

            SetText(
                candidateText,
                $"Promotion candidate: " +
                $"{demoName}"
            );

            SetText(
                sourceSetText,
                $"Recorded from: " +
                $"{sourceSetName}"
            );

            SetText(
                recordingDetailsText,
                $"Tracks: " +
                $"{row.PromotableDemoTrackCount} | " +
                $"Takes: " +
                $"{row.PromotableDemoTakeCount} | " +
                $"Average conveyance: " +
                $"{conveyancePercent}% | " +
                $"Recorded turn: " +
                $"{row.PromotableDemoRecordedTurn}"
            );

            SetText(
                promotionStatusText,
                "Status: Ready for release " +
                "into KVLT"
            );
        }

        private void ShowNoCandidateState(
            int totalDemoCount)
        {
            SetText(
                candidateText,
                "Promotion candidate: None"
            );

            SetText(
                sourceSetText,
                "Recorded from: —"
            );

            SetText(
                recordingDetailsText,
                $"Recorded demos: " +
                $"{totalDemoCount}"
            );

            SetText(
                promotionStatusText,
                totalDemoCount == 0
                    ? "Status: Record a demo " +
                      "before promoting"
                    : "Status: No unreleased " +
                      "demo is available"
            );
        }

        private void ShowSynchronizingState()
        {
            SetText(
                candidateText,
                "Promotion candidate: " +
                "Synchronizing..."
            );

            SetText(
                sourceSetText,
                "Recorded from: " +
                "Synchronizing..."
            );

            SetText(
                recordingDetailsText,
                "Recording details: " +
                "Synchronizing..."
            );

            SetText(
                promotionStatusText,
                "Status: Synchronizing..."
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